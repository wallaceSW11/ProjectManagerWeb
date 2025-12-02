# Plano de Implementação - Gerenciamento de IDEs

- [x] 1. Configurar estrutura base do backend


  - Criar DTOs para IDE e Migration
  - Criar interfaces e estrutura de pastas
  - _Requisitos: 1.1, 2.1_

- [x] 1.1 Criar IDEDTO.cs


  - Implementar record com Identificador, Nome e ComandoParaExecutar
  - Adicionar validações de atributos se necessário
  - _Requisitos: 1.1_

- [x] 1.2 Criar DTOs de Migration


  - Implementar MigrationRecordDTO com Name e ExecutedAt
  - Implementar MigrationsDTO com lista de ExecutedMigrations
  - _Requisitos: 2.1, 5.4_

- [x] 1.3 Atualizar ComandoDTO


  - Adicionar campo IDEIdentificador (Guid?)
  - Manter campo AbrirNoVSCode temporariamente para compatibilidade
  - _Requisitos: 3.2_

- [x] 2. Implementar IDEJsonService


  - Criar serviço seguindo padrão do RepositorioJsonService
  - Implementar CRUD completo com SemaphoreSlim
  - Adicionar método para verificar referências
  - _Requisitos: 1.1, 1.2, 1.3, 1.4, 6.1_

- [x] 2.1 Implementar métodos de leitura


  - GetAllAsync() - retorna lista de IDEs
  - GetByIdAsync(Guid) - retorna IDE específica
  - _Requisitos: 1.4_

- [x] 2.2 Implementar métodos de escrita

  - AddAsync(IDEDTO) - adiciona nova IDE
  - UpdateAsync(Guid, IDEDTO) - atualiza IDE existente
  - DeleteAsync(Guid) - remove IDE
  - _Requisitos: 1.1, 1.2, 1.3_

- [x] 2.3 Implementar validação de integridade referencial


  - IsReferencedByProjectsAsync(Guid) - verifica se IDE está em uso
  - Integrar com RepositorioJsonService para buscar referências
  - _Requisitos: 6.1, 6.5_

- [ ]* 2.4 Escrever teste de propriedade para persistência de criação
  - **Propriedade 1: Persistência de criação de IDE**
  - **Valida: Requisito 1.1**

- [ ]* 2.5 Escrever teste de propriedade para preservação de identificador
  - **Propriedade 2: Preservação de identificador na atualização**
  - **Valida: Requisito 1.2**

- [ ]* 2.6 Escrever teste de propriedade para remoção efetiva
  - **Propriedade 3: Remoção efetiva de IDE**
  - **Valida: Requisito 1.3**

- [ ]* 2.7 Escrever teste de propriedade para completude da listagem
  - **Propriedade 4: Completude da listagem**
  - **Valida: Requisito 1.4**

- [ ]* 2.8 Escrever teste de propriedade para integridade referencial
  - **Propriedade 12: Integridade referencial na exclusão**
  - **Valida: Requisito 6.1**

- [x] 3. Implementar IDEController


  - Criar controller com rotas RESTful
  - Implementar injeção de dependência do IDEJsonService
  - Adicionar tratamento de erros apropriado
  - _Requisitos: 1.1, 1.2, 1.3, 1.4, 6.1_

- [x] 3.1 Implementar endpoints de leitura


  - GET /api/ides - listar todas
  - GET /api/ides/{id} - buscar por ID
  - _Requisitos: 1.4_


- [ ] 3.2 Implementar endpoints de escrita
  - POST /api/ides - criar nova IDE
  - PUT /api/ides/{id} - atualizar IDE
  - DELETE /api/ides/{id} - excluir IDE

  - _Requisitos: 1.1, 1.2, 1.3_

- [ ] 3.3 Adicionar validações e tratamento de erros
  - Validar campos obrigatórios
  - Retornar 409 Conflict se IDE estiver em uso
  - Retornar 404 Not Found se IDE não existir
  - _Requisitos: 6.1_

- [x]* 3.4 Escrever testes unitários para IDEController


  - Testar todos os endpoints
  - Testar validações e códigos de status HTTP
  - Testar tratamento de erros

- [x] 4. Implementar MigrationService


  - Criar serviço de migration com controle de execução
  - Implementar migration 001_AddIDEs
  - Garantir thread-safety e idempotência
  - _Requisitos: 2.1, 2.2, 2.3, 2.4, 5.1, 5.2, 5.3, 5.4_



- [ ] 4.1 Criar estrutura base do MigrationService
  - Implementar leitura/escrita de migrations.json
  - Criar método IsMigrationExecutedAsync
  - Criar método RecordMigrationAsync


  - _Requisitos: 5.4_

- [ ] 4.2 Implementar Migration_001_AddIDEs
  - Criar 3 IDEs padrão (VS Code, Kiro, Delphi)
  - Buscar todos os repositórios
  - Atualizar projetos: converter abrirNoVSCode para ideIdentificador
  - Preservar todos os outros dados dos repositórios
  - _Requisitos: 2.1, 2.3, 2.4_

