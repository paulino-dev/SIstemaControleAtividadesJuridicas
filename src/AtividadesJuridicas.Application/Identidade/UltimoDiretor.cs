using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.Application.Identidade;

/// <summary>Regra: o sistema nunca fica sem um usuário ativo de tipo Diretoria.</summary>
internal static class UltimoDiretor
{
    public const string Mensagem = "O sistema precisa de ao menos um usuário ativo de Diretoria.";

    /// <summary>Verdadeiro quando <paramref name="usuario"/> é hoje o único diretor ativo.</summary>
    public static async Task<bool> EhAsync(
        Usuario usuario, IUsuarioRepositorio usuarios, IPerfilRepositorio perfis, CancellationToken ct)
    {
        if (!usuario.Ativo) return false;

        var perfil = await perfis.ObterPorIdAsync(usuario.PerfilId, ct);
        if (perfil?.Tipo != TipoPerfil.Diretoria) return false;

        return await usuarios.ContarDiretoresAtivosAsync(usuario.Id, ct) == 0;
    }
}
