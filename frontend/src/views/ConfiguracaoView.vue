<template>
  <v-container>
    <v-row no-gutters>
      <v-col cols="12">
        <div class="d-flex justify-space-between">
          <div>
            <h1>Configurações</h1>
          </div>
        </div>
      </v-col>

      <v-col cols="12">
        <v-row no-gutters>
          <!-- Diretório Raiz -->
          <v-col cols="12">
            <v-text-field
              label="Diretório raiz"
              v-model="configuracao.diretorioRaiz"
              @change="salvarConfiguracao"
            />
          </v-col>

          <v-divider />

          <!-- Perfis -->
          <v-col cols="12">
            <div class="d-flex flex-column justify-center">
              <!-- Input e botão de adicionar perfil -->
              <div class="d-flex align-center">
                <v-text-field
                  label="Perfil"
                  v-model="nomePerfil"
                  autocomplete="new-password"
                />
                <v-btn class="ml-2" @click="adicionarPerfil">
                  <v-icon>mdi-plus</v-icon>
                  Adicionar
                </v-btn>
              </div>

              <!-- Tabela de perfis -->
              <div>
                <v-data-table
                  :items="configuracao.perfisVSCode"
                  :headers="colunas"
                  hide-default-footer
                >
                  <template #[`item.actions`]="{ item }">
                    <v-icon @click="editarPerfil(item)">mdi-pencil</v-icon>
                    <v-icon @click="removerPerfil(item)">mdi-delete</v-icon>
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
import { onMounted, reactive, ref } from "vue";
import ConfiguracaoModel from "../models/ConfiguracaoModel";
import ConfiguracaoService from "../services/ConfiguracaoService";

// --- STATE ---
const nomePerfil = ref(""); // input do perfil
const configuracao = reactive(new ConfiguracaoModel());

const colunas = ref([
  { title: "Perfil", key: "nome", align: "start" },
  { title: "Actions", key: "actions", align: "center", width: "200px" },
]);

// --- ACTIONS / HELPERS ---
const consultarConfiguracao = async () => {
  try {
    const response = await ConfiguracaoService.getConfiguracao();
    Object.assign(configuracao, new ConfiguracaoModel(response));
  } catch (error) {
    console.error("Falha ao consultar as configurações:", error);
  }
};

const salvarConfiguracao = async () => {
  try {
    await ConfiguracaoService.postConfiguracao(configuracao);
  } catch (error) {
    console.error("Falha ao salvar as configurações:", error);
  }
};

// Valida nome do perfil
const perfilValido = () => {
  if (!nomePerfil.value.trim()) {
    alert("O nome do perfil é obrigatório");
    return false;
  }

  const jaInformado = configuracao.perfisVSCode.some(
    (p) => p.nome === nomePerfil.value
  );

  if (jaInformado) {
    alert("Já existe um perfil com esse nome");
    return false;
  }

  return true;
};

// Adiciona perfil
const adicionarPerfil = () => {
  if (!perfilValido()) return;

  configuracao.perfisVSCode.push({ nome: nomePerfil.value });
  nomePerfil.value = ""; // limpa input

  salvarConfiguracao(); // opcional, salva direto
};

// Editar perfil (exemplo)
const editarPerfil = (item) => {
  const novoNome = prompt("Editar nome do perfil:", item.nome);
  if (novoNome && novoNome.trim()) {
    item.nome = novoNome.trim();
    salvarConfiguracao();
  }
};

// Remover perfil
const removerPerfil = (item) => {
  const confirmDelete = confirm(`Deseja remover o perfil "${item.nome}"?`);
  if (confirmDelete) {
    configuracao.perfisVSCode = configuracao.perfisVSCode.filter(
      (p) => p !== item
    );
    salvarConfiguracao();
  }
};

// --- LIFECYCLE ---
onMounted(() => {
  consultarConfiguracao();
});
</script>