- [ ] 4.3 Implementar ExecuteMigrationsAsync
  - Verificar migrations pendentes
  - Executar em ordem
  - Registrar execução bem-sucedida
  - Logar erros e continuar em caso de falha
  - _Requisitos: 5.1, 5.2, 5.3_

- [x]* 4.4 Escrever teste de propriedade para idempotência


  - **Propriedade 5: Idempotência da migration**
  - **Valida: Requisito 2.2**

- [ ]* 4.5 Escrever teste de propriedade para preservação de dados
  - **Propriedade 6: Preservação de dados na migration**


  - **Valida: Requisito 2.3**

- [x]* 4.6 Escrever teste de exemplo para migration padrão


  - Verificar criação das 3 IDEs padrão
  - Verificar atualização de projetos para VS Code
  - **Valida: Requisitos 2.1, 2.4**





- [ ] 5. Integrar MigrationService no Program.cs
  - Registrar MigrationService no DI container
  - Executar migrations na inicialização da aplicação
  - Adicionar logging apropriado


  - _Requisitos: 5.1, 5.2_

- [x] 5.1 Registrar serviços no DI

  - Adicionar IDEJsonService como Singleton
  - Adicionar MigrationService como Singleton
  - _Requisitos: 1.1_

- [ ] 5.2 Executar migrations no startup
  - Chamar ExecuteMigrationsAsync após Build()
  - Adicionar try-catch para não bloquear inicialização
  - Logar início e fim das migrations
  - _Requisitos: 5.1, 5.2, 5.3_

- [x] 6. Checkpoint Backend - Garantir que testes passam



  - Garantir que todos os testes passam, perguntar ao usuário se surgem dúvidas

- [ ] 7. Atualizar RepositorioJsonService para suporte a IDEs
  - Adicionar método para enriquecer projetos com dados de IDE
  - Modificar GetAllAsync para incluir informações de IDE


  - _Requisitos: 6.4_


- [ ] 7.1 Criar método GetIDEForProjectAsync
  - Receber identificador de IDE
  - Retornar dados completos da IDE (nome, comando)
  - _Requisitos: 6.4_



- [ ] 7.2 Enriquecer resposta de repositórios
  - Ao retornar repositórios, incluir dados de IDE nos projetos


  - Tratar casos onde IDE não existe mais (dados órfãos)
  - _Requisitos: 6.2, 6.4_

- [ ]* 7.3 Escrever teste de propriedade para dados completos
  - **Propriedade 14: Inclusão de dados completos da IDE**


  - **Valida: Requisito 6.4**

- [ ]* 7.4 Escrever teste de propriedade para validação de identificadores
  - **Propriedade 15: Validação de identificadores de IDE**
  - **Valida: Requisito 6.5**



- [ ] 8. Criar estrutura base do frontend
  - Criar interfaces TypeScript
  - Criar modelo IDEModel

  - Criar serviço IDEsService
  - _Requisitos: 1.1, 4.1_

- [ ] 8.1 Criar interface IIDE em types/index.ts
  - Adicionar identificador, nome, comandoParaExecutar

  - _Requisitos: 1.1_

- [ ] 8.2 Atualizar interface IComando
  - Adicionar campo ideIdentificador (string | null)
  - Manter abrirNoVSCode temporariamente

  - _Requisitos: 3.2_

- [ ] 8.3 Criar IDEModel.ts
  - Implementar classe seguindo padrão de RepositorioModel
  - Adicionar construtor e método toDTO()
  - _Requisitos: 1.1_

- [ ] 8.4 Criar IDEsService.ts
  - Estender BaseApiService
  - Implementar getIDEs, adicionarIDE, atualizarIDE, excluirIDE


  - _Requisitos: 1.1, 1.2, 1.3, 1.4_



- [ ]* 8.5 Escrever testes unitários para IDEsService
  - Testar transformação de DTOs
  - Testar chamadas de API corretas

- [x] 9. Criar página de gerenciamento de IDEs


  - Criar IDEsView.vue com lista de cards
  - Implementar modal para cadastro/edição
  - Adicionar rota no Vue Router


  - _Requisitos: 4.1, 4.2, 4.3, 4.4, 4.5_

- [ ] 9.1 Criar IDEsView.vue
  - Implementar estrutura base com v-container
  - Adicionar botão "Adicionar" no topo


  - Criar área para lista de cards
  - _Requisitos: 4.1, 4.2_

- [ ] 9.2 Implementar lista de cards de IDEs
  - Criar v-row com v-col para cada IDE
  - Exibir nome e comando em cada card


  - Adicionar botões de editar e excluir
  - _Requisitos: 4.1_

- [ ] 9.3 Implementar modal de cadastro/edição
  - Usar componente ModalPadrao

  - Adicionar campos para nome e comando
  - Implementar validação de campos obrigatórios
  - _Requisitos: 4.2, 4.3, 4.4_


