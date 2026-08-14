export function corPorUso(percentual: number): string {
  return percentual < 60 ? 'success' : percentual < 85 ? 'warning' : 'error';
}
