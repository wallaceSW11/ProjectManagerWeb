# Checklist de Testes - Deploy Automático IIS

## 🧪 Testes Backend

### API Endpoints
- [ ] GET `/api/sites-iis` - Listar sites (deve retornar array vazio inicialmente)
- [ ] POST `/api/sites-iis` - Cadastrar site de exemplo
- [ ] GET `/api/sites-iis/{id}` - Buscar site específico
- [ ] PUT `/api/sites-iis/{id}` - Atualizar site
- [ ] DELETE `/api/sites-iis/{id}` - Excluir site

### Deploy Service
- [ ] Verificar se arquivo JSON é criado em `%ProgramData%/PMW/Banco/sites-iis.json`
- [ ] POST `/api/sites-iis/{id}/atualizar` - Testar geração do script
- [ ] Verificar script gerado em `%ProgramData%/PMW/Banco/deploy-logs/`
- [ ] Verificar se PowerShell abre corretamente

---

## 🎨 Testes Frontend

### Navegação
- [ ] Menu "Gerenciar Sites" aparece e funciona
- [ ] Menu dropdown "Deploy" aparece
- [ ] Rota `/sites-iis` carrega corretamente

### CRUD de Sites
- [ ] Botão "Adicionar Site" abre modal
- [ ] Modal com stepper (3 etapas) funciona
- [ ] **Etapa 1**: Campos Nome e Pasta Raiz validam
- [ ] **Etapa 2**: Adicionar/Editar/Excluir pastas funciona
- [ ] **Etapa 3**: Adicionar/Remover pools funciona
- [ ] Salvar site persiste no backend
- [ ] Tabela exibe sites cadastrados
- [ ] Editar site carrega dados corretamente
- [ ] Excluir site remove da lista

### Deploy
- [ ] Dropdown "Deploy" lista sites cadastrados
- [ ] Clicar em "Atualizar [Site]" abre modal de log
- [ ] Modal de log exibe processo
- [ ] PowerShell abre visualmente
- [ ] Log mostra cores diferentes (info/success/error)
- [ ] Mensagem final aparece (sucesso ou erro)

---

## 🐛 Possíveis Problemas

### Backend
- [ ] Services registrados no Program.cs?
- [ ] Porta 2025 está disponível?
- [ ] Permissões para criar pastas em ProgramData?

### Frontend
- [ ] Importações corretas nos componentes?
- [ ] Store Pinia inicializada?
- [ ] BaseURL do axios configurada?

### PowerShell
- [ ] ExecutionPolicy permite rodar scripts?
- [ ] Comandos appcmd disponíveis (IIS instalado)?
- [ ] Permissões para gerenciar IIS?

---

## 📝 Comandos Úteis

### Verificar arquivo JSON gerado:
```powershell
cat "$env:ProgramData\PMW\Banco\sites-iis.json"
```

### Verificar scripts de deploy:
```powershell
ls "$env:ProgramData\PMW\Banco\deploy-logs\"
```

### Ver último script gerado:
```powershell
cat "$env:ProgramData\PMW\Banco\deploy-logs\deploy_*.ps1" | Select-Object -Last 1
```

---

## 🎯 Fluxo de Teste Completo

1. Subir backend (porta 2025)
2. Subir frontend (dev mode)
3. Acessar http://localhost:5173 (ou porta do vite)
4. Ir em "Gerenciar Sites"
5. Adicionar site de teste:
   - Nome: "Teste_PMW"
   - Pasta Raiz: "C:\inetpub\wwwroot\Teste"
   - Pasta 1:
     - Diretório: "C:\git\teste\frontend"
     - Comando: "echo 'Build simulado'"
     - Origem: "C:\git\teste\frontend\dist"
     - Destino: "C:\inetpub\wwwroot\Teste\app"
     - Nome Pasta: "app"
   - Pools: ["Teste_AppPool"]
6. Salvar e verificar se aparece na tabela
7. Menu "Deploy" → "Atualizar Teste_PMW"
8. Verificar se PowerShell abre
9. Verificar log no modal

---

**Estou pronto para testar! 🚀**
