using Clinica.API.Extensions;
using Clinica.Application.Dtos;
using Clinica.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IUserUseCase _useCase;

    public AuthController(IUserUseCase useCase) => _useCase = useCase;

    /// <summary>Login: devolve o JWT. Usuários iniciais: admin@clinica.com / Admin@123 e user@clinica.com / User@123.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(GetAuthDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Post(PostAuthDto model) =>
        this.ToActionResult(await _useCase.AutenticarAsync(model));
}
