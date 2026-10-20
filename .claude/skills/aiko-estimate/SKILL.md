---
name: aiko-estimate
description: Estimate an Aiko card - judge the size step and every scoring criterion the project defines.
---

Estimate a card in Aiko. The user named the card: its id is the first argument.

Read aiko_get_project_context and aiko_get_card. The context carries the project's size grid, with
each step's description, and every scoring criterion with its range and weight; the card carries its
title, its requirements and its declared scope.

Judge the work from those and call aiko_estimate_card with the card id, the revision you read and
the estimate: the size step whose description matches the work, and one "criterionId=score" entry per
criterion the project defines. Set the own priority only when the criteria do not already account for
it. A step that means "too large for one stage" is a plan to split the card, not an estimate to start
from - say so instead of writing it.

Report at the end: the size and the scores you wrote, what you judged them from, and whether the
card looks too large to run as it stands.

<!-- Managed by Aiko -->
