using Sandbox;
using System;
using System.Collections.Generic;

namespace Reclaimer
{
	public sealed class TrinitySpawnManager : Component, Component.INetworkListener
	{
		[Property] public GameObject TankPrefab { get; set; }
		[Property] public GameObject HealerPrefab { get; set; }
		[Property] public GameObject DPSPrefab { get; set; }
		[Property] public GameObject DefaultPlayerPrefab { get; set; }
		[Property] public List<GameObject> SpawnPoints { get; set; } = new();
		
		[Sync] public Dictionary<Guid, TrinityClassType> PlayerClasses { get; set; } = new();
		[Sync] public int TankCount { get; set; }
		[Sync] public int HealerCount { get; set; }
		[Sync] public int DPSCount { get; set; }
		
		private Dictionary<Connection, GameObject> connectionToPlayer = new();
		private int nextSpawnIndex = 0;
		
		protected override void OnStart()
		{
			base.OnStart();
			
			if (Networking.IsHost)
			{
				Log.Info("Trinity Spawn Manager initialized on host");
			}
		}
		
		public void OnActive(Connection channel)
		{
			Log.Info($"Player {channel.DisplayName} connected. Current class counts - Tank:{TankCount}, Healer:{HealerCount}, DPS:{DPSCount}");
			
			// Force sync class counts to all clients (especially the new one)
			SyncClassCountsToClient(TankCount, HealerCount, DPSCount);
			
			ShowClassSelectionUI(channel);
		}
		
		public void OnDisconnected(Connection channel)
		{
			Log.Info($"Player {channel.DisplayName} disconnected");
			
			if (connectionToPlayer.TryGetValue(channel, out var player))
			{
				if (player.IsValid())
				{
					var trinityPlayer = player.Components.Get<TrinityPlayer>();
					if (trinityPlayer != null)
					{
						UpdateClassCount(trinityPlayer.ClassType, -1);
						PlayerClasses.Remove(player.Id);
					}
					
					player.Destroy();
				}
				
				connectionToPlayer.Remove(channel);
			}
		}
		
		void ShowClassSelectionUI(Connection channel)
		{
			SpawnTemporaryPlayer(channel);
		}
		
		void SpawnTemporaryPlayer(Connection channel)
		{
			var spawnPoint = GetNextSpawnPoint();
			var tempPlayer = DefaultPlayerPrefab?.Clone(spawnPoint) ?? Scene.CreateObject();
			
			tempPlayer.Name = $"TempPlayer_{channel.DisplayName}";
			tempPlayer.NetworkSpawn(channel);
			
			connectionToPlayer[channel] = tempPlayer;
			
			Log.Info($"Temp player spawned for {channel.DisplayName}");
			
			// Send RPC to create UI on the target client
			CreateClassSelectionUIForPlayer(channel.DisplayName);
		}
		
		[Rpc.Broadcast]
		void CreateClassSelectionUIForPlayer(string targetPlayerName)
		{
			// Only create UI if this RPC is for the local player
			if (Connection.Local?.DisplayName != targetPlayerName) 
			{
				Log.Info($"RPC not for local player. Target: {targetPlayerName}, Local: {Connection.Local?.DisplayName}");
				return;
			}
			
			Log.Info($"Creating class selection UI for local player: {targetPlayerName}");
			
			// Find the TrinitySpawnManager on this client
			var spawnManager = Scene.GetAllComponents<TrinitySpawnManager>().FirstOrDefault();
			if (spawnManager == null)
			{
				Log.Error("Cannot find TrinitySpawnManager on client");
				return;
			}
			
			Log.Info($"Client sees class counts - Tank:{spawnManager.TankCount}, Healer:{spawnManager.HealerCount}, DPS:{spawnManager.DPSCount}");
			
			// Create a local UI GameObject (not networked)
			var uiObject = Scene.CreateObject();
			uiObject.Name = "ClassSelectionHUD";
			
			// Add ScreenPanel component
			var screenPanel = uiObject.Components.GetOrCreate<ScreenPanel>();
			
			// Add ClassSelectionPanel component first
			var panel = uiObject.Components.GetOrCreate<ClassSelectionPanel>();
			
			// Add ClassSelectionUI component locally
			var classSelectionUI = uiObject.Components.GetOrCreate<ClassSelectionUI>();
			classSelectionUI.SpawnManager = spawnManager;
			classSelectionUI.PlayerConnection = Connection.Local;
			
			// Give the ClassSelectionUI component a reference to its UI object for cleanup
			classSelectionUI.SetUIObject(uiObject);
			
			// Set the ClassSelectionUI reference on the panel component
			if (panel != null)
			{
				panel.SetSelector(classSelectionUI);
				Log.Info($"Panel selector reference set successfully");
			}
			else
			{
				Log.Warning("ClassSelectionPanel component not found on UI object");
			}
			
			Log.Info($"Local ClassSelectionUI created for {targetPlayerName} with SpawnManager: {spawnManager != null} and PlayerConnection: {Connection.Local?.DisplayName}");
		}
		
