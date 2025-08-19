using Microsoft.AspNetCore.Mvc;
using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Services;

namespace ProjectManagerWeb.src.Controllers
{
    [ApiController]
    [Route("api/repositorios")]
    public class RepositorioController(RepositorioJsonService repositorioService) : ControllerBase
    {

        [HttpGet]
        public async Task<IActionResult> ConsultarTodos()
        {
            var repositorios = await repositorioService.GetAllAsync();
            return Ok(repositorios);
        }

        [HttpPost]
        public async Task<IActionResult> CadastrarRepositorio([FromBody] RepositorioRequestDTO repositorio)
        {
            if (repositorio == null) return BadRequest("O corpo da requisição não pode ser nulo.");

            try
            {
                var cadastrado = await repositorioService.AddAsync(repositorio);
                return Ok(cadastrado);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        
        // PUT: api/Configuracao/repositorios/{id}
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] RepositorioRequestDTO repositorioAtualizado)
        {
            if (repositorioAtualizado == null) return BadRequest();

            var sucesso = await repositorioService.UpdateAsync(id, repositorioAtualizado);
            if (!sucesso)
            {
                return NotFound(); // Retorna 404 se o ID a ser atualizado não existe
            }
            
            return NoContent(); // Retorna 204 para indicar sucesso na atualização
        }

        // DELETE: api/Configuracao/repositorios/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var sucesso = await repositorioService.DeleteAsync(id);
            if (!sucesso)
            {
                return NotFound(); // Retorna 404 se o ID a ser excluído não existe
            }

            return NoContent(); // Retorna 204 para indicar sucesso na exclusão
        }
    }
}