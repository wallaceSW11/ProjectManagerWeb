interface NavegadorComUserAgentData extends Navigator {
  userAgentData?: {
    mobile: boolean;
  };
}

const IDENTIFICADOR_MOVEL = /Android|iPhone|iPad|iPod|Mobi/i;

export function ehDispositivoMovel(): boolean {
  const navegador = navigator as NavegadorComUserAgentData;

  return (
    navegador.userAgentData?.mobile ??
    IDENTIFICADOR_MOVEL.test(navegador.userAgent)
  );
}
