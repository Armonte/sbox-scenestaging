# Proc Systems and Monetization Design

## Proc System Architecture (Brobaon's Design)

### Core Philosophy
Probability systems should enhance player agency rather than replace it. All proc systems reward skill, coordination, and good decision-making while providing protection against devastating RNG streaks.

---

## Fundamental Proc Mechanics

### Base Proc Chances
**Conservative Foundation**:
- Base percentages: 10-30% to avoid over-reliance
- Scaling bonuses for skill expression and coordination
- Protection systems against consecutive failures
- Maximum proc chance capped at 95% (never guaranteed)

### Coordination Bonuses
**Perfect Timing**: Abilities used within 1 second of each other gain +50% proc chance
**Role Harmony**: Each role maintaining primary function increases team proc rates by 5%
**Clutch Saves**: Emergency abilities during critical moments trigger team-wide buffs

### Progressive Scaling
**Execution Mastery**: Successful mechanical execution increases proc rates temporarily
**Momentum System**: Good plays build "momentum" that enhances future procs for 30 seconds
**Desperation Mode**: When team health drops below 30%, beneficial proc rates double

---

## Anti-Frustration Systems (RuneScape-Inspired)

### Bad Luck Protection
```csharp
if (consecutiveFailures >= 5) { 
    procChance *= 1.5;
    maxFailureStreak = 8; // Hard cap on bad luck
}
```

**Pity Timers**:
- Critical abilities (Execute, Divine Shield procs) have maximum failure streaks
- Important coordination procs guaranteed after extended dry periods
- Ensures mechanical viability isn't entirely RNG dependent

**Baseline Effectiveness**:
- All abilities function adequately without procs
- Procs enhance performance rather than enable basic function
- No encounter mechanics require specific proc outcomes

### Skill Expression Through RNG

**Frame-Perfect Bonuses**:
- Abilities used at optimal moments gain proc bonuses
- Rewards deep encounter knowledge and precise execution
- Creates skill ceiling through probability manipulation

**Coordination Multipliers**:
- Team actions performed in sequence amplify individual proc chances
- Rewards communication and planning
- Creates team skill expression beyond individual performance

---

## Class-Specific Proc Systems

### Leo the Phranklyn (Tank) Procs

#### Shell Parry Enhancements
- **Base Chance**: 25% to reflect damage back to boss
- **Perfect Timing Bonus**: +15% for frame-perfect parries
- **Coordination Bonus**: +10% if used to protect orb carrier

#### Emergency Pull Synergy
- **Base Chance**: 15% to reset all orb timers by 3 seconds
- **Desperation Bonus**: +20% when multiple orbs under 5 seconds
- **Team Coordination**: +10% if healer cleanses debuffs within 2 seconds

#### Stance Management
- **Battle Stance Proc**: 20% chance for temporary defensive stance benefits
- **Defensive Stance Proc**: 15% chance for temporary battle stance benefits
- **Perfect Switch**: 30% chance for 5 seconds of combined benefits

#### Lil Frank Summoning
- **Base Enhancement**: 10% chance Lil Frank gains double damage
- **Family Synergy**: +15% if Abby heals Leo within 3 seconds of summon
- **Coordination**: +20% if summoned during enemy vulnerability window

### Holy Milker Abby (Healer) Procs

#### Milk Gun Mastery
- **Double Effectiveness**: 25% chance for 800 HP heal instead of 400
- **Splash Enhancement**: 20% chance splash heal affects larger radius
- **Resource Efficiency**: 15% chance heal costs no milk

#### Cork Revolver Synergy
- **Critical Milk Build**: 30% chance to generate double milk (20 instead of 10)
- **Chain Shots**: 20% chance cork bounces to nearby enemy
- **Reload Speed**: 25% chance for instant reload

#### Divine Spill Ultimate
- **Extended Duration**: 20% chance invincibility lasts 4 seconds instead of 2
- **Milk Preservation**: 25% chance puddle lasts 20 seconds instead of 10
- **Team Cleanse**: 30% chance also removes all debuffs from team

#### Milksong Synergy
- **Proc Rate Manipulation**: Successfully landing other abilities increases Milksong chance
- **Chain Stuns**: 15% chance successful Milksong triggers again in 3 seconds
- **Area Enhancement**: 20% chance stun radius doubles

#### Milk Portal Network
- **Portal Efficiency**: 15% chance portals last 60 seconds instead of 45
- **Emergency Teleport**: 10% chance failed portal attempt still teleports player
- **Team Speed**: 25% chance teleporting grants movement buff

### Mighty Trunk Warrior (DPS) Procs

#### Trunk Grab Combo
- **Chain Grab**: 30% chance to pull additional nearby enemies
- **Extended Window**: 20% chance combo window extends to 5 seconds
- **Damage Scaling**: 25% chance grabbed enemies take +50% damage from next ability

#### Trunk Slam Mastery
- **Area Enhancement**: 20% chance radius increases to 6 meters
- **Knockup Duration**: 25% chance enemies stay airborne 50% longer
- **Combo Extension**: 15% chance enables second slam within combo window

#### Trunk Whirl Synergy
- **Deflection**: 30% chance deflects projectiles back to enemies
- **Movement Enhancement**: 20% chance can move at full speed during whirl
- **Duration Extension**: 25% chance whirl lasts 6 seconds instead of 4

