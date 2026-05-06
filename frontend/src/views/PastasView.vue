<template>
  <v-container>
    <div class="d-flex flex-column">
      <v-row no-gutters>
        <v-col
          cols="8"
          class="d-flex justify-space-between align-center pb-2"
        >
          <h2>Pastas <span v-if="pastas.length > 0" class="text-medium-emphasis text-body-1">({{ pastas.length }})</span></h2>

          <div class="d-flex align-center" style="gap: 8px;">
            <v-text-field
              ref="campoPesquisa"
              v-model="termoPesquisa"
              placeholder="Código ou descrição"
              density="compact"
              variant="underlined"
              hide-details
              clearable
              style="width: 220px; min-width: 220px; padding-right: 8px;"
              prepend-inner-icon="mdi-magnify"
              @keydown.esc="termoPesquisa = ''"
            />

            <IconeComTooltip
              icone="mdi-refresh"
              texto="Atualizar listagem"
              :acao="carregarPastas"
            />

            <PastasOcultas ref="pastasOcultas" @atualizar="carregarPastas" />
          </div>
        </v-col>

        <v-col class="ml-4 d-flex align-center justify-space-between">
          <div>
            <h3>Projetos / Ações</h3>
          </div>
          <div class="d-flex align-center gap-2 pr-3">
            <v-select
              v-if="perfisDisponiveis.length > 0"
              v-model="perfilSelecionadoId"
              :items="perfisDisponiveis"
              item-title="nome"
              item-value="identificador"
              placeholder="Aplicar perfil"
              density="compact"
              variant="underlined"
              hide-details
              clearable
              style="width: 180px; min-width: 180px; padding-right: 8px;"
              @update:modelValue="aplicarPerfil"
            />
            <v-tooltip text="Desmarcar todos">
              <template #activator="{ props }">
                <v-icon
                  v-bind="props"
                  size="16px"
                  @click="
                    pastaSelecionada.projetos.forEach(
                      projeto => (projeto.comandosSelecionados = [])
                    )
                  "
                >
                  mdi-close-box-multiple-outline
                </v-icon>
              </template>
            </v-tooltip>
          </div>
        </v-col>
      </v-row>

      <v-row
        no-gutters
        class="d-flex"
      >
        <v-col
          cols="8"
          class="altura-limitada mr-2"
        >
          <div v-if="pastas.length === 0">
            Não há pastas no diretório raiz informado.
            <p>{{ configuracaoStore.diretorioRaiz }}</p>
          </div>

          <div v-else>
            <draggable
              v-model="pastasExibidas"
              item-key="diretorio"
              :animation="200"
              group="pastas"
              class="drag-area"
              :disabled="!!termoPesquisa"
              @end="atualizarIndicesPastas"
            >
              <template #item="{ element }">
                <CardPasta
                  :pasta="element"
                  :pasta-selecionada="pastaSelecionada"
                  :cor="element.cor"
                  @selecionarPasta="selecionarPasta"
                  @exibir-cadastro-pasta="exibirCadastroPasta"
                  @executar-menu="executarMenu"
                  @executar-menus-multiplos="executarMenusMultiplos"
                  @abrirDiretorio="abrirDiretorio"
                  @abrirNaIDE="abrirPastaNaIDE"
                  @abrirKiroCli="abrirPastaKiroCli"
                  @ocultar-pasta="ocultarPasta"
                  @excluir-pasta="excluirPasta"
                />
              </template>
            </draggable>
          </div>
        </v-col>

        <v-col>
          <div
            class="d-flex flex-column ml-2"
            style="height: calc(100dvh - 140px)"
          >
            <div
              class="d-flex flex-grow-1 flex-column pr-2"
              style="overflow: auto"
            >
              <div v-if="pastaSelecionada?.projetos.length === 0">
                Não há projetos disponíveis.
              </div>

              <div
                v-else
                v-for="projeto in pastaSelecionada.projetos"
                :key="projeto.identificador"
              >
                <v-card
                  class="mb-2"
                  style="background-color: #2d2d30"
                >
                  <v-card-title class="pb-0 d-flex align-center">
                    <div
                      class="d-flex flex-grow-1 justify-space-between align-center"
                    >
                      <div>
                        <v-icon
                          @click="toggleExpandirProjeto(projeto)"
                          size="small"
                          class="mr-2"
                        >
                          {{
                            projeto.expandido
                              ? 'mdi-chevron-down'
                              : 'mdi-chevron-right'
                          }}
                        </v-icon>
                      </div>

                      <div
                        class="d-flex flex-grow-1 justify-space-between align-center"
                      >
                        <div class="d-flex align-center">
                          {{ projeto.nome }}
                          <v-chip
                            v-if="
                              !projeto.expandido &&
                              projeto.comandosSelecionados &&
                              projeto.comandosSelecionados.length > 0
                            "
                            color="primary"
                            size="x-small"
                            class="ml-2"
                            variant="elevated"
                          >
                            {{ projeto.comandosSelecionados?.length || 0 }}
                          </v-chip>
                        </div>

                        <div>
                          <v-menu location="bottom">
                            <template #activator="{ props }">
                              <v-btn
                                v-bind="props"
                                icon
                                size="small"
                                variant="text"
                              >
                                <v-icon small>mdi-dots-vertical</v-icon>
                              </v-btn>
                            </template>

                            <v-list dense>
                              <v-list-item
                                v-for="menu in menusProjetoDisponiveis(projeto)"
                                :key="menu.identificador"
                                @click="menu.acao(projeto)"
                              >
                                <v-list-item-title>
                                  <v-icon class="pr-1" color="primary">{{ menu.icone }}</v-icon>
                                  {{ menu.titulo }}
                                </v-list-item-title>
                              </v-list-item>
                            </v-list>
                          </v-menu>
                        </div>
                      </div>
                    </div>
                  </v-card-title>

                  <v-card-text class="pa-0 ma-0 ml-4 pr-4">
                    <v-expand-transition>
                      <div v-if="projeto.expandido">
                        <v-switch
                          v-for="comando in projeto.getComandosDisponiveis?.()"
                          :key="comando.valor"
                          :label="comando.titulo"
                          :value="comando.valor"
                          v-model="projeto.comandosSelecionados"
                          hide-details
                          height="40px"
                          color="primary"
                          density="compact"
                          :append-icon="iconeAcaoMenu(comando.valor)"
                          @click:append="() => executarAcaoMenuAvulso(projeto, comando.valor)"
                        />
                      </div>
                    </v-expand-transition>
                  </v-card-text>
                </v-card>
              </div>
            </div>

            <div class="d-flex shrink mt-1">
              <v-btn
                size="large"
                color="primary"
                width="100%"
                @click="executarAcoes"
                :disabled="
                  !pastaSelecionada.projetos.some(
                    p =>
                      p.comandosSelecionados &&
                      p.comandosSelecionados.length > 0
                  )
                "
              >
                <v-icon>mdi-lightning-bolt</v-icon>
                Executar
              </v-btn>
            </div>
          </div>
        </v-col>
      </v-row>
    </div>
  </v-container>

  <CadastroPasta
    v-model="exibirModalPasta"
    :pasta="pastaSelecionada"
  />
