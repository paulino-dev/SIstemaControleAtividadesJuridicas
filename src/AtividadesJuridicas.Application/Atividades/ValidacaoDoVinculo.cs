using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Domain.Atividades;
using AtividadesJuridicas.Domain.Cadastros;

namespace AtividadesJuridicas.Application.Atividades;

/// <summary>
/// O que cliente, processo e área têm de cumprir para entrar no vínculo de uma atividade. É a conferência de quem salva
/// uma atividade e de quem a cria a partir de uma solicitação: as duas recusam com as mesmas mensagens.
/// </summary>
internal static class ValidacaoDoVinculo
{
    /// <summary>
    /// Cliente e área inativos não entram em vínculo novo; quem já os tinha (<paramref name="existente"/>) pode mantê-los.
    /// Devolve a mensagem de validação (nulo se válido) e o processo carregado, quando informado.
    /// </summary>
    public static async Task<(string? Erro, Processo? Processo)> ValidarAsync(
        IClienteRepositorio clientes, IProcessoRepositorio processos, IAreaAtuacaoRepositorio areas,
        Guid? clienteId, Guid? processoId, Guid? areaId, Atividade? existente, CancellationToken ct)
    {
        if (clienteId is { } idDoCliente)
        {
            var cliente = await clientes.ObterPorIdAsync(idDoCliente, ct);
            if (cliente is null) return ("Cliente não encontrado.", null);
            if (!cliente.Ativo && existente?.ClienteId != cliente.Id) return ("O cliente está inativo.", null);
        }

        Processo? processo = null;
        if (processoId is { } idDoProcesso)
        {
            processo = await processos.ObterPorIdAsync(idDoProcesso, ct);
            if (processo is null) return ("Processo não encontrado.", null);
        }
        else if (areaId is { } idDaArea)
        {
            var area = await areas.ObterPorIdAsync(idDaArea, ct);
            if (area is null) return ("Área de atuação não encontrada.", null);
            if (!area.Ativo && existente?.AreaAtuacaoId != area.Id) return ("A área de atuação está inativa.", null);
        }

        return (null, processo);
    }
}
