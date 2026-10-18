#!/bin/zsh
# Merge the HumPer CSV logs of all participants into two SPSS-ready files.
#   Usage:  zsh Tools/prepare_for_spss.sh <folder with the CSV files> [output folder]
# Output (default <folder>/spss_out):
#   trials.csv      one row per real trial (long format; for the mixed model, plots, correlations)
#   per_person.csv  one row per participant, per-condition means (wide format; for the repeated-measures ANOVA)
# Only needs the tools macOS and Linux already have (zsh and awk). Works in bash too.

dir="${1:-}"
if [ -z "$dir" ] || [ ! -d "$dir" ]; then
  echo "Usage: zsh $0 <folder with the CSV files> [output folder]" >&2
  exit 1
fi
out="${2:-$dir/spss_out}"
mkdir -p "$out" || exit 1

# real sessions only: skip Debug files and anything we wrote ourselves
files=()
for f in "$dir"/*.csv; do
  [ -f "$f" ] || continue
  case "$(basename "$f")" in
    *_DEBUG*|trials.csv|per_person.csv) continue ;;
  esac
  files+=("$f")
done
if [ ${#files[@]} -eq 0 ]; then
  echo "No CSV files found in $dir" >&2
  exit 1
fi
echo "Reading ${#files[@]} file(s):"
for f in "${files[@]}"; do echo "  $(basename "$f")"; done

LC_ALL=C awk -F, -v trials="$out/trials.csv" -v person="$out/per_person.csv" '
function abs(x) { return x < 0 ? -x : x }
BEGIN {
  print "participant,condition,cond,trial,visual_az_deg,audio_start_az_deg,audio_final_az_deg,signed_error_deg,abs_error,stop_short_deg,abs_offset,abs_visual,side,duration_s,source_file" > trials
  code["quiet"] = 1; code["noisy"] = 2; code["reverb"] = 3
}
{ sub(/\r$/, "") }
$1 == "trial" && $5 == "EXPERIMENT" && ($6 in code) {
  id = $4; c = code[$6]
  vis = $8 + 0; start = $9 + 0; fin = $10 + 0; err = $11 + 0; dur = $12 + 0
  s = (start > 0) ? 1 : -1
  stop = err * s                 # positive = stopped on the side the sound came from (short of the picture)
  if (!(id in seen)) { seen[id] = 1; ids[++nid] = id }
  k = id SUBSEP c
  n[k]++; sStop[k] += stop; sAbs[k] += abs(err); sDur[k] += dur; sErr[k] += err; sErr2[k] += err * err
  src = FILENAME; sub(/.*\//, "", src)
  printf "%s,%s,%d,%s,%s,%s,%s,%s,%.3f,%.3f,%.3f,%.3f,%d,%s,%s\n", id, $6, c, $7, $8, $9, $10, $11, abs(err), stop, abs(start - vis), abs(vis), (vis > 0), $12, src > trials
}
END {
  split("stopshort abs signed_mean sd_error dur", nm, " ")
  hdr = "participant"
  for (m = 1; m <= 5; m++) for (c = 1; c <= 3; c++) hdr = hdr "," nm[m] "_" c
  hdr = hdr ",n_1,n_2,n_3"
  print hdr > person
  bad = 0
  for (i = 1; i <= nid; i++) {
    id = ids[i]; line = id
    for (c = 1; c <= 3; c++) { k = id SUBSEP c; if (n[k] != 6) bad = 1 }
    for (m = 1; m <= 5; m++) for (c = 1; c <= 3; c++) {
      k = id SUBSEP c; nn = n[k]
      if (nn == 0) { line = line ","; continue }
      if (m == 1) v = sStop[k] / nn
      else if (m == 2) v = sAbs[k] / nn
      else if (m == 3) v = sErr[k] / nn
      else if (m == 4) { if (nn < 2) { line = line ","; continue } ; var = (sErr2[k] - sErr[k] * sErr[k] / nn) / (nn - 1); v = sqrt(var < 0 ? 0 : var) }
      else v = sDur[k] / nn
      line = line sprintf(",%.3f", v)
    }
    for (c = 1; c <= 3; c++) line = line "," (n[id SUBSEP c] + 0)
    print line > person
  }
  printf "\nParticipants: %d\n", nid
  for (i = 1; i <= nid; i++) {
    id = ids[i]
    printf "  %s: quiet %d, noisy %d, reverb %d trials\n", id, n[id SUBSEP 1] + 0, n[id SUBSEP 2] + 0, n[id SUBSEP 3] + 0
  }
  if (bad) print "WARNING: every participant should have 6 trials per condition. Check cancelled or repeated sessions." > "/dev/stderr"
  else print "Check passed: 6 trials per condition for everyone."
}' "${files[@]}"

echo
echo "Written:"
echo "  $out/trials.csv"
echo "  $out/per_person.csv"
