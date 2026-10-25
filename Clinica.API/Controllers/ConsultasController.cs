using Clinica.API.Extensions;
using Clinica.Application.Common;
using Clinica.Application.Dtos;
using Clinica.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

[ApiController]
[Authorize]
[Route("api/consultas")]
[Produces("application/json")]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
public class ConsultasController : ControllerBase
{
    private readonly IConsultaUseCase _useCase;

    public ConsultasController(IConsultaUseCase useCase) => _useCase = useCase;

    /// <summary>Lista paginada de consultas (PageResult + HATEOAS), opcionalmente filtrada por paciente.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PageResult<GetConsultaDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] int? pacienteId = null) =>
        this.ToActionResult(await _useCase.ObterTodosAsync(page, pageSize, pacienteId));

    /// <summary>Detalhe da consulta com links HATEOAS (self, paciente, cancelar).</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(GetConsultaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int id) =>
        this.ToActionResult(await _useCase.ObterUmAsync(id));

    /// <summary>Agenda uma consulta. O Repository DEVOLVE erros (paciente inexistente, horário ocupado) sem lançar.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(GetConsultaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Post(PostConsultaDto model) =>
        this.ToActionResult(await _useCase.AgendarAsync(model));

    /// <summary>Cancela a consulta (muda o status para Cancelada).</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(GetConsultaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id) =>
        this.ToActionResult(await _useCase.CancelarAsync(id));
}
