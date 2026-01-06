# 🤖 Instruções para Geração de Descrição de Pull Request

## 📋 Objetivo
Replicar o comportamento do script `generate-pr-description.js` (que usa Gemini AI) de forma manual através da IA (GitHub Copilot), gerando uma descrição completa e profissional da Pull Request em português.

## 🏗️ Contexto do Projeto
Este é um projeto de câmbio (TechCambio) com arquitetura de microserviços contendo:
- **Frontend** (Vue.js + Vuetify)
- **Backend** (Node.js/Express)
- **Authenticator** (serviço de autenticação)
- **Common** (bibliotecas compartilhadas)
- **Documentação**

---

## 🔧 Passo a Passo da IA

### **ETAPA 1: Listar Arquivos Alterados**

Execute o comando:
```bash
git diff --name-only origin/dev...HEAD
```

**O que fazer:**
- Listar TODOS os arquivos alterados
- Contar quantos arquivos foram modificados
- Mostrar a lista para o usuário

**Exemplo de saída esperada:**
```
📁 7 arquivo(s) alterado(s):
  - frontend/src/api/external/federal-revenue/federal-revenue-service.js
  - frontend/src/components/comum/FederalRevenueImportButton.vue
  - frontend/src/components/form-customer/PJ/cover/PJCustomerCover.vue
  ...
```

---

### **ETAPA 2: Categorizar Arquivos**

Categorize os arquivos nas seguintes categorias:

```javascript
const categories = {
  frontend: arquivos que começam com 'frontend/',
  backend: arquivos que começam com 'backend/',
  authenticator: arquivos que começam com 'authenticator/',
  common: arquivos que começam com 'common/',
  docs: arquivos que começam com 'docs/',
  config: arquivos que contêm 'config', 'package.json' ou 'Dockerfile',
  other: todos os demais arquivos
};
```

**Formato de apresentação:**
```
ARQUIVOS ALTERADOS POR CATEGORIA:

FRONTEND:
  - frontend/src/api/external/federal-revenue/federal-revenue-service.js
  - frontend/src/components/comum/FederalRevenueImportButton.vue
  
BACKEND:
  (nenhum arquivo)
  
COMMON:
  (nenhum arquivo)
```

---

### **ETAPA 3: Analisar Mudanças Detalhadas**

Execute o comando para pegar o diff de cada arquivo:
```bash
git diff origin/dev...HEAD -- "caminho/do/arquivo"
```

**Regras importantes:**
- Analisar **no máximo 10 arquivos** (os mais importantes)
- Pegar **no máximo 2000 caracteres** do diff de cada arquivo
- Se houver mais de 10 arquivos, mencionar: "... e mais X arquivo(s) alterado(s)."

**Prioridade de análise:**
1. Novos arquivos criados (A)
2. Arquivos modificados principais (M)
3. Arquivos de configuração modificados

**Para cada arquivo analisado, mostrar:**
```
--- frontend/src/api/external/federal-revenue/federal-revenue-service.js ---
+import BrasilAPIProvider from './providers/brasilapi-provider';
+import CNPJDataAdapter from './adapters/cnpj-data-adapter';
+
+export default class FederalRevenueService {
...
```

---

### **ETAPA 4: Gerar Descrição da PR**

Com base na análise, gerar uma descrição seguindo o formato especificado abaixo.

---

### **ETAPA 5: Salvar em Arquivo**

**IMPORTANTE:** Após gerar a descrição, **SEMPRE** salvar em um arquivo na raiz do projeto.

**📝 Nome do arquivo:** `PR-DESCRIPTION.md`

**📂 Localização:** Raiz do repositório (mesmo nível de package.json)

**💾 Ação:** Criar o arquivo com o conteúdo completo da descrição gerada

**✅ Informar ao usuário:**
```
✅ Descrição da PR gerada com sucesso!
💾 Arquivo salvo em: PR-DESCRIPTION.md
📋 Você pode copiar e colar o conteúdo direto no Pull Request
```

---

## 📋 Formato da Descrição (ETAPA 4)

### **ETAPA 4: Gerar Descrição da PR**

Com base na análise, gerar uma descrição seguindo **EXATAMENTE** este formato:

```markdown
# 📋 Resumo das Mudanças

## 🎯 Tipo de Alteração
[Descreva o tipo principal da alteração: feature, bugfix, refatoração, etc.]

## 🔧 Alterações Técnicas
[Detalhes técnicos das mudanças - O QUE foi feito tecnicamente]

## 📁 Arquivos Modificados
[Lista organizada dos arquivos alterados por categoria]

## ⚠️ Observações Importantes
[Alertas sobre quebradores de compatibilidade ou mudanças críticas]

## 🧪 Testes
[Sugestões de testes ou validações necessárias]
```

