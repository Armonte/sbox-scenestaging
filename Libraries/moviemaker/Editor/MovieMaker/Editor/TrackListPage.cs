﻿using System.Collections.Immutable;
using Sandbox.MovieMaker;
using System.Linq;
using System.Reflection;
using Sandbox.UI;

namespace Editor.MovieMaker;

#nullable enable

/// <summary>
/// Lists tracks in the current movie, and allows you to add or remove them.
/// </summary>
public sealed class TrackListPage : Widget, IListPanelPage
{
	public Session Session { get; }

	private SceneEditorSession SceneEditorSession { get; }

	public ToolBarItemDisplay Display { get; } = new( "Track List", "list_alt",
		"Lists tracks in the current movie, and allows you to add or remove them." );

	public IEnumerable<TrackWidget> RootTracks => _rootTracks;
	public IEnumerable<TrackWidget> Tracks => RootTracks.SelectMany( EnumerateDescendants );

	private static IEnumerable<TrackWidget> EnumerateDescendants( TrackWidget track ) =>
		[track, ..track.Children.SelectMany( EnumerateDescendants )];

	private TrackListView? _trackList;
	private readonly SynchronizedSet<TrackView, TrackWidget> _rootTracks;

	private readonly Widget _trackContainer;
	private readonly Widget _dragTarget;
	private Widget? _placeholder;

	public TrackListPage( ListPanel parent, Session session )
		: base( parent )
	{
		Session = session;

		SceneEditorSession = SceneEditorSession.Resolve( Session.Player.Scene );
		SceneEditorSession.Selection.OnItemAdded += OnSelectionAdded;

		_trackContainer = new Widget( this )
		{
			Layout = Layout.Column(),
			FixedWidth = Width
		};

		_trackContainer.Layout.Margin = new Margin( 4f, 0f );

		_dragTarget = new DragTargetWidget( this ) { FixedWidth = Width };

		_rootTracks = new SynchronizedSet<TrackView, TrackWidget>(
			AddRootTrack, RemoveRootTrack, UpdateChildTrack );

		Session.ViewChanged += Session_ViewChanged;

		Load( Session.TrackList );
	}

	private TrackWidget AddRootTrack( TrackView source ) => _trackContainer.Layout.Add( new TrackWidget( this, null, source ) );
	private void RemoveRootTrack( TrackWidget item ) => item.Destroy();
	private bool UpdateChildTrack( TrackView source, TrackWidget item ) => item.UpdateLayout();

	private void OnSelectionAdded( object item )
	{
		if ( Tracks.Any( x => x.IsFocused ) || Session.Editor.TimelinePanel?.Timeline.IsFocused is not true ) return;
		if ( item is not GameObject go ) return;
		if ( Tracks.FirstOrDefault( x => x.View.Target is ITrackReference<GameObject> { IsBound: true } target && target.Value == go ) is not { } track ) return;
		
		track.Focus( false );

		if ( Parent is ScrollArea scrollArea )
		{
			scrollArea.MakeVisible( track );
		}
	}

	public override void OnDestroyed()
	{
		if ( _trackList is not null )
		{
			_trackList.Changed -= TrackList_Changed;
		}

		Session.ViewChanged -= Session_ViewChanged;
		SceneEditorSession.Selection.OnItemAdded -= OnSelectionAdded;
	}

	protected override void OnWheel( WheelEvent e )
	{
		Session.TrackListScrollPosition -= e.Delta / 5f;
		e.Accept();
	}

	private Vector2 _lastMouseScreenPos;

	protected override void OnMousePress( MouseEvent e )
	{
		base.OnMousePress( e );

		_lastMouseScreenPos = e.ScreenPosition;
	}

	protected override void OnMouseMove( MouseEvent e )
	{
		base.OnMouseMove( e );

		var delta = e.ScreenPosition - _lastMouseScreenPos;

		if ( e.ButtonState == MouseButtons.Middle )
		{
			Session.TrackListScrollPosition -= delta.y;
			e.Accepted = true;
		}

		_lastMouseScreenPos = e.ScreenPosition;
	}

	private void Load( TrackListView trackList )
	{
		if ( _trackList == trackList ) return;

		if ( _trackList is not null )
		{
			_trackList.Changed -= TrackList_Changed;
		}

		_trackList = trackList;
		_trackList.Changed += TrackList_Changed;

		TrackList_Changed( trackList );
	}

