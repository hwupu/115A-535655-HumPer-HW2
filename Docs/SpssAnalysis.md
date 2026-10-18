# Analysing the CSV files in SPSS (step by step, with a little statistics)

Written for someone who has just installed SPSS and has not done this before. Read part 1 and 2 once (10 minutes); then follow parts 3 to 7 with the data open.

Design in one line: 5 participants, each did all 3 conditions (Quiet, Noisy, Reverb, always in that order) with 6 trials each: 18 real trials per person, 90 in total.

---

## 1. What the experiment measures

**Task (method of adjustment).** On each trial a picture (the cardboard) is shown at a fixed azimuth (one of +-7.5, +-22.5, +-37.5 deg). A noise sound starts far away, at +60 or -60 deg, and the participant moves it with the triggers until the sound seems to come from the picture, then presses A. We log where it ended (`audio_final_az_deg`).

**Ventriloquist effect.** When sound and picture are close, the brain merges them into one event and the sound is *heard* at the picture's place although it is physically elsewhere. So when the participant stops moving the sound, a physical gap between sound and picture can remain, because the two already feel like one. That leftover gap is how much disparity the participant tolerates: an estimate of the **audio-visual disparity threshold** (the largest gap at which sound and picture still merge). The research question is whether room acoustics (Quiet 0.3 s, Noisy 0.7 s + background talk, Reverb 2.2 s) change that gap. Hypothesis H1: the gap is larger in Reverb than in Quiet (a blurred sound is easier to "capture" by the picture).

This is a method-of-adjustment estimate, not the classic staircase threshold. It is a fair operational definition, but say so in the report.

### The error measures (this is the part that confuses everybody)

The log has `signed_error_deg = audio_final - visual`. Positive means the sound ended to the **right** of the picture. We use three versions:

| Name | Formula | What it tells you | Weakness |
|---|---|---|---|
| **Signed error** `signed_error_deg` | final - visual | Direction: is the sound systematically placed left or right of the picture? A constant offset here is a localisation *bias* (head turned, controller drift). | Left and right errors cancel out: +6 and -6 average to 0 although both are wrong. Never use it alone to measure "how far off". |
| **Absolute error** `abs_error` | abs(signed error) | Size of the gap, whatever the side. | Cannot be negative, so even a participant with pure noise and no bias has a positive mean. It is skewed (many small values, a few big), which bends the normal-distribution assumption. |
| **Stop-short** `stop_short_deg` | signed error x (+1 if the sound started on the right, -1 if on the left) | Gap measured **from the side the sound came from**. Positive = stopped before reaching the picture (still on the start side) = the sound is accepted as matching the picture while physically apart. Negative = overshoot, went past the picture. | The sign depends on the start side, which alternates by design (+60, -60, ...), so each condition is balanced. |

**Use `stop_short_deg` as the main measure of the threshold** (it has the direction that matters and does not cancel). Use `abs_error` as a robustness check and `signed_error_deg` to look for a left/right bias. The script in part 3 computes all three.

The assumptions behind them: the participant stops when the sound seems to match the picture and does not stop early out of impatience; the adjustment speed (10 deg/s) is the same for everyone; each trial is a fresh judgement. The fixed order (Quiet, Noisy, Reverb) is the big weakness: any practice, tiredness or boredom effect adds to the condition effect and cannot be separated from it (see part 6).

---

## 2. Statistics you need, in ten lines

