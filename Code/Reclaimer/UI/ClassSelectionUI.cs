using Sandbox;
using Reclaimer;

namespace Reclaimer
{
	/// <summary>
	/// Component that manages showing the class selection UI for a player
	/// </summary>
	public class ClassSelectionUI : Component
	{
		public TrinitySpawnManager SpawnManager { get; set; }
		public Connection PlayerConnection { get; set; }
		
		private GameObject uiObject;
		
		protected override void OnStart()
		{
			base.OnStart();
			
			Log.Info($"ClassSelectionUI OnStart - IsProxy: {IsProxy}");
			
			// Only show UI for local player
			if (!IsProxy)
			{
				// Disable player movement
				DisablePlayerMovement();
				
				// Create a UI GameObject with ScreenPanel (like your MMO HUD setup)
				uiObject = Scene.CreateObject();
				uiObject.Name = "ClassSelectionHUD";
				
				// Add ScreenPanel component
				var screenPanel = uiObject.Components.GetOrCreate<ScreenPanel>();
				
				// Add our ClassSelectionPanel component
				var panel = uiObject.Components.GetOrCreate<ClassSelectionPanel>();
				
				Log.Info($"ClassSelectionHUD created with ScreenPanel and ClassSelectionPanel");
			}
			else
			{
				Log.Info("Skipping UI creation - this is a proxy player");
			}
		}
		
		public void RequestClassSelection(TrinityClassType classType)
		{
			Log.Info($"RequestClassSelection called with {classType}");
			Log.Info($"SpawnManager: {SpawnManager?.GameObject?.Name ?? "NULL"}");
			Log.Info($"PlayerConnection: {PlayerConnection?.DisplayName ?? "NULL"}");
			
			if (SpawnManager != null && PlayerConnection != null)
			{
				Log.Info("Calling SpawnManager.SelectClassForPlayer");
				
				// Remove the UI and re-enable movement
				CleanupUI();
				EnablePlayerMovement();
				
				// Request class selection
				SpawnManager.SelectClassForPlayer(PlayerConnection, classType);
				Log.Info($"Requested class: {classType}");
			}
			else
			{
				Log.Error($"Cannot request class selection - SpawnManager: {SpawnManager != null}, PlayerConnection: {PlayerConnection != null}");
			}
		}
		
		void DisablePlayerMovement()
		{
			// Disable CharacterController to prevent movement
			var cc = GameObject.Components.Get<CharacterController>();
			if (cc != null)
			{
				cc.Enabled = false;
				Log.Info("Player movement disabled");
			}
			
			Log.Info("Mouse cursor enabled for UI");
		}
		
		void EnablePlayerMovement()
		{
			// Re-enable CharacterController
			var cc = GameObject.Components.Get<CharacterController>();
			if (cc != null)
			{
				cc.Enabled = true;
				Log.Info("Player movement enabled");
			}
			
			Log.Info("Mouse cursor hidden for gameplay");
		}
		
		void CleanupUI()
		{
			if (uiObject != null && uiObject.IsValid())
			{
				uiObject.Destroy();
				Log.Info("Class selection UI cleaned up");
			}
		}
		
		protected override void OnDestroy()
		{
			CleanupUI();
			EnablePlayerMovement();
			base.OnDestroy();
		}
	}
}