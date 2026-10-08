using System.Reflection;
using AtividadesJuridicas.Domain.Atividades;
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.UnitTests.Domain;

public class AtividadeComentarioTests
{
    private static readonly DateTime Agora = new(2026, 10, 9, 12, 0, 0, DateTimeKind.Utc);
    private readonly Guid _atividade = Guid.CreateVersion7();
    private readonly Guid _usuario = Guid.CreateVersion7();

    [Fact]
    public void Cria_com_texto_aparado_e_quebras_de_linha_internas_preservadas()
    {
        var comentario = AtividadeComentario.Criar(_atividade, _usuario, "  Primeira linha\nSegunda linha  \n", Agora);

        comentario.AtividadeId.Should().Be(_atividade);
        comentario.UsuarioId.Should().Be(_usuario);
        comentario.Texto.Should().Be("Primeira linha\nSegunda linha");
        comentario.DataHora.Should().Be(Agora);
        comentario.Id.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   \n ")]
    public void Texto_vazio_e_recusado(string texto)
    {
        var criar = () => AtividadeComentario.Criar(_atividade, _usuario, texto, Agora);

        criar.Should().Throw<ExcecaoDeDominio>().WithMessage("Escreva o comentário.");
    }

    [Fact]
    public void Texto_longo_e_recusado_e_o_limite_exato_passa()
    {
        AtividadeComentario.Criar(_atividade, _usuario, new string('a', AtividadeComentario.TamanhoMaximoTexto), Agora)
            .Texto.Should().HaveLength(4000);

        var criar = () => AtividadeComentario.Criar(_atividade, _usuario, new string('a', 4001), Agora);

        criar.Should().Throw<ExcecaoDeDominio>().WithMessage("O comentário deve ter até 4000 caracteres.");
    }

    [Fact]
    public void Ids_vazios_sao_argumento_invalido()
    {
        var semAtividade = () => AtividadeComentario.Criar(Guid.Empty, _usuario, "Oi", Agora);
        var semUsuario = () => AtividadeComentario.Criar(_atividade, Guid.Empty, "Oi", Agora);

        semAtividade.Should().Throw<ArgumentException>();
        semUsuario.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Instante_fora_de_utc_e_argumento_invalido()
    {
        var local = () => AtividadeComentario.Criar(_atividade, _usuario, "Oi", new DateTime(2026, 10, 9, 12, 0, 0, DateTimeKind.Local));
        var semKind = () => AtividadeComentario.Criar(_atividade, _usuario, "Oi", new DateTime(2026, 10, 9, 12, 0, 0, DateTimeKind.Unspecified));

        local.Should().Throw<ArgumentException>();
        semKind.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void A_classe_nao_expoe_metodo_publico_que_altere_o_estado()
    {
        var metodos = typeof(AtividadeComentario)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName || m.Name.StartsWith("set_", StringComparison.Ordinal));

        metodos.Select(m => m.Name).Should().BeEmpty();
        typeof(AtividadeComentario).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Should().OnlyContain(p => p.SetMethod == null || !p.SetMethod.IsPublic);
    }
}
