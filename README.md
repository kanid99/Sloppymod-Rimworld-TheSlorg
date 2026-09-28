# The Slorg

*We are the Slorg. Your biological and technological distinctiveness will be added to our own.*

A RimWorld 1.6 mod that adds the **Slorg**, a cybernetic hive-mind xenotype. Requires **Biotech** and **Harmony**.

## Features

### The Slorg xenotype
Pale, bald and gaunt cybernetic drones. They don't sleep, age or get sick. They're calm in combat and slow on their feet, and they're useless at negotiating or trading. The xenotype isn't inheritable: the Slorg don't breed, they assimilate.

| Gene | Effect |
| --- | --- |
| Collective link | Links the drone to the collective and grants **Assimilate** |
| Subdermal exoplating | +20% sharp armor, +10% blunt armor |
| Ocular implant | Aiming time ×0.8 |
| Nanoprobe swarm | Wound healing ×2, immunity gain ×1.5 |
| Drone conditioning | Mental break threshold −10%. Social impact, negotiation and trade prices ×0.5 |
| Drone skin | Pallid grey skin |

Plus the vanilla genes: bald, no beard, gaunt head, never sleep, ageless, disease-free, total toxic resistance, reduced pain, dead calm, dark vision, slow runner and ugly.

### Assimilate
A touch ability. Inject a **downed** person or one of **your prisoners** with nanoprobes. They become a Slorg drone and join your faction. Half-day cooldown.

### The collective
Every drone in a faction on a map or in a caravan belongs to that faction's collective. The collective has no knowledge of its own. It only has what its drones carry.

- **Skills.** In every skill, each drone works at the level of the best drone in the collective. If you assimilate a pawn with Shooting 14 and the collective's best was 8, every drone now shoots at 14. Drones keep their own skill levels and XP underneath. If that pawn dies, is captured or leaves, the collective drops back to the next best drone. You get a message whenever the collective gains or loses a skill this way.
- **Stats.** Each drone gets a *collective link* health effect that scales with the number of linked drones:
  - **Isolated (1 drone):** −10% consciousness, −10% work speed, −20% learning, easier mental breaks.
  - **Cluster (2+):** small work-speed and mental-break bonuses.
  - **Unimatrix (5+):** bigger bonuses, plus consciousness.
  - **Cube (10+):** bigger again, plus manipulation.
  - **Full collective (20+):** biggest bonuses, plus moving.

  Hover over the effect to see which drone provides each skill.
- **Traits.** Some useful traits spread from the drone that has them to every other drone, unless they clash with a trait that drone already has. When the source drone is gone, the lent trait goes away. The shareable traits are industrious, hard worker, tough, nimble, careful shooter, fast learner, great memory, quick sleeper, fast walker, jogger, steadfast and iron-willed.

Prisoners and slaves are cut off from the collective.

Tuning (the refresh rate and the list of shareable traits) is in `Defs/SlorgDefs/Slorg_Collective.xml`. The stat stages are in `Defs/HediffDefs/Hediff_CollectiveLink.xml`.

## Layout

```
About/                 mod metadata + preview
Defs/                  genes, xenotype, ability, hediff, collective tuning
Textures/              icons
1.6/Assemblies/        compiled TheSlorg.dll
Source/TheSlorg/       C# source
LoadFolders.xml
```

## Building

The compiled DLL is committed, so you can drop the folder into `RimWorld/Mods` as is. To rebuild after changing the C#:

```
cd Source/TheSlorg
dotnet build -c Release
```

The build writes to `1.6/Assemblies/`. RimWorld's reference assemblies come from the `Krafs.Rimworld.Ref` NuGet package, so you don't need a local game install to compile.
