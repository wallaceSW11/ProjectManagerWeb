using ProjectManagerWeb.src.DTOs;

namespace ProjectManagerWeb.src.Services;

public interface IIDEJsonService
{
    Task<List<IDEDTO>> GetAllAsync();
    Task<IDEDTO?> GetByIdAsync(Guid identificador);
    Task<IDEDTO> AddAsync(IDEDTO novaIDE);
    Task<bool> UpdateAsync(Guid identificador, IDEDTO atualizada);
    Task<bool> DeleteAsync(Guid identificador);
}
