# The Slorg

*We are the Slorg. Your biological and technological distinctiveness will be added to our own.*

A RimWorld 1.6 mod that adds the **Slorg Collective**, a cybernetic hive-mind enemy faction. Requires **Biotech** and **Harmony**.

## The enemy

The Slorg are a permanent enemy. They don't trade or negotiate. They raid with melee drones and tactical drones whose weapons down your colonists rather than kill them. **Assault drones** also carry a lethal **plasma lance**, which they use on turrets and mechs, or on anyone once the fight turns against them.

### Assimilation is an infection
Drones walk up to downed colonists and inject them with nanoprobes. The nanoprobe infection runs in three stages and takes about **4–6 hours** to finish. Tending slows it.

- **Stage 1:** any tend of 50%+ quality cures it.
- **Stage 2:** a glitterworld-quality tend (over 100%) knocks it back to stage 1, where another good tend cures it.
- **Stage 3:** only the **Purge nanoprobes** surgery works.
- If it finishes, the pawn becomes a full **Slorg drone**: it drops all its gear, gains the drone implants and goes hairless.
  - On your colony map, they turn hostile right there and try to assimilate the colony from inside.
  - Elsewhere, they join the Slorg and leave.
  - As your prisoner or slave, they either break out hostile or become a hidden sleeper (50/50).

### Sleeper agents and uprisings
- A "cured" infection has a **40% chance** to have only gone dormant. You can't see this on the health tab.
- After a day, the sleeper secretly injects sleeping or isolated colonists ("checking on …"), turning them into sleepers too. Sometimes someone notices.
- Once sleepers are **3 or more and a third of the colony**, they rise up as thralls. **50% chance** a Slorg raid arrives about an hour later to back them up.
- **Counter:** the *Nanoprobe detection* research unlocks the **Nanoprobe scan** surgery, which finds and purges sleepers.

### The collective
Each Slorg faction shares one collective, and it only knows what its living drones know.

- **Designations:** drones carry hive designations like *Three of Nine of Unimatrix 07*, not names. An assimilated colonist's old name is kept on file (the *hive designation* entry on the health tab shows it), and they get it back if they're freed. A drone born to the hive gets a new name, with its old number as a nickname.
- **Adaptive physiology:** every hit a linked drone takes teaches the whole faction. After 12 hits of one damage type, every drone resists 30% of it, then 55% and 75%. A message warns you. If they aren't hit by that damage type for a day, the adaptation fades. Mix your weapons.
- **Skills:** in every skill, each drone works at the best level of any drone in the faction, including off-map ones. If they assimilate your Shooting 16 colonist, every drone shoots at 16. Kill that drone and the collective loses it. You get a message when the collective gains or loses a skill in front of you.
- **Stats:** a collective link effect grows with the number of drones on the map. A lone drone is isolated and weakened. A queen on the map counts as 10 extra drones.
- **Traits:** useful traits (industrious, tough, careful shooter, iron-willed and others) spread from the drone that has them to every other drone.

### The queen
- She appears only in **very large raids**: 5000+ points, 50% chance.
- If she's **killed or captured**, every drone on that map is **severed** from the collective. They collapse for about 2 days.
- While a drone is severed, capture it and do the **Sever link** surgery. It needs the *Neural severance* research (in the Slorg research tab), Medicine 10 and 2 glitterworld medicine. The drone becomes a **disconnected drone** you can recruit, with its resistance lowered.
- Severed drones that aren't operated on reconnect when the severance wears off.
- The collective always raises a **new queen**.
- **Captive queens** can be held on a **queen containment platform** (research *Queen containment*). While it's powered she's in stasis: fully suppressed, no warden needed, and she can still summon drones for you. If the power fails, her suppression drains and she breaks free.

