using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Web.Infra;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>O filtro que resulta de clicar num botão de linha da tela de auditoria.</summary>
public class FiltroDaTelaDeAuditoriaTests
{
    private static FiltroDaTelaDeAuditoria Cheio() => new()
    {
        De = new DateOnly(2026, 1, 1),
        Ate = new DateOnly(2026, 1, 31),
        UsuarioId = Guid.CreateVersion7(),
        Entidade = "Usuario",
        EntidadeId = Guid.CreateVersion7().ToString(),
        IdCorrelacao = Guid.CreateVersion7().ToString(),
        Operacao = OperacaoAuditada.Alteracao,
        TipoRecurso = "Usuario",
        Acao = AcaoDeAcesso.Listou,
    };

    [Fact]
    public void Clicar_no_registro_deixa_so_o_periodo_e_o_registro()
    {
        var atual = Cheio();
        var registro = Guid.CreateVersion7();

        var filtro = FiltroDaTelaDeAuditoria.PorRegistro(atual, registro);

        filtro.De.Should().Be(atual.De);
        filtro.Ate.Should().Be(atual.Ate);
        filtro.EntidadeId.Should().Be(registro.ToString());
        filtro.IdCorrelacao.Should().BeNull();
        filtro.Entidade.Should().BeNull();
        filtro.Operacao.Should().BeNull();
        filtro.UsuarioId.Should().BeNull();
    }

    [Fact]
    public void Clicar_na_correlacao_deixa_so_o_periodo_e_a_correlacao()
    {
        var atual = Cheio();
        var correlacao = Guid.CreateVersion7();

        var filtro = FiltroDaTelaDeAuditoria.PorCorrelacao(atual, correlacao);

        filtro.De.Should().Be(atual.De);
        filtro.Ate.Should().Be(atual.Ate);
        filtro.IdCorrelacao.Should().Be(correlacao.ToString());
        filtro.EntidadeId.Should().BeNull();
        filtro.Entidade.Should().BeNull();
        filtro.Operacao.Should().BeNull();
        filtro.UsuarioId.Should().BeNull();
    }

    [Fact]
    public void O_filtro_de_partida_nao_e_alterado()
    {
        var atual = Cheio();
        var entidadeId = atual.EntidadeId;

        _ = FiltroDaTelaDeAuditoria.PorRegistro(atual, Guid.CreateVersion7());
        _ = FiltroDaTelaDeAuditoria.PorCorrelacao(atual, Guid.CreateVersion7());

        atual.EntidadeId.Should().Be(entidadeId);
        atual.Entidade.Should().Be("Usuario");
        atual.Operacao.Should().Be(OperacaoAuditada.Alteracao);
        atual.UsuarioId.Should().NotBeNull();
    }
}

/// <summary>As frases da confirmação de inativação de um usuário.</summary>
public class TextosDaInativacaoTests
{
    [Fact]
    public void So_partes()
    {
        TextosDaInativacao.Pendencias("Pedro", new PendenciasDoUsuario(2, 0, 0)).Should()
            .Be("Este usuário tem 2 partes pendentes. Esses itens continuam atribuídos a Pedro até serem redistribuídos.");
        TextosDaInativacao.Pendencias("Pedro", new PendenciasDoUsuario(1, 0, 0)).Should()
            .Be("Este usuário tem 1 parte pendente. Esses itens continuam atribuídos a Pedro até serem redistribuídos.");
    }

    [Fact]
    public void So_revisoes()
    {
        TextosDaInativacao.Pendencias("Davi", new PendenciasDoUsuario(0, 1, 0)).Should()
            .Be("Este usuário tem 1 revisão em aberto. Esses itens continuam atribuídos a Davi até serem redistribuídos.");
        TextosDaInativacao.Pendencias("Davi", new PendenciasDoUsuario(0, 3, 0)).Should()
            .Be("Este usuário tem 3 revisões em aberto. Esses itens continuam atribuídos a Davi até serem redistribuídos.");
    }

    [Fact]
    public void So_eventos()
    {
        TextosDaInativacao.Pendencias("Paula", new PendenciasDoUsuario(0, 0, 1)).Should()
            .Be("Este usuário tem 1 evento futuro. Esses itens continuam atribuídos a Paula até serem redistribuídos.");
        TextosDaInativacao.Pendencias("Paula", new PendenciasDoUsuario(0, 0, 4)).Should()
            .Be("Este usuário tem 4 eventos futuros. Esses itens continuam atribuídos a Paula até serem redistribuídos.");
    }

    [Fact]
    public void Os_tres_e_dois_a_dois()
    {
        TextosDaInativacao.Pendencias("Ana", new PendenciasDoUsuario(1, 2, 3)).Should()
            .Be("Este usuário tem 1 parte pendente, 2 revisões em aberto e 3 eventos futuros. " +
                "Esses itens continuam atribuídos a Ana até serem redistribuídos.");
        TextosDaInativacao.Pendencias("Ana", new PendenciasDoUsuario(1, 0, 3)).Should()
            .Be("Este usuário tem 1 parte pendente e 3 eventos futuros. " +
                "Esses itens continuam atribuídos a Ana até serem redistribuídos.");
    }

    [Fact]
    public void Sem_pendencias_nao_ha_frase_de_pendencias_so_a_pergunta()
    {
        TextosDaInativacao.Pendencias("Ana", new PendenciasDoUsuario(0, 0, 0)).Should().BeNull();
        TextosDaInativacao.Pergunta("Ana").Should().Be("Confirmar a inativação de Ana?");
    }
}
