<template>
  <v-col cols="12">
    <div class="d-flex gap-2 mb-4">
      <v-text-field
        v-model="novoPool"
        label="Nome do Pool de Aplicação"
        placeholder="seu-site"
        @keyup.enter="adicionarPool"
        hide-details
      />
      <BotaoTerciario
        texto="Adicionar"
        icone="mdi-plus"
        @click="adicionarPool"
      />
    </div>

    <v-list v-if="site.poolsAplicacao.length > 0">
      <v-list-item
        v-for="(pool, index) in site.poolsAplicacao"
        :key="index"
        class="mb-2 border rounded"
      >
        <template v-slot:prepend>
          <v-icon>mdi-application-cog</v-icon>
        </template>
        <v-list-item-title>{{ pool }}</v-list-item-title>
        <template v-slot:append>
          <IconeComTooltip
            icone="mdi-delete"
            texto="Excluir"
            :acao="() => removerPool(index)"
            top
          />
        </template>
      </v-list-item>
    </v-list>

    <v-alert v-else type="info" variant="tonal" class="mt-4">
      Nenhum pool de aplicação configurado
    </v-alert>
  </v-col>
</template>

<script setup lang="ts">
  import { ref } from 'vue';
  import type { ISiteIIS } from '@/models/SiteIISModel';

  const site = defineModel<ISiteIIS>({ required: true });

  const novoPool = ref('');

  const adicionarPool = () => {
    if (!novoPool.value.trim()) return;
    if (!site.value.poolsAplicacao.includes(novoPool.value.trim())) {
      site.value.poolsAplicacao.push(novoPool.value.trim());
    }
    novoPool.value = '';
  };

  const removerPool = (index: number) => {
    site.value.poolsAplicacao.splice(index, 1);
  };
</script>

<style scoped>
  .gap-2 {
    gap: 8px;
  }

  .border {
    border: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
  }
</style>
