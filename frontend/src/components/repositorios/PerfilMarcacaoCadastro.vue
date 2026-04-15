<template>
  <v-col cols="12">
    <div>
      <BotaoTerciario
        texto="Adicionar"
        icone="mdi-plus"
        @click="prepararParaCadastro"
        class="my-2"
      />
    </div>

    <div>
      <v-data-table
        :headers="colunas"
        :items="repositorio.perfis"
        hide-default-footer
      >
        <template #[`item.actions`]="{ item }">
          <IconeComTooltip
            icone="mdi-pencil"
            texto="Editar"
            :acao="() => mudarParaEdicao(item)"
            top
          />
          <IconeComTooltip
            icone="mdi-delete"
            texto="Excluir"
            :acao="() => excluirPerfil(item)"
            top
          />
        </template>
      </v-data-table>
    </div>

    <ModalPadrao
      v-model="exibirModal"
      titulo="Perfil de Marcação"
      :textoBotaoPrimario="emModoCadastro ? 'Adicionar' : 'Salvar'"
      textoBotaoSecundario="Cancelar"
      :acaoBotaoPrimario="salvarAlteracoes"
      :acaoBotaoSecundario="descartarAlteracoes"
      larguraMinima="600px"
    >
      <v-form ref="formPerfil">
        <v-text-field
          label="Nome do perfil"
          v-model="perfilSelecionado.nome"
          :rules="obrigatorio"
          placeholder="Ex: Iniciar tudo, Só IDE, Build completo"
        />
      </v-form>

      <div class="mt-4">
        <h3 class="mb-2">Marcações</h3>
        <v-divider class="mb-3" />

        <div
          v-for="grupo in projetosComComandos"
          :key="grupo.projeto.identificador"
        >
          <v-card
            class="mb-2"
            style="background-color: #2d2d30"
          >
            <v-card-title class="pb-0 text-body-1">
              {{ grupo.projeto.nome }}
              <span
                v-if="grupo.nomeRepositorio"
                class="text-caption text-medium-emphasis ml-1"
              >
                ({{ grupo.nomeRepositorio }})
              </span>
            </v-card-title>
            <v-card-text class="pa-0 ml-4 pr-4">
              <v-switch
                v-for="comando in grupo.comandosDisponiveis"
                :key="comando.valor"
                :label="comando.titulo"
                :value="comando.valor"
                v-model="comandosSelecionadosPorProjeto[grupo.projeto.identificador]"
                hide-details
                height="40px"
                color="primary"
                density="compact"
              />
            </v-card-text>
          </v-card>
        </div>
      </div>
    </ModalPadrao>
  </v-col>
</template>

