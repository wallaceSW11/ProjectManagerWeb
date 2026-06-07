<template>
  <v-dialog
    v-model="exibirModalClone"
    max-width="500"
  >
    <v-card>
      <v-card-title>Clonar</v-card-title>

      <v-card-text class="pt-0">
        <v-form
          ref="formClone"
          autocomplete="off"
        >
          <div class="d-flex align-center gap-1">
            <v-text-field
              ref="campoCodigoTarefa"
              label="Código da tarefa"
              v-model.uppercase="codigoTarefaInput"
              class="uppercase-input flex-grow-1"
              :rules="obrigatorio"
              autocomplete="off"
              name="pmw-clone-codigo"
              @blur="processarCodigoTarefa"
              @paste="aoColar"
            />
            <IconeComTooltip
              icone="mdi-clipboard-outline"
              texto="Colar da área de transferência"
              :acao="colarDoClipboard"
            />
          </div>

          <v-text-field
            ref="campoDescricao"
            label="Descrição"
            v-model="clone.descricao"
            :rules="obrigatorio"
            autocomplete="off"
            name="pmw-clone-desc"
          />

          <SelectRepositorio
            v-model="clone.repositorio"
            obrigatorio
            carregar-ultimo-selecionado
            :chave-local-storage="CHAVE_ULTIMO_REPOSITORIO"
          />

          <SelectBranch
            v-model="clone.branch"
            obrigatorio
            :adicionarNoLocalStorage="adicionarNoLocalStorage"
            :disabled="!repositorioSelecionado"
            :loading="validandoBranch"
            :hint="hintBranch"
            :error="branchInvalida"
            persistent-hint
            @blur="validarBranch"
          />

          <v-checkbox
            label="Salvar branch no storage"
            hide-details
            v-model="clone.salvarNoStorage"
          />

          <v-checkbox
            label="Clonar agregados"
            hide-details
            v-model="clone.baixarAgregados"
            :disabled="!clone.repositorio.agregados?.length"
          />

          <v-checkbox
            label="Criar branch remoto"
            hide-details
            v-model="clone.criarBranchRemoto"
          />

          <v-checkbox
            label="Baixar histórico completo"
            hide-details
            v-model="clone.historicoCompleto"
            hint="Sem --depth, baixa o histórico completo do git"
          />

          <v-radio-group
            label="Tipo"
            inline
            hide-details
            v-model="clone.tipo"
            :disabled="!clone.criarBranchRemoto"
          >
            <v-radio
              label="Nenhum"
              value="nenhum"
            />
            <v-radio
              label="Feature"
              value="feature"
            />
            <v-radio
              label="Bug"
              value="bugfix"
            />
            <v-radio
              label="HotFix"
              value="hotfix"
            />
          </v-radio-group>
        </v-form>
      </v-card-text>

      <v-card-actions>
        <v-spacer />
        <v-btn
          color="primary"
          variant="outlined"
          :disabled="branchInvalida"
          @click="clonar"
        >
          <v-icon>mdi-download</v-icon>
          Clonar
        </v-btn>
        <v-btn @click="fecharClone">
          <v-icon>mdi-close</v-icon>
          Fechar
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script setup lang="ts">
  import { computed, nextTick, reactive, ref, watch } from 'vue';
  import type { IClone } from '@/types';
  import CloneModel from '@/models/CloneModel';
  import CloneService from '@/services/CloneService';
  import RepositoriosService from '@/services/RepositoriosService';
  import { useConfiguracaoStore } from '@/stores/configuracao';
  import { useFeaturesStore } from '@/stores/features';
  import { notificar, atualizarListaPastas } from '@/utils/eventBus';
  import SelectRepositorio from '@/components/repositorios/SelectRepositorio.vue';
  import SelectBranch from '@/components/comum/SelectBranch.vue';
  import IconeComTooltip from '@/components/comum/botao/IconeComTooltip.vue';

  const CHAVE_ULTIMO_REPOSITORIO = 'pmw_ultimo_repositorio_selecionado';
  const CHAVE_BRANCH_BASE_PREFIXO = 'pmw_branch_base_';
  const BRANCHES_BASE = ['develop', 'dev', 'main', 'master'];

  const clone = reactive<IClone>(new CloneModel());
  const configuracaoStore = useConfiguracaoStore();
  const featuresStore = useFeaturesStore();
  const exibirModalClone = defineModel<boolean>({ default: false });
  const formClone = ref<any>(null);
  const adicionarNoLocalStorage = ref<boolean>(false);
  const branchInvalida = ref<boolean>(false);
  const hintBranch = ref<string>('');
  const validandoBranch = ref<boolean>(false);
  const codigoTarefaInput = ref<string>('');
  const campoCodigoTarefa = ref<any>(null);
  const campoDescricao = ref<any>(null);

  const obrigatorio = [(v: string) => !!v || 'Obrigatório'];

  const repositorioSelecionado = computed(() => !!clone.repositorio?.url);

  watch(codigoTarefaInput, valor => {
    clone.codigo = valor;
  });

  watch(
    () => clone.branch,
    () => {
      branchInvalida.value = false;
      hintBranch.value = '';
    }
  );

  watch(repositorioSelecionado, temRepositorio => {
    if (!temRepositorio) {
      clone.branch = '';
      branchInvalida.value = false;
      hintBranch.value = '';
    }
  });

  watch(
    () => clone.repositorio?.identificador,
    () => {
      const repo = clone.repositorio;
      if (!repo?.url) {
        clone.branch = '';
        branchInvalida.value = false;
        hintBranch.value = '';
        return;
      }

      clone.diretorioRaiz = repo.pastaCentralizadora
        ? `${configuracaoStore.diretorioRaiz}${featuresStore.pathSeparator}${repo.pastaCentralizadora}${featuresStore.pathSeparator}`
        : `${configuracaoStore.diretorioRaiz}${featuresStore.pathSeparator}`;

      if (!codigoTarefaInput.value) {
        const branchSalva = recuperarBranchDoLocalStorage();
        if (branchSalva && !clone.branch) clone.branch = branchSalva;
      }
    }
  );

  watch(exibirModalClone, async abriu => {
    if (!abriu) return;

    clone.diretorioRaiz = `${configuracaoStore.diretorioRaiz}${featuresStore.pathSeparator}`;

    if (clone.repositorio?.identificador) {
      const branchSalva = recuperarBranchDoLocalStorage();
      if (branchSalva && !clone.branch) clone.branch = branchSalva;
    }

    await nextTick();
    campoCodigoTarefa.value?.focus();

    try {
      const texto = await navigator.clipboard.readText();
      const padrao = /^[A-Za-z]+\d+$/;
      if (padrao.test(texto.trim())) {
        codigoTarefaInput.value = texto.trim().toUpperCase();
        await processarCodigoTarefa();
        await nextTick();
        campoDescricao.value?.focus();
      }
    } catch {
      // Clipboard API sem permissão — usuário cola manualmente com Ctrl+V
    }
  });

  const aoColar = async (event: ClipboardEvent) => {
    const texto = event.clipboardData?.getData('text') || '';
    const padrao = /^[A-Za-z]+\d+$/;
    if (!padrao.test(texto.trim())) return;

    event.preventDefault();
    codigoTarefaInput.value = texto.trim().toUpperCase();
    await processarCodigoTarefa();
    await nextTick();
    campoDescricao.value?.focus();
  };

  const colarDoClipboard = async () => {
    try {
      const texto = await navigator.clipboard.readText();
      const padrao = /^[A-Za-z]+\d+$/;
      if (padrao.test(texto.trim())) {
        codigoTarefaInput.value = texto.trim().toUpperCase();
        await processarCodigoTarefa();
        await nextTick();
        campoDescricao.value?.focus();
      }
    } catch {
      notificar('aviso', 'Não foi possível ler a área de transferência');
    }
  };

  function extrairIniciais(codigo: string): string {
    const match = codigo.match(/^([A-Za-z]+)/);
    return match ? match[1].toUpperCase() : '';
  }

  async function processarCodigoTarefa(): Promise<void> {
    const codigo = codigoTarefaInput.value?.trim();
    if (!codigo) return;

    const iniciais = extrairIniciais(codigo);
    if (!iniciais) return;

    try {
      const resultado = await RepositoriosService.buscarCodigoTarefa(iniciais);
      if (!resultado) return;

      const { codigoTarefa, repositorio } = resultado;

      if (repositorio) {
        clone.repositorio = repositorio;
      }

      // Prioriza pasta centralizadora do código de tarefa, senão usa a do repositório
      const pastaCentralizadora =
        codigoTarefa.pastaCentralizadora || repositorio.pastaCentralizadora;
      clone.diretorioRaiz = pastaCentralizadora
        ? `${configuracaoStore.diretorioRaiz}${featuresStore.pathSeparator}${pastaCentralizadora}${featuresStore.pathSeparator}`
        : `${configuracaoStore.diretorioRaiz}${featuresStore.pathSeparator}`;

      if (codigoTarefa.usarTarefaComoBranch) clone.branch = codigo;
      else
        clone.branch =
          repositorio.branchBase ||
          codigoTarefa.branchPrincipal ||
          clone.branch;

      if (clone.branch) {
        salvarBranchNoLocalStorage(clone.branch);
        salvarBranchBaseRepositorio(clone.branch);
      }

      clone.criarBranchRemoto = codigoTarefa.criarBranchRemoto;
      clone.baixarAgregados = codigoTarefa.clonarAgregados;
      clone.historicoCompleto = codigoTarefa.baixarHistoricoCompleto;

      if (!codigoTarefa.habilitarTipos) clone.tipo = 'nenhum';

      if (!clone.criarBranchRemoto) await validarBranch();
    } catch {
      notificar('erro', 'Erro ao buscar código da tarefa');
    }
  }

  const ehBranchBase = (branch: string): boolean =>
    BRANCHES_BASE.includes(branch.toLowerCase());

  const validarBranch = async (): Promise<void> => {
    if (clone.criarBranchRemoto) {
      branchInvalida.value = false;
      hintBranch.value = '';
      return;
    }

    if (!clone.branch || !repositorioSelecionado.value) return;
    if (ehBranchBase(clone.branch)) {
      branchInvalida.value = false;
      hintBranch.value = '';
      return;
    }

    validandoBranch.value = true;
    try {
      const url = clone.repositorio.url ?? '';
      const existe = await CloneService.verificarBranch(url, clone.branch);
      branchInvalida.value = !existe;
      hintBranch.value = existe ? '' : 'Branch não encontrada no remote';
    } catch {
      branchInvalida.value = false;
      hintBranch.value = '';
    } finally {
      validandoBranch.value = false;
    }
  };

  const formularioValido = async (): Promise<boolean> => {
    const form = await formClone.value.validate();
    return form.valid;
  };

  const clonar = async (): Promise<void> => {
    if (!(await formularioValido())) return;

    const payload = {
      ...clone,
      repositorioId: clone.repositorio.identificador
    };
    payload.codigo = clone.codigo.toUpperCase();

    salvarUltimoRepositorio();

    if (clone.salvarNoStorage) {
      adicionarNoLocalStorage.value = true;
      await nextTick(() => (adicionarNoLocalStorage.value = false));
    }

    fecharClone();
    notificar('sucesso', 'Clone iniciado');
    setTimeout(atualizarListaPastas, 5000);

    CloneService.clonar(payload).catch(() => {
      notificar('erro', 'Falha ao clonar');
    });
  };

  const salvarBranchNoLocalStorage = (branch: string): void => {
    if (!branch) return;

    const chaveBranchs = 'branchs';
    const branchsSalvas = localStorage.getItem(chaveBranchs);
    const lista: string[] = branchsSalvas ? JSON.parse(branchsSalvas) : [];

    if (!lista.includes(branch)) {
      lista.push(branch);
      localStorage.setItem(chaveBranchs, JSON.stringify(lista));
    }
  };

  const recuperarBranchDoLocalStorage = (): string | null => {
    if (!clone.repositorio?.identificador) return null;
    return localStorage.getItem(
      `${CHAVE_BRANCH_BASE_PREFIXO}${clone.repositorio.identificador}`
    );
  };

  const salvarBranchBaseRepositorio = (branch: string): void => {
    if (!clone.repositorio?.identificador || !branch) return;
    localStorage.setItem(
      `${CHAVE_BRANCH_BASE_PREFIXO}${clone.repositorio.identificador}`,
      branch
    );
  };

  const salvarUltimoRepositorio = (): void => {
    if (!clone.repositorio?.identificador) return;

    localStorage.setItem(
      CHAVE_ULTIMO_REPOSITORIO,
      clone.repositorio.identificador
    );
  };

  const fecharClone = (): void => {
    exibirModalClone.value = false;
    Object.assign(clone, new CloneModel());
    clone.diretorioRaiz = `${configuracaoStore.diretorioRaiz}${featuresStore.pathSeparator}`;
    codigoTarefaInput.value = '';
    branchInvalida.value = false;
    hintBranch.value = '';
  };
</script>

<style scoped>
  .uppercase-input :deep(input) {
    text-transform: uppercase;
  }
</style>
