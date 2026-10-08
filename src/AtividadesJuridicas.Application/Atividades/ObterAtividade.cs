using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Atividades;
using AtividadesJuridicas.Domain.Auditoria;

namespace AtividadesJuridicas.Application.Atividades;

public sealed record ObterAtividade(Guid Id) : IRequisicao<Resultado<AtividadeDetalhe>>, IConsultaAuditavel
{
    public string TipoRecurso => "Atividade";
    public AcaoDeAcesso Acao => AcaoDeAcesso.Visualizou;
    public Guid? RecursoId => Id;
}

public sealed class ObterAtividadeManipulador(
    IContextoDeAcesso contexto,
    IAtividadeRepositorio atividades,
    IUsuarioRepositorio usuarios,
    IClienteRepositorio clientes,
    IProcessoRepositorio processos,
    IAreaAtuacaoRepositorio areas,
    ICalendarioDoEscritorio calendario,
    IAuditoria auditoria) : IManipulador<ObterAtividade, Resultado<AtividadeDetalhe>>
{
    public async Task<Resultado<AtividadeDetalhe>> ManipularAsync(ObterAtividade requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { } ator)
            return Resultado<AtividadeDetalhe>.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        var atividade = await atividades.ObterPorIdAsync(requisicao.Id, ct);
        if (atividade is null)
            return Resultado<AtividadeDetalhe>.Falha(TipoErro.NaoEncontrado, AcessoAAtividade.NaoEncontrada);

        if (!AcessoAAtividade.PodeVer(ator, atividade))
        {
            await AcessoAAtividade.RegistrarNegadoAsync(auditoria, atividade.Id, nameof(ObterAtividade));
            return Resultado<AtividadeDetalhe>.Falha(TipoErro.NaoEncontrado, AcessoAAtividade.NaoEncontrada);
        }

        var idsDeUsuarios = atividade.Responsaveis.Select(r => r.UsuarioId)
            .Concat(atividade.Historico.Where(h => h.UsuarioId is not null).Select(h => h.UsuarioId!.Value))
            .Concat(new[] { atividade.AutorId, atividade.RevisorId }.Where(x => x is not null).Select(x => x!.Value))
            .Distinct().ToList();
        var nomes = (await usuarios.ObterVariosAsync(idsDeUsuarios, ct)).ToDictionary(u => u.Id, u => u.Nome);

        string? NomeDoUsuario(Guid? id) => id is { } x && nomes.TryGetValue(x, out var nome) ? nome : null;

        var cliente = atividade.ClienteId is { } c ? await clientes.ObterPorIdAsync(c, ct) : null;
        var processo = atividade.ProcessoId is { } p ? await processos.ObterPorIdAsync(p, ct) : null;
        var area = atividade.AreaAtuacaoId is { } a ? await areas.ObterPorIdAsync(a, ct) : null;

        var responsaveis = atividade.Responsaveis
            .Select(r =>
            {
                var visivel = ator.EhDiretoria || r.UsuarioId == ator.Id;
                return new ResponsavelDetalhe(
                    r.UsuarioId, NomeDoUsuario(r.UsuarioId) ?? string.Empty, visivel,
                    visivel ? r.Direcionamento : null, r.ConcluidoEm);
            })
            .OrderBy(r => r.Nome, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var historico = atividade.Historico
            .OrderBy(h => h.Ordem)
            .Select(h => new HistoricoDetalhe(h.StatusAnterior, h.StatusNovo, NomeDoUsuario(h.UsuarioId), h.DataHora, h.Motivo))
            .ToList();

        return Resultado<AtividadeDetalhe>.Ok(new AtividadeDetalhe(
            atividade.Id, atividade.Titulo, atividade.Descricao, atividade.DataEntrega, atividade.Status,
            atividade.EstaAtrasada(calendario.Hoje), atividade.ConcluidaEm,
            atividade.ClienteId, cliente?.Nome, atividade.ProcessoId, processo?.Titulo,
            atividade.AreaAtuacaoId, area?.Nome, atividade.TipoAutor, NomeDoUsuario(atividade.AutorId),
            atividade.RevisorId, NomeDoUsuario(atividade.RevisorId), responsaveis, historico,
            Acoes(ator, atividade), atividade.Versao));
    }

    private static AcoesDaAtividade Acoes(UsuarioAutenticado ator, Atividade atividade)
    {
        var parteAberta = atividade.Responsaveis.Any(r => r.UsuarioId == ator.Id && r.Pendente);

        return new AcoesDaAtividade(
            Editar: ator.EhDiretoria,
            DefinirResponsaveis: ator.EhDiretoria && atividade.PodeAlterarResponsaveis,
            TrocarRevisor: ator.EhDiretoria && atividade.PodeTrocarRevisor,
            ConcluirMinhaParte: parteAberta && atividade.Status is
                StatusAtividade.Direcionada or StatusAtividade.ParaCorrecao or StatusAtividade.Reaberta,
            AprovarOuDevolver: ator.EhDiretoria && atividade.RevisorId == ator.Id && atividade.Status is
                StatusAtividade.Feita or StatusAtividade.Corrigida,
            Reabrir: ator.EhDiretoria && atividade.Status == StatusAtividade.Concluida,
            Excluir: ator.EhDiretoria);
    }
}
