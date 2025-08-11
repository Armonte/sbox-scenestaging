# S&Box Development Roadmap - Implementation Priority Order

## PHASE 1: PROJECT FOUNDATION (Days 1-3)

### Step 1: Project Setup & NetworkHelper
**Priority**: CRITICAL - Everything builds on this

```csharp
// 1. Create new S&Box project with multiplayer template
// 2. Add NetworkHelper component to your main scene GameObject
```

**NetworkHelper Configuration**:
- ✅ **StartServer**: `true` (auto-creates server on scene load)
- ✅ **PlayerPrefab**: We'll create this next
- ✅ **SpawnPoints**: Empty for now (will spawn at NetworkHelper location)

### Step 2: Basic Player Prefab

```csharp
// Create GameObject called "Player"
// Add these components to Player prefab:
public class PlayerController : Component
{
    protected override void OnUpdate()
    {
        // Critical S&Box pattern: Only control if not Proxy
        if (IsProxy) return;
        
        // S&Box input system - AnalogMove is built-in
        if (!Input.AnalogMove.IsNearZeroLength)
        {
            WorldPosition += Input.AnalogMove.Normal * Time.Delta * 300.0f;
        }
    }
}
```

**S&Box Specific Notes**:
- `IsProxy` is the key property - only `false` for the player who owns this object
- `Input.AnalogMove` handles WASD/joystick automatically
- `WorldPosition` directly moves the GameObject (no Rigidbody needed for basic movement)

### Step 3: Camera System

```csharp
// Add to PlayerController or separate CameraController
var camera = Scene.GetAllComponents<CameraComponent>().FirstOrDefault();
if (camera != null)
{
    camera.WorldRotation = new Angles(45, 0, 0); // Top-down view
    camera.WorldPosition = WorldPosition + camera.WorldRotation.Backward * 1500;
}
```

**Milestone Check**: You should be able to run the game, have multiple clients connect, and each control their own player character.

---

## PHASE 2: CLASS SELECTION & TRINITY FOUNDATION (Days 4-7)

### Step 4: Class Selection UI

```razor
@using Sandbox.UI

<root class="class-selection">
    <div class="class-grid">
        <div class="class-card" @onclick="@(() => SelectClass(ClassType.Tank))">
            <img src="images/tank.png" />
            <h3>🐢 Leo the Phranklyn</h3>
            <p>Tank - Shell parrying turtle</p>
        </div>
        
        <div class="class-card" @onclick="@(() => SelectClass(ClassType.Healer))">
            <img src="images/healer.png" />
            <h3>🥛 Holy Milker Abby</h3>
            <p>Healer - Milk gun priest</p>
        </div>
        
        <div class="class-card" @onclick="@(() => SelectClass(ClassType.DPS))">
            <img src="images/dps.png" />
            <h3>🐘 Mighty Trunk Warrior</h3>
            <p>DPS - Trunk combo specialist</p>
        </div>
    </div>
</root>

@code {
    void SelectClass(ClassType classType)
    {
        SelectClassRPC(classType);
    }
    
    [Rpc.Broadcast]
    void SelectClassRPC(ClassType classType)
    {
        // Server spawns appropriate class prefab
    }
}
```

### Step 5: Trinity Class Base

```csharp
public abstract class TrinityPlayer : Component
{
    [Property] public float MaxHealth { get; set; } = 1000f;
    [Property] public float MovementSpeed { get; set; } = 300f;
    
    // S&Box networking - [Sync] automatically networks to all clients
    [Sync] public float CurrentHealth { get; set; }
    [Sync] public float CurrentMana { get; set; }
    
    // Abstract methods for class-specific behavior
    public abstract void UseAbility1();
    public abstract void UseAbility2();
    public abstract void UseUltimate();
    
    protected override void OnUpdate()
    {
        if (IsProxy) return; // S&Box networking check
        
        HandleMovement();
        HandleAbilityInput();
    }
    
    void HandleMovement()
    {
        if (!Input.AnalogMove.IsNearZeroLength)
        {
            var movement = Input.AnalogMove.Normal * MovementSpeed * Time.Delta;
            WorldPosition += movement;
        }
    }
    
    void HandleAbilityInput()
    {
        if (Input.Pressed("Attack1")) UseAbility1();
        if (Input.Pressed("Attack2")) UseAbility2();
        if (Input.Pressed("Ultimate")) UseUltimate();
    }
}
```

### Step 6: Specific Class Implementations

