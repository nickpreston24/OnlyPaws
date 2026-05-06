#!/usr/bin/env bash
set -euo pipefail

SCRIPTDIR="$HOME/scripts/private"
# source all .sh libs (only function defs; they must not execute on source)
for f in "$SCRIPTDIR"/*.sh; do
  # optional: skip non-readable
  [[ -r $f ]] || continue
  # shellcheck source=/dev/null
  source "$f"
done

# Build a list of library functions by prefix (adjust prefix as you use)
mapfile -t FUNCS < <(declare -F | awk '{print $3}' | grep '^chk_' | sort)

PS3="Choose action: "
select fn in "${FUNCS[@]}" "Quit"; do
  case "$fn" in
    Quit) exit 0 ;;
    '')
      echo "Invalid choice";;
    *)
      read -rp "Args (space-separated): " args
      # run the function in a subshell to avoid set -e killing index if func fails
      ( "$fn" $args )
      echo "Press enter to continue"; read -r _
      ;;
  esac
done
