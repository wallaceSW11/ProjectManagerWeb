<template>
  <v-form ref="form">
    <v-row no-gutters>
      <v-col cols="12">
        <v-text-field
          label="Nome"
          v-model="repositorio.nome"
          :rules="obrigatorio"
        />
      </v-col>

      <v-col cols="12">
        <v-text-field
          label="Url"
          v-model="repositorio.url"
          :rules="obrigatorio"
        />
      </v-col>

      <v-col cols="12">
        <div class="pb-2">
          <v-btn @click="() => {}">
            <v-icon>mdi-plus</v-icon>
            Adicionar
          </v-btn>
        </div>

        <v-tabs-window v-model="pagina">
          <v-tabs-window-item>
            <v-data-table
              :headers="colunas"
              :items="repositorio.projetos"
              hide-default-footer
            >
              <template #[`item.actions`]>
                <v-icon @click="() => {}">mdi-pencil</v-icon>
                <v-icon @click="() => {}">mdi-delete</v-icon>
              </template>
            </v-data-table>
          </v-tabs-window-item>

          <v-tabs-window-item> cadastro </v-tabs-window-item>
        </v-tabs-window>
      </v-col>
    </v-row>
  </v-form>
</template>

<script setup>
import { reactive, ref } from "vue";
import RepositorioModel from "../../models/RepositorioModel";

const repositorio = defineModel(new RepositorioModel());

const obrigatorio = [(v) => !!v || "Obrigatório"];

const pagina = ref(0);

const colunas = reactive([
  { title: "Nome", key: "nome", align: "start" },
  { title: "Subdiretorio", key: "subdiretorio", align: "start" },
  { title: "Perfil VS Code", key: "perfilVScode", align: "start" },
  { title: "Actions", key: "actions", align: "center", width: "200px" },
]);
</script>
