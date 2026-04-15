---
inclusion: manual
---

# skill-criar-steering

Como criar e manter arquivos de steering neste projeto.

## 1. antes de escrever

Perguntas obrigatórias:
1. O que o steering deve fazer o agente fazer?
2. Quando ativa? (sempre / por arquivo / manualmente)
3. Já existe algo cobrindo isso? (ler `.kiro/steering/` antes)
4. Qual o nível de autonomia esperado?

Nunca inventar padrões — basear tudo no código existente.

## 2. tipo e inclusion

| Tipo | inclusion | Quando usar |
|------|-----------|-------------|
| Persona / comportamento global | `always` | Vale em toda conversa |
| Visão geral do projeto | `always` | Mapa central |
| Git | `always` | Convenções de versionamento |
| Flow de feature | `fileMatch` | Fluxo ponta a ponta |
| Rule de code style | `fileMatch` | Regras técnicas por escopo |
| Lib de componentes | `fileMatch` | API de lib do projeto |
| Referência pesada | `manual` | Só quando chamado via `#nome` |

## 3. prefixos obrigatórios

| Prefixo | Uso |
|---------|-----|
| `skill-` | Persona, comportamento, libs |
| `flow-` | Fluxo de feature ponta a ponta |
| `rule-` | Regras técnicas obrigatórias |
| `ecosystem-` | Visão geral (único por projeto) |

Nomes em inglês. Nunca duplicar — referenciar com `#nome`.

## 4. front-matter

```yaml
---
inclusion: always
---

---
inclusion: fileMatch
fileMatchPattern: "**/auth*/**,**/Login*"
description: Descrição curta do escopo.
---

---
inclusion: manual
---
```

## 5. estrutura por tipo

### flow-*.md
```
front-matter → contexto/identidade → rotas → fluxo (numerado, front e back separados)
→ stores envolvidas → contrato do endpoint (body tipado) → arquivos envolvidos
```

### rule-*.md
```
front-matter → ## proibido (NUNCA, críticos primeiro) → regras com exemplo errado vs certo
```

### skill-*.md
```
front-matter → identidade/objetivo → comportamento/regras → proibições absolutas
```

## 6. princípios de escrita

**Explique o porquê:**
```
Ruim:  NUNCA use AutoMapper.
Bom:   Nunca use AutoMapper — o projeto usa MapToDto private static no service
       para manter rastreabilidade e evitar dependência de lib externa.
```

**Calibre o detalhe:**
| Situação | Detalhe |
|----------|---------|
| Tarefa simples | Só a regra |
| Padrão específico do projeto | Regra + exemplo |
| Edge cases | Regra + certo + errado |
| Agente erra frequentemente | Workflow passo a passo |

**Mantenha enxuto:** steerings `always` carregam em toda conversa. Se remover uma linha não muda o comportamento, remova.

## 7. hardening

- Seção "proibido" no topo das rules — NUNCA, críticos primeiro
- Exemplos: sempre errado vs certo para regras de código
- Uma regra = uma interpretação
- fileMatchPattern: testar mentalmente se cobre os arquivos reais

## 8. padrões de fileMatchPattern

| Feature | Padrão |
|---------|--------|
| Backend (geral) | `backend/**` |
| Frontend (geral) | `frontend/**` |
| Repositórios | `**/Repositorio*,**/repositorio*` |
| Pastas | `**/Pasta*,**/pasta*` |
| Sites IIS / Deploy | `**/SiteIIS*,**/siteIIS*,**/Deploy*` |
| IDEs | `**/IDE*,**/ide*` |
| Configuração | `**/Configuracao*,**/configuracao*` |
| Steerings / hooks | `.kiro/steering/**,.kiro/hooks/**` |

## 9. troubleshooting

| Sintoma | Causa | Solução |
|---------|-------|---------|
| Agente ignora | Instrução vaga ou enterrada | Mova pro topo, adicione exemplo |
| Agente faz o oposto | Ambígua ou contraditória | Reescreva com certo vs errado |
| Agente inventa | Falta contexto | Adicione input → output esperado |
| Agente segue parcialmente | Instrução complexa demais | Quebre em passos numerados |
| Agente aplica errado | Falta escopo | Adicione "quando NÃO usar" |
| Conflito com outro steering | Regras contraditórias | Revise ambos, unifique |

## 10. nunca documentar

- IDs, tokens, senhas, URLs temporárias
- Código em desenvolvimento ativo
- Bug pontual sem regra geral
- Dados de ambiente (.env)
- Informação já coberta por steering existente

## 11. hook de auto-aprendizado

Dispara quando: 10+ leituras de arquivo + descoberta ausente nos steerings.
Abaixo disso: silêncio.

## checklist final

- [ ] Prefixo correto (skill-/ flow-/ rule-/ ecosystem-)
- [ ] Nome em inglês
- [ ] front-matter correto
- [ ] Explica o porquê (não só o quê)
- [ ] Exemplos reais do projeto
- [ ] Não duplica steering existente
- [ ] fileMatchPattern cobre os arquivos reais
- [ ] Referência adicionada no ecosystem-pmw.md (se permanente)
