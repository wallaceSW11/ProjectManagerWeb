using System.Text.Json;
using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Utils;

namespace ProjectManagerWeb.src.Services
{
    public class PastaJsonService
    {
        private static readonly string BasePath = PathHelper.BancoPath;

        private static readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
        private readonly string _filePath;

        public PastaJsonService()
        {
            _filePath = Path.Combine(BasePath, "pastas.json");

            if (!Directory.Exists(BasePath))
                Directory.CreateDirectory(BasePath);
        }

        internal PastaJsonService(string testFilePath) : this()
        {
            _filePath = testFilePath;
        }

        public async Task<List<PastaCadastroRequestDTO>> GetAllAsync()
        {
            var pastas = await LerListaDoArquivoAsync();

            return [.. pastas.OrderBy(p => p.Indice)];
        }

        public async Task<PastaCadastroRequestDTO?> GetByDiretorioAsync(string diretorio)
        {
            var pastas = await LerListaDoArquivoAsync();
            return pastas.FirstOrDefault(p => p.Diretorio.Equals(diretorio, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<List<PastaCadastroRequestDTO>> GetByRepositorioIdAsync(Guid repositorioId)
        {
            var pastas = await LerListaDoArquivoAsync();
            return pastas.Where(p => p.RepositorioId == repositorioId).ToList();
        }


        public async Task<PastaCadastroRequestDTO> AddAsync(PastaCadastroRequestDTO novaPasta)
        {
            await _semaphore.WaitAsync();
            try
            {
                var pastas = await LerListaDoArquivoAsync(locked: true);

                pastas.RemoveAll(p => p.Diretorio.Equals(novaPasta.Diretorio, StringComparison.OrdinalIgnoreCase));

                pastas.Add(novaPasta);
                await GravarListaNoArquivoAsync(pastas, locked: true);

                return novaPasta;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<bool> UpdateAsync(string diretorio, PastaCadastroRequestDTO pastaAtualizada)
        {
            await _semaphore.WaitAsync();
            try
            {
                var pastas = await LerListaDoArquivoAsync(locked: true);
                var index = pastas.FindIndex(p => p.Diretorio.Equals(diretorio, StringComparison.OrdinalIgnoreCase));

                if (index == -1) return false;

                pastas[index] = pastaAtualizada;

                await GravarListaNoArquivoAsync(pastas, locked: true);
                return true;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<bool> DeleteAsync(string diretorio)
        {
            await _semaphore.WaitAsync();
            try
            {
                var pastas = await LerListaDoArquivoAsync(locked: true);
                var itemsRemovidos = pastas.RemoveAll(p => p.Diretorio.Equals(diretorio, StringComparison.OrdinalIgnoreCase));

                if (itemsRemovidos == 0) return false;

                await GravarListaNoArquivoAsync(pastas, locked: true);
                return true;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<bool> DeleteByRepositorioIdAsync(Guid repositorioId)
        {
            await _semaphore.WaitAsync();
            try
            {
                var pastas = await LerListaDoArquivoAsync(locked: true);
                var itemsRemovidos = pastas.RemoveAll(p => p.RepositorioId == repositorioId);

                if (itemsRemovidos == 0) return false;

                await GravarListaNoArquivoAsync(pastas, locked: true);
                return true;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task<List<PastaCadastroRequestDTO>> LerListaDoArquivoAsync(bool locked = false)
        {
            if (!locked) await _semaphore.WaitAsync();
            try
            {
                if (!File.Exists(_filePath)) return [];

                var jsonString = await File.ReadAllTextAsync(_filePath);
                if (string.IsNullOrWhiteSpace(jsonString)) return [];
                return JsonSerializer.Deserialize<List<PastaCadastroRequestDTO>>(jsonString) ?? [];
            }
            finally
            {
                if (!locked) _semaphore.Release();
            }
        }

        private async Task GravarListaNoArquivoAsync(List<PastaCadastroRequestDTO> pastas, bool locked = false)
        {
            if (!locked) await _semaphore.WaitAsync();
            try
            {
                var jsonString = JsonSerializer.Serialize(pastas, _jsonOptions);
                await File.WriteAllTextAsync(_filePath, jsonString);
            }
            finally
            {
                if (!locked) _semaphore.Release();
            }
        }
    }
}