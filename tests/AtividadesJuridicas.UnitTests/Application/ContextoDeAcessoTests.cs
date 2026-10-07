using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.UnitTests.Application;

public class ContextoDeAcessoTests
{
    private readonly Cenario _cenario = new();

    private ContextoDeAcesso Criar(Guid? usuarioId)
    {
        var identidade = new IdentidadeAtual();
        identidade.Definir(usuarioId);
        return new ContextoDeAcesso(identidade, _cenario.Usuarios, _cenario.Perfis);
    }

    [Fact]
    public async Task Devolve_o_usuario_com_o_tipo_do_perfil_lido_do_repositorio()
    {
        var ator = await Criar(_cenario.Diretor.Id).ObterAsync(default);

        ator.Should().Be(new UsuarioAutenticado(_cenario.Diretor.Id, "Diana Diretora", TipoPerfil.Diretoria));
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
}
