<template>
  <IconeComTooltip
    v-if="temOcultas"
    icone="mdi-eye-off-outline"
    :texto="`Pastas ocultas (${diretoriosOcultos.length})`"
    :acao="abrirModal"
    class="ml-2"
  />

  <ModalPadrao
    v-model="exibirModal"
    titulo="Pastas Ocultas"
    :exibir-botao-primario="false"
    textoBotaoSecundario="Fechar"
    :acaoBotaoSecundario="fechar"
  >
    <div v-if="diretoriosOcultos.length === 0" class="text-medium-emphasis py-2">
      Nenhuma pasta oculta.
    </div>

    <div v-else>
      <p class="text-caption text-medium-emphasis mb-3">
        Clique em restaurar para que a pasta volte a aparecer na listagem.
      </p>

      <v-card
        v-for="diretorio in diretoriosOcultos"
        :key="diretorio"
        class="mb-2 d-flex align-center px-3 py-2 justify-space-between"
        style="background-color: #2d2d30"
      >
        <div class="d-flex align-center gap-2">
          <v-icon size="small" color="grey">mdi-folder-off</v-icon>
          <span class="text-body-2 pl-2">{{ diretorio }}</span>
        </div>

        <IconeComTooltip
          icone="mdi-eye"
          texto="Restaurar pasta"
          :acao="() => restaurar(diretorio)"
        />
      </v-card>
    </div>
  </ModalPadrao>
</template>

<script setup lang="ts">
  import { computed, onMounted, ref } from 'vue';
  import PastasService from '@/services/PastasService';
  import { notificar } from '@/utils/eventBus';

  const emit = defineEmits<{ atualizar: [] }>();

  const exibirModal = ref<boolean>(false);
  const diretoriosOcultos = ref<string[]>([]);

  const temOcultas = computed(() => diretoriosOcultos.value.length > 0);

  onMounted(async () => {
    await carregarOcultas();
  });

  const abrirModal = async (): Promise<void> => {
    await carregarOcultas();
    exibirModal.value = true;
  };

  const carregarOcultas = async (): Promise<void> => {
    try {
      diretoriosOcultos.value = await PastasService.getOcultas();
    } catch (error) {
      console.error('Falha ao carregar pastas ocultas:', error);
    }
  };

  const restaurar = async (diretorio: string): Promise<void> => {
    const confirmar = confirm(`Deseja exibir novamente a pasta "${diretorio}"?`);
    if (!confirmar) return;

    try {
      await PastasService.restaurar(diretorio);
      diretoriosOcultos.value = diretoriosOcultos.value.filter(d => d !== diretorio);
      notificar('sucesso', 'Pasta restaurada');
      emit('atualizar');

      if (diretoriosOcultos.value.length === 0) fechar();
    } catch (error) {
      console.error('Falha ao restaurar pasta:', error);
      notificar('erro', 'Falha ao restaurar pasta');
    }
  };

  const fechar = (): void => {
    exibirModal.value = false;
  };

  defineExpose({ recarregar: carregarOcultas });
</script>