	private void Session_ViewChanged()
	{
		_dragTarget.Position = 0f;
		_dragTarget.FixedSize = new Vector2( Width, 64f );

		if ( _rootTracks.Count == 0 )
		{
			_trackContainer.Position = 0f;
			_trackContainer.FixedSize = Size;
			return;
		}

		_trackContainer.Position = new Vector2( 0f, Session.TrackListScrollOffset - Session.TrackListScrollPosition - ListPanel.TitleHeight );
		_trackContainer.FixedWidth = Width;
		_trackContainer.FixedHeight = _rootTracks
			.Select( x => x.View.Position + x.View.Height + Timeline.RootTrackSpacing )
			.DefaultIfEmpty( 64f )
			.Max();
	}

	private void TrackList_Changed( TrackListView trackList )
	{
		_placeholder?.Destroy();
		_rootTracks.Update( trackList.RootTracks );

		_trackContainer.Layout.Clear( false );

		foreach ( var track in _rootTracks )
		{
			_trackContainer.Layout.Add( track );
			_trackContainer.Layout.AddSpacingCell( Timeline.RootTrackSpacing );
		}

		if ( _rootTracks.Count == 0 )
		{
			CreatePlaceholder();
		}

		Session_ViewChanged();
	}

	private void CreatePlaceholder()
	{
		var row = _trackContainer.Layout.AddRow();

		row.Margin = 32f;

		_placeholder = new Label( "Drag a <b>GameObject</b>, <b>Component</b>, <b>MovieResource</b> or <b>inspector property</b> here to create a track." )
		{
			Alignment = TextFlag.Center | TextFlag.WordWrap,
			WordWrap = true
		};

		row.Add( _placeholder );
	}

	internal static bool HasDraggedTracks( DragData data )
	{
		if ( data.OfType<GameObject>().Any() ) return true;
		if ( data.OfType<Component>().Any() ) return true;

		if ( data.OfType<SerializedProperty>().FirstOrDefault() is { } property )
		{
			if ( property.Parent.Targets?.FirstOrDefault() is Component parentComponent )
			{
				return true;
			}

			return false;
		}

		if ( data.Assets.FirstOrDefault( x => x.AssetPath?.EndsWith( ".movie" ) ?? false ) is { } assetData )
		{
			var assetTask = assetData.GetAssetAsync();

			if ( !assetTask.IsCompleted ) return false;
			if ( assetTask.Result?.LoadResource<MovieResource>() is not { } resource ) return false;

			return true;
		}

		return false;
	}

	private IEnumerable<IProjectTrack> CreateDraggedTracks( DragData data )
	{
		if ( data.OfType<GameObject>().FirstOrDefault() is { } go )
		{
			yield return Session.GetOrCreateTrack( go );
			yield return Session.GetOrCreateTrack( go, nameof(GameObject.Enabled) );
			yield return Session.GetOrCreateTrack( go, nameof(GameObject.LocalPosition) );
			yield return Session.GetOrCreateTrack( go, nameof(GameObject.LocalRotation) );

			if ( go.GetComponent<PlayerController>() is { } controller )
			{
				yield return Session.GetOrCreateTrack( controller );
				yield return Session.GetOrCreateTrack( controller, nameof(PlayerController.EyeAngles) );
				yield return Session.GetOrCreateTrack( controller, nameof(PlayerController.WishVelocity) );
				yield return Session.GetOrCreateTrack( controller, nameof(PlayerController.IsSwimming) );
				yield return Session.GetOrCreateTrack( controller, nameof(PlayerController.IsClimbing) );
				yield return Session.GetOrCreateTrack( controller, nameof(PlayerController.IsDucking) );
			}


			if ( go.GetComponent<Rigidbody>() is { } rigidBody )
			{
				yield return Session.GetOrCreateTrack( rigidBody );
				yield return Session.GetOrCreateTrack( rigidBody, nameof(Rigidbody.Velocity) );
			}

			yield break;
		}

		if ( data.OfType<Component>().FirstOrDefault() is { } component )
		{
			yield return Session.GetOrCreateTrack( component );

			if ( component is SkinnedModelRenderer skinnedRenderer )
			{
				if ( skinnedRenderer.Parameters.Graph is { } graph )
				{
					for ( var i = 0; i < graph.ParamCount; ++i )
					{
						var paramName = graph.GetParameterName( i );

						yield return Session.GetOrCreateTrack( component, $"{nameof( SkinnedModelRenderer.Parameters )}.{paramName}" );
					}
				}

				foreach ( var morphName in skinnedRenderer.Morphs.Names )
				{
					yield return Session.GetOrCreateTrack( component, $"{nameof(SkinnedModelRenderer.Morphs)}.{morphName}" );
				}
			}

			yield break;
		}

		if ( data.OfType<SerializedProperty>().FirstOrDefault() is { } property )
		{
			if ( property.Parent.Targets?.FirstOrDefault() is Component parentComponent )
			{
				yield return Session.GetOrCreateTrack( parentComponent, property.Name );
			}

			yield break;
		}

		if ( data.Assets.FirstOrDefault( x => x.AssetPath?.EndsWith( ".movie" ) ?? false ) is { } assetData )
		{
			var assetTask = assetData.GetAssetAsync();

			if ( !assetTask.IsCompleted ) yield break;

			if ( assetTask.Result?.LoadResource<MovieResource>() is not { } resource ) yield break;

			yield return Session.GetOrCreateTrack( resource );
		}
	}

