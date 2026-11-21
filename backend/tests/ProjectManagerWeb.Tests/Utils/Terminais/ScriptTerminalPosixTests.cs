using System.Diagnostics;
using ProjectManagerWeb.src.Utils.Terminais;

namespace ProjectManagerWeb.Tests.Utils.Terminais;

public class ScriptTerminalPosixTests
{
    public class AdicionarComplemento
    {
        [Fact]
        public void Deve_adicionar_argumentos_quando_complemento_tem_aspas()
        {
            var psi = new ProcessStartInfo();

            ScriptTerminalPosix.AdicionarComplemento(psi, "-T \"Frontend\"");

            psi.ArgumentList.Should().Equal("-T", "Frontend");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Nao_deve_adicionar_argumento_quando_complemento_vazio(string? complemento)
        {
            var psi = new ProcessStartInfo();

            ScriptTerminalPosix.AdicionarComplemento(psi, complemento);

            psi.ArgumentList.Should().BeEmpty();
        }
    }

    public class AdicionarShell
    {
        [Fact]
        public void Deve_adicionar_shell_e_script_quando_sem_diretorio()
        {
            var psi = new ProcessStartInfo();

            ScriptTerminalPosix.AdicionarShell(psi, "/usr/bin/zsh", "npm i;", null);

            psi.ArgumentList.Should().Equal(
                "/usr/bin/zsh",
                "-l",
                "-i",
                "-c",
                "trap '' INT; (trap - INT; npm i); stty sane && exec /usr/bin/zsh");
        }

        [Fact]
        public void Deve_citar_diretorio_com_apostrofo_ao_reabrir_shell()
        {
            var psi = new ProcessStartInfo();

            ScriptTerminalPosix.AdicionarShell(psi, "/bin/bash", "ls", "/tmp/João's teste");

            psi.ArgumentList.Last().Should().Contain("cd '/tmp/João'\\''s teste' && stty sane && exec /bin/bash");
        }

        [Fact]
        public void Nao_deve_incluir_cd_quando_diretorio_vazio()
        {
            var psi = new ProcessStartInfo();

            ScriptTerminalPosix.AdicionarShell(psi, "/bin/bash", "ls", "   ");

            var script = psi.ArgumentList.Last();
            script.Should().NotContain("cd ");
            script.Should().Contain("stty sane && exec /bin/bash");
        }
    }
}
