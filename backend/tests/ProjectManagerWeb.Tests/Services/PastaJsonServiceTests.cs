using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Services;

namespace ProjectManagerWeb.Tests.Services;

public class PastaJsonServiceTests : IDisposable
{
    private readonly string _tempPath;
    private readonly string _tempFile;
    private readonly PastaJsonService _sut;

    public PastaJsonServiceTests()
    {
        _tempPath = Path.Combine(Path.GetTempPath(), "pmw-tests-" + Guid.NewGuid());
        _tempFile = Path.Combine(_tempPath, "pastas.json");
        Directory.CreateDirectory(_tempPath);
        _sut = new PastaJsonService(_tempFile);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempPath))
            Directory.Delete(_tempPath, true);
    }

    private async Task SalvarDireto(List<PastaCadastroRequestDTO> pastas)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(pastas);
        await File.WriteAllTextAsync(_tempFile, json);
    }

    public class AddAsync : PastaJsonServiceTests
    {
        [Fact]
        public async Task AddAsync_faz_upsert_quando_diretorio_ja_existe()
        {
            var pasta1 = new PastaCadastroRequestDTO(
                Guid.NewGuid(), "/tmp/projeto", "PRJ1", "Projeto Um",
                "feature", "main", "git@github.com:user/repo.git", Guid.NewGuid());

            var pasta2 = new PastaCadastroRequestDTO(
                Guid.NewGuid(), "/tmp/projeto", "PRJ2", "Projeto Dois Atualizado",
                "bugfix", "develop", "git@github.com:user/repo.git", Guid.NewGuid());

            await _sut.AddAsync(pasta1);
            await _sut.AddAsync(pasta2);

            var todas = await _sut.GetAllAsync();
            todas.Should().HaveCount(1);
            todas[0].Codigo.Should().Be("PRJ2");
            todas[0].Descricao.Should().Be("Projeto Dois Atualizado");
        }

        [Fact]
        public async Task AddAsync_adiciona_normal_quando_diretorio_novo()
        {
            var pasta1 = new PastaCadastroRequestDTO(
                Guid.NewGuid(), "/tmp/projeto-a", "PRJ-A", "Projeto A",
                "feature", "main", "git@github.com:user/repo.git", Guid.NewGuid());

            var pasta2 = new PastaCadastroRequestDTO(
                Guid.NewGuid(), "/tmp/projeto-b", "PRJ-B", "Projeto B",
                "feature", "main", "git@github.com:user/repo2.git", Guid.NewGuid());

            await _sut.AddAsync(pasta1);
            await _sut.AddAsync(pasta2);

            var todas = await _sut.GetAllAsync();
            todas.Should().HaveCount(2);
        }
    }

    public class GetByDiretorioAsync : PastaJsonServiceTests
    {
        [Fact]
        public async Task GetByDiretorioAsync_retorna_null_quando_nao_encontrado()
        {
            var resultado = await _sut.GetByDiretorioAsync("/tmp/inexistente");

            resultado.Should().BeNull();
        }

        [Fact]
        public async Task GetByDiretorioAsync_retorna_pasta_quando_encontrado()
        {
            var pasta = new PastaCadastroRequestDTO(
                Guid.NewGuid(), "/tmp/projeto", "PRJ", "Projeto",
                "feature", "main", "git@github.com:user/repo.git", Guid.NewGuid());

            await _sut.AddAsync(pasta);

            var resultado = await _sut.GetByDiretorioAsync("/tmp/projeto");

            resultado.Should().NotBeNull();
            resultado!.Identificador.Should().Be(pasta.Identificador);
            resultado.Diretorio.Should().Be("/tmp/projeto");
        }
    }

    public class DeleteAsync : PastaJsonServiceTests
    {
        [Fact]
        public async Task DeleteAsync_retorna_false_quando_nao_encontrado()
        {
            var resultado = await _sut.DeleteAsync("/tmp/inexistente");

            resultado.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteAsync_retorna_true_quando_remove()
        {
            var pasta = new PastaCadastroRequestDTO(
                Guid.NewGuid(), "/tmp/projeto", "PRJ", "Projeto",
                "feature", "main", "git@github.com:user/repo.git", Guid.NewGuid());

            await _sut.AddAsync(pasta);

            var resultado = await _sut.DeleteAsync("/tmp/projeto");

            resultado.Should().BeTrue();
            var todas = await _sut.GetAllAsync();
            todas.Should().BeEmpty();
        }
    }

    public class UpdateAsync : PastaJsonServiceTests
    {
        [Fact]
        public async Task UpdateAsync_retorna_false_quando_nao_encontrado()
        {
            var pasta = new PastaCadastroRequestDTO(
                Guid.NewGuid(), "/tmp/projeto", "PRJ", "Projeto",
                "feature", "main", "git@github.com:user/repo.git", Guid.NewGuid());

            var resultado = await _sut.UpdateAsync("/tmp/inexistente", pasta);

            resultado.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateAsync_atualiza_e_retorna_true()
        {
            var pasta = new PastaCadastroRequestDTO(
                Guid.NewGuid(), "/tmp/projeto", "PRJ", "Projeto Original",
                "feature", "main", "git@github.com:user/repo.git", Guid.NewGuid());

            await _sut.AddAsync(pasta);

            var atualizada = pasta with { Descricao = "Projeto Atualizado" };

            var resultado = await _sut.UpdateAsync("/tmp/projeto", atualizada);

            resultado.Should().BeTrue();
            var obtida = await _sut.GetByDiretorioAsync("/tmp/projeto");
            obtida!.Descricao.Should().Be("Projeto Atualizado");
        }
    }

    public class DeleteByRepositorioIdAsync : PastaJsonServiceTests
    {
        [Fact]
        public async Task DeleteByRepositorioIdAsync_remove_multiplos()
        {
            var repoId = Guid.NewGuid();
            var outroRepoId = Guid.NewGuid();

            var pasta1 = new PastaCadastroRequestDTO(
                Guid.NewGuid(), "/tmp/repo1-a", "R1A", "Repo1 A",
                "feature", "main", "url", repoId);

            var pasta2 = new PastaCadastroRequestDTO(
                Guid.NewGuid(), "/tmp/repo1-b", "R1B", "Repo1 B",
                "feature", "main", "url", repoId);

            var pasta3 = new PastaCadastroRequestDTO(
                Guid.NewGuid(), "/tmp/repo2-a", "R2A", "Repo2 A",
                "feature", "main", "url", outroRepoId);

            await _sut.AddAsync(pasta1);
            await _sut.AddAsync(pasta2);
            await _sut.AddAsync(pasta3);

            var resultado = await _sut.DeleteByRepositorioIdAsync(repoId);

            resultado.Should().BeTrue();
            var todas = await _sut.GetAllAsync();
            todas.Should().HaveCount(1);
            todas[0].Identificador.Should().Be(pasta3.Identificador);
        }
    }
}
