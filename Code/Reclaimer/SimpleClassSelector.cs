using Sandbox;
using System;

namespace Reclaimer
{
	/// <summary>
	/// Simple class selector that works with existing GameNetworkManager
	/// Attach this to your spawned player to let them choose a class
	/// </summary>
	public class SimpleClassSelector : Component
	{
		[Property] public GameObject LeoTankPrefab { get; set; }
		[Property] public GameObject AbbyHealerPrefab { get; set; }
		[Property] public GameObject TrunkWarriorPrefab { get; set; }
		
		protected override void OnStart()
		{
			base.OnStart();
			
			// Only show for the local player (not proxies)
			if (!IsProxy)
			{
				Log.Info("Press 1 for Leo Tank, 2 for Abby Healer, 3 for Trunk Warrior");
			}
		}
		
		protected override void OnUpdate()
		{
			if (IsProxy) return; // Only local player can select
			
			if (Input.Pressed("Slot1"))
			{
				SelectClass(TrinityClassType.Tank);
			}
			else if (Input.Pressed("Slot2"))
			{
				SelectClass(TrinityClassType.Healer);
			}
			else if (Input.Pressed("Slot3"))
			{
				SelectClass(TrinityClassType.DPS);
			}
		}
		
		void SelectClass(TrinityClassType classType)
		{
			var prefab = GetPrefabForClass(classType);
			if (prefab == null)
			{
				Log.Error($"No prefab set for {classType}!");
				return;
			}
			
			SpawnTrinityClassRPC(classType);
		}
		
		[Rpc.Owner]
		void SpawnTrinityClassRPC(TrinityClassType classType)
		{
			var prefab = GetPrefabForClass(classType);
			if (prefab == null) return;
			
			// Get our connection from the GameObject
			var connection = Network.Owner;
			if (connection == null) return;
			
			// Spawn the trinity class at our position
			var trinityPlayer = prefab.Clone();
			trinityPlayer.WorldPosition = WorldPosition;
			trinityPlayer.WorldRotation = WorldRotation;
			
			// Network spawn for this connection
			trinityPlayer.NetworkSpawn(connection);
			
			// Destroy this selector
			GameObject.Destroy();
			
			Log.Info($"Spawned {TrinityClassInfo.GetClassName(classType)}!");
		}
		
		GameObject GetPrefabForClass(TrinityClassType classType)
		{
			return classType switch
			{
				TrinityClassType.Tank => LeoTankPrefab,
				TrinityClassType.Healer => AbbyHealerPrefab,
				TrinityClassType.DPS => TrunkWarriorPrefab,
				_ => null
			};
		}
	}
}