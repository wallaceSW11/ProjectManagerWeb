using System.Text.Json;
using ProjectManagerWeb.src.DTOs;

namespace ProjectManagerWeb.src.Services
{
    public class RepositorioJsonService
    {
        private static readonly string BasePath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "PMW", "Banco");

        private static readonly string FilePath =
            Path.Combine(BasePath, "repositorios.json");

        private static readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

        public RepositorioJsonService()
        {
            if (!Directory.Exists(BasePath))
            {
                Directory.CreateDirectory(BasePath);
            }
        }

        // --- MÉTODOS PÚBLICOS DO CRUD ---

        public async Task<List<RepositorioRequestDTO>> GetAllAsync()
        {
            return await LerListaDoArquivoAsync();
        }

        public async Task<RepositorioRequestDTO?> GetByIdAsync(Guid identificador)
        {
            var repositorios = await LerListaDoArquivoAsync();
            return repositorios.FirstOrDefault(r => r.Identificador == identificador);
        }

        public async Task<ProjetoDTO> GetProjetoByIdAsync(Guid identificadorProjeto)
        {
            var repositorios = await LerListaDoArquivoAsync();
            return repositorios
                .SelectMany(r => r.Projetos)
                .FirstOrDefault(p => p.Identificador == identificadorProjeto)
                ?? throw new Exception($"Projeto não encontrado com o id {identificadorProjeto}");
        }

        public async Task<RepositorioRequestDTO?> GetByUrlAsync(string url)
        {
            var repositorios = await LerListaDoArquivoAsync();
            return repositorios.FirstOrDefault(r => r.Url.Equals(url, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<RepositorioRequestDTO> AddAsync(RepositorioRequestDTO novoRepositorio)
        {
            await _semaphore.WaitAsync();
            try
            {
                var repositorios = await LerListaDoArquivoAsync(locked: true);

                if (repositorios.Exists(repo => repo.Url.Equals(novoRepositorio.Url, StringComparison.OrdinalIgnoreCase)))
                    throw new Exception("Já existe um repositório com essa URL");

                var repositorioParaAdicionar = novoRepositorio with { Identificador = Guid.NewGuid() };

                repositorios.Add(repositorioParaAdicionar);
                await GravarListaNoArquivoAsync(repositorios, locked: true);

                return repositorioParaAdicionar;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<bool> UpdateAsync(Guid identificador, RepositorioRequestDTO repositorioAtualizado)
        {
            await _semaphore.WaitAsync();
            try
            {
                var repositorios = await LerListaDoArquivoAsync(locked: true);
                var index = repositorios.FindIndex(r => r.Identificador == identificador);

                if (index == -1) return false;

                repositorios[index] = repositorioAtualizado with { Identificador = identificador };

                await GravarListaNoArquivoAsync(repositorios, locked: true);
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
                var repositorios = await LerListaDoArquivoAsync(locked: true);
                var itemsRemovidos = repositorios.RemoveAll(r => r.Identificador == identificador);

                if (itemsRemovidos == 0) return false;

                await GravarListaNoArquivoAsync(repositorios, locked: true);
                return true;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        // --- MÉTODOS PRIVADOS DE ACESSO AO ARQUIVO ---

        private static async Task<List<RepositorioRequestDTO>> LerListaDoArquivoAsync(bool locked = false)
        {
            if (!locked) await _semaphore.WaitAsync();
            try
            {
                if (!File.Exists(FilePath)) return [];

                var jsonString = await File.ReadAllTextAsync(FilePath);
                if (string.IsNullOrWhiteSpace(jsonString)) return [];
                return JsonSerializer.Deserialize<List<RepositorioRequestDTO>>(jsonString) ?? [];
            }
            finally
            {
                if (!locked) _semaphore.Release();
            }
        }

        private async Task GravarListaNoArquivoAsync(List<RepositorioRequestDTO> repositorios, bool locked = false)
        {
            if (!locked) await _semaphore.WaitAsync();
            try
            {
                var jsonString = JsonSerializer.Serialize(repositorios, _jsonOptions);
                await File.WriteAllTextAsync(FilePath, jsonString);
            }
            finally
            {
                if (!locked) _semaphore.Release();
            }
        }
    }
}
