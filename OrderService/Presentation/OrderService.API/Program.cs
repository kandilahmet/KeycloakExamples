using System.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using CrossCutting.Abstractions;
using CrossCutting.Configuration;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq; 
using OrderService.Application.Features.Queries.Request;
using OrderService.Core.Application;
using OrderService.Core.Application.Features.Commands.Create;
using OrderService.Infrastructure.Cache.Redis;
using OrderService.Infrastructure.Infrastructure;
using OrderService.Infrastructure.Persistence;
using OrderService.Infrastructure.Security.Auth.Cookies;
using OrderService.Infrastructure.Security.Auth.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddOpenApi();
builder.Services.AddApplicationServices();
builder.Services.AddPersistenceServices(builder.Configuration);
builder.Services.AddInfrastructureService(builder.Configuration);
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "myclients",
        policy =>
            policy
                .WithOrigins(
                    "https://localhost:7047",
                    "http://localhost:5054",
                    "http://localhost:8080"
                )
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials()
    );
});

// Redis yapılandırması
builder.Services.AddStackExchangeRedisCache(opt =>
{
    opt.Configuration = builder.Configuration.GetSection("Redis").Value;
});

// Authentication yapılandırması
builder
    .Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    }).AddCustomCookies(builder.Configuration)
    .AddCustomOpenIdConnect(builder.Configuration)
    ;

var app = builder.Build();

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json/", "OpenAPI V1");
    });
}

app.UseCors("myclients");

//app.UseSession(); // Session middleware'ini ekle
app.UseAuthentication();
app.UseAuthorization();

//app.UseHttpsRedirection();

app.MapGet(
    "login-callback",
    (HttpContext context) =>
    {
        if (context.Response.StatusCode != 200)
        {
            return Results.Problem(
                title: "Authentication Error",
                statusCode: context.Response.StatusCode,
                detail: "An error occurred during authentication callback"
            );
        }
        return Results.Ok(new { message = "Authentication successful" });
    }
).ExcludeFromDescription();

 
app.MapGet("Login", () => "Hello Guest").ExcludeFromDescription();

app.MapGet(
    "NotLogin",
    (HttpContext context) =>
    {
        var _context = context;
    }
).ExcludeFromDescription();

app.MapPost(
    "CreateOrder",
    async ([FromBody] CreateOrderCommandRequest createOrderVM, IMediator mediator) =>
    {
        await mediator.Send(createOrderVM);
    }
);

app.MapGet(
        "GetAllOrders",
        async (HttpContext context, IMediator mediator) =>
        {
            try
            {
                // Rol kontrolü
                if (
                    !context.User.Claims.Any(c =>
                        (c.Type == ClaimTypes.Role || c.Type == "roles")
                        && c.Value.Equals("admin", StringComparison.OrdinalIgnoreCase)
                    )
                )
                {
                    return Results.Forbid();
                }

                var result = await mediator.Send(new GetAllOrdersQueryRequest());
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Error",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        }
    )
    .RequireAuthorization()
    .WithName("GetAllOrders");//.RequireAuthorization(x => x.RequireRole("admin")); ;

app.MapGet(
    "logout",
    async (IHttpContextAccessor contextAccessor) =>
    {
         

        var settings = builder.Configuration
                .GetSection(nameof(OpenIdConnectSettings))
                .Get<OpenIdConnectSettings>();


        var context = contextAccessor?.HttpContext;
        if (context == null)
            return Results.BadRequest("Invalid HTTP context");

        try
        {
            // Session'ı temizle
            context.Session?.Clear();
            if (context.Session != null)
                await context.Session.CommitAsync();

            // Authentication cookie'lerini temizle
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);

            // Keycloak'tan çıkış yap
            var keycloakLogoutUrl =
                "http://localhost:8080/realms/bp/protocol/openid-connect/logout";
            var clientId = "service3";
            var redirectUri = "https://localhost:7047/signout-remote";
            var idToken = await context.GetTokenAsync("id_token");

            if (string.IsNullOrEmpty(idToken))
            {
                return Results.BadRequest("ID token not found");
            }

            var logoutUrl =
                $"{keycloakLogoutUrl}?"
                + $"client_id={clientId}"
                + $"&id_token_hint={idToken}"
                + $"&post_logout_redirect_uri={Uri.EscapeDataString(redirectUri)}";

            return Results.Redirect(logoutUrl);
        }
        catch (Exception ex)
        {
            return Results.BadRequest($"Logout failed: {ex.Message}");
        }
    }
).ExcludeFromDescription();
  
app.MapGet(
    "api/logout",
    async (IHttpContextAccessor contextAccessor, [FromServices] ITicketStore ticketStore) =>
    {
        var context = contextAccessor?.HttpContext;
        if (context == null)
            return Results.BadRequest("Invalid HTTP context");

        try
        {
            // Authentication ticket'ı temizle
            var ticket = await context.AuthenticateAsync(
                CookieAuthenticationDefaults.AuthenticationScheme
            );
            if (ticket?.Succeeded == true && ticket.Properties != null)
            {
                var ticketId = ticket.Properties.GetTokenValue(".AspNetCore.Ticket");
                if (!string.IsNullOrEmpty(ticketId))
                {
                    await ticketStore.RemoveAsync(ticketId);
                }
            }

            // Authentication cookie'lerini temizle
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);

            // Keycloak'tan çıkış yap
            var keycloakLogoutUrl =
                "http://localhost:8080/realms/bp/protocol/openid-connect/logout";
            var clientId = "service3";
            var redirectUri = "https://localhost:7047/signout-remote";

            // ID token'ı ticket'tan al
            var idToken = ticket?.Properties?.GetTokenValue("id_token");
            if (string.IsNullOrEmpty(idToken))
            {
                return Results.Ok(new { message = "Local logout successful" });
            }

            var logoutUrl =
                $"{keycloakLogoutUrl}?"
                + $"client_id={clientId}"
                + $"&id_token_hint={idToken}"
                + $"&post_logout_redirect_uri={Uri.EscapeDataString(redirectUri)}";

            return Results.Ok(new { logoutUrl });
        }
        catch (Exception ex)
        {
            return Results.BadRequest($"Logout failed: {ex.Message}");
        }
    }
).ExcludeFromDescription();

// Giriş başlatma
app.MapGet("/auth/login", async (HttpContext context) =>
    await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, new()
    {
        RedirectUri = "/"
    })).ExcludeFromDescription();

// Callback endpoint (otomatik işlenir, açıkça tanımlamaya gerek yok)

// Front-channel logout endpoint
app.MapGet("/api/auth/frontchannel-signout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Ok();
}).DisableAntiforgery().ExcludeFromDescription();  

// Backchannel logout callback
app.MapPost("/api/auth/signout-callback-oidc", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Ok();
}).ExcludeFromDescription(); 

// Logout başlatma
app.MapGet("/auth/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme, new()
    {
        RedirectUri = "/auth/signout-complete"
    });
}).ExcludeFromDescription();

// Logout tamamlandı sayfası
app.MapGet("/auth/signout-complete", () =>
    Results.Ok("Çıkış işlemi başarıyla tamamlandı")).ExcludeFromDescription();

// Erişim reddedildi sayfası
app.MapGet("/auth/access-denied", () =>
    Results.Forbid()).ExcludeFromDescription();
app.Run();

public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger
    )
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception has occurred.");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var response = new
        {
            error = "An error occurred while processing your request.",
            details = exception.Message,
        };

        return context.Response.WriteAsJsonAsync(response);
    }
}
