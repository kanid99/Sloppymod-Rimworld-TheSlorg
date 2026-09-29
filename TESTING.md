# The Slorg: testing guide

How every mechanic works, and how to test each one quickly with dev mode.

> **Status:** everything compiles and every XML file is valid, but nothing has been run in RimWorld yet. Expect some bugs on the first load. When you find one, copy the **red lines from the debug log** (and what you were doing) back to me.

---

## 0. Setup

1. **Mods** (in this load order): Harmony → Core → Biotech → any other DLCs → **The Slorg**.
   - Put the repo folder in `RimWorld/Mods/` (the folder containing `About/`, `Defs/`, `1.6/` and so on).
2. **Options → Development mode: on.** This adds the bug icon (debug log) and the wrench icon (debug actions) to the top of the screen.
3. Start the game and **open the debug log right away**. Look for red errors that mention `Slorg`, `TheSlorg` or `kanid99.theslorg`.
   - Yellow warnings are usually harmless. Red errors are what I need.
4. **New colony:** on the faction screen, check that **Slorg Collective** is listed. It should be there by default.

### Debug actions (wrench icon → "The Slorg")

| Action | What it does |
|---|---|
| **Infect with nanoprobes** | Click a pawn to give them the visible nanoprobe infection |
| **Advance infection +30%** | Pushes an infection along |
| **Advance infection to stage 2** | Jumps an infection straight to stage 2 (implants and genes form) |
| **Complete infection now** | Finishes the infection immediately (turns them into a thrall) |
| **Make sleeper agent (active now)** | Gives a colonist hidden dormant nanoprobes, active immediately |
| **Force uprising on this map** | All sleepers on the map rise up now |
| **Sever from collective** | Click a drone to collapse it as if its queen fell |
| **Install full drone implant set** | Click a pawn to give them every Slorg implant |
| **Trigger collective's call** | Click a colonist to start the "rejoin the collective" mental break |
| **Slorg raid (1500 pts)** | Normal Slorg raid |
| **Slorg raid with queen (6000 pts)** | Big raid that always includes the queen, if she's available |
| **Spawn queen core** | Places a queen core at the mouse position |
| **Log collective state** | Writes each Slorg faction, its queen, Unicomplex and shared skills to the debug log |

Useful vanilla dev tools:
- **Tool: "Down pawn"** to down a colonist.
- **Research: "Finish all"** or the Slorg research tab.
- **Spawn thing: MedicineUltratech** (glitterworld medicine).
- **Time speed:** the dev-mode ultra speed button.

---

## 1. Xenotypes

| Xenotype | Where it comes from | Looks | Key genes |
|---|---|---|---|
| **Slorg drone** | Slorg raids and settlements, and completed infections | Pale grey, hairless, gaunt | Collective link (Assimilate), nanoprobes, drone conditioning, plus vanilla: never sleep, disease-free, tox resistance and more. **Not ageless.** |
| **Slorg queen** | Faction leader; very large raids | **Dark violet skin, glowing spined crown** | Drone genes plus **Hive sovereign**, queen skin, ageless, robust, super-fast healing, extreme psychic ability |
| **Slorg thrall** | Anyone who succumbs to the infection or rises up as a sleeper | **Keeps their own look** | Collective link, nanoprobes, drone conditioning, reduced pain, dead calm, partial tox resistance |
| **Disconnected drone** | Freed drones (surgery, the core falls, or joining you) | Like a drone | Drone genes minus link and conditioning, plus **Severed link** |

**Drone conditioning** sets social impact, negotiation and trade to 0 and disables Social work. **Linked drones never have mental breaks.**

Genes are only the biology. **All Slorg technology is implants** (section 9). Thralls never have implants. Freed **thralls** lose their Slorg genes entirely and keep only *Severed link*.

