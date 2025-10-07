<template>
  <v-row no-gutters>
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
          :items="repositorio.menus"
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
              :acao="() => excluirProjeto(item)"
              top
            />
          </template>
        </v-data-table>
      </div>

      <div>
        <ModalPadrao
          v-model="exibirModalMenuCadastro"
          titulo="Cadastro de Menu de Contexto"
          :textoBotaoPrimario="emModoCadastro ? 'Adicionar' : 'Salvar'"
          :acaoBotaoPrimario="() => metodoBotaoPrimario()"
          :acaoBotaoSecundario="() => metodoBotaoSecundario()"
          larguraMinima="800px"
        >
          <v-tabs-window v-model="paginaMenu">
            <v-tabs-window-item>
              <v-form ref="formProjeto">
                <v-text-field
                  label="Título"
                  v-model="menuSelecionado.titulo"
                  :rules="obrigatorio"
                />

                <div>
                  <div>
                    <BotaoTerciario
                      texto="Adicionar"
                      icone="mdi-plus"
                      @click="prepararParaCadastroArquivos"
                      class="my-2"
                    />
                  </div>
                  <div>
                    <v-data-table
                      :headers="colunasMenuArquivos"
                      :items="menuSelecionado.arquivos"
                      hide-default-footer
                    >
                      <template #[`item.actions`]="{ item }">
                        <IconeComTooltip
                          icone="mdi-pencil"
                          texto="Editar"
                          :acao="() => mudarParaEdicaoArquivo(item)"
                          top
                        />
                        <IconeComTooltip
                          icone="mdi-delete"
                          texto="Excluir"
                          :acao="() => excluirArquivo(item)"
                          top
                        />
                      </template>
                    </v-data-table>
                  </div>
                </div>
              </v-form>
            </v-tabs-window-item>

            <v-tabs-window-item>
              <v-form ref="formArquivo">
                <v-text-field
                  label="Arquivo"
                  v-model="arquivoSelecionado.arquivo"
                  :rules="obrigatorio"
                />
                <v-text-field
                  label="Destino"
                  v-model="arquivoSelecionado.destino"
                  hint="Caminho relativo ao repositório"
                  persistent-hint
                />
                <v-checkbox
                  label="Ignorar no git diff"
                  v-model="arquivoSelecionado.ignorarGit"
                />
              </v-form>
            </v-tabs-window-item>
          </v-tabs-window>
        </ModalPadrao>
      </div>
    </v-col>
  </v-row>
</template>

