# Fase 1 — Fundação e Identidade: Plano de Implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Colocar de pé a solução em quatro camadas com usuários, perfis, login por cookie e gestão de usuários pela Diretoria.

**Architecture:** Monolito DDD em quatro projetos (`Domain`, `Application`, `Infrastructure`, `Web`). As páginas Blazor Interactive Server enviam requisições a um despachante, que abre um escopo de DI por requisição, fixa nele o usuário autenticado e executa o manipulador do caso de uso. O perfil e o estado ativo do usuário são lidos do banco a cada requisição; o cookie guarda apenas o Id e o nome.

**Tech Stack:** .NET 10, Blazor Interactive Server, EF Core 10 + Npgsql, PostgreSQL 17, xUnit 2.9, FluentAssertions 7.2, Testcontainers.

**Spec:** `docs/superpowers/specs/2026-10-06-modelagem-dominio-design.md` (seções 2, 4, 5, 11 e 12).

## Sequência de planos

A spec cobre seis contextos. Cada um vira um plano próprio, que entrega software funcionando e testável:

1. **Fundação e Identidade** (este plano)
2. Auditoria (`RegistroAuditoria`, `RegistroAcesso`, tela de consulta) — antes dos cadastros, para que tudo já nasça auditado
3. Cadastros (`AreaAtuacao`, `Cliente`, `Processo`)
4. Documentos (armazenamento, listas de acesso, áreas do cliente, gestão de perfis)
5. Atividades
6. Agenda

Fora deste plano, por pertencerem a planos seguintes: eventos de segurança na auditoria (plano 2); criação e edição de perfis (plano 4, porque criar um perfil altera a lista de acesso dos Documentos Gerais); exclusão de usuários pela interface (a inativação cobre a necessidade desta fase).

## Global Constraints

- TFM único `net10.0`, declarado somente em `Directory.Build.props`. Nenhum `.csproj` declara `TargetFramework`.
- `Nullable` habilitado e `TreatWarningsAsErrors=true` em todos os projetos.
- Versões de pacote somente em `Directory.Packages.props` (Central Package Management).
- `Domain` não referencia nenhum pacote nem projeto.
- `Application` referencia apenas `Domain`. Não referencia EF Core.
- Código, nomes de tabela e mensagens em português. Tabelas e colunas em `snake_case`.
- `Id` é Guid versão 7 (`Guid.CreateVersion7()`).
- Datas gravadas em UTC; exibição no fuso configurado em `Escritorio:FusoHorario`.
- Exclusão sempre lógica (`ExcluidoEm`, `ExcluidoPor`) com filtro global.
- Um caso de uso por arquivo, contendo a requisição e o manipulador, no namespace do contexto.
- Autorização verificada no manipulador; a interface apenas oculta.
- Toda página exige autenticação por padrão; anônimo é exceção declarada.
- `SenhaHash` em PBKDF2; nunca exposto em modelos de leitura.
- Sempre existe ao menos um usuário ativo com perfil de tipo `Diretoria`.
- Sem MediatR, sem FluentValidation, sem biblioteca de componentes de UI nesta fase.

## Review Focus

1. **E-mail com maiúsculas ou espaços** no login e no cadastro: `" Ana@X.com "` e `"ana@x.com"` são o mesmo usuário; o segundo cadastro é recusado como conflito. (Tarefas 2 e 4)
2. **Usuário inativado com cookie ainda válido**: toda requisição passa a ser negada, sem esperar o cookie expirar. (Tarefas 3 e 6)
3. **Diretor inativando a si mesmo ou trocando o próprio perfil** sendo o último diretor ativo: recusado. (Tarefa 4)
4. **`ReturnUrl` apontando para outro site** no login (`https://mal.example`, `//mal.example`, `/\mal.example`): redireciona para `/`. (Tarefa 6)
5. **Página nova sem atributo de autorização**: nasce protegida; acesso anônimo redireciona para o login. (Tarefa 6)

---

## Estrutura de arquivos

```
Directory.Build.props
Directory.Packages.props
global.json
.gitignore
docker-compose.yml
src/
  AtividadesJuridicas.Domain/
    Comum/Entidade.cs                  base: Id, carimbos, exclusão lógica
    Comum/ExcecaoDeDominio.cs
    Identidade/Email.cs                value object
    Identidade/TipoPerfil.cs
    Identidade/Perfil.cs
    Identidade/Usuario.cs
  AtividadesJuridicas.Application/
    Comum/Resultado.cs                 Resultado, Resultado<T>, TipoErro
    Comum/Requisicoes.cs               IRequisicao, IManipulador, IDespachante
    Comum/Portas.cs                    IUnidadeDeTrabalho, IRelogio, ExcecaoDeConflito
    Identidade/IdentidadeAtual.cs      IIdentidadeAtual + implementação mutável
    Identidade/ContextoDeAcesso.cs     UsuarioAutenticado, IContextoDeAcesso
    Identidade/Portas.cs               repositórios, IHasherDeSenha, modelos de leitura
    Identidade/PoliticaDeSenha.cs
    Identidade/UltimoDiretor.cs
    Identidade/<CasoDeUso>.cs          um arquivo por caso de uso
    DependencyInjection.cs
  AtividadesJuridicas.Infrastructure/
    Persistencia/AppDbContext.cs
    Persistencia/AppDbContextFabrica.cs       design-time
    Persistencia/Configuracoes/*.cs           uma por entidade
    Persistencia/Repositorios/*.cs
    Persistencia/UnidadeDeTrabalho.cs
    Persistencia/SemeadorInicial.cs
    Persistencia/Migrations/
    Seguranca/HasherDeSenhaPbkdf2.cs
    Comum/RelogioDoSistema.cs
    DependencyInjection.cs
  AtividadesJuridicas.Web/
    Program.cs
    Infra/Despachante.cs
    Infra/ValidacaoDeSessao.cs
    Infra/UrlLocal.cs
    Infra/FusoDoEscritorio.cs
    Components/App.razor, Routes.razor, _Imports.razor
    Components/Layout/MainLayout.razor, LayoutVazio.razor
    Components/Pages/Home.razor, Usuarios.razor, MinhaSenha.razor
    Components/Pages/Conta/Entrar.razor
tests/
  AtividadesJuridicas.UnitTests/         Domain + Application, sem banco
  AtividadesJuridicas.IntegrationTests/  Infrastructure + Web, PostgreSQL em contêiner
```

---

### Task 1: Estrutura da solução e base do domínio

**Files:**
- Create: `Directory.Build.props`, `Directory.Packages.props`, `global.json`, `.gitignore`, `docker-compose.yml`
- Create: os seis `.csproj` listados abaixo
- Create: `src/AtividadesJuridicas.Domain/Comum/Entidade.cs`, `src/AtividadesJuridicas.Domain/Comum/ExcecaoDeDominio.cs`
- Test: `tests/AtividadesJuridicas.UnitTests/Domain/EntidadeTests.cs`, `tests/AtividadesJuridicas.UnitTests/ArquiteturaTests.cs`

**Interfaces:**
- Produces: `AtividadesJuridicas.Domain.Comum.Entidade` com `Id`, `CriadoEm`, `CriadoPor`, `AtualizadoEm`, `AtualizadoPor`, `ExcluidoEm`, `ExcluidoPor`, `Excluido`, `CarimbarCriacao(DateTime, Guid?)`, `CarimbarAtualizacao(DateTime, Guid?)`, `MarcarExcluido(DateTime, Guid?)`; `ExcecaoDeDominio(string mensagem)`.

- [ ] **Step 1: Gerar os projetos**

Na raiz do repositório:

```bash
dotnet new sln -n AtividadesJuridicas
dotnet new classlib -o src/AtividadesJuridicas.Domain
dotnet new classlib -o src/AtividadesJuridicas.Application
dotnet new classlib -o src/AtividadesJuridicas.Infrastructure
dotnet new blazor -o src/AtividadesJuridicas.Web --interactivity Server --all-interactive --empty
dotnet new xunit -o tests/AtividadesJuridicas.UnitTests
dotnet new xunit -o tests/AtividadesJuridicas.IntegrationTests
dotnet sln add src/AtividadesJuridicas.Domain src/AtividadesJuridicas.Application src/AtividadesJuridicas.Infrastructure src/AtividadesJuridicas.Web tests/AtividadesJuridicas.UnitTests tests/AtividadesJuridicas.IntegrationTests
dotnet new gitignore
dotnet new tool-manifest
dotnet tool install dotnet-ef --version 10.0.1
```

Apague os arquivos de exemplo: `Class1.cs` nos três `classlib` e `UnitTest1.cs` nos dois projetos de teste. Acrescente ao `.gitignore` uma linha `logs/`.

- [ ] **Step 2: Escrever os arquivos de build**

`global.json`:

```json
{
  "sdk": { "version": "10.0.203", "rollForward": "latestFeature" }
}
```

`Directory.Build.props`:

```xml
<Project>
  <!-- Fonte única do TFM. Nenhum .csproj declara TargetFramework. -->
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>latest</LangVersion>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

`Directory.Packages.props`:

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.1" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="10.0.1" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.1" />
    <PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.0" />
    <PackageVersion Include="EFCore.NamingConventions" Version="10.0.0" />
    <PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.1" />
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageVersion Include="xunit" Version="2.9.3" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="3.1.4" />
    <PackageVersion Include="FluentAssertions" Version="7.2.0" />
    <PackageVersion Include="Testcontainers.PostgreSql" Version="4.1.0" />
  </ItemGroup>
</Project>
```

- [ ] **Step 3: Substituir o conteúdo dos `.csproj`**

`src/AtividadesJuridicas.Domain/AtividadesJuridicas.Domain.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
</Project>
```

`src/AtividadesJuridicas.Application/AtividadesJuridicas.Application.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <ProjectReference Include="..\AtividadesJuridicas.Domain\AtividadesJuridicas.Domain.csproj" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />
  </ItemGroup>
</Project>
```

`src/AtividadesJuridicas.Infrastructure/AtividadesJuridicas.Infrastructure.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <ProjectReference Include="..\AtividadesJuridicas.Application\AtividadesJuridicas.Application.csproj" />
    <ProjectReference Include="..\AtividadesJuridicas.Domain\AtividadesJuridicas.Domain.csproj" />
    <PackageReference Include="Microsoft.EntityFrameworkCore" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" PrivateAssets="all" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
    <PackageReference Include="EFCore.NamingConventions" />
  </ItemGroup>
</Project>
```

`src/AtividadesJuridicas.Web/AtividadesJuridicas.Web.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <BlazorDisableThrowNavigationException>true</BlazorDisableThrowNavigationException>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\AtividadesJuridicas.Application\AtividadesJuridicas.Application.csproj" />
    <ProjectReference Include="..\AtividadesJuridicas.Infrastructure\AtividadesJuridicas.Infrastructure.csproj" />
  </ItemGroup>
</Project>
```

`tests/AtividadesJuridicas.UnitTests/AtividadesJuridicas.UnitTests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\AtividadesJuridicas.Application\AtividadesJuridicas.Application.csproj" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="FluentAssertions" />
  </ItemGroup>
  <ItemGroup>
    <Using Include="Xunit" />
    <Using Include="FluentAssertions" />
  </ItemGroup>
</Project>
```

`tests/AtividadesJuridicas.IntegrationTests/AtividadesJuridicas.IntegrationTests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\AtividadesJuridicas.Web\AtividadesJuridicas.Web.csproj" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="Testcontainers.PostgreSql" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />
  </ItemGroup>
  <ItemGroup>
    <Using Include="Xunit" />
    <Using Include="FluentAssertions" />
  </ItemGroup>
</Project>
```

`docker-compose.yml` (banco de desenvolvimento, porta 5544 para não colidir com outros PostgreSQL da máquina):

```yaml
services:
  banco:
    image: postgres:17-alpine
    environment:
      POSTGRES_USER: atividades
      POSTGRES_PASSWORD: atividades
      POSTGRES_DB: atividades
    ports:
      - "5544:5432"
    volumes:
      - dados_banco:/var/lib/postgresql/data
volumes:
  dados_banco:
```

- [ ] **Step 4: Escrever os testes que falham**

`tests/AtividadesJuridicas.UnitTests/Domain/EntidadeTests.cs`:

```csharp
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.UnitTests.Domain;

public class EntidadeTests
{
    private sealed class Exemplo : Entidade;

    private static readonly DateTime Agora = new(2026, 10, 6, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Id_nasce_preenchido_com_guid_versao_7()
    {
        var entidade = new Exemplo();

        entidade.Id.Should().NotBe(Guid.Empty);
        entidade.Id.Version.Should().Be(7);
    }

    [Fact]
    public void Carimbar_criacao_registra_momento_e_usuario()
    {
        var entidade = new Exemplo();
        var usuario = Guid.CreateVersion7();

        entidade.CarimbarCriacao(Agora, usuario);

        entidade.CriadoEm.Should().Be(Agora);
        entidade.CriadoPor.Should().Be(usuario);
        entidade.AtualizadoEm.Should().BeNull();
    }

    [Fact]
    public void Carimbar_atualizacao_nao_altera_a_criacao()
    {
        var entidade = new Exemplo();
        entidade.CarimbarCriacao(Agora, null);

        entidade.CarimbarAtualizacao(Agora.AddHours(1), Guid.CreateVersion7());

        entidade.CriadoEm.Should().Be(Agora);
        entidade.AtualizadoEm.Should().Be(Agora.AddHours(1));
        entidade.AtualizadoPor.Should().NotBeNull();
    }

    [Fact]
    public void Marcar_excluido_e_idempotente()
    {
        var entidade = new Exemplo();
        var primeiro = Guid.CreateVersion7();

        entidade.MarcarExcluido(Agora, primeiro);
        entidade.MarcarExcluido(Agora.AddDays(1), Guid.CreateVersion7());

        entidade.Excluido.Should().BeTrue();
        entidade.ExcluidoEm.Should().Be(Agora);
        entidade.ExcluidoPor.Should().Be(primeiro);
    }

    [Fact]
    public void Carimbos_recusam_data_que_nao_seja_utc()
    {
        var entidade = new Exemplo();
        var local = new DateTime(2026, 10, 6, 12, 0, 0, DateTimeKind.Local);

        var acao = () => entidade.CarimbarCriacao(local, null);

        acao.Should().Throw<ArgumentException>();
    }
}
```

`tests/AtividadesJuridicas.UnitTests/ArquiteturaTests.cs`:

```csharp
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.UnitTests;

public class ArquiteturaTests
{
    [Fact]
    public void Domain_nao_referencia_nada_alem_da_biblioteca_base()
    {
        var referencias = typeof(Entidade).Assembly.GetReferencedAssemblies().Select(a => a.Name!);

        referencias.Should().OnlyContain(nome => nome.StartsWith("System") || nome == "netstandard");
    }
}
```

- [ ] **Step 5: Rodar e confirmar a falha**

Run: `dotnet test tests/AtividadesJuridicas.UnitTests`
Expected: erro de compilação, `Entidade` não existe.

- [ ] **Step 6: Implementar**

`src/AtividadesJuridicas.Domain/Comum/ExcecaoDeDominio.cs`:

```csharp
namespace AtividadesJuridicas.Domain.Comum;

/// <summary>Violação de uma regra do domínio. A mensagem é exibida ao usuário.</summary>
public sealed class ExcecaoDeDominio(string mensagem) : Exception(mensagem);
```

`src/AtividadesJuridicas.Domain/Comum/Entidade.cs`:

```csharp
namespace AtividadesJuridicas.Domain.Comum;

public abstract class Entidade
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public DateTime CriadoEm { get; private set; }
    public Guid? CriadoPor { get; private set; }
    public DateTime? AtualizadoEm { get; private set; }
    public Guid? AtualizadoPor { get; private set; }
    public DateTime? ExcluidoEm { get; private set; }
    public Guid? ExcluidoPor { get; private set; }

    public bool Excluido => ExcluidoEm is not null;

    public void CarimbarCriacao(DateTime agoraUtc, Guid? usuarioId)
    {
        CriadoEm = ExigirUtc(agoraUtc);
        CriadoPor = usuarioId;
    }

    public void CarimbarAtualizacao(DateTime agoraUtc, Guid? usuarioId)
    {
        AtualizadoEm = ExigirUtc(agoraUtc);
        AtualizadoPor = usuarioId;
    }

    public void MarcarExcluido(DateTime agoraUtc, Guid? usuarioId)
    {
        if (Excluido) return;
        ExcluidoEm = ExigirUtc(agoraUtc);
        ExcluidoPor = usuarioId;
    }

    private static DateTime ExigirUtc(DateTime valor) =>
        valor.Kind == DateTimeKind.Utc
            ? valor
            : throw new ArgumentException("A data deve estar em UTC.", nameof(valor));
}
```

