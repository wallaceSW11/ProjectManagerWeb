<template>
  <v-container>
    <v-row no-gutters>
      <v-col cols="12">
        <div class="d-flex justify-space-between">
          <div>
            <h1>Repositórios</h1>
          </div>
        </div>
      </v-col>

      <v-col cols="12">
        <div class="d-flex justify-space-between align-center py-2">
          <BotaoTerciario
            v-if="!emModoCadastroEdicao"
            @click="prepararParaCadastro()"
            texto="Adicionar"
            icone="mdi-plus"
          />
        </div>
      </v-col>

      <v-col cols="12">
        <div>
          <v-tabs-window
            v-model="paginaPrincipal"
            class="altura-limitada"
          >
            <v-tabs-window-item>
              <ListaRepositorios
                :itens="repositorios"
                @editar="mudarParaEdicao"
                @excluir="excluirRepositorio"
              />
            </v-tabs-window-item>

            <v-tabs-window-item>
              <v-tabs v-model="paginaCadastro">
                <v-tab>Geral</v-tab>
                <v-tab>Projetos</v-tab>
                <v-tab>Menu de contexo</v-tab>
              </v-tabs>

              <v-tabs-window v-model="paginaCadastro">
                <v-tabs-window-item>
                  <RepositorioCadastro
                    v-model="repositorioSelecionado"
                    :repositorios="repositorios"
                    class="pt-4"
                  />
                </v-tabs-window-item>
                <v-tabs-window-item>
                  <ProjetoCadastro v-model="repositorioSelecionado" />
                </v-tabs-window-item>
                <v-tabs-window-item>
                  <MenuCadastro
                    v-model="repositorioSelecionado"
                    class="pt-4"
                  />
                </v-tabs-window-item>
              </v-tabs-window>
            </v-tabs-window-item>
          </v-tabs-window>
        </div>
      </v-col>

      <v-col
        cols="12"
        v-if="!emModoInicial"
      >
        <div class="d-flex align-center justify-end">
          <div>
            <BotaoPrimario
              @click="salvarAlteracoes"
              texto="Salvar"
              icone="mdi-check"
            />
            <BotaoSecundario
              @click="descartarAlteracoes"
              texto="Cancelar"
              icone="mdi-cancel"
            />
          </div>
        </div>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup lang="ts">
  import { computed, onMounted, reactive, ref } from 'vue';
  import type { IRepositorio } from '@/types';
  import ListaRepositorios from '../components/repositorios/ListaRepositorios.vue';
  import RepositorioCadastro from '../components/repositorios/RepositorioCadastro.vue';
  import RepositorioModel from '../models/RepositorioModel';
  import RepositoriosService from '../services/RepositoriosService';
  import MenuCadastro from '@/components/repositorios/MenuCadastro.vue';
  import ProjetoCadastro from '../components/repositorios/ProjetoCadastro.vue';
  import { carregandoAsync, notificar } from '@/utils/eventBus';
  import { MODO_OPERACAO } from '@/constants/geral-constants';

  const repositorios = reactive<IRepositorio[]>([]);
  const repositorioSelecionado = reactive<IRepositorio>(new RepositorioModel());
  const paginaPrincipal = ref<number>(0);
  const paginaCadastro = ref<number>(0);
  const camposObrigatoriosPreenchidos = ref<boolean>(true);

  onMounted(async () => {
    await preencherRepositorios();
  });

  const preencherRepositorios = async (): Promise<void> => {
    try {
      const resposta = await carregandoAsync(async () => {
        return await RepositoriosService.getRepositorios();
      });

      Object.assign(
        repositorios,
        resposta.map((r: any) => new RepositorioModel(r))
      );
    } catch (error) {
      console.error('Falha ao obter os relatorios:', error);
    }
  };

  let modoOperacao = ref<string>(MODO_OPERACAO.INICIAL.valor);

  const emModoInicial = computed(
    () => modoOperacao.value === MODO_OPERACAO.INICIAL.valor
  );
  const emModoCadastro = computed(
    () => modoOperacao.value === MODO_OPERACAO.NOVO.valor
  );
  const emModoEdicao = computed(
    () => modoOperacao.value === MODO_OPERACAO.EDICAO.valor
  );
  const emModoCadastroEdicao = computed(
    () => emModoCadastro.value || emModoEdicao.value
  );

  const irParaListagem = (): void => {
    paginaPrincipal.value = 0;
    modoOperacao.value = MODO_OPERACAO.INICIAL.valor;
  };

  const irParaCadastro = (): void => {
    paginaCadastro.value = 0;
    paginaPrincipal.value = 1;
  };

  const mudarParaEdicao = (identificador: string): void => {
    let repo = repositorios.find(r => r.identificador === identificador);

    if (!repo) {
      notificar('erro', 'Repositorio nao encontrado');
      return;
    }

    modoOperacao.value = MODO_OPERACAO.EDICAO.valor;
    Object.assign(repositorioSelecionado, repo);
    irParaCadastro();
  };

  const prepararParaCadastro = (): void => {
    modoOperacao.value = MODO_OPERACAO.NOVO.valor;
    limparCampos();
    irParaCadastro();
  };

  const formularioValido = (): boolean => {
    return camposObrigatoriosPreenchidos.value;
  };

  const salvarAlteracoes = async (): Promise<void> => {
    if (!formularioValido()) return;

    try {
      emModoCadastro.value
        ? await criarRepositorio()
        : await atualizarRepositorio();
    } catch (error) {
      console.error('Falha ao salvar alteracoes: ', error);
      notificar('erro', 'Falha ao salvar alteracoes');
    }
  };

  const criarRepositorio = async (): Promise<void> => {
    try {
      await RepositoriosService.adicionarRepositorio(repositorioSelecionado);
      repositorios.push(new RepositorioModel(repositorioSelecionado));
      limparCampos();
      notificar('sucesso', 'Repositorio criado');
      irParaListagem();
    } catch (error) {
      console.error('Falha ao criar repositorio: ' + error);
      notificar('erro', 'Falha ao criar repositorio');
    }
  };

  const atualizarRepositorio = async (): Promise<void> => {
    try {
      await RepositoriosService.atualizarRepositorio(repositorioSelecionado);

      const indice = repositorios.findIndex(
        r => r.identificador === repositorioSelecionado.identificador
      );

      indice !== -1 &&
        Object.assign(repositorios[indice], repositorioSelecionado);

      limparCampos();
      notificar('sucesso', 'Repositorio atualizado');
      irParaListagem();
    } catch (error) {
      console.error('Falha ao criar repositorio' + error);
      notificar('erro', 'Falha ao criar repositorio');
    }
  };

  const excluirRepositorio = async (item: IRepositorio): Promise<void> => {
    const confirmDelete = confirm(
      `Deseja excluir o repositorio "${item.titulo}"?`
    );

    if (!confirmDelete) return;

    try {
      await RepositoriosService.excluirRepositorio(item);
      const indice = repositorios.findIndex(
        r => r.identificador === item.identificador
      );
      indice !== -1 && repositorios.splice(indice, 1);

      notificar('sucesso', 'Repositorio excluido');
    } catch (error) {
      console.error('Falha ao excluir repositorio' + error);
      notificar('erro', 'Falha ao excluir repositorio');
    }
  };

  const descartarAlteracoes = (): void => {
    limparCampos();
    irParaListagem();
  };

  const limparCampos = (): void => {
    Object.assign(repositorioSelecionado, new RepositorioModel());
  };
</script>

<style scoped>
  .altura-limitada {
    height: calc(100dvh - 220px);
    overflow: auto;
  }
</style>
