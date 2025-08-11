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
			Log.Info($"Player {channel.DisplayName} connected");
			
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
			
			// Add UI component that will show class selection
			var classSelectionUI = tempPlayer.Components.GetOrCreate<ClassSelectionUI>();
			classSelectionUI.SpawnManager = this;
			classSelectionUI.PlayerConnection = channel;
			
			Log.Info($"ClassSelectionUI component added to temp player for {channel.DisplayName}");
		}
		
		public void SelectClassForPlayer(Connection channel, TrinityClassType classType)
		{
			if (!Networking.IsHost) return;
			
			if (!CanSelectClass(classType))
			{
				Log.Warning($"Cannot select {classType} - role limit reached");
				return;
			}
			
			if (connectionToPlayer.TryGetValue(channel, out var oldPlayer) && oldPlayer.IsValid())
			{
				oldPlayer.Destroy();
			}
			
			SpawnPlayerWithClass(channel, classType);
		}
		
		void SpawnPlayerWithClass(Connection channel, TrinityClassType classType)
		{
			var spawnPoint = GetNextSpawnPoint();
			GameObject playerPrefab = GetPrefabForClass(classType);
			
			if (playerPrefab == null || !playerPrefab.IsValid())
			{
				Log.Error($"No prefab found for class {classType}");
				return;
			}
			
			var player = playerPrefab.Clone(spawnPoint);
			player.Name = $"{classType}_{channel.DisplayName}";
			
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
			return classType switch
			{
				TrinityClassType.Tank => TankPrefab,
				TrinityClassType.Healer => HealerPrefab,
				TrinityClassType.DPS => DPSPrefab,
				_ => DefaultPlayerPrefab
			};
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