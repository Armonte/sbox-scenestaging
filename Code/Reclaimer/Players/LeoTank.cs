using Sandbox;
using System;

namespace Reclaimer
{
	public class LeoTank : TrinityPlayer
	{
		[Property] public float ShellParryWindow { get; set; } = 0.5f;
		[Property] public float ShellDuration { get; set; } = 2.0f;
		[Property] public float ThreatMultiplier { get; set; } = 2.0f;
		[Property] public GameObject SwordPrefab { get; set; }
		[Property] public GameObject LilFrankPrefab { get; set; }
		
		[Sync] public bool InShell { get; set; }
		[Sync] public bool HasSword { get; set; } = true;
		[Sync] public bool HeadRotated { get; set; }
		[Sync] public bool LactoseIntolerant { get; set; } = true;
		[Sync] public float LactaidTimer { get; set; }
		[Sync] public bool LilFrankActive { get; set; }
		
		private float shellTimer;
		private float parryTimer;
		private bool canParry;
		private GameObject droppedSword;
		private GameObject lilFrank;
		private float originalMovementSpeed;
		
		protected override void InitializeClass()
		{
			ClassType = TrinityClassType.Tank;
			// ALL other properties (MaxHealth, MaxMana, MovementSpeed, etc.) set via prefab editor
		}
		
		protected override void OnStart()
		{
			base.OnStart();
			// Store original speed AFTER prefab properties are fully loaded
			originalMovementSpeed = MovementSpeed;
		}
		
		public override void UseAbility1()
		{
			// LMB - Enter Shell Mode (drops sword) or Pick up Sword
			if (!HasSword) 
			{
				PickupSwordRPC();
				return;
			}
			
			PerformShellParryRPC();
		}
		
		public override void UseAbility2()
		{
			// RMB - Head Rotation Stance Switch (Battle vs Defensive)
			if (CurrentMana < 30) return;
			
			PerformHeadRotationRPC();
		}
		
		public override void UseUltimate()
		{
			// MMB - Summon Lil Frank
			if (LilFrankActive) return;
			if (CurrentMana < 60) return;
			
			SpawnLilFrankRPC();
		}
		
		protected override void HandleClassSpecificUpdate()
		{
			if (InShell)
			{
				shellTimer -= Time.Delta;
				if (shellTimer <= 0)
				{
					ExitShellRPC();
				}
				
				if (canParry)
				{
					parryTimer -= Time.Delta;
					if (parryTimer <= 0)
					{
						canParry = false;
					}
				}
			}
			
			if (LactaidTimer > 0)
			{
				LactaidTimer -= Time.Delta;
				if (LactaidTimer <= 0)
				{
					LactoseIntolerant = true;
				}
			}
			
			RegenerateMana();
		}
		
		void RegenerateMana()
		{
			if (!IsAlive) return;
			CurrentMana = Math.Min(MaxMana, CurrentMana + 2f * Time.Delta);
		}
		
		[Rpc.Broadcast]
		void PerformShellParryRPC()
		{
			if (!Networking.IsHost) return;
			
			InShell = true;
			HasSword = false;
			shellTimer = ShellDuration;
			parryTimer = ShellParryWindow;
			canParry = true;
			MovementSpeed = originalMovementSpeed * 0.3f;
			
			DropSword();
			
			Log.Info($"Leo enters shell! Parry window: {ShellParryWindow}s");
		}
		
		[Rpc.Broadcast]
		void ExitShellRPC()
		{
			if (!Networking.IsHost) return;
			
			InShell = false;
			canParry = false;
			MovementSpeed = originalMovementSpeed;
			
			Log.Info("Leo exits shell");
		}
		
		[Rpc.Broadcast]
		void PickupSwordRPC()
		{
			if (!Networking.IsHost) return;
			
			if (droppedSword != null && droppedSword.IsValid())
			{
				float distance = Vector3.DistanceBetween(WorldPosition, droppedSword.WorldPosition);
				if (distance < 100f)
				{
					HasSword = true;
					droppedSword.Destroy();
					droppedSword = null;
					Log.Info("Leo picks up sword");
				}
			}
		}
		
		void DropSword()
		{
			if (SwordPrefab != null && SwordPrefab.IsValid())
			{
				droppedSword = SwordPrefab.Clone();
				droppedSword.WorldPosition = WorldPosition + EyeAngles.Forward * 50f;
				droppedSword.WorldRotation = WorldRotation;
			}
		}
		
		[Rpc.Broadcast]
		void SpawnLilFrankRPC()
		{
			if (!Networking.IsHost) return;
			
			CurrentMana -= 60;
			LilFrankActive = true;
			
			if (LilFrankPrefab != null && LilFrankPrefab.IsValid())
			{
				lilFrank = LilFrankPrefab.Clone();
				lilFrank.WorldPosition = WorldPosition + EyeAngles.Forward * 100f;
				
				var ai = lilFrank.Components.GetOrCreate<LilFrankAI>();
				ai.ParentTank = this;
				ai.Initialize();
			}
			
			Log.Info("Leo summons Lil Frank!");
		}
		
