using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Atividades;

namespace AtividadesJuridicas.Application.Atividades;

/// <summary>A regra de quem vê uma atividade e as respostas comuns dos casos de uso.</summary>
internal static class AcessoAAtividade
{
    public const string NaoEncontrada = "Atividade não encontrada.";
    public const string Alterada = "A atividade foi alterada por outra pessoa. Recarregue e tente novamente.";

    /// <summary>A Diretoria vê todas; os demais, só as atividades em que são responsáveis.</summary>
    public static bool PodeVer(UsuarioAutenticado ator, Atividade atividade) =>
        ator.EhDiretoria || atividade.EhResponsavel(ator.Id);

    /// <summary>O usuário como a atividade o conhece: o autenticado é sempre um usuário ativo.</summary>
    public static PessoaDaAtividade Pessoa(UsuarioAutenticado ator) => new(ator.Id, ator.TipoPerfil, true);

    /// <summary>Para quem pediu, a atividade não existe; a tentativa fica registrada.</summary>
    public static Task RegistrarNegadoAsync(IAuditoria auditoria, Guid atividadeId, string requisicao) =>
        auditoria.RegistrarAsync(
            EventosDeSeguranca.AcessoNegado, "Atividade", atividadeId,
            new { requisicao, resposta = "nao_encontrado" }, ct: CancellationToken.None);
}
