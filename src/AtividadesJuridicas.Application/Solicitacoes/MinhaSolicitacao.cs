using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Solicitacoes;

namespace AtividadesJuridicas.Application.Solicitacoes;

/// <summary>
/// O que os comandos do cliente sobre uma solicitação que já existe têm em comum. Cada um começa pelo ator (um usuário
/// de cliente, com o cliente dele) e só alcança a solicitação do cliente do ator: para qualquer outra, ela não existe.
/// </summary>
internal static class MinhaSolicitacao
{
    public const string AcessoNegado = "Acesso negado.";

    /// <summary>
    /// A solicitação viva do cliente. Nula quando não existe, foi excluída ou é de outro cliente: o cliente do ator é o
    /// único que vale, e a resposta é a mesma para todos esses casos.
    /// </summary>
    public static async Task<SolicitacaoDoCliente?> ObterDoClienteAsync(
        ISolicitacaoRepositorio solicitacoes, Guid id, Guid clienteId, CancellationToken ct)
    {
        var solicitacao = await solicitacoes.ObterPorIdAsync(id, ct);
        return solicitacao is not null && solicitacao.ClienteId == clienteId ? solicitacao : null;
    }

    /// <summary>
    /// Carrega a solicitação do cliente do ator e confere, nesta ordem, quem abriu e a versão com que a tela foi aberta.
    /// Devolve a solicitação ou a recusa.
    /// </summary>
    public static async Task<(SolicitacaoDoCliente? Solicitacao, Resultado? Recusa)> CarregarParaAlterarAsync(
        ISolicitacaoRepositorio solicitacoes, UsuarioAutenticado ator, Guid clienteId, Guid id, uint versaoEsperada,
        CancellationToken ct)
    {
        var solicitacao = await ObterDoClienteAsync(solicitacoes, id, clienteId, ct);
        if (solicitacao is null) return (null, NaoEncontrada());

        return Conferir(ator, solicitacao, versaoEsperada) is { } recusa ? (null, recusa) : (solicitacao, null);
    }

    /// <summary>Só quem abriu a solicitação a altera (<see cref="TipoErro.Validacao"/>) e só com a versão que a tela leu (<see cref="TipoErro.Conflito"/>).</summary>
    public static Resultado? Conferir(UsuarioAutenticado ator, SolicitacaoDoCliente solicitacao, uint versaoEsperada)
    {
        if (!solicitacao.EhDoSolicitante(ator.Id))
            return Resultado.Falha(TipoErro.Validacao, SolicitacoesOpcoes.SoQuemAbriu);

        if (versaoEsperada != solicitacao.Versao)
            return Resultado.Falha(TipoErro.Conflito, SolicitacoesOpcoes.Alterada);

        return null;
    }

    public static Resultado NaoEncontrada() => Resultado.Falha(TipoErro.NaoEncontrado, SolicitacoesOpcoes.NaoEncontrada);

    /// <summary>Outra gravação passou na frente: o mesmo aviso de versão antiga.</summary>
    public static Resultado Alterada() => Resultado.Falha(TipoErro.Conflito, SolicitacoesOpcoes.Alterada);

    /// <summary>A mesma recusa, na forma de resposta de um comando que devolve um valor.</summary>
    public static Resultado<T> Como<T>(this Resultado recusa) => Resultado<T>.Falha(recusa.Erro!.Value, recusa.Mensagem!);
}