- [ ] 9.4 Implementar lógica de CRUD
  - Carregar IDEs no onMounted
  - Implementar funções de criar, editar e excluir
  - Adicionar confirmação antes de excluir
  - Exibir notificações de sucesso/erro

  - _Requisitos: 1.1, 1.2, 1.3, 4.5_

- [ ]* 9.5 Escrever teste de propriedade para validação de formulário
  - **Propriedade 10: Validação de campos obrigatórios**
  - **Valida: Requisito 4.4**

- [ ] 9.6 Adicionar rota no router
  - Adicionar rota /ides apontando para IDEsView
  - Adicionar item no menu de navegação
  - _Requisitos: 4.1_



- [x] 10. Atualizar cadastro de projeto para seleção de IDE



  - Modificar ProjetoCadastro.vue
  - Substituir checkbox por select de IDE
  - Carregar lista de IDEs disponíveis
  - _Requisitos: 3.1, 3.2_


- [ ] 10.1 Carregar lista de IDEs no componente
  - Buscar IDEs via IDEsService no onMounted
  - Armazenar em ref reativa
  - _Requisitos: 3.1_


- [ ] 10.2 Substituir checkbox por v-select
  - Remover campo abrirNoVSCode
  - Adicionar v-select com lista de IDEs
  - Configurar item-title="nome" e item-value="identificador"
  - Adicionar propriedade clearable

  - _Requisitos: 3.1, 3.2_

- [ ] 10.3 Atualizar ProjetoModel
  - Modificar interface de comandos
  - Garantir compatibilidade com backend
  - _Requisitos: 3.2_


- [ ]* 10.4 Escrever teste de propriedade para persistência de associação
  - **Propriedade 7: Persistência de associação projeto-IDE**
  - **Valida: Requisito 3.2**


- [ ] 11. Atualizar exibição de projetos na lista de pastas
  - Modificar componente que exibe botão de abrir IDE
  - Buscar nome da IDE associada
  - Exibir "Abrir no {Nome da IDE}" ou ocultar se vazio
  - _Requisitos: 3.3, 3.4, 6.2_

- [ ] 11.1 Identificar componente de exibição de pastas
  - Localizar onde o botão "Abrir no VS Code" é renderizado
  - Analisar estrutura atual
  - _Requisitos: 3.4_

- [ ] 11.2 Modificar lógica de exibição do botão
  - Verificar se projeto tem ideIdentificador
  - Se vazio/null, não exibir botão
  - Se preenchido, buscar nome da IDE e exibir
  - _Requisitos: 3.3, 3.4_

- [ ] 11.3 Tratar casos de IDE não encontrada
  - Se ideIdentificador existe mas IDE foi excluída
  - Exibir "IDE não configurada" ou ocultar botão
  - _Requisitos: 6.2_

- [ ]* 11.4 Escrever teste de propriedade para renderização de nome
  - **Propriedade 8: Renderização correta do nome da IDE**
  - **Valida: Requisito 3.4**

- [ ]* 11.5 Escrever teste de propriedade para atualização em cascata
  - **Propriedade 13: Atualização em cascata de nomes**
  - **Valida: Requisito 6.3**

- [ ] 12. Checkpoint Final - Garantir que todos os testes passam
  - Garantir que todos os testes passam, perguntar ao usuário se surgem dúvidas

- [ ] 13. Testes de integração e ajustes finais
  - Testar fluxo completo de ponta a ponta
  - Verificar migration em ambiente limpo
  - Ajustar estilos e UX conforme necessário
  - _Requisitos: Todos_

- [ ] 13.1 Testar fluxo de CRUD de IDEs
  - Criar, editar, listar e excluir IDEs via UI
  - Verificar persistência e atualização em tempo real
  - _Requisitos: 1.1, 1.2, 1.3, 1.4_

- [ ] 13.2 Testar fluxo de migration
  - Limpar banco de dados
  - Iniciar aplicação
  - Verificar criação de IDEs padrão
  - Reiniciar e verificar não duplicação
  - _Requisitos: 2.1, 2.2, 2.3, 2.4_

- [ ] 13.3 Testar fluxo de associação projeto-IDE
  - Criar nova IDE
  - Criar/editar projeto e associar IDE
  - Verificar exibição correta na lista de pastas
  - Testar abertura do projeto na IDE
  - _Requisitos: 3.1, 3.2, 3.3, 3.4, 3.5_

- [ ] 13.4 Testar integridade referencial
  - Criar IDE e associar a projeto
  - Tentar excluir IDE
  - Verificar bloqueio e mensagem de erro
  - _Requisitos: 6.1_

- [ ] 13.5 Ajustes de UX e polish
  - Revisar estilos e layout
  - Adicionar loading states
  - Melhorar mensagens de erro
  - Adicionar tooltips se necessário
  - _Requisitos: Todos_
