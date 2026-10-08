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
using Microsoft.EntityFrameworkCore.Metadata;

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

        GarantirSomenteInsercao(db);

        var agora = relogio.AgoraUtc;

        foreach (var entrada in db.ChangeTracker.Entries<Entidade>().ToList())
        {
            if (Descrever(entrada) is not (var operacao, var dados)) continue;

            db.Add(RegistroAuditoria.DeAlteracao(
                agora, identidade.UsuarioId, operacao, entrada.Metadata.GetRootType().ClrType.Name, entrada.Entity.Id,
                dados, identidade.Ip, identidade.IdCorrelacao));
        }
    }

    /// <summary>Os registros de auditoria e de acesso só podem ser inseridos, nunca alterados ou excluídos.</summary>
    private static void GarantirSomenteInsercao(DbContext db)
    {
        var violacao = db.ChangeTracker.Entries()
            .FirstOrDefault(e => e.Entity is RegistroAuditoria or RegistroAcesso &&
                                 e.State is EntityState.Modified or EntityState.Deleted);
        if (violacao is not null)
            throw new InvalidOperationException(
                $"{violacao.Metadata.ClrType.Name} é somente de inserção e não pode ser alterado nem excluído.");
    }

    private static (OperacaoAuditada Operacao, string Dados)? Descrever(EntityEntry<Entidade> entrada)
    {
        var auditaveis = Folhas(entrada).ToList();

        switch (entrada.State)
        {
            case EntityState.Added:
                return (OperacaoAuditada.Criacao,
                    Serializar(auditaveis.ToDictionary(f => f.Nome, f => Valor(f.Entrada, f.Entrada.CurrentValue))));

            case EntityState.Modified:
                var exclusao = entrada.Property(nameof(Entidade.ExcluidoEm));
                if (exclusao is { IsModified: true, OriginalValue: null, CurrentValue: not null })
                    return (OperacaoAuditada.Exclusao,
                        Serializar(auditaveis.ToDictionary(f => f.Nome, f => Valor(f.Entrada, f.Entrada.OriginalValue))));

                var alteradas = auditaveis.Where(f => f.Entrada.IsModified).ToList();
                if (alteradas.Count == 0) return null;

                return (OperacaoAuditada.Alteracao, Serializar(alteradas.ToDictionary(
                    f => f.Nome,
                    f => (object?)new
                    {
                        de = Valor(f.Entrada, f.Entrada.OriginalValue),
                        para = Valor(f.Entrada, f.Entrada.CurrentValue),
                    })));

            default:
                return null;
        }
    }

    /// <summary>
    /// Propriedades escalares da entidade e, achatadas, as dos seus tipos complexos
    /// (por exemplo "Endereco.Cidade"), já sem as que ficam fora da auditoria.
    /// </summary>
    private static IEnumerable<(string Nome, PropertyEntry Entrada)> Folhas(EntityEntry entrada)
    {
        foreach (var propriedade in entrada.Properties.Where(p => Auditavel(p.Metadata)))
            yield return (propriedade.Metadata.Name, propriedade);

        foreach (var complexa in entrada.ComplexProperties)
            foreach (var folha in Folhas(complexa, complexa.Metadata.Name))
                yield return folha;
    }

    private static IEnumerable<(string Nome, PropertyEntry Entrada)> Folhas(ComplexPropertyEntry complexa, string prefixo)
    {
        foreach (var propriedade in complexa.Properties.Where(p => Auditavel(p.Metadata)))
            yield return ($"{prefixo}.{propriedade.Metadata.Name}", propriedade);

        foreach (var interna in complexa.ComplexProperties)
            foreach (var folha in Folhas(interna, $"{prefixo}.{interna.Metadata.Name}"))
                yield return folha;
    }

    /// <summary>Nomes das propriedades de <paramref name="tipo"/> que vão para os dados da auditoria.</summary>
    public static IReadOnlyList<string> PropriedadesAuditadas(IEntityType tipo) =>
        [.. tipo.GetProperties().Where(Auditavel).Select(p => p.Name),
         .. tipo.GetComplexProperties().SelectMany(c => PropriedadesAuditadas(c, c.Name))];

    private static IEnumerable<string> PropriedadesAuditadas(IComplexProperty complexa, string prefixo) =>
        complexa.ComplexType.GetProperties().Where(Auditavel).Select(p => $"{prefixo}.{p.Name}")
            .Concat(complexa.ComplexType.GetComplexProperties()
                .SelectMany(c => PropriedadesAuditadas(c, $"{prefixo}.{c.Name}")));

    private static bool Auditavel(IProperty propriedade) =>
        !ForaDosDados.Contains(propriedade.Name) &&
        propriedade.PropertyInfo?.GetCustomAttribute<NaoAuditarAttribute>() is null;

    private static object? Valor(PropertyEntry propriedade, object? valor) => valor switch
    {
        null => null,
        Enum e => e.ToString(),
        _ => propriedade.Metadata.GetValueConverter() is { } conversor ? conversor.ConvertToProvider(valor) : valor,
    };

    private static string Serializar(Dictionary<string, object?> dados) => JsonSerializer.Serialize(dados, Json);
}
