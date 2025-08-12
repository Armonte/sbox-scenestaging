# Cork Revolver Hand Attachment Setup

## S&Box Testbed Pattern
Following the exact structure from the networking scene testbed gun:

```
Body → pelvis → spine_0 → spine_1 → spine_2 → clavicle_r → arm_upper_r → arm_lower_r → hand_r → hold_r
└── CorkRevolverGun (GameObject)
    ├── CorkRevolver (Component) - Our weapon logic
    │   └── CorkProjectilePrefab (Property) → Points to Cork Projectile Prefab
    ├── RevolverBarrel (Child GameObject) - Visual part 1
    │   └── ModelRenderer (scaled/positioned for barrel)
    └── RevolverCylinder (Child GameObject) - Visual part 2
        └── ModelRenderer (scaled/positioned for cylinder)
```

## Setup Steps

### 1. Create Cork Projectile Prefab First
1. Create new GameObject: "CorkProjectile"
2. Add **Rigidbody** component
3. Add **SelfDestructComponent** (set Seconds to 5)
4. Add **CorkProjectile** component (our custom script)
5. Add child GameObject: "CorkVisual"
   - Add **ModelRenderer** (small cube/sphere for cork)
   - Add **Collider** - Box (for collision detection)
6. Save as prefab

### 2. Create Cork Revolver Gun Object
1. Navigate to: `Body → pelvis → spine_0 → spine_1 → spine_2 → clavicle_r → arm_upper_r → arm_lower_r → hand_r → hold_r`
2. Create child GameObject: "CorkRevolverGun"
3. Add **CorkRevolver** component
4. Set CorkRevolver properties:
   - **CorkProjectilePrefab**: Drag the prefab from step 1
   - **Damage**: 100
   - **MilkPerHit**: 10
   - **MaxAmmo**: 6
   - **ReloadTime**: 2.0

### 3. Create Visual Gun Parts
Under CorkRevolverGun, create two child objects:

#### RevolverBarrel:
1. Create child GameObject: "RevolverBarrel"
2. Add **ModelRenderer**
3. Set Model to cube/cylinder
4. Scale and position to look like gun barrel
5. Set material (dark metal)

#### RevolverCylinder:
1. Create child GameObject: "RevolverCylinder"  
2. Add **ModelRenderer**
3. Set Model to cylinder
4. Scale and position to look like revolver cylinder
5. Set material (dark metal)

### 4. Verify AbbyHealer Integration
1. Ensure AbbyHealer GameObject has:
   - **CorkRevolver** component reference (automatically found by Components.Get)
   - **MilkSpray** component
   - Proper networking setup

## Expected Hierarchy
```
AbbyHealer (GameObject)
├── AbbyHealer (Component)
├── CorkRevolver (Component) - Found automatically 
├── MilkSpray (Component) - Found automatically
└── Body
    └── pelvis
        └── spine_0
            └── spine_1
                └── spine_2
                    └── clavicle_r
                        └── arm_upper_r
                            └── arm_lower_r
                                └── hand_r
                                    └── hold_r
                                        └── CorkRevolverGun
                                            ├── CorkRevolver (Component)
                                            ├── RevolverBarrel (Child)
                                            └── RevolverCylinder (Child)
```

## Key Points

1. **Component Location**: CorkRevolver component goes on the gun object in hold_r, NOT on the main AbbyHealer
2. **Auto-Discovery**: AbbyHealer finds CorkRevolver using `Components.Get<CorkRevolver>()` which searches the entire GameObject hierarchy
3. **Projectile Spawning**: Cork projectiles spawn from the gun's world position
4. **Visual Separation**: Gun visuals are separate child objects for easy modeling
5. **Collision Detection**: Cork projectiles have colliders and detect hits using OnCollisionStart

## Testing
1. **Fire**: Attack1 should fire cork projectiles from gun position
2. **Reload**: After 6 shots, active reload UI should appear
3. **Hit Detection**: Corks should damage DamageableTestCube and generate milk
4. **Hand Attachment**: Gun should follow hand movement correctly

## Troubleshooting

**Gun not found by AbbyHealer:**
- Check CorkRevolver component is on gun object in hold_r hierarchy
- Verify AbbyHealer.OnStart() calls Components.Get<CorkRevolver>()

**Corks not firing:**
- Check CorkProjectilePrefab is assigned
- Verify Rigidbody on projectile prefab
- Check firing position (should be gun's WorldPosition)

**Collision not working:**
- Ensure cork projectile has Collider component
- Verify CorkProjectile.OnCollisionStart method
- Check DamageableTestCube has IReclaimerDamageable interface