<template>
  <v-card
    :class="[
      'mb-4 mr-2',
      {
        'card-selecionado': pastaSelecionada.diretorio === pasta.diretorio,
      },
    ]"
    :style="estiloBorda"
    @click="selecionarPasta(pasta)"
  >
    <v-card-title>
      <div class="d-flex justify-space-between">
        <div class="d-flex align-center">
          <div>
            <IconeComTooltip
              :icone="dadosTipo.icone"
              :texto="dadosTipo.titulo"
              :cor="dadosTipo.cor"
              sem-botao
            />
          </div>
          <div class="ml-2 d-flex align-center" style="gap: 4px;">
            <template v-if="temLinkTarefa">
              <a
                :href="linkTarefa"
                target="_blank"
                rel="noopener"
                class="link-tarefa"
                @click.stop
              >
                {{ descricaoPasta(pasta) }}
              </a>
              <v-icon
                size="x-small"
                class="link ml-1"
                @click.stop="copiarLink"
              >
                mdi-content-copy
              </v-icon>
            </template>
            <template v-else>
              {{ descricaoPasta(pasta) }}
            </template>
          </div>
        </div>

        <div v-if="!pasta.descricao" class="d-flex align-center gap-1">
          <IconeComTooltip
            icone="mdi-plus"
            texto="Cadastrar pasta"
            :acao="() => exibirCadastroPasta(pasta)"
            top
          />
          <IconeComTooltip
            icone="mdi-eye-off"
            texto="Ocultar pasta"
            :acao="() => emit('ocultarPasta', pasta.diretorio)"
            top
          />
          <IconeComTooltip
            icone="mdi-delete"
            texto="Excluir pasta"
            cor="error"
            :acao="() => emit('excluirPasta', pasta.diretorio)"
            top
          />
        </div>

        <div v-if="pasta.descricao">
          <IconeComTooltip
            :icone="pasta.fixada ? 'mdi-pin-off' : 'mdi-pin'"
            :texto="pasta.fixada ? 'Desafixar' : 'Fixar'"
            :acao="() => emit('toggleFixar', pasta)"
            top
          />
          <v-menu location="bottom" v-model="menuAberto">
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
                v-for="menu in menusAtivos"
                :key="menu.identificador"
                @click.stop="toggleMenuSelecionado(menu.identificador)"
              >
                <template #prepend>
                  <v-checkbox
                    :model-value="menusSelecionados.includes(menu.identificador)"
                    hide-details
                    density="compact"
                    @click.stop="toggleMenuSelecionado(menu.identificador)"
                  />
                </template>
                <v-list-item-title>
                  <v-icon color="primary" class="px-3">{{ obterIconeMenu(menu.tipo) }}</v-icon>
                  {{ menu.titulo }}
                </v-list-item-title>
              </v-list-item>

              <v-divider v-if="menusSelecionados.length > 0" class="my-2" />

              <v-list-item
                v-if="menusSelecionados.length > 0"
                @click="executarMenusSelecionados"
                class="bg-primary"
              >
                <v-list-item-title class="text-center font-weight-bold">
                  <v-icon>mdi-play</v-icon>
                  Executar ({{ menusSelecionados.length }})
                </v-list-item-title>
              </v-list-item>

              <v-divider v-if="menusAtivos.length > 0" class="my-2" />

              <v-list-item @click.stop="emit('excluirPasta', pasta.diretorio)">
                <v-list-item-title>
                  <v-icon color="error" class="px-3">mdi-delete</v-icon>
                  Excluir pasta
                </v-list-item-title>
              </v-list-item>
            </v-list>
          </v-menu>
        </div>
      </div>
    </v-card-title>

    <v-card-text class="pb-2">
      <v-row no-gutters class="pt-2">
        <v-col
          cols="12"
          class="pb-1"
        >
          <v-icon>mdi-folder</v-icon>
          <span @click="() => abrirDiretorio(pasta.diretorio)" class="link pl-1">{{ pasta.diretorio }}</span>
        </v-col>
      </v-row>
    </v-card-text>
  </v-card>
</template>

