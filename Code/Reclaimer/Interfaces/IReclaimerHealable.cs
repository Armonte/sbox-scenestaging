using Sandbox;

namespace Reclaimer
{
	/// <summary>
	/// Interface for Reclaimer entities that can be healed
	/// Used by healing abilities like Milk Spray
	/// </summary>
	public interface IReclaimerHealable
	{
		/// <summary>
		/// Apply healing to this entity
		/// </summary>
		/// <param name="healAmount">Amount of healing to apply</param>
		/// <param name="healer">GameObject that provided the healing</param>
		void OnHeal(float healAmount, GameObject healer = null);
		
		/// <summary>
		/// Check if this entity is currently alive/healable
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
		
		/// <summary>
		/// Check if entity needs healing (not at max health)
		/// </summary>
		bool NeedsHealing => IsAlive && CurrentHealth < MaxHealth;
	}
}