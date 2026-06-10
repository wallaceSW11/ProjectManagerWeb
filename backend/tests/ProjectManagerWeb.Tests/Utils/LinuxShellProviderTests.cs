using ProjectManagerWeb.src.Utils;

namespace ProjectManagerWeb.Tests.Utils;

public class LinuxShellProviderTests
{
    public class ExtrairDiretorioDoComando
    {
        [Fact]
        public void Deve_extrair_diretorio_quando_comando_inicia_com_cd()
        {
            var dir = LinuxShellProvider.ExtrairDiretorioDoComando("cd \"/home/user/projeto\"; npm start;");

            dir.Should().Be("/home/user/projeto");
        }

        [Fact]
        public void Deve_extrair_diretorio_quando_tem_export_antes()
        {
            var dir = LinuxShellProvider.ExtrairDiretorioDoComando("export PATH=\"$HOME/bin\"; cd \"/app/backend\"; dotnet run;");

            dir.Should().Be("/app/backend");
        }

        [Fact]
        public void Deve_retornar_null_quando_nao_tem_cd()
        {
            var dir = LinuxShellProvider.ExtrairDiretorioDoComando("npm start;");

            dir.Should().BeNull();
        }

        [Fact]
        public void Deve_extrair_apenas_primeiro_cd_quando_tem_varios()
        {
            var dir = LinuxShellProvider.ExtrairDiretorioDoComando("cd \"/primeiro\"; cd \"/segundo\";");

            dir.Should().Be("/primeiro");
        }

        [Fact]
        public void Deve_retornar_null_quando_cd_sem_aspas()
        {
            var dir = LinuxShellProvider.ExtrairDiretorioDoComando("cd /tmp; echo ok;");

            dir.Should().BeNull();
        }
    }

    public class RemoverMarcadorExit
    {
        [Fact]
        public void Deve_remover_exit_do_final()
        {
            var resultado = LinuxShellProvider.RemoverMarcadorExit("cd \"/tmp\"; code .; Exit;");

            resultado.Should().Be("cd \"/tmp\"; code .;");
        }

        [Fact]
        public void Deve_remover_exit_case_insensitive()
        {
            var resultado = LinuxShellProvider.RemoverMarcadorExit("comando; EXIT;");

            resultado.Should().Be("comando;");
        }

        [Fact]
        public void Deve_retornar_mesmo_quando_nao_tem_exit()
        {
            var resultado = LinuxShellProvider.RemoverMarcadorExit("cd \"/tmp\"; npm start;");

            resultado.Should().Be("cd \"/tmp\"; npm start;");
        }

        [Fact]
        public void Deve_trim_trailing_spaces_antes_de_remover()
        {
            var resultado = LinuxShellProvider.RemoverMarcadorExit("comando; Exit;   ");

            resultado.Should().Be("comando;");
        }

        [Fact]
        public void Deve_retornar_vazio_quando_so_tem_exit()
        {
            var resultado = LinuxShellProvider.RemoverMarcadorExit("Exit;");

            resultado.Should().BeEmpty();
        }

        [Fact]
        public void Nao_deve_remover_quando_exit_no_meio()
        {
            var resultado = LinuxShellProvider.RemoverMarcadorExit("Exit; cd /tmp;");

            resultado.Should().Be("Exit; cd /tmp;");
        }
    }
}
