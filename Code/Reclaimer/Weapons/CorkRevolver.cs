using Sandbox;
using System;

namespace Reclaimer
{
	/// <summary>
	/// Abby's Cork Revolver - Basic attack that builds milk resource
	/// Based on S&Box TestWeapon patterns with active reload mechanics
	/// </summary>
	public sealed class CorkRevolver : Component
	{
		[Property] public GameObject CorkProjectilePrefab { get; set; }
		[Property] public SoundEvent FireSound { get; set; }
		[Property] public SoundEvent ReloadSound { get; set; }
		[Property] public SoundEvent PerfectReloadSound { get; set; }
		
		[Property, Group("Cork Gun Stats")]
		public int MaxAmmo { get; set; } = 6;
		[Property, Group("Cork Gun Stats")]
		public float Damage { get; set; } = 100f;
		[Property, Group("Cork Gun Stats")]
		public float MilkPerHit { get; set; } = 10f;
		[Property, Group("Cork Gun Stats")]
		public float BaseReloadTime { get; set; } = 2.0f;
		[Property, Group("Cork Gun Stats")]
		public float FireDelay { get; set; } = 0.3f;
		[Property, Group("Cork Gun Stats")]
		public float ProjectileSpeed { get; set; } = 800f;
		
		[Property] public float SpawnForward { get; set; } = 50f; // How far forward from player
		[Property] public float SpawnRight { get; set; } = 0f; // How far right from player (negative = left)  
		[Property] public float SpawnUp { get; set; } = 64f; // How far up from player (chest height)
		[Property] public float UpwardForce { get; set; } = 200f; // Upward velocity component (like Gun.cs)
		
		[Property, Group("Active Reload")]
		public float PerfectReloadMultiplier { get; set; } = 0.5f; // 50% faster
		[Property, Group("Active Reload")]
		public float MissReloadMultiplier { get; set; } = 1.5f; // 50% slower
		[Property, Group("Active Reload")]
		public float PerfectZoneSize { get; set; } = 0.15f; // 15% of bar
		
		// Current state
		[Sync] public int CurrentAmmo { get; set; }
		[Sync] public bool IsReloading { get; set; }
		[Sync] public bool HasDamageBuff { get; set; }
		
		private AbbyHealer owner;
		private float timeSinceLastShot = 0f;
		private int damageBuffShots = 0;
		private bool autoReloadTriggered = false;
		
		protected override void OnStart()
		{
			base.OnStart();
			
			// Get the AbbyHealer owner - try different methods
			owner = Components.GetInAncestors<AbbyHealer>();
			
			if (owner == null)
			{
				Log.Warning("CorkRevolver: No AbbyHealer found in ancestors!");
			}
			else
			{
				Log.Info("CorkRevolver: Successfully found AbbyHealer owner!");
			}
			
			// If not found in ancestors, try scene search as fallback
			if (owner == null)
			{
				var players = Scene.GetAllComponents<AbbyHealer>();
				if (players.Any())
				{
					owner = players.First(); // Use first AbbyHealer found
					Log.Info($"Cork Revolver found AbbyHealer via scene search: {owner.GameObject.Name}");
				}
			}
			
			if (owner == null)
			{
				Log.Error("CorkRevolver: No AbbyHealer found! Check hierarchy or scene setup.");
				return;
			}
			
			// Initialize full ammo
			CurrentAmmo = MaxAmmo;
			
			// Validate prefab on start
			if (CorkProjectilePrefab == null)
			{
				Log.Error("CorkProjectilePrefab not assigned in inspector!");
			}
			else
			{
				Log.Info($"Cork Revolver initialized for {owner.GameObject.Name} with prefab: {CorkProjectilePrefab.Name}");
			}
		}
		
		protected override void OnUpdate()
		{
			if (IsProxy) return; // Only process on owner's client
			
			timeSinceLastShot += Time.Delta;
			
			// Handle auto-reload when ammo runs out
			if (CurrentAmmo <= 0 && !IsReloading && !autoReloadTriggered)
			{
				autoReloadTriggered = true;
				StartReload();
			}
			
			// Handle manual reload
			if (Input.Pressed("Reload") && CanReload())
			{
				StartReload();
			}
			
			// Handle firing
			if (Input.Pressed("attack1"))
			{
				Log.Info("CorkRevolver: attack1 input detected!");
				if (CanFire())
				{
					Log.Info("CorkRevolver: Can fire, firing cork!");
					FireCork();
				}
				else
				{
					Log.Info("CorkRevolver: Cannot fire - checking conditions...");
					Log.Info($"CurrentAmmo: {CurrentAmmo}, IsReloading: {IsReloading}, timeSinceLastShot: {timeSinceLastShot}, FireDelay: {FireDelay}");
				}
			}
		}
		
		bool CanFire()
		{
			return CurrentAmmo > 0 
				&& !IsReloading 
				&& timeSinceLastShot >= FireDelay 
				&& owner != null;
		}
		
