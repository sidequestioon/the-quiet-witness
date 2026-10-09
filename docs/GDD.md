# The Quiet Witness — Game Design Document

**Version:** 0.2 (draft) · **Status:** Block 1 — Paper design

> This is the public, spoiler-free version of the design document. The full solution of the case — what really happened, who is responsible, and what triggers each ending — is kept in a private story bible.

---

## 1. Overview

**Pitch:** 48 hours until the trial. The whole town is sure he's guilty. You're not. A defense attorney and private investigator has two days and two nights to search a rain-soaked town for the real killer before the judge's gavel falls.

| | |
|---|---|
| Genre | Narrative detective adventure, deduction |
| Perspective | 2D side view, pixel art |
| Length | 3–5 hours, one complete story |
| Platforms | PC (Windows, macOS) — Steam, itch.io |
| Engine | Unity 6.3 LTS, URP 2D, C# |
| Languages | English first, Russian later |

**References:** Loco Motive (characters, humor, side-view walking), Tails Noir / Backbone (light and atmosphere), The Case of the Golden Idol and Return of the Obra Dinn (deduction), Ace Attorney (presenting evidence, courtroom), Shadows of Doubt (evidence board), Her Story (archive search).

**Unique hooks:**
- **Time without a timer** — story events move the clock, not a countdown.
- **Newspaper as chapter** — every time segment opens with a front page that reacts to the player's actions.
- **Courtroom finale** — the player delivers the defense speech, built from the evidence they found.
- **A witness who never spoke** — the victim was Deaf and spoke through her drawings. They are scattered all over town.
- **Silence as an answer** — sometimes saying nothing makes people say too much.
- **Multiple endings** — what happens in court depends on what the player found and the choices they made.

---

## 2. Setting & tone

**Setting:** Millbrook, autumn, late 1980s. A rainy, mid-sized town: small enough to have a single judge, big enough to glow with neon at night. A downtown strip with a cinema, a theater and bars, a few high-rises, a motel on the edge of town. VHS, answering machines, payphones, polaroids.

The case is high-profile. Every newspaper writes about it, the town is divided, and public opinion is against the client.

**Tone:** noir that doesn't take itself too seriously. The death is real and dark, but the people around it are eccentric. Humor lives in object descriptions, the investigator's inner monologue and the witnesses' quirks.

---

## 3. Premise

**The victim.** A Deaf street artist. She lived on the street, but all of Millbrook knew her: she drew in the park every day and traded her drawings for food. One cold autumn morning she is found dead in the park. The first headlines say she froze to death. Then the coroner finds a blow to the head.

**The accused.** A homeless man, her friend — the only person in town who understood her sign language. He found the body and took her blanket. That night he was drunk and remembers nothing, and he already has a record. For the police, the case is closed. The trial is set for Monday.

**The investigator.** A defense attorney who also works as a private investigator, running a one-man agency from a small office downtown. The owner of the diner where the artist traded her drawings and her regulars don't believe the official story. They pooled their money and hired him to defend the accused. The game begins on Friday evening, when he has already taken the case. On his office wall hangs a portrait of him that the artist once drew.

**The quiet witness.** She couldn't hear, and she couldn't speak. But she saw — and she drew. Her drawings are scattered all over Millbrook, and some of them remember more than people do.

---

## 4. Characters

> Names are not final.

| Character | Role |
|---|---|
| The investigator | Defense attorney and private investigator; the player character |
| The client | Homeless man accused of the killing; the victim's friend, knows sign language |
| The victim | Deaf street artist; present through memories and drawings |
| The diner owner | Hires the investigator with money collected by the regulars |
| The judge | The only judge in Millbrook; presides over the trial |
| The prosecutor | Ambitious, and sure the case is open and shut |
| The coroner | Examined the body; wrote the official report |
| The developer | Has big plans for the park |
| The journalist | Writes the headlines; knows every rumor in town |
| The park kids | Skateboarders who hang out in the park at night |

**Also in town:** a gallery owner who sold the artist's drawings, a rival from the homeless camp, a bartender, a motel night clerk.

---

## 5. Locations

| Location | Notes |
|---|---|
| Office (hub) | The investigator's agency; returns here between trips |
| The park | Where the artist drew — and where she was found. Fountain, fence with drawings, homeless camp |
| The diner | Where she traded drawings for food |
| Police station | Case files, evidence, visits to the client |
| Morgue | The coroner's domain |
| Downtown bar | Neon, night life, the bartender who sees everything |
| The motel | On the edge of town |
| Courthouse | The finale |

> The full clue list (23 clues, 3 red herrings) is in the private story bible.

---

## 6. Core loop

1. Read the newspaper at the start of a time segment.
2. Plan in the office: answering machine, phone book, archive, evidence board.
3. Travel to a location through the town map.
4. Explore: walk around, examine objects, find clues and drawings.
5. Interrogate people; new clues unlock new questions.
6. Connect clues on the board to form conclusions.
7. Conclusions unlock new locations and questions. The key clue of the segment moves time forward.

The loop repeats until the board shows the full picture. On Monday morning the trial begins.

---

## 7. Time system

There is no visible timer. Time is moved by key story events: when the player finds the main clue of a segment, the next segment begins.

