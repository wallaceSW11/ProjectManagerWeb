# Implementação do Endpoint de Atualização do Campo Expandido

## ✅ Implementado com Sucesso

### 1. **Endpoint PATCH no PastaController**
```csharp
[HttpPatch("{pastaId}/projetos/{projetoId}/expandido")]
public async Task<IActionResult> AtualizarExpandidoProjeto(
  Guid pastaId, 
  Guid projetoId, 
  [FromBody] bool expandido)
```

**URL**: `PATCH /api/pastas/{pastaId}/projetos/{projetoId}/expandido`

**Parâmetros**:
- `pastaId` (Guid): Identificador da pasta
- `projetoId` (Guid): Identificador do projeto
- `expandido` (bool): Valor true/false no body da requisição

### 2. **Método no PastaService**
```csharp
public async Task AtualizarExpandidoProjeto(Guid pastaId, Guid projetoId, bool expandido)
```

**Funcionalidade**:
- Localiza a pasta pelo ID
- Encontra o repositório associado à pasta
- Procura o projeto no repositório principal ou nos agregados
- Atualiza o campo `Expandido` do projeto
- Salva as alterações no arquivo JSON

### 3. **Como Usar**

**Exemplo de requisição**:
```http
PATCH /api/pastas/12345678-1234-1234-1234-123456789012/projetos/87654321-4321-4321-4321-210987654321/expandido
Content-Type: application/json

true
```

**Resposta de sucesso**: Status 200 OK
**Resposta de erro**: Status 400 Bad Request com mensagem de erro

### 4. **Tratamento de Erros**
- Pasta não encontrada
- Repositório não encontrado  
- Projeto não encontrado (em repositórios principais ou agregados)

### 5. **Integração Existente**
O campo `Expandido` já está sendo enviado na resposta através do `ProjetoDisponivelDTO`, então a interface já pode consumir essa informação.