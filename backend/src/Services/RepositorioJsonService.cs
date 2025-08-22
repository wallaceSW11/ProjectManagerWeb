using System.Text.Json;
using ProjectManagerWeb.src.DTOs;

namespace ProjectManagerWeb.src.Services
{
    public class RepositorioJsonService
    {
        private const string FilePath = "Banco\\repositorios.json";
        private static readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

        public RepositorioJsonService()
        {
           // Pega o nome do diretório a partir do caminho completo do arquivo.
            var directoryName = Path.GetDirectoryName(FilePath);

            // Se o nome do diretório não for nulo ou vazio...
            if (!string.IsNullOrEmpty(directoryName))
            {
                // ...e se o diretório não existir...
                if (!Directory.Exists(directoryName))
                {
                    // ...cria o diretório.
                    Directory.CreateDirectory(directoryName);
                }
            }
        }

        // --- MÉTODOS PÚBLICOS DO CRUD ---

        public async Task<List<RepositorioRequestDTO>> GetAllAsync()
        {
            return await LerListaDoArquivoAsync();
        }

        public async Task<RepositorioRequestDTO?> GetByIdAsync(Guid id)
        {
            var repositorios = await LerListaDoArquivoAsync();
            return repositorios.FirstOrDefault(r => r.Id == id);
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

                if (repositorios.Exists(repo => repo.Url.Equals(novoRepositorio.Url))) throw new Exception("Já exite um repositório com essa url");
                
                // Cria um novo objeto com um novo Guid para garantir a unicidade
                var repositorioParaAdicionar = novoRepositorio with { Id = Guid.NewGuid() };

                repositorios.Add(repositorioParaAdicionar);
                await GravarListaNoArquivoAsync(repositorios, locked: true);
                
                return repositorioParaAdicionar; // Retorna o objeto com o novo ID
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<bool> UpdateAsync(Guid id, RepositorioRequestDTO repositorioAtualizado)
        {
            await _semaphore.WaitAsync();
            try
            {
                var repositorios = await LerListaDoArquivoAsync(locked: true);
                var index = repositorios.FindIndex(r => r.Id == id);

                if (index == -1) return false; // Não encontrou, não pode atualizar

                // Garante que o ID do objeto atualizado é o ID correto da URL
                repositorios[index] = repositorioAtualizado with { Id = id };
                
                await GravarListaNoArquivoAsync(repositorios, locked: true);
                return true; // Sucesso
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            await _semaphore.WaitAsync();
            try
            {
                var repositorios = await LerListaDoArquivoAsync(locked: true);
                var itemsRemovidos = repositorios.RemoveAll(r => r.Id == id);
                
                if (itemsRemovidos == 0) return false; // Não encontrou, nada a excluir

                await GravarListaNoArquivoAsync(repositorios, locked: true);
                return true; // Sucesso
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