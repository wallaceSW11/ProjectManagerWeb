---
inclusion: always
---

# rule-git

Convenções de git para o projeto Confeitaria.

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
feat(products): adicionar upload de imagem
fix(auth): corrigir expiração do cookie em produção
chore(deps): atualizar vuetify para 3.8
refactor(orders): extrair lógica de status para service
```

## regras

- Commits atômicos — uma mudança por commit
- Nunca commitar .env, secrets ou credenciais
- Nunca commitar código comentado
- PR sempre para develop, nunca direto para main
- Descrição do PR em pt-br com contexto do que foi feito e por quê
