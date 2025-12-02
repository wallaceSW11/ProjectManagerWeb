namespace ProjectManagerWeb.src.DTOs
{
    /// <summary>
    /// DTO que representa o registro de uma migration executada.
    /// </summary>
    /// <param name="Name">Nome da migration (ex: "001_AddIDEs").</param>
    /// <param name="ExecutedAt">Data e hora de execução da migration.</param>
    public sealed record MigrationRecordDTO(
        string Name,
        DateTime ExecutedAt
    );

    /// <summary>
    /// DTO que representa o arquivo de controle de migrations.
    /// </summary>
    /// <param name="ExecutedMigrations">Lista de migrations já executadas.</param>
    public sealed record MigrationsDTO(
        List<MigrationRecordDTO> ExecutedMigrations
    );
}
