#!/bin/bash
# PMW - Project Manager Web - Script de gerenciamento
# Diretórios:
#   /opt/pmw          → aplicação (backup + frontend + dlls)
#   /opt/pmw-tools    → scripts de infra (pmw.sh, pmw.service)
#   O script em /opt/pmw-tools/pmw.sh gerencia a aplicação em /opt/pmw

PMW_DIR="/opt/pmw"
PMW_TOOLS="/opt/pmw-tools"
SERVICE_NAME="pmw"

case "$1" in
  start)
    systemctl --user start $SERVICE_NAME
    echo "PMW iniciado. Acesse http://localhost:2025"
    ;;

  stop)
    systemctl --user stop $SERVICE_NAME
    echo "PMW parado."
    ;;

  restart)
    systemctl --user restart $SERVICE_NAME
    echo "PMW reiniciado."
    ;;

  status)
    systemctl --user status $SERVICE_NAME
    ;;

  logs)
    journalctl --user -u $SERVICE_NAME -f
    ;;

  install)
    echo "Instalando PMW..."

    # Cria diretório da aplicação
    sudo mkdir -p "$PMW_DIR"
    sudo chown "$USER:$USER" "$PMW_DIR"

    # Cria diretório de tools (scripts de infra)
    sudo mkdir -p "$PMW_TOOLS"
    sudo chown "$USER:$USER" "$PMW_TOOLS"

    # Copia scripts de infra para PMW_TOOLS
    SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
    cp "$SCRIPT_DIR/pmw.service" "$PMW_TOOLS/"
    cp "$SCRIPT_DIR/pmw.sh" "$PMW_TOOLS/"

    # Ajusta permissões dos scripts
    chmod +x "$PMW_TOOLS/pmw.sh"

    # Cria link simbólico para o comando pmw (se não existir)
    if [ ! -L "/usr/local/bin/pmw" ]; then
      sudo ln -s "$PMW_TOOLS/pmw.sh" /usr/local/bin/pmw
    fi

    # Instala o systemd service
    mkdir -p ~/.config/systemd/user/
    cp "$PMW_TOOLS/pmw.service" ~/.config/systemd/user/
    systemctl --user daemon-reload
    systemctl --user enable $SERVICE_NAME

    echo "PMW instalado em $PMW_DIR"
    echo "Scripts de infra em $PMW_TOOLS"
    echo "Use 'pmw start' para iniciar."
    ;;

  update)
    echo "Atualizando PMW..."

    # Para o serviço
    systemctl --user stop $SERVICE_NAME 2>/dev/null

    # Backup da instalação atual com data e hora
    DATA_HORA=$(date +"%Y%m%d_%H%M%S")
    BACKUP_DIR="/opt/pmw-backup-$DATA_HORA"
    if [ -d "$PMW_DIR" ]; then
      echo "Criando backup em $BACKUP_DIR ..."
      cp -a "$PMW_DIR" "$BACKUP_DIR"
      echo "Backup concluído."
    fi

    # Obtém URL do último release do GitHub
    REPO_URL=$(git -C "$PMW_DIR" remote get-url origin 2>/dev/null | sed 's|.*github.com/||;s|\.git$||')
    if [ -z "$REPO_URL" ]; then
      # Fallback: tenta detectar do próprio script ou usa fixo
      echo "Aviso: não foi possível detectar o repositório git. Usando fallback."
      REPO_URL="PMW/ProjectManagerWeb"
    fi

    LATEST_URL=$(curl -s "https://api.github.com/repos/$REPO_URL/releases/latest" | grep "browser_download_url.*Linux" | cut -d '"' -f 4)

    if [ -z "$LATEST_URL" ]; then
      echo "Erro: não foi possível obter a URL do release."
      exit 1
    fi

    # Baixa e descompacta
    TMP_DIR=$(mktemp -d)
    echo "Baixando release..."
    curl -sL "$LATEST_URL" -o "$TMP_DIR/pmw.zip"

    echo "Descompactando..."
    unzip -o "$TMP_DIR/pmw.zip" -d "$PMW_DIR"

    # Ajusta permissões
    echo "Ajustando permissões..."
    chmod +x "$PMW_DIR/ProjectManagerWeb" 2>/dev/null
    chown -R "$USER:$USER" "$PMW_DIR"

    # Atualiza scripts de infra (se o pacote tiver a pasta infra)
    if [ -d "$PMW_DIR/infra" ]; then
      echo "Atualizando scripts de infra em $PMW_TOOLS ..."
      cp "$PMW_DIR/infra/pmw.service" "$PMW_TOOLS/" 2>/dev/null
      cp "$PMW_DIR/infra/pmw.sh" "$PMW_TOOLS/" 2>/dev/null
      chmod +x "$PMW_TOOLS/pmw.sh"

      # Atualiza o systemd service
      cp "$PMW_TOOLS/pmw.service" ~/.config/systemd/user/
      systemctl --user daemon-reload

      # Remove a pasta infra do diretório da aplicação (já está em tools)
      rm -rf "$PMW_DIR/infra"
    fi

    # Limpa temporários
    rm -rf "$TMP_DIR"

    echo "Atualização concluída."

    # Inicia o serviço
    systemctl --user start $SERVICE_NAME
    echo "PMW reiniciado."
    ;;

  *)
    echo "Uso: pmw {start|stop|restart|status|logs|install|update}"
    exit 1
    ;;

esac
