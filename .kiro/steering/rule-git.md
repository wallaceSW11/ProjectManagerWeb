---
inclusion: always
---

# rule-git

Convenções de git para o projeto PMW.

## branches

main — produção, protegida
develop — integração
feature/nome-da-feature — nova funcionalidade
fix/nome-do-bug — correção
chore/descricao — tarefas técnicas (deps, config, refactor)

## commits

Formato: `tipo(escopo): descrição curta em pt-br`

Tipos:
- feat: nova funcionalidade
- fix: correção de bug
- chore: tarefa técnica sem impacto funcional
- refactor: refatoração sem mudança de comportamento
- style: formatação, sem mudança de lógica
- docs: documentação
- test: testes

Exemplos:
```
feat(clone): adicionar opção de clone raso
fix(pastas): corrigir listagem de pastas ocultas
chore(deps): atualizar vuetify para 3.8
refactor(repositorios): extrair lógica de agregados para service
docs(steering): criar flow-sitesiis
style(sitesiis): ajustar espaçamento dos cards
```

## regras

- Commits atômicos — uma mudança por commit
- Nunca commitar .env, secrets ou credenciais
- Nunca commitar código comentado
- PR sempre para develop, nunca direto para main
- Descrição do PR em pt-br com contexto do que foi feito e por quê
