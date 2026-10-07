# Fase 2 — Auditoria e Segurança de Sessão: Plano de Implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Registrar toda alteração, evento de segurança e consulta do sistema, dar à Diretoria uma tela de consulta, e fechar as duas lacunas de sessão da fase 1 (revogação e bloqueio de login).

**Architecture:** Carimbos, exclusão lógica e auditoria de alterações passam a viver em interceptores do `DbContext`, de modo que qualquer gravação é coberta e o registro de auditoria entra na mesma transação da alteração. Eventos de segurança e registros de consulta são gravados por uma porta `IAuditoria`. Um `ExecutorDeRequisicoes` na `Application` passa a ser o ponto único por onde todo caso de uso roda, e é nele que ficam o registro de acesso negado e o de consultas.

**Tech Stack:** .NET 10, Blazor Interactive Server, EF Core 10 + Npgsql (interceptores, `jsonb`), PostgreSQL 17, xUnit 2.9, FluentAssertions 7.2, Testcontainers.

**Spec:** `docs/superpowers/specs/2026-10-06-modelagem-dominio-design.md` (seções 4, 5 — incluindo "Sessões" e "Bloqueio de login" —, 10, 11 e 12).

## Como ler este plano

Os **testes** de cada tarefa definem o comportamento exigido. O código de implementação é o desenho pretendido, escrito sem ter sido executado: quando um detalhe do EF Core 10, do Npgsql ou do Blazor exigir, ajuste o mínimo necessário para preservar o comportamento e registre o desvio no relatório, com a mensagem de erro que o motivou. Nunca apague um teste nem afrouxe uma asserção. Ajustar como um teste obtém suas entradas (regex, nomes de campo, decodificação de HTML) é permitido.

## Estado de partida (fase 1, já na `main`)

- `Entidade` (Id Guid v7, carimbos, exclusão lógica), `Usuario`, `Perfil`, `Email`.
- `Resultado`, `IRequisicao`/`IManipulador`/`IDespachante`, `IdentidadeAtual` (só `UsuarioId`), `IContextoDeAcesso`, casos de uso de usuário.
- `UnidadeDeTrabalho` faz os carimbos e a exclusão lógica antes de `SaveChangesAsync`.
- `Despachante` (Web) abre um escopo por requisição, fixa a identidade, resolve o manipulador por reflexão e registra exceções em log.
- Testes: `tests/AtividadesJuridicas.UnitTests` (77) e `tests/AtividadesJuridicas.IntegrationTests` (50), todos passando.

## Global Constraints

- TFM único `net10.0` em `Directory.Build.props`; nenhum `.csproj` declara `TargetFramework`.
- `Nullable` habilitado e `TreatWarningsAsErrors=true`. Versões de pacote só em `Directory.Packages.props`. Nenhum pacote novo.
- `Domain` não referencia nada. `Application` não referencia EF Core, Npgsql, `Infrastructure` nem `Web` (há teste de arquitetura para os dois).
- Código, nomes de tabela e mensagens em português. Tabelas e colunas em `snake_case`.
- Datas gravadas em UTC; exibição e filtros no fuso de `Escritorio:FusoHorario`.
- `RegistroAuditoria` e `RegistroAcesso` são somente-inserção: sem edição, sem exclusão, sem exclusão lógica, sem carimbos.
- O registro de uma alteração é gravado na mesma transação da alteração. Se o registro falhar, a operação falha.
- `SenhaHash` e `VersaoDeSessao` nunca aparecem em `Dados`, em modelos de leitura nem em log.
- Gravações que não são alteração de negócio (último acesso, falhas e bloqueio de login) não geram registro de alteração nem mudam `AtualizadoEm`/`AtualizadoPor`.
- Bloqueio de login: 5 falhas seguidas, 15 minutos.
- Autorização verificada no manipulador; a interface apenas oculta. A consulta de auditoria é exclusiva da Diretoria.
- Toda página exige autenticação por padrão; só `/conta/entrar`, `/erro` e os arquivos estáticos são anônimos.
- Um caso de uso por arquivo, com requisição e manipulador. Novas dependências de construtor entram **por último**.
- Sem MediatR, sem FluentValidation, sem biblioteca de componentes de UI.

## Review Focus

1. **Gravação que falha** (por exemplo, e-mail duplicado): não sobra registro de auditoria da alteração que não aconteceu. (Tarefa 3)
2. **Edição que não muda nada**: não gera registro de alteração. (Tarefa 3)
3. **Sessão antiga depois de troca de senha ou de inativar e reativar**: é recusada, inclusive em tela já aberta. (Tarefa 6)
4. **Senha correta durante o bloqueio**: é recusada; e-mail inexistente recebe a mesma mensagem de credenciais inválidas e nunca bloqueia. (Tarefa 7)
5. **Usuário de Produção abrindo a consulta de auditoria**: acesso negado, e a tentativa fica registrada. (Tarefas 4 e 8)
6. **Consultar a auditoria**: gera um registro de acesso com os filtros usados. (Tarefa 8)

---

## Estrutura de arquivos

```
src/AtividadesJuridicas.Domain/
  Comum/NaoAuditarAttribute.cs                    novo
  Auditoria/RegistroAuditoria.cs                  novo (com OperacaoAuditada)
  Auditoria/RegistroAcesso.cs                     novo (com AcaoDeAcesso)
  Identidade/Usuario.cs                           + VersaoDeSessao, FalhasDeLogin, BloqueadoAte
src/AtividadesJuridicas.Application/
  Comum/ExecutorDeRequisicoes.cs                  novo
  Comum/Pagina.cs                                 novo
  Auditoria/IAuditoria.cs                         novo (porta + EventosDeSeguranca + IConsultaAuditavel)
  Auditoria/ConsultaDeAuditoria.cs                novo (filtros, resumos, porta de leitura)
  Auditoria/ConsultarAuditoria.cs                 novo
  Auditoria/ConsultarAcessos.cs                   novo
  Identidade/IdentidadeAtual.cs                   + Ip, IdCorrelacao, VersaoDeSessao
  Identidade/*.cs                                 eventos, sessão, bloqueio
src/AtividadesJuridicas.Infrastructure/
  Persistencia/Interceptores/CarimbosInterceptor.cs    novo
  Persistencia/Interceptores/AuditoriaInterceptor.cs   novo
  Persistencia/RegistradorDeAuditoria.cs               novo (implementa IAuditoria)
  Persistencia/Repositorios/ConsultaDeAuditoria.cs     novo
  Persistencia/Configuracoes/RegistroAuditoriaConfiguracao.cs, RegistroAcessoConfiguracao.cs   novos
  Persistencia/UnidadeDeTrabalho.cs               só traduz violação de unicidade
src/AtividadesJuridicas.Web/
  Infra/OrigemDaConexao.cs, IdentidadeHttp.cs     novos
  Infra/Despachante.cs                            delega ao executor
  Components/Pages/Auditoria.razor                novo
tests/AtividadesJuridicas.IntegrationTests/
  TesteComBanco.cs, AplicacaoDeTeste.cs           novos (apoio)
```

---

### Task 1: Carimbos e exclusão lógica no `DbContext`

Hoje os carimbos e a exclusão lógica só acontecem quando a gravação passa pelo `UnidadeDeTrabalho`. Um `db.SaveChangesAsync()` direto pula os dois, e um `Remove` vira DELETE físico. Esta tarefa move a lógica para um interceptor, pré-requisito da auditoria.

**Files:**
- Create: `src/AtividadesJuridicas.Infrastructure/Persistencia/Interceptores/CarimbosInterceptor.cs`
- Modify: `src/AtividadesJuridicas.Infrastructure/Persistencia/UnidadeDeTrabalho.cs`, `src/AtividadesJuridicas.Infrastructure/DependencyInjection.cs`, `src/AtividadesJuridicas.Infrastructure/Persistencia/SemeadorInicial.cs`
- Test: `tests/AtividadesJuridicas.IntegrationTests/PersistenciaTests.cs` (acrescentar)

**Interfaces:**
- Consumes: `Entidade.CarimbarCriacao/CarimbarAtualizacao/MarcarExcluido(DateTime, Guid?)`, `IIdentidadeAtual.UsuarioId`, `IRelogio.AgoraUtc`.
- Produces: `CarimbosInterceptor` (registrado como scoped e adicionado ao `AppDbContext`); `UnidadeDeTrabalho(AppDbContext db)`; `SemeadorInicial(AppDbContext db, IHasherDeSenha hasher)`.

- [ ] **Step 1: Escrever o teste que falha**

Acrescente a `PersistenciaTests`:

```csharp
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
```

- [ ] **Step 2: Rodar e confirmar a falha**

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --filter "FullyQualifiedName~PersistenciaTests"`
Expected: `Gravar_direto_pelo_contexto_tambem_carimba_e_exclui_logicamente` falha (a linha foi removida fisicamente, ou `CriadoPor` é nulo).

- [ ] **Step 3: Implementar o interceptor**

`src/AtividadesJuridicas.Infrastructure/Persistencia/Interceptores/CarimbosInterceptor.cs`:

```csharp
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Comum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AtividadesJuridicas.Infrastructure.Persistencia.Interceptores;

/// <summary>
/// Aplica os carimbos e a exclusão lógica em toda gravação do contexto, venha ela do
/// <c>UnidadeDeTrabalho</c> ou de um <c>SaveChanges</c> direto. Deve ser registrado antes
/// do interceptor de auditoria, que depende de ver a exclusão já convertida em alteração.
/// </summary>
public sealed class CarimbosInterceptor(IIdentidadeAtual identidade, IRelogio relogio) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Aplicar(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Aplicar(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Aplicar(DbContext? db)
    {
        if (db is null) return;

        var agora = relogio.AgoraUtc;
        var usuarioId = identidade.UsuarioId;

        foreach (var entrada in db.ChangeTracker.Entries<Entidade>().ToList())
        {
            switch (entrada.State)
            {
                case EntityState.Added:
                    entrada.Entity.CarimbarCriacao(agora, usuarioId);
                    break;
                case EntityState.Modified:
                    entrada.Entity.CarimbarAtualizacao(agora, usuarioId);
                    break;
                case EntityState.Deleted:
                    // Nada é removido fisicamente: a exclusão vira uma atualização.
                    entrada.State = EntityState.Unchanged;
                    entrada.Entity.MarcarExcluido(agora, usuarioId);
                    break;
            }
        }
    }
}
```

- [ ] **Step 4: Simplificar `UnidadeDeTrabalho`, registrar o interceptor e limpar o semeador**

`src/AtividadesJuridicas.Infrastructure/Persistencia/UnidadeDeTrabalho.cs` (conteúdo completo):

```csharp
using AtividadesJuridicas.Application.Comum;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AtividadesJuridicas.Infrastructure.Persistencia;

/// <summary>Grava as alterações pendentes. Carimbos, exclusão lógica e auditoria são dos interceptores do contexto.</summary>
public sealed class UnidadeDeTrabalho(AppDbContext db) : IUnidadeDeTrabalho
{
    public async Task SalvarAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ExcecaoDeConflito("Já existe um registro com o mesmo valor único.", ex);
        }
    }
}
```

Em `src/AtividadesJuridicas.Infrastructure/DependencyInjection.cs`, substitua o registro do contexto por:

```csharp
        servicos.AddScoped<CarimbosInterceptor>();

        servicos.AddDbContext<AppDbContext>((sp, opcoes) => opcoes
            .UseNpgsql(obterStringDeConexao(sp))
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(sp.GetRequiredService<CarimbosInterceptor>()));
```

e acrescente `using AtividadesJuridicas.Infrastructure.Persistencia.Interceptores;`.

Em `SemeadorInicial.cs`: remova o parâmetro `IRelogio relogio` do construtor, a variável `agora` e as três chamadas a `CarimbarCriacao` (o interceptor passa a fazê-las). Remova os `using` que ficarem sem uso.

- [ ] **Step 5: Rodar e confirmar que passa**

Run: `dotnet test`
Expected: tudo aprovado, zero avisos. Os testes existentes de carimbo e de exclusão lógica continuam passando sem alteração.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "refactor(infra): carimbos e exclusão lógica em interceptor do contexto"
```

---

### Task 2: Executor de requisições e origem da requisição

**Files:**
- Create: `src/AtividadesJuridicas.Application/Comum/ExecutorDeRequisicoes.cs`, `src/AtividadesJuridicas.Web/Infra/OrigemDaConexao.cs`, `src/AtividadesJuridicas.Web/Infra/IdentidadeHttp.cs`
- Modify: `src/AtividadesJuridicas.Application/Identidade/IdentidadeAtual.cs`, `src/AtividadesJuridicas.Application/DependencyInjection.cs`, `src/AtividadesJuridicas.Web/Infra/Despachante.cs`, `src/AtividadesJuridicas.Web/Program.cs`, `src/AtividadesJuridicas.Web/Components/App.razor`, `src/AtividadesJuridicas.Web/Components/Routes.razor`
- Test: `tests/AtividadesJuridicas.UnitTests/Application/ExecutorDeRequisicoesTests.cs`, `tests/AtividadesJuridicas.UnitTests/Application/IdentidadeAtualTests.cs`

**Interfaces:**
- Produces:
  - `IIdentidadeAtual { Guid? UsuarioId; string? Ip; Guid IdCorrelacao; }`; `IdentidadeAtual.Definir(Guid? usuarioId, string? ip = null)`. `IdCorrelacao` é gerado na construção (um por escopo de requisição).
  - `ExecutorDeRequisicoes(IServiceProvider servicos)` com `Task<TResposta> ExecutarAsync<TResposta>(IRequisicao<TResposta> requisicao, CancellationToken ct)`; registrado como scoped em `AddApplication()`.
  - `OrigemDaConexao { string? Ip { get; set; } }` (scoped na Web).
  - `IdentidadeHttp.UsuarioId(ClaimsPrincipal)` e `IdentidadeHttp.Definir(HttpContext)`.

- [ ] **Step 1: Escrever os testes que falham**

`tests/AtividadesJuridicas.UnitTests/Application/IdentidadeAtualTests.cs`:

```csharp
using AtividadesJuridicas.Application.Identidade;

namespace AtividadesJuridicas.UnitTests.Application;

public class IdentidadeAtualTests
{
    [Fact]
    public void Definir_guarda_usuario_e_ip()
    {
        var identidade = new IdentidadeAtual();
        var usuario = Guid.CreateVersion7();

        identidade.Definir(usuario, "10.0.0.7");

        identidade.UsuarioId.Should().Be(usuario);
        identidade.Ip.Should().Be("10.0.0.7");
    }

    [Fact]
    public void Cada_instancia_tem_um_id_de_correlacao_proprio()
    {
        var a = new IdentidadeAtual();
        var b = new IdentidadeAtual();

        a.IdCorrelacao.Should().NotBe(Guid.Empty);
        a.IdCorrelacao.Should().NotBe(b.IdCorrelacao);
    }
}
```

`tests/AtividadesJuridicas.UnitTests/Application/ExecutorDeRequisicoesTests.cs`:

```csharp
using AtividadesJuridicas.Application.Comum;

namespace AtividadesJuridicas.UnitTests.Application;

public sealed class ProvedorFalso : IServiceProvider
{
    private readonly Dictionary<Type, object> _servicos = [];

    public ProvedorFalso Com<T>(T servico) where T : notnull
    {
        _servicos[typeof(T)] = servico;
        return this;
    }

    public object? GetService(Type serviceType) => _servicos.GetValueOrDefault(serviceType);
}

public class ExecutorDeRequisicoesTests
{
    private sealed record Eco(string Texto) : IRequisicao<string>;

    private sealed class EcoManipulador : IManipulador<Eco, string>
    {
        public Task<string> ManipularAsync(Eco requisicao, CancellationToken ct) =>
            Task.FromResult(requisicao.Texto.ToUpperInvariant());
    }

    private sealed record Quebra : IRequisicao<string>;

    private sealed class QuebraManipulador : IManipulador<Quebra, string>
    {
        // Lança de forma síncrona, antes de devolver a Task.
        public Task<string> ManipularAsync(Quebra requisicao, CancellationToken ct) =>
            throw new InvalidOperationException("falha síncrona");
    }

    private static ExecutorDeRequisicoes Criar(ProvedorFalso provedor) => new(provedor);

    [Fact]
    public async Task Executa_o_manipulador_registrado_para_a_requisicao()
    {
        var executor = Criar(new ProvedorFalso().Com<IManipulador<Eco, string>>(new EcoManipulador()));

        (await executor.ExecutarAsync(new Eco("oi"), default)).Should().Be("OI");
    }

    [Fact]
    public async Task Excecao_sincrona_do_manipulador_chega_sem_embrulho_de_reflexao()
    {
        var executor = Criar(new ProvedorFalso().Com<IManipulador<Quebra, string>>(new QuebraManipulador()));

        var acao = () => executor.ExecutarAsync(new Quebra(), default);

        (await acao.Should().ThrowAsync<InvalidOperationException>()).WithMessage("falha síncrona");
    }

    [Fact]
    public async Task Requisicao_sem_manipulador_falha_com_erro_claro()
    {
        var acao = () => Criar(new ProvedorFalso()).ExecutarAsync(new Eco("oi"), default);

        await acao.Should().ThrowAsync<InvalidOperationException>();
    }
}
```

- [ ] **Step 2: Rodar e confirmar a falha**

Run: `dotnet test tests/AtividadesJuridicas.UnitTests`
Expected: erro de compilação (`ExecutorDeRequisicoes` não existe; `Definir` não aceita IP).

