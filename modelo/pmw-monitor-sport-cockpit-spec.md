# Especificação Visual — PMW Monitor / Sport Cockpit

## Objetivo

Transformar o dashboard de monitoramento do Project Manager Web em um **cockpit automotivo esportivo**, inspirado visualmente em painéis de supercarros de alto desempenho.

A referência visual é um painel de Lamborghini/Ferrari/supercarro moderno, mas **não usar logos, marcas, símbolos ou elementos proprietários de fabricantes reais**.

O dispositivo final é um **Xiaomi Mi 9T em orientação landscape**, usado como um painel dedicado e ligado continuamente.

A tela é pequena, portanto o design deve ser **premium, extremamente limpo e legível**, sem excesso de informação.

---

# 1. Conceito visual

A interface deve transmitir:

- cockpit de carro esportivo;
- alta performance;
- precisão;
- tecnologia;
- sensação de instrumento físico;
- acabamento premium;
- visual escuro;
- contraste forte;
- leitura instantânea a alguns metros de distância.

A interface NÃO deve parecer:

- dashboard administrativo;
- sistema corporativo;
- página de CRUD;
- conjunto de cards tradicionais;
- painel cheio de informações;
- interface genérica de Vuetify.

A sensação desejada é:

> "Estou olhando para o painel de instrumentos de um supercarro, mas o carro é o meu notebook."

---

# 2. Estrutura geral da tela

A tela é horizontal.

Layout principal:

```text
┌──────────────────────────────────────────────────────────────┐
│ ⚡ PMW MONITOR                                      ● ONLINE │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│     ┌────────────────────┐       ┌────────────────────┐     │
│     │                    │       │                    │     │
│     │      CPU           │       │       RAM          │     │
│     │                    │       │                    │     │
│     │    0  20 40 60...  │       │   ...60 80 100     │     │
│     │       ╲  ╱         │       │         ╲  ╱       │     │
│     │        ╲╱          │       │          ╲╱        │     │
│     │       28%          │       │        68%         │     │
│     │                    │       │                    │     │
│     └────────────────────┘       └────────────────────┘     │
│                                                              │
│              dados secundários muito discretos              │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

Os dois velocímetros são os elementos visuais dominantes.

CPU fica à esquerda.

RAM fica à direita.

---

# 3. Cabeçalho

O cabeçalho deve ser extremamente discreto.

No canto esquerdo:

```text
⚡ PMW MONITOR
```

O raio pode ser amarelo/dourado.

O texto deve ser pequeno, branco/cinza claro, com aparência tecnológica.

No canto direito:

```text
● ONLINE
```

O ponto deve ser verde e ter um brilho muito sutil.

Não criar uma barra de navegação tradicional.

O cabeçalho serve apenas para identidade e status.

---

# 4. Velocímetro de CPU

## Posição

CPU ocupa aproximadamente metade esquerda da tela.

O velocímetro deve ser grande.

Formato circular, semelhante a um instrumento analógico de carro esportivo.

## Aparência

Características:

- fundo preto;
- círculo externo escuro;
- escala numérica;
- pequenos marcadores;
- marcadores maiores a cada 10%;
- faixa final da escala em vermelho;
- arco de progresso verde;
- ponteiro verde;
- centro metálico/escuro;
- pequenas sombras e brilhos;
- aparência tridimensional sutil.

Não exagerar nos efeitos.

Deve parecer um instrumento premium, não um gráfico gamer.

---

# 5. Escala da CPU

A escala representa:

```text
0 ─ 20 ─ 40 ─ 60 ─ 80 ─ 100
```

O velocímetro deve começar aproximadamente na posição de 7 horas e terminar aproximadamente na posição de 5 horas.

A maior parte da escala é neutra.

A região de uso extremo, aproximadamente acima de 90%, pode ter pequenos marcadores vermelhos.

O valor atual deve ser representado pelo ponteiro.

---

# 6. Valor da CPU

No centro do velocímetro:

```text
28%
CPU
USO ATUAL
```

O percentual é o elemento mais importante.

Exemplo:

```text
        28%
        CPU
     USO ATUAL
