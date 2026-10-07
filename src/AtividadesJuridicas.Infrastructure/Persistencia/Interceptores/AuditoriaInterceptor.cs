using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Comum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AtividadesJuridicas.Infrastructure.Persistencia.Interceptores;

/// <summary>
/// Acrescenta um RegistroAuditoria ao mesmo SaveChanges de cada Entidade criada, alterada ou
/// excluída: os dois são gravados na mesma transação, ou nenhum é. Roda depois do
/// CarimbosInterceptor, quando a exclusão já é uma alteração de ExcluidoEm.
/// </summary>
public sealed class AuditoriaInterceptor(IIdentidadeAtual identidade, IRelogio relogio) : SaveChangesInterceptor
{
    private static readonly HashSet<string> ForaDosDados =
    [
        nameof(Entidade.Id),
        nameof(Entidade.CriadoEm), nameof(Entidade.CriadoPor),
        nameof(Entidade.AtualizadoEm), nameof(Entidade.AtualizadoPor),
        nameof(Entidade.ExcluidoEm), nameof(Entidade.ExcluidoPor),
    ];

    // Mantém acentos legíveis no JSON gravado.
    private static readonly JsonSerializerOptions Json = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Registrar(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Registrar(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Registrar(DbContext? db)
    {
        if (db is null) return;

        var agora = relogio.AgoraUtc;

        foreach (var entrada in db.ChangeTracker.Entries<Entidade>().ToList())
        {
            if (Descrever(entrada) is not (var operacao, var dados)) continue;

            db.Add(RegistroAuditoria.DeAlteracao(
                agora, identidade.UsuarioId, operacao, entrada.Metadata.ClrType.Name, entrada.Entity.Id,
                dados, identidade.Ip, identidade.IdCorrelacao));
        }
    }

    private static (OperacaoAuditada Operacao, string Dados)? Descrever(EntityEntry<Entidade> entrada)
    {
        var auditaveis = entrada.Properties.Where(Auditavel).ToList();

        switch (entrada.State)
        {
            case EntityState.Added:
                return (OperacaoAuditada.Criacao,
                    Serializar(auditaveis.ToDictionary(Nome, p => Valor(p, p.CurrentValue))));

            case EntityState.Modified:
                var exclusao = entrada.Property(nameof(Entidade.ExcluidoEm));
                if (exclusao is { IsModified: true, OriginalValue: null, CurrentValue: not null })
                    return (OperacaoAuditada.Exclusao,
                        Serializar(auditaveis.ToDictionary(Nome, p => Valor(p, p.OriginalValue))));

                var alteradas = auditaveis.Where(p => p.IsModified).ToList();
                if (alteradas.Count == 0) return null;

                return (OperacaoAuditada.Alteracao, Serializar(alteradas.ToDictionary(
                    Nome, p => (object?)new { de = Valor(p, p.OriginalValue), para = Valor(p, p.CurrentValue) })));

            default:
                return null;
        }
    }

    private static bool Auditavel(PropertyEntry propriedade) =>
        !ForaDosDados.Contains(propriedade.Metadata.Name) &&
        propriedade.Metadata.PropertyInfo?.GetCustomAttribute<NaoAuditarAttribute>() is null;

    private static string Nome(PropertyEntry propriedade) => propriedade.Metadata.Name;

    private static object? Valor(PropertyEntry propriedade, object? valor) => valor switch
    {
        null => null,
        Enum e => e.ToString(),
        _ => propriedade.Metadata.GetValueConverter() is { } conversor ? conversor.ConvertToProvider(valor) : valor,
    };

    private static string Serializar(Dictionary<string, object?> dados) => JsonSerializer.Serialize(dados, Json);
}
