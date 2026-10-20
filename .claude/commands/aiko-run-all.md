Work the project's board: take the cards whose pipeline is unfinished, in board order, and drive
each one to the end of its own workflow. This is what the board's "work the board" button asks for,
and what the user means by running the board.

Read aiko_get_project_context for the card types and their pipelines, and aiko_list_board for the
cards, where each stage got to, and the priority the board shows them with. That order is the
board's own - the computed priority, then the card id - and not the card's own score, so read it
from the board rather than sorting the cards yourself.

For each card, in that order:

- Skip a card that is already finished: it sits in the last stage of its own workflow with a
  completed run. Skip a card that waits for another one in the same way - aiko_start_stage refuses it
  and names the blocking card, and that refusal is the skip. Do not walk the blocking graph
  yourself: asking and reading the answer is the one place that rule lives.
- Work each unfinished stage as /aiko-run does: aiko_start_stage with "claude-code", do what
  the stage's instruction asks, produce the artifacts it requires, report progress, re-estimate the
  card with aiko_estimate_card, write the outcome into the card's feed with aiko_add_comment, and
  complete it with aiko_complete_stage. Then start the next stage of that card's own pipeline.
- The card is done when its last stage's run is completed. Then take the next card.

When a question for the person comes up on a card, do not stop the pass: write the question into
that card's feed with aiko_add_comment, put its run into the waiting state with
aiko_report_agent_state and "waiting-for-user", and take the next card. That card is not lost - it
is exactly where the person will look for it.

Stop the whole pass only when you cannot go on at all: the agent fails or hits its rate limit, a
policy forbids the action (a refused scope expansion, a commit or push the project denies), or a
required artifact cannot be produced. Everything else - a blocked card, a card waiting for an
answer, a card someone else has already finished - is a reason to move on, not to stop.

The pass needs no bookkeeping to be resumable: run it again and it starts from the board as it now
is. Finished cards are not picked again; a card waiting for an answer is skipped because
aiko_start_stage refuses to restart an unfinished stage; and a card the pass stopped on is picked up
by the same start, which continues that run instead of opening a second one.

The state of the project is read and written through these tools. Do not open a file under .aiko to
find out what the project says: read the order with aiko_list_work_queue, a card with aiko_get_card,
what sits beside it with aiko_get_card_artifact, the settings with aiko_get_settings. Produce a stage's
artifacts with aiko_save_card_artifact rather than by writing the file yourself - that is Aiko's own
data, and a document written past the daemon is a document the daemon does not know about. A card whose
subject is the .aiko format itself is the exception: there the file is the work.

Do not push and do not open branches: the shared checkout is the user's, and the git, commit and
push policies the project states say who may write to it.

Report at the end: which cards you worked and what changed, which you skipped and why, which are
waiting for an answer, and what is left. If an aiko_list_commands and aiko_claim_command pass
brought you here - the command's action reads "RunBoard" - close that command with
aiko_finish_command when the pass ends: "completed" with the summary, or "failed" with the reason it
stopped. Never leave it open, because the queue is what the person reads to see whether their
request happened.

<!-- Managed by Aiko -->
