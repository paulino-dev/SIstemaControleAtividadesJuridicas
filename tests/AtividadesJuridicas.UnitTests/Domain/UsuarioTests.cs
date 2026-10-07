using AtividadesJuridicas.Domain.Comum;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.UnitTests.Domain;

public class UsuarioTests
{
    private static readonly Perfil Diretor = Perfil.Criar("Diretor", TipoPerfil.Diretoria);

    private static Usuario Novo() =>
        Usuario.Criar("Ana Souza", Email.Criar("ana@x.com"), "hash", Diretor, " AL 1234 ");

    [Fact]
    public void Nasce_ativo_com_dados_aparados()
    {
        var usuario = Novo();

        usuario.Nome.Should().Be("Ana Souza");
        usuario.Email.Valor.Should().Be("ana@x.com");
        usuario.PerfilId.Should().Be(Diretor.Id);
        usuario.NumeroOab.Should().Be("AL 1234");
        usuario.Ativo.Should().BeTrue();
        usuario.UltimoAcessoEm.Should().BeNull();
    }

    [Fact]
    public void Oab_em_branco_vira_nulo()
    {
        var usuario = Usuario.Criar("Ana", Email.Criar("ana@x.com"), "hash", Diretor, "   ");

        usuario.NumeroOab.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Recusa_nome_vazio(string nome)
    {
        var acao = () => Usuario.Criar(nome, Email.Criar("ana@x.com"), "hash", Diretor);

        acao.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void Recusa_nome_maior_que_200_caracteres()
    {
        var acao = () => Usuario.Criar(new string('a', 201), Email.Criar("ana@x.com"), "hash", Diretor);

        acao.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void Recusa_perfil_inativo_na_criacao_e_na_troca()
    {
        var inativo = Perfil.Criar("Antigo", TipoPerfil.Producao);
        inativo.Inativar();

        var criar = () => Usuario.Criar("Ana", Email.Criar("ana@x.com"), "hash", inativo);
        var trocar = () => Novo().AlterarPerfil(inativo);

        criar.Should().Throw<ExcecaoDeDominio>();
        trocar.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void Recusa_hash_de_senha_vazio()
    {
        var acao = () => Novo().DefinirSenha(" ");

        acao.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Inativar_e_ativar_alternam_o_estado()
    {
        var usuario = Novo();

        usuario.Inativar();
        usuario.Ativo.Should().BeFalse();

        usuario.Ativar();
        usuario.Ativo.Should().BeTrue();
    }

    [Fact]
    public void Registrar_acesso_guarda_o_momento()
    {
        var usuario = Novo();
        var agora = new DateTime(2026, 10, 6, 12, 0, 0, DateTimeKind.Utc);

        usuario.RegistrarAcesso(agora);

        usuario.UltimoAcessoEm.Should().Be(agora);
    }

    [Fact]
    public void Versao_de_sessao_nasce_preenchida_e_muda_ao_definir_senha_e_ao_inativar()
    {
        var usuario = Novo();
        var inicial = usuario.VersaoDeSessao;
        inicial.Should().NotBe(Guid.Empty);

        usuario.DefinirSenha("outro-hash");
        var aposSenha = usuario.VersaoDeSessao;
        aposSenha.Should().NotBe(inicial);

        usuario.Inativar();
        usuario.VersaoDeSessao.Should().NotBe(aposSenha);
    }

    [Fact]
    public void Versao_de_sessao_nao_muda_ao_editar_dados_trocar_perfil_ou_reativar()
    {
        var usuario = Novo();
        usuario.Inativar();
        var versao = usuario.VersaoDeSessao;

        usuario.Ativar();
        usuario.AlterarDados("Outro Nome", Email.Criar("outro@x.com"), null);
        usuario.AlterarPerfil(Perfil.Criar("Produção", TipoPerfil.Producao));

        usuario.VersaoDeSessao.Should().Be(versao, "reativar não revive nem renova sessões");
    }
}
