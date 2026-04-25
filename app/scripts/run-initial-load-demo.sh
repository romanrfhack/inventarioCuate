#!/usr/bin/env bash
set -euo pipefail

API_URL="${API_URL:-http://localhost:5098}"
USER_NAME="${USER_NAME:-admin.demo}"
PASSWORD="${PASSWORD:-Demo123!}"
CSV_PATH="${1:-/root/projects/refaccionaria-cuate/data/templates/inventario_inicial_template.csv}"

if [[ ! -f "$CSV_PATH" ]]; then
  echo "CSV no encontrado: $CSV_PATH" >&2
  exit 1
fi

LOGIN_RESPONSE=$(curl -sS -X POST "$API_URL/api/auth/login" \
  -H 'Content-Type: application/json' \
  -d "{\"userName\":\"$USER_NAME\",\"password\":\"$PASSWORD\"}")

TOKEN=$(echo "$LOGIN_RESPONSE" | python3 -c 'import sys,json; print(json.load(sys.stdin).get("accessToken",""))')
if [[ -z "$TOKEN" ]]; then
  echo "No se pudo obtener token" >&2
  echo "$LOGIN_RESPONSE" >&2
  exit 1
fi

CSV_JSON=$(python3 - <<PY
import json, pathlib
print(json.dumps(pathlib.Path("$CSV_PATH").read_text(encoding="utf-8")))
PY
)

PREVIEW_RESPONSE=$(curl -sS -X POST "$API_URL/api/initial-load/preview" \
  -H "Authorization: Bearer $TOKEN" \
  -H 'Content-Type: application/json' \
  -d "{\"fileName\":\"$(basename "$CSV_PATH")\",\"csvContent\":$CSV_JSON}")

LOAD_ID=$(echo "$PREVIEW_RESPONSE" | python3 -c 'import sys,json; print(json.load(sys.stdin).get("loadId",""))')
CONFIRMATION_TOKEN=$(echo "$PREVIEW_RESPONSE" | python3 -c 'import sys,json; print(json.load(sys.stdin).get("confirmationToken",""))')
STATUS=$(echo "$PREVIEW_RESPONSE" | python3 -c 'import sys,json; print(json.load(sys.stdin).get("status",""))')

if [[ -z "$LOAD_ID" || -z "$CONFIRMATION_TOKEN" ]]; then
  echo "Preview inválido" >&2
  echo "$PREVIEW_RESPONSE" >&2
  exit 1
fi

if [[ "$STATUS" != "previewed" ]]; then
  echo "Preview no aplicable automáticamente. Estado: $STATUS" >&2
  echo "$PREVIEW_RESPONSE"
  exit 2
fi

APPLY_RESPONSE=$(curl -sS -X POST "$API_URL/api/initial-load/apply/$LOAD_ID" \
  -H "Authorization: Bearer $TOKEN" \
  -H 'Content-Type: application/json' \
  -d "{\"confirmationToken\":\"$CONFIRMATION_TOKEN\"}")

printf 'Preview:\n%s\n\nApply:\n%s\n' "$PREVIEW_RESPONSE" "$APPLY_RESPONSE"
