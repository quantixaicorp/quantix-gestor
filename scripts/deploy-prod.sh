#!/bin/bash
set -e

GH_TOKEN="${GESTORAI_GH_TOKEN:?Defina GESTORAI_GH_TOKEN com seu GitHub PAT}"
WEBHOOK_TOKEN="${GESTORAI_WEBHOOK_TOKEN:?Defina GESTORAI_WEBHOOK_TOKEN com o token do webhook}"
REPO="quantixaicorp/quantix-gestor"
WEBHOOK_URL="https://gestor.quantixai.cloud/internal/deploy-hook"

echo "Buscando último CI aprovado em main..."

SHA=$(curl -sf -H "Authorization: token $GH_TOKEN" \
  "https://api.github.com/repos/$REPO/actions/runs?branch=main&status=success&per_page=1" \
  | python3 -c "
import sys, json
runs = json.load(sys.stdin).get('workflow_runs', [])
if not runs:
    print('', end='')
else:
    print(runs[0]['head_sha'], end='')
")

if [ -z "$SHA" ]; then
  echo "Nenhum CI aprovado encontrado em main. Verifique o GitHub Actions."
  exit 1
fi

echo "SHA: $SHA"
echo "Iniciando deploy em produção..."

RESULT=$(curl -sf --max-time 180 \
  -X POST "$WEBHOOK_URL" \
  -H "X-Deploy-Token: $WEBHOOK_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"sha\":\"$SHA\"}")

STATUS=$(echo "$RESULT" | python3 -c "import sys,json; print(json.load(sys.stdin).get('status',''))" 2>/dev/null)

if [ "$STATUS" = "ok" ]; then
  echo "Deploy concluído com sucesso!"
else
  echo "Erro no deploy:"
  echo "$RESULT" | python3 -c "import sys,json; print(json.load(sys.stdin).get('output','')[-500:])" 2>/dev/null
  exit 1
fi
