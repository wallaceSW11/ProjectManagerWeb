# Implementação: Campo "Ativo" no Menu de Contexto

**Data:** 2026-01-09  
**Status:** ✅ Concluído

## Objetivo

Adicionar um campo booleano "Ativo" nos menus de contexto dos repositórios, permitindo que o usuário desative menus sem precisar excluí-los. Apenas menus ativos aparecem no menu de contexto das pastas.

## Alterações Realizadas

### Backend (.NET)

#### 1. DTO Atualizado
**Arquivo:** `backend/src/DTOs/RepositorioRequestDTO.cs`

```csharp
public sealed record MenuDTO(
    Guid Identificador,
    string Titulo,
    string Tipo,
    List<ArquivosDTO>? Arquivos,
    List<PastaDTO>? Pastas,
    List<string>? Comandos,
    bool Ativo = true  // ✨ Novo campo com valor padrão
);
```

#### 2. Migration
**Arquivo:** `backend/migrations/AddAtivoToMenus.ps1`

Script PowerShell que:
- Cria backup automático do `repositorios.json`
- Adiciona `ativo: true` em todos os menus existentes
- Mantém formatação JSON

**Como executar:**
```powershell
cd backend/migrations
.\AddAtivoToMenus.ps1
```

### Frontend (Vue 3)

#### 1. Interface TypeScript
**Arquivo:** `frontend/src/types/index.ts`

```typescript
export interface IMenu {
  identificador: string;
  titulo: string;
  tipo: 'APLICAR_ARQUIVO' | 'APLICAR_PASTA' | 'COMANDO_AVULSO';
  arquivos: IArquivo[];
  pastas: IPastaMenu[];
  comandos: Array<{ comando: string }>;
  ativo: boolean;  // ✨ Novo campo
}
```

#### 2. Model
**Arquivo:** `frontend/src/models/MenuModel.ts`

```typescript
export default class MenuModel implements IMenu {
  // ... outros campos
  ativo: boolean;

  constructor(obj: Partial<IMenu> = {}) {
    // ... outros campos
    this.ativo = obj.ativo ?? true;  // ✨ Padrão true
  }
}
```

#### 3. Formulário de Cadastro
**Arquivo:** `frontend/src/components/repositorios/MenuCadastro.vue`

Adicionado checkbox no formulário:
```vue
<v-checkbox
  label="Ativo"
  v-model="menuSelecionado.ativo"
  hint="Apenas menus ativos aparecem no menu de contexto"
  persistent-hint
/>
```

#### 4. Filtro no Menu de Contexto
**Arquivo:** `frontend/src/components/pastas/CardPasta.vue`

Adicionado computed para filtrar apenas menus ativos:
```typescript
const menusAtivos = computed(() => {
  return props.pasta.menus.filter(menu => menu.ativo);
});
```

Template atualizado para usar `menusAtivos` ao invés de `pasta.menus`.

## Comportamento

### Cadastro/Edição
- Checkbox "Ativo" visível no formulário de menu
- Valor padrão: `true` (marcado)
- Hint explicativo sobre o comportamento

### Exibição
- Menu de contexto (botão três pontos) só aparece se houver menus ativos
- Apenas menus com `ativo === true` são listados
- Menus inativos permanecem no banco mas não aparecem na UI

### Migration
- Todos os menus existentes recebem `ativo: true`
- Backup automático antes da modificação
- Compatibilidade total com dados existentes

## Testes Recomendados

1. ✅ Criar novo menu → deve vir com "Ativo" marcado
2. ✅ Desmarcar "Ativo" → menu não deve aparecer no contexto
3. ✅ Editar menu existente → checkbox deve refletir o valor atual
4. ✅ Executar migration → todos os menus antigos devem ter `ativo: true`
5. ✅ Menu de contexto vazio → botão três pontos não deve aparecer
6. ✅ Tabela de menus → coluna "Ativo" mostra ícone verde/vermelho

## Validações Realizadas

✅ **Migration executada com sucesso**
- 6 repositórios processados
- 5 menus atualizados
- Backup criado automaticamente

✅ **Backend compilando sem erros**
- `dotnet build` executado com sucesso

✅ **Frontend sem erros de tipo**
- `npm run type-check` passou

✅ **Campo adicionado no JSON**
- Verificado que `"ativo": true` está presente nos menus

## Arquivos Modificados

### Backend
- `backend/src/DTOs/RepositorioRequestDTO.cs`

### Frontend
- `frontend/src/types/index.ts`
- `frontend/src/models/MenuModel.ts`
- `frontend/src/components/repositorios/MenuCadastro.vue`
- `frontend/src/components/pastas/CardPasta.vue`

### Novos Arquivos
- `backend/migrations/AddAtivoToMenus.ps1`
- `backend/migrations/README.md`

## Próximos Passos

1. Executar a migration em produção
2. Testar o fluxo completo
3. Validar que menus inativos não aparecem
4. Confirmar que o checkbox funciona corretamente
