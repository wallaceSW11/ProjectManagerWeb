# Implementação: APLICAR_PASTA

## Resumo
Implementada a funcionalidade de **APLICAR_PASTA** no sistema de menus, permitindo copiar pastas completas de uma origem para um destino dentro dos repositórios.

## Alterações no Backend

### 1. DTOs (`backend/src/DTOs/RepositorioRequestDTO.cs`)
- Adicionado `List<PastaDTO>? Pastas` no `MenuDTO`
- Criado novo record `PastaDTO` com:
  - `string Origem` - Caminho completo da pasta origem
  - `string Destino` - Caminho relativo ao repositório

### 2. Service (`backend/src/Services/ComandoService.cs`)
- Atualizado método `ExecutarComandoMenu` para processar pastas
- Lógica implementada:
  - Remove pasta de destino se já existir (sobrescreve)
  - Cria diretório pai se não existir
  - Copia pasta completa usando PowerShell `Copy-Item -Recurse -Force`

## Alterações no Frontend

### 1. Types (`frontend/src/types/index.ts`)
- Adicionado tipo `'APLICAR_PASTA'` ao enum de tipos de menu
- Criada interface `IPastaMenu` com:
  - `identificador: string`
  - `origem: string`
  - `destino: string`
- Adicionado `pastas: IPastaMenu[]` na interface `IMenu`

### 2. Models
- **`frontend/src/models/PastaMenuModel.ts`** (novo)
  - Modelo para pastas de menu
  - Implementa `IPastaMenu`
  
- **`frontend/src/models/MenuModel.ts`** (atualizado)
  - Adicionado suporte a `pastas: IPastaMenu[]`
  - Mapeia pastas usando `PastaMenuModel`

### 3. Componentes

#### `frontend/src/components/repositorios/MenuCadastro.vue` (atualizado)
- Adicionado select para escolher tipo de menu:
  - Aplicar Arquivo
  - **Aplicar Pasta** (novo)
  - Comando Avulso
- Renderização condicional baseada no tipo selecionado
- Gerenciamento de estado para pastas (adicionar, editar, excluir)
- Sistema de abas para navegação entre cadastro principal e itens

#### `frontend/src/components/repositorios/menuItemCadastro/MenuItemPastaCadastro.vue` (novo)
- Componente dedicado para cadastro de pastas
- Campos:
  - **Pasta Origem**: Caminho completo (ex: `C:\__Arquivos_Wallace__\AmazonQ\kiro\.kiro`)
  - **Destino**: Caminho relativo ao repositório (ex: `bimerup\frontend\bimerup`)
- Tabela com listagem de pastas cadastradas
- Ações: Adicionar, Editar, Excluir

## Como Usar

### 1. Cadastrar Menu com Pasta
1. Acesse a tela de cadastro de repositórios
2. Na seção de Menus, clique em "Adicionar"
3. Preencha o título do menu
4. Selecione o tipo **"Aplicar Pasta"**
5. Clique em "Adicionar Pasta"
6. Informe:
   - **Pasta Origem**: Caminho completo da pasta que deseja copiar
   - **Destino**: Caminho relativo dentro do repositório onde a pasta será colada
7. Salve o menu

### 2. Executar Menu
Ao executar o menu do tipo APLICAR_PASTA:
- A pasta de destino será **removida completamente** se já existir
- A pasta origem será copiada para o destino especificado
- Todos os arquivos e subpastas serão copiados recursivamente

## Exemplo de Uso

**Cenário**: Copiar a pasta `.kiro` para dentro do projeto bimerup

**Configuração**:
- Título: "Aplicar configuração Kiro"
- Tipo: Aplicar Pasta
- Pasta Origem: `C:\__Arquivos_Wallace__\AbrirProjetoEXE\AmazonQ\kiro\.kiro`
- Destino: `bimerup\frontend\bimerup`

**Resultado**: 
A pasta `.kiro` será copiada para:
`C:\tools\git\FATWEB1170_mensagem_ICMS_nao_informado\bimerup\frontend\bimerup\.kiro`

## Comportamento Importante

⚠️ **ATENÇÃO**: A funcionalidade **sempre sobrescreve** a pasta de destino sem perguntar. Se já existir uma pasta com o mesmo nome no destino, ela será removida completamente antes da cópia.

## Testes Recomendados

1. ✅ Backend compila sem erros
2. ✅ Frontend compila sem erros de tipo
3. ⏳ Testar cadastro de menu com pasta via interface
4. ⏳ Testar execução do menu e verificar se a pasta é copiada corretamente
5. ⏳ Testar sobrescrita de pasta existente
6. ⏳ Testar com múltiplas pastas no mesmo menu
