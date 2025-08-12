using Sandbox;

namespace Reclaimer
{
	/// <summary>
	/// Basic HUD component - placeholder that gets replaced by class-specific HUDs
	/// </summary>
	public class SimpleGameHUD : Component
	{
		protected override void OnStart()
		{
			Log.Info("SimpleGameHUD created - this should be replaced by class-specific HUD");
		}
		
		protected override void OnDestroy()
		{
			Log.Info("SimpleGameHUD destroyed");
		}
	}
}