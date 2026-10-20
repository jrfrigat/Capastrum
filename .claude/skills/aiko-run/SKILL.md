---
name: aiko-run
description: Run an Aiko card - do what its current stage asks for, report progress and complete the stage.
---

Run a card in Aiko. The user named the card: its id is the first argument, an optional stage id may
follow as the second, and --all may follow as the last one.

Read aiko_get_project_context and aiko_get_card for that card. The context lists the card's type, its
pipeline and what each stage demands; the card says which stage it is in right now, and which cards
block it.

A card that waits for another one is not yours to start: aiko_start_stage refuses it and names the
blocking card. Do not look for a way around that - say so to the user and offer the blocking card
instead, because finishing it is what unblocks the work they asked for.

Run ONE stage and stop: the user asked for this stage, and asking for the next one is theirs to do.
Start it with aiko_start_stage, passing the card id, the id of the stage you mean to work, and
"claude-code" as the agent adapter id. The start moves the card into that stage, so a card may
still be sitting in its backlog when you begin - and until that start, the card has no work in it:
do not create or change any file for it beforehand. A stage that is not finished is the stage you
work: starting it again continues that same run, and starting a different one is refused until it is
done.

If the user passed --all, do not stop between stages: after aiko_complete_stage, start the next stage
of the pipeline and keep going until it ends, filling in every card as you go (progress, artifacts and
the re-score below). Even then, stop and tell the user when a stage asks them a question, when an agent
fails or hits its rate limit, when the stage's policy forbids an action, or when a required artifact
cannot be produced.

--all descends into the card's children when the card is a container - an epic holds its goal across
stories, a story across tasks, and the stage that asks for the breakdown is the stage that gets it.
Create the children a stage calls for with aiko_create_card and link each one to its parent with
aiko_link_cards ("parent-child"). A created child is not stage work, so creating it costs nothing; when
a child has to be worked, drive it to the end of its own pipeline - the same start, artifacts,
re-estimate, feed note and complete - and go back to its parent only afterwards. The children and the
order to take them in come from the parent's own statement on its card; the board's priority is not
that order.

The whole tree shares one run slot. Aiko counts the runs whose state is Running against the project's
maxConcurrentRuns, and a run that is parked, waiting or needs attention holds no slot, so a parent left
running refuses its own child's start. Park the parent with aiko_pause_execution, the reason naming the
child you are descending into, work the child, then continue the parent with aiko_resume_execution and
finish the stage it was in. Park a stage that is still open: a stage that already has a completed run
reads as completed on the board, and parking it would hide that the card is waiting - do not do it.
Never leave a parent parked and forgotten: a stop names the card it stopped on and says which parent
was left parked and why.

A child that cannot be worked stops the pass: aiko_start_stage refusing it because another card blocks
it, a question to the user, a failure or a rate limit, a forbidden action. The parent's stage cannot be
completed honestly without its child, so do not skip it and do not complete the parent around it.

Do what the stage's instruction asks for and honour its beforeSkills and afterSkills. Produce the
artifacts the stage requires, because they are what the stage is judged by. Complete the stage with
aiko_complete_stage once its instruction and artifacts are done - the card moves on from there, and
a card whose stage was never run cannot move on at all.

Keep aiko_report_progress updated with the summary, the steps done and left, and the complete current
list of the files you changed. If the work needs files outside the card's declaredScopeFiles, call
aiko_request_scope_expansion and wait for the user's decision before touching them.

Do not push and do not open branches: the shared checkout is the user's, and the git, commit and push
policies the project states in aiko_get_project_context say who may write to it. Aiko has no push of its
own, so nothing enforces this but the rule itself - follow it.

Read the card's feed with aiko_list_comments before you start the work: the stages before you leave
notes there - what they learned, what the user asked for, what they left for you - and the feed is the
only place that survives a stage. Post the outcome of the stage with aiko_add_comment before you
complete it - what you did, what you found, what is left and what the next stage or agent will need -
and sign it with your own adapter id as the author, so the feed says which agent wrote what. Answer what
is already there rather than repeating it.

Finish with aiko_complete_stage, recording the files you changed, the artifacts you produced and how
you verified the result, and keep durable conclusions with aiko_store_memory. If you cannot finish - a
rate limit, a failure - report the state with aiko_report_agent_state and hand the execution to
another agent with aiko_handoff_execution rather than dropping it.

The state of the project is read and written through these tools. Do not open a file under .aiko to
find out what the project says: read the order with aiko_list_work_queue, a card with aiko_get_card,
what sits beside it with aiko_get_card_artifact, the settings with aiko_get_settings. Produce a stage's
artifacts with aiko_save_card_artifact rather than by writing the file yourself - that is Aiko's own
data, and a document written past the daemon is a document the daemon does not know about. A card whose
subject is the .aiko format itself is the exception: there the file is the work.

Report at the end: the card, the stage you ran, what changed, what is left, and whether the card is
ready for its next stage.

<!-- Managed by Aiko -->
