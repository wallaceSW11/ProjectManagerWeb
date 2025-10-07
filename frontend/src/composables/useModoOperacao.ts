import { computed, ref, type ComputedRef, type Ref } from 'vue'
import { MODO_OPERACAO } from '@/constants/geral-constants'

interface UseModoOperacaoReturn {
  modoOperacao: Ref<string>
  emModoInicial: ComputedRef<boolean>
  emModoCadastro: ComputedRef<boolean>
  emModoEdicao: ComputedRef<boolean>
  emModoCadastroEdicao: ComputedRef<boolean>
  definirModoInicial: () => void
  definirModoCadastro: () => void
  definirModoEdicao: () => void
}

export function useModoOperacao(modoInicial = MODO_OPERACAO.INICIAL.valor): UseModoOperacaoReturn {
  const modoOperacao = ref(modoInicial)

  const emModoInicial = computed(
    () => modoOperacao.value === MODO_OPERACAO.INICIAL.valor
  )

  const emModoCadastro = computed(
    () => modoOperacao.value === MODO_OPERACAO.NOVO.valor
  )

  const emModoEdicao = computed(
    () => modoOperacao.value === MODO_OPERACAO.EDICAO.valor
  )

  const emModoCadastroEdicao = computed(
    () => emModoCadastro.value || emModoEdicao.value
  )

  const definirModoInicial = (): void => {
    modoOperacao.value = MODO_OPERACAO.INICIAL.valor
  }

  const definirModoCadastro = (): void => {
    modoOperacao.value = MODO_OPERACAO.NOVO.valor
  }

  const definirModoEdicao = (): void => {
    modoOperacao.value = MODO_OPERACAO.EDICAO.valor
  }

  return {
    modoOperacao,
    emModoInicial,
    emModoCadastro,
    emModoEdicao,
    emModoCadastroEdicao,
    definirModoInicial,
    definirModoCadastro,
    definirModoEdicao
  }
}