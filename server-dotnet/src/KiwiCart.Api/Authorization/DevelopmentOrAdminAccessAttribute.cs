using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace KiwiCart.Api.Authorization;

public sealed class DevelopmentOrAdminAccessAttribute : Attribute, IAsyncAuthorizationFilter
{
    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var environment = context.HttpContext.RequestServices
            .GetRequiredService<IWebHostEnvironment>();

        if (environment.IsDevelopment())
            return Task.CompletedTask;

        var authorizationService = context.HttpContext.RequestServices
            .GetRequiredService<IAuthorizationService>();

        return AuthorizeAsync(context, authorizationService);
    }

    private static async Task AuthorizeAsync(
        AuthorizationFilterContext context,
        IAuthorizationService authorizationService)
    {
        var result = await authorizationService.AuthorizeAsync(
            context.HttpContext.User,
            AdminAuthorization.PolicyName);

        if (result.Succeeded)
            return;

        context.Result = context.HttpContext.User.Identity?.IsAuthenticated == true
            ? new Microsoft.AspNetCore.Mvc.ForbidResult()
            : new Microsoft.AspNetCore.Mvc.ChallengeResult();
    }
}
