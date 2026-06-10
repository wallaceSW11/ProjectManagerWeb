using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Services;

namespace ProjectManagerWeb.Tests.Services;

public class CloneServiceTests
{
    private readonly IGitCommandRunner _gitRunner = Substitute.For<IGitCommandRunner>();
    private readonly RepositorioJsonService _repositorioJson = Substitute.For<RepositorioJsonService>();
    private readonly CloneService _sut;

    public CloneServiceTests()
    {
        _sut = new CloneService(_repositorioJson, _gitRunner);
    }

    public class VerificarBranchExisteAsync : CloneServiceTests
    {
        [Fact]
        public async Task Deve_retornar_false_com_mensagem_do_git_quando_git_falha()
        {
            _gitRunner.RunAsync(Arg.Any<string>(), Arg.Any<string>())
                .Returns(new ComandoResultado(string.Empty, "fatal: not found", 128));

            var (existe, erro) = await _sut.VerificarBranchExisteAsync("url", "branch");

            existe.Should().BeFalse();
            erro.Should().Be("fatal: not found");
        }

        [Fact]
        public async Task Deve_retornar_false_com_mensagem_padrao_quando_git_falha_sem_mensagem()
        {
            _gitRunner.RunAsync(Arg.Any<string>(), Arg.Any<string>())
                .Returns(new ComandoResultado(string.Empty, string.Empty, 1));

            var (existe, erro) = await _sut.VerificarBranchExisteAsync("url", "branch");

            existe.Should().BeFalse();
            erro.Should().Be("Falha ao consultar repositório remoto");
        }

        [Fact]
        public async Task Deve_retornar_false_sem_mensagem_quando_branch_nao_existe()
        {
            _gitRunner.RunAsync(Arg.Any<string>(), Arg.Any<string>())
                .Returns(new ComandoResultado(string.Empty, string.Empty, 0));

            var (existe, erro) = await _sut.VerificarBranchExisteAsync("url", "branch");

            existe.Should().BeFalse();
            erro.Should().BeNull();
        }

        [Fact]
        public async Task Deve_retornar_true_quando_branch_existe()
        {
            _gitRunner.RunAsync(Arg.Any<string>(), Arg.Any<string>())
                .Returns(new ComandoResultado("abc123\thead", string.Empty, 0));

            var (existe, erro) = await _sut.VerificarBranchExisteAsync("url", "branch");

            existe.Should().BeTrue();
            erro.Should().BeNull();
        }
    }

    public class DetectarBranchPrincipalAsync : CloneServiceTests
    {
        [Fact]
        public async Task Deve_retornar_vazio_quando_git_falha_ao_consultar()
        {
            _gitRunner.RunAsync(Arg.Any<string>(), Arg.Any<string>())
                .Returns(new ComandoResultado(string.Empty, string.Empty, 1));

            var resultado = await _sut.DetectarBranchPrincipalAsync("url");

            resultado.Should().BeEmpty();
        }

        [Fact]
        public async Task Deve_retornar_vazio_quando_remoto_nao_tem_head()
        {
            _gitRunner.RunAsync(Arg.Any<string>(), Arg.Any<string>())
                .Returns(new ComandoResultado("abc123\thead", string.Empty, 0));

            var resultado = await _sut.DetectarBranchPrincipalAsync("url");

            resultado.Should().BeEmpty();
        }

        [Fact]
        public async Task Deve_retornar_main_quando_branch_padrao_eh_main()
        {
            _gitRunner.RunAsync(Arg.Any<string>(), Arg.Any<string>())
                .Returns(new ComandoResultado("ref: refs/heads/main\tHEAD", string.Empty, 0));

            var resultado = await _sut.DetectarBranchPrincipalAsync("url");

            resultado.Should().Be("main");
        }

        [Fact]
        public async Task Deve_retornar_develop_quando_branch_padrao_eh_develop()
        {
            _gitRunner.RunAsync(Arg.Any<string>(), Arg.Any<string>())
                .Returns(new ComandoResultado("ref: refs/heads/develop\tHEAD\nabc123\thead", string.Empty, 0));

            var resultado = await _sut.DetectarBranchPrincipalAsync("url");

            resultado.Should().Be("develop");
        }
    }