| Chapter | Newspaper | What happens | Weather |
|---|---|---|---|
| 1. Friday | Evening edition: "Trial on Monday" | Start in the evening in the office: the case file and the first clues | Rain |
| 2. Saturday | Saturday edition | Morning → day → evening → night, advanced by the story | Fog |
| 3. Sunday | Sunday edition | Last leads, pressure grows | Clear and cold |
| 4. Monday | Morning edition before the trial | Dawn, trial, finale | — |
| Epilogue | Tuesday's newspaper | The verdict and what happened to everyone | — |

**Urgency without a timer:** every segment has side leads. When the player advances time, some of them close forever (a bar closes, a witness leaves town). Before such a step the investigator warns the player. More side clues mean a stronger defense in court.

**Light:** each location is drawn once; morning, evening and night are lighting setups. At night different people are around and different places are open.

> The detailed breakdown of each day is in the private story bible.

---

## 8. Screens & systems

| Screen | What the player does |
|---|---|
| Main menu | Case folder with a stamp, save slots |
| Office (hub) | Desk, phone, answering machine, board; returns here between trips |
| Town map | Paper map on the desk; locations appear during the investigation; instant travel with a location title card |
| Location | Walks in side view, examines objects, talks to people |
| Newspaper | Front page opening each time segment |
| Dossiers & archive | People's photos, jobs, connections; old documents and clippings |
| Interrogation | Questions unlocked by clues; present evidence to catch a lie |
| Evidence journal | Clue cards with descriptions; dialogue log |
| Evidence board | Photos and clues, red threads, conclusions |
| Drawings | The artist's drawings the player has found |
| Courtroom | Finale: the defense speech |

---

## 9. Evidence board

The player connects two cards with a red thread. If the connection is correct (for example, "café receipt" + "brother's alibi"), a conclusion appears: "the alibi is false". A conclusion unlocks a new question or location. Wrong connections simply do nothing — no penalty.

> The full "what unlocks what" chart is in the private story bible.

---

## 10. Dialogue & interrogation

- Questions unlock from clues found.
- **Present evidence** during a conversation to catch a lie.
- **Silence** is an answer option: sometimes the other person fills the pause and says too much.
- **Dialogue log:** everything said can be reread.
- **Notebook:** the investigator automatically notes small details that are easy to miss; rereading them reveals leads.
- **Trades:** some people only talk after you bring them something they need.

Dialogue is written in Ink.

---

## 11. Newspapers

- Every chapter opens with a newspaper; Tuesday's issue shows the outcome of the trial.
- Clues are hidden not only in articles but also in ads, obituaries and the weather forecast.
- Headlines depend on the player's actions.

---

## 12. Courtroom finale & endings

On Monday the investigator stands in court as the defense attorney. First the player cross-examines the prosecution's witnesses, presenting evidence to break their testimony. Then the player builds the defense speech: for each point, they choose specific evidence. Strong evidence for most points can carry the case even if something was missed; too little, and the defense falls apart.

There are **five endings**. They depend on the evidence the player collected and on the choices they make before the trial. The outcome is revealed in Tuesday's newspaper, followed by an epilogue: a newspaper chronicle of what happened to every character.

> Prosecution witnesses, the defense speech points and ending conditions are in the private story bible.

---

## 13. Collectibles & side stories

- **The artist's drawings.** Scattered around town: in the diner, in bars, in people's homes. Some of them contain leads, but the investigation can be completed without finding them all. Finding every drawing is an achievement.
- **Side stories:** one or two per chapter, about the town and its people.
- **Rewards:** in-game badges and a journal of solved cases; Steam achievements later.

---

## 14. Art direction

- **Style:** detailed pixel art with living light (Tails Noir) and quirky, funny characters (Loco Motive).
- **Base resolution:** 480×270, scaled ×4 to 1920×1080.
- **Palette:** 24–32 muted colors, cold blues and browns. One accent: red (threads, stamps, blood, "UNVERIFIED").
- **Light:** URP 2D Lights with normal maps — desk lamp, neon signs, rain outside.
- **Characters:** only the investigator walks; others stand with an idle breathing animation. Emotions are shown on dialogue portraits (3 emotions each).
- **UI:** paper look — folders, case cards, polaroids, stamps, red thread.
- **The artist's drawings** have their own hand-drawn style that stands out from the pixel art.

---

## 15. Audio

- **Sounds:** footsteps, rain, neon hum, answering machine, typewriter.
- **Music:** synthwave and jazz.

---

## 16. Technical design

- **Engine:** Unity 6.3 LTS, URP 2D, Pixel Perfect Camera.
- **Code:** C#. **Dialogue:** Ink (ink-unity-integration).
- **Data-driven cases:** a case is data, not code. People, clues, drawings, locations, board connections and unlock conditions are described in ScriptableObjects. One codebase runs any case.
- **Saves:** JSON.

> Details in `docs/ARCHITECTURE.md` (issue in v0.1).

---

## 17. Scope

**Demo (v0.2 — vertical slice):** Friday evening and night, fully in final art.

**Full game:** Friday to Monday, courtroom, five endings, epilogue — 3–5 hours.

**If the game succeeds:** a new case with the same investigator — another 48 hours.

---

## 18. Open questions

- Character names, including the investigator.
- Detailed clue scenes and dialogue.

---

*Writing note: in all game text, use "Deaf" (capital D) rather than "deaf-mute", which is considered outdated and offensive.*