		[Rpc.Broadcast]
		public void RequestClassSelection(TrinityClassType classType)
		{
			if (!Networking.IsHost) return;
			
			// Find the connection that made this RPC call
			var callerConnection = Rpc.Caller;
			
			Log.Info($"RequestClassSelection RPC called - {callerConnection.DisplayName} wants {classType}");
			
			SelectClassForPlayer(callerConnection, classType);
		}
		
		public void SelectClassForPlayer(Connection channel, TrinityClassType classType)
		{
			if (!Networking.IsHost) return;
			
			Log.Info($"SelectClassForPlayer called - {channel.DisplayName} wants {classType}");
			
			if (!CanSelectClass(classType))
			{
				Log.Warning($"Cannot select {classType} - role limit reached");
				return;
			}
			
			if (connectionToPlayer.TryGetValue(channel, out var oldPlayer) && oldPlayer.IsValid())
			{
				Log.Info($"Destroying old player: {oldPlayer.Name}");
				oldPlayer.Destroy();
			}
			else
			{
				Log.Info("No old player found to destroy");
			}
			
			SpawnPlayerWithClass(channel, classType);
			
			// Check if we should trigger game flow transition
			CheckGameFlowTransition();
		}
		
		void CheckGameFlowTransition()
		{
			var gameFlow = Scene.GetAllComponents<GameFlowManager>().FirstOrDefault();
			if (gameFlow != null && gameFlow.CurrentState == GameState.ClassSelection)
			{
				// Check if we have minimum trinity composition
				if (TankCount >= 1 && HealerCount >= 1 && DPSCount >= 1)
				{
					Log.Info($"Trinity composition achieved! Tank:{TankCount}, Healer:{HealerCount}, DPS:{DPSCount}");
					// Let GameFlowManager handle the transition in its HandleClassSelection method
				}
			}
		}
		
		void SpawnPlayerWithClass(Connection channel, TrinityClassType classType)
		{
			Log.Info($"SpawnPlayerWithClass called - Channel: {channel.DisplayName}, Class: {classType}");
			
			var spawnPoint = GetNextSpawnPoint();
			GameObject playerPrefab = GetPrefabForClass(classType);
			
			Log.Info($"Spawn point: {spawnPoint.Position}, Prefab: {playerPrefab?.Name ?? "NULL"}");
			
			if (playerPrefab == null || !playerPrefab.IsValid())
			{
				Log.Error($"No prefab found for class {classType}");
				return;
			}
			
			var player = playerPrefab.Clone(spawnPoint);
			player.Name = $"{classType}_{channel.DisplayName}";
			
			Log.Info($"Player spawned: {player.Name} at {player.WorldPosition}");
			
			var clothing = new ClothingContainer();
			clothing.Deserialize(channel.GetUserData("avatar"));
			
			if (player.Components.TryGet<SkinnedModelRenderer>(out var body, FindMode.EverythingInSelfAndDescendants))
			{
				clothing.Apply(body);
			}
			
			var nameTag = player.Components.Get<NameTagPanel>(FindMode.EverythingInSelfAndDescendants);
			if (nameTag != null && nameTag.IsValid())
			{
				nameTag.Name = channel.DisplayName;
			}
			
			player.NetworkSpawn(channel);
			
			connectionToPlayer[channel] = player;
			PlayerClasses[player.Id] = classType;
			UpdateClassCount(classType, 1);
			
			Log.Info($"{channel.DisplayName} spawned as {TrinityClassInfo.GetClassName(classType)}");
			
			// Notify game flow manager about class selection
			BroadcastClassSelection(channel.DisplayName, classType);
		}
		
