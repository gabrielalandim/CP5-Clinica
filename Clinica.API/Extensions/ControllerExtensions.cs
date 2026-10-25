using Clinica.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Extensions;

/// <summary>Corpo padrão de erro devolvido pela API.</summary>
public record ErrorResponse(int Status, string Code, string Error, string Path);

public static class ControllerExtensions
{
    /// <summary>Converte um OperationResult em resposta HTTP.</summary>
    public static IActionResult ToActionResult<T>(this ControllerBase controller, OperationResult<T> result)
    {
        if (!result.IsSuccess)
        {
            var body = new ErrorResponse(
                result.StatusCode,
                result.ErrorCode ?? "ERRO",
                result.Error ?? "Erro desconhecido.",
                controller.HttpContext?.Request.Path.Value ?? string.Empty);

            return controller.StatusCode(result.StatusCode, body);
        }

        if (result.StatusCode == StatusCodes.Status204NoContent)
            return controller.NoContent();

        return controller.StatusCode(result.StatusCode, result.Value);
    }
}
