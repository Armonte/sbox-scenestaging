using Sandbox;

namespace Reclaimer
{
	/// <summary>
	/// Simple health display component for test objects
	/// Shows floating text with current/max health above the object
	/// </summary>
	public sealed class HealthDisplay : Component
	{
		[Property] public Vector3 TextOffset { get; set; } = new Vector3(0, 0, 50f);
		[Property] public bool HideWhenFull { get; set; } = false;
		
		private GameObject textObject;
		private TextRenderer textRenderer;
		private DamageableTestCube testCube;
		
		protected override void OnStart()
		{
			base.OnStart();
			
			// Find the test cube component
			testCube = Components.Get<DamageableTestCube>();
			if (testCube == null)
			{
				Log.Warning("HealthDisplay: No DamageableTestCube found on this GameObject");
				return;
			}
			
			CreateHealthText();
		}
		
		protected override void OnUpdate()
		{
			if (testCube == null || textRenderer == null) return;
			
			// Update visibility
			bool shouldShow = !HideWhenFull || testCube.CurrentHealth < testCube.MaxHealth;
			textObject.Enabled = shouldShow;
			
			if (!shouldShow) return;
			
			// Update text content
			textRenderer.Text = $"{testCube.CurrentHealth:F0}/{testCube.MaxHealth:F0}";
			
			// Note: TextRenderer color changes not supported in this S&Box version
			
			// Update position
			textObject.WorldPosition = WorldPosition + TextOffset;
			
			// Face the camera (fix backwards text)
			if (Scene.Camera != null)
			{
				var cameraPos = Scene.Camera.WorldPosition;
				var direction = (cameraPos - textObject.WorldPosition).Normal;
				// Use -direction to fix backwards text, and set up vector
				textObject.WorldRotation = Rotation.LookAt(-direction, Vector3.Up);
			}
		}
		
		void CreateHealthText()
		{
			textObject = Scene.CreateObject();
			textObject.Name = "HealthText";
			textObject.Parent = GameObject;
			
			textRenderer = textObject.Components.GetOrCreate<TextRenderer>();
		}
	}
}