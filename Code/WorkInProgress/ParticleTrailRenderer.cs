﻿
namespace Sandbox;

[Title( "Particle Trail Renderer" )]
[Category( "Particles" )]
[Icon( "category" )]
public sealed class ParticleTrailRenderer : ParticleController, Component.ExecuteInEditor
{
	[Group( "Trail" )]
	[Property] public int MaxPoints { get; set; } = 64;

	[Group( "Trail" )]
	[Property] public float PointDistance { get; set; } = 8;

	[Group( "Trail" )]
	[Property] public float LifeTime { get; set; } = 2;

	[Group( "Appearance" )]
	[Property] public TrailTextureConfig Texturing { get; set; } = TrailTextureConfig.Default;

	[Group( "Appearance" )]
	[Property] public Gradient Color { get; set; } = global::Color.Cyan;

	[Group( "Appearance" )]
	[Property] public Curve Width { get; set; } = 5;

	[Group( "Particles" )]
	[Property] public bool TintFromParticle { get; set; } = true;

	[Group( "Particles" )]
	[Property] public bool ScaleFromParticle { get; set; } = true;

	[Group( "Rendering" )]
	[Property] public bool Wireframe { get; set; }

	[Group( "Rendering" )]
	[Property] public bool Opaque { get; set; } = true;

	[ShowIf( "Opaque", true )]
	[Group( "Rendering" )]
	[Property] public bool CastShadows { get; set; } = false;

	[ShowIf( "Opaque", false )]
	[Group( "Rendering" )]
	[Property] public BlendMode BlendMode { get; set; } = BlendMode.Normal;

	List<SceneTrailObject> adopted = new();

	protected override void OnParticleCreated( Particle p )
	{
		p.AddListener( new ParticleModel( this ), this );
	}

	// If we have to do this where we want to wait for a SceneSystem effect to finish before 
	// destroying it more than once, we should really think about the ability to add them
	// to a system that runs through them and does it for us.


	internal void Adopt( SceneTrailObject obj )
	{
		lock ( adopted )
		{
			adopted.Add( obj );
		}
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		Sandbox.Utility.Parallel.ForEach( adopted, p =>
		{
			p.AdvanceTime( Time.Delta );
			p.Build();

		} );

		adopted.RemoveAll( TryDelete );
	}

	static bool TryDelete( SceneTrailObject obj )
	{
		if ( !obj.IsEmpty ) return false;

		obj.Delete();
		return true;
	}

	protected override void OnEnabled()
	{
		base.OnEnabled();

		adopted = new();
	}

	protected override void OnDisabled()
	{
		base.OnDisabled();

		foreach ( var obj in adopted )
		{
			obj.Delete();
		}

		adopted.Clear();
	}
}


class ParticleModel : Particle.BaseListener
{
	public ParticleTrailRenderer Renderer;

	SceneTrailObject so;

	public ParticleModel( ParticleTrailRenderer renderer )
	{
		Renderer = renderer;
	}

	public override void OnEnabled( Particle p )
	{
		so = new SceneTrailObject( Renderer.Scene.SceneWorld );
		so.MaxPoints = Renderer.MaxPoints;
		so.PointDistance = Renderer.PointDistance;
		so.LifeTime = Renderer.LifeTime;
		so.Texturing = Renderer.Texturing;
		so.TrailColor = Renderer.Color;
		so.Width = Renderer.Width;
		so.Flags.CastShadows = Renderer.Opaque && Renderer.CastShadows;
		so.Opaque = Renderer.Opaque;
		so.BlendMode = Renderer.BlendMode;
		so.Wireframe = Renderer.Wireframe;
	}

	public override void OnDisabled( Particle p )
	{
		if ( so is null ) return;

		if ( so.IsEmpty || !Renderer.Active )
		{
			so.Delete();
		}
		else
		{
			Renderer.Adopt( so );
		}
	}

	public override void OnUpdate( Particle p, float dt )
	{
		if ( so is null ) return;

		if ( Renderer.TintFromParticle )
			so.LineTint = p.Color;

		if ( Renderer.ScaleFromParticle )
			so.LineScale = p.Size.x;

		so.Transform = new Transform( p.Position );
		so.TryAddPosition( p.Position );
		so.AdvanceTime( Time.Delta );
		so.Build();
	}
}
