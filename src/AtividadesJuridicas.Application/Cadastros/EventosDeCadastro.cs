namespace AtividadesJuridicas.Application.Cadastros;

public static class EventosDeCadastro
{
    /// <summary>
    /// Os clientes de um processo mudaram. Anotado no processo, para que a mudança apareça ao filtrar
    /// a auditoria por "Processo"; cada vínculo tem ainda o seu próprio registro.
    /// </summary>
    public const string ClientesDoProcessoAlterados = "processo_clientes_alterados";
}
