<template>
  <div>
    <div v-if="!itens.length">Nenhum site cadastrado</div>

    <div v-else>
      <v-card
        v-for="site in itens"
        :key="site.identificador"
        class="mb-2"
      >
        <v-card-title>
          {{ site.titulo }}
          <v-divider />
        </v-card-title>

        <v-card-text>
          <v-row no-gutters>
            <v-col cols="12">
              {{ site.pastaRaiz }}
            </v-col>

            <v-col
              cols="12"
              class="pt-3"
            >
              <h3>Pastas Deploy</h3>
              {{ site.quantidadePastas }} pasta(s) configurada(s)
            </v-col>

            <v-col
              cols="12"
              class="pt-2"
            >
              <h3>Pools de Aplicação</h3>
              {{ site.quantidadePools }} pool(s) configurado(s)
            </v-col>
          </v-row>
        </v-card-text>

        <v-card-actions class="d-flex justify-end">
          <div>
            <IconeComTooltip
              icone="mdi-pencil"
              texto="Editar"
              :acao="() => emit('editar', site.identificador)"
              top
            />
            <IconeComTooltip
              icone="mdi-delete"
              texto="Excluir"
              :acao="() => emit('excluir', site)"
              top
            />
          </div>
        </v-card-actions>
      </v-card>
    </div>
  </div>
</template>

<script setup lang="ts">
  import type { ISiteIISDeployResponse } from '@/models/SiteIISModel';

  interface Props {
    itens: ISiteIISDeployResponse[];
  }

  defineProps<Props>();

  const emit = defineEmits<{
    editar: [identificador: string];
    excluir: [site: ISiteIISDeployResponse];
  }>();
</script>
