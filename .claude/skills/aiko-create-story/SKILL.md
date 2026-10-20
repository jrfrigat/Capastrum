---
name: aiko-create-story
description: Create an Aiko Stories card and estimate it in the same pass.
---

Create a Stories card in Aiko. A functional requirement or a user-facing capability, large enough to be decomposed into tasks. Its app-point and user-point say why it matters; its complete score is re-scored after every piece of work.

This type is the workflow story: call aiko_create_card with kind=Story, a clear title taken
from what the user asked for, an own priority and, for work that changes files, the declared
scope patterns - and nothing else. Aiko names the card and lands it in the workflow's
backlog stage, so never invent a card id or a stage; the project
context lists the rest of that pipeline if you need it. Put the user's own wording into request and
what the card is asked to do into requirements when the title alone is not enough.

Estimate the card in the same pass, unless the user gave the numbers themselves: the creation
answer already carries the card's revision, so judge the size step and every criterion the
project defines, and write them with aiko_estimate_card.

Report at the end: the created card id, the size and the scores you wrote, and one line on what
the card is about.

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
