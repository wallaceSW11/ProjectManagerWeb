# Infraestrutura PMW — Scripts de instalação e atualização

## Pontos críticos

Estes scripts são a porta de entrada do PMW. Um erro aqui impede instalação e atualização em **todas** as máquinas. Revise com atenção redobrada.

## Estrutura de diretórios

```
/opt/pmw/                  ← binários da aplicação (Linux)
/opt/pmw-tools/            ← scripts de infra (pmw.sh, pmw.service)
/opt/pmw-bkps/             ← backups de atualização
  └── YYYYMMDD_HHMMSS/
      ├── app/             ← backup dos binários
      └── banco/           ← backup dos dados JSON

C:\inetpub\wwwroot\
  ├── PMW\                 ← binários (Windows)
  ├── PMW-Tools\           ← scripts de infra
  └── PMW-Bkps\            ← backups
```

## Arquivos

| Arquivo | SO | Função | Chamado por |
|---------|-----|--------|-------------|
| `bootstrap.sh` | Linux | Instalação do zero (`curl pipe bash`) | Usuário |
| `bootstrap.ps1` | Windows | Instalação do zero | Usuário |
| `pmw.sh` | Linux | Gerenciamento (`start/stop/restart/status/logs/install/update`) | `bootstrap.sh`, comando `pmw` |
| `pmw.ps1` | Windows | Gerenciamento (`start/stop/restart/status/install/update`) | `bootstrap.ps1`, comando `pmw` |
| `pmw.service` | Linux | Systemd user service | `pmw.sh install` |
| `Atualizar_PMW.ps1` | Windows | Atualização legada | Usuário (script independente) |
| `pmw-start.vbs` | Windows | Inicia sem console visível | Atalho/autostart |
| `pmw-start.bat` | Windows | Atalho para o .vbs | Usuário |

## Fluxo de instalação (Linux)

```
curl bootstrap.sh | bash
  → baixa último release do GitHub
  → extrai em /opt/pmw
  → chama pmw.sh install
      → sudo mkdir /opt/pmw, /opt/pmw-tools, /opt/pmw-bkps
      → copia pmw.sh e pmw.service para /opt/pmw-tools
      → sudo ln -s /opt/pmw-tools/pmw.sh → /usr/local/bin/pmw
      → systemctl --user enable pmw
  → pmw start
```

## Fluxo de atualização (Linux)

```
pmw update
  → systemctl --user stop pmw
  → backup: PMW-Bkps/YYYYMMDD_HHMMSS/{app,banco}
  → curl último release → unzip em /opt/pmw
  → copia novos scripts de infra para /opt/pmw-tools
  → systemctl --user start pmw
```

## Checklist de revisão

Ao modificar qualquer script de infra, verifique:

- [ ] `bash -n bootstrap.sh` e `bash -n infra/pmw.sh` passam sem erros
- [ ] `pwsh -NoProfile -Command "& { ...Parser::ParseFile... }"` nos `.ps1`
- [ ] Testar `bootstrap.sh` em Linux limpo (VM ou container)
- [ ] Testar `pmw update` em Linux com instalação existente
- [ ] Testar `bootstrap.ps1` em Windows limpo
- [ ] Testar `pmw.ps1 update` em Windows com instalação existente
- [ ] Caminhos de backup (`PMW-Bkps`) são criados corretamente
- [ ] Backup do banco (`%APPDATA%/PMW/Banco` ou `~/.config/PMW/Banco`) funciona
- [ ] O serviço reinicia corretamente após update
- [ ] A versão exibida no app confere com a tag do release

## CI

Os workflows `ci.yml` (PR) e `release.yml` (push na main) validam sintaxe de todos os `.sh` e `.ps1` antes do build. Um PR com script quebrado **não passa** no CI.