---

## 📝 Diretrizes de Conteúdo

### **🎯 Tipo de Alteração**
Identifique o tipo principal:
- ✨ **Feature:** Nova funcionalidade
- 🐛 **Bugfix:** Correção de bug
- ♻️ **Refactor:** Refatoração de código
- 🎨 **Style:** Ajustes de UI/UX
- 📝 **Docs:** Documentação
- 🔧 **Config:** Configurações
- ✅ **Tests:** Testes unitários/integração
- 🚀 **Performance:** Melhorias de performance
- 🔒 **Security:** Segurança

**Exemplo:**
```
✨ Feature: Implementação de integração com BrasilAPI para importação de dados da Receita Federal
```

---

### **🔧 Alterações Técnicas**

Descreva as mudanças técnicas de forma clara e organizada:

**O que incluir:**
- Novos componentes/serviços criados
- Alterações em APIs (endpoints, métodos)
- Novos models/adapters
- Mudanças em fluxos existentes
- Integrações com APIs externas
- Validações adicionadas
- Tratamento de erros
- Testes unitários criados/atualizados

**O que NÃO incluir:**
- Nomes de métodos específicos
- Código inline
- Detalhes de implementação muito granulares

**Formato:**
Use bullet points (⦁) para organizar:
```
⦁ Criado serviço de integração com BrasilAPI para consulta de CNPJ
⦁ Implementado adapter para transformar dados da API em modelo do sistema
⦁ Adicionado componente de botão para importação visual
⦁ Implementadas validações de CPF/CNPJ obrigatório para sócios
⦁ Adicionado tratamento de erros da API com mensagens amigáveis
```

---

### **📁 Arquivos Modificados**

Liste os arquivos de forma **organizada por categoria**:

**Formato:**
```
**FRONTEND** (7 arquivos alterados):
⦁ Novos arquivos:
  - src/api/external/federal-revenue/federal-revenue-service.js
  - src/api/external/federal-revenue/adapters/cnpj-data-adapter.js
  - src/components/comum/FederalRevenueImportButton.vue

⦁ Modificados:
  - src/components/form-customer/PJ/cover/PJCustomerCover.vue
  - src/views/customers/FormCustomer.vue

**BACKEND** (nenhum arquivo alterado)

**COMMON** (nenhum arquivo alterado)

**CONFIGURAÇÃO**:
⦁ package.json - Dependências atualizadas
```

**Agrupe por:**
- Novos arquivos (A - Added)
- Modificados (M - Modified)
- Removidos (D - Deleted)

---

### **⚠️ Observações Importantes**

Destaque pontos críticos que os revisores devem saber:

**O que incluir:**
- Quebradores de compatibilidade (breaking changes)
- Mudanças que afetam outros módulos/serviços
- Migrações de dados necessárias
- Dependências novas ou atualizadas
- Variáveis de ambiente necessárias
- Permissões/acessos novos necessários
- Mudanças em contratos de API
- Performance impacts

**Exemplo:**
```
⚠️ Esta implementação adiciona integração com API externa (BrasilAPI)
⦁ Não há breaking changes
⦁ Funciona de forma independente, não afeta fluxos existentes
⦁ Requer conexão com internet para funcionar
⦁ Validação de sócios agora é obrigatória antes de salvar
```

---

### **🧪 Testes**

Sugira testes específicos e acionáveis:

**Categorias de teste:**

**1. Fluxo Principal (Happy Path):**
```
⦁ Acesse o cadastro de cliente PJ
⦁ Informe um CNPJ válido
⦁ Clique no botão de importação (logo da Receita Federal)
⦁ Verifique se os dados foram preenchidos corretamente
```

**2. Validações:**
```
⦁ Tente salvar um cliente com sócios sem CPF/CNPJ
⦁ Verifique se a mensagem de erro é exibida
⦁ Informe um CNPJ inválido e tente importar
```

**3. Casos de Erro:**
```
⦁ Teste com CNPJ inexistente
⦁ Teste com empresa inativa
⦁ Simule erro de conexão (desconecte internet)
```

**4. Casos de Borda:**
```
⦁ Teste com CNPJ que não tem sócios
⦁ Teste com CNPJ de filial (matriz deve ser consultada)
⦁ Teste com CNPJ formatado e sem formatação
```

