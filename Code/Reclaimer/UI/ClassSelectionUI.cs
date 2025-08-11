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
		
		public void SetUIObject(GameObject uiObj)
		{
			uiObject = uiObj;
			Log.Info($"ClassSelectionUI UI object reference set");
		}
		
		protected override void OnStart()
		{
			base.OnStart();
			
			Log.Info($"ClassSelectionUI OnStart - IsProxy: {IsProxy}, PlayerConnection: {PlayerConnection?.DisplayName}");
			
			// Don't create UI in OnStart - it should be created by RPC only
			// The RPC will set the references and create all necessary UI components
			if (SpawnManager != null && PlayerConnection != null)
			{
				Log.Info($"ClassSelectionUI initialized for player: {PlayerConnection?.DisplayName}");
				// Disable player movement when UI is properly configured
				DisablePlayerMovement();
			}
			else
			{
				Log.Info($"ClassSelectionUI waiting for RPC configuration - SpawnManager: {SpawnManager != null}, PlayerConnection: {PlayerConnection != null}");
			}
		}
		
		public void RequestClassSelection(TrinityClassType classType)
		{
			Log.Info($"RequestClassSelection called with {classType}");
			Log.Info($"SpawnManager: {SpawnManager?.GameObject?.Name ?? "NULL"}");
			Log.Info($"PlayerConnection: {PlayerConnection?.DisplayName ?? "NULL"}");
			
			if (SpawnManager != null && PlayerConnection != null)
			{
				Log.Info("Calling SpawnManager.RequestClassSelection RPC");
				
				// Remove the UI and re-enable movement
				CleanupUI();
				EnablePlayerMovement();
				
				// Request class selection via RPC (will be handled by host)
				SpawnManager.RequestClassSelection(classType);
				Log.Info($"Requested class via RPC: {classType}");
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