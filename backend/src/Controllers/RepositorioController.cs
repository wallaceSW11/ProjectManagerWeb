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

        // PUT: api/Configuracao/repositorios/{identificador}
        [HttpPut("{identificador:guid}")]
        public async Task<IActionResult> Update(Guid identificador, [FromBody] RepositorioRequestDTO repositorioAtualizado)
        {
            if (repositorioAtualizado == null) return BadRequest();

            var sucesso = await repositorioService.UpdateAsync(identificador, repositorioAtualizado);
            if (!sucesso)
            {
                return NotFound(); // Retorna 404 se o ID a ser atualizado não existe
            }

            return NoContent(); // Retorna 204 para indicar sucesso na atualização
        }

        // DELETE: api/Configuracao/repositorios/{identificador}
        [HttpDelete("{identificador:guid}")]
        public async Task<IActionResult> Delete(Guid identificador)
        {
            var sucesso = await repositorioService.DeleteAsync(identificador);
            if (!sucesso)
            {
                return NotFound();
            }

            return NoContent();
        }

        [HttpPut("indices")]
        public async Task<IActionResult> AtualizarIndices([FromBody] List<RepositorioIndiceRequestDTO> indices)
        {
            if (indices == null || indices.Count == 0) return BadRequest();

            await repositorioService.AtualizarIndicesAsync(indices);
            return Ok();
        }

        [HttpGet("codigos-tarefa/{iniciais}")]
        public async Task<IActionResult> BuscarCodigoTarefa(string iniciais)
        {
            if (string.IsNullOrWhiteSpace(iniciais))
                return BadRequest("Iniciais são obrigatórias");

            var repositorios = await repositorioService.GetAllAsync();

            foreach (var repo in repositorios)
            {
                if (repo.CodigosTarefa is null) continue;

                var codigo = repo.CodigosTarefa.FirstOrDefault(c =>
                    c.Iniciais.Equals(iniciais, StringComparison.OrdinalIgnoreCase));

                if (codigo is null) continue;

                return Ok(new
                {
                    codigoTarefa = codigo,
                    repositorio = new
                    {
                        repo.Identificador,
                        repo.Url,
                        repo.Nome,
                        repo.Titulo,
                        repo.Cor,
                        repo.BranchBase,
                        repo.IDEIdentificador,
                        repo.PerfilVSCode,
                        repo.Subdiretorio,
                        repo.CliComando,
                        repo.PerfilTerminal,
                        repo.AbrirWorkspace,
                        repo.CliComandoComplementar,
                        repo.PastaCentralizadora,
                        repo.UrlBaseGestorTarefas
                    }
                });
            }

            return NotFound("Código de tarefa não encontrado");
        }

        [HttpPost("{identificador:guid}/codigos-tarefa")]
        public async Task<IActionResult> AdicionarCodigoTarefa(Guid identificador, [FromBody] CodigoTarefaDTO codigoTarefa)
        {
            if (codigoTarefa is null || string.IsNullOrWhiteSpace(codigoTarefa.Iniciais))
                return BadRequest("Iniciais são obrigatórias");

            try
            {
                var repo = await repositorioService.GetByIdAsync(identificador);
                if (repo is null) return NotFound("Repositório não encontrado");

                var codigos = repo.CodigosTarefa ?? [];
                if (codigos.Any(c => c.Iniciais.Equals(codigoTarefa.Iniciais, StringComparison.OrdinalIgnoreCase)))
                    return BadRequest("Já existe um código de tarefa com essas iniciais");

                var atualizado = repo with { CodigosTarefa = [.. codigos, codigoTarefa] };
                await repositorioService.UpdateAsync(identificador, atualizado);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{identificador:guid}/codigos-tarefa/{codigoId:guid}")]
        public async Task<IActionResult> AtualizarCodigoTarefa(Guid identificador, Guid codigoId, [FromBody] CodigoTarefaDTO codigoTarefa)
        {
            if (codigoTarefa is null || string.IsNullOrWhiteSpace(codigoTarefa.Iniciais))
                return BadRequest("Iniciais são obrigatórias");

            try
            {
                var repo = await repositorioService.GetByIdAsync(identificador);
                if (repo is null) return NotFound("Repositório não encontrado");

                var codigos = repo.CodigosTarefa ?? [];
                var indice = codigos.FindIndex(c => c.Identificador == codigoId);
                if (indice == -1) return NotFound("Código de tarefa não encontrado");

                if (codigos.Any(c => c.Iniciais.Equals(codigoTarefa.Iniciais, StringComparison.OrdinalIgnoreCase) && c.Identificador != codigoId))
                    return BadRequest("Já existe outro código de tarefa com essas iniciais");

                codigos[indice] = codigoTarefa;
                var atualizado = repo with { CodigosTarefa = codigos };
                await repositorioService.UpdateAsync(identificador, atualizado);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{identificador:guid}/codigos-tarefa/{codigoId:guid}")]
        public async Task<IActionResult> RemoverCodigoTarefa(Guid identificador, Guid codigoId)
        {
            try
            {
                var repo = await repositorioService.GetByIdAsync(identificador);
                if (repo is null) return NotFound("Repositório não encontrado");

                var codigos = repo.CodigosTarefa ?? [];
                var atualizados = codigos.Where(c => c.Identificador != codigoId).ToList();
                if (atualizados.Count == codigos.Count)
                    return NotFound("Código de tarefa não encontrado");

                var atualizado = repo with { CodigosTarefa = atualizados };
                await repositorioService.UpdateAsync(identificador, atualizado);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}