```

O `28%` deve ser grande, pesado e verde.

O texto `CPU` pode ser branco/cinza.

`USO ATUAL` deve ser pequeno e discreto.

---

# 7. CPU disponível

Próximo ao velocímetro, mas sem competir com o valor principal:

```text
72%
DISPONÍVEL
```

Esse dado deve ser visualmente secundário.

O usuário precisa conseguir entender rapidamente:

```text
CPU
28% usado
72% disponível
```

---

# 8. Velocímetro de RAM

A RAM segue exatamente a mesma linguagem visual da CPU.

Posição:

lado direito.

Cor principal:

**laranja/dourado esportivo**.

Não usar azul.

O velocímetro deve ter:

- escala;
- ponteiro;
- arco de progresso;
- centro;
- marcadores;
- faixa de alerta;
- valor grande no centro.

Exemplo:

```text
        68%
        RAM
     USO ATUAL
```

O `68%` deve ser laranja.

---

# 9. RAM disponível

Ao redor do velocímetro ou em uma região lateral discreta:

```text
32%
DISPONÍVEL
```

A informação deve ser claramente secundária ao percentual de uso.

---

# 10. Dados secundários

O foco do painel é exclusivamente:

1. CPU;
2. RAM.

Dados adicionais podem existir, mas devem ser pequenos.

Exemplos aceitáveis:

```text
3.42 GHz
52°C
```

Para RAM:

```text
21.6 GB
30.2 GB
```

Esses dados não podem roubar atenção dos velocímetros.

Não adicionar dezenas de métricas.

---

# 11. Centro da tela

Entre CPU e RAM pode existir uma pequena área central.

Essa área deve ser muito discreta.

Pode conter:

```text
PMW
Ubuntu 26.04 LTS
Uptime
```

ou simplesmente:

```text
Ubuntu 26.04 LTS
```

A área central NÃO deve virar um card grande.

Ela existe apenas para equilibrar visualmente os dois velocímetros.

---

# 12. Estética automotiva

A interface deve parecer um painel físico.

Usar:

- preto;
- grafite;
- cinza metálico;
- verde para CPU;
- laranja para RAM;
- vermelho apenas como alerta;
- pequenos reflexos;
- pequenos brilhos;
- sombras;
- profundidade;
- textura extremamente sutil semelhante a fibra de carbono.

Evitar:

- gradientes exagerados;
- neon excessivo;
- excesso de glow;
- glassmorphism;
- cards arredondados demais;
- sombras gigantes;
- bordas coloridas fortes.

O acabamento deve ser premium e discreto.

---

# 13. Ponteiros

Os ponteiros são uma das partes mais importantes.

Devem parecer ponteiros físicos de velocímetro.

Características:

- ponta fina;
- corpo vermelho/verde/laranja conforme o instrumento;
- centro circular;
- pequena sombra;
- movimento suave;
- animação com easing;
- nunca saltar instantaneamente de um valor para outro.

Exemplo:

```text
20% → 35%
```

O ponteiro deve fazer uma pequena animação:

```text
20 → 23 → 27 → 31 → 35
```

com movimento fluido.

Não usar animação exageradamente lenta.

A atualização real ocorre a cada aproximadamente 1 segundo.

---

# 14. Arco de progresso

Além do ponteiro, cada velocímetro possui um arco circular que representa visualmente o percentual.

CPU:

```text
verde
```

RAM:

```text
laranja
```

O arco acompanha o valor atual.

Exemplo:

CPU 28%:

```text
██████████░░░░░░░░░░░░░░░
```

Visualmente isso deve ser representado por um arco circular, não por uma barra horizontal.

O arco deve ter uma pequena luminosidade.

---

# 15. Responsividade

O alvo principal é:

```text
Xiaomi Mi 9T
Landscape
```

A interface precisa ocupar a tela inteira.

Não deve existir:

- scroll;
- conteúdo cortado;
- barras de rolagem;
- excesso de padding;
- elementos que ultrapassem a tela.

O design deve continuar funcional em outras telas horizontais, mas o Mi 9T é a referência principal.

---

# 16. Performance

Este é um painel de monitoramento local.

Ele será atualizado aproximadamente a cada segundo.

Priorizar:

- HTML;
- CSS;
- SVG;
- JavaScript;
- Vue 3.5.

Evitar bibliotecas pesadas apenas para desenhar os velocímetros.

Idealmente os velocímetros devem ser feitos com **SVG puro**.

Não usar imagens rasterizadas para os instrumentos se SVG/CSS puderem reproduzir o efeito.

O painel precisa consumir pouquíssimos recursos porque estará rodando continuamente em um celular antigo.

---

# 17. Integração com os dados

A interface deve ser desacoplada da origem dos dados.

A UI deve receber algo conceitualmente equivalente a:

```js
{
  cpu: 28.5,
  ram: 68.2
}
```

E atualizar:

```text
CPU:
- percentual usado
- percentual disponível
- posição do ponteiro
- arco

