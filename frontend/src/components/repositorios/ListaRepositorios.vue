<template>
  <div>
    <div v-if="!modelValue.length">Nenhum repositório cadastrado</div>

    <draggable
      v-else
      v-model="lista"
      item-key="identificador"
      :animation="200"
      handle=".drag-handle"
      @end="salvarOrdem"
    >
      <template #item="{ element }">
        <v-card class="mb-2">
          <v-card-title class="d-flex align-center pa-2">
            <v-icon
              class="drag-handle mr-2"
              style="cursor: grab"
              color="grey"
            >
              mdi-drag
            </v-icon>
            <span class="ml-1">{{ element.titulo }}</span>
            <v-divider class="ml-2" />
          </v-card-title>

          <v-card-text>
            <v-row no-gutters>
              <v-col
                cols="12"
                class="d-flex align-center"
              >
                <span class="flex-grow-1">{{ element.url }}</span>
                <IconeComTooltip
                  v-if="element.url"
                  icone="mdi-content-copy"
                  texto="Copiar link"
                  :acao="() => copiarParaAreaTransferencia(element.url!)"
                  top
                />
              </v-col>

              <v-col
                cols="12"
                class="pt-3"
              >
                <h3>Projetos</h3>
                {{ element.projetos.map((p: IProjeto) => p.nome).join(', ') }}
              </v-col>
            </v-row>
          </v-card-text>

          <v-card-actions class="d-flex justify-end">
            <div>
              <IconeComTooltip
                icone="mdi-pencil"
                texto="Editar"
                :acao="() => emit('editar', element.identificador)"
                top
              />
              <IconeComTooltip
                icone="mdi-content-copy"
                texto="Duplicar"
                :acao="() => emit('duplicar', element)"
                top
              />
              <IconeComTooltip
                icone="mdi-delete"
                texto="Excluir"
                :acao="() => emit('excluir', element)"
                top
              />
            </div>
          </v-card-actions>
        </v-card>
      </template>
    </draggable>
  </div>
</template>

<script setup lang="ts">
  import { computed } from 'vue';
  import type { IRepositorio, IProjeto } from '@/types';
  import draggable from 'vuedraggable';
  import { notificar } from '@/utils/eventBus';

  interface Props {
    modelValue: IRepositorio[];
  }

  const props = defineProps<Props>();
  const emit = defineEmits<{
    editar: [identificador: string];
    excluir: [repositorio: IRepositorio];
    duplicar: [repositorio: IRepositorio];
    ordenado: [repositorios: IRepositorio[]];
    'update:modelValue': [repositorios: IRepositorio[]];
  }>();

  const lista = computed({
    get: () => props.modelValue,
    set: val => emit('update:modelValue', val)
  });

  const salvarOrdem = (): void => {
    emit('ordenado', props.modelValue);
  };

  const copiarParaAreaTransferencia = async (texto: string): Promise<void> => {
    try {
      await navigator.clipboard.writeText(texto);
      notificar('sucesso', 'Link copiado para a área de transferência');
    } catch (error) {
      console.error('Erro ao copiar para área de transferência:', error);
      notificar('erro', 'Falha ao copiar link');
    }
  };
</script>
