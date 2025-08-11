using Sandbox;
using System;
using System.Collections.Generic;

namespace Reclaimer
{
	public enum TrunkLevel
	{
		Level1_Basic = 1,
		Level2_Enhanced = 2,
		Level3_Improved = 3,
		Level4_Advanced = 4,
		Level5_Electric = 5,
		Level6_Whirling = 6,
		Level7_Blazing = 7,
		Level8_Diamond = 8,
		Level9_Champion = 9,
		Level10_Royal = 10,
		Level11_Cosmic = 11,
		Level12_Transcendent = 12
	}
	
	public class TrunkWarrior : TrinityPlayer
	{
		[Property] public float BaseTrunkDamage { get; set; } = 150f;
		[Property] public float TrunkGrabRange { get; set; } = 200f;
		[Property] public float TrunkSlamRadius { get; set; } = 150f;
		[Property] public float ComboWindow { get; set; } = 2f;
		[Property] public float TrunkLevelXPRequired { get; set; } = 1000f;
		
		[Sync] public int TrunkLevel { get; set; } = 1;
		[Sync] public float TrunkExperience { get; set; }
		[Sync] public float TrunkLength { get; set; } = 30f;
		[Sync] public int ComboCount { get; set; }
		[Sync] public bool IsGrabbing { get; set; }
		[Sync] public GameObject GrabbedTarget { get; set; }
		[Sync] public bool TrunkEnhancementActive { get; set; }
		
		private float comboTimer;
		private string[] trunkPuns = new string[]
		{
			"That's trunking amazing!",
			"Holy trunk!",
			"What the trunk!",
			"Trunk yeah!",
			"Get trunked!",
			"Trunk this!",
			"Mother trunker!",
			"Son of a trunk!"
		};
		
		protected override void InitializeClass()
		{
			ClassType = TrinityClassType.DPS;
			
			// Initialize trunk-specific state
			TrunkLevel = 1;
			TrunkLength = 30f;
			
			// ALL other properties set via prefab editor
		}
		
		public override void UseAbility1()
		{
			if (IsGrabbing)
			{
				PerformTrunkSlamRPC();
			}
			else
			{
				PerformTrunkGrabRPC();
			}
		}
		
		public override void UseAbility2()
		{
			var ability = GetTrunkAbilityForLevel();
			if (ability != null)
			{
				ExecuteTrunkAbilityRPC(ability);
			}
		}
		
		public override void UseUltimate()
		{
			if (TrunkLevel < 9)
			{
				Log.Warning($"Ultimate requires trunk level 9+! Current level: {TrunkLevel}");
				return;
			}
			
			if (CurrentMana < 80) return;
			
			ExecuteUltimateAbilityRPC();
		}
		
		protected override void HandleClassSpecificUpdate()
		{
			UpdateTrunkLength();
			UpdateComboTimer();
			RegenerateMana();
			
			if (TrunkEnhancementActive)
			{
				
			}
		}
		
		void UpdateTrunkLength()
		{
			float targetLength = 30f + (TrunkLevel - 1) * 15f;
			TrunkLength = TrunkLength.LerpTo(targetLength, Time.Delta * 2f);
			TrunkGrabRange = 200f + TrunkLength;
		}
		
		void UpdateComboTimer()
		{
			if (ComboCount > 0)
			{
				comboTimer -= Time.Delta;
				if (comboTimer <= 0)
				{
					ResetComboRPC();
				}
			}
		}
		
		void RegenerateMana()
		{
			if (!IsAlive) return;
			CurrentMana = Math.Min(MaxMana, CurrentMana + 4f * Time.Delta);
		}
		
