using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Agenda;

namespace AtividadesJuridicas.Application.Agenda;

/// <summary>A regra de quem vê e quem gerencia um evento e as respostas comuns dos casos de uso.</summary>
internal static class AcessoAoEvento
{
    public const string NaoEncontrado = "Evento não encontrado.";
    public const string Alterado = "O evento foi alterado por outra pessoa. Recarregue e tente novamente.";

    /// <summary>A Diretoria vê todos; os demais, os eventos em que participam ou que criaram.</summary>
    public static bool PodeVer(UsuarioAutenticado ator, Evento evento) =>
        ator.EhDiretoria || evento.CriadorId == ator.Id || evento.Participa(ator.Id);

    /// <summary>Editar e excluir: a Diretoria e o criador.</summary>
    public static bool PodeGerenciar(UsuarioAutenticado ator, Evento evento) =>
        ator.EhDiretoria || evento.CriadorId == ator.Id;

    /// <summary>Para quem pediu, o evento não existe; a tentativa fica registrada.</summary>
    public static Task RegistrarNegadoAsync(IAuditoria auditoria, Guid eventoId, string requisicao) =>
        auditoria.RegistrarAsync(
            EventosDeSeguranca.AcessoNegado, "Evento", eventoId,
            new { requisicao, resposta = "nao_encontrado" }, ct: CancellationToken.None);
}
