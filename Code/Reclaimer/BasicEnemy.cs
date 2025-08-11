using Sandbox;
using System;

namespace Reclaimer
{
	public class BasicEnemy : Component
	{
		[Property] public float MaxHealth { get; set; } = 500f;
		[Property] public float AttackDamage { get; set; } = 50f;
		[Property] public float AttackRange { get; set; } = 100f;
		[Property] public float MovementSpeed { get; set; } = 200f;
		[Property] public float AttackCooldown { get; set; } = 2f;
		[Property] public string EnemyName { get; set; } = "Training Dummy";
		
		[Sync] public float CurrentHealth { get; set; }
		[Sync] public bool IsAlive { get; set; } = true;
		[Sync] public GameObject CurrentTarget { get; set; }
		[Sync] public bool IsStunned { get; set; }
		
		private float attackTimer;
		private float stunDuration;
		
		protected override void OnStart()
		{
			base.OnStart();
			CurrentHealth = MaxHealth;
			IsAlive = true;
			
			GameObject.Name = EnemyName;
		}
		
		protected override void OnUpdate()
		{
			if (!Networking.IsHost) return;
			if (!IsAlive) return;
			
			if (IsStunned)
			{
				stunDuration -= Time.Delta;
				if (stunDuration <= 0)
				{
					IsStunned = false;
				}
				return;
			}
			
			FindTarget();
			
			if (CurrentTarget.IsValid())
			{
				MoveTowardsTarget();
				TryAttack();
			}
		}
		
		void FindTarget()
		{
			if (CurrentTarget.IsValid())
			{
				var player = CurrentTarget.Components.Get<TrinityPlayer>();
				if (player != null && player.IsAlive) return;
			}
			
			var nearestPlayer = Scene.GetAllComponents<TrinityPlayer>()
				.Where(p => p.IsAlive)
				.OrderBy(p => Vector3.DistanceBetween(WorldPosition, p.WorldPosition))
				.FirstOrDefault();
			
			if (nearestPlayer != null)
			{
				CurrentTarget = nearestPlayer.GameObject;
			}
		}
		
		void MoveTowardsTarget()
		{
			var direction = (CurrentTarget.WorldPosition - WorldPosition).Normal;
			var distance = Vector3.DistanceBetween(WorldPosition, CurrentTarget.WorldPosition);
			
			if (distance > AttackRange)
			{
				WorldPosition += direction * MovementSpeed * Time.Delta;
				WorldRotation = Rotation.LookAt(direction, Vector3.Up);
			}
		}
		
		void TryAttack()
		{
			var distance = Vector3.DistanceBetween(WorldPosition, CurrentTarget.WorldPosition);
			if (distance > AttackRange) return;
			
			attackTimer -= Time.Delta;
			if (attackTimer <= 0)
			{
				PerformAttackRPC();
				attackTimer = AttackCooldown;
			}
		}
		
		[Rpc.Broadcast]
		void PerformAttackRPC()
		{
			if (!CurrentTarget.IsValid()) return;
			
			var player = CurrentTarget.Components.Get<TrinityPlayer>();
			if (player != null && player.IsAlive)
			{
				player.TakeDamage(AttackDamage, null);
				Log.Info($"{EnemyName} attacks {player.ClassType} for {AttackDamage} damage!");
			}
		}
		
		public void TakeDamage(float damage)
		{
			if (!Networking.IsHost) return;
			if (!IsAlive) return;
			
			CurrentHealth -= damage;
			CurrentHealth = Math.Max(0, CurrentHealth);
			
			OnDamagedRPC(damage);
			
			if (CurrentHealth <= 0)
			{
				Die();
			}
		}
		
		[Rpc.Broadcast]
		void OnDamagedRPC(float damage)
		{
			Log.Info($"{EnemyName} takes {damage} damage! ({CurrentHealth}/{MaxHealth} HP)");
		}
		
		public void ApplyStun(float duration)
		{
			if (!Networking.IsHost) return;
			
			IsStunned = true;
			stunDuration = Math.Max(stunDuration, duration);
			
			OnStunnedRPC(duration);
		}
		
		[Rpc.Broadcast]
		void OnStunnedRPC(float duration)
		{
			Log.Info($"{EnemyName} is stunned for {duration} seconds!");
		}
		
		void Die()
		{
			IsAlive = false;
			OnDeathRPC();
		}
		
		[Rpc.Broadcast]
		void OnDeathRPC()
		{
			Log.Info($"{EnemyName} has been defeated!");
		}
		
		public void Resurrect()
		{
			if (!Networking.IsHost) return;
			
			IsAlive = true;
			CurrentHealth = MaxHealth;
			IsStunned = false;
			stunDuration = 0;
			
			OnResurrectRPC();
		}
		
		[Rpc.Broadcast]
		void OnResurrectRPC()
		{
			Log.Info($"{EnemyName} has respawned!");
		}
	}
}