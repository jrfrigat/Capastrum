---
name: aiko-create
description: Create an Aiko card of any type the project defines, and estimate it in the same pass.
---

Create a card in Aiko. The user named a type first and then what the card is about.

Read aiko_get_project_context first: it lists every card type this project defines, the workflow
behind each one, the stages of its pipeline, the size grid and the scoring criteria. Use the type the
user named; if they named none, ask which type they mean.

Then call aiko_create_card with that type's kind, a clear title taken from what the user asked for, an
own priority and, for work that changes files, the declared scope patterns - and nothing else. Aiko
names the card and lands it in the type's backlog stage, so never invent a card id or a stage.

Give the card both of its texts. Put the user's own wording into request - verbatim, or as close as you
can get it without tidying it up into a task: the request records what was asked for, and the card stops
being able to change it once it leaves the backlog. Put what the card is asked to do into requirements
when it says more than the title: that one describes the work, and it changes as the work is understood.

Estimate the card in the same pass, unless the user gave the numbers themselves: the creation answer
already carries the card's revision, so judge the size step whose description matches the work and
every criterion the project defines, and write them with aiko_estimate_card.

Report at the end: the created card id, the size and the scores you wrote, and one line on what the
card is about.

Then stop. Creating the card, and estimating it, is the whole answer to what the user asked: do not
start a stage, do not create or change files for the card, and do not begin the work - the user starts
it with aiko-run <cardId> or "выполни <cardId>".

One exception, and only one: when the request asked for the card and its work in the same breath -
"создай и выполни", "и сразу начни", /aiko-create --run, or words to that effect - the card is not the
whole answer. Then, after estimating it, take it into work at once: start its first stage with
aiko_start_stage (the stage it landed in), and work it exactly as /aiko-run does - one stage, its
instruction, its artifacts, the re-estimate, the feed, aiko_complete_stage. Never do this on your own
initiative: the words have to be there.

<!-- Managed by Aiko -->
