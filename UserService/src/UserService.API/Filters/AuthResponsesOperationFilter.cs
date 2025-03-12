using Microsoft.OpenApi.Models;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Swashbuckle.AspNetCore.SwaggerGen;


namespace UserService.API.Filters;

public class AuthResponsesOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Получаем атрибуты Authorize с метода и класса (если есть)
        var methodAttributes = context.MethodInfo
            .GetCustomAttributes(true)
            .OfType<AuthorizeAttribute>();

        var classAttributes = context.MethodInfo.DeclaringType?
            .GetCustomAttributes(true)
            .OfType<AuthorizeAttribute>() ?? Enumerable.Empty<AuthorizeAttribute>();

        var authAttributes = methodAttributes.Union(classAttributes);

        // Проверяем, есть ли атрибут AllowAnonymous на методе
        var allowAnonymous = context.MethodInfo
            .GetCustomAttributes(true)
            .OfType<AllowAnonymousAttribute>()
            .Any();

        // Если есть Authorize и нет AllowAnonymous, добавляем требование безопасности
        if (authAttributes.Any() && !allowAnonymous)
        {
            operation.Security = new List<OpenApiSecurityRequirement>
            {
                new()
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer" // Должно совпадать с Id в AddSecurityDefinition
                            }
                        },
                        new List<string>()
                    }
                }
            };
        }
    }
}