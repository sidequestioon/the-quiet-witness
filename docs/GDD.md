# The Quiet Witness — Game Design Document

**Version:** 0.1 (draft) · **Status:** Block 1 — Paper design

---

## 1. Overview

**Pitch:** 48 hours until the trial. The whole city is sure he's guilty. You're not. A private investigator hired by the defense searches a neon-lit city over two days and two nights to find the real killer before the judge's gavel falls.

| | |
|---|---|
| Genre | Narrative detective adventure, deduction |
| Perspective | 2D side view, pixel art |
| Length | 3–5 hours, one complete story |
| Platforms | PC (Windows, macOS) — Steam, itch.io |
| Engine | Unity 6.3 LTS, URP 2D, C# |
| Languages | English first, Russian later |

**References:** Loco Motive (characters, humor, side-view walking), Tails Noir / Backbone (light and atmosphere), The Case of the Golden Idol and Return of the Obra Dinn (deduction), Ace Attorney (presenting evidence), Shadows of Doubt (evidence board), Her Story (archive search).

**Unique hooks:**
- **Time without a timer** — story events move the clock, not a countdown.
- **Newspaper as chapter** — every time segment opens with a front page that reacts to the player's actions.
- **Courtroom finale** — the player builds the defense speech from the evidence they found.
- **Silence as an answer** — sometimes saying nothing makes people say too much.

---

## 2. Setting & tone

**Setting:** 1988, a big American city. Neon, rain, VHS, answering machines, payphones, polaroids. The case is high-profile: every newspaper writes about it, and public opinion is against the client.

**Tone:** noir that doesn't take itself too seriously. The murder is real and dark, but the people around it are eccentric. Humor lives in object descriptions, the heroine's inner monologue and the witnesses' quirks.

---

## 3. Story

> To be written in issues #2 and #3.

- **The client:** who is on trial and for what — *TBD*
- **The crime:** what happened and how — *TBD*
- **The real killer and motive:** *TBD*
- **Why the city believes the client is guilty:** *TBD*
- **The Quiet Witness:** a mute character who communicates through gestures and drawings. They hold the key to the case.

---

## 4. Characters

> To be written in issue #4. Target: 8–10 characters.

| Name | Role | What they know | What they hide |
|---|---|---|---|
| The heroine | Private investigator hired by the defense | — | — |
| *TBD* | | | |

---

## 5. Locations

> To be written in issue #4. Target: 6–8 locations.

| Location | Open when | Day / night differences |
|---|---|---|
| Office (hub) | Always | — |
| *TBD* | | |

---

## 6. Core loop

1. Read the newspaper at the start of a time segment.
2. Plan in the office: answering machine, phone book, archive, evidence board.
3. Travel to a location through the city map.
4. Explore: walk around, examine objects, find clues.
5. Interrogate people; new clues unlock new questions.
6. Connect clues on the board to form conclusions.
7. Conclusions unlock new locations and questions. The key clue of the segment moves time forward.

The loop repeats until the board shows the full picture. On Monday morning the trial begins.

---

## 7. Time system

There is no visible timer. Time is moved by key story events: when the player finds the main clue of a segment, the next segment begins.

| Chapter | Newspaper | What happens | Weather |
|---|---|---|---|
| 1. Friday | Evening edition: "Trial on Monday" | Start in the evening: case file and 3–4 basic clues | Rain |
| 2. Saturday | Saturday edition | Morning → day → evening → night, advanced by story | Fog |
| 3. Sunday | Sunday edition | Last leads, pressure grows | Clear cold |
| 4. Monday | Morning edition before the trial | Dawn, trial, finale | — |
| Epilogue | Tuesday's newspaper | Trial result and what happened to everyone | — |

**Urgency without a timer:** every segment has side leads. When the player advances time, some of them close forever (a bar closes, a witness leaves town). Before such a step the heroine warns the player. More side clues mean a stronger defense in court.

**Light:** each location is drawn once; morning, evening and night are lighting setups. At night different people are around and different places are open.