RAM:
- percentual usado
- percentual disponível
- posição do ponteiro
- arco
```

A comunicação real será feita pelo WebSocket já existente no projeto.

Não implementar polling adicional para buscar CPU/RAM.

---

# 18. Arquitetura sugerida em Vue

Sugestão:

```text
MonitorView.vue
│
├── MonitorHeader.vue
│
├── SportGauge.vue
│   ├── CPU
│   └── RAM
│
└── MonitorFooter.vue
```

Ou, se fizer sentido simplificar:

```text
MonitorView.vue
└── SportGauge.vue
```

O componente `SportGauge` deve receber propriedades semelhantes a:

```js
{
  label: "CPU",
  value: 28.5,
  color: "green",
  available: 71.5
}
```

Para RAM:

```js
{
  label: "RAM",
  value: 68.2,
  color: "orange",
  available: 31.8
}
```

---

# 19. Animações

As animações devem ser sutis.

Quando o valor muda:

- ponteiro se move suavemente;
- arco acompanha;
- número muda;
- nenhum elemento inteiro da página deve piscar;
- não reconstruir o DOM inteiro;
- evitar animações CSS contínuas desnecessárias.

A interface deve parecer viva, mas não chamativa.

---

# 20. Hierarquia visual

A ordem de importância é:

### Nível 1 — extremamente importante

```text
28%
68%
```

### Nível 2

```text
CPU
RAM
```

### Nível 3

```text
72% DISPONÍVEL
32% DISPONÍVEL
```

### Nível 4

```text
GHz
temperatura
GB
sistema
uptime
```

Se houver conflito por falta de espaço, remover os dados de nível 4 antes de reduzir os velocímetros.

---

# 21. Regra mais importante

**Não transformar o painel em um dashboard cheio de informações.**

O objetivo é que o usuário olhe para a tela por 1 segundo e saiba:

```text
CPU: tranquilo ou sobrecarregada?
RAM: tranquila ou sobrecarregada?
```

O restante é decoração funcional.

O painel deve parecer um **cockpit de supercarro minimalista**, não um painel de administração.

---

# 22. Referência visual resumida

Imagine dois grandes instrumentos analógicos de um supercarro moderno:

```text
             PMW MONITOR                         ● ONLINE

       ╭─────────────────╮              ╭─────────────────╮
      ╱                   ╲            ╱                   ╲
     │   0  20  40  60 80 │          │ 80 60 40 20  0     │
     │                     │          │                     │
     │         ╲           │          │           ╱         │
     │          ╲          │          │          ╱          │
     │           ●         │          │         ●           │
     │                     │          │                     │
     │        28%          │          │        68%          │
     │        CPU          │          │        RAM          │
     │      USO ATUAL      │          │      USO ATUAL      │
      ╲                   ╱            ╲                   ╱
       ╰─────────────────╯              ╰─────────────────╯

                72% DISP.                       32% DISP.

                         Ubuntu 26.04 LTS
```

A implementação deve reproduzir essa sensação de **instrumentação automotiva premium**, mantendo a interface leve, funcional e adequada para uma tela pequena em landscape.

---

# 23. Relação com o HTML de referência

O HTML fornecido junto com esta especificação é uma **referência funcional inicial**.

Preservar:

- conceito dos dois gauges;
- CPU à esquerda;
- RAM à direita;
- atualização dinâmica;
- SVG;
- ponteiro;
- arco;
- responsividade landscape;
- baixo custo de renderização.

Pode melhorar:

- acabamento visual;
- escala;
- ponteiros;
- tipografia;
- textura;
- profundidade;
- proporções;
- espaçamento;
- aparência automotiva.

Não voltar para o conceito original de cards administrativos.

O resultado final deve parecer uma evolução do HTML de referência para um **cockpit automotivo premium**.