- [ ] **Step 7: Rodar e confirmar que passa**

Run: `dotnet build` e depois `dotnet test tests/AtividadesJuridicas.UnitTests`
Expected: build sem avisos; 6 testes aprovados.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "chore: estrutura da solução e entidade base do domínio"
```

---

### Task 2: Identidade no domínio

**Files:**
- Create: `src/AtividadesJuridicas.Domain/Identidade/Email.cs`, `TipoPerfil.cs`, `Perfil.cs`, `Usuario.cs`
- Test: `tests/AtividadesJuridicas.UnitTests/Domain/EmailTests.cs`, `PerfilTests.cs`, `UsuarioTests.cs`

**Interfaces:**
- Consumes: `Entidade`, `ExcecaoDeDominio`.
- Produces:
  - `Email.Criar(string?)` (lança `ExcecaoDeDominio`), `Email.TentarCriar(string?, out Email?)`, `Email.Valor`.
  - `enum TipoPerfil { Diretoria = 1, Producao = 2 }`.
  - `Perfil.Criar(string nome, TipoPerfil tipo)`, `Nome`, `Tipo`, `Ativo`, `Inativar()`, `Ativar()`.
  - `Usuario.Criar(string nome, Email email, string senhaHash, Perfil perfil, string? numeroOab = null)`, `AlterarDados(string, Email, string?)`, `AlterarPerfil(Perfil)`, `DefinirSenha(string)`, `Inativar()`, `Ativar()`, `RegistrarAcesso(DateTime)`; propriedades `Nome`, `Email`, `SenhaHash`, `PerfilId`, `NumeroOab`, `Ativo`, `UltimoAcessoEm`.

- [ ] **Step 1: Escrever os testes que falham**

`tests/AtividadesJuridicas.UnitTests/Domain/EmailTests.cs`:

```csharp
using AtividadesJuridicas.Domain.Comum;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.UnitTests.Domain;

public class EmailTests
{
    [Fact]
    public void Normaliza_espacos_e_maiusculas()
    {
        Email.Criar("  Ana@Escritorio.COM ").Valor.Should().Be("ana@escritorio.com");
    }

