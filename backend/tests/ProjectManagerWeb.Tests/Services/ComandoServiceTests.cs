using System.Diagnostics;
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

    public class ExecutarComandoMenu : ComandoServiceTests
    {
        private readonly Guid _repositorioId = Guid.NewGuid();
        private readonly Guid _comandoId = Guid.NewGuid();
        private readonly string _destinoDir = Path.Combine(Path.GetTempPath(), "pmw-test-menu-" + Guid.NewGuid());

        private RepositorioRequestDTO CriarRepositorio(List<MenuDTO> menus) =>
            new(_repositorioId, "https://github.com/test/repo", "test-repo",
                "Test Repo", null, "main", new List<ProjetoDTO>(),
                null, menus, null);

        private MenuRequestDTO CriarMenu() =>
            new(_repositorioId, _comandoId, _destinoDir);

        [Fact]
        public async Task Deve_lancar_key_not_found_quando_repositorio_nao_encontrado()
        {
            _repositorioJson.GetByIdAsync(Arg.Any<Guid>()).Returns((RepositorioRequestDTO?)null);

            var act = () => _sut.ExecutarComandoMenu(CriarMenu());

            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Repositório não encontrado");
        }

        [Fact]
        public async Task Deve_lancar_key_not_found_quando_comando_nao_encontrado_no_repositorio()
        {
            var repo = CriarRepositorio(new List<MenuDTO>());
            _repositorioJson.GetByIdAsync(_repositorioId).Returns(repo);

            var act = () => _sut.ExecutarComandoMenu(CriarMenu());

            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Comando não encontrado");
        }

        [Fact]
        public async Task Deve_executar_comandos_via_shell_quando_menu_possui_comandos()
        {
            var comandos = new List<string> { "echo hello" };
            var menuDto = new MenuDTO(_comandoId, "Test", "comando", null, null, comandos);
            var repo = CriarRepositorio(new List<MenuDTO> { menuDto });
            _repositorioJson.GetByIdAsync(_repositorioId).Returns(repo);

            var resultado = await _sut.ExecutarComandoMenu(CriarMenu());

            resultado.Should().BeTrue();
            _shell.Received(1).ExecutarComando(
                Arg.Is<string>(c => c.Contains("echo hello")),
                Arg.Any<string?>(),
                Arg.Any<string?>());
        }

        [Fact]
        public async Task Deve_passar_github_token_para_comandos_do_shell_quando_repositorio_possui_token()
        {
            var token = "ghp_token123";
            var comandos = new List<string> { "git pull" };
            var menuDto = new MenuDTO(_comandoId, "Test", "comando", null, null, comandos);
            var repo = CriarRepositorio(new List<MenuDTO> { menuDto }) with { GitHubToken = token };
            _repositorioJson.GetByIdAsync(_repositorioId).Returns(repo);

            var resultado = await _sut.ExecutarComandoMenu(CriarMenu());

            resultado.Should().BeTrue();
            _shell.Received(1).ExecutarComando(
                Arg.Is<string>(c => c.Contains("git pull")),
                Arg.Any<string?>(),
                Arg.Is<string?>(t => t == token));
        }

        [Fact]
        public async Task Deve_copiar_arquivo_para_destino_quando_ignorar_git_true_e_arquivo_origem_existe()
        {
            var dirTeste = Path.Combine(Path.GetTempPath(), "pmw-test-menu-src-" + Guid.NewGuid());
            Directory.CreateDirectory(dirTeste);
            var arquivoOrigem = Path.Combine(dirTeste, "web.config");
            File.WriteAllText(arquivoOrigem, "<configuration></configuration>");

            try
            {
                var arquivos = new List<ArquivosDTO>
                {
                    new(arquivoOrigem, "subpasta", true)
                };
                var menuDto = new MenuDTO(_comandoId, "Test", "arquivo", arquivos, null, null);
                var repo = CriarRepositorio(new List<MenuDTO> { menuDto });
                _repositorioJson.GetByIdAsync(_repositorioId).Returns(repo);

                var resultado = await _sut.ExecutarComandoMenu(CriarMenu());

                resultado.Should().BeTrue();

                var destinoEsperado = Path.Combine(_destinoDir, "subpasta", "web.config");
                File.Exists(destinoEsperado).Should().BeTrue();
                var conteudoDestino = await File.ReadAllTextAsync(destinoEsperado);
                conteudoDestino.Should().Be("<configuration></configuration>");
            }
            finally
            {
                if (Directory.Exists(dirTeste))
                    Directory.Delete(dirTeste, true);
            }
        }

        [Fact]
        public async Task Deve_copiar_arquivo_para_destino_quando_ignorar_git_false_e_arquivo_origem_existe()
        {
            var dirTeste = Path.Combine(Path.GetTempPath(), "pmw-test-menu-src-" + Guid.NewGuid());
            Directory.CreateDirectory(dirTeste);
            var arquivoOrigem = Path.Combine(dirTeste, "appsettings.json");
            File.WriteAllText(arquivoOrigem, "{}");

            try
            {
                var arquivos = new List<ArquivosDTO>
                {
                    new(arquivoOrigem, "config", false)
                };
                var menuDto = new MenuDTO(_comandoId, "Test", "arquivo", arquivos, null, null);
                var repo = CriarRepositorio(new List<MenuDTO> { menuDto });
                _repositorioJson.GetByIdAsync(_repositorioId).Returns(repo);

                var resultado = await _sut.ExecutarComandoMenu(CriarMenu());

                resultado.Should().BeTrue();

                var destinoEsperado = Path.Combine(_destinoDir, "config", "appsettings.json");
                File.Exists(destinoEsperado).Should().BeTrue();
            }
            finally
            {
                if (Directory.Exists(dirTeste))
                    Directory.Delete(dirTeste, true);
            }
        }

        [Fact]
        public async Task Deve_usar_destino_como_nome_completo_quando_destino_tem_extensao()
        {
            var dirTeste = Path.Combine(Path.GetTempPath(), "pmw-test-menu-src-" + Guid.NewGuid());
            Directory.CreateDirectory(dirTeste);
            var arquivoOrigem = Path.Combine(dirTeste, "original.txt");
            File.WriteAllText(arquivoOrigem, "conteudo");

            try
            {
                var arquivos = new List<ArquivosDTO>
                {
                    new(arquivoOrigem, "renomeado.txt", false)
                };
                var menuDto = new MenuDTO(_comandoId, "Test", "arquivo", arquivos, null, null);
                var repo = CriarRepositorio(new List<MenuDTO> { menuDto });
                _repositorioJson.GetByIdAsync(_repositorioId).Returns(repo);

                var resultado = await _sut.ExecutarComandoMenu(CriarMenu());

                resultado.Should().BeTrue();

                var destinoEsperado = Path.Combine(_destinoDir, "renomeado.txt");
                File.Exists(destinoEsperado).Should().BeTrue();
            }
            finally
            {
                if (Directory.Exists(dirTeste))
                    Directory.Delete(dirTeste, true);
            }
        }

        [Fact]
        public async Task Nao_deve_lancar_excecao_quando_arquivo_origem_inexistente()
        {
            var arquivos = new List<ArquivosDTO>
            {
                new("/caminho/inexistente/arquivo.exe", "sub", false)
            };
            var menuDto = new MenuDTO(_comandoId, "Test", "arquivo", arquivos, null, null);
            var repo = CriarRepositorio(new List<MenuDTO> { menuDto });
            _repositorioJson.GetByIdAsync(_repositorioId).Returns(repo);

            var resultado = await _sut.ExecutarComandoMenu(CriarMenu());

            resultado.Should().BeTrue();
        }

        [Fact]
        public async Task Deve_criar_diretorio_destino_quando_nao_existe()
        {
            var dirTeste = Path.Combine(Path.GetTempPath(), "pmw-test-menu-src-" + Guid.NewGuid());
            Directory.CreateDirectory(dirTeste);
            var arquivoOrigem = Path.Combine(dirTeste, "index.html");
            File.WriteAllText(arquivoOrigem, "<html></html>");

            var destinoSubdir = "nova_pasta_criada";

            try
            {
                var arquivos = new List<ArquivosDTO>
                {
                    new(arquivoOrigem, destinoSubdir, false)
                };
                var menuDto = new MenuDTO(_comandoId, "Test", "arquivo", arquivos, null, null);
                var repo = CriarRepositorio(new List<MenuDTO> { menuDto });
                _repositorioJson.GetByIdAsync(_repositorioId).Returns(repo);

                var resultado = await _sut.ExecutarComandoMenu(CriarMenu());

                resultado.Should().BeTrue();

                var destinoEsperado = Path.Combine(_destinoDir, destinoSubdir, "index.html");
                File.Exists(destinoEsperado).Should().BeTrue();
            }
            finally
            {
                if (Directory.Exists(dirTeste))
                    Directory.Delete(dirTeste, true);
            }
        }

        [Fact]
        public async Task Deve_copiar_pasta_recursivamente_quando_origem_existe()
        {
            var dirOrigem = Path.Combine(Path.GetTempPath(), "pmw-test-pasta-src-" + Guid.NewGuid());
            var subDir = Path.Combine(dirOrigem, "sub");
            Directory.CreateDirectory(subDir);
            File.WriteAllText(Path.Combine(dirOrigem, "root.txt"), "root");
            File.WriteAllText(Path.Combine(subDir, "sub.txt"), "sub");

            try
            {
                var pastas = new List<PastaDTO>
                {
                    new(dirOrigem, "destino_pasta")
                };
                var menuDto = new MenuDTO(_comandoId, "Test", "pasta", null, pastas, null);
                var repo = CriarRepositorio(new List<MenuDTO> { menuDto });
                _repositorioJson.GetByIdAsync(_repositorioId).Returns(repo);

                var resultado = await _sut.ExecutarComandoMenu(CriarMenu());

                resultado.Should().BeTrue();

                var dirDestino = Path.Combine(_destinoDir, "destino_pasta");
                Directory.Exists(dirDestino).Should().BeTrue();
                File.Exists(Path.Combine(dirDestino, "root.txt")).Should().BeTrue();
                File.Exists(Path.Combine(dirDestino, "sub", "sub.txt")).Should().BeTrue();
            }
            finally
            {
                if (Directory.Exists(dirOrigem))
                    Directory.Delete(dirOrigem, true);
            }
        }

        [Fact]
        public async Task Nao_deve_lancar_excecao_quando_pasta_origem_inexistente()
        {
            var pastas = new List<PastaDTO>
            {
                new("/caminho/inexistente/pasta", "destino")
            };
            var menuDto = new MenuDTO(_comandoId, "Test", "pasta", null, pastas, null);
            var repo = CriarRepositorio(new List<MenuDTO> { menuDto });
            _repositorioJson.GetByIdAsync(_repositorioId).Returns(repo);

            var resultado = await _sut.ExecutarComandoMenu(CriarMenu());

            resultado.Should().BeTrue();
        }

        [Fact]
        public async Task Deve_executar_todos_os_tipos_quando_menu_completo()
        {
            var dirSrc = Path.Combine(Path.GetTempPath(), "pmw-test-full-src-" + Guid.NewGuid());
            Directory.CreateDirectory(dirSrc);
            File.WriteAllText(Path.Combine(dirSrc, "file.txt"), "conteudo");

            var dirPasta = Path.Combine(Path.GetTempPath(), "pmw-test-full-pasta-" + Guid.NewGuid());
            Directory.CreateDirectory(dirPasta);
            File.WriteAllText(Path.Combine(dirPasta, "pasta_file.txt"), "pasta");

            try
            {
                var arquivos = new List<ArquivosDTO>
                {
                    new(Path.Combine(dirSrc, "file.txt"), "arquivos", false)
                };
                var pastas = new List<PastaDTO>
                {
                    new(dirPasta, "pastas_copiadas")
                };
                var comandos = new List<string> { "echo fim" };
                var menuDto = new MenuDTO(_comandoId, "Completo", "completo", arquivos, pastas, comandos);
                var repo = CriarRepositorio(new List<MenuDTO> { menuDto });
                _repositorioJson.GetByIdAsync(_repositorioId).Returns(repo);

                var resultado = await _sut.ExecutarComandoMenu(CriarMenu());

                resultado.Should().BeTrue();

                File.Exists(Path.Combine(_destinoDir, "arquivos", "file.txt")).Should().BeTrue();
                Directory.Exists(Path.Combine(_destinoDir, "pastas_copiadas")).Should().BeTrue();
                File.Exists(Path.Combine(_destinoDir, "pastas_copiadas", "pasta_file.txt")).Should().BeTrue();
                _shell.Received(1).ExecutarComando(
                    Arg.Is<string>(c => c.Contains("echo fim")),
                    Arg.Any<string?>(),
                    Arg.Any<string?>());
            }
            finally
            {
                if (Directory.Exists(dirSrc))
                    Directory.Delete(dirSrc, true);
                if (Directory.Exists(dirPasta))
                    Directory.Delete(dirPasta, true);
            }
        }

        [Fact]
        public async Task Deve_ignorar_proximos_arquivos_quando_um_falha_mas_nao_interromper()
        {
            var dirSrc = Path.Combine(Path.GetTempPath(), "pmw-test-continuar-src-" + Guid.NewGuid());
            Directory.CreateDirectory(dirSrc);
            var arquivoValido = Path.Combine(dirSrc, "valido.txt");
            File.WriteAllText(arquivoValido, "ok");

            try
            {
                var arquivos = new List<ArquivosDTO>
                {
                    new("/caminho/inexistente/falha.exe", "sub", false),
                    new(arquivoValido, "sub", false)
                };
                var menuDto = new MenuDTO(_comandoId, "Test", "arquivo", arquivos, null, null);
                var repo = CriarRepositorio(new List<MenuDTO> { menuDto });
                _repositorioJson.GetByIdAsync(_repositorioId).Returns(repo);

                var resultado = await _sut.ExecutarComandoMenu(CriarMenu());

                resultado.Should().BeTrue();

                var destinoEsperado = Path.Combine(_destinoDir, "sub", "valido.txt");
                File.Exists(destinoEsperado).Should().BeTrue();
            }
            finally
            {
                if (Directory.Exists(dirSrc))
                    Directory.Delete(dirSrc, true);
            }
        }
    }

    public class RemoverSkipWorktreeDaPasta : ComandoServiceTests
    {
        [Fact]
        public void Nao_deve_lancar_excecao_quando_diretorio_inexistente()
        {
            var diretorio = Path.Combine(Path.GetTempPath(), "pmw-skip-inexistente-" + Guid.NewGuid());

            var resultado = ComandoService.RemoverSkipWorktreeDaPasta(
                new ReverterSkipWorktreeRequestDTO(diretorio, "repo"));

            resultado.Should().ContainSingle();
            resultado[0].Should().StartWith("Diretório não é um repositório git");
        }

        [Fact]
        public void Deve_reverter_skip_worktree_no_subdiretorio_quando_informado()
        {
            var raiz = Path.Combine(Path.GetTempPath(), "pmw-skip-subdir-" + Guid.NewGuid());
            var repositorio = Path.Combine(raiz, "meu-repo", "sub");
            CriarRepositorioComSkipWorktree(repositorio);

            try
            {
                var resultado = ComandoService.RemoverSkipWorktreeDaPasta(
                    new ReverterSkipWorktreeRequestDTO(raiz, "meu-repo", "sub"));

                ObterFlagGit(repositorio, "arquivo.txt").Should().NotBe('S');
                resultado.Should().Contain(r => r.Contains("OK"));
            }
            finally
            {
                if (Directory.Exists(raiz))
                    Directory.Delete(raiz, true);
            }
        }

        [Fact]
        public void Deve_reverter_skip_worktree_no_clone_principal_quando_sem_subdiretorio_e_sem_workspace()
        {
            var raiz = Path.Combine(Path.GetTempPath(), "pmw-skip-clone-" + Guid.NewGuid());
            var repositorio = Path.Combine(raiz, "meu-repo");
            CriarRepositorioComSkipWorktree(repositorio);

            try
            {
                var resultado = ComandoService.RemoverSkipWorktreeDaPasta(
                    new ReverterSkipWorktreeRequestDTO(raiz, "meu-repo"));

                ObterFlagGit(repositorio, "arquivo.txt").Should().NotBe('S');
                resultado.Should().Contain(r => r.Contains("OK"));
            }
            finally
            {
                if (Directory.Exists(raiz))
                    Directory.Delete(raiz, true);
            }
        }

        [Fact]
        public void Deve_reverter_apenas_pasta_com_git_quando_workspace()
        {
            var raiz = Path.Combine(Path.GetTempPath(), "pmw-skip-workspace-" + Guid.NewGuid());
            var comGit = Path.Combine(raiz, "com-git");
            var semGit = Path.Combine(raiz, "sem-git");
            CriarRepositorioComSkipWorktree(comGit);
            Directory.CreateDirectory(semGit);

            var workspace = Path.Combine(raiz, "projeto.code-workspace");
            File.WriteAllText(workspace, """
                {
                  "folders": [
                    { "path": "com-git" },
                    { "path": "sem-git" }
                  ]
                }
                """);

            try
            {
                var resultado = ComandoService.RemoverSkipWorktreeDaPasta(
                    new ReverterSkipWorktreeRequestDTO(raiz, "repo-inexistente"));

                ObterFlagGit(comGit, "arquivo.txt").Should().NotBe('S');
                resultado.Should().ContainSingle(r => r.Contains("OK"));
                resultado.Should().NotContain(r => r.Contains("não é um repositório git"));
            }
            finally
            {
                if (Directory.Exists(raiz))
                    Directory.Delete(raiz, true);
            }
        }

        [Fact]
        public void Nao_deve_lancar_excecao_quando_workspace_sem_pasta_git()
        {
            var raiz = Path.Combine(Path.GetTempPath(), "pmw-skip-workspace-vazio-" + Guid.NewGuid());
            Directory.CreateDirectory(Path.Combine(raiz, "sem-git"));

            var workspace = Path.Combine(raiz, "projeto.code-workspace");
            File.WriteAllText(workspace, """
                {
                  "folders": [
                    { "path": "sem-git" }
                  ]
                }
                """);

            try
            {
                var resultado = ComandoService.RemoverSkipWorktreeDaPasta(
                    new ReverterSkipWorktreeRequestDTO(raiz, "repo-inexistente"));

                resultado.Should().ContainSingle();
                resultado[0].Should().StartWith("Diretório não é um repositório git");
            }
            finally
            {
                if (Directory.Exists(raiz))
                    Directory.Delete(raiz, true);
            }
        }

        private static void CriarRepositorioComSkipWorktree(string caminho)
        {
            Directory.CreateDirectory(caminho);
            ExecutarGit(caminho, "init");
            ExecutarGit(caminho, "config user.email teste@pmw.local");
            ExecutarGit(caminho, "config user.name PMW Teste");
            File.WriteAllText(Path.Combine(caminho, "arquivo.txt"), "conteudo");
            ExecutarGit(caminho, "add arquivo.txt");
            ExecutarGit(caminho, "update-index --skip-worktree arquivo.txt");
        }

        private static void ExecutarGit(string diretorio, string argumentos)
        {
            var inicioProcesso = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"-C \"{diretorio}\" {argumentos}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var processo = Process.Start(inicioProcesso)!;
            processo.StandardOutput.ReadToEnd();
            processo.StandardError.ReadToEnd();
            processo.WaitForExit();
        }

        private static char ObterFlagGit(string repositorio, string arquivo)
        {
            var inicioProcesso = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"-C \"{repositorio}\" ls-files -v",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var processo = Process.Start(inicioProcesso)!;
            var saida = processo.StandardOutput.ReadToEnd();
            processo.StandardError.ReadToEnd();
            processo.WaitForExit();

            return saida
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .First(linha => linha.EndsWith(arquivo, StringComparison.Ordinal))[0];
        }
    }
}
