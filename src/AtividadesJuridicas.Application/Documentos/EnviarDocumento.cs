using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Documentos;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.Application.Documentos;

/// <summary>
/// Envia um documento geral, um contrato de honorários ou um documento do cliente (visível a ele no
/// portal). <paramref name="NomeArquivo"/> é o nome que o navegador informou: só o último segmento é
/// guardado, e ele nunca é usado como caminho.
/// </summary>
public sealed record EnviarDocumento(
    CategoriaDocumento Categoria, Guid ClienteId, string? Nome, string NomeArquivo, string? TipoConteudo, Stream Conteudo)
    : IRequisicao<Resultado<Guid>>;

public sealed class EnviarDocumentoManipulador(
    IContextoDeAcesso contexto,
    IDocumentoRepositorio documentos,
    IClienteRepositorio clientes,
    IPerfilRepositorio perfis,
    IArmazenamentoDeArquivos armazenamento,
    IUnidadeDeTrabalho unidade) : IManipulador<EnviarDocumento, Resultado<Guid>>
{
    public async Task<Resultado<Guid>> ManipularAsync(EnviarDocumento requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Falha(TipoErro.AcessoNegado, "Acesso negado.");

        if (requisicao.Categoria is not (CategoriaDocumento.DocumentoGeral or CategoriaDocumento.ContratoHonorarios
                or CategoriaDocumento.DocumentoDoCliente))
            return Falha(TipoErro.Validacao, "Categoria inválida para envio.");

        if (ArquivoEnviado.Validar(requisicao.NomeArquivo, requisicao.Conteudo, requisicao.Nome, out var nomeDoArquivo) is { } recusa)
            return Falha(TipoErro.Validacao, recusa);

        if (await clientes.ObterPorIdAsync(requisicao.ClienteId, ct) is null)
            return Falha(TipoErro.Validacao, "Cliente não encontrado.");

        return await ArquivoEnviado.GravarAsync(
            armazenamento, documentos, unidade, nomeDoArquivo, requisicao.TipoConteudo, requisicao.Conteudo,
            // O Documento Geral nasce com os perfis internos: o perfil Cliente nunca entra na lista de acesso.
            // O contrato e o documento do cliente nascem sem ninguém na lista.
            async arquivo => requisicao.Categoria switch
            {
                CategoriaDocumento.DocumentoGeral => Documento.CriarGeral(requisicao.ClienteId, requisicao.Nome, arquivo,
                    [.. (await perfis.ObterAtivosAsync(ct)).Where(p => p.Tipo != TipoPerfil.Cliente)]),
                CategoriaDocumento.DocumentoDoCliente => Documento.CriarDoCliente(requisicao.ClienteId, requisicao.Nome, arquivo),
                _ => Documento.CriarContratoDeHonorarios(requisicao.ClienteId, requisicao.Nome, arquivo),
            },
            ct);
    }

    private static Resultado<Guid> Falha(TipoErro erro, string mensagem) => Resultado<Guid>.Falha(erro, mensagem);
}
