using Sandbox;
using System;
using System.Linq;

namespace Reclaimer
{
	/// <summary>
	/// Milk Spray - Cone-shaped healing spray for Abby
	/// Heals allies in cone with distance falloff, consumes milk resource
	/// Based on Moira-style healing mechanics from healing spray spec
	/// </summary>
	public sealed class MilkSpray : Component
	{
		[Property] public float ConeAngle { get; set; } = 60f;
		[Property] public float MaxRange { get; set; } = 500f;
		[Property] public float MaxHealPerSecond { get; set; } = 150f;
		[Property] public float MilkUsagePerSecond { get; set; } = 20f;
		[Property] public SoundEvent SpraySound { get; set; }
		[Property] public GameObject SprayEffectPrefab { get; set; }
		
		private AbbyHealer owner;
		private bool isSpraying = false;
		private GameObject activeSprayEffect;
		
		protected override void OnStart()
		{
			base.OnStart();
			owner = Components.Get<AbbyHealer>();
			
			if (owner == null)
			{
				Log.Warning("MilkSpray: No AbbyHealer component found!");
			}
		}
		
		protected override void OnUpdate()
		{
			if (IsProxy) return; // Only handle input on authoritative client
			
			// Check for Attack2 input (milk spray)
			if (Input.Down("Attack2"))
			{
				if (CanStartSpray())
				{
					if (!isSpraying)
					{
						StartSprayRPC();
					}
					PerformHealSprayRPC();
				}
				else if (isSpraying)
				{
					StopSprayRPC();
				}
			}
			else if (isSpraying)
			{
				StopSprayRPC();
			}
		}
		
		bool CanStartSpray()
		{
			return owner != null && 
				   owner.CurrentMilk > 0 && 
				   owner.IsAlive;
		}
		
		[Rpc.Broadcast]
		void StartSprayRPC()
		{
			if (!Networking.IsHost) return;
			
			isSpraying = true;
			
			// Create spray visual effect
			CreateSprayEffect();
			
			// Play spray sound
			if (SpraySound != null)
			{
				Sound.Play(SpraySound, WorldPosition);
			}
			
			Log.Info("Abby starts milk spray healing!");
		}
		
		[Rpc.Broadcast]
		void StopSprayRPC()
		{
			if (!Networking.IsHost) return;
			
			isSpraying = false;
			
			// Remove spray effect
			DestroySprayEffect();
			
			Log.Info("Abby stops milk spray");
		}
		
		[Rpc.Broadcast]
		void PerformHealSprayRPC()
		{
			if (!Networking.IsHost) return;
			if (owner == null) return;
			
			// Calculate milk consumption this tick
			float milkNeeded = MilkUsagePerSecond * Time.Delta;
			if (!owner.ConsumeMilk(milkNeeded))
			{
				// Not enough milk, stop spraying
				StopSprayRPC();
				return;
			}
			
			// Get spray origin and direction
			Vector3 origin = GameObject.WorldPosition;
			Vector3 forward = GameObject.WorldRotation.Forward;
			
			// Use owner's eye position and angles if available
			if (owner.Eye != null)
			{
				origin = owner.Eye.WorldPosition;
				forward = owner.EyeAngles.ToRotation().Forward;
			}
			
			// Find all healable entities in range (players + test objects)
			var allHealableEntities = Scene.GetAllComponents<IReclaimerHealable>()
				.Where(h => h.IsAlive && h.NeedsHealing); // Only alive entities that need healing
			int healsApplied = 0;
			
			foreach (var healableEntity in allHealableEntities)
			{
				// Get the Component's GameObject position
				var component = healableEntity as Component;
				if (component == null) continue;
				
				// Calculate direction and distance to target
				Vector3 toTarget = component.WorldPosition - origin;
				float distance = toTarget.Length;
				
				// Check if within range
				if (distance > MaxRange) continue;
				
				Vector3 dirToTarget = toTarget.Normal;
				
				// Calculate angle using dot product (more efficient than Vector3.Angle)
				float dotProduct = Vector3.Dot(forward, dirToTarget);
				float angleRadians = MathF.Acos(Math.Clamp(dotProduct, -1f, 1f));
				float angleDegrees = angleRadians * (180f / MathF.PI);
				
				// Check if target is within cone
				if (angleDegrees > ConeAngle * 0.5f) continue;
				
				// Calculate healing with distance falloff
				float falloff = 1f - (distance / MaxRange);
				float healThisTick = MaxHealPerSecond * falloff * Time.Delta;
				
				// Apply healing using the interface
				healableEntity.OnHeal(healThisTick, owner?.GameObject);
				healsApplied++;
				
				// Create heal effect on target (optional)
				CreateHealEffect(component.WorldPosition);
			}
			
			if (healsApplied > 0)
			{
				Log.Info($"Milk spray healed {healsApplied} entities");
			}
		}
		
		void CreateSprayEffect()
		{
			if (SprayEffectPrefab != null && SprayEffectPrefab.IsValid())
			{
				activeSprayEffect = SprayEffectPrefab.Clone();
				activeSprayEffect.WorldPosition = GameObject.WorldPosition;
				activeSprayEffect.WorldRotation = GameObject.WorldRotation;
				
				// Parent to this GameObject so it follows
				activeSprayEffect.SetParent(GameObject, false);
			}
			else
			{
				// Create simple spray effect GameObject as fallback
				activeSprayEffect = Scene.CreateObject();
				activeSprayEffect.Name = "MilkSprayEffect";
				activeSprayEffect.WorldPosition = GameObject.WorldPosition;
				activeSprayEffect.WorldRotation = GameObject.WorldRotation;
				activeSprayEffect.SetParent(GameObject, false);
				
				// TODO: Add particle system or other visual effects
			}
		}
		
		void DestroySprayEffect()
		{
			if (activeSprayEffect != null && activeSprayEffect.IsValid())
			{
				activeSprayEffect.Destroy();
				activeSprayEffect = null;
			}
		}
		
		void CreateHealEffect(Vector3 position)
		{
			// Create temporary healing effect at target position
			var healEffect = Scene.CreateObject();
			healEffect.WorldPosition = position;
			healEffect.Name = "MilkHealEffect";
			
			// TODO: Add healing particle effect or visual feedback
			
			// Self-destruct after short time
			var timer = healEffect.Components.GetOrCreate<TimedDestroy>();
			timer.DestroyAfter(1f);
		}
		
		protected override void OnDestroy()
		{
			base.OnDestroy();
			
			// Clean up spray effect
			DestroySprayEffect();
		}
		
		// Public getters for debugging/UI
		public bool IsSpraying => isSpraying;
		public float CurrentRange => MaxRange;
		public float CurrentAngle => ConeAngle;
	}
}