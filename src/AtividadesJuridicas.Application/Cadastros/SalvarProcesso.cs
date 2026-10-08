using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Application.Cadastros;

/// <summary>Cria o processo quando <paramref name="Id"/> é nulo; caso contrário, altera os dados e os clientes.</summary>
public sealed record SalvarProcesso(
    Guid? Id, DadosDoProcessoEntrada Dados, IReadOnlyList<ClienteDoProcessoEntrada> Clientes)
    : IRequisicao<Resultado<Guid>>;

public sealed class SalvarProcessoManipulador(
    IContextoDeAcesso contexto,
    IProcessoRepositorio processos,
    IAreaAtuacaoRepositorio areas,
    IClienteRepositorio clientes,
    IUnidadeDeTrabalho unidade,
    IAuditoria auditoria) : IManipulador<SalvarProcesso, Resultado<Guid>>
{
    private const string ProcessoAlterado = "O processo foi alterado por outra pessoa. Recarregue e tente novamente.";
    private const string NumeroEmUso = "Já existe um processo com este número.";

    public async Task<Resultado<Guid>> ManipularAsync(SalvarProcesso requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Falha(TipoErro.AcessoNegado, "Acesso negado.");

        Processo? existente = null;
        if (requisicao.Id is { } id)
        {
            existente = await processos.ObterPorIdAsync(id, ct);
            if (existente is null) return Falha(TipoErro.NaoEncontrado, "Processo não encontrado.");
        }

        // Tudo é verificado antes de qualquer alteração, para não deixar o processo pela metade.
        NumeroCnj? numero = null;
        if (!string.IsNullOrWhiteSpace(requisicao.Dados.NumeroCnj) &&
            !NumeroCnj.TentarCriar(requisicao.Dados.NumeroCnj, out numero))
            return Falha(TipoErro.Validacao, "Número CNJ inválido.");

        if (numero is not null && await processos.ExisteNumeroCnjAsync(numero, existente?.Id, ct))
            return Falha(TipoErro.Conflito, NumeroEmUso);

        var area = await areas.ObterPorIdAsync(requisicao.Dados.AreaAtuacaoId, ct);
        if (area is null) return Falha(TipoErro.Validacao, "Área de atuação não encontrada.");

        // Área inativa não entra em processo novo nem em troca; quem já a tinha pode mantê-la.
        if (!area.Ativo && existente?.AreaAtuacaoId != area.Id)
            return Falha(TipoErro.Validacao, "A área de atuação está inativa.");

        var pedidos = requisicao.Clientes ?? [];
        var ids = pedidos.Select(c => c.ClienteId).Distinct().ToList();
        var encontrados = await clientes.ObterVariosAsync(ids, ct);
        if (encontrados.Count != ids.Count) return Falha(TipoErro.Validacao, "Cliente não encontrado.");

        // Cliente inativo não entra em vínculo novo; quem já estava no processo pode permanecer.
        var jaVinculados = existente?.Clientes.Select(c => c.ClienteId).ToHashSet() ?? [];
        var inativoNovo = encontrados.FirstOrDefault(c => !c.Ativo && !jaVinculados.Contains(c.Id));
        if (inativoNovo is not null)
            return Falha(TipoErro.Validacao, $"O cliente {inativoNovo.Nome} está inativo.");

        try
        {
            var d = requisicao.Dados;
            var dados = new DadosDoProcesso(
                numero, d.Titulo, d.AreaAtuacaoId, d.Orgao, d.Comarca, d.ParteContraria,
                d.DataDistribuicao, d.ValorCausa, d.Observacoes);
            var vinculos = pedidos.Select(c => new ClienteDoProcesso(c.ClienteId, c.Polo)).ToList();

            if (existente is null)
            {
                existente = Processo.Criar(dados, vinculos);
                processos.Adicionar(existente);
            }
            else
            {
                // Cada chamada valida tudo antes de aplicar. Se a segunda falhar, o que a primeira aplicou
                // fica só na entidade em memória: nada é gravado, e o escopo da requisição é descartado.
                existente.Alterar(dados);
                var alteracao = existente.DefinirClientes(vinculos);

                if (alteracao.Houve)
                    auditoria.Anotar(EventosDeCadastro.ClientesDoProcessoAlterados, "Processo", existente.Id, new
                    {
                        adicionados = alteracao.Adicionados,
                        removidos = alteracao.Removidos,
                        poloAlterado = alteracao.PoloAlterado,
                    });
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
            return Falha(TipoErro.Conflito, ProcessoAlterado);
        }
        catch (ExcecaoDeConflito ex)
        {
            return Falha(TipoErro.Conflito,
                ex.Restricao?.Contains("numero_cnj", StringComparison.OrdinalIgnoreCase) == true
                    ? NumeroEmUso
                    : "Os clientes do processo não podem se repetir.");
        }
    }

    private static Resultado<Guid> Falha(TipoErro erro, string mensagem) => Resultado<Guid>.Falha(erro, mensagem);
}