**5. Regressão:**
```
⦁ Verifique se o cadastro manual ainda funciona
⦁ Teste fluxos existentes não foram afetados
```

**6. Responsividade:**
```
⦁ Teste em desktop (Chrome, Firefox, Edge)
⦁ Teste em tablet
⦁ Teste em mobile
```

---

## 🎨 Tom e Estilo

### **Tom Profissional mas Acessível:**
- Use linguagem clara e direta
- Seja específico, não genérico
- Use emojis para melhorar legibilidade
- Organize por seções lógicas
- Evite jargões desnecessários

### **Seja Específico:**
❌ **Ruim:** "Foram feitas melhorias no sistema"  
✅ **Bom:** "Implementada integração com BrasilAPI para importação automática de dados de CNPJ"

❌ **Ruim:** "Corrigidos alguns bugs"  
✅ **Bom:** "Corrigida validação de CPF que permitia salvar com formato incorreto"

---

## 🚀 Comandos para Usar

### **Comando Completo:**
```
"Analise o diff entre origin/dev e HEAD e gere a descrição da Pull Request seguindo o formato do arquivo .github/copilot-merge-request-template.md. Salve a descrição no arquivo PR-DESCRIPTION.md na raiz do projeto."
```

### **Comando Resumido:**
```
"Gere a descrição da PR baseada no diff com dev e salve em PR-DESCRIPTION.md"
```

### **Comando Direto:**
```
"PR description"
```

**Resultado esperado:**
1. Análise completa do diff
2. Descrição gerada seguindo o template
3. Arquivo `PR-DESCRIPTION.md` criado na raiz
4. Mensagem de confirmação com caminho do arquivo

---

## ✅ Checklist para a IA

Antes de gerar a descrição, verifique:

- [ ] Executei `git diff --name-only origin/dev...HEAD`?
- [ ] Categorizei os arquivos por área?
- [ ] Analisei o diff dos arquivos principais (máx 10)?
- [ ] Identifiquei o tipo de alteração (feature/bugfix/etc)?
- [ ] Entendi a funcionalidade principal implementada?
- [ ] Listo os arquivos modificados por categoria?
- [ ] Destaquei observações importantes?
- [ ] Sugeri testes específicos e acionáveis?
- [ ] Usei o formato EXATO do template?
- [ ] Mantive tom profissional mas acessível?
- [ ] **Salvei a descrição em PR-DESCRIPTION.md na raiz?** ✨
- [ ] **Informei ao usuário que o arquivo foi criado?** ✨

**Se todas as respostas forem SIM → Gerar descrição e salvar arquivo!** ✅

---

## 📚 Exemplo Completo

