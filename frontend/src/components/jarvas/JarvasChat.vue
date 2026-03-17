<template>
  <!-- Botão flutuante -->
  <v-btn
    class="jarvas-fab"
    icon
    size="56"
    color="primary"
    elevation="6"
    @click="abrirChat"
  >
    <v-badge
      v-if="!online"
      color="error"
      dot
      location="top end"
    >
      <v-icon size="28">mdi-robot</v-icon>
    </v-badge>
    <v-icon
      v-else
      size="28"
    >mdi-robot</v-icon>
    <v-tooltip
      activator="parent"
      location="top"
      text="JARVAS"
    />
  </v-btn>

  <!-- Modal de chat -->
  <v-dialog
    v-model="aberto"
    max-width="520"
  >
    <v-card
      class="d-flex flex-column"
      style="height: 680px"
    >
      <!-- Header -->
      <v-card-title class="d-flex align-center justify-space-between pa-3">
        <div class="d-flex align-center gap-2">
          <v-icon
            color="primary"
            class="mr-2"
          >mdi-robot</v-icon>
          JARVAS
          <v-chip
            :color="online ? 'success' : 'error'"
            size="x-small"
            class="ml-2"
          >
            {{ online ? 'online' : 'offline' }}
          </v-chip>
        </div>
        <div>
          <v-btn
            icon
            size="small"
            variant="text"
            @click="limparHistorico"
          >
            <v-icon size="18">mdi-delete-outline</v-icon>
            <v-tooltip
              activator="parent"
              location="bottom"
              text="Limpar conversa"
            />
          </v-btn>
          <v-btn
            icon
            size="small"
            variant="text"
            @click="aberto = false"
          >
            <v-icon size="18">mdi-close</v-icon>
          </v-btn>
        </div>
      </v-card-title>

      <v-divider />

      <!-- Mensagens -->
      <div
        ref="mensagensRef"
        class="flex-grow-1 pa-3"
        style="overflow-y: auto"
      >
        <!-- Estado vazio -->
        <div
          v-if="historico.length === 0"
          class="d-flex flex-column align-center justify-center h-100 text-center"
          style="opacity: 0.5"
        >
          <v-icon
            size="48"
            color="primary"
            class="mb-3"
          >mdi-robot-outline</v-icon>
          <p class="text-body-2">Fala aí! O que você quer fazer?</p>
          <p class="text-caption mt-1">ex: "inicia o PMW"</p>
        </div>

        <!-- Bolhas de mensagem -->
        <div
          v-for="(msg, i) in historico"
          :key="i"
          class="mb-3"
          :class="msg.role === 'user' ? 'd-flex justify-end' : 'd-flex justify-start'"
        >
          <div
            class="mensagem-bolha pa-3 rounded-lg"
            :class="msg.role === 'user' ? 'bolha-user' : 'bolha-jarvas'"
          >
            <p
              class="text-body-2 mb-0"
              style="white-space: pre-wrap"
            >{{ msg.conteudo }}</p>

            <!-- Chips de ações executadas -->
            <div v-if="msg.acoes?.length" class="mt-2 d-flex flex-wrap gap-1">
              <v-chip
                v-for="acao in msg.acoes"
                :key="acao"
                :color="msg.sucesso ? 'success' : 'error'"
                size="x-small"
                variant="tonal"
              >
                <v-icon start size="12">{{ msg.sucesso ? 'mdi-check' : 'mdi-alert' }}</v-icon>
                {{ acao }}
              </v-chip>
            </div>
          </div>
        </div>

        <!-- Digitando... -->
        <div
          v-if="digitando"
          class="d-flex justify-start mb-3"
        >
          <div class="mensagem-bolha bolha-jarvas pa-3 rounded-lg">
            <div class="digitando">
              <span /><span /><span />
            </div>
          </div>
        </div>
      </div>

      <v-divider />

      <!-- Input -->
      <div class="pa-3">
        <v-textarea
          ref="inputRef"
          v-model="textoAtual"
          placeholder="Fala aí... ex: inicia o Project Manager Web"
          variant="outlined"
          density="compact"
          hide-details
          rows="3"
          no-resize
          :disabled="digitando || !online"
          @keydown="onKeydown"
          append-inner-icon="mdi-send"
          @click:append-inner="enviar"
        />
        <p
          v-if="!online"
          class="text-caption text-error mt-1"
        >
          JARVAS offline — verifique se o Ollama está rodando
        </p>
      </div>
    </v-card>
  </v-dialog>
