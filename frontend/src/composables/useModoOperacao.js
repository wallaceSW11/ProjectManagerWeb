import { computed, ref } from 'vue';
import { MODO_OPERACAO } from '@/constants/geral-constants';

/**
 * Composable para gerenciar modos de operação (Inicial, Cadastro, Edição)
 * @param {*} modoInicial - Modo inicial (opcional, padrão: MODO_OPERACAO.INICIAL.valor)
 * @returns {Object} Objeto com reactive references e computed properties para modos de operação
 */
export function useModoOperacao(modoInicial = MODO_OPERACAO.INICIAL.valor) {
  // Estado reativo do modo de operação
  const modoOperacao = ref(modoInicial);

  // Computed properties para verificar o modo atual
  const emModoInicial = computed(
    () => modoOperacao.value === MODO_OPERACAO.INICIAL.valor
  );

  const emModoCadastro = computed(
    () => modoOperacao.value === MODO_OPERACAO.NOVO.valor
  );

  const emModoEdicao = computed(
    () => modoOperacao.value === MODO_OPERACAO.EDICAO.valor
  );

  const emModoCadastroEdicao = computed(
    () => emModoCadastro.value || emModoEdicao.value
  );

  // Métodos auxiliares para mudança de modo
  const definirModoInicial = () => {
    modoOperacao.value = MODO_OPERACAO.INICIAL.valor;
  };

  const definirModoCadastro = () => {
    modoOperacao.value = MODO_OPERACAO.NOVO.valor;
  };

  const definirModoEdicao = () => {
    modoOperacao.value = MODO_OPERACAO.EDICAO.valor;
  };

  return {
    // Estado
    modoOperacao,
    
    // Computed properties
    emModoInicial,
    emModoCadastro,
    emModoEdicao,
    emModoCadastroEdicao,
    
    // Métodos auxiliares
    definirModoInicial,
    definirModoCadastro,
    definirModoEdicao
  };
}