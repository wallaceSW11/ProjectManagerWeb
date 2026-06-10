using System.Text.Json;
using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Services;

namespace ProjectManagerWeb.Tests.Services;

public class SiteIISJsonServiceTests : IDisposable
{
    private readonly string _tempPath;
    private readonly string _tempFile;
    private readonly SiteIISJsonService _sut;

    public SiteIISJsonServiceTests()
    {
        _tempPath = Path.Combine(Path.GetTempPath(), "pmw-tests-" + Guid.NewGuid());
        _tempFile = Path.Combine(_tempPath, "sites-iis.json");
        Directory.CreateDirectory(_tempPath);
        _sut = new SiteIISJsonService(_tempFile);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempPath))
            Directory.Delete(_tempPath, true);
    }

    private async Task SalvarDireto(List<SiteIISRequestDTO> sites)
    {
        var json = JsonSerializer.Serialize(sites);
        await File.WriteAllTextAsync(_tempFile, json);
    }

    private static SiteIISRequestDTO CriarSite(string nome, string titulo)
    {
        return new SiteIISRequestDTO(
            Guid.NewGuid(), titulo, nome,
            @"C:\inetpub\wwwroot", [], []);
    }

    public class AddAsync : SiteIISJsonServiceTests
    {
        [Fact]
        public async Task AddAsync_lanca_excecao_quando_nome_duplicado()
        {
            var site1 = CriarSite("MeuSite", "Meu Site");
            await _sut.AddAsync(site1);

            var site2 = CriarSite("MeuSite", "Outro Título");
            var act = () => _sut.AddAsync(site2);

            await act.Should().ThrowAsync<Exception>().WithMessage("*já existe*");
        }

        [Fact]
        public async Task AddAsync_adiciona_com_sucesso()
        {
            var site = CriarSite("MeuSite", "Meu Site");

            await _sut.AddAsync(site);

            var todos = await _sut.GetAllAsync();
            todos.Should().HaveCount(1);
            todos[0].Nome.Should().Be("MeuSite");
        }
    }

    public class GetByIdAsync : SiteIISJsonServiceTests
    {
        [Fact]
        public async Task GetByIdAsync_retorna_site_quando_encontrado()
        {
            var site = CriarSite("MeuSite", "Meu Site");
            await _sut.AddAsync(site);

            var resultado = await _sut.GetByIdAsync(site.Identificador);

            resultado.Should().NotBeNull();
            resultado!.Identificador.Should().Be(site.Identificador);
        }

        [Fact]
        public async Task GetByIdAsync_retorna_null_quando_nao_encontrado()
        {
            var resultado = await _sut.GetByIdAsync(Guid.NewGuid());

            resultado.Should().BeNull();
        }
    }

    public class DeleteAsync : SiteIISJsonServiceTests
    {
        [Fact]
        public async Task DeleteAsync_retorna_false_quando_nao_encontrado()
        {
            var resultado = await _sut.DeleteAsync(Guid.NewGuid());

            resultado.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteAsync_retorna_true_quando_remove()
        {
            var site = CriarSite("MeuSite", "Meu Site");
            await _sut.AddAsync(site);

            var resultado = await _sut.DeleteAsync(site.Identificador);

            resultado.Should().BeTrue();
            var todos = await _sut.GetAllAsync();
            todos.Should().BeEmpty();
        }
    }

    public class UpdateAsync : SiteIISJsonServiceTests
    {
        [Fact]
        public async Task UpdateAsync_atualiza_e_preserva_identificador()
        {
            var site = CriarSite("MeuSite", "Meu Site");
            await _sut.AddAsync(site);

            var atualizado = site with { Titulo = "Meu Site Atualizado" };
            var resultado = await _sut.UpdateAsync(site.Identificador, atualizado);

            resultado.Should().BeTrue();
            var obtido = await _sut.GetByIdAsync(site.Identificador);
            obtido.Should().NotBeNull();
            obtido!.Titulo.Should().Be("Meu Site Atualizado");
            obtido.Identificador.Should().Be(site.Identificador);
        }
    }
}
