using Sandbox;
using System;

namespace Reclaimer
{
	public class AbbyHealer : TrinityPlayer
	{
		[Property] public float MaxMilk { get; set; } = 100f;
		[Property] public float MilkGunHealAmount { get; set; } = 400f;
		[Property] public float MilkCostPerHeal { get; set; } = 15f;
		[Property] public float CorkGunDamage { get; set; } = 100f;
		[Property] public float MilkPerCorkHit { get; set; } = 10f;
		[Property] public float MilkSpoilTime { get; set; } = 30f;
		[Property] public float DivineSplillDuration { get; set; } = 3f;
		[Property] public float DivineSplillHealRadius { get; set; } = 500f;
		[Property] public GameObject MilkPortalPrefab { get; set; }
		
		// Weapon Components
		private CorkRevolver corkRevolver;
		private MilkSpray milkSpray;
		
		// UI Components
		private GameObject hudObject;
		
		[Sync] public float CurrentMilk { get; set; }
		[Sync] public float MilkSpoilageTimer { get; set; }
		[Sync] public bool IsCrying { get; set; }
		[Sync] public bool IsInvincible { get; set; }
		[Sync] public bool PremiumMilkActive { get; set; }
		[Sync] public bool IsPortalRecasting { get; set; } // True when first portal placed, waiting for second
		
		private float portalRecastTimer = 0f;
		private GameObject entryPortal; // Simple reference to first portal
		private GameObject exitPortal;  // Simple reference to second portal
		
		private float cryingTimer;
		private float invincibilityTimer;
		private float milksongProcChance = 0.01f;
		private float originalMovementSpeed;
		
		protected override void InitializeClass()
		{
			ClassType = TrinityClassType.Healer;
			MaxResource = MaxMilk; // Sync resource with milk property
			
			// Initialize runtime state - START WITH FULL MILK FOR TESTING
			CurrentMilk = MaxMilk; // Start with full milk instead of 0
			MilkSpoilageTimer = MilkSpoilTime;
			
			// ALL other properties set via prefab editor
		}
		
		protected override void OnStart()
		{
			base.OnStart();
			// Store original speed AFTER prefab properties are fully loaded
			originalMovementSpeed = MovementSpeed;
			
			// Find weapon components in entire hierarchy (including children)
			corkRevolver = Components.GetInDescendants<CorkRevolver>();
			milkSpray = Components.GetInDescendants<MilkSpray>();
			
			Log.Info($"=== AbbyHealer Component Hierarchy Debug ===");
			Log.Info($"AbbyHealer GameObject: {GameObject.Name}");
			Log.Info($"AbbyHealer Components: {string.Join(", ", Components.GetAll().Select(c => c.GetType().Name))}");
			
			if (corkRevolver == null)
			{
				Log.Warning("❌ No CorkRevolver found in AbbyHealer hierarchy!");
			}
			else
			{
				Log.Info($"✅ Found CorkRevolver: {corkRevolver.GameObject.Name}");
			}
			
			if (milkSpray == null)  
			{
				Log.Warning("❌ No MilkSpray found in AbbyHealer hierarchy!");
			}
			else
			{
				Log.Info($"✅ Found MilkSpray: {milkSpray.GameObject.Name}");
			}
			
			Log.Info($"Abby's weapons initialized: Cork Revolver and Milk Spray");
			
			// Create HUD for local player (non-proxy)
			if (!IsProxy)
			{
				CreateAbbyHUD();
			}
		}
		
		public override void UseAbility1()
		{
			// Ability1 = Milk Portal (Key 1) with simple recast logic
			if (CurrentMana < 40) 
			{
				return;
			}
			
			// Simple approach like Cork Gun: pass portal type as parameter
			if (!IsPortalRecasting)
			{
				PlaceMilkPortalRPC(true); // true = entry portal
				IsPortalRecasting = true;
				portalRecastTimer = 3.0f;
			}
			else if (portalRecastTimer > 0)
			{
				PlaceMilkPortalRPC(false); // false = exit portal
				IsPortalRecasting = false;
				portalRecastTimer = 0f;
			}
		}
		
		public override void UseAbility2()
		{
			// Ability2 is now handled by Milk Spray component  
			// This method kept for compatibility with TrinityPlayer base class
			Log.Info("UseAbility2 called - Milk Spray handles Attack2 input directly");
		}
		
		public override void UseUltimate()
		{
			if (CurrentMana < 80) return;
			if (IsCrying) return;
			
			PerformDivineSpillRPC();
		}
		
