namespace Reclaimer
{
	public enum TrinityClassType
	{
		None = 0,
		Tank = 1,
		Healer = 2,
		DPS = 3
	}
	
	public static class TrinityClassInfo
	{
		public static string GetClassName(TrinityClassType type)
		{
			return type switch
			{
				TrinityClassType.Tank => "Leo the Phranklyn",
				TrinityClassType.Healer => "Holy Milker Abby",
				TrinityClassType.DPS => "Mighty Trunk Warrior",
				_ => "Unknown"
			};
		}
		
		public static string GetClassDescription(TrinityClassType type)
		{
			return type switch
			{
				TrinityClassType.Tank => "Shell-parrying turtle tank with lactose intolerance",
				TrinityClassType.Healer => "Milk gun priest with spoilage mechanics",
				TrinityClassType.DPS => "Trunk-obsessed elephant with 12-level progression",
				_ => ""
			};
		}
		
		public static string GetClassIcon(TrinityClassType type)
		{
			return type switch
			{
				TrinityClassType.Tank => "🐢",
				TrinityClassType.Healer => "🥛",
				TrinityClassType.DPS => "🐘",
				_ => "❓"
			};
		}
	}
}