namespace AtividadesJuridicas.Domain.Comum;

/// <summary>Violação de uma regra do domínio. A mensagem é exibida ao usuário.</summary>
public sealed class ExcecaoDeDominio(string mensagem) : Exception(mensagem);
