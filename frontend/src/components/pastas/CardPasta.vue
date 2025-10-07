<template>
  <v-card
    :class="[
      'mb-4 mr-2',
      {
        'card-selecionado': pastaSelecionada.diretorio === pasta.diretorio,
      },
    ]"
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
          <div class="ml-2">
            {{ descricaoPasta(pasta) }}
          </div>
        </div>

        <div v-if="!pasta.identificador">
          <IconeComTooltip
            icone="mdi-plus"
            texto="Cadastrar pasta"
            :acao="() => exibirCadastroPasta(pasta)"
            top
          />
        </div>

        <div v-if="pasta.menus.length > 0">
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
        <v-col
          cols="12"
          class="pb-1"
        >
          <v-icon>mdi-folder</v-icon>
          {{ pasta.diretorio }}
        </v-col>

        <v-col
          cols="12"
          class="pt-1"
        >
        </v-col>
      </v-row>
    </v-card-text>
  </v-card>
</template>

<script setup lang="ts">
  import { computed } from 'vue';
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
  }>();

  const selecionarPasta = (pasta: IPasta): void => {
    emit('selecionarPasta', pasta);
  };

  const exibirCadastroPasta = (pasta: IPasta): void => {
    emit('exibirCadastroPasta', pasta);
  };

  const executarMenu = (pasta: IPasta, menuId: string): void => {
    emit('executarMenu', pasta, menuId);
  };

  const descricaoPasta = (pasta: IPasta): string => {
    return pasta.codigo
      ? `${pasta.codigo} - ${pasta.descricao}`
      : pasta.descricao;
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
</script>

<style scoped>
  .card-selecionado {
    border: 1px solid rgb(var(--v-theme-primary));
  }
</style>
