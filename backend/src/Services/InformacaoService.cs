using System.Reflection;

namespace ProjectManagerWeb.src.Services;

public class InformacaoBuildService
{
    public DateTime ObterDataCompilacao()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        string assemblyPath = assembly.Location;
        return File.GetLastWriteTime(assemblyPath);
    }
}