</template>

<script setup lang="ts">
  import { nextTick, onMounted, ref } from 'vue';
  import JarvasService, { type MensagemChat } from '@/services/JarvasService';
  import { atualizarListaPastas, notificar } from '@/utils/eventBus';

  interface MensagemUI extends MensagemChat {
    acoes?: string[] | null;
    sucesso?: boolean;
  }

  const aberto = ref(false);
  const online = ref(true);
  const digitando = ref(false);
  const textoAtual = ref('');
  const historico = ref<MensagemUI[]>([]);
  const mensagensRef = ref<HTMLElement | null>(null);
  const inputRef = ref<{ focus: () => void } | null>(null);

  onMounted(async () => {
    online.value = await JarvasService.verificarStatus();
  });

  const abrirChat = async () => {
    aberto.value = true;
    online.value = await JarvasService.verificarStatus();
    await scrollParaBaixo();
    await nextTick();
    inputRef.value?.focus();
  };

  const onKeydown = (e: KeyboardEvent) => {
    // Enter envia, Ctrl+Enter pula linha
    if (e.key === 'Enter' && !e.ctrlKey) {
      e.preventDefault();
      enviar();
    }
  };

  const enviar = async () => {    const texto = textoAtual.value.trim();
    if (!texto || digitando.value) return;

    textoAtual.value = '';
    historico.value.push({ role: 'user', conteudo: texto });
    digitando.value = true;
    await scrollParaBaixo();

    try {
      // Envia só role + conteudo para o backend (sem campos de UI)
      const historicoParaEnvio: MensagemChat[] = historico.value
        .slice(0, -1) // exclui a mensagem que acabou de adicionar (já vai no campo mensagem)
        .map(m => ({ role: m.role, conteudo: m.conteudo }));

      const resposta = await JarvasService.chat(texto, historicoParaEnvio);

      historico.value.push({
        role: 'assistant',
        conteudo: resposta.resposta,
        acoes: resposta.acoesExecutadas ?? null,
        sucesso: resposta.sucesso,
      });

      // Se executou clone, atualiza lista de pastas
      if (resposta.sucesso && resposta.acoesExecutadas?.includes('clonar_repositorio')) {
        setTimeout(() => atualizarListaPastas(), 2000);
      }
    } catch {
      notificar('erro', 'Falha ao falar com o JARVAS');
      historico.value.push({
        role: 'assistant',
        conteudo: 'Não consegui me conectar. Verifique se o backend está rodando.',
        sucesso: false,
      });
    } finally {
      digitando.value = false;
      await scrollParaBaixo();
    }
  };

  const limparHistorico = () => {
    historico.value = [];
  };

  const scrollParaBaixo = async () => {
    await nextTick();
    if (mensagensRef.value)
      mensagensRef.value.scrollTop = mensagensRef.value.scrollHeight;
  };
</script>

<style scoped>
.jarvas-fab {
  position: fixed;
  bottom: 24px;
  right: 24px;
  z-index: 1000;
}

.mensagem-bolha {
  max-width: 80%;
  word-break: break-word;
}

.bolha-user {
  background-color: rgba(255, 85, 51, 0.15);
  border: 1px solid rgba(255, 85, 51, 0.3);
}

.bolha-jarvas {
  background-color: rgba(255, 255, 255, 0.05);
  border: 1px solid rgba(255, 255, 255, 0.1);
}

/* Animação de digitando */
.digitando {
  display: flex;
  gap: 4px;
  align-items: center;
  height: 20px;
}

.digitando span {
  width: 7px;
  height: 7px;
  border-radius: 50%;
  background-color: rgba(255, 85, 51, 0.7);
  animation: bounce 1.2s infinite ease-in-out;
}

.digitando span:nth-child(2) { animation-delay: 0.2s; }
.digitando span:nth-child(3) { animation-delay: 0.4s; }

@keyframes bounce {
  0%, 80%, 100% { transform: scale(0.7); opacity: 0.5; }
  40%            { transform: scale(1);   opacity: 1;   }
}
</style>
