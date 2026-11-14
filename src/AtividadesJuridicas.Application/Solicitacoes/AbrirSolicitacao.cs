using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Documentos;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Comum;
using AtividadesJuridicas.Domain.Documentos;
using AtividadesJuridicas.Domain.Solicitacoes;

namespace AtividadesJuridicas.Application.Solicitacoes;

/// <summary>
/// O cliente abre uma solicitação para o escritório, com até <see cref="SolicitacoesOpcoes.MaximoDeDocumentos"/>
/// documentos. Não há Id de cliente aqui: é sempre o do usuário que pede, e quem abre é o solicitante.
/// </summary>
public sealed record AbrirSolicitacao(string Descricao, UrgenciaDaSolicitacao Urgencia, IReadOnlyList<ArquivoParaEnviar> Arquivos)
    : IRequisicao<Resultado<Guid>>, IRequisicaoAbertaAoCliente;

public sealed class AbrirSolicitacaoManipulador(
    IContextoDeAcesso contexto,
    ISolicitacaoRepositorio solicitacoes,
    IDocumentoRepositorio documentos,
    IArmazenamentoDeArquivos armazenamento,
    IUnidadeDeTrabalho unidade) : IManipulador<AbrirSolicitacao, Resultado<Guid>>
{
    public async Task<Resultado<Guid>> ManipularAsync(AbrirSolicitacao requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhCliente: true, ClienteId: { } clienteId } ator)
            return Falha(TipoErro.AcessoNegado, MinhaSolicitacao.AcessoNegado);

        // O domínio cria a solicitação antes de qualquer arquivo ser gravado: dado inválido não grava nada.
        SolicitacaoDoCliente solicitacao;
        try
        {
            solicitacao = SolicitacaoDoCliente.Abrir(clienteId, ator.Id, requisicao.Descricao, requisicao.Urgencia);
        }
        catch (ExcecaoDeDominio ex)
        {
            return Falha(TipoErro.Validacao, ex.Message);
        }

        if (requisicao.Arquivos.Count > SolicitacoesOpcoes.MaximoDeDocumentos)
            return Falha(TipoErro.Validacao, SolicitacoesOpcoes.LimiteDeDocumentos);

        var gravacao = await ArquivoEnviado.GravarVariosAsync(
            armazenamento, unidade, requisicao.Arquivos,
            gravados =>
            {
                // Todos os documentos são criados antes de qualquer coisa entrar nos repositórios: se um for recusado, a
                // solicitação também não entra.
                var novos = gravados.Select(arquivo =>
                    Documento.CriarDaSolicitacao(solicitacao.Id, clienteId, null, arquivo)).ToList();

                solicitacoes.Adicionar(solicitacao);
                foreach (var documento in novos) documentos.Adicionar(documento);
                return Task.CompletedTask;
            },
            ct);

        return gravacao.Sucesso ? Resultado<Guid>.Ok(solicitacao.Id) : gravacao.Como<Guid>();
    }

    private static Resultado<Guid> Falha(TipoErro erro, string mensagem) => Resultado<Guid>.Falha(erro, mensagem);
}
