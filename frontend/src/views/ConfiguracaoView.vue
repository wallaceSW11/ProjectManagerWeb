<template>
  <v-container>
    <v-row no-gutters>
      <v-col cols="12">
        <div class="d-flex justify-space-between">
          <div>
            <h1>Configurações</h1>
          </div>
        </div>
      </v-col>

      <v-col cols="12">
        <v-tabs v-model="abaAtiva">
          <v-tab>Geral</v-tab>
          <v-tab>Perfis IDE</v-tab>
          <v-tab>Pasta Centralizadora</v-tab>
          <v-tab>CLI de IA</v-tab>
        </v-tabs>

        <v-tabs-window v-model="abaAtiva" class="conteudo-aba">
          <!-- Aba: Geral -->
          <v-tabs-window-item>
            <v-row no-gutters>
              <v-col cols="12" class="pt-4">
                <v-text-field
                  label="Diretório raiz"
                  v-model="configuracao.diretorioRaiz"
                  @change="salvarConfiguracao"
                  autocomplete="new-password"
                />
              </v-col>

              <v-col cols="12" v-if="featuresStore.isLinux">
                <v-select
                  label="Terminal"
                  v-model="configuracao.terminalLinux"
                  :items="terminaisLinux"
                  @update:model-value="salvarConfiguracao"
                />
              </v-col>
            </v-row>
          </v-tabs-window-item>

          <!-- Aba: Perfis IDE -->
          <v-tabs-window-item>
            <div class="d-flex flex-column justify-center pt-4">
              <div class="d-flex align-center">
                <v-text-field
                  label="Perfil"
                  v-model="nomePerfil"
                  autocomplete="new-password"
                />
                <v-btn
                  class="ml-2"
                  @click="adicionarPerfil"
                >
                  <v-icon>mdi-plus</v-icon>
                  Adicionar
                </v-btn>
              </div>

              <div>
                <v-data-table
                  :items="configuracao.perfisVSCode"
                  :headers="colunas"
                  hide-default-footer
                >
                  <template #[`item.actions`]="{ item }">
                    <IconeComTooltip
                      icone="mdi-pencil"
                      texto="Editar"
                      :acao="() => editarPerfil(item)"
                      top
                    />
                    <IconeComTooltip
                      icone="mdi-delete"
                      texto="Excluir"
                      :acao="() => removerPerfil(item)"
                      top
                    />
                  </template>
                </v-data-table>
              </div>
            </div>
          </v-tabs-window-item>

          <!-- Aba: Pasta Centralizadora -->
          <v-tabs-window-item>
            <div class="d-flex flex-column justify-center pt-4">
              <div class="d-flex align-center">
                <v-text-field
                  label="Nome da pasta"
                  v-model="nomePastaCentralizadora"
                  autocomplete="new-password"
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
                  label="Nome"
                  v-model="nomeCliNovo"
                  autocomplete="new-password"
                  class="mr-2"
                />
                <v-text-field
                  label="Comando"
                  v-model="comandoCliNovo"
                  autocomplete="new-password"
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
        </v-tabs-window>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup lang="ts">
  import { onMounted, reactive, ref } from 'vue';
  import type { IConfiguracao, IPastaCentralizadora, IPerfilVSCode } from '@/types';
  import ConfiguracaoModel from '../models/ConfiguracaoModel';
  import ConfiguracaoService from '../services/ConfiguracaoService';
  import { useConfiguracaoStore } from '@/stores/configuracao';
  import { useFeaturesStore } from '@/stores/features';
  import { notificar } from '@/utils/eventBus';

  const configuracaoStore = useConfiguracaoStore();
  const featuresStore = useFeaturesStore();

  // --- STATE ---
  const abaAtiva = ref<number>(0);
  const terminaisLinux = ['ptyxis', 'ghostty'];
  const nomePerfil = ref<string>(''); // input do perfil
  const nomeCliNovo = ref<string>('');
  const comandoCliNovo = ref<string>('');
  const nomePastaCentralizadora = ref<string>('');
  const configuracao = reactive<IConfiguracao>(new ConfiguracaoModel());

  onMounted(() => {
    Object.assign(configuracao, new ConfiguracaoModel(configuracaoStore));
  });

  const colunas = reactive([
    { title: 'Perfil', key: 'nome', align: 'start' },
    { title: 'Actions', key: 'actions', align: 'center', width: '200px' },
  ] as const);

  const colunasCli = reactive([
    { title: 'Nome', key: 'nome', align: 'start' },
    { title: 'Comando', key: 'comando', align: 'start' },
    { title: 'Actions', key: 'actions', align: 'center', width: '200px' },
  ] as const);

  const colunasPastasCentralizadoras = reactive([
    { title: 'Nome', key: 'nome', align: 'start' },
    { title: 'Actions', key: 'actions', align: 'center', width: '200px' },
  ] as const);

  const salvarConfiguracao = async (): Promise<void> => {
    try {
      await ConfiguracaoService.postConfiguracao(configuracao);
      configuracaoStore.salvarConfiguracao(configuracao);
      notificar('sucesso', 'Configurações atualizadas');
    } catch (error: any) {
      notificar('erro', 'Falha ao salvar configuração', error.message);
    }
  };

  // Valida nome do perfil
  const perfilValido = (): boolean => {
    if (!nomePerfil.value.trim()) {
      alert('O nome do perfil é obrigatório');
      return false;
    }

    const jaInformado = configuracao.perfisVSCode?.some(
      p => p.nome === nomePerfil.value
    );

    if (jaInformado) {
      alert('Já existe um perfil com esse nome');
      return false;
    }

    return true;
  };

  // Adiciona perfil
  const adicionarPerfil = (): void => {
    if (!perfilValido()) return;

    configuracao.perfisVSCode.push({ nome: nomePerfil.value });
    nomePerfil.value = ''; // limpa input

    salvarConfiguracao(); // opcional, salva direto
  };

  const editarPerfil = async (item: IPerfilVSCode): Promise<void> => {
    const novoNome = prompt('Editar nome do perfil:', item.nome);
    if (!novoNome || !novoNome.trim()) return;

    const nomeAntigo = item.nome;
    const nomeTrimado = novoNome.trim();

    try {
      await ConfiguracaoService.renomearPerfil(nomeAntigo, nomeTrimado);
      item.nome = nomeTrimado;
      configuracaoStore.salvarConfiguracao(configuracao);
      notificar('sucesso', 'Perfil renomeado e atualizado em todos os projetos');
    } catch (error: any) {
      notificar('erro', 'Falha ao renomear perfil', error.message);
    }
  };

  // Remover perfil
  const removerPerfil = (item: IPerfilVSCode): void => {
    const confirmDelete = confirm(`Deseja remover o perfil "${item.nome}"?`);
    if (confirmDelete) {
      configuracao.perfisVSCode = configuracao.perfisVSCode.filter(
        p => p !== item
      );
      salvarConfiguracao();
    }
  };

  // --- CLIs ---
  const adicionarCli = (): void => {
    if (!nomeCliNovo.value.trim() || !comandoCliNovo.value.trim()) {
      alert('Nome e comando são obrigatórios');
      return;
    }

    configuracao.clis.push({ nome: nomeCliNovo.value, comando: comandoCliNovo.value });
    nomeCliNovo.value = '';
    comandoCliNovo.value = '';
    salvarConfiguracao();
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
      await ConfiguracaoService.adicionarPastaCentralizadora(nomePastaCentralizadora.value.trim());
      configuracao.pastasCentralizadoras.push({ nome: nomePastaCentralizadora.value.trim() });
      nomePastaCentralizadora.value = '';
      notificar('sucesso', 'Pasta centralizadora adicionada');
    } catch (error: any) {
      notificar('erro', 'Falha ao adicionar pasta centralizadora', error.message);
    }
  };

  const editarPastaCentralizadora = async (item: IPastaCentralizadora): Promise<void> => {
    const novoNome = prompt('Editar nome da pasta centralizadora:', item.nome);
    if (!novoNome || !novoNome.trim()) return;

    const nomeAntigo = item.nome;
    const nomeTrimado = novoNome.trim();

    try {
      await ConfiguracaoService.renomearPastaCentralizadora(nomeAntigo, nomeTrimado);
      item.nome = nomeTrimado;
      notificar('sucesso', 'Pasta centralizadora renomeada');
    } catch (error: any) {
      notificar('erro', 'Falha ao renomear pasta centralizadora', error.message);
    }
  };

  const removerPastaCentralizadora = async (item: IPastaCentralizadora): Promise<void> => {
    const confirmado = confirm(`Deseja remover a pasta centralizadora "${item.nome}"?`);
    if (!confirmado) return;

    try {
      await ConfiguracaoService.removerPastaCentralizadora(item.nome);
      configuracao.pastasCentralizadoras = configuracao.pastasCentralizadoras.filter(
        p => p !== item
      );
      notificar('sucesso', 'Pasta centralizadora removida');
    } catch (error: any) {
      notificar('erro', 'Falha ao remover pasta centralizadora', error.message);
    }
  };
</script>

<style scoped>
  .conteudo-aba {
    height: calc(100dvh - 220px);
    overflow: auto;
  }
</style>
