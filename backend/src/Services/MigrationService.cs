using System.Text.Json;
using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Utils;

namespace ProjectManagerWeb.src.Services
{
    public class MigrationService
    {
        private static readonly string BasePath = PathHelper.BancoPath;

        private static readonly string FilePath =
            Path.Combine(BasePath, "migrations.json");

        private static readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

        private readonly IDEJsonService _ideService;
        private readonly RepositorioJsonService _repositorioService;
        private readonly ILogger<MigrationService> _logger;

        public MigrationService(
            IDEJsonService ideService,
            RepositorioJsonService repositorioService,
            ILogger<MigrationService> logger)
        {
            _ideService = ideService;
            _repositorioService = repositorioService;
            _logger = logger;

            if (!Directory.Exists(BasePath))
            {
                Directory.CreateDirectory(BasePath);
            }
        }

        /// <summary>
        /// Verifica se uma migration já foi executada.
        /// </summary>
        public async Task<bool> IsMigrationExecutedAsync(string migrationName)
        {
            var migrations = await LerMigrationsDoArquivoAsync();
            return migrations.ExecutedMigrations.Any(m => m.Name == migrationName);
        }

        /// <summary>
        /// Registra uma migration como executada.
        /// </summary>
        public async Task RecordMigrationAsync(string migrationName)
        {
            await _semaphore.WaitAsync();
            try
            {
                var migrations = await LerMigrationsDoArquivoAsync(locked: true);
                
                // Evitar duplicatas
                if (!migrations.ExecutedMigrations.Any(m => m.Name == migrationName))
                {
                    var novoRegistro = new MigrationRecordDTO(migrationName, DateTime.UtcNow);
                    migrations.ExecutedMigrations.Add(novoRegistro);
                    await GravarMigrationsNoArquivoAsync(migrations, locked: true);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Executa todas as migrations pendentes.
        /// </summary>
        public async Task ExecuteMigrationsAsync()
        {
            _logger.LogInformation("Verificando migrations pendentes...");

            try
            {
                await ExecutarMigration("001_AddIDEs", Migration_001_AddIDEs);
                await ExecutarMigration("002_MigrateProgramDataToUserProfile", Migration_002_MigrateProgramDataToUserProfile);
                await ExecutarMigration("003_AddComandoClone", Migration_003_AddComandoClone);

                _logger.LogInformation("Todas as migrations foram verificadas");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao executar migrations. A aplicação continuará a inicialização.");
            }
        }

        private async Task ExecutarMigration(string nome, Func<Task> migration)
        {
            if (await IsMigrationExecutedAsync(nome))
            {
                _logger.LogInformation($"Migration {nome} já foi executada anteriormente");
                return;
            }

            _logger.LogInformation($"Executando migration: {nome}");
            await migration();
            await RecordMigrationAsync(nome);
            _logger.LogInformation($"Migration {nome} concluída");
        }

        /// <summary>
        /// Migration 001: Adiciona IDEs padrão e converte campo AbrirNoVSCode para IDEIdentificador.
        /// </summary>
        public async Task Migration_001_AddIDEs()
        {
            const string migrationName = "001_AddIDEs";

            _logger.LogInformation($"Iniciando migration: {migrationName}");

            try
            {
                // 1. Criar IDEs padrão
                var ides = await _ideService.GetAllAsync();
                Guid vsCodeId = Guid.Empty;

                // VS Code
                if (!ides.Any(i => i.Nome.Equals("VS Code", StringComparison.OrdinalIgnoreCase)))
                {
                    vsCodeId = Guid.NewGuid();
                    var vsCode = new IDEDTO(vsCodeId, "VS Code", "code .", AceitaPerfilPersonalizado: true);
                    await _ideService.AddAsync(vsCode);
                    _logger.LogInformation("IDE VS Code criada");
                }
                else
                {
                    vsCodeId = ides.First(i => i.Nome.Equals("VS Code", StringComparison.OrdinalIgnoreCase)).Identificador;
                }

                // Kiro
                if (!ides.Any(i => i.Nome.Equals("Kiro", StringComparison.OrdinalIgnoreCase)))
                {
                    var kiroId = Guid.NewGuid();
                    var kiro = new IDEDTO(kiroId, "Kiro", "kiro .", AceitaPerfilPersonalizado: true);
                    await _ideService.AddAsync(kiro);
                    _logger.LogInformation("IDE Kiro criada");
                }

                // Delphi
                if (!ides.Any(i => i.Nome.Equals("Delphi", StringComparison.OrdinalIgnoreCase)))
                {
                    var delphiId = Guid.NewGuid();
                    var delphi = new IDEDTO(delphiId, "Delphi", "bds -pDelphi -rBDSERP110203", AceitaPerfilPersonalizado: false);
                    await _ideService.AddAsync(delphi);
                    _logger.LogInformation("IDE Delphi criada");
                }

                // 2. Atualizar repositórios existentes
                var repositorios = await _repositorioService.GetAllAsync();
                
                foreach (var repo in repositorios)
                {
                    bool repoModificado = false;
                    var projetosAtualizados = new List<ProjetoDTO>();

                    foreach (var projeto in repo.Projetos)
                    {
                        // Converter todos os projetos para usar IDEIdentificador
                        // Se IDEIdentificador já existe, manter; senão, definir como VS Code
                        if (projeto.Comandos.IDEIdentificador == null)
                        {
                            // Criar novo ComandoDTO com IDEIdentificador
                            var novosComandos = projeto.Comandos with 
                            { 
                                IDEIdentificador = vsCodeId
                            };

                            // Criar novo ProjetoDTO com comandos atualizados
                            var projetoAtualizado = projeto with { Comandos = novosComandos };
                            projetosAtualizados.Add(projetoAtualizado);
                            
                            repoModificado = true;
                        }
                        else
                        {
                            projetosAtualizados.Add(projeto);
                        }
                    }

                    // Salvar repositório se foi modificado
                    if (repoModificado)
                    {
                        var repoAtualizado = repo with { Projetos = projetosAtualizados };
                        await _repositorioService.UpdateAsync(repo.Identificador, repoAtualizado);
                        _logger.LogInformation($"Repositório {repo.Nome} atualizado com IDEs");
                    }
                }

                _logger.LogInformation($"Migration {migrationName} concluída com sucesso");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao executar migration {migrationName}");
                throw;
            }
        }

        public async Task Migration_002_MigrateProgramDataToUserProfile()        {
            var origemPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "PMW", "Banco"
            );

            if (!Directory.Exists(origemPath))
            {
                _logger.LogInformation("Migration 002: Nenhum dado encontrado em ProgramData. Nada a migrar.");
                return;
            }

            var arquivos = Directory.GetFiles(origemPath, "*.json")
                .Concat(Directory.GetFiles(origemPath, "*.txt"))
                .ToList();

            if (arquivos.Count == 0)
            {
                _logger.LogInformation("Migration 002: ProgramData existe mas está vazio. Nada a migrar.");
                return;
            }

            _logger.LogInformation($"Migration 002: {arquivos.Count} arquivo(s) encontrado(s) em ProgramData. Iniciando migração...");

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var backupPath = Path.Combine(origemPath, $"bkp_{timestamp}");
            Directory.CreateDirectory(backupPath);

            var destino = PathHelper.BancoPath;
            if (!Directory.Exists(destino))
                Directory.CreateDirectory(destino);

            var erros = new List<string>();

            foreach (var arquivo in arquivos)
            {
                var nomeArquivo = Path.GetFileName(arquivo);
                var destinoArquivo = Path.Combine(destino, nomeArquivo);

                try
                {
                    File.Copy(arquivo, destinoArquivo, overwrite: true);
                    _logger.LogInformation($"Migration 002: Copiado {nomeArquivo} → {destinoArquivo}");
                }
                catch (Exception ex)
                {
                    erros.Add(nomeArquivo);
                    _logger.LogError(ex, $"Migration 002: Falha ao copiar {nomeArquivo}");
                }
            }

            if (erros.Count > 0)
            {
                _logger.LogWarning($"Migration 002: {erros.Count} arquivo(s) falharam na cópia: {string.Join(", ", erros)}. Arquivos originais mantidos.");
                return;
            }

            foreach (var arquivo in arquivos)
            {
                var nomeArquivo = Path.GetFileName(arquivo);
                var backupArquivo = Path.Combine(backupPath, nomeArquivo);

                try
                {
                    File.Move(arquivo, backupArquivo);
                    _logger.LogInformation($"Migration 002: Movido para backup: {nomeArquivo}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Migration 002: Falha ao mover {nomeArquivo} para backup");
                }
            }

            _logger.LogInformation($"Migration 002: Concluída. Backup em: {backupPath}");
        }

        public async Task Migration_003_AddComandoClone()
        {
            var repositorios = await _repositorioService.GetAllAsync();

            foreach (var repo in repositorios)
            {
                if (!string.IsNullOrWhiteSpace(repo.ComandoClone)) continue;

                var repoAtualizado = repo with { ComandoClone = "git clone --depth 1" };
                await _repositorioService.UpdateAsync(repo.Identificador, repoAtualizado);
                _logger.LogInformation($"Migration 003: ComandoClone definido para repositório {repo.Nome}");
            }
        }

        // --- MÉTODOS PRIVADOS DE ACESSO AO ARQUIVO ---

        private static async Task<MigrationsDTO> LerMigrationsDoArquivoAsync(bool locked = false)
        {
            if (!locked) await _semaphore.WaitAsync();
            try
            {
                if (!File.Exists(FilePath))
                    return new MigrationsDTO(new List<MigrationRecordDTO>());

                var jsonString = await File.ReadAllTextAsync(FilePath);
                if (string.IsNullOrWhiteSpace(jsonString))
                    return new MigrationsDTO(new List<MigrationRecordDTO>());

                return JsonSerializer.Deserialize<MigrationsDTO>(jsonString)
                    ?? new MigrationsDTO(new List<MigrationRecordDTO>());
            }
            finally
            {
                if (!locked) _semaphore.Release();
            }
        }

        private async Task GravarMigrationsNoArquivoAsync(MigrationsDTO migrations, bool locked = false)
        {
            if (!locked) await _semaphore.WaitAsync();
            try
            {
                var jsonString = JsonSerializer.Serialize(migrations, _jsonOptions);
                await File.WriteAllTextAsync(FilePath, jsonString);
            }
            finally
            {
                if (!locked) _semaphore.Release();
            }
        }
    }
}