- [ ] **Step 3: Implementar na Application**

`src/AtividadesJuridicas.Application/Identidade/IdentidadeAtual.cs` (conteúdo completo):

```csharp
namespace AtividadesJuridicas.Application.Identidade;

/// <summary>Quem está por trás da requisição em curso e de onde ela veio.</summary>
public interface IIdentidadeAtual
{
    /// <summary>Nulo quando anônimo ou sistema.</summary>
    Guid? UsuarioId { get; }

    string? Ip { get; }

    /// <summary>Um por requisição; liga os registros de auditoria gerados por ela.</summary>
    Guid IdCorrelacao { get; }
}

/// <summary>Preenchida no início de cada escopo de requisição por quem o abre.</summary>
public sealed class IdentidadeAtual : IIdentidadeAtual
{
    public Guid? UsuarioId { get; private set; }
    public string? Ip { get; private set; }
    public Guid IdCorrelacao { get; } = Guid.CreateVersion7();

    public void Definir(Guid? usuarioId, string? ip = null)
    {
        UsuarioId = usuarioId;
        Ip = ip;
    }
}
```

`src/AtividadesJuridicas.Application/Comum/ExecutorDeRequisicoes.cs`:

```csharp
using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.Application.Comum;

/// <summary>
/// Ponto único por onde todo caso de uso roda, dentro do escopo já aberto para a requisição.
/// É aqui que entram os comportamentos comuns a todos eles.
/// </summary>
public sealed class ExecutorDeRequisicoes(IServiceProvider servicos)
{
    public async Task<TResposta> ExecutarAsync<TResposta>(IRequisicao<TResposta> requisicao, CancellationToken ct)
    {
        var contrato = typeof(IManipulador<,>).MakeGenericType(requisicao.GetType(), typeof(TResposta));
        var manipulador = servicos.GetRequiredService(contrato);
        var metodo = contrato.GetMethod("ManipularAsync")!;

        try
        {
            return await (Task<TResposta>)metodo.Invoke(manipulador, [requisicao, ct])!;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            // Invoke embrulha lançamentos síncronos; relança a exceção real, com a pilha original.
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }
}
```

Em `src/AtividadesJuridicas.Application/DependencyInjection.cs`, acrescente `servicos.AddScoped<ExecutorDeRequisicoes>();` junto dos demais registros.

- [ ] **Step 4: Ligar na Web**

`src/AtividadesJuridicas.Web/Infra/OrigemDaConexao.cs`:

```csharp
namespace AtividadesJuridicas.Web.Infra;

/// <summary>
/// IP de quem abriu a página. Um circuito Blazor não tem HttpContext, então o valor é
/// capturado na renderização inicial e entregue ao circuito como parâmetro do componente raiz.
/// </summary>
public sealed class OrigemDaConexao
{
    public string? Ip { get; set; }
}
```

`src/AtividadesJuridicas.Web/Infra/IdentidadeHttp.cs`:

```csharp
using System.Security.Claims;
using AtividadesJuridicas.Application.Identidade;

namespace AtividadesJuridicas.Web.Infra;

public static class IdentidadeHttp
{
    public static Guid? UsuarioId(ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    /// <summary>Para endpoints fora do Blazor: fixa a identidade no escopo da própria requisição HTTP.</summary>
    public static void Definir(HttpContext contexto) =>
        contexto.RequestServices.GetRequiredService<IdentidadeAtual>()
            .Definir(UsuarioId(contexto.User), contexto.Connection.RemoteIpAddress?.ToString());
}
```

`src/AtividadesJuridicas.Web/Infra/Despachante.cs` (conteúdo completo):

```csharp
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using Microsoft.AspNetCore.Components.Authorization;

namespace AtividadesJuridicas.Web.Infra;

/// <summary>
/// Executa cada requisição em um escopo de DI próprio. O escopo de um circuito Blazor dura
/// a sessão inteira do usuário; um DbContext com essa vida acumularia entidades rastreadas
/// e falharia com operações concorrentes do mesmo componente.
/// </summary>
public sealed class Despachante(
    IServiceScopeFactory fabrica,
    AuthenticationStateProvider autenticacao,
    OrigemDaConexao origem,
    ILogger<Despachante> log) : IDespachante
{
    public async Task<TResposta> EnviarAsync<TResposta>(IRequisicao<TResposta> requisicao, CancellationToken ct = default)
    {
        var estado = await autenticacao.GetAuthenticationStateAsync();

        await using var escopo = fabrica.CreateAsyncScope();
        escopo.ServiceProvider.GetRequiredService<IdentidadeAtual>()
            .Definir(IdentidadeHttp.UsuarioId(estado.User), origem.Ip);

        try
        {
            return await escopo.ServiceProvider.GetRequiredService<ExecutorDeRequisicoes>()
                .ExecutarAsync(requisicao, ct);
        }
        catch (Exception ex)
        {
            // Registra só o tipo da requisição: requisições carregam senhas.
            log.LogError(ex, "Falha inesperada ao tratar {TipoDaRequisicao}.", requisicao.GetType().Name);
            throw;
        }
    }
}
```

Em `Program.cs`, acrescente `builder.Services.AddScoped<OrigemDaConexao>();` junto do registro de `IDespachante`.

Em `Components/App.razor`, passe o IP ao componente raiz:

```razor
    <Routes Ip="@HttpContext.Connection.RemoteIpAddress?.ToString()" @rendermode="ModoDaPagina" />
```

`Components/Routes.razor` (conteúdo completo):

```razor
@inject OrigemDaConexao Origem

<Router AppAssembly="typeof(Program).Assembly" NotFoundPage="typeof(Pages.NaoEncontrada)">
    <Found Context="routeData">
        <AuthorizeRouteView RouteData="routeData" DefaultLayout="typeof(MainLayout)" />
        <FocusOnNavigate RouteData="routeData" Selector="h1" />
    </Found>
</Router>

@code {
    /// <summary>Capturado do HttpContext em App.razor; chega ao circuito protegido pelo framework.</summary>
    [Parameter]
    public string? Ip { get; set; }

    protected override void OnInitialized() => Origem.Ip = Ip;
}
```

- [ ] **Step 5: Rodar e confirmar que passa**

Run: `dotnet test`
Expected: tudo aprovado, zero avisos. Os testes de página existentes continuam passando: o comportamento visível não mudou.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "refactor: executor de requisições na aplicação e origem (IP, correlação) na identidade"
```

---

### Task 3: Auditoria de alterações

**Files:**
- Create: `src/AtividadesJuridicas.Domain/Comum/NaoAuditarAttribute.cs`, `src/AtividadesJuridicas.Domain/Auditoria/RegistroAuditoria.cs`
- Create: `src/AtividadesJuridicas.Infrastructure/Persistencia/Configuracoes/RegistroAuditoriaConfiguracao.cs`, `src/AtividadesJuridicas.Infrastructure/Persistencia/Interceptores/AuditoriaInterceptor.cs`
- Create (gerado): migration `Auditoria`
- Modify: `src/AtividadesJuridicas.Domain/Identidade/Usuario.cs` (atributo em `SenhaHash`), `src/AtividadesJuridicas.Infrastructure/Persistencia/AppDbContext.cs`, `src/AtividadesJuridicas.Infrastructure/DependencyInjection.cs`
- Test: `tests/AtividadesJuridicas.IntegrationTests/TesteComBanco.cs` (apoio), `tests/AtividadesJuridicas.IntegrationTests/AuditoriaDeAlteracoesTests.cs`

**Interfaces:**
- Consumes: `CarimbosInterceptor`, `IIdentidadeAtual` (`UsuarioId`, `Ip`, `IdCorrelacao`), `ExecutorDeRequisicoes`, casos de uso de usuário.
- Produces:
  - `[NaoAuditar]` (atributo de propriedade): a propriedade é omitida por inteiro dos registros.
  - `enum OperacaoAuditada { Criacao = 1, Alteracao = 2, Exclusao = 3, Evento = 4 }`.
  - `RegistroAuditoria` com `Id`, `DataHora`, `UsuarioId`, `Operacao`, `Entidade`, `EntidadeId`, `Evento`, `Dados`, `Ip`, `IdCorrelacao`; fábricas `DeAlteracao(...)` e `DeEvento(...)`.
  - `AppDbContext.RegistrosAuditoria`.
  - `TesteComBanco` (classe base de teste) com `EmEscopoAsync`, `ExecutarAsync`, `IdDoAdminAsync`, `IdDoPerfilAsync`, `AuditoriaAsync`.
- Formato de `Dados`: criação e exclusão trazem `{ "Campo": valor }`; alteração traz `{ "Campo": { "de": antigo, "para": novo } }` só com os campos que mudaram. Nomes são os das propriedades. Enums saem pelo nome; value objects, pelo valor gravado.

- [ ] **Step 1: Escrever o apoio e os testes que falham**

`tests/AtividadesJuridicas.IntegrationTests/TesteComBanco.cs`:

```csharp
using AtividadesJuridicas.Application;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Infrastructure;
using AtividadesJuridicas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>Base para testes de Application + Infrastructure contra um banco novo, sem a camada Web.</summary>
public abstract class TesteComBanco(BancoFixture banco) : IAsyncLifetime
{
    protected static readonly AdminInicialOpcoes Admin = new("Diana Diretora", "diana@x.com", "senha-segura");

    private ServiceProvider _raiz = null!;

    public virtual async Task InitializeAsync()
    {
        var conexao = banco.NovaConexao();
        _raiz = new ServiceCollection()
            .AddApplication()
            .AddInfrastructure(_ => conexao)
            .BuildServiceProvider();

        await EmEscopoAsync(null, async sp =>
        {
            await sp.GetRequiredService<SemeadorInicial>().ExecutarAsync(Admin);
            return 0;
        });
    }

    public virtual async Task DisposeAsync() => await _raiz.DisposeAsync();

    /// <summary>Roda em um escopo novo, como o despachante faz, agindo como <paramref name="usuarioId"/>.</summary>
    protected async Task<T> EmEscopoAsync<T>(Guid? usuarioId, Func<IServiceProvider, Task<T>> acao)
    {
        await using var escopo = _raiz.CreateAsyncScope();
        escopo.ServiceProvider.GetRequiredService<IdentidadeAtual>().Definir(usuarioId, "127.0.0.9");
        return await acao(escopo.ServiceProvider);
    }

    protected Task<TResposta> ExecutarAsync<TResposta>(Guid? usuarioId, IRequisicao<TResposta> requisicao) =>
        EmEscopoAsync(usuarioId, sp => sp.GetRequiredService<ExecutorDeRequisicoes>().ExecutarAsync(requisicao, default));

    protected Task<Guid> IdDoAdminAsync() => EmEscopoAsync(null, async sp =>
        (await sp.GetRequiredService<AppDbContext>().Usuarios.AsNoTracking()
            .SingleAsync(u => u.Email == Email.Criar(Admin.Email))).Id);

    protected Task<Guid> IdDoPerfilAsync(TipoPerfil tipo) => EmEscopoAsync(null, async sp =>
        (await sp.GetRequiredService<AppDbContext>().Perfis.AsNoTracking().SingleAsync(p => p.Tipo == tipo)).Id);

    protected Task<List<RegistroAuditoria>> AuditoriaAsync() => EmEscopoAsync(null, sp =>
        sp.GetRequiredService<AppDbContext>().RegistrosAuditoria.AsNoTracking().OrderBy(r => r.Id).ToListAsync());
}
```

`tests/AtividadesJuridicas.IntegrationTests/AuditoriaDeAlteracoesTests.cs`:

```csharp
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
```

- [ ] **Step 2: Rodar e confirmar a falha**

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --filter "FullyQualifiedName~AuditoriaDeAlteracoesTests"`
Expected: erro de compilação (`AtividadesJuridicas.Domain.Auditoria` não existe).

- [ ] **Step 3: Implementar no domínio**

`src/AtividadesJuridicas.Domain/Comum/NaoAuditarAttribute.cs`:

```csharp
namespace AtividadesJuridicas.Domain.Comum;

/// <summary>Propriedade que nunca aparece nos registros de auditoria: segredo ou controle técnico.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class NaoAuditarAttribute : Attribute;
```

`src/AtividadesJuridicas.Domain/Auditoria/RegistroAuditoria.cs`:

```csharp
namespace AtividadesJuridicas.Domain.Auditoria;

public enum OperacaoAuditada
{
    Criacao = 1,
    Alteracao = 2,
    Exclusao = 3,
    Evento = 4,
}

/// <summary>Somente inserção: não é uma Entidade, não tem carimbos nem exclusão lógica.</summary>
public sealed class RegistroAuditoria
{
    private RegistroAuditoria() { }

    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public DateTime DataHora { get; private set; }
    public Guid? UsuarioId { get; private set; }
    public OperacaoAuditada Operacao { get; private set; }
    public string? Entidade { get; private set; }
    public Guid? EntidadeId { get; private set; }
    public string? Evento { get; private set; }
    public string? Dados { get; private set; }
    public string? Ip { get; private set; }
    public Guid? IdCorrelacao { get; private set; }

    public static RegistroAuditoria DeAlteracao(
        DateTime dataHoraUtc, Guid? usuarioId, OperacaoAuditada operacao, string entidade, Guid entidadeId,
        string? dados, string? ip, Guid? idCorrelacao)
    {
        if (operacao == OperacaoAuditada.Evento)
            throw new ArgumentException("Use DeEvento para eventos.", nameof(operacao));

        return new RegistroAuditoria
        {
            DataHora = ExigirUtc(dataHoraUtc), UsuarioId = usuarioId, Operacao = operacao,
            Entidade = entidade, EntidadeId = entidadeId, Dados = dados, Ip = ip, IdCorrelacao = idCorrelacao,
        };
    }

    public static RegistroAuditoria DeEvento(
        DateTime dataHoraUtc, Guid? usuarioId, string evento, string? entidade, Guid? entidadeId,
        string? dados, string? ip, Guid? idCorrelacao)
    {
        if (string.IsNullOrWhiteSpace(evento))
            throw new ArgumentException("O nome do evento é obrigatório.", nameof(evento));

        return new RegistroAuditoria
        {
            DataHora = ExigirUtc(dataHoraUtc), UsuarioId = usuarioId, Operacao = OperacaoAuditada.Evento,
            Evento = evento, Entidade = entidade, EntidadeId = entidadeId, Dados = dados, Ip = ip,
            IdCorrelacao = idCorrelacao,
        };
    }

    private static DateTime ExigirUtc(DateTime valor) =>
        valor.Kind == DateTimeKind.Utc
            ? valor
            : throw new ArgumentException("A data deve estar em UTC.", nameof(valor));
}
```

Em `Usuario.cs`, marque a propriedade:

```csharp
    [NaoAuditar]
    public string SenhaHash { get; private set; } = null!;
```

- [ ] **Step 4: Implementar o mapeamento e o interceptor**

`src/AtividadesJuridicas.Infrastructure/Persistencia/Configuracoes/RegistroAuditoriaConfiguracao.cs`:

```csharp
using AtividadesJuridicas.Domain.Auditoria;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AtividadesJuridicas.Infrastructure.Persistencia.Configuracoes;

public sealed class RegistroAuditoriaConfiguracao : IEntityTypeConfiguration<RegistroAuditoria>
{
    public void Configure(EntityTypeBuilder<RegistroAuditoria> b)
    {
        b.ToTable("registros_auditoria");
        b.HasKey(r => r.Id);
        b.Property(r => r.Id).ValueGeneratedNever();
        b.Property(r => r.Operacao).HasConversion<int>();
        b.Property(r => r.Entidade).HasMaxLength(100);
        b.Property(r => r.Evento).HasMaxLength(100);
        b.Property(r => r.Dados).HasColumnType("jsonb");
        b.Property(r => r.Ip).HasMaxLength(45);

        b.HasIndex(r => r.DataHora);
        b.HasIndex(r => new { r.Entidade, r.EntidadeId });
        b.HasIndex(r => r.UsuarioId);
    }
}
```

Em `AppDbContext.cs`, acrescente `public DbSet<RegistroAuditoria> RegistrosAuditoria => Set<RegistroAuditoria>();` e o `using AtividadesJuridicas.Domain.Auditoria;`.

`src/AtividadesJuridicas.Infrastructure/Persistencia/Interceptores/AuditoriaInterceptor.cs`:

```csharp
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Comum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AtividadesJuridicas.Infrastructure.Persistencia.Interceptores;

/// <summary>
/// Acrescenta um RegistroAuditoria ao mesmo SaveChanges de cada Entidade criada, alterada ou
/// excluída: os dois são gravados na mesma transação, ou nenhum é. Roda depois do
/// CarimbosInterceptor, quando a exclusão já é uma alteração de ExcluidoEm.
/// </summary>
public sealed class AuditoriaInterceptor(IIdentidadeAtual identidade, IRelogio relogio) : SaveChangesInterceptor
{
    private static readonly HashSet<string> ForaDosDados =
    [
        nameof(Entidade.Id),
        nameof(Entidade.CriadoEm), nameof(Entidade.CriadoPor),
        nameof(Entidade.AtualizadoEm), nameof(Entidade.AtualizadoPor),
        nameof(Entidade.ExcluidoEm), nameof(Entidade.ExcluidoPor),
    ];

    // Mantém acentos legíveis no JSON gravado.
    private static readonly JsonSerializerOptions Json = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Registrar(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Registrar(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Registrar(DbContext? db)
    {
        if (db is null) return;

        var agora = relogio.AgoraUtc;

        foreach (var entrada in db.ChangeTracker.Entries<Entidade>().ToList())
        {
            if (Descrever(entrada) is not (var operacao, var dados)) continue;

            db.Add(RegistroAuditoria.DeAlteracao(
                agora, identidade.UsuarioId, operacao, entrada.Metadata.ClrType.Name, entrada.Entity.Id,
                dados, identidade.Ip, identidade.IdCorrelacao));
        }
    }

    private static (OperacaoAuditada Operacao, string Dados)? Descrever(EntityEntry<Entidade> entrada)
    {
        var auditaveis = entrada.Properties.Where(Auditavel).ToList();

        switch (entrada.State)
        {
            case EntityState.Added:
                return (OperacaoAuditada.Criacao,
                    Serializar(auditaveis.ToDictionary(Nome, p => Valor(p, p.CurrentValue))));

            case EntityState.Modified:
                var exclusao = entrada.Property(nameof(Entidade.ExcluidoEm));
                if (exclusao is { IsModified: true, OriginalValue: null, CurrentValue: not null })
                    return (OperacaoAuditada.Exclusao,
                        Serializar(auditaveis.ToDictionary(Nome, p => Valor(p, p.OriginalValue))));

                var alteradas = auditaveis.Where(p => p.IsModified).ToList();
                if (alteradas.Count == 0) return null;

                return (OperacaoAuditada.Alteracao, Serializar(alteradas.ToDictionary(
                    Nome, p => (object?)new { de = Valor(p, p.OriginalValue), para = Valor(p, p.CurrentValue) })));

            default:
                return null;
        }
    }

    private static bool Auditavel(PropertyEntry propriedade) =>
        !ForaDosDados.Contains(propriedade.Metadata.Name) &&
        propriedade.Metadata.PropertyInfo?.GetCustomAttribute<NaoAuditarAttribute>() is null;

    private static string Nome(PropertyEntry propriedade) => propriedade.Metadata.Name;

    private static object? Valor(PropertyEntry propriedade, object? valor) => valor switch
    {
        null => null,
        Enum e => e.ToString(),
        _ => propriedade.Metadata.GetValueConverter() is { } conversor ? conversor.ConvertToProvider(valor) : valor,
    };

    private static string Serializar(Dictionary<string, object?> dados) => JsonSerializer.Serialize(dados, Json);
}
```

Em `DependencyInjection.cs` (Infrastructure), registre e encadeie depois do de carimbos:

```csharp
        servicos.AddScoped<CarimbosInterceptor>();
        servicos.AddScoped<AuditoriaInterceptor>();

        servicos.AddDbContext<AppDbContext>((sp, opcoes) => opcoes
            .UseNpgsql(obterStringDeConexao(sp))
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(
                sp.GetRequiredService<CarimbosInterceptor>(),
                sp.GetRequiredService<AuditoriaInterceptor>()));
```

- [ ] **Step 5: Gerar a migration**

Run:

```bash
dotnet ef migrations add Auditoria --project src/AtividadesJuridicas.Infrastructure --startup-project src/AtividadesJuridicas.Infrastructure --output-dir Persistencia/Migrations
```

Expected: a migration cria só a tabela `registros_auditoria`, com `dados` do tipo `jsonb`, `data_hora` `timestamp with time zone`, `id` sem geração pelo banco e os três índices. Confira e registre no relatório.

- [ ] **Step 6: Rodar e confirmar que passa**

Run: `dotnet test`
Expected: tudo aprovado, zero avisos.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "feat(auditoria): registro de criação, alteração e exclusão na mesma transação"
```

---

### Task 4: Eventos de segurança

**Files:**
- Create: `src/AtividadesJuridicas.Application/Auditoria/IAuditoria.cs`, `src/AtividadesJuridicas.Infrastructure/Persistencia/RegistradorDeAuditoria.cs`
- Modify: `src/AtividadesJuridicas.Application/Comum/ExecutorDeRequisicoes.cs`, `Identidade/EditarUsuario.cs`, `Identidade/DefinirAtivoUsuario.cs`, `Identidade/RedefinirSenhaUsuario.cs`, `Identidade/AlterarMinhaSenha.cs`
- Modify: `src/AtividadesJuridicas.Infrastructure/DependencyInjection.cs`, `src/AtividadesJuridicas.Web/Program.cs`
- Modify (testes): `tests/AtividadesJuridicas.UnitTests/Application/Falsos.cs`, `GestaoDeUsuariosTests.cs`, `SenhaTests.cs`, `ExecutorDeRequisicoesTests.cs`
- Test: `tests/AtividadesJuridicas.UnitTests/Application/EventosDeSegurancaTests.cs`, `tests/AtividadesJuridicas.IntegrationTests/AplicacaoDeTeste.cs` (apoio), `tests/AtividadesJuridicas.IntegrationTests/EventosDeSegurancaWebTests.cs`

**Interfaces:**
- Produces (namespace `AtividadesJuridicas.Application.Auditoria`):
  - `static class EventosDeSeguranca` com as constantes `LoginSucesso = "login_sucesso"`, `LoginFalha = "login_falha"`, `LoginBloqueado = "login_bloqueado"`, `Logout = "logout"`, `SenhaAlterada = "senha_alterada"`, `SenhaRedefinida = "senha_redefinida"`, `PerfilAlterado = "perfil_alterado"`, `UsuarioAtivado = "usuario_ativado"`, `UsuarioInativado = "usuario_inativado"`, `AcessoNegado = "acesso_negado"`.
  - `interface IAuditoria`:
    - `void Anotar(string evento, string? entidade = null, Guid? entidadeId = null, object? dados = null);` — fica pendente e é gravado junto do próximo `SalvarAsync`, na mesma transação.
    - `Task RegistrarAsync(string evento, string? entidade = null, Guid? entidadeId = null, object? dados = null, CancellationToken ct = default);` — grava já, em conexão própria, sem tocar no que estiver pendente no escopo.
  - `ExecutorDeRequisicoes(IServiceProvider servicos, IAuditoria auditoria)`.
  - Os quatro manipuladores alterados ganham `IAuditoria auditoria` como último parâmetro do construtor.
- Produces (testes): `AuditoriaFalsa` e `Cenario.Auditoria` em `Falsos.cs`; `AplicacaoDeTeste` para testes de página.

- [ ] **Step 1: Atualizar os duplos de teste**

Em `Falsos.cs`, acrescente:

```csharp
public sealed record EventoAnotado(string Evento, string? Entidade, Guid? EntidadeId, object? Dados, bool Imediato);

public sealed class AuditoriaFalsa : IAuditoria
{
    public List<EventoAnotado> Eventos { get; } = [];

    public void Anotar(string evento, string? entidade = null, Guid? entidadeId = null, object? dados = null) =>
        Eventos.Add(new EventoAnotado(evento, entidade, entidadeId, dados, Imediato: false));

    public Task RegistrarAsync(string evento, string? entidade = null, Guid? entidadeId = null, object? dados = null,
        CancellationToken ct = default)
    {
        Eventos.Add(new EventoAnotado(evento, entidade, entidadeId, dados, Imediato: true));
        return Task.CompletedTask;
    }
}
```

com `using AtividadesJuridicas.Application.Auditoria;`, e em `Cenario` a propriedade `public AuditoriaFalsa Auditoria { get; } = new();`.

Nos testes existentes, passe `_c.Auditoria` como último argumento do construtor de `EditarUsuarioManipulador`, `DefinirAtivoUsuarioManipulador`, `RedefinirSenhaUsuarioManipulador` e `AlterarMinhaSenhaManipulador` (em `GestaoDeUsuariosTests.cs` e `SenhaTests.cs`). Em `ExecutorDeRequisicoesTests.cs`, troque o auxiliar por `private static ExecutorDeRequisicoes Criar(ProvedorFalso provedor) => new(provedor, new AuditoriaFalsa());`.

- [ ] **Step 2: Escrever os testes que falham**

`tests/AtividadesJuridicas.UnitTests/Application/EventosDeSegurancaTests.cs`:

```csharp
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

        await new EditarUsuarioManipulador(_c.Como(_c.Diretor), _c.Usuarios, _c.Perfis, _c.Unidade, _c.Auditoria)
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

        await new EditarUsuarioManipulador(_c.Como(_c.Diretor), _c.Usuarios, _c.Perfis, _c.Unidade, _c.Auditoria)
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
```

Acrescente a `ExecutorDeRequisicoesTests.cs`:

```csharp
    private sealed record Negada : IRequisicao<Resultado>;

    private sealed class NegadaManipulador : IManipulador<Negada, Resultado>
    {
        public Task<Resultado> ManipularAsync(Negada requisicao, CancellationToken ct) =>
            Task.FromResult(Resultado.Falha(TipoErro.AcessoNegado, "Acesso negado."));
    }

    private sealed record Invalida : IRequisicao<Resultado<int>>;

    private sealed class InvalidaManipulador : IManipulador<Invalida, Resultado<int>>
    {
        public Task<Resultado<int>> ManipularAsync(Invalida requisicao, CancellationToken ct) =>
            Task.FromResult(Resultado<int>.Falha(TipoErro.Validacao, "Inválido."));
    }

    [Fact]
    public async Task Acesso_negado_e_registrado_imediatamente_com_o_nome_da_requisicao()
    {
        var auditoria = new AuditoriaFalsa();
        var executor = new ExecutorDeRequisicoes(
            new ProvedorFalso().Com<IManipulador<Negada, Resultado>>(new NegadaManipulador()), auditoria);

        var resultado = await executor.ExecutarAsync(new Negada(), default);

        resultado.Erro.Should().Be(TipoErro.AcessoNegado);
        var evento = auditoria.Eventos.Should().ContainSingle().Subject;
        evento.Evento.Should().Be(EventosDeSeguranca.AcessoNegado);
        evento.Imediato.Should().BeTrue();
        evento.Dados.Should().BeEquivalentTo(new { requisicao = "Negada" });
    }

    [Fact]
    public async Task Outras_falhas_nao_geram_evento_de_acesso_negado()
    {
        var auditoria = new AuditoriaFalsa();
        var executor = new ExecutorDeRequisicoes(
            new ProvedorFalso().Com<IManipulador<Invalida, Resultado<int>>>(new InvalidaManipulador()), auditoria);

        await executor.ExecutarAsync(new Invalida(), default);

        auditoria.Eventos.Should().BeEmpty();
    }
```

com `using AtividadesJuridicas.Application.Auditoria;`.

`tests/AtividadesJuridicas.IntegrationTests/AplicacaoDeTeste.cs`:

```csharp
using System.Net;
using System.Text.RegularExpressions;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Infrastructure.Persistencia;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>A aplicação Web inteira contra um banco novo, com atalhos para os testes de página.</summary>
public sealed partial class AplicacaoDeTeste(string conexao) : WebApplicationFactory<Program>
{
    public const string EmailDoAdmin = "diana@x.com";
    public const string SenhaDoAdmin = "senha-segura";
    public const string EmailDoPedro = "pedro@x.com";
    public const string SenhaDoPedro = "senha-do-pedro";

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        // Acrescentada por último, esta fonte vence o appsettings.Development.json.
        builder.ConfigureAppConfiguration(c => c.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Padrao"] = conexao,
            ["AdminInicial:Nome"] = "Diana Diretora",
            ["AdminInicial:Email"] = EmailDoAdmin,
            ["AdminInicial:Senha"] = SenhaDoAdmin,
        }));

    [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"")]
    private static partial Regex TokenRegex();

    public static string Token(string html) => TokenRegex().Match(html).Groups[1].Value;

    public HttpClient NovoCliente() =>
        CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    public static async Task<HttpResponseMessage> EntrarAsync(HttpClient cliente, string email, string senha)
    {
        var pagina = await cliente.GetStringAsync("/conta/entrar");
        return await cliente.PostAsync("/conta/entrar", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["_handler"] = "entrar",
            ["__RequestVerificationToken"] = Token(pagina),
            ["Entrada.Email"] = email,
            ["Entrada.Senha"] = senha,
        }));
    }

    public async Task<HttpClient> ClienteAutenticadoAsync(string email = EmailDoAdmin, string senha = SenhaDoAdmin)
    {
        var cliente = NovoCliente();
        (await EntrarAsync(cliente, email, senha)).StatusCode.Should().Be(HttpStatusCode.Redirect);
        return cliente;
    }

    public async Task<T> EmEscopoAsync<T>(Guid? usuarioId, Func<IServiceProvider, Task<T>> acao)
    {
        await using var escopo = Services.CreateAsyncScope();
        escopo.ServiceProvider.GetRequiredService<IdentidadeAtual>().Definir(usuarioId);
        return await acao(escopo.ServiceProvider);
    }

    public Task<TResposta> ExecutarAsync<TResposta>(Guid? usuarioId, IRequisicao<TResposta> requisicao) =>
        EmEscopoAsync(usuarioId, sp => sp.GetRequiredService<ExecutorDeRequisicoes>().ExecutarAsync(requisicao, default));

    public Task<Guid> IdDoUsuarioAsync(string email) => EmEscopoAsync(null, async sp =>
        (await sp.GetRequiredService<AppDbContext>().Usuarios.AsNoTracking()
            .SingleAsync(u => u.Email == Email.Criar(email))).Id);

    public async Task<Guid> CriarPedroAsync()
    {
        var adminId = await IdDoUsuarioAsync(EmailDoAdmin);
        var producao = await EmEscopoAsync(null, async sp =>
            (await sp.GetRequiredService<AppDbContext>().Perfis.AsNoTracking()
                .SingleAsync(p => p.Tipo == TipoPerfil.Producao)).Id);

        var criado = await ExecutarAsync(adminId,
            new CriarUsuario("Pedro Produção", EmailDoPedro, SenhaDoPedro, producao, null));
        criado.Sucesso.Should().BeTrue(criado.Mensagem);
        return criado.Valor;
    }

    public Task<List<RegistroAuditoria>> EventosAsync(string evento) => EmEscopoAsync(null, sp =>
        sp.GetRequiredService<AppDbContext>().RegistrosAuditoria.AsNoTracking()
            .Where(r => r.Evento == evento).OrderBy(r => r.Id).ToListAsync());
}
```

`tests/AtividadesJuridicas.IntegrationTests/EventosDeSegurancaWebTests.cs`:

```csharp
using System.Net;
using System.Text.Json;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class EventosDeSegurancaWebTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco.NovaConexao());

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _app.DisposeAsync();

    [Fact]
    public async Task Producao_tentando_listar_usuarios_gera_evento_de_acesso_negado()
    {
        var pedroId = await _app.CriarPedroAsync();

        // A página nem chega a pedir a lista a quem não é da Diretoria; a tentativa é feita direto no caso de uso.
        (await _app.ExecutarAsync(pedroId, new ListarUsuarios())).Sucesso.Should().BeFalse();

        var evento = (await _app.EventosAsync(EventosDeSeguranca.AcessoNegado)).Should().ContainSingle().Subject;
        evento.UsuarioId.Should().Be(pedroId);
        evento.Operacao.Should().Be(OperacaoAuditada.Evento);
        evento.Dados.Should().Contain("ListarUsuarios");
    }

    [Fact]
    public async Task Sair_gera_evento_de_logout_do_usuario()
    {
        var adminId = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        var cliente = await _app.ClienteAutenticadoAsync();
        var token = AplicacaoDeTeste.Token(await cliente.GetStringAsync("/"));

        var saida = await cliente.PostAsync("/conta/sair", new FormUrlEncodedContent(
            new Dictionary<string, string> { ["__RequestVerificationToken"] = token }));

        saida.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var evento = (await _app.EventosAsync(EventosDeSeguranca.Logout)).Should().ContainSingle().Subject;
        evento.UsuarioId.Should().Be(adminId);
        evento.EntidadeId.Should().Be(adminId);
    }

    [Fact]
    public async Task Evento_anotado_e_gravado_na_mesma_transacao_da_alteracao()
    {
        var adminId = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        var pedroId = await _app.CriarPedroAsync();

        (await _app.ExecutarAsync(adminId, new DefinirAtivoUsuario(pedroId, false))).Sucesso.Should().BeTrue();

        var evento = (await _app.EventosAsync(EventosDeSeguranca.UsuarioInativado)).Should().ContainSingle().Subject;
        evento.UsuarioId.Should().Be(adminId);
        evento.EntidadeId.Should().Be(pedroId);

        var alteracao = await _app.EmEscopoAsync(null, sp => sp.GetRequiredService<AppDbContext>().RegistrosAuditoria
            .AsNoTracking().SingleAsync(r => r.EntidadeId == pedroId && r.Operacao == OperacaoAuditada.Alteracao));
        alteracao.IdCorrelacao.Should().Be(evento.IdCorrelacao, "evento e alteração nascem da mesma requisição");
    }

    [Fact]
    public async Task Registro_imediato_nao_grava_alteracoes_pendentes_do_escopo()
    {
        var adminId = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);

        await _app.EmEscopoAsync(adminId, async sp =>
        {
            var usuario = await sp.GetRequiredService<IUsuarioRepositorio>().ObterPorIdAsync(adminId, default);
            usuario!.AlterarDados("Nome Não Salvo", usuario.Email, null);
            await sp.GetRequiredService<IAuditoria>().RegistrarAsync("evento_de_teste", dados: new { motivo = "teste" });
            return 0;
        });

        var evento = (await _app.EventosAsync("evento_de_teste")).Should().ContainSingle().Subject;
        JsonDocument.Parse(evento.Dados!).RootElement.GetProperty("motivo").GetString().Should().Be("teste");

        var nome = await _app.EmEscopoAsync(null, async sp =>
            (await sp.GetRequiredService<AppDbContext>().Usuarios.AsNoTracking().SingleAsync(u => u.Id == adminId)).Nome);
        nome.Should().Be("Diana Diretora");
    }
}
```

- [ ] **Step 3: Rodar e confirmar a falha**

Run: `dotnet test`
Expected: erro de compilação (`IAuditoria` não existe).

- [ ] **Step 4: Implementar a porta e o registrador**

`src/AtividadesJuridicas.Application/Auditoria/IAuditoria.cs`:

```csharp
namespace AtividadesJuridicas.Application.Auditoria;