**Tank Class (Leo)**:
```csharp
public class LeoTank : TrinityPlayer
{
    [Property] public float ShellParryWindow { get; set; } = 0.5f;
    [Sync] public bool InShell { get; set; }
    [Sync] public bool HasSword { get; set; } = true;
    
    public override void UseAbility1() // Shell Parry
    {
        if (!HasSword) return;
        PerformShellParryRPC();
    }
    
    [Rpc.Broadcast]
    void PerformShellParryRPC()
    {
        InShell = true;
        HasSword = false;
        // Drop sword logic
        // Start parry timer
    }
    
    public override void UseAbility2() // Summon Lil Frank
    {
        SpawnLilFrankRPC();
    }
    
    [Rpc.Broadcast] 
    void SpawnLilFrankRPC()
    {
        var lilFrank = Scene.CreateObject();
        lilFrank.Name = "LilFrank";
        var ai = lilFrank.Components.Create<LilFrankAI>();
        ai.SetTarget(GetNearestEnemy());
    }
}
```

**Healer Class (Abby)**:
```csharp
public class AbbyHealer : TrinityPlayer
{
    [Property] public float MaxMilk { get; set; } = 100f;
    [Sync] public float CurrentMilk { get; set; }
    [Sync] public float MilkSpoilageTimer { get; set; } = 30f;
    
    protected override void OnUpdate()
    {
        base.OnUpdate();
        
        // Milk spoilage system
        if (CurrentMilk > 0)
        {
            MilkSpoilageTimer -= Time.Delta;
            if (MilkSpoilageTimer <= 0)
            {
                CurrentMilk = 0;
                MilkSpoilageTimer = 30f;
            }
        }
    }
    
    public override void UseAbility1() // Milk Gun Heal
    {
        var target = GetTargetedAlly();
        if (target != null && CurrentMilk >= 15)
        {
            HealAllyRPC(target.Id);
        }
    }
    
    [Rpc.Broadcast]
    void HealAllyRPC(Guid targetId)
    {
        var target = Scene.Directory.FindByGuid(targetId)?.Components.Get<TrinityPlayer>();
        if (target != null)
        {
            target.CurrentHealth = Math.Min(target.MaxHealth, target.CurrentHealth + 400);
            CurrentMilk -= 15;
        }
    }
}
```

---

## PHASE 3: BOSS ENCOUNTER FOUNDATION (Days 8-12)

### Step 7: Boss Entity Creation

```csharp
public class BossEntity : Component
{
    [Property] public float MaxHealth { get; set; } = 10000f;
    [Property] public List<Transform> SpawnPoints { get; set; }
    
    [Sync] public float CurrentHealth { get; set; }
    [Sync] public BossPhase CurrentPhase { get; set; }
    [Sync] public float PhaseTimer { get; set; }
    
    protected override void OnStart()
    {
        CurrentHealth = MaxHealth;
        CurrentPhase = BossPhase.Phase1;
        StartPhase1();
    }
    
    protected override void OnUpdate()
    {
        // Only server runs boss logic
        if (!GameNetworkSystem.IsHost) return;
        
        UpdateCurrentPhase();
        CheckPhaseTransitions();
    }
}
```

### Step 8: Volatile Charge System

```csharp
public class VolatileCharge : Component
{
    [Property] public float CountdownTime { get; set; } = 12f;
    [Property] public float ExplosionRadius { get; set; } = 8f;
    
    [Sync] public float TimeRemaining { get; set; }
    [Sync] public bool IsCarried { get; set; }
    [Sync] public Guid CarrierId { get; set; }
    
    protected override void OnStart()
    {
        TimeRemaining = CountdownTime;
    }
    
    protected override void OnUpdate()
    {
        if (!GameNetworkSystem.IsHost) return; // Server authority
        
        TimeRemaining -= Time.Delta;
        
        if (TimeRemaining <= 0)
        {
            ExplodeRPC();
        }
    }
    
    // S&Box interaction system
    protected override void OnInteract(Vector3 hitPosition, Vector3 hitNormal)
    {
        if (IsCarried) return;
        
        var player = GetInteractingPlayer();
        if (player != null)
        {
            PickupOrbRPC(player.Id);
        }
    }
    
    [Rpc.Broadcast]
    void ExplodeRPC()
    {
        // Damage all players in radius - instant kill
        var players = Scene.GetAllComponents<TrinityPlayer>();
        foreach (var player in players)
        {
            float distance = Vector3.DistanceBetween(WorldPosition, player.WorldPosition);
            if (distance <= ExplosionRadius)
            {
                player.CurrentHealth = 0;
            }
        }
        
        GameObject.Destroy();
    }
}
```

---

## PHASE 4: UI SYSTEMS & FEEDBACK (Days 13-16)

### Step 9: Game HUD with S&Box Razor

