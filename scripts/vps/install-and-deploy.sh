#!/bin/bash
# Instala webhook de deploy do GestorAI na VPS e faz o primeiro deploy
# Cole este script no console da Hostinger (hPanel → VPS → Console)
set -e

GH_TOKEN="${GESTORAI_GH_TOKEN:?Defina GESTORAI_GH_TOKEN com seu GitHub PAT}"
REPO="quantixaicorp/quantix-gestor"
DEPLOY_DIR="/opt/gestorai-deploy"
WEBHOOK_TOKEN="c84a12e9f3b507d6e1a28c4f90d37b58e2461785a3c9d012b6f48e7059a1c3d4"
WEBHOOK_PORT=9989

echo "=== GestorAI Deploy Setup ==="

# ── Detecta caminhos automaticamente ──────────────────────────────────────────
echo "Detectando configuração da VPS..."

# Serviço systemd
SERVICE_NAME=$(systemctl list-units --type=service --state=running 2>/dev/null \
  | grep -iE "gestorai|gestor-ai|gestor_ai" | awk '{print $1}' | sed 's/\.service//' | head -1)
if [ -z "$SERVICE_NAME" ]; then
  SERVICE_NAME=$(systemctl list-unit-files --type=service 2>/dev/null \
    | grep -iE "gestorai|gestor-ai|gestor_ai" | awk '{print $1}' | sed 's/\.service//' | head -1)
fi
[ -z "$SERVICE_NAME" ] && { echo "ERRO: serviço gestorai não encontrado. Verifique com: systemctl list-units | grep gestor"; exit 1; }
echo "  Serviço: $SERVICE_NAME"

# Caminho do binário (ExecStart)
EXEC_START=$(systemctl show "$SERVICE_NAME" --property=ExecStart 2>/dev/null \
  | grep -oP 'path=\K[^ ;]+' | head -1)
BACKEND_PATH=$(dirname "$EXEC_START")
[ -z "$BACKEND_PATH" ] && BACKEND_PATH="/var/www/gestorai-api"
echo "  Backend: $BACKEND_PATH"

# Arquivo de env (EnvironmentFile do serviço)
ENV_FILE=$(systemctl show "$SERVICE_NAME" --property=EnvironmentFiles 2>/dev/null \
  | grep -oP '/[^ ]+' | head -1)
[ -z "$ENV_FILE" ] && ENV_FILE=$(systemctl show "$SERVICE_NAME" --property=EnvironmentFile 2>/dev/null \
  | grep -oP '/[^ ]+' | head -1)
[ -z "$ENV_FILE" ] && ENV_FILE="/etc/gestorai/gestorai.env"
echo "  Env file: $ENV_FILE"

# Frontend (nginx root para gestor.quantixai.cloud)
FRONTEND_PATH=$(grep -r "gestor.quantixai.cloud" /etc/nginx/ 2>/dev/null \
  | grep -v "server_name\|proxy_pass\|ssl\|#" \
  | grep -oP 'root \K[^;]+' | head -1)
[ -z "$FRONTEND_PATH" ] && FRONTEND_PATH=$(grep -rA5 "gestor.quantixai.cloud" /etc/nginx/ 2>/dev/null \
  | grep "root " | grep -oP 'root \K[^;]+' | head -1)
[ -z "$FRONTEND_PATH" ] && FRONTEND_PATH="/var/www/gestorai-frontend"
echo "  Frontend: $FRONTEND_PATH"

# ── Cria o deploy.sh ──────────────────────────────────────────────────────────
mkdir -p "$DEPLOY_DIR"

