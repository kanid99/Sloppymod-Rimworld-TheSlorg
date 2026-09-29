# The Slorg

*We are the Slorg. Your biological and technological distinctiveness will be added to our own.*

A RimWorld 1.6 mod that adds the **Slorg Collective**, a cybernetic hive-mind enemy faction. Requires **Biotech** and **Harmony**.

## The enemy

The Slorg are a permanent enemy. They don't trade or negotiate. They raid with melee drones that down your colonists and tactical drones with spacer guns.

### Assimilation is an infection
Drones walk up to downed colonists and inject them with nanoprobes. The nanoprobe infection takes about **3 days** to finish:

- Normal tending only slows it (to about 5–6 days).
- A tend of **105%+ quality** purges part of the infection, and two or three of those clear it. That needs **glitterworld medicine and a skilled doctor**.
- If it finishes, the pawn becomes a **Slorg thrall**: collective genes, no cybernetics, and their own look.
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

- **Skills:** in every skill, each drone works at the best level of any drone in the faction, including off-map ones. If they assimilate your Shooting 16 colonist, every drone shoots at 16. Kill that drone and the collective loses it. You get a message when the collective gains or loses a skill in front of you.
- **Stats:** a collective link effect grows with the number of drones on the map. A lone drone is isolated and weakened. A queen on the map counts as 10 extra drones.
- **Traits:** useful traits (industrious, tough, careful shooter, iron-willed and others) spread from the drone that has them to every other drone.

### The queen
- She appears only in **very large raids**: 5000+ points, 50% chance.
- If she's **killed or captured**, every drone on that map is **severed** from the collective. They collapse for about 2 days.
- While a drone is severed, capture it and do the **Sever link** surgery. It needs the *Neural severance* research (in the Slorg research tab), Medicine 10 and 2 glitterworld medicine. The drone becomes a **disconnected drone** you can recruit, with its resistance lowered.
- Severed drones that aren't operated on reconnect when the severance wears off.
- The collective always raises a **new queen**.

### The Unicomplex and the queen core
The faction's oldest surface settlement is renamed the **Unicomplex** and holds the **queen core**, a 3×3 building with 4000 HP. Destroying it:
- permanently disconnects every Slorg on the planet surface. They lose their faction and are left dazed for 2 days, so you can capture and recruit them.
- destroys their other surface settlements.
- stops Slorg raids on the surface. They're still out there in space. Space content for gravships (Odyssey) is planned.

## Player Slorg: disconnected drones
The player can't have linked drones. Any drone that joins you burns out into a **disconnected drone**. Disconnected drones keep the armor, implants and nanoprobes, but lose the link, Assimilate and drone conditioning. You can also pick them as starting colonists.

**Slorg genes can't be passed on.** They are xenogenes (not inherited by children), the gene extractor refuses Slorg pawns, and Slorg pawns can't reimplant a xenogerm.

## Xenotypes

| Xenotype | Key genes |
| --- | --- |
| **Slorg drone** | Collective link (grants Assimilate), subdermal exoplating, ocular implant, nanoprobe swarm, drone conditioning, drone skin, plus never sleep, ageless, disease-free, total tox resistance, reduced pain, dead calm, dark vision, slow runner, ugly |
| **Slorg queen** | Drone genes (without drone conditioning) plus **Hive sovereign** (extra armor, iron-willed, powers the queen mechanics), robust, super-fast healing, extreme psychic ability |
| **Slorg thrall** | Collective link, nanoprobe swarm, drone conditioning, reduced pain, dead calm, partial tox resistance. Keeps their own look, no cybernetics. |
| **Disconnected drone** | Drone genes without collective link, drone conditioning or dead calm, plus **Severed link** (a slightly lower breaking point, genes can't be taken) |

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
