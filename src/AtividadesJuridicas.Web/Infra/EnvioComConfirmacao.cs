namespace AtividadesJuridicas.Web.Infra;

/// <summary>
/// O envio em dois passos de uma área cujo conteúdo fica visível a todos os clientes: o primeiro clique só mostra o
/// aviso; o envio acontece na confirmação. O aviso vale para um envio só.
/// </summary>
public sealed class EnvioComConfirmacao(bool exigeConfirmacao)
{
    public bool AguardandoConfirmacao { get; private set; }

    /// <summary>Clique em "Enviar". Verdadeiro quando o envio segue já; falso quando o aviso passou a ser exibido.</summary>
    public bool Pedir()
    {
        if (!exigeConfirmacao) return true;

        AguardandoConfirmacao = true;
        return false;
    }

    /// <summary>Clique em "Confirmar envio". Verdadeiro só quando o aviso estava na tela.</summary>
    public bool Confirmar()
    {
        if (!AguardandoConfirmacao) return false;

        AguardandoConfirmacao = false;
        return true;
    }

    /// <summary>Clique em "Cancelar", ou troca do arquivo escolhido: o próximo envio avisa de novo.</summary>
    public void Cancelar() => AguardandoConfirmacao = false;
}
