namespace Reclaimer
{
	/// <summary>
	/// Result of an active reload attempt
	/// Determines reload speed and any bonuses
	/// </summary>
	public enum ActiveReloadResult
	{
		/// <summary>
		/// Perfect timing - 50% faster reload, bonus damage
		/// </summary>
		Perfect,
		
		/// <summary>
		/// Good/Normal reload - standard timing, no bonus
		/// </summary>
		Good,
		
		/// <summary>
		/// Missed timing - 50% slower reload, no bonus
		/// </summary>
		Miss
	}
}