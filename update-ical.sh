#!/usr/bin/env bash
set -euo pipefail

cd "${0%/*}"

if [ -n "$(git status --porcelain)" ]; then
  echo "Update metadata."
  jq -cn \
    --arg now "$(date -u +"%FT%T.%3NZ")" \
    --arg name "$ICAL_NAME" \
    '
      .updated_at |= $now
      | .source |= "'$ICAL_SOURCE'"
      | .name |= $name
      | .filename |= "ical/'$ICAL_SOURCE'.ics"
    ' > ical/$ICAL_SOURCE.json
fi
