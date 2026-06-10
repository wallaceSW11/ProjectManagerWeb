using System.Text.Json;
using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Services;

namespace ProjectManagerWeb.Tests.Services;

public class IDEJsonServiceTests : IDisposable
{
    private readonly string _tempPath;
    private readonly string _tempFile;
    private readonly IDEJsonService _sut;

    public IDEJsonServiceTests()
    {
        _tempPath = Path.Combine(Path.GetTempPath(), "pmw-tests-" + Guid.NewGuid());
        _tempFile = Path.Combine(_tempPath, "IDEs.json");
        Directory.CreateDirectory(_tempPath);
        _sut = new IDEJsonService(_tempFile);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempPath))
            Directory.Delete(_tempPath, true);
    }

    private async Task SalvarDireto(List<IDEDTO> ides)
    {
        var json = JsonSerializer.Serialize(ides);
        await File.WriteAllTextAsync(_tempFile, json);
    }

    private static IDEDTO CriarIDE(string nome)
    {
        return new IDEDTO(Guid.NewGuid(), nome, "code .", true);
    }

    public class AddAsync : IDEJsonServiceTests
    {
        [Fact]
        public async Task AddAsync_lanca_excecao_quando_nome_duplicado()
        {
            var ide1 = CriarIDE("VS Code");
            await _sut.AddAsync(ide1);

            var ide2 = CriarIDE("VS Code");
            var act = () => _sut.AddAsync(ide2);

            await act.Should().ThrowAsync<Exception>().WithMessage("*já existe*");
        }

        [Fact]
        public async Task AddAsync_adiciona_com_sucesso()
        {
            var ide = CriarIDE("VS Code");

            await _sut.AddAsync(ide);

            var todas = await _sut.GetAllAsync();
            todas.Should().HaveCount(1);
            todas[0].Nome.Should().Be("VS Code");
        }
    }

    public class GetByIdAsync : IDEJsonServiceTests
    {
        [Fact]
        public async Task GetByIdAsync_retorna_ide_quando_encontrado()
        {
            var ide = CriarIDE("VS Code");
            await _sut.AddAsync(ide);

            var resultado = await _sut.GetByIdAsync(ide.Identificador);

            resultado.Should().NotBeNull();
            resultado!.Identificador.Should().Be(ide.Identificador);
        }

        [Fact]
        public async Task GetByIdAsync_retorna_null_quando_nao_encontrado()
        {
            var resultado = await _sut.GetByIdAsync(Guid.NewGuid());

            resultado.Should().BeNull();
        }
    }

    public class DeleteAsync : IDEJsonServiceTests
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
            var ide = CriarIDE("VS Code");
            await _sut.AddAsync(ide);

            var resultado = await _sut.DeleteAsync(ide.Identificador);

            resultado.Should().BeTrue();
            var todas = await _sut.GetAllAsync();
            todas.Should().BeEmpty();
        }
    }

    public class UpdateAsync : IDEJsonServiceTests
    {
        [Fact]
        public async Task UpdateAsync_atualiza_e_preserva_identificador()
        {
            var ide = CriarIDE("VS Code");
            await _sut.AddAsync(ide);

            var atualizada = ide with { Nome = "VS Code Insiders", ComandoParaExecutar = "code-insiders ." };
            var resultado = await _sut.UpdateAsync(ide.Identificador, atualizada);

            resultado.Should().BeTrue();
            var obtida = await _sut.GetByIdAsync(ide.Identificador);
            obtida.Should().NotBeNull();
            obtida!.Nome.Should().Be("VS Code Insiders");
            obtida.Identificador.Should().Be(ide.Identificador);
        }
    }
}
