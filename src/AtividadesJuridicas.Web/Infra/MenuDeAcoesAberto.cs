namespace AtividadesJuridicas.Web.Infra;

/// <summary>
/// O menu de ações aberto agora no circuito. Abrir um menu fecha o que estava aberto antes: nunca ficam dois abertos,
/// nem quando o segundo se abre sem o foco sair do primeiro (no Safari o clique não foca o botão; o leitor de tela
/// aciona o botão sem mover o foco).
/// </summary>
public sealed class MenuDeAcoesAberto
{
    private object? _aberto;
    private Action? _fechar;

    /// <summary>Guarda o menu que acaba de abrir e fecha o que estava aberto antes dele (sem mexer no foco).</summary>
    public void Abrir(object menu, Action fechar)
    {
        var anterior = ReferenceEquals(_aberto, menu) ? null : _fechar;
        _aberto = menu;
        _fechar = fechar;
        anterior?.Invoke();
    }

    /// <summary>Esquece o menu que fechou (ou saiu da tela), se for ele o aberto.</summary>
    public void Esquecer(object menu)
    {
        if (!ReferenceEquals(_aberto, menu)) return;

        _aberto = null;
        _fechar = null;
    }
}