		[Rpc.Broadcast]
		void PerformHeadRotationRPC()
		{
			if (!Networking.IsHost) return;
			
			CurrentMana -= 30;
			HeadRotated = !HeadRotated;
			
			if (HeadRotated)
			{
				// Defensive stance - high damage reduction, low damage output
				DamageReduction = 0.5f;
				BaseDamageMultiplier = 0.5f;
				ThreatMultiplier = 3.0f; // Generate more threat
				Log.Info("Leo rotates head 180° - DEFENSIVE STANCE! (50% damage reduction, 50% damage dealt)");
			}
			else
			{
				// Battle stance - moderate defense, better damage
				DamageReduction = 0.2f;
				BaseDamageMultiplier = 1.0f;
				ThreatMultiplier = 2.0f;
				Log.Info("Leo rotates head back - BATTLE STANCE! (20% damage reduction, 100% damage dealt)");
			}
		}
		
		public override void TakeDamage(float damage, TrinityPlayer attacker = null)
		{
			if (!Networking.IsHost) return;
			if (!IsAlive) return;
			
			float actualDamage = damage;
			
			if (InShell && canParry)
			{
				PerformParryRPC(attacker?.GameObject.Id ?? Guid.Empty);
				return;
			}
			else if (InShell)
			{
				actualDamage *= 0.1f;
			}
			else
			{
				actualDamage *= (1f - DamageReduction);
			}
			
			base.TakeDamage(actualDamage, attacker);
		}
		
		[Rpc.Broadcast]
		void PerformParryRPC(Guid attackerId)
		{
			canParry = false;
			Log.Info("PARRY! Leo successfully parries the attack!");
			
			var attacker = Scene.Directory.FindByGuid(attackerId)?.Components.Get<TrinityPlayer>();
			if (attacker != null)
			{
				attacker.TakeDamage(100f, this);
			}
		}
		
		public override void Heal(float amount, TrinityPlayer healer = null)
		{
			if (!Networking.IsHost) return;
			if (!IsAlive) return;
			
			float actualHeal = amount;
			
			if (LactoseIntolerant && healer is AbbyHealer)
			{
				actualHeal *= 0.5f;
				Log.Info("Leo's lactose intolerance reduces healing by 50%!");
			}
			
			base.Heal(actualHeal, healer);
		}
		
		public void OnLilFrankDestroyed()
		{
			LilFrankActive = false;
			lilFrank = null;
		}
		
		protected override float GetAbility1Cooldown() => InShell ? 0f : 6f; // Shell Parry / Sword Pickup
		protected override float GetAbility2Cooldown() => 10f; // Head Rotation Stance
		protected override float GetUltimateCooldown() => 45f; // Summon Lil Frank
		
		public float GetParryWindowRemaining()
		{
			return canParry ? Math.Max(0, parryTimer) : 0;
		}
	}
	
	public class LilFrankAI : Component
	{
		[Property] public float AttackDamage { get; set; } = 50f;
		[Property] public float AttackSpeed { get; set; } = 1.5f;
		[Property] public float MovementSpeed { get; set; } = 400f;
		[Property] public float Lifetime { get; set; } = 20f;
		
		public LeoTank ParentTank { get; set; }
		
		[Sync] public GameObject CurrentTarget { get; set; }
		
		private float attackTimer;
		private float lifetimeTimer;
		
		public void Initialize()
		{
			lifetimeTimer = Lifetime;
			FindNewTarget();
		}
		
		protected override void OnUpdate()
		{
			if (!Networking.IsHost) return;
			
			lifetimeTimer -= Time.Delta;
			if (lifetimeTimer <= 0)
			{
				DestroyLilFrank();
				return;
			}
			
			if (!CurrentTarget.IsValid() || !IsTargetValid())
			{
				FindNewTarget();
			}
			
			if (CurrentTarget.IsValid())
			{
				MoveTowardsTarget();
				TryAttack();
			}
		}
		
		void FindNewTarget()
		{
			var enemy = Scene.GetAllComponents<BasicEnemy>()
				.Where(e => e.IsAlive)
				.OrderBy(e => Vector3.DistanceBetween(WorldPosition, e.WorldPosition))
				.FirstOrDefault();
			
			if (enemy != null)
			{
				CurrentTarget = enemy.GameObject;
			}
		}
		
		bool IsTargetValid()
		{
			var enemy = CurrentTarget?.Components.Get<BasicEnemy>();
			return enemy != null && enemy.IsAlive;
		}
		
		void MoveTowardsTarget()
		{
			var direction = (CurrentTarget.WorldPosition - WorldPosition).Normal;
			WorldPosition += direction * MovementSpeed * Time.Delta;
			WorldRotation = Rotation.LookAt(direction, Vector3.Up);
		}
		
		void TryAttack()
		{
			float distance = Vector3.DistanceBetween(WorldPosition, CurrentTarget.WorldPosition);
			if (distance > 50f) return;
			
			attackTimer -= Time.Delta;
			if (attackTimer <= 0)
			{
				PerformSpinAttackRPC();
				attackTimer = 1f / AttackSpeed;
			}
		}
		
		[Rpc.Broadcast]
		void PerformSpinAttackRPC()
		{
			Log.Info("Lil Frank performs spinning attack!");
			
			var enemy = CurrentTarget?.Components.Get<BasicEnemy>();
			enemy?.TakeDamage(AttackDamage);
		}
		
		void DestroyLilFrank()
		{
			if (ParentTank != null)
			{
				ParentTank.OnLilFrankDestroyed();
			}
			GameObject.Destroy();
		}
	}
}