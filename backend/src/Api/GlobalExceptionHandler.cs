using Application;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace Api;
public sealed class GlobalExceptionHandler(IProblemDetailsService problems, ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var status = exception switch { ValidationException => 400, NaoEncontradoException => 404, RegraNegocioException => 409, DbUpdateException => 409, _ => 500 };
        var titulo = status switch { 400 => "Dados inválidos", 404 => "Recurso não encontrado", 409 => "Conflito na operação", _ => "Erro interno" };
        if (status == 500) logger.LogError(exception, "Erro ao processar {Caminho}", context.Request.Path);
        else logger.LogWarning("Falha {Status} ao processar {Caminho}: {Mensagem}", status, context.Request.Path, exception.Message);
        var problem = new ProblemDetails { Status = status, Title = titulo, Detail = status == 500 ? "Não foi possível concluir a operação." : exception is DbUpdateException ? "Não foi possível persistir a alteração por conflito de dados." : exception.Message, Instance = context.Request.Path };
        problem.Extensions["traceId"] = context.TraceIdentifier;
        if (exception is ValidationException validation) problem.Extensions["errors"] = validation.Errors.GroupBy(x => x.PropertyName).ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
        context.Response.StatusCode = status;
        await problems.WriteAsync(new ProblemDetailsContext { HttpContext = context, ProblemDetails = problem });
        return true;
    }
}
