<template>
  <v-col cols="12">
    <h2>{{ titulo }}</h2>
    <v-divider class="py-2" />

    <!-- BOTÕES SUPERIORES -->
    <div
      :class="[
        'd-flex align-center',
        emModoInicial ? 'justify-space-between' : 'justify-end',
      ]"
      style="height: 70px"
    >
      <div>
        <v-btn @click="emModoInicial ? iniciarCadastro() : salvar()">
          <v-icon>{{ emModoInicial ? "mdi-plus" : "mdi-check" }}</v-icon>
          {{ emModoInicial ? "Adicionar" : "Salvar" }}
        </v-btn>

        <v-btn
          v-if="emModoCadastroEdicao"
          variant="plain"
          class="ml-2"
          @click="cancelar"
        >
          <v-icon>mdi-cancel</v-icon>
          Cancelar
        </v-btn>
      </div>
    </div>

    <!-- ÁREA DE LISTA E FORM -->
    <v-tabs-window v-model="pagina">
      <!-- LISTA -->
      <v-tabs-window-item>
        <v-data-table :headers="headers" :items="items" hide-default-footer>
          <template #[`item.actions`]="{ item }">
            <v-btn icon @click="editar(item)">
              <v-icon>mdi-pencil</v-icon>
            </v-btn>
            <v-btn icon @click="$emit('delete', item)">
              <v-icon>mdi-delete</v-icon>
            </v-btn>
          </template>
        </v-data-table>
      </v-tabs-window-item>

      <!-- FORM -->
      <v-tabs-window-item>
        <v-form ref="formRef">
          <component
            v-for="(field, i) in formFields"
            :key="i"
            :is="field.component"
            :model-value="getModel(field.model).value"
            @update:model-value="(val) => (getModel(field.model).value = val)"
            v-bind="field.props"
          />
        </v-form>
      </v-tabs-window-item>
    </v-tabs-window>
  </v-col>
</template>

<script setup>
import { ref, reactive } from "vue";

const props = defineProps({
  titulo: { type: String, required: true },
  headers: { type: Array, required: true },
  items: { type: Array, required: true },
  formFields: { type: Array, required: true }, // [{ model: 'nome', component: 'v-text-field', props: {...} }]
  // initialForm: { type: Object, required: true },
});

const emit = defineEmits(["save", "delete"]);

// Estados
const pagina = ref(0);
const formRef = ref(null);
const formData = reactive({ ...props.initialForm });

const emModoInicial = ref(true);
const emModoCadastroEdicao = ref(false);

// Métodos
function iniciarCadastro() {
  Object.assign(formData, props.initialForm); // reset
  emModoInicial.value = false;
  emModoCadastroEdicao.value = true;
  pagina.value = 1;
}

function editar(item) {
  Object.assign(formData, JSON.parse(JSON.stringify(item)));
  emModoInicial.value = false;
  emModoCadastroEdicao.value = true;
  pagina.value = 1;
}

async function salvar() {
  const valido = await formRef.value?.validate();
  if (valido) {
    emit("save", { ...formData });
    reset();
  }
}

function cancelar() {
  reset();
}

function reset() {
  emModoInicial.value = true;
  emModoCadastroEdicao.value = false;
  pagina.value = 0;
  Object.assign(formData, props.initialForm);
}

/**
 * Permite acessar propriedades aninhadas no formData (ex: comandos.instalar)
 */
function getModel(path) {
  return {
    get value() {
      return path.split(".").reduce((acc, key) => acc?.[key], formData);
    },
    set value(val) {
      const keys = path.split(".");
      let target = formData;
      for (let i = 0; i < keys.length - 1; i++) {
        target = target[keys[i]];
      }
      target[keys[keys.length - 1]] = val;
    },
  };
}
</script>