		GameObject GetPrefabForClass(TrinityClassType classType)
		{
			GameObject prefab = classType switch
			{
				TrinityClassType.Tank => TankPrefab,
				TrinityClassType.Healer => HealerPrefab,
				TrinityClassType.DPS => DPSPrefab,
				_ => DefaultPlayerPrefab
			};
			
			// Fallback to default if class prefab is missing
			if (prefab == null || !prefab.IsValid())
			{
				Log.Warning($"Missing prefab for {classType}, falling back to DefaultPlayerPrefab");
				prefab = DefaultPlayerPrefab;
			}
			
			// Final fallback - this should never happen but prevents crashes
			if (prefab == null || !prefab.IsValid())
			{
				Log.Error($"No valid prefab available for {classType} - critical error!");
			}
			
			return prefab;
		}
		
		bool CanSelectClass(TrinityClassType classType)
		{
			const int maxPerClass = 2;
			const int maxTanks = 1;
			
			return classType switch
			{
				TrinityClassType.Tank => TankCount < maxTanks,
				TrinityClassType.Healer => HealerCount < maxPerClass,
				TrinityClassType.DPS => DPSCount < maxPerClass,
				_ => false
			};
		}
		
		TrinityClassType GetFirstAvailableClass()
		{
			if (CanSelectClass(TrinityClassType.Tank)) return TrinityClassType.Tank;
			if (CanSelectClass(TrinityClassType.Healer)) return TrinityClassType.Healer;
			if (CanSelectClass(TrinityClassType.DPS)) return TrinityClassType.DPS;
			
			// Fallback to DPS if all full
			return TrinityClassType.DPS;
		}
		
		void UpdateClassCount(TrinityClassType classType, int delta)
		{
			var oldTank = TankCount;
			var oldHealer = HealerCount; 
			var oldDPS = DPSCount;
			
			switch (classType)
			{
				case TrinityClassType.Tank:
					TankCount = Math.Max(0, TankCount + delta);
					break;
				case TrinityClassType.Healer:
					HealerCount = Math.Max(0, HealerCount + delta);
					break;
				case TrinityClassType.DPS:
					DPSCount = Math.Max(0, DPSCount + delta);
					break;
			}
			
			Log.Info($"Class counts updated: Tank:{oldTank}→{TankCount}, Healer:{oldHealer}→{HealerCount}, DPS:{oldDPS}→{DPSCount}");
			
			// Force sync to all clients
			SyncClassCountsToClient(TankCount, HealerCount, DPSCount);
		}
		
		[Rpc.Broadcast]
		void BroadcastClassCounts()
		{
			Log.Info($"Broadcasting class counts - Tank:{TankCount}, Healer:{HealerCount}, DPS:{DPSCount}");
		}
		
		[Rpc.Broadcast]
		void SyncClassCountsToClient(int tankCount, int healerCount, int dpsCount)
		{
			// Only update counts on clients, not on the host (host already has correct counts)
			if (Networking.IsHost) return;
			
			Log.Info($"SyncClassCountsToClient RPC received - Tank:{tankCount}, Healer:{healerCount}, DPS:{dpsCount}");
			
			// Force update the counts on the client
			TankCount = tankCount;
			HealerCount = healerCount;
			DPSCount = dpsCount;
			
			Log.Info($"Client class counts updated - Tank:{TankCount}, Healer:{HealerCount}, DPS:{DPSCount}");
		}
		
		Transform GetNextSpawnPoint()
		{
			if (SpawnPoints == null || SpawnPoints.Count == 0)
			{
				return new Transform(Vector3.Zero, Rotation.Identity, 1f);
			}
			
			var spawnPoint = SpawnPoints[nextSpawnIndex % SpawnPoints.Count];
			nextSpawnIndex++;
			
			return spawnPoint?.WorldTransform ?? new Transform(Vector3.Zero, Rotation.Identity, 1f);
		}
		
		public int GetTotalPlayerCount()
		{
			return TankCount + HealerCount + DPSCount;
		}
		
		public bool IsPartyComplete()
		{
			return TankCount >= 1 && HealerCount >= 1 && DPSCount >= 1;
		}
		
		[Rpc.Broadcast]
		public void BroadcastClassSelection(string playerName, TrinityClassType classType)
		{
			Log.Info($"[Party] {playerName} has selected {TrinityClassInfo.GetClassName(classType)}");
		}
		
		
	}
}