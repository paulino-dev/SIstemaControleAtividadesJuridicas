namespace AtividadesJuridicas.Application.Identidade;

/// <summary>Id do usuário autenticado na requisição em curso; nulo quando anônimo ou sistema.</summary>
public interface IIdentidadeAtual
{
    Guid? UsuarioId { get; }
}

/// <summary>Preenchida pelo despachante no início de cada escopo de requisição.</summary>
public sealed class IdentidadeAtual : IIdentidadeAtual
{
    public Guid? UsuarioId { get; private set; }

    public void Definir(Guid? usuarioId) => UsuarioId = usuarioId;
}