    [Fact]
    public void Emails_que_diferem_so_em_caixa_sao_iguais()
    {
        Email.Criar("Ana@X.com").Should().Be(Email.Criar("ana@x.com"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("semarroba.com")]
    [InlineData("@x.com")]
    [InlineData("a@")]
    [InlineData("a@b")]
    [InlineData("a@@b.com")]
    [InlineData("a b@c.com")]
    [InlineData("a@.com")]
    [InlineData("a@b.")]
    public void Recusa_formato_invalido(string? texto)
    {
        Email.TentarCriar(texto, out var email).Should().BeFalse();
        email.Should().BeNull();

        var acao = () => Email.Criar(texto);
        acao.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void Recusa_email_maior_que_254_caracteres()
    {
        var longo = new string('a', 250) + "@x.com";

        Email.TentarCriar(longo, out _).Should().BeFalse();
    }
}
```

`tests/AtividadesJuridicas.UnitTests/Domain/PerfilTests.cs`:

```csharp
using AtividadesJuridicas.Domain.Comum;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.UnitTests.Domain;

public class PerfilTests
{
    [Fact]
    public void Nasce_ativo_com_nome_aparado()
    {
        var perfil = Perfil.Criar("  Diretor ", TipoPerfil.Diretoria);

        perfil.Nome.Should().Be("Diretor");
        perfil.Tipo.Should().Be(TipoPerfil.Diretoria);
        perfil.Ativo.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Recusa_nome_vazio(string nome)
    {
        var acao = () => Perfil.Criar(nome, TipoPerfil.Producao);

        acao.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void Recusa_tipo_fora_do_enum()
    {
        var acao = () => Perfil.Criar("X", (TipoPerfil)99);

        acao.Should().Throw<ExcecaoDeDominio>();
    }
}
```

`tests/AtividadesJuridicas.UnitTests/Domain/UsuarioTests.cs`:

```csharp
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
}
```

- [ ] **Step 2: Rodar e confirmar a falha**

Run: `dotnet test tests/AtividadesJuridicas.UnitTests`
Expected: erro de compilação, namespace `AtividadesJuridicas.Domain.Identidade` não existe.

- [ ] **Step 3: Implementar**

`src/AtividadesJuridicas.Domain/Identidade/Email.cs`:

```csharp
using System.Diagnostics.CodeAnalysis;
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Domain.Identidade;

public sealed record Email
{
    public const int TamanhoMaximo = 254;

    public string Valor { get; }

    private Email(string valor) => Valor = valor;

    public static Email Criar(string? texto) =>
        TentarCriar(texto, out var email) ? email : throw new ExcecaoDeDominio("E-mail inválido.");

    public static bool TentarCriar(string? texto, [NotNullWhen(true)] out Email? email)
    {
        email = null;
        var valor = texto?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(valor) || valor.Length > TamanhoMaximo) return false;
        if (valor.Any(char.IsWhiteSpace)) return false;

        var arroba = valor.IndexOf('@');
        if (arroba <= 0 || arroba != valor.LastIndexOf('@')) return false;

        var dominio = valor[(arroba + 1)..];
        if (dominio.Length < 3 || !dominio.Contains('.')) return false;
        if (dominio.StartsWith('.') || dominio.EndsWith('.')) return false;

        email = new Email(valor);
        return true;
    }

    public override string ToString() => Valor;
}
```

`src/AtividadesJuridicas.Domain/Identidade/TipoPerfil.cs`:

```csharp
namespace AtividadesJuridicas.Domain.Identidade;

public enum TipoPerfil
{
    Diretoria = 1,
    Producao = 2,
}
```

`src/AtividadesJuridicas.Domain/Identidade/Perfil.cs`:

```csharp
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Domain.Identidade;

public sealed class Perfil : Entidade
{
    public const int TamanhoMaximoNome = 100;

    private Perfil() { }

    public string Nome { get; private set; } = null!;
    public TipoPerfil Tipo { get; private set; }
    public bool Ativo { get; private set; }

    public static Perfil Criar(string nome, TipoPerfil tipo)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ExcecaoDeDominio("O nome do perfil é obrigatório.");
        if (nome.Trim().Length > TamanhoMaximoNome)
            throw new ExcecaoDeDominio($"O nome do perfil deve ter até {TamanhoMaximoNome} caracteres.");
        if (!Enum.IsDefined(tipo))
            throw new ExcecaoDeDominio("Tipo de perfil inválido.");

        return new Perfil { Nome = nome.Trim(), Tipo = tipo, Ativo = true };
    }

    public void Inativar() => Ativo = false;

    public void Ativar() => Ativo = true;
}
```

`src/AtividadesJuridicas.Domain/Identidade/Usuario.cs`:

```csharp
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Domain.Identidade;

public sealed class Usuario : Entidade
{
    public const int TamanhoMaximoNome = 200;
    public const int TamanhoMaximoOab = 20;

    private Usuario() { }

    public string Nome { get; private set; } = null!;
    public Email Email { get; private set; } = null!;
    public string SenhaHash { get; private set; } = null!;
    public Guid PerfilId { get; private set; }
    public string? NumeroOab { get; private set; }
    public bool Ativo { get; private set; }
    public DateTime? UltimoAcessoEm { get; private set; }

    public static Usuario Criar(string nome, Email email, string senhaHash, Perfil perfil, string? numeroOab = null)
    {
        var usuario = new Usuario { Ativo = true };
        usuario.AlterarDados(nome, email, numeroOab);
        usuario.DefinirSenha(senhaHash);
        usuario.AlterarPerfil(perfil);
        return usuario;
    }

    public void AlterarDados(string nome, Email email, string? numeroOab)
    {
        ArgumentNullException.ThrowIfNull(email);
        if (string.IsNullOrWhiteSpace(nome))
            throw new ExcecaoDeDominio("O nome é obrigatório.");
        if (nome.Trim().Length > TamanhoMaximoNome)
            throw new ExcecaoDeDominio($"O nome deve ter até {TamanhoMaximoNome} caracteres.");

        var oab = string.IsNullOrWhiteSpace(numeroOab) ? null : numeroOab.Trim();
        if (oab is { Length: > TamanhoMaximoOab })
            throw new ExcecaoDeDominio($"O número da OAB deve ter até {TamanhoMaximoOab} caracteres.");

        Nome = nome.Trim();
        Email = email;
        NumeroOab = oab;
    }

    public void AlterarPerfil(Perfil perfil)
    {
        ArgumentNullException.ThrowIfNull(perfil);
        if (!perfil.Ativo)
            throw new ExcecaoDeDominio("Não é possível atribuir um perfil inativo.");
        PerfilId = perfil.Id;
    }

    public void DefinirSenha(string senhaHash)
    {
        if (string.IsNullOrWhiteSpace(senhaHash))
            throw new ArgumentException("O hash da senha é obrigatório.", nameof(senhaHash));
        SenhaHash = senhaHash;
    }

    public void Inativar() => Ativo = false;

    public void Ativar() => Ativo = true;

    public void RegistrarAcesso(DateTime agoraUtc) => UltimoAcessoEm = agoraUtc;
}
```

- [ ] **Step 4: Rodar e confirmar que passa**

Run: `dotnet test tests/AtividadesJuridicas.UnitTests`
Expected: todos aprovados, incluindo `Domain_nao_referencia_nada_alem_da_biblioteca_base`.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat(identidade): Email, Perfil e Usuario no domínio"
```

---

### Task 3: Fundação da Application

**Files:**
- Create: `src/AtividadesJuridicas.Application/Comum/Resultado.cs`, `Comum/Requisicoes.cs`, `Comum/Portas.cs`
- Create: `src/AtividadesJuridicas.Application/Identidade/IdentidadeAtual.cs`, `Identidade/Portas.cs`, `Identidade/ContextoDeAcesso.cs`, `Identidade/ObterMeuAcesso.cs`
- Create: `src/AtividadesJuridicas.Application/DependencyInjection.cs`
- Test: `tests/AtividadesJuridicas.UnitTests/Application/Falsos.cs`, `ContextoDeAcessoTests.cs`, `RegistroDeManipuladoresTests.cs`

**Interfaces:**
- Consumes: `Usuario`, `Perfil`, `Email`, `TipoPerfil`.
- Produces:
  - `enum TipoErro { Validacao, NaoEncontrado, AcessoNegado, Conflito }`; `Resultado.Ok()`, `Resultado.Falha(TipoErro, string)`, `Resultado<T>.Ok(T)`, `Resultado<T>.Falha(TipoErro, string)`; propriedades `Sucesso`, `Erro`, `Mensagem`, `Valor`.
  - `IRequisicao<TResposta>`; `IManipulador<TRequisicao, TResposta>.ManipularAsync(TRequisicao, CancellationToken)`; `IDespachante.EnviarAsync<TResposta>(IRequisicao<TResposta>, CancellationToken = default)`.
  - `IUnidadeDeTrabalho.SalvarAsync(CancellationToken)`; `IRelogio.AgoraUtc`; `ExcecaoDeConflito`.
  - `IIdentidadeAtual.UsuarioId`; `IdentidadeAtual.Definir(Guid?)`.
  - `IUsuarioRepositorio`, `IPerfilRepositorio`, `IHasherDeSenha`, `UsuarioResumo`, `PerfilResumo` (assinaturas no Step 3).
  - `record UsuarioAutenticado(Guid Id, string Nome, TipoPerfil TipoPerfil)` com `EhDiretoria`; `IContextoDeAcesso.ObterAsync(CancellationToken)`.
  - `record ObterMeuAcesso : IRequisicao<UsuarioAutenticado?>`.
  - `services.AddApplication()`.

- [ ] **Step 1: Escrever os testes que falham**

`tests/AtividadesJuridicas.UnitTests/Application/Falsos.cs` (duplos de teste usados por esta tarefa e pela seguinte):

```csharp
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.UnitTests.Application;

public sealed class PerfilRepositorioFalso : IPerfilRepositorio
{
    public List<Perfil> Itens { get; } = [];

    public Task<Perfil?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Itens.FirstOrDefault(p => p.Id == id));

    public Task<IReadOnlyList<PerfilResumo>> ListarAtivosAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<PerfilResumo>>(
            [.. Itens.Where(p => p.Ativo).Select(p => new PerfilResumo(p.Id, p.Nome, p.Tipo))]);
}

public sealed class UsuarioRepositorioFalso(PerfilRepositorioFalso perfis) : IUsuarioRepositorio
{
    public List<Usuario> Itens { get; } = [];

    public Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Itens.FirstOrDefault(u => u.Id == id));

    public Task<Usuario?> ObterPorEmailAsync(Email email, CancellationToken ct) =>
        Task.FromResult(Itens.FirstOrDefault(u => u.Email == email));

    public Task<bool> ExisteEmailAsync(Email email, Guid? excetoUsuarioId, CancellationToken ct) =>
        Task.FromResult(Itens.Any(u => u.Email == email && u.Id != excetoUsuarioId));

    public Task<int> ContarDiretoresAtivosAsync(Guid? excetoUsuarioId, CancellationToken ct) =>
        Task.FromResult(Itens.Count(u =>
            u.Ativo && u.Id != excetoUsuarioId &&
            perfis.Itens.Any(p => p.Id == u.PerfilId && p.Tipo == TipoPerfil.Diretoria)));

    public Task<IReadOnlyList<UsuarioResumo>> ListarAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<UsuarioResumo>>(
            [.. Itens.OrderBy(u => u.Nome).Select(u =>
            {
                var perfil = perfis.Itens.First(p => p.Id == u.PerfilId);
                return new UsuarioResumo(u.Id, u.Nome, u.Email.Valor, perfil.Id, perfil.Nome,
                    perfil.Tipo, u.NumeroOab, u.Ativo, u.UltimoAcessoEm);
            })]);

    public void Adicionar(Usuario usuario) => Itens.Add(usuario);
}

public sealed class UnidadeDeTrabalhoFalsa : IUnidadeDeTrabalho
{
    public int Salvamentos { get; private set; }

    public Task SalvarAsync(CancellationToken ct)
    {
        Salvamentos++;
        return Task.CompletedTask;
    }
}

public sealed class HasherFalso : IHasherDeSenha
{
    public string Gerar(string senha) => "hash:" + senha;

    public bool Verificar(string senha, string hash) => hash == "hash:" + senha;
}

public sealed class RelogioFixo : IRelogio
{
    public DateTime AgoraUtc { get; set; } = new(2026, 10, 6, 12, 0, 0, DateTimeKind.Utc);
}

public sealed class ContextoDeAcessoFixo(UsuarioAutenticado? ator) : IContextoDeAcesso
{
    public Task<UsuarioAutenticado?> ObterAsync(CancellationToken ct) => Task.FromResult(ator);
}

/// <summary>Monta um cenário com os dois perfis padrão e um diretor ativo.</summary>
public sealed class Cenario
{
    public PerfilRepositorioFalso Perfis { get; } = new();
    public UsuarioRepositorioFalso Usuarios { get; }
    public UnidadeDeTrabalhoFalsa Unidade { get; } = new();
    public HasherFalso Hasher { get; } = new();
    public RelogioFixo Relogio { get; } = new();
    public Perfil PerfilDiretor { get; } = Perfil.Criar("Diretor", TipoPerfil.Diretoria);
    public Perfil PerfilProducao { get; } = Perfil.Criar("Produção", TipoPerfil.Producao);
    public Usuario Diretor { get; }

    public Cenario()
    {
        Usuarios = new UsuarioRepositorioFalso(Perfis);
        Perfis.Itens.AddRange([PerfilDiretor, PerfilProducao]);
        Diretor = NovoUsuario("Diana Diretora", "diana@x.com", PerfilDiretor);
    }

    public Usuario NovoUsuario(string nome, string email, Perfil perfil, string senha = "senha-segura")
    {
        var usuario = Usuario.Criar(nome, Email.Criar(email), Hasher.Gerar(senha), perfil);
        Usuarios.Itens.Add(usuario);
        return usuario;
    }

    public IContextoDeAcesso Como(Usuario? usuario)
    {
        if (usuario is null) return new ContextoDeAcessoFixo(null);
        var perfil = Perfis.Itens.First(p => p.Id == usuario.PerfilId);
        return new ContextoDeAcessoFixo(new UsuarioAutenticado(usuario.Id, usuario.Nome, perfil.Tipo));
    }
}
```

`tests/AtividadesJuridicas.UnitTests/Application/ContextoDeAcessoTests.cs`:

```csharp
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
```

`tests/AtividadesJuridicas.UnitTests/Application/RegistroDeManipuladoresTests.cs`:

```csharp
using AtividadesJuridicas.Application;
using AtividadesJuridicas.Application.Comum;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.UnitTests.Application;

public class RegistroDeManipuladoresTests
{
    [Fact]
    public void Toda_requisicao_tem_exatamente_um_manipulador_registrado()
    {
        var servicos = new ServiceCollection().AddApplication();

        var requisicoes = typeof(DependencyInjection).Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequisicao<>))
                .Select(i => typeof(IManipulador<,>).MakeGenericType(t, i.GetGenericArguments()[0])))
            .ToList();

        requisicoes.Should().NotBeEmpty();
        foreach (var contrato in requisicoes)
            servicos.Count(s => s.ServiceType == contrato).Should().Be(1, $"{contrato} deve ter um manipulador");
    }
}
```

- [ ] **Step 2: Rodar e confirmar a falha**

Run: `dotnet test tests/AtividadesJuridicas.UnitTests`
Expected: erro de compilação, namespace `AtividadesJuridicas.Application.Comum` não existe.

- [ ] **Step 3: Implementar**

`src/AtividadesJuridicas.Application/Comum/Resultado.cs`:

```csharp
namespace AtividadesJuridicas.Application.Comum;

public enum TipoErro
{
    Validacao,
    NaoEncontrado,
    AcessoNegado,
    Conflito,
}

public class Resultado
{
    protected Resultado(bool sucesso, TipoErro? erro, string? mensagem)
    {
        Sucesso = sucesso;
        Erro = erro;
        Mensagem = mensagem;
    }

    public bool Sucesso { get; }
    public TipoErro? Erro { get; }
    public string? Mensagem { get; }

    public static Resultado Ok() => new(true, null, null);

    public static Resultado Falha(TipoErro erro, string mensagem) => new(false, erro, mensagem);
}

public sealed class Resultado<T> : Resultado
{
    private Resultado(T? valor, bool sucesso, TipoErro? erro, string? mensagem)
        : base(sucesso, erro, mensagem) => Valor = valor;

    public T? Valor { get; }

    public static Resultado<T> Ok(T valor) => new(valor, true, null, null);

    public static new Resultado<T> Falha(TipoErro erro, string mensagem) => new(default, false, erro, mensagem);
}
```

`src/AtividadesJuridicas.Application/Comum/Requisicoes.cs`:

```csharp
namespace AtividadesJuridicas.Application.Comum;

/// <summary>Marca um comando ou consulta e declara o tipo da resposta.</summary>
public interface IRequisicao<TResposta>;

public interface IManipulador<in TRequisicao, TResposta>
    where TRequisicao : IRequisicao<TResposta>
{
    Task<TResposta> ManipularAsync(TRequisicao requisicao, CancellationToken ct);
}

/// <summary>
/// Ponto único de entrada da interface para os casos de uso. Cada envio roda em
/// um escopo de DI próprio, para que o DbContext não viva o tempo do circuito Blazor.
/// </summary>
public interface IDespachante
{
    Task<TResposta> EnviarAsync<TResposta>(IRequisicao<TResposta> requisicao, CancellationToken ct = default);
}
```

`src/AtividadesJuridicas.Application/Comum/Portas.cs`:

```csharp
namespace AtividadesJuridicas.Application.Comum;

public interface IUnidadeDeTrabalho
{
    /// <summary>Grava as alterações pendentes. Lança <see cref="ExcecaoDeConflito"/> em violação de unicidade.</summary>
    Task SalvarAsync(CancellationToken ct);
}

public interface IRelogio
{
    DateTime AgoraUtc { get; }
}

/// <summary>Outra operação gravou um valor que deveria ser único.</summary>
public sealed class ExcecaoDeConflito(string mensagem, Exception? interna = null) : Exception(mensagem, interna);
```

`src/AtividadesJuridicas.Application/Identidade/IdentidadeAtual.cs`:

```csharp
namespace AtividadesJuridicas.Application.Identidade;

/// <summary>Id do usuário autenticado na requisição em curso; nulo quando anônimo ou sistema.</summary>
public interface IIdentidadeAtual
{
    Guid? UsuarioId { get; }
}

/// <summary>Preenchida pelo despachante no início de cada escopo de requisição.</summary>
public sealed class IdentidadeAtual : IIdentidadeAtual
{
    public Guid? UsuarioId { get; private set; }

    public void Definir(Guid? usuarioId) => UsuarioId = usuarioId;
}
```

`src/AtividadesJuridicas.Application/Identidade/Portas.cs`:

```csharp
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.Application.Identidade;

public sealed record UsuarioResumo(
    Guid Id,
    string Nome,
    string Email,
    Guid PerfilId,
    string PerfilNome,
    TipoPerfil TipoPerfil,
    string? NumeroOab,
    bool Ativo,
    DateTime? UltimoAcessoEm);

public sealed record PerfilResumo(Guid Id, string Nome, TipoPerfil Tipo);

public interface IUsuarioRepositorio
{
    Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken ct);
    Task<Usuario?> ObterPorEmailAsync(Email email, CancellationToken ct);
    Task<bool> ExisteEmailAsync(Email email, Guid? excetoUsuarioId, CancellationToken ct);

    /// <summary>Usuários ativos cujo perfil é de tipo Diretoria, desconsiderando <paramref name="excetoUsuarioId"/>.</summary>
    Task<int> ContarDiretoresAtivosAsync(Guid? excetoUsuarioId, CancellationToken ct);

    Task<IReadOnlyList<UsuarioResumo>> ListarAsync(CancellationToken ct);
    void Adicionar(Usuario usuario);
}

public interface IPerfilRepositorio
{
    Task<Perfil?> ObterPorIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<PerfilResumo>> ListarAtivosAsync(CancellationToken ct);
}

public interface IHasherDeSenha
{
    string Gerar(string senha);
    bool Verificar(string senha, string hash);
}
```

`src/AtividadesJuridicas.Application/Identidade/ContextoDeAcesso.cs`:

```csharp
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.Application.Identidade;

public sealed record UsuarioAutenticado(Guid Id, string Nome, TipoPerfil TipoPerfil)
{
    public bool EhDiretoria => TipoPerfil == TipoPerfil.Diretoria;
}

public interface IContextoDeAcesso
{
    /// <summary>
    /// Usuário da requisição, com o perfil lido do banco agora. Nulo quando não há
    /// usuário autenticado, quando ele não existe mais ou quando está inativo.
    /// </summary>
    Task<UsuarioAutenticado?> ObterAsync(CancellationToken ct);
}

public sealed class ContextoDeAcesso(
    IIdentidadeAtual identidade,
    IUsuarioRepositorio usuarios,
    IPerfilRepositorio perfis) : IContextoDeAcesso
{
    public async Task<UsuarioAutenticado?> ObterAsync(CancellationToken ct)
    {
        if (identidade.UsuarioId is not { } id) return null;

        var usuario = await usuarios.ObterPorIdAsync(id, ct);
        if (usuario is null || !usuario.Ativo) return null;

        var perfil = await perfis.ObterPorIdAsync(usuario.PerfilId, ct);
        if (perfil is null) return null;

        return new UsuarioAutenticado(usuario.Id, usuario.Nome, perfil.Tipo);
    }
}
```

`src/AtividadesJuridicas.Application/Identidade/ObterMeuAcesso.cs`:

```csharp
using AtividadesJuridicas.Application.Comum;

namespace AtividadesJuridicas.Application.Identidade;

/// <summary>Usado pela interface para decidir o que exibir. Nulo quando o usuário não tem mais acesso.</summary>
public sealed record ObterMeuAcesso : IRequisicao<UsuarioAutenticado?>;

public sealed class ObterMeuAcessoManipulador(IContextoDeAcesso contexto)
    : IManipulador<ObterMeuAcesso, UsuarioAutenticado?>
{
    public Task<UsuarioAutenticado?> ManipularAsync(ObterMeuAcesso requisicao, CancellationToken ct) =>
        contexto.ObterAsync(ct);
}
```

`src/AtividadesJuridicas.Application/DependencyInjection.cs`:

```csharp
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection servicos)
    {
        servicos.AddScoped<IdentidadeAtual>();
        servicos.AddScoped<IIdentidadeAtual>(sp => sp.GetRequiredService<IdentidadeAtual>());
        servicos.AddScoped<IContextoDeAcesso, ContextoDeAcesso>();

        var tipos = typeof(DependencyInjection).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false });

        foreach (var tipo in tipos)
        {
            var contratos = tipo.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IManipulador<,>));

            foreach (var contrato in contratos)
                servicos.AddScoped(contrato, tipo);
        }

        return servicos;
    }
}
```

- [ ] **Step 4: Rodar e confirmar que passa**

Run: `dotnet test tests/AtividadesJuridicas.UnitTests`
Expected: todos aprovados.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat(application): resultado, requisições, portas e contexto de acesso"
```

---

### Task 4: Casos de uso de usuário

**Files:**
- Create em `src/AtividadesJuridicas.Application/Identidade/`: `PoliticaDeSenha.cs`, `UltimoDiretor.cs`, `AutenticarUsuario.cs`, `ListarUsuarios.cs`, `ListarPerfisAtivos.cs`, `CriarUsuario.cs`, `EditarUsuario.cs`, `DefinirAtivoUsuario.cs`, `RedefinirSenhaUsuario.cs`, `AlterarMinhaSenha.cs`
- Test: `tests/AtividadesJuridicas.UnitTests/Application/AutenticarUsuarioTests.cs`, `GestaoDeUsuariosTests.cs`, `SenhaTests.cs`

**Interfaces:**
- Consumes: tudo o que a Task 3 produz; `Cenario` e os duplos de `Falsos.cs`.
- Produces (todas no namespace `AtividadesJuridicas.Application.Identidade`):
  - `record AutenticarUsuario(string Email, string Senha) : IRequisicao<Resultado<UsuarioAutenticado>>`
  - `record ListarUsuarios : IRequisicao<Resultado<IReadOnlyList<UsuarioResumo>>>`
  - `record ListarPerfisAtivos : IRequisicao<Resultado<IReadOnlyList<PerfilResumo>>>`
  - `record CriarUsuario(string Nome, string Email, string Senha, Guid PerfilId, string? NumeroOab) : IRequisicao<Resultado<Guid>>`
  - `record EditarUsuario(Guid Id, string Nome, string Email, Guid PerfilId, string? NumeroOab) : IRequisicao<Resultado>`
  - `record DefinirAtivoUsuario(Guid Id, bool Ativo) : IRequisicao<Resultado>`
  - `record RedefinirSenhaUsuario(Guid Id, string NovaSenha) : IRequisicao<Resultado>`
  - `record AlterarMinhaSenha(string SenhaAtual, string NovaSenha) : IRequisicao<Resultado>`
  - `PoliticaDeSenha.Validar(string?)` devolve a mensagem de erro ou nulo.
- Cada manipulador se chama `<Requisicao>Manipulador` e recebe as dependências por construtor primário, na ordem usada nos testes abaixo.

- [ ] **Step 1: Escrever os testes que falham**

`tests/AtividadesJuridicas.UnitTests/Application/AutenticarUsuarioTests.cs`:

```csharp
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.UnitTests.Application;

public class AutenticarUsuarioTests
{
    private readonly Cenario _c = new();

    private Task<Resultado<UsuarioAutenticado>> Autenticar(string email, string senha) =>
        new AutenticarUsuarioManipulador(_c.Usuarios, _c.Perfis, _c.Hasher, _c.Unidade, _c.Relogio)
            .ManipularAsync(new AutenticarUsuario(email, senha), default);

    [Fact]
    public async Task Credenciais_corretas_autenticam_e_registram_o_acesso()
    {
        var resultado = await Autenticar("diana@x.com", "senha-segura");

        resultado.Sucesso.Should().BeTrue();
        resultado.Valor.Should().Be(new UsuarioAutenticado(_c.Diretor.Id, "Diana Diretora", TipoPerfil.Diretoria));
        _c.Diretor.UltimoAcessoEm.Should().Be(_c.Relogio.AgoraUtc);
        _c.Unidade.Salvamentos.Should().Be(1);
    }

    [Fact]
    public async Task Email_com_maiusculas_e_espacos_encontra_o_mesmo_usuario()
    {
        (await Autenticar("  DIANA@X.com ", "senha-segura")).Sucesso.Should().BeTrue();
    }

    [Theory]
    [InlineData("diana@x.com", "senha-errada")]
    [InlineData("ninguem@x.com", "senha-segura")]
    [InlineData("nao-e-email", "senha-segura")]
    [InlineData("", "")]
    public async Task Falhas_devolvem_sempre_a_mesma_mensagem(string email, string senha)
    {
        var resultado = await Autenticar(email, senha);

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be(TipoErro.AcessoNegado);
        resultado.Mensagem.Should().Be("E-mail ou senha inválidos.");
        _c.Unidade.Salvamentos.Should().Be(0);
    }

    [Fact]
    public async Task Usuario_inativo_nao_autentica_mesmo_com_a_senha_certa()
    {
        _c.Diretor.Inativar();

        var resultado = await Autenticar("diana@x.com", "senha-segura");

        resultado.Sucesso.Should().BeFalse();
        resultado.Mensagem.Should().Be("E-mail ou senha inválidos.");
    }
}
```

`tests/AtividadesJuridicas.UnitTests/Application/GestaoDeUsuariosTests.cs`:

```csharp
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.UnitTests.Application;

public class GestaoDeUsuariosTests
{
    private readonly Cenario _c = new();

    private Task<Resultado<Guid>> Criar(Usuario? ator, CriarUsuario requisicao) =>
        new CriarUsuarioManipulador(_c.Como(ator), _c.Usuarios, _c.Perfis, _c.Hasher, _c.Unidade)
            .ManipularAsync(requisicao, default);

    private Task<Resultado> Editar(Usuario? ator, EditarUsuario requisicao) =>
        new EditarUsuarioManipulador(_c.Como(ator), _c.Usuarios, _c.Perfis, _c.Unidade)
            .ManipularAsync(requisicao, default);

    private Task<Resultado> DefinirAtivo(Usuario? ator, Guid id, bool ativo) =>
        new DefinirAtivoUsuarioManipulador(_c.Como(ator), _c.Usuarios, _c.Perfis, _c.Unidade)
            .ManipularAsync(new DefinirAtivoUsuario(id, ativo), default);

    private CriarUsuario NovoPedido(string email = "paulo@x.com") =>
        new("Paulo Produção", email, "senha-segura", _c.PerfilProducao.Id, "AL 99");

    // ---- Criar ----

    [Fact]
    public async Task Diretor_cria_usuario_com_senha_em_hash()
    {
        var resultado = await Criar(_c.Diretor, NovoPedido());

        resultado.Sucesso.Should().BeTrue();
        var criado = _c.Usuarios.Itens.Single(u => u.Id == resultado.Valor);
        criado.Email.Valor.Should().Be("paulo@x.com");
        criado.PerfilId.Should().Be(_c.PerfilProducao.Id);
        criado.SenhaHash.Should().Be("hash:senha-segura");
        criado.Ativo.Should().BeTrue();
        _c.Unidade.Salvamentos.Should().Be(1);
    }

    [Fact]
    public async Task Producao_e_anonimo_nao_criam_usuario()
    {
        var producao = _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);

        (await Criar(producao, NovoPedido())).Erro.Should().Be(TipoErro.AcessoNegado);
        (await Criar(null, NovoPedido())).Erro.Should().Be(TipoErro.AcessoNegado);
        _c.Usuarios.Itens.Should().HaveCount(2);
    }

    [Fact]
    public async Task Email_ja_usado_em_outra_caixa_e_conflito()
    {
        var resultado = await Criar(_c.Diretor, NovoPedido(" DIANA@x.com "));

        resultado.Erro.Should().Be(TipoErro.Conflito);
    }

    [Theory]
    [InlineData("", "paulo@x.com", "senha-segura")]
    [InlineData("Paulo", "invalido", "senha-segura")]
    [InlineData("Paulo", "paulo@x.com", "curta")]
    public async Task Dados_invalidos_sao_erro_de_validacao(string nome, string email, string senha)
    {
        var resultado = await Criar(_c.Diretor, new CriarUsuario(nome, email, senha, _c.PerfilProducao.Id, null));

        resultado.Erro.Should().Be(TipoErro.Validacao);
        _c.Unidade.Salvamentos.Should().Be(0);
    }

    [Fact]
    public async Task Perfil_inexistente_ou_inativo_e_erro_de_validacao()
    {
        _c.PerfilProducao.Inativar();

        (await Criar(_c.Diretor, NovoPedido())).Erro.Should().Be(TipoErro.Validacao);
        (await Criar(_c.Diretor, NovoPedido() with { PerfilId = Guid.CreateVersion7() }))
            .Erro.Should().Be(TipoErro.Validacao);
    }

    // ---- Editar ----

    [Fact]
    public async Task Diretor_edita_dados_e_perfil_de_outro_usuario()
    {
        var alvo = _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);

        var resultado = await Editar(_c.Diretor,
            new EditarUsuario(alvo.Id, "Pedro Silva", "pedro.silva@x.com", _c.PerfilDiretor.Id, null));

        resultado.Sucesso.Should().BeTrue();
        alvo.Nome.Should().Be("Pedro Silva");
        alvo.Email.Valor.Should().Be("pedro.silva@x.com");
        alvo.PerfilId.Should().Be(_c.PerfilDiretor.Id);
    }

