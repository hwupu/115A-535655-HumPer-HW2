# Sprint Board

## Sprint 1: build the experiment (done, Quest Link tested)
| To Do | Doing | Done |
|---|---|---|
| | | PB-01 hygiene, LFS |
| | | PB-02 room + shoebox, PB-03 GI lighting |
| | | PB-04 stimuli, PB-05 trial task |
| | | PB-06 practice, PB-07 experiment |
| | | PB-08 GUI, PB-09 logging |
| | | PB-10 Debug bench, PB-11 conditions |
| | | PB-12 volume (closed), PB-13 luminance (closed) |
| | | PB-19 docs |

## Sprint 2: verify on hardware and finish
| To Do | Doing | Done |
|---|---|---|
| PB-15 RT60 measurement tool (needs approval) | | PB-14 reverb material (superseded, closed) |
| PB-17 cardboard image (PO to provide) | | PB-16 standalone headset test |
| PB-20 analysis export (PO later) | | PB-18 "Hot Fix" step, PB-21 resume / cancel |
| PB-23 CSV retrieval on macOS (untested) | | PB-22 number pad, PB-24 setup instructions |
| | | PB-25 controller labels (tried, reverted), PB-26 fixed backgrounds, PB-27 stimulus pulse |
| | | PB-28 Debug reverb + playback, PB-29 acoustics per condition, PB-30 room 8.72 x 3.5 x 15 |

## Working agreement
- Small steps. Each step is verified headless before handoff: a copy of the project is run with Unity in batch mode (setup steps + EditMode tests), because the PO's editor is open on the real project.
- Things only a headset can show (rendering, ray alignment, system keyboard, real audio) are listed as tests for the PO.
- Definition of done: compiles, unit tests pass, setup steps run and Validate Setup is clean, behavior documented, decision recorded, prompt logged.
