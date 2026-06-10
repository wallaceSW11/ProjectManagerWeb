using ProjectManagerWeb.src.Utils.Terminais;

namespace ProjectManagerWeb.Tests.Utils;

public class TerminalEmulatorFactoryTests
{
    public class Criar
    {
        [Fact]
        public void Deve_retornar_ptyxis_quando_terminal_null()
        {
            var terminal = TerminalEmulatorFactory.Criar(null);

            terminal.Should().BeOfType<PtyxisTerminal>();
        }

        [Fact]
        public void Deve_retornar_ptyxis_quando_terminal_vazio()
        {
            var terminal = TerminalEmulatorFactory.Criar(string.Empty);

            terminal.Should().BeOfType<PtyxisTerminal>();
        }

        [Fact]
        public void Deve_retornar_ptyxis_quando_valor_desconhecido()
        {
            var terminal = TerminalEmulatorFactory.Criar("konsole");

            terminal.Should().BeOfType<PtyxisTerminal>();
        }

        [Fact]
        public void Deve_retornar_ghostty_quando_terminal_eh_ghostty()
        {
            var terminal = TerminalEmulatorFactory.Criar("ghostty");

            terminal.Should().BeOfType<GhosttyTerminal>();
        }

        [Fact]
        public void Deve_ignorar_case_ao_comparar_nome()
        {
            var terminal = TerminalEmulatorFactory.Criar("GHOSTTY");

            terminal.Should().BeOfType<GhosttyTerminal>();
        }
    }
}
