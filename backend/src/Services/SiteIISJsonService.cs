using System.Text.Json;
using ProjectManagerWeb.src.DTOs;

namespace ProjectManagerWeb.src.Services
{
    /// <summary>
    /// Service para gerenciar persistência de Sites IIS em arquivo JSON
    /// </summary>
    public class SiteIISJsonService
    {
        private static readonly string BasePath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "PMW", "Banco");

        private static readonly string FilePath =
            Path.Combine(BasePath, "sites-iis.json");

        private static readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

        public SiteIISJsonService()
        {
            if (!Directory.Exists(BasePath))
            {
                Directory.CreateDirectory(BasePath);
            }
        }

        // --- MÉTODOS PÚBLICOS DO CRUD ---

        public async Task<List<SiteIISRequestDTO>> GetAllAsync()
        {
            return await LerListaDoArquivoAsync();
        }

        public async Task<SiteIISRequestDTO?> GetByIdAsync(Guid identificador)
        {
            var sites = await LerListaDoArquivoAsync();
            return sites.FirstOrDefault(s => s.Identificador == identificador);
        }

        public async Task<SiteIISRequestDTO?> GetByNomeAsync(string nome)
        {
            var sites = await LerListaDoArquivoAsync();
            return sites.FirstOrDefault(s => s.Nome.Equals(nome, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<SiteIISRequestDTO> AddAsync(SiteIISRequestDTO novoSite)
        {
            await _semaphore.WaitAsync();
            try
            {
                var sites = await LerListaDoArquivoAsync(locked: true);

                if (sites.Exists(s => s.Nome.Equals(novoSite.Nome, StringComparison.OrdinalIgnoreCase)))
                    throw new Exception("Já existe um site com esse nome");

                sites.Add(novoSite);
                await GravarListaNoArquivoAsync(sites, locked: true);

                return novoSite;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<bool> UpdateAsync(Guid identificador, SiteIISRequestDTO siteAtualizado)
        {
            await _semaphore.WaitAsync();
            try
            {
                var sites = await LerListaDoArquivoAsync(locked: true);
                var index = sites.FindIndex(s => s.Identificador == identificador);

                if (index == -1) return false;

                sites[index] = siteAtualizado with { Identificador = identificador };

                await GravarListaNoArquivoAsync(sites, locked: true);
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
                var sites = await LerListaDoArquivoAsync(locked: true);
                var itemsRemovidos = sites.RemoveAll(s => s.Identificador == identificador);

                if (itemsRemovidos == 0) return false;

                await GravarListaNoArquivoAsync(sites, locked: true);
                return true;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        // --- MÉTODOS PRIVADOS DE ACESSO AO ARQUIVO ---

        private static async Task<List<SiteIISRequestDTO>> LerListaDoArquivoAsync(bool locked = false)
        {
            if (!locked) await _semaphore.WaitAsync();
            try
            {
                if (!File.Exists(FilePath)) return [];

                var jsonString = await File.ReadAllTextAsync(FilePath);
                if (string.IsNullOrWhiteSpace(jsonString)) return [];
                return JsonSerializer.Deserialize<List<SiteIISRequestDTO>>(jsonString) ?? [];
            }
            finally
            {
                if (!locked) _semaphore.Release();
            }
        }

        private async Task GravarListaNoArquivoAsync(List<SiteIISRequestDTO> sites, bool locked = false)
        {
            if (!locked) await _semaphore.WaitAsync();
            try
            {
                var jsonString = JsonSerializer.Serialize(sites, _jsonOptions);
                await File.WriteAllTextAsync(FilePath, jsonString);
            }
            finally
            {
                if (!locked) _semaphore.Release();
            }
        }
    }
}
