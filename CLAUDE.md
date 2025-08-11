# Claude Context - The Reclaimer MMO Boss Rush Project

## Project Status & Context

**Current Phase**: Moving to Claude Code for implementation  
**Engine**: S&Box (Facepunch Studios)  
**Language**: C# with Source 2 backend  
**Target**: 3-6 player MMO boss encounter prototype  
**Timeline**: 21-day sprint development  

## S&Box Engine Information

**Official Documentation**: https://sbox.game/dev/doc/  
**Architecture**: Scene/GameObject/Component system (Unity-like)  
**Networking**: Built-in multiplayer with hot-reloading  
**UI System**: Razor panels (HTML/CSS-like with C# code-behind)  

### Critical S&Box Patterns You Must Follow

#### 1. Networking Fundamentals
- **Always check `IsProxy`** before handling client input
- **Use `[Sync]` properties** for automatic client synchronization
- **Use `[Rpc.Broadcast]`** for server-to-all-clients communication
- **Check `GameNetworkSystem.IsHost`** for server-only logic

#### 2. Essential Components
- **NetworkHelper**: Core multiplayer component for player spawning
- **Component**: Base class for all game logic (like MonoBehaviour in Unity)
- **Scene.CreateObject()**: Standard object creation pattern
- **Scene.GetAllComponents<T>()**: Find components across scene

#### 3. Input System
- **Input.AnalogMove**: WASD/joystick movement (Vector3)
- **Input.Pressed("Action")**: Custom action bindings
- **Input.Down("Action")**: Held input detection

## Project Overview

### Game Concept
**"The Reclaimer"** - MMO-style boss rush with unique trinity classes requiring coordinated gameplay. Features traditional tank/DPS/healer roles with unconventional character designs and mechanics.

### Unique Trinity Classes

#### 🐢 Leo the Phranklyn (Tank)
- **Shell parry system**: Drop sword to enter shell, timing-based defense
- **180° head rotation**: Battle vs Defensive stance switching
- **Lactose intolerant**: Reduced healing from Abby (cash shop fix available)
- **Summon Lil Frank**: Child with Abby, spinning attack ally

#### 🥛 Holy Milker Abby (Healer)  
- **Dual gun system**: Milk gun (healing) + Cork revolver (damage/resource)
- **Milk spoilage**: Must use milk within 30 seconds or lose it
- **Divine Spill**: Falls over crying, AoE heal + invincibility
- **Milksong**: 1/100 proc chance stun with "I'm coming!" audio
- **Milk portals**: Team teleportation network

#### 🐘 Mighty Trunk Warrior (DPS)
- **12-level trunk progression**: 30cm → 200cm (6.5 feet!)
- **Trunk pun obsession**: Replaces profanity with "trunk"
- **Combo system**: Trunk Grab → Trunk Slam → advanced abilities
- **Ultimate power**: Level 12 "Transcendent Trunk" can end encounters

### Boss Encounters

#### The Reclaimer (Primary Boss)
**Multi-phase mechanical guardian requiring perfect coordination:**

1. **Volatile Charges**: Timed explosive orbs (12s countdown, instant group wipe)
2. **Ephemeral Motes**: Add waves that buff boss if not intercepted
3. **Planetcracker Beams**: Rotating laser walls forcing coordinated movement
4. **Reclaim Shield**: DPS burn phases with team pull mechanics
5. **Eternity Overdrive**: Soft enrage with escalating pressure

**Phase Flow**: Foundation → Complexity → Burn (with traversal breaks)

#### Orc Chieftain (Secondary Boss)
- **Dimensional Shift**: Portal to shadow realm every 25% HP
- **DPS check phases**: Kill shadow boss or realm explodes
- **Goblin reinforcements**: Melee chargers + arrow rain

### Advanced Systems

#### Proc System (Brobaon's Design)
- **RuneScape-inspired** probability mechanics
- **Bad luck protection**: Prevents devastating failure streaks
- **Coordination bonuses**: Team synergy increases proc rates
- **Skill expression**: Better play correlates with better RNG

#### Cash Shop Integration
- **Lactaid Pills** ($2.99): Removes Leo's lactose intolerance
- **Trunk Enhancement** ($4.99): Temporary trunk level boost
- **Premium Milk** ($1.99): Prevents Abby's milk spoilage

## S&Box Implementation Requirements

### Project Structure
```
/code/
  /entities/     - Boss, orbs, motes
  /players/      - Trinity class implementations  
  /systems/      - Proc manager, cash shop
  /ui/           - Razor panels for HUD
  /networking/   - RPC handlers, sync systems
```

### Core Components Needed

#### NetworkHelper Setup
```csharp
// Add to main scene GameObject
StartServer = true;        // Auto-server creation
PlayerPrefab = playerPrefab; // Trinity class prefab
SpawnPoints = spawnPoints;   // Optional spawn locations
```

#### Trinity Base Class
```csharp
public abstract class TrinityPlayer : Component
{
    [Sync] public float CurrentHealth { get; set; }
    [Sync] public float CurrentMana { get; set; }
    
    protected override void OnUpdate()
    {
        if (IsProxy) return; // Essential networking check
        HandleInput();
    }
}
```

#### Boss Entity Pattern
```csharp
public class BossEntity : Component
{
    [Sync] public float CurrentHealth { get; set; }
    [Sync] public BossPhase CurrentPhase { get; set; }
    
    protected override void OnUpdate()
    {
        if (!GameNetworkSystem.IsHost) return; // Server-only logic
        UpdateBossAI();
    }
}
```

#### Volatile Charge System
```csharp
public class VolatileCharge : Component
{
    [Sync] public float TimeRemaining { get; set; } = 12f;
    [Sync] public bool IsCarried { get; set; }
    
    protected override void OnUpdate()
    {
        if (!GameNetworkSystem.IsHost) return;
        
        TimeRemaining -= Time.Delta;
        if (TimeRemaining <= 0) ExplodeRPC();
    }
    
    [Rpc.Broadcast]
    void ExplodeRPC()
    {
        // Instant group wipe on explosion
        var players = Scene.GetAllComponents<TrinityPlayer>();
        foreach (var player in players)
        {
            if (Vector3.DistanceBetween(WorldPosition, player.WorldPosition) <= 800f)
                player.CurrentHealth = 0;
        }
        GameObject.Destroy();
    }
}
```

## Starting Point: sbox-scene-staging Repository

### Repository Context
**Base Repository**: sbox-scene-staging  
**Contains**: S&Box editor examples and scene setups  
**Our Approach**: Modify existing examples to build The Reclaimer  

### Leveraging Existing Examples
The sbox-scene-staging repo contains various S&Box editor examples that we can adapt:

#### Useful Examples to Examine
- **Multiplayer templates**: NetworkHelper implementations
- **Player controllers**: Movement and input handling patterns  
- **UI examples**: Razor panel implementations
- **Physics examples**: Interaction and collision systems
- **Component patterns**: Proper S&Box component architecture

#### Files to Focus On
- **Network-related scenes**: Study multiplayer setup patterns
- **Player prefabs**: Base player controller implementations
- **UI scenes**: Razor panel structure and styling
- **Component examples**: Proper networking and lifecycle patterns

### Adaptation Strategy

#### Phase 1: Foundation (Days 1-3)
1. **Study existing multiplayer scene** in sbox-scene-staging
2. **Modify player controller** to support trinity class selection
3. **Add NetworkHelper** with our class prefabs
4. **Basic movement and camera** working with 3 players

#### Phase 2: Trinity Classes (Days 4-7)  
1. **Extend existing player controller** into trinity base class
2. **Create Leo, Abby, Trunk prefabs** inheriting from trinity base
3. **Implement class-specific abilities** using S&Box patterns
4. **Add class selection UI** using existing Razor examples

#### Phase 3: Boss Implementation (Days 8-12)
1. **Create boss entity** using component patterns from examples
2. **Implement Volatile Charge system** with interaction examples
3. **Add Planetcracker beams** using physics/collision examples  
4. **Build encounter flow** with phase management

## S&Box Documentation References

**Always reference official documentation**: https://sbox.game/dev/doc/

### Key Documentation Sections
- **Networking & Multiplayer**: https://sbox.game/dev/doc/networking-multiplayer/
- **Components**: https://sbox.game/dev/doc/the-scene-system/components/
- **GameObject**: https://sbox.game/dev/doc/the-scene-system/gameobject/
- **Input System**: https://sbox.game/dev/doc/input/
- **UI (Razor)**: https://sbox.game/dev/doc/ui/
- **Physics**: https://sbox.game/dev/doc/physics/

### Critical Documentation to Check
1. **NetworkHelper component** - Core multiplayer foundation
2. **Component lifecycle methods** - OnStart, OnUpdate, OnDestroy
3. **RPC system** - [Rpc.Broadcast] and networking patterns
4. **Sync properties** - [Sync] attribute for networked data
5. **Scene queries** - Scene.GetAllComponents and object finding
6. **Input handling** - Input.AnalogMove, Input.Pressed patterns
7. **Razor panels** - UI system with HTML/CSS-like syntax

## Team Member Roles & Expertise

### Development Team
- **monte (10%)**: S&Box integration, performance optimization, build pipeline
- **Claude (90%)**: Primary implementation using S&Box patterns
- **Radakai**: Combat feel validation, LoL-inspired mechanics (pre-2012 champions)
- **ComfortingSmile**: MMO authenticity, WoW/POE trinity validation  
- **Brobaon (MR. FOSTER)**: RuneScape-inspired proc systems
- **CatSugoKyam**: Creative design and visual concepts
- **MOONJUMP**: Audio systems and dynamic music

### Authority Structure
- **Radakai**: Final say on combat timing and skill expression
- **ComfortingSmile**: MMO authenticity and UI/UX validation
- **Brobaon**: All probability and proc-related mechanics
- **monte**: Technical architecture and S&Box best practices

## Implementation Priorities

### Week 1: Foundation (CRITICAL)
1. **NetworkHelper setup** - Get multiplayer working
2. **Basic trinity classes** - Leo, Abby, Trunk with basic abilities  
3. **Class selection UI** - Razor panel for character choice
4. **Basic boss entity** - Spawning and health tracking

### Week 2: Core Mechanics  
1. **Volatile Charge system** - Pickup, carry, dispose, explode
2. **Mote wave system** - Spawning, pathing, shield mechanics
3. **Planetcracker beams** - Rotating collision detection
4. **Shield phases** - Team coordination DPS checks

### Week 3: Advanced Systems
1. **Proc system integration** - Brobaon's probability mechanics
2. **Cash shop framework** - Lactaid, trunk boosts, premium milk
3. **UI polish** - Complete HUD with class-specific elements
4. **Performance optimization** - 6-player stability

## Technical Constraints & Requirements

### S&Box Limitations to Remember
- **No browser storage APIs** (localStorage/sessionStorage not supported)
- **Server authority required** for all gameplay logic
- **Component-based architecture** - avoid traditional OOP inheritance
- **Hot-reloading friendly** - code changes apply instantly

### Performance Targets
- **60+ FPS** with 6 players and all mechanics active
- **<100ms** network latency for ability responses  
- **<2GB** total memory usage
- **Stable networking** under full coordination load

### Code Quality Guidelines (Crunch Mode)
- **Function over form** - Working code over perfect architecture
- **NetworkHelper first** - Build on solid multiplayer foundation
- **[Sync] everything** - Use S&Box networking, don't reinvent
- **IsProxy checks** - Essential for client input handling
- **GameNetworkSystem.IsHost** - Server-only logic

## Success Criteria

### Technical Milestones
- [ ] 3+ players can connect and select classes
- [ ] Trinity roles feel distinct and essential  
- [ ] Boss encounter completable with coordination
- [ ] Proc systems enhance without dominating gameplay
- [ ] Performance stable with 6 players

### Design Validation
- [ ] Each trinity role has clear identity and purpose
- [ ] Encounter requires actual team coordination
- [ ] Proc systems reward skill over pure luck
- [ ] Cash shop feels convenient, not mandatory
- [ ] Boss mechanics create memorable moments

## Next Steps for Claude Code

1. **Clone/examine sbox-scene-staging repository**
2. **Study existing multiplayer examples** and NetworkHelper usage
3. **Reference S&Box documentation** at https://sbox.game/dev/doc/ for any uncertainties
4. **Start with NetworkHelper setup** and basic player controller modification
5. **Build incrementally** using established S&Box patterns from examples

**Remember**: Always check the official S&Box documentation for current API patterns and best practices. The engine is actively developed, so examples in sbox-scene-staging represent current implementation standards.

**Project Goal**: Create a working MMO boss encounter prototype that demonstrates the unique trinity system, coordinated mechanics, and memorable character designs that make The Reclaimer special.