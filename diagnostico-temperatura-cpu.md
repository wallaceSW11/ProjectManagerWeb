# Diagnóstico — Temperatura da CPU no Windows (PMW)

Data: 22/08/2026
Máquina: Positivo Bahia — VAIO VJFE69F11X-B0221H · Windows 11 Pro build 26200 · AMD Ryzen 7 5825U (Cezanne, Zen 3, 8C/16T)

## Síntese

A temperatura da CPU não aparece no painel de monitoramento do PMW no Windows 11.
A hipótese original (WinRing0 bloqueado pela Microsoft Vulnerable Driver Blocklist) estava **incorreta** para a versão da lib em uso.

**Causa raiz comprovada:** a `LibreHardwareMonitorLib` 0.9.6 depende do driver de kernel **PawnIO**, que precisa estar **instalado no sistema** (device `\\.\PawnIO`). O PMW usa a lib sem instalar o driver — o GUI oficial do LHM instala antes, o PMW não. Sem o device, todos os sensores que dependem de MSR/SMU retornam `0`/`null`.

No Linux funciona porque o coletor usa `/sys/class/thermal` (sem driver de kernel).

## Evidências coletadas (empírico, na máquina real)

| Verificação | Resultado |
|---|---|
| `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\PawnIO` | Não existe |
| Device `\\?\GLOBALROOT\Device\PawnIO` | `False` (não existe) |
| Driver/serviço `PawnIO` no SCM (`Win32_SystemDriver`) | Não existe |
| `sc query winring0_1_2_0` / `R0LibreHardwareMonitor` | 1060 — serviço não existe |
| DiagTemp **sem admin**: `Computer.Open()` | OK, sensor `Tctl/Tdie = 0` |
| DiagTemp **com admin** (UAC): `Computer.Open()` | OK, sensor `Tctl/Tdie = 0` |
| PMW rodando **elevado** (TokenElevation=1, porta 2024) — WebSocket | `cpuTemperaturaCelsius: null` |
| Fallback WMI `MSAcpi_ThermalZoneTemperature` (`root\WMI`) | Classe não existe ("Sem suporte") |
| Fallback WMI `Win32_PerfFormattedData_Counters_ThermalZoneInformation` | Vazio — não existe |
| `VulnerableDriverBlocklistEnable` (HKLM CI\Config) | `1` (ativa) |
| Device Guard | `VirtualizationBasedSecurityStatus=2`, `SecurityServicesRunning={2}` (HVCI ativo) |
| Eventos CodeIntegrity / Kernel-PnP de bloqueio de driver | Nenhum (o driver nunca foi tentado carregar) |

## Como o driver funciona (fonte oficial v0.9.6)

`LibreHardwareMonitor/PawnIo/PawnIo.cs`:

```csharp
SafeFileHandle handle = PInvoke.CreateFile(@"\\?\GLOBALROOT\Device\PawnIO", ...);
if (handle.IsInvalid)
    return new PawnIo(null);   // driver não instalado → instância nula
```

`Execute()`:

```csharp
if (IsLoaded) { ... }
return new long[outLength];    // sem driver → retorna ZEROS
```

`LibreHardwareMonitor/UI/MainForm.cs` (GUI oficial — o que o PMW não faz):

```csharp
MessageBox.Show("PawnIO is not installed, do you want to install it?", ...);
InstallPawnIO();   // extrai PawnIO_setup.exe embutido e roda "-install"
```

## Detalhes da lib em uso

- DLL: `backend/libs/LibreHardwareMonitorLib.dll` — versão `0.9.6+3d331e3370efb858411f19511373eff65a218701`
- Referenciada por `HintPath` (não PackageReference) — ver `backend/ProjectManagerWeb.csproj`
- Recurso embutido `LibreHardwareMonitor.Resources.PawnIO_setup.exe` está **apenas no exe do GUI oficial** (LibreHardwareMonitor.exe), não na lib
- Recursos `.bin` do PawnIO presentes na lib: `AMDFamily17.bin`, `RyzenSMU.bin` (relevantes para o Ryzen 5825U), `AMDFamily0F/10`, `IntelMSR`, `IsaBridgeEC`, `LpcACPIEC`, `LpcCrOSEC`, `LpcIO`, `SmbusI801`, `SmbusNCT6793`, `SmbusPIIX4`
- A lib **não embute** o instalador do driver — só os módulos de firmware que são carregados no driver já instalado

