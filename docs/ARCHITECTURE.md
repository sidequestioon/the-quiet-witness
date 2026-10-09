# The Quiet Witness — Architecture

**Version:** v0.1 (grey-box prototype) · Unity 6.3 LTS, URP 2D, C#, Ink

This document explains how the game is put together: where things live, how the systems talk to each other and how to add new content. It is spoiler-free.

---

## 1. Big picture

The game is **data-driven**. A case is not code: clues, conclusions, locations, time segments, newspapers, archive records and the trial are **ScriptableObject assets**. The code only knows how to run *any* case.

Everything the player has done is stored as **flags** — short strings like `clue:bottle_no_blood` or `visited:diner`. Systems never ask each other "did the player do X?"; they ask `GameState` whether a flag is set. This keeps systems independent and makes saving trivial (a save is mostly a list of flags).

```
        ┌──────────── data (ScriptableObjects) ────────────┐
        │ Clue · Conclusion · Location · TimeSegment ·     │
        │ Newspaper · ArchiveBook/Entry · CourtCase · Ink  │
        └───────────────────────┬──────────────────────────┘
                                │ read by
 scene objects ──calls──▶  persistent systems  ──set/read──▶  GameState (flags + clues)
 (Interactable,             (TimeManager, SceneLoader,         ▲
  *Opener, Starter)          panels, DialogueRunner)           │ save / load
                                                          SaveSystem (JSON)
```

---

## 2. Folders

```
Assets/
  _Project/
    Cases/        game data: Prologue (clues, conclusions, newspapers, trial),
                  Time (segments + Lighting presets), Locations, Archive
    Dialogue/     Ink files (Main.ink includes the others)
    Prefabs/      Systems (all persistent systems + UI), Investigator (player)
    Scenes/       Office (hub), Diner, PoliceStation
    Scripts/
      Core/       GameState, flags, conditions, time, input blocking
      World/      player movement, interaction, lighting
      Map/        locations, town map, scene travel
      Archive/    phone book and record search
      Dialogue/   Ink runner, dialogue box, present evidence
      Board/      evidence board and conclusions
      UI/         journal, newspaper, yes/no box, "call it a day"
      Court/      courtroom finale
      Save/       save and load
      Editor/     UI builders and one-click setup scripts (editor only)
  Settings/       URP settings (keep)
docs/             GDD.md, ARCHITECTURE.md
```

Namespaces follow the folders: `QuietWitness.Core`, `QuietWitness.World`, `QuietWitness.Map`, …

---

## 3. The Systems prefab

Every scene contains one copy of **`Prefabs/Systems`**. The first copy that loads survives scene changes (`DontDestroyOnLoad`); copies in later scenes destroy themselves. So any scene can be played directly from the editor.

| Object | Components |
|---|---|
| Systems (root) | `GameState`, `TimeManager`, `SceneLoader`, `DialogueRunner`, `SaveSystem`, `DebugTime` |
| Canvas | Map, Archive, Dialogue box, Evidence picker, Journal + notice, Board, Newspaper, Court, Confirm box, Fader + title card |
| EventSystem | UI input (Input System module) |

**Rule:** change Systems only in **Prefab Mode** (double-click the prefab). Changes made in a scene become overrides that other scenes don't get.

Each persistent system exposes a static `Instance`. Panels follow the same shape: `Open()` / `Close()`, a `window` object that is shown and hidden, and `InputBlocker.Push()/Pop()` while open.

---

## 4. Core

| Class | Job |
|---|---|
| `GameState` | The single source of truth: a set of flags and the list of found clues. Events: `FlagSet`, `ClueFound`. |
| `Condition` | "All of these clues and flags." Used everywhere something can be locked (locations, records, headlines, verdicts…). Empty = always true. |
| `UnlockOnCondition` | Shows a scene object only when a condition is met. |
| `TimeSegment` (SO) | A part of a day: key condition, warning text, next segment, lighting preset. |
| `TimeManager` | Current segment; fires `ReadyToAdvance` when the key condition is met and `SegmentChanged` when the player moves on. |
| `AvailableInSegments` | Shows a scene object only in certain segments. |
| `InputBlocker` | A counter. While anything is open (map, dialogue, travel…) the player can't walk or interact. Also blocks the rest of the frame it was released in, so the key that closed a window doesn't reopen it. |

### Flag conventions

| Prefix | Set by | Example |
|---|---|---|
| `clue:` | finding a clue | `clue:coroner_blow_to_head` |
| `conclusion:` | the evidence board (or ink) | `conclusion:bottle_not_weapon` |
| `time:` | entering a time segment | `time:sat_morning` |
| `visited:` | arriving at a location | `visited:police` |
| `archive:` | reading an archive record | `archive:phone_diner` |
| `talked:` | finishing a conversation (knot name) | `talked:test_stranger` |
| `paper:` | reading a newspaper edition | `paper:fri_evening` |
| `seen:` | opening a clue in the journal | `seen:clue:paper_ad` |
| `ending:` / `court:` | the verdict | `ending:not_guilty` |

Ink and data can also set any custom flag (e.g. `test:stranger_lied`).

