using Sandbox;

namespace Reclaimer
{
	/// <summary>
	/// Simple health indicator that shows health as floating text above objects
	/// Automatically follows the target GameObject and updates health display
	/// </summary>
	public sealed class WorldHealthBar : Component
	{
		[Property] public GameObject Target { get; set; }
		[Property] public Vector3 Offset { get; set; } = new Vector3(0, 0, 50f); // Offset above target
		[Property] public float MaxHealth { get; set; } = 100f;
		[Property] public float CurrentHealth { get; set; } = 100f;
		[Property] public bool AutoFindTarget { get; set; } = true; // Auto-find damageable on same GameObject
		[Property] public bool HideWhenFull { get; set; } = true; // Hide when at full health
		
		private IReclaimerDamageable damageable;
		private GameObject healthDisplay;
		private TextRenderer healthText;
		
		protected override void OnStart()
		{
			base.OnStart();
			
			// Auto-find target if not set
			if (Target == null && AutoFindTarget)
			{
				Target = GameObject;
			}
			
			// Find damageable component on target
			if (Target != null)
			{
				damageable = Target.Components.Get<IReclaimerDamageable>();
				if (damageable is DamageableTestCube testCube)
				{
					MaxHealth = testCube.MaxHealth;
				}
			}
			
			// Create health display text
			CreateHealthDisplay();
		}
		
		protected override void OnUpdate()
		{
			if (Target == null || !Target.IsValid()) return;
			
			// Update health from target
			UpdateHealthFromTarget();
			
			// Update position
			UpdatePosition();
			
			// Update visibility
			UpdateVisibility();
			
			// Update text display
			UpdateHealthDisplay();
		}
		
		void CreateHealthDisplay()
		{
			healthDisplay = Scene.CreateObject();
			healthDisplay.Name = "HealthDisplay";
			healthDisplay.Parent = GameObject;
			
			healthText = healthDisplay.Components.GetOrCreate<TextRenderer>();
			healthText.Text = $"{CurrentHealth:F0}/{MaxHealth:F0}";
		}
		
		void UpdatePosition()
		{
			if (healthDisplay != null && Target != null)
			{
				healthDisplay.WorldPosition = Target.WorldPosition + Offset;
				// Face camera
				if (Scene.Camera != null)
				{
					var direction = (Scene.Camera.WorldPosition - healthDisplay.WorldPosition).Normal;
					healthDisplay.WorldRotation = Rotation.LookAt(direction);
				}
			}
		}
		
		void UpdateVisibility()
		{
			if (healthDisplay != null)
			{
				bool shouldShow = !HideWhenFull || CurrentHealth < MaxHealth;
				healthDisplay.Enabled = shouldShow;
			}
		}
		
		void UpdateHealthDisplay()
		{
			if (healthText != null)
			{
				healthText.Text = $"{CurrentHealth:F0}/{MaxHealth:F0}";
				// Note: TextRenderer color changes not supported in this S&Box version
			}
		}
		
		void UpdateHealthFromTarget()
		{
			if (damageable is DamageableTestCube testCube)
			{
				CurrentHealth = testCube.CurrentHealth;
				MaxHealth = testCube.MaxHealth;
			}
		}
		
	}
}