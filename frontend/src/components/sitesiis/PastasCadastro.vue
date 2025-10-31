<template>
  <v-col cols="12">
    <div>
      <BotaoTerciario
        texto="Adicionar"
        icone="mdi-plus"
        @click="prepararParaCadastro"
        class="my-2"
      />
    </div>

    <div>
      <v-data-table
        :headers="colunas"
        :items="site.pastas"
        hide-default-footer
        class="tabela-sem-rodape"
      >
        <template #[`item.actions`]="{ item }">
          <IconeComTooltip
            icone="mdi-pencil"
            texto="Editar"
            :acao="() => mudarParaEdicao(item)"
            top
          />
          <IconeComTooltip
            icone="mdi-delete"
            texto="Excluir"
            :acao="() => excluirPasta(item)"
            top
          />
        </template>
      </v-data-table>
    </div>

    <div>
      <ModalPadrao
        v-model="exibirModalCadastroPasta"
        titulo="Cadastro de Pasta"
        :textoBotaoPrimario="emModoCadastro ? 'Adicionar' : 'Salvar'"
        textoBotaoSecundario="Cancelar"
        :acaoBotaoPrimario="salvarAlteracoes"
        :acaoBotaoSecundario="descartarAlteracoes"
        larguraMaxima="900px"
      >
        <v-form ref="formPasta">
          <v-row no-gutters>
            <v-col cols="12" class="mb-4">
              <v-text-field
                label="Nome da Pasta Destino"
                v-model="pastaEmEdicao.nomePastaDestino"
                :rules="obrigatorio"
                hint="Nome da pasta para backup (timestamp será adicionado)"
                persistent-hint
                @blur="sugerirCaminhosComBaseNaPasta"
              />
            </v-col>
            
            <v-col cols="12" class="mb-4">
              <v-text-field
                label="Diretório de Trabalho"
                v-model="pastaEmEdicao.diretorioTrabalho"
                :rules="obrigatorio"
                placeholder="C:\git\seu-projeto\frontend"
                hint="Onde o comando de build será executado"
                persistent-hint
                @blur="sugerirCaminhoOrigem"
              />
            </v-col>
            
            <v-col cols="12" class="mb-4">
              <v-text-field
                label="Comando de Publish"
                v-model="pastaEmEdicao.comandoPublish"
                :rules="obrigatorio"
                placeholder="npm run build"
                hint="Comando para fazer build/publish do projeto"
                persistent-hint
              />
            </v-col>
            
            <v-col cols="12" class="mb-4">
              <v-text-field
                label="Caminho Origem"
                v-model="pastaEmEdicao.caminhoOrigem"
                :rules="obrigatorio"
                placeholder="C:\git\seu-projeto\frontend\dist"
                hint="Pasta gerada após o build"
                persistent-hint
              />
            </v-col>
            
            <v-col cols="12" class="mb-4">
              <v-text-field
                label="Caminho Destino"
                v-model="pastaEmEdicao.caminhoDestino"
                :rules="obrigatorio"
                placeholder="C:\inetpub\wwwroot\seu-site\seu-micro-frontend"
                hint="Destino no IIS"
                persistent-hint
              />
            </v-col>
          </v-row>
        </v-form>
      </ModalPadrao>
    </div>
  </v-col>
</template>

<script setup lang="ts">
  import { reactive, ref } from 'vue';
  import type { ISiteIIS, IPastaDeploy } from '@/models/SiteIISModel';
  import { PastaDeployModel } from '@/models/SiteIISModel';

  const site = defineModel<ISiteIIS>({ required: true });

  const colunas = [
    { title: 'Nome Pasta', key: 'nomePastaDestino' },
    { title: 'Diretório', key: 'diretorioTrabalho' },
    { title: 'Comando', key: 'comandoPublish' },
    { title: 'Ações', key: 'actions', sortable: false, align: 'center' as const },
  ];

  const obrigatorio = [(v: any) => !!v || 'Campo obrigatório'];

  const exibirModalCadastroPasta = ref(false);
  const pastaEmEdicao = reactive(new PastaDeployModel());
  const formPasta = ref();
  const emModoCadastro = ref(true);

  const prepararParaCadastro = () => {
    Object.assign(pastaEmEdicao, new PastaDeployModel());
    
    // Sugerir caminho de destino baseado na pasta raiz do site
    if (site.value.pastaRaiz) {
      pastaEmEdicao.caminhoDestino = site.value.pastaRaiz;
    }
    
    emModoCadastro.value = true;
    exibirModalCadastroPasta.value = true;
  };

  const mudarParaEdicao = (pasta: IPastaDeploy) => {
    Object.assign(pastaEmEdicao, pasta);
    emModoCadastro.value = false;
    exibirModalCadastroPasta.value = true;
  };

  const sugerirCaminhosComBaseNaPasta = () => {
    if (!pastaEmEdicao.nomePastaDestino) return;

    // Sugerir caminho de destino se estiver vazio
    if (!pastaEmEdicao.caminhoDestino && site.value.pastaRaiz) {
      pastaEmEdicao.caminhoDestino = `${site.value.pastaRaiz}\\${pastaEmEdicao.nomePastaDestino}`;
    }
  };

  const sugerirCaminhoOrigem = () => {
    if (!pastaEmEdicao.diretorioTrabalho) return;

    // Sugerir caminho de origem baseado no diretório de trabalho (completo)
    if (!pastaEmEdicao.caminhoOrigem) {
      pastaEmEdicao.caminhoOrigem = pastaEmEdicao.diretorioTrabalho;
    }
  };

  const salvarAlteracoes = async () => {
    const { valid } = await formPasta.value.validate();
    if (!valid) return;

    const index = site.value.pastas.findIndex(
      (p) => p.identificador === pastaEmEdicao.identificador
    );

    if (index >= 0) {
      site.value.pastas[index] = { ...pastaEmEdicao };
    } else {
      site.value.pastas.push({ ...pastaEmEdicao });
    }

    descartarAlteracoes();
  };

  const excluirPasta = (pasta: IPastaDeploy) => {
    const index = site.value.pastas.findIndex(
      (p) => p.identificador === pasta.identificador
    );
    if (index >= 0) {
      site.value.pastas.splice(index, 1);
    }
  };

  const descartarAlteracoes = () => {
    Object.assign(pastaEmEdicao, new PastaDeployModel());
    exibirModalCadastroPasta.value = false;
  };
</script>

<style scoped>
  .tabela-sem-rodape :deep(.v-data-table-footer) {
    display: none;
  }
</style>
