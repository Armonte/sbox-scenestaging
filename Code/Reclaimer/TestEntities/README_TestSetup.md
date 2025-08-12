# Testing Abby's Dual Gun System

## Quick Test Setup

### 1. Create Test Cube (Damage + Healing Target)
1. Create a new GameObject in your scene
2. Add a **ModelRenderer** component 
3. Set the Model to a simple cube or box
4. Add the **DamageableTestCube** component
5. Set MaxHealth (default: 100)
6. Set materials for visual feedback:
   - **HealthyMaterial** (bright/glowing for 80%+ health)
   - **DamagedMaterial** (dark/cracked for <50% health)  
7. Optionally set **HitSound** and **HealSound**

### 2. Setup Abby Player
1. Create or use existing AbbyHealer prefab
2. Ensure it has **CorkRevolver** and **MilkSpray** components
3. On CorkRevolver, assign **CorkProjectilePrefab** property
4. Set damage values (default: 100 damage per cork)

### 3. Create Cork Projectile Prefab
1. Create new GameObject
2. Add **ModelRenderer** with small sphere/cube model  
3. Add **Rigidbody** component
4. Add **CorkProjectile** component
5. Save as prefab and assign to CorkRevolver

### 4. Test Controls
- **Attack1** (Mouse1): Fire Cork Revolver
- **Attack2** (Mouse2): Milk Spray (heals allies)
- **R**: Active Reload (during reload sequence)

### 5. Test Cube Debug Buttons
The DamageableTestCube has inspector buttons for testing:
- **Take 25 Damage**: Quick damage test  
- **Heal 25 HP**: Quick healing test
- **Heal to Full**: Reset to max health
- **Kill Instantly**: Destroy cube immediately
- **Revive**: Bring back destroyed cube with 50% health

## Expected Behavior

### Cork Revolver:
1. Fire 6 shots, then auto-reload
2. During reload, active reload UI appears
3. Press R in golden zone for Perfect reload (faster + damage bonus)
4. Cork projectiles should hit cube and deal damage
5. Each hit generates milk for Abby

### Milk Spray:
1. Hold Attack2 to spray healing
2. Heals entities (players + test cubes) in 60° cone up to 500 units
3. Consumes milk while active (20/second)
4. Visual spray effect should appear
5. Test cube should change color as it heals (damaged → normal → healthy material)

## Troubleshooting

**Cork doesn't fire:**
- Check CorkProjectilePrefab is assigned
- Verify Rigidbody on projectile prefab
- Check if ammo is empty (auto-reloads)

**No damage on impact:**
- Ensure test cube has DamageableTestCube component
- Check collision detection on projectile
- Verify OnCollisionStart is being called

**Active reload not working:**
- Check Input.Pressed("Reload") binding
- Verify ActiveReloadUI is created during reload

**Milk spray not healing:**
- Test with test cube or another player in range
- Check milk resource (need milk to spray - shoot corks first!)
- Verify cone angle/range settings
- Make sure target needs healing (not at full health)