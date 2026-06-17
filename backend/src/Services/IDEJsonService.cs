using System.Text.Json;
using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Utils;

namespace ProjectManagerWeb.src.Services
{
    public class IDEJsonService : IIDEJsonService
    {
        private static readonly string BasePath = PathHelper.BancoPath;

        private static readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
        private readonly string _filePath;

        public IDEJsonService()
        {
            _filePath = Path.Combine(BasePath, "IDEs.json");

            if (!Directory.Exists(BasePath))
                Directory.CreateDirectory(BasePath);
        }

        internal IDEJsonService(string testFilePath) : this()
        {
            _filePath = testFilePath;
        }

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

        public async Task<bool> IsReferencedByProjectsAsync(Guid identificador, RepositorioJsonService repositorioService)
        {
            var repositorios = await repositorioService.GetAllAsync();

            return repositorios
                .SelectMany(r => r.Projetos)
                .Any(p => p.Comandos.IDEIdentificador == identificador);
        }

        private async Task<List<IDEDTO>> LerListaDoArquivoAsync(bool locked = false)
        {
            if (!locked) await _semaphore.WaitAsync();
            try
            {
                if (!File.Exists(_filePath)) return [];

                var jsonString = await File.ReadAllTextAsync(_filePath);
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
                await File.WriteAllTextAsync(_filePath, jsonString);
            }
            finally
            {
                if (!locked) _semaphore.Release();
            }
        }
    }
}
