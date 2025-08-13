using Sandbox;
using Sandbox.Citizen;
using System;

namespace Reclaimer
{
	public abstract class TrinityPlayer : Component, IReclaimerDamageable, IReclaimerHealable
	{
		[Property] public float MaxHealth { get; set; } = 1000f;
		[Property] public float MaxMana { get; set; } = 100f;
		[Property] public float MaxResource { get; set; } = 100f;
		[Property] public float MovementSpeed { get; set; } = 2000.0f;
		[Property] public float RunSpeedMultiplier { get; set; } = 1.5f;
		[Property] public float BaseDamageMultiplier { get; set; } = 1.0f;
		[Property] public float BaseHealingMultiplier { get; set; } = 1.0f;
		[Property] public float DamageReduction { get; set; } = 0f;
		[Property] public float GravityStrength { get; set; } = 200f;
		
		[Property] public GameObject Body { get; set; }
		[Property] public GameObject Eye { get; set; }
		[Property] public CitizenAnimationHelper AnimationHelper { get; set; }
		
		[Sync] public float CurrentHealth { get; set; }
		[Sync] public float CurrentMana { get; set; }
		[Sync] public float CurrentResource { get; set; }
		[Sync] public bool IsAlive { get; set; } = true;
		[Sync] public bool IsCasting { get; set; }
		[Sync] public float CastProgress { get; set; }
		[Sync] public TrinityClassType ClassType { get; set; }
		
		// IReclaimerHealable implementation
		public bool NeedsHealing => IsAlive && CurrentHealth < MaxHealth;
		
		[Sync] public Angles EyeAngles { get; set; }
		[Sync] public bool IsRunning { get; set; }
		
		public Vector3 WishVelocity { get; private set; }
		
		public CharacterController CharacterController => GameObject.Components.Get<CharacterController>();
		
		public float Ability1Cooldown { get; protected set; }
		public float Ability2Cooldown { get; protected set; }
		public float UltimateCooldown { get; protected set; }
		
		protected float GlobalCooldown { get; set; } = 0f;
		protected const float GlobalCooldownTime = 1.0f;
		
		public abstract void UseAbility1();
		public abstract void UseAbility2();
		public abstract void UseUltimate();
		
		protected virtual void HandleClassSpecificUpdate() { }
		protected virtual void HandleClassSpecificInput() { }
		
		// Virtual methods for recast ability system
		protected virtual bool ShouldApplyAbility1Cooldown() => true; // Default: always apply cooldown
		protected virtual bool ShouldApplyAbility2Cooldown() => true;
		protected virtual bool ShouldApplyUltimateCooldown() => true;
		
		protected override void OnStart()
		{
			base.OnStart();
			CurrentHealth = MaxHealth;
			CurrentMana = MaxMana;
			CurrentResource = MaxResource;
			IsAlive = true;
			
			// Reset eye angles to prevent movement drift
			EyeAngles = new Angles(0, 0, 0);
			
			InitializeClass();
		}
		
		protected abstract void InitializeClass();
		
		
		protected override void OnUpdate()
		{
			if (IsProxy) return;
			
			if (!IsAlive)
			{
				HandleDeath();
				return;
			}
			
			HandleMovementInput();
			HandleAbilityInput();
			UpdateCooldowns();
			HandleClassSpecificUpdate();
			UpdateCamera();
			UpdateAnimation();
		}
		
		protected override void OnFixedUpdate()
		{
			if (IsProxy) return;
			
			var cc = CharacterController;
			if (!cc.IsValid()) return;
			
			BuildWishVelocity();
			
			if (cc.IsOnGround && Input.Down("Jump"))
			{
				float jumpForce = 268.3281572999747f * 1.2f;
				cc.Punch(Vector3.Up * jumpForce);
				TriggerJumpRPC();
			}
			
			if (cc.IsOnGround)
			{
				// Force exact velocity to override S&Box built-in run speed logic
				cc.Velocity = WishVelocity.WithZ(cc.Velocity.z);
			}
			else
			{
				var gravity = new Vector3(0, 0, GravityStrength);
				cc.Velocity -= gravity * Time.Delta * 0.5f;
				cc.Accelerate(WishVelocity.ClampLength(50));
				cc.ApplyFriction(0.1f);
			}
			
			cc.Move();
			
			if (!cc.IsOnGround)
			{
				var gravity = new Vector3(0, 0, GravityStrength);
				cc.Velocity -= gravity * Time.Delta * 0.5f;
			}
			else
			{
				cc.Velocity = cc.Velocity.WithZ(0);
			}
		}
		
