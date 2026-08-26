const formatadorDecimal = new Intl.NumberFormat('pt-BR', {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2
});

export function formatarDecimal(valor: number): string {
  return formatadorDecimal.format(valor);
}
