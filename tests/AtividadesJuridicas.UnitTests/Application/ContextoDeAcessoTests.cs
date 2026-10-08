using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.UnitTests.Application;

public class ContextoDeAcessoTests
{
    private readonly Cenario _cenario = new();

    private ContextoDeAcesso Criar(Guid? usuarioId)
    {
        var identidade = new IdentidadeAtual();
        identidade.DefinirInterno(usuarioId);
        return new ContextoDeAcesso(identidade, _cenario.Usuarios, _cenario.Perfis);
    }

    [Fact]
    public async Task Devolve_o_usuario_com_o_tipo_do_perfil_lido_do_repositorio()
    {
        var ator = await Criar(_cenario.Diretor.Id).ObterAsync(default);

        ator.Should().Be(new UsuarioAutenticado(
            _cenario.Diretor.Id, "Diana Diretora", TipoPerfil.Diretoria, _cenario.PerfilDiretor.Id));
        ator!.EhDiretoria.Should().BeTrue();
    }

    [Fact]
    public async Task Sem_identidade_devolve_nulo()
    {
        (await Criar(null).ObterAsync(default)).Should().BeNull();
    }

    [Fact]
    public async Task Usuario_inexistente_devolve_nulo()
    {
        (await Criar(Guid.CreateVersion7()).ObterAsync(default)).Should().BeNull();
    }

    [Fact]
    public async Task Usuario_inativado_devolve_nulo_mesmo_com_identidade_valida()
    {
        _cenario.Diretor.Inativar();

        (await Criar(_cenario.Diretor.Id).ObterAsync(default)).Should().BeNull();
    }

    [Fact]
    public async Task Troca_de_perfil_vale_na_consulta_seguinte()
    {
        var contexto = Criar(_cenario.Diretor.Id);
        (await contexto.ObterAsync(default))!.EhDiretoria.Should().BeTrue();

        _cenario.Diretor.AlterarPerfil(_cenario.PerfilProducao);

        (await contexto.ObterAsync(default))!.EhDiretoria.Should().BeFalse();
    }

    private ContextoDeAcesso CriarComVersao(Guid usuarioId, Guid? versao)
    {
        var identidade = new IdentidadeAtual();
        identidade.Definir(usuarioId, null, versao);
        return new ContextoDeAcesso(identidade, _cenario.Usuarios, _cenario.Perfis);
    }

    [Fact]
    public async Task Sessao_com_a_versao_vigente_e_aceita()
    {
        var contexto = CriarComVersao(_cenario.Diretor.Id, _cenario.Diretor.VersaoDeSessao);

        (await contexto.ObterAsync(default)).Should().NotBeNull();
    }

    [Fact]
    public async Task Sessao_anterior_a_troca_de_senha_e_recusada_na_consulta_seguinte()
    {
        var contexto = CriarComVersao(_cenario.Diretor.Id, _cenario.Diretor.VersaoDeSessao);
        (await contexto.ObterAsync(default)).Should().NotBeNull();

        _cenario.Diretor.DefinirSenha("novo-hash");

        (await contexto.ObterAsync(default)).Should().BeNull();
    }

    [Fact]
    public async Task Sessao_antiga_nao_revive_ao_reativar_o_usuario()
    {
        var contexto = CriarComVersao(_cenario.Diretor.Id, _cenario.Diretor.VersaoDeSessao);

        _cenario.Diretor.Inativar();
        _cenario.Diretor.Ativar();

        (await contexto.ObterAsync(default)).Should().BeNull();
    }

    [Fact]
    public async Task Versao_vazia_nunca_e_aceita()
    {
        (await CriarComVersao(_cenario.Diretor.Id, Guid.Empty).ObterAsync(default)).Should().BeNull();
    }

    [Fact]
    public async Task Usuario_com_versao_armazenada_vazia_nunca_aceita_sessao_vazia()
    {
        // Simula uma linha anterior à migração, cuja versão armazenada era só zeros.
        typeof(Usuario).GetProperty(nameof(Usuario.VersaoDeSessao))!.SetValue(_cenario.Diretor, Guid.Empty);

        (await CriarComVersao(_cenario.Diretor.Id, Guid.Empty).ObterAsync(default)).Should().BeNull();
    }
}
