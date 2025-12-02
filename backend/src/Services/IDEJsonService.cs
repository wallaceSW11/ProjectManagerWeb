using System.Text.Json;
using ProjectManagerWeb.src.DTOs;

namespace ProjectManagerWeb.src.Services
{
    public class IDEJsonService
    {
        private static readonly string BasePath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "PMW", "Banco");

        private static readonly string FilePath =
            Path.Combine(BasePath, "IDEs.json");

        private static readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

        public IDEJsonService()
        {
            if (!Directory.Exists(BasePath))
            {
                Directory.CreateDirectory(BasePath);
            }
        }

        // --- MÉTODOS PÚBLICOS DO CRUD ---

        public async Task<List<IDEDTO>> GetAllAsync()
        {
            return await LerListaDoArquivoAsync();
        }

        public async Task<IDEDTO?> GetByIdAsync(Guid identificador)
        {
            var ides = await LerListaDoArquivoAsync();
            return ides.FirstOrDefault(i => i.Identificador == identificador);
        }

        public async Task<IDEDTO> AddAsync(IDEDTO novaIDE)
        {
            await _semaphore.WaitAsync();
            try
            {
                var ides = await LerListaDoArquivoAsync(locked: true);

                // Validação: não permitir nomes duplicados
                if (ides.Exists(ide => ide.Nome.Equals(novaIDE.Nome, StringComparison.OrdinalIgnoreCase)))
                    throw new Exception("Já existe uma IDE com esse nome");

                ides.Add(novaIDE);
                await GravarListaNoArquivoAsync(ides, locked: true);

                return novaIDE;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<bool> UpdateAsync(Guid identificador, IDEDTO ideAtualizada)
        {
            await _semaphore.WaitAsync();
            try
            {
                var ides = await LerListaDoArquivoAsync(locked: true);
                var index = ides.FindIndex(i => i.Identificador == identificador);

                if (index == -1) return false;

                ides[index] = ideAtualizada with { Identificador = identificador };

                await GravarListaNoArquivoAsync(ides, locked: true);
                return true;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<bool> DeleteAsync(Guid identificador)
        {
            await _semaphore.WaitAsync();
            try
            {
                var ides = await LerListaDoArquivoAsync(locked: true);
                var itemsRemovidos = ides.RemoveAll(i => i.Identificador == identificador);

                if (itemsRemovidos == 0) return false;

                await GravarListaNoArquivoAsync(ides, locked: true);
                return true;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Verifica se uma IDE está sendo referenciada por algum projeto.
        /// </summary>
        /// <param name="identificador">Identificador da IDE a verificar.</param>
        /// <param name="repositorioService">Serviço de repositórios para buscar referências.</param>
        /// <returns>True se a IDE está em uso, False caso contrário.</returns>
        public async Task<bool> IsReferencedByProjectsAsync(Guid identificador, RepositorioJsonService repositorioService)
        {
            var repositorios = await repositorioService.GetAllAsync();
            
            return repositorios
                .SelectMany(r => r.Projetos)
                .Any(p => p.Comandos.IDEIdentificador == identificador);
        }

        // --- MÉTODOS PRIVADOS DE ACESSO AO ARQUIVO ---

        private static async Task<List<IDEDTO>> LerListaDoArquivoAsync(bool locked = false)
        {
            if (!locked) await _semaphore.WaitAsync();
            try
            {
                if (!File.Exists(FilePath)) return [];

                var jsonString = await File.ReadAllTextAsync(FilePath);
                if (string.IsNullOrWhiteSpace(jsonString)) return [];
                return JsonSerializer.Deserialize<List<IDEDTO>>(jsonString) ?? [];
            }
            finally
            {
                if (!locked) _semaphore.Release();
            }
        }

        private async Task GravarListaNoArquivoAsync(List<IDEDTO> ides, bool locked = false)
        {
            if (!locked) await _semaphore.WaitAsync();
            try
            {
                var jsonString = JsonSerializer.Serialize(ides, _jsonOptions);
                await File.WriteAllTextAsync(FilePath, jsonString);
            }
            finally
            {
                if (!locked) _semaphore.Release();
            }
        }
    }
}