</template>

<script setup lang="ts">
  import { computed, onMounted, onUnmounted, reactive, ref } from 'vue';
  import type { IPasta, IProjeto, IRepositorio, IPerfilMarcacao } from '@/types';
  import PastasService from '@/services/PastasService';
  import PastaModel from '@/models/PastaModel';
  import ProjetoModel from '@/models/ProjetoModel';
  import ComandosService from '@/services/ComandosService';
  import RepositoriosService from '@/services/RepositoriosService';
  import RepositorioModel from '@/models/RepositorioModel';
  import emitter, { carregandoAsync, notificar } from '@/utils/eventBus';
  import CadastroPasta from '@/components/pastas/PastaCadastro.vue';
  import CardPasta from '@/components/pastas/CardPasta.vue';
  import PastasOcultas from '@/components/pastas/PastasOcultas.vue';  import draggable from 'vuedraggable';
  import { useConfiguracaoStore } from '@/stores/configuracao';
  import { TIPO_COMANDO } from '@/constants/geral-constants';

  interface MenuProjeto {
    identificador: number;
    titulo: string;
    icone: string;
    acao: (projeto: any) => void;
  }

  interface PayloadComando {
    diretorio: string;
    repositorioId: string;
    projetos: {
      identificador: string;
      nome: string;
      comandos: any[];
      identificadorRepositorioAgregado?: string;
      nomeRepositorio: string;
    }[];
  }

  interface PayloadMenuComando {
    diretorio: string;
    repositorioId: string;
    comandoId: string;
  }

  const pastas = ref<IPasta[]>([]);
  const pastaSelecionada = reactive<IPasta>(new PastaModel());
  const exibirModalPasta = ref<boolean>(false);
  const termoPesquisa = ref<string>('');
  const campoPesquisa = ref<InstanceType<typeof import('vuetify/components').VTextField> | null>(null);
  const pastasOcultas = ref<{ recarregar: () => Promise<void> } | null>(null);
  const configuracaoStore = useConfiguracaoStore();
  const repositorios = ref<IRepositorio[]>([]);
  const perfilSelecionadoId = ref<string | null>(null);

  const perfisDisponiveis = computed((): IPerfilMarcacao[] => {
    if (!pastaSelecionada.repositorioId) return [];
    const repo = repositorios.value.find(r => r.identificador === pastaSelecionada.repositorioId);
    return repo?.perfis || [];
  });

  const aplicarPerfil = (identificadorPerfil: string | null): void => {
    if (!identificadorPerfil) return;

    const perfil = perfisDisponiveis.value.find(p => p.identificador === identificadorPerfil);
    if (!perfil) return;

    pastaSelecionada.projetos.forEach((projeto: any) => {
      const marcacao = perfil.projetos.find(
        pp => pp.identificadorProjeto === projeto.identificador
      );
      projeto.comandosSelecionados = marcacao ? [...marcacao.comandos] : [];
    });

    perfilSelecionadoId.value = null;
  };

  const focarPesquisa = (event: KeyboardEvent): void => {
    if (event.ctrlKey && event.key === 'f') {
      event.preventDefault();
      campoPesquisa.value?.focus();
    }
  };

  const pastasExibidas = computed({
    get: () => {
      if (!termoPesquisa.value) return pastas.value;

      const termo = termoPesquisa.value.toLowerCase();
      return pastas.value.filter(
        (p: IPasta) =>
          p.codigo?.toLowerCase().includes(termo) ||
          p.descricao?.toLowerCase().includes(termo)
      );
    },
    set: (valor: IPasta[]) => {
      pastas.value = valor;
    },
  });

  const carregarPastasListener = (): void => {
    carregarPastas();
  };

  onMounted(async () => {
    await inicializarPagina();
    emitter.on('atualizarListaPastas', carregarPastasListener);
    window.addEventListener('keydown', focarPesquisa);
  });

  onUnmounted(() => {
    emitter.off('atualizarListaPastas', carregarPastasListener);
    window.removeEventListener('keydown', focarPesquisa);
  });

  const inicializarPagina = async (): Promise<void> => {
    await Promise.all([carregarPastas(), carregarRepositorios()]);
    selecionarPastaSalva();
  };

  const carregarRepositorios = async (): Promise<void> => {
    try {
      const resposta = await RepositoriosService.getRepositorios();
      repositorios.value = resposta.map((r: any) => new RepositorioModel(r));
    } catch (error) {
      console.error('Falha ao carregar repositórios:', error);
    }
  };

  const selecionarPastaSalva = (): void => {
    if (!pastas.value.length) return;

    const diretorioSalvo = consultarPastaSelecionadaDoStorage();

    if (!diretorioSalvo) {
      selecionarPasta(pastas.value[0]);
      return;
    }

    const selecionada = pastas.value.find(
      (p: IPasta) => p.diretorio === diretorioSalvo
    );

    if (!selecionada) return;

    const indice = pastas.value.findIndex(
      (p: IPasta) => p.diretorio === diretorioSalvo
    );

    indice !== -1 && selecionarPasta(pastas.value[indice]);
  };

  const carregarPastas = async (): Promise<void> => {
    pastas.value = [];
    try {
      const resposta = await carregandoAsync(async () => {
        return await PastasService.getPastas();
      });

      // Garantir que os projetos sejam instâncias de ProjetoModel
      pastas.value = resposta.map((pasta: any) => {
        const pastaModel = new PastaModel(pasta);
        pastaModel.projetos =
          pasta.projetos?.map((projeto: any) => new ProjetoModel(projeto)) || [];
        return pastaModel;
      });
    } catch (error) {
      console.error('Falha ao obter as pastas', error);
      notificar('erro', 'Falha ao obter as pastas');
    }
  };

  const selecionarPasta = (pasta: IPasta): void => {
    // Garantir que a pasta selecionada tenha projetos como instâncias de ProjetoModel
    const pastaComProjetosModels = new PastaModel(pasta);
    pastaComProjetosModels.projetos =
      pasta.projetos?.map((projeto: any) => new ProjetoModel(projeto)) || [];

    Object.assign(pastaSelecionada, pastaComProjetosModels);

    const acoes = consultarAcoesSelecionadas();

    if (pastaSelecionada.projetos) {
      pastaSelecionada.projetos.forEach((projeto: any) => {
        const comandos = acoes
          ?.find((a: any) => a.diretorio === pasta.diretorio)
          ?.projetos.find(
            (p: any) => p.identificador === projeto.identificador
          )?.comandos;

        projeto.comandosSelecionados = comandos || [];
      });
    }

    salvarPastaSelecionadaNoStorage();
  };

  const CHAVE_PASTA_SELECIONADA = 'PastaSelecionada';

  const salvarPastaSelecionadaNoStorage = (): void => {
    localStorage.setItem(
      CHAVE_PASTA_SELECIONADA,
      JSON.stringify(pastaSelecionada.diretorio)
    );
  };

  const consultarPastaSelecionadaDoStorage = (): string | null => {
    const pasta = localStorage.getItem(CHAVE_PASTA_SELECIONADA);

    if (!pasta) return null;

    return JSON.parse(pasta);
  };

  const executarAcoes = async (): Promise<void> => {
    const payload: PayloadComando = {
      diretorio: pastaSelecionada.diretorio,
      repositorioId: pastaSelecionada.repositorioId || '',
      projetos: pastaSelecionada.projetos
        .filter(
          (p: any) =>
            p.comandosSelecionados && p.comandosSelecionados.length > 0
        )
        .map((p: any) => {
          return {
            identificador: p.identificador,
            nome: p.nome,
            comandos: p.comandosSelecionados || [],
            identificadorRepositorioAgregado:
              p.identificadorRepositorioAgregado,
            nomeRepositorio: p.nomeRepositorio,
          };
        }),
    };

    try {
      salvarAcoesSelecionadas(payload);
      notificar('sucesso', 'Comando solicitado');
      
      await carregandoAsync(async () => {
        await ComandosService.executarComando(payload);
      });
    } catch (error) {
      console.error('Falha ao executar as acoes: ', error);
      notificar('erro', 'Falha ao executar a ação', String(error));
    }
  };

  const CHAVE_ACOES_SELECIONADAS = 'AcoesSelecionadas';

  const salvarAcoesSelecionadas = (payload: PayloadComando): void => {
    const salvarNoLocalStorage = (valor: PayloadComando[]): void => {
      localStorage.setItem(CHAVE_ACOES_SELECIONADAS, JSON.stringify(valor));
    };

    let acoes = consultarAcoesSelecionadas();

    if (!acoes?.length) {
      salvarNoLocalStorage([payload]);
      return;
    }

    const indice = acoes.findIndex(
      (a: any) => a.diretorio === payload.diretorio
    );

    indice === -1 ? acoes.push(payload) : acoes.splice(indice, 1, payload);

    salvarNoLocalStorage(acoes);
  };

  const consultarAcoesSelecionadas = (): any[] | null => {
    const acoes = localStorage.getItem(CHAVE_ACOES_SELECIONADAS);

    return acoes ? JSON.parse(acoes) : null;
  };

  const executarMenu = async (pasta: IPasta, menuId: string): Promise<void> => {
    const payload: PayloadMenuComando = {
      diretorio: pasta.diretorio,
      repositorioId: pasta.repositorioId || '',
      comandoId: menuId,
    };

    notificar('sucesso', 'Comando solicitado');

    try {
      await ComandosService.executarComandoMenu(payload);
    } catch (error) {
      console.error('Falha ao executar o menu: ', error);
      notificar('erro', 'Falha ao executar o menu', String(error));
    }
  };

  const executarMenusMultiplos = async (pasta: IPasta, menuIds: string[]): Promise<void> => {
    if (!menuIds.length) return;

    notificar('sucesso', `${menuIds.length} comando(s) solicitado(s)`);

    try {
      for (const menuId of menuIds) {
        const payload: PayloadMenuComando = {
          diretorio: pasta.diretorio,
          repositorioId: pasta.repositorioId || '',
          comandoId: menuId,
        };
        await ComandosService.executarComandoMenu(payload);
      }
    } catch (error) {
      console.error('Falha ao executar os menus: ', error);
      notificar('erro', 'Falha ao executar os menus', String(error));
    }
  };

  const exibirCadastroPasta = (pasta: IPasta): void => {
    exibirModalPasta.value = true;
    Object.assign(pastaSelecionada, pasta);
  };

  const menusProjetos: MenuProjeto[] = [
    {
      identificador: 1,
      titulo: 'Desmarcar todos',
      icone: 'mdi-close-box-multiple-outline',
      acao: (projeto: any) => {
        projeto.comandosSelecionados = [];
      },
    },
    {
      identificador: 2,
      titulo: 'Abrir no Explorer',
      icone: 'mdi-folder-open',
      acao: (projeto: any) => {
        let comando = '';

        if (projeto.identificadorRepositorioAgregado)
          comando = `cd ${pastaSelecionada.diretorio}\\${projeto.nomeRepositorio}\\${projeto.subdiretorio}; explorer .; Exit;`;

        comando = `cd ${pastaSelecionada.diretorio}\\${projeto.nomeRepositorio}\\${projeto.subdiretorio}; explorer .; Exit;`;
        executarComandoAvulso(comando);
      },
    },
    {
      identificador: 3,
      titulo: 'Abrir no PowerShell',
      icone: 'mdi-console',
      acao: (projeto: any) => {
        let comando = '';

        if (projeto.identificadorRepositorioAgregado)
          comando = `cd ${pastaSelecionada.diretorio}\\${projeto.nomeRepositorio}\\${projeto.subdiretorio}; pwsh.exe;`;

        comando = `cd ${pastaSelecionada.diretorio}\\${projeto.nomeRepositorio}\\${projeto.subdiretorio}; pwsh.exe;`;
        executarComandoAvulso(comando);
      },
    },
    {
      identificador: 4,
      titulo: 'Baixar histórico completo (git fetch --unshallow)',
      icone: 'mdi-source-branch-sync',
      acao: (projeto: any) => {
        const diretorio = `${pastaSelecionada.diretorio}\\${projeto.nomeRepositorio}`;
        executarComandoAvulso(`cd ${diretorio}; git fetch --unshallow;`);
      },
    },
  ];

  const menusProjetoDisponiveis = (projeto: any): MenuProjeto[] => {
    let menus = [...menusProjetos];

    if (projeto.arquivoCoverage) {
      menus.push({
        identificador: 5,
        titulo: 'Abrir coverage',
        icone: 'mdi-file-chart',
        acao: (projeto: any) => {
          const comando = `cd ${pastaSelecionada.diretorio}\\${projeto.nomeRepositorio}\\${projeto.arquivoCoverage}`;
          executarComandoAvulso(comando);
        },
      });
    }

    return menus;
  };

  const executarComandoAvulso = (comando: string): void => {
    try {
      ComandosService.executarComandoAvulso({
        comando,
      });
    } catch (error) {
      console.error('Falha ao executar o comando avulso: ', error);
    }
  };

  const atualizarIndicesPastas = async (): Promise<void> => {
    try {
      await PastasService.atualizarIndices(
        pastas.value
          .filter((p: IPasta) => p.identificador)
          .map((p: IPasta, index: number) => ({
            identificador: p.identificador,
            indice: index,
          }))
      );
    } catch (error) {
      console.error('Falha ao atualizar a ordem das pastas: ', error);
      notificar('erro', 'Falha ao atualizar a ordem das pastas');
    }
  };

  const toggleExpandirProjeto = async (projeto: any): Promise<void> => {
    const novoEstado = !projeto.expandido;

    try {
      await PastasService.atualizarExpandido({
        pastaId: pastaSelecionada.identificador || '',
        projetoId: projeto.identificador,
        expandido: novoEstado,
      });

      projeto.expandido = novoEstado;
    } catch (error) {
      console.error(
        'Falha ao atualizar o estado expandido do projeto: ',
        error
      );
      notificar('erro', 'Falha ao atualizar o estado do projeto');
    }
  };

  const iconeAcaoMenu = computed(() => (comando: string) => {
    return comando === TIPO_COMANDO.ABRIR_NA_IDE.valor
      ? 'mdi-open-in-new'
      : 'mdi-flash';
  });

  const executarAcaoMenuAvulso = async (projeto: IProjeto, comando: string) => {
    const projetoComando = Object.assign({}, projeto);

    projetoComando.comandos = [comando];

    const payload: PayloadComando = {
      diretorio: pastaSelecionada.diretorio,
      repositorioId: pastaSelecionada.repositorioId || '',
      projetos: [projetoComando],
    };

    notificar('sucesso', 'Comando solicitado');

    try {
      await carregandoAsync(async () => {
        await ComandosService.executarComando(payload);
      });
    } catch (error) {
      console.error('Falha ao executar a acao: ', error);
      notificar('erro', 'Falha ao executar a ação', String(error));
    }
  };

  const abrirDiretorio = (diretorio: string): void => {
    try {
      const comando = `cd ${diretorio}; explorer .; Exit;`;
      ComandosService.executarComandoAvulso({ comando });
      notificar('sucesso', 'Comando solicitado');
    } catch (error) {
      console.error('Falha ao abrir o diretório: ', error);
      notificar('erro', 'Falha ao abrir o diretório', String(error));
    }
  };

  const ocultarPasta = async (diretorio: string): Promise<void> => {
    try {
      await PastasService.ocultar(diretorio);
      pastas.value = pastas.value.filter((p: IPasta) => p.diretorio !== diretorio);
      notificar('sucesso', 'Pasta ocultada');
      await pastasOcultas.value?.recarregar();
    } catch (error) {
      console.error('Falha ao ocultar pasta:', error);
      notificar('erro', 'Falha ao ocultar pasta');
    }
  };

  const excluirPasta = async (diretorio: string): Promise<void> => {
    const confirmado = confirm(
      `Tem certeza que deseja excluir permanentemente a pasta?\n\n${diretorio}\n\nEsta ação não pode ser desfeita.`
    );
    if (!confirmado) return;

    try {
      await PastasService.excluir(diretorio);
      pastas.value = pastas.value.filter((p: IPasta) => p.diretorio !== diretorio);
      notificar('sucesso', 'Pasta excluída');
    } catch (error) {
      console.error('Falha ao excluir pasta:', error);
      notificar('erro', 'Falha ao excluir pasta', String(error));
    }
  };

  const abrirPastaNaIDE = async (pasta: IPasta): Promise<void> => {
    try {
      if (!pasta.ideIdentificador) {
        notificar('aviso', 'Nenhuma IDE configurada para este repositório');
        return;
      }

      let diretorioCompleto = pasta.nomeRepositorio 
        ? `${pasta.diretorio}\\${pasta.nomeRepositorio}`
        : pasta.diretorio;

      if (pasta.subdiretorio)
        diretorioCompleto += `\\${pasta.subdiretorio}`;

      await carregandoAsync(async () => {
        await ComandosService.abrirPastaIDE({
          diretorio: diretorioCompleto,
          ideIdentificador: pasta.ideIdentificador!,
          perfilVSCode: pasta.perfilVSCode,
        });
      });

      notificar('sucesso', 'Abrindo pasta na IDE');
    } catch (error) {
      console.error('Falha ao abrir na IDE: ', error);
      notificar('erro', 'Falha ao abrir na IDE', String(error));
    }
  };

  const abrirPastaKiroCli = (pasta: IPasta): void => {
    try {
      if (!pasta.cliComando) {
        notificar('aviso', 'Nenhuma CLI configurada para este repositório');
        return;
      }

      let diretorio = pasta.diretorio;
      if (pasta.subdiretorio)
        diretorio += `\\${pasta.subdiretorio}`;

      const comando = `cd ${diretorio}; ${pasta.cliComando}`;
      ComandosService.executarComandoAvulso({ comando });
      notificar('sucesso', `Abrindo ${pasta.cliComando}`);
    } catch (error) {
      notificar('erro', 'Falha ao abrir CLI', String(error));
    }
  };
</script>

<style scoped>
  .altura-limitada {
    height: calc(100dvh - 140px);
    overflow: auto;
  }

  .altura-acoes {
    height: calc(100dvh - 140px);
  }

  .corpo-acoes {
    overflow: auto;
  }

  :deep(.v-switch .v-label) {
    padding-left: 16px;
  }

  :deep(.v-switch .v-input__append .v-icon) {
    color: rgb(var(--v-theme-primary));
  }

  .drag-area {
    min-height: 50px;
  }
</style>
