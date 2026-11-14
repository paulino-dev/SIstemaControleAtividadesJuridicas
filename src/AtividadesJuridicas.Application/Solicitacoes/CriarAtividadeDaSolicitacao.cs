using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Documentos;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Atividades;
using AtividadesJuridicas.Domain.Comum;
using AtividadesJuridicas.Domain.Documentos;
using AtividadesJuridicas.Domain.Solicitacoes;

namespace AtividadesJuridicas.Application.Solicitacoes;

/// <summary>
/// A Diretoria cria uma atividade a partir de uma solicitação do cliente; o valor devolvido é o Id da atividade nova. Os
/// campos da atividade são os de <see cref="SalvarAtividade"/>, sem o cliente: é sempre o da solicitação.
/// <paramref name="VersaoDaSolicitacao"/>: a versão com que o formulário leu a solicitação; se ela mudou, a criação é
/// recusada, mesmo com <paramref name="ConfirmouSolicitacaoCancelada"/>, que dispensa só o aviso de cancelamento.
/// <paramref name="AnexarDescricaoComoComentario"/>: cria na atividade um comentário com a descrição da solicitação.
/// <paramref name="DocumentosDaSolicitacao"/>: os documentos marcados, que viram documentos da atividade.
/// </summary>
public sealed record CriarAtividadeDaSolicitacao(
    Guid SolicitacaoId, uint VersaoDaSolicitacao, string Titulo, string Descricao, DateOnly? DataEntrega,
    Guid? ProcessoId, Guid? AreaAtuacaoId, int Complexidade, bool AnexarDescricaoComoComentario,
    IReadOnlyList<Guid> DocumentosDaSolicitacao, bool ConfirmouSolicitacaoCancelada = false)
    : IRequisicao<Resultado<Guid>>;

public sealed class CriarAtividadeDaSolicitacaoManipulador(
    IContextoDeAcesso contexto,
    ISolicitacaoRepositorio solicitacoes,
    IAtividadeRepositorio atividades,
    IClienteRepositorio clientes,
    IProcessoRepositorio processos,
    IAreaAtuacaoRepositorio areas,
    IDocumentoRepositorio documentos,
    IComentarioRepositorio comentarios,
    IRelogio relogio,
    IUnidadeDeTrabalho unidade) : IManipulador<CriarAtividadeDaSolicitacao, Resultado<Guid>>
{
    public async Task<Resultado<Guid>> ManipularAsync(CriarAtividadeDaSolicitacao requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true } ator)
            return Falha(TipoErro.AcessoNegado, "Acesso negado.");

        // A solicitação vem rastreada: a gravação dela confere a versão (xmin), e é isso que faz duas criações
        // simultâneas não passarem as duas.
        var solicitacao = await solicitacoes.ObterPorIdAsync(requisicao.SolicitacaoId, ct);
        if (solicitacao is null) return Falha(TipoErro.NaoEncontrado, SolicitacoesOpcoes.NaoEncontrada);

        if (solicitacao.Situacao == SituacaoDaSolicitacao.EmAtividade)
            return Falha(TipoErro.Conflito, SolicitacaoDoCliente.JaVirouAtividade);
        if (solicitacao.Situacao == SituacaoDaSolicitacao.Cancelada && !requisicao.ConfirmouSolicitacaoCancelada)
            return Falha(TipoErro.ConfirmacaoNecessaria, SolicitacaoDoCliente.CanceladaPeloCliente);
        if (requisicao.VersaoDaSolicitacao != solicitacao.Versao)
            return Falha(TipoErro.Conflito, SolicitacoesOpcoes.AlteradaDuranteACriacao);

        // Só os documentos vivos desta solicitação. Um repetido na lista vale uma vez; sem lista (nulo), nenhum.
        var marcados = new List<Documento>();
        foreach (var id in (requisicao.DocumentosDaSolicitacao ?? []).Distinct())
        {
            var documento = await documentos.ObterPorIdAsync(id, ct);
            if (documento is not { Categoria: CategoriaDocumento.DocumentoDaSolicitacao } ||
                documento.SolicitacaoId != solicitacao.Id)
                return Falha(TipoErro.Validacao, SolicitacoesOpcoes.DocumentoForaDaSolicitacao);
            marcados.Add(documento);
        }

        var (erro, processo) = await ValidacaoDoVinculo.ValidarAsync(
            clientes, processos, areas, solicitacao.ClienteId, requisicao.ProcessoId, requisicao.AreaAtuacaoId, null, ct);
        if (erro is not null) return Falha(TipoErro.Validacao, erro);

        try
        {
            // Tudo o que o domínio pode recusar é montado antes de qualquer coisa mudar ou entrar nos repositórios:
            // recusado, nada fica pela metade.
            var dados = new DadosDaAtividade(
                requisicao.Titulo, requisicao.Descricao, requisicao.DataEntrega, requisicao.Complexidade);
            var vinculo = VinculoDaAtividade.Criar(solicitacao.ClienteId, processo, requisicao.AreaAtuacaoId);
            var atividade = Atividade.Criar(
                dados, vinculo, TipoAutor.Usuario, ator.Id, relogio.AgoraUtc, solicitacao.Id);

            var comentario = requisicao.AnexarDescricaoComoComentario
                ? AtividadeComentario.Criar(atividade.Id, ator.Id, solicitacao.Descricao, relogio.AgoraUtc)
                : null;
            var levados = marcados.Select(d => d.LevarParaAtividade(atividade.Id, solicitacao.ClienteId)).ToList();

            solicitacao.VincularAtividade(atividade, requisicao.ConfirmouSolicitacaoCancelada);

            // Uma só gravação: a atividade, o vínculo, o comentário e os documentos entram juntos ou não entram.
            atividades.Adicionar(atividade);
            if (comentario is not null) comentarios.Adicionar(comentario);
            foreach (var levado in levados) documentos.Adicionar(levado);

            await unidade.SalvarAsync(ct);
            return Resultado<Guid>.Ok(atividade.Id);
        }
        catch (ExcecaoDeDominio ex)
        {
            return Falha(TipoErro.Validacao, ex.Message);
        }
        catch (ExcecaoDeConflito)
        {
            // O índice único de atividades.solicitacao_id: outra criação gravou a atividade desta solicitação antes.
            return Falha(TipoErro.Conflito, SolicitacaoDoCliente.JaVirouAtividade);
        }
        catch (ExcecaoDeConcorrencia)
        {
            // A solicitação mudou (o cliente a alterou, ou outra criação a vinculou) entre a leitura e a gravação.
            return Falha(TipoErro.Conflito, SolicitacoesOpcoes.AlteradaDuranteACriacao);
        }
    }

    private static Resultado<Guid> Falha(TipoErro erro, string mensagem) => Resultado<Guid>.Falha(erro, mensagem);
}
