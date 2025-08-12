
# s&box Active Reload System Specification (Pop Gun)

## Overview
This system implements an active reload mechanic for the Pop Gun in s&box. The mechanic rewards precise timing with faster reload speeds or bonus effects and penalizes missed timings with slower reloads. Reloading can be triggered manually or automatically when the weapon runs out of ammo.

## Goals
- **Interactive Reloading**: The player can improve reload performance by timing a key press correctly.
- **Automatic Trigger**: Reload begins automatically when the player runs out of ammo.
- **Manual Trigger**: Player can initiate reload by pressing the reload key (e.g., `R`).
- **Feedback**: Provide clear visual and audio cues to guide the player’s timing.

## Core Mechanics
1. **Triggering Reload**
   - **Manual**: Player presses the reload key.
   - **Automatic**: Reload starts immediately when ammo reaches 0.
2. **Reload Bar Display**
   - Appears on screen when reload starts.
   - Moving marker sweeps across the bar.
   - “Perfect Zone” highlighted for bonus reload timing.
3. **Player Input Check**
   - Player attempts to press reload key again when the marker is within the Perfect Zone.
4. **Results**:
   - **Perfect Reload**: Reload completes faster and grants a small damage buff for the next few shots.
   - **Good Reload**: Standard reload speed (no buff).
   - **Miss Reload**: Reload takes longer than normal.
5. **Cancel Conditions**
   - If the player switches weapons mid-reload, the reload is canceled.

## Technical Design
- **Components**:
  - `PopGun` (weapon logic, ammo count, firing)
  - `ActiveReloadComponent` (reload timing logic, event handling)
  - `ReloadUIComponent` (HUD element showing the reload bar)
- **Events**:
  - `OnReloadStart`
  - `OnReloadAttempt`
  - `OnReloadSuccess`
  - `OnReloadFail`
  - `OnReloadComplete`
- **Timing**:
  - Marker speed is constant per weapon type.
  - Perfect Zone size can be tuned for difficulty.

## Example Flow
1. Player fires Pop Gun until ammo = 0 → Automatic reload starts.
2. Reload bar appears with moving marker.
3. Player presses reload key in the Perfect Zone → Perfect Reload.
4. Reload finishes quickly; player receives short-term damage buff.

## Balancing Variables
- **Perfect Zone Size**: Smaller for harder timing, larger for casual play.
- **Perfect Reload Speed Multiplier**: e.g., 0.5x normal reload time.
- **Miss Reload Penalty**: e.g., 1.5x normal reload time.
- **Buff Duration**: e.g., next 3 shots have +10% damage.

## Visual & Audio
- **UI**: Bright highlight for Perfect Zone, smooth marker animation.
- **Audio**: Satisfying “click” for perfect timing, dull “clunk” for misses.
- **Weapon Animation**: Slightly snappier animation for perfect reloads.

## Testing Plan
- Verify reload starts on key press and auto-triggers at 0 ammo.
- Adjust Perfect Zone timing to feel fair.
- Ensure buffs/penalties apply correctly.
- Test canceling reload when switching weapons.
