using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Atividades;
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Application.Atividades;

public sealed record ComentarAtividade(Guid AtividadeId, string Texto) : IRequisicao<Resultado<Guid>>;

public sealed class ComentarAtividadeManipulador(
    IContextoDeAcesso contexto,
    IAtividadeRepositorio atividades,
    IComentarioRepositorio comentarios,
    IRelogio relogio,
    IUnidadeDeTrabalho unidade,
    IAuditoria auditoria) : IManipulador<ComentarAtividade, Resultado<Guid>>
{
    public async Task<Resultado<Guid>> ManipularAsync(ComentarAtividade requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { } ator)
            return Resultado<Guid>.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        var atividade = await atividades.ObterPorIdAsync(requisicao.AtividadeId, ct);
        if (atividade is null) return Resultado<Guid>.Falha(TipoErro.NaoEncontrado, AcessoAAtividade.NaoEncontrada);

        if (!AcessoAAtividade.PodeVer(ator, atividade))
        {
            await AcessoAAtividade.RegistrarNegadoAsync(auditoria, atividade.Id, nameof(ComentarAtividade));
            return Resultado<Guid>.Falha(TipoErro.NaoEncontrado, AcessoAAtividade.NaoEncontrada);
        }

        try
        {
            var comentario = AtividadeComentario.Criar(atividade.Id, ator.Id, requisicao.Texto, relogio.AgoraUtc);
            comentarios.Adicionar(comentario);
            await unidade.SalvarAsync(ct);
            return Resultado<Guid>.Ok(comentario.Id);
        }
        catch (ExcecaoDeDominio ex)
        {
            return Resultado<Guid>.Falha(TipoErro.Validacao, ex.Message);
        }
    }
}
