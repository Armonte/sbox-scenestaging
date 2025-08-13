using Sandbox;
using System;

namespace Reclaimer
{
	/// <summary>
	/// Active Reload System - Player times reload for bonuses/penalties
	/// Creates interactive reload mechanic with visual feedback
	/// </summary>
	public sealed class ActiveReloadComponent : Component
	{
		[Property] public float MarkerSpeed { get; set; } = 25.0f;
		
		// Perfect Zone Configuration
		[Property] public float PerfectZonePosition { get; set; } = 0.5f; // 0.0 = left, 1.0 = right
		[Property] public float PerfectZoneWidth { get; set; } = 0.25f; // 0.15 = 15% of bar width
		[Property] public bool RandomizePerfectZone { get; set; } = false; // Set to true for random positioning
		
		// UI Position Configuration
		[Property] public string BarVerticalPosition { get; set; } = "75%"; // CSS position (75% = lower on screen)
		[Property] public string BarHorizontalPosition { get; set; } = "50%"; // CSS position (50% = center)
		
		// Reload state
		private bool isActive = false;
		private float totalReloadTime;
		private float perfectZoneSize;
		private float perfectZoneCenter;
		private float currentMarkerPosition = 0f;
		private Action<ActiveReloadResult> onComplete;
		private bool playerAttempted = false;
		private float reloadStartTime;
		private GameObject reloadUIObject;
		
		protected override void OnUpdate()
		{
			if (!isActive) return;
			
			// Move the timing marker across the reload bar
			UpdateMarker();
			
			// Check for player input
			if (Input.Pressed("Reload") && !playerAttempted)
			{
				playerAttempted = true;
				AttemptPerfectReload();
			}
			
			// Check if reload time has elapsed (auto-complete)
			if (Time.Now - reloadStartTime >= totalReloadTime)
			{
				if (!playerAttempted)
				{
					CompleteReload(ActiveReloadResult.Good); // No attempt = normal reload
				}
			}
		}
		
		public void StartReload(float baseTime, float zoneSize, Action<ActiveReloadResult> callback)
		{
			isActive = true;
			totalReloadTime = baseTime;
			perfectZoneSize = PerfectZoneWidth; // Use configurable width instead of parameter
			
			// Use configurable position or randomize if enabled
			if (RandomizePerfectZone)
			{
				perfectZoneCenter = Random.Shared.Float(0.3f, 0.7f); // Random position
			}
			else
			{
				perfectZoneCenter = PerfectZonePosition; // Fixed position
			}
			
			onComplete = callback;
			playerAttempted = false;
			currentMarkerPosition = 0f;
			reloadStartTime = Time.Now;
			
			// Create UI for the reload bar
			CreateReloadUI();
			
			Log.Info($"Active reload started - Perfect zone at {perfectZoneCenter:F2} ± {perfectZoneSize/2:F2}, Speed: {MarkerSpeed:F2}");
		Log.Info($"UI Position: V={BarVerticalPosition}, H={BarHorizontalPosition}");
		}
		
		void UpdateMarker()
		{
			// Update perfect zone size dynamically (allows real-time adjustment)
			perfectZoneSize = PerfectZoneWidth;
			
			// Move marker left to right once
			float deltaTime = Time.Delta * MarkerSpeed;
			currentMarkerPosition += deltaTime;
			
			// When marker reaches the end, complete reload automatically
			if (currentMarkerPosition >= 1.0f)
			{
				currentMarkerPosition = 1.0f;
				
				// If player hasn't attempted, auto-complete as "Good" (normal reload)
				if (!playerAttempted)
				{
					CompleteReload(ActiveReloadResult.Good);
				}
			}
		}
		
		void AttemptPerfectReload()
		{
			float perfectMin = perfectZoneCenter - perfectZoneSize / 2f;
			float perfectMax = perfectZoneCenter + perfectZoneSize / 2f;
			
			ActiveReloadResult result;
			float actualReloadTime;
			
			if (currentMarkerPosition >= perfectMin && currentMarkerPosition <= perfectMax)
			{
				// Perfect reload!
				result = ActiveReloadResult.Perfect;
				actualReloadTime = totalReloadTime * 0.5f; // 50% faster
				Log.Info($"PERFECT RELOAD! Marker at {currentMarkerPosition:F2}, zone [{perfectMin:F2}-{perfectMax:F2}]");
			}
			else
			{
				// Missed timing
				result = ActiveReloadResult.Miss;
				actualReloadTime = totalReloadTime * 1.5f; // 50% slower
				Log.Info($"MISSED RELOAD. Marker at {currentMarkerPosition:F2}, zone [{perfectMin:F2}-{perfectMax:F2}]");
			}
			
			// For now, complete immediately (TODO: implement proper delayed callback)
			CompleteReload(result);
		}
		
		void CompleteReload(ActiveReloadResult result)
		{
			isActive = false;
			DestroyReloadUI();
			
			onComplete?.Invoke(result);
			
			// Component will be cleaned up when the weapon is destroyed
		}
		
		void CreateReloadUI()
		{
			// Create simple Razor UI GameObject for the reload bar
			reloadUIObject = Scene.CreateObject();
			reloadUIObject.Name = "SimpleReloadBar";
			
			// Add ScreenPanel for UI rendering
			var screenPanel = reloadUIObject.Components.GetOrCreate<ScreenPanel>();
			
			// Add the simple reload bar component
			var reloadBar = reloadUIObject.Components.GetOrCreate<SimpleReloadBar>();
			reloadBar.Initialize(this);
			
			Log.Info("Simple reload bar created");
		}
		
		void DestroyReloadUI()
		{
			if (reloadUIObject != null && reloadUIObject.IsValid())
			{
				reloadUIObject.Destroy();
				reloadUIObject = null;
			}
		}
		
		// Public getters for UI
		public float MarkerPosition => currentMarkerPosition;
		public float PerfectZoneCenter => perfectZoneCenter;
		public float PerfectZoneSize => perfectZoneSize;
		public bool IsActive => isActive;
		public float ReloadProgress => (Time.Now - reloadStartTime) / totalReloadTime;
	}
}