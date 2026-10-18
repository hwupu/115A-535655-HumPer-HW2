# ADR-0001: Restart and architecture (2026-10-17)

Status: accepted.

**Context.** The first build was written in one go and was too buggy: compile errors reached the PO, flow/audio/UI-placement bugs, and a scene builder that rebuilt the scene and fought with the PO's manually added Meta Building Blocks.

**Decisions**
1. Start over in small steps; each step is verified headless (batch-mode Unity on a copy of the project: setup steps, Validate Setup, EditMode tests) before handing over.
2. The XR rig, controller tracking and spatial audio come from Meta Building Blocks added by hand. Setup steps never touch them (menu steps under **Magic**, idempotent, edit the open scene only).
3. Pure logic lives in an asmdef without Unity references (`HumPer.Core`: trial planner, math, localization, CSV log) and is unit-tested; the runtime layer is thin.
4. One scene, a coroutine flow (`ExperimentFlow`), scene references held in `GuiRefs`.
5. Scene hierarchy is grouped: XR, Audio, Stimuli, Experiment, Environment, UI.
