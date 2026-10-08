using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;

namespace AtividadesJuridicas.Application.Agenda;

public sealed record UsuarioDaAgenda(Guid Id, string Nome);

/// <summary>
/// Os usuários ativos, só com Id e nome, para escolher participantes. Aberta a qualquer usuário autenticado; apoio de
/// formulário, por isso não é consulta auditável.
/// </summary>
public sealed record ListarUsuariosParaAgenda : IRequisicao<Resultado<IReadOnlyList<UsuarioDaAgenda>>>;

public sealed class ListarUsuariosParaAgendaManipulador(IContextoDeAcesso contexto, IUsuarioRepositorio usuarios)
    : IManipulador<ListarUsuariosParaAgenda, Resultado<IReadOnlyList<UsuarioDaAgenda>>>
{
    public async Task<Resultado<IReadOnlyList<UsuarioDaAgenda>>> ManipularAsync(
        ListarUsuariosParaAgenda requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is null)
            return Resultado<IReadOnlyList<UsuarioDaAgenda>>.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        var ativos = await usuarios.ListarNomesDosAtivosAsync(ct);
        return Resultado<IReadOnlyList<UsuarioDaAgenda>>.Ok(
            [.. ativos.Select(u => new UsuarioDaAgenda(u.Id, u.Nome)).OrderBy(u => u.Nome, StringComparer.CurrentCultureIgnoreCase)]);
    }
}
