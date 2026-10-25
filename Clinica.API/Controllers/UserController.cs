using Clinica.API.Extensions;
using Clinica.Application.Dtos;
using Clinica.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/user")]
[Produces("application/json")]
public class UserController : ControllerBase
{
    private readonly IUserUseCase _useCase;

    public UserController(IUserUseCase useCase) => _useCase = useCase;

    /// <summary>Cadastro público. Sempre cria usuário com perfil "user".</summary>
    [HttpPost]
    [ProducesResponseType(typeof(GetUserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Post(PostUserDto model) =>
        this.ToActionResult(await _useCase.RegistrarAsync(model));
}
