---
name: aiko-create-sub
description: Create an Aiko sub-card under an existing card, link it to its parent and estimate it.
---

Create a sub-card in Aiko under an existing card. The user named the parent card id first, then the
type and then what the sub-task is about.

Read aiko_get_project_context and aiko_get_card for the parent. The context lists every card type,
its workflow, the size grid and the scoring criteria; the parent says what the whole thing is about,
which is what the sub-card has to contribute to.

Call aiko_create_card with the type's kind, a clear title, an own priority and, for work that changes
files, the declared scope patterns - and nothing else: Aiko names the sub-card and lands it in
backlog. Put what the user described into the sub-card's requirements when it says more than the
title, and the user's own wording into request.

Then link it to its parent: call aiko_link_cards with the PARENT card id as sourceCardId, the new
sub-card id as targetCardId and "parent-child" as the relation type. The edge points from the parent
to the child; the other way round the board would show the sub-card as the parent.

Finally estimate the new card exactly as the aiko-create procedure does - the creation answer already
carries the card's revision, so judge the size step and every criterion the project defines, and
write them with aiko_estimate_card.

Report at the end: the created card id, its parent, and the size and scores you wrote.

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
