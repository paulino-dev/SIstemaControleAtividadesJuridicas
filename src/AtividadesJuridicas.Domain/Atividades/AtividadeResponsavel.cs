using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Domain.Atividades;

/// <summary>Um responsável da atividade, com o direcionamento que só ele (e a Diretoria) vê e a conclusão da sua parte.</summary>
public sealed class AtividadeResponsavel : Entidade
{
    public const int TamanhoMaximoDirecionamento = 4000;

    private AtividadeResponsavel() { }

    internal AtividadeResponsavel(Guid atividadeId, Guid usuarioId, string? direcionamento)
    {
        AtividadeId = atividadeId;
        UsuarioId = usuarioId;
        Direcionamento = direcionamento;
    }

    public Guid AtividadeId { get; private set; }
    public Guid UsuarioId { get; private set; }
    public string? Direcionamento { get; private set; }

    /// <summary>Nulo enquanto a parte está pendente.</summary>
    public DateTime? ConcluidoEm { get; private set; }

    public bool Pendente => ConcluidoEm is null;

    internal void AlterarDirecionamento(string? direcionamento) => Direcionamento = direcionamento;

    internal void Concluir(DateTime agoraUtc) => ConcluidoEm = agoraUtc;

    internal void Reabrir() => ConcluidoEm = null;
}
