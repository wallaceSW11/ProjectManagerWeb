using System.Text.Json;
using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Utils;

namespace ProjectManagerWeb.src.Services
{
    public class SiteIISJsonService
    {
        private static readonly string BasePath = PathHelper.BancoPath;

        private static readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
        private readonly string _filePath;

        public SiteIISJsonService()
        {
            _filePath = Path.Combine(BasePath, "sites-iis.json");

            if (!Directory.Exists(BasePath))
                Directory.CreateDirectory(BasePath);
        }

        internal SiteIISJsonService(string testFilePath) : this()
        {
            _filePath = testFilePath;
        }

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

        private async Task<List<SiteIISRequestDTO>> LerListaDoArquivoAsync(bool locked = false)
        {
            if (!locked) await _semaphore.WaitAsync();
            try
            {
                if (!File.Exists(_filePath)) return [];

                var jsonString = await File.ReadAllTextAsync(_filePath);
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
                await File.WriteAllTextAsync(_filePath, jsonString);
            }
            finally
            {
                if (!locked) _semaphore.Release();
            }
        }
    }
}
