using System.Text.Json;
using System.Text.Json.Nodes;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class ClientesTests(BancoFixture banco) : TesteComBanco(banco)
{
    private static DadosDoClienteEntrada Dados(string nome = "Maria Silva", string? cidade = "Maceió") => new(
        nome, "maria@x.com", "(82) 99999-0000",
        new EnderecoEntrada("Rua A", "10", null, "Centro", cidade, "AL", "57020-000"), "Cliente antigo");

    private async Task<Guid> CriarPfAsync(Guid ator, string nome = "Maria Silva", string cpf = "529.982.247-25")
    {
        var criado = await ExecutarAsync(ator,
            new SalvarClientePessoaFisica(null, Dados(nome), cpf, "123", new DateOnly(1980, 5, 17)));
        criado.Sucesso.Should().BeTrue(criado.Mensagem);
        return criado.Valor;
    }

    private async Task<Guid> CriarPjAsync(Guid ator, string nome = "Acme Ltda", string cnpj = "12.abc.345/01de-35")
    {
        var criado = await ExecutarAsync(ator,
            new SalvarClientePessoaJuridica(null, Dados(nome), cnpj, "Acme", "João", "123", null));
        criado.Sucesso.Should().BeTrue(criado.Mensagem);
        return criado.Valor;
    }

    private async Task<Guid> CriarProducaoAsync(Guid adminId)
    {
        var perfil = await IdDoPerfilAsync(TipoPerfil.Producao);
        return (await ExecutarAsync(adminId, new CriarUsuario("Pedro", "pedro@x.com", "senha-do-pedro", perfil, null))).Valor;
    }

    private async Task<List<RegistroAuditoria>> DoClienteAsync(Guid clienteId) =>
        (await AuditoriaAsync()).Where(r => r.EntidadeId == clienteId).ToList();

    [Fact]
    public async Task Pessoa_fisica_e_juridica_vao_e_voltam_do_banco_inteiras()
    {
        var adminId = await IdDoAdminAsync();
        var pfId = await CriarPfAsync(adminId);
        var pjId = await CriarPjAsync(adminId);

        var pf = (await ExecutarAsync(adminId, new ObterCliente(pfId))).Valor!;
        var pj = (await ExecutarAsync(adminId, new ObterCliente(pjId))).Valor!;

        pf.Should().BeEquivalentTo(new
        {
            Tipo = TipoCliente.PessoaFisica, Nome = "Maria Silva", Email = "maria@x.com", Telefone = "(82) 99999-0000",
            Logradouro = "Rua A", Numero = "10", Bairro = "Centro", Cidade = "Maceió", Uf = "AL", Cep = "57020000",
            Observacoes = "Cliente antigo", Ativo = true,
            Cpf = "529.982.247-25", Rg = "123", DataNascimento = new DateOnly(1980, 5, 17),
        });
        pf.Complemento.Should().BeNull();
        pf.Cnpj.Should().BeNull();

        pj.Tipo.Should().Be(TipoCliente.PessoaJuridica);
        pj.Cnpj.Should().Be("12.ABC.345/01DE-35");
        pj.NomeFantasia.Should().Be("Acme");
        pj.InscricaoMunicipal.Should().BeNull();
        pj.Cpf.Should().BeNull();
    }

    [Fact]
    public async Task Cliente_sem_endereco_volta_com_o_endereco_vazio()
    {
        var adminId = await IdDoAdminAsync();
        var criado = await ExecutarAsync(adminId, new SalvarClientePessoaFisica(
            null, new DadosDoClienteEntrada("José", null, null, new EnderecoEntrada(), null), "111.444.777-35", null, null));

        var detalhe = (await ExecutarAsync(adminId, new ObterCliente(criado.Valor))).Valor!;

        detalhe.Cidade.Should().BeNull();
        detalhe.Cep.Should().BeNull();
        detalhe.Email.Should().BeNull();
        detalhe.DataNascimento.Should().BeNull();
    }

    [Fact]
    public async Task Criacao_e_auditada_pelo_tipo_raiz_com_o_endereco_achatado()
    {
        var adminId = await IdDoAdminAsync();
        var pfId = await CriarPfAsync(adminId);

        var registro = (await DoClienteAsync(pfId)).Should().ContainSingle().Subject;

        registro.Entidade.Should().Be("Cliente");
        registro.Operacao.Should().Be(OperacaoAuditada.Criacao);
        var dados = JsonDocument.Parse(registro.Dados!).RootElement;
        dados.GetProperty("Tipo").GetString().Should().Be("PessoaFisica");
        dados.GetProperty("Cpf").GetString().Should().Be("52998224725");
        dados.GetProperty("Email").GetString().Should().Be("maria@x.com");
        dados.GetProperty("Endereco.Cidade").GetString().Should().Be("Maceió");
        dados.GetProperty("Endereco.Cep").GetString().Should().Be("57020000");
        dados.GetProperty("DataNascimento").GetString().Should().Be("1980-05-17");
        dados.TryGetProperty("Cnpj", out _).Should().BeFalse("o registro traz só as propriedades do tipo do cliente");
    }

    [Fact]
    public async Task Editar_so_o_endereco_registra_o_campo_alterado_e_carimba()
    {
        var adminId = await IdDoAdminAsync();
        var pfId = await CriarPfAsync(adminId);

        var editado = await ExecutarAsync(adminId, new SalvarClientePessoaFisica(
            pfId, Dados(cidade: "Arapiraca"), "529.982.247-25", "123", new DateOnly(1980, 5, 17)));
        editado.Sucesso.Should().BeTrue(editado.Mensagem);

        var alteracao = (await DoClienteAsync(pfId)).Last();
        alteracao.Operacao.Should().Be(OperacaoAuditada.Alteracao);
        alteracao.Entidade.Should().Be("Cliente");
        // A coluna é jsonb: o PostgreSQL normaliza os espaços, então compara-se o JSON, não o texto.
        JsonNode.DeepEquals(
                JsonNode.Parse(alteracao.Dados!),
                JsonNode.Parse("""{"Endereco.Cidade":{"de":"Maceió","para":"Arapiraca"}}"""))
            .Should().BeTrue($"os dados registrados foram {alteracao.Dados}");

        var salvo = await EmEscopoAsync(null, sp =>
            sp.GetRequiredService<AppDbContext>().Clientes.AsNoTracking().SingleAsync(c => c.Id == pfId));
        salvo.AtualizadoPor.Should().Be(adminId);
        salvo.AtualizadoEm.Should().NotBeNull();
    }

    [Fact]
    public async Task Salvar_sem_mudar_nada_nao_gera_registro()
    {
        var adminId = await IdDoAdminAsync();
        var pfId = await CriarPfAsync(adminId);

        await ExecutarAsync(adminId, new SalvarClientePessoaFisica(
            pfId, Dados(), "52998224725", "123", new DateOnly(1980, 5, 17)));

        (await DoClienteAsync(pfId)).Should().ContainSingle("só a criação");
    }

    [Fact]
    public async Task Documento_duplicado_e_recusado_pelo_caso_de_uso_e_pelo_banco()
    {
        var adminId = await IdDoAdminAsync();
        await CriarPfAsync(adminId);
        await CriarPjAsync(adminId);

        (await ExecutarAsync(adminId, new SalvarClientePessoaFisica(null, Dados("Outra"), "52998224725", null, null)))
            .Erro.Should().Be(TipoErro.Conflito);
        (await ExecutarAsync(adminId, new SalvarClientePessoaJuridica(null, Dados("Outra"), "12ABC34501DE35", null, null, null, null)))
            .Erro.Should().Be(TipoErro.Conflito);

        var direto = () => EmEscopoAsync(adminId, async sp =>
        {
            sp.GetRequiredService<AppDbContext>().Add(ClientePessoaFisica.Criar(
                new DadosDoCliente("Outra", null, null, Endereco.Vazio, null), Cpf.Criar("52998224725"), null, null));
            await sp.GetRequiredService<IUnidadeDeTrabalho>().SalvarAsync(default);
            return 0;
        });
        await direto.Should().ThrowAsync<ExcecaoDeConflito>();
    }

    [Fact]
    public async Task Busca_por_nome_ignora_caixa_e_trata_curingas_como_texto()
    {
        var adminId = await IdDoAdminAsync();
        await CriarPfAsync(adminId, "Maria Silva", "529.982.247-25");
        await CriarPfAsync(adminId, "100% Certo", "111.444.777-35");
        await CriarPjAsync(adminId, "Acme_Ltda");

        async Task<IEnumerable<string>> NomesAsync(string? busca) =>
            (await ExecutarAsync(adminId, new ListarClientes(busca))).Valor!.Itens.Select(c => c.Nome);

        (await NomesAsync(null)).Should().Equal("100% Certo", "Acme_Ltda", "Maria Silva");
        (await NomesAsync("MARIA")).Should().Equal("Maria Silva");
        (await NomesAsync("%")).Should().Equal("100% Certo");
        (await NomesAsync("_")).Should().Equal("Acme_Ltda");
        (await NomesAsync("529.982.247-25")).Should().Equal("Maria Silva");
        (await NomesAsync("12ABC34501DE35")).Should().Equal("Acme_Ltda");
        (await NomesAsync("xyz")).Should().BeEmpty();
    }

    [Fact]
    public async Task Listagem_pagina_de_vinte_em_vinte_e_esconde_inativos()
    {
        var adminId = await IdDoAdminAsync();
        await EmEscopoAsync(adminId, async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            for (var i = 0; i < 45; i++)
            {
                db.Add(ClientePessoaFisica.Criar(
                    new DadosDoCliente($"Cliente {i:00}", null, null, Endereco.Vazio, null),
                    CpfDeTeste(i), null, null));
            }
            await sp.GetRequiredService<IUnidadeDeTrabalho>().SalvarAsync(default);
            return 0;
        });
        var inativo = (await ExecutarAsync(adminId, new ListarClientes())).Valor!.Itens[0];
        await ExecutarAsync(adminId, new DefinirAtivoCliente(inativo.Id, false));

        var primeira = (await ExecutarAsync(adminId, new ListarClientes())).Valor!;
        var terceira = (await ExecutarAsync(adminId, new ListarClientes(Pagina: 3))).Valor!;
        var comInativos = (await ExecutarAsync(adminId, new ListarClientes(IncluirInativos: true))).Valor!;

        primeira.Total.Should().Be(44);
        primeira.Itens.Should().HaveCount(20);
        primeira.TotalDePaginas.Should().Be(3);
        terceira.Itens.Should().HaveCount(4);
        comInativos.Total.Should().Be(45);
    }

    // Gera CPFs válidos distintos a partir de um número.
    private static Cpf CpfDeTeste(int n)
    {
        var baseDoCpf = (100000000 + n).ToString();
        int Digito(string d, int q)
        {
            var soma = 0;
            for (var i = 0; i < q; i++) soma += (d[i] - '0') * (q + 1 - i);
            var r = soma * 10 % 11;
            return r == 10 ? 0 : r;
        }
        var d1 = Digito(baseDoCpf, 9);
        var d2 = Digito(baseDoCpf + d1, 10);
        return Cpf.Criar(baseDoCpf + d1 + d2);
    }

    [Fact]
    public async Task Producao_consulta_e_fica_registrado_mas_nao_altera()
    {
        var adminId = await IdDoAdminAsync();
        var pfId = await CriarPfAsync(adminId);
        var pedroId = await CriarProducaoAsync(adminId);

        (await ExecutarAsync(pedroId, new ListarClientes("maria"))).Valor!.Itens.Should().ContainSingle();
        (await ExecutarAsync(pedroId, new ObterCliente(pfId))).Sucesso.Should().BeTrue();
        (await ExecutarAsync(pedroId, new DefinirAtivoCliente(pfId, false))).Erro.Should().Be(TipoErro.AcessoNegado);
        (await ExecutarAsync(pedroId, new SalvarClientePessoaFisica(pfId, Dados("Invasor"), "52998224725", null, null)))
            .Erro.Should().Be(TipoErro.AcessoNegado);

        var acessos = await EmEscopoAsync(null, sp => sp.GetRequiredService<AppDbContext>().RegistrosAcesso
            .AsNoTracking().Where(a => a.UsuarioId == pedroId && a.TipoRecurso == "Cliente").ToListAsync());
        acessos.Should().Contain(a => a.Acao == AcaoDeAcesso.Listou && a.RecursoId == null && a.Detalhe!.Contains("maria"));
        acessos.Should().Contain(a => a.Acao == AcaoDeAcesso.Visualizou && a.RecursoId == pfId);

        (await AuditoriaAsync()).Count(r => r.Evento == EventosDeSeguranca.AcessoNegado && r.UsuarioId == pedroId)
            .Should().Be(2);
        (await ExecutarAsync(adminId, new ObterCliente(pfId))).Valor!.Nome.Should().Be("Maria Silva");
    }

    [Fact]
    public async Task Exclusao_logica_e_auditada_e_libera_o_documento()
    {
        var adminId = await IdDoAdminAsync();
        var pfId = await CriarPfAsync(adminId);

        await EmEscopoAsync(adminId, async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            db.Remove(await db.Clientes.SingleAsync(c => c.Id == pfId));
            await sp.GetRequiredService<IUnidadeDeTrabalho>().SalvarAsync(default);
            return 0;
        });

        var exclusao = (await DoClienteAsync(pfId)).Last();
        exclusao.Operacao.Should().Be(OperacaoAuditada.Exclusao);
        exclusao.Entidade.Should().Be("Cliente");
        JsonDocument.Parse(exclusao.Dados!).RootElement.GetProperty("Endereco.Cidade").GetString().Should().Be("Maceió");

        (await ExecutarAsync(adminId, new ObterCliente(pfId))).Erro.Should().Be(TipoErro.NaoEncontrado);
        await CriarPfAsync(adminId, "Maria Nova");
    }
}
