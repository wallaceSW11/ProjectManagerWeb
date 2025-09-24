using System.Text.Json;
using ProjectManagerWeb.src.DTOs;

namespace ProjectManagerWeb.src.Services
{
    public class PastaJsonService
    {
        private static readonly string BasePath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "PMW", "Banco");

        private static readonly string FilePath =
            Path.Combine(BasePath, "pastas.json");

        private static readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

        public PastaJsonService()
        {
            if (!Directory.Exists(BasePath))
            {
                Directory.CreateDirectory(BasePath);
            }
        }

        // --- MÉTODOS PÚBLICOS DO CRUD ---

        public async Task<List<PastaCadastroRequestDTO>> GetAllAsync()
        {
            return await LerListaDoArquivoAsync();
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

                if (pastas.Exists(pasta => pasta.Diretorio.Equals(novaPasta.Diretorio, StringComparison.OrdinalIgnoreCase)))
                    await DeleteAsync(novaPasta.Diretorio);

                var pastaParaAdicionar = novaPasta;
                pastas.Add(pastaParaAdicionar);
                await GravarListaNoArquivoAsync(pastas, locked: true);

                return pastaParaAdicionar;
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

        // --- MÉTODOS PRIVADOS DE ACESSO AO ARQUIVO ---

        private static async Task<List<PastaCadastroRequestDTO>> LerListaDoArquivoAsync(bool locked = false)
        {
            if (!locked) await _semaphore.WaitAsync();
            try
            {
                if (!File.Exists(FilePath)) return [];

                var jsonString = await File.ReadAllTextAsync(FilePath);
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
                await File.WriteAllTextAsync(FilePath, jsonString);
            }
            finally
            {
                if (!locked) _semaphore.Release();
            }
        }
    }
}