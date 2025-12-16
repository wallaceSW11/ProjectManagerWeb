<template>
  <div>
    <div v-if="!itens.length">Nenhum repositório cadastrado</div>

    <div v-else>
      <v-card
        v-for="repositorio in itens"
        :key="repositorio.identificador"
        class="mb-2"
      >
        <v-card-title>
          {{ repositorio.titulo }}
          <v-divider />
        </v-card-title>

        <v-card-text>
          <v-row no-gutters>
            <v-col cols="12" class="d-flex align-center">
              <span class="flex-grow-1">{{ repositorio.url }}</span>
              <IconeComTooltip
                icone="mdi-content-copy"
                texto="Copiar link"
                :acao="() => copiarParaAreaTransferencia(repositorio.url)"
                top
              />
            </v-col>

            <v-col
              cols="12"
              class="pt-3"
            >
              <h3>Projetos</h3>
              {{ repositorio.projetos.map(p => p.nome).join(', ') }}
            </v-col>
          </v-row>
        </v-card-text>

        <v-card-actions class="d-flex justify-end">
          <div>
            <IconeComTooltip
              icone="mdi-pencil"
              texto="Editar"
              :acao="() => emit('editar', repositorio.identificador)"
              top
            />
            <IconeComTooltip
              icone="mdi-delete"
              texto="Excluir"
              :acao="() => emit('excluir', repositorio)"
              top
            />
          </div>
        </v-card-actions>
      </v-card>
    </div>
  </div>
</template>

<script setup lang="ts">
  import type { IRepositorio } from '@/types';
  import { notificar } from '@/utils/eventBus';

  interface Props {
    itens: IRepositorio[];
  }

  defineProps<Props>();

  const emit = defineEmits<{
    editar: [identificador: string];
    excluir: [repositorio: IRepositorio];
  }>();

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
