using AtividadesJuridicas.Application.Comum;

namespace AtividadesJuridicas.Infrastructure.Comum;

public sealed class RelogioDoSistema : IRelogio
{
    public DateTime AgoraUtc => DateTime.UtcNow;
}
