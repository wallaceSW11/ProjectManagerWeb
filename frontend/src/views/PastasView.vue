<template>
  <v-container>
    <div class="d-flex flex-column">
      <v-row no-gutters>
        <v-col cols="8" class="d-flex justify-space-between">
          <h2>Pastas</h2>

          <v-btn @click="carregarPastas" size="small">
            <v-icon>mdi-refresh</v-icon>
            <v-tooltip
              location="top"
              text="Atualizar listagem"
              activator="parent"
            />
          </v-btn>
        </v-col>

        <v-col class="ml-4 d-flex align-center justify-space-between">
          <div>
            <h3>Projetos / Ações</h3>
          </div>
          <div class="pr-3">
            <v-tooltip text="Desmarcar todos">
              <template #activator="{ props }">
                <v-icon
                  v-bind="props"
                  size="16px"
                  @click="
                    pastaSelecionada.projetos.forEach(
                      (projeto) => (projeto.comandosSelecionados = [])
                    )
                  "
                  >mdi-close-box-multiple-outline</v-icon
                >
              </template>
            </v-tooltip>
          </div>
        </v-col>
      </v-row>

      <v-row no-gutters class="d-flex">
        <v-col cols="8" class="altura-limitada mr-2">
          <div v-if="pastas.length === 0">
            Não há pastas no diretório raiz informado.
            <p>{{ configuracaoStore.diretorioRaiz }}</p>
          </div>

          <v-card
            v-else
            v-for="pasta in pastas"
            :key="pasta.diretorio"
            :class="[
              'mb-4 mr-2',
              {
                'card-selecionado':
                  pastaSelecionada.diretorio === pasta.diretorio,
              },
            ]"
            @click="selecionarPasta(pasta)"
          >
            <v-card-title>
              <div class="d-flex justify-space-between">
                <div>
                  {{ descricaoPasta(pasta) }}
                </div>

                <div v-if="!pasta.identificador">
                  <v-tooltip text="Cadastrar pasta">
                    <template #activator="{ props }">
                      <v-btn
                        v-bind="props"
                        icon
                        size="small"
                        variant="flat"
                        @click="exibirCadastroPasta(pasta)"
                      >
                        <v-icon small>mdi-plus</v-icon>
                      </v-btn>
                    </template>
                  </v-tooltip>
                </div>

                <div v-if="pasta.menus.length > 0">
                  <v-menu location="bottom">
                    <template #activator="{ props }">
                      <v-btn v-bind="props" icon size="small" variant="flat">
                        <v-icon small>mdi-dots-vertical</v-icon>
                      </v-btn>
                    </template>

                    <v-list dense>
                      <v-list-item
                        v-for="menu in pasta.menus"
                        :key="menu.identificador"
                        @click="executarMenu(pasta, menu.identificador)"
                      >
                        <v-list-item-title>
                          <v-icon>mdi-folder</v-icon>
                          {{ menu.titulo }}
                        </v-list-item-title>
                      </v-list-item>
                    </v-list>
                  </v-menu>
                </div>
              </div>
            </v-card-title>

            <v-card-text>
              <v-row no-gutters>
                <v-col cols="12" class="pb-1">
                  <v-icon>mdi-folder</v-icon>
                  {{ pasta.diretorio }}
                </v-col>

                <v-col cols="12" class="pt-1">
                  <v-icon>mdi-git</v-icon>
                  {{ pasta.branch }}
                </v-col>
              </v-row>
            </v-card-text>
          </v-card>
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
                <v-card class="mb-2" style="background-color: #2d2d30">
                  <v-card-title>
                    <div class="d-flex justify-space-between">
                      <div>
                        {{ projeto.nome }}
                      </div>

                      <div>
                        <v-menu location="bottom">
                          <template #activator="{ props }">
                            <v-btn
                              v-bind="props"
                              icon
                              size="small"
                              variant="flat"
                            >
                              <v-icon small>mdi-dots-vertical</v-icon>
                            </v-btn>
                          </template>

                          <v-list dense>
                            <v-list-item
                              v-for="menu in menusProjetos"
                              :key="menu.identificador"
                              @click="menu.acao(projeto)"
                            >
                              <v-list-item-title>
                                <v-icon class="pr-1">{{ menu.icone }}</v-icon>
                                {{ menu.titulo }}
                              </v-list-item-title>
                            </v-list-item>
                          </v-list>
                        </v-menu>
                      </div>
                    </div>
                  </v-card-title>

                  <v-card-text>
                    <v-checkbox
                      v-for="(comando, indice) in projeto.comandos"
                      :key="indice"
                      :label="comando"
                      :value="comando"
                      v-model="projeto.comandosSelecionados"
                      hide-details
                      height="40px"
                    />
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
                    (p) => p.comandosSelecionados.length > 0
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

  <CadastroPasta v-model="exibirModalPasta" :pasta="pastaSelecionada" />
