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
			
			// Initialize runtime state
			CurrentMilk = 0f;
			MilkSpoilageTimer = MilkSpoilTime;
			
			// ALL other properties set via prefab editor
		}
		
		protected override void OnStart()
		{
			base.OnStart();
			// Store original speed AFTER prefab properties are fully loaded
			originalMovementSpeed = MovementSpeed;
		}
		
		public override void UseAbility1()
		{
			var target = GetTargetedAlly();
			if (target == null)
			{
				Log.Warning("No ally targeted for healing!");
				return;
			}
			
			if (CurrentMilk < MilkCostPerHeal)
			{
				Log.Warning("Not enough milk! Need to shoot enemies with cork gun first.");
				return;
			}
			
			FireMilkGunRPC(target.GameObject.Id);
		}
		
		public override void UseAbility2()
		{
			var target = GetNearestEnemy();
			if (target == null)
			{
				Log.Warning("No enemy to shoot!");
				return;
			}
			
			FireCorkGunRPC(target.GameObject.Id);
		}
		
		public override void UseUltimate()
		{
			if (CurrentMana < 80) return;
			if (IsCrying) return;
			
			PerformDivineSpillRPC();
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
			if (Input.Pressed("SecondaryAction"))
			{
				PlaceMilkPortalRPC();
			}
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
				
				if (lastPortal != null && lastPortal.IsValid())
				{
					var lastPortalComponent = lastPortal.Components.Get<MilkPortal>();
					if (lastPortalComponent != null)
					{
						portalComponent.LinkedPortal = lastPortalComponent;
						lastPortalComponent.LinkedPortal = portalComponent;
					}
				}
				
				lastPortal = portal;
			}
			
			Log.Info($"Milk portal placed! ({MilkPortalCount}/2)");
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
	}
	
	public class MilkPortal : Component
	{
		[Property] public float TeleportCooldown { get; set; } = 3f;
		[Property] public float Lifetime { get; set; } = 60f;
		
		public AbbyHealer Owner { get; set; }
		public MilkPortal LinkedPortal { get; set; }
		
		[Sync] public bool IsActive { get; set; } = true;
		
		private float lifetimeTimer;
		private float cooldownTimer;
		
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
			
			if (cooldownTimer > 0)
			{
				cooldownTimer -= Time.Delta;
			}
			
			if (IsActive && LinkedPortal != null && cooldownTimer <= 0)
			{
				CheckForTeleport();
			}
		}
		
		void CheckForTeleport()
		{
			var players = Scene.GetAllComponents<TrinityPlayer>()
				.Where(p => p.IsAlive)
				.Where(p => Vector3.DistanceBetween(WorldPosition, p.WorldPosition) < 50f);
			
			foreach (var player in players)
			{
				TeleportPlayerRPC(player.GameObject.Id);
				cooldownTimer = TeleportCooldown;
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