cat > "$DEPLOY_DIR/deploy.sh" << DEPLOY_SCRIPT
#!/bin/bash
set -e
GH_TOKEN="$GH_TOKEN"
REPO="$REPO"
BACKEND_PATH="$BACKEND_PATH"
FRONTEND_PATH="$FRONTEND_PATH"
ENV_FILE="$ENV_FILE"
SERVICE_NAME="$SERVICE_NAME"
SHA="\$1"
[ -z "\$SHA" ] && { echo "Uso: \$0 <sha>"; exit 1; }
echo "[deploy] SHA: \$SHA"
WORK_DIR=\$(mktemp -d)
trap 'rm -rf "\$WORK_DIR"' EXIT
apt-get install -y unzip rsync > /dev/null 2>&1 || true
ARTIFACT_ID=\$(curl -sf -H "Authorization: token \$GH_TOKEN" \\
  "https://api.github.com/repos/\$REPO/actions/artifacts?per_page=20" \\
  | python3 -c "
import sys,json
arts=json.load(sys.stdin).get('artifacts',[])
sha='\$SHA'[:8]
arts=[a for a in arts if not a.get('expired') and a['name'].startswith('app-'+sha)]
print(arts[0]['id'] if arts else '',end='')
")
[ -z "\$ARTIFACT_ID" ] && { echo "[deploy] Artifact nao encontrado para \$SHA"; exit 1; }
echo "[deploy] Baixando artifact \$ARTIFACT_ID..."
curl -sfL -H "Authorization: token \$GH_TOKEN" \\
  "https://api.github.com/repos/\$REPO/actions/artifacts/\$ARTIFACT_ID/zip" \\
  -o "\$WORK_DIR/app.zip"
unzip -q "\$WORK_DIR/app.zip" -d "\$WORK_DIR/app"
echo "[deploy] Rodando migrations..."
chmod +x "\$WORK_DIR/app/migrate"
set -a && . "\$ENV_FILE" && set +a
"\$WORK_DIR/app/migrate"
echo "[deploy] Publicando backend..."
mkdir -p "\$BACKEND_PATH"
rsync -a --delete "\$WORK_DIR/app/backend/" "\$BACKEND_PATH/"
chown -R www-data:www-data "\$BACKEND_PATH" 2>/dev/null || true
echo "[deploy] Publicando frontend..."
mkdir -p "\$FRONTEND_PATH"
rsync -a --delete "\$WORK_DIR/app/frontend/" "\$FRONTEND_PATH/"
chown -R www-data:www-data "\$FRONTEND_PATH" 2>/dev/null || true
echo "[deploy] Reiniciando \$SERVICE_NAME..."
systemctl restart "\$SERVICE_NAME"
sleep 4
systemctl is-active --quiet "\$SERVICE_NAME" && echo "[deploy] Servico ativo OK" || { echo "[deploy] ERRO servico nao subiu"; journalctl -u "\$SERVICE_NAME" -n 20 --no-pager; exit 1; }
echo "[deploy] Deploy concluido com sucesso!"
DEPLOY_SCRIPT
chmod +x "$DEPLOY_DIR/deploy.sh"

# ── Cria o webhook.py ─────────────────────────────────────────────────────────
cat > "$DEPLOY_DIR/webhook.py" << 'WEBHOOK_PY'
#!/usr/bin/env python3
import http.server, json, os, subprocess, threading
TOKEN  = "c84a12e9f3b507d6e1a28c4f90d37b58e2461785a3c9d012b6f48e7059a1c3d4"
DEPLOY = "/opt/gestorai-deploy/deploy.sh"
PORT   = 9989
lock   = threading.Lock()
class Handler(http.server.BaseHTTPRequestHandler):
    def log_message(self, *_): pass
    def do_POST(self):
        if self.path != "/deploy":
            self.send_response(404); self.end_headers(); return
        if self.headers.get("X-Deploy-Token") != TOKEN:
            self.send_response(403); self.end_headers(); return
        length = int(self.headers.get("Content-Length", 0))
        body = json.loads(self.rfile.read(length) or b"{}")
        sha = body.get("sha", "").strip()
        if not sha:
            self.send_response(400); self.send_header("Content-Type","application/json"); self.end_headers()
            self.wfile.write(b'{"status":"error","output":"sha ausente"}'); return
        if not lock.acquire(blocking=False):
            self.send_response(409); self.send_header("Content-Type","application/json"); self.end_headers()
            self.wfile.write(b'{"status":"error","output":"deploy em andamento"}'); return
        try:
            result = subprocess.run(["bash", DEPLOY, sha], capture_output=True, text=True, timeout=300)
            output = result.stdout + result.stderr
            ok = result.returncode == 0
            self.send_response(200); self.send_header("Content-Type","application/json"); self.end_headers()
            self.wfile.write(json.dumps({"status":"ok" if ok else "error","output":output[-2000:]}).encode())
        except subprocess.TimeoutExpired:
            self.send_response(200); self.send_header("Content-Type","application/json"); self.end_headers()
            self.wfile.write(b'{"status":"error","output":"timeout"}')
        finally:
            lock.release()
if __name__ == "__main__":
    srv = http.server.HTTPServer(("127.0.0.1", PORT), Handler)
    print(f"gestorai-webhook listening on 127.0.0.1:{PORT}")
    srv.serve_forever()
WEBHOOK_PY

# ── Systemd service ───────────────────────────────────────────────────────────
cat > /etc/systemd/system/gestorai-webhook.service << EOF
[Unit]
Description=GestorAI Deploy Webhook
After=network.target

[Service]
ExecStart=/usr/bin/python3 $DEPLOY_DIR/webhook.py
Restart=always
User=root

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload
systemctl enable --now gestorai-webhook
echo "Webhook service: $(systemctl is-active gestorai-webhook)"

# ── Nginx location ────────────────────────────────────────────────────────────
NGINX_CONF=$(grep -rl "gestor.quantixai.cloud" /etc/nginx/ 2>/dev/null | head -1)
if [ -n "$NGINX_CONF" ] && ! grep -q "deploy-hook" "$NGINX_CONF"; then
  sed -i '/server_name.*gestor.quantixai.cloud/a \    location /internal/deploy-hook {\n        proxy_pass http://127.0.0.1:9989/deploy;\n        proxy_read_timeout 300;\n    }' "$NGINX_CONF"
  nginx -t && nginx -s reload && echo "Nginx atualizado OK"
fi

# ── Primeiro deploy ───────────────────────────────────────────────────────────
SHA=$(curl -sf -H "Authorization: token $GH_TOKEN" \
  "https://api.github.com/repos/$REPO/actions/runs?branch=main&status=success&per_page=1" \
  | python3 -c "
import sys, json
runs = json.load(sys.stdin).get('workflow_runs', [])
print(runs[0]['head_sha'] if runs else '', end='')
")
echo "=== Iniciando deploy SHA: $SHA ==="
bash "$DEPLOY_DIR/deploy.sh" "$SHA"