</template>

<script setup>
import { onMounted, onUnmounted, reactive, ref } from "vue";
import PastasService from "@/services/PastasService.js";
import { useRouter } from "vue-router";
import PastaModel from "@/models/PastaModel";
import ComandosService from "@/services/ComandosService";
import emitter, {
  carregandoAsync,
  notificar,
  atualizarListaPastas,
} from "@/utils/eventBus";
import CadastroPasta from "@/components/pastas/PastaCadastro.vue";

const pastas = reactive([]);
const router = useRouter();
const pastaSelecionada = reactive(new PastaModel());
const exibirModalPasta = ref(false);

import { useConfiguracaoStore } from "@/stores/configuracao";

const configuracaoStore = useConfiguracaoStore();

const carregarPastasListener = () => {
  carregarPastas();
};

onMounted(async () => {
  await inicializarPagina();

  emitter.on("atualizarListaPastas", carregarPastasListener);
});

onUnmounted(() => {
  emitter.off("atualizarListaPastas", carregarPastasListener);
});

const inicializarPagina = async () => {
  if (redirecionarParaConfiguracaoInicial()) return;

  await carregarPastas();

  selecionarPastaSalva();
};

const redirecionarParaConfiguracaoInicial = () => {
  if (!configuracaoStore.diretorioRaiz) {
    alert(
      "O diretório raiz não foi informado, será redirecionado para a tela de configurações"
    );

    router.push({ name: "configuracao" });

    return true;
  }

  return false;
};

const selecionarPastaSalva = () => {
  if (!pastas.length) return;

  const diretorioSalvo = consultarPastaSelecionadaDoStorage();

  if (!diretorioSalvo) {
    selecionarPasta(pastas[0]);

    return;
  }

  const selecionada = pastas.find((p) => p.diretorio === diretorioSalvo);

  if (!selecionada) return;

  const indice = pastas.findIndex((p) => p.diretorio === diretorioSalvo);

  indice !== -1 && selecionarPasta(pastas[indice]);
};

const carregarPastas = async () => {
  Object.assign(pastas, []);
  try {
    const resposta = await carregandoAsync(async () => {
      return await PastasService.getPastas();
    });
    Object.assign(pastas, resposta);
  } catch (error) {
    console.error("Falha ao obter as pastas", error);
    notificar("erro", "Falha ao obter as pastas");
  }
};

const descricaoPasta = (pasta) => {
  return pasta.codigo
    ? `${pasta.codigo} - ${pasta.descricao}`
    : pasta.descricao;
};

const selecionarPasta = (pasta) => {
  Object.assign(pastaSelecionada, pasta);

  const acoes = consultarAcoesSelecionadas();

  if (pastaSelecionada.projetos) {
    pastaSelecionada.projetos.forEach((projeto) => {
      const comandos = acoes
        ?.find((a) => a.diretorio === pasta.diretorio)
        ?.projetos.find((p) => p.nome === projeto.nome)?.comandos;

      projeto.comandosSelecionados = comandos || [];
    });
  }

  salvarPastaSelecionadaNoStorage();
};

const CHAVE_PASTA_SELECIONADA = "PastaSelecionada";

const salvarPastaSelecionadaNoStorage = () => {
  localStorage.setItem(
    CHAVE_PASTA_SELECIONADA,
    JSON.stringify(pastaSelecionada.diretorio)
  );
};

