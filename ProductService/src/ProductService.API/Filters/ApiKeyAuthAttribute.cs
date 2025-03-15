using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ProductService.API.Filters;

public class ApiKeyAuthAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        // Синхронная реализация
        const string API_KEY_HEADER = "X-API-Key";
        
        if (!context.HttpContext.Request.Headers.TryGetValue(API_KEY_HEADER, out var receivedApiKey))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var apiKey = config["ApiKey"];
        
        if (apiKey != receivedApiKey)
            context.Result = new UnauthorizedResult();
    }
}