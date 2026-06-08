---
name: criar-mr
description: Cria um Pull Request no GitHub contra a branch main. Commit, push e PR.
---

# Skill: criar-mr

Use quando quiser criar um Pull Request. Faz todo o fluxo: commit (se necessário), push e PR.

## Comportamento

- Autorizado a git add, commit, push e gh pr create (essa skill sobrescreve a proibição geral)
- Tudo em português
- Seja direto sem enrolação

## Workflow

### 1. Verificar estado do git

```bash
git status
git log --oneline -5
git branch --show-current
```

### 2. Se houver arquivos não commitados

Pergunte: "Tem X arquivos não commitados. Descreva o que foi feito para eu montar a mensagem de commit."

Use o formato do projeto: `tipo(escopo): descrição em pt-br`

Após receber a descrição:
```bash
git add -A
git commit -m "tipo(escopo): descrição"
```

### 3. Push

```bash
git push origin <branch-atual>
```

### 4. Se não houver commits a enviar (working tree limpo e up-to-date)

Pular direto para o passo 5. Usar `git log main..HEAD` para obter os commits e `git diff --stat main..HEAD` para o resumo dos arquivos modificados. Extrair o tipo/escopo do commit mais recente para o título do PR.

### 5. Criar PR

Usar `gh pr create` com o template abaixo. Extrair informações do `git log` e do contexto.

**Template do PR:**

```markdown
## O que mudou

### `{arquivos principais}`
{descrição concisa das mudanças}

### Correções de bugs
{se houver, listar}

## Como testar
{comandos ou passos}
```

O título do PR deve seguir: `tipo(escopo): descrição`

### 5. Retornar a URL do PR
