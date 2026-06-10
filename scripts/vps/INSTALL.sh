#!/bin/bash
# Rodar uma vez na VPS como root para configurar o deploy do GestorAI
set -e

DEPLOY_DIR="/opt/gestorai-deploy"
mkdir -p "$DEPLOY_DIR"

# Copiar scripts (rodar do repositório local ou colar manualmente)
cp deploy.sh    "$DEPLOY_DIR/deploy.sh"
cp webhook.py   "$DEPLOY_DIR/webhook.py"
chmod +x "$DEPLOY_DIR/deploy.sh"

# ── Systemd: webhook ────────────────────────────────────────────────────────
cat > /etc/systemd/system/gestorai-deploy-webhook.service <<'EOF'
[Unit]
Description=GestorAI Deploy Webhook
After=network.target

[Service]
ExecStart=/usr/bin/python3 /opt/gestorai-deploy/webhook.py
Restart=always
User=root

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload
systemctl enable --now gestorai-deploy-webhook

# ── Nginx: proxy /internal/deploy-hook ─────────────────────────────────────
# Adicione ao bloco server de gestor.quantixai.cloud:
# location /internal/deploy-hook {
#     allow 127.0.0.1;
#     deny all;
#     proxy_pass http://127.0.0.1:9989/deploy;
# }
#
# ATENÇÃO: remova "allow 127.0.0.1; deny all;" se o acesso vier de IP externo
# (deploy-prod.sh local). Deixe só o proxy_pass e proteja pelo token no header.

echo "Instalação concluída."
echo "Adicione o bloco location no nginx de gestor.quantixai.cloud e rode: nginx -s reload"