public static class EventosDeSeguranca
{
    public const string LoginSucesso = "login_sucesso";
    public const string LoginFalha = "login_falha";
    public const string LoginBloqueado = "login_bloqueado";
    public const string Logout = "logout";
    public const string SenhaAlterada = "senha_alterada";
    public const string SenhaRedefinida = "senha_redefinida";
    public const string PerfilAlterado = "perfil_alterado";
    public const string UsuarioAtivado = "usuario_ativado";
    public const string UsuarioInativado = "usuario_inativado";
    public const string AcessoNegado = "acesso_negado";
}

public interface IAuditoria
{
    /// <summary>
    /// Deixa o evento pendente; ele é gravado pelo próximo <c>SalvarAsync</c>, na mesma transação
    /// da alteração que o acompanha. Se nada for salvo, o evento não é gravado.
    /// </summary>
    void Anotar(string evento, string? entidade = null, Guid? entidadeId = null, object? dados = null);

    /// <summary>
    /// Grava o evento imediatamente, em conexão própria. Não grava nem descarta o que estiver
    /// pendente no escopo. Para fatos que não acompanham uma alteração (login, acesso negado).
    /// </summary>
    Task RegistrarAsync(string evento, string? entidade = null, Guid? entidadeId = null, object? dados = null,
        CancellationToken ct = default);
}
```

`src/AtividadesJuridicas.Infrastructure/Persistencia/RegistradorDeAuditoria.cs`:

```csharp
using System.Text.Encodings.Web;
using System.Text.Json;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;
using Microsoft.EntityFrameworkCore;

namespace AtividadesJuridicas.Infrastructure.Persistencia;

public sealed class RegistradorDeAuditoria(
    AppDbContext db,
    DbContextOptions<AppDbContext> opcoes,
    IIdentidadeAtual identidade,
    IRelogio relogio) : IAuditoria
{
    private static readonly JsonSerializerOptions Json = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public void Anotar(string evento, string? entidade = null, Guid? entidadeId = null, object? dados = null) =>
        db.Add(Criar(evento, entidade, entidadeId, dados));

    public async Task RegistrarAsync(string evento, string? entidade = null, Guid? entidadeId = null,
        object? dados = null, CancellationToken ct = default)
    {
        // Contexto isolado: a gravação não arrasta nem descarta o que o escopo tiver pendente.
        await using var isolado = new AppDbContext(opcoes);
        isolado.Add(Criar(evento, entidade, entidadeId, dados));
        await isolado.SaveChangesAsync(ct);
    }

    private RegistroAuditoria Criar(string evento, string? entidade, Guid? entidadeId, object? dados) =>
        RegistroAuditoria.DeEvento(
            relogio.AgoraUtc, identidade.UsuarioId, evento, entidade, entidadeId,
            dados is null ? null : JsonSerializer.Serialize(dados, Json),
            identidade.Ip, identidade.IdCorrelacao);
}
```

Em `DependencyInjection.cs` (Infrastructure): `servicos.AddScoped<IAuditoria, RegistradorDeAuditoria>();`.

- [ ] **Step 5: Registrar acesso negado no executor**

Em `ExecutorDeRequisicoes.cs`, o construtor passa a ser `ExecutorDeRequisicoes(IServiceProvider servicos, IAuditoria auditoria)` e o corpo do `try` passa a ser:

```csharp
            var resposta = await (Task<TResposta>)metodo.Invoke(manipulador, [requisicao, ct])!;

            // O login tem eventos próprios; as demais recusas são tentativas de acesso negadas.
            if (resposta is Resultado { Erro: TipoErro.AcessoNegado } && requisicao is not AutenticarUsuario)
                await auditoria.RegistrarAsync(
                    EventosDeSeguranca.AcessoNegado, dados: new { requisicao = requisicao.GetType().Name }, ct: ct);

            return resposta;
```

com os `using` de `AtividadesJuridicas.Application.Auditoria` e `AtividadesJuridicas.Application.Identidade`.

- [ ] **Step 6: Anotar os eventos nos casos de uso**

Em cada manipulador, acrescente `IAuditoria auditoria` como **último** parâmetro do construtor e o `using AtividadesJuridicas.Application.Auditoria;`.

`EditarUsuario.cs` — dentro do `try`, a troca de perfil passa a ser:

```csharp
            if (perfil.Id != usuario.PerfilId)
            {
                auditoria.Anotar(EventosDeSeguranca.PerfilAlterado, "Usuario", usuario.Id,
                    new { de = usuario.PerfilId, para = perfil.Id });
                usuario.AlterarPerfil(perfil);
            }
```

`DefinirAtivoUsuario.cs` — depois de localizar o usuário, o restante do método passa a ser:

```csharp
        if (usuario.Ativo == requisicao.Ativo) return Resultado.Ok();

        if (requisicao.Ativo)
        {
            usuario.Ativar();
            auditoria.Anotar(EventosDeSeguranca.UsuarioAtivado, "Usuario", usuario.Id);
        }
        else
        {
            if (await UltimoDiretor.EhAsync(usuario, usuarios, perfis, ct))
                return Resultado.Falha(TipoErro.Conflito, UltimoDiretor.Mensagem);
            usuario.Inativar();
            auditoria.Anotar(EventosDeSeguranca.UsuarioInativado, "Usuario", usuario.Id);
        }

        await unidade.SalvarAsync(ct);
        return Resultado.Ok();
```

`RedefinirSenhaUsuario.cs` — logo após `usuario.DefinirSenha(...)`:

```csharp
        auditoria.Anotar(EventosDeSeguranca.SenhaRedefinida, "Usuario", usuario.Id);
```

`AlterarMinhaSenha.cs` — logo após `usuario.DefinirSenha(...)`:

```csharp
        auditoria.Anotar(EventosDeSeguranca.SenhaAlterada, "Usuario", usuario.Id);
```

- [ ] **Step 7: Registrar o logout**

Em `Program.cs`, o endpoint de saída passa a ser:

```csharp
app.MapPost("/conta/sair", async (HttpContext contexto, IAntiforgery antiforgery, IAuditoria auditoria) =>
{
    if (!await antiforgery.IsRequestValidAsync(contexto))
        return Results.BadRequest();

    IdentidadeHttp.Definir(contexto);
    await auditoria.RegistrarAsync(
        EventosDeSeguranca.Logout, "Usuario", IdentidadeHttp.UsuarioId(contexto.User), ct: contexto.RequestAborted);

    await contexto.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.LocalRedirect("/conta/entrar");
}).DisableAntiforgery();
```

com `using AtividadesJuridicas.Application.Auditoria;`.

- [ ] **Step 8: Rodar e confirmar que passa**

Run: `dotnet test`
Expected: tudo aprovado, zero avisos.

- [ ] **Step 9: Commit**

```bash
git add -A
git commit -m "feat(auditoria): eventos de segurança, acesso negado e logout"
```

---

### Task 5: Registro de consultas

**Files:**
- Create: `src/AtividadesJuridicas.Domain/Auditoria/RegistroAcesso.cs`, `src/AtividadesJuridicas.Infrastructure/Persistencia/Configuracoes/RegistroAcessoConfiguracao.cs`
- Create (gerado): migration `RegistroDeAcessos`
- Modify: `src/AtividadesJuridicas.Application/Auditoria/IAuditoria.cs`, `src/AtividadesJuridicas.Application/Comum/ExecutorDeRequisicoes.cs`, `Identidade/ListarUsuarios.cs`, `Identidade/ListarPerfisAtivos.cs`
- Modify: `src/AtividadesJuridicas.Infrastructure/Persistencia/AppDbContext.cs`, `RegistradorDeAuditoria.cs`
- Modify (testes): `tests/AtividadesJuridicas.UnitTests/Application/Falsos.cs`, `ExecutorDeRequisicoesTests.cs`
- Test: `tests/AtividadesJuridicas.IntegrationTests/RegistroDeAcessosTests.cs`

**Interfaces:**
- Produces:
  - `enum AcaoDeAcesso { Listou = 1, Visualizou = 2, Baixou = 3 }`.
  - `RegistroAcesso` com `Id`, `DataHora`, `UsuarioId` (obrigatório), `TipoRecurso`, `RecursoId`, `Acao`, `Detalhe`, `Ip`; fábrica `RegistroAcesso.Criar(DateTime dataHoraUtc, Guid usuarioId, string tipoRecurso, Guid? recursoId, AcaoDeAcesso acao, string? detalhe, string? ip)`.
  - `interface IConsultaAuditavel { string TipoRecurso { get; } AcaoDeAcesso Acao { get; } Guid? RecursoId => null; string? Detalhe => null; }` em `AtividadesJuridicas.Application.Auditoria`.
  - `IAuditoria.RegistrarAcessoAsync(IConsultaAuditavel consulta, CancellationToken ct = default)`.
  - `AppDbContext.RegistrosAcesso`.
- Regra: o executor registra o acesso quando a requisição é `IConsultaAuditavel` e terminou com sucesso. Resposta que não é `Resultado` conta como sucesso. Sem usuário identificado não há registro.

- [ ] **Step 1: Atualizar o duplo e escrever os testes que falham**

Em `AuditoriaFalsa` (`Falsos.cs`), acrescente:

```csharp
    public List<IConsultaAuditavel> Acessos { get; } = [];

    public Task RegistrarAcessoAsync(IConsultaAuditavel consulta, CancellationToken ct = default)
    {
        Acessos.Add(consulta);
        return Task.CompletedTask;
    }
```

Acrescente a `ExecutorDeRequisicoesTests.cs`:

```csharp
    private sealed record Listagem(bool Falhar) : IRequisicao<Resultado<int>>, IConsultaAuditavel
    {
        public string TipoRecurso => "Coisa";
        public AcaoDeAcesso Acao => AcaoDeAcesso.Listou;
        public string? Detalhe => "filtro=x";
    }

    private sealed class ListagemManipulador : IManipulador<Listagem, Resultado<int>>
    {
        public Task<Resultado<int>> ManipularAsync(Listagem requisicao, CancellationToken ct) => Task.FromResult(
            requisicao.Falhar ? Resultado<int>.Falha(TipoErro.AcessoNegado, "Acesso negado.") : Resultado<int>.Ok(3));
    }

    [Fact]
    public async Task Consulta_bem_sucedida_registra_o_acesso()
    {
        var auditoria = new AuditoriaFalsa();
        var executor = new ExecutorDeRequisicoes(
            new ProvedorFalso().Com<IManipulador<Listagem, Resultado<int>>>(new ListagemManipulador()), auditoria);

        await executor.ExecutarAsync(new Listagem(Falhar: false), default);

        var acesso = auditoria.Acessos.Should().ContainSingle().Subject;
        acesso.TipoRecurso.Should().Be("Coisa");
        acesso.Acao.Should().Be(AcaoDeAcesso.Listou);
        acesso.Detalhe.Should().Be("filtro=x");
        auditoria.Eventos.Should().BeEmpty();
    }

    [Fact]
    public async Task Consulta_recusada_nao_registra_acesso_mas_registra_a_tentativa()
    {
        var auditoria = new AuditoriaFalsa();
        var executor = new ExecutorDeRequisicoes(
            new ProvedorFalso().Com<IManipulador<Listagem, Resultado<int>>>(new ListagemManipulador()), auditoria);

        await executor.ExecutarAsync(new Listagem(Falhar: true), default);

        auditoria.Acessos.Should().BeEmpty();
        auditoria.Eventos.Should().ContainSingle().Which.Evento.Should().Be(EventosDeSeguranca.AcessoNegado);
    }