```markdown
# 📋 Resumo das Mudanças

## 🎯 Tipo de Alteração
✨ Feature: Implementação de integração com BrasilAPI para importação automática de dados da Receita Federal em cadastros de clientes PJ

## 🔧 Alterações Técnicas
⦁ Criada arquitetura completa de integração com BrasilAPI (Service → Provider → Adapter)
⦁ Implementado FederalRevenueService para orquestrar consultas de CNPJ
⦁ Criado BrasilAPIProvider para comunicação com API externa
⦁ Desenvolvido CNPJDataAdapter para transformar dados da API no modelo do sistema
⦁ Adicionado componente FederalRevenueImportButton reutilizável
⦁ Integrado botão de importação na tela de cadastro PJ (capa e wizard de sócios)
⦁ Implementada validação obrigatória de CPF/CNPJ para todos os sócios antes de salvar
⦁ Adicionado tratamento de erros com mensagens amigáveis (CNPJ inválido, empresa inativa, timeout, etc)
⦁ Implementada normalização de dados de endereço (cidade com/sem acento)
⦁ Criada lógica de identificação de sócio PF vs PJ usando campo identifier da API

## 📁 Arquivos Modificados
**FRONTEND** (14 arquivos):
⦁ Novos arquivos criados:
  - src/api/external/federal-revenue/federal-revenue-service.js
  - src/api/external/federal-revenue/providers/base-provider.js
  - src/api/external/federal-revenue/providers/brasilapi-provider.js
  - src/api/external/federal-revenue/adapters/cnpj-data-adapter.js
  - src/api/external/federal-revenue/models/cnpj-data.js
  - src/components/comum/FederalRevenueImportButton.vue
  - assets/images/logo_receita_federal.jpg

⦁ Arquivos modificados:
  - src/components/form-customer/PJ/cover/PJCustomerCover.vue
  - src/components/form-customer/PJ/partnersShareholders/PartnerWizardBasicStep.vue
  - src/views/customers/FormCustomer.vue
  - src/components/comum/AddressComponent.vue
  - src/components/comum/CNAEField.vue
  - src/components/form-customer/CustomerPartners.vue

**BACKEND**: nenhum arquivo alterado

**COMMON**: nenhum arquivo alterado

**DOCUMENTAÇÃO**:
⦁ .github/copilot-instructions.md - Atualizado com regras de código

## ⚠️ Observações Importantes
⚠️ Esta feature adiciona integração com API externa (BrasilAPI)
⦁ Não há breaking changes - funcionalidade adicional, não substitui fluxo existente
⦁ Cadastro manual continua funcionando normalmente
⦁ Botão de importação só aparece quando CNPJ é válido
⦁ Requer conexão com internet para funcionar
⦁ API BrasilAPI tem limite de requisições (sem autenticação)
⦁ Validação de CPF/CNPJ de sócios agora é obrigatória antes de salvar cliente
⦁ Código segue novas regras de estilo (if de 1 linha sem chaves)

## 🧪 Testes

**Fluxo Principal:**
⦁ Acesse Cadastro de Cliente → Tipo PJ
⦁ Informe um CNPJ válido (ex: 00.000.000/0001-91)
⦁ Clique no botão com logo da Receita Federal ao lado do campo CNPJ
⦁ Verifique se dados foram preenchidos: Razão Social, Nome Fantasia, Capital Social, CNAE, Endereço, Telefone, Sócios
⦁ Valide se sócios PJ têm CNPJ preenchido e sócios PF têm CPF vazio
⦁ Salve o cliente e confirme sucesso

**Validações:**
⦁ Tente salvar cliente com sócios sem CPF/CNPJ → deve mostrar erro
⦁ Tente importar com CNPJ inválido → botão deve ficar desabilitado
⦁ Tente importar com CNPJ vazio → botão não deve aparecer

**Casos de Erro:**
⦁ Informe CNPJ inexistente (ex: 11.111.111/1111-11) → deve mostrar mensagem "CNPJ não encontrado"
⦁ Informe CNPJ de empresa inativa → deve mostrar erro "empresa com situação cadastral inativa"
⦁ Desconecte internet e tente importar → deve mostrar erro de conexão

**Wizard de Sócios:**
⦁ No cadastro PJ, adicione um sócio PJ
⦁ Informe CNPJ do sócio
⦁ Clique no botão de importação no wizard do sócio
⦁ Verifique se dados do sócio foram preenchidos

**Casos de Borda:**
⦁ Teste com CNPJ sem sócios cadastrados
⦁ Teste com CNPJ formatado (00.000.000/0001-91) e sem formatação (00000000000191)
⦁ Teste com cidade que tem acento (ex: São Paulo) - deve normalizar corretamente

**Regressão:**
⦁ Cadastre cliente PJ manualmente (sem usar importação) → deve funcionar normalmente
⦁ Edite cliente PJ existente → dados devem carregar corretamente
⦁ Teste outros tipos de cadastro (PF, Remessa) → não devem ser afetados

**Responsividade:**
⦁ Teste em desktop (1920x1080)
⦁ Teste em tablet (768px)
⦁ Teste em mobile (375px)
⦁ Botão de importação deve aparecer corretamente em todas as resoluções
```

---

## 🎯 Resumo Final

**Este template substitui o script `generate-pr-description.js`**

Ao invés de rodar o script com Gemini API, você simplesmente pede:
```
"Gere a descrição da PR baseada no diff com dev"
```

E a IA fará **EXATAMENTE** o que o script faria:
1. ✅ Listar arquivos alterados
2. ✅ Categorizar por área (frontend/backend/etc)
3. ✅ Analisar diff dos principais arquivos
4. ✅ Gerar descrição profissional
5. ✅ **Salvar em PR-DESCRIPTION.md na raiz**
6. ✅ **Informar que o arquivo foi criado**

**Você não precisa mais:**
- ❌ Configurar Gemini API Key
- ❌ Instalar dependências do script
- ❌ Rodar script manualmente
- ❌ Copiar e colar do terminal

**Agora é só:**
- ✅ Pedir: "PR description"
- ✅ Copiar conteúdo de `PR-DESCRIPTION.md`
- ✅ Colar no Pull Request

🚀 **Simples, rápido e eficiente!**
