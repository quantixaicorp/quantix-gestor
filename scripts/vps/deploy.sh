#!/bin/bash
set -e

GH_TOKEN="${GESTORAI_GH_TOKEN:?}"
REPO="quantixaicorp/quantix-gestor"
BACKEND_PATH="/var/www/gestorai-api"
FRONTEND_PATH="/var/www/gestorai-frontend"
ENV_FILE="/etc/gestorai/gestorai.env"
SERVICE_NAME="gestorai"
SHA="$1"

if [ -z "$SHA" ]; then
  echo "Uso: $0 <sha>"
  exit 1
fi

echo "[gestorai-deploy] SHA: $SHA"

ARTIFACT_NAME="app-$SHA"
WORK_DIR=$(mktemp -d)
trap 'rm -rf "$WORK_DIR"' EXIT

echo "[gestorai-deploy] Buscando artifact $ARTIFACT_NAME..."
ARTIFACT_ID=$(curl -sf \
  -H "Authorization: token $GH_TOKEN" \
  "https://api.github.com/repos/$REPO/actions/artifacts?name=$ARTIFACT_NAME&per_page=5" \
  | python3 -c "
import sys, json
arts = json.load(sys.stdin).get('artifacts', [])
arts = [a for a in arts if not a.get('expired', False)]
print(arts[0]['id'] if arts else '', end='')
")

if [ -z "$ARTIFACT_ID" ]; then
  echo "[gestorai-deploy] Artifact não encontrado ou expirado."
  exit 1
fi

echo "[gestorai-deploy] Baixando artifact $ARTIFACT_ID..."
curl -sfL \
  -H "Authorization: token $GH_TOKEN" \
  "https://api.github.com/repos/$REPO/actions/artifacts/$ARTIFACT_ID/zip" \
  -o "$WORK_DIR/app.zip"

apt-get install -y unzip > /dev/null 2>&1 || true
unzip -q "$WORK_DIR/app.zip" -d "$WORK_DIR/app"

echo "[gestorai-deploy] Rodando migrations..."
chmod +x "$WORK_DIR/app/migrate"
set -a && . "$ENV_FILE" && set +a
"$WORK_DIR/app/migrate"

echo "[gestorai-deploy] Publicando backend..."
mkdir -p "$BACKEND_PATH"
rsync -a --delete --exclude 'wwwroot/logos/' --exclude 'wwwroot/uploads/' "$WORK_DIR/app/backend/" "$BACKEND_PATH/"
chown -R www-data:www-data "$BACKEND_PATH"

echo "[gestorai-deploy] Publicando frontend..."
mkdir -p "$FRONTEND_PATH"
rsync -a --delete "$WORK_DIR/app/frontend/" "$FRONTEND_PATH/"
chown -R www-data:www-data "$FRONTEND_PATH"

echo "[gestorai-deploy] Reiniciando serviço..."
systemctl restart "$SERVICE_NAME"
sleep 3
systemctl is-active --quiet "$SERVICE_NAME" && echo "[gestorai-deploy] Serviço ativo." || { echo "[gestorai-deploy] ERRO: serviço não subiu!"; exit 1; }

echo "[gestorai-deploy] Deploy concluído."