<script setup lang="ts">
  import { computed, reactive, ref } from 'vue'
  import type { IRepositorio, IMenu, IArquivo } from '@/types'
  import RepositorioModel from '@/models/RepositorioModel'
  import MenuModel from '@/models/MenuModel'
  import BotaoTerciario from '../comum/botao/BotaoTerciario.vue'
  import { useModoOperacao } from '@/composables/useModoOperacao'
  import ArquivoModel from '@/models/ArquivoModel'

  const {
    emModoCadastro,
    definirModoCadastro,
    definirModoEdicao,
    definirModoInicial,
  } = useModoOperacao()

  const repositorio = defineModel<IRepositorio>({
    default: () => new RepositorioModel(),
  })
  const menuSelecionado = reactive<IMenu>(new MenuModel())
  const arquivoSelecionado = reactive<IArquivo>(new ArquivoModel())
  const arquivoEmEdicao = ref<boolean>(false)

  const obrigatorio = [(v: string) => !!v || 'Obrigatório']

  const exibirModalMenuCadastro = ref<boolean>(false)
  const paginaMenu = ref<number>(0)

  const colunas = reactive([
    { title: 'Título', key: 'titulo', align: 'start' },
    { title: 'Tipo', key: 'tipo', align: 'start' },
    { title: 'Actions', key: 'actions', align: 'center', width: '200px' },
  ] as const)

  const colunasMenuArquivos = reactive([
    { title: 'Arquivo', key: 'arquivo', align: 'start' },
    { title: 'Destino', key: 'destino', align: 'start' },
    { title: 'Ignorar Git Diff', key: 'ignorarGit', align: 'start' },
    { title: 'Actions', key: 'actions', align: 'center', width: '200px' },
  ] as const)

  const paginaTabela = computed(() => paginaMenu.value === 0)

  const metodoBotaoPrimario = computed(() => {
    return paginaTabela.value ? salvarAlteracoes : salvarAlteracoesArquivos
  })

  const metodoBotaoSecundario = computed(() => {
    return paginaTabela.value
      ? descartarAlteracoes
      : descartarAlteracoesArquivos
  })

  const mudarParaPaginaTabela = (): void => {
    paginaMenu.value = 0
  }

  const descartarAlteracoesArquivos = (): void => {
    mudarParaPaginaTabela()
    limparCamposArquivos()
  }

  const prepararParaCadastro = (): void => {
    definirModoCadastro()
    limparCampos()
    abrirModalMenuCadastro()
  }

  const prepararParaCadastroArquivos = (): void => {
    paginaMenu.value = 1
    limparCamposArquivos()
  }

  const limparCamposArquivos = (): void => {
    Object.assign(arquivoSelecionado, new ArquivoModel())
  }

  const abrirModalMenuCadastro = (): void => {
    exibirModalMenuCadastro.value = true
  }

  const mudarParaEdicaoArquivo = (item: IArquivo): void => {
    Object.assign(arquivoSelecionado, item)
    arquivoEmEdicao.value = true
    paginaMenu.value = 1
  }

  const mudarParaEdicao = (item: IMenu): void => {
    Object.assign(menuSelecionado, item)
    definirModoEdicao()
    abrirModalMenuCadastro()
  }

  const formProjeto = ref<any>(null)
  const formArquivo = ref<any>(null)

  const formularioProjetoValido = async (): Promise<boolean> => {
    const resposta = await formProjeto.value.validate()
    return resposta.valid
  }

  const formularioArquivoValido = async (): Promise<boolean> => {
    const resposta = await formArquivo.value.validate()
    return resposta.valid
  }

  const salvarAlteracoes = async (): Promise<void> => {
    if (!(await formularioProjetoValido())) return

    try {
      emModoCadastro.value ? adicionarMenu() : atualizarProjeto()
      descartarAlteracoes()
    } catch (error) {
      console.error('Falha ao salvar alteracoes do cadastro:', error)
    }
  }

  const salvarAlteracoesArquivos = async (): Promise<void> => {
    if (!(await formularioArquivoValido())) return

    if (arquivoEmEdicao.value) {
      const indice = menuSelecionado.arquivos.findIndex(
        a => a.identificador === arquivoSelecionado.identificador
      )

      if (indice !== -1) {
        Object.assign(menuSelecionado.arquivos[indice], arquivoSelecionado)
      }

      arquivoEmEdicao.value = false
      limparCamposArquivos()
      mudarParaPaginaTabela()
      return
    }

    menuSelecionado.arquivos.push(new ArquivoModel(arquivoSelecionado))
    mudarParaPaginaTabela()
  }

  const adicionarMenu = (): void => {
    repositorio.value.menus.push(new MenuModel(menuSelecionado))
  }

  const atualizarProjeto = (): void => {
    const indice = repositorio.value.menus.findIndex(
      p => p.identificador === menuSelecionado.identificador
    )

    indice !== -1 &&
      Object.assign(repositorio.value.menus[indice], menuSelecionado)
  }

  const excluirProjeto = (item: IMenu): void => {
    const confirmDelete = confirm(`Deseja remover o projeto "${item.titulo}"?`)

    if (!confirmDelete) return

    repositorio.value.menus = repositorio.value.menus.filter(
      p => p.identificador !== item.identificador
    )
  }

  const excluirArquivo = (item: IArquivo): void => {
    const confirmDelete = confirm(`Deseja remover o arquivo "${item.arquivo}"?`)

    if (!confirmDelete) return

    menuSelecionado.arquivos = menuSelecionado.arquivos.filter(
      a => a.identificador !== item.identificador
    )
  }

  const limparCampos = (): void => {
    Object.assign(menuSelecionado, new MenuModel())
  }

  const descartarAlteracoes = (): void => {
    // Perguntar sobre perder alteracoes
    limparCampos()
    definirModoInicial()
    exibirModalMenuCadastro.value = false
  }
</script>
