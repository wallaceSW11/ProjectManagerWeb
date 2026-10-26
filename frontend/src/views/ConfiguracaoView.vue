<template>
  <v-container>
    <v-row no-gutters>
      <v-col cols="12">
        <div class="d-flex justify-space-between align-center">
          <div class="d-flex align-center ga-2">
            <h1>Configurações</h1>
            <IconeComTooltip
              icone="mdi-folder-open-outline"
              texto="Abrir local do banco de dados"
              :acao="abrirBancoPath"
              top
            />
          </div>
        </div>
      </v-col>

      <v-col cols="12">
        <v-tabs v-model="abaAtiva">
          <v-tab>Geral</v-tab>
          <v-tab>Pasta Centralizadora</v-tab>
          <v-tab>CLI de IA</v-tab>
          <v-tab>Monitoramento</v-tab>
        </v-tabs>

        <v-tabs-window
          v-model="abaAtiva"
          class="conteudo-aba"
        >
          <!-- Aba: Geral -->
          <v-tabs-window-item>
            <v-row no-gutters>
              <v-col
                cols="12"
                class="pt-4"
              >
                <v-text-field
                  label="Diretório raiz"
                  v-model="configuracao.diretorioRaiz"
                  @change="salvarConfiguracao"
                  autocomplete="off"
                  name="pmw-dir-raiz"
                />
              </v-col>

              <v-col
                cols="12"
                v-if="featuresStore.isLinux"
              >
                <v-select
                  label="Terminal"
                  v-model="configuracao.terminalLinux"
                  :items="terminaisLinux"
                  @update:model-value="salvarConfiguracao"
                />
              </v-col>
            </v-row>
          </v-tabs-window-item>

          <!-- Aba: Pasta Centralizadora -->
          <v-tabs-window-item>
            <div class="d-flex flex-column justify-center pt-4">
              <div class="d-flex align-center">
                <v-text-field
                  label="Nome da pasta"
                  v-model="nomePastaCentralizadora"
                  autocomplete="off"
                  name="pmw-pasta"
                  @keydown.enter.prevent="adicionarPastaCentralizadora"
                />
                <v-btn
                  class="ml-2"
                  @click="adicionarPastaCentralizadora"
                >
                  <v-icon>mdi-plus</v-icon>
                  Adicionar
                </v-btn>
              </div>

              <div>
                <v-data-table
                  :items="configuracao.pastasCentralizadoras"
                  :headers="colunasPastasCentralizadoras"
                  hide-default-footer
                >
                  <template #[`item.actions`]="{ item }">
                    <IconeComTooltip
                      icone="mdi-pencil"
                      texto="Editar"
                      :acao="() => editarPastaCentralizadora(item)"
                      top
                    />
                    <IconeComTooltip
                      icone="mdi-delete"
                      texto="Excluir"
                      :acao="() => removerPastaCentralizadora(item)"
                      top
                    />
                  </template>
                </v-data-table>
              </div>
            </div>
          </v-tabs-window-item>

          <!-- Aba: CLI de IA -->
          <v-tabs-window-item>
            <div class="d-flex flex-column justify-center pt-4">
              <div class="d-flex align-center">
                <v-text-field
                  ref="campoNomeCli"
                  label="Nome"
                  v-model="nomeCliNovo"
                  autocomplete="off"
                  name="pmw-cli-nome"
                  class="mr-2"
                  @keydown.enter.prevent="adicionarCli"
                />
                <v-text-field
                  label="Comando"
                  v-model="comandoCliNovo"
                  autocomplete="off"
                  name="pmw-cli-comando"
                  @keydown.enter.prevent="adicionarCli"
                />
                <v-btn
                  class="ml-2"
                  @click="adicionarCli"
                >
                  <v-icon>mdi-plus</v-icon>
                  Adicionar
                </v-btn>
              </div>

              <div>
                <v-data-table
                  :items="configuracao.clis"
                  :headers="colunasCli"
                  hide-default-footer
                >
                  <template #[`item.actions`]="{ item }">
                    <IconeComTooltip
                      icone="mdi-delete"
                      texto="Excluir"
                      :acao="() => removerCli(item)"
                      top
                    />
                  </template>
                </v-data-table>
              </div>
            </div>
          </v-tabs-window-item>

          <!-- Aba: Monitoramento -->
          <v-tabs-window-item>
            <v-row
              no-gutters
              class="pt-4"
            >
              <v-col cols="12">
                <v-switch
                  label="Registrar uso da máquina"
                  v-model="configuracao.usoRecursos.ativo"
                  @update:model-value="salvarConfiguracao"
                  color="primary"
                  hide-details
                />
                <div class="text-caption text-medium-emphasis mb-4">
                  Registra apenas com o painel de monitoramento aberto e a CPU
                  igual ou acima do percentual informado. Um arquivo por dia,
                  dentro da pasta do banco de dados.
                </div>
              </v-col>

              <v-col
                cols="12"
                md="4"
              >
                <v-text-field
                  label="CPU (%)"
                  v-model="limiarCpu"
                  type="number"
                  min="1"
                  max="100"
                  :disabled="usoInativo"
                  @change="salvarConfiguracao"
                  autocomplete="off"
                  name="pmw-uso-limiar"
                />
              </v-col>

              <v-col
                cols="12"
                md="4"
              >
                <v-text-field
                  label="Intervalo entre registros (s)"
                  v-model="intervaloRegistro"
                  type="number"
                  min="1"
                  :disabled="usoInativo"
                  @change="salvarConfiguracao"
                  autocomplete="off"
                  name="pmw-uso-intervalo"
                  class="px-4"
                />
              </v-col>

              <v-col
                cols="12"
                md="4"
              >
                <v-text-field
                  label="Retenção (dias)"
                  v-model="retencaoDias"
                  type="number"
                  min="1"
                  :disabled="usoInativo"
                  @change="salvarConfiguracao"
                  autocomplete="off"
                  name="pmw-uso-retencao"
                />
              </v-col>

              <v-col cols="12">
                <v-btn
                  color="primary"
                  :loading="usoStore.gerandoRelatorio"
                  @click="gerarRelatorio"
                >
                  <v-icon start>mdi-file-chart-outline</v-icon>
                  Gerar relatório
                </v-btn>
                <span class="text-caption text-medium-emphasis ml-3">
                  Gera o HTML com todos os registros do período e abre no
                  navegador.
                </span>
              </v-col>
            </v-row>
          </v-tabs-window-item>
        </v-tabs-window>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup lang="ts">
  import { computed, onMounted, reactive, ref, nextTick } from 'vue';
  import type { IConfiguracao, IPastaCentralizadora } from '@/types';
  import ConfiguracaoModel from '../models/ConfiguracaoModel';
  import ConfiguracaoService from '../services/ConfiguracaoService';
  import ComandosService from '@/services/ComandosService';
  import { useConfiguracaoStore } from '@/stores/configuracao';
  import { useFeaturesStore } from '@/stores/features';
  import { useUsoStore } from '@/stores/uso';
  import { notificar } from '@/utils/eventBus';
  import IconeComTooltip from '@/components/comum/botao/IconeComTooltip.vue';

  const configuracaoStore = useConfiguracaoStore();
  const featuresStore = useFeaturesStore();
  const usoStore = useUsoStore();

  // --- STATE ---
  const abaAtiva = ref<number>(0);
  const terminaisLinux = ['ptyxis', 'ghostty'];

  const nomeCliNovo = ref<string>('');
  const comandoCliNovo = ref<string>('');
  const nomePastaCentralizadora = ref<string>('');
  const campoNomeCli = ref<any>(null);
  const configuracao = reactive<IConfiguracao>(new ConfiguracaoModel());

  const usoInativo = computed<boolean>(() => !configuracao.usoRecursos.ativo);

  const limiarCpu = computed<number, string | number>({
    get: () => configuracao.usoRecursos.limiarCpuPercentual,
    set: valor => {
      configuracao.usoRecursos.limiarCpuPercentual = Number(valor) || 1;
    }
  });

  const intervaloRegistro = computed<number, string | number>({
    get: () => configuracao.usoRecursos.intervaloRegistroSegundos,
    set: valor => {
      configuracao.usoRecursos.intervaloRegistroSegundos = Number(valor) || 1;
    }
  });

  const retencaoDias = computed<number, string | number>({
    get: () => configuracao.usoRecursos.retencaoDias,
    set: valor => {
      configuracao.usoRecursos.retencaoDias = Number(valor) || 1;
    }
  });

  onMounted(() => {
    Object.assign(
      configuracao,
      new ConfiguracaoModel(configuracaoStore.configuracao)
    );
  });

  const colunasCli = reactive([
    { title: 'Nome', key: 'nome', align: 'start' },
    { title: 'Comando', key: 'comando', align: 'start' },
    { title: 'Actions', key: 'actions', align: 'center', width: '200px' }
  ] as const);

  const colunasPastasCentralizadoras = reactive([
    { title: 'Nome', key: 'nome', align: 'start' },
    { title: 'Actions', key: 'actions', align: 'center', width: '200px' }
  ] as const);

  const salvarConfiguracao = async (): Promise<void> => {
    try {
      await configuracaoStore.salvarConfiguracao(configuracao);
      notificar('sucesso', 'Configurações atualizadas');
    } catch (error: any) {
      notificar('erro', 'Falha ao salvar configuração', error.message);
    }
  };

  // --- CLIs ---
  const adicionarCli = (): void => {
    if (!nomeCliNovo.value.trim() || !comandoCliNovo.value.trim()) {
      alert('Nome e comando são obrigatórios');
      return;
    }

    configuracao.clis.push({
      nome: nomeCliNovo.value,
      comando: comandoCliNovo.value
    });
    nomeCliNovo.value = '';
    comandoCliNovo.value = '';
    salvarConfiguracao();
    nextTick(() => campoNomeCli.value?.focus());
  };

  const removerCli = (item: { nome: string; comando: string }): void => {
    const confirmDelete = confirm(`Deseja remover a CLI "${item.nome}"?`);
    if (confirmDelete) {
      configuracao.clis = configuracao.clis.filter(c => c !== item);
      salvarConfiguracao();
    }
  };

  // --- Pastas Centralizadoras ---
  const adicionarPastaCentralizadora = async (): Promise<void> => {
    if (!nomePastaCentralizadora.value.trim()) {
      alert('O nome da pasta é obrigatório');
      return;
    }

    const jaExiste = configuracao.pastasCentralizadoras.some(
      p => p.nome === nomePastaCentralizadora.value.trim()
    );

    if (jaExiste) {
      alert('Já existe uma pasta centralizadora com esse nome');
      return;
    }

    try {
      await ConfiguracaoService.adicionarPastaCentralizadora(
        nomePastaCentralizadora.value.trim()
      );
      configuracao.pastasCentralizadoras.push({
        nome: nomePastaCentralizadora.value.trim()
      });
      nomePastaCentralizadora.value = '';
      notificar('sucesso', 'Pasta centralizadora adicionada');
    } catch (error: any) {
      notificar(
        'erro',
        'Falha ao adicionar pasta centralizadora',
        error.message
      );
    }
  };

  const editarPastaCentralizadora = async (
    item: IPastaCentralizadora
  ): Promise<void> => {
    const novoNome = prompt('Editar nome da pasta centralizadora:', item.nome);
    if (!novoNome || !novoNome.trim()) return;

    const nomeAntigo = item.nome;
    const nomeTrimado = novoNome.trim();

    try {
      await ConfiguracaoService.renomearPastaCentralizadora(
        nomeAntigo,
        nomeTrimado
      );
      item.nome = nomeTrimado;
      notificar('sucesso', 'Pasta centralizadora renomeada');
    } catch (error: any) {
      notificar(
        'erro',
        'Falha ao renomear pasta centralizadora',
        error.message
      );
    }
  };

  const removerPastaCentralizadora = async (
    item: IPastaCentralizadora
  ): Promise<void> => {
    const confirmado = confirm(
      `Deseja remover a pasta centralizadora "${item.nome}"?`
    );
    if (!confirmado) return;

    try {
      await ConfiguracaoService.removerPastaCentralizadora(item.nome);
      configuracao.pastasCentralizadoras =
        configuracao.pastasCentralizadoras.filter(p => p !== item);
      notificar('sucesso', 'Pasta centralizadora removida');
    } catch (error: any) {
      notificar('erro', 'Falha ao remover pasta centralizadora', error.message);
    }
  };

  const gerarRelatorio = async (): Promise<void> => {
    try {
      await usoStore.gerarRelatorio();
      notificar(
        'sucesso',
        'Relatório gerado',
        usoStore.ultimoRelatorio?.caminho ?? ''
      );
    } catch (error: any) {
      notificar('erro', 'Falha ao gerar relatório', error.message);
    }
  };

  const abrirBancoPath = async (): Promise<void> => {
    try {
      const caminho = await ConfiguracaoService.obterCaminhoBanco();
      const explorador = featuresStore.isWindows ? 'explorer' : 'xdg-open';
      await ComandosService.executarComandoAvulso({
        comando: `cd "${caminho}"; ${explorador} .; Exit;`
      });
    } catch (error: any) {
      notificar('erro', 'Falha ao abrir diretório', error.message);
    }
  };
</script>

<style scoped>
  .conteudo-aba {
    height: calc(100dvh - 220px);
    overflow: auto;
  }
</style>
