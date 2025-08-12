# s&box Healing Spray Ability Specification

## Goal
Implement a **Moira-style healing spray** for a player character in s&box (Source 2, C#) that:
- Emits a healing effect in a **cone shape**, not a beam.
- Heals only players in the cone area.
- Healing amount **falls off with distance** (closer targets get more).
- Consumes a **finite resource** while active.
- Applies healing **over time** while the ability is held.
- Requires **aiming skill** to keep allies in the cone.
- Works in **multiplayer** with proper prediction and replication.

---

## Core Behavior
1. **Activation**
   - Ability triggers when player holds a specific input (e.g., `attack1`).
   - Ability stops when input is released or resource runs out.

2. **Detection**
   - Use `Physics.GetEntitiesInSphere()` to find all entities in range.
   - Filter by cone angle:  
     - Get `Vector3.Angle()` between `forward` direction and vector to target.
     - Must be ≤ `coneAngle / 2`.

3. **Healing**
   - Healing rate = `maxHealPerSecond * distanceFalloff * Time.Delta`.
   - Distance falloff: `1 - (distance / maxRange)`.
   - Applies only to valid ally entities.

4. **Resource Management**
   - Each tick reduces resource by `resourceUsagePerSecond * Time.Delta`.
   - If resource ≤ 0, stop healing.

5. **Feedback**
   - Particle system for spray visuals (cone-shaped).
   - Audio cue when healing is connecting.
   - Optional hit indicator for confirmation.

---

## Example s&box C# Implementation

```csharp
float coneAngle = 60f;
float maxRange = 500f;
float maxHealPerSecond = 50f;
float resource = 100f;
float resourceUsagePerSecond = 10f;

public override void Simulate( IClient client )
{
    if ( Input.Down( "attack1" ) && resource > 0 )
    {
        PerformHealSpray();
    }
}

[ServerRpc]
void PerformHealSpray()
{
    Vector3 origin = this.Position;
    Vector3 forward = this.Rotation.Forward;

    var entities = Physics.GetEntitiesInSphere( origin, maxRange );

    foreach ( var ent in entities )
    {
        if ( ent is not Player target ) continue; 
        if ( target == this ) continue;

        Vector3 toTarget = (target.Position - origin);
        float distance = toTarget.Length;
        Vector3 dirToTarget = toTarget.Normal;
        float angleToTarget = Vector3.Angle( forward, dirToTarget );

        if ( angleToTarget > coneAngle * 0.5f ) continue;

        float falloff = 1f - (distance / maxRange);
        float healThisTick = maxHealPerSecond * falloff * Time.Delta;

        target.Health = Math.Min(target.Health + healThisTick, target.MaxHealth);
    }

    resource -= resourceUsagePerSecond * Time.Delta;
    if ( resource <= 0f )
    {
        resource = 0f;
        // Optional: trigger cooldown or disable
    }
}
```

---

## Multiplayer / Networking Considerations
s&box uses **client-side prediction** and **server authority** for gameplay logic.

- **Server Authoritative Healing**  
  Healing calculations should run on the **server** to prevent cheating.
  
- **Client-Side Prediction**  
  The spray visual effects can run client-side immediately when the button is pressed to avoid input lag.

- **RPC Usage**  
  - Use `[ServerRpc]` on the healing logic method.
  - Optionally use `[ClientRpc]` for triggering VFX/SFX for all players in the match.

- **Hit Detection**  
  - Only the server runs `Physics.GetEntitiesInSphere()` and applies health changes.
  - Clients can run the same logic locally for *prediction only*, but the results will be authoritative from the server.

- **Resource Syncing**  
  - Resource amount should be a `[Net]` networked property to sync to clients.
  - Clients can interpolate resource values for UI display.

Example snippet for networking fields:
```csharp
[Net] public float resource { get; set; }
[Net] public bool IsSpraying { get; set; }
```

---

## Implementation Notes
- **Physics Query**: `Physics.GetEntitiesInSphere()` is cheaper than raycasting per target.
- **Angle Filtering**: Ensures true cone-shaped effect.
- **Distance Scaling**: Rewards close-range positioning.
- **Time.Delta Scaling**: Makes healing consistent across frame rates.
- **Networking**: Run healing logic server-side with `[ServerRpc]`; visual effects can be predicted client-side.

---

## References
- s&box docs: [https://sbox.game/dev/doc](https://sbox.game/dev/doc)
- Physics API: `Physics.GetEntitiesInSphere()`
- Vector math: `Vector3.Angle()`
- Networking: `[Net]`, `[ServerRpc]`, `[ClientRpc]`
