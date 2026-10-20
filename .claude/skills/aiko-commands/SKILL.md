---
name: aiko-commands
description: Carry out the commands the Aiko board placed for an agent - start, pause, resume or answer a stage.
---

Carry out what the Aiko board asked this project's agents to do. The person pressed a button while
no agent was running, so the request has been waiting in the project's queue since.

Read the queue with aiko_list_commands (state "open"). Take one command with aiko_claim_command,
passing "claude-code" as the agent adapter id. A refusal is not a failure: it means another
agent already has that command, or the person placed it for a different agent - say so and take the
next one rather than insisting.

Do what the command's action asks, with the tool that already does it:

- start - aiko_start_stage with the command's cardId and stageId, and your own adapter id. Then work
  that stage exactly as /aiko-run does: the same rules about one stage per run, the card's feed, the
  artifacts the stage requires, the re-score before completing it, and no push.
- pause - aiko_pause_execution for the command's executionId, with the command's text as the reason.
- resume - aiko_resume_execution for the command's executionId.
- answer - write the command's text into the card's feed with aiko_add_comment, then call
  aiko_resume_execution, so the run that is waiting for that answer reads it and continues.
- work the board - a command whose action reads "RunBoard" names no card: it asks for the whole
  board. Run the /aiko-run-all procedure and let the board decide the order and what is left.

Close the command with aiko_finish_command: completed when the work the command asked for happened,
failed when it could not be, with a message naming which. Never leave a taken command open - the
person reads the queue to see whether their request happened, and a command that stays open after
the work is done is worse than having no queue at all.

Report at the end: which commands you took, what each asked for, what you did with them, and what is
still waiting.

<!-- Managed by Aiko -->
