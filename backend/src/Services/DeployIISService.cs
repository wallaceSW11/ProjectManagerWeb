using System.Text;
using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Utils;

namespace ProjectManagerWeb.src.Services
{
    /// <summary>
    /// Service responsável por orquestrar o processo de deploy de sites IIS
    /// </summary>
    public class DeployIISService(SiteIISJsonService siteIISService)
    {
        private static readonly string LogPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "PMW", "Banco", "deploy-logs");

        public DeployIISService() : this(null!)
        {
            if (!Directory.Exists(LogPath))
            {
                Directory.CreateDirectory(LogPath);
            }
        }

        /// <summary>
        /// Executa o processo completo de atualização de um site IIS
        /// </summary>
        public async Task<AtualizarSiteResponseDTO> AtualizarSiteAsync(Guid siteId)
        {
            var log = new List<string>();
            
            try
            {
                log.Add($"=== INICIANDO DEPLOY - {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
                
                // 1. Buscar configuração do site
                var site = await siteIISService.GetByIdAsync(siteId);
                if (site == null)
                {
                    log.Add("ERRO: Site não encontrado!");
                    return new AtualizarSiteResponseDTO(false, "Site não encontrado", log);
                }

                log.Add($"Site: {site.Nome}");
                log.Add($"Pasta Raiz: {site.PastaRaiz}");
                log.Add("");

                // 2. Gerar script PowerShell
                log.Add("=== GERANDO SCRIPT POWERSHELL ===");
                var scriptPath = GerarScriptPowerShell(site, log);
                log.Add($"Script gerado em: {scriptPath}");
                log.Add("");

                // 3. Executar script e capturar output
                log.Add("=== EXECUTANDO DEPLOY ===");
                var sucesso = await ExecutarScriptPowerShell(scriptPath, log);

                if (sucesso)
                {
                    log.Add("");
                    log.Add("=== DEPLOY INICIADO! ===");
                    log.Add("Acompanhe o progresso na janela do PowerShell.");
                    return new AtualizarSiteResponseDTO(true, "Deploy iniciado! Acompanhe na janela do PowerShell.", log);
                }
                else
                {
                    log.Add("");
                    log.Add("=== DEPLOY FALHOU AO INICIAR! ===");
                    return new AtualizarSiteResponseDTO(false, "Falha ao iniciar deploy. Verifique o log.", log);
                }
            }
            catch (Exception ex)
            {
                log.Add("");
                log.Add($"ERRO CRÍTICO: {ex.Message}");
                log.Add(ex.StackTrace ?? "");
                return new AtualizarSiteResponseDTO(false, $"Erro crítico: {ex.Message}", log);
            }
        }

        /// <summary>
        /// Gera o script PowerShell dinâmico baseado na configuração do site
        /// </summary>
        private string GerarScriptPowerShell(SiteIISRequestDTO site, List<string> log)
        {
            var script = new StringBuilder();
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            script.AppendLine("# Script de Deploy Automático - Gerado pelo PMW");
            script.AppendLine($"# Site: {site.Nome}");
            script.AppendLine($"# Data: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            script.AppendLine("");
            script.AppendLine("$ErrorActionPreference = 'Stop'");
            script.AppendLine($"$timestamp = '{timestamp}'");
            script.AppendLine("");

            // FASE 1: BUILD E VALIDAÇÃO
            script.AppendLine("# ========================================");
            script.AppendLine("# FASE 1: BUILD E VALIDAÇÃO");
            script.AppendLine("# ========================================");
            script.AppendLine("Write-Host '=== FASE 1: EXECUTANDO BUILDS ===' -ForegroundColor Cyan");
            script.AppendLine("");

            for (int i = 0; i < site.Pastas.Count; i++)
            {
                var pasta = site.Pastas[i];
                script.AppendLine($"# Pasta {i + 1}: {pasta.NomePastaDestino}");
                script.AppendLine($"Write-Host 'Build {i + 1}/{site.Pastas.Count}: {pasta.NomePastaDestino}' -ForegroundColor Yellow");
                script.AppendLine($"Set-Location '{pasta.DiretorioTrabalho}'");
                script.AppendLine($"Write-Host 'Executando: {pasta.ComandoPublish}' -ForegroundColor Gray");
                script.AppendLine($"Invoke-Expression '{pasta.ComandoPublish}'");
                script.AppendLine("if ($LASTEXITCODE -ne 0) {");
                script.AppendLine($"    Write-Host 'ERRO: Build da pasta {pasta.NomePastaDestino} falhou!' -ForegroundColor Red");
                script.AppendLine("    exit 1");
                script.AppendLine("}");
                script.AppendLine($"Write-Host 'Build {i + 1} concluído com sucesso!' -ForegroundColor Green");
                script.AppendLine("");
            }

            script.AppendLine("Write-Host 'Todos os builds foram concluídos com sucesso!' -ForegroundColor Green");
            script.AppendLine("");

            // FASE 2: PARAR SERVIÇOS
            script.AppendLine("# ========================================");
            script.AppendLine("# FASE 2: PARAR SITE E POOLS");
            script.AppendLine("# ========================================");
            script.AppendLine("Write-Host '=== FASE 2: PARANDO SERVIÇOS ===' -ForegroundColor Cyan");
            script.AppendLine("");

            script.AppendLine($"Write-Host 'Parando site: {site.Nome}' -ForegroundColor Yellow");
            script.AppendLine($"& \"$env:SystemRoot\\system32\\inetsrv\\appcmd\" stop site \"{site.Nome}\"");
            script.AppendLine("");

            foreach (var pool in site.PoolsAplicacao)
            {
                script.AppendLine($"Write-Host 'Parando pool: {pool}' -ForegroundColor Yellow");
                script.AppendLine($"& \"$env:SystemRoot\\system32\\inetsrv\\appcmd\" stop apppool \"{pool}\"");
            }
            script.AppendLine("");

            // FASE 3: BACKUP E CÓPIA
            script.AppendLine("# ========================================");
            script.AppendLine("# FASE 3: BACKUP E CÓPIA");
            script.AppendLine("# ========================================");
            script.AppendLine("Write-Host '=== FASE 3: BACKUP E CÓPIA DE ARQUIVOS ===' -ForegroundColor Cyan");
            script.AppendLine("");

            foreach (var pasta in site.Pastas)
            {
                script.AppendLine($"Write-Host 'Processando: {pasta.NomePastaDestino}' -ForegroundColor Yellow");
                
                // Monta o caminho real da pasta (PastaRaiz + NomePastaDestino)
                script.AppendLine($"$caminhoRealPasta = Join-Path '{site.PastaRaiz}' '{pasta.NomePastaDestino}'");
                
                // Backup da pasta atual (renomeia adicionando timestamp)
                script.AppendLine("if (Test-Path $caminhoRealPasta) {");
                script.AppendLine($"    Write-Host 'Fazendo backup de {pasta.NomePastaDestino}...' -ForegroundColor Gray");
                script.AppendLine("    $pastaBackup = $caminhoRealPasta + '_' + $timestamp");
                script.AppendLine("    Rename-Item $caminhoRealPasta $pastaBackup");
                script.AppendLine($"    Write-Host 'Backup salvo como: {pasta.NomePastaDestino}_$timestamp' -ForegroundColor Green");
                script.AppendLine("}");
                
                // Copiar nova versão
                script.AppendLine($"Write-Host 'Copiando arquivos de {pasta.CaminhoOrigem}...' -ForegroundColor Gray");
                script.AppendLine($"Copy-Item '{pasta.CaminhoOrigem}' $caminhoRealPasta -Recurse -Force");
                script.AppendLine($"Write-Host 'Cópia concluída!' -ForegroundColor Green");
                script.AppendLine("");
            }

            // FASE 4: INICIAR SERVIÇOS
            script.AppendLine("# ========================================");
            script.AppendLine("# FASE 4: INICIAR SITE E POOLS");
            script.AppendLine("# ========================================");
            script.AppendLine("Write-Host '=== FASE 4: INICIANDO SERVIÇOS ===' -ForegroundColor Cyan");
            script.AppendLine("");

            script.AppendLine($"Write-Host 'Iniciando site: {site.Nome}' -ForegroundColor Yellow");
            script.AppendLine($"& \"$env:SystemRoot\\system32\\inetsrv\\appcmd\" start site \"{site.Nome}\"");
            script.AppendLine("");

            foreach (var pool in site.PoolsAplicacao)
            {
                script.AppendLine($"Write-Host 'Iniciando pool: {pool}' -ForegroundColor Yellow");
                script.AppendLine($"& \"$env:SystemRoot\\system32\\inetsrv\\appcmd\" start apppool \"{pool}\"");
                script.AppendLine("");
                script.AppendLine($"Write-Host 'Reciclando pool: {pool}' -ForegroundColor Yellow");
                script.AppendLine($"& \"$env:SystemRoot\\system32\\inetsrv\\appcmd\" recycle apppool \"{pool}\"");
                script.AppendLine("");
            }

            // CONCLUSÃO
            script.AppendLine("# ========================================");
            script.AppendLine("# DEPLOY CONCLUÍDO");
            script.AppendLine("# ========================================");
            script.AppendLine("Write-Host '=== DEPLOY CONCLUÍDO COM SUCESSO! ===' -ForegroundColor Green");
            script.AppendLine("Write-Host \"Backup criado com timestamp: $timestamp\" -ForegroundColor Cyan");
            script.AppendLine("");
            script.AppendLine("# Pausar e fechar");
            script.AppendLine("Write-Host 'Pressione qualquer tecla para fechar...' -ForegroundColor Yellow");
            script.AppendLine("$null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown')");
            script.AppendLine("Stop-Process -Id $PID");

            // Garantir que o diretório de logs existe
            if (!Directory.Exists(LogPath))
            {
                Directory.CreateDirectory(LogPath);
            }

            // Salvar script em arquivo
            var scriptFileName = $"deploy_{site.Nome}_{timestamp}.ps1";
            var scriptPath = Path.Combine(LogPath, scriptFileName);
            File.WriteAllText(scriptPath, script.ToString());

            log.Add($"Script salvo: {scriptFileName}");

            return scriptPath;
        }

        /// <summary>
        /// Executa o script PowerShell usando o ShellExecute
        /// </summary>
        private async Task<bool> ExecutarScriptPowerShell(string scriptPath, List<string> log)
        {
            try
            {
                // Comando PowerShell para executar o script
                var comando = $"& '{scriptPath}'";

                log.Add("Abrindo PowerShell como ADMINISTRADOR para execução...");
                log.Add($"Comando: {comando}");
                log.Add("IMPORTANTE: Aceite o UAC e acompanhe o progresso na janela do PowerShell que será aberta.");
                
                // Executar como Administrador para ter acesso ao IIS
                ShellExecute.ExecutarComandoComoAdministrador(comando);
                
                log.Add("PowerShell iniciado com privilégios administrativos!");
                log.Add("O processo está rodando em segundo plano.");
                
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                log.Add($"ERRO ao executar script: {ex.Message}");
                return false;
            }
        }
    }
}
