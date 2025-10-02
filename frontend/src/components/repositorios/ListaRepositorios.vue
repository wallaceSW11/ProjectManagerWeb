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
          {{ repositorio.nome }}
          <v-divider />
        </v-card-title>

        <v-card-text>
          <v-row no-gutters>
            <v-col cols="12">
              {{ repositorio.url }}
            </v-col>

            <v-col cols="12" class="pt-3">
              <h3>Projetos</h3>
              {{ repositorio.projetos.map((p) => p.nome).join(", ") }}
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

<script setup>
const emit = defineEmits(["editar", "excluir"]);

const props = defineProps({
  itens: {
    type: Array,
    required: true,
  },
});
</script>