const consultarPastaSelecionadaDoStorage = () => {
  const pasta = localStorage.getItem(CHAVE_PASTA_SELECIONADA);

  if (!pasta) return;

  return JSON.parse(pasta);
};

const executarAcoes = async () => {
  const payload = {
    diretorio: pastaSelecionada.diretorio,
    repositorioId: pastaSelecionada.repositorioId,
    projetos: pastaSelecionada.projetos
      .filter((p) => p.comandosSelecionados.length > 0)
      .map((p) => {
        return {
          identificador: p.identificador,
          nome: p.nome,
          comandos: p.comandosSelecionados,
          identificadorRepositorioAgregado: p.identificadorRepositorioAgregado,
        };
      }),
  };

  try {
    await carregandoAsync(async () => {
      await ComandosService.executarComando(payload);
    });
    salvarAcoesSelecionadas(payload);
    notificar("sucesso", "Comando solicitado");
  } catch (error) {
    console.error("Falha ao executar as acoes: ", error);
    notificar("erro", "Falha ao executar a ação", error);
  }
};

const CHAVE_ACOES_SELECIONADAS = "AcoesSelecionadas";

const salvarAcoesSelecionadas = (payload) => {
  const salvarNoLocalStorage = (valor) => {
    localStorage.setItem(CHAVE_ACOES_SELECIONADAS, JSON.stringify(valor));
  };

  let acoes = consultarAcoesSelecionadas();

  if (!acoes?.length) {
    salvarNoLocalStorage([payload]);

    return;
  }

  const indice = acoes.findIndex((a) => a.diretorio === payload.diretorio);

  indice === -1 ? acoes.push(payload) : acoes.splice(indice, payload);

  salvarNoLocalStorage(acoes);
};

const consultarAcoesSelecionadas = () => {
  const acoes = localStorage.getItem(CHAVE_ACOES_SELECIONADAS);

  return acoes ? JSON.parse(acoes) : null;
};

const executarMenu = async (pasta, menuId) => {
  const payload = {
    diretorio: pasta.diretorio,
    repositorioId: pasta.repositorioId,
    comandoId: menuId,
  };

  try {
    await ComandosService.executarComandoMenu(payload);
  } catch (error) {
    console.error("Falha ao executar o menu: ", error);
  }
};

const exibirCadastroPasta = (pasta) => {
  exibirModalPasta.value = true;
  Object.assign(pastaSelecionada, pasta);
};

const menusProjetos = [
  {
    identificador: 1,
    titulo: "Desmarcar todos",
    icone: "mdi-close-box-multiple-outline",
    acao: (projeto) => {
      projeto.comandosSelecionados = [];
    },
  },
  {
    identificador: 2,
    titulo: "Abrir no Explorer",
    icone: "mdi-folder-open",
    acao: (projeto) => {
      const comando = `cd ${pastaSelecionada.diretorio}\\${projeto.nomeRepositorio}\\${projeto.nome}; explorer .; Exit;`;
      executarComandoAvulso(comando);
    },
  },
  {
    identificador: 3,
    titulo: "Abrir no PowerShell",
    icone: "mdi-console",
    acao: (projeto) => {
      const comando = `cd ${pastaSelecionada.diretorio}\\${projeto.nomeRepositorio}\\${projeto.nome}; pwsh.exe;`;
      executarComandoAvulso(comando);
    },
  },
];

const executarComandoAvulso = (comando) => {
  try {
    ComandosService.executarComandoAvulso({
      comando,
    });
  } catch (error) {
    console.error("Falha ao executar o comando avulso: ", error);
  }
};
</script>

<style scoped>
.altura-limitada {
  height: calc(100dvh - 140px);
  overflow: auto;
}

.card-selecionado {
  border: 1px solid orange;
}

:deep(.v-checkbox .v-selection-control) {
  min-height: 40px;
}

.altura-acoes {
  height: calc(100dvh - 140px);
}

.corpo-acoes {
  overflow: auto;
}
</style>
