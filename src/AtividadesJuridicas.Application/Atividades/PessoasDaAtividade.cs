using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Atividades;

namespace AtividadesJuridicas.Application.Atividades;

/// <summary>Carrega usuários com os seus perfis, como o domínio de atividades precisa conhecê-los.</summary>
internal static class PessoasDaAtividade
{
    /// <summary>Nulo quando algum dos Ids não corresponde a um usuário (ou o perfil dele não é encontrado).</summary>
    public static async Task<IReadOnlyDictionary<Guid, PessoaDaAtividade>?> CarregarAsync(
        IUsuarioRepositorio usuarios, IPerfilRepositorio perfis, IReadOnlyCollection<Guid> ids, CancellationToken ct)
    {
        var distintos = ids.Distinct().ToList();
        var encontrados = await usuarios.ObterVariosAsync(distintos, ct);
        if (encontrados.Count != distintos.Count) return null;

        var perfisDosUsuarios = (await perfis.ObterVariosAsync([.. encontrados.Select(u => u.PerfilId).Distinct()], ct))
            .ToDictionary(p => p.Id);
        if (encontrados.Any(u => !perfisDosUsuarios.ContainsKey(u.PerfilId))) return null;

        return encontrados.ToDictionary(u => u.Id, u => PessoaDaAtividade.De(u, perfisDosUsuarios[u.PerfilId]));
    }
}
