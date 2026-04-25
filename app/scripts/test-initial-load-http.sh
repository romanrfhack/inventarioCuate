#!/usr/bin/env bash
set -euo pipefail

API_URL="${API_URL:-http://localhost:5098}"
USER_NAME="${USER_NAME:-admin.demo}"
PASSWORD="${PASSWORD:-Demo123!}"
TMP_CSV=$(mktemp)

cat > "$TMP_CSV" <<'CSV'
codigo,descripcion,marca,proveedor,costo,precio_venta,existencia_inicial,unidad,ubicacion,observaciones
SL42-001,Producto HTTP 1,Marca HTTP,Proveedor HTTP,10.00,15.00,4,pieza,H1,ok
,Producto HTTP 2,Marca HTTP,,8.00,,2,pieza,H2,warning
CSV

cleanup() {
  rm -f "$TMP_CSV"
}
trap cleanup EXIT

LOGIN_RESPONSE=$(curl -sS -X POST "$API_URL/api/auth/login" \
  -H 'Content-Type: application/json' \
  -d "{\"userName\":\"$USER_NAME\",\"password\":\"$PASSWORD\"}")
TOKEN=$(echo "$LOGIN_RESPONSE" | python3 -c 'import sys,json; print(json.load(sys.stdin).get("accessToken",""))')

if [[ -z "$TOKEN" ]]; then
  echo "Login falló" >&2
  echo "$LOGIN_RESPONSE" >&2
  exit 1
fi

CSV_JSON=$(python3 - <<PY
import json, pathlib
print(json.dumps(pathlib.Path("$TMP_CSV").read_text(encoding="utf-8")))
PY
)

PREVIEW_RESPONSE=$(curl -sS -X POST "$API_URL/api/initial-load/preview" \
  -H "Authorization: Bearer $TOKEN" \
  -H 'Content-Type: application/json' \
  -d "{\"fileName\":\"slice-4-2-http.csv\",\"csvContent\":$CSV_JSON}")

LOAD_ID=$(echo "$PREVIEW_RESPONSE" | python3 -c 'import sys,json; print(json.load(sys.stdin).get("loadId",""))')
CONFIRMATION_TOKEN=$(echo "$PREVIEW_RESPONSE" | python3 -c 'import sys,json; print(json.load(sys.stdin).get("confirmationToken",""))')
STATUS=$(echo "$PREVIEW_RESPONSE" | python3 -c 'import sys,json; print(json.load(sys.stdin).get("status",""))')

if [[ "$STATUS" != "previewed" ]]; then
  echo "Preview no quedó en estado previewed" >&2
  echo "$PREVIEW_RESPONSE" >&2
  exit 2
fi

APPLY_RESPONSE=$(curl -sS -X POST "$API_URL/api/initial-load/apply/$LOAD_ID" \
  -H "Authorization: Bearer $TOKEN" \
  -H 'Content-Type: application/json' \
  -d "{\"confirmationToken\":\"$CONFIRMATION_TOKEN\"}")

LOADS_RESPONSE=$(curl -sS -X GET "$API_URL/api/initial-load" \
  -H "Authorization: Bearer $TOKEN")

DETAIL_RESPONSE=$(curl -sS -X GET "$API_URL/api/initial-load/$LOAD_ID" \
  -H "Authorization: Bearer $TOKEN")

python3 - <<PY
import json
preview = json.loads('''$PREVIEW_RESPONSE''')
apply = json.loads('''$APPLY_RESPONSE''')
loads = json.loads('''$LOADS_RESPONSE''')
detail = json.loads('''$DETAIL_RESPONSE''')
assert preview['status'] == 'previewed'
assert apply['status'] == 'applied'
assert apply['createdInventoryBalances'] >= 1
assert apply['createdMovements'] >= 1
assert any(x['loadId'] == preview['loadId'] for x in loads)
assert detail['status'] == 'applied'
print('HTTP end-to-end OK')
PY

printf 'Preview:\n%s\n\nApply:\n%s\n\nLoads:\n%s\n\nDetail:\n%s\n' "$PREVIEW_RESPONSE" "$APPLY_RESPONSE" "$LOADS_RESPONSE" "$DETAIL_RESPONSE"