---

## 5. Systems

**World.** `PlayerMover` (A/D or arrows), `PlayerInteractor` (nearest `Interactable` in reach, **E**). An `Interactable` has a prompt and a UnityEvent; scene objects plug behaviour into it.

**Scene links.** Scene objects never reference persistent objects directly (those references would break after a scene change). Instead small "link" components call `Instance`: `MapOpener`, `ArchiveOpener`, `BoardOpener`, `DialogueStarter`, `CourtOpener`, `TimeAdvancer`, `ClueGiver`, `LocationTravel`.

**Lighting.** Each `TimeSegment` has a `LightingPreset` (global light colour/intensity, lamps on/off). `GlobalLightController` and `LampSwitch` apply it; locations are drawn once and relit.

**Map & travel.** `LocationData` (SO) = id, name, scene, unlock condition. `MapPanel` shows unlocked places (NEW until visited). `SceneLoader` fades out, shows a title card, loads the scene, fades in and fires `Arrived`.

**Archive.** `ArchiveBook` (phone book, police archive…) holds `ArchiveEntry` records. Records are found by exact keyword; reading one sets `archive:<id>` and can give clues/flags — e.g. an address that unlocks a location.

**Dialogue.** One Ink story for the whole game (`Dialogue/Main.ink`), run by `DialogueRunner`. Ink talks to the game through `is_set()`, `set_flag()` and `give_clue()`. `DialoguePanel` types lines out and shows choices. Special choices: `present:<clue id>` / `present:any` (open the evidence picker) and `silence` (a pause). See §7.

**Journal (case file).** `JournalPanel` (**J**): evidence cards (newest first, NEW until opened) and the full conversation log. `JournalToast` shows "New clue" notices.

**Evidence board.** `ConclusionData` (SO) = two input cards (clues or other conclusions). `BoardPanel`: click two cards; a right pair sets `conclusion:<id>` and draws a red thread, a wrong pair does nothing.

**Newspaper.** `NewspaperData` (SO) per time segment: conditional headlines (first match wins) and articles that can hide clues. Opens when its segment starts; **N** re-reads.

**Time moving on.** `TimeAdvancer` ("Call it a day") asks through `ConfirmPanel` and shows the segment's warning.

**Courtroom.** `CourtCase` (SO): claims answered by clues or conclusions, verdicts picked by score and flags. Sets `ending:<id>` and can open Tuesday's newspaper.

**Save & load.** `SaveSystem` writes one JSON slot (`save1.json` in `Application.persistentDataPath`): flags, clue ids, time segment, scene, Ink state and dialogue history. Autosaves after travel and time changes; **F5** saves, **F9** loads.

---

## 6. Adding content

| I want to add… | Do this |
|---|---|
| A clue | Create ▸ The Quiet Witness ▸ **Clue** (unique `id`). Give it with a `ClueGiver` on an `Interactable`, from Ink with `give_clue("id")`, or from a newspaper/archive record. Then run *Collect all clues* on `DialogueRunner` and `SaveSystem` (or Setup ▸ Save and load). |
| A conclusion | Create ▸ **Conclusion** with exactly two input cards. Run *Collect all conclusions* on the Board. |
| A location | Create ▸ **Location**, add the scene to Build Profiles, add a `MapButton` to the map in the Systems prefab. |
| A time segment | Create ▸ **Time Segment**, link it from the previous segment's `Next`, pick a lighting preset. |
| A conversation | Add a knot to an `.ink` file (included from `Main.ink`); put a `DialogueStarter` with the knot name on a character. |
| A newspaper | Create ▸ **Newspaper**, set its segment; *Collect all newspapers* on the Newspaper panel. |
| An archive record | Create ▸ **Archive Entry**, add it to an **Archive Book**; open the book with an `ArchiveOpener`. |

The menu **Tools ▸ The Quiet Witness** has UI builders (`Build … UI`) and one-click setup scripts (`Setup ▸ …`) that do Inspector work automatically. They skip work that is already done.

---

## 7. Writing Ink

```ink
Stranger: A character speaks.            // "Name: text"
Plain text is narration or a thought.    // shown in italics
* [Ask about the park]                   // a choice
* {is_set("clue:coroner_blow_to_head")} [Mention the blow]
    ~ set_flag("test:surprised")
    ~ give_clue("bottle_no_blood")
* [present:bottle_no_blood]              // answer to that clue
+ [present:any]                          // answer to any other clue
* [silence]                              // "... (stay silent)"
```

---

## 8. Controls (v0.1)

| Key | Action |
|---|---|
| A / D, ← / → | Walk |
| E | Interact, continue dialogue |
| 1–9 / click | Choose an answer |
| P | Present evidence (when offered) |
| J | Case file |
| N | Re-read the newspaper |
| Esc | Close a window |
| F5 / F9 | Save / load |

---

## 9. Known limitations of v0.1

- Grey boxes instead of art; placeholder test content.
- One save slot, no main menu yet.
- The courtroom is reachable from the police station for testing.
- Debug helpers (`DebugTime`, `DebugClueGiver`) are still in the project.
