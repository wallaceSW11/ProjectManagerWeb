<template>
  <div>
    pastas {{ configuracao.diretorioRaiz }}
  </div>
</template>

<script setup>
import { onMounted, reactive } from 'vue';
import ConfiguracaoModel from '../models/ConfiguracaoModel';
import ConfiguracaoService from '../services/ConfiguracaoService';

let configuracao = reactive(new ConfiguracaoModel());

async function consultarConfiguracao() {
  try {
    let response = await ConfiguracaoService.getConfiguracao();
    Object.assign(configuracao, new ConfiguracaoModel(response));  
  } catch (error) {
    console.log('Falha ao consultar as configurações', error)
  }
}


onMounted(() => {
  consultarConfiguracao();

  // if não tem diretorio raiz, apresenta mensagem
  // sem tem, busca as pastas do diretorio raiz
})
</script>