- **Mean, SD, SE, CI.** Mean = average. SD = spread of single values. SE = SD / sqrt(n) = how uncertain the mean is. A 95% **confidence interval** (CI) is the mean +- about 2 SE; if it does not include 0 (or does not overlap another mean's CI) that is evidence of an effect.
- **Null hypothesis (H0)** = "the conditions make no difference". The **p-value** is the probability of seeing a difference at least this big if H0 were true. p < .05 is the usual cut-off for "significant". It says nothing about how *big* the effect is.
- **Effect size.** Always report one: the mean difference in degrees (most readable), and **partial eta squared** for an ANOVA (.01 small, .06 medium, .14 large) or **Cohen's d** for a difference between two conditions (.2 small, .5 medium, .8 large).
- **Within-subjects design.** Everybody did all conditions, so each person is their own control and the person-to-person differences drop out. That is why a *repeated-measures* test is used, not an ordinary ANOVA that compares separate groups.
- **Repeated-measures ANOVA** asks: are the three condition means further apart than chance would produce? It gives an **F** value and a p. Its assumptions: roughly normal data, and **sphericity** (the differences between each pair of conditions vary about equally). SPSS tests that with *Mauchly's test*; if p < .05 read the **Greenhouse-Geisser** row.
- **Post-hoc / pairwise tests.** If the ANOVA is significant you compare pairs (Quiet-Noisy, Quiet-Reverb, Noisy-Reverb). With three comparisons you could get a false alarm by luck, so p is multiplied by 3 (**Bonferroni**). For a hypothesis stated *beforehand* with a direction (H1: Reverb > Quiet) a one-sided test (halve the two-sided p) is allowed.
- **Friedman test.** The non-parametric cousin: it uses only the ranks (who is largest) so it needs no normal data. Good when the sample is tiny.
- **Mixed model.** Handles all 90 trials at once. The person is a **random effect** (each person has their own baseline), condition is a **fixed effect**. It copes with trials of the same person being similar to each other (**not independent**), which a plain t-test or correlation on 90 rows would wrongly ignore.
- **Correlation** (r, rho) measures how two quantities move together (-1 to +1). It does not show cause. Use **Spearman** (ranks) when data are skewed.
- **Power.** With n = 5 only large effects can be detected. A non-significant result means "not shown", not "no effect". Report effect sizes, CIs and individual data.

---

## 3. Prepare the data (one command)

1. Copy the CSVs of all participants (not the `_DEBUG` ones) into one folder, for example `~/analysis/` (macOS: `adb pull`, see the README).
2. Open **Terminal** and run (the script is in the repository, folder `Tools`):

   ```
   zsh /path/to/repo/Tools/prepare_for_spss.sh ~/analysis
   ```

   It prints how many trials each participant has per condition (must be 6/6/6) and writes two files into `~/analysis/spss_out/`:
   - `trials.csv`: one row per real trial, with the columns below. (The practice and debug trials are already removed.)
   - `per_person.csv`: one row per participant with the per-condition means, in the wide layout SPSS needs for a repeated-measures ANOVA.

   Needs only what macOS has (zsh and awk), nothing to install.

`trials.csv` columns: `participant, condition, cond` (1 Quiet, 2 Noisy, 3 Reverb), `trial` (1 to 6), `visual_az_deg, audio_start_az_deg, audio_final_az_deg, signed_error_deg, abs_error, stop_short_deg, abs_offset` (gap at the start), `abs_visual` (how far to the side the picture was), `side` (0 left, 1 right picture), `duration_s, source_file`.

`per_person.csv` columns: `participant`, then for each measure three columns (`_1` Quiet, `_2` Noisy, `_3` Reverb): `stopshort`, `abs`, `signed_mean`, `sd_error` (spread of the person's errors: how precisely they place the sound), `dur` (seconds), and the trial counts `n_1..n_3`.

## 4. Open them in SPSS

**File > Import Data > CSV Data...** and choose `trials.csv`. Header row: Yes. Keep the suggested types (numbers numeric). Finish. Then open `per_person.csv` the same way: SPSS opens each file in its own **Data Editor** window (switch with the **Window** menu; the *Output Viewer* window collects all the results). Save both as `.sav` (File > Save as) so you do not need to import again.

Now, in the `trials` window, label the condition code (**Variable View**, `cond` row, *Values* cell: 1 = Quiet, 2 = Noisy, 3 = Reverb), or run:

```
VALUE LABELS cond 1 'Quiet' 2 'Noisy' 3 'Reverb'.
VARIABLE LEVEL cond (NOMINAL).
```

(Syntax: **File > New > Syntax**, paste, select the lines, click the green triangle.)

**Look before you test.** In the `trials` window: **Analyze > Descriptive Statistics > Explore**, Dependent List `stop_short_deg`, `abs_error`, `duration_s`, Factor List `cond`, Plots: Boxplots. Look for values that look wrong (for example an error near 90 or 150 deg = a clamped or forgotten trial). Then **Graphs > Chart Builder > Bar (Simple Error Bar)**: `cond` on X, `stop_short_deg` on Y, 95% CI.

```
EXAMINE VARIABLES=stop_short_deg abs_error duration_s BY cond /PLOT=BOXPLOT /STATISTICS=DESCRIPTIVES.
GRAPH /ERRORBAR(CI 95)=stop_short_deg BY cond.
```

---

## 5. Test H1: does the condition change the threshold?

This is the section that answers the research question. Do route A, then B, and see whether they agree.

### A. Repeated-measures ANOVA on per-person means

In the `per_person` window: **Analyze > General Linear Model > Repeated Measures...**

1. Within-Subject Factor Name `condition`, Number of Levels `3`, **Add**, **Define**.
2. Put `stopshort_1`, `stopshort_2`, `stopshort_3` into the three slots (in this order).
3. **Options:** tick *Descriptive statistics* and *Estimates of effect size*; move `condition` into "Display Means for" and tick *Compare main effects* with **Bonferroni**. **Plots:** `condition` on the horizontal axis, Add. OK.

```
GLM stopshort_1 stopshort_2 stopshort_3
  /WSFACTOR=condition 3 Polynomial
  /METHOD=SSTYPE(3)
  /EMMEANS=TABLES(condition) COMPARE ADJ(BONFERRONI)
  /PRINT=DESCRIPTIVE ETASQ
  /WSDESIGN=condition.
```

Read the Output Viewer from the top:
- **Within-Subjects Factors / Descriptive Statistics:** the means of each condition in degrees. Look at them first: is Reverb > Noisy > Quiet?
- **Mauchly's Test:** p < .05 means sphericity is violated, so use the **Greenhouse-Geisser** line below.
- **Tests of Within-Subjects Effects**, row `condition`: **F**, **Sig.** (p) and **Partial Eta Squared**. Significant = the three means are not all equal.
- **Tests of Within-Subjects Contrasts**: the `Linear` row tests a steady rise Quiet -> Noisy -> Reverb (that is exactly H1's ordering). Remember that the order of the blocks and the condition are confounded.
- **Pairwise Comparisons:** Mean Difference and the Bonferroni-adjusted Sig. for each pair. **The Quiet-Reverb row is the H1 test.** If it shows a positive difference (Reverb larger) and p/2 < .05 your one-sided hypothesis is supported.

Repeat with `abs_1..3` (robustness) and with `dur_1..3` (does time per trial change?).

**Non-parametric check:** *Analyze > Nonparametric Tests > Legacy Dialogs > K Related Samples*, add `stopshort_1`, `stopshort_2`, `stopshort_3`, tick **Friedman**:

```
NPAR TESTS /FRIEDMAN=stopshort_1 stopshort_2 stopshort_3.
```

With only 5 people even a perfectly consistent ordering gives p of about .007 at best, so do not expect miracles; look at how many of the 5 people show the same ordering (the line plot of the three means per person).

### B. Mixed model on all 90 trials

In the `trials` window: **Analyze > Mixed Models > Linear...**
1. Subjects: `participant` (leave Repeated empty), Continue.
2. Dependent: `stop_short_deg`; Factor: `cond`; Covariates: `trial`, `abs_visual` (the picture's eccentricity: sounds off to the side are harder to localise).
3. **Fixed...:** Factorial, add `cond`, `trial`, `abs_visual`. **Random...:** move `participant` into Combinations, tick *Include intercept*. **Statistics:** *Parameter estimates*. **EM Means:** `cond` with *Compare main effects*, Bonferroni. OK.

```
MIXED stop_short_deg BY cond WITH trial abs_visual
  /FIXED=cond trial abs_visual | SSTYPE(3)
  /METHOD=REML
  /PRINT=SOLUTION TESTCOV
  /RANDOM=INTERCEPT | SUBJECT(participant) COVTYPE(VC)
  /EMMEANS=TABLES(cond) COMPARE ADJ(BONFERRONI).
```

Read **Type III Tests of Fixed Effects**: `cond` row, Sig. < .05 means the conditions differ after taking each person's baseline into account. **Estimated Marginal Means** give the condition means with CIs, **Pairwise Comparisons** the pairs. In **Estimates of Fixed Effects**, the `trial` row is the change per trial (a practice or fatigue slope).

If SPSS says the model did not converge or a covariance parameter is redundant, you have too little data for the random intercept: remove `abs_visual` or accept route A as the main result.

---

## 6. More analyses that suit this study

1. **Is there a gap at all?** One-sample t-test of `stop_short_deg` against 0 in each condition (split the file by `cond`: *Data > Split File*, then *Analyze > Compare Means > One-Sample T Test*, test value 0). A mean clearly above 0 means participants stop short of the picture, i.e. the picture "captures" the sound (the ventriloquist effect itself). The per-person means in `per_person.csv` are the right input (n = 5).
2. **Precision.** `sd_error_1..3` is each person's spread of errors: a large spread means a vague sound location. The theory (maximum-likelihood cue combination, Alais & Burr 2004) says vision wins more when sound location is *less precise*, so Reverb should raise this spread *and* the gap. Test with the same repeated-measures ANOVA on `sd_error_1..3`.
3. **Left/right bias.** `signed_error_deg` means per side (`side` as a factor): a consistent positive or negative value is a localisation bias (controller or head offset), not a ventriloquist effect.
4. **Learning, fatigue, order.** `trial` in the mixed model; add `cond*trial` (*Fixed...*, select both, Add) to see whether the trend differs per condition. Plot: Chart Builder > Line, `trial` on X, `stop_short_deg` on Y, `cond` as colour. **Important:** the order Quiet -> Noisy -> Reverb is the same for everyone, so a condition effect cannot be separated from time on task. State it as a limitation (or run a later group with the order reversed).
5. **Duration and error.** Do slower answers mean smaller gaps? Trials of one person are dependent, so do not correlate all 90 rows. Per person: *Data > Split File > Organize output by groups* on `participant`, then *Analyze > Correlate > Bivariate*, `duration_s` and `abs_error`, Spearman. Or add `duration_s` as a covariate in the mixed model.

```
SORT CASES BY participant.
SPLIT FILE LAYERED BY participant.
NONPAR CORR /VARIABLES=duration_s abs_error /PRINT=SPEARMAN.
SPLIT FILE OFF.
```

6. **Check the acoustics.** Look in the CSV for the `rt60_measured` events if you measured RT60 on the headset in the Debug bench (not logged during the trials). Reporting "target 0.3 / 0.7 / 2.2 s, measured X / Y / Z s" makes the condition definition credible.
7. **Not collected, worth adding if there is time:** an audio-only baseline (the same adjustment task with no picture) would show how accurate the sound localisation is in each room *without* any visual capture, so the gap can be attributed to the picture and not to poor hearing of the sound.

## 7. What to report

For each result: the mean (and SD or 95% CI) per condition in degrees, the test (F with its degrees of freedom, p, partial eta squared; or the mixed-model F and p), and the pairwise comparisons. State n = 5, exploratory, the fixed order, and show the individual participants (line plot: `stopshort_1..3` per person). Interpret in degrees: "the sound was accepted as matching the picture at 6 +- 3 deg in Quiet and 14 +- 5 deg in Reverb" says more than a p-value.

## Common problems

| Problem | Fix |
|---|---|
| Script prints `WARNING: every participant should have 6 trials per condition` | A session was cancelled, repeated, or a resumed session was saved as a second file. Look at `source_file` in `trials.csv`; fix the folder and run again. |
| Numbers imported as text | In the import wizard set the type to Numeric. The log always uses a dot as decimal separator. |
| `participant` appears once per row but should be a grouping | In *Variable View* set Measure to *Nominal* for `participant` and `cond`. |
| Mixed model: "Hessian matrix is not positive definite" | Too little data for that model: remove a covariate or use route A. |
| Nothing happens when running syntax | Select the lines first, then Run Selection; every command ends with a full stop. |
