using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Documentos;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Documentos;

namespace AtividadesJuridicas.Application.Atividades;

/// <summary>
/// Anexa um documento a uma atividade: quem a vê (a Diretoria e os responsáveis) pode anexar. O documento fica
/// também no cliente que a atividade tem agora. <paramref name="NomeArquivo"/> é o nome que o navegador
/// informou: só o último segmento é guardado, e ele nunca é usado como caminho.
/// </summary>
public sealed record EnviarDocumentoDaAtividade(
    Guid AtividadeId, string? Nome, string NomeArquivo, string? TipoConteudo, Stream Conteudo)
    : IRequisicao<Resultado<Guid>>;

public sealed class EnviarDocumentoDaAtividadeManipulador(
    IContextoDeAcesso contexto,
    IAtividadeRepositorio atividades,
    IDocumentoRepositorio documentos,
    IArmazenamentoDeArquivos armazenamento,
    IUnidadeDeTrabalho unidade,
    IAuditoria auditoria) : IManipulador<EnviarDocumentoDaAtividade, Resultado<Guid>>
{
    public async Task<Resultado<Guid>> ManipularAsync(EnviarDocumentoDaAtividade requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { } ator)
            return Falha(TipoErro.AcessoNegado, "Acesso negado.");

        var atividade = await atividades.ObterPorIdAsync(requisicao.AtividadeId, ct);
        if (atividade is null) return Falha(TipoErro.NaoEncontrado, AcessoAAtividade.NaoEncontrada);

        if (!AcessoAAtividade.PodeVer(ator, atividade))
        {
            await AcessoAAtividade.RegistrarNegadoAsync(auditoria, atividade.Id, nameof(EnviarDocumentoDaAtividade));
            return Falha(TipoErro.NaoEncontrado, AcessoAAtividade.NaoEncontrada);
        }

        if (ArquivoEnviado.Validar(requisicao.NomeArquivo, requisicao.Conteudo, requisicao.Nome, out var nomeDoArquivo) is { } recusa)
            return Falha(TipoErro.Validacao, recusa);

        return await ArquivoEnviado.GravarAsync(
            armazenamento, documentos, unidade, nomeDoArquivo, requisicao.TipoConteudo, requisicao.Conteudo,
            arquivo => Task.FromResult(Documento.CriarDeAtividade(atividade.Id, atividade.ClienteId, requisicao.Nome, arquivo)),
            ct);
    }

    private static Resultado<Guid> Falha(TipoErro erro, string mensagem) => Resultado<Guid>.Falha(erro, mensagem);
}
