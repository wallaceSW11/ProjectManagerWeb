---
name: criar-mr
description: Cria um Pull Request no GitHub contra a branch main. Commit, push e PR.
---

# Skill: criar-mr

Use quando quiser criar um Pull Request. Faz todo o fluxo: commit (se necessário), push e PR contra `main`.

## Comportamento

- Autorizado a git add, commit, push e gh pr create (sobrescreve a proibição geral)
- Tudo em português, direto sem enrolação

## Workflow

### 1. Verificar estado do git

```bash
git status
git log --oneline -5
git branch --show-current
```

### 2. Commit (se houver alterações)

Pergunte: "Tem X arquivos não commitados. Descreva o que foi feito para eu montar a mensagem de commit."

Formato: `tipo(escopo): descrição em pt-br`

```bash
git add -A
git commit -m "tipo(escopo): descrição"
```

### 3. Push

```bash
git push origin <branch-atual>
```

### 4. Criar PR

Se não houver commits novos (working tree limpo e up-to-date), pule direto para cá.

Use `git log main..HEAD --oneline` para os commits e `git diff --stat main..HEAD` para o resumo dos arquivos.

`gh pr create --base main` com o template:

```markdown
## O que mudou

### `{arquivos principais}`
{descrição concisa das mudanças}

### Correções de bugs
{se houver}

## Como testar
{comandos ou passos}
```

Título do PR: `tipo(escopo): descrição`

### 5. Retornar a URL do PR
