using Sandbox;

namespace Reclaimer
{
	/// <summary>
	/// Simple component that destroys GameObject after specified time
	/// Based on S&Box SelfDestructComponent pattern
	/// </summary>
	public sealed class TimedDestroy : Component
	{
		private TimeUntil timeUntilDestroy;
		private bool isActive = false;
		
		public void DestroyAfter(float seconds)
		{
			timeUntilDestroy = seconds;
			isActive = true;
		}
		
		protected override void OnUpdate()
		{
			if (GameObject.IsProxy) return;
			
			if (isActive && timeUntilDestroy <= 0.0f)
			{
				GameObject.Destroy();
			}
		}
	}
}