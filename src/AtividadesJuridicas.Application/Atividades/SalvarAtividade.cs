using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Atividades;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Application.Atividades;

/// <summary>Cria a atividade quando <paramref name="Id"/> é nulo; caso contrário, altera os dados e o vínculo.</summary>
public sealed record SalvarAtividade(
    Guid? Id, string Titulo, string Descricao, DateOnly? DataEntrega, Guid? ClienteId, Guid? ProcessoId, Guid? AreaAtuacaoId)
    : IRequisicao<Resultado<Guid>>;

public sealed class SalvarAtividadeManipulador(
    IContextoDeAcesso contexto,
    IAtividadeRepositorio atividades,
    IClienteRepositorio clientes,
    IProcessoRepositorio processos,
    IAreaAtuacaoRepositorio areas,
    IRelogio relogio,
    IUnidadeDeTrabalho unidade) : IManipulador<SalvarAtividade, Resultado<Guid>>
{
    public async Task<Resultado<Guid>> ManipularAsync(SalvarAtividade requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true } ator)
            return Falha(TipoErro.AcessoNegado, "Acesso negado.");

        Atividade? existente = null;
        if (requisicao.Id is { } id)
        {
            existente = await atividades.ObterPorIdAsync(id, ct);
            if (existente is null) return Falha(TipoErro.NaoEncontrado, AcessoAAtividade.NaoEncontrada);
        }

        // Cliente e área inativos não entram em vínculo novo; quem já os tinha pode mantê-los.
        if (requisicao.ClienteId is { } clienteId)
        {
            var cliente = await clientes.ObterPorIdAsync(clienteId, ct);
            if (cliente is null) return Falha(TipoErro.Validacao, "Cliente não encontrado.");
            if (!cliente.Ativo && existente?.ClienteId != cliente.Id)
                return Falha(TipoErro.Validacao, "O cliente está inativo.");
        }

        Processo? processo = null;
        if (requisicao.ProcessoId is { } processoId)
        {
            processo = await processos.ObterPorIdAsync(processoId, ct);
            if (processo is null) return Falha(TipoErro.Validacao, "Processo não encontrado.");
        }
        else if (requisicao.AreaAtuacaoId is { } areaId)
        {
            var area = await areas.ObterPorIdAsync(areaId, ct);
            if (area is null) return Falha(TipoErro.Validacao, "Área de atuação não encontrada.");
            if (!area.Ativo && existente?.AreaAtuacaoId != area.Id)
                return Falha(TipoErro.Validacao, "A área de atuação está inativa.");
        }

        try
        {
            var dados = new DadosDaAtividade(requisicao.Titulo, requisicao.Descricao, requisicao.DataEntrega);
            var vinculo = VinculoDaAtividade.Criar(requisicao.ClienteId, processo, requisicao.AreaAtuacaoId);

            if (existente is null)
            {
                existente = Atividade.Criar(dados, vinculo, TipoAutor.Usuario, ator.Id, relogio.AgoraUtc);
                atividades.Adicionar(existente);
            }
            else
            {
                existente.Alterar(dados, vinculo);
            }

            await unidade.SalvarAsync(ct);
            return Resultado<Guid>.Ok(existente.Id);
        }
        catch (ExcecaoDeDominio ex)
        {
            return Falha(TipoErro.Validacao, ex.Message);
        }
        catch (ExcecaoDeConcorrencia)
        {
            return Falha(TipoErro.Conflito, AcessoAAtividade.Alterada);
        }
    }

    private static Resultado<Guid> Falha(TipoErro erro, string mensagem) => Resultado<Guid>.Falha(erro, mensagem);
}
