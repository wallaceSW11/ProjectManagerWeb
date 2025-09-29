using System.Diagnostics;
using System.Text.Json;
using ProjectManagerWeb.src.DTOs;

namespace ProjectManagerWeb.src.Services;

public class IISService
{
    private static readonly object _lockCache = new object();
    private static List<SiteIISResponseDTO>? _cacheSites;
    private static DateTime _ultimaConsulta = DateTime.MinValue;
    private static readonly TimeSpan _tempoCache = TimeSpan.FromSeconds(5); // Cache por 5 segundos
    
    /// <summary>
    /// Lista todos os sites do IIS com suas informações básicas
    /// </summary>
    /// <returns>Lista de sites do IIS</returns>
    public async Task<List<SiteIISResponseDTO>> ListarSitesAsync()
    {
        // Verifica cache primeiro
        lock (_lockCache)
        {
            if (_cacheSites != null && DateTime.Now - _ultimaConsulta < _tempoCache)
            {
                return _cacheSites;
            }
        }
        
        try
        {
            var sites = new List<SiteIISResponseDTO>();
            
            // Primeiro tenta método direto (mais rápido)
            try
            {
                sites = await ListarSitesMetodoRapidoAsync();
                if (sites.Count > 0 && !sites[0].Nome.Contains("Erro") && !sites[0].Nome.Contains("Inacessível"))
                {
                    // Atualiza cache
                    lock (_lockCache)
                    {
                        _cacheSites = sites;
                        _ultimaConsulta = DateTime.Now;
                    }
                    return sites;
                }
            }
            catch
            {
                // Se falhar, continua para método com arquivo
            }
            
            // Fallback para método com arquivo temporário (mais lento mas mais confiável)
            sites = await ListarSitesComArquivoAsync();
            
            // Se não conseguiu obter sites, retorna lista com informação
            if (sites.Count == 0)
            {
                sites.Add(new SiteIISResponseDTO(
                    "IIS Inacessível",
                    "N/A",
                    "Clique em 'Sim' quando aparecer o UAC ou execute a aplicação como Administrador"
                ));
            }
            
            // Atualiza cache
            lock (_lockCache)
            {
                _cacheSites = sites;
                _ultimaConsulta = DateTime.Now;
            }
            
            return sites;
        }
        catch (Exception ex)
        {
            throw new Exception($"Erro ao listar sites do IIS: {ex.Message}", ex);
        }
    }
    