	private static PropertyInfo? DragData_Current { get; } = typeof(DragData)
		.GetProperty( "Current", BindingFlags.Static | BindingFlags.NonPublic );

	private static DragData? CurrentDrag => (DragData?)DragData_Current?.GetValue( null );

	[EditorEvent.Frame]
	private void Frame()
	{
		_dragTarget.Visible = CurrentDrag is { } data && HasDraggedTracks( data );
	}

	internal void AddTracksFromDrag( DragData data )
	{
		CreateDraggedTracks( data ).ToImmutableArray();

		_dragTarget.Hide();

		Session.TrackList.Update();
		Session.ClipModified();
	}
}

file sealed class DragTargetWidget : Widget
{
	public bool HasDrag { get; private set; }

	public new TrackListPage Parent { get; }

	public DragTargetWidget( TrackListPage parent )
		: base ( parent )
	{
		Parent = parent;

		AcceptDrops = true;

		Layout = Layout.Row();
		Layout.Margin = new Margin( 32f, 8f, 32f, 8f );
		Layout.Spacing = 8f;

		Layout.AddStretchCell();
		Layout.Add( new Icon( "playlist_add" )
		{
			Color = Theme.Green,
			PixelHeight = 32f,
			Alignment = TextFlag.RightCenter,
			FixedSize = 32f
		} );
		Layout.Add( new Label( "Drag here to create a new track." )
		{
			Color = Theme.Green,
			Alignment = TextFlag.LeftCenter | TextFlag.WordWrap, WordWrap = true
		} );
		Layout.AddStretchCell();
	}

	protected override void OnPaint()
	{
		var background = Theme.WidgetBackground;

		Paint.ClearPen();
		Paint.SetBrushLinear( LocalRect.BottomLeft, LocalRect.BottomLeft - new Vector2( 0f, 8f ), background.WithAlpha( 0f ), background );
		Paint.DrawRect( LocalRect );

		if ( HasDrag )
		{
			Paint.SetBrush( Theme.ControlBackground );
		}
		else
		{
			Paint.ClearBrush();
		}

		Paint.SetPen( Theme.Green, 2f, PenStyle.Dash );
		Paint.DrawRect( LocalRect.Shrink( 8f ), 4f );
	}

	public override void OnDragHover( DragEvent ev )
	{
		HasDrag = true;

		ev.Action = TrackListPage.HasDraggedTracks( ev.Data )
			? DropAction.Link
			: DropAction.Ignore;
	}

	public override void OnDragLeave()
	{
		HasDrag = false;
	}

	public override void OnDragDrop( DragEvent ev )
	{
		Parent.AddTracksFromDrag( ev.Data );

		HasDrag = false;
	}
}

file sealed class Icon : Widget
{
	public TextFlag Alignment { get; set; } = TextFlag.Center;
	public Color Color { get; set; } = Theme.ControlText;
	public string IconName { get; set; }
	public float PixelHeight { get; set; } = 16f;

	public Icon( string name )
	{
		IconName = name;
	}

	protected override void OnPaint()
	{
		Paint.SetPen( Color );
		Paint.DrawIcon( LocalRect, IconName, PixelHeight, Alignment );
	}
}
