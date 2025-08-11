using Sandbox;
using System;

namespace Reclaimer
{
	public enum BossPhase
	{
		Phase1,
		Transition1,
		Phase2,
		Transition2,
		Phase3,
		Dead
	}
	
	public class BossEntity : BasicEnemy
	{
		[Property] public string BossName { get; set; } = "The Reclaimer";
		
		[Sync] public BossPhase CurrentPhase { get; set; }
		[Sync] public float StunDuration { get; set; }
		
		protected override void OnStart()
		{
			MaxHealth = 100000f;
			EnemyName = BossName;
			base.OnStart();
			CurrentPhase = BossPhase.Phase1;
		}
		
		protected override void OnUpdate()
		{
			base.OnUpdate();
			
			if (!Networking.IsHost) return;
			
			if (IsStunned && StunDuration > 0)
			{
				StunDuration -= Time.Delta;
				if (StunDuration <= 0)
				{
					IsStunned = false;
				}
			}
		}
		
		public new void ApplyStun(float duration)
		{
			base.ApplyStun(duration);
			StunDuration = Math.Max(StunDuration, duration);
		}
	}
}