    public class EhBranchBase : CloneServiceTests
    {
        [Theory]
        [InlineData("main", true)]
        [InlineData("master", true)]
        [InlineData("develop", true)]
        [InlineData("dev", true)]
        [InlineData("feature/nova", false)]
        [InlineData("fix/bug", false)]
        [InlineData("release", false)]
        public void Deve_considerar_base_apenas_main_master_develop_dev(string branch, bool esperado)
        {
            CloneService.EhBranchBase(branch).Should().Be(esperado);
        }
    }

    public class MontarScript : CloneServiceTests
    {
        private static CloneRequestDTO CriarClone(string branch, string tipo = "feature", bool criarBranchRemoto = false, bool historicoCompleto = false)
            => new("/raiz", "FAT-123", "descricao", tipo, branch, Guid.NewGuid(), criarBranchRemoto, false, false, historicoCompleto);

        [Fact]
        public void Deve_gerar_clone_com_filter_e_single_branch_quando_branch_base()
        {
            var clone = CriarClone("main");

            var script = CloneService.MontarScript("/raiz/FAT-123_descricao", "url", "repo", clone);

            script.Should().Contain("git clone --filter=blob:none --single-branch --branch main \"url\"");
            script.Should().Contain("cd \"/raiz/FAT-123_descricao\"");
            script.Should().Contain("cd \"repo\"");
        }

        [Fact]
        public void Deve_clonar_main_primeiro_e_depois_fazer_fetch_quando_branch_nao_eh_base()
        {
            var clone = CriarClone("feature/nova");

            var script = CloneService.MontarScript("/raiz/FAT-123_descricao", "url", "repo", clone, "main");

            script.Should().Contain("git clone --filter=blob:none --single-branch --branch main \"url\"");
            script.Should().Contain("git fetch origin feature/nova");
            script.Should().Contain("git checkout -b feature/nova FETCH_HEAD");
        }

        [Fact]
        public void Nao_deve_usar_filter_quando_historico_completo()
        {
            var clone = CriarClone("main", historicoCompleto: true);

            var script = CloneService.MontarScript("/raiz/FAT-123_descricao", "url", "repo", clone);

            script.Should().NotContain("--filter=blob:none");
            script.Should().Contain("git clone --branch main");
        }

        [Fact]
        public void Deve_criar_branch_com_apenas_codigo_quando_tipo_eh_nenhum()
        {
            var clone = CriarClone("main", "nenhum", criarBranchRemoto: true);

            var script = CloneService.MontarScript("/raiz/FAT-123_descricao", "url", "repo", clone);

            script.Should().Contain("git checkout -b FAT-123");
        }

        [Fact]
        public void Deve_criar_branch_com_prefixo_do_tipo_quando_tipo_informado()
        {
            var clone = CriarClone("main", "feature", criarBranchRemoto: true);

            var script = CloneService.MontarScript("/raiz/FAT-123_descricao", "url", "repo", clone);

            script.Should().Contain("git checkout -b feature/FAT-123");
        }

        [Fact]
        public void Nao_deve_gerar_checkout_quando_nao_deve_criar_branch_remoto()
        {
            var clone = CriarClone("main");

            var script = CloneService.MontarScript("/raiz/FAT-123_descricao", "url", "repo", clone);

            script.Should().NotContain("git checkout -b");
        }
    }

    public class FiltrarWarnings : CloneServiceTests
    {
        [Fact]
        public void Deve_retornar_vazio_quando_stderr_eh_vazio()
        {
            var resultado = GitCommandRunner.FiltrarWarnings(string.Empty);

            resultado.Should().BeEmpty();
        }

        [Fact]
        public void Deve_remover_linhas_de_environment_variable()
        {
            var stderr = "Environment variable $HOME not set\nreal error message";

            var resultado = GitCommandRunner.FiltrarWarnings(stderr);

            resultado.Should().Be("real error message");
        }

        [Fact]
        public void Deve_retornar_mesmo_texto_quando_nao_tem_environment_variable()
        {
            var stderr = "error: something went wrong";

            var resultado = GitCommandRunner.FiltrarWarnings(stderr);

            resultado.Should().Be("error: something went wrong");
        }
    }
}