    [Fact]
    public async Task Editar_mantendo_o_proprio_email_nao_e_conflito()
    {
        var resultado = await Editar(_c.Diretor,
            new EditarUsuario(_c.Diretor.Id, "Diana D.", "diana@x.com", _c.PerfilDiretor.Id, null));

        resultado.Sucesso.Should().BeTrue();
    }

    [Fact]
    public async Task Editar_para_email_de_outro_usuario_e_conflito()
    {
        var alvo = _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);

        var resultado = await Editar(_c.Diretor,
            new EditarUsuario(alvo.Id, "Pedro", "diana@x.com", _c.PerfilProducao.Id, null));

        resultado.Erro.Should().Be(TipoErro.Conflito);
    }

    [Fact]
    public async Task Editar_usuario_inexistente_e_nao_encontrado()
    {
        var resultado = await Editar(_c.Diretor,
            new EditarUsuario(Guid.CreateVersion7(), "X", "x@x.com", _c.PerfilProducao.Id, null));

        resultado.Erro.Should().Be(TipoErro.NaoEncontrado);
    }

    [Fact]
    public async Task Ultimo_diretor_nao_pode_trocar_o_proprio_perfil_para_producao()
    {
        var resultado = await Editar(_c.Diretor,
            new EditarUsuario(_c.Diretor.Id, "Diana", "diana@x.com", _c.PerfilProducao.Id, null));

        resultado.Erro.Should().Be(TipoErro.Conflito);
        _c.Diretor.PerfilId.Should().Be(_c.PerfilDiretor.Id);
        _c.Unidade.Salvamentos.Should().Be(0);
    }

    [Fact]
    public async Task Havendo_outro_diretor_ativo_a_troca_de_perfil_e_permitida()
    {
        _c.NovoUsuario("Dora", "dora@x.com", _c.PerfilDiretor);

        var resultado = await Editar(_c.Diretor,
            new EditarUsuario(_c.Diretor.Id, "Diana", "diana@x.com", _c.PerfilProducao.Id, null));

        resultado.Sucesso.Should().BeTrue();
    }

    [Fact]
    public async Task Outro_diretor_inativo_nao_conta_para_liberar_a_troca()
    {
        _c.NovoUsuario("Dora", "dora@x.com", _c.PerfilDiretor).Inativar();

        var resultado = await Editar(_c.Diretor,
            new EditarUsuario(_c.Diretor.Id, "Diana", "diana@x.com", _c.PerfilProducao.Id, null));

        resultado.Erro.Should().Be(TipoErro.Conflito);
    }

    // ---- Ativar / inativar ----

    [Fact]
    public async Task Ultimo_diretor_nao_pode_inativar_a_si_mesmo()
    {
        var resultado = await DefinirAtivo(_c.Diretor, _c.Diretor.Id, false);

        resultado.Erro.Should().Be(TipoErro.Conflito);
        _c.Diretor.Ativo.Should().BeTrue();
    }

    [Fact]
    public async Task Diretor_inativa_e_reativa_usuario_de_producao()
    {
        var alvo = _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);

        (await DefinirAtivo(_c.Diretor, alvo.Id, false)).Sucesso.Should().BeTrue();
        alvo.Ativo.Should().BeFalse();

        (await DefinirAtivo(_c.Diretor, alvo.Id, true)).Sucesso.Should().BeTrue();
        alvo.Ativo.Should().BeTrue();
    }

    [Fact]
    public async Task Producao_nao_inativa_ninguem()
    {
        var producao = _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);

        (await DefinirAtivo(producao, _c.Diretor.Id, false)).Erro.Should().Be(TipoErro.AcessoNegado);
    }

    // ---- Listar ----

    [Fact]
    public async Task Diretor_lista_usuarios_e_perfis_ativos()
    {
        _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);

        var usuarios = await new ListarUsuariosManipulador(_c.Como(_c.Diretor), _c.Usuarios)
            .ManipularAsync(new ListarUsuarios(), default);
        var perfis = await new ListarPerfisAtivosManipulador(_c.Como(_c.Diretor), _c.Perfis)
            .ManipularAsync(new ListarPerfisAtivos(), default);

        usuarios.Valor!.Select(u => u.Nome).Should().Equal("Diana Diretora", "Pedro");
        perfis.Valor!.Select(p => p.Nome).Should().BeEquivalentTo("Diretor", "Produção");
    }

    [Fact]
    public async Task Producao_nao_lista_usuarios()
    {
        var producao = _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);

        var resultado = await new ListarUsuariosManipulador(_c.Como(producao), _c.Usuarios)
            .ManipularAsync(new ListarUsuarios(), default);

        resultado.Erro.Should().Be(TipoErro.AcessoNegado);
        resultado.Valor.Should().BeNull();
    }
}
```

`tests/AtividadesJuridicas.UnitTests/Application/SenhaTests.cs`:

```csharp
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;

namespace AtividadesJuridicas.UnitTests.Application;

public class SenhaTests
{
    private readonly Cenario _c = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1234567")]
    [InlineData("        ")]
    public void Politica_recusa_senha_curta_ou_em_branco(string? senha)
    {
        PoliticaDeSenha.Validar(senha).Should().NotBeNull();
    }

    [Fact]
    public void Politica_recusa_senha_maior_que_128_caracteres()
    {
        PoliticaDeSenha.Validar(new string('a', 129)).Should().NotBeNull();
    }

    [Fact]
    public void Politica_aceita_senha_de_8_a_128_caracteres()
    {
        PoliticaDeSenha.Validar("12345678").Should().BeNull();
        PoliticaDeSenha.Validar(new string('a', 128)).Should().BeNull();
    }

    [Fact]
    public async Task Diretor_redefine_a_senha_de_outro_usuario()
    {
        var alvo = _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);

        var resultado = await new RedefinirSenhaUsuarioManipulador(_c.Como(_c.Diretor), _c.Usuarios, _c.Hasher, _c.Unidade)
            .ManipularAsync(new RedefinirSenhaUsuario(alvo.Id, "nova-senha-1"), default);

        resultado.Sucesso.Should().BeTrue();
        alvo.SenhaHash.Should().Be("hash:nova-senha-1");
    }

    [Fact]
    public async Task Producao_nao_redefine_senha_de_ninguem()
    {
        var producao = _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);

        var resultado = await new RedefinirSenhaUsuarioManipulador(_c.Como(producao), _c.Usuarios, _c.Hasher, _c.Unidade)
            .ManipularAsync(new RedefinirSenhaUsuario(_c.Diretor.Id, "nova-senha-1"), default);

        resultado.Erro.Should().Be(TipoErro.AcessoNegado);
        _c.Diretor.SenhaHash.Should().Be("hash:senha-segura");
    }

    [Fact]
    public async Task Usuario_altera_a_propria_senha_informando_a_atual()
    {
        var producao = _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);

        var resultado = await new AlterarMinhaSenhaManipulador(_c.Como(producao), _c.Usuarios, _c.Hasher, _c.Unidade)
            .ManipularAsync(new AlterarMinhaSenha("senha-segura", "nova-senha-1"), default);

        resultado.Sucesso.Should().BeTrue();
        producao.SenhaHash.Should().Be("hash:nova-senha-1");
    }

    [Fact]
    public async Task Senha_atual_errada_nao_altera_nada()
    {
        var resultado = await new AlterarMinhaSenhaManipulador(_c.Como(_c.Diretor), _c.Usuarios, _c.Hasher, _c.Unidade)
            .ManipularAsync(new AlterarMinhaSenha("errada", "nova-senha-1"), default);

        resultado.Erro.Should().Be(TipoErro.Validacao);
        _c.Diretor.SenhaHash.Should().Be("hash:senha-segura");
    }

    [Fact]
    public async Task Anonimo_nao_altera_senha()
    {
        var resultado = await new AlterarMinhaSenhaManipulador(_c.Como(null), _c.Usuarios, _c.Hasher, _c.Unidade)
            .ManipularAsync(new AlterarMinhaSenha("senha-segura", "nova-senha-1"), default);

        resultado.Erro.Should().Be(TipoErro.AcessoNegado);
    }
}
```

- [ ] **Step 2: Rodar e confirmar a falha**

Run: `dotnet test tests/AtividadesJuridicas.UnitTests`
Expected: erro de compilação, `AutenticarUsuarioManipulador` e demais não existem.

- [ ] **Step 3: Implementar os auxiliares**

`src/AtividadesJuridicas.Application/Identidade/PoliticaDeSenha.cs`:

```csharp
namespace AtividadesJuridicas.Application.Identidade;

public static class PoliticaDeSenha
{
    public const int TamanhoMinimo = 8;
    public const int TamanhoMaximo = 128;

    /// <summary>Devolve a mensagem de erro, ou nulo quando a senha é aceita.</summary>
    public static string? Validar(string? senha)
    {
        if (string.IsNullOrWhiteSpace(senha) || senha.Length < TamanhoMinimo)
            return $"A senha deve ter ao menos {TamanhoMinimo} caracteres.";
        if (senha.Length > TamanhoMaximo)
            return $"A senha deve ter até {TamanhoMaximo} caracteres.";
        return null;
    }
}
```

`src/AtividadesJuridicas.Application/Identidade/UltimoDiretor.cs`:

```csharp
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.Application.Identidade;

/// <summary>Regra: o sistema nunca fica sem um usuário ativo de tipo Diretoria.</summary>
internal static class UltimoDiretor
{
    public const string Mensagem = "O sistema precisa de ao menos um usuário ativo de Diretoria.";

    /// <summary>Verdadeiro quando <paramref name="usuario"/> é hoje o único diretor ativo.</summary>
    public static async Task<bool> EhAsync(
        Usuario usuario, IUsuarioRepositorio usuarios, IPerfilRepositorio perfis, CancellationToken ct)
    {
        if (!usuario.Ativo) return false;

        var perfil = await perfis.ObterPorIdAsync(usuario.PerfilId, ct);
        if (perfil?.Tipo != TipoPerfil.Diretoria) return false;

        return await usuarios.ContarDiretoresAtivosAsync(usuario.Id, ct) == 0;
    }
}
```

- [ ] **Step 4: Implementar os casos de uso**

`src/AtividadesJuridicas.Application/Identidade/AutenticarUsuario.cs`:

```csharp
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.Application.Identidade;

public sealed record AutenticarUsuario(string Email, string Senha) : IRequisicao<Resultado<UsuarioAutenticado>>;

public sealed class AutenticarUsuarioManipulador(
    IUsuarioRepositorio usuarios,
    IPerfilRepositorio perfis,
    IHasherDeSenha hasher,
    IUnidadeDeTrabalho unidade,
    IRelogio relogio) : IManipulador<AutenticarUsuario, Resultado<UsuarioAutenticado>>
{
    // Mensagem única: não revela se o e-mail existe nem se o usuário está inativo.
    private static readonly Resultado<UsuarioAutenticado> Invalido =
        Resultado<UsuarioAutenticado>.Falha(TipoErro.AcessoNegado, "E-mail ou senha inválidos.");

    public async Task<Resultado<UsuarioAutenticado>> ManipularAsync(AutenticarUsuario requisicao, CancellationToken ct)
    {
        if (!Email.TentarCriar(requisicao.Email, out var email)) return Invalido;

        var usuario = await usuarios.ObterPorEmailAsync(email, ct);
        if (usuario is null || !usuario.Ativo) return Invalido;
        if (!hasher.Verificar(requisicao.Senha ?? string.Empty, usuario.SenhaHash)) return Invalido;

        var perfil = await perfis.ObterPorIdAsync(usuario.PerfilId, ct);
        if (perfil is null) return Invalido;

        usuario.RegistrarAcesso(relogio.AgoraUtc);
        await unidade.SalvarAsync(ct);

        return Resultado<UsuarioAutenticado>.Ok(new UsuarioAutenticado(usuario.Id, usuario.Nome, perfil.Tipo));
    }
}
```

`src/AtividadesJuridicas.Application/Identidade/ListarUsuarios.cs`:

```csharp
using AtividadesJuridicas.Application.Comum;

namespace AtividadesJuridicas.Application.Identidade;

public sealed record ListarUsuarios : IRequisicao<Resultado<IReadOnlyList<UsuarioResumo>>>;

public sealed class ListarUsuariosManipulador(IContextoDeAcesso contexto, IUsuarioRepositorio usuarios)
    : IManipulador<ListarUsuarios, Resultado<IReadOnlyList<UsuarioResumo>>>
{
    public async Task<Resultado<IReadOnlyList<UsuarioResumo>>> ManipularAsync(ListarUsuarios requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Resultado<IReadOnlyList<UsuarioResumo>>.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        return Resultado<IReadOnlyList<UsuarioResumo>>.Ok(await usuarios.ListarAsync(ct));
    }
}
```

`src/AtividadesJuridicas.Application/Identidade/ListarPerfisAtivos.cs`:

```csharp
using AtividadesJuridicas.Application.Comum;

namespace AtividadesJuridicas.Application.Identidade;

public sealed record ListarPerfisAtivos : IRequisicao<Resultado<IReadOnlyList<PerfilResumo>>>;

public sealed class ListarPerfisAtivosManipulador(IContextoDeAcesso contexto, IPerfilRepositorio perfis)
    : IManipulador<ListarPerfisAtivos, Resultado<IReadOnlyList<PerfilResumo>>>
{
    public async Task<Resultado<IReadOnlyList<PerfilResumo>>> ManipularAsync(ListarPerfisAtivos requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Resultado<IReadOnlyList<PerfilResumo>>.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        return Resultado<IReadOnlyList<PerfilResumo>>.Ok(await perfis.ListarAtivosAsync(ct));
    }
}
```

`src/AtividadesJuridicas.Application/Identidade/CriarUsuario.cs`:

```csharp
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Domain.Comum;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.Application.Identidade;

