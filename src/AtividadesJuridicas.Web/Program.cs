using AtividadesJuridicas.Application;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Infrastructure;
using AtividadesJuridicas.Infrastructure.Persistencia;
using AtividadesJuridicas.Web.Components;
using AtividadesJuridicas.Web.Infra;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.WebEncoders;
using System.Text.Encodings.Web;
using System.Text.Unicode;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(
    sp => sp.GetRequiredService<IConfiguration>().GetConnectionString("Padrao")
          ?? throw new InvalidOperationException("ConnectionStrings:Padrao não configurada."),
    sp =>
    {
        // Caminho relativo é resolvido a partir da pasta da aplicação.
        var pasta = sp.GetRequiredService<IConfiguration>()["Armazenamento:Pasta"]
                    ?? throw new InvalidOperationException("Armazenamento:Pasta não configurada.");
        return Path.GetFullPath(pasta, sp.GetRequiredService<IWebHostEnvironment>().ContentRootPath);
    });

// Sem isto o HTML sai com acentos como entidades numéricas (&#xE1;).
builder.Services.Configure<WebEncoderOptions>(opcoes =>
    opcoes.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));
builder.Services.AddScoped<IDespachante, Despachante>();
builder.Services.AddScoped<OrigemDaConexao>();
builder.Services.AddSingleton(new FusoDoEscritorio(
    builder.Configuration["Escritorio:FusoHorario"] ?? "America/Sao_Paulo"));

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opcoes =>
    {
        opcoes.Cookie.Name = "atividades.auth";
        opcoes.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        opcoes.LoginPath = "/conta/entrar";
        opcoes.AccessDeniedPath = "/conta/entrar";
        opcoes.ExpireTimeSpan = TimeSpan.FromHours(8);
        opcoes.SlidingExpiration = true;
        opcoes.Events.OnValidatePrincipal = ValidacaoDeSessao.ValidarAsync;
    });

// Toda rota exige usuário autenticado; o acesso anônimo é exceção declarada.
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

var app = builder.Build();

await using (var escopo = app.Services.CreateAsyncScope())
{
    var secao = app.Configuration.GetSection("AdminInicial");
    var admin = secao["Email"] is { Length: > 0 } email
        ? new AdminInicialOpcoes(secao["Nome"] ?? "Administrador", email, secao["Senha"] ?? string.Empty)
        : null;
    await escopo.ServiceProvider.GetRequiredService<SemeadorInicial>().ExecutarAsync(admin);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/erro", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStatusCodePagesWithReExecute("/nao-encontrada", createScopeForStatusCodePages: true);
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets().AllowAnonymous();

app.MapPost("/conta/sair", async (HttpContext contexto, IAntiforgery antiforgery, IAuditoria auditoria) =>
{
    if (!await antiforgery.IsRequestValidAsync(contexto))
        return Results.BadRequest();

    IdentidadeHttp.Definir(contexto);
    await auditoria.RegistrarAsync(
        EventosDeSeguranca.Logout, "Usuario", IdentidadeHttp.UsuarioId(contexto.User), ct: contexto.RequestAborted);

    await contexto.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.LocalRedirect("/conta/entrar");
}).DisableAntiforgery();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();

public partial class Program;
