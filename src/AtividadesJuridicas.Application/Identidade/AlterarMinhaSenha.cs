using AtividadesJuridicas.Application.Comum;

namespace AtividadesJuridicas.Application.Identidade;

public sealed record AlterarMinhaSenha(string SenhaAtual, string NovaSenha) : IRequisicao<Resultado>;

public sealed class AlterarMinhaSenhaManipulador(
    IContextoDeAcesso contexto,
    IUsuarioRepositorio usuarios,
    IHasherDeSenha hasher,
    IUnidadeDeTrabalho unidade) : IManipulador<AlterarMinhaSenha, Resultado>
{
    public async Task<Resultado> ManipularAsync(AlterarMinhaSenha requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { } ator)
            return Resultado.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        var usuario = await usuarios.ObterPorIdAsync(ator.Id, ct);
        if (usuario is null)
            return Resultado.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        if (!hasher.Verificar(requisicao.SenhaAtual ?? string.Empty, usuario.SenhaHash))
            return Resultado.Falha(TipoErro.Validacao, "A senha atual não confere.");

        if (PoliticaDeSenha.Validar(requisicao.NovaSenha) is { } erro)
            return Resultado.Falha(TipoErro.Validacao, erro);

        usuario.DefinirSenha(hasher.Gerar(requisicao.NovaSenha));
        await unidade.SalvarAsync(ct);
        return Resultado.Ok();
    }
}
