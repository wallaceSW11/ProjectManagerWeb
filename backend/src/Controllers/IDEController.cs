using Microsoft.AspNetCore.Mvc;
using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Services;

namespace ProjectManagerWeb.src.Controllers
{
    [ApiController]
    [Route("api/ides")]
    public class IDEController(IDEJsonService ideService, RepositorioJsonService repositorioService) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> ConsultarTodos()
        {
            var ides = await ideService.GetAllAsync();
            return Ok(ides);
        }

        [HttpGet("{identificador:guid}")]
        public async Task<IActionResult> ConsultarPorId(Guid identificador)
        {
            var ide = await ideService.GetByIdAsync(identificador);
            
            if (ide == null)
                return NotFound($"IDE com identificador {identificador} não encontrada");

            return Ok(ide);
        }

        [HttpPost]
        public async Task<IActionResult> Cadastrar([FromBody] IDEDTO ide)
        {
            if (ide == null)
                return BadRequest("O corpo da requisição não pode ser nulo");

            if (string.IsNullOrWhiteSpace(ide.Nome))
                return BadRequest("O nome da IDE é obrigatório");

            if (string.IsNullOrWhiteSpace(ide.ComandoParaExecutar))
                return BadRequest("O comando para executar é obrigatório");

            try
            {
                var cadastrada = await ideService.AddAsync(ide);
                return Ok(cadastrada);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{identificador:guid}")]
        public async Task<IActionResult> Atualizar(Guid identificador, [FromBody] IDEDTO ideAtualizada)
        {
            if (ideAtualizada == null)
                return BadRequest("O corpo da requisição não pode ser nulo");

            if (string.IsNullOrWhiteSpace(ideAtualizada.Nome))
                return BadRequest("O nome da IDE é obrigatório");

            if (string.IsNullOrWhiteSpace(ideAtualizada.ComandoParaExecutar))
                return BadRequest("O comando para executar é obrigatório");

            var sucesso = await ideService.UpdateAsync(identificador, ideAtualizada);
            
            if (!sucesso)
                return NotFound($"IDE com identificador {identificador} não encontrada");

            return NoContent();
        }

        [HttpDelete("{identificador:guid}")]
        public async Task<IActionResult> Excluir(Guid identificador)
        {
            // Verificar se a IDE está sendo usada por algum projeto
            var estaEmUso = await ideService.IsReferencedByProjectsAsync(identificador, repositorioService);
            
            if (estaEmUso)
                return Conflict("Não é possível excluir esta IDE pois ela está sendo utilizada por um ou mais projetos");

            var sucesso = await ideService.DeleteAsync(identificador);
            
            if (!sucesso)
                return NotFound($"IDE com identificador {identificador} não encontrada");

            return NoContent();
        }
    }
}
