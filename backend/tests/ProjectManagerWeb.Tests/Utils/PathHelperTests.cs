using System.Reflection;
using ProjectManagerWeb.src.Utils;

namespace ProjectManagerWeb.Tests.Utils;

public class PathHelperTests
{
    public PathHelperTests()
    {
        PathHelper.Configure("Production");
    }

    public class Configure : PathHelperTests
    {
        [Fact]
        public void Configure_dev_retorna_Banco_Dev()
        {
            PathHelper.Configure("Development");

            Path.GetFileName(PathHelper.BancoPath).Should().Be("Banco_Dev");
        }

        [Fact]
        public void Configure_production_retorna_Banco()
        {
            PathHelper.Configure("Production");

            Path.GetFileName(PathHelper.BancoPath).Should().Be("Banco");
        }

        [Fact]
        public void Configure_qualquer_outro_retorna_Banco()
        {
            PathHelper.Configure("Staging");

            Path.GetFileName(PathHelper.BancoPath).Should().Be("Banco");
        }

        [Theory]
        [InlineData("development")]
        [InlineData("DEVELOPMENT")]
        [InlineData("Development")]
        public void Configure_ignora_case(string environment)
        {
            PathHelper.Configure(environment);

            Path.GetFileName(PathHelper.BancoPath).Should().Be("Banco_Dev");
        }
    }

    public class BancoPath : PathHelperTests
    {
        [Fact]
        public void BancoPath_sem_configure_fallback_production()
        {
            var field = typeof(PathHelper).GetField("_bancoPath",
                BindingFlags.Static | BindingFlags.NonPublic);
            field!.SetValue(null, null);

            Path.GetFileName(PathHelper.BancoPath).Should().Be("Banco");
        }
    }
}
