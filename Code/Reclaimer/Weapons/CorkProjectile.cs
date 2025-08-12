using Sandbox;
using System;
using System.Collections.Generic;

namespace Reclaimer
{
	/// <summary>
	/// Cork Projectile - Flies through air, deals damage, generates milk for Abby
	/// Based on S&Box Gun.cs patterns with physics and networking
	/// </summary>
	public sealed class CorkProjectile : Component, Component.ICollisionListener
	{
		[Property] public float LifeTime { get; set; } = 5f;
		[Property] public SoundEvent HitSound { get; set; }
		[Property] public SoundEvent PopSound { get; set; }
		
		private float damage;
		private float milkGenerated;
		private AbbyHealer owner;
		private bool hasHit = false;
		private float spawnTime;
		private GameObject shooterGameObject;
		
		protected override void OnStart()
		{
			base.OnStart();
			spawnTime = Time.Now;
			
			// Play the cork "pop" sound when fired
			if (PopSound != null)
			{
				Sound.Play(PopSound, WorldPosition);
			}
		}
		
		protected override void OnUpdate()
		{
			// Self-destruct after lifetime
			if (Time.Now - spawnTime >= LifeTime)
			{
				DestroyCork();
			}
		}
		
		public void Initialize(float dmg, float milk, AbbyHealer abby, Vector3 velocity)
		{
			damage = dmg;
			milkGenerated = milk;
			owner = abby;
			shooterGameObject = abby?.GameObject;
			
			// Set up physics
			var rigidbody = Components.Get<Rigidbody>();
			if (rigidbody != null)
			{
				rigidbody.Velocity = velocity;
			}
			
			Log.Info($"Cork initialized: {damage} damage, {milk} milk generation");
		}
		
		public void OnCollisionStart(Collision collision)
		{
			Log.Info($"CORK COLLISION DETECTED! Hit: {collision.Other.GameObject.Name}");
			if (hasHit) return; // Prevent multiple hits
			
			var hitGameObject = collision.Other.GameObject;
			
			// Ignore collision with the shooter or any of its children/parents
			if (IsShooterOrRelated(hitGameObject))
			{
				Log.Info($"Cork ignoring collision with shooter-related object: {hitGameObject.Name}");
				return;
			}
			
			hasHit = true;
			
			// Check if we hit an enemy that can take damage
			var damageable = hitGameObject.Components.Get<IReclaimerDamageable>();
			if (damageable != null)
			{
				// Deal damage using simplified interface
				damageable.OnDamage(damage, owner?.GameObject);
				
				// Generate milk for Abby
				if (owner != null)
				{
					var corkRevolver = owner.Components.GetInDescendants<CorkRevolver>();
					if (corkRevolver != null)
					{
						corkRevolver.OnCorkHit(milkGenerated);
						Log.Info($"CORK HIT! {hitGameObject.Name} took {damage} damage, Abby gained {milkGenerated} milk");
					}
				}
			}
			
			// Create hit effects
			CreateHitEffects(collision.Contact.Point, collision.Contact.Normal);
			
			// Apply physics forces
			ApplyPhysicsForce(collision);
			
			// Destroy the cork
			DestroyCork();
		}
		
		void CreateHitEffects(Vector3 position, Vector3 normal)
		{
			// Play hit sound
			if (HitSound != null)
			{
				Sound.Play(HitSound, position);
			}
			
			// Create impact effect (could be enhanced with particles later)
			// For now, just a simple impact GameObject
			var impact = Scene.CreateObject();
			impact.WorldPosition = position;
			impact.WorldRotation = Rotation.LookAt(normal);
			impact.Name = "CorkImpact";
			
			// Self-destruct impact after short time (using timer approach)
			var timer = impact.Components.GetOrCreate<TimedDestroy>();
			timer.DestroyAfter(2f);
		}
		
		bool IsShooterOrRelated(GameObject hitObject)
		{
			if (shooterGameObject == null || hitObject == null) return false;
			
			// Direct match
			if (hitObject == shooterGameObject) return true;
			
			// Check if hit object is a child of shooter
			var parent = hitObject.Parent;
			while (parent != null)
			{
				if (parent == shooterGameObject) return true;
				parent = parent.Parent;
			}
			
			// Check if hit object is a parent of shooter
			parent = shooterGameObject.Parent;
			while (parent != null)
			{
				if (parent == hitObject) return true;
				parent = parent.Parent;
			}
			
			// Check by name patterns (Physics Push is likely part of player)
			if (hitObject.Name.Contains("Physics") && shooterGameObject.Name.Contains("Healer"))
			{
				return true;
			}
			
			return false;
		}
		
		void DestroyCork()
		{
			if (GameObject.IsValid())
			{
				GameObject.Destroy();
			}
		}
		
		// Apply physics forces on hit (integrated into main OnCollision)
		void ApplyPhysicsForce(Collision collision)
		{
			// Apply some force to physics objects we hit
			if (collision.Other.Body != null)
			{
				var force = collision.Contact.Normal * -100f; // Push objects away
				collision.Other.Body.ApplyImpulseAt(collision.Contact.Point, force);
			}
		}
	}
}