**Test:**
- [ ] In the gene library (Xenotype editor on the colonist screen), check that all 4 xenotypes show with icons and readable descriptions.
- [ ] Start with a colonist set to **Slorg drone**. Within a few seconds a message should say their link has *burned out*, and they become a **disconnected drone**. Player Slorg are never linked.
- [ ] Put a disconnected drone in a **gene extractor**. It should refuse: *"Slorg nanoprobes destroy any extracted genetic material."*

---

## 2. The faction and raids

- **Slorg Collective**: permanent enemy, spacer tech, no trade.
- Raids mix **slorg drones** (melee: neural lash) and **tactical drones** (ranged: disruptor beam). **Slorg spawn with no clothes and no weapons.** Their implants are their gear.
- Faction and settlement names are Slorg-style ("Unimatrix 42", "Cube 317", "Node K-204").
- **Slorg weapons down rather than kill.** They cause **neural shock**, which builds up per hit: pain, then −15%/−35% consciousness, then collapse. It fades over a few hours and does no physical damage. They can't show up before day 20 on the storyteller's own schedule. Debug raids ignore that.
- The faction's oldest surface settlement is renamed **Unicomplex**.

**Test:**
- [ ] World map: find the Slorg settlements. One should be called **Unicomplex**.
- [ ] Debug action **Slorg raid (1500 pts)**. Drones arrive and are grey, bald, and armed and dressed sensibly.
- [ ] Select a drone. The health tab should show **collective link (N drones)**, and the tooltip lists collective knowledge (skill, level, source drone).
- [ ] Their skills tab should show boosted levels (see section 4).

---

## 3. Assimilation: the nanoprobe infection

**How it works**
- Enemy drones look for **downed** enemies within 40 cells and use **Assimilate** on them.
- The infection runs in **three stages** and completes in about **4–6 in-game hours**. Bleeding is stopped the whole time, so the victim can't bleed out.

