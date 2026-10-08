using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Documentos;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Atividades;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Application.Atividades;

/// <summary>
/// Cria a atividade quando <paramref name="Id"/> é nulo; caso contrário, altera os dados e o vínculo.
/// <paramref name="VersaoEsperada"/>: na edição, a versão com que o formulário foi aberto (nulo não confere).
/// </summary>
public sealed record SalvarAtividade(
    Guid? Id, string Titulo, string Descricao, DateOnly? DataEntrega, Guid? ClienteId, Guid? ProcessoId, Guid? AreaAtuacaoId,
    uint? VersaoEsperada = null)
    : IRequisicao<Resultado<Guid>>;

public sealed class SalvarAtividadeManipulador(
    IContextoDeAcesso contexto,
    IAtividadeRepositorio atividades,
    IClienteRepositorio clientes,
    IProcessoRepositorio processos,
    IAreaAtuacaoRepositorio areas,
    IRelogio relogio,
    IUnidadeDeTrabalho unidade,
    IDocumentoRepositorio documentos) : IManipulador<SalvarAtividade, Resultado<Guid>>
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
            if (requisicao.VersaoEsperada is { } esperada && esperada != existente.Versao)
                return Falha(TipoErro.Conflito, AcessoAAtividade.Alterada);
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

                // Os documentos da atividade acompanham o cliente dela, na mesma gravação. Em toda edição, para que
                // um documento que ficou no cliente errado volte a ficar certo; o que já está alinhado não é tocado.
                foreach (var documento in await documentos.ObterDaAtividadeAsync(existente.Id, ct))
                    if (documento.ClienteId != existente.ClienteId)
                        documento.AcompanharClienteDaAtividade(existente.ClienteId);
            }

            await unidade.SalvarAsync(ct);
            return Resultado<Guid>.Ok(existente.Id);
        }
        catch (ExcecaoDeDominio ex)
        {
            return Falha(TipoErro.Validacao, ex.Message);
        }
        catch (ExcecaoDeConflito)
        {
            return Falha(TipoErro.Conflito, AcessoAAtividade.Alterada);
        }
        catch (ExcecaoDeConcorrencia)
        {
            return Falha(TipoErro.Conflito, AcessoAAtividade.Alterada);
        }
    }

    private static Resultado<Guid> Falha(TipoErro erro, string mensagem) => Resultado<Guid>.Falha(erro, mensagem);
}
