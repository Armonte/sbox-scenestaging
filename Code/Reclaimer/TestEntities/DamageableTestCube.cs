using Sandbox;

namespace Reclaimer
{
	/// <summary>
	/// Test cube that can receive both damage and healing
	/// Used for testing Cork Revolver (damage) and Milk Spray (healing)
	/// </summary>
	public sealed class DamageableTestCube : Component, IReclaimerDamageable, IReclaimerHealable
	{
		[Property] public float MaxHealth { get; set; } = 100f;
		[Property] public Material DamagedMaterial { get; set; }
		[Property] public Material HealthyMaterial { get; set; }
		[Property] public SoundEvent HitSound { get; set; }
		[Property] public SoundEvent HealSound { get; set; }
		
		[Sync] public float CurrentHealth { get; set; }
		[Sync] public bool IsAlive { get; set; } = true;
		
		// IReclaimerHealable implementation
		public bool NeedsHealing => IsAlive && CurrentHealth < MaxHealth;
		
		private Material originalMaterial;
		private ModelRenderer modelRenderer;
		
		protected override void OnStart()
		{
			base.OnStart();
			
			CurrentHealth = MaxHealth;
			modelRenderer = Components.Get<ModelRenderer>();
			
			if (modelRenderer != null)
			{
				originalMaterial = modelRenderer.MaterialOverride;
			}
			
			Log.Info($"Test cube created with {MaxHealth} HP");
		}
		
		public void OnDamage(float damage, GameObject attacker = null)
		{
			Log.Info($"DamageableTestCube.OnDamage called: {damage} damage from {attacker?.Name ?? "Unknown"}");
			if (!Networking.IsHost) return;
			if (!IsAlive) return;
			
			CurrentHealth -= damage;
			CurrentHealth = Math.Max(0, CurrentHealth);
			
			// Visual feedback
			ShowDamageEffectRPC(damage, attacker?.Name ?? "Unknown");
			
			if (CurrentHealth <= 0)
			{
				Die();
			}
		}
		
		public void OnHeal(float healAmount, GameObject healer = null)
		{
			if (!Networking.IsHost) return;
			if (!IsAlive) return;
			if (CurrentHealth >= MaxHealth) return; // Already at full health
			
			float actualHealAmount = Math.Min(healAmount, MaxHealth - CurrentHealth);
			CurrentHealth += actualHealAmount;
			
			// If was dead, revive
			if (!IsAlive && CurrentHealth > 0)
			{
				IsAlive = true;
			}
			
			// Visual feedback
			ShowHealEffectRPC(actualHealAmount, healer?.Name ?? "Unknown");
		}
		
		[Rpc.Broadcast]
		void ShowHealEffectRPC(float healAmount, string healerName)
		{
			// Play heal sound
			if (HealSound != null)
			{
				Sound.Play(HealSound, WorldPosition);
			}
			
			// Update visual state
			UpdateVisualState();
			
			Log.Info($"Test cube healed by {healerName} for {healAmount:F1} HP! HP: {CurrentHealth:F1}/{MaxHealth}");
		}
		
		[Rpc.Broadcast]
		void ShowDamageEffectRPC(float damage, string attackerName)
		{
			// Play hit sound
			if (HitSound != null)
			{
				Sound.Play(HitSound, WorldPosition);
			}
			
			// Change color based on health percentage
			UpdateVisualState();
			
			Log.Info($"Test cube hit by {attackerName} for {damage} damage! HP: {CurrentHealth:F1}/{MaxHealth}");
		}
		
		void UpdateVisualState()
		{
			if (modelRenderer == null) return;
			
			float healthPercentage = CurrentHealth / MaxHealth;
			
			if (healthPercentage >= 0.8f && HealthyMaterial != null)
			{
				// Use healthy material when above 80% health (bright/glowing)
				modelRenderer.MaterialOverride = HealthyMaterial;
			}
			else if (healthPercentage < 0.5f && DamagedMaterial != null)
			{
				// Use damaged material when below 50% health (dark/cracked)
				modelRenderer.MaterialOverride = DamagedMaterial;
			}
			else
			{
				// Use original material for middle health range
				modelRenderer.MaterialOverride = originalMaterial;
			}
		}
		
		void Die()
		{
			IsAlive = false;
			DestroyTestCubeRPC();
		}
		
		[Rpc.Broadcast]
		void DestroyTestCubeRPC()
		{
			Log.Info("Test cube destroyed!");
			
			// Create simple destruction effect
			CreateDestructionEffect();
			
			// For now, just mark as destroyed (TODO: add respawn system)
			Log.Info("Test cube destroyed - respawn manually using 'Heal to Full' button");
		}
		
		void CreateDestructionEffect()
		{
			// Create simple particle effect or explosion
			var effect = Scene.CreateObject();
			effect.WorldPosition = WorldPosition;
			effect.Name = "CubeDestructionEffect";
			
			// Add simple visual feedback (could be enhanced with particles)
			// For now just log the destruction
			
			// Self-destruct effect after short time
			var timer = effect.Components.GetOrCreate<TimedDestroy>();
			timer.DestroyAfter(2f);
		}
		
		// Debug methods for testing
		[Button("Take 25 Damage")]
		void TakeDamage25()
		{
			OnDamage(25f, GameObject);
		}
		
		[Button("Heal 25 HP")]
		void Heal25()
		{
			OnHeal(25f, GameObject);
		}
		
		[Button("Heal to Full")]
		void HealToFull()
		{
			OnHeal(MaxHealth, GameObject);
		}
		
		[Button("Kill Instantly")]
		void KillInstantly()
		{
			OnDamage(CurrentHealth, GameObject);
		}
		
		[Button("Revive")]
		void Revive()
		{
			if (!Networking.IsHost) return;
			IsAlive = true;
			CurrentHealth = MaxHealth * 0.5f; // Revive with 50% health
			UpdateVisualState();
			Log.Info("Test cube revived with 50% health!");
		}
	}
}