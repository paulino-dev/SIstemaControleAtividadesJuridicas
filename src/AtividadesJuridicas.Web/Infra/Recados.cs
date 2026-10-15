using AtividadesJuridicas.Application.Agenda;
using AtividadesJuridicas.Web.Components.Layout;

namespace AtividadesJuridicas.Web.Infra;

/// <summary>
/// Aviso que atravessa uma navegação: a tela de origem deixa ("Usuário salvo."), a de destino mostra uma vez.
/// Vive no escopo do circuito, então só vale para a navegação dentro da mesma conexão.
/// </summary>
public sealed class Recados
{
    private Recado? _pendente;

    /// <summary>
    /// Deixa o recado para a próxima tela. <paramref name="sobreposicoes"/>: os conflitos de agenda do evento salvo, que
    /// a agenda e a ficha do evento mostram numa redação por visão (<c>AvisoDeEventoSalvo</c>).
    /// </summary>
    public void Deixar(
        string texto, TipoDeAviso tipo = TipoDeAviso.Sucesso, IReadOnlyList<SobreposicaoDeAgenda>? sobreposicoes = null) =>
        _pendente = new Recado(texto, tipo, sobreposicoes);

    /// <summary>O recado deixado, uma vez só; nulo quando não há.</summary>
    public Recado? Retirar()
    {
        var recado = _pendente;
        _pendente = null;
        return recado;
    }
}

public sealed record Recado(string Texto, TipoDeAviso Tipo, IReadOnlyList<SobreposicaoDeAgenda>? Sobreposicoes = null);
