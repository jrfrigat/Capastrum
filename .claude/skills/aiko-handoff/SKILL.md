---
name: aiko-handoff
description: Hand an Aiko stage execution to another agent without losing its history.
---

The current agent cannot continue this execution (for example after a rate limit). Report the
state with aiko_report_agent_state (rate-limited or failed) and hand the execution to another
agent with aiko_handoff_execution, preserving the full attempt history.

<!-- Managed by Aiko -->
