using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;

namespace AtividadesJuridicas.UnitTests.Application;

public class EventosDeSegurancaTests
{
    private readonly Cenario _c = new();

    private EventoAnotado Unico() => _c.Auditoria.Eventos.Should().ContainSingle().Subject;

    [Fact]
    public async Task Trocar_o_perfil_anota_o_evento_com_origem_e_destino()
    {
        var pedro = _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);

        await new EditarUsuarioManipulador(_c.Como(_c.Diretor), _c.Usuarios, _c.Perfis, _c.Unidade, _c.Auditoria, _c.ClientesDaIdentidade)
            .ManipularAsync(new EditarUsuario(pedro.Id, "Pedro", "pedro@x.com", _c.PerfilDiretor.Id, null), default);

        var evento = Unico();
        evento.Should().BeEquivalentTo(new
        {
            Evento = EventosDeSeguranca.PerfilAlterado, Entidade = "Usuario", EntidadeId = pedro.Id, Imediato = false,
        });
        evento.Dados.Should().BeEquivalentTo(new { de = _c.PerfilProducao.Id, para = _c.PerfilDiretor.Id });
    }

    [Fact]
    public async Task Editar_sem_trocar_o_perfil_nao_anota_evento()
    {
        var pedro = _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);

        await new EditarUsuarioManipulador(_c.Como(_c.Diretor), _c.Usuarios, _c.Perfis, _c.Unidade, _c.Auditoria, _c.ClientesDaIdentidade)
            .ManipularAsync(new EditarUsuario(pedro.Id, "Pedro Silva", "pedro@x.com", _c.PerfilProducao.Id, null), default);

        _c.Auditoria.Eventos.Should().BeEmpty();
    }

    [Theory]
    [InlineData(false, EventosDeSeguranca.UsuarioInativado)]
    [InlineData(true, EventosDeSeguranca.UsuarioAtivado)]
    public async Task Ativar_e_inativar_anotam_o_evento_correspondente(bool ativar, string esperado)
    {
        var pedro = _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);
        if (ativar) pedro.Inativar();

        await new DefinirAtivoUsuarioManipulador(_c.Como(_c.Diretor), _c.Usuarios, _c.Perfis, _c.Unidade, _c.Auditoria)
            .ManipularAsync(new DefinirAtivoUsuario(pedro.Id, ativar), default);

        Unico().Should().BeEquivalentTo(new { Evento = esperado, Entidade = "Usuario", EntidadeId = pedro.Id });
    }

    [Fact]
    public async Task Definir_o_estado_que_o_usuario_ja_tem_nao_anota_nem_grava()
    {
        var pedro = _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);

        var resultado = await new DefinirAtivoUsuarioManipulador(
                _c.Como(_c.Diretor), _c.Usuarios, _c.Perfis, _c.Unidade, _c.Auditoria)
            .ManipularAsync(new DefinirAtivoUsuario(pedro.Id, true), default);

        resultado.Sucesso.Should().BeTrue();
        _c.Auditoria.Eventos.Should().BeEmpty();
        _c.Unidade.Salvamentos.Should().Be(0);
    }

    [Fact]
    public async Task Redefinir_a_senha_de_outro_anota_senha_redefinida()
    {
        var pedro = _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);

        await new RedefinirSenhaUsuarioManipulador(_c.Como(_c.Diretor), _c.Usuarios, _c.Hasher, _c.Unidade, _c.Auditoria)
            .ManipularAsync(new RedefinirSenhaUsuario(pedro.Id, "nova-senha-1"), default);

        Unico().Should().BeEquivalentTo(new
        {
            Evento = EventosDeSeguranca.SenhaRedefinida, Entidade = "Usuario", EntidadeId = pedro.Id,
        });
    }

    [Fact]
    public async Task Alterar_a_propria_senha_anota_senha_alterada_e_nunca_a_senha()
    {
        await new AlterarMinhaSenhaManipulador(_c.Como(_c.Diretor), _c.Usuarios, _c.Hasher, _c.Unidade, _c.Auditoria)
            .ManipularAsync(new AlterarMinhaSenha("senha-segura", "nova-senha-1"), default);

        var evento = Unico();
        evento.Evento.Should().Be(EventosDeSeguranca.SenhaAlterada);
        evento.EntidadeId.Should().Be(_c.Diretor.Id);
        evento.Dados.Should().BeNull();
    }

    [Fact]
    public async Task Operacao_recusada_nao_anota_evento_de_alteracao()
    {
        var pedro = _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);

        await new RedefinirSenhaUsuarioManipulador(_c.Como(pedro), _c.Usuarios, _c.Hasher, _c.Unidade, _c.Auditoria)
            .ManipularAsync(new RedefinirSenhaUsuario(_c.Diretor.Id, "nova-senha-1"), default);

        _c.Auditoria.Eventos.Should().BeEmpty();
    }
}