public sealed record CriarUsuario(string Nome, string Email, string Senha, Guid PerfilId, string? NumeroOab)
    : IRequisicao<Resultado<Guid>>;

public sealed class CriarUsuarioManipulador(
    IContextoDeAcesso contexto,
    IUsuarioRepositorio usuarios,
    IPerfilRepositorio perfis,
    IHasherDeSenha hasher,
    IUnidadeDeTrabalho unidade) : IManipulador<CriarUsuario, Resultado<Guid>>
{
    private const string EmailEmUso = "Já existe um usuário com este e-mail.";

    public async Task<Resultado<Guid>> ManipularAsync(CriarUsuario requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Resultado<Guid>.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        if (!Email.TentarCriar(requisicao.Email, out var email))
            return Resultado<Guid>.Falha(TipoErro.Validacao, "E-mail inválido.");
        if (PoliticaDeSenha.Validar(requisicao.Senha) is { } erroSenha)
            return Resultado<Guid>.Falha(TipoErro.Validacao, erroSenha);

        var perfil = await perfis.ObterPorIdAsync(requisicao.PerfilId, ct);
        if (perfil is null)
            return Resultado<Guid>.Falha(TipoErro.Validacao, "Perfil não encontrado.");

        if (await usuarios.ExisteEmailAsync(email, null, ct))
            return Resultado<Guid>.Falha(TipoErro.Conflito, EmailEmUso);

        try
        {
            var usuario = Usuario.Criar(requisicao.Nome, email, hasher.Gerar(requisicao.Senha), perfil, requisicao.NumeroOab);
            usuarios.Adicionar(usuario);
            await unidade.SalvarAsync(ct);
            return Resultado<Guid>.Ok(usuario.Id);
        }
        catch (ExcecaoDeDominio ex)
        {
            return Resultado<Guid>.Falha(TipoErro.Validacao, ex.Message);
        }
        catch (ExcecaoDeConflito)
        {
            return Resultado<Guid>.Falha(TipoErro.Conflito, EmailEmUso);
        }
    }
}
```

`src/AtividadesJuridicas.Application/Identidade/EditarUsuario.cs`:

```csharp
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Domain.Comum;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.Application.Identidade;

public sealed record EditarUsuario(Guid Id, string Nome, string Email, Guid PerfilId, string? NumeroOab)
    : IRequisicao<Resultado>;

public sealed class EditarUsuarioManipulador(
    IContextoDeAcesso contexto,
    IUsuarioRepositorio usuarios,
    IPerfilRepositorio perfis,
    IUnidadeDeTrabalho unidade) : IManipulador<EditarUsuario, Resultado>
{
    private const string EmailEmUso = "Já existe um usuário com este e-mail.";

    public async Task<Resultado> ManipularAsync(EditarUsuario requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Resultado.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        var usuario = await usuarios.ObterPorIdAsync(requisicao.Id, ct);
        if (usuario is null)
            return Resultado.Falha(TipoErro.NaoEncontrado, "Usuário não encontrado.");

        if (!Email.TentarCriar(requisicao.Email, out var email))
            return Resultado.Falha(TipoErro.Validacao, "E-mail inválido.");

        var perfil = await perfis.ObterPorIdAsync(requisicao.PerfilId, ct);
        if (perfil is null)
            return Resultado.Falha(TipoErro.Validacao, "Perfil não encontrado.");

        if (await usuarios.ExisteEmailAsync(email, usuario.Id, ct))
            return Resultado.Falha(TipoErro.Conflito, EmailEmUso);

        // Verificado antes de qualquer alteração, para não deixar a entidade pela metade.
        var deixaDeSerDiretor = perfil.Id != usuario.PerfilId && perfil.Tipo != TipoPerfil.Diretoria;
        if (deixaDeSerDiretor && await UltimoDiretor.EhAsync(usuario, usuarios, perfis, ct))
            return Resultado.Falha(TipoErro.Conflito, UltimoDiretor.Mensagem);

        try
        {
            if (perfil.Id != usuario.PerfilId) usuario.AlterarPerfil(perfil);
            usuario.AlterarDados(requisicao.Nome, email, requisicao.NumeroOab);
            await unidade.SalvarAsync(ct);
            return Resultado.Ok();
        }
        catch (ExcecaoDeDominio ex)
        {
            return Resultado.Falha(TipoErro.Validacao, ex.Message);
        }
        catch (ExcecaoDeConflito)
        {
            return Resultado.Falha(TipoErro.Conflito, EmailEmUso);
        }
    }
}
```

`src/AtividadesJuridicas.Application/Identidade/DefinirAtivoUsuario.cs`:

```csharp
using AtividadesJuridicas.Application.Comum;

namespace AtividadesJuridicas.Application.Identidade;

public sealed record DefinirAtivoUsuario(Guid Id, bool Ativo) : IRequisicao<Resultado>;

public sealed class DefinirAtivoUsuarioManipulador(
    IContextoDeAcesso contexto,
    IUsuarioRepositorio usuarios,
    IPerfilRepositorio perfis,
    IUnidadeDeTrabalho unidade) : IManipulador<DefinirAtivoUsuario, Resultado>
{
    public async Task<Resultado> ManipularAsync(DefinirAtivoUsuario requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Resultado.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        var usuario = await usuarios.ObterPorIdAsync(requisicao.Id, ct);
        if (usuario is null)
            return Resultado.Falha(TipoErro.NaoEncontrado, "Usuário não encontrado.");

        if (requisicao.Ativo)
        {
            usuario.Ativar();
        }
        else
        {
            if (await UltimoDiretor.EhAsync(usuario, usuarios, perfis, ct))
                return Resultado.Falha(TipoErro.Conflito, UltimoDiretor.Mensagem);
            usuario.Inativar();
        }

        await unidade.SalvarAsync(ct);
        return Resultado.Ok();
    }
}
```

`src/AtividadesJuridicas.Application/Identidade/RedefinirSenhaUsuario.cs`:

```csharp
using AtividadesJuridicas.Application.Comum;

namespace AtividadesJuridicas.Application.Identidade;

public sealed record RedefinirSenhaUsuario(Guid Id, string NovaSenha) : IRequisicao<Resultado>;

public sealed class RedefinirSenhaUsuarioManipulador(
    IContextoDeAcesso contexto,
    IUsuarioRepositorio usuarios,
    IHasherDeSenha hasher,
    IUnidadeDeTrabalho unidade) : IManipulador<RedefinirSenhaUsuario, Resultado>
{
    public async Task<Resultado> ManipularAsync(RedefinirSenhaUsuario requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Resultado.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        if (PoliticaDeSenha.Validar(requisicao.NovaSenha) is { } erro)
            return Resultado.Falha(TipoErro.Validacao, erro);

        var usuario = await usuarios.ObterPorIdAsync(requisicao.Id, ct);
        if (usuario is null)
            return Resultado.Falha(TipoErro.NaoEncontrado, "Usuário não encontrado.");

        usuario.DefinirSenha(hasher.Gerar(requisicao.NovaSenha));
        await unidade.SalvarAsync(ct);
        return Resultado.Ok();
    }
}
```

`src/AtividadesJuridicas.Application/Identidade/AlterarMinhaSenha.cs`:

```csharp
using AtividadesJuridicas.Application.Comum;

namespace AtividadesJuridicas.Application.Identidade;

public sealed record AlterarMinhaSenha(string SenhaAtual, string NovaSenha) : IRequisicao<Resultado>;

public sealed class AlterarMinhaSenhaManipulador(
    IContextoDeAcesso contexto,
    IUsuarioRepositorio usuarios,
    IHasherDeSenha hasher,
    IUnidadeDeTrabalho unidade) : IManipulador<AlterarMinhaSenha, Resultado>
{
    public async Task<Resultado> ManipularAsync(AlterarMinhaSenha requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { } ator)
            return Resultado.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        var usuario = await usuarios.ObterPorIdAsync(ator.Id, ct);
        if (usuario is null)
            return Resultado.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        if (!hasher.Verificar(requisicao.SenhaAtual ?? string.Empty, usuario.SenhaHash))
            return Resultado.Falha(TipoErro.Validacao, "A senha atual não confere.");

        if (PoliticaDeSenha.Validar(requisicao.NovaSenha) is { } erro)
            return Resultado.Falha(TipoErro.Validacao, erro);

        usuario.DefinirSenha(hasher.Gerar(requisicao.NovaSenha));
        await unidade.SalvarAsync(ct);
        return Resultado.Ok();
    }
}
```

- [ ] **Step 5: Rodar e confirmar que passa**

Run: `dotnet test tests/AtividadesJuridicas.UnitTests`
Expected: todos aprovados, incluindo `Toda_requisicao_tem_exatamente_um_manipulador_registrado`.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "feat(identidade): casos de uso de autenticação e gestão de usuários"
```

---

### Task 5: Persistência

**Files:**
- Create em `src/AtividadesJuridicas.Infrastructure/`: `Comum/RelogioDoSistema.cs`, `Seguranca/HasherDeSenhaPbkdf2.cs`, `Persistencia/AppDbContext.cs`, `Persistencia/AppDbContextFabrica.cs`, `Persistencia/Configuracoes/PerfilConfiguracao.cs`, `Persistencia/Configuracoes/UsuarioConfiguracao.cs`, `Persistencia/Repositorios/UsuarioRepositorio.cs`, `Persistencia/Repositorios/PerfilRepositorio.cs`, `Persistencia/UnidadeDeTrabalho.cs`, `Persistencia/SemeadorInicial.cs`, `DependencyInjection.cs`
- Create (gerado): `src/AtividadesJuridicas.Infrastructure/Persistencia/Migrations/*`
- Test: `tests/AtividadesJuridicas.IntegrationTests/BancoFixture.cs`, `PersistenciaTests.cs`, `HasherDeSenhaTests.cs`

**Interfaces:**
- Consumes: portas da Task 3, entidades da Task 2.
- Produces:
  - `services.AddInfrastructure(Func<IServiceProvider, string> obterStringDeConexao)`. A string é resolvida quando o `DbContext` é criado, não no registro, para que a configuração final da aplicação (inclusive a dos testes) seja a usada.
  - `AppDbContext` com `DbSet<Usuario> Usuarios` e `DbSet<Perfil> Perfis`.
  - `record AdminInicialOpcoes(string Nome, string Email, string Senha)`; `SemeadorInicial.ExecutarAsync(AdminInicialOpcoes? admin, CancellationToken ct = default)`.
  - `BancoFixture.NovaConexao()` e a coleção xUnit `ColecaoBanco.Nome`, reutilizadas na Task 6.

- [ ] **Step 1: Escrever os testes que falham**

`tests/AtividadesJuridicas.IntegrationTests/BancoFixture.cs`:

```csharp
using Npgsql;
using Testcontainers.PostgreSql;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>Um contêiner PostgreSQL para toda a execução; cada teste pede um banco novo.</summary>
public sealed class BancoFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _conteiner = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .Build();

    public Task InitializeAsync() => _conteiner.StartAsync();

    public Task DisposeAsync() => _conteiner.DisposeAsync().AsTask();

    public string NovaConexao() =>
        new NpgsqlConnectionStringBuilder(_conteiner.GetConnectionString())
        {
            Database = "t_" + Guid.NewGuid().ToString("N"),
        }.ConnectionString;
}

[CollectionDefinition(Nome)]
public sealed class ColecaoBanco : ICollectionFixture<BancoFixture>
{
    public const string Nome = "banco";
}
```

`tests/AtividadesJuridicas.IntegrationTests/HasherDeSenhaTests.cs`:

```csharp
using AtividadesJuridicas.Infrastructure.Seguranca;

namespace AtividadesJuridicas.IntegrationTests;

public class HasherDeSenhaTests
{
    private readonly HasherDeSenhaPbkdf2 _hasher = new();

    [Fact]
    public void Hash_confere_com_a_senha_original_e_nao_a_contem()
    {
        var hash = _hasher.Gerar("senha-segura");

        hash.Should().StartWith("pbkdf2-sha256$").And.NotContain("senha-segura");
        _hasher.Verificar("senha-segura", hash).Should().BeTrue();
        _hasher.Verificar("senha-Segura", hash).Should().BeFalse();
    }

    [Fact]
    public void Mesma_senha_gera_hashes_diferentes()
    {
        _hasher.Gerar("senha-segura").Should().NotBe(_hasher.Gerar("senha-segura"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("texto-qualquer")]
    [InlineData("pbkdf2-sha256$abc$def$ghi")]
    [InlineData("pbkdf2-sha256$600000$%%%$%%%")]
    public void Hash_malformado_nao_confere_e_nao_lanca(string hash)
    {
        _hasher.Verificar("senha-segura", hash).Should().BeFalse();
    }
}
```

`tests/AtividadesJuridicas.IntegrationTests/PersistenciaTests.cs`:

```csharp
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
            sp.GetRequiredService<IManipulador<AutenticarUsuario, Resultado<UsuarioAutenticado>>>().ManipularAsync(
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
}
```

- [ ] **Step 2: Rodar e confirmar a falha**

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests`
Expected: erro de compilação, namespace `AtividadesJuridicas.Infrastructure` não existe. (O Docker precisa estar em execução a partir do Step 6.)

- [ ] **Step 3: Implementar relógio e hasher**

`src/AtividadesJuridicas.Infrastructure/Comum/RelogioDoSistema.cs`:

```csharp
using AtividadesJuridicas.Application.Comum;

namespace AtividadesJuridicas.Infrastructure.Comum;

public sealed class RelogioDoSistema : IRelogio
{
    public DateTime AgoraUtc => DateTime.UtcNow;
}
```

`src/AtividadesJuridicas.Infrastructure/Seguranca/HasherDeSenhaPbkdf2.cs`:

```csharp
using System.Security.Cryptography;
using AtividadesJuridicas.Application.Identidade;

namespace AtividadesJuridicas.Infrastructure.Seguranca;

/// <summary>Formato gravado: <c>pbkdf2-sha256$iteracoes$salt(base64)$hash(base64)</c>.</summary>
public sealed class HasherDeSenhaPbkdf2 : IHasherDeSenha
{
    private const string Prefixo = "pbkdf2-sha256";
    private const int Iteracoes = 600_000;
    private const int TamanhoSalt = 16;
    private const int TamanhoHash = 32;

    public string Gerar(string senha)
    {
        var salt = RandomNumberGenerator.GetBytes(TamanhoSalt);
        var hash = Rfc2898DeriveBytes.Pbkdf2(senha, salt, Iteracoes, HashAlgorithmName.SHA256, TamanhoHash);
        return $"{Prefixo}${Iteracoes}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public bool Verificar(string senha, string hash)
    {
        var partes = hash.Split('$');
        if (partes.Length != 4 || partes[0] != Prefixo) return false;
        if (!int.TryParse(partes[1], out var iteracoes) || iteracoes <= 0) return false;

        var salt = new byte[TamanhoSalt];
        var esperado = new byte[TamanhoHash];
        if (!Convert.TryFromBase64String(partes[2], salt, out var lidosSalt) || lidosSalt != TamanhoSalt) return false;
        if (!Convert.TryFromBase64String(partes[3], esperado, out var lidosHash) || lidosHash != TamanhoHash) return false;

        var calculado = Rfc2898DeriveBytes.Pbkdf2(senha, salt, iteracoes, HashAlgorithmName.SHA256, TamanhoHash);
        return CryptographicOperations.FixedTimeEquals(calculado, esperado);
    }
}
```

- [ ] **Step 4: Implementar o DbContext e os mapeamentos**

`src/AtividadesJuridicas.Infrastructure/Persistencia/Configuracoes/PerfilConfiguracao.cs`:

```csharp
using AtividadesJuridicas.Domain.Identidade;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AtividadesJuridicas.Infrastructure.Persistencia.Configuracoes;

public sealed class PerfilConfiguracao : IEntityTypeConfiguration<Perfil>
{
    public void Configure(EntityTypeBuilder<Perfil> b)
    {
        b.ToTable("perfis");
        b.Property(p => p.Nome).HasMaxLength(Perfil.TamanhoMaximoNome).IsRequired();
        b.Property(p => p.Tipo).HasConversion<int>();
        b.HasIndex(p => p.Nome).IsUnique().HasFilter("excluido_em IS NULL");
    }
}
```

`src/AtividadesJuridicas.Infrastructure/Persistencia/Configuracoes/UsuarioConfiguracao.cs`:

```csharp
using AtividadesJuridicas.Domain.Identidade;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AtividadesJuridicas.Infrastructure.Persistencia.Configuracoes;

public sealed class UsuarioConfiguracao : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> b)
    {
        b.ToTable("usuarios");
        b.Property(u => u.Nome).HasMaxLength(Usuario.TamanhoMaximoNome).IsRequired();
        b.Property(u => u.Email)
            .HasConversion(e => e.Valor, v => Email.Criar(v))
            .HasMaxLength(Email.TamanhoMaximo)
            .IsRequired();
        b.Property(u => u.SenhaHash).HasMaxLength(200).IsRequired();
        b.Property(u => u.NumeroOab).HasMaxLength(Usuario.TamanhoMaximoOab);

        // Filtrado: um e-mail de usuário excluído logicamente pode ser reutilizado.
        b.HasIndex(u => u.Email).IsUnique().HasFilter("excluido_em IS NULL");

        b.HasOne<Perfil>().WithMany().HasForeignKey(u => u.PerfilId).OnDelete(DeleteBehavior.Restrict);
    }
}
```

`src/AtividadesJuridicas.Infrastructure/Persistencia/AppDbContext.cs`:

```csharp
using System.Linq.Expressions;
using AtividadesJuridicas.Domain.Comum;
using AtividadesJuridicas.Domain.Identidade;
using Microsoft.EntityFrameworkCore;

