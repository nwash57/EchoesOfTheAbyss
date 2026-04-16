# TODO

## 1. World State Consistency & LLM Control ("The Guardrails")
- 2. Limit player actions based on timeline/grandiosity (Item 13)
    - Prevent skipping plot (e.g., "I wait 12 years") — **confirmed**: narrator accepted "I wait 12 years" while clinging to a cliff, no guardrail rejection
    - Prevent interacting with items not presented or in possession — **partial fix**: narrator rejected phantom "enchanted diamond staff" but still used it as a narrative beat to cause damage
    - **PhantomItemRule failed** (playtest 2026-04-05): "I use my enchanted diamond staff" was NOT rejected (`is_rejected: false`) when all equipment was Empty. Narrator played along and the phantom staff was solidified into equipment as "Arcane Staff" by the extractor. Rule needs to reject when all slots are empty.
    - **PhantomItemRule now rejects phantom items** (playtest 2026-04-05 #2): "I use my enchanted diamond staff to cast a spell" correctly rejected. BUT regex is fundamentally the wrong approach here — both sides are unpredictable. Player input is freeform natural language, AND the equipment names are LLM-generated (narrator says "rusted belt knife", extractor registers "Hood" or "Cloak" with descriptions). Regex can't reliably match unpredictable input against unpredictable state. Caused FALSE POSITIVE: "I draw my belt knife and examine it closely" rejected because regex captured the whole trailing clause as the item name, and the belt knife was only present as a substring in the torso description, not a matchable equipment name. **This rule needs to be reimplemented as an LLM-based input rule** that receives the player input + current equipment/narrative context and semantically judges whether the player is referencing something they don't have.
- 3. Fix narrator cliché: remove "rags to riches" phrasing (Item 14) — not observed in latest playtest (exposition was unique)
- 3. Equipment often ends up in the wrong slot - I cant wear a glove on my head
    - **Belt knife placed as torso "accessory"** (playtest 2026-04-05 #2): narrator described "rusted belt knife" from Turn 0, but it was embedded in the Torso equipment description as "(accessory)" instead of being in Right Hand. Never corrected across 8 turns.
- 3. Narrator needs to not reference "second wind" as a game mechanic — death summary used "miraculous surge of adrenaline" language (borderline leak)
- 3. **Narrator leaks HP values in prose** (playtest 2026-04-05): Turn 9 said "Your health stays firm at 94/100", Turn 10 said "Your health plummets to 72/100". Chronicle also leaked: "health dropped to 72". System prompt needs explicit "never reference numeric HP" instruction.
- 3. Minor things "paw slices your shoulder", "jagged rocks that slice your bare feet" dont impact HP — **also the reverse**: HP dropped 3 when narrator only described wind tearing at tunic (no physical harm)
    - **Confirmed again** (playtest 2026-04-05): health_delta=-3 for pulling on a chain and getting splashed. HealthEnforcementRule also corrected phantom HP drift by player extractor on Turns 1 and 3 (extractor returned lower HP with delta=0).
    - **HealthEnforcementRule intermittent** (playtest 2026-04-05 #2): Only fired 1 of 8 rounds. Rounds 1-2 had phantom deltas (-1, -2) for harmless actions that were never applied. Round 5 had unexplained 2-point HP drift with delta=0. Rule needs to enforce every round.
- 3. I randomly died with no explanation on hardest difficulty?
- 3. **Second Wind did not trigger on Hard** (playtest 2026-04-05 #2): HP went from 10→0 with `has_used_second_wind: false`. By design — Hard difficulty reduces Second Wind chance. Not a bug.
- 3. Adventure log is not capturing exposition — **partial fix**: chronicle captured 1 entry on exposition, but location/equipment/player were not set
- 3. Exposition is often the same. The "rags to riches" nature of the story is taken very literally.
    - Maybe run some pre-game prompts to try to build out a world/plot in a more unique way each run
    - The setting is often too destitute. You start in a gutter as a homeless freak with literally nothing and zero direction
    - **Note**: latest playtest had a unique "Obsidian Spire" opening — not destitute, but narrative variety across runs still needs testing
- Exposition isnt reliably setting equipment — **confirmed**: narrator described "rusted iron dagger" but equipment was empty until Turn 2
- Equipment extractor uses different names than narrator (narrator: "rusted iron dagger", extractor: "Heavy Dagger")
    - **Confirmed** (playtest 2026-04-05): narrator said "enchanted diamond staff", extractor registered "Arcane Staff"
- Equipment doesn't track narrative losses (narrator described clothing being ripped away, equipment panel unchanged)
- Age not updated after time-skip was accepted (stayed 25 despite "12 years" passing)

## 2. Core Gameplay Mechanics & Systems
- 4. Inventory system (Item 2)
- 5. Fix: Equipment stats not saving/populating (Item 5)
- 6. Discrete action system? (Item 6)
- 7. DnD style roll system? (Item 7)

## 3. Exploration & Map Logic
- 9. Granular map updates (Item 3)
- 10. More interesting map generation (Item 4)
- **Location extractor is unreliable**: only worked 1 out of 7 turns in playtest. No title/description for Turns 0-3 despite "Obsidian Spire" being named. Location description became stale after Turn 4 (still said "clinging to stone" after player fell and died).
- **Coordinates don't update on movement**: player moved north but coordinates stayed at (+0, +0) until Turn 4 when it shifted to (+0, -1)
- **Confirmed** (playtest 2026-04-05): coordinates stuck at (+3, +2) for all 10 turns despite moving north and entering/climbing lighthouse
- **Coordinates stuck again** (playtest 2026-04-05 #2): Moved once from (+3,-2) to (+3,-3) on Turn 3, then frozen for 5 more turns despite player charging through fence and into fog

## 3b. New Issues (playtest 2026-04-05)
- **Difficulty setting reverts to Balanced**: Set to Hard in setup overlay, but `world_state.log` shows "Balanced" and header slider reverted. Setup value not propagating to game session.
- **Location title has duplicate coordinate prefix**: Title is "(3,2) - Edge of the World Lighthouse", header is "Location: (3,2) - (3,2) - Edge of the World Lighthouse". Extractor is embedding coordinates in the title string.
- **Empty narrator block after input rejection**: When TimeSkipRule rejects input, an empty "The Narrator" block with blank paragraph is rendered and persists.
- **Log API returns "No active session"**: `latest-session` pointer file has UTF-8 BOM prefix causing path resolution to fail. Logs only accessible via direct disk reads.
- **Equipment format in logs is raw strings**: world_state.log equipment values are strings like "Right Hand Equipment: Arcane Staff - 0 armor, 10 damage - ..." instead of structured JSON objects.
- **No EquipmentGainValidationRule**: EquipmentPersistenceRule prevents unjustified losses but nothing prevents phantom items from being solidified into equipment by the extractor.
- **Rejected inputs trigger full pipeline logging** (playtest 2026-04-05 #2): TimeSkipRule and PhantomItemRule rejections still cause world_state.log, narration.log, narrative_eval.log, and state_rules.log to write duplicate round entries with empty/stale data. Round 4 appeared 3 times in world_state.log. Pipeline should not run on rejected inputs.
- **Death summary incorporates rejected actions** (playtest 2026-04-05 #2): Player attempted "My name is Lord Darkblade, 90 years old" which was narratively rejected by the LLM, but the death summary referenced "reducing a ninety-year-old lord" as fact. Death summary prompt should exclude rejected inputs.
- **Equipment descriptions mutate when update_equipment is false** (playtest 2026-04-05 #2): Turn 9 had `update_equipment: false` in narrative_eval but equipment descriptions changed dramatically — belt knife vanished from torso, empty slots filled with "None" + flavor text, head name changed from "Hood" to "Leather hood of the cloak". Extractor runs regardless of the flag.

## 4. UI, UX & Content Polish
- 11. Debug log viewer (Item 9)
- 14. Adventure log (Item 12)

## 5. Imagination Pipeline Reliability
- **Turn 0 imagination pipeline fails silently**: after exposition, Speak button was disabled for 80+ seconds. Player demographics, location, and equipment all stayed at defaults (age 0, no name, no calling, all equipment empty). Only Chronicle updated. Pipeline worked on subsequent turns.
- **Hard difficulty funnels narrative too quickly**: entire 7-turn game took place on one cliff face with wind/falling variations. No opportunity for exploration or recovery before death. Consider giving players a few turns of exploration before escalating life-threatening situations.

---

# Recommended Task Groups (Address Together)

### Group A: The "World State Guardrail" Update (High Priority)
*Addressing Items 1, 13, and 15.*
- Refine `NarrativeEvaluator` logic to check for "illegal" player actions and accidental equipment drift.
- Add time-skip rejection (flag actions with unrealistic timescales or physically impossible given current situation)
- Ensure equipment extractor reflects narrative item losses, not just gains

### Group B: Survival & Game Setup
*Addressing Item 11.*
- Formally implement the "Initial Game Setup" (Verbosity/Difficulty selection) and ensure it integrates well with the existing "Second Wind" and "Game Over" logic.

### Group C: Equipment & Inventory Foundation
*Addressing Items 2, 5, and 15 (partially).*
- Fix item stat population before expanding into a full inventory system.
- Fix equipment naming consistency (extractor should match narrator's item names)

### Group D: Map & Exploration Depth
*Addressing Items 3 and 4.*
- Improve `LocationService` coordinate logic and descriptive variety.
- Fix location extractor reliability (fails most turns, description goes stale)

### Group E: Imagination Pipeline Reliability (New — High Priority)
- Investigate and fix Turn 0 imagination pipeline failure (hangs silently after exposition)
- Ensure all three extractors (player, location, equipment) complete reliably every turn
- Location extractor is the least reliable — only worked 1/7 turns in playtest