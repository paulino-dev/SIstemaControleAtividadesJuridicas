using System.Text.Json;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class AuditoriaDeAlteracoesTests(BancoFixture banco) : TesteComBanco(banco)
{
    private async Task<Guid> CriarPedroAsync(Guid adminId)
    {
        var producao = await IdDoPerfilAsync(TipoPerfil.Producao);
        var criado = await ExecutarAsync(adminId,
            new CriarUsuario("Pedro", "pedro@x.com", "senha-do-pedro", producao, "AL 1"));
        criado.Sucesso.Should().BeTrue(criado.Mensagem);
        return criado.Valor;
    }

    private async Task<List<RegistroAuditoria>> DoUsuarioAsync(Guid usuarioId) =>
        (await AuditoriaAsync()).Where(r => r.Entidade == "Usuario" && r.EntidadeId == usuarioId).ToList();

    [Fact]
    public async Task Criacao_gera_registro_com_o_estado_inicial_e_sem_dados_sensiveis()
    {
        var adminId = await IdDoAdminAsync();
        var pedroId = await CriarPedroAsync(adminId);

        var registro = (await DoUsuarioAsync(pedroId)).Should().ContainSingle().Subject;

        registro.Operacao.Should().Be(OperacaoAuditada.Criacao);
        registro.UsuarioId.Should().Be(adminId);
        registro.Ip.Should().Be("127.0.0.9");
        registro.IdCorrelacao.Should().NotBeNull();
        registro.DataHora.Kind.Should().Be(DateTimeKind.Utc);

        var dados = JsonDocument.Parse(registro.Dados!).RootElement;
        dados.GetProperty("Nome").GetString().Should().Be("Pedro");
        dados.GetProperty("Email").GetString().Should().Be("pedro@x.com");
        dados.GetProperty("Ativo").GetBoolean().Should().BeTrue();
        dados.TryGetProperty("SenhaHash", out _).Should().BeFalse();
        dados.TryGetProperty("CriadoEm", out _).Should().BeFalse("carimbos não são dado de negócio");
        registro.Dados.Should().NotContain("pbkdf2");
    }

    [Fact]
    public async Task Alteracao_registra_somente_os_campos_que_mudaram_com_de_e_para()
    {
        var adminId = await IdDoAdminAsync();
        var pedroId = await CriarPedroAsync(adminId);
        var producao = await IdDoPerfilAsync(TipoPerfil.Producao);

        (await ExecutarAsync(adminId, new EditarUsuario(pedroId, "Pedro Silva", "pedro@x.com", producao, "AL 1")))
            .Sucesso.Should().BeTrue();

        var registro = (await DoUsuarioAsync(pedroId)).Last();
        registro.Operacao.Should().Be(OperacaoAuditada.Alteracao);

        var dados = JsonDocument.Parse(registro.Dados!).RootElement;
        dados.EnumerateObject().Select(p => p.Name).Should().Equal("Nome");
        dados.GetProperty("Nome").GetProperty("de").GetString().Should().Be("Pedro");
        dados.GetProperty("Nome").GetProperty("para").GetString().Should().Be("Pedro Silva");
    }

    [Fact]
    public async Task Troca_de_perfil_aparece_com_os_dois_identificadores()
    {
        var adminId = await IdDoAdminAsync();
        var pedroId = await CriarPedroAsync(adminId);
        var producao = await IdDoPerfilAsync(TipoPerfil.Producao);
        var diretoria = await IdDoPerfilAsync(TipoPerfil.Diretoria);

        (await ExecutarAsync(adminId, new EditarUsuario(pedroId, "Pedro", "pedro@x.com", diretoria, "AL 1")))
            .Sucesso.Should().BeTrue();

        var perfil = JsonDocument.Parse((await DoUsuarioAsync(pedroId)).Last().Dados!).RootElement.GetProperty("PerfilId");
        perfil.GetProperty("de").GetGuid().Should().Be(producao);
        perfil.GetProperty("para").GetGuid().Should().Be(diretoria);
    }

    [Fact]
    public async Task Edicao_que_nao_muda_nada_nao_gera_registro()
    {
        var adminId = await IdDoAdminAsync();
        var pedroId = await CriarPedroAsync(adminId);
        var producao = await IdDoPerfilAsync(TipoPerfil.Producao);

        (await ExecutarAsync(adminId, new EditarUsuario(pedroId, "Pedro", "pedro@x.com", producao, "AL 1")))
            .Sucesso.Should().BeTrue();

        (await DoUsuarioAsync(pedroId)).Should().ContainSingle("só a criação");
    }

    [Fact]
    public async Task Exclusao_logica_e_registrada_como_exclusao_com_o_ultimo_estado()
    {
        var adminId = await IdDoAdminAsync();
        var pedroId = await CriarPedroAsync(adminId);

        await EmEscopoAsync(adminId, async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            db.Usuarios.Remove(await db.Usuarios.SingleAsync(u => u.Id == pedroId));
            await db.SaveChangesAsync();
            return 0;
        });

        var registros = await DoUsuarioAsync(pedroId);
        registros.Select(r => r.Operacao).Should().Equal(OperacaoAuditada.Criacao, OperacaoAuditada.Exclusao);
        JsonDocument.Parse(registros[1].Dados!).RootElement.GetProperty("Nome").GetString().Should().Be("Pedro");
    }

    [Fact]
    public async Task Gravacao_que_falha_nao_deixa_registro_de_auditoria()
    {
        var adminId = await IdDoAdminAsync();
        await CriarPedroAsync(adminId);
        var antes = (await AuditoriaAsync()).Count;

        var acao = () => EmEscopoAsync(adminId, async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            db.Usuarios.Add(Usuario.Criar("Outro Pedro", Email.Criar("pedro@x.com"), "hash", await db.Perfis.FirstAsync()));
            await sp.GetRequiredService<IUnidadeDeTrabalho>().SalvarAsync(default);
            return 0;
        });

        await acao.Should().ThrowAsync<ExcecaoDeConflito>();
        (await AuditoriaAsync()).Should().HaveCount(antes);
    }

    [Fact]
    public async Task Login_nao_gera_registro_de_alteracao_do_usuario()
    {
        var adminId = await IdDoAdminAsync();
        var antes = (await DoUsuarioAsync(adminId)).Count(r => r.Operacao == OperacaoAuditada.Alteracao);

        (await ExecutarAsync(null, new AutenticarUsuario("diana@x.com", "senha-segura"))).Sucesso.Should().BeTrue();

        (await DoUsuarioAsync(adminId)).Count(r => r.Operacao == OperacaoAuditada.Alteracao).Should().Be(antes);
    }

    [Fact]
    public async Task Carga_inicial_e_registrada_como_sistema_e_enum_sai_pelo_nome()
    {
        var diretoria = await IdDoPerfilAsync(TipoPerfil.Diretoria);

        var registro = (await AuditoriaAsync()).Single(r => r.Entidade == "Perfil" && r.EntidadeId == diretoria);

        registro.Operacao.Should().Be(OperacaoAuditada.Criacao);
        registro.UsuarioId.Should().BeNull();
        JsonDocument.Parse(registro.Dados!).RootElement.GetProperty("Tipo").GetString().Should().Be("Diretoria");
    }
}
