using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Reclaimer
{
	public enum GameState
	{
		ClassSelection,
		TavernWaiting,
		ArenaFight,
		GameComplete
	}

	public class GameFlowManager : Component
	{
		[Property] public List<GameObject> TavernSpawnPoints { get; set; } = new();
		[Property] public List<GameObject> ArenaSpawnPoints { get; set; } = new();
		[Property] public float TavernWaitTime { get; set; } = 30f; // Max wait time in tavern
		[Property] public int MinPlayersToStart { get; set; } = 3;
		
		[Sync] public GameState CurrentState { get; set; } = GameState.ClassSelection;
		[Sync] public float TavernTimer { get; set; }
		[Sync] public bool AllPlayersReady { get; set; }
		
		private TrinitySpawnManager spawnManager;
		private List<TrinityPlayer> tavernPlayers = new();
		
		protected override void OnStart()
		{
			base.OnStart();
			spawnManager = Scene.GetAllComponents<TrinitySpawnManager>().FirstOrDefault();
			
			if (Networking.IsHost)
			{
				Log.Info("Game Flow Manager initialized");
				CurrentState = GameState.ClassSelection;
			}
		}
		
		protected override void OnUpdate()
		{
			if (!Networking.IsHost) return;
			
			switch (CurrentState)
			{
				case GameState.ClassSelection:
					HandleClassSelection();
					break;
					
				case GameState.TavernWaiting:
					HandleTavernWaiting();
					break;
					
				case GameState.ArenaFight:
					HandleArenaFight();
					break;
					
				case GameState.GameComplete:
					HandleGameComplete();
					break;
			}
		}
		
		void HandleClassSelection()
		{
			// Check if minimum players have selected classes
			if (spawnManager != null && spawnManager.GetTotalPlayerCount() >= MinPlayersToStart)
			{
				// Check if we have proper trinity composition (1 tank, 1 healer, 1+ DPS)
				if (spawnManager.TankCount >= 1 && spawnManager.HealerCount >= 1 && spawnManager.DPSCount >= 1)
				{
					TransitionToTavern();
				}
			}
		}
		
		void HandleTavernWaiting()
		{
			TavernTimer -= Time.Delta;
			
			// Check if all players are ready or timer expired
			if (AllPlayersReady || TavernTimer <= 0)
			{
				TransitionToArena();
			}
		}
		
		void HandleArenaFight()
		{
			// Check if all players are dead or boss is defeated
			var alivePlayers = Scene.GetAllComponents<TrinityPlayer>().Where(p => p.IsAlive).Count();
			if (alivePlayers == 0)
			{
				// All players dead - restart in tavern
				TransitionToTavern();
			}
			
			// Check if boss is defeated
			var bosses = Scene.GetAllComponents<BossEntity>().Where(b => b.IsAlive);
			if (!bosses.Any())
			{
				// Boss defeated - victory!
				TransitionToComplete();
			}
		}
		
		void HandleGameComplete()
		{
			// Victory state - maybe restart after delay
		}
		
		[Rpc.Broadcast]
		void TransitionToTavern()
		{
			CurrentState = GameState.TavernWaiting;
			TavernTimer = TavernWaitTime;
			AllPlayersReady = false;
			
			Log.Info("Transitioning to Tavern - waiting for party to ready up");
			
			// Teleport all players to tavern
			TeleportPlayersToTavern();
			
			// Show tavern UI
			ShowTavernUIRPC();
		}
		
		[Rpc.Broadcast]
		void TransitionToArena()
		{
			CurrentState = GameState.ArenaFight;
			
			Log.Info("Party ready! Teleporting to Arena");
			
			// Teleport all players to arena
			TeleportPlayersToArena();
			
			// Hide tavern UI and spawn boss
			HideTavernUIRPC();
			SpawnBoss();
		}
		
		[Rpc.Broadcast]
		void TransitionToComplete()
		{
			CurrentState = GameState.GameComplete;
			Log.Info("VICTORY! Boss defeated!");
			
			ShowVictoryUIRPC();
		}
		
		void TeleportPlayersToTavern()
		{
			if (!Networking.IsHost) return;
			
			var players = Scene.GetAllComponents<TrinityPlayer>().ToList();
			tavernPlayers.Clear();
			
			for (int i = 0; i < players.Count; i++)
			{
				var player = players[i];
				if (player.IsAlive)
				{
					var spawnPoint = GetTavernSpawnPoint(i);
					player.WorldTransform = spawnPoint;
					tavernPlayers.Add(player);
					
					// Heal to full
					player.Heal(player.MaxHealth);
				}
			}
		}
		
		void TeleportPlayersToArena()
		{
			if (!Networking.IsHost) return;
			
			var players = Scene.GetAllComponents<TrinityPlayer>().ToList();
			
			for (int i = 0; i < players.Count; i++)
			{
				var player = players[i];
				if (player.IsAlive)
				{
					var spawnPoint = GetArenaSpawnPoint(i);
					player.WorldTransform = spawnPoint;
					
					// Reset resources to full
					player.CurrentHealth = player.MaxHealth;
					player.CurrentMana = player.MaxMana;
					player.CurrentResource = player.MaxResource;
				}
			}
		}
		
		Transform GetTavernSpawnPoint(int index)
		{
			if (TavernSpawnPoints.Count == 0)
				return new Transform(Vector3.Zero, Rotation.Identity);
				
			var spawnPoint = TavernSpawnPoints[index % TavernSpawnPoints.Count];
			return spawnPoint?.WorldTransform ?? new Transform(Vector3.Zero, Rotation.Identity);
		}
		
		Transform GetArenaSpawnPoint(int index)
		{
			if (ArenaSpawnPoints.Count == 0)
				return new Transform(new Vector3(0, 0, 100), Rotation.Identity);
				
			var spawnPoint = ArenaSpawnPoints[index % ArenaSpawnPoints.Count];
			return spawnPoint?.WorldTransform ?? new Transform(new Vector3(0, 0, 100), Rotation.Identity);
		}
		
		void SpawnBoss()
		{
			if (!Networking.IsHost) return;
			
			// TODO: Spawn The Reclaimer boss
			Log.Info("Boss spawning system ready - TODO: Implement boss prefab");
		}
		
		[Rpc.Broadcast]
		public void PlayerReady(Connection playerConnection)
		{
			if (CurrentState != GameState.TavernWaiting) return;
			
			var readyPlayers = Scene.GetAllComponents<TrinityPlayer>()
				.Where(p => p.IsAlive)
				.Count(); // TODO: Track individual ready states
				
			// For now, if any player readies up, start arena
			Log.Info($"Player {playerConnection.DisplayName} is ready!");
			AllPlayersReady = true;
		}
		
		[Rpc.Broadcast]
		void ShowTavernUIRPC()
		{
			// Show tavern waiting UI with ready button and timer
			Log.Info("Tavern UI should show - TODO: Fix UI spawning");
		}
		
		[Rpc.Broadcast]
		void HideTavernUIRPC()
		{
			// Remove tavern UI
			Log.Info("Tavern UI should hide - TODO: Fix UI removal");
		}
		
		[Rpc.Broadcast]
		void ShowVictoryUIRPC()
		{
			// Show victory screen
			Log.Info("VICTORY SCREEN - TODO: Implement victory UI");
		}
		
		public float GetTavernTimeRemaining()
		{
			return Math.Max(0, TavernTimer);
		}
		
		public int GetPlayersInTavern()
		{
			return tavernPlayers?.Count ?? 0;
		}
	}
}