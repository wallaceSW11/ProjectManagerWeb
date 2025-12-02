using System.ComponentModel.Design;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using ProjectManagerWeb.src.Enuns;

namespace ProjectManagerWeb.src.DTOs
{

    /// <summary>
    /// DTO principal que representa um repositório Git com seus projetos.
    /// Corresponde ao objeto raiz no array JSON.
    /// </summary>
    /// <param name="Url">A URL do repositório Git principal.</param>
    /// <param name="Projetos">A lista de projetos contidos neste repositório.</param>
    /// <param name="Agregados">Uma lista opcional de URLs de repositórios relacionados/agregados.</param>
    public sealed record RepositorioRequestDTO(
        Guid Identificador,
        string Url,
        string Nome,
        string Titulo,
        string? Cor,
        List<ProjetoDTO> Projetos,
        List<Guid>? Agregados,
        List<MenuDTO>? Menus
    );

    /// <summary>
    /// Representa um projeto específico dentro de um repositório.
    /// </summary>
    /// <param name="Nome">O nome de exibição do projeto.</param>
    /// <param name="Subdiretorio">O caminho relativo para a pasta do projeto dentro do repositório.</param>
    /// <param name="Comandos">A lista de objetos de comando associados a este projeto.</param>
    public sealed record ProjetoDTO(
        Guid Identificador,
        string Nome,
        string? Subdiretorio,
        string? PerfilVSCode,
        ComandoDTO Comandos,
        string? ArquivoCoverage,
        bool Expandido = false
    );

    /// <summary>
    /// Representa um conjunto de comandos para um projeto.
    /// As propriedades são strings nuláveis para acomodar comandos que podem ou não existir.
    /// </summary>
    /// <param name="Instalar">O comando para instalar dependências (ex: "npm i").</param>
    /// <param name="Iniciar">O comando para iniciar o projeto (ex: "npm start" ou "dotnet run").</param>
    /// <param name="Buildar">O comando para compilar o projeto.</param>
    /// <param name="IDEIdentificador">Identificador da IDE a ser usada para abrir o projeto.</param>
    public sealed record ComandoDTO(
        string? Instalar,
        string? Iniciar,
        string? Buildar,
        Guid? IDEIdentificador
    );

    public sealed record MenuDTO(
        Guid Identificador,
        string Titulo,
        string Tipo,
        List<ArquivosDTO>? Arquivos,
        List<string>? Comandos
    );

    public sealed record ArquivosDTO(
        string Arquivo,
        string Destino,
        bool IgnorarGit
    );
}