		/// <summary>
		/// Called by Cork Revolver when cork hits an enemy
		/// </summary>
		public void AddMilk(float amount)
		{
			CurrentMilk = Math.Min(CurrentMilk + amount, MaxMilk);
			
			// Reset spoilage timer when milk is added
			MilkSpoilageTimer = MilkSpoilTime;
			
			Log.Info($"Milk added: +{amount}. Total: {CurrentMilk}/{MaxMilk}");
		}
		
		/// <summary>
		/// Used by Milk Spray to consume milk resource
		/// </summary>
		public bool ConsumeMilk(float amount)
		{
			if (CurrentMilk >= amount)
			{
				CurrentMilk -= amount;
				return true;
			}
			return false;
		}
		
		protected override void HandleClassSpecificUpdate()
		{
			// Handle portal recast timer
			if (IsPortalRecasting && portalRecastTimer > 0)
			{
				portalRecastTimer -= Time.Delta;
				if (portalRecastTimer <= 0)
				{
					// Recast window expired, reset state
					IsPortalRecasting = false;
					Log.Info("Portal recast window expired");
				}
			}
			
			if (CurrentMilk > 0 && !PremiumMilkActive)
			{
				MilkSpoilageTimer -= Time.Delta;
				if (MilkSpoilageTimer <= 0)
				{
					SpoilMilkRPC();
				}
			}
			
			if (IsCrying)
			{
				cryingTimer -= Time.Delta;
				if (cryingTimer <= 0)
				{
					StopCryingRPC();
				}
			}
			
			if (IsInvincible)
			{
				invincibilityTimer -= Time.Delta;
				if (invincibilityTimer <= 0)
				{
					IsInvincible = false;
				}
			}
			
			RegenerateMana();
		}
		
		protected override void HandleClassSpecificInput()
		{
			// All abilities now handled through UseAbility1/2/3 methods via standardized input system
			// This method can be used for additional class-specific inputs if needed
		}
		
		void RegenerateMana()
		{
			if (!IsAlive) return;
			CurrentMana = Math.Min(MaxMana, CurrentMana + 3f * Time.Delta);
		}
		
		[Rpc.Broadcast]
		void FireMilkGunRPC(Guid targetId)
		{
			if (!Networking.IsHost) return;
			
			var target = Scene.Directory.FindByGuid(targetId)?.Components.Get<TrinityPlayer>();
			if (target == null || !target.IsAlive) return;
			
			CurrentMilk -= MilkCostPerHeal;
			
			float healAmount = MilkGunHealAmount * BaseHealingMultiplier;
			target.Heal(healAmount, this);
			
			if (Random.Shared.Float() <= milksongProcChance)
			{
				TriggerMilksongRPC();
			}
			
			Log.Info($"Abby heals {target.ClassType} for {healAmount} HP!");
		}
		
		[Rpc.Broadcast]
		void FireCorkGunRPC(Guid targetId)
		{
			if (!Networking.IsHost) return;
			
			var target = Scene.Directory.FindByGuid(targetId)?.Components.Get<TrinityPlayer>();
			if (target == null) return;
			
			target.TakeDamage(CorkGunDamage * BaseDamageMultiplier, this);
			
			CurrentMilk = Math.Min(MaxMilk, CurrentMilk + MilkPerCorkHit);
			MilkSpoilageTimer = MilkSpoilTime;
			
			Log.Info($"Abby shoots cork gun for {CorkGunDamage} damage, gains {MilkPerCorkHit} milk!");
		}
		
		[Rpc.Broadcast]
		void PerformDivineSpillRPC()
		{
			if (!Networking.IsHost) return;
			
			CurrentMana -= 80;
			IsCrying = true;
			IsInvincible = true;
			cryingTimer = DivineSplillDuration;
			invincibilityTimer = DivineSplillDuration;
			MovementSpeed = 0f;
			
			var allies = Scene.GetAllComponents<TrinityPlayer>()
				.Where(p => p.IsAlive && p.ClassType != TrinityClassType.None)
				.Where(p => Vector3.DistanceBetween(WorldPosition, p.WorldPosition) <= DivineSplillHealRadius);
			
			foreach (var ally in allies)
			{
				ally.Heal(MaxHealth * 0.5f, this);
			}
			
			Log.Info("DIVINE SPILL! Abby falls over crying, healing all nearby allies!");
		}
		
		[Rpc.Broadcast]
		void StopCryingRPC()
		{
			if (!Networking.IsHost) return;
			
			IsCrying = false;
			MovementSpeed = originalMovementSpeed;
			Log.Info("Abby stops crying and gets back up");
		}
		
		[Rpc.Broadcast]
		void SpoilMilkRPC()
		{
			if (!Networking.IsHost) return;
			
			CurrentMilk = 0;
			MilkSpoilageTimer = MilkSpoilTime;
			Log.Warning("Milk has spoiled! All milk lost!");
		}
		
