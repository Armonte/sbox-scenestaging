using Sandbox;

namespace Reclaimer
{
	/// <summary>
	/// Interface for Reclaimer entities that can take damage
	/// Used by weapons, projectiles, and environmental hazards  
	/// Named to avoid conflict with S&Box's built-in IDamageable
	/// </summary>
	public interface IReclaimerDamageable
	{
		/// <summary>
		/// Apply damage to this entity - simplified version for projectiles
		/// </summary>
		/// <param name="damage">Amount of damage to deal</param>
		/// <param name="attacker">GameObject that dealt the damage</param>
		void OnDamage(float damage, GameObject attacker = null);
		
		/// <summary>
		/// Check if this entity is currently alive/damageable
		/// </summary>
		bool IsAlive { get; }
		
		/// <summary>
		/// Current health value
		/// </summary>
		float CurrentHealth { get; }
		
		/// <summary>
		/// Maximum health value
		/// </summary>
		float MaxHealth { get; }
	}
}