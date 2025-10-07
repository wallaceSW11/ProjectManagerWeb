<template>
  <v-form ref="formRepositorio">
    <v-row no-gutters>
      <v-col cols="12">
        <v-text-field
          label="Url"
          v-model="repositorio.url"
          :rules="obrigatorio"
          @change="atualizarNomeRepositorio()"
        />
      </v-col>

      <v-col cols="12">
        <v-text-field
          label="Nome"
          v-model="repositorio.nome"
          :rules="obrigatorio"
        />
      </v-col>

      <v-col cols="12">
        <v-text-field
          label="Título"
          v-model="repositorio.titulo"
          :rules="obrigatorio"
        />
      </v-col>

      <v-col cols="12">
        <v-select
          label="Agregados"
          v-model="repositorio.agregados"
          :items="repositoriosDisponiveis"
          item-title="nome"
          item-value="identificador"
          multiple
          chips
          clearable
        />
      </v-col>
    </v-row>
  </v-form>
</template>

<script setup lang="ts">
  import { computed } from 'vue'
  import type { IRepositorio } from '@/types'

  interface Props {
    repositorios: IRepositorio[]
  }

  const props = defineProps<Props>()
  const repositorio = defineModel<IRepositorio>({ required: true })

  const obrigatorio = [(v: string) => !!v || 'Obrigatório']

  const repositoriosDisponiveis = computed(() => {
    return props.repositorios.filter(
      r => r.identificador !== repositorio.value.identificador
    )
  })

  const atualizarNomeRepositorio = (): void => {
    if (!repositorio.value.url) {
      repositorio.value.nome = ''
      return
    }

    const partesUrl = repositorio.value.url.split('/')
    let nomeExtraido = partesUrl.pop() || ''

    if (nomeExtraido.endsWith('.git')) {
      nomeExtraido = nomeExtraido.slice(0, -4)
    }

    repositorio.value.nome = nomeExtraido
  }
</script>