		[Rpc.Broadcast]
		void TriggerMilksongRPC()
		{
			Log.Info("MILKSONG PROC! 'I'm coming!' - All enemies stunned!");
			
			var enemies = Scene.GetAllComponents<BasicEnemy>()
				.Where(e => e.IsAlive);
			
			foreach (var enemy in enemies)
			{
				enemy.ApplyStun(2f);
			}
		}
		
		[Rpc.Broadcast]
		void PlaceMilkPortalRPC(bool isEntryPortal)
		{
			// Host consumes mana (like Cork Gun consuming ammo)
			if (Networking.IsHost)
			{
				CurrentMana -= 40;
			}
			
			if (MilkPortalPrefab == null || !MilkPortalPrefab.IsValid())
			{
				Log.Error("MilkPortalPrefab is null or invalid!");
				return;
			}
			
			// Calculate portal position (same as Cork Gun calculates projectile position)
			var lookDir = EyeAngles.ToRotation();
			var rayStart = WorldPosition + Vector3.Up * 64f;
			var rayEnd = rayStart + lookDir.Forward * 1000f;
			
			// Simple raycast to ground
			var trace = Scene.Trace.Ray(rayStart, rayEnd).Run();
			Vector3 portalPosition;
			if (trace.Hit)
			{
				portalPosition = trace.HitPosition + Vector3.Up * 10f;
			}
			else
			{
				portalPosition = WorldPosition + lookDir.Forward * 200f;
				portalPosition.z = WorldPosition.z;
			}
			
			// Clone portal (exactly like Cork Gun clones projectile)
			var portal = MilkPortalPrefab.Clone(portalPosition);
			if (portal == null)
			{
				Log.Error("Failed to clone MilkPortalPrefab!");
				return;
			}
			
			portal.Enabled = true;
			portal.Name = isEntryPortal ? "EntryPortal" : "ExitPortal";
			
			// Setup portal component (like Cork Gun sets up projectile)
			var portalComponent = portal.Components.Get<MilkPortal>();
			if (portalComponent == null)
			{
				Log.Warning("No MilkPortal component found on prefab, creating one...");
				portalComponent = portal.Components.GetOrCreate<MilkPortal>();
			}
			
			if (portalComponent != null)
			{
				portalComponent.Owner = this;
				portalComponent.IsEntryPortal = isEntryPortal;
				Log.Info($"Portal component setup complete: {(isEntryPortal ? "ENTRY" : "EXIT")}");
				
				// Simple portal linking (like how Cork Gun tracks owner)
				if (isEntryPortal)
				{
					entryPortal = portal;
					Log.Info("✅ Entry portal placed!");
				}
				else
				{
					exitPortal = portal;
					// Link to existing entry portal
					if (entryPortal != null && entryPortal.IsValid())
					{
						var entryComponent = entryPortal.Components.Get<MilkPortal>();
						if (entryComponent != null)
						{
							entryComponent.LinkedPortal = portalComponent;
							portalComponent.LinkedPortal = entryComponent;
							Log.Info("✅ Exit portal placed and linked!");
						}
						else
						{
							Log.Warning("Entry portal has no MilkPortal component!");
						}
					}
					else
					{
						Log.Warning("No valid entry portal found for linking!");
					}
				}
			}
			else
			{
				Log.Error("Failed to create MilkPortal component!");
			}
			
			// Network spawn last (exactly like Cork Gun)
			portal.NetworkSpawn();
		}
		
		public override void TakeDamage(float damage, TrinityPlayer attacker = null)
		{
			if (IsInvincible) return;
			base.TakeDamage(damage, attacker);
		}
		
		public void OnPortalDestroyed(GameObject destroyedPortal)
		{
			// Simple cleanup like Cork Gun
			if (destroyedPortal == entryPortal)
			{
				entryPortal = null;
			}
			if (destroyedPortal == exitPortal)
			{
				exitPortal = null;
			}
		}
		
		public void ActivatePremiumMilk()
		{
			PremiumMilkActive = true;
			Log.Info("Premium Milk activated! Milk will never spoil!");
		}
		
		TrinityPlayer GetTargetedAlly()
		{
			var forward = EyeAngles.ToRotation().Forward;
			var trace = Scene.Trace.Ray(Eye.WorldPosition, Eye.WorldPosition + forward * 1000f)
				.WithTag("player")
				.Run();
			
			if (trace.Hit && trace.GameObject != null)
			{
				return trace.GameObject.Components.Get<TrinityPlayer>();
			}
			
			return GetNearestAlly();
		}
		
		
		protected override float GetAbility1Cooldown() => 1.5f;
		protected override float GetAbility2Cooldown() => 0.5f;
		protected override float GetUltimateCooldown() => 120f;
		