		bool CanReload()
		{
			return CurrentAmmo < MaxAmmo && !IsReloading;
		}
		
		void FireCork()
		{
			// Pre-flight check on prefab validity
			if (CorkProjectilePrefab == null || !CorkProjectilePrefab.IsValid())
			{
				Log.Error($"Cork prefab invalid before firing! Prefab null: {CorkProjectilePrefab == null}");
				return;
			}
			
			timeSinceLastShot = 0f;
			CurrentAmmo--;
			autoReloadTriggered = false; // Reset auto-reload flag
			
			// Calculate damage with potential buff
			float actualDamage = Damage;
			if (HasDamageBuff)
			{
				actualDamage *= 1.1f; // 10% damage bonus
				damageBuffShots--;
				
				if (damageBuffShots <= 0)
				{
					HasDamageBuff = false;
				}
			}
			
			// Fire the cork projectile
			FireCorkProjectileRPC(actualDamage);
			
			Log.Info($"Cork fired! Ammo: {CurrentAmmo}/{MaxAmmo}, Damage: {actualDamage}");
		}
		
		[Rpc.Broadcast]
		void FireCorkProjectileRPC(float damage)
		{
			if (CorkProjectilePrefab == null)
			{
				Log.Warning("CorkProjectilePrefab is null!");
				return;
			}
			
			// Use mouse world position for targeting
			var trinityController = owner.Components.Get<TrinityPlayerController>();
			var mousePos = trinityController?.GetMouseWorldPosition();
			Vector3 direction;
			
			if (mousePos.HasValue)
			{
				// Calculate horizontal direction from character to mouse position
				var toMouse = (mousePos.Value - owner.WorldPosition).WithZ(0).Normal;
				direction = toMouse;
			}
			else
			{
				// Fallback to camera forward direction (horizontal)
				direction = owner.EyeAngles.ToRotation().Forward.WithZ(0).Normal;
			}
			
			// Spawn position offset from character
			var pos = owner.WorldPosition + Vector3.Up * SpawnUp + direction * SpawnForward;
			
			// Clone prefab
			var cork = CorkProjectilePrefab.Clone(pos);
			if (cork == null)
			{
				Log.Error("Failed to clone CorkProjectilePrefab!");
				return;
			}
			
			cork.Enabled = true;
			
			// Fire horizontally towards mouse with slight upward arc
			var velocity = direction * ProjectileSpeed + Vector3.Up * UpwardForce;
			
			// Set up the cork projectile
			var corkComponent = cork.Components.Get<CorkProjectile>();
			if (corkComponent != null)
			{
				corkComponent.Initialize(damage, MilkPerHit, owner, velocity);
			}
			
			Log.Info($"Cork fired: pos={pos}, velocity={velocity}");
			
			cork.NetworkSpawn(); // Network spawn last (like Gun.cs)
			
			// Play sound effect
			if (FireSound != null)
			{
				Sound.Play(FireSound, WorldPosition);
			}
			
			// Visual effects would go here (muzzle flash, etc.)
		}
		
		void StartReload()
		{
			if (IsReloading) return;
			
			IsReloading = true;
			Log.Info("Starting cork revolver reload...");
			
			// Find existing active reload component (preserves inspector settings)
			var activeReload = GameObject.Components.Get<ActiveReloadComponent>();
			if (activeReload == null)
			{
				Log.Warning("No ActiveReloadComponent found! Creating one with default settings. Add one manually to configure position/timing.");
				activeReload = GameObject.Components.GetOrCreate<ActiveReloadComponent>();
			}
			
			activeReload.StartReload(BaseReloadTime, PerfectZoneSize, OnReloadComplete);
		}
		
		void OnReloadComplete(ActiveReloadResult result)
		{
			IsReloading = false;
			CurrentAmmo = MaxAmmo;
			
			switch (result)
			{
				case ActiveReloadResult.Perfect:
					Log.Info("Perfect reload! Damage buff applied.");
					HasDamageBuff = true;
					damageBuffShots = 3; // Next 3 shots have bonus damage
					Sound.Play(PerfectReloadSound, WorldPosition);
					break;
					
				case ActiveReloadResult.Good:
					Log.Info("Good reload completed.");
					Sound.Play(ReloadSound, WorldPosition);
					break;
					
				case ActiveReloadResult.Miss:
					Log.Info("Reload missed - took longer than normal.");
					Sound.Play(ReloadSound, WorldPosition);
					break;
			}
		}
		
		// Called by CorkProjectile when it hits something
		public void OnCorkHit(float milkGenerated)
		{
			if (owner != null)
			{
				owner.AddMilk(milkGenerated);
				Log.Info($"Cork hit! Generated {milkGenerated} milk. Total: {owner.CurrentMilk}");
			}
		}
		
		// Public API for other systems
		public bool IsEmpty => CurrentAmmo <= 0;
		public float GetAmmoPercentage() => (float)CurrentAmmo / MaxAmmo;
	}
	
}