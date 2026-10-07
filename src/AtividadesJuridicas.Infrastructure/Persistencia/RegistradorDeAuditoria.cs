using System.Text.Encodings.Web;
using System.Text.Json;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;
using Microsoft.EntityFrameworkCore;

namespace AtividadesJuridicas.Infrastructure.Persistencia;

public sealed class RegistradorDeAuditoria(
    AppDbContext db,
    DbContextOptions<AppDbContext> opcoes,
    IIdentidadeAtual identidade,
    IRelogio relogio) : IAuditoria
{
    private static readonly JsonSerializerOptions Json = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public void Anotar(string evento, string? entidade = null, Guid? entidadeId = null, object? dados = null) =>
        db.Add(Criar(evento, entidade, entidadeId, dados));

    public async Task RegistrarAsync(string evento, string? entidade = null, Guid? entidadeId = null,
        object? dados = null, CancellationToken ct = default)
    {
        // Contexto isolado: a gravação não arrasta nem descarta o que o escopo tiver pendente.
        await using var isolado = new AppDbContext(opcoes);
        isolado.Add(Criar(evento, entidade, entidadeId, dados));
        await isolado.SaveChangesAsync(ct);
    }

    private RegistroAuditoria Criar(string evento, string? entidade, Guid? entidadeId, object? dados) =>
        RegistroAuditoria.DeEvento(
            relogio.AgoraUtc, identidade.UsuarioId, evento, entidade, entidadeId,
            dados is null ? null : JsonSerializer.Serialize(dados, Json),
            identidade.Ip, identidade.IdCorrelacao);
}
