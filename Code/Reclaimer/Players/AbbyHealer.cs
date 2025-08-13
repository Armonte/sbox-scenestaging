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
		[Sync] public int MilkPortalCount { get; set; }
		[Sync] public bool PremiumMilkActive { get; set; }
		
		private float cryingTimer;
		private float invincibilityTimer;
		private GameObject lastPortal;
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
			// Ability1 = Milk Portal (Key 1)
			PlaceMilkPortalRPC();
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
		void PlaceMilkPortalRPC()
		{
			if (!Networking.IsHost) return;
			
			if (CurrentMana < 40) return;
			if (MilkPortalCount >= 2) return;
			
			CurrentMana -= 40;
			MilkPortalCount++;
			
			if (MilkPortalPrefab != null && MilkPortalPrefab.IsValid())
			{
				var portal = MilkPortalPrefab.Clone();
				portal.WorldPosition = WorldPosition;
				
				var portalComponent = portal.Components.GetOrCreate<MilkPortal>();
				portalComponent.Owner = this;
				
				if (MilkPortalCount == 1)
				{
					// First cast: Place exit portal (Portal A)
					portalComponent.IsEntryPortal = false;
					lastPortal = portal;
					Log.Info("Exit portal (Portal A) placed!");
				}
				else if (MilkPortalCount == 2)
				{
					// Second cast: Place entry portal (Portal B) and link to exit portal
					portalComponent.IsEntryPortal = true;
					
					if (lastPortal != null && lastPortal.IsValid())
					{
						var exitPortalComponent = lastPortal.Components.Get<MilkPortal>();
						if (exitPortalComponent != null)
						{
							portalComponent.LinkedPortal = exitPortalComponent;
							exitPortalComponent.LinkedPortal = portalComponent;
							Log.Info("Entry portal (Portal B) placed and linked! Walk on Portal B to teleport to Portal A.");
						}
					}
				}
			}
		}
		
		public override void TakeDamage(float damage, TrinityPlayer attacker = null)
		{
			if (IsInvincible) return;
			base.TakeDamage(damage, attacker);
		}
		
		public void OnPortalDestroyed()
		{
			MilkPortalCount = Math.Max(0, MilkPortalCount - 1);
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
		[Property] public float TeleportRadius { get; set; } = 50f;
		
		public AbbyHealer Owner { get; set; }
		public MilkPortal LinkedPortal { get; set; }
		
		[Sync] public bool IsActive { get; set; } = true;
		[Sync] public bool IsEntryPortal { get; set; } = false; // true = teleports TO linked portal
		
		private float lifetimeTimer;
		
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
			
			// Only entry portals check for teleportation
			if (IsActive && IsEntryPortal && LinkedPortal != null && LinkedPortal.IsValid())
			{
				CheckForTeleport();
			}
		}
		
		void CheckForTeleport()
		{
			var players = Scene.GetAllComponents<TrinityPlayer>()
				.Where(p => p.IsAlive)
				.Where(p => Vector3.DistanceBetween(WorldPosition, p.WorldPosition) < TeleportRadius);
			
			foreach (var player in players)
			{
				TeleportPlayerRPC(player.GameObject.Id);
			}
		}
		
		[Rpc.Broadcast]
		void TeleportPlayerRPC(Guid playerId)
		{
			if (LinkedPortal == null || !LinkedPortal.IsValid()) return;
			
			var player = Scene.Directory.FindByGuid(playerId)?.Components.Get<TrinityPlayer>();
			if (player != null)
			{
				player.WorldPosition = LinkedPortal.WorldPosition + Vector3.Up * 50f;
				Log.Info($"{player.ClassType} teleported through milk portal!");
			}
		}
		
		void DestroyPortal()
		{
			if (Owner != null)
			{
				Owner.OnPortalDestroyed();
			}
			
			if (LinkedPortal != null && LinkedPortal.IsValid())
			{
				LinkedPortal.LinkedPortal = null;
			}
			
			GameObject.Destroy();
		}
	}
}