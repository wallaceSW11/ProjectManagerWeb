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
          <v-icon>{{ emModoInicial ? 'mdi-plus' : 'mdi-check' }}</v-icon>
          {{ emModoInicial ? 'Adicionar' : 'Salvar' }}
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
        <v-data-table
          :headers="headers"
          :items="items"
          hide-default-footer
        >
          <template #[`item.actions`]="{ item }">
            <v-btn
              icon
              @click="editar(item)"
            >
              <v-icon>mdi-pencil</v-icon>
            </v-btn>
            <v-btn
              icon
              @click="$emit('delete', item)"
            >
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
            @update:model-value="updateFieldValue(field.model, $event)"
            v-bind="field.props"
          />
        </v-form>
      </v-tabs-window-item>
    </v-tabs-window>
  </v-col>
</template>

<script setup lang="ts">
  import { ref, reactive } from 'vue'

  interface FormField {
    model: string
    component: string
    props: Record<string, any>
  }

  interface Props {
    titulo: string
    headers: readonly any[]
    items: any[]
    formFields: FormField[]
    initialForm?: Record<string, any>
  }

  const props = withDefaults(defineProps<Props>(), {
    initialForm: () => ({}),
  })

  const emit = defineEmits<{
    save: [data: any]
    delete: [item: any]
  }>()

  // Estados
  const pagina = ref<number>(0)
  const formRef = ref<any>(null)
  const formData = reactive<Record<string, any>>({ ...props.initialForm })

  const emModoInicial = ref<boolean>(true)
  const emModoCadastroEdicao = ref<boolean>(false)

  // Métodos
  function iniciarCadastro(): void {
    Object.assign(formData, props.initialForm) // reset
    emModoInicial.value = false
    emModoCadastroEdicao.value = true
    pagina.value = 1
  }

  function editar(item: any): void {
    Object.assign(formData, JSON.parse(JSON.stringify(item)))
    emModoInicial.value = false
    emModoCadastroEdicao.value = true
    pagina.value = 1
  }

  async function salvar(): Promise<void> {
    const valido = await formRef.value?.validate()
    if (valido) {
      emit('save', { ...formData })
      reset()
    }
  }

  function cancelar(): void {
    reset()
  }

  function reset(): void {
    emModoInicial.value = true
    emModoCadastroEdicao.value = false
    pagina.value = 0
    Object.assign(formData, props.initialForm)
  }

  function updateFieldValue(path: string, value: any): void {
    getModel(path).value = value
  }

  /**
   * Permite acessar propriedades aninhadas no formData (ex: comandos.instalar)
   */
  function getModel(path: string) {
    return {
      get value() {
        return path
          .split('.')
          .reduce((acc: any, key: string) => acc?.[key], formData)
      },
      set value(val: any) {
        const keys = path.split('.')
        let target = formData
        for (let i = 0; i < keys.length - 1; i++) {
          target = target[keys[i]]
        }
        target[keys[keys.length - 1]] = val
      },
    }
  }
</script>
