using Microsoft.Extensions.Logging;
using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Services;
using ProjectManagerWeb.src.Utils;

namespace ProjectManagerWeb.Tests.Services;

public class MigrationServiceTests : IDisposable
{
    private readonly string _tempPath;
    private readonly RepositorioJsonService _repositorioService;
    private readonly MigrationService _sut;

    public MigrationServiceTests()
    {
        PathHelper.Configure("Production");

        _tempPath = Path.Combine(Path.GetTempPath(), "pmw-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(_tempPath);

        var repoFile = Path.Combine(_tempPath, "repositorios.json");
        var ideFile = Path.Combine(_tempPath, "ides.json");
        var configFile = Path.Combine(_tempPath, "configuracao.json");
        var pastaFile = Path.Combine(_tempPath, "pastas.json");

        File.WriteAllText(repoFile, "[]");
        File.WriteAllText(ideFile, "[]");
        File.WriteAllText(configFile, "{}");
        File.WriteAllText(pastaFile, "[]");

        _repositorioService = new RepositorioJsonService(repoFile);
        var ideService = new IDEJsonService(ideFile);
        var configuracaoService = new ConfiguracaoService(configFile);
        var pastaService = new PastaJsonService(pastaFile);
        var logger = Substitute.For<ILogger<MigrationService>>();

        _sut = new MigrationService(ideService, _repositorioService, configuracaoService, pastaService, logger);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempPath))
            Directory.Delete(_tempPath, true);
    }

    public class Migration_006_AdicionarIndiceProjetosMenusPerfis : MigrationServiceTests
    {
        [Fact]
        public async Task Migration_006_adiciona_indices_a_projetos()
        {
            var repo = CriarRepositorioComProjetos(["Projeto A", "Projeto B", "Projeto C"]);
            await _repositorioService.AddAsync(repo);

            await _sut.Migration_006_AdicionarIndiceProjetosMenusPerfis();

            var repositorios = await _repositorioService.GetAllAsync();
            var projetos = repositorios[0].Projetos;
            projetos[0].Indice.Should().Be(0);
            projetos[1].Indice.Should().Be(1);
            projetos[2].Indice.Should().Be(2);
        }

        [Fact]
        public async Task Migration_006_adiciona_indices_a_menus()
        {
            var repo = CriarRepositorioComMenus(["Menu 1", "Menu 2"]);
            await _repositorioService.AddAsync(repo);

            await _sut.Migration_006_AdicionarIndiceProjetosMenusPerfis();

            var repositorios = await _repositorioService.GetAllAsync();
            var menus = repositorios[0].Menus!;
            menus[0].Indice.Should().Be(0);
            menus[1].Indice.Should().Be(1);
        }

        [Fact]
        public async Task Migration_006_adiciona_indices_a_perfis()
        {
            var perfis = new List<PerfilMarcacaoDTO>
            {
                new(Guid.NewGuid(), "Perfil 1", []),
                new(Guid.NewGuid(), "Perfil 2", [])
            };
            var repo = CriarRepositorioComPerfis(perfis);
            await _repositorioService.AddAsync(repo);

            await _sut.Migration_006_AdicionarIndiceProjetosMenusPerfis();

            var repositorios = await _repositorioService.GetAllAsync();
            var perfisSalvos = repositorios[0].Perfis!;
            perfisSalvos[0].Indice.Should().Be(0);
            perfisSalvos[1].Indice.Should().Be(1);
        }

        [Fact]
        public async Task Migration_006_lida_com_menus_e_perfis_nulos()
        {
            var repo = new RepositorioRequestDTO(
                Guid.NewGuid(), "https://teste.com/repo.git", "Teste", "Teste",
                null, "main",
                [new ProjetoDTO(Guid.NewGuid(), "Unico", null, null, new ComandoDTO(null, null, null, null), null)],
                null, null, null
            );
            await _repositorioService.AddAsync(repo);

            await _sut.Migration_006_AdicionarIndiceProjetosMenusPerfis();

            var repositorios = await _repositorioService.GetAllAsync();
            repositorios.Should().HaveCount(1);
            repositorios[0].Projetos[0].Indice.Should().Be(0);
        }
    }

    private static RepositorioRequestDTO CriarRepositorioComProjetos(string[] nomes)
    {
        var projetos = nomes.Select(n => new ProjetoDTO(
            Guid.NewGuid(), n, null, null, new ComandoDTO(null, null, null, null), null
        )).ToList();

        return new RepositorioRequestDTO(
            Guid.NewGuid(), $"https://teste.com/{Guid.NewGuid()}.git", "Repo Teste", "Repo Teste",
            null, "main", projetos, null, null, null
        );
    }

    private static RepositorioRequestDTO CriarRepositorioComMenus(string[] nomes)
    {
        var menus = nomes.Select(n => new MenuDTO(
            Guid.NewGuid(), n, "comando", null, null, null
        )).ToList();

        return new RepositorioRequestDTO(
            Guid.NewGuid(), $"https://teste.com/{Guid.NewGuid()}.git", "Repo Teste", "Repo Teste",
            null, "main",
            [new ProjetoDTO(Guid.NewGuid(), "Projeto", null, null, new ComandoDTO(null, null, null, null), null)],
            null, menus, null
        );
    }

    private static RepositorioRequestDTO CriarRepositorioComPerfis(List<PerfilMarcacaoDTO> perfis)
    {
        return new RepositorioRequestDTO(
            Guid.NewGuid(), $"https://teste.com/{Guid.NewGuid()}.git", "Repo Teste", "Repo Teste",
            null, "main",
            [new ProjetoDTO(Guid.NewGuid(), "Projeto", null, null, new ComandoDTO(null, null, null, null), null)],
            null, null, null,
            Perfis: perfis
        );
    }
}
