# S&Box Implementation Guide - Technical Specifications

## Critical S&Box API Patterns

### 1. Networking Architecture

#### Essential Networking Patterns
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

#### NetworkHelper Configuration
The **NetworkHelper component** is your multiplayer foundation:
```csharp
// Add to main scene GameObject
// Configuration:
StartServer = true;        // Auto-creates server on scene load
PlayerPrefab = playerPrefab; // Reference to player prefab
SpawnPoints = spawnPoints;   // List of spawn transforms (optional)
```

### 2. Object Creation and Management

#### S&Box Object Creation Pattern
```csharp
// Create new GameObject
var gameObject = Scene.CreateObject();
gameObject.Name = "MyObject";
gameObject.WorldPosition = spawnPosition;

// Add components
var component = gameObject.Components.Create<MyComponent>();
component.Property = value;

// Destroy objects
GameObject.Destroy();
```

#### Component Lifecycle
```csharp
public class MyComponent : Component
{
    protected override void OnStart()    // Object created and ready
    protected override void OnUpdate()   // Every frame
    protected override void OnDestroy()  // Before object destruction
}
```

### 3. Input System

#### Built-in Input Handling
```csharp
// S&Box built-in inputs
Input.AnalogMove        // WASD/Jo