using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;

namespace AtividadesJuridicas.Application.Auditoria;

public sealed record ConsultarAcessos(FiltroDeAcessos Filtro, int Pagina = 1)
    : IRequisicao<Resultado<Pagina<RegistroAcessoResumo>>>, IConsultaAuditavel
{
    public string TipoRecurso => "RegistroAcesso";
    public AcaoDeAcesso Acao => AcaoDeAcesso.Listou;
    public string? Detalhe => DetalheDeConsulta.Serializar(new { Filtro, Pagina });
}

public sealed class ConsultarAcessosManipulador(IContextoDeAcesso contexto, IConsultaDeAuditoria consulta)
    : IManipulador<ConsultarAcessos, Resultado<Pagina<RegistroAcessoResumo>>>
{
    public async Task<Resultado<Pagina<RegistroAcessoResumo>>> ManipularAsync(
        ConsultarAcessos requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Resultado<Pagina<RegistroAcessoResumo>>.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        return Resultado<Pagina<RegistroAcessoResumo>>.Ok(await consulta.ConsultarAcessosAsync(
            requisicao.Filtro, Math.Max(1, requisicao.Pagina), ConsultaDeAuditoriaOpcoes.TamanhoDaPagina, ct));
    }
}