```

com `using AtividadesJuridicas.Domain.Auditoria;`.

`tests/AtividadesJuridicas.IntegrationTests/RegistroDeAcessosTests.cs`:

```csharp
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class RegistroDeAcessosTests(BancoFixture banco) : TesteComBanco(banco)
{
    private Task<List<RegistroAcesso>> AcessosAsync() => EmEscopoAsync(null, sp =>
        sp.GetRequiredService<AppDbContext>().RegistrosAcesso.AsNoTracking().OrderBy(r => r.Id).ToListAsync());

    [Fact]
    public async Task Listar_usuarios_registra_quem_listou_o_que_e_de_onde()
    {
        var adminId = await IdDoAdminAsync();

        (await ExecutarAsync(adminId, new ListarUsuarios())).Sucesso.Should().BeTrue();

        var acesso = (await AcessosAsync()).Should().ContainSingle().Subject;
        acesso.UsuarioId.Should().Be(adminId);
        acesso.TipoRecurso.Should().Be("Usuario");
        acesso.Acao.Should().Be(AcaoDeAcesso.Listou);
        acesso.RecursoId.Should().BeNull();
        acesso.Ip.Should().Be("127.0.0.9");
        acesso.DataHora.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public async Task Consulta_recusada_nao_gera_registro_de_acesso()
    {
        var adminId = await IdDoAdminAsync();
        var producao = await IdDoPerfilAsync(TipoPerfil.Producao);
        var pedro = await ExecutarAsync(adminId, new CriarUsuario("Pedro", "pedro@x.com", "senha-do-pedro", producao, null));

        (await ExecutarAsync(pedro.Valor, new ListarUsuarios())).Sucesso.Should().BeFalse();

        (await AcessosAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Saber_quem_sou_nao_gera_registro_de_acesso()
    {
        var adminId = await IdDoAdminAsync();

        (await ExecutarAsync(adminId, new ObterMeuAcesso())).Should().NotBeNull();

        (await AcessosAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Comandos_nao_geram_registro_de_acesso()
    {
        var adminId = await IdDoAdminAsync();
        var producao = await IdDoPerfilAsync(TipoPerfil.Producao);

        await ExecutarAsync(adminId, new CriarUsuario("Pedro", "pedro@x.com", "senha-do-pedro", producao, null));

        (await AcessosAsync()).Should().BeEmpty();
    }
}
```

- [ ] **Step 2: Rodar e confirmar a falha**

Run: `dotnet test`
Expected: erro de compilação (`IConsultaAuditavel`, `RegistroAcesso` não existem).

- [ ] **Step 3: Implementar**

`src/AtividadesJuridicas.Domain/Auditoria/RegistroAcesso.cs`:

```csharp
namespace AtividadesJuridicas.Domain.Auditoria;

public enum AcaoDeAcesso
{
    Listou = 1,
    Visualizou = 2,
    Baixou = 3,
}

/// <summary>Uma consulta feita por um usuário. Somente inserção.</summary>
public sealed class RegistroAcesso
{
    private RegistroAcesso() { }

    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public DateTime DataHora { get; private set; }
    public Guid UsuarioId { get; private set; }
    public string TipoRecurso { get; private set; } = null!;
    public Guid? RecursoId { get; private set; }
    public AcaoDeAcesso Acao { get; private set; }
    public string? Detalhe { get; private set; }
    public string? Ip { get; private set; }

    public static RegistroAcesso Criar(
        DateTime dataHoraUtc, Guid usuarioId, string tipoRecurso, Guid? recursoId, AcaoDeAcesso acao,
        string? detalhe, string? ip)
    {
        if (dataHoraUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("A data deve estar em UTC.", nameof(dataHoraUtc));
        if (string.IsNullOrWhiteSpace(tipoRecurso))
            throw new ArgumentException("O tipo do recurso é obrigatório.", nameof(tipoRecurso));

        return new RegistroAcesso
        {
            DataHora = dataHoraUtc, UsuarioId = usuarioId, TipoRecurso = tipoRecurso, RecursoId = recursoId,
            Acao = acao, Detalhe = detalhe, Ip = ip,
        };
    }
}
```

Em `IAuditoria.cs`, acrescente a interface e o método:

```csharp
/// <summary>Marca uma requisição de consulta: o executor registra o acesso quando ela termina bem.</summary>
public interface IConsultaAuditavel
{
    string TipoRecurso { get; }
    AcaoDeAcesso Acao { get; }
    Guid? RecursoId => null;

    /// <summary>Filtros usados, quando houver.</summary>
    string? Detalhe => null;
}
```

```csharp
    /// <summary>Grava imediatamente quem consultou o quê. Sem usuário identificado, não grava.</summary>
    Task RegistrarAcessoAsync(IConsultaAuditavel consulta, CancellationToken ct = default);
```

com `using AtividadesJuridicas.Domain.Auditoria;`.

`RegistroAcessoConfiguracao.cs`:

```csharp
using AtividadesJuridicas.Domain.Auditoria;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AtividadesJuridicas.Infrastructure.Persistencia.Configuracoes;

public sealed class RegistroAcessoConfiguracao : IEntityTypeConfiguration<RegistroAcesso>
{
    public void Configure(EntityTypeBuilder<RegistroAcesso> b)
    {
        b.ToTable("registros_acesso");
        b.HasKey(r => r.Id);
        b.Property(r => r.Id).ValueGeneratedNever();
        b.Property(r => r.TipoRecurso).HasMaxLength(100).IsRequired();
        b.Property(r => r.Acao).HasConversion<int>();
        b.Property(r => r.Detalhe).HasMaxLength(2000);
        b.Property(r => r.Ip).HasMaxLength(45);

        b.HasIndex(r => r.DataHora);
        b.HasIndex(r => r.UsuarioId);
        b.HasIndex(r => new { r.TipoRecurso, r.RecursoId });
    }
}
```

Em `AppDbContext.cs`: `public DbSet<RegistroAcesso> RegistrosAcesso => Set<RegistroAcesso>();`.

Em `RegistradorDeAuditoria.cs`:

```csharp
    public async Task RegistrarAcessoAsync(IConsultaAuditavel consulta, CancellationToken ct = default)
    {
        if (identidade.UsuarioId is not { } usuarioId) return;

        await using var isolado = new AppDbContext(opcoes);
        isolado.Add(RegistroAcesso.Criar(
            relogio.AgoraUtc, usuarioId, consulta.TipoRecurso, consulta.RecursoId, consulta.Acao,
            consulta.Detalhe, identidade.Ip));
        await isolado.SaveChangesAsync(ct);
    }
```

Em `ExecutorDeRequisicoes.cs`, depois do bloco de acesso negado e antes do `return resposta;`:

```csharp
            // Se o registro do acesso falhar, a exceção sobe e a consulta falha.
            if (requisicao is IConsultaAuditavel consulta && resposta is not Resultado { Sucesso: false })
                await auditoria.RegistrarAcessoAsync(consulta, ct);
```

`ListarUsuarios.cs` — a requisição passa a ser:

```csharp
public sealed record ListarUsuarios : IRequisicao<Resultado<IReadOnlyList<UsuarioResumo>>>, IConsultaAuditavel
{
    public string TipoRecurso => "Usuario";
    public AcaoDeAcesso Acao => AcaoDeAcesso.Listou;
}
```

`ListarPerfisAtivos.cs` — a requisição passa a ser:

```csharp
public sealed record ListarPerfisAtivos : IRequisicao<Resultado<IReadOnlyList<PerfilResumo>>>, IConsultaAuditavel
{
    public string TipoRecurso => "Perfil";
    public AcaoDeAcesso Acao => AcaoDeAcesso.Listou;
}
```

(ambos com os `using` de `AtividadesJuridicas.Application.Auditoria` e `AtividadesJuridicas.Domain.Auditoria`).

- [ ] **Step 4: Gerar a migration**

Run:

```bash
dotnet ef migrations add RegistroDeAcessos --project src/AtividadesJuridicas.Infrastructure --startup-project src/AtividadesJuridicas.Infrastructure --output-dir Persistencia/Migrations
```

Expected: cria só a tabela `registros_acesso` com os três índices.

- [ ] **Step 5: Rodar e confirmar que passa**

Run: `dotnet test`
Expected: tudo aprovado, zero avisos.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "feat(auditoria): registro de consultas pelo executor de requisições"
```

---

### Task 6: Sessão revogável

**Files:**
- Modify: `src/AtividadesJuridicas.Domain/Identidade/Usuario.cs`
- Modify: `src/AtividadesJuridicas.Application/Identidade/IdentidadeAtual.cs`, `ContextoDeAcesso.cs`, `AutenticarUsuario.cs`
- Modify: `src/AtividadesJuridicas.Infrastructure/Persistencia/Configuracoes/UsuarioConfiguracao.cs` (se necessário) e migration `SessaoEBloqueio` (gerada na Tarefa 7; ver nota)
- Modify: `src/AtividadesJuridicas.Web/Infra/IdentidadeHttp.cs`, `Despachante.cs`, `ValidacaoDeSessao.cs`, `Components/Pages/Conta/Entrar.razor`, `Components/Pages/MinhaSenha.razor`
- Modify (testes): `tests/AtividadesJuridicas.UnitTests/Application/AutenticarUsuarioTests.cs`, `ContextoDeAcessoTests.cs`, `tests/AtividadesJuridicas.UnitTests/Domain/UsuarioTests.cs`, `tests/AtividadesJuridicas.IntegrationTests/PersistenciaTests.cs` (tipo do resultado do login)
- Test: `tests/AtividadesJuridicas.IntegrationTests/SessaoWebTests.cs`

**Nota sobre a migration:** esta tarefa acrescenta a coluna `versao_de_sessao`. Gere aqui a migration `VersaoDeSessao`; a Tarefa 7 gera outra para as colunas de bloqueio.

**Interfaces:**
- Produces:
  - `Usuario.VersaoDeSessao` (`Guid`, `[NaoAuditar]`): nova a cada `DefinirSenha` e a cada `Inativar`.
  - `record SessaoIniciada(Guid UsuarioId, string Nome, TipoPerfil TipoPerfil, Guid VersaoDeSessao)`; `AutenticarUsuario : IRequisicao<Resultado<SessaoIniciada>>`.
  - `IIdentidadeAtual.VersaoDeSessao` (`Guid?`); `IdentidadeAtual.Definir(Guid? usuarioId, string? ip = null, Guid? versaoDeSessao = null)`.
  - `ContextoDeAcesso`: devolve nulo quando a identidade traz uma versão diferente da vigente do usuário. Versão nula significa chamador interno confiável (testes, rotinas) e não é comparada.
  - `IdentidadeHttp.ClaimDaVersao = "atividades:sessao"` e `IdentidadeHttp.VersaoDeSessao(ClaimsPrincipal)`, que devolve `Guid.Empty` quando a claim falta, de modo que um cookie sem versão é sempre recusado.

- [ ] **Step 1: Escrever os testes que falham**

Acrescente a `UsuarioTests.cs`:

```csharp
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
```

Acrescente a `ContextoDeAcessoTests.cs` (o auxiliar `Criar` passa a aceitar a versão):

```csharp
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
```

Em `AutenticarUsuarioTests.cs`, o tipo do resultado passa a ser `Resultado<SessaoIniciada>` e a asserção do login correto passa a ser:

```csharp
        resultado.Valor.Should().Be(new SessaoIniciada(
            _c.Diretor.Id, "Diana Diretora", TipoPerfil.Diretoria, _c.Diretor.VersaoDeSessao));
```

Em `PersistenciaTests.cs`, onde o manipulador de login é resolvido, troque o tipo para `IManipulador<AutenticarUsuario, Resultado<SessaoIniciada>>`; onde o teste lia `login.Valor!.TipoPerfil`, a propriedade continua existindo.

`tests/AtividadesJuridicas.IntegrationTests/SessaoWebTests.cs`:

```csharp
using System.Net;
using AtividadesJuridicas.Application.Identidade;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class SessaoWebTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco.NovaConexao());

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _app.DisposeAsync();

    [Fact]
    public async Task Redefinir_a_senha_derruba_a_sessao_aberta_do_usuario()
    {
        var adminId = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        var pedroId = await _app.CriarPedroAsync();
        var doPedro = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);
        (await doPedro.GetAsync("/")).StatusCode.Should().Be(HttpStatusCode.OK);

        (await _app.ExecutarAsync(adminId, new RedefinirSenhaUsuario(pedroId, "senha-nova-do-pedro")))
            .Sucesso.Should().BeTrue();

        (await doPedro.GetAsync("/")).StatusCode.Should().Be(HttpStatusCode.Redirect);
        (await AplicacaoDeTeste.EntrarAsync(_app.NovoCliente(), AplicacaoDeTeste.EmailDoPedro, "senha-nova-do-pedro"))
            .StatusCode.Should().Be(HttpStatusCode.Redirect, "a senha nova abre uma sessão nova");
    }

    [Fact]
    public async Task Inativar_e_reativar_nao_revive_a_sessao_antiga()
    {
        var adminId = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        var pedroId = await _app.CriarPedroAsync();
        var doPedro = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        await _app.ExecutarAsync(adminId, new DefinirAtivoUsuario(pedroId, false));
        await _app.ExecutarAsync(adminId, new DefinirAtivoUsuario(pedroId, true));

        (await doPedro.GetAsync("/")).StatusCode.Should().Be(HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task Sessao_de_outro_usuario_nao_e_afetada()
    {
        var adminId = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        var pedroId = await _app.CriarPedroAsync();
        var doAdmin = await _app.ClienteAutenticadoAsync();

        await _app.ExecutarAsync(adminId, new RedefinirSenhaUsuario(pedroId, "senha-nova-do-pedro"));

        (await doAdmin.GetAsync("/")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Pagina_ja_renderizada_de_sessao_revogada_nao_mostra_dados()
    {
        var adminId = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        var doAdmin = await _app.ClienteAutenticadoAsync();

        // Um segundo diretor redefine a senha do primeiro.
        var diretoria = await _app.EmEscopoAsync(null, async sp =>
            (await sp.GetRequiredService<IPerfilRepositorio>().ListarAtivosAsync(default))
                .Single(p => p.Tipo == Domain.Identidade.TipoPerfil.Diretoria).Id);
        var dora = await _app.ExecutarAsync(adminId,
            new CriarUsuario("Dora", "dora@x.com", "senha-da-dora", diretoria, null));
        await _app.ExecutarAsync(dora.Valor, new RedefinirSenhaUsuario(adminId, "senha-nova-da-diana"));

        var resposta = await doAdmin.GetAsync("/usuarios");

        resposta.StatusCode.Should().Be(HttpStatusCode.Redirect);
    }
}
```

com `using Microsoft.Extensions.DependencyInjection;`.

- [ ] **Step 2: Rodar e confirmar a falha**

Run: `dotnet test`
Expected: erro de compilação (`VersaoDeSessao`, `SessaoIniciada` não existem).

- [ ] **Step 3: Implementar no domínio e na aplicação**

Em `Usuario.cs`:

```csharp
    /// <summary>Geração das sessões do usuário. Uma sessão aberta com outra versão é recusada.</summary>
    [NaoAuditar]
    public Guid VersaoDeSessao { get; private set; }
```

e os métodos passam a ser:

```csharp
    public void DefinirSenha(string senhaHash)
    {
        if (string.IsNullOrWhiteSpace(senhaHash))
            throw new ArgumentException("O hash da senha é obrigatório.", nameof(senhaHash));
        SenhaHash = senhaHash;
        RenovarSessao();
    }

    public void Inativar()
    {
        Ativo = false;
        RenovarSessao();
    }

    private void RenovarSessao() => VersaoDeSessao = Guid.CreateVersion7();
```

(`Criar` já chama `DefinirSenha`, então todo usuário nasce com versão.)

Em `IdentidadeAtual.cs`: acrescente `Guid? VersaoDeSessao { get; }` à interface, a propriedade com `private set` à classe, e o método passa a ser:

```csharp
    public void Definir(Guid? usuarioId, string? ip = null, Guid? versaoDeSessao = null)
    {
        UsuarioId = usuarioId;
        Ip = ip;
        VersaoDeSessao = versaoDeSessao;
    }
```

com o comentário na interface: `/// <summary>Versão que a sessão trouxe. Nulo quando o chamador é interno e confiável (testes, rotinas).</summary>`.

Em `ContextoDeAcesso.cs`, depois da verificação de `Ativo`:

```csharp
        // Sessão aberta antes de uma troca de senha ou de uma inativação.
        if (identidade.VersaoDeSessao is { } versao && versao != usuario.VersaoDeSessao) return null;
```

Em `AutenticarUsuario.cs`: acrescente o registro

```csharp
public sealed record SessaoIniciada(Guid UsuarioId, string Nome, TipoPerfil TipoPerfil, Guid VersaoDeSessao);
```

troque `Resultado<UsuarioAutenticado>` por `Resultado<SessaoIniciada>` em todo o arquivo, e o retorno de sucesso passa a ser:

```csharp
        return Resultado<SessaoIniciada>.Ok(
            new SessaoIniciada(usuario.Id, usuario.Nome, perfil.Tipo, usuario.VersaoDeSessao));
```

- [ ] **Step 4: Implementar na Web**

Em `IdentidadeHttp.cs`:

```csharp
    public const string ClaimDaVersao = "atividades:sessao";

    /// <summary>Cookie sem a claim devolve Guid.Empty, que nunca coincide com a versão de um usuário.</summary>
    public static Guid VersaoDeSessao(ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(ClaimDaVersao), out var versao) ? versao : Guid.Empty;
```

e `Definir(HttpContext)` passa a enviar também `VersaoDeSessao(contexto.User)` quando houver usuário identificado:

```csharp
    public static void Definir(HttpContext contexto)
    {
        var usuarioId = UsuarioId(contexto.User);
        contexto.RequestServices.GetRequiredService<IdentidadeAtual>().Definir(
            usuarioId,
            contexto.Connection.RemoteIpAddress?.ToString(),
            usuarioId is null ? null : VersaoDeSessao(contexto.User));
    }
```

Em `Despachante.cs`, a identidade passa a ser fixada assim:

```csharp
        var usuarioId = IdentidadeHttp.UsuarioId(estado.User);
        escopo.ServiceProvider.GetRequiredService<IdentidadeAtual>().Definir(
            usuarioId, origem.Ip, usuarioId is null ? null : IdentidadeHttp.VersaoDeSessao(estado.User));
```

`ValidacaoDeSessao.cs` — a verificação passa a ser:

```csharp
        var principal = contexto.Principal;
        var valido =
            principal is not null &&
            IdentidadeHttp.UsuarioId(principal) is { } id &&
            await usuarios.ObterPorIdAsync(id, contexto.HttpContext.RequestAborted) is { Ativo: true } usuario &&
            usuario.VersaoDeSessao == IdentidadeHttp.VersaoDeSessao(principal);
```

e o comentário do método: `/// <summary>A cada requisição HTTP, derruba o cookie de quem foi inativado, não existe mais ou teve as sessões renovadas.</summary>`.

Em `Entrar.razor`, as claims passam a ser:

```csharp
        var identidade = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, resultado.Valor!.UsuarioId.ToString()),
            new Claim(ClaimTypes.Name, resultado.Valor.Nome),
            new Claim(IdentidadeHttp.ClaimDaVersao, resultado.Valor.VersaoDeSessao.ToString()),
        ], CookieAuthenticationDefaults.AuthenticationScheme);
```

Ainda em `Entrar.razor`, mostre o aviso de senha alterada. Acima do formulário:

```razor
@if (Aviso == "senha-alterada")
{
    <p class="ok" role="status">Senha alterada. Entre novamente com a nova senha.</p>
}
```

e no `@code`:

```csharp
    [SupplyParameterFromQuery]
    private string? Aviso { get; set; }
```

Em `MinhaSenha.razor`: injete `NavigationManager Navegacao` e, no sucesso, em vez de mostrar "Senha alterada.", navegue:

```csharp
        if (resultado.Sucesso)
        {
            // A troca renovou as sessões do usuário, inclusive esta: volta ao login.
            Navegacao.NavigateTo("/conta/entrar?aviso=senha-alterada", forceLoad: true);
            return;
        }

        _falhou = true;
        _mensagem = resultado.Mensagem;
```

- [ ] **Step 5: Gerar a migration**

Run:

```bash
dotnet ef migrations add VersaoDeSessao --project src/AtividadesJuridicas.Infrastructure --startup-project src/AtividadesJuridicas.Infrastructure --output-dir Persistencia/Migrations
```

Expected: acrescenta só a coluna `versao_de_sessao uuid NOT NULL` em `usuarios`, com valor padrão de zeros para linhas existentes. Isso é intencional: cookies emitidos antes desta versão não têm a claim e são recusados, e a versão zerada é substituída na primeira troca de senha.

- [ ] **Step 6: Rodar e confirmar que passa**

Run: `dotnet test`
Expected: tudo aprovado, zero avisos.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "feat(identidade): sessões revogadas por troca de senha e inativação"
```

---

### Task 7: Bloqueio de login e eventos de login

**Files:**
- Modify: `src/AtividadesJuridicas.Domain/Identidade/Usuario.cs`
- Modify: `src/AtividadesJuridicas.Application/Identidade/Portas.cs`, `AutenticarUsuario.cs`, `RedefinirSenhaUsuario.cs`
- Modify: `src/AtividadesJuridicas.Infrastructure/Persistencia/Repositorios/UsuarioRepositorio.cs`, `src/AtividadesJuridicas.Infrastructure/Seguranca/HasherDeSenhaPbkdf2.cs`
- Create (gerado): migration `BloqueioDeLogin`
- Modify (testes): `tests/AtividadesJuridicas.UnitTests/Application/Falsos.cs`, `AutenticarUsuarioTests.cs`, `tests/AtividadesJuridicas.UnitTests/Domain/UsuarioTests.cs`
- Test: `tests/AtividadesJuridicas.IntegrationTests/BloqueioDeLoginTests.cs`

**Interfaces:**
- Produces:
  - `Usuario.MaximoDeFalhasDeLogin = 5`, `Usuario.DuracaoDoBloqueio = 15 min`, `Usuario.FalhasDeLogin` e `Usuario.BloqueadoAte` (ambos `[NaoAuditar]`), `EstaBloqueado(DateTime agoraUtc)`, `RegistrarFalhaDeLogin(DateTime agoraUtc)` (devolve `true` quando a falha causou o bloqueio), `RegistrarAcesso(DateTime agoraUtc)` (zera falhas e bloqueio), `Desbloquear()`.
  - `IUsuarioRepositorio.GravarEstadoDeLoginAsync(Usuario usuario, CancellationToken ct)` substitui `RegistrarAcessoAsync`: grava só `ultimo_acesso_em`, `falhas_de_login` e `bloqueado_ate`, fora da unidade de trabalho, sem carimbos nem auditoria de alteração.
  - `IHasherDeSenha.HashDeDescarte` (`string`): um hash válido de uma senha que ninguém conhece.
  - `AutenticarUsuarioManipulador(IUsuarioRepositorio usuarios, IPerfilRepositorio perfis, IHasherDeSenha hasher, IRelogio relogio, IAuditoria auditoria)`.
- Mensagens: credenciais inválidas continua `"E-mail ou senha inválidos."`; bloqueado é `"Muitas tentativas de login. Aguarde 15 minutos e tente novamente."` (ambas com `TipoErro.AcessoNegado`).

- [ ] **Step 1: Atualizar os duplos e escrever os testes que falham**

Em `Falsos.cs`:

- `UsuarioRepositorioFalso`: troque `RegistrarAcessoAsync` por

```csharp
    public int GravacoesDeLogin { get; private set; }

    public Task GravarEstadoDeLoginAsync(Usuario usuario, CancellationToken ct)
    {
        GravacoesDeLogin++;
        return Task.CompletedTask;
    }
```

- `HasherFalso`: acrescente

```csharp
    public int Verificacoes { get; private set; }

    public string HashDeDescarte => "hash:descarte-que-ninguem-conhece";
```

e faça `Verificar` incrementar `Verificacoes` antes de comparar.

Acrescente a `UsuarioTests.cs`:

```csharp
    private static readonly DateTime Agora = new(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Quinta_falha_seguida_bloqueia_por_quinze_minutos()
    {
        var usuario = Novo();

        for (var i = 1; i <= 4; i++)
            usuario.RegistrarFalhaDeLogin(Agora).Should().BeFalse($"a falha {i} ainda não bloqueia");
        usuario.EstaBloqueado(Agora).Should().BeFalse();

        usuario.RegistrarFalhaDeLogin(Agora).Should().BeTrue();

        usuario.EstaBloqueado(Agora).Should().BeTrue();
        usuario.EstaBloqueado(Agora.AddMinutes(14).AddSeconds(59)).Should().BeTrue();
        usuario.EstaBloqueado(Agora.AddMinutes(15)).Should().BeFalse();
    }

    [Fact]
    public void Depois_do_bloqueio_a_contagem_recomeca()
    {
        var usuario = Novo();
        for (var i = 0; i < 5; i++) usuario.RegistrarFalhaDeLogin(Agora);

        usuario.RegistrarFalhaDeLogin(Agora.AddMinutes(16)).Should().BeFalse();
    }

    [Fact]
    public void Acesso_bem_sucedido_zera_as_falhas()
    {
        var usuario = Novo();
        for (var i = 0; i < 4; i++) usuario.RegistrarFalhaDeLogin(Agora);

        usuario.RegistrarAcesso(Agora);

        usuario.FalhasDeLogin.Should().Be(0);
        for (var i = 0; i < 4; i++) usuario.RegistrarFalhaDeLogin(Agora).Should().BeFalse();
    }

    [Fact]
    public void Desbloquear_remove_o_bloqueio_e_as_falhas()
    {
        var usuario = Novo();
        for (var i = 0; i < 5; i++) usuario.RegistrarFalhaDeLogin(Agora);

        usuario.Desbloquear();

        usuario.EstaBloqueado(Agora).Should().BeFalse();
        usuario.FalhasDeLogin.Should().Be(0);
    }
```

Substitua `AutenticarUsuarioTests.cs` por:

```csharp
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.UnitTests.Application;

public class AutenticarUsuarioTests
{
    private const string Invalido = "E-mail ou senha inválidos.";
    private const string Bloqueado = "Muitas tentativas de login. Aguarde 15 minutos e tente novamente.";

    private readonly Cenario _c = new();

    private Task<Resultado<SessaoIniciada>> Autenticar(string email, string senha) =>
        new AutenticarUsuarioManipulador(_c.Usuarios, _c.Perfis, _c.Hasher, _c.Relogio, _c.Auditoria)
            .ManipularAsync(new AutenticarUsuario(email, senha), default);

    private string UltimoEvento() => _c.Auditoria.Eventos.Last().Evento;

    [Fact]
    public async Task Credenciais_corretas_autenticam_registram_o_acesso_e_o_evento()
    {
        var resultado = await Autenticar("diana@x.com", "senha-segura");

        resultado.Sucesso.Should().BeTrue();
        resultado.Valor.Should().Be(new SessaoIniciada(
            _c.Diretor.Id, "Diana Diretora", TipoPerfil.Diretoria, _c.Diretor.VersaoDeSessao));
        _c.Diretor.UltimoAcessoEm.Should().Be(_c.Relogio.AgoraUtc);
        _c.Usuarios.GravacoesDeLogin.Should().Be(1);
        _c.Unidade.Salvamentos.Should().Be(0, "registrar o acesso não é alteração de negócio");

        var evento = _c.Auditoria.Eventos.Should().ContainSingle().Subject;
        evento.Should().BeEquivalentTo(new
        {
            Evento = EventosDeSeguranca.LoginSucesso, Entidade = "Usuario", EntidadeId = _c.Diretor.Id, Imediato = true,
        });
    }

    [Fact]
    public async Task Email_com_maiusculas_e_espacos_encontra_o_mesmo_usuario()
    {
        (await Autenticar("  DIANA@X.com ", "senha-segura")).Sucesso.Should().BeTrue();
    }

    [Theory]
    [InlineData("ninguem@x.com", "senha-segura")]
    [InlineData("nao-e-email", "senha-segura")]
    [InlineData("", "")]
    public async Task Email_desconhecido_recebe_a_mensagem_generica_e_ainda_assim_gasta_uma_verificacao(
        string email, string senha)
    {
        var resultado = await Autenticar(email, senha);

        resultado.Erro.Should().Be(TipoErro.AcessoNegado);
        resultado.Mensagem.Should().Be(Invalido);
        _c.Hasher.Verificacoes.Should().Be(1, "o tempo de resposta não deve distinguir e-mail inexistente");
        UltimoEvento().Should().Be(EventosDeSeguranca.LoginFalha);
        _c.Auditoria.Eventos.Last().EntidadeId.Should().BeNull();
        _c.Usuarios.GravacoesDeLogin.Should().Be(0);
    }

    [Fact]
    public async Task Usuario_inativo_nao_autentica_mesmo_com_a_senha_certa()
    {
        _c.Diretor.Inativar();

        var resultado = await Autenticar("diana@x.com", "senha-segura");

        resultado.Mensagem.Should().Be(Invalido);
        UltimoEvento().Should().Be(EventosDeSeguranca.LoginFalha);
    }

    [Fact]
    public async Task Senha_errada_conta_a_falha_e_registra_o_evento_com_o_usuario()
    {
        var resultado = await Autenticar("diana@x.com", "senha-errada");

        resultado.Mensagem.Should().Be(Invalido);
        _c.Diretor.FalhasDeLogin.Should().Be(1);
        _c.Usuarios.GravacoesDeLogin.Should().Be(1);
        _c.Auditoria.Eventos.Last().Should().BeEquivalentTo(new
        {
            Evento = EventosDeSeguranca.LoginFalha, Entidade = "Usuario", EntidadeId = _c.Diretor.Id, Imediato = true,
        });
    }

    [Fact]
    public async Task Quinta_senha_errada_bloqueia_e_avisa()
    {
        for (var i = 0; i < 4; i++)
            (await Autenticar("diana@x.com", "senha-errada")).Mensagem.Should().Be(Invalido);

        var quinta = await Autenticar("diana@x.com", "senha-errada");

        quinta.Erro.Should().Be(TipoErro.AcessoNegado);
        quinta.Mensagem.Should().Be(Bloqueado);
        UltimoEvento().Should().Be(EventosDeSeguranca.LoginBloqueado);
    }

    [Fact]
    public async Task Senha_correta_durante_o_bloqueio_e_recusada_sem_verificar_a_senha()
    {
        for (var i = 0; i < 5; i++) await Autenticar("diana@x.com", "senha-errada");
        var verificacoesAntes = _c.Hasher.Verificacoes;

        var resultado = await Autenticar("diana@x.com", "senha-segura");

        resultado.Sucesso.Should().BeFalse();
        resultado.Mensagem.Should().Be(Bloqueado);
        _c.Hasher.Verificacoes.Should().Be(verificacoesAntes);
        UltimoEvento().Should().Be(EventosDeSeguranca.LoginFalha);
    }

    [Fact]
    public async Task Passados_quinze_minutos_a_senha_correta_volta_a_funcionar()
    {
        for (var i = 0; i < 5; i++) await Autenticar("diana@x.com", "senha-errada");

        _c.Relogio.AgoraUtc = _c.Relogio.AgoraUtc.AddMinutes(15);

        (await Autenticar("diana@x.com", "senha-segura")).Sucesso.Should().BeTrue();
    }

    [Fact]
    public async Task Login_correto_zera_a_contagem_de_falhas()
    {
        for (var i = 0; i < 4; i++) await Autenticar("diana@x.com", "senha-errada");

        (await Autenticar("diana@x.com", "senha-segura")).Sucesso.Should().BeTrue();

        _c.Diretor.FalhasDeLogin.Should().Be(0);
        for (var i = 0; i < 4; i++)
            (await Autenticar("diana@x.com", "senha-errada")).Mensagem.Should().Be(Invalido);
    }

    [Fact]
    public async Task Nenhum_evento_de_login_carrega_a_senha()
    {
        await Autenticar("diana@x.com", "senha-errada-secreta");
        await Autenticar("ninguem@x.com", "outra-senha-secreta");

        _c.Auditoria.Eventos.Select(e => System.Text.Json.JsonSerializer.Serialize(e.Dados))
            .Should().NotContain(d => d.Contains("secreta"));
    }
}
```

Acrescente a `SenhaTests.cs`:

```csharp
    [Fact]
    public async Task Redefinir_a_senha_remove_o_bloqueio_de_login()
    {
        var alvo = _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);
        for (var i = 0; i < 5; i++) alvo.RegistrarFalhaDeLogin(_c.Relogio.AgoraUtc);

        await new RedefinirSenhaUsuarioManipulador(_c.Como(_c.Diretor), _c.Usuarios, _c.Hasher, _c.Unidade, _c.Auditoria)
            .ManipularAsync(new RedefinirSenhaUsuario(alvo.Id, "nova-senha-1"), default);

        alvo.EstaBloqueado(_c.Relogio.AgoraUtc).Should().BeFalse();
    }
```

`tests/AtividadesJuridicas.IntegrationTests/BloqueioDeLoginTests.cs`:

```csharp
using System.Net;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class BloqueioDeLoginTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco.NovaConexao());

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _app.DisposeAsync();

    private async Task<string> TentarAsync(string senha)
    {
        var resposta = await AplicacaoDeTeste.EntrarAsync(_app.NovoCliente(), AplicacaoDeTeste.EmailDoAdmin, senha);
        return resposta.StatusCode == HttpStatusCode.Redirect
            ? "entrou"
            : WebUtility.HtmlDecode(await resposta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Cinco_senhas_erradas_bloqueiam_ate_a_senha_certa()
    {
        for (var i = 0; i < 4; i++)
            (await TentarAsync("senha-errada")).Should().Contain("E-mail ou senha inválidos.");

        (await TentarAsync("senha-errada")).Should().Contain("Muitas tentativas de login.");
        (await TentarAsync(AplicacaoDeTeste.SenhaDoAdmin)).Should().Contain("Muitas tentativas de login.");

        (await _app.EventosAsync(EventosDeSeguranca.LoginBloqueado)).Should().ContainSingle();
        (await _app.EventosAsync(EventosDeSeguranca.LoginSucesso)).Should().BeEmpty();
    }

    [Fact]
    public async Task Falhas_de_login_nao_mexem_nos_carimbos_nem_geram_registro_de_alteracao()
    {
        var adminId = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);

        await TentarAsync("senha-errada");
        await TentarAsync("senha-errada");

        var (usuario, alteracoes) = await _app.EmEscopoAsync(null, async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            return (await db.Usuarios.AsNoTracking().SingleAsync(u => u.Id == adminId),
                    await db.RegistrosAuditoria.CountAsync(r =>
                        r.EntidadeId == adminId && r.Operacao == OperacaoAuditada.Alteracao));
        });

        usuario.FalhasDeLogin.Should().Be(2);
        usuario.AtualizadoEm.Should().BeNull();
        alteracoes.Should().Be(0);
        (await _app.EventosAsync(EventosDeSeguranca.LoginFalha)).Should().HaveCount(2)
            .And.OnlyContain(e => e.EntidadeId == adminId);
    }

    [Fact]
    public async Task Login_correto_gera_evento_de_sucesso_e_zera_as_falhas()
    {
        var adminId = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        await TentarAsync("senha-errada");

        (await TentarAsync(AplicacaoDeTeste.SenhaDoAdmin)).Should().Be("entrou");

        var usuario = await _app.EmEscopoAsync(null, sp =>
            sp.GetRequiredService<AppDbContext>().Usuarios.AsNoTracking().SingleAsync(u => u.Id == adminId));
        usuario.FalhasDeLogin.Should().Be(0);
        usuario.UltimoAcessoEm.Should().NotBeNull();
        (await _app.EventosAsync(EventosDeSeguranca.LoginSucesso)).Should().ContainSingle()
            .Which.EntidadeId.Should().Be(adminId);
    }

    [Fact]
    public async Task Email_inexistente_nunca_bloqueia()
    {
        for (var i = 0; i < 7; i++)
        {
            var resposta = await AplicacaoDeTeste.EntrarAsync(_app.NovoCliente(), "ninguem@x.com", "qualquer-senha");
            WebUtility.HtmlDecode(await resposta.Content.ReadAsStringAsync())
                .Should().Contain("E-mail ou senha inválidos.").And.NotContain("Muitas tentativas");
        }

        (await _app.EventosAsync(EventosDeSeguranca.LoginBloqueado)).Should().BeEmpty();
    }
}
```

- [ ] **Step 2: Rodar e confirmar a falha**

Run: `dotnet test`
Expected: erro de compilação (`RegistrarFalhaDeLogin`, `GravarEstadoDeLoginAsync`, `HashDeDescarte` não existem).

- [ ] **Step 3: Implementar no domínio**

Em `Usuario.cs`:

```csharp
    public const int MaximoDeFalhasDeLogin = 5;
    public static readonly TimeSpan DuracaoDoBloqueio = TimeSpan.FromMinutes(15);

    [NaoAuditar]
    public int FalhasDeLogin { get; private set; }

    [NaoAuditar]
    public DateTime? BloqueadoAte { get; private set; }

    public bool EstaBloqueado(DateTime agoraUtc) => BloqueadoAte is { } ate && ate > agoraUtc;

    /// <summary>Conta uma senha errada. Devolve verdadeiro quando esta falha causou o bloqueio.</summary>
    public bool RegistrarFalhaDeLogin(DateTime agoraUtc)
    {
        FalhasDeLogin++;
        if (FalhasDeLogin < MaximoDeFalhasDeLogin) return false;

        FalhasDeLogin = 0;
        BloqueadoAte = agoraUtc + DuracaoDoBloqueio;
        return true;
    }

    public void RegistrarAcesso(DateTime agoraUtc)
    {
        UltimoAcessoEm = agoraUtc;
        Desbloquear();
    }

    public void Desbloquear()
    {
        FalhasDeLogin = 0;
        BloqueadoAte = null;
    }
```

(substitui o `RegistrarAcesso` de uma linha que existe hoje).

- [ ] **Step 4: Implementar na aplicação e na infraestrutura**

Em `Portas.cs`: troque `RegistrarAcessoAsync` por

```csharp
    /// <summary>
    /// Grava somente último acesso, falhas e bloqueio de login, fora da unidade de trabalho: não são
    /// alteração de negócio e não devem mudar os carimbos nem gerar registro de alteração.
    /// </summary>
    Task GravarEstadoDeLoginAsync(Usuario usuario, CancellationToken ct);
```

e acrescente a `IHasherDeSenha`:

```csharp
    /// <summary>Hash válido de uma senha que ninguém conhece, para gastar o mesmo tempo quando o usuário não existe.</summary>
    string HashDeDescarte { get; }
```

`UsuarioRepositorio.cs` — troque `RegistrarAcessoAsync` por:

```csharp
    public async Task GravarEstadoDeLoginAsync(Usuario usuario, CancellationToken ct)
    {
        await db.Usuarios.Where(u => u.Id == usuario.Id).ExecuteUpdateAsync(c => c
            .SetProperty(u => u.UltimoAcessoEm, usuario.UltimoAcessoEm)
            .SetProperty(u => u.FalhasDeLogin, usuario.FalhasDeLogin)
            .SetProperty(u => u.BloqueadoAte, usuario.BloqueadoAte), ct);

        // Os valores já estão no banco: a entidade rastreada não pode ficar como alterada,
        // ou uma gravação posterior no mesmo escopo a carimbaria e a auditaria.
        db.Entry(usuario).State = EntityState.Unchanged;
    }
```

`HasherDeSenhaPbkdf2.cs` — acrescente:

```csharp
    private readonly Lazy<string> _descarte;

    public HasherDeSenhaPbkdf2() =>
        _descarte = new Lazy<string>(() => Gerar(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))));

    public string HashDeDescarte => _descarte.Value;
```

`AutenticarUsuario.cs` — o manipulador passa a ser:

```csharp
public sealed class AutenticarUsuarioManipulador(
    IUsuarioRepositorio usuarios,
    IPerfilRepositorio perfis,
    IHasherDeSenha hasher,
    IRelogio relogio,
    IAuditoria auditoria) : IManipulador<AutenticarUsuario, Resultado<SessaoIniciada>>
{
    // Mesma mensagem para e-mail inexistente, usuário inativo e senha errada.
    private static readonly Resultado<SessaoIniciada> Invalido =
        Resultado<SessaoIniciada>.Falha(TipoErro.AcessoNegado, "E-mail ou senha inválidos.");

    private static readonly Resultado<SessaoIniciada> Bloqueado = Resultado<SessaoIniciada>.Falha(
        TipoErro.AcessoNegado, "Muitas tentativas de login. Aguarde 15 minutos e tente novamente.");

    public async Task<Resultado<SessaoIniciada>> ManipularAsync(AutenticarUsuario requisicao, CancellationToken ct)
    {
        var senha = requisicao.Senha ?? string.Empty;
        var usuario = Email.TentarCriar(requisicao.Email, out var email)
            ? await usuarios.ObterPorEmailAsync(email, ct)
            : null;

        if (usuario is null || !usuario.Ativo)
        {
            // Gasta o tempo de uma verificação, para a resposta não revelar que o e-mail não existe.
            hasher.Verificar(senha, hasher.HashDeDescarte);
            await auditoria.RegistrarAsync(EventosDeSeguranca.LoginFalha, ct: ct);
            return Invalido;
        }

        var agora = relogio.AgoraUtc;

        if (usuario.EstaBloqueado(agora))
        {
            await auditoria.RegistrarAsync(EventosDeSeguranca.LoginFalha, "Usuario", usuario.Id,
                new { motivo = "bloqueado" }, ct);
            return Bloqueado;
        }

        if (!hasher.Verificar(senha, usuario.SenhaHash))
        {
            var bloqueou = usuario.RegistrarFalhaDeLogin(agora);
            await usuarios.GravarEstadoDeLoginAsync(usuario, ct);
            await auditoria.RegistrarAsync(
                bloqueou ? EventosDeSeguranca.LoginBloqueado : EventosDeSeguranca.LoginFalha,
                "Usuario", usuario.Id, ct: ct);
            return bloqueou ? Bloqueado : Invalido;
        }

        var perfil = await perfis.ObterPorIdAsync(usuario.PerfilId, ct);
        if (perfil is null) return Invalido;

        usuario.RegistrarAcesso(agora);
        await usuarios.GravarEstadoDeLoginAsync(usuario, ct);
        await auditoria.RegistrarAsync(EventosDeSeguranca.LoginSucesso, "Usuario", usuario.Id, ct: ct);

        return Resultado<SessaoIniciada>.Ok(
            new SessaoIniciada(usuario.Id, usuario.Nome, perfil.Tipo, usuario.VersaoDeSessao));
    }
}
```

com `using AtividadesJuridicas.Application.Auditoria;`. O e-mail digitado não é gravado no evento de falha: pode conter, por engano, a própria senha do usuário.

`RedefinirSenhaUsuario.cs` — logo após `usuario.DefinirSenha(...)`: `usuario.Desbloquear();`.

- [ ] **Step 5: Gerar a migration**

Run:

```bash
dotnet ef migrations add BloqueioDeLogin --project src/AtividadesJuridicas.Infrastructure --startup-project src/AtividadesJuridicas.Infrastructure --output-dir Persistencia/Migrations
```

Expected: acrescenta só `falhas_de_login integer NOT NULL DEFAULT 0` e `bloqueado_ate timestamp with time zone NULL` em `usuarios`.

- [ ] **Step 6: Rodar e confirmar que passa**

Run: `dotnet test`
Expected: tudo aprovado, zero avisos. O teste existente `Login_registra_o_acesso_sem_alterar_os_carimbos_de_atualizacao` continua passando.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "feat(identidade): bloqueio após cinco falhas e eventos de login na auditoria"
```

---

### Task 8: Consulta de auditoria

**Files:**
- Create: `src/AtividadesJuridicas.Application/Comum/Pagina.cs`, `src/AtividadesJuridicas.Application/Auditoria/ConsultaDeAuditoria.cs`, `ConsultarAuditoria.cs`, `ConsultarAcessos.cs`
- Create: `src/AtividadesJuridicas.Infrastructure/Persistencia/Repositorios/ConsultaDeAuditoria.cs`
- Create: `src/AtividadesJuridicas.Web/Components/Pages/Auditoria.razor`
- Modify: `src/AtividadesJuridicas.Infrastructure/DependencyInjection.cs`, `src/AtividadesJuridicas.Web/Infra/FusoDoEscritorio.cs`, `src/AtividadesJuridicas.Web/Components/Layout/MainLayout.razor`, `src/AtividadesJuridicas.Web/Components/_Imports.razor`
- Test: `tests/AtividadesJuridicas.IntegrationTests/ConsultaDeAuditoriaTests.cs`, `AuditoriaWebTests.cs`; acrescentar a `FusoDoEscritorioTests.cs`

**Interfaces:**
- Produces (Application):
  - `record Pagina<T>(IReadOnlyList<T> Itens, int Total, int Numero, int Tamanho)` com `TotalDePaginas`.
  - `record FiltroDeAuditoria(DateTime? DeUtc = null, DateTime? AteUtc = null, Guid? UsuarioId = null, string? Entidade = null, OperacaoAuditada? Operacao = null)`. `DeUtc` é inclusivo, `AteUtc` é exclusivo.
  - `record FiltroDeAcessos(DateTime? DeUtc = null, DateTime? AteUtc = null, Guid? UsuarioId = null, string? TipoRecurso = null, AcaoDeAcesso? Acao = null)`.
  - `record RegistroAuditoriaResumo(Guid Id, DateTime DataHora, Guid? UsuarioId, string? UsuarioNome, OperacaoAuditada Operacao, string? Entidade, Guid? EntidadeId, string? Evento, string? Dados, string? Ip)`.
  - `record RegistroAcessoResumo(Guid Id, DateTime DataHora, Guid UsuarioId, string? UsuarioNome, string TipoRecurso, Guid? RecursoId, AcaoDeAcesso Acao, string? Detalhe, string? Ip)`.
  - `interface IConsultaDeAuditoria` com `ConsultarAsync(FiltroDeAuditoria, int pagina, int tamanho, CancellationToken)` e `ConsultarAcessosAsync(FiltroDeAcessos, int pagina, int tamanho, CancellationToken)`.
  - `record ConsultarAuditoria(FiltroDeAuditoria Filtro, int Pagina = 1) : IRequisicao<Resultado<Pagina<RegistroAuditoriaResumo>>>, IConsultaAuditavel` e `record ConsultarAcessos(FiltroDeAcessos Filtro, int Pagina = 1)` análogo. Tamanho da página fixo em `ConsultaDeAuditoriaOpcoes.TamanhoDaPagina = 50`.
- Produces (Web): `FusoDoEscritorio.InicioDoDiaEmUtc(DateOnly dia)`; rota `/auditoria`.
- Ordenação: mais recentes primeiro (`DataHora` e depois `Id`, ambos decrescentes).

- [ ] **Step 1: Escrever os testes que falham**

Acrescente a `FusoDoEscritorioTests.cs`:

```csharp
    [Fact]
    public void Inicio_do_dia_local_vira_o_instante_utc_correspondente()
    {
        var fuso = new FusoDoEscritorio("America/Sao_Paulo");

        fuso.InicioDoDiaEmUtc(new DateOnly(2026, 10, 7))
            .Should().Be(new DateTime(2026, 10, 7, 3, 0, 0, DateTimeKind.Utc));
    }
```

`tests/AtividadesJuridicas.IntegrationTests/ConsultaDeAuditoriaTests.cs`:

```csharp
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class ConsultaDeAuditoriaTests(BancoFixture banco) : TesteComBanco(banco)
{
    private async Task<(Guid AdminId, Guid PedroId)> PrepararAsync()
    {
        var adminId = await IdDoAdminAsync();
        var producao = await IdDoPerfilAsync(TipoPerfil.Producao);
        var pedro = await ExecutarAsync(adminId, new CriarUsuario("Pedro", "pedro@x.com", "senha-do-pedro", producao, null));
        await ExecutarAsync(adminId, new EditarUsuario(pedro.Valor, "Pedro Silva", "pedro@x.com", producao, null));
        return (adminId, pedro.Valor);
    }

    private async Task<Pagina<RegistroAuditoriaResumo>> ConsultarAsync(Guid ator, FiltroDeAuditoria filtro, int pagina = 1)
    {
        var resultado = await ExecutarAsync(ator, new ConsultarAuditoria(filtro, pagina));
        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        return resultado.Valor!;
    }

    [Fact]
    public async Task Sem_filtro_traz_tudo_do_mais_recente_para_o_mais_antigo_com_o_nome_de_quem_fez()
    {
        var (adminId, pedroId) = await PrepararAsync();

        var pagina = await ConsultarAsync(adminId, new FiltroDeAuditoria());

        pagina.Itens.Should().BeInDescendingOrder(r => r.DataHora);
        pagina.Total.Should().Be(pagina.Itens.Count);
        var maisRecente = pagina.Itens[0];
        maisRecente.Should().BeEquivalentTo(new
        {
            Operacao = OperacaoAuditada.Alteracao, Entidade = "Usuario", EntidadeId = pedroId,
            UsuarioId = adminId, UsuarioNome = "Diana Diretora",
        });
        pagina.Itens.Should().Contain(r => r.UsuarioId == null && r.UsuarioNome == null, "a carga inicial é do sistema");
    }

    [Fact]
    public async Task Filtra_por_entidade_operacao_e_usuario()
    {
        var (adminId, pedroId) = await PrepararAsync();

        var criacoesDeUsuario = await ConsultarAsync(adminId,
            new FiltroDeAuditoria(Entidade: "Usuario", Operacao: OperacaoAuditada.Criacao, UsuarioId: adminId));

        criacoesDeUsuario.Itens.Should().ContainSingle().Which.EntidadeId.Should().Be(pedroId);
    }

    [Fact]
    public async Task Filtra_por_periodo_com_inicio_inclusivo_e_fim_exclusivo()
    {
        var (adminId, _) = await PrepararAsync();
        var todos = (await ConsultarAsync(adminId, new FiltroDeAuditoria())).Itens;
        var corte = todos[0].DataHora;

        (await ConsultarAsync(adminId, new FiltroDeAuditoria(DeUtc: corte))).Itens
            .Should().Contain(r => r.Id == todos[0].Id);
        (await ConsultarAsync(adminId, new FiltroDeAuditoria(AteUtc: corte))).Itens
            .Should().NotContain(r => r.Id == todos[0].Id);
        (await ConsultarAsync(adminId, new FiltroDeAuditoria(DeUtc: corte.AddDays(1)))).Itens.Should().BeEmpty();
    }

    [Fact]
    public async Task Pagina_de_cinquenta_em_cinquenta()
    {
        var adminId = await IdDoAdminAsync();
        await EmEscopoAsync(adminId, async sp =>
        {
            var auditoria = sp.GetRequiredService<IAuditoria>();
            for (var i = 0; i < 120; i++) await auditoria.RegistrarAsync("evento_de_teste", dados: new { i });
            return 0;
        });

        var filtro = new FiltroDeAuditoria(Operacao: OperacaoAuditada.Evento);
        var primeira = await ConsultarAsync(adminId, filtro);
        var terceira = await ConsultarAsync(adminId, filtro, 3);

        primeira.Itens.Should().HaveCount(50);
        primeira.Total.Should().Be(120);
        primeira.TotalDePaginas.Should().Be(3);
        terceira.Itens.Should().HaveCount(20);
        primeira.Itens.Select(r => r.Id).Should().NotIntersectWith(terceira.Itens.Select(r => r.Id));
    }

    [Fact]
    public async Task Usuario_excluido_ainda_aparece_pelo_nome()
    {
        var (adminId, pedroId) = await PrepararAsync();
        await ExecutarAsync(pedroId, new AlterarMinhaSenha("senha-do-pedro", "senha-nova-do-pedro"));
        await EmEscopoAsync(adminId, async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            db.Usuarios.Remove(await db.Usuarios.SingleAsync(u => u.Id == pedroId));
            await db.SaveChangesAsync();
            return 0;
        });

        var doPedro = await ConsultarAsync(adminId, new FiltroDeAuditoria(UsuarioId: pedroId));

        doPedro.Itens.Should().NotBeEmpty().And.OnlyContain(r => r.UsuarioNome == "Pedro Silva");
    }

    [Fact]
    public async Task Producao_nao_consulta_e_a_tentativa_fica_registrada()
    {
        var (_, pedroId) = await PrepararAsync();

        var auditoria = await ExecutarAsync(pedroId, new ConsultarAuditoria(new FiltroDeAuditoria()));
        var acessos = await ExecutarAsync(pedroId, new ConsultarAcessos(new FiltroDeAcessos()));

        auditoria.Erro.Should().Be(TipoErro.AcessoNegado);
        auditoria.Valor.Should().BeNull();
        acessos.Erro.Should().Be(TipoErro.AcessoNegado);
        (await AuditoriaAsync()).Count(r => r.Evento == EventosDeSeguranca.AcessoNegado && r.UsuarioId == pedroId)
            .Should().Be(2);
    }

    [Fact]
    public async Task Consultar_a_auditoria_gera_registro_de_acesso_com_os_filtros()
    {
        var (adminId, _) = await PrepararAsync();

        await ConsultarAsync(adminId, new FiltroDeAuditoria(Entidade: "Usuario"));

        var acessos = await ExecutarAsync(adminId, new ConsultarAcessos(new FiltroDeAcessos(TipoRecurso: "RegistroAuditoria")));
        var acesso = acessos.Valor!.Itens.Should().ContainSingle().Subject;
        acesso.Acao.Should().Be(AcaoDeAcesso.Listou);
        acesso.UsuarioId.Should().Be(adminId);
        acesso.UsuarioNome.Should().Be("Diana Diretora");
        acesso.Detalhe.Should().Contain("Usuario");
    }

    [Fact]
    public async Task Acessos_filtram_por_usuario_e_acao()
    {
        var (adminId, _) = await PrepararAsync();
        await ExecutarAsync(adminId, new ListarUsuarios());

        var acessos = await ExecutarAsync(adminId,
            new ConsultarAcessos(new FiltroDeAcessos(UsuarioId: adminId, TipoRecurso: "Usuario", Acao: AcaoDeAcesso.Listou)));

        acessos.Valor!.Itens.Should().ContainSingle().Which.TipoRecurso.Should().Be("Usuario");
    }
}
```

`tests/AtividadesJuridicas.IntegrationTests/AuditoriaWebTests.cs`:

```csharp
using System.Net;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class AuditoriaWebTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco.NovaConexao());

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _app.DisposeAsync();

    [Fact]
    public async Task Diretor_ve_os_registros_sem_nenhum_hash_de_senha()
    {
        await _app.CriarPedroAsync();
        var cliente = await _app.ClienteAutenticadoAsync();

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/auditoria"));

        html.Should().Contain("Auditoria").And.Contain("login_sucesso").And.Contain("pedro@x.com");
        html.Should().Contain("Diana Diretora");
        html.Should().NotContain("pbkdf2-sha256");
    }

    [Fact]
    public async Task Producao_recebe_acesso_negado_e_nao_ve_registros()
    {
        await _app.CriarPedroAsync();
        var cliente = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/auditoria"));

        html.Should().Contain("Acesso negado");
        html.Should().NotContain("login_sucesso").And.NotContain("diana@x.com");
    }

    [Fact]
    public async Task Menu_mostra_auditoria_so_para_a_diretoria()
    {
        await _app.CriarPedroAsync();
        var doDiretor = await _app.ClienteAutenticadoAsync();
        var doPedro = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        (await doDiretor.GetStringAsync("/")).Should().Contain("href=\"/auditoria\"");
        (await doPedro.GetStringAsync("/")).Should().NotContain("href=\"/auditoria\"");
    }

    [Fact]
    public async Task Anonimo_e_redirecionado_ao_login()
    {
        var resposta = await _app.NovoCliente().GetAsync("/auditoria");

        resposta.StatusCode.Should().Be(HttpStatusCode.Redirect);
        resposta.Headers.Location!.AbsolutePath.Should().Be("/conta/entrar");
    }
}
```

- [ ] **Step 2: Rodar e confirmar a falha**

Run: `dotnet test`
Expected: erro de compilação (`ConsultarAuditoria`, `Pagina<>` não existem).

- [ ] **Step 3: Implementar na aplicação**

`src/AtividadesJuridicas.Application/Comum/Pagina.cs`:

```csharp
namespace AtividadesJuridicas.Application.Comum;

public sealed record Pagina<T>(IReadOnlyList<T> Itens, int Total, int Numero, int Tamanho)
{
    public int TotalDePaginas => Total == 0 ? 1 : (int)Math.Ceiling(Total / (double)Tamanho);
}
```

`src/AtividadesJuridicas.Application/Auditoria/ConsultaDeAuditoria.cs`:

```csharp
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Domain.Auditoria;

namespace AtividadesJuridicas.Application.Auditoria;

public static class ConsultaDeAuditoriaOpcoes
{
    public const int TamanhoDaPagina = 50;
}

/// <summary><paramref name="DeUtc"/> é inclusivo; <paramref name="AteUtc"/> é exclusivo.</summary>
public sealed record FiltroDeAuditoria(
    DateTime? DeUtc = null,
    DateTime? AteUtc = null,
    Guid? UsuarioId = null,
    string? Entidade = null,
    OperacaoAuditada? Operacao = null);

/// <summary><paramref name="DeUtc"/> é inclusivo; <paramref name="AteUtc"/> é exclusivo.</summary>
public sealed record FiltroDeAcessos(
    DateTime? DeUtc = null,
    DateTime? AteUtc = null,
    Guid? UsuarioId = null,
    string? TipoRecurso = null,
    AcaoDeAcesso? Acao = null);

public sealed record RegistroAuditoriaResumo(
    Guid Id, DateTime DataHora, Guid? UsuarioId, string? UsuarioNome, OperacaoAuditada Operacao,
    string? Entidade, Guid? EntidadeId, string? Evento, string? Dados, string? Ip);

public sealed record RegistroAcessoResumo(
    Guid Id, DateTime DataHora, Guid UsuarioId, string? UsuarioNome, string TipoRecurso,
    Guid? RecursoId, AcaoDeAcesso Acao, string? Detalhe, string? Ip);

public interface IConsultaDeAuditoria
{
    Task<Pagina<RegistroAuditoriaResumo>> ConsultarAsync(
        FiltroDeAuditoria filtro, int pagina, int tamanho, CancellationToken ct);

    Task<Pagina<RegistroAcessoResumo>> ConsultarAcessosAsync(
        FiltroDeAcessos filtro, int pagina, int tamanho, CancellationToken ct);
}
```

`src/AtividadesJuridicas.Application/Auditoria/ConsultarAuditoria.cs`:

```csharp
using System.Text.Json;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;

namespace AtividadesJuridicas.Application.Auditoria;

public sealed record ConsultarAuditoria(FiltroDeAuditoria Filtro, int Pagina = 1)
    : IRequisicao<Resultado<Pagina<RegistroAuditoriaResumo>>>, IConsultaAuditavel
{
    public string TipoRecurso => "RegistroAuditoria";
    public AcaoDeAcesso Acao => AcaoDeAcesso.Listou;
    public string? Detalhe => JsonSerializer.Serialize(new { Filtro, Pagina });
}

public sealed class ConsultarAuditoriaManipulador(IContextoDeAcesso contexto, IConsultaDeAuditoria consulta)
    : IManipulador<ConsultarAuditoria, Resultado<Pagina<RegistroAuditoriaResumo>>>
{
    public async Task<Resultado<Pagina<RegistroAuditoriaResumo>>> ManipularAsync(
        ConsultarAuditoria requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Resultado<Pagina<RegistroAuditoriaResumo>>.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        return Resultado<Pagina<RegistroAuditoriaResumo>>.Ok(await consulta.ConsultarAsync(
            requisicao.Filtro, Math.Max(1, requisicao.Pagina), ConsultaDeAuditoriaOpcoes.TamanhoDaPagina, ct));
    }
}
```

`src/AtividadesJuridicas.Application/Auditoria/ConsultarAcessos.cs`:

```csharp
using System.Text.Json;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;

namespace AtividadesJuridicas.Application.Auditoria;

public sealed record ConsultarAcessos(FiltroDeAcessos Filtro, int Pagina = 1)
    : IRequisicao<Resultado<Pagina<RegistroAcessoResumo>>>, IConsultaAuditavel
{
    public string TipoRecurso => "RegistroAcesso";
    public AcaoDeAcesso Acao => AcaoDeAcesso.Listou;
    public string? Detalhe => JsonSerializer.Serialize(new { Filtro, Pagina });
}

public sealed class ConsultarAcessosManipulador(IContextoDeAcesso contexto, IConsultaDeAuditoria consulta)
    : IManipulador<ConsultarAcessos, Resultado<Pagina<RegistroAcessoResumo>>>
{
    public async Task<Resultado<Pagina<RegistroAcessoResumo>>> ManipularAsync(
        ConsultarAcessos requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Resultado<Pagina<RegistroAcessoResumo>>.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        return Resultado<Pagina<RegistroAcessoResumo>>.Ok(await consulta.ConsultarAcessosAsync(
            requisicao.Filtro, Math.Max(1, requisicao.Pagina), ConsultaDeAuditoriaOpcoes.TamanhoDaPagina, ct));
    }
}
```

- [ ] **Step 4: Implementar a leitura na infraestrutura**

`src/AtividadesJuridicas.Infrastructure/Persistencia/Repositorios/ConsultaDeAuditoria.cs`:

```csharp
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using Microsoft.EntityFrameworkCore;

namespace AtividadesJuridicas.Infrastructure.Persistencia.Repositorios;

public sealed class ConsultaDeAuditoria(AppDbContext db) : IConsultaDeAuditoria
{
    public async Task<Pagina<RegistroAuditoriaResumo>> ConsultarAsync(
        FiltroDeAuditoria filtro, int pagina, int tamanho, CancellationToken ct)
    {
        var consulta = db.RegistrosAuditoria.AsNoTracking();

        if (filtro.DeUtc is { } de) consulta = consulta.Where(r => r.DataHora >= de);
        if (filtro.AteUtc is { } ate) consulta = consulta.Where(r => r.DataHora < ate);
        if (filtro.UsuarioId is { } usuarioId) consulta = consulta.Where(r => r.UsuarioId == usuarioId);
        if (!string.IsNullOrWhiteSpace(filtro.Entidade)) consulta = consulta.Where(r => r.Entidade == filtro.Entidade);
        if (filtro.Operacao is { } operacao) consulta = consulta.Where(r => r.Operacao == operacao);

        var total = await consulta.CountAsync(ct);

        // Usuários excluídos logicamente continuam identificados pelo nome.
        var usuarios = db.Usuarios.IgnoreQueryFilters();
        var itens = await consulta
            .OrderByDescending(r => r.DataHora).ThenByDescending(r => r.Id)
            .Skip((pagina - 1) * tamanho).Take(tamanho)
            .Select(r => new RegistroAuditoriaResumo(
                r.Id, r.DataHora, r.UsuarioId,
                usuarios.Where(u => u.Id == r.UsuarioId).Select(u => u.Nome).FirstOrDefault(),
                r.Operacao, r.Entidade, r.EntidadeId, r.Evento, r.Dados, r.Ip))
            .ToListAsync(ct);

        return new Pagina<RegistroAuditoriaResumo>(itens, total, pagina, tamanho);
    }

    public async Task<Pagina<RegistroAcessoResumo>> ConsultarAcessosAsync(
        FiltroDeAcessos filtro, int pagina, int tamanho, CancellationToken ct)
    {
        var consulta = db.RegistrosAcesso.AsNoTracking();

        if (filtro.DeUtc is { } de) consulta = consulta.Where(r => r.DataHora >= de);
        if (filtro.AteUtc is { } ate) consulta = consulta.Where(r => r.DataHora < ate);
        if (filtro.UsuarioId is { } usuarioId) consulta = consulta.Where(r => r.UsuarioId == usuarioId);
        if (!string.IsNullOrWhiteSpace(filtro.TipoRecurso)) consulta = consulta.Where(r => r.TipoRecurso == filtro.TipoRecurso);
        if (filtro.Acao is { } acao) consulta = consulta.Where(r => r.Acao == acao);

        var total = await consulta.CountAsync(ct);

        var usuarios = db.Usuarios.IgnoreQueryFilters();
        var itens = await consulta
            .OrderByDescending(r => r.DataHora).ThenByDescending(r => r.Id)
            .Skip((pagina - 1) * tamanho).Take(tamanho)
            .Select(r => new RegistroAcessoResumo(
                r.Id, r.DataHora, r.UsuarioId,
                usuarios.Where(u => u.Id == r.UsuarioId).Select(u => u.Nome).FirstOrDefault(),
                r.TipoRecurso, r.RecursoId, r.Acao, r.Detalhe, r.Ip))
            .ToListAsync(ct);

        return new Pagina<RegistroAcessoResumo>(itens, total, pagina, tamanho);
    }
}
```

Em `DependencyInjection.cs` (Infrastructure): `servicos.AddScoped<IConsultaDeAuditoria, ConsultaDeAuditoria>();`.

- [ ] **Step 5: Implementar a tela**

Em `FusoDoEscritorio.cs`:

```csharp
    /// <summary>Instante UTC em que o dia começa no fuso do escritório.</summary>
    public DateTime InicioDoDiaEmUtc(DateOnly dia) =>
        TimeZoneInfo.ConvertTimeToUtc(dia.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified), _fuso);
```

Em `_Imports.razor`: `@using AtividadesJuridicas.Application.Auditoria` e `@using AtividadesJuridicas.Domain.Auditoria`.

Em `MainLayout.razor`, dentro do bloco exibido só para a Diretoria, depois do link de Usuários:

```razor
            <NavLink href="/auditoria">Auditoria</NavLink>
```

`src/AtividadesJuridicas.Web/Components/Pages/Auditoria.razor`:

```razor
@page "/auditoria"
@inject IDespachante Despachante
@inject FusoDoEscritorio Fuso

<PageTitle>Auditoria</PageTitle>

<h1>Auditoria</h1>

@if (!_carregado)
{
    <p>Carregando…</p>
}
else if (!_autorizado)
{
    <p class="erro">Acesso negado.</p>
}
else
{
    <div class="acoes" role="tablist">
        <button type="button" class="@(_aba == Aba.Alteracoes ? null : "secundario")"
                @onclick="() => TrocarAbaAsync(Aba.Alteracoes)">Alterações e eventos</button>
        <button type="button" class="@(_aba == Aba.Acessos ? null : "secundario")"
                @onclick="() => TrocarAbaAsync(Aba.Acessos)">Consultas</button>
    </div>

    <EditForm Model="_filtro" OnSubmit="FiltrarAsync" FormName="filtro-auditoria" class="formulario filtros">
        <label>De <InputDate @bind-Value="_filtro.De" /></label>
        <label>Até <InputDate @bind-Value="_filtro.Ate" /></label>
        <label>
            Usuário
            <InputSelect @bind-Value="_filtro.UsuarioId">
                <option value="">Todos</option>
                @foreach (var usuario in _usuarios)
                {
                    <option value="@usuario.Id">@usuario.Nome</option>
                }
            </InputSelect>
        </label>

        @if (_aba == Aba.Alteracoes)
        {
            <label>Entidade <InputText @bind-Value="_filtro.Entidade" placeholder="Usuario, Perfil…" /></label>
            <label>
                Operação
                <InputSelect @bind-Value="_filtro.Operacao">
                    <option value="">Todas</option>
                    <option value="@OperacaoAuditada.Criacao">Criação</option>
                    <option value="@OperacaoAuditada.Alteracao">Alteração</option>
                    <option value="@OperacaoAuditada.Exclusao">Exclusão</option>
                    <option value="@OperacaoAuditada.Evento">Evento</option>
                </InputSelect>
            </label>
        }
        else
        {
            <label>Recurso <InputText @bind-Value="_filtro.TipoRecurso" placeholder="Usuario, RegistroAuditoria…" /></label>
            <label>
                Ação
                <InputSelect @bind-Value="_filtro.Acao">
                    <option value="">Todas</option>
                    <option value="@AcaoDeAcesso.Listou">Listou</option>
                    <option value="@AcaoDeAcesso.Visualizou">Visualizou</option>
                    <option value="@AcaoDeAcesso.Baixou">Baixou</option>
                </InputSelect>
            </label>
        }

        <button type="submit">Filtrar</button>
    </EditForm>

    @if (_erro is not null)
    {
        <p class="erro" role="alert">@_erro</p>
    }

    @if (_aba == Aba.Alteracoes)
    {
        <table>
            <thead>
                <tr><th>Quando</th><th>Quem</th><th>Operação</th><th>Sobre</th><th>Dados</th><th>IP</th></tr>
            </thead>
            <tbody>
                @foreach (var registro in _alteracoes.Itens)
                {
                    <tr @key="registro.Id">
                        <td>@Fuso.Formatar(registro.DataHora)</td>
                        <td>@(registro.UsuarioNome ?? (registro.UsuarioId is null ? "Sistema" : "—"))</td>
                        <td>@(registro.Evento ?? Rotulo(registro.Operacao))</td>
                        <td>@registro.Entidade @registro.EntidadeId</td>
                        <td><code>@registro.Dados</code></td>
                        <td>@registro.Ip</td>
                    </tr>
                }
            </tbody>
        </table>
    }
    else
    {
        <table>
            <thead>
                <tr><th>Quando</th><th>Quem</th><th>Ação</th><th>Recurso</th><th>Detalhe</th><th>IP</th></tr>
            </thead>
            <tbody>
                @foreach (var registro in _acessos.Itens)
                {
                    <tr @key="registro.Id">
                        <td>@Fuso.Formatar(registro.DataHora)</td>
                        <td>@(registro.UsuarioNome ?? "—")</td>
                        <td>@registro.Acao</td>
                        <td>@registro.TipoRecurso @registro.RecursoId</td>
                        <td><code>@registro.Detalhe</code></td>
                        <td>@registro.Ip</td>
                    </tr>
                }
            </tbody>
        </table>
    }

    <div class="acoes">
        <button type="button" class="secundario" disabled="@(_pagina <= 1)" @onclick="() => IrParaAsync(_pagina - 1)">Anterior</button>
        <span>Página @_pagina de @TotalDePaginas (@Total registros)</span>
        <button type="button" class="secundario" disabled="@(_pagina >= TotalDePaginas)" @onclick="() => IrParaAsync(_pagina + 1)">Próxima</button>
    </div>
}

@code {
    private enum Aba { Alteracoes, Acessos }

    private static readonly Pagina<RegistroAuditoriaResumo> SemAlteracoes = new([], 0, 1, ConsultaDeAuditoriaOpcoes.TamanhoDaPagina);
    private static readonly Pagina<RegistroAcessoResumo> SemAcessos = new([], 0, 1, ConsultaDeAuditoriaOpcoes.TamanhoDaPagina);

    private bool _carregado;
    private bool _autorizado;
    private string? _erro;
    private Aba _aba = Aba.Alteracoes;
    private int _pagina = 1;
    private Filtro _filtro = new();
    private IReadOnlyList<UsuarioResumo> _usuarios = [];
    private Pagina<RegistroAuditoriaResumo> _alteracoes = SemAlteracoes;
    private Pagina<RegistroAcessoResumo> _acessos = SemAcessos;

    private int Total => _aba == Aba.Alteracoes ? _alteracoes.Total : _acessos.Total;
    private int TotalDePaginas => _aba == Aba.Alteracoes ? _alteracoes.TotalDePaginas : _acessos.TotalDePaginas;

    protected override async Task OnInitializedAsync()
    {
        _autorizado = await Despachante.EnviarAsync(new ObterMeuAcesso()) is { EhDiretoria: true };
        if (_autorizado)
        {
            _usuarios = (await Despachante.EnviarAsync(new ListarUsuarios())).Valor ?? [];
            await CarregarAsync();
        }
        _carregado = true;
    }

    private Task TrocarAbaAsync(Aba aba)
    {
        _aba = aba;
        return IrParaAsync(1);
    }

    private Task FiltrarAsync() => IrParaAsync(1);

    private Task IrParaAsync(int pagina)
    {
        _pagina = pagina;
        return CarregarAsync();
    }

    private async Task CarregarAsync()
    {
        // Os dois limites são dias do escritório: "até" inclui o dia informado inteiro.
        DateTime? de = _filtro.De is { } d ? Fuso.InicioDoDiaEmUtc(d) : null;
        DateTime? ate = _filtro.Ate is { } a ? Fuso.InicioDoDiaEmUtc(a.AddDays(1)) : null;

        if (_aba == Aba.Alteracoes)
        {
            var resultado = await Despachante.EnviarAsync(new ConsultarAuditoria(
                new FiltroDeAuditoria(de, ate, _filtro.UsuarioId, Limpo(_filtro.Entidade), _filtro.Operacao), _pagina));
            _alteracoes = resultado.Valor ?? SemAlteracoes;
            Concluir(resultado);
        }
        else
        {
            var resultado = await Despachante.EnviarAsync(new ConsultarAcessos(
                new FiltroDeAcessos(de, ate, _filtro.UsuarioId, Limpo(_filtro.TipoRecurso), _filtro.Acao), _pagina));
            _acessos = resultado.Valor ?? SemAcessos;
            Concluir(resultado);
        }
    }

    private void Concluir(Resultado resultado)
    {
        // Perder o acesso no meio do uso é diferente de qualquer outra falha.
        if (resultado.Erro == TipoErro.AcessoNegado) _autorizado = false;
        _erro = resultado.Sucesso || resultado.Erro == TipoErro.AcessoNegado ? null : resultado.Mensagem;
    }

    private static string? Limpo(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    private static string Rotulo(OperacaoAuditada operacao) => operacao switch
    {
        OperacaoAuditada.Criacao => "Criação",
        OperacaoAuditada.Alteracao => "Alteração",
        OperacaoAuditada.Exclusao => "Exclusão",
        _ => "Evento",
    };

    private sealed class Filtro
    {
        public DateOnly? De { get; set; }
        public DateOnly? Ate { get; set; }
        public Guid? UsuarioId { get; set; }
        public string? Entidade { get; set; }
        public OperacaoAuditada? Operacao { get; set; }
        public string? TipoRecurso { get; set; }
        public AcaoDeAcesso? Acao { get; set; }
    }
}
```

Acrescente a `wwwroot/app.css`:

```css
.filtros { grid-template-columns: repeat(auto-fit, minmax(11rem, 1fr)); align-items: end; margin: 1rem 0; }
td code { font-size: .8rem; word-break: break-all; }
```

- [ ] **Step 6: Rodar e confirmar que passa**

Run: `dotnet test`
Expected: tudo aprovado, zero avisos.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "feat(auditoria): consulta de alterações, eventos e acessos para a Diretoria"
```
