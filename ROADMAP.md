# The Slorg: roadmap and ideas

Ideas for after the current systems are playtested. Nothing here is built yet. Each idea has a rough size: **S** (a few XML defs), **M** (some C#), **L** (a new system).

## 1. Total war mode (the big one)

A storyteller option (mod setting) where the Slorg actively conquer the world instead of only raiding you.

| Piece | How it works | Size |
|---|---|---|
| **Assimilation front** | Every few days the Slorg pick a nearby non-Slorg settlement and assault it off-screen. Success depends on their strength vs the target's. A taken settlement becomes a Slorg **Cube**, its people become drones, and the faction may be wiped out. | L |
| **Growing threat** | Raid frequency and size scale with how many settlements the Slorg hold. The world map visibly turns green over time. | M |
| **Defend the settlement** quest | An ally asks for help. A Slorg force is about to hit their town; send a caravan to fight on their map before the deadline. Win: goodwill and rewards. Lose: the town becomes a Cube. | L |
| **Destroy the Cube** quest | Take down a Slorg settlement. Bigger Cubes have more drones, turrets and a regional node. Rewards: implant tech, glitterworld medicine, freed prisoners. | M |
| **Liberate the assimilated** quest | A Cube holds recently assimilated people from an allied faction. Capture and sever them and return them for big goodwill. | M |
| **Regional nodes** | Destroying a Cube's node severs every drone in that region for a few days. It's the local version of the queen core. | M |
| **Collapse** | Destroying the queen core (already built) ends total war on the planet. It's the victory condition. | built |

## 2. New genes (drone biology)

| Gene | Effect | Size |
|---|---|---|
| **Adaptive physiology** | Each time a drone takes a damage type, the whole collective gets a stacking resistance to it for a while. "They've adapted." Change weapons to beat it. | M |
| **Regeneration nodes** | Drones regrow lost limbs slowly while linked | S |
| **Hive synchrony** | Linked drones on the same map share a fraction of each other's healing | M |
| **Cold-hardened / void-hardened** | Temperature and vacuum tolerance, for space Cubes | S |
| **Maturation** | Children who are assimilated grow up faster in a maturation chamber | M |

## 3. New implants (drone tech)

| Implant | Effect | Size |
|---|---|---|
| **Cortical node** | Shows the drone's designation ("Three of Nine") over its head and lets the collective see through its eyes. Destroying it severs that drone early. | M |
| **Interlink node** | A drone with this relays the collective to nearby drones even if the queen falls. Priority target. | M |
| **Adaptive shield** | Upgrade of the shield emitter: after N hits from one weapon type it blocks that type completely for a while | M |
| **Assimilation drone "tendrils"** | Long-reach tubules: inject downed pawns from 2 cells away | S |
| **Grapple / tractor emitter** | Pulls a downed colonist toward the drone to kidnap them | M |
| **Welding arm** | Drones repair Slorg buildings and each other mid-fight | M |

## 4. More challenge for the player

| Idea | Effect | Size |
|---|---|---|
| **Drone designations** | Names like "Seven of Nine" instead of normal names | S |
| **Assimilation of buildings** | Drones convert your turrets or power grid into Slorg tech if left alone near them | L |
| **Kidnap-and-assimilate** | Raiders prefer carrying downed colonists off over killing, and those colonists come back later as drones in raids | M |
| **Cube landing** | A late-game event: a small Cube crashes near the colony and sets up a mini base that sends drones every day until destroyed | L |
| **Signal jamming research** | A building that weakens the collective link on your map (lower collective bonuses, slower infection). A defensive tech race. | M |
| **Hive mind hints** | When the collective gains a colonist's skill, the next raid comes tuned to counter your defences | M |

## 5. Player-side payoffs

| Idea | Effect | Size |
|---|---|---|
| **Captive queen** | Built: summon bound drones | built |
| **Reverse-engineered implants** | Research that lets you install Slorg implants on your own colonists, each carrying a small collective's call risk | M |
| **Resonance cage** | An Anomaly-style containment building for the queen, so she's safer to hold, with a power cost and an escape chance when the power fails | M |
| **Disconnected drone backstories** | Freed drones get a backstory ("former drone") and a chance of useful traits from their old hive life | S |

## 6. Space (Odyssey)

| Idea | Effect | Size |
|---|---|---|
| **Slorg Cube encounters** | Orbital sites with a Cube you can board with a gravship | L |
| **Space raids** | Drones board your gravship (already allowed after surface collapse) | M |

## Suggested next pick

After the current playtest is stable:
1. **Drone designations** (S), **adaptive physiology** (M), **kidnap-and-assimilate** (M). Cheap, and they add a lot of flavor and threat.
2. Then **total war mode**, starting with **Destroy the Cube** quests and **growing threat**, since those don't need off-screen battles. The **assimilation front** and **Defend the settlement** come after.
