using Microsoft.AspNetCore.Mvc;
using ReferWell.Application.Common.Models;

namespace ReferWell.Api.Extensions;

public static class AppResultExtensions
{
    public static IActionResult ToActionResult(this AppResult result, ControllerBase controller)
    {
        return result.Status switch
        {
            AppStatus.Ok => controller.Ok(result.Value),
            AppStatus.Created => controller.Ok(result.Value),
            AppStatus.BadRequest => controller.BadRequest(result.Value ?? new { message = result.Message }),
            AppStatus.NotFound => result.Value != null ? controller.NotFound(result.Value) : controller.NotFound(),
            AppStatus.Forbid => controller.Forbid(),
            AppStatus.Conflict => controller.Conflict(result.Value ?? new { message = result.Message }),
            AppStatus.Unauthorized => controller.Unauthorized(result.Value ?? new { message = result.Message }),
            AppStatus.ServerError => controller.StatusCode(500, result.Value),
            AppStatus.File when result.FileBytes != null => ToFileResult(result, controller),
            _ => controller.StatusCode(500, new { message = "Unexpected error." })
        };
    }

    private static IActionResult ToFileResult(AppResult result, ControllerBase controller)
    {
        if (result.InlineDisposition)
        {
            controller.Response.Headers.Append("Content-Disposition", "inline");
            return controller.File(result.FileBytes!, result.ContentType ?? "application/octet-stream");
        }

        return controller.File(result.FileBytes!, result.ContentType ?? "application/octet-stream", result.FileDownloadName);
    }
}
