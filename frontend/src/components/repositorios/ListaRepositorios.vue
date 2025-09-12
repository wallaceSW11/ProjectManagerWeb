<template>
  <div>
    <div v-if="!itens.length">Nenhum repositório cadastrado</div>

    <div v-else>
      <v-card
        v-for="repositorio in itens"
        :key="repositorio.id"
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

            <v-col cols="12" class="pt-2">
              <p>Projetos</p>
              {{ repositorio.projetos.map((p) => p.nome).join(", ") }}
            </v-col>
          </v-row>
        </v-card-text>

        <v-card-actions class="d-flex justify-end">
          <div>
            <v-btn icon @click="emit('editar', repositorio.id)">
              <v-icon>mdi-pencil</v-icon>
              <v-tooltip activator="parent" location="top">Editar</v-tooltip>
            </v-btn>

            <v-btn icon @click="emit('excluir', repositorio)">
              <v-icon>mdi-delete</v-icon>
              <v-tooltip activator="parent" location="top">Excluir</v-tooltip>
            </v-btn>
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