		void HandleMovementInput()
		{
			var ee = EyeAngles;
			ee += Input.AnalogLook * 0.5f;
			ee.roll = 0;
			EyeAngles = ee;
			
			IsRunning = Input.Down("Run");
		}
		
		void HandleAbilityInput()
		{
			if (GlobalCooldown > 0) return;
			if (IsCasting) return;
			
			// Standard Input System:
			// LMB (attack1) = Primary Fire - handled by weapon components
			// RMB (attack2) = Alternate Fire - handled by weapon components
			// Key 1-4 = Abilities 1-4
			
			if (Input.Pressed("Slot1") && Ability1Cooldown <= 0)
			{
				UseAbility1();
				// Only apply cooldown if the ability says to (handles recast abilities)
				if (ShouldApplyAbility1Cooldown())
				{
					Ability1Cooldown = GetAbility1Cooldown();
					GlobalCooldown = GlobalCooldownTime;
				}
			}
			
			if (Input.Pressed("Slot2") && Ability2Cooldown <= 0)
			{
				UseAbility2();
				Ability2Cooldown = GetAbility2Cooldown();
				GlobalCooldown = GlobalCooldownTime;
			}
			
			if (Input.Pressed("Slot3") && UltimateCooldown <= 0)
			{
				UseUltimate();
				UltimateCooldown = GetUltimateCooldown();
				GlobalCooldown = GlobalCooldownTime;
			}
			
			HandleClassSpecificInput();
		}
		
		void UpdateCooldowns()
		{
			if (Ability1Cooldown > 0) Ability1Cooldown -= Time.Delta;
			if (Ability2Cooldown > 0) Ability2Cooldown -= Time.Delta;
			if (UltimateCooldown > 0) UltimateCooldown -= Time.Delta;
			if (GlobalCooldown > 0) GlobalCooldown -= Time.Delta;
		}
		
		void BuildWishVelocity()
		{
			var rot = EyeAngles.ToRotation();
			WishVelocity = rot * Input.AnalogMove;
			WishVelocity = WishVelocity.WithZ(0);
			
			if (!WishVelocity.IsNearZeroLength)
				WishVelocity = WishVelocity.Normal;
			
			var speed = IsRunning ? MovementSpeed * RunSpeedMultiplier : MovementSpeed;
			WishVelocity *= speed;
		}
		
		void UpdateCamera()
		{
			var cam = Scene.GetAllComponents<CameraComponent>().FirstOrDefault();
			if (!cam.IsValid()) return;
			
			var lookDir = EyeAngles.ToRotation();
			
			cam.WorldPosition = WorldPosition + lookDir.Backward * 400 + Vector3.Up * 200.0f;
			cam.WorldRotation = Rotation.LookAt((WorldPosition + Vector3.Up * 50) - cam.WorldPosition, Vector3.Up);
		}
		
		void UpdateAnimation()
		{
			if (!AnimationHelper.IsValid()) return;
			if (!Body.IsValid()) return;
			
			var cc = CharacterController;
			if (!cc.IsValid()) return;
			
			var targetAngle = new Angles(0, EyeAngles.yaw, 0).ToRotation();
			var v = cc.Velocity.WithZ(0);
			
			if (v.Length > 10.0f)
			{
				targetAngle = Rotation.LookAt(v, Vector3.Up);
			}
			
			float rotateDifference = Body.WorldRotation.Distance(targetAngle);
			if (rotateDifference > 50.0f || cc.Velocity.Length > 10.0f)
			{
				Body.WorldRotation = Rotation.Lerp(Body.WorldRotation, targetAngle, Time.Delta * 2.0f);
			}
			
			AnimationHelper.WithVelocity(cc.Velocity);
			AnimationHelper.WithWishVelocity(WishVelocity);
			AnimationHelper.IsGrounded = cc.IsOnGround;
			AnimationHelper.WithLook(EyeAngles.Forward, 1, 1, 1.0f);
			
			AnimationHelper.MoveStyle = IsRunning ? 
				CitizenAnimationHelper.MoveStyles.Run : 
				CitizenAnimationHelper.MoveStyles.Walk;
		}
		
