using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Documentos;
using Microsoft.EntityFrameworkCore;

namespace AtividadesJuridicas.Infrastructure.Persistencia.Repositorios;

/// <summary>
/// Inclui um perfil novo na lista de acesso de todos os Documentos Gerais com um único comando. É uma
/// gravação por fora dos interceptores (uma linha por documento, sem registro de auditoria por linha):
/// o caso de uso a registra com o evento perfil_incluido_nos_documentos_gerais, na mesma transação.
/// </summary>
public sealed class AcessoInicialDePerfil(AppDbContext db, IIdentidadeAtual identidade, IRelogio relogio)
    : IAcessoInicialDePerfil
{
    public Task<int> IncluirNosDocumentosGeraisAsync(Guid perfilId, CancellationToken ct)
    {
        var agora = relogio.AgoraUtc;
        var autor = identidade.UsuarioId;
        var geral = (int)CategoriaDocumento.DocumentoGeral;

        // Os valores entram como parâmetros; os CAST dão tipo ao parâmetro mesmo quando ele é nulo.
        return db.Database.ExecuteSqlAsync($"""
            INSERT INTO documentos_perfis_acesso (id, documento_id, perfil_id, criado_em, criado_por)
            SELECT gen_random_uuid(), d.id, CAST({perfilId} AS uuid), CAST({agora} AS timestamptz), CAST({autor} AS uuid)
            FROM documentos d
            WHERE d.categoria = {geral}
              AND d.excluido_em IS NULL
              AND NOT EXISTS (
                  SELECT 1 FROM documentos_perfis_acesso a
                  WHERE a.documento_id = d.id AND a.perfil_id = CAST({perfilId} AS uuid) AND a.excluido_em IS NULL)
            """, ct);
    }
}
