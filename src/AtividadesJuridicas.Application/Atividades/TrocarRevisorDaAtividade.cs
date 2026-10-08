using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Application.Atividades;

public sealed record TrocarRevisorDaAtividade(Guid Id, Guid RevisorId) : IRequisicao<Resultado>;

public sealed class TrocarRevisorDaAtividadeManipulador(
    IContextoDeAcesso contexto,
    IAtividadeRepositorio atividades,
    IUsuarioRepositorio usuarios,
    IPerfilRepositorio perfis,
    IUnidadeDeTrabalho unidade) : IManipulador<TrocarRevisorDaAtividade, Resultado>
{
    public async Task<Resultado> ManipularAsync(TrocarRevisorDaAtividade requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Resultado.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        var atividade = await atividades.ObterPorIdAsync(requisicao.Id, ct);
        if (atividade is null) return Resultado.Falha(TipoErro.NaoEncontrado, AcessoAAtividade.NaoEncontrada);

        var atuais = atividade.Responsaveis.Select(r => r.UsuarioId).ToList();
        var pessoas = await PessoasDaAtividade.CarregarAsync(usuarios, perfis, [.. atuais, requisicao.RevisorId], ct);
        if (pessoas is null) return Resultado.Falha(TipoErro.Validacao, "Usuário não encontrado.");

        try
        {
            atividade.TrocarRevisor(pessoas[requisicao.RevisorId], [.. atuais.Select(id => pessoas[id])]);
            await unidade.SalvarAsync(ct);
            return Resultado.Ok();
        }
        catch (ExcecaoDeDominio ex)
        {
            return Resultado.Falha(TipoErro.Validacao, ex.Message);
        }
        catch (ExcecaoDeConcorrencia)
        {
            return Resultado.Falha(TipoErro.Conflito, AcessoAAtividade.Alterada);
        }
    }
}
