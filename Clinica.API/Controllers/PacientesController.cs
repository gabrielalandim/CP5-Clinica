using Clinica.API.Extensions;
using Clinica.Application.Common;
using Clinica.Application.Dtos;
using Clinica.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

[ApiController]
[Authorize]
[Route("api/pacientes")]
[Produces("application/json")]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
public class PacientesController : ControllerBase
{
    private readonly IPacienteUseCase _useCase;

    public PacientesController(IPacienteUseCase useCase) => _useCase = useCase;

    /// <summary>Lista paginada de pacientes (PageResult) com links HATEOAS de navegação.</summary>
    /// <param name="page">Página (começa em 1).</param>
    /// <param name="pageSize">Itens por página (1 a 50).</param>
    [HttpGet]
    [ProducesResponseType(typeof(PageResult<GetPacienteDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 10) =>
        this.ToActionResult(await _useCase.ObterTodosAsync(page, pageSize));

    /// <summary>Detalhe do paciente com links HATEOAS (self, update, delete, consultas).</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(GetPacienteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int id) =>
        this.ToActionResult(await _useCase.ObterUmAsync(id));

    /// <summary>Cadastra o paciente e preenche o endereço consultando o serviço externo ViaCEP.</summary>
    /// <remarks>CEP de exemplo: 01001-000 (Praça da Sé, São Paulo/SP).</remarks>
    [HttpPost]
    [ProducesResponseType(typeof(GetPacienteDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Post(PostPacienteDto model) =>
        this.ToActionResult(await _useCase.AdicionarAsync(model));

    /// <summary>Atualiza nome e e-mail do paciente.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(GetPacienteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Put(int id, PutPacienteDto model) =>
        this.ToActionResult(await _useCase.AtualizarAsync(id, model));

    /// <summary>Remove o paciente. Somente perfil "admin" (usuário comum recebe 403).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id) =>
        this.ToActionResult(await _useCase.DeletarAsync(id));
}
