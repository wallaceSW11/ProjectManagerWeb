---
name: manter-docs
description: Como criar, editar ou consolidar a documentação do agente em .opencode/ (agentes, skills, flows, code-style, ecosystem). Mantém coesão e economia de tokens.
---

# Skill: manter-docs

Use ao documentar uma descoberta do projeto ou ao revisar/consolidar a documentação do `.opencode/`.

## Onde cada coisa mora

| Arquivo | Conteúdo | Carregado |
|---------|----------|-----------|
| `AGENTS.md` | Regras globais, comandos, git, proibições | sempre |
| `.opencode/ecosystem-pmw.md` | Mapa: domínio, persistência, infra, índice de fluxos | sempre |
| `.opencode/code-style/*.md` | Regras de código por stack | sempre |
| `.opencode/agents/*.md` | Persona e permissões de cada agente | ao usar o agente |
| `.opencode/flows/*.md` | Fluxo ponta a ponta de uma feature | sob demanda |
| `.opencode/referencias/*.md` | Boilerplates e exemplos de código real | sob demanda |
| `.opencode/skills/*/SKILL.md` | Procedimento reutilizável | sob demanda |
| `.opencode/plugins/` | Verificação automática (code style + infra) | runtime |

## Regras ao escrever

- Sem duplicar: se a informação já existe, referencie o arquivo em vez de repetir.
- `AGENTS.md`, `ecosystem-pmw.md` e `code-style/` custam token em **toda** conversa — só entra o que muda comportamento.
- Fluxo vai em `flows/`, boilerplate e exemplo de código em `referencias/`, histórico de decisão em `docs/`.
- Toda regra precisa de um porquê quando não for óbvia.
- Uma regra = uma interpretação. Se o agente fizer o oposto, reescreva com errado vs certo.
- Nome de skill e de arquivo em kebab-case, em português.

## Quando documentar uma descoberta

Documentar só se as duas condições valerem:

1. Houve investigação real no código (não dedução).
2. A regra/comportamento não está em nenhum arquivo acima e se repete no futuro.

Nunca documentar: bug pontual, código em desenvolvimento ativo, dado que muda (ID, token, URL temporária), ajuste de UI sem impacto técnico.

## Checklist

- [ ] Arquivo certo pelo tipo (regra global × fluxo × skill × code-style)
- [ ] Sem redundância com outro arquivo
- [ ] `ecosystem-pmw.md` aponta para o novo arquivo, se ele for permanente
- [ ] Exemplos reais do projeto, não genéricos
- [ ] Removida a linha que não muda comportamento
