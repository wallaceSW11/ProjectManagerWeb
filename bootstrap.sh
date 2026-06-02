#!/bin/bash
# PMW - Project Manager Web - Bootstrap de instalação
# Uso: curl -sL https://raw.githubusercontent.com/wallaceSW11/ProjectManagerWeb/main/bootstrap.sh | bash
# Ou: wget -qO- https://raw.githubusercontent.com/wallaceSW11/ProjectManagerWeb/main/bootstrap.sh | bash

set -e

REPO="wallaceSW11/ProjectManagerWeb"
PMW_DIR="/opt/pmw"
PMW_TOOLS="/opt/pmw-tools"

echo "========================================"
echo "  PMW - Project Manager Web"
echo "  Instalação automatizada"
echo "========================================"
echo ""

# ------------------------------------------------------------------
# 1. Detecta sistema operacional
# ------------------------------------------------------------------
OS="$(uname -s)"
ARCH="$(uname -m)"

if [ "$OS" != "Linux" ]; then
  echo "❌ Este script é apenas para Linux."
  echo "   Windows: use Atualizar_PMW.ps1"
  exit 1
fi

echo "✅ Sistema: $OS ($ARCH)"

# ------------------------------------------------------------------
# 2. Instala dependências
# ------------------------------------------------------------------
echo ""
echo "📦 Verificando dependências..."

HAS_CURL=$(command -v curl >/dev/null 2>&1 && echo "ok" || echo "")
HAS_UNZIP=$(command -v unzip >/dev/null 2>&1 && echo "ok" || echo "")
HAS_GIT=$(command -v git >/dev/null 2>&1 && echo "ok" || echo "")

TO_INSTALL=""

if [ -z "$HAS_CURL" ]; then TO_INSTALL="$TO_INSTALL curl"; fi
if [ -z "$HAS_UNZIP" ]; then TO_INSTALL="$TO_INSTALL unzip"; fi
if [ -z "$HAS_GIT" ]; then TO_INSTALL="$TO_INSTALL git"; fi

if [ -n "$TO_INSTALL" ]; then
  echo "   Instalando:$TO_INSTALL ..."
  sudo apt update -qq
  sudo apt install -y -qq $TO_INSTALL
fi

echo "✅ Dependências ok"

# ------------------------------------------------------------------
# 3. Obtém URL do último release
# ------------------------------------------------------------------
echo ""
echo "📡 Buscando último release do PMW..."

API_URL="https://api.github.com/repos/$REPO/releases/latest"
echo "   $API_URL"

LATEST_JSON=$(curl -sL "$API_URL")
LATEST_URL=$(echo "$LATEST_JSON" | grep "browser_download_url.*Linux" | head -1 | cut -d '"' -f 4)
LATEST_TAG=$(echo "$LATEST_JSON" | grep '"tag_name"' | head -1 | cut -d '"' -f 4)

if [ -z "$LATEST_URL" ]; then
  echo "❌ Não foi possível encontrar o último release."
  echo "   Verifique em: https://github.com/$REPO/releases"
  exit 1
fi

echo "✅ Release encontrado: $LATEST_TAG"

# ------------------------------------------------------------------
# 4. Baixa e extrai
# ------------------------------------------------------------------
echo ""
echo "📥 Baixando $LATEST_TAG ..."

TMP_DIR=$(mktemp -d)
curl -sL "$LATEST_URL" -o "$TMP_DIR/pmw.zip"

echo "📂 Extraindo para $PMW_DIR ..."
sudo mkdir -p "$PMW_DIR"
sudo unzip -o "$TMP_DIR/pmw.zip" -d "$PMW_DIR" >/dev/null
sudo chown -R "$USER:$USER" "$PMW_DIR"

rm -rf "$TMP_DIR"

# ------------------------------------------------------------------
# 5. Executa o install (configura infra, service, permissões)
# ------------------------------------------------------------------
echo ""
echo "🔧 Configurando infraestrutura..."

cd "$PMW_DIR"

# Se não tiver a pasta infra no zip (release antigo), sai com instruções manuais
if [ ! -f "$PMW_DIR/infra/pmw.sh" ]; then
  echo "⚠️  Release antigo detectado. Configuração manual necessária:"
  echo "   1. Crie $PMW_TOOLS com os scripts de infra"
  echo "   2. Instale o systemd service manualmente"
  exit 0
fi

chmod +x "$PMW_DIR/infra/pmw.sh"
"$PMW_DIR/infra/pmw.sh" install

# ------------------------------------------------------------------
# 6. Ajusta permissões finais
# ------------------------------------------------------------------
chmod -R +x "$PMW_DIR/backend" 2>/dev/null
chmod +x "$PMW_DIR/backend/ProjectManagerWeb" 2>/dev/null
chown -R "$USER:$USER" "$PMW_DIR"

# ------------------------------------------------------------------
# 7. Mensagem final
# ------------------------------------------------------------------
echo ""
echo "========================================"
echo "  ✅ PMW instalado com sucesso!"
echo "========================================"
echo ""
echo "   📍 Aplicação:  $PMW_DIR"
echo "   📍 Scripts:    $PMW_TOOLS"
echo "   🔗 Comando:    pmw"
echo ""
echo "   Para iniciar:"
echo "     pmw start"
echo ""
echo "   Acesse: http://localhost:2025"
echo ""
echo "   Para ver os logs:"
echo "     pmw logs"
echo ""
echo "   Para atualizar:"
echo "     pmw update"
echo ""
echo "========================================"
