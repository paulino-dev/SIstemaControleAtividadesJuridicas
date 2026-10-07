using AtividadesJuridicas.Application;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Infrastructure;
using AtividadesJuridicas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class PersistenciaTests(BancoFixture banco) : IAsyncLifetime
{
    private static readonly AdminInicialOpcoes Admin = new("Diana Diretora", "diana@x.com", "senha-segura");

    private ServiceProvider _raiz = null!;

    public async Task InitializeAsync()
    {
        var conexao = banco.NovaConexao();
        _raiz = new ServiceCollection()
            .AddApplication()
            .AddInfrastructure(_ => conexao)
            .BuildServiceProvider();

        await using var escopo = _raiz.CreateAsyncScope();
        await escopo.ServiceProvider.GetRequiredService<SemeadorInicial>().ExecutarAsync(Admin);
    }

    public async Task DisposeAsync() => await _raiz.DisposeAsync();

    /// <summary>Executa em um escopo novo, como o despachante faz, agindo como <paramref name="usuarioId"/>.</summary>
    private async Task<T> EmEscopoAsync<T>(Guid? usuarioId, Func<IServiceProvider, Task<T>> acao)
    {
        await using var escopo = _raiz.CreateAsyncScope();
        escopo.ServiceProvider.GetRequiredService<IdentidadeAtual>().Definir(usuarioId);
        return await acao(escopo.ServiceProvider);
    }

    private Task<Guid> IdDoAdminAsync() => EmEscopoAsync(null, async sp =>
        (await sp.GetRequiredService<IUsuarioRepositorio>().ObterPorEmailAsync(Email.Criar(Admin.Email), default))!.Id);

    [Fact]
    public async Task Semeador_cria_os_dois_perfis_e_o_administrador()
    {
        var (perfis, usuarios) = await EmEscopoAsync(null, async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            return (await db.Perfis.OrderBy(p => p.Nome).ToListAsync(), await db.Usuarios.ToListAsync());
        });

        perfis.Select(p => (p.Nome, p.Tipo)).Should().Equal(
            ("Diretor", TipoPerfil.Diretoria), ("Produção", TipoPerfil.Producao));
        usuarios.Should().ContainSingle().Which.Email.Valor.Should().Be("diana@x.com");
        usuarios[0].SenhaHash.Should().NotContain("senha-segura");
        usuarios[0].CriadoEm.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public async Task Semeador_executado_de_novo_nao_duplica()
    {
        await EmEscopoAsync(null, async sp =>
        {
            await sp.GetRequiredService<SemeadorInicial>().ExecutarAsync(Admin);
            return 0;
        });

        var total = await EmEscopoAsync(null, async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            return (await db.Perfis.CountAsync(), await db.Usuarios.CountAsync());
        });

        total.Should().Be((2, 1));
    }

    [Fact]
    public async Task Criar_usuario_pelo_caso_de_uso_carimba_autor_e_permite_autenticar()
    {
        var adminId = await IdDoAdminAsync();
        var perfilProducao = await EmEscopoAsync(adminId, async sp =>
            (await sp.GetRequiredService<IPerfilRepositorio>().ListarAtivosAsync(default))
                .Single(p => p.Tipo == TipoPerfil.Producao));

        var criado = await EmEscopoAsync(adminId, sp =>
            sp.GetRequiredService<IManipulador<CriarUsuario, Resultado<Guid>>>().ManipularAsync(
                new CriarUsuario("Pedro", "Pedro@X.com", "senha-do-pedro", perfilProducao.Id, null), default));

        criado.Sucesso.Should().BeTrue(criado.Mensagem);

        var salvo = await EmEscopoAsync(null, sp =>
            sp.GetRequiredService<AppDbContext>().Usuarios.SingleAsync(u => u.Id == criado.Valor));
        salvo.CriadoPor.Should().Be(adminId);
        salvo.Email.Valor.Should().Be("pedro@x.com");

        var login = await EmEscopoAsync(null, sp =>
            sp.GetRequiredService<IManipulador<AutenticarUsuario, Resultado<SessaoIniciada>>>().ManipularAsync(
                new AutenticarUsuario("pedro@x.com", "senha-do-pedro"), default));
        login.Sucesso.Should().BeTrue();
        login.Valor!.TipoPerfil.Should().Be(TipoPerfil.Producao);
    }

    [Fact]
    public async Task Alteracao_carimba_atualizacao_com_o_usuario_da_requisicao()
    {
        var adminId = await IdDoAdminAsync();

        await EmEscopoAsync(adminId, async sp =>
        {
            var usuario = await sp.GetRequiredService<IUsuarioRepositorio>().ObterPorIdAsync(adminId, default);
            usuario!.AlterarDados("Diana D.", usuario.Email, null);
            await sp.GetRequiredService<IUnidadeDeTrabalho>().SalvarAsync(default);
            return 0;
        });

        var salvo = await EmEscopoAsync(null, sp =>
            sp.GetRequiredService<AppDbContext>().Usuarios.SingleAsync(u => u.Id == adminId));
        salvo.Nome.Should().Be("Diana D.");
        salvo.AtualizadoPor.Should().Be(adminId);
        salvo.AtualizadoEm.Should().NotBeNull();
    }

    [Fact]
    public async Task Login_registra_o_acesso_sem_alterar_os_carimbos_de_atualizacao()
    {
        var adminId = await IdDoAdminAsync();
        await EmEscopoAsync(adminId, async sp =>
        {
            var usuario = await sp.GetRequiredService<IUsuarioRepositorio>().ObterPorIdAsync(adminId, default);
            usuario!.AlterarDados("Diana D.", usuario.Email, null);
            await sp.GetRequiredService<IUnidadeDeTrabalho>().SalvarAsync(default);
            return 0;
        });
        var antes = await EmEscopoAsync(null, sp =>
            sp.GetRequiredService<AppDbContext>().Usuarios.AsNoTracking().SingleAsync(u => u.Id == adminId));
        antes.AtualizadoPor.Should().Be(adminId);

        var login = await EmEscopoAsync(null, sp =>
            sp.GetRequiredService<IManipulador<AutenticarUsuario, Resultado<SessaoIniciada>>>().ManipularAsync(
                new AutenticarUsuario("diana@x.com", "senha-segura"), default));

        login.Sucesso.Should().BeTrue();
        var depois = await EmEscopoAsync(null, sp =>
            sp.GetRequiredService<AppDbContext>().Usuarios.AsNoTracking().SingleAsync(u => u.Id == adminId));
        depois.UltimoAcessoEm.Should().NotBeNull();
        depois.AtualizadoPor.Should().Be(adminId);
        depois.AtualizadoEm.Should().Be(antes.AtualizadoEm);
    }

    [Fact]
    public async Task Email_duplicado_gravado_direto_vira_excecao_de_conflito()
    {
        var acao = () => EmEscopoAsync(null, async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var perfil = await db.Perfis.FirstAsync();
            db.Usuarios.Add(Usuario.Criar("Outra Diana", Email.Criar("diana@x.com"), "hash", perfil));
            await sp.GetRequiredService<IUnidadeDeTrabalho>().SalvarAsync(default);
            return 0;
        });

        await acao.Should().ThrowAsync<ExcecaoDeConflito>();
    }

    [Fact]
    public async Task Remover_e_exclusao_logica_e_libera_o_email()
    {
        var adminId = await IdDoAdminAsync();
        var pedroId = await EmEscopoAsync(adminId, async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var pedro = Usuario.Criar("Pedro", Email.Criar("pedro@x.com"), "hash", await db.Perfis.FirstAsync());
            db.Usuarios.Add(pedro);
            await sp.GetRequiredService<IUnidadeDeTrabalho>().SalvarAsync(default);
            return pedro.Id;
        });

        await EmEscopoAsync(adminId, async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            db.Usuarios.Remove(await db.Usuarios.SingleAsync(u => u.Id == pedroId));
            await sp.GetRequiredService<IUnidadeDeTrabalho>().SalvarAsync(default);
            return 0;
        });

        await EmEscopoAsync(adminId, async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();

            (await db.Usuarios.AnyAsync(u => u.Id == pedroId)).Should().BeFalse("o filtro global oculta excluídos");

            var linha = await db.Usuarios.IgnoreQueryFilters().SingleAsync(u => u.Id == pedroId);
            linha.ExcluidoEm.Should().NotBeNull();
            linha.ExcluidoPor.Should().Be(adminId);

            db.Usuarios.Add(Usuario.Criar("Pedro Novo", Email.Criar("pedro@x.com"), "hash", await db.Perfis.FirstAsync()));
            await sp.GetRequiredService<IUnidadeDeTrabalho>().SalvarAsync(default);
            return 0;
        });
    }

    [Fact]
    public async Task Contagem_de_diretores_ignora_inativos_producao_e_o_excetuado()
    {
        var adminId = await IdDoAdminAsync();
        await EmEscopoAsync(adminId, async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var diretor = await db.Perfis.SingleAsync(p => p.Tipo == TipoPerfil.Diretoria);
            var producao = await db.Perfis.SingleAsync(p => p.Tipo == TipoPerfil.Producao);
            var inativo = Usuario.Criar("Dora", Email.Criar("dora@x.com"), "hash", diretor);
            inativo.Inativar();
            db.Usuarios.AddRange(inativo, Usuario.Criar("Pedro", Email.Criar("pedro@x.com"), "hash", producao));
            await sp.GetRequiredService<IUnidadeDeTrabalho>().SalvarAsync(default);
            return 0;
        });

        var (todos, semAdmin) = await EmEscopoAsync(adminId, async sp =>
        {
            var repo = sp.GetRequiredService<IUsuarioRepositorio>();
            return (await repo.ContarDiretoresAtivosAsync(null, default),
                    await repo.ContarDiretoresAtivosAsync(adminId, default));
        });

        todos.Should().Be(1);
        semAdmin.Should().Be(0);
    }

    [Fact]
    public async Task Listagem_traz_nome_do_perfil_e_ordena_por_nome()
    {
        var adminId = await IdDoAdminAsync();

        var lista = await EmEscopoAsync(adminId, sp =>
            sp.GetRequiredService<IUsuarioRepositorio>().ListarAsync(default));

        lista.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            Nome = "Diana Diretora",
            Email = "diana@x.com",
            PerfilNome = "Diretor",
            TipoPerfil = TipoPerfil.Diretoria,
            Ativo = true,
        });
    }

    [Fact]
    public async Task Gravar_direto_pelo_contexto_tambem_carimba_e_exclui_logicamente()
    {
        var adminId = await IdDoAdminAsync();

        var pedroId = await EmEscopoAsync(adminId, async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var pedro = Usuario.Criar("Pedro", Email.Criar("pedro@x.com"), "hash", await db.Perfis.FirstAsync());
            db.Usuarios.Add(pedro);
            await db.SaveChangesAsync();
            return pedro.Id;
        });

        await EmEscopoAsync(adminId, async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            db.Usuarios.Remove(await db.Usuarios.SingleAsync(u => u.Id == pedroId));
            await db.SaveChangesAsync();
            return 0;
        });

        var linha = await EmEscopoAsync(null, sp => sp.GetRequiredService<AppDbContext>()
            .Usuarios.IgnoreQueryFilters().AsNoTracking().SingleAsync(u => u.Id == pedroId));

        linha.CriadoPor.Should().Be(adminId);
        linha.CriadoEm.Should().NotBe(default);
        linha.ExcluidoEm.Should().NotBeNull("a linha não pode ser removida fisicamente");
        linha.ExcluidoPor.Should().Be(adminId);
    }

    [Fact]
    public async Task Semeador_carimba_a_criacao_como_sistema()
    {
        var usuario = await EmEscopoAsync(null, sp =>
            sp.GetRequiredService<AppDbContext>().Usuarios.AsNoTracking().SingleAsync());

        usuario.CriadoEm.Should().NotBe(default);
        usuario.CriadoPor.Should().BeNull();
    }
}