namespace AtividadesJuridicas.Infrastructure.Persistencia;

public sealed class AppDbContext(DbContextOptions<AppDbContext> opcoes) : DbContext(opcoes)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Perfil> Perfis => Set<Perfil>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Convenções de toda Entidade: chave gerada no domínio e filtro de exclusão lógica.
        var raizes = modelBuilder.Model.GetEntityTypes()
            .Where(t => typeof(Entidade).IsAssignableFrom(t.ClrType) && t.BaseType is null)
            .Select(t => t.ClrType)
            .ToList();

        foreach (var tipo in raizes)
        {
            var entidade = modelBuilder.Entity(tipo);
            entidade.HasKey(nameof(Entidade.Id));
            entidade.Property(nameof(Entidade.Id)).ValueGeneratedNever();

            var parametro = Expression.Parameter(tipo, "e");
            var naoExcluido = Expression.Equal(
                Expression.Property(parametro, nameof(Entidade.ExcluidoEm)),
                Expression.Constant(null, typeof(DateTime?)));
            entidade.HasQueryFilter(Expression.Lambda(naoExcluido, parametro));
        }
    }
}
```

`src/AtividadesJuridicas.Infrastructure/Persistencia/AppDbContextFabrica.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AtividadesJuridicas.Infrastructure.Persistencia;

/// <summary>Usada só pelo <c>dotnet ef</c> para gerar migrations; não abre conexão.</summary>
public sealed class AppDbContextFabrica : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=design_time")
            .UseSnakeCaseNamingConvention()
            .Options);
}
```

- [ ] **Step 5: Implementar repositórios, unidade de trabalho, semeador e DI**

`src/AtividadesJuridicas.Infrastructure/Persistencia/Repositorios/PerfilRepositorio.cs`:

```csharp
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Identidade;
using Microsoft.EntityFrameworkCore;

namespace AtividadesJuridicas.Infrastructure.Persistencia.Repositorios;

public sealed class PerfilRepositorio(AppDbContext db) : IPerfilRepositorio
{
    public Task<Perfil?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        db.Perfis.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<PerfilResumo>> ListarAtivosAsync(CancellationToken ct) =>
        await db.Perfis.AsNoTracking()
            .Where(p => p.Ativo)
            .OrderBy(p => p.Nome)
            .Select(p => new PerfilResumo(p.Id, p.Nome, p.Tipo))
            .ToListAsync(ct);
}
```

`src/AtividadesJuridicas.Infrastructure/Persistencia/Repositorios/UsuarioRepositorio.cs`:

```csharp
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Identidade;
using Microsoft.EntityFrameworkCore;

namespace AtividadesJuridicas.Infrastructure.Persistencia.Repositorios;

public sealed class UsuarioRepositorio(AppDbContext db) : IUsuarioRepositorio
{
    public Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        db.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<Usuario?> ObterPorEmailAsync(Email email, CancellationToken ct) =>
        db.Usuarios.FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<bool> ExisteEmailAsync(Email email, Guid? excetoUsuarioId, CancellationToken ct) =>
        db.Usuarios.AnyAsync(u => u.Email == email && u.Id != excetoUsuarioId, ct);

    public Task<int> ContarDiretoresAtivosAsync(Guid? excetoUsuarioId, CancellationToken ct) =>
        db.Usuarios
            .Where(u => u.Ativo && u.Id != excetoUsuarioId)
            .Join(db.Perfis.Where(p => p.Tipo == TipoPerfil.Diretoria), u => u.PerfilId, p => p.Id, (u, p) => u.Id)
            .CountAsync(ct);

    public async Task<IReadOnlyList<UsuarioResumo>> ListarAsync(CancellationToken ct)
    {
        // O e-mail usa conversor de valor; a leitura de .Valor acontece depois de materializar.
        var linhas = await db.Usuarios.AsNoTracking()
            .Join(db.Perfis, u => u.PerfilId, p => p.Id, (u, p) => new
            {
                u.Id, u.Nome, u.Email, PerfilId = p.Id, PerfilNome = p.Nome, p.Tipo,
                u.NumeroOab, u.Ativo, u.UltimoAcessoEm,
            })
            .OrderBy(x => x.Nome)
            .ToListAsync(ct);

        return [.. linhas.Select(x => new UsuarioResumo(
            x.Id, x.Nome, x.Email.Valor, x.PerfilId, x.PerfilNome, x.Tipo, x.NumeroOab, x.Ativo, x.UltimoAcessoEm))];
    }

    public void Adicionar(Usuario usuario) => db.Usuarios.Add(usuario);
}
```

`src/AtividadesJuridicas.Infrastructure/Persistencia/UnidadeDeTrabalho.cs`:

```csharp
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Comum;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AtividadesJuridicas.Infrastructure.Persistencia;

public sealed class UnidadeDeTrabalho(AppDbContext db, IIdentidadeAtual identidade, IRelogio relogio) : IUnidadeDeTrabalho
{
    public async Task SalvarAsync(CancellationToken ct)
    {
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

`src/AtividadesJuridicas.Infrastructure/Persistencia/SemeadorInicial.cs`:

```csharp
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Identidade;
using Microsoft.EntityFrameworkCore;

namespace AtividadesJuridicas.Infrastructure.Persistencia;

public sealed record AdminInicialOpcoes(string Nome, string Email, string Senha);

/// <summary>Aplica as migrations e, em banco vazio, cria os perfis padrão e o primeiro diretor.</summary>
public sealed class SemeadorInicial(AppDbContext db, IHasherDeSenha hasher, IRelogio relogio)
{
    public async Task ExecutarAsync(AdminInicialOpcoes? admin, CancellationToken ct = default)
    {
        await db.Database.MigrateAsync(ct);

        if (await db.Perfis.IgnoreQueryFilters().AnyAsync(ct)) return;

        if (admin is null)
            throw new InvalidOperationException(
                "Banco vazio: configure AdminInicial:Nome, AdminInicial:Email e AdminInicial:Senha para criar o primeiro diretor.");
        if (PoliticaDeSenha.Validar(admin.Senha) is { } erro)
            throw new InvalidOperationException("AdminInicial:Senha inválida. " + erro);

        var agora = relogio.AgoraUtc;
        var diretor = Perfil.Criar("Diretor", TipoPerfil.Diretoria);
        var producao = Perfil.Criar("Produção", TipoPerfil.Producao);
        var usuario = Usuario.Criar(admin.Nome, Email.Criar(admin.Email), hasher.Gerar(admin.Senha), diretor);

        diretor.CarimbarCriacao(agora, null);
        producao.CarimbarCriacao(agora, null);
        usuario.CarimbarCriacao(agora, null);

        db.AddRange(diretor, producao, usuario);
        await db.SaveChangesAsync(ct);
    }
}
```

`src/AtividadesJuridicas.Infrastructure/DependencyInjection.cs`:

```csharp
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Infrastructure.Comum;
using AtividadesJuridicas.Infrastructure.Persistencia;
using AtividadesJuridicas.Infrastructure.Persistencia.Repositorios;
using AtividadesJuridicas.Infrastructure.Seguranca;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection servicos, Func<IServiceProvider, string> obterStringDeConexao)
    {
        servicos.AddDbContext<AppDbContext>((sp, opcoes) => opcoes
            .UseNpgsql(obterStringDeConexao(sp))
            .UseSnakeCaseNamingConvention());

        servicos.AddSingleton<IRelogio, RelogioDoSistema>();
        servicos.AddSingleton<IHasherDeSenha, HasherDeSenhaPbkdf2>();
        servicos.AddScoped<IUnidadeDeTrabalho, UnidadeDeTrabalho>();
        servicos.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
        servicos.AddScoped<IPerfilRepositorio, PerfilRepositorio>();
        servicos.AddScoped<SemeadorInicial>();

        return servicos;
    }
}
```

- [ ] **Step 6: Gerar a migration inicial**

Run:

```bash
dotnet ef migrations add Inicial --project src/AtividadesJuridicas.Infrastructure --startup-project src/AtividadesJuridicas.Infrastructure --output-dir Persistencia/Migrations
```

Expected: três arquivos em `src/AtividadesJuridicas.Infrastructure/Persistencia/Migrations/`. Abra o `*_Inicial.cs` e confirme: tabelas `perfis` e `usuarios`; colunas em `snake_case` (`senha_hash`, `perfil_id`, `criado_em`, `excluido_em`); índices únicos `ix_usuarios_email` e `ix_perfis_nome` com `filter: "excluido_em IS NULL"`; FK de `usuarios.perfil_id` com `ReferentialAction.Restrict`.

- [ ] **Step 7: Rodar e confirmar que passa**

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests` (com o Docker em execução)
Expected: todos aprovados.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "feat(infra): persistência com EF Core, exclusão lógica, carimbos e semeador"
```

---

### Task 6: Autenticação na Web

**Files:**
- Create em `src/AtividadesJuridicas.Web/Infra/`: `Despachante.cs`, `ValidacaoDeSessao.cs`, `UrlLocal.cs`, `FusoDoEscritorio.cs`
- Modify: `src/AtividadesJuridicas.Web/Program.cs` (substituir por completo)
- Modify: `src/AtividadesJuridicas.Web/Components/App.razor`, `Routes.razor`, `_Imports.razor`, `Layout/MainLayout.razor`, `Pages/Home.razor`
- Create: `src/AtividadesJuridicas.Web/Components/Layout/LayoutVazio.razor`, `Components/Pages/Conta/Entrar.razor`
- Modify: `src/AtividadesJuridicas.Web/appsettings.json`, `appsettings.Development.json`, `wwwroot/app.css`
- Test: `tests/AtividadesJuridicas.IntegrationTests/UrlLocalTests.cs`, `AutenticacaoWebTests.cs`

**Interfaces:**
- Consumes: `IDespachante`, `IdentidadeAtual`, `AutenticarUsuario`, `ObterMeuAcesso`, `UsuarioAutenticado`, `IUsuarioRepositorio`, `SemeadorInicial`, `AdminInicialOpcoes`, `AddApplication()`, `AddInfrastructure(Func<IServiceProvider, string>)`, `BancoFixture`.
- Produces:
  - `AtividadesJuridicas.Web.Infra.Despachante : IDespachante`.
  - `UrlLocal.Normalizar(string?)` devolve um caminho local seguro (`/` quando a entrada não é local).
  - `FusoDoEscritorio.ParaLocal(DateTime utc)` e `FusoDoEscritorio.Formatar(DateTime? utc)`.
  - Rotas: `GET/POST /conta/entrar` (anônima), `POST /conta/sair`, `GET /`.
  - `public partial class Program` para os testes.
  - Configuração lida: `ConnectionStrings:Padrao`, `AdminInicial:Nome|Email|Senha`, `Escritorio:FusoHorario`.

- [ ] **Step 1: Escrever os testes que falham**

`tests/AtividadesJuridicas.IntegrationTests/UrlLocalTests.cs`:

```csharp
using AtividadesJuridicas.Web.Infra;

namespace AtividadesJuridicas.IntegrationTests;

public class UrlLocalTests
{
    [Theory]
    [InlineData("/", "/")]
    [InlineData("/usuarios", "/usuarios")]
    [InlineData("/usuarios?pagina=2", "/usuarios?pagina=2")]
    public void Caminho_local_e_mantido(string entrada, string esperado)
    {
        UrlLocal.Normalizar(entrada).Should().Be(esperado);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("usuarios")]
    [InlineData("https://mal.example")]
    [InlineData("//mal.example")]
    [InlineData("/\\mal.example")]
    [InlineData("/conta/entrar")]
    [InlineData("/conta/entrar?ReturnUrl=%2F")]
    public void Qualquer_outra_coisa_vira_a_raiz(string? entrada)
    {
        UrlLocal.Normalizar(entrada).Should().Be("/");
    }
}
```

`tests/AtividadesJuridicas.IntegrationTests/AutenticacaoWebTests.cs`:

```csharp
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public partial class AutenticacaoWebTests(BancoFixture banco) : IAsyncLifetime
{
    private WebApplicationFactory<Program> _fabrica = null!;
    private HttpClient _cliente = null!;

    public Task InitializeAsync()
    {
        // Acrescentada por último, esta fonte vence o appsettings.Development.json.
        var configuracao = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Padrao"] = banco.NovaConexao(),
            ["AdminInicial:Nome"] = "Diana Diretora",
            ["AdminInicial:Email"] = "diana@x.com",
            ["AdminInicial:Senha"] = "senha-segura",
        };
        _fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
            b.ConfigureAppConfiguration(c => c.AddInMemoryCollection(configuracao)));
        _cliente = _fabrica.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _fabrica.DisposeAsync();

    [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"")]
    private static partial Regex TokenRegex();

    private async Task<HttpResponseMessage> EntrarAsync(string email, string senha, string consulta = "")
    {
        var pagina = await _cliente.GetStringAsync("/conta/entrar" + consulta);
        var token = TokenRegex().Match(pagina).Groups[1].Value;
        token.Should().NotBeEmpty("o formulário de login deve trazer o token antifalsificação");

        return await _cliente.PostAsync("/conta/entrar" + consulta, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["_handler"] = "entrar",
            ["__RequestVerificationToken"] = token,
            ["Entrada.Email"] = email,
            ["Entrada.Senha"] = senha,
        }));
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/usuarios")]
    [InlineData("/minha-senha")]
    [InlineData("/rota-que-nao-existe")]
    public async Task Anonimo_e_redirecionado_ao_login_em_qualquer_rota(string rota)
    {
        var resposta = await _cliente.GetAsync(rota);

        resposta.StatusCode.Should().Be(HttpStatusCode.Redirect);
        resposta.Headers.Location!.AbsolutePath.Should().Be("/conta/entrar");
    }

    [Fact]
    public async Task Pagina_de_login_e_seu_css_sao_publicos()
    {
        (await _cliente.GetAsync("/conta/entrar")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await _cliente.GetAsync("/app.css")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_correto_redireciona_e_libera_a_pagina_inicial()
    {
        var resposta = await EntrarAsync("Diana@X.com", "senha-segura");

        resposta.StatusCode.Should().Be(HttpStatusCode.Redirect);
        resposta.Headers.Location!.OriginalString.Should().EndWith("/");

        var inicio = await _cliente.GetAsync("/");
        inicio.StatusCode.Should().Be(HttpStatusCode.OK);
        (await inicio.Content.ReadAsStringAsync()).Should().Contain("Diana Diretora");
    }

    [Fact]
    public async Task Login_errado_fica_na_pagina_com_mensagem_generica()
    {
        var resposta = await EntrarAsync("diana@x.com", "senha-errada");

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        WebUtility.HtmlDecode(await resposta.Content.ReadAsStringAsync()).Should().Contain("E-mail ou senha inválidos.");
        (await _cliente.GetAsync("/")).StatusCode.Should().Be(HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task ReturnUrl_externo_e_ignorado()
    {
        var resposta = await EntrarAsync("diana@x.com", "senha-segura", "?ReturnUrl=https%3A%2F%2Fmal.example");

        resposta.StatusCode.Should().Be(HttpStatusCode.Redirect);
        resposta.Headers.Location!.OriginalString.Should().NotContain("mal.example");
    }

    [Fact]
    public async Task ReturnUrl_local_e_respeitado()
    {
        var resposta = await EntrarAsync("diana@x.com", "senha-segura", "?ReturnUrl=%2Fminha-senha");

        resposta.Headers.Location!.OriginalString.Should().EndWith("/minha-senha");
    }

    [Fact]
    public async Task Sair_encerra_a_sessao()
    {
        await EntrarAsync("diana@x.com", "senha-segura");
        var inicio = await _cliente.GetStringAsync("/");
        var token = TokenRegex().Match(inicio).Groups[1].Value;

        var saida = await _cliente.PostAsync("/conta/sair", new FormUrlEncodedContent(
            new Dictionary<string, string> { ["__RequestVerificationToken"] = token }));

        saida.StatusCode.Should().Be(HttpStatusCode.Redirect);
        (await _cliente.GetAsync("/")).StatusCode.Should().Be(HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task Sair_sem_token_antifalsificacao_e_recusado()
    {
        await EntrarAsync("diana@x.com", "senha-segura");

        var saida = await _cliente.PostAsync("/conta/sair", new FormUrlEncodedContent(new Dictionary<string, string>()));

        saida.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await _cliente.GetAsync("/")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Usuario_inativado_perde_o_acesso_na_requisicao_seguinte()
    {
        await EntrarAsync("diana@x.com", "senha-segura");
        (await _cliente.GetAsync("/")).StatusCode.Should().Be(HttpStatusCode.OK);

        // Inativa direto no banco: o caso de uso recusaria inativar o último diretor.
        await using (var escopo = _fabrica.Services.CreateAsyncScope())
        {
            var db = escopo.ServiceProvider.GetRequiredService<Infrastructure.Persistencia.AppDbContext>();
            (await db.Usuarios.SingleAsync()).Inativar();
            await db.SaveChangesAsync();
        }

        (await _cliente.GetAsync("/")).StatusCode.Should().Be(HttpStatusCode.Redirect);
    }
}
```

- [ ] **Step 2: Rodar e confirmar a falha**

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests`
Expected: erro de compilação, `AtividadesJuridicas.Web.Infra.UrlLocal` não existe e `Program` não é acessível.

- [ ] **Step 3: Implementar os serviços de infraestrutura da Web**

`src/AtividadesJuridicas.Web/Infra/UrlLocal.cs`:

```csharp
namespace AtividadesJuridicas.Web.Infra;

public static class UrlLocal
{
    /// <summary>
    /// Devolve <paramref name="url"/> apenas quando é um caminho deste site; caso contrário "/".
    /// Impede que o login seja usado para redirecionar a outro domínio.
    /// </summary>
    public static string Normalizar(string? url)
    {
        if (string.IsNullOrEmpty(url) || url[0] != '/') return "/";
        if (url.Length > 1 && (url[1] == '/' || url[1] == '\\')) return "/";
        if (url.StartsWith("/conta/", StringComparison.OrdinalIgnoreCase)) return "/";
        return url;
    }
}
```

`src/AtividadesJuridicas.Web/Infra/FusoDoEscritorio.cs`:

```csharp
using System.Globalization;

namespace AtividadesJuridicas.Web.Infra;

/// <summary>Converte as datas, gravadas em UTC, para o fuso configurado do escritório.</summary>
public sealed class FusoDoEscritorio(string idDoFuso)
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("pt-BR");
    private readonly TimeZoneInfo _fuso = TimeZoneInfo.FindSystemTimeZoneById(idDoFuso);

    public DateTime ParaLocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), _fuso);

    public string Formatar(DateTime? utc) =>
        utc is { } valor ? ParaLocal(valor).ToString("dd/MM/yyyy HH:mm", Cultura) : "—";
}
```

`src/AtividadesJuridicas.Web/Infra/Despachante.cs`:

```csharp
using System.Security.Claims;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using Microsoft.AspNetCore.Components.Authorization;

