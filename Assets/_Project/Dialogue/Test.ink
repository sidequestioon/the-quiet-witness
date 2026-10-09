// Test conversation for v0.1. Delete when real characters arrive.

=== test_stranger ===
Stranger: Rough night, huh?
Never seen him before. Probably a regular.
Stranger: You're the lawyer. The one taking the drifter's case.
- (questions)
* [Ask about the park]
    You: Were you in the park that morning?
    Stranger: Me? I sleep till noon.
    -> questions
* {is_set("clue:coroner_blow_to_head")} [Mention the blow to the head]
    You: The coroner found a blow to the head.
    Stranger: So it wasn't the cold. Huh.
    ~ set_flag("test:stranger_surprised")
    -> questions
* [Ask if he saw anything]
    Stranger: Saw a bottle by the bench. No blood on it, if that's what you're after.
    ~ give_clue("bottle_no_blood")
    -> questions
* [Leave]
    You: Have a good night.
    -> DONE