		[Rpc.Broadcast]
		protected void TriggerJumpRPC()
		{
			AnimationHelper?.TriggerJump();
		}
		
		public virtual void TakeDamage(float damage, TrinityPlayer attacker = null)
		{
			if (!Networking.IsHost) return;
			if (!IsAlive) return;
			
			float reducedDamage = damage * (1f - DamageReduction);
			CurrentHealth -= reducedDamage;
			CurrentHealth = Math.Max(0, CurrentHealth);
			
			if (CurrentHealth <= 0)
			{
				Die(attacker);
			}
			
			OnDamageTakenRPC(damage, attacker?.GameObject.Id ?? Guid.Empty);
		}
		
		// IDamageable implementation - simplified interface
		public void OnDamage(float damage, GameObject attacker = null)
		{
			var attackerPlayer = attacker?.Components.Get<TrinityPlayer>();
			TakeDamage(damage, attackerPlayer);
		}
		
		// IReclaimerHealable implementation
		public void OnHeal(float healAmount, GameObject healer = null)
		{
			var healerPlayer = healer?.Components.Get<TrinityPlayer>();
			Heal(healAmount, healerPlayer);
		}
		
		[Rpc.Broadcast]
		protected virtual void OnDamageTakenRPC(float damage, Guid attackerId)
		{
			
		}
		
		public virtual void Heal(float amount, TrinityPlayer healer = null)
		{
			if (!Networking.IsHost) return;
			if (!IsAlive) return;
			
			float actualHeal = amount * (healer?.BaseHealingMultiplier ?? 1.0f);
			CurrentHealth = Math.Min(MaxHealth, CurrentHealth + actualHeal);
			
			OnHealReceivedRPC(actualHeal, healer?.GameObject.Id ?? Guid.Empty);
		}
		
		[Rpc.Broadcast]
		protected virtual void OnHealReceivedRPC(float amount, Guid healerId)
		{
			
		}
		
		protected virtual void Die(TrinityPlayer killer = null)
		{
			if (!Networking.IsHost) return;
			
			IsAlive = false;
			CurrentHealth = 0;
			
			OnDeathRPC(killer?.GameObject.Id ?? Guid.Empty);
		}
		
		[Rpc.Broadcast]
		protected virtual void OnDeathRPC(Guid killerId)
		{
			
		}
		
		protected virtual void HandleDeath()
		{
			
		}
		
		public void Resurrect()
		{
			if (!Networking.IsHost) return;
			
			IsAlive = true;
			CurrentHealth = MaxHealth;
			CurrentMana = MaxMana;
			CurrentResource = MaxResource;
			
			OnResurrectRPC();
		}
		
		[Rpc.Broadcast]
		protected virtual void OnResurrectRPC()
		{
			
		}
		
		protected abstract float GetAbility1Cooldown();
		protected abstract float GetAbility2Cooldown();
		protected abstract float GetUltimateCooldown();
		
		protected BasicEnemy GetNearestEnemy()
		{
			return Scene.GetAllComponents<BasicEnemy>()
				.Where(e => e.IsAlive)
				.OrderBy(e => Vector3.DistanceBetween(WorldPosition, e.WorldPosition))
				.FirstOrDefault();
		}
		
		protected TrinityPlayer GetNearestAlly()
		{
			return Scene.GetAllComponents<TrinityPlayer>()
				.Where(p => p != this && p.IsAlive && p.ClassType != TrinityClassType.None)
				.OrderBy(p => Vector3.DistanceBetween(WorldPosition, p.WorldPosition))
				.FirstOrDefault();
		}
	}
}