| Stage | When | What happens | Cure |
|---|---|---|---|
| **1. Neural takeover** | First ~1.5–2 h | The victim **switches to the Slorg** and fights like a drone, with no implants | **Any tend of 50%+ quality** cures it: capture them (they're hostile now), then tend |
| **2. Implants forming** | Until done | Ocular implant and dermal plating grow, and Slorg genes are written in. Tending does nothing. | **Purge nanoprobes** surgery (Medicine 8, 1 glitterworld medicine). Slorg genes are removed, but the **implants stay** and must be cut out. |
| **3. Complete** | ~4–6 h | Full **Slorg drone**: drone xenotype, drone implants, hairless. Nanoprobes **heal chronic conditions** (Alzheimer's, dementia, cataracts, bad back, frailty, artery blockage, asthma, carcinoma). | **None.** Only disconnection (queen death, queen core) frees them. |

- **The queen's injection is instant**: her victim is a full drone on the spot.
- A cure returns the pawn to its original faction. **40% of cures only look cured** and leave a hidden sleeper agent (section 7).
- On completion:
  - **On your home map**: they attack the colony.
  - **Elsewhere**: they join the Slorg and leave.
  - **Your prisoner or slave**: 50/50 they break out hostile, or become a hidden sleeper (implants and genes silently dissolve).

**Test:**
- [ ] Raid, then down a colonist next to a drone. It injects them, you get a letter, and the colonist **turns hostile**.
- [ ] Health tab: *nanoprobe infection (neural takeover)*. Wounds should stop bleeding.
- [ ] **Stage 1 cure**: arrest the downed victim, tend them with any medicine and a decent doctor. They're cured and rejoin your colony.
- [ ] **Stage 2**: **Advance infection to stage 2**. Ocular implant and dermal plating appear, plus Slorg genes. Tending says it can't help. Run **Purge nanoprobes**: cured, genes gone, implants still there (cut them out with the removal bills).
- [ ] **Complete infection now**: full drone. Check it's hairless and that any chronic condition is gone.
- [ ] Spawn a raid with the queen, down a colonist near her. She injects them and they rise as a drone at once.

## 4. The collective (skills, stats, traits)

**How it works** (refreshes every 500 ticks, about 8 seconds)
- **Skills:** every linked drone of a faction uses the **highest level** any linked drone has, including drones off the map. Their own levels are unchanged underneath. Kill the source drone and the collective drops to the next best. You get a message when a skill is gained or lost in front of you.
- **Stats:** the *collective link* effect scales with drones on the **same map**:

  | Drones | Stage | Effect |
  |---|---|---|
  | 1 | isolated | −10% consciousness, −10% work speed, −20% learning, easier breaks |
  | 2+ | cluster | +5% work speed |
  | 5+ | unimatrix | +10% work, +10% learning, +5% consciousness |
  | 10+ | cube | +15% work, +20% learning, +10% consciousness, +5% manipulation |
  | 20+ | full collective | +25% work, +30% learning, +15% consciousness, +10% manipulation and moving |

  A **queen on the map counts as 10 extra drones.**
- **Traits:** these spread from the drone that naturally has them to all the others, unless they clash with a trait the drone already has: industrious, hard worker, tough, nimble, careful shooter, fast learner, great memory, quick sleeper, fast walker, jogger, steadfast, iron-willed. They're taken back when the source is gone.
- **Prisoners, slaves and severed drones are cut off.**

**Test:**
- [ ] Raid, then **Log collective state**: the log lists skills, their sources and shared traits.
- [ ] Compare one drone's Shooting against the best shooter's. They should match (or be higher).
- [ ] Kill the drone that provides a skill. You get a *"collective has lost…"* message, and the others drop.
- [ ] Hover the collective link label. It shows the drone count and *queen present* when she's there.

---

## 5. The queen

**How it works**
- She's the faction leader: *Slorg queen* xenotype, female, spacer gear, high skills.
- She joins **Combat raids of 5000+ points, 50% of the time**. The debug action forces it. When she's on your map you get a **"The Slorg queen is here"** letter.
- She's easy to spot: violet skin and a tall glowing crown.
- **Anyone she injects becomes a full drone immediately.**
- **Killed** on a map: every Slorg drone on that map is **severed from collective immediately**. **Captured**: it happens at the next collective refresh, within about 8 seconds. They're downed (consciousness capped at 10%) for **about 1.7–2.3 days**. You get a *Slorg queen fallen* letter.
- A **new queen** is raised straight away. She's a new pawn, not the same one.
- Severed drones that aren't operated on reconnect when it wears off.

**Test:**
- [ ] **Slorg raid with queen (6000 pts)**. A queen appears with the raid.
- [ ] Kill her. All drones on the map collapse, and the letter appears.
- [ ] **Log collective state**: there should be a new queen (off map).
- [ ] Run the raid with queen again: the new queen shows up.

---

## 6. Sever link surgery (freeing drones)

**How it works**
- Research **Neural severance** (Slorg research tab, hi-tech bench, 2500).
- Surgery **Sever collective link**: needs Medicine 10 and 2 glitterworld medicine. It's only offered while the drone is **severed**.
- On success: **disconnected drone**, resistance drops to ≤8 and will to ≤2, so they're easy to recruit.

**Test:**
- [ ] Kill the queen, capture a severed drone (arrest it while it's downed), and check the bill appears on the prisoner.
- [ ] Also try **Sever from collective** on any captured drone.
- [ ] After surgery: xenotype *disconnected drone*, no Assimilate ability, low resistance. Recruit them.
- [ ] Once recruited: no *burned out* message (already disconnected), and the gene extractor refuses them.

---

## 7. Sleeper agents and uprisings

**How it works**
- When a purge clears an infection completely, there's a **40% chance** it only *looks* cured. The pawn gets hidden **dormant nanoprobes**, which don't show on the health tab.
- After **1 day** of incubation, the sleeper starts injecting a colonist every **0.5–1.5 days**:
  - Target preference: **asleep or downed** colonists first, otherwise one who is alone (no awake colonist with line of sight within 12 cells).
  - The job shows as **"checking on [name]"**.
  - The victim becomes a sleeper too (with their own 1-day incubation).
  - **Clue:** a yellow message *"X was seen leaning over sleeping Y…"*. That's a 60% chance if someone awake could see, otherwise 10%.
- **Uprising:** when sleepers are **3 or more and at least 34% of free colonists**, they all become **thralls** at once and attack together. You get a *Slorg uprising* letter.
  - **50% chance** the collective backs them up with a raid **about 1 hour later** at 70% of a normal big-threat size.
- **Counter:** research **Nanoprobe detection** (1500), then the **Nanoprobe scan** surgery (Medicine 6, 1 glitterworld medicine, no anesthetic). It finds and purges dormant nanoprobes.

**Test:**
- [ ] **Make sleeper agent** on one colonist. Their health tab looks normal.
- [ ] Let time pass (at night is best). Watch their job report for *checking on…* and a possible witness message.
- [ ] **Log collective state**: shows the number of hidden sleepers on the map.
- [ ] Make 2 more sleepers, or wait. The uprising should fire on its own at the next refresh when the threshold is met, or use **Force uprising**.
- [ ] During an uprising: former colonists keep their looks, are now thralls, and attack. There's a letter, sometimes with *"A Slorg force is on its way"*, and then a raid about an hour later.
- [ ] **Nanoprobe scan** a sleeper: *Sleeper found* letter. Scan a clean pawn: *no nanoprobes*.

---

## 8. The queen core and planetary collapse

**How it works**
- When the **Unicomplex** map is generated (attack it with a caravan), a **queen core** (3×3, 4000 HP, glowing) spawns at the map center.
- It can't be claimed or deconstructed, so it has to be destroyed by damage.
- **When it's destroyed:**
  - every Slorg (drone, queen, thrall) on the **planet surface** becomes a **disconnected drone** with **no faction**, and the ones on maps are severed (downed about 2 days);
  - all other Slorg **surface** settlements are destroyed;
  - the Slorg can no longer raid surface maps (space maps are still allowed);
  - you get a *Queen core destroyed* letter.

**Test** (quick way):
- [ ] **Spawn queen core** on your own map, then destroy it with the dev *Destroy* tool or weapons.
- [ ] Letter appears, any Slorg on your map collapse and lose their faction, and the world map loses the Slorg surface settlements.
- [ ] Try a Slorg raid via the vanilla incident menu (Raid → Slorg). The faction should no longer be an option on a surface map.

**Test** (real way):
- [ ] Caravan to the Unicomplex and attack it. The core is at the map center.

---

## 9. Implants

**How it works**
- All Slorg tech is **implants**, which show on the health tab. Drones and queens are generated with them. **Players can't install them.**
- **Every implant can only come out by surgery** ("cut out …" bills, Medicine 6–10, 2 industrial medicine). Removal **always destroys the implant and injures the patient.**

| Implant | Part | Effect | Visual | Removal |
|---|---|---|---|---|
| **Ocular implant** | Eye | +15% sight, aiming time ×0.8 | Eyepiece with a red laser | 2 wounds near the eye |
| **Dermal plating** | Whole body | +35% sharp, +18% blunt, +20% heat armor, −5% moving | Bolted plates on the body and a cranial plate | **10 wounds all over, 80% scar, 25% death** |
| **Shield emitter** | Torso | Blocks ranged and explosive damage (60 pts), recharges, overloads for 15 s | Flash on hit | 3 wounds |
| **Assimilation tubules** | Hand | Tubule stab (3 plus 10 neural shock). Infections start at 15% (well into stage 1). | none | 2 wounds |
| **Beam emitter** | Arm | **Disruptor beam** ability: 22 neural shock, range 25, 4 s cooldown. Drones fire it at the nearest enemy in sight. | Green bolt | 3 wounds |
| **Neural lash** | Arm | Melee: 5 blunt plus 18 neural shock | none | 3 wounds |
| **Tactical cortex** | Brain | **+4 Shooting, +4 Melee** (on top of collective skills), +4 melee dodge | none | 2 wounds on the head, 10% death |

What each pawn kind gets:
- **Drone** (melee): plating, eye, tubules, neural lash. 30% chance each of shield and cortex.
- **Tactical drone**: plating, eye, shield, beam. Cortex 60%, tubules 50%.
- **Queen**: all seven.

**Test:**
- [ ] Raid, then select drones. The health tab lists the implants, and the drones show plates on their bodies plus a head plate and eyepiece.
- [ ] Shoot a shielded drone. There's a flash, no damage, and the shield % drops, then *Shield overloaded* and it recharges.
- [ ] Tactical drones fire **disruptor beam** bolts. Colonists they hit build up *neural shock* and collapse instead of dying. If drones only punch, tell me.
- [ ] Drones arrive naked and unarmed, and are hairless.
- [ ] The skills tab of a drone with a tactical cortex shows +4 Shooting and Melee.
- [ ] Use **Install full drone implant set** on a prisoner, then run each *cut out* bill. The implant is gone and never drops as an item, and the patient takes wounds (many scars for the plating, sometimes death).

---

## 10. The collective's call

**How it works**
- A **colonist who still carries any Slorg implant** (a freed drone you didn't strip of implants) sometimes hears the collective. It's about **once per 60 days per pawn**, and only while a Slorg faction still controls the world.
- They get the **Answering the collective** mental break and walk off the map to rejoin the Slorg (like a vanilla *give up and leave*). You get a letter. Arrest or down them before they leave.
- Cutting out all their implants makes them immune.

**Test:**
- [ ] Recruit a disconnected drone (or use **Install full drone implant set** on a colonist), then **Trigger collective's call**. They walk to the map edge.
- [ ] Cut out every implant, then leave them for a long time. It should never happen.

---

## 11. Known risks (please watch for these)

| Risk | What you'd see |
|---|---|
| The queen goes missing | After a big raid is generated but never spawns, *Log collective state* shows no queen, or a new one keeps being made |
| Freed drones behave strangely | After the core falls, factionless drones stand still or act oddly |
| Skill boost not applied | A drone's skills tab shows its own low level instead of the collective's |
| Drones don't inject | Downed colonists near drones never get infected (the AI hook didn't load) |
| Purge never possible | Glitterworld tends never reach 105% |
| Trait names | A red error mentioning `Tough` or `SpeedOffset` in `Slorg_Collective` |
| Colonist converting in a caravan | A colonist who completes the infection while travelling turns hostile inside your caravan |
| Plating not drawn or misplaced | Drones look plain, or plates float off the body. The body-overlay renderer is custom. |
| Beam never used by the AI | Tactical drones never fire cutting beams |
| Shield blocks too much or too little | Tell me what got through or what didn't |

---

## 12. Tuning without recompiling

| File | What's in it |
|---|---|
| `Defs/SlorgDefs/Slorg_Collective.xml` | Refresh rate, queen raid threshold and chance, queen bonus, **sleeper, captive and uprising numbers**, collective's call frequency, shareable traits |
| `Defs/HediffDefs/Hediffs_Slorg.xml` | Infection speed, purge threshold and amount, severance length |
| `Defs/HediffDefs/Hediff_CollectiveLink.xml` | Collective stat stages |
| `Defs/FactionDefs/Faction_Slorg.xml` | Raid composition, earliest raid day |
| `Defs/PawnKindDefs/PawnKinds_Slorg.xml` | Drone and queen gear, skills, resistance |
| `Defs/RecipeDefs/Recipes_Slorg.xml` | Surgery costs and skill requirements, including implant removal |
| `Defs/HediffDefs/Implants_Slorg.xml` | Implant stats, shield strength, weapon damage, removal wounds and death chance |
| `Defs/ResearchProjectDefs/Research_Slorg.xml` | Research costs |

After editing XML, restart RimWorld. There's no need to rebuild the DLL.