## Hipóteses descartadas

1. **WinRing0 bloqueado pela blocklist** — a lib 0.9.6 não usa WinRing0; não há referência a ele na DLL. A blocklist está ativa (`=1`) mas é irrelevante aqui.
2. **Falta de elevação** — mesmo com `dotnet run` como admin (TokenElevation=1) o sensor retorna `0`. A elevação é necessária para abrir o device, mas o device nem existe.
3. **HVCI/Memory Integrity bloqueando** — nenhum evento de bloqueio em CodeIntegrity/Kernel-PnP; o driver nunca chegou a ser carregado.
4. **Fallback WMI** — nenhuma das classes ACPI existe nesta máquina VAIO/Ryzen; não há atalho WMI.

## Código atual do coletor (já corrigido)

`backend/src/Services/Monitoramento/Coletores/WindowsCpuRamColetor.cs`

- Linha 145: `if (!double.IsFinite(celsius) || celsius is < -20 or > 150) continue;` — filtra NaN/infinito
- Linhas 97–102: `_temperaturaWmiDefinitiva` memoiza o "não existe" do ACPI (evita consulta por snapshot)
- Linha 118–158: `LerTemperaturaCpu()` — a fonte primária; o `catch` vazio (linha 154) engole qualquer exceção do LHM (comportamento intencional de fallback, mas dificultou o diagnóstico)

## Opções de solução

### A — Instalar o driver manualmente (validação imediata)

Executar como admin o instalador oficial extraído do release v0.9.6:

```
PawnIO_setup.exe -install
```

- Instalador: `PawnIO_setup.exe` (v2.1.0.0, assinatura `namazso.eu` — **Valid**)
- Extraído de: `LibreHardwareMonitor.exe` v0.9.6 (release oficial GitHub)
- Após instalar: PMW deve rodar **como administrador** (o GUI oficial também exige) para abrir `\\.\PawnIO`
- Validação: DiagTemp devolve temperatura real (~40–60°C) em vez de `0`

### B — Automatizar no código do PMW

O `WindowsCpuRamColetor` (ou serviço de monitoramento) verifica `PawnIo.IsInstalled` na primeira coleta; se ausente, extrai o `PawnIO_setup.exe` (embutido como recurso) e executa `-install`, tratando falha e exigindo elevação.

- Prós: transparente para o usuário final (mesmo comportamento do GUI oficial)
- Contras: instalar driver de kernel em runtime, requer elevação, mais código
- Exige embutir o `PawnIO_setup.exe` no projeto (licença MPL-2.0 do LHM + driver separado do namazso)

### C — Sem driver, aceitar `--` no Windows

Documentar que a temperatura da CPU no Windows exige o driver PawnIO instalado (ou HWiNFO/CoreTemp rodando em paralelo — só Shared Memory, dependência externa).

## Artefatos de diagnóstico (diretório temp, fora do repositório)

- `C:\Users\<user>\AppData\Local\Temp\opencode\diag-temp\` — utilitário .NET que reproduz o `Computer.Open()` e expõe a exceção/sensores reais
- Logs: `%TEMP%\pmw-diag-temperatura.txt`, `%TEMP%\pmw-diag-multi.txt`
- `%TEMP%\opencode\lhm.zip` / `lhm-release\` — release oficial v0.9.6 (GUI)
- `%TEMP%\opencode\lhm-src\` — fonte oficial v0.9.6
- `%TEMP%\opencode\PawnIO_setup.exe` — instalador do driver extraído

## Passos de validação pendentes

1. [ ] (Decidir com o usuário) Instalar o PawnIO driver na máquina e revalidar o DiagTemp + PMW admin
2. [ ] Se confirmado: decidir entre Opção A (manual/documentação), B (automatizar) ou C (aceitar `--`)
