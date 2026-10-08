using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;

namespace AtividadesJuridicas.Application.Auditoria;

public sealed record ConsultarAuditoria(FiltroDeAuditoria Filtro, int Pagina = 1)
    : IRequisicao<Resultado<Pagina<RegistroAuditoriaResumo>>>, IConsultaAuditavel
{
    public string TipoRecurso => "RegistroAuditoria";
    public AcaoDeAcesso Acao => AcaoDeAcesso.Listou;
    public string? Detalhe => DetalheDeConsulta.Serializar(new { Filtro, Pagina });
}

public sealed class ConsultarAuditoriaManipulador(IContextoDeAcesso contexto, IConsultaDeAuditoria consulta)
    : IManipulador<ConsultarAuditoria, Resultado<Pagina<RegistroAuditoriaResumo>>>
{
    public async Task<Resultado<Pagina<RegistroAuditoriaResumo>>> ManipularAsync(
        ConsultarAuditoria requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Resultado<Pagina<RegistroAuditoriaResumo>>.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        return Resultado<Pagina<RegistroAuditoriaResumo>>.Ok(await consulta.ConsultarAsync(
            requisicao.Filtro, Math.Max(1, requisicao.Pagina), ConsultaDeAuditoriaOpcoes.TamanhoDaPagina, ct));
    }
}
