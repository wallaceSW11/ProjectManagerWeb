# Documento de Requisitos

## Introdução

Esta funcionalidade introduz capacidades de gerenciamento de IDEs (Ambientes de Desenvolvimento Integrado) no ProjectManagerWeb. Os usuários poderão registrar múltiplas IDEs com seus respectivos comandos de execução, e associar projetos a IDEs específicas para abertura rápida. O sistema incluirá um mecanismo de migração para atualizar estruturas de dados existentes e pré-popular IDEs comuns.

## Glossário

- **IDE**: Ambiente de Desenvolvimento Integrado - Uma aplicação de software que fornece facilidades abrangentes para desenvolvimento de software
- **Sistema Backend**: A API Web .NET 9 que gerencia lógica de negócio e persistência de dados
- **Sistema Frontend**: A aplicação Vue 3 que fornece a interface do usuário
- **Repositório**: Um repositório Git gerenciado pelo sistema
- **Projeto**: Uma configuração dentro de um repositório que define como executar servidores de desenvolvimento
- **Migration**: Um processo de transformação de dados executado uma única vez que atualiza o esquema do banco de dados e dados existentes
- **Registro de IDEs**: O arquivo JSON (IDEs.json) que armazena configurações de IDEs registradas

## Requisitos

### Requisito 1

**História de Usuário:** Como desenvolvedor, eu quero registrar múltiplas IDEs com seus comandos de execução, para que eu possa abrir projetos rapidamente no meu ambiente de desenvolvimento preferido.

#### Critérios de Aceitação

1. QUANDO um usuário cria uma nova entrada de IDE ENTÃO o Sistema Backend DEVE armazenar a IDE com um identificador GUID único, nome e comando para executar
2. QUANDO um usuário atualiza uma entrada de IDE ENTÃO o Sistema Backend DEVE modificar o registro existente da IDE preservando seu identificador
3. QUANDO um usuário exclui uma entrada de IDE ENTÃO o Sistema Backend DEVE remover a IDE do registro
4. QUANDO um usuário recupera todas as IDEs ENTÃO o Sistema Backend DEVE retornar a lista completa de IDEs registradas
5. O Sistema Backend DEVE persistir dados de IDE no arquivo IDEs.json no diretório %ProgramData%\PMW\Banco\

### Requisito 2

**História de Usuário:** Como desenvolvedor, eu quero que o sistema pré-popule IDEs comuns durante a configuração inicial, para que eu não precise configurar manualmente ferramentas de desenvolvimento padrão.

#### Critérios de Aceitação

1. QUANDO o processo de migration executa ENTÃO o Sistema Backend DEVE criar três entradas de IDE padrão: VS Code (comando: "code ."), Kiro (comando: "kiro ."), e Delphi (comando: "bds -pDelphi -rBDSERP110203")
2. QUANDO o processo de migration executa múltiplas vezes ENTÃO o Sistema Backend DEVE executar a migration apenas uma vez para prevenir entradas duplicadas
3. QUANDO a migration atualiza dados de repositório ENTÃO o Sistema Backend DEVE preservar todas as informações existentes do repositório exceto os campos modificados
4. QUANDO a migration completa ENTÃO o Sistema Backend DEVE atualizar todos os projetos existentes para usar VS Code como IDE padrão

### Requisito 3

**História de Usuário:** Como desenvolvedor, eu quero associar um projeto a uma IDE específica, para que eu possa abrir o projeto no meu ambiente de desenvolvimento preferido com um clique.

#### Critérios de Aceitação

1. QUANDO um usuário configura um projeto ENTÃO o Sistema Frontend DEVE permitir seleção de uma IDE da lista de IDEs registradas
2. QUANDO um usuário salva um projeto com uma seleção de IDE ENTÃO o Sistema Backend DEVE armazenar o identificador da IDE na configuração do projeto
3. QUANDO um usuário deixa o campo de IDE em branco ENTÃO o Sistema Backend DEVE armazenar um valor vazio e o sistema NÃO DEVE exibir uma opção de abrir IDE
4. QUANDO exibindo um projeto com uma IDE associada ENTÃO o Sistema Frontend DEVE mostrar "Abrir no {Nome da IDE}" onde {Nome da IDE} é o nome da IDE selecionada
5. QUANDO um usuário clica no botão de abrir IDE ENTÃO o Sistema Backend DEVE executar o comando da IDE no diretório do projeto

### Requisito 4

**História de Usuário:** Como desenvolvedor, eu quero gerenciar IDEs através de uma interface dedicada, para que eu possa facilmente adicionar, editar ou remover configurações de IDE.

#### Critérios de Aceitação

1. QUANDO um usuário navega para a página de gerenciamento de IDEs ENTÃO o Sistema Frontend DEVE exibir todas as IDEs registradas como cards
2. QUANDO um usuário clica para adicionar uma nova IDE ENTÃO o Sistema Frontend DEVE abrir um modal com campos para nome e comando
3. QUANDO um usuário clica para editar uma IDE ENTÃO o Sistema Frontend DEVE abrir um modal pré-preenchido com os dados atuais da IDE
4. QUANDO um usuário submete o formulário de IDE ENTÃO o Sistema Frontend DEVE validar que nome e comando não estão vazios
5. QUANDO um usuário exclui uma IDE ENTÃO o Sistema Frontend DEVE solicitar confirmação antes de remover a IDE

### Requisito 5

**História de Usuário:** Como administrador do sistema, eu quero que a migration execute automaticamente durante o deployment, para que o esquema do banco de dados permaneça sincronizado com a versão da aplicação.

#### Critérios de Aceitação

1. QUANDO a aplicação inicia ENTÃO o Sistema Backend DEVE verificar se migrations precisam ser executadas
2. QUANDO uma migration está pendente ENTÃO o Sistema Backend DEVE executá-la antes de processar qualquer requisição de API
3. QUANDO uma migration falha ENTÃO o Sistema Backend DEVE registrar o erro e continuar a inicialização da aplicação
4. QUANDO uma migration tem sucesso ENTÃO o Sistema Backend DEVE registrar a migration como completa para prevenir re-execução
5. O Sistema Backend DEVE executar migrations de maneira thread-safe para prevenir execução concorrente

### Requisito 6

**História de Usuário:** Como desenvolvedor, eu quero que o sistema mantenha integridade referencial entre projetos e IDEs, para que eu não encontre erros quando uma IDE for excluída.

#### Critérios de Aceitação

1. QUANDO um usuário tenta excluir uma IDE que está referenciada por projetos ENTÃO o Sistema Backend DEVE prevenir a exclusão e retornar uma mensagem de erro
2. QUANDO exibindo projetos ENTÃO o Sistema Frontend DEVE tratar referências de IDE ausentes graciosamente mostrando uma mensagem padrão
3. QUANDO uma IDE é renomeada ENTÃO o Sistema Frontend DEVE refletir imediatamente o novo nome em todas as exibições de projeto
4. QUANDO recuperando dados de projeto ENTÃO o Sistema Backend DEVE incluir a informação completa da IDE (não apenas o identificador)
5. O Sistema Backend DEVE validar identificadores de IDE ao salvar configurações de projeto
