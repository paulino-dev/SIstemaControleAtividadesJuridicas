using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Documentos;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Infrastructure.Armazenamento;
using AtividadesJuridicas.Infrastructure.Comum;
using AtividadesJuridicas.Infrastructure.Persistencia;
using AtividadesJuridicas.Infrastructure.Persistencia.Interceptores;
using AtividadesJuridicas.Infrastructure.Persistencia.Repositorios;
using AtividadesJuridicas.Infrastructure.Seguranca;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection servicos,
        Func<IServiceProvider, string> obterStringDeConexao,
        Func<IServiceProvider, string> obterPastaDeArquivos)
    {
        servicos.AddScoped<CarimbosInterceptor>();
        servicos.AddScoped<AuditoriaInterceptor>();

        servicos.AddDbContext<AppDbContext>((sp, opcoes) => opcoes
            .UseNpgsql(obterStringDeConexao(sp))
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(
                sp.GetRequiredService<CarimbosInterceptor>(),
                sp.GetRequiredService<AuditoriaInterceptor>()));

        servicos.AddSingleton<IRelogio, RelogioDoSistema>();
        servicos.AddSingleton<IHasherDeSenha, HasherDeSenhaPbkdf2>();
        servicos.AddScoped<IAuditoria, RegistradorDeAuditoria>();
        servicos.AddScoped<IUnidadeDeTrabalho, UnidadeDeTrabalho>();
        servicos.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
        servicos.AddScoped<IPerfilRepositorio, PerfilRepositorio>();
        servicos.AddScoped<IAreaAtuacaoRepositorio, AreaAtuacaoRepositorio>();
        servicos.AddScoped<IClienteRepositorio, ClienteRepositorio>();
        servicos.AddScoped<IProcessoRepositorio, ProcessoRepositorio>();
        servicos.AddScoped<IDocumentoRepositorio, DocumentoRepositorio>();
        servicos.AddScoped<IConsultaDeAuditoria, ConsultaDeAuditoria>();
        servicos.AddScoped<SemeadorInicial>();

        servicos.AddSingleton<IArmazenamentoDeArquivos>(sp =>
            new ArmazenamentoEmDisco(obterPastaDeArquivos(sp), sp.GetRequiredService<IRelogio>()));

        return servicos;
    }
}
