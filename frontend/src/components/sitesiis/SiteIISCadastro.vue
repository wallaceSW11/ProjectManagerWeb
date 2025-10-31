<template>
  <v-form ref="formSite">
    <v-row no-gutters>
      <v-col cols="12" class="mb-4">
        <v-text-field
          label="Título"
          v-model="site.titulo"
          :rules="obrigatorio"
          hint="Nome que aparecerá no menu de Deploy"
          persistent-hint
        />
      </v-col>

      <v-col cols="12" class="mb-4">
        <v-text-field
          label="Nome"
          v-model="site.nome"
          :rules="obrigatorio"
          hint="Nome do site no IIS"
          persistent-hint
          @blur="sugerirCaminhoPastaRaiz"
        />
      </v-col>

      <v-col cols="12" class="mb-4">
        <v-text-field
          label="Pasta Raiz"
          v-model="site.pastaRaiz"
          :rules="obrigatorio"
          placeholder="C:\inetpub\wwwroot\seu-site"
          hint="Diretório raiz do site no servidor"
          persistent-hint
        />
      </v-col>
    </v-row>
  </v-form>
</template>

<script setup lang="ts">
  import type { ISiteIIS } from '@/models/SiteIISModel';

  const site = defineModel<ISiteIIS>({ required: true });

  const obrigatorio = [(v: any) => !!v || 'Campo obrigatório'];

  const sugerirCaminhoPastaRaiz = () => {
    if (site.value.nome && !site.value.pastaRaiz) {
      site.value.pastaRaiz = `C:\\inetpub\\wwwroot\\${site.value.nome}`;
    }
  };
</script>
