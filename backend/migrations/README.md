# Migrations

Scripts de migração para atualizar a estrutura de dados do banco JSON.

## Como executar

Execute o script PowerShell desejado:

```powershell
cd backend/migrations
.\AddAtivoToMenus.ps1
```

## Migrations disponíveis

### AddAtivoToMenus.ps1
**Data:** 2026-01-09  
**Descrição:** Adiciona o campo `ativo: true` em todos os menus existentes nos repositórios.

**O que faz:**
- Cria backup automático do arquivo antes de modificar
- Adiciona o campo `ativo` com valor `true` em todos os menus
- Mantém a estrutura JSON formatada

**Arquivo afetado:** `%ProgramData%\PMW\Banco\repositorios.json`

## Segurança

Todos os scripts de migration:
- Criam backup automático antes de modificar
- Validam a existência do arquivo
- Tratam erros adequadamente
- Mantêm encoding UTF-8