<script setup lang="ts">
  import { computed, reactive, ref } from 'vue';
  import type { IRepositorio, IPerfilMarcacao, IProjeto } from '@/types';
  import PerfilMarcacaoModel from '@/models/PerfilMarcacaoModel';
  import PerfilMarcacaoProjetoModel from '@/models/PerfilMarcacaoProjetoModel';
  import { useModoOperacao } from '@/composables/useModoOperacao';
  import { TIPO_COMANDO } from '@/constants/geral-constants';

  interface Props {
    repositorios: IRepositorio[];
  }

  const props = defineProps<Props>();
  const repositorio = defineModel<IRepositorio>({ required: true });

  const { emModoCadastro, definirModoCadastro, definirModoEdicao, definirModoInicial } =
    useModoOperacao();

  const obrigatorio = [(v: string) => !!v || 'Obrigatório'];
  const exibirModal = ref<boolean>(false);
  const formPerfil = ref<any>(null);
  const perfilSelecionado = reactive<IPerfilMarcacao>(new PerfilMarcacaoModel());
  const comandosSelecionadosPorProjeto = reactive<Record<string, string[]>>({});

  const colunas = [
    { title: 'Nome', key: 'nome', align: 'start' },
    { title: 'Ações', key: 'actions', align: 'center', width: '120px' },
  ] as const;

  interface GrupoProjeto {
    projeto: IProjeto;
    nomeRepositorio: string | null;
    comandosDisponiveis: Array<{ titulo: string; valor: string }>;
  }

  const projetosComComandos = computed((): GrupoProjeto[] => {
    const grupos: GrupoProjeto[] = [];

    repositorio.value.projetos.forEach(projeto => {
      const comandos = resolverComandosDisponiveis(projeto);
      if (comandos.length)
        grupos.push({ projeto, nomeRepositorio: null, comandosDisponiveis: comandos });
    });

    repositorio.value.agregados.forEach(agregadoId => {
      const repoAgregado = props.repositorios.find(r => r.identificador === agregadoId);
      if (!repoAgregado) return;

      repoAgregado.projetos.forEach(projeto => {
        const comandos = resolverComandosDisponiveis(projeto);
        if (comandos.length)
          grupos.push({
            projeto,
            nomeRepositorio: repoAgregado.titulo,
            comandosDisponiveis: comandos,
          });
      });
    });

    return grupos;
  });

  const resolverComandosDisponiveis = (
    projeto: IProjeto
  ): Array<{ titulo: string; valor: string }> => {
    const disponiveis = [];
    const obj = (projeto as any).comandosObj;

    if (obj?.instalar) disponiveis.push({ titulo: TIPO_COMANDO.INSTALAR.titulo, valor: TIPO_COMANDO.INSTALAR.valor });
    if (obj?.iniciar) disponiveis.push({ titulo: TIPO_COMANDO.INICIAR.titulo, valor: TIPO_COMANDO.INICIAR.valor });
    if (obj?.buildar) disponiveis.push({ titulo: TIPO_COMANDO.BUILDAR.titulo, valor: TIPO_COMANDO.BUILDAR.valor });
    if (obj?.ideIdentificador) disponiveis.push({ titulo: TIPO_COMANDO.ABRIR_NA_IDE.titulo, valor: TIPO_COMANDO.ABRIR_NA_IDE.valor });

    return disponiveis;
  };

  const inicializarComandosSelecionados = (): void => {
    projetosComComandos.value.forEach(grupo => {
      const projetoNoPerfil = perfilSelecionado.projetos.find(
        p => p.identificadorProjeto === grupo.projeto.identificador
      );
      comandosSelecionadosPorProjeto[grupo.projeto.identificador] =
        projetoNoPerfil ? [...projetoNoPerfil.comandos] : [];
    });
  };

  const prepararParaCadastro = (): void => {
    definirModoCadastro();
    Object.assign(perfilSelecionado, new PerfilMarcacaoModel());
    inicializarComandosSelecionados();
    exibirModal.value = true;
  };

  const mudarParaEdicao = (item: IPerfilMarcacao): void => {
    definirModoEdicao();
    Object.assign(perfilSelecionado, new PerfilMarcacaoModel(item));
    inicializarComandosSelecionados();
    exibirModal.value = true;
  };

  const salvarAlteracoes = async (): Promise<void> => {
    const resultado = await formPerfil.value?.validate();
    if (!resultado?.valid) return;

    perfilSelecionado.projetos = Object.entries(comandosSelecionadosPorProjeto)
      .filter(([, comandos]) => comandos.length > 0)
      .map(([identificadorProjeto, comandos]) =>
        new PerfilMarcacaoProjetoModel({ identificadorProjeto, comandos })
      );

    if (emModoCadastro.value) {
      repositorio.value.perfis.push(new PerfilMarcacaoModel(perfilSelecionado));
    } else {
      const indice = repositorio.value.perfis.findIndex(
        p => p.identificador === perfilSelecionado.identificador
      );
      indice !== -1 && Object.assign(repositorio.value.perfis[indice], new PerfilMarcacaoModel(perfilSelecionado));
    }

    descartarAlteracoes();
  };

  const excluirPerfil = (item: IPerfilMarcacao): void => {
    const confirmar = confirm(`Deseja remover o perfil "${item.nome}"?`);
    if (!confirmar) return;

    repositorio.value.perfis = repositorio.value.perfis.filter(
      p => p.identificador !== item.identificador
    );
  };

  const descartarAlteracoes = (): void => {
    Object.assign(perfilSelecionado, new PerfilMarcacaoModel());
    definirModoInicial();
    exibirModal.value = false;
  };
</script>
