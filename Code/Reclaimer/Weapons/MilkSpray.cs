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
			
			// Try multiple ways to find the AbbyHealer owner
			owner = Components.Get<AbbyHealer>();
			if (owner == null)
			{
				owner = Components.GetInAncestors<AbbyHealer>();
			}
			if (owner == null)
			{
				owner = Components.GetInDescendants<AbbyHealer>();
			}
			if (owner == null)
			{
				// Last resort: find in scene
				owner = Scene.GetAllComponents<AbbyHealer>()?.FirstOrDefault(a => !a.IsProxy);
			}
			
			Log.Info($"=== MILK SPRAY OWNER DETECTION ===");
			Log.Info($"MilkSpray GameObject: {GameObject.Name}");
			Log.Info($"Found AbbyHealer owner: {owner != null}");
			Log.Info($"=== MILK SPRAY PROPERTIES ===");
			Log.Info($"MaxRange: {MaxRange}, ConeAngle: {ConeAngle}, MaxHealPerSecond: {MaxHealPerSecond}");
			Log.Info($"MilkUsagePerSecond: {MilkUsagePerSecond}");
			
			if (owner != null)
			{
				Log.Info($"Owner GameObject: {owner.GameObject.Name}");
				Log.Info($"Owner CurrentMilk: {owner.CurrentMilk}");
				Log.Info($"Owner IsAlive: {owner.IsAlive}");
			}
			else
			{
				Log.Error("❌ CRITICAL: MilkSpray cannot find AbbyHealer owner!");
			}
		}
		
		protected override void OnUpdate()
		{
			if (IsProxy) return; // Only handle input on authoritative client
			
			bool isHoldingRMB = Input.Down("attack2");
			
			if (isHoldingRMB && !isSpraying && CanStartSpray())
			{
				// Start spraying
				Log.Info("🔵 STARTING milk spray");
				StartSprayRPC();
			}
			else if (isHoldingRMB && isSpraying)
			{
				// Continue spraying
				PerformHealSprayRPC();
			}
			else if (!isHoldingRMB && isSpraying)
			{
				// Stop spraying
				Log.Info("🔴 STOPPING milk spray");
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
			
			Log.Info("=== MILK SPRAY START DEBUG ===");
			Log.Info($"SprayEffectPrefab assigned: {SprayEffectPrefab != null}");
			Log.Info($"SpraySound assigned: {SpraySound != null}");
			
			// Create spray visual effect
			CreateSprayEffect();
			
			// Play spray sound
			if (SpraySound != null)
			{
				Sound.Play(SpraySound, WorldPosition);
				Log.Info("Playing milk spray sound");
			}
			else
			{
				Log.Warning("No SpraySound assigned!");
			}
			
			Log.Info("Abby starts milk spray healing!");
		}
		
		[Rpc.Broadcast]
		void StopSprayRPC()
		{
			if (!Networking.IsHost) return;
			
			isSpraying = false;
			
			// Stop emission immediately but let existing particles continue
			if (activeSprayEffect != null && activeSprayEffect.IsValid())
			{
				// Find and disable emitter components to stop new particles
				var emitters = activeSprayEffect.Components.GetAll<Component>()
					.Where(c => c.GetType().Name.Contains("Emit") || c.GetType().Name.Contains("Spawn"));
				
				foreach (var emitter in emitters)
				{
					emitter.Enabled = false; // Stop emission
				}
				
				// Delay destruction to let existing particles finish naturally
				var timedDestroy = activeSprayEffect.Components.GetOrCreate<TimedDestroy>();
				timedDestroy.DestroyAfter(3.0f);
				
				activeSprayEffect = null;
			}
			
			Log.Info("Abby stops milk spray");
		}
		
		[Rpc.Broadcast]
		void PerformHealSprayRPC()
		{
			if (!Networking.IsHost) return;
			if (owner == null) return;
			
			// Update spray effect position every frame to follow camera
			UpdateSprayEffectPosition();
			
			// Calculate milk consumption this tick
			float milkNeeded = MilkUsagePerSecond * Time.Delta;
			if (!owner.ConsumeMilk(milkNeeded))
			{
				// Not enough milk, stop spraying
				StopSprayRPC();
				return;
			}
			
			// Get spray origin and direction using MOUSE position for horizontal targeting
			Vector3 origin = owner.WorldPosition + Vector3.Up * 64f; // Chest height instead of eye level
			Vector3 forward;
			
			// Use mouse world position for horizontal spray direction
			var trinityController = owner.Components.Get<TrinityPlayerController>();
			var mousePos = trinityController?.GetMouseWorldPosition();
			if (mousePos.HasValue)
			{
				// Calculate horizontal direction from character to mouse position
				forward = (mousePos.Value - owner.WorldPosition).WithZ(0).Normal;
			}
			else
			{
				// Fallback to camera horizontal direction
				var camera = Scene.GetAllComponents<CameraComponent>().FirstOrDefault();
				forward = camera?.WorldRotation.Forward.WithZ(0).Normal ?? owner.EyeAngles.ToRotation().Forward.WithZ(0).Normal;
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
				Log.Info($"✅ Milk spray healed {healsApplied} entities");
			}
			else
			{
				Log.Info($"Milk spray found no targets to heal in range {MaxRange} with cone angle {ConeAngle}°");
			}
		}
		
		void CreateSprayEffect()
		{
			if (SprayEffectPrefab != null && SprayEffectPrefab.IsValid())
			{
				Log.Info("MilkSpray: Creating spray effect from prefab");
				activeSprayEffect = SprayEffectPrefab.Clone();
				
				// Position it at player with mouse direction
				if (owner != null)
				{
					Vector3 sprayOrigin = owner.WorldPosition + Vector3.Up * 64f; // Player chest height
					Vector3 sprayDirection;
					
					// Use mouse position for spray direction
					var trinityController = owner.Components.Get<TrinityPlayerController>();
					var mousePos = trinityController?.GetMouseWorldPosition();
					if (mousePos.HasValue)
					{
						sprayDirection = (mousePos.Value - owner.WorldPosition).WithZ(0).Normal;
					}
					else
					{
						// Fallback to camera direction
						var camera = Scene.GetAllComponents<CameraComponent>().FirstOrDefault();
						sprayDirection = camera?.WorldRotation.Forward.WithZ(0).Normal ?? owner.EyeAngles.ToRotation().Forward.WithZ(0).Normal;
					}
					
					activeSprayEffect.WorldPosition = sprayOrigin + sprayDirection * 20f;
					activeSprayEffect.WorldRotation = Rotation.LookAt(sprayDirection, Vector3.Up);
					
					Log.Info($"Prefab positioned at player: {activeSprayEffect.WorldPosition}");
				}
				else
				{
					// Fallback - don't use GameObject position as it's wrong
					activeSprayEffect.WorldPosition = owner?.WorldPosition ?? Vector3.Zero;
					activeSprayEffect.WorldRotation = owner?.WorldRotation ?? Rotation.Identity;
					Log.Warning("No camera found, using player position fallback");
				}
				
				// Don't parent it - we'll update position manually
			}
			else
			{
				Log.Warning("MilkSpray: No SprayEffectPrefab assigned! Creating simple fallback effect");
				// Create simple spray effect GameObject as fallback
				activeSprayEffect = Scene.CreateObject();
				activeSprayEffect.Name = "MilkSprayEffect_Fallback";
				
				// Get camera for proper look direction
				var camera = Scene.GetAllComponents<CameraComponent>().FirstOrDefault();
				
				Vector3 sprayOrigin;
				Vector3 sprayDirection;
				
				if (camera != null)
				{
					// Use PLAYER position but CAMERA direction (third person fix)
					sprayOrigin = owner.WorldPosition + Vector3.Up * 64f; // Player chest height
					sprayDirection = camera.WorldRotation.Forward; // Camera look direction
					Log.Info($"Using player position with camera direction - Position: {sprayOrigin}, Forward: {sprayDirection}");
				}
				else
				{
					// Fallback to player position and direction
					sprayOrigin = owner.WorldPosition + Vector3.Up * 64f; // Player eye height
					sprayDirection = owner.WorldRotation.Forward;
					Log.Info($"Using player - Position: {sprayOrigin}, Forward: {sprayDirection}");
				}
				
				// Position effect close to camera, pointing AWAY from camera (like a spray)
				activeSprayEffect.WorldPosition = sprayOrigin + sprayDirection * 20f; // Much closer
				
				if (camera != null)
				{
					activeSprayEffect.WorldRotation = camera.WorldRotation; // Use camera's exact rotation
				}
				else
				{
					activeSprayEffect.WorldRotation = owner.WorldRotation; // Use player's rotation
				}
				
				// Don't parent it, just position it in world space
				// activeSprayEffect.SetParent(owner.GameObject, false);
				
				// Add a simple ModelRenderer as visual indicator
				var renderer = activeSprayEffect.Components.GetOrCreate<ModelRenderer>();
				renderer.Model = Model.Load("models/sphere_test.vmdl"); // Use existing sphere model
				renderer.Tint = Color.Cyan; // Bright cyan for visibility
				
				// Scale it to be cone-like and more visible
				activeSprayEffect.LocalScale = new Vector3(1.0f, 3.0f, 1.0f); // Bigger elongated shape
				
				Log.Info($"Spray positioned at: {activeSprayEffect.WorldPosition}, Player at: {owner.WorldPosition}");
				
				Log.Info("✅ Created bright cyan milk spray visual fallback - should be visible!");
			}
		}
		
		void UpdateSprayEffectPosition()
		{
			// Only update if we're actively spraying and have a valid effect
			if (!isSpraying || activeSprayEffect == null || !activeSprayEffect.IsValid()) return;
			
			Vector3 sprayOrigin = owner.WorldPosition + Vector3.Up * 64f; // Player chest height
			Vector3 sprayDirection;
			
			// Use mouse position for spray direction
			var trinityController = owner.Components.Get<TrinityPlayerController>();
			var mousePos = trinityController?.GetMouseWorldPosition();
			if (mousePos.HasValue)
			{
				sprayDirection = (mousePos.Value - owner.WorldPosition).WithZ(0).Normal;
			}
			else
			{
				// Fallback to camera direction
				var camera = Scene.GetAllComponents<CameraComponent>().FirstOrDefault();
				sprayDirection = camera?.WorldRotation.Forward.WithZ(0).Normal ?? owner.EyeAngles.ToRotation().Forward.WithZ(0).Normal;
			}
			
			activeSprayEffect.WorldPosition = sprayOrigin + sprayDirection * 20f;
			activeSprayEffect.WorldRotation = Rotation.LookAt(sprayDirection, Vector3.Up);
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