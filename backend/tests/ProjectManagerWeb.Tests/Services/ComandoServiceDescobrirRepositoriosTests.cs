using ProjectManagerWeb.src.Services;

namespace ProjectManagerWeb.Tests.Services;

public class ComandoServiceDescobrirRepositoriosTests : IDisposable
{
    private readonly string _raiz;

    public ComandoServiceDescobrirRepositoriosTests()
    {
        _raiz = Path.Combine(Path.GetTempPath(), "pmw-descobrir-repositorios-" + Guid.NewGuid());
        Directory.CreateDirectory(_raiz);
    }

    public void Dispose()
    {
        if (Directory.Exists(_raiz))
            Directory.Delete(_raiz, true);
    }

    private static string CriarRepositorio(string caminho)
    {
        Directory.CreateDirectory(caminho);
        Directory.CreateDirectory(Path.Combine(caminho, ".git"));

        return caminho;
    }

    private static string CriarWorktree(string caminho)
    {
        Directory.CreateDirectory(caminho);
        File.WriteAllText(Path.Combine(caminho, ".git"), "gitdir: ../../.git/worktrees/repo");

        return caminho;
    }

    private static List<string> Normalizar(List<string> caminhos) =>
        caminhos.Select(Path.GetFullPath).ToList();

    public class DescobrirRepositorios : ComandoServiceDescobrirRepositoriosTests
    {
        [Fact]
        public void Deve_incluir_a_raiz_quando_ela_e_repositorio()
        {
            CriarRepositorio(_raiz);

            var repositorios = Normalizar(ComandoService.DescobrirRepositorios(_raiz));

            repositorios.Should().Equal(Path.GetFullPath(_raiz));
        }

        [Fact]
        public void Deve_incluir_subpasta_que_e_repositorio()
        {
            var repositorio = CriarRepositorio(Path.Combine(_raiz, "techcambio"));

            var repositorios = Normalizar(ComandoService.DescobrirRepositorios(_raiz));

            repositorios.Should().ContainSingle().Which.Should().Be(Path.GetFullPath(repositorio));
        }

        [Fact]
        public void Deve_incluir_repositorio_no_segundo_nivel()
        {
            var repositorio = CriarRepositorio(Path.Combine(_raiz, "nivel1", "repo"));

            var repositorios = Normalizar(ComandoService.DescobrirRepositorios(_raiz));

            repositorios.Should().ContainSingle().Which.Should().Be(Path.GetFullPath(repositorio));
        }

        [Fact]
        public void Deve_ignorar_repositorio_acima_da_profundidade()
        {
            CriarRepositorio(Path.Combine(_raiz, "n1", "n2", "n3"));

            var repositorios = ComandoService.DescobrirRepositorios(_raiz);

            repositorios.Should().BeEmpty();
        }

        [Fact]
        public void Deve_ignorar_node_modules()
        {
            CriarRepositorio(Path.Combine(_raiz, "node_modules", "lib"));

            var repositorios = ComandoService.DescobrirRepositorios(_raiz);

            repositorios.Should().BeEmpty();
        }

        [Theory]
        [InlineData("bin")]
        [InlineData("obj")]
        [InlineData("dist")]
        [InlineData("vendor")]
        [InlineData("venv")]
        public void Deve_ignorar_diretorios_de_dependencia(string nomeIgnorado)
        {
            CriarRepositorio(Path.Combine(_raiz, nomeIgnorado, "pacote"));

            var repositorios = ComandoService.DescobrirRepositorios(_raiz);

            repositorios.Should().BeEmpty();
        }

        [Fact]
        public void Deve_ignorar_diretorios_ocultos()
        {
            CriarRepositorio(Path.Combine(_raiz, ".cache", "repo"));

            var repositorios = ComandoService.DescobrirRepositorios(_raiz);

            repositorios.Should().BeEmpty();
        }

        [Fact]
        public void Deve_reconhecer_git_como_arquivo()
        {
            var worktree = CriarWorktree(Path.Combine(_raiz, "repo-worktree"));

            var repositorios = Normalizar(ComandoService.DescobrirRepositorios(_raiz));

            repositorios.Should().ContainSingle().Which.Should().Be(Path.GetFullPath(worktree));
        }

        [Fact]
        public void Deve_parar_de_descer_quando_o_diretorio_ja_e_repositorio()
        {
            var repositorio = CriarRepositorio(Path.Combine(_raiz, "techcambio"));
            CriarRepositorio(Path.Combine(repositorio, "frontend"));

            var repositorios = Normalizar(ComandoService.DescobrirRepositorios(_raiz));

            repositorios.Should().Equal(Path.GetFullPath(repositorio));
        }

        [Fact]
        public void Deve_retornar_vazio_quando_raiz_nao_existe()
        {
            var inexistente = Path.Combine(Path.GetTempPath(), "pmw-descobrir-repositorios-" + Guid.NewGuid());

            var repositorios = ComandoService.DescobrirRepositorios(inexistente);

            repositorios.Should().BeEmpty();
        }

        [Fact]
        public void Deve_respeitar_profundidade_informada()
        {
            var repositorio = CriarRepositorio(Path.Combine(_raiz, "n1", "n2", "repo"));

            var repositorios = Normalizar(ComandoService.DescobrirRepositorios(_raiz, 3));

            repositorios.Should().ContainSingle().Which.Should().Be(Path.GetFullPath(repositorio));
        }
    }
}
