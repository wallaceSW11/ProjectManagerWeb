using System.Text.Json;
using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Services;

namespace ProjectManagerWeb.Tests.Services;

public class ConfiguracaoServiceTests : IDisposable
{
    private readonly string _tempPath;
    private readonly string _tempFile;
    private readonly ConfiguracaoService _sut;

    public ConfiguracaoServiceTests()
    {
        _tempPath = Path.Combine(Path.GetTempPath(), "pmw-tests-" + Guid.NewGuid());
        _tempFile = Path.Combine(_tempPath, "Configuracao.json");
        Directory.CreateDirectory(_tempPath);
        _sut = new ConfiguracaoService(_tempFile);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempPath))
            Directory.Delete(_tempPath, true);
    }

    public class ObterConfiguracaoAsync : ConfiguracaoServiceTests
    {
        [Fact]
        public async Task Deve_retornar_configuracao_vazia_quando_arquivo_nao_existe()
        {
            var config = await _sut.ObterConfiguracaoAsync();

            config.Should().NotBeNull();
            config.DiretorioRaiz.Should().BeNull();
        }

        [Fact]
        public async Task Deve_retornar_configuracao_vazia_quando_arquivo_esta_vazio()
        {
            await File.WriteAllTextAsync(_tempFile, string.Empty);

            var config = await _sut.ObterConfiguracaoAsync();

            config.Should().NotBeNull();
            config.DiretorioRaiz.Should().BeNull();
        }

        [Fact]
        public async Task Deve_retornar_dados_salvos_quando_arquivo_existe_com_json_valido()
        {
            var salva = new ConfiguracaoRequestDTO { DiretorioRaiz = "/projetos" };
            await SalvarDireto(salva);

            var config = await _sut.ObterConfiguracaoAsync();

            config.DiretorioRaiz.Should().Be("/projetos");
        }
    }

    public class SalvarConfiguracaoAsync : ConfiguracaoServiceTests
    {
        [Fact]
        public async Task Deve_salvar_e_ler_mesma_configuracao()
        {
            var original = new ConfiguracaoRequestDTO
            {
                DiretorioRaiz = "/meus-projetos",
                TerminalLinux = "ghostty"
            };

            await _sut.SalvarConfiguracaoAsync(original);
            var lida = await _sut.ObterConfiguracaoAsync();

            lida.DiretorioRaiz.Should().Be("/meus-projetos");
            lida.TerminalLinux.Should().Be("ghostty");
        }
    }

    public class RenomearPerfilAsync : ConfiguracaoServiceTests
    {
        [Fact]
        public async Task Deve_retornar_true_quando_perfil_existe_e_renomear()
        {
            await SalvarDireto(new ConfiguracaoRequestDTO
            {
                PerfisVSCode =
                [
                    new PerfilVSCodeRequestDTO("web"),
                    new PerfilVSCodeRequestDTO("mobile")
                ]
            });

            var resultado = await _sut.RenomearPerfilAsync("web", "frontend");

            resultado.Should().BeTrue();
            var config = await _sut.ObterConfiguracaoAsync();
            config.PerfisVSCode.Should().Contain(p => p.Nome == "frontend");
            config.PerfisVSCode.Should().NotContain(p => p.Nome == "web");
        }

        [Fact]
        public async Task Deve_retornar_false_quando_perfil_nao_existe()
        {
            await SalvarDireto(new ConfiguracaoRequestDTO
            {
                PerfisVSCode = [new PerfilVSCodeRequestDTO("web")]
            });

            var resultado = await _sut.RenomearPerfilAsync("inexistente", "novo");

            resultado.Should().BeFalse();
        }
    }

    public class ObterDiretoriosOcultosAsync : ConfiguracaoServiceTests
    {
        [Fact]
        public async Task Deve_retornar_lista_vazia_quando_nao_tem_ocultos()
        {
            await SalvarDireto(new ConfiguracaoRequestDTO());

            var ocultos = await _sut.ObterDiretoriosOcultosAsync();

            ocultos.Should().BeEmpty();
        }

        [Fact]
        public async Task Deve_retornar_lista_quando_tem_ocultos()
        {
            await SalvarDireto(new ConfiguracaoRequestDTO
            {
                DiretoriosOcultos = ["/tmp/antigo", "/tmp/legado"]
            });

            var ocultos = await _sut.ObterDiretoriosOcultosAsync();

            ocultos.Should().Contain("/tmp/antigo");
            ocultos.Should().Contain("/tmp/legado");
        }
    }

    public class OcultarDiretorioAsync : ConfiguracaoServiceTests
    {
        [Fact]
        public async Task Deve_adicionar_diretorio_na_lista_de_ocultos()
        {
            await _sut.OcultarDiretorioAsync("/tmp/esconder");

            var config = await _sut.ObterConfiguracaoAsync();
            config.DiretoriosOcultos.Should().Contain("/tmp/esconder");
        }

        [Fact]
        public async Task Nao_deve_duplicar_quando_diretorio_ja_esta_oculto()
        {
            await _sut.OcultarDiretorioAsync("/tmp/esconder");
            await _sut.OcultarDiretorioAsync("/tmp/esconder");

            var config = await _sut.ObterConfiguracaoAsync();
            config.DiretoriosOcultos.Should().HaveCount(1);
        }
    }

    public class RestaurarDiretorioAsync : ConfiguracaoServiceTests
    {
        [Fact]
        public async Task Deve_remover_diretorio_da_lista_de_ocultos()
        {
            await SalvarDireto(new ConfiguracaoRequestDTO
            {
                DiretoriosOcultos = ["/tmp/a", "/tmp/b"]
            });

            await _sut.RestaurarDiretorioAsync("/tmp/a");

            var config = await _sut.ObterConfiguracaoAsync();
            config.DiretoriosOcultos.Should().NotContain("/tmp/a");
            config.DiretoriosOcultos.Should().Contain("/tmp/b");
        }

        [Fact]
        public async Task Nao_deve_lancar_erro_quando_diretorio_nao_esta_oculto()
        {
            await _sut.RestaurarDiretorioAsync("/tmp/inexistente");

            var config = await _sut.ObterConfiguracaoAsync();
            config.DiretoriosOcultos.Should().BeEmpty();
        }
    }

    public class AdicionarPastaCentralizadoraAsync : ConfiguracaoServiceTests
    {
        [Fact]
        public async Task Deve_adicionar_nova_pasta_centralizadora()
        {
            await _sut.AdicionarPastaCentralizadoraAsync("Projetos");

            var config = await _sut.ObterConfiguracaoAsync();
            config.PastasCentralizadoras.Should().Contain(p => p.Nome == "Projetos");
        }

        [Fact]
        public async Task Deve_lancar_excecao_quando_nome_ja_existe()
        {
            await _sut.AdicionarPastaCentralizadoraAsync("Projetos");

            var act = () => _sut.AdicionarPastaCentralizadoraAsync("Projetos");

            await act.Should().ThrowAsync<Exception>().WithMessage("*já existe*");
        }
    }

    public class RenomearPastaCentralizadoraAsync : ConfiguracaoServiceTests
    {
        [Fact]
        public async Task Deve_renomear_pasta_existente()
        {
            await SalvarDireto(new ConfiguracaoRequestDTO
            {
                PastasCentralizadoras = [new PastaCentralizadoraDTO("Dev"), new PastaCentralizadoraDTO("Infra")]
            });

            await _sut.RenomearPastaCentralizadoraAsync("Dev", "Desenvolvimento");

            var config = await _sut.ObterConfiguracaoAsync();
            config.PastasCentralizadoras.Should().Contain(p => p.Nome == "Desenvolvimento");
            config.PastasCentralizadoras.Should().NotContain(p => p.Nome == "Dev");
            config.PastasCentralizadoras.Should().Contain(p => p.Nome == "Infra");
        }
    }

    public class RemoverPastaCentralizadoraAsync : ConfiguracaoServiceTests
    {
        [Fact]
        public async Task Deve_remover_pasta_existente()
        {
            await SalvarDireto(new ConfiguracaoRequestDTO
            {
                PastasCentralizadoras = [new PastaCentralizadoraDTO("Dev")]
            });

            await _sut.RemoverPastaCentralizadoraAsync("Dev");

            var config = await _sut.ObterConfiguracaoAsync();
            config.PastasCentralizadoras.Should().BeEmpty();
        }

        [Fact]
        public async Task Deve_lancar_excecao_quando_pasta_nao_encontrada()
        {
            var act = () => _sut.RemoverPastaCentralizadoraAsync("Inexistente");

            await act.Should().ThrowAsync<Exception>().WithMessage("*não encontrada*");
        }
    }

    private async Task SalvarDireto(ConfiguracaoRequestDTO config)
    {
        var json = JsonSerializer.Serialize(config);
        await File.WriteAllTextAsync(_tempFile, json);
    }
}
