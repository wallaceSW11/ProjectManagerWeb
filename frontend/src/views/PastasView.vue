<template>
  <v-container>
    <div class="d-flex flex-column">
      <div>
        <h2>Pastas</h2>
      </div>

      <v-row no-gutters class="d-flex altura-limitada">
        <v-col cols="8">
          <v-card
            v-for="pasta in pastas"
            :key="pasta.diretorio"
            :class="[
              'mb-2',
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

                <div>
                  <v-btn icon variant="outlined" size="small">
                    <v-icon small>mdi-dots-vertical</v-icon>
                  </v-btn>
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

        <v-col class="ml-2">
          <div class="altura-acoes d-flex flex-column">
            <div class="d-flex flex-grow-1 flex-column corpo-acoes">
              <v-card
                v-for="projeto in pastaSelecionada.projetos"
                :key="projeto.id"
                class="mb-2"
              >
                <v-card-title>
                  {{ projeto.nome }}
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

            <div class="d-flex">
              <v-btn
                size="large"
                color="primary"
                width="100%"
                @click="executarAcoes"
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
</template>

<script setup>
import { computed, onMounted, reactive, ref } from "vue";
import ConfiguracaoModel from "../models/ConfiguracaoModel";
import ConfiguracaoService from "../services/ConfiguracaoService";
import PastasService from "../services/PastasService";
import { useRouter } from "vue-router";
import PastaModel from "../models/PastaModel";
import ComandosService from "../services/ComandosService";

let configuracao = reactive(new ConfiguracaoModel());
const pastas = reactive([]);
const router = useRouter();
const pastaSelecionada = reactive(new PastaModel());

onMounted(async () => {
  await consultarConfiguracao();

  if (!configuracao.diretorioRaiz) {
    alert(
      "O diretório raiz não foi informado, será redirecionado para a tela de configurações"
    );

    router.push({ name: "configuracao" });

    return;
  }

  await carregarPastas();
  if (pastas.length > 0) selecionarPasta(pastas[0]); // mudar para pegar do storage
});

const consultarConfiguracao = async () => {
  try {
    let response = await ConfiguracaoService.getConfiguracao();
    Object.assign(configuracao, new ConfiguracaoModel(response));
  } catch (error) {
    console.log("Falha ao consultar as configurações", error);
  }
};

const carregarPastas = async () => {
  try {
    const resposta = await PastasService.getPastas();
    Object.assign(pastas, resposta);
  } catch (error) {
    console.error("Falha ao obter as pastas", error);
  }
};

const descricaoPasta = (pasta) => {
  return pasta.codigo
    ? `${pasta.codigo} - ${pasta.descricao}`
    : pasta.descricao;
};

const selecionarPasta = (pasta) => {
  Object.assign(pastaSelecionada, pasta);

  if (pastaSelecionada.projetos) {
    pastaSelecionada.projetos.forEach((projeto) => {
      if (!projeto.comandosSelecionados) projeto.comandosSelecionados = [];
    });
  }
};

const executarAcoes = async () => {
  const payload = {
    diretorio: pastaSelecionada.diretorio,
    gitId: pastaSelecionada.gitId,
    projetos: pastaSelecionada.projetos
      .filter((p) => p.comandosSelecionados.length > 0)
      .map((p) => {
        return {
          nome: p.nome,
          comandos: p.comandosSelecionados,
        };
      }),
  };

  try {
    await ComandosService.executarComando(payload);
  } catch (error) {
    console.error("Falha ao executar as acoes: ", error);
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