namespace AtividadesJuridicas.Web.Infra;

/// <summary>
/// Executa cada requisição em um escopo de DI próprio. O escopo de um circuito Blazor dura
/// a sessão inteira do usuário; um DbContext com essa vida acumularia entidades rastreadas
/// e falharia com operações concorrentes do mesmo componente.
/// </summary>
public sealed class Despachante(IServiceScopeFactory fabrica, AuthenticationStateProvider autenticacao) : IDespachante
{
    public async Task<TResposta> EnviarAsync<TResposta>(IRequisicao<TResposta> requisicao, CancellationToken ct = default)
    {
        var estado = await autenticacao.GetAuthenticationStateAsync();
        Guid? usuarioId = Guid.TryParse(estado.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

        await using var escopo = fabrica.CreateAsyncScope();
        escopo.ServiceProvider.GetRequiredService<IdentidadeAtual>().Definir(usuarioId);

        var contrato = typeof(IManipulador<,>).MakeGenericType(requisicao.GetType(), typeof(TResposta));
        var manipulador = escopo.ServiceProvider.GetRequiredService(contrato);
        var metodo = contrato.GetMethod("ManipularAsync")!;

        return await (Task<TResposta>)metodo.Invoke(manipulador, [requisicao, ct])!;
    }
}
```

`src/AtividadesJuridicas.Web/Infra/ValidacaoDeSessao.cs`:

```csharp
using System.Security.Claims;
using AtividadesJuridicas.Application.Identidade;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace AtividadesJuridicas.Web.Infra;

public static class ValidacaoDeSessao
{
    /// <summary>A cada requisição HTTP, derruba o cookie de quem foi inativado ou não existe mais.</summary>
    public static async Task ValidarAsync(CookieValidatePrincipalContext contexto)
    {
        var usuarios = contexto.HttpContext.RequestServices.GetRequiredService<IUsuarioRepositorio>();

        var valido =
            Guid.TryParse(contexto.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) &&
            await usuarios.ObterPorIdAsync(id, contexto.HttpContext.RequestAborted) is { Ativo: true };

        if (valido) return;

        contexto.RejectPrincipal();
        await contexto.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
```

- [ ] **Step 4: Substituir `Program.cs` e a configuração**

`src/AtividadesJuridicas.Web/Program.cs`:

```csharp
using AtividadesJuridicas.Application;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Infrastructure;
using AtividadesJuridicas.Infrastructure.Persistencia;
using AtividadesJuridicas.Web.Components;
using AtividadesJuridicas.Web.Infra;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.WebEncoders;
using System.Text.Encodings.Web;
using System.Text.Unicode;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(sp =>
    sp.GetRequiredService<IConfiguration>().GetConnectionString("Padrao")
    ?? throw new InvalidOperationException("ConnectionStrings:Padrao não configurada."));

// Sem isto o HTML sai com acentos como entidades numéricas (&#xE1;).
builder.Services.Configure<WebEncoderOptions>(opcoes =>
    opcoes.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));
builder.Services.AddScoped<IDespachante, Despachante>();
builder.Services.AddSingleton(new FusoDoEscritorio(
    builder.Configuration["Escritorio:FusoHorario"] ?? "America/Sao_Paulo"));

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opcoes =>
    {
        opcoes.Cookie.Name = "atividades.auth";
        opcoes.LoginPath = "/conta/entrar";
        opcoes.AccessDeniedPath = "/conta/entrar";
        opcoes.ExpireTimeSpan = TimeSpan.FromHours(8);
        opcoes.SlidingExpiration = true;
        opcoes.Events.OnValidatePrincipal = ValidacaoDeSessao.ValidarAsync;
    });

// Toda rota exige usuário autenticado; o acesso anônimo é exceção declarada.
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

var app = builder.Build();

await using (var escopo = app.Services.CreateAsyncScope())
{
    var secao = app.Configuration.GetSection("AdminInicial");
    var admin = secao["Email"] is { Length: > 0 } email
        ? new AdminInicialOpcoes(secao["Nome"] ?? "Administrador", email, secao["Senha"] ?? string.Empty)
        : null;
    await escopo.ServiceProvider.GetRequiredService<SemeadorInicial>().ExecutarAsync(admin);
}

if (!app.Environment.IsDevelopment())
    app.UseHsts();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets().AllowAnonymous();

app.MapPost("/conta/sair", async (HttpContext contexto, IAntiforgery antiforgery) =>
{
    if (!await antiforgery.IsRequestValidAsync(contexto))
        return Results.BadRequest();

    await contexto.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.LocalRedirect("/conta/entrar");
}).DisableAntiforgery();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();

public partial class Program;
```

`src/AtividadesJuridicas.Web/appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Escritorio": {
    "FusoHorario": "America/Sao_Paulo"
  }
}
```

`src/AtividadesJuridicas.Web/appsettings.Development.json` (credenciais apenas do banco local do `docker-compose.yml`):

```json
{
  "ConnectionStrings": {
    "Padrao": "Host=localhost;Port=5544;Database=atividades;Username=atividades;Password=atividades"
  },
  "AdminInicial": {
    "Nome": "Administrador",
    "Email": "admin@escritorio.local",
    "Senha": "trocar-no-primeiro-acesso"
  }
}
```

- [ ] **Step 5: Ajustar os componentes raiz**

Em `src/AtividadesJuridicas.Web/Components/App.razor`, mantenha tudo o que o template gerou e faça só duas mudanças: troque o valor dos dois atributos `@rendermode` (em `HeadOutlet` e em `Routes`) por `ModoDaPagina` e acrescente ao fim do arquivo:

```razor
@code {
    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

    // As páginas de /conta são renderizadas no servidor sem circuito: só assim
    // o HttpContext está disponível para gravar o cookie de autenticação.
    private IComponentRenderMode? ModoDaPagina =>
        HttpContext.Request.Path.StartsWithSegments("/conta") ? null : InteractiveServer;
}
```

`src/AtividadesJuridicas.Web/Components/_Imports.razor` — acrescente ao que o template gerou:

```razor
@using Microsoft.AspNetCore.Authorization
@using Microsoft.AspNetCore.Components.Authorization
@using AtividadesJuridicas.Application.Comum
@using AtividadesJuridicas.Application.Identidade
@using AtividadesJuridicas.Domain.Identidade
@using AtividadesJuridicas.Web.Components.Layout
@using AtividadesJuridicas.Web.Infra
```

`src/AtividadesJuridicas.Web/Components/Routes.razor`:

```razor
<Router AppAssembly="typeof(Program).Assembly">
    <Found Context="routeData">
        <AuthorizeRouteView RouteData="routeData" DefaultLayout="typeof(MainLayout)" />
        <FocusOnNavigate RouteData="routeData" Selector="h1" />
    </Found>
</Router>
```

`src/AtividadesJuridicas.Web/Components/Layout/LayoutVazio.razor`:

```razor
@inherits LayoutComponentBase

<main class="centro">
    @Body
</main>
```

`src/AtividadesJuridicas.Web/Components/Layout/MainLayout.razor`:

```razor
@inherits LayoutComponentBase
@inject IDespachante Despachante

<header class="topo">
    <a class="marca" href="/">Atividades Jurídicas</a>
    <nav>
        <NavLink href="/" Match="NavLinkMatch.All">Início</NavLink>
        @if (_ator is { EhDiretoria: true })
        {
            <NavLink href="/usuarios">Usuários</NavLink>
        }
        <NavLink href="/minha-senha">Minha senha</NavLink>
    </nav>
    <span class="quem">@_ator?.Nome</span>
    <form method="post" action="/conta/sair">
        <AntiforgeryToken />
        <button type="submit">Sair</button>
    </form>
</header>

<main class="conteudo">
    @Body
</main>

@code {
    private UsuarioAutenticado? _ator;

    protected override async Task OnInitializedAsync() =>
        _ator = await Despachante.EnviarAsync(new ObterMeuAcesso());
}
```

`src/AtividadesJuridicas.Web/Components/Pages/Home.razor`:

```razor
@page "/"
@inject IDespachante Despachante

<PageTitle>Início</PageTitle>

<h1>Início</h1>

@if (_ator is null)
{
    <p class="erro">Seu acesso foi encerrado. Entre novamente.</p>
}
else
{
    <p>Olá, <strong>@_ator.Nome</strong>. Seu perfil é de @(_ator.EhDiretoria ? "Diretoria" : "Produção").</p>
}

@code {
    private UsuarioAutenticado? _ator;

    protected override async Task OnInitializedAsync() =>
        _ator = await Despachante.EnviarAsync(new ObterMeuAcesso());
}
```

`src/AtividadesJuridicas.Web/Components/Pages/Conta/Entrar.razor`:

```razor
@page "/conta/entrar"
@layout LayoutVazio
@attribute [AllowAnonymous]
@using System.ComponentModel.DataAnnotations
@using System.Security.Claims
@using Microsoft.AspNetCore.Authentication
@using Microsoft.AspNetCore.Authentication.Cookies
@inject IDespachante Despachante
@inject NavigationManager Navegacao

<PageTitle>Entrar</PageTitle>

<h1>Atividades Jurídicas</h1>

<EditForm Model="Entrada" FormName="entrar" OnValidSubmit="EntrarAsync" method="post" class="cartao">
    <DataAnnotationsValidator />

    @if (_erro is not null)
    {
        <p class="erro" role="alert">@_erro</p>
    }

    <label>
        E-mail
        <InputText @bind-Value="Entrada.Email" type="email" autocomplete="username" />
        <ValidationMessage For="() => Entrada.Email" />
    </label>

    <label>
        Senha
        <InputText @bind-Value="Entrada.Senha" type="password" autocomplete="current-password" />
        <ValidationMessage For="() => Entrada.Senha" />
    </label>

    <button type="submit">Entrar</button>
</EditForm>