<script setup lang="ts">
  import { computed, ref } from 'vue';
  import type { IPasta } from '@/types';

  interface Props {
    pasta: IPasta;
    pastaSelecionada: IPasta;
  }

  const props = defineProps<Props>();

  const emit = defineEmits<{
    selecionarPasta: [pasta: IPasta];
    exibirCadastroPasta: [pasta: IPasta];
    executarMenu: [pasta: IPasta, menuId: string];
    executarMenusMultiplos: [pasta: IPasta, menuIds: string[]];
    abrirDiretorio: [diretorio: string];
    ocultarPasta: [diretorio: string];
    excluirPasta: [diretorio: string];
    toggleFixar: [pasta: IPasta];
  }>(); 

  const menuAberto = ref<boolean>(false);
  const menusSelecionados = ref<string[]>([]);

  const menusAtivos = computed(() => {
    return props.pasta.menus.filter(menu => menu.ativo);
  });

  const selecionarPasta = (pasta: IPasta): void => {
    emit('selecionarPasta', pasta);
  };

  const exibirCadastroPasta = (pasta: IPasta): void => {
    emit('exibirCadastroPasta', pasta);
  };

  const executarMenu = (pasta: IPasta, menuId: string): void => {
    emit('executarMenu', pasta, menuId);
  };

  const toggleMenuSelecionado = (menuId: string): void => {
    const index = menusSelecionados.value.indexOf(menuId);
    if (index > -1) {
      menusSelecionados.value.splice(index, 1);
    } else {
      menusSelecionados.value.push(menuId);
    }
  };

  const executarMenusSelecionados = (): void => {
    if (menusSelecionados.value.length === 0) return;
    
    emit('executarMenusMultiplos', props.pasta, [...menusSelecionados.value]);
    menusSelecionados.value = [];
    menuAberto.value = false;
  };

  import { notificar } from '@/utils/eventBus';

  const descricaoPasta = (pasta: IPasta): string => {
    return pasta.codigo
      ? `${pasta.codigo} - ${pasta.descricao}`
      : pasta.descricao;
  };

  const linkTarefa = computed((): string | undefined => {
    const p = props.pasta;
    if (!p.urlBaseGestorTarefas || !p.codigo) return undefined;
    const base = p.urlBaseGestorTarefas.replace(/\/+$/, '');
    return `${base}/${p.codigo}`;
  });

  const temLinkTarefa = computed((): boolean => !!linkTarefa.value);

  const copiarLink = async (): Promise<void> => {
    const url = linkTarefa.value;
    if (!url) return;
    try {
      await navigator.clipboard.writeText(url);
      notificar('sucesso', 'Link copiado');
    } catch {
      notificar('erro', 'Falha ao copiar link');
    }
  };

  interface TipoInfo {
    icone: string;
    titulo: string;
    cor: string;
  }

  const TIPOS: Record<string, TipoInfo> = {
    NENHUM: {
      icone: 'mdi-set-none',
      titulo: 'Nenhum',
      cor: 'grey',
    },
    FEATURE: {
      icone: 'mdi-creation',
      titulo: 'Melhoria',
      cor: 'green',
    },
    BUG: {
      icone: 'mdi-bug',
      titulo: 'Erro',
      cor: 'red',
    },
    HOTFIX: {
      icone: 'mdi-ambulance',
      titulo: 'Hotfix',
      cor: 'purple',
    },
  };

  const dadosTipo = computed((): TipoInfo => {
    return TIPOS[props.pasta.tipo.toUpperCase()] || TIPOS.NENHUM;
  });

  const estiloBorda = computed(() => {
    const cor = props.pasta.cor || 'rgb(var(--v-theme-primary))';
    const isSelected = props.pastaSelecionada.diretorio === props.pasta.diretorio;
    
    return {
      borderLeft: `6px solid ${cor}`,
      ...(isSelected && { 
        borderTop: `1px solid ${cor}`,
        borderRight: `1px solid ${cor}`,
        borderBottom: `1px solid ${cor}`
      })
    };
  });

  const abrirDiretorio = (diretorio: string): void => {
    emit('abrirDiretorio', diretorio);
  };

  const obterIconeMenu = (tipo: string): string => {
    const icones: Record<string, string> = {
      'APLICAR_ARQUIVO': 'mdi-file',
      'APLICAR_PASTA': 'mdi-folder',
      'COMANDO_AVULSO': 'mdi-console'
    };
    return icones[tipo] || 'mdi-cog';
  };
</script>

<style scoped>
  .link-tarefa {
    color: rgb(var(--v-theme-primary));
    text-decoration: none;
    cursor: pointer;
  }

  .link-tarefa:hover {
    text-decoration: underline;
  }
</style>
