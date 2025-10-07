<template>
  <v-col cols="12">
    <div>
      <h2>Comandos</h2>
    </div>

    <div class="d-flex flex-column justify-center">
      <div class="d-flex align-center">
        <v-text-field
          label="Comando"
          v-model="comando"
          autocomplete="new-password"
        />
        <v-btn
          class="ml-2"
          @click="adicionarComando"
        >
          <v-icon>mdi-plus</v-icon>
          Adicionar
        </v-btn>
      </div>

      <div>
        <v-data-table
          :items="menuSelecionado.comandos"
          :headers="colunas"
          hide-default-footer
        >
          <template #[`item.actions`]="{ item }">
            <v-icon @click="editarComando(item)">mdi-pencil</v-icon>
            <v-icon @click="removerComando(item)">mdi-delete</v-icon>
          </template>
        </v-data-table>
      </div>
    </div>
  </v-col>
</template>

<script setup>
  import MenuModel from '@/models/MenuModel'
  import { reactive, ref } from 'vue'

  const colunas = reactive([
    { title: 'Comando', value: 'comando' },
    { title: 'Actions', key: 'actions', align: 'center', width: '100px' },
  ])

  const menuSelecionado = defineModel(new MenuModel())

  const comando = ref('') // input do comando
  const comandos = ref([]) // lista de comandos

  const adicionarComando = () => {
    if (comando.value && comando.value.trim() !== '') {
      menuSelecionado.value.comandos.push({ comando: comando.value.trim() })
      comando.value = ''
    }
  }

  const editarComando = item => {
    comando.value = item.comando
    removerComando(item)
  }

  const removerComando = item => {
    const index = menuSelecionado.value.comandos.indexOf(item)
    if (index > -1) {
      menuSelecionado.value.comandos.splice(index, 1)
    }
  }
</script>
