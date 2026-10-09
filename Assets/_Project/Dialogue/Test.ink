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
* [Ask where he was that night]
    Stranger: Home. All night. Didn't even step outside.
    -> alibi
+ [Leave]
    You: Have a good night.
    -> DONE

// A statement the player can break with evidence or with silence.
= alibi
* [present:bottle_no_blood]
    You: Then how do you know what the bottle by the bench looked like?
    Stranger: ...Fine. I cut through the park around six. Didn't see anyone.
    ~ set_flag("test:stranger_lied")
    -> questions
+ [present:any]
    Stranger: And? What's that got to do with me?
    -> alibi
* [silence]
    Stranger: What? Why are you looking at me like that?
    Stranger: Okay, I went out for cigarettes. Ten minutes, tops.
    -> alibi
* [Let it go]
    -> questions