#### Extendo-Trunk Mobility
- **Double Distance**: 15% chance range extends to 25 meters instead of 15
- **Team Rally**: 25% chance movement buff affects larger radius
- **Damage Amplification**: 20% chance destination AoE damage doubles

#### Trunk Level Scaling Procs
**Levels 1-4**: Basic proc chances as listed above
**Levels 5-8**: +5% to all base proc chances
**Levels 9-11**: +10% to base chances, +15% to coordination bonuses
**Level 12**: +15% to base chances, unique transcendent procs

---

## Team Coordination Proc Systems

### Perfect Synchronization
**Trigger**: Multiple abilities used within 1-second window
**Effects**:
- All abilities in sync window gain +50% proc chance
- Team gains "Momentum" buff increasing all procs for 30 seconds
- Visual and audio feedback celebrates perfect coordination

### Role Synergy Bonuses
**Tank + Healer**: Leo's shell parry while receiving Abby's heal
- **Proc**: 40% chance for both abilities to gain enhanced effects

**Tank + DPS**: Leo's positioning enabling Trunk Warrior combo
- **Proc**: 35% chance Leo's next ability resets cooldown

**Healer + DPS**: Abby's milk building while DPS clears motes
- **Proc**: 30% chance mote kills grant Abby bonus milk

### Clutch Save Mechanics
**Emergency Situations**: Player below 20% health receiving critical heal
- **Proc**: 50% chance emergency abilities gain double effectiveness
- **Team Buff**: Success grants entire team +25% proc chance for 10 seconds

**Perfect Rescue**: Tank saving orb carrier with emergency ability
- **Proc**: 60% chance saved player gains temporary invincibility
- **Momentum**: Team gains extended coordination window

---

## Monetization Integration

### Cash Shop Proc Enhancements

#### Lactaid Pills ($2.99)
**Base Effect**: Removes Leo's lactose intolerance for 24 hours
**Proc Enhancement**: While active, +10% to all Leo's base proc chances
**Team Synergy**: Leo + Abby coordination procs gain +15% bonus

#### Trunk Enhancement Serum ($4.99)
**Base Effect**: +1 trunk level for 48 hours
**Proc Scaling**: Access to higher tier proc chances temporarily
**Unique Procs**: Temporary access to level-specific proc abilities

#### Premium Milk ($1.99)
**Base Effect**: Milk doesn't spoil for 1 hour
**Proc Enhancement**: Milk-related abilities gain +10% proc chance
**Resource Efficiency**: 20% chance abilities cost 25% less milk

### Cosmetic Proc Effects
**Golden Trunk Skin**: Visual enhancement to all trunk-based procs
**Angel Wings Abby**: Divine light effects on successful healing procs
**Flaming Shell Leo**: Fire effects on successful parry procs

### Progression Boosters
**Experience Boost**: Successful procs grant additional XP
**Encounter Skip**: Simulates "perfect proc luck" for completion
**Proc Rate Display**: Shows real-time proc chances (premium feature)

---

## Implementation Guidelines

### Proc Calculation System
```csharp
public class ProcManager : Component
{
    public static float CalculateProcChance(TrinityPlayer player, AbilityType ability)
    {
        float baseChance = GetBaseChance(ability);
        float coordinationBonus = CalculateTeamSynergy(player);
        float momentumBonus = player.GetMomentumBonus();
        float protectionBonus = GetBadLuckProtection(player, ability);
        float cashShopBonus = GetCashShopModifiers(player);
        
        return Math.Clamp(
            baseChance + coordinationBonus + momentumBonus + protectionBonus + cashShopBonus, 
            0f, 0.95f
        );
    }
    
    public static bool CheckProc(TrinityPlayer player, AbilityType ability)
    {
        float chance = CalculateProcChance(player, ability);
        bool success = Random.Shared.Float() <= chance;
        
        UpdateProcHistory(player, ability, success);
        return success;
    }
}
```

### Visual Feedback Systems
**Proc Indicators**: Screen effects for successful procs
**Streak Counters**: UI showing current lucky/unlucky streaks
**Momentum Meters**: Visual representation of coordination bonuses
**Audio Cues**: Satisfying sounds for different proc types

### Performance Optimization
**Proc Caching**: Pre-calculate common proc scenarios
**Network Efficiency**: Batch proc results for network transmission
**Memory Management**: Efficient proc history storage
**Audio Pooling**: Reuse proc sound effects

---

## Balancing Framework

### Success Metrics
**Player Satisfaction**: "That was a clutch proc!" vs "This is just RNG"
**Skill Expression**: Better coordination correlates with better outcomes
**Progression Feel**: Procs enhance advancement without replacing effort
**Team Dynamics**: Procs encourage cooperation over individual play

### Testing Methodology
**Statistical Analysis**: Long-term proc rate tracking
**Player Feedback**: Qualitative satisfaction surveys
**Performance Metrics**: Encounter completion correlation with proc usage
**Monetization Impact**: Purchase behavior analysis

### Continuous Balancing
**Data-Driven Adjustments**: Regular proc rate tuning based on analytics
**Community Feedback**: Player input on proc feel and satisfaction
**Competitive Balance**: Ensuring procs don't break encounter difficulty
**Accessibility**: Maintaining viability for non-paying players

This proc system creates meaningful choices and skill expression while maintaining the excitement and unpredictability that makes encounters memorable, all while supporting sustainable monetization through convenience rather than necessity.