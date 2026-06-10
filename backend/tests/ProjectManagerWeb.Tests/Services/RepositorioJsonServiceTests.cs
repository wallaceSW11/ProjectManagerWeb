using System.Text.Json;
using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Services;

namespace ProjectManagerWeb.Tests.Services;

public class RepositorioJsonServiceTests : IDisposable
{
    private readonly string _tempPath;
    private readonly string _tempFile;
    private readonly RepositorioJsonService _sut;

    public RepositorioJsonServiceTests()
    {
        _tempPath = Path.Combine(Path.GetTempPath(), "pmw-tests-" + Guid.NewGuid());
        _tempFile = Path.Combine(_tempPath, "repositorios.json");
        Directory.CreateDirectory(_tempPath);
        _sut = new RepositorioJsonService(_tempFile);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempPath))
            Directory.Delete(_tempPath, true);
    }

    private async Task SalvarDireto(List<RepositorioRequestDTO> repositorios)
    {
        var json = JsonSerializer.Serialize(repositorios);
        await File.WriteAllTextAsync(_tempFile, json);
    }

    private static RepositorioRequestDTO CriarRepositorio(string url, string nome)
    {
        return new RepositorioRequestDTO(
            Guid.NewGuid(), url, nome, nome,
            null, "main", [], null, null, null);
    }

    public class AddAsync : RepositorioJsonServiceTests
    {
        [Fact]
        public async Task AddAsync_lanca_excecao_quando_url_duplicada()
        {
            var repo = CriarRepositorio("https://github.com/user/repo.git", "Repo1");

            await _sut.AddAsync(repo);

            var act = () => _sut.AddAsync(repo);

            await act.Should().ThrowAsync<Exception>().WithMessage("*já existe*");
        }

        [Fact]
        public async Task AddAsync_adiciona_com_sucesso()
        {
            var repo = CriarRepositorio("https://github.com/user/repo.git", "Repo1");

            await _sut.AddAsync(repo);

            var todos = await _sut.GetAllAsync();
            todos.Should().HaveCount(1);
            todos[0].Url.Should().Be("https://github.com/user/repo.git");
        }
    }

    public class GetByIdAsync : RepositorioJsonServiceTests
    {
        [Fact]
        public async Task GetByIdAsync_retorna_repositorio_quando_encontrado()
        {
            var repo = CriarRepositorio("https://github.com/user/repo.git", "Repo1");
            await _sut.AddAsync(repo);

            var resultado = await _sut.GetByIdAsync(repo.Identificador);

            resultado.Should().NotBeNull();
            resultado!.Identificador.Should().Be(repo.Identificador);
        }

        [Fact]
        public async Task GetByIdAsync_retorna_null_quando_nao_encontrado()
        {
            var resultado = await _sut.GetByIdAsync(Guid.NewGuid());

            resultado.Should().BeNull();
        }
    }

    public class DeleteAsync : RepositorioJsonServiceTests
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
            var repo = CriarRepositorio("https://github.com/user/repo.git", "Repo1");
            await _sut.AddAsync(repo);

            var resultado = await _sut.DeleteAsync(repo.Identificador);

            resultado.Should().BeTrue();
            var todos = await _sut.GetAllAsync();
            todos.Should().BeEmpty();
        }
    }

    public class UpdateAsync : RepositorioJsonServiceTests
    {
        [Fact]
        public async Task UpdateAsync_retorna_false_quando_nao_encontrado()
        {
            var repo = CriarRepositorio("https://github.com/user/repo.git", "Repo1");

            var resultado = await _sut.UpdateAsync(Guid.NewGuid(), repo);

            resultado.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateAsync_atualiza_e_preserva_identificador()
        {
            var repo = CriarRepositorio("https://github.com/user/repo.git", "Repo1");
            await _sut.AddAsync(repo);

            var atualizado = repo with { Nome = "Repo Atualizado" };
            var resultado = await _sut.UpdateAsync(repo.Identificador, atualizado);

            resultado.Should().BeTrue();
            var obtido = await _sut.GetByIdAsync(repo.Identificador);
            obtido.Should().NotBeNull();
            obtido!.Nome.Should().Be("Repo Atualizado");
            obtido.Identificador.Should().Be(repo.Identificador);
        }
    }

    public class RenomearPerfilVSCodeAsync : RepositorioJsonServiceTests
    {
        [Fact]
        public async Task RenomearPerfilVSCodeAsync_atualiza_no_repo_e_nos_projetos()
        {
            var projeto = new ProjetoDTO(
                Guid.NewGuid(), "WebApp", "src/web", "perfil-antigo",
                new ComandoDTO("npm i", "npm start", "npm build", null), null);

            var repo = new RepositorioRequestDTO(
                Guid.NewGuid(), "https://github.com/user/repo.git", "Repo1", "Repo1",
                null, "main", [projeto], null, null, null,
                PerfilVSCode: "perfil-antigo");

            await _sut.AddAsync(repo);

            await _sut.RenomearPerfilVSCodeAsync("perfil-antigo", "perfil-novo");

            var obtido = await _sut.GetByIdAsync(repo.Identificador);
            obtido.Should().NotBeNull();
            obtido!.PerfilVSCode.Should().Be("perfil-novo");
            obtido.Projetos[0].PerfilVSCode.Should().Be("perfil-novo");
        }
    }
}