> Segment breakdown to be written in issue #5.

---

## 8. Screens & systems

| Screen | What the player does |
|---|---|
| Main menu | Case folder with a stamp, save slots |
| Office (hub) | Desk, phone, answering machine, board; returns here between trips |
| City map | Paper map on the desk; locations appear during the investigation; instant travel with a location title card |
| Location | Walks in side view, examines objects, talks to people |
| Newspaper | Front page opening each time segment |
| Dossiers & archive | People's photos, jobs, connections; old documents and clippings |
| Interrogation | Questions unlocked by clues; present evidence to catch a lie |
| Evidence journal | Clue cards with descriptions; dialogue log |
| Evidence board | Photos and clues, red threads, conclusions |
| Courtroom | Finale: break the prosecution's testimony and name the real killer |

---

## 9. Evidence board

The player connects two cards with a red thread. If the connection is correct (for example, "café receipt" + "brother's alibi"), a conclusion appears: "the alibi is false". A conclusion unlocks a new question or location. Wrong connections simply do nothing — no penalty.

> Full "what unlocks what" chart: issue #6.

---

## 10. Dialogue & interrogation

- Questions unlock from clues found.
- **Present evidence** during a conversation to catch a lie.
- **Silence** is an answer option: sometimes the other person fills the pause and says too much.
- **Dialogue log:** everything said can be reread.
- **Notebook:** the heroine automatically notes small details that are easy to miss; rereading them reveals leads.
- **The Quiet Witness** communicates through gestures and drawings.

Dialogue is written in Ink.

---

## 11. Newspapers

- Every chapter opens with a newspaper; Tuesday's issue shows the trial result.
- Clues are hidden not only in articles but also in ads, obituaries and the weather forecast.
- Headlines depend on the player's actions.

---

## 12. Courtroom finale & endings

On Monday the player builds the defense speech: for each point, they choose specific clues. Correct clues for most points convince the court even if something was missed. Too few — the speech falls apart. It is also possible to convince the court and accuse the wrong person.

| Ending | Tuesday's newspaper |
|---|---|
| Client acquitted, real killer caught | Justice is served, the detective solved the case |
| Client acquitted, wrong person accused | An article that makes it clear: the arrested person is innocent, the killer is free |
| Client convicted | Verdict delivered, the city is satisfied |

After the result — an epilogue as a newspaper chronicle about every character.

> Courtroom script: issue #7.

---

## 13. Art direction

- **Style:** detailed pixel art with living light (Tails Noir) and quirky, funny characters (Loco Motive).
- **Base resolution:** 480×270, scaled ×4 to 1920×1080.
- **Palette:** 24–32 muted colors, cold blues and browns. One accent: red (threads, stamps, blood, "UNVERIFIED").
- **Light:** URP 2D Lights with normal maps — desk lamp, café neon, rain outside.
- **Characters:** only the heroine walks; others stand with an idle breathing animation. Emotions are shown on dialogue portraits (3 emotions each).
- **UI:** paper look — folders, case cards, polaroids, stamps, red thread.

---

## 14. Audio

- **Sounds:** footsteps, rain, neon hum, answering machine, typewriter.
- **Music:** synthwave and jazz.

---

## 15. Technical design

- **Engine:** Unity 6.3 LTS, URP 2D, Pixel Perfect Camera.
- **Code:** C#. **Dialogue:** Ink (ink-unity-integration).
- **Data-driven cases:** a case is data, not code. People, clues, locations, board connections and unlock conditions are described in ScriptableObjects. One codebase runs any case.
- **Saves:** JSON.

> Details in `docs/ARCHITECTURE.md` (issue in v0.1).

---

## 16. Scope

**Demo (v0.2 — vertical slice):** Friday evening and night, fully in final art.

**Full game:** Friday to Monday, courtroom, three endings, epilogue — 3–5 hours.

**If the game succeeds:** a new case with the same heroine — new 48 hours.

---

## 17. Open questions

- Who is on trial and for what?
- Who is the real killer?
- The heroine's name and backstory.
- The name of the city.