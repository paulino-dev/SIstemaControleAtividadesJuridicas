using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Agenda;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Application.Agenda;

/// <summary>
/// Cria o evento quando <paramref name="Id"/> é nulo (quem cria é o criador e entra como participante); caso contrário,
/// altera os dados, os vínculos e os participantes. <paramref name="VersaoEsperada"/>: na edição, a versão com que o
/// formulário foi aberto (nulo não confere).
/// </summary>
public sealed record SalvarEvento(
    Guid? Id, string Titulo, string? Descricao, string? Local, TipoEvento Tipo, DateTime InicioEm,
    int DuracaoEstimadaMinutos, Guid? ClienteId, Guid? ProcessoId, Guid? AtividadeId, IReadOnlyList<Guid> Participantes,
    uint? VersaoEsperada = null)
    : IRequisicao<Resultado<EventoSalvo>>;

public sealed class SalvarEventoManipulador(
    IContextoDeAcesso contexto,
    IEventoRepositorio eventos,
    IUsuarioRepositorio usuarios,
    IClienteRepositorio clientes,
    IProcessoRepositorio processos,
    IAtividadeRepositorio atividades,
    IUnidadeDeTrabalho unidade,
    IAuditoria auditoria) : IManipulador<SalvarEvento, Resultado<EventoSalvo>>
{
    public async Task<Resultado<EventoSalvo>> ManipularAsync(SalvarEvento requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { } ator)
            return Falha(TipoErro.AcessoNegado, "Acesso negado.");

        Evento? existente = null;
        if (requisicao.Id is { } id)
        {
            existente = await eventos.ObterPorIdAsync(id, ct);
            if (existente is null) return Falha(TipoErro.NaoEncontrado, AcessoAoEvento.NaoEncontrado);
            if (!AcessoAoEvento.PodeVer(ator, existente))
            {
                await AcessoAoEvento.RegistrarNegadoAsync(auditoria, existente.Id, nameof(SalvarEvento));
                return Falha(TipoErro.NaoEncontrado, AcessoAoEvento.NaoEncontrado);
            }

            if (!AcessoAoEvento.PodeGerenciar(ator, existente)) return Falha(TipoErro.AcessoNegado, "Acesso negado.");
            if (requisicao.VersaoEsperada is { } esperada && esperada != existente.Versao)
                return Falha(TipoErro.Conflito, AcessoAoEvento.Alterado);
        }

        if (requisicao.InicioEm.Kind != DateTimeKind.Utc)
            return Falha(TipoErro.Validacao, "O início do evento deve estar em UTC.");

        if (requisicao.Participantes is null) return Falha(TipoErro.Validacao, "Informe os participantes.");

        // Os participantes pedidos têm de existir; quem é novo e está inativo é recusado pelo domínio.
        var idsPedidos = requisicao.Participantes.Distinct().ToList();
        var encontrados = (await usuarios.ObterVariosAsync(idsPedidos, ct)).ToDictionary(u => u.Id);
        if (encontrados.Count != idsPedidos.Count) return Falha(TipoErro.Validacao, "Usuário não encontrado.");
        var pessoas = requisicao.Participantes.Select(p => new PessoaDoEvento(p, encontrados[p].Ativo)).ToList();

        // O vínculo (cliente, processo e atividade) só é revalidado quando muda: se o pedido traz o mesmo que o evento já
        // tem, ele é mantido como está, ainda que o cliente tenha saído do processo, o cliente tenha sido inativado ou o
        // editor não veja mais a atividade. Se qualquer um dos três muda, o vínculo novo é validado por inteiro.
        var vinculoMantido = existente is not null
            && existente.ClienteId == requisicao.ClienteId
            && existente.ProcessoId == requisicao.ProcessoId
            && existente.AtividadeId == requisicao.AtividadeId;

        if (!vinculoMantido && requisicao.ClienteId is { } clienteId && existente?.ClienteId != clienteId)
        {
            var cliente = await clientes.ObterPorIdAsync(clienteId, ct);
            if (cliente is null) return Falha(TipoErro.Validacao, "Cliente não encontrado.");
            if (!cliente.Ativo) return Falha(TipoErro.Validacao, "O cliente está inativo.");
        }

        Processo? processo = null;
        if (!vinculoMantido && requisicao.ProcessoId is { } processoId)
        {
            processo = await processos.ObterPorIdAsync(processoId, ct);
            if (processo is null) return Falha(TipoErro.Validacao, "Processo não encontrado.");
        }

        if (!vinculoMantido && requisicao.AtividadeId is { } atividadeId && existente?.AtividadeId != atividadeId)
        {
            // Quem não pode ver a atividade a recebe como se não existisse.
            var atividade = await atividades.ObterPorIdAsync(atividadeId, ct);
            if (atividade is null || !AcessoAAtividade.PodeVer(ator, atividade))
                return Falha(TipoErro.Validacao, "Atividade não encontrada.");
        }

        try
        {
            var dados = new DadosDoEvento(
                requisicao.Titulo, requisicao.Descricao, requisicao.Local, requisicao.Tipo, requisicao.InicioEm,
                requisicao.DuracaoEstimadaMinutos);

            if (existente is null)
            {
                var vinculo = VinculoDoEvento.Criar(requisicao.ClienteId, processo, requisicao.AtividadeId);
                existente = Evento.Criar(dados, vinculo, new PessoaDoEvento(ator.Id, true), pessoas);
                eventos.Adicionar(existente);
            }
            else
            {
                if (vinculoMantido) existente.Alterar(dados);
                else existente.Alterar(dados, VinculoDoEvento.Criar(requisicao.ClienteId, processo, requisicao.AtividadeId));

                // Só participantes mudaram? O evento também fica com a marca na auditoria (como as atividades fazem
                // com os responsáveis), além das linhas de cada participante.
                var alteracao = existente.DefinirParticipantes(pessoas);
                if (alteracao.Houve)
                    auditoria.Anotar(EventosDeAgenda.ParticipantesAlterados, "Evento", existente.Id, new
                    {
                        adicionados = alteracao.Adicionados,
                        removidos = alteracao.Removidos,
                    });
            }

            // Aviso, não impedimento: quem dos participantes finais já tem outro evento nesse horário.
            var participantes = existente.Participantes.Select(p => p.UsuarioId).ToList();
            var sobreposicoes = (await eventos.SobreposicoesAsync(
                    existente.Id, participantes, existente.InicioEm, existente.FimEm, ct))
                .OrderBy(s => s.UsuarioNome, StringComparer.CurrentCultureIgnoreCase).ThenBy(s => s.InicioEm)
                .ToList();

            await unidade.SalvarAsync(ct);
            return Resultado<EventoSalvo>.Ok(new EventoSalvo(existente.Id, sobreposicoes));
        }
        catch (ExcecaoDeDominio ex)
        {
            return Falha(TipoErro.Validacao, ex.Message);
        }
        catch (ExcecaoDeConflito)
        {
            return Falha(TipoErro.Conflito, AcessoAoEvento.Alterado);
        }
        catch (ExcecaoDeConcorrencia)
        {
            return Falha(TipoErro.Conflito, AcessoAoEvento.Alterado);
        }
    }

    private static Resultado<EventoSalvo> Falha(TipoErro erro, string mensagem) =>
        Resultado<EventoSalvo>.Falha(erro, mensagem);
}
