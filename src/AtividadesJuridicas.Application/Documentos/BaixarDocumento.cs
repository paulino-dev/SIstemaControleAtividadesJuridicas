using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Documentos;

namespace AtividadesJuridicas.Application.Documentos;

public sealed record BaixarDocumento(Guid Id) : IRequisicao<Resultado<ArquivoParaBaixar>>, IConsultaAuditavel
{
    public string TipoRecurso => "Documento";
    public AcaoDeAcesso Acao => AcaoDeAcesso.Baixou;
    public Guid? RecursoId => Id;
}

public sealed class BaixarDocumentoManipulador(
    IContextoDeAcesso contexto,
    IDocumentoRepositorio documentos,
    IArmazenamentoDeArquivos armazenamento,
    IAuditoria auditoria,
    IResponsaveisDeAtividade responsaveis) : IManipulador<BaixarDocumento, Resultado<ArquivoParaBaixar>>
{
    private const string NaoEncontrado = "Documento não encontrado.";

    public async Task<Resultado<ArquivoParaBaixar>> ManipularAsync(BaixarDocumento requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { } ator)
            return Falha(TipoErro.AcessoNegado, "Acesso negado.");

        var documento = await documentos.ObterPorIdAsync(requisicao.Id, ct);
        if (documento is null) return Falha(TipoErro.NaoEncontrado, NaoEncontrado);

        // Só o documento de atividade pergunta pelos responsáveis, e a Diretoria não precisa da resposta.
        var ehResponsavel = documento is { Categoria: CategoriaDocumento.DocumentoAtividade, AtividadeId: { } atividadeId }
            && !ator.EhDiretoria
            && await responsaveis.EhResponsavelAsync(atividadeId, ator.Id, ct);

        if (!documento.PodeSerAcessadoPor(ator.Id, ator.PerfilId, ator.TipoPerfil, ehResponsavel))
        {
            // Para quem pediu, o documento não existe; para a Diretoria, a tentativa fica registrada.
            await auditoria.RegistrarAsync(
                EventosDeSeguranca.AcessoNegado, "Documento", documento.Id,
                new { requisicao = nameof(BaixarDocumento), resposta = "nao_encontrado" }, ct: CancellationToken.None);
            return Falha(TipoErro.NaoEncontrado, NaoEncontrado);
        }

        var conteudo = await armazenamento.AbrirAsync(documento.ChaveArmazenamento, ct);
        if (conteudo is null)
        {
            // Quem pode ver o documento e não consegue o arquivo está diante de uma falha do servidor, não de um 404.
            await auditoria.RegistrarAsync(
                EventosDeDocumento.ArquivoAusente, "Documento", documento.Id,
                new { chave = documento.ChaveArmazenamento }, ct: CancellationToken.None);
            throw new ExcecaoDeArquivoAusente(documento.Id, documento.ChaveArmazenamento);
        }

        return Resultado<ArquivoParaBaixar>.Ok(new ArquivoParaBaixar(documento.NomeParaBaixar(), documento.TamanhoBytes, conteudo));
    }

    private static Resultado<ArquivoParaBaixar> Falha(TipoErro erro, string mensagem) =>
        Resultado<ArquivoParaBaixar>.Falha(erro, mensagem);
}
