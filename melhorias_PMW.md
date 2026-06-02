Melhorias PMW

## Pasta centralizadora

# A ideia é ter um CRUD para o nome da pasta centralizadora, ou seja, essa pasta vai ser a pasta dentro do diretório raiz.
 - Imagine o cenário: c:\tools\git.
 - A pasta centralizadora poderia ser: Forizi, DeepRocket e Pessoal.
 - Tendo o seguinte cenário: c:\tools\git\Forizi, c:\tools\git\DeepRocket, c:\tools\git\Pessoal.
 - Assim, quando fizer o clone, ele já vai fazer dentro das pastas respectivas.
 - A pasta centralizadora será cadastrada por Repositório, onde posso ter cenários em que dois ou mais repositórios usam a mesma pasta centralizadora.
 - Sugiro que seja adicionado o CRUD de pastas centralizadoras na tela de Configuração, que tenha um campo para informar o nome e o botão adicionar. Abaixo a tabela exibindo as pastas (nomes) cadastrados. Com o botão de Editar e Excluir.
 - Pasta não é obrigatória e ela vai trabalhar junto com o clone e com a visualização das pastas.


## Visualização das pastas.
 - Caso tenha cadastrado alguma pasta centralizadora, a lista principal de pastas terá um novo comportamento:
 - Entre o título de Pastas (contador) e o Pesquisar, terão abas com os nomes das pastas, assim vou conseguir facilmente listar cada pasta de cada pasta centralizadora.
 - Ou seja, tenho as 3 pastas: Forizi, DeepRocket e Pessoal.
 - Vou ter 3 abas, e o comportamento é apenas filtro, ou seja, na lista de pastas, vai filtrar de acordo com a pasta marcada. Se clicar em Forizi, apenas os clones da pasta Forizi, e assim por diante.

## No cadastro do Repositório, adicionar um CRUD para de código de tarefa
 - Quero informar quais tarefas aquele repositório vai atender para facilitar no momento de clone.
 - Aqui vamos ter um CRUD mais completo, pois vamos informar as iniciais do código de tarefa, e o que ele vai vir marcado (clonar agregados, criar branch remoto, e afins).
 - Exemplo: Quando for FATWEB, vai selecionar o repositório X, marcar para criar branch remoto e habilitar as opções de tipo de tarefa (feature, bugfix e hotfix).
 - Quando for TC, não marca nada e nem habilita o tipo de tarefa.
 - Ou seja, vamos deixar mais automatizado o clone que vai linkar o código da tarefa com o repositório e as marcações daquele Repositório.
 - Também o campo para informar qual é a branch principal (dev, develop, main)

## Melhorias na tela de clone
  - Em resumo, quero informar o código da tarefa e de acordo com o cadastro, já selecionar o repositorio e as devidas marcaçòes. Assim, só vou informar a descrição e clicar em clone. Mas, temos que deixar os campos atuais disponíveis caso queira ediar algo na tela.
  - pensei em ter os campos na seguinte ordem:
  Código da tarefa
  Descrição
  Repositório
  Branch
  Checkbox atuais (Salvar branch, clonar agregados, criar branch remoto, Baixar histórico completo)
  Tipo
  Clonar Cancelar.
  - Ao abrir a tela de clone, deve verificar o que tem no clipboard, se for no formato de código de tarefs, ou seja, Letras no começo e depois números, vai colar direto no campo, e assim já vai preencher os demais dados.

## Resumo da implementação
 - Quero ter a possibilidade de serparar os clones por grupos, que no caso serão pastas, ou seja, em uma máquina vou ter 3 pastas de repositórios do github de diferentes usuários, estes eu vou usar a configuração de ssh e cada pasta terá seu usuário.
 - Em outra máquina, vou ter separado por projeto, uma pasta para o faturamento, outra para a api, e outra para demais assuntos. Mas tbm quero ter a opção de continuar usando a pasta raiz para coisas genéricas.
 - Na rotina de clone, quero facilitar o clone apenas pelo código da tarefa que será colado do clipboard ou digitado manualmente, e após sair do campo, validar o que for pré-selecionado.


 ## Importante
 Não podemos deixar nenhum placeholder ou algo do tipo informando sobre Forizi, Deeprocket, Faturamento, API, ou afins. É meramente exemplos para vc entender o que vamos implementar
