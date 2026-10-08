using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Domain.Auditoria;

namespace AtividadesJuridicas.Application.Identidade;

/// <summary>Todos os perfis, ativos e inativos, para a gestão. Quem escolhe um perfil para um usuário usa ListarPerfisAtivos.</summary>
public sealed record ListarPerfis : IRequisicao<Resultado<IReadOnlyList<PerfilDetalhe>>>, IConsultaAuditavel
{
    public string TipoRecurso => "Perfil";
    public AcaoDeAcesso Acao => AcaoDeAcesso.Listou;
}

public sealed class ListarPerfisManipulador(IContextoDeAcesso contexto, IPerfilRepositorio perfis)
    : IManipulador<ListarPerfis, Resultado<IReadOnlyList<PerfilDetalhe>>>
{
    public async Task<Resultado<IReadOnlyList<PerfilDetalhe>>> ManipularAsync(ListarPerfis requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Resultado<IReadOnlyList<PerfilDetalhe>>.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        return Resultado<IReadOnlyList<PerfilDetalhe>>.Ok(await perfis.ListarAsync(ct));
    }
}
