using ProjectManagerWeb.src.DTOs;

namespace ProjectManagerWeb.src.Services.MeuQuerido;

public interface IOpenCodeClient
{
    Task<StatusOpenCodeDTO> ObterStatusAsync(CancellationToken ct = default);
}
