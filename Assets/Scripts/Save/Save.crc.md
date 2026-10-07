# Save — thinking file (delete when Save is built)

Strict (option b): Save knows no module. Facts knows no Save.
An adapter in Facts' installer connects them.

    Root ──"load / save now"──> Save ──participant port──> FactsSaveAdapter
                                 │                              │
                                 └──storage port──> FileSaveStorage
                                                    FactsSaveAdapter ──snapshot port──> Facts

## Step 1 — Wishes
Four conversations meet here. Write what each caller wishes to say.

**A. Someone → Save** (who starts a save or a load? see newborn-plan.md,
"Phase 2 — load"):
- Root, at startup:
- Something, when it's a good moment to save (which moments?):

**B. Save → a participant** (worked example, mine):
- "give me your section to keep"
- "here is the section you gave me last time, take it back"
Your addition: what if a participant has NO section in the save
(a module added in an update, or a new game)?
-

**C. Save → storage:**
-

**D. The adapter → Facts** (Facts' new offered snapshot port):
-

## Step 2 — Questions
1. What IS a section, as a type? Options: a string Save never reads
   (participant serializes itself), or a plain object Save serializes
   (Save must then know how). Which keeps Save more ignorant?
2. How does Save tell sections apart in the file?
3. One save slot or several? (A baby game: probably one autosave.)
4. A saved game from an older build: who notices, who converts?