    /// <summary>
    /// Método rápido para listar sites usando saída direta
    /// </summary>
    /// <returns>Lista de sites</returns>
    private async Task<List<SiteIISResponseDTO>> ListarSitesMetodoRapidoAsync()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "C:\\Windows\\System32\\inetsrv\\appcmd.exe",
                Arguments = "list site",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                Verb = "runas"
            };

            using var process = new Process { StartInfo = psi };
            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            // Timeout de 3 segundos para não travar
            var timeoutTask = Task.Delay(3000);
            var processTask = process.WaitForExitAsync();
            
            if (await Task.WhenAny(processTask, timeoutTask) == timeoutTask)
            {
                try { process.Kill(); } catch { }
                throw new TimeoutException("Comando demorou mais que 3 segundos");
            }

            if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
            {
                var linhas = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                return ProcessarLinhasSitesAppcmd(linhas);
            }
            
            throw new Exception($"Falha no appcmd: {error}");
        }
        catch
        {
            // Se falhar, retorna lista vazia para tentar próximo método
            return new List<SiteIISResponseDTO>();
        }
    }
    
    /// <summary>
    /// Método com arquivo temporário (fallback)
    /// </summary>
    /// <returns>Lista de sites</returns>
    private async Task<List<SiteIISResponseDTO>> ListarSitesComArquivoAsync()
    {
        var arquivoTemporario = Path.Combine(Path.GetTempPath(), $"sites_{Guid.NewGuid()}.txt");
        
        try
        {
            // Comando para salvar lista de sites em arquivo
            var comando = $"/C C:\\Windows\\System32\\inetsrv\\appcmd.exe list site > \"{arquivoTemporario}\"";
            
            // Executa com runas (como administrador) similar ao Delphi
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = comando,
                UseShellExecute = true,
                Verb = "runas", // Equivalente ao 'runas' do Delphi
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process != null)
            {
                // Timeout reduzido para 5 segundos
                var timeoutTask = Task.Delay(5000);
                var processTask = process.WaitForExitAsync();
                
                if (await Task.WhenAny(processTask, timeoutTask) == timeoutTask)
                {
                    try { process.Kill(); } catch { }
                    throw new TimeoutException("Comando demorou mais que 5 segundos");
                }
                
                // Aguarda menos tempo - apenas 300ms
                await Task.Delay(300);
                
                if (File.Exists(arquivoTemporario))
                {
                    var linhas = await File.ReadAllLinesAsync(arquivoTemporario);
                    return ProcessarLinhasSitesAppcmd(linhas);
                }
            }
            
            return new List<SiteIISResponseDTO>();
        }
        catch (Exception ex)
        {
            // Se falhar com runas, tenta método alternativo sem privilégios
            if (ex.Message.Contains("canceled") || ex.Message.Contains("UAC"))
            {
                return await ListarSitesMetodoAlternativoAsync();
            }
            return new List<SiteIISResponseDTO>();
        }
        finally
        {
            // Limpa arquivo temporário
            if (File.Exists(arquivoTemporario))
            {
                try { File.Delete(arquivoTemporario); } catch { }
            }
        }
    }
    
    /// <summary>
    /// Processa as linhas do appcmd seguindo a lógica do Delphi
    /// </summary>
    /// <param name="linhas">Linhas do arquivo gerado pelo appcmd</param>
    /// <returns>Lista de sites processados</returns>
    private List<SiteIISResponseDTO> ProcessarLinhasSitesAppcmd(string[] linhas)
    {
        var sites = new List<SiteIISResponseDTO>();
        
        foreach (var linha in linhas)
        {
            if (string.IsNullOrWhiteSpace(linha) || !linha.StartsWith("SITE"))
                continue;
                
            try
            {
                // Exemplo de linha: SITE "Default Web Site" (id:1,bindings:http/*:80:,state:Started)
                
                // Extrai o nome do site (entre aspas)
                var inicioNome = linha.IndexOf('"');
                var fimNome = linha.IndexOf('"', inicioNome + 1);
                
                if (inicioNome == -1 || fimNome == -1)
                    continue;
                    
                var nomeSite = linha.Substring(inicioNome + 1, fimNome - inicioNome - 1);
                
                // Extrai a porta (similar ao Delphi: http/*:80:)
                var porta = "80";
                var indexHttpPort = linha.IndexOf("http/*:");
                if (indexHttpPort != -1)
                {
                    var startPort = indexHttpPort + 7; // após "http/*:"
                    var endPort = linha.IndexOf(':', startPort);
                    if (endPort != -1)
                    {
                        porta = linha.Substring(startPort, endPort - startPort);
                    }
                }
                
                // Extrai o status (similar ao Delphi: state:Started)
                var status = "Desconhecido";
                var indexState = linha.IndexOf("state:");
                if (indexState != -1)
                {
                    var startState = indexState + 6; // após "state:"
                    var endState = linha.IndexOf(')', startState);
                    if (endState == -1) endState = linha.Length;
                    
                    var estadoRaw = linha.Substring(startState, endState - startState);
                    status = ConverterStatusIIS(estadoRaw);
                }
                
                sites.Add(new SiteIISResponseDTO(
                    nomeSite,
                    porta,
                    status
                ));
            }
            catch (Exception ex)
            {
                // Se falhar ao processar uma linha, adiciona como erro mas continua
                sites.Add(new SiteIISResponseDTO(
                    "Erro ao processar linha",
                    "N/A",
                    $"Erro: {ex.Message.Substring(0, Math.Min(ex.Message.Length, 50))}"
                ));
            }
        }
        
        return sites;
    }
    
    /// <summary>
    /// Método alternativo para listar sites quando não há privilégios administrativos
    /// </summary>
    /// <returns>Lista de sites usando método alternativo</returns>
    private async Task<List<SiteIISResponseDTO>> ListarSitesMetodoAlternativoAsync()
    {
        try
        {
            var sites = new List<SiteIISResponseDTO>();
            
            // Primeiro tenta com appcmd list apenas
            try
            {
                var command = "C:\\Windows\\System32\\inetsrv\\appcmd.exe list sites";
                var result = await ExecutarComandoPowerShellAsync(command);
                
                if (!string.IsNullOrEmpty(result))
                {
                    var linhas = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    
                    foreach (var linha in linhas)
                    {
                        if (linha.StartsWith("SITE "))
                        {
                            // Formato: SITE "Nome do Site" (id:1,bindings:http/*:80:,state:Started)
                            var partes = linha.Split('"');
                            if (partes.Length >= 3)
                            {
                                var nome = partes[1];
                                var info = partes[2];
                                
                                // Extrai porta do binding
                                var porta = "80";
                                var portaMatch = System.Text.RegularExpressions.Regex.Match(info, @":(\d+):");
                                if (portaMatch.Success)
                                {
                                    porta = portaMatch.Groups[1].Value;
                                }
                                
                                // Extrai status
                                var status = "Desconhecido";
                                var statusMatch = System.Text.RegularExpressions.Regex.Match(info, @"state:(\w+)");
                                if (statusMatch.Success)
                                {
                                    status = ConverterStatusIIS(statusMatch.Groups[1].Value);
                                }
                                
                                sites.Add(new SiteIISResponseDTO(nome, porta, status));
                            }
                        }
                    }
                }
            }
            catch
            {
                // Se appcmd falhar, tenta com netstat para detectar portas ativas
                sites = await ListarSitesComNetstatAsync();
            }
            
            // Se ainda não conseguiu obter sites, retorna lista com informação de como resolver
            if (sites.Count == 0)
            {
                sites.Add(new SiteIISResponseDTO(
                    "IIS Inacessível",
                    "N/A",
                    "Execute a aplicação como Administrador ou instale o IIS Management Console"
                ));
            }
            
            return sites;
        }
        catch (Exception ex)
        {
            // Última tentativa - retorna site de exemplo com erro
            return new List<SiteIISResponseDTO>
            {
                new SiteIISResponseDTO(
                    "Erro de Acesso",
                    "N/A",
                    $"IIS não disponível. Erro: {ex.Message.Substring(0, Math.Min(ex.Message.Length, 100))}"
                )
            };
        }
    }
    
    /// <summary>
    /// Lista sites usando netstat para detectar portas comuns do IIS
    /// </summary>
    /// <returns>Lista de sites detectados por netstat</returns>
    private async Task<List<SiteIISResponseDTO>> ListarSitesComNetstatAsync()
    {
        try
        {
            var sites = new List<SiteIISResponseDTO>();
            
            // Verifica portas comuns do IIS (80, 443, 8080, etc.)
            var portasComuns = new[] { "80", "443", "8080", "8443", "8081", "8082" };
            
            var command = "netstat -an | findstr :80";
            var result = await ExecutarComandoPowerShellAsync(command);
            
            if (!string.IsNullOrEmpty(result))
            {
                var linhas = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                var portasAtivas = new HashSet<string>();
                
                foreach (var linha in linhas)
                {
                    if (linha.Contains("LISTENING"))
                    {
                        var partes = linha.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (partes.Length >= 2)
                        {
                            var endereco = partes[1];
                            if (endereco.Contains(':'))
                            {
                                var porta = endereco.Split(':').Last();
                                if (portasComuns.Contains(porta))
                                {
                                    portasAtivas.Add(porta);
                                }
                            }
                        }
                    }
                }
                
                // Para cada porta ativa, cria um site genérico
                foreach (var porta in portasAtivas.OrderBy(p => int.Parse(p)))
                {
                    var protocolo = porta == "443" || porta == "8443" ? "HTTPS" : "HTTP";
                    sites.Add(new SiteIISResponseDTO(
                        $"Site {protocolo} (Porta {porta})",
                        porta,
                        "Detectado via netstat - Execute como Admin para mais detalhes"
                    ));
                }
            }
            
            return sites;
        }
        catch
        {
            return new List<SiteIISResponseDTO>();
        }
    }
    
    /// <summary>
    /// Inicia um site do IIS
    /// </summary>
    /// <param name="nomeSite">Nome do site</param>
    /// <returns>True se iniciado com sucesso</returns>
    public async Task<bool> IniciarSiteAsync(string nomeSite)
    {
        if (string.IsNullOrWhiteSpace(nomeSite))
            throw new ArgumentException("Nome do site não pode ser vazio", nameof(nomeSite));
            
        try
        {
            // Usa comando similar ao Delphi: start site + start apppool
            var comando = $"/C C: && C:\\Windows\\System32\\inetsrv\\appcmd start site /site.name:\"{nomeSite}\" && C:\\Windows\\System32\\inetsrv\\appcmd start apppool /apppool.name:\"{nomeSite}\"";
            
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = comando,
                UseShellExecute = true,
                Verb = "runas", // Executa como administrador
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process != null)
            {
                // Timeout de 10 segundos para start
                var timeoutTask = Task.Delay(10000);
                var processTask = process.WaitForExitAsync();
                
                if (await Task.WhenAny(processTask, timeoutTask) == timeoutTask)
                {
                    try { process.Kill(); } catch { }
                    throw new TimeoutException("Comando demorou mais que 10 segundos");
                }
                
                // Invalida cache para forçar atualização na próxima consulta
                lock (_lockCache)
                {
                    _cacheSites = null;
                }
                
                // Aguarda apenas 500ms para o IIS processar
                await Task.Delay(500);
                
                // Verifica se funcionou
                return await VerificarStatusSiteAsync(nomeSite, "Started");
            }
            
            return false;
        }
        catch (Exception ex)
        {
            throw new Exception($"Erro ao iniciar site '{nomeSite}': {ex.Message}", ex);
        }
    }
    
    /// <summary>
    /// Para um site do IIS
    /// </summary>
    /// <param name="nomeSite">Nome do site</param>
    /// <returns>True se parado com sucesso</returns>
    public async Task<bool> PararSiteAsync(string nomeSite)
    {
        if (string.IsNullOrWhiteSpace(nomeSite))
            throw new ArgumentException("Nome do site não pode ser vazio", nameof(nomeSite));
            
        try
        {
            // Usa comando similar ao Delphi: stop site + stop apppool
            var comando = $"/C C: && C:\\Windows\\System32\\inetsrv\\appcmd stop site /site.name:\"{nomeSite}\" && C:\\Windows\\System32\\inetsrv\\appcmd stop apppool /apppool.name:\"{nomeSite}\"";
            
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = comando,
                UseShellExecute = true,
                Verb = "runas", // Executa como administrador
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process != null)
            {
                // Timeout de 10 segundos para stop
                var timeoutTask = Task.Delay(10000);
                var processTask = process.WaitForExitAsync();
                
                if (await Task.WhenAny(processTask, timeoutTask) == timeoutTask)
                {
                    try { process.Kill(); } catch { }
                    throw new TimeoutException("Comando demorou mais que 10 segundos");
                }
                
                // Invalida cache para forçar atualização na próxima consulta
                lock (_lockCache)
                {
                    _cacheSites = null;
                }
                
                // Aguarda apenas 500ms para o IIS processar
                await Task.Delay(500);
                
                // Verifica se funcionou
                return await VerificarStatusSiteAsync(nomeSite, "Stopped");
            }
            
            return false;
        }
        catch (Exception ex)
        {
            throw new Exception($"Erro ao parar site '{nomeSite}': {ex.Message}", ex);
        }
    }
    
    /// <summary>
    /// Reinicia um site do IIS (para e inicia novamente)
    /// </summary>
    /// <param name="nomeSite">Nome do site</param>
    /// <returns>True se reiniciado com sucesso</returns>
    public async Task<bool> ReiniciarSiteAsync(string nomeSite)
    {
        if (string.IsNullOrWhiteSpace(nomeSite))
            throw new ArgumentException("Nome do site não pode ser vazio", nameof(nomeSite));
            
        try
        {
            // Para o site primeiro
            await PararSiteAsync(nomeSite);
            
            // Aguarda um pouco para garantir que parou
            await Task.Delay(1000);
            
            // Inicia o site novamente
            return await IniciarSiteAsync(nomeSite);
        }
        catch (Exception ex)
        {
            throw new Exception($"Erro ao reiniciar site '{nomeSite}': {ex.Message}", ex);
        }
    }
    
    /// <summary>
    /// Executa ação específica em um site (iniciar, parar, reiniciar)
    /// </summary>
    /// <param name="request">Request com nome do site e ação desejada</param>
    /// <returns>True se ação executada com sucesso</returns>
    public async Task<bool> ExecutarAcaoSiteAsync(AcaoSiteIISRequestDTO request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));
            
        var acaoLower = request.Acao.ToLower();
        return acaoLower switch
        {
            "iniciar" => await IniciarSiteAsync(request.NomeSite),
            "start" => await IniciarSiteAsync(request.NomeSite),
            "parar" => await PararSiteAsync(request.NomeSite),
            "stop" => await PararSiteAsync(request.NomeSite),
            "reiniciar" => await ReiniciarSiteAsync(request.NomeSite),
            "restart" => await ReiniciarSiteAsync(request.NomeSite),
            _ => throw new ArgumentException($"Ação '{request.Acao}' não é válida. Use: iniciar, parar ou reiniciar")
        };
    }
    
    #region Métodos Privados
    
    /// <summary>
    /// Executa comando PowerShell e retorna o resultado
    /// </summary>
    /// <param name="command">Comando a ser executado</param>
    /// <returns>Resultado do comando</returns>
    private async Task<string> ExecutarComandoPowerShellAsync(string command)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };
            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0 && !string.IsNullOrEmpty(error))
            {
                throw new Exception($"Erro no PowerShell: {error}");
            }

            return output.Trim();
        }
        catch (Exception ex)
        {
            throw new Exception($"Erro ao executar comando PowerShell: {ex.Message}", ex);
        }
    }
    
    /// <summary>
    /// Verifica se o site está no status esperado
    /// </summary>
    /// <param name="nomeSite">Nome do site</param>
    /// <param name="statusEsperado">Status esperado</param>
    /// <returns>True se está no status esperado</returns>
    private async Task<bool> VerificarStatusSiteAsync(string nomeSite, string statusEsperado)
    {
        try
        {
            // Usa estratégia similar - lista todos os sites e verifica o específico
            var sites = await ListarSitesAsync();
            var siteEncontrado = sites.FirstOrDefault(s => 
                s.Nome.Equals(nomeSite, StringComparison.OrdinalIgnoreCase));
                
            if (siteEncontrado != null)
            {
                var statusAtual = siteEncontrado.Status.ToLower();
                var statusEsperadoConvertido = ConverterStatusIIS(statusEsperado).ToLower();
                
                return statusAtual.Contains(statusEsperadoConvertido.ToLower()) || 
                       statusEsperadoConvertido.Contains(statusAtual);
            }
            
            return false;
        }
        catch
        {
            return await VerificarStatusSiteMetodoAlternativoAsync(nomeSite, statusEsperado);
        }
    }
    
    /// <summary>
    /// Verifica status do site usando método alternativo (appcmd)
    /// </summary>
    /// <param name="nomeSite">Nome do site</param>
    /// <param name="statusEsperado">Status esperado</param>
    /// <returns>True se está no status esperado</returns>
    private async Task<bool> VerificarStatusSiteMetodoAlternativoAsync(string nomeSite, string statusEsperado)
    {
        try
        {
            var command = $"C:\\Windows\\System32\\inetsrv\\appcmd.exe list site \"{nomeSite}\" /text:state";
            var result = await ExecutarComandoPowerShellAsync(command);
            
            return result.Trim().Equals(statusEsperado, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Converte status do IIS para formato amigável
    /// </summary>
    /// <param name="status">Status do IIS</param>
    /// <returns>Status formatado</returns>
    private string ConverterStatusIIS(string status)
    {
        return status.ToLower() switch
        {
            "started" => "Iniciado",
            "stopped" => "Parado",
            "starting" => "Iniciando",
            "stopping" => "Parando",
            "unknown" => "Desconhecido",
            _ => status
        };
    }
    
    #endregion
}

/// <summary>
/// Classe auxiliar para deserialização do JSON do PowerShell
/// </summary>
internal class SiteIISInfo
{
    public string? Name { get; set; }
    public string? Porta { get; set; }
    public string? State { get; set; }
}