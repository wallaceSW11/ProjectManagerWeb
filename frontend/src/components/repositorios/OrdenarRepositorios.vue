<template>
  <IconeComTooltip
    icone="mdi-sort"
    texto="Ordenar repositórios"
    :acao="abrirModal"
  />

  <ModalPadrao
    v-model="exibirModal"
    titulo="Ordenar Repositórios"
    textoBotaoPrimario="Salvar"
    textoBotaoSecundario="Cancelar"
    :acaoBotaoPrimario="salvar"
    :acaoBotaoSecundario="fechar"
    larguraMinima="400px"
  >
    <p class="text-caption text-medium-emphasis mb-3">
      Arraste para reordenar. A ordem será salva e refletida no select de repositórios.
    </p>

    <draggable
      v-model="listaLocal"
      item-key="identificador"
      :animation="200"
      handle=".drag-handle"
    >
      <template #item="{ element }">
        <v-card
          class="mb-2 d-flex align-center px-3 py-2"
          style="background-color: #2d2d30; cursor: default"
        >
          <v-icon class="drag-handle mr-3" style="cursor: grab" color="grey">
            mdi-drag
          </v-icon>
          <div
            v-if="element.cor"
            class="mr-3"
            :style="{
              width: '6px',
              height: '32px',
              borderRadius: '3px',
              backgroundColor: element.cor,
              flexShrink: 0,
            }"
          />
          <span>{{ element.titulo }}</span>
        </v-card>
      </template>
    </draggable>
  </ModalPadrao>
</template>

<script setup lang="ts">
  import { ref, watch } from 'vue';
  import type { IRepositorio } from '@/types';
  import RepositoriosService from '@/services/RepositoriosService';
  import draggable from 'vuedraggable';
  import { notificar } from '@/utils/eventBus';

  interface Props {
    repositorios: IRepositorio[];
  }

  const props = defineProps<Props>();
  const emit = defineEmits<{ ordenado: [] }>();

  const exibirModal = ref<boolean>(false);
  const listaLocal = ref<IRepositorio[]>([]);

  watch(exibirModal, (aberto) => {
    if (aberto) listaLocal.value = [...props.repositorios];
  });

  const abrirModal = (): void => {
    exibirModal.value = true;
  };

  const salvar = async (): Promise<void> => {
    try {
      const indices = listaLocal.value.map((r, index) => ({
        identificador: r.identificador,
        indice: index,
      }));

      await RepositoriosService.atualizarOrdem(indices);
      notificar('sucesso', 'Ordem salva');
      emit('ordenado');
      fechar();
    } catch (error) {
      console.error('Falha ao salvar ordem:', error);
      notificar('erro', 'Falha ao salvar a ordem');
    }
  };

  const fechar = (): void => {
    exibirModal.value = false;
  };
</script>