```razor
@using Sandbox.UI
@inherits PanelComponent

<root class="game-hud">
    <!-- Health Bar -->
    <div class="health-section">
        <div class="health-bar">
            <div class="health-fill" style="width: @GetHealthPercentage()%"></div>
        </div>
        <div class="health-text">@Player?.CurrentHealth / @Player?.MaxHealth</div>
    </div>
    
    <!-- Resource Bar -->
    <div class="resource-section">
        @if (Player is AbbyHealer healer)
        {
            <div class="milk-bar">
                <div class="milk-fill" style="width: @GetMilkPercentage(healer)%"></div>
            </div>
            <div class="spoilage-timer">Spoils in: @healer.MilkSpoilageTimer.ToString("F1")s</div>
        }
    </div>
    
    <!-- Boss Health -->
    <div class="boss-health">
        <div class="boss-bar">
            <div class="boss-fill" style="width: @GetBossHealthPercentage()%"></div>
        </div>
        <div class="boss-name">The Reclaimer</div>
    </div>
</root>

@code {
    TrinityPlayer Player => PlayerState.Local?.GameObject?.Components.Get<TrinityPlayer>();
    BossEntity Boss => Scene.GetAllComponents<BossEntity>().FirstOrDefault();
    
    float GetHealthPercentage()
    {
        if (Player == null) return 0;
        return (Player.CurrentHealth / Player.MaxHealth) * 100f;
    }
}
```

---

## PHASE 5: ADVANCED MECHANICS (Days 17-21)

### Step 10: Proc System Integration

```csharp
public class ProcManager : Component
{
    public static float CalculateProcChance(TrinityPlayer player, AbilityType ability)
    {
        float baseChance = GetBaseChance(ability);
        float coordinationBonus = CalculateTeamSynergy(player);
        float momentumBonus = player.GetMomentumBonus();
        
        return Math.Clamp(baseChance + coordinationBonus + momentumBonus, 0f, 0.95f);
    }
    
    public static bool CheckProc(TrinityPlayer player, AbilityType ability)
    {
        float chance = CalculateProcChance(player, ability);
        return Random.Shared.Float() <= chance;
    }
}
```

### Step 11: Cash Shop Integration

```csharp
public class CashShopManager : Component
{
    [Rpc.Broadcast]
    public static void PurchaseLactaid(Guid playerId)
    {
        var player = Scene.Directory.FindByGuid(playerId)?.Components.Get<LeoTank>();
        if (player != null)
        {
            player.LactoseIntolerant = false;
            player.LactaidTimer = 24f * 3600f; // 24 hours
        }
    }
}
```

---

## CRITICAL S&BOX PATTERNS

### 1. Networking Best Practices
```csharp
// ALWAYS check IsProxy for client input
if (IsProxy) return;

// ALWAYS use [Sync] for networked properties
[Sync] public float Health { get; set; }

// ALWAYS use [Rpc.Broadcast] for actions all clients need to see
[Rpc.Broadcast]
void DealDamage(float amount) { }

// ALWAYS check GameNetworkSystem.IsHost for server-only logic
if (!GameNetworkSystem.IsHost) return;
```

### 2. Object Creation Pattern
```csharp
// S&Box object creation
var gameObject = Scene.CreateObject();
gameObject.Name = "MyObject";
gameObject.WorldPosition = spawnPosition;

// Add components
var component = gameObject.Components.Create<MyComponent>();
component.Property = value;
```

### 3. Input Handling
```csharp
// S&Box built-in inputs
Input.AnalogMove        // WASD/Joystick (Vector3)
Input.AnalogLook        // Mouse look (Vector3)
Input.Pressed("Action") // Custom actions
Input.Down("Action")    // Held down
Input.Released("Action") // Just released
```

### 4. Scene Queries
```csharp
// Find components
var players = Scene.GetAllComponents<TrinityPlayer>();
var camera = Scene.GetAllComponents<CameraComponent>().FirstOrDefault();

// Find by GUID
var obj = Scene.Directory.FindByGuid(objectId);
```

---

## TESTING ROADMAP

### Week 1 Testing
- Multiple clients can connect and select classes
- Basic movement and input works for each class
- NetworkHelper properly spawns players

### Week 2 Testing
- Boss spawns and basic abilities work
- Volatile charges can be picked up and explode
- Health/mana systems sync across clients

### Week 3 Testing
- Full encounter playable start to finish
- All three phases transition correctly
- UI shows accurate information

### Week 4 Testing
- Proc systems work and feel satisfying
- Cash shop items apply correctly
- Performance stable with 6 players

---

## COMMON S&BOX PITFALLS TO AVOID

1. **Don't forget `IsProxy` checks** - Your game will feel broken without them
2. **Don't use regular events across network** - Use RPCs instead
3. **Don't assume client order** - Always validate on server
4. **Don't create objects on clients** - Server authority only
5. **Don't forget `GameNetworkSystem.IsHost`** - Only host runs boss logic

This roadmap leverages S&Box's actual API patterns and will get you from empty project to playable MMO boss encounter in ~3 weeks!