# Editor Setup - Cork Revolver (Step by Step)

## Phase 1: Create Cork Projectile Prefab

### Step 1: Create Cork Projectile
1. **Create new GameObject** in scene: "CorkProjectile"
2. **Add Components** (in this order):
   - **Rigidbody** (Mass: 1, Drag: 0.1)
   - **SelfDestructComponent** (Seconds: 5)
   - **CorkProjectile** (our custom component)

### Step 2: Add Visual + Collision
1. **Create Child GameObject**: "CorkVisual"
2. **Add to CorkVisual**:
   - **ModelRenderer** (Model: models/dev/dev_primitive_cube.vmdl)
   - **BoxCollider** (Size: 1,1,1)
3. **Scale CorkVisual**: Transform scale to (0.1, 0.1, 0.1) - make it small like a cork

### Step 3: Save as Prefab
1. **Drag CorkProjectile** from scene hierarchy to Project/Assets folder
2. **Name**: "CorkProjectile.prefab"
3. **Delete** the CorkProjectile from scene (we only need the prefab)

## Phase 2: Create Test Target

### Step 4: Create Damageable Test Cube
1. **Create new GameObject**: "TestCube"
2. **Add Components**:
   - **ModelRenderer** (Model: models/dev/dev_primitive_cube.vmdl)
   - **DamageableTestCube** (our custom component)
3. **Set DamageableTestCube properties**:
   - MaxHealth: 100
   - (Leave materials empty for now - we'll add visual feedback later)

## Phase 3: Setup Abby Player

### Step 5: Create/Find Abby Player
**If you have existing AbbyHealer prefab:**
- Use that and skip to Step 6

**If creating new:**
1. **Create GameObject**: "AbbyHealer"  
2. **Add Components**:
   - **AbbyHealer** (our component)
   - **MilkSpray** (our component)
3. **Add basic body hierarchy** (or copy from existing player)

### Step 6: Navigate to Hand Position
1. **Expand AbbyHealer hierarchy**
2. **Navigate**: Body → pelvis → spine_0 → spine_1 → spine_2 → clavicle_r → arm_upper_r → arm_lower_r → hand_r → hold_r
3. **If hold_r doesn't exist**: Create empty GameObject "hold_r" under hand_r

### Step 7: Create Gun Object in Hand
1. **Right-click hold_r** → Create Empty Child
2. **Name**: "CorkRevolverGun"
3. **Add Component**: **CorkRevolver** (our component)

### Step 8: Configure Cork Revolver
**On CorkRevolverGun's CorkRevolver component:**
- **CorkProjectilePrefab**: Drag the CorkProjectile.prefab here
- **Damage**: 100
- **MilkPerHit**: 10
- **MaxAmmo**: 6
- **ReloadTime**: 2
- **ProjectileSpeed**: 1000

### Step 9: Add Gun Visuals (Optional)
1. **Create Child of CorkRevolverGun**: "RevolverBarrel"
   - Add **ModelRenderer** (cube model)
   - **Scale**: (0.05, 0.05, 0.2) - long thin barrel
   - **Position**: (0, 0, 0.1) - forward from gun center

2. **Create Child of CorkRevolverGun**: "RevolverCylinder"  
   - Add **ModelRenderer** (cube model)
   - **Scale**: (0.1, 0.1, 0.1) - cylinder shape
   - **Position**: (0, 0, -0.1) - behind barrel

## Phase 4: Test Setup

### Step 10: Position Everything
1. **Place TestCube** in scene where you can see it
2. **Place AbbyHealer** facing the TestCube
3. **Make sure CorkRevolverGun** is positioned in hand_r → hold_r

### Step 11: Test in Game
1. **Start the scene**
2. **Left-click (Attack1)** to fire cork
3. **Look for**:
   - Cork cubes flying toward crosshair
   - TestCube taking damage (check console logs)
   - After 6 shots, active reload UI should appear
   - Press **R** during reload for timing bonus

## Expected Results

✅ **Cork fires**: Small cubes fly from gun position  
✅ **Collision works**: Corks hit TestCube and deal damage  
✅ **Milk generates**: Console shows milk generation messages  
✅ **Reload system**: After 6 shots, reload UI appears  
✅ **Auto-reload**: If you don't time it, reload completes automatically  

## Troubleshooting

**❌ Cork doesn't fire:**
- Check CorkProjectilePrefab is assigned on CorkRevolver
- Verify Rigidbody on cork projectile prefab
- Check console for errors

**❌ No collision/damage:**
- Verify TestCube has DamageableTestCube component
- Check cork prefab has BoxCollider on CorkVisual child
- Make sure CorkProjectile component is on root of prefab

**❌ Reload UI doesn't appear:**
- Fire 6 shots first (check ammo count in inspector)
- Look for ActiveReloadUI GameObject being created in scene

**❌ Cork flies in wrong direction:**
- CorkRevolver uses camera forward direction
- Make sure scene has active camera component

## Next Phase: Milk Spray
Once corks are working, we'll add:
1. **Left-hand milk spray effect**
2. **Cone visualization**  
3. **Healing the TestCube**
4. **Visual milk spray particles**

Let me know when corks are firing and we'll move to milk spray setup! 🚀