		[Rpc.Broadcast]
		void PerformTrunkGrabRPC()
		{
			if (!Networking.IsHost) return;
			
			var forward = EyeAngles.ToRotation().Forward;
			var trace = Scene.Trace.Ray(Eye.WorldPosition, Eye.WorldPosition + forward * TrunkGrabRange)
				.Run();
			
			if (trace.Hit && trace.GameObject != null)
			{
				var enemy = trace.GameObject.Components.Get<BasicEnemy>();
				if (enemy != null && enemy.IsAlive)
				{
					IsGrabbing = true;
					GrabbedTarget = trace.GameObject;
					IncrementCombo();
					
					Log.Info($"TRUNK GRAB! (Level {TrunkLevel}, {TrunkLength:F0}cm trunk)");
					YellTrunkPun();
				}
			}
		}
		
		[Rpc.Broadcast]
		void PerformTrunkSlamRPC()
		{
			if (!Networking.IsHost) return;
			if (!IsGrabbing || GrabbedTarget == null) return;
			
			var slamDamage = BaseTrunkDamage * (1f + TrunkLevel * 0.2f) * BaseDamageMultiplier;
			
			var enemies = Scene.GetAllComponents<BasicEnemy>()
				.Where(e => e.IsAlive)
				.Where(e => Vector3.DistanceBetween(WorldPosition, e.WorldPosition) <= TrunkSlamRadius);
			
			foreach (var enemy in enemies)
			{
				enemy.TakeDamage(slamDamage);
			}
			
			IsGrabbing = false;
			GrabbedTarget = null;
			IncrementCombo();
			GainExperience(100f);
			
			Log.Info($"TRUNK SLAM! {slamDamage} damage in {TrunkSlamRadius}m radius!");
			YellTrunkPun();
		}
		
		[Rpc.Broadcast]
		void ExecuteTrunkAbilityRPC(string abilityName)
		{
			if (!Networking.IsHost) return;
			
			switch (TrunkLevel)
			{
				case 5:
					TrunkZap();
					break;
				case 6:
					TrunkWhirl();
					break;
				case 7:
					BlazingTrunk();
					break;
				case 8:
					DiamondTrunk();
					break;
				default:
					break;
			}
			
			IncrementCombo();
			GainExperience(50f);
		}
		
		[Rpc.Broadcast]
		void ExecuteUltimateAbilityRPC()
		{
			if (!Networking.IsHost) return;
			
			CurrentMana -= 80;
			
			switch (TrunkLevel)
			{
				case 9:
					ChampionReach();
					break;
				case 10:
					RoyalCommand();
					break;
				case 11:
					CosmicPower();
					break;
				case 12:
					TranscendentSupremacy();
					break;
			}
			
			GainExperience(200f);
			YellTrunkPun();
		}
		
		void TrunkZap()
		{
			Log.Info("TRUNK ZAP! Electric damage to all nearby enemies!");
			var enemies = Scene.GetAllComponents<BasicEnemy>()
				.Where(e => e.IsAlive)
				.Where(e => Vector3.DistanceBetween(WorldPosition, e.WorldPosition) <= 300f);
			
			foreach (var enemy in enemies)
			{
				enemy.TakeDamage(200f * BaseDamageMultiplier);
			}
		}
		
		void TrunkWhirl()
		{
			Log.Info("TRUNK WHIRL! Spinning attack!");
			var enemies = Scene.GetAllComponents<BasicEnemy>()
				.Where(e => e.IsAlive)
				.Where(e => Vector3.DistanceBetween(WorldPosition, e.WorldPosition) <= 250f);
			
			foreach (var enemy in enemies)
			{
				enemy.TakeDamage(250f * BaseDamageMultiplier);
				enemy.ApplyStun(1f);
			}
		}
		
		void BlazingTrunk()
		{
			Log.Info("BLAZING TRUNK! Fire damage over time!");
		}
		
		void DiamondTrunk()
		{
			Log.Info("DIAMOND TRUNK! Impenetrable defense!");
			DamageReduction = 0.8f;
		}
		
		void ChampionReach()
		{
			Log.Info($"CHAMPION REACH! Trunk extends to {TrunkLength * 2}cm!");
			TrunkGrabRange *= 2;
		}
		
