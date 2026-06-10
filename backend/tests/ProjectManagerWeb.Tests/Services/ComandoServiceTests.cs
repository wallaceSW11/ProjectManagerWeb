using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Services;
using ProjectManagerWeb.src.Utils;

namespace ProjectManagerWeb.Tests.Services;

public class ComandoServiceTests
{
    private readonly RepositorioJsonService _repositorioJson = Substitute.For<RepositorioJsonService>();
    private readonly IIDEJsonService _ideJson = Substitute.For<IIDEJsonService>();
    private readonly IShellExecutor _shell = Substitute.For<IShellExecutor>();
    private readonly ComandoService _sut;

    public ComandoServiceTests()
    {
        _sut = new ComandoService(_repositorioJson, _ideJson, _shell);
    }

    public class ObterAlvoIDE : ComandoServiceTests
    {
        [Fact]
        public void Deve_retornar_ponto_quando_workspace_desabilitado()
        {
            var alvo = ComandoService.ObterAlvoIDE("/tmp/dir", false);

            alvo.Should().Be(".");
        }

        [Fact]
        public void Deve_retornar_ponto_quando_diretorio_nao_existe()
        {
            var alvo = ComandoService.ObterAlvoIDE("/tmp/dir_inexistente", true);

            alvo.Should().Be(".");
        }

        [Fact]
        public void Deve_retornar_ponto_quando_diretorio_existe_mas_sem_workspace()
        {
            var alvo = ComandoService.ObterAlvoIDE(Path.GetTempPath(), true);

            alvo.Should().Be(".");
        }

        [Fact]
        public void Deve_retornar_caminho_do_workspace_quando_existe_arquivo_code_workspace()
        {
            var dir = Path.Combine(Path.GetTempPath(), "test-workspace-" + Guid.NewGuid());
            Directory.CreateDirectory(dir);
            var workspacePath = Path.Combine(dir, "projeto.code-workspace");
            File.WriteAllText(workspacePath, "{}");

            var alvo = ComandoService.ObterAlvoIDE(dir, true);

            alvo.Should().Be($"\"{workspacePath}\"");

            Directory.Delete(dir, true);
        }
    }

    public class ExecutarComandoAvulso : ComandoServiceTests
    {
        [Fact]
        public void Deve_retornar_true_quando_comando_executa_com_sucesso()
        {
            var resultado = _sut.ExecutarComandoAvulso("echo hello");

            resultado.Should().BeTrue();
            _shell.Received(1).ExecutarComando("echo hello", null, null);
        }

        [Fact]
        public void Deve_retornar_false_quando_shell_lanca_excecao()
        {
            _shell.When(x => x.ExecutarComando(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()))
                .Do(_ => throw new Exception("erro simulado"));

            var resultado = _sut.ExecutarComandoAvulso("comando que falha");

            resultado.Should().BeFalse();
        }

        [Fact]
        public void Deve_passar_token_e_perfil_para_o_shell()
        {
            _sut.ExecutarComandoAvulso("echo hello", "perfil-x", "token-abc");

            _shell.Received(1).ExecutarComando("echo hello", "perfil-x", "token-abc");
        }
    }

    public class AbrirPastaIDE : ComandoServiceTests
    {
        private readonly Guid _ideId = Guid.NewGuid();

        [Fact]
        public async Task Deve_lancar_excecao_quando_ide_nao_encontrada()
        {
            _ideJson.GetByIdAsync(_ideId).Returns((IDEDTO?)null);

            var request = new AbrirPastaIDERequestDTO("/tmp/dir", _ideId);
            var act = () => _sut.AbrirPastaIDE(request);

            await act.Should().ThrowAsync<Exception>().WithMessage("IDE não encontrada");
        }

        [Fact]
        public async Task Deve_chamar_executar_comando_sem_interface_quando_ide_existe()
        {
            var ide = new IDEDTO(_ideId, "VS Code", "code", true);
            _ideJson.GetByIdAsync(_ideId).Returns(ide);

            var request = new AbrirPastaIDERequestDTO("/tmp/dir", _ideId);
            var resultado = await _sut.AbrirPastaIDE(request);

            resultado.Should().BeTrue();
            _shell.Received(1).ExecutarComandoSemInterface(Arg.Is<string>(c =>
                c.Contains("cd \"/tmp/dir\"") && c.Contains("code .") && c.Contains("Exit;")));
        }

        [Fact]
        public async Task Deve_incluir_perfil_vscode_quando_ide_aceita_e_perfil_informado()
        {
            var ide = new IDEDTO(_ideId, "VS Code", "code", true);
            _ideJson.GetByIdAsync(_ideId).Returns(ide);

            var request = new AbrirPastaIDERequestDTO("/tmp/dir", _ideId, "perfil-web");
            var resultado = await _sut.AbrirPastaIDE(request);

            resultado.Should().BeTrue();
            _shell.Received(1).ExecutarComandoSemInterface(Arg.Is<string>(c =>
                c.Contains("--profile \"perfil-web\"") && c.Contains("code")));
        }

        [Fact]
        public async Task Nao_deve_incluir_perfil_quando_ide_nao_aceita_perfil()
        {
            var ide = new IDEDTO(_ideId, "Kiro", "kiro", false);
            _ideJson.GetByIdAsync(_ideId).Returns(ide);

            var request = new AbrirPastaIDERequestDTO("/tmp/dir", _ideId, "perfil-web");
            var resultado = await _sut.AbrirPastaIDE(request);

            resultado.Should().BeTrue();
            _shell.Received(1).ExecutarComandoSemInterface(Arg.Is<string>(c =>
                c.Contains("kiro .") && !c.Contains("--profile")));
        }

        [Fact]
        public async Task Deve_retornar_false_quando_shell_lanca_excecao()
        {
            var ide = new IDEDTO(_ideId, "VS Code", "code", true);
            _ideJson.GetByIdAsync(_ideId).Returns(ide);
            _shell.When(x => x.ExecutarComandoSemInterface(Arg.Any<string>()))
                .Do(_ => throw new Exception("erro simulado"));

            var request = new AbrirPastaIDERequestDTO("/tmp/dir", _ideId);
            var resultado = await _sut.AbrirPastaIDE(request);

            resultado.Should().BeFalse();
        }
    }
}