@code {
    private string? _erro;

    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

    [SupplyParameterFromForm]
    private Modelo Entrada { get; set; } = new();

    [SupplyParameterFromQuery]
    private string? ReturnUrl { get; set; }

    private async Task EntrarAsync()
    {
        var resultado = await Despachante.EnviarAsync(new AutenticarUsuario(Entrada.Email, Entrada.Senha));
        if (!resultado.Sucesso)
        {
            _erro = resultado.Mensagem;
            return;
        }

        // O cookie guarda só a identidade. Perfil e estado ativo são lidos do banco a cada requisição.
        var identidade = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, resultado.Valor!.Id.ToString()),
            new Claim(ClaimTypes.Name, resultado.Valor.Nome),
        ], CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identidade));

        Navegacao.NavigateTo(UrlLocal.Normalizar(ReturnUrl));
    }

    private sealed class Modelo
    {
        [Required(ErrorMessage = "Informe o e-mail.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe a senha.")]
        public string Senha { get; set; } = string.Empty;
    }
}
```

Substitua `src/AtividadesJuridicas.Web/wwwroot/app.css` por:

```css
:root { --borda: #d0d5dd; --texto: #1d2939; --primaria: #1f4e79; --erro: #b42318; --ok: #067647; }
* { box-sizing: border-box; }
body { margin: 0; font-family: system-ui, sans-serif; color: var(--texto); background: #f9fafb; }
h1 { font-size: 1.5rem; }
h2 { font-size: 1.15rem; margin-top: 2rem; }
.topo { display: flex; align-items: center; gap: 1.5rem; padding: .75rem 1.5rem; background: var(--primaria); color: #fff; }
.topo a { color: #fff; text-decoration: none; }
.topo a.active { text-decoration: underline; }
.topo nav { display: flex; gap: 1rem; flex: 1; }
.topo .marca { font-weight: 600; }
.conteudo { padding: 1.5rem; max-width: 70rem; margin: 0 auto; }
.centro { min-height: 100vh; display: grid; place-content: center; gap: 1rem; padding: 1rem; }
.cartao, form.formulario { display: grid; gap: .75rem; min-width: 20rem; padding: 1.25rem; background: #fff; border: 1px solid var(--borda); border-radius: .5rem; }
label { display: grid; gap: .25rem; font-size: .9rem; }
input, select { padding: .5rem; border: 1px solid var(--borda); border-radius: .375rem; font: inherit; }
button { padding: .5rem 1rem; border: 1px solid var(--primaria); border-radius: .375rem; background: var(--primaria); color: #fff; font: inherit; cursor: pointer; }
button.secundario { background: #fff; color: var(--primaria); }
table { width: 100%; border-collapse: collapse; background: #fff; }
th, td { padding: .5rem .75rem; border-bottom: 1px solid var(--borda); text-align: left; }
.acoes { display: flex; gap: .5rem; }
.erro, .validation-message { color: var(--erro); }
.ok { color: var(--ok); }
.inativo { opacity: .55; }
```

- [ ] **Step 6: Rodar e confirmar que passa**

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests`
Expected: todos aprovados. Os casos de `Anonimo_e_redirecionado_ao_login_em_qualquer_rota` para `/usuarios` e `/minha-senha` passam mesmo sem essas páginas existirem ainda, porque a política padrão vale também para rotas sem endpoint.

Se `Sair_encerra_a_sessao` falhar por não encontrar o token no HTML de `/`: o `<AntiforgeryToken />` do `MainLayout` é emitido na pré-renderização; confirme que `App.razor` não desativou a pré-renderização.

- [ ] **Step 7: Verificação manual**

```bash
docker compose up -d
dotnet run --project src/AtividadesJuridicas.Web
```

Abra o endereço exibido no console. Confirme: redireciona para `/conta/entrar`; o login com `admin@escritorio.local` / `trocar-no-primeiro-acesso` leva à página inicial com o nome e "Diretoria"; o botão Sair volta ao login.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "feat(web): login por cookie, despachante por escopo e proteção padrão de rotas"
```

---

### Task 7: Gestão de usuários e troca de senha

**Files:**
- Create: `src/AtividadesJuridicas.Web/Components/Pages/Usuarios.razor`, `src/AtividadesJuridicas.Web/Components/Pages/MinhaSenha.razor`
- Test: `tests/AtividadesJuridicas.IntegrationTests/PaginasProtegidasTests.cs`

**Interfaces:**
- Consumes: `IDespachante`, `FusoDoEscritorio`, `ObterMeuAcesso`, `ListarUsuarios`, `ListarPerfisAtivos`, `CriarUsuario`, `EditarUsuario`, `DefinirAtivoUsuario`, `RedefinirSenhaUsuario`, `AlterarMinhaSenha`, `UsuarioResumo`, `PerfilResumo`, `Resultado`.
- Produces: rotas `/usuarios` e `/minha-senha`.

- [ ] **Step 1: Escrever os testes que falham**

`tests/AtividadesJuridicas.IntegrationTests/PaginasProtegidasTests.cs`:

```csharp
using System.Net;
using System.Text.RegularExpressions;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Identidade;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public partial class PaginasProtegidasTests(BancoFixture banco) : IAsyncLifetime
{
    private WebApplicationFactory<Program> _fabrica = null!;

    public async Task InitializeAsync()
    {
        // Acrescentada por último, esta fonte vence o appsettings.Development.json.
        var configuracao = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Padrao"] = banco.NovaConexao(),
            ["AdminInicial:Nome"] = "Diana Diretora",
            ["AdminInicial:Email"] = "diana@x.com",
            ["AdminInicial:Senha"] = "senha-segura",
        };
        _fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
            b.ConfigureAppConfiguration(c => c.AddInMemoryCollection(configuracao)));

        // Cria um usuário de Produção agindo como a diretora semeada.
        await using var escopo = _fabrica.Services.CreateAsyncScope();
        var sp = escopo.ServiceProvider;
        var diana = await sp.GetRequiredService<IUsuarioRepositorio>().ObterPorEmailAsync(Email.Criar("diana@x.com"), default);
        sp.GetRequiredService<IdentidadeAtual>().Definir(diana!.Id);
        var producao = (await sp.GetRequiredService<IPerfilRepositorio>().ListarAtivosAsync(default))
            .Single(p => p.Tipo == TipoPerfil.Producao);
        var criado = await sp.GetRequiredService<IManipulador<CriarUsuario, Resultado<Guid>>>().ManipularAsync(
            new CriarUsuario("Pedro Produção", "pedro@x.com", "senha-do-pedro", producao.Id, null), default);
        criado.Sucesso.Should().BeTrue(criado.Mensagem);
    }

    public async Task DisposeAsync() => await _fabrica.DisposeAsync();

    [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"")]
    private static partial Regex TokenRegex();

    private async Task<HttpClient> ClienteAutenticadoAsync(string email, string senha)
    {
        var cliente = _fabrica.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var pagina = await cliente.GetStringAsync("/conta/entrar");
        var resposta = await cliente.PostAsync("/conta/entrar", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["_handler"] = "entrar",
            ["__RequestVerificationToken"] = TokenRegex().Match(pagina).Groups[1].Value,
            ["Entrada.Email"] = email,
            ["Entrada.Senha"] = senha,
        }));
        resposta.StatusCode.Should().Be(HttpStatusCode.Redirect);
        return cliente;
    }

    [Fact]
    public async Task Diretor_ve_a_lista_de_usuarios_sem_nenhum_hash_de_senha()
    {
        var cliente = await ClienteAutenticadoAsync("diana@x.com", "senha-segura");

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/usuarios"));

        html.Should().Contain("Diana Diretora").And.Contain("pedro@x.com").And.Contain("Novo usuário");
        html.Should().NotContain("pbkdf2-sha256");
    }

    [Fact]
    public async Task Producao_recebe_acesso_negado_e_nao_ve_dados_de_usuarios()
    {
        var cliente = await ClienteAutenticadoAsync("pedro@x.com", "senha-do-pedro");

        var html = await cliente.GetStringAsync("/usuarios");

        html.Should().Contain("Acesso negado");
        html.Should().NotContain("diana@x.com").And.NotContain("Novo usuário");
    }

    [Fact]
    public async Task Menu_de_producao_nao_mostra_o_link_de_usuarios()
    {
        var cliente = await ClienteAutenticadoAsync("pedro@x.com", "senha-do-pedro");

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/"));

        html.Should().Contain("Pedro Produção").And.NotContain("href=\"/usuarios\"");
    }

    [Fact]
    public async Task Qualquer_usuario_autenticado_abre_a_troca_de_senha()
    {
        var cliente = await ClienteAutenticadoAsync("pedro@x.com", "senha-do-pedro");

        var html = await cliente.GetStringAsync("/minha-senha");

        html.Should().Contain("Senha atual");
    }
}
```

- [ ] **Step 2: Rodar e confirmar a falha**

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --filter PaginasProtegidasTests`
Expected: FAIL. `/usuarios` e `/minha-senha` respondem sem o conteúdo esperado, pois as páginas não existem.

- [ ] **Step 3: Implementar a página de usuários**

`src/AtividadesJuridicas.Web/Components/Pages/Usuarios.razor`:

```razor
@page "/usuarios"
@using System.ComponentModel.DataAnnotations
@inject IDespachante Despachante
@inject FusoDoEscritorio Fuso

<PageTitle>Usuários</PageTitle>

<h1>Usuários</h1>

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
    @if (_mensagem is not null)
    {
        <p class="@(_falhou ? "erro" : "ok")" role="status">@_mensagem</p>
    }

    <table>
        <thead>
            <tr>
                <th>Nome</th>
                <th>E-mail</th>
                <th>Perfil</th>
                <th>OAB</th>
                <th>Último acesso</th>
                <th>Situação</th>
                <th></th>
            </tr>
        </thead>
        <tbody>
            @foreach (var usuario in _usuarios)
            {
                <tr class="@(usuario.Ativo ? null : "inativo")" @key="usuario.Id">
                    <td>@usuario.Nome</td>
                    <td>@usuario.Email</td>
                    <td>@usuario.PerfilNome</td>
                    <td>@usuario.NumeroOab</td>
                    <td>@Fuso.Formatar(usuario.UltimoAcessoEm)</td>
                    <td>@(usuario.Ativo ? "Ativo" : "Inativo")</td>
                    <td class="acoes">
                        <button type="button" class="secundario" @onclick="() => Editar(usuario)">Editar</button>
                        <button type="button" class="secundario" @onclick="() => AlternarAtivoAsync(usuario)">
                            @(usuario.Ativo ? "Inativar" : "Ativar")
                        </button>
                    </td>
                </tr>
            }
        </tbody>
    </table>

    <h2>@(_form.Id is null ? "Novo usuário" : "Editar usuário")</h2>

    <EditForm Model="_form" OnValidSubmit="SalvarAsync" FormName="usuario" class="formulario">
        <DataAnnotationsValidator />

        <label>
            Nome
            <InputText @bind-Value="_form.Nome" />
            <ValidationMessage For="() => _form.Nome" />
        </label>

        <label>
            E-mail
            <InputText @bind-Value="_form.Email" type="email" />
            <ValidationMessage For="() => _form.Email" />
        </label>

        <label>
            Perfil
            <InputSelect @bind-Value="_form.PerfilId">
                @foreach (var perfil in _perfis)
                {
                    <option value="@perfil.Id">@perfil.Nome</option>
                }
            </InputSelect>
        </label>

        <label>
            Número da OAB (opcional)
            <InputText @bind-Value="_form.NumeroOab" />
        </label>

        <label>
            @(_form.Id is null ? "Senha" : "Nova senha (deixe em branco para manter)")
            <InputText @bind-Value="_form.Senha" type="password" autocomplete="new-password" />
        </label>

        <div class="acoes">
            <button type="submit">Salvar</button>
            <button type="button" class="secundario" @onclick="Limpar">Cancelar</button>
        </div>
    </EditForm>
}

@code {
    private bool _carregado;
    private bool _autorizado;
    private bool _falhou;
    private string? _mensagem;
    private IReadOnlyList<UsuarioResumo> _usuarios = [];
    private IReadOnlyList<PerfilResumo> _perfis = [];
    private Formulario _form = new();

    protected override async Task OnInitializedAsync()
    {
        _autorizado = await Despachante.EnviarAsync(new ObterMeuAcesso()) is { EhDiretoria: true };
        if (_autorizado)
        {
            _perfis = (await Despachante.EnviarAsync(new ListarPerfisAtivos())).Valor ?? [];
            await RecarregarAsync();
            Limpar();
        }
        _carregado = true;
    }

    private async Task RecarregarAsync()
    {
        var resultado = await Despachante.EnviarAsync(new ListarUsuarios());
        _autorizado = resultado.Sucesso;
        _usuarios = resultado.Valor ?? [];
    }

    private void Limpar() =>
        _form = new Formulario { PerfilId = _perfis.Count > 0 ? _perfis[0].Id : Guid.Empty };

    private void Editar(UsuarioResumo usuario)
    {
        _mensagem = null;
        _form = new Formulario
        {
            Id = usuario.Id,
            Nome = usuario.Nome,
            Email = usuario.Email,
            PerfilId = usuario.PerfilId,
            NumeroOab = usuario.NumeroOab,
        };
    }

    private async Task SalvarAsync()
    {
        Resultado resultado;
        if (_form.Id is { } id)
        {
            resultado = await Despachante.EnviarAsync(
                new EditarUsuario(id, _form.Nome, _form.Email, _form.PerfilId, _form.NumeroOab));
            if (resultado.Sucesso && !string.IsNullOrEmpty(_form.Senha))
                resultado = await Despachante.EnviarAsync(new RedefinirSenhaUsuario(id, _form.Senha));
        }
        else
        {
            resultado = await Despachante.EnviarAsync(
                new CriarUsuario(_form.Nome, _form.Email, _form.Senha ?? string.Empty, _form.PerfilId, _form.NumeroOab));
        }

        await ConcluirAsync(resultado, "Usuário salvo.");
        if (resultado.Sucesso) Limpar();
    }

    private async Task AlternarAtivoAsync(UsuarioResumo usuario)
    {
        var resultado = await Despachante.EnviarAsync(new DefinirAtivoUsuario(usuario.Id, !usuario.Ativo));
        await ConcluirAsync(resultado, usuario.Ativo ? "Usuário inativado." : "Usuário ativado.");
    }

    private async Task ConcluirAsync(Resultado resultado, string mensagemDeSucesso)
    {
        _falhou = !resultado.Sucesso;
        _mensagem = resultado.Sucesso ? mensagemDeSucesso : resultado.Mensagem;
        await RecarregarAsync();
    }

    private sealed class Formulario
    {
        public Guid? Id { get; set; }

        [Required(ErrorMessage = "Informe o nome.")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe o e-mail.")]
        public string Email { get; set; } = string.Empty;

        public Guid PerfilId { get; set; }
        public string? NumeroOab { get; set; }
        public string? Senha { get; set; }
    }
}
```

- [ ] **Step 4: Implementar a troca de senha**

`src/AtividadesJuridicas.Web/Components/Pages/MinhaSenha.razor`:

```razor
@page "/minha-senha"
@using System.ComponentModel.DataAnnotations
@inject IDespachante Despachante

<PageTitle>Minha senha</PageTitle>

<h1>Minha senha</h1>

@if (_mensagem is not null)
{
    <p class="@(_falhou ? "erro" : "ok")" role="status">@_mensagem</p>
}

<EditForm Model="_form" OnValidSubmit="SalvarAsync" FormName="minha-senha" class="formulario">
    <DataAnnotationsValidator />

    <label>
        Senha atual
        <InputText @bind-Value="_form.SenhaAtual" type="password" autocomplete="current-password" />
        <ValidationMessage For="() => _form.SenhaAtual" />
    </label>

    <label>
        Nova senha
        <InputText @bind-Value="_form.NovaSenha" type="password" autocomplete="new-password" />
        <ValidationMessage For="() => _form.NovaSenha" />
    </label>

    <label>
        Confirme a nova senha
        <InputText @bind-Value="_form.Confirmacao" type="password" autocomplete="new-password" />
        <ValidationMessage For="() => _form.Confirmacao" />
    </label>

    <button type="submit">Alterar senha</button>
</EditForm>

@code {
    private bool _falhou;
    private string? _mensagem;
    private Formulario _form = new();

    private async Task SalvarAsync()
    {
        var resultado = await Despachante.EnviarAsync(new AlterarMinhaSenha(_form.SenhaAtual, _form.NovaSenha));

        _falhou = !resultado.Sucesso;
        _mensagem = resultado.Sucesso ? "Senha alterada." : resultado.Mensagem;
        if (resultado.Sucesso) _form = new Formulario();
    }

    private sealed class Formulario
    {
        [Required(ErrorMessage = "Informe a senha atual.")]
        public string SenhaAtual { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe a nova senha.")]
        public string NovaSenha { get; set; } = string.Empty;

        [Compare(nameof(NovaSenha), ErrorMessage = "A confirmação não confere com a nova senha.")]
        public string Confirmacao { get; set; } = string.Empty;
    }
}
```

- [ ] **Step 5: Rodar e confirmar que passa**

Run: `dotnet test`
Expected: todos os testes dos dois projetos aprovados.

- [ ] **Step 6: Verificação manual**

```bash
docker compose up -d
dotnet run --project src/AtividadesJuridicas.Web
```

Como `admin@escritorio.local`:

1. Em Usuários, crie "Pedro" com perfil Produção. Ele aparece na tabela.
2. Edite Pedro e troque o nome. A tabela reflete a mudança.
3. Tente inativar o próprio administrador. Aparece "O sistema precisa de ao menos um usuário ativo de Diretoria."
4. Tente criar outro usuário com o e-mail do Pedro em maiúsculas. Aparece "Já existe um usuário com este e-mail."
5. Em outra janela anônima, entre como Pedro. O menu não mostra Usuários; abrir `/usuarios` mostra "Acesso negado."
6. Na janela do administrador, inative Pedro. Na janela do Pedro, recarregue a página: volta ao login.
7. Em Minha senha, troque a senha do administrador, saia e entre com a nova.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "feat(web): gestão de usuários pela Diretoria e troca da própria senha"
```
