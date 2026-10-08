using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Application.Cadastros;

/// <summary>Cria a área quando <paramref name="Id"/> é nulo; caso contrário, renomeia.</summary>
public sealed record SalvarAreaAtuacao(Guid? Id, string Nome) : IRequisicao<Resultado<Guid>>;

public sealed class SalvarAreaAtuacaoManipulador(
    IContextoDeAcesso contexto,
    IAreaAtuacaoRepositorio areas,
    IUnidadeDeTrabalho unidade) : IManipulador<SalvarAreaAtuacao, Resultado<Guid>>
{
    private const string NomeEmUso = "Já existe uma área com este nome.";

    public async Task<Resultado<Guid>> ManipularAsync(SalvarAreaAtuacao requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Resultado<Guid>.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        AreaAtuacao? existente = null;
        if (requisicao.Id is { } id)
        {
            existente = await areas.ObterPorIdAsync(id, ct);
            if (existente is null)
                return Resultado<Guid>.Falha(TipoErro.NaoEncontrado, "Área não encontrada.");
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(requisicao.Nome) &&
                await areas.ExisteNomeAsync(requisicao.Nome, existente?.Id, ct))
                return Resultado<Guid>.Falha(TipoErro.Conflito, NomeEmUso);

            if (existente is null)
            {
                existente = AreaAtuacao.Criar(requisicao.Nome);
                areas.Adicionar(existente);
            }
            else
            {
                existente.Renomear(requisicao.Nome);
            }

            await unidade.SalvarAsync(ct);
            return Resultado<Guid>.Ok(existente.Id);
        }
        catch (ExcecaoDeDominio ex)
        {
            return Resultado<Guid>.Falha(TipoErro.Validacao, ex.Message);
        }
        catch (ExcecaoDeConflito)
        {
            return Resultado<Guid>.Falha(TipoErro.Conflito, NomeEmUso);
        }
    }
}