		// Recast system: Only apply cooldown when portal sequence is complete
		protected override bool ShouldApplyAbility1Cooldown()
		{
			// Don't apply cooldown if we're in recast mode (first portal placed, waiting for second)
			// Only apply cooldown when both portals are placed or recast window expires
			return !IsPortalRecasting;
		}
		
		void CreateAbbyHUD()
		{
			Log.Info("Creating extensible SimpleGameHUD for Abby...");
			
			// Create HUD GameObject
			hudObject = Scene.CreateObject();
			hudObject.Name = "SimpleGameHUD_Abby";
			
			// Add ScreenPanel for UI rendering
			var screenPanel = hudObject.Components.GetOrCreate<ScreenPanel>();
			
			// Add the extensible SimpleGameHUD Razor component
			// This will automatically show Abby-specific icons, ammo, and status
			var hudComponent = hudObject.Components.GetOrCreate<SimpleGameHUD>();
			
			Log.Info("Extensible HUD created - will show Cork Revolver ammo, milk status, and class-specific icons!");
		}
		
		protected override void OnDestroy()
		{
			if (hudObject != null && hudObject.IsValid)
			{
				hudObject.Destroy();
				hudObject = null;
			}
			base.OnDestroy();
		}
	}
	
	public class MilkPortal : Component
	{
		[Property] public float Lifetime { get; set; } = 60f;
		
		public AbbyHealer Owner { get; set; }
		public MilkPortal LinkedPortal { get; set; }
		
		[Sync] public bool IsActive { get; set; } = true;
		[Sync] public bool IsEntryPortal { get; set; } = false; // true = teleports TO linked portal
		
		private float lifetimeTimer;
		private float teleportCooldown = 0f;
		
		protected override void OnStart()
		{
			base.OnStart();
			lifetimeTimer = Lifetime;
		}
		
		protected override void OnUpdate()
		{
			if (!Networking.IsHost) return;
			
			lifetimeTimer -= Time.Delta;
			if (lifetimeTimer <= 0)
			{
				DestroyPortal();
				return;
			}
			
			// Update teleport cooldown
			if (teleportCooldown > 0)
				teleportCooldown -= Time.Delta;
			
			// Simple distance-based teleportation
			if (IsActive && IsEntryPortal && LinkedPortal != null && LinkedPortal.IsValid() && teleportCooldown <= 0)
			{
				CheckForTeleportDistance();
			}
			// Portal is ready but not checking for teleportation (inactive, no link, etc.)
		}
		
		void CheckForTeleportDistance()
		{
			var players = Scene.GetAllComponents<TrinityPlayer>()
				.Where(p => p.IsAlive)
				.Where(p => Vector3.DistanceBetween(WorldPosition, p.WorldPosition) < 100f); // 100 unit trigger radius
			
			foreach (var player in players)
			{
				TeleportPlayerRPC(player.GameObject.Id);
				teleportCooldown = 2.0f; // 2 second cooldown to prevent spam
				break; // Only teleport one player at a time
			}
		}
		
		[Rpc.Broadcast]
		void TeleportPlayerRPC(Guid playerId)
		{
			if (LinkedPortal == null || !LinkedPortal.IsValid()) return;
			
			var player = Scene.Directory.FindByGuid(playerId)?.Components.Get<TrinityPlayer>();
			if (player != null)
			{
				// Simple teleportation to ground level at exit portal
				Vector3 teleportPos = LinkedPortal.WorldPosition;
				teleportPos.z = LinkedPortal.WorldPosition.z + 10f;
				
				player.WorldPosition = teleportPos;
				Log.Info($"{player.ClassType} teleported through milk portal");
				
				// Destroy both portals (like Cork Gun destroys projectile on hit)
				DestroyPortalPair();
			}
		}
		
		void DestroyPortalPair()
		{
			// Simple cleanup like Cork Gun
			if (LinkedPortal != null && LinkedPortal.IsValid())
			{
				LinkedPortal.GameObject.Destroy();
			}
			
			if (Owner != null)
			{
				Owner.OnPortalDestroyed(GameObject);
			}
			
			GameObject.Destroy();
		}
		
		void DestroyPortal()
		{
			if (Owner != null)
			{
				Owner.OnPortalDestroyed(GameObject);
			}
			
			if (LinkedPortal != null && LinkedPortal.IsValid())
			{
				LinkedPortal.LinkedPortal = null;
			}
			
			GameObject.Destroy();
		}
	}
}