		void RoyalCommand()
		{
			Log.Info("ROYAL COMMAND! All allies gain damage boost!");
			var allies = Scene.GetAllComponents<TrinityPlayer>()
				.Where(p => p.IsAlive && p != this);
			
			foreach (var ally in allies)
			{
				ally.BaseDamageMultiplier *= 1.5f;
			}
		}
		
		void CosmicPower()
		{
			Log.Info("COSMIC POWER! Reality bends to the trunk!");
			CurrentHealth = MaxHealth;
			CurrentMana = MaxMana;
			BaseDamageMultiplier *= 2f;
		}
		
		void TranscendentSupremacy()
		{
			Log.Info("TRANSCENDENT TRUNK SUPREMACY! The encounter trembles!");
			
			var bosses = Scene.GetAllComponents<BasicEnemy>()
				.Where(b => b.IsAlive);
			
			foreach (var boss in bosses)
			{
				boss.TakeDamage(boss.CurrentHealth * 0.25f);
				Log.Info("The Transcendent Trunk deals 25% max health damage!");
			}
		}
		
		void IncrementCombo()
		{
			ComboCount++;
			comboTimer = ComboWindow;
			
			if (ComboCount >= 3)
			{
				Log.Info($"TRUNK COMBO x{ComboCount}!");
				BaseDamageMultiplier = 1.5f + (ComboCount * 0.1f);
			}
		}
		
		[Rpc.Broadcast]
		void ResetComboRPC()
		{
			if (!Networking.IsHost) return;
			
			ComboCount = 0;
			BaseDamageMultiplier = 1.5f;
		}
		
		void GainExperience(float xp)
		{
			if (TrunkLevel >= 12) return;
			
			TrunkExperience += xp;
			
			float xpNeeded = TrunkLevelXPRequired * TrunkLevel;
			if (TrunkExperience >= xpNeeded)
			{
				LevelUpTrunkRPC();
			}
		}
		
		[Rpc.Broadcast]
		void LevelUpTrunkRPC()
		{
			if (!Networking.IsHost) return;
			
			TrunkLevel++;
			TrunkExperience = 0;
			
			Log.Info($"TRUNK LEVEL UP! Now level {TrunkLevel} - {GetTrunkLevelName()}!");
			Log.Info($"Trunk length: {TrunkLength:F0}cm → {30f + (TrunkLevel - 1) * 15f:F0}cm!");
			
			YellTrunkPun();
		}
		
		void YellTrunkPun()
		{
			var pun = trunkPuns[Random.Shared.Int(0, trunkPuns.Length - 1)];
			Log.Info($"Trunk Warrior: '{pun}'");
		}
		
		string GetTrunkAbilityForLevel()
		{
			return TrunkLevel switch
			{
				5 => "Trunk Zap",
				6 => "Trunk Whirl",
				7 => "Blazing Trunk",
				8 => "Diamond Trunk",
				_ => null
			};
		}
		
		string GetTrunkLevelName()
		{
			return TrunkLevel switch
			{
				1 => "Basic Trunk",
				2 => "Enhanced Trunk",
				3 => "Improved Trunk",
				4 => "Advanced Trunk",
				5 => "Electric Trunk",
				6 => "Whirling Trunk",
				7 => "Blazing Trunk",
				8 => "Diamond Trunk",
				9 => "Champion Trunk",
				10 => "Royal Trunk",
				11 => "Cosmic Trunk",
				12 => "TRANSCENDENT TRUNK",
				_ => "Unknown Trunk"
			};
		}
		
		public void ActivateTrunkEnhancement()
		{
			TrunkEnhancementActive = true;
			TrunkLevel = Math.Min(12, TrunkLevel + 2);
			Log.Info($"Trunk Enhancement activated! Temporary +2 levels! (Now level {TrunkLevel})");
		}
		
		protected override float GetAbility1Cooldown() => IsGrabbing ? 0f : 3f;
		protected override float GetAbility2Cooldown() => 8f;
		protected override float GetUltimateCooldown() => 90f;
	}
}