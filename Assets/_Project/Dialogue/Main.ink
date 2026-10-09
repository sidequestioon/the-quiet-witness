// The Quiet Witness - master ink file.
// The game jumps straight into knots (DialogueStarter > Knot), so the top flow just stops.
//
// Writing rules:
//   Name: text      -> a character speaks
//   plain text      -> narration or the investigator's thoughts (shown in italics)
//   * [Choice]      -> the brackets hide the choice text from the dialogue
//   * [silence]     -> shown as "... (stay silent)"
//   * [present:<clue id>] -> taken when the player presents that clue
//   + [present:any]       -> answer for any other clue (use + so it can repeat)

// Game functions. In the game they talk to GameState;
// the bodies at the bottom only run in Inky, for testing.
EXTERNAL is_set(flag)
EXTERNAL set_flag(flag)
EXTERNAL give_clue(id)

INCLUDE Test.ink

-> DONE

=== function is_set(flag) ===
~ return false

=== function set_flag(flag) ===
~ return

=== function give_clue(id) ===
~ return
