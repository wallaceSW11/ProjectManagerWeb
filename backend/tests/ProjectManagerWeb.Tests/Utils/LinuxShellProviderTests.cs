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

    public class ResolverGerenciadorArquivos : IDisposable
    {
        private readonly string _raiz = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        public ResolverGerenciadorArquivos()
        {
            Directory.CreateDirectory(_raiz);
        }

        [Fact]
        public void Deve_retornar_nautilus_quando_apenas_ele_existe()
        {
            var diretorio = CriarDiretorioCom("nautilus");

            var gerenciador = LinuxShellProvider.ResolverGerenciadorArquivos(diretorio);

            gerenciador.Should().Be("nautilus");
        }

        [Fact]
        public void Deve_retornar_dolphin_quando_apenas_ele_existe()
        {
            var diretorio = CriarDiretorioCom("dolphin");

            var gerenciador = LinuxShellProvider.ResolverGerenciadorArquivos(diretorio);

            gerenciador.Should().Be("dolphin");
        }

        [Fact]
        public void Deve_preferir_nautilus_quando_mais_de_um_existe()
        {
            var diretorioDolphin = CriarDiretorioCom("dolphin");
            var diretorioNautilus = CriarDiretorioCom("nautilus");

            var gerenciador = LinuxShellProvider.ResolverGerenciadorArquivos(MontarPath(diretorioDolphin, diretorioNautilus));

            gerenciador.Should().Be("nautilus");
        }

        [Fact]
        public void Deve_retornar_nulo_quando_nenhum_gerenciador_existe()
        {
            var diretorio = CriarDiretorioCom("gedit", "code");

            var gerenciador = LinuxShellProvider.ResolverGerenciadorArquivos(diretorio);

            gerenciador.Should().BeNull();
        }

        [Fact]
        public void Deve_ignorar_diretorio_inexistente_no_path()
        {
            var diretorioInexistente = Path.Combine(_raiz, Guid.NewGuid().ToString());
            var diretorioValido = CriarDiretorioCom("thunar");

            var gerenciador = LinuxShellProvider.ResolverGerenciadorArquivos(MontarPath(diretorioInexistente, diretorioValido));

            gerenciador.Should().Be("thunar");
        }

        [Fact]
        public void Deve_retornar_nulo_quando_path_e_vazio()
        {
            var gerenciador = LinuxShellProvider.ResolverGerenciadorArquivos(string.Empty);

            gerenciador.Should().BeNull();
        }

        public void Dispose()
        {
            if (Directory.Exists(_raiz)) Directory.Delete(_raiz, true);
        }

        private string CriarDiretorioCom(params string[] executaveis)
        {
            var diretorio = Path.Combine(_raiz, Guid.NewGuid().ToString());
            Directory.CreateDirectory(diretorio);

            foreach (var executavel in executaveis)
                File.WriteAllText(Path.Combine(diretorio, executavel), string.Empty);

            return diretorio;
        }

        private static string MontarPath(params string[] diretorios) =>
            string.Join(Path.PathSeparator, diretorios);
    }

    public class ResolverEditorTexto : IDisposable
    {
        private readonly string _raiz = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        public ResolverEditorTexto()
        {
            Directory.CreateDirectory(_raiz);
        }

        [Fact]
        public void Deve_retornar_code_quando_apenas_ele_existe()
        {
            var diretorio = CriarDiretorioCom("code");

            var editor = LinuxShellProvider.ResolverEditorTexto(diretorio);

            editor.Should().Be("code");
        }

        [Fact]
        public void Deve_retornar_gedit_quando_apenas_ele_existe()
        {
            var diretorio = CriarDiretorioCom("gedit");

            var editor = LinuxShellProvider.ResolverEditorTexto(diretorio);

            editor.Should().Be("gedit");
        }

        [Fact]
        public void Deve_preferir_code_quando_mais_de_um_existe()
        {
            var diretorioGedit = CriarDiretorioCom("gedit");
            var diretorioCode = CriarDiretorioCom("code");

            var editor = LinuxShellProvider.ResolverEditorTexto(MontarPath(diretorioGedit, diretorioCode));

            editor.Should().Be("code");
        }

        [Fact]
        public void Deve_retornar_nulo_quando_nenhum_editor_existe()
        {
            var diretorio = CriarDiretorioCom("nautilus", "dolphin");

            var editor = LinuxShellProvider.ResolverEditorTexto(diretorio);

            editor.Should().BeNull();
        }

        [Fact]
        public void Deve_ignorar_diretorio_inexistente_no_path()
        {
            var diretorioInexistente = Path.Combine(_raiz, Guid.NewGuid().ToString());
            var diretorioValido = CriarDiretorioCom("kate");

            var editor = LinuxShellProvider.ResolverEditorTexto(MontarPath(diretorioInexistente, diretorioValido));

            editor.Should().Be("kate");
        }

        [Fact]
        public void Deve_retornar_nulo_quando_path_e_vazio()
        {
            var editor = LinuxShellProvider.ResolverEditorTexto(string.Empty);

            editor.Should().BeNull();
        }

        public void Dispose()
        {
            if (Directory.Exists(_raiz)) Directory.Delete(_raiz, true);
        }

        private string CriarDiretorioCom(params string[] executaveis)
        {
            var diretorio = Path.Combine(_raiz, Guid.NewGuid().ToString());
            Directory.CreateDirectory(diretorio);

            foreach (var executavel in executaveis)
                File.WriteAllText(Path.Combine(diretorio, executavel), string.Empty);

            return diretorio;
        }

        private static string MontarPath(params string[] diretorios) =>
            string.Join(Path.PathSeparator, diretorios);
    }
}
