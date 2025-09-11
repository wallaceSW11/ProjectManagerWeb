<template>
  <v-container>
    <v-row no-gutters>
      <v-col cols="12">
        <div class="d-flex justify-space-between">
          <div>
            <h1>Configurações</h1>
          </div>

          <!-- <div>
            <v-btn color="primary" class="pr-2" variant="outlined">
              <v-icon>mdi-check</v-icon>
              Salvar
            </v-btn>
            <v-btn color="tertiary" variant="plain">
              <v-icon>mdi-cancel</v-icon>
              Fechar
            </v-btn>
          </div> -->
        </div>
        
      </v-col>

      <v-col cols="12">
        <v-row no-gutters>
          <v-col cols="12">
            <v-text-field label="Diretório raiz"  v-model="configuracao.diretorioRaiz" />
          </v-col>

          <v-divider />

          <v-col cols="12">
            <div class="d-flex flex-column justify-center">
              <div class="d-flex align-center">
                <v-text-field label="Perfil"/>
                <v-btn class="ml-2">
                  <v-icon>mdi-plus</v-icon>
                  Adicionar
                </v-btn>
              </div>

              <div>
                <v-data-table 
                  :items="configuracao.perfisVSCode"
                  :headers="colunas"
                  hide-default-footer
                >

                  <template #[`item.actions`]>
                    <v-icon icon="mdi-pencil" @click="() => {}"/>
                    <v-icon icon="mdi-delete" @click="() => {}"/>
                  </template>

                </v-data-table>
              </div>
            </div>
          </v-col>
        </v-row>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup>
import { onMounted, reactive, ref } from 'vue';
import ConfiguracaoModel from '../models/ConfiguracaoModel';
import ConfiguracaoService from '../services/ConfiguracaoService';

let configuracao = reactive(new ConfiguracaoModel());
const colunas = ref([
  {
    title: "Perfil",
    key: "nome",
    align: "start"
  },
  {
    title: "Actions",
    key: "actions",
    align: "center",
    width: '200px'
  },
]);

async function consultarConfiguracao() {
  try {
    let response = await ConfiguracaoService.getConfiguracao();
    Object.assign(configuracao, new ConfiguracaoModel(response));  
  } catch (error) {
    console.log('Falha ao consultar as configurações', error)
  }
}

onMounted(async () => {
  consultarConfiguracao();
})
</script>