### The Unicomplex and the queen core
One Slorg settlement is secretly the **Unicomplex**. It stays hidden until you trace the hive signal with a queen's control implant. Once it's found, attacking it means facing:
- the **queen core**, a 3×3 building with 4000 HP;
- **the queen herself, always**. There is only ever one queen. If she's away, even mid-raid on your colony, she **transwarps home** the moment you arrive. If she's dead or your prisoner, her successor is waiting. She won't join raids while the Unicomplex is under attack;
- a heavy garrison of **3 assault drones, 2 tactical drones and 3 melee drones**;
- **4 plasma turrets** (lethal), **4 disruptor turrets** (neural shock), regeneration alcoves and hive conduits.

If you kill or capture the queen at the Unicomplex, that's a real win: her successor is raised elsewhere and only appears on your **next** visit. The base can't be defeated while the queen core still stands, however many drones are down.

Destroying the core:
- permanently disconnects every Slorg on the planet surface. They lose their faction and are left dazed for 2 days, so you can capture and recruit them.
- destroys their other surface settlements.
- stops Slorg raids on the surface. They're still out there in space. Space content for gravships (Odyssey) is planned.

## Player Slorg: disconnected drones
The player can't have linked drones. Any drone that joins you burns out into a **disconnected drone**. Disconnected drones keep the armor, implants and nanoprobes, but lose the link, Assimilate and drone conditioning. You can also pick them as starting colonists.

**Slorg genes can't be passed on.** They are xenogenes (not inherited by children), the gene extractor refuses Slorg pawns, and Slorg pawns can't reimplant a xenogerm.

## Xenotypes and implants
Genes are the biology. All Slorg technology is **implants**, and implants can only be cut out by surgery, which destroys them and injures the pawn.

| Xenotype | Key genes |
| --- | --- |
| **Slorg drone** | Collective link (grants Assimilate), nanoprobe swarm, drone conditioning, drone skin, plus never sleep, ageless, disease-free, total tox resistance, reduced pain, dead calm, dark vision, slow runner, ugly |
| **Slorg queen** | Drone genes (without drone conditioning) plus **Hive sovereign**, robust, super-fast healing, extreme psychic ability |
| **Slorg thrall** | Collective link, nanoprobe swarm, drone conditioning, reduced pain, dead calm, partial tox resistance. Keeps their own look. **No implants.** |
| **Disconnected drone** | Drone genes without collective link or drone conditioning, plus **Severed link**. Keeps whatever implants they had. |

| Implant | Effect |
| --- | --- |
| Ocular implant (eye) | Better sight, faster aiming |
| Dermal plating (body) | Bolted-on armor plates. Removal: massive scarring, 25% death |
| Shield emitter (torso) | Blocks ranged and explosive damage until it overloads |
| Assimilation tubules (hand) | Infections start much further along |
| Beam emitter (arm) | Cutting beam ranged ability |
| Arm blade (arm) | Built-in melee weapon |
| Tactical cortex (brain) | +4 Shooting, +4 Melee |

Colonists who still carry implants very rarely hear **the collective's call** and try to walk off to rejoin it.

## Testing
See **[TESTING.md](TESTING.md)**: setup, dev-mode debug actions ("The Slorg" category), and a checklist for every mechanic.

## Tuning
- `Defs/SlorgDefs/Slorg_Collective.xml`: refresh rate, queen raid threshold and chance, queen bonus, shareable traits.
- `Defs/HediffDefs/Hediffs_Slorg.xml`: infection speed, purge threshold, severance length.
- `Defs/HediffDefs/Hediff_CollectiveLink.xml`: stat stages by collective size.
- `Defs/FactionDefs/Faction_Slorg.xml` and `Defs/PawnKindDefs/PawnKinds_Slorg.xml`: raid composition and gear.

## Building
The compiled DLL is committed, so you can drop the folder into `RimWorld/Mods` as is. To rebuild after changing the C#:

```
cd Source/TheSlorg
dotnet build -c Release
```

The build writes to `1.6/Assemblies/`. RimWorld's reference assemblies come from the `Krafs.Rimworld.Ref` NuGet package, so you don't need a local game install to compile.
