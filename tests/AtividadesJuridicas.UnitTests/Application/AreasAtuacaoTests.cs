using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.UnitTests.Application;

public class AreasAtuacaoTests
{
    private readonly Cenario _c = new();

    private Usuario Producao() => _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);

    private Task<Resultado<Guid>> Salvar(Usuario? ator, Guid? id, string nome) =>
        new SalvarAreaAtuacaoManipulador(_c.Como(ator), _c.Areas, _c.Unidade)
            .ManipularAsync(new SalvarAreaAtuacao(id, nome), default);

    private Task<Resultado> DefinirAtivo(Usuario? ator, Guid id, bool ativo) =>
        new DefinirAtivoAreaAtuacaoManipulador(_c.Como(ator), _c.Areas, _c.Unidade)
            .ManipularAsync(new DefinirAtivoAreaAtuacao(id, ativo), default);

    private Task<Resultado<IReadOnlyList<AreaAtuacaoResumo>>> Listar(Usuario? ator, bool somenteAtivas = false) =>
        new ListarAreasAtuacaoManipulador(_c.Como(ator), _c.Areas)
            .ManipularAsync(new ListarAreasAtuacao(somenteAtivas), default);

    [Fact]
    public async Task Diretor_cria_e_renomeia()
    {
        var criada = await Salvar(_c.Diretor, null, " Trabalhista ");
        criada.Sucesso.Should().BeTrue();

        (await Salvar(_c.Diretor, criada.Valor, "Direito do Trabalho")).Sucesso.Should().BeTrue();

        _c.Areas.Itens.Should().ContainSingle().Which.Nome.Should().Be("Direito do Trabalho");
        _c.Unidade.Salvamentos.Should().Be(2);
    }

    [Fact]
    public async Task Nome_repetido_em_outra_caixa_e_conflito()
    {
        await Salvar(_c.Diretor, null, "Cível");

        (await Salvar(_c.Diretor, null, " CÍVEL ")).Erro.Should().Be(TipoErro.Conflito);
        _c.Areas.Itens.Should().ContainSingle();
    }

    [Fact]
    public async Task Salvar_mantendo_o_proprio_nome_nao_e_conflito()
    {
        var criada = await Salvar(_c.Diretor, null, "Cível");

        (await Salvar(_c.Diretor, criada.Valor, "Cível")).Sucesso.Should().BeTrue();
    }

    [Fact]
    public async Task Nome_vazio_e_erro_de_validacao_e_id_inexistente_e_nao_encontrado()
    {
        (await Salvar(_c.Diretor, null, "  ")).Erro.Should().Be(TipoErro.Validacao);
        (await Salvar(_c.Diretor, Guid.CreateVersion7(), "Cível")).Erro.Should().Be(TipoErro.NaoEncontrado);
        _c.Unidade.Salvamentos.Should().Be(0);
    }

    [Fact]
    public async Task Producao_e_anonimo_nao_gerenciam_areas()
    {
        var criada = await Salvar(_c.Diretor, null, "Cível");
        var producao = Producao();

        (await Salvar(producao, null, "Tributário")).Erro.Should().Be(TipoErro.AcessoNegado);
        (await Salvar(null, null, "Tributário")).Erro.Should().Be(TipoErro.AcessoNegado);
        (await DefinirAtivo(producao, criada.Valor, false)).Erro.Should().Be(TipoErro.AcessoNegado);
        _c.Areas.Itens.Should().ContainSingle().Which.Ativo.Should().BeTrue();
    }

    [Fact]
    public async Task Inativar_e_reativar()
    {
        var criada = await Salvar(_c.Diretor, null, "Cível");

        (await DefinirAtivo(_c.Diretor, criada.Valor, false)).Sucesso.Should().BeTrue();
        _c.Areas.Itens[0].Ativo.Should().BeFalse();

        (await DefinirAtivo(_c.Diretor, criada.Valor, true)).Sucesso.Should().BeTrue();
        _c.Areas.Itens[0].Ativo.Should().BeTrue();
    }

    [Fact]
    public async Task Producao_lista_as_areas_e_o_filtro_de_ativas_funciona()
    {
        await Salvar(_c.Diretor, null, "Tributário");
        var civel = await Salvar(_c.Diretor, null, "Cível");
        await DefinirAtivo(_c.Diretor, civel.Valor, false);
        var producao = Producao();

        (await Listar(producao)).Valor!.Select(a => a.Nome).Should().Equal("Cível", "Tributário");
        (await Listar(producao, somenteAtivas: true)).Valor!.Select(a => a.Nome).Should().Equal("Tributário");
        (await Listar(null)).Erro.Should().Be(TipoErro.AcessoNegado);
    }

    [Fact]
    public void Listagem_e_uma_consulta_auditavel()
    {
        var consulta = (AtividadesJuridicas.Application.Auditoria.IConsultaAuditavel)new ListarAreasAtuacao();

        consulta.TipoRecurso.Should().Be("AreaAtuacao");
        consulta.Acao.Should().Be(AcaoDeAcesso.Listou);
    }
}
