# TODO

## 1. World State Consistency & LLM Control ("The Guardrails")
- 1. Agent to determine whether parts of the narration pipeline should execute (Items 1, 15)
    - Safeguard against losing equipment randomly or "drift" in descriptions (e.g., "shortsword" to "steel sword")
    - Equipment should only be modified if explicitly requested or narrated as a change
- 2. Limit player actions based on timeline/grandiosity (Item 13)
    - Prevent skipping plot (e.g., "I wait 12 years")
    - Prevent interacting with items not presented or in possession
- 3. Fix narrator cliché: remove "rags to riches" phrasing (Item 14)

## 2. Core Gameplay Mechanics & Systems
- 4. Inventory system (Item 2)
- 5. Fix: Equipment stats not saving/populating (Item 5)
- 6. Discrete action system? (Item 6)
- 7. DnD style roll system? (Item 7)

## 3. Exploration & Map Logic
- 9. Granular map updates (Item 3)
- 10. More interesting map generation (Item 4)

## 4. UI, UX & Content Polish
- 11. Debug log viewer (Item 9)
- 13. Initial Game Setup: Select Verbosity/Difficulty before first narration (Item 11)
- 14. Adventure log (Item 12)

---

# Recommended Task Groups (Address Together)

### Group A: The "World State Guardrail" Update (High Priority)
*Addressing Items 1, 13, and 15.*
- Refine `NarrativeEvaluator` logic to check for "illegal" player actions and accidental equipment drift.

### Group B: Survival & Game Setup
*Addressing Item 11.*
- Formally implement the "Initial Game Setup" (Verbosity/Difficulty selection) and ensure it integrates well with the existing "Second Wind" and "Game Over" logic.

### Group C: Equipment & Inventory Foundation
*Addressing Items 2, 5, and 15 (partially).*
- Fix item stat population before expanding into a full inventory system.

### Group D: Map & Exploration Depth
*Addressing Items 3 and 4.*
- Improve `LocationService` coordinate logic and descriptive variety.