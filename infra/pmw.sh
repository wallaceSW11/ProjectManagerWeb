#!/bin/bash
# PMW - Project Manager Web - Script de gerenciamento

PMW_DIR="/opt/pmw"
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
    sudo mkdir -p $PMW_DIR
    sudo chown "$USER:$USER" $PMW_DIR
    mkdir -p ~/.config/systemd/user/
    cp "$(dirname "$0")/pmw.service" ~/.config/systemd/user/
    systemctl --user daemon-reload
    systemctl --user enable $SERVICE_NAME
    echo "PMW instalado. Use 'pmw start' para iniciar."
    ;;
  update)
    echo "Atualizando PMW..."
    systemctl --user stop $SERVICE_NAME
    LATEST_URL=$(curl -s https://api.github.com/repos/$(git -C "$PMW_DIR" remote get-url origin 2>/dev/null | sed 's|.*github.com/||;s|\.git$||')/releases/latest | grep "browser_download_url.*Linux" | cut -d '"' -f 4)
    if [ -n "$LATEST_URL" ]; then
      TMP_DIR=$(mktemp -d)
      curl -sL "$LATEST_URL" -o "$TMP_DIR/pmw.zip"
      unzip -o "$TMP_DIR/pmw.zip" -d $PMW_DIR
      rm -rf "$TMP_DIR"
      echo "Atualização concluída."
    else
      echo "Erro: não foi possível obter a URL do release."
      exit 1
    fi
    systemctl --user start $SERVICE_NAME
    echo "PMW reiniciado."
    ;;
  *)
    echo "Uso: pmw {start|stop|restart|status|logs|install|update}"
    exit 1
    ;;
esac
