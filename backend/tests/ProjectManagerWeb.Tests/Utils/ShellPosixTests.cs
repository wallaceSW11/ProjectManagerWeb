using ProjectManagerWeb.src.Utils;

namespace ProjectManagerWeb.Tests.Utils;

public class ShellPosixTests
{
    public class Citar
    {
        [Fact]
        public void Deve_envolver_em_aspas_simples_quando_sem_apostrofo()
        {
            var resultado = ShellPosix.Citar("/home/user/projeto");

            resultado.Should().Be("'/home/user/projeto'");
        }

        [Fact]
        public void Deve_escapar_apostrofo_quando_caminho_tem_apostrofo()
        {
            var resultado = ShellPosix.Citar("/tmp/João's teste");

            resultado.Should().Be("'/tmp/João'\\''s teste'");
        }

        [Fact]
        public void Deve_retornar_aspas_vazias_quando_valor_vazio()
        {
            var resultado = ShellPosix.Citar(string.Empty);

            resultado.Should().Be("''");
        }
    }

    public class SepararArgumentos
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Deve_retornar_lista_vazia_quando_linha_vazia(string? linha)
        {
            var resultado = ShellPosix.SepararArgumentos(linha);

            resultado.Should().BeEmpty();
        }

        [Fact]
        public void Deve_separar_argumentos_quando_aspas_duplas()
        {
            var resultado = ShellPosix.SepararArgumentos("-T \"Frontend\"");

            resultado.Should().Equal("-T", "Frontend");
        }

        [Fact]
        public void Deve_manter_espaco_interno_quando_aspas_simples_com_espaco()
        {
            var resultado = ShellPosix.SepararArgumentos("--title='Meu Título'");

            resultado.Should().Equal("--title=Meu Título");
        }

        [Fact]
        public void Deve_ignorar_espacos_multiplos_e_tabs_entre_argumentos()
        {
            var resultado = ShellPosix.SepararArgumentos("  -T\t  Frontend   --maximize ");

            resultado.Should().Equal("-T", "Frontend", "--maximize");
        }

        [Fact]
        public void Deve_retornar_token_quando_aspas_sem_fechamento()
        {
            var resultado = ShellPosix.SepararArgumentos("\"Frontend");

            resultado.Should().Equal("Frontend");
        }

        [Fact]
        public void Deve_retornar_token_vazio_quando_aspas_vazias_entre_argumentos()
        {
            var resultado = ShellPosix.SepararArgumentos("a \"\" b");

            resultado.Should().Equal("a", "", "b");
        }
    }

    public class EhSuportado
    {
        [Theory]
        [InlineData("/bin/sh")]
        [InlineData("/bin/bash")]
        public void Deve_retornar_verdadeiro_quando_shell_posix_existe(string caminho)
        {
            var resultado = ShellPosix.EhSuportado(caminho);

            resultado.Should().BeTrue();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("bash")]
        [InlineData("/usr/bin/fish")]
        [InlineData("/nao/existe/zsh")]
        public void Deve_retornar_falso_quando_shell_nao_suportado_ou_inexistente(string? caminho)
        {
            var resultado = ShellPosix.EhSuportado(caminho);

            resultado.Should().BeFalse();
        }
    }
}
