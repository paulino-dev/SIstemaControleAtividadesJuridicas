# Fase 3A — Áreas de Atuação e Clientes: Plano de Implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Cadastrar áreas de atuação e clientes (pessoa física e jurídica), com a auditoria cobrindo objetos de valor e hierarquias, e fechar as pendências de apoio de teste herdadas da fase 2.

**Architecture:** `AreaAtuacao` e `Cliente` são agregados do contexto Cadastros. `Cliente` é uma classe abstrata com duas derivadas em uma tabela só; o endereço é um objeto de valor mapeado como tipo complexo do EF. O interceptor de auditoria passa a registrar a entidade pelo tipo raiz e a achatar tipos complexos (`Endereco.Cidade`). A Diretoria mantém os cadastros; a Produção consulta.

**Tech Stack:** .NET 10, Blazor Interactive Server, EF Core 10 + Npgsql (TPH, `ComplexProperty`), PostgreSQL 17, xUnit 2.9, FluentAssertions 7.2, Testcontainers.

**Spec:** `docs/superpowers/specs/2026-10-06-modelagem-dominio-design.md` (seções 4, 5 — matriz de acesso —, 6, 10, 11 e 12).

## A fase 3 em dois planos

- **3A (este plano):** apoio de teste, áreas de atuação, clientes.
- **3B (próximo):** processos, vínculo processo–cliente e os processos na ficha do cliente.

Ficam para depois, por dependerem de fases seguintes: exclusão de clientes e de áreas pela interface (a inativação cobre); as áreas de documentos do cliente (fase 4); o registro de "acesso negado respondido como não encontrado" (fase 4, quando existir recurso oculto).

## Como ler este plano

Os **testes** de cada tarefa definem o comportamento exigido. O código de implementação é o desenho pretendido, escrito sem ter sido executado: quando um detalhe do EF Core 10, do Npgsql ou do Blazor exigir, ajuste o mínimo necessário para preservar o comportamento e registre o desvio no relatório, com a mensagem de erro que o motivou. Nunca apague um teste nem afrouxe uma asserção. Ajustar como um teste obtém suas entradas é permitido.

## Estado de partida (fases 1 e 2, na `main`)

- `Entidade` (Id Guid v7, carimbos, exclusão lógica); `Usuario`, `Perfil`, `Email`.
- `ExecutorDeRequisicoes`: todo caso de uso passa por ele; registra acesso negado e, para requisições `IConsultaAuditavel` bem-sucedidas, o acesso.
- Interceptores: `CarimbosInterceptor` (carimbos, exclusão lógica) e `AuditoriaInterceptor` (um `RegistroAuditoria` por entidade criada, alterada ou excluída, na mesma transação). `PropriedadesAuditadasTests` lista as propriedades auditadas de cada entidade.
- `IAuditoria` (`Anotar`, `RegistrarAsync`, `RegistrarAcessoAsync`); `Pagina<T>`.
- Testes: `Falsos.cs` com `Cenario`; `TesteComBanco`; `AplicacaoDeTeste(BancoFixture)`; 120 unitários e 109 de integração passando.

## Global Constraints

- TFM único `net10.0` em `Directory.Build.props`. `Nullable` e `TreatWarningsAsErrors=true`. Versões de pacote só em `Directory.Packages.props`. Nenhum pacote novo.
- `Domain` não referencia nada. `Application` não referencia EF Core, Npgsql, `Infrastructure` nem `Web`.
- Código, nomes de tabela e mensagens em português. Tabelas e colunas em `snake_case`. Datas em UTC.
- Exclusão sempre lógica. Agregados se referenciam só por Id.
- Toda alteração de `Entidade` gera registro de auditoria na mesma transação. Toda gravação que não passa pelos interceptores (`ExecuteUpdate`, `ExecuteDelete`, SQL bruto) precisa estar na lista permitida do teste de guarda.
- Toda propriedade nova de entidade é classificada em `PropriedadesAuditadasTests`: entra na lista esperada ou recebe `[NaoAuditar]`.
- Matriz de acesso: clientes — Diretoria tudo, Produção somente leitura; áreas de atuação — Diretoria tudo, Produção sem gestão (lê os nomes ao consultar cadastros).
- Autorização verificada no manipulador; a interface apenas oculta. Enquanto a consulta de acesso não termina, a página mostra "Carregando…", nunca "Acesso negado".
- Consultas são `IConsultaAuditavel`: listagem registra `Listou`; abrir um registro registra `Visualizou` com o `RecursoId`.
- Um caso de uso por arquivo, com requisição e manipulador. Novas dependências de construtor entram por último.
- `Cpf` e `Cnpj` validam dígitos verificadores. O CNPJ aceita o formato alfanumérico (letras maiúsculas nas doze primeiras posições).
- O tipo do cliente (PF ou PJ) não muda depois de criado.
- Sem MediatR, sem FluentValidation, sem biblioteca de componentes de UI.

## Review Focus

1. **CPF ou CNPJ com pontuação, espaços ou letras minúsculas** (`529.982.247-25`, `12.abc.345/01de-35`): é normalizado, e o mesmo documento com outra pontuação é recusado como duplicado. (Tarefas 3 e 4)
2. **Editar só o endereço de um cliente**: gera registro de alteração com o campo do endereço (`Endereco.Cidade`), e carimba a atualização. (Tarefa 4)
3. **Salvar pessoa jurídica informando o Id de uma pessoa física**: é recusado; o tipo não muda. (Tarefa 4)
4. **Usuário de Produção tentando criar, editar ou inativar**: acesso negado e a tentativa registrada; consultar funciona e fica registrado. (Tarefas 2 e 4)
5. **Busca com `%` ou `_`** no texto: são caracteres comuns, não curingas. (Tarefa 4)
6. **Duas execuções da suíte ao mesmo tempo**: uma não remove o contêiner de banco da outra. (Tarefa 1)

---

## Estrutura de arquivos

```
src/AtividadesJuridicas.Domain/Cadastros/
  AreaAtuacao.cs
  Cpf.cs, Cnpj.cs, Endereco.cs
  Cliente.cs (com TipoCliente e DadosDoCliente), ClientePessoaFisica.cs, ClientePessoaJuridica.cs
src/AtividadesJuridicas.Application/Cadastros/
  PortasDeAreas.cs, ListarAreasAtuacao.cs, SalvarAreaAtuacao.cs, DefinirAtivoAreaAtuacao.cs
  PortasDeClientes.cs (porta, entradas e modelos de leitura), ClienteMapeamento.cs
  ListarClientes.cs, ObterCliente.cs, SalvarClientePessoaFisica.cs, SalvarClientePessoaJuridica.cs, DefinirAtivoCliente.cs
src/AtividadesJuridicas.Infrastructure/Persistencia/
  Configuracoes/AreaAtuacaoConfiguracao.cs, ClienteConfiguracao.cs
  Repositorios/AreaAtuacaoRepositorio.cs, ClienteRepositorio.cs
  Interceptores/AuditoriaInterceptor.cs          tipo raiz + tipos complexos
src/AtividadesJuridicas.Web/Components/Pages/
  Areas.razor, Clientes.razor
tests/
  UnitTests/GravacoesPorForaTests.cs
  UnitTests/Domain/…, UnitTests/Application/…
  IntegrationTests/ConteineresOrfaosTests.cs, AreasAtuacaoTests.cs, ClientesTests.cs, CadastrosWebTests.cs
```

---

### Task 1: Apoio de teste e guarda de gravações por fora

**Files:**
- Modify: `tests/AtividadesJuridicas.IntegrationTests/BancoFixture.cs`
- Test: `tests/AtividadesJuridicas.IntegrationTests/ConteineresOrfaosTests.cs`, `tests/AtividadesJuridicas.UnitTests/GravacoesPorForaTests.cs`

**Interfaces:**
- Produces: `BancoFixture.IdsOrfaos(string saidaDoDocker, Func<int, bool> processoVivo)` (público, estático, puro): recebe linhas no formato `<id> <pid>` e devolve os ids dos contêineres cujo dono não está vivo.
- Regra: o contêiner de teste recebe, além do rótulo `atividades-juridicas.testes`, o rótulo `atividades-juridicas.testes.pid` com o PID do processo de teste. A limpeza no início só remove contêineres cujo PID não corresponde a um processo vivo. Contêiner sem PID legível é órfão.

- [ ] **Step 1: Escrever os testes que falham**

`tests/AtividadesJuridicas.IntegrationTests/ConteineresOrfaosTests.cs`:

```csharp
namespace AtividadesJuridicas.IntegrationTests;

public class ConteineresOrfaosTests
{
    private static readonly Func<int, bool> SoO100EstaVivo = pid => pid == 100;

    [Fact]
    public void Remove_so_os_conteineres_cujo_dono_morreu()
    {
        var saida = "aaa111 100\nbbb222 200\r\nccc333 300\n";

        BancoFixture.IdsOrfaos(saida, SoO100EstaVivo).Should().Equal("bbb222", "ccc333");
    }

    [Fact]
    public void Conteiner_de_uma_execucao_em_andamento_e_preservado()
    {
        BancoFixture.IdsOrfaos("aaa111 100\n", SoO100EstaVivo).Should().BeEmpty();
    }

    [Theory]
    [InlineData("aaa111")]
    [InlineData("aaa111 ")]
    [InlineData("aaa111 nao-e-numero")]
    public void Conteiner_sem_pid_legivel_e_orfao(string linha)
    {
        BancoFixture.IdsOrfaos(linha, SoO100EstaVivo).Should().Equal("aaa111");
    }

    [Theory]
    [InlineData("")]
    [InlineData("\n\n")]
    public void Saida_vazia_nao_tem_orfaos(string saida)
    {
        BancoFixture.IdsOrfaos(saida, SoO100EstaVivo).Should().BeEmpty();
    }
}
```

`tests/AtividadesJuridicas.UnitTests/GravacoesPorForaTests.cs`:

```csharp
using System.Text.RegularExpressions;

namespace AtividadesJuridicas.UnitTests;

/// <summary>
/// ExecuteUpdate, ExecuteDelete e SQL bruto não passam pelos interceptores: não carimbam, não fazem
/// exclusão lógica e não geram registro de auditoria. Cada uso precisa ser uma decisão consciente.
/// </summary>
public partial class GravacoesPorForaTests
{
    // Arquivo → motivo pelo qual a gravação por fora é aceita.
    private static readonly Dictionary<string, string> Permitidos = new()
    {
        ["UsuarioRepositorio.cs"] = "estado de login (falhas, bloqueio, último acesso): não é alteração de negócio",
    };

    [GeneratedRegex(@"\b(ExecuteUpdate|ExecuteDelete|ExecuteSql|SqlQuery|FromSql)\w*\s*[<(]")]
    private static partial Regex GravacaoPorFora();

    [Fact]
    public void So_os_arquivos_permitidos_gravam_sem_passar_pelos_interceptores()
    {
        var raiz = RaizDoRepositorio();
        var separadores = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

        var encontrados = Directory.EnumerateFiles(Path.Combine(raiz, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(caminho =>
            {
                var partes = caminho.Split(separadores);
                return !partes.Contains("obj") && !partes.Contains("bin") && !partes.Contains("Migrations");
            })
            .Where(caminho => GravacaoPorFora().IsMatch(File.ReadAllText(caminho)))
            .Select(Path.GetFileName)
            .Order()
            .ToList();

        encontrados.Should().BeEquivalentTo(Permitidos.Keys,
            "um arquivo novo grava no banco sem passar pelos interceptores de carimbos e de auditoria. " +
            "Se isso for intencional, acrescente o arquivo e o motivo a Permitidos e registre o fato por um " +
            "evento de auditoria; caso contrário, grave pelo DbContext");
    }

    private static string RaizDoRepositorio()
    {
        var pasta = new DirectoryInfo(AppContext.BaseDirectory);
        while (pasta is not null && !File.Exists(Path.Combine(pasta.FullName, "Directory.Build.props")))
            pasta = pasta.Parent;
        return pasta?.FullName ?? throw new InvalidOperationException("Raiz do repositório não encontrada.");
    }
}
```

- [ ] **Step 2: Rodar e confirmar a falha**

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --filter "FullyQualifiedName~ConteineresOrfaosTests"`
Expected: erro de compilação (`BancoFixture.IdsOrfaos` não existe).

Run: `dotnet test tests/AtividadesJuridicas.UnitTests --filter "FullyQualifiedName~GravacoesPorForaTests"`
Expected: PASS já de início (só `UsuarioRepositorio.cs` grava por fora). Para ver o teste falhar, acrescente temporariamente uma linha `// db.X.ExecuteDelete(` não comentada em outro arquivo de `src/`, rode, confirme a mensagem, e desfaça.

- [ ] **Step 3: Implementar**

Em `BancoFixture.cs`:

- Acrescente a constante e o rótulo com o PID no construtor do contêiner:

```csharp
    private const string RotuloDoPid = RotuloDoConteiner + ".pid";
```

```csharp
        .WithLabel(RotuloDoConteiner, "banco")
        // Identifica a execução dona do contêiner, para a limpeza não remover o de outra em andamento.
        .WithLabel(RotuloDoPid, Environment.ProcessId.ToString())
```

- Substitua `RemoverConteineresOrfaosAsync` e `ExecutarDockerAsync` por:

```csharp
    // Sem o Ryuk, uma execução morta deixa o contêiner rodando; a próxima remove os que não têm dono vivo.
    private static async Task RemoverConteineresOrfaosAsync()
    {
        try
        {
            var formato = "{{.ID}} {{.Label \"" + RotuloDoPid + "\"}}";
            var listar = await ExecutarDockerAsync("ps", "-a", "--filter", "label=" + RotuloDoConteiner, "--format", formato);
            var orfaos = IdsOrfaos(listar, ProcessoVivo);
            if (orfaos.Count > 0) await ExecutarDockerAsync(["rm", "-f", .. orfaos]);
        }
        catch (Exception)
        {
            // A limpeza é só um cuidado: não pode derrubar os testes.
        }
    }

    /// <summary>
    /// Recebe as linhas "<id> <pid>" do docker e devolve os ids cujo processo dono não está vivo.
    /// Contêiner sem PID legível é tratado como órfão.
    /// </summary>
    public static IReadOnlyList<string> IdsOrfaos(string saidaDoDocker, Func<int, bool> processoVivo) =>
        [.. saidaDoDocker
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(linha => linha.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Where(partes => partes.Length > 0)
            .Where(partes => partes.Length < 2 || !int.TryParse(partes[1], out var pid) || !processoVivo(pid))
            .Select(partes => partes[0])];

    private static bool ProcessoVivo(int pid)
    {
        try
        {
            using var processo = Process.GetProcessById(pid);
            return !processo.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static async Task<string> ExecutarDockerAsync(params string[] argumentos)
    {
        var inicio = new ProcessStartInfo("docker")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var argumento in argumentos) inicio.ArgumentList.Add(argumento);

        using var processo = Process.Start(inicio)!;
        using var limite = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        try
        {
            var saida = await processo.StandardOutput.ReadToEndAsync(limite.Token);
            await processo.WaitForExitAsync(limite.Token);
            return saida;
        }
        catch (OperationCanceledException)
        {
            processo.Kill(entireProcessTree: true);
            throw;
        }
    }
```

- [ ] **Step 4: Rodar e confirmar que passa**

Run: `dotnet test`
Expected: tudo aprovado, zero avisos. Depois da execução, `docker ps -a --filter label=atividades-juridicas.testes` não lista nada.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "test: limpeza de contêineres só de execuções mortas e guarda de gravações por fora da auditoria"
```

---

### Task 2: Áreas de atuação

**Files:**
- Create: `src/AtividadesJuridicas.Domain/Cadastros/AreaAtuacao.cs`
- Create: `src/AtividadesJuridicas.Application/Cadastros/PortasDeAreas.cs`, `ListarAreasAtuacao.cs`, `SalvarAreaAtuacao.cs`, `DefinirAtivoAreaAtuacao.cs`
- Create: `src/AtividadesJuridicas.Infrastructure/Persistencia/Configuracoes/AreaAtuacaoConfiguracao.cs`, `Repositorios/AreaAtuacaoRepositorio.cs`; migration `AreasDeAtuacao`
- Create: `src/AtividadesJuridicas.Web/Components/Pages/Areas.razor`
- Modify: `AppDbContext.cs`, `Infrastructure/DependencyInjection.cs`, `MainLayout.razor`, `_Imports.razor`, `tests/.../Falsos.cs`, `tests/.../PropriedadesAuditadasTests.cs`
- Test: `tests/AtividadesJuridicas.UnitTests/Domain/AreaAtuacaoTests.cs`, `tests/AtividadesJuridicas.UnitTests/Application/AreasAtuacaoTests.cs`, `tests/AtividadesJuridicas.IntegrationTests/AreasAtuacaoTests.cs`

**Interfaces:**
- Produces (Domain, namespace `AtividadesJuridicas.Domain.Cadastros`): `AreaAtuacao.Criar(string nome)`, `Renomear(string)`, `Ativar()`, `Inativar()`; `Nome`, `Ativo`; `AreaAtuacao.TamanhoMaximoNome = 100`.
- Produces (Application, namespace `AtividadesJuridicas.Application.Cadastros`):
  - `record AreaAtuacaoResumo(Guid Id, string Nome, bool Ativo)`.
  - `IAreaAtuacaoRepositorio`: `ObterPorIdAsync(Guid, CancellationToken)`, `ExisteNomeAsync(string nome, Guid? excetoId, CancellationToken)` (sem diferenciar maiúsculas), `ListarAsync(bool somenteAtivas, CancellationToken)` (ordenado por nome), `Adicionar(AreaAtuacao)`.
  - `record ListarAreasAtuacao(bool SomenteAtivas = false) : IRequisicao<Resultado<IReadOnlyList<AreaAtuacaoResumo>>>, IConsultaAuditavel` — qualquer usuário autenticado.
  - `record SalvarAreaAtuacao(Guid? Id, string Nome) : IRequisicao<Resultado<Guid>>` — Diretoria; cria quando `Id` é nulo.
  - `record DefinirAtivoAreaAtuacao(Guid Id, bool Ativo) : IRequisicao<Resultado>` — Diretoria.
- Produces (testes): `AreaAtuacaoRepositorioFalso` e `Cenario.Areas` em `Falsos.cs`.
- Rota `/areas` (Diretoria).

- [ ] **Step 1: Escrever os testes que falham**

`tests/AtividadesJuridicas.UnitTests/Domain/AreaAtuacaoTests.cs`:

```csharp
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.UnitTests.Domain;

public class AreaAtuacaoTests
{
    [Fact]
    public void Nasce_ativa_com_nome_aparado()
    {
        var area = AreaAtuacao.Criar("  Trabalhista ");

        area.Nome.Should().Be("Trabalhista");
        area.Ativo.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Recusa_nome_vazio_ao_criar_e_ao_renomear(string nome)
    {
        var criar = () => AreaAtuacao.Criar(nome);
        var renomear = () => AreaAtuacao.Criar("Cível").Renomear(nome);

        criar.Should().Throw<ExcecaoDeDominio>();
        renomear.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void Recusa_nome_maior_que_100_caracteres()
    {
        var acao = () => AreaAtuacao.Criar(new string('a', 101));

        acao.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void Renomear_e_alternar_o_estado()
    {
        var area = AreaAtuacao.Criar("Civel");

        area.Renomear(" Cível ");
        area.Inativar();

        area.Nome.Should().Be("Cível");
        area.Ativo.Should().BeFalse();

        area.Ativar();
        area.Ativo.Should().BeTrue();
    }
}
```

Em `Falsos.cs`, acrescente:

```csharp
public sealed class AreaAtuacaoRepositorioFalso : IAreaAtuacaoRepositorio
{
    public List<AreaAtuacao> Itens { get; } = [];

    public Task<AreaAtuacao?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Itens.FirstOrDefault(a => a.Id == id));

    public Task<bool> ExisteNomeAsync(string nome, Guid? excetoId, CancellationToken ct) =>
        Task.FromResult(Itens.Any(a =>
            a.Id != excetoId && string.Equals(a.Nome, nome.Trim(), StringComparison.OrdinalIgnoreCase)));

    public Task<IReadOnlyList<AreaAtuacaoResumo>> ListarAsync(bool somenteAtivas, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<AreaAtuacaoResumo>>(
            [.. Itens.Where(a => !somenteAtivas || a.Ativo).OrderBy(a => a.Nome)
                .Select(a => new AreaAtuacaoResumo(a.Id, a.Nome, a.Ativo))]);

    public void Adicionar(AreaAtuacao area) => Itens.Add(area);
}
```

com os `using` de `AtividadesJuridicas.Application.Cadastros` e `AtividadesJuridicas.Domain.Cadastros`, e em `Cenario`: `public AreaAtuacaoRepositorioFalso Areas { get; } = new();`.

`tests/AtividadesJuridicas.UnitTests/Application/AreasAtuacaoTests.cs`:

```csharp
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
```

`tests/AtividadesJuridicas.IntegrationTests/AreasAtuacaoTests.cs`:

```csharp
using System.Net;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class AreasAtuacaoTests(BancoFixture banco) : TesteComBanco(banco)
{
    [Fact]
    public async Task Criar_listar_e_auditar()
    {
        var adminId = await IdDoAdminAsync();

        var criada = await ExecutarAsync(adminId, new SalvarAreaAtuacao(null, "Trabalhista"));
        criada.Sucesso.Should().BeTrue(criada.Mensagem);

        var lista = await ExecutarAsync(adminId, new ListarAreasAtuacao());
        lista.Valor.Should().ContainSingle().Which.Should().Be(new AreaAtuacaoResumo(criada.Valor, "Trabalhista", true));

        var registro = (await AuditoriaAsync()).Single(r => r.EntidadeId == criada.Valor);
        registro.Entidade.Should().Be("AreaAtuacao");
        registro.Operacao.Should().Be(OperacaoAuditada.Criacao);
        registro.UsuarioId.Should().Be(adminId);
        registro.Dados.Should().Contain("Trabalhista");
    }

    [Fact]
    public async Task Nome_duplicado_e_barrado_tambem_pelo_banco()
    {
        var adminId = await IdDoAdminAsync();
        await ExecutarAsync(adminId, new SalvarAreaAtuacao(null, "Cível"));

        (await ExecutarAsync(adminId, new SalvarAreaAtuacao(null, "cível"))).Erro.Should().Be(TipoErro.Conflito);

        var direto = () => EmEscopoAsync(adminId, async sp =>
        {
            sp.GetRequiredService<AppDbContext>().Add(AreaAtuacao.Criar("Cível"));
            await sp.GetRequiredService<IUnidadeDeTrabalho>().SalvarAsync(default);
            return 0;
        });
        await direto.Should().ThrowAsync<ExcecaoDeConflito>();
    }

    [Fact]
    public async Task Renomear_registra_de_e_para()
    {
        var adminId = await IdDoAdminAsync();
        var criada = await ExecutarAsync(adminId, new SalvarAreaAtuacao(null, "Civel"));

        (await ExecutarAsync(adminId, new SalvarAreaAtuacao(criada.Valor, "Cível"))).Sucesso.Should().BeTrue();

        var alteracao = (await AuditoriaAsync())
            .Single(r => r.EntidadeId == criada.Valor && r.Operacao == OperacaoAuditada.Alteracao);
        alteracao.Dados.Should().Be("""{"Nome":{"de":"Civel","para":"Cível"}}""");
    }
}

[Collection(ColecaoBanco.Nome)]
public class AreasWebTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco);

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _app.DisposeAsync();

    [Fact]
    public async Task Diretor_ve_as_areas_e_o_formulario()
    {
        var adminId = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        await _app.ExecutarAsync(adminId, new SalvarAreaAtuacao(null, "Trabalhista"));
        var cliente = await _app.ClienteAutenticadoAsync();

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/areas"));

        html.Should().Contain("Áreas de atuação").And.Contain("Trabalhista").And.Contain("Nova área");
    }

    [Fact]
    public async Task Producao_recebe_acesso_negado_e_nao_ve_o_link_no_menu()
    {
        await _app.CriarPedroAsync();
        var cliente = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        WebUtility.HtmlDecode(await cliente.GetStringAsync("/areas"))
            .Should().Contain("Acesso negado").And.NotContain("Nova área");
        (await cliente.GetStringAsync("/")).Should().NotContain("href=\"/areas\"");
    }
}
```

Em `PropriedadesAuditadasTests.cs`, acrescente ao dicionário esperado: `["AreaAtuacao"] = ["Ativo", "Nome"],`.

- [ ] **Step 2: Rodar e confirmar a falha**

Run: `dotnet test tests/AtividadesJuridicas.UnitTests`
Expected: erro de compilação (`AtividadesJuridicas.Domain.Cadastros` não existe).

- [ ] **Step 3: Implementar o domínio**

`src/AtividadesJuridicas.Domain/Cadastros/AreaAtuacao.cs`:

```csharp
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Domain.Cadastros;

/// <summary>Ramo do direito (Trabalhista, Cível, Tributário…).</summary>
public sealed class AreaAtuacao : Entidade
{
    public const int TamanhoMaximoNome = 100;

    private AreaAtuacao() { }

    public string Nome { get; private set; } = null!;
    public bool Ativo { get; private set; }

    public static AreaAtuacao Criar(string nome)
    {
        var area = new AreaAtuacao { Ativo = true };
        area.Renomear(nome);
        return area;
    }

    public void Renomear(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ExcecaoDeDominio("O nome da área é obrigatório.");
        if (nome.Trim().Length > TamanhoMaximoNome)
            throw new ExcecaoDeDominio($"O nome da área deve ter até {TamanhoMaximoNome} caracteres.");
        Nome = nome.Trim();
    }

    public void Inativar() => Ativo = false;

    public void Ativar() => Ativo = true;
}
```

- [ ] **Step 4: Implementar a aplicação**

`src/AtividadesJuridicas.Application/Cadastros/PortasDeAreas.cs`:

```csharp
using AtividadesJuridicas.Domain.Cadastros;

namespace AtividadesJuridicas.Application.Cadastros;

public sealed record AreaAtuacaoResumo(Guid Id, string Nome, bool Ativo);

public interface IAreaAtuacaoRepositorio
{
    Task<AreaAtuacao?> ObterPorIdAsync(Guid id, CancellationToken ct);

    /// <summary>Compara sem diferenciar maiúsculas de minúsculas.</summary>
    Task<bool> ExisteNomeAsync(string nome, Guid? excetoId, CancellationToken ct);

    Task<IReadOnlyList<AreaAtuacaoResumo>> ListarAsync(bool somenteAtivas, CancellationToken ct);
    void Adicionar(AreaAtuacao area);
}
```

`src/AtividadesJuridicas.Application/Cadastros/ListarAreasAtuacao.cs`:

```csharp
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;

namespace AtividadesJuridicas.Application.Cadastros;

public sealed record ListarAreasAtuacao(bool SomenteAtivas = false)
    : IRequisicao<Resultado<IReadOnlyList<AreaAtuacaoResumo>>>, IConsultaAuditavel
{
    public string TipoRecurso => "AreaAtuacao";
    public AcaoDeAcesso Acao => AcaoDeAcesso.Listou;
}

public sealed class ListarAreasAtuacaoManipulador(IContextoDeAcesso contexto, IAreaAtuacaoRepositorio areas)
    : IManipulador<ListarAreasAtuacao, Resultado<IReadOnlyList<AreaAtuacaoResumo>>>
{
    public async Task<Resultado<IReadOnlyList<AreaAtuacaoResumo>>> ManipularAsync(
        ListarAreasAtuacao requisicao, CancellationToken ct)
    {
        // Qualquer usuário autenticado lê os nomes das áreas; a gestão é da Diretoria.
        if (await contexto.ObterAsync(ct) is null)
            return Resultado<IReadOnlyList<AreaAtuacaoResumo>>.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        return Resultado<IReadOnlyList<AreaAtuacaoResumo>>.Ok(await areas.ListarAsync(requisicao.SomenteAtivas, ct));
    }
}
```

`src/AtividadesJuridicas.Application/Cadastros/SalvarAreaAtuacao.cs`:

```csharp
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Application.Cadastros;

/// <summary>Cria a área quando <paramref name="Id"/> é nulo; caso contrário, renomeia.</summary>
public sealed record SalvarAreaAtuacao(Guid? Id, string Nome) : IRequisicao<Resultado<Guid>>;

public sealed class SalvarAreaAtuacaoManipulador(
    IContextoDeAcesso contexto,
    IAreaAtuacaoRepositorio areas,
    IUnidadeDeTrabalho unidade) : IManipulador<SalvarAreaAtuacao, Resultado<Guid>>
{
    private const string NomeEmUso = "Já existe uma área com este nome.";

    public async Task<Resultado<Guid>> ManipularAsync(SalvarAreaAtuacao requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Resultado<Guid>.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        AreaAtuacao? existente = null;
        if (requisicao.Id is { } id)
        {
            existente = await areas.ObterPorIdAsync(id, ct);
            if (existente is null)
                return Resultado<Guid>.Falha(TipoErro.NaoEncontrado, "Área não encontrada.");
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(requisicao.Nome) &&
                await areas.ExisteNomeAsync(requisicao.Nome, existente?.Id, ct))
                return Resultado<Guid>.Falha(TipoErro.Conflito, NomeEmUso);

            if (existente is null)
            {
                existente = AreaAtuacao.Criar(requisicao.Nome);
                areas.Adicionar(existente);
            }
            else
            {
                existente.Renomear(requisicao.Nome);
            }

            await unidade.SalvarAsync(ct);
            return Resultado<Guid>.Ok(existente.Id);
        }
        catch (ExcecaoDeDominio ex)
        {
            return Resultado<Guid>.Falha(TipoErro.Validacao, ex.Message);
        }
        catch (ExcecaoDeConflito)
        {
            return Resultado<Guid>.Falha(TipoErro.Conflito, NomeEmUso);
        }
    }
}
```

`src/AtividadesJuridicas.Application/Cadastros/DefinirAtivoAreaAtuacao.cs`:

```csharp
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;

namespace AtividadesJuridicas.Application.Cadastros;

public sealed record DefinirAtivoAreaAtuacao(Guid Id, bool Ativo) : IRequisicao<Resultado>;

public sealed class DefinirAtivoAreaAtuacaoManipulador(
    IContextoDeAcesso contexto,
    IAreaAtuacaoRepositorio areas,
    IUnidadeDeTrabalho unidade) : IManipulador<DefinirAtivoAreaAtuacao, Resultado>
{
    public async Task<Resultado> ManipularAsync(DefinirAtivoAreaAtuacao requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Resultado.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        var area = await areas.ObterPorIdAsync(requisicao.Id, ct);
        if (area is null)
            return Resultado.Falha(TipoErro.NaoEncontrado, "Área não encontrada.");

        if (area.Ativo == requisicao.Ativo) return Resultado.Ok();

        if (requisicao.Ativo) area.Ativar();
        else area.Inativar();

        await unidade.SalvarAsync(ct);
        return Resultado.Ok();
    }
}
```

- [ ] **Step 5: Implementar a persistência**

`src/AtividadesJuridicas.Infrastructure/Persistencia/Configuracoes/AreaAtuacaoConfiguracao.cs`:

```csharp
using AtividadesJuridicas.Domain.Cadastros;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AtividadesJuridicas.Infrastructure.Persistencia.Configuracoes;

public sealed class AreaAtuacaoConfiguracao : IEntityTypeConfiguration<AreaAtuacao>
{
    public void Configure(EntityTypeBuilder<AreaAtuacao> b)
    {
        b.ToTable("areas_atuacao");
        b.Property(a => a.Nome).HasMaxLength(AreaAtuacao.TamanhoMaximoNome).IsRequired();
        b.HasIndex(a => a.Nome).IsUnique().HasFilter("excluido_em IS NULL");
    }
}
```

`src/AtividadesJuridicas.Infrastructure/Persistencia/Repositorios/AreaAtuacaoRepositorio.cs`:

```csharp
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Domain.Cadastros;
using Microsoft.EntityFrameworkCore;

namespace AtividadesJuridicas.Infrastructure.Persistencia.Repositorios;

public sealed class AreaAtuacaoRepositorio(AppDbContext db) : IAreaAtuacaoRepositorio
{
    public Task<AreaAtuacao?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        db.AreasAtuacao.FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<bool> ExisteNomeAsync(string nome, Guid? excetoId, CancellationToken ct)
    {
        var procurado = nome.Trim().ToLower();
        return db.AreasAtuacao.AnyAsync(a => a.Id != excetoId && a.Nome.ToLower() == procurado, ct);
    }

    public async Task<IReadOnlyList<AreaAtuacaoResumo>> ListarAsync(bool somenteAtivas, CancellationToken ct) =>
        await db.AreasAtuacao.AsNoTracking()
            .Where(a => !somenteAtivas || a.Ativo)
            .OrderBy(a => a.Nome)
            .Select(a => new AreaAtuacaoResumo(a.Id, a.Nome, a.Ativo))
            .ToListAsync(ct);

    public void Adicionar(AreaAtuacao area) => db.AreasAtuacao.Add(area);
}
```

Em `AppDbContext.cs`: `public DbSet<AreaAtuacao> AreasAtuacao => Set<AreaAtuacao>();` com `using AtividadesJuridicas.Domain.Cadastros;`.
Em `DependencyInjection.cs` (Infrastructure): `servicos.AddScoped<IAreaAtuacaoRepositorio, AreaAtuacaoRepositorio>();` com `using AtividadesJuridicas.Application.Cadastros;`.

Gere a migration:

```bash
dotnet ef migrations add AreasDeAtuacao --project src/AtividadesJuridicas.Infrastructure --startup-project src/AtividadesJuridicas.Infrastructure --output-dir Persistencia/Migrations
```

Expected: cria só a tabela `areas_atuacao`, com índice único `ix_areas_atuacao_nome` filtrado por `excluido_em IS NULL`.

- [ ] **Step 6: Implementar a tela**

Em `_Imports.razor`: `@using AtividadesJuridicas.Application.Cadastros` e `@using AtividadesJuridicas.Domain.Cadastros`.

Em `MainLayout.razor`, dentro do bloco da Diretoria, antes do link de Usuários: `<NavLink href="/areas">Áreas</NavLink>`.

`src/AtividadesJuridicas.Web/Components/Pages/Areas.razor`:

```razor
@page "/areas"
@using System.ComponentModel.DataAnnotations
@inject IDespachante Despachante

<PageTitle>Áreas de atuação</PageTitle>

<h1>Áreas de atuação</h1>

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
            <tr><th>Nome</th><th>Situação</th><th></th></tr>
        </thead>
        <tbody>
            @foreach (var area in _areas)
            {
                <tr class="@(area.Ativo ? null : "inativo")" @key="area.Id">
                    <td>@area.Nome</td>
                    <td>@(area.Ativo ? "Ativa" : "Inativa")</td>
                    <td class="acoes">
                        <button type="button" class="secundario" @onclick="() => Editar(area)">Renomear</button>
                        <button type="button" class="secundario" @onclick="() => AlternarAtivoAsync(area)">
                            @(area.Ativo ? "Inativar" : "Ativar")
                        </button>
                    </td>
                </tr>
            }
        </tbody>
    </table>

    <h2>@(_form.Id is null ? "Nova área" : "Renomear área")</h2>

    <EditForm Model="_form" OnValidSubmit="SalvarAsync" FormName="area" class="formulario">
        <DataAnnotationsValidator />
        <label>
            Nome
            <InputText @bind-Value="_form.Nome" />
            <ValidationMessage For="() => _form.Nome" />
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
    private IReadOnlyList<AreaAtuacaoResumo> _areas = [];
    private Formulario _form = new();

    protected override async Task OnInitializedAsync()
    {
        _autorizado = await Despachante.EnviarAsync(new ObterMeuAcesso()) is { EhDiretoria: true };
        if (_autorizado) await RecarregarAsync();
        _carregado = true;
    }

    private async Task RecarregarAsync()
    {
        var resultado = await Despachante.EnviarAsync(new ListarAreasAtuacao());
        if (resultado.Erro == TipoErro.AcessoNegado) _autorizado = false;
        _areas = resultado.Valor ?? [];
    }

    private void Limpar()
    {
        _form = new Formulario();
        _mensagem = null;
    }

    private void Editar(AreaAtuacaoResumo area)
    {
        _mensagem = null;
        _form = new Formulario { Id = area.Id, Nome = area.Nome };
    }

    private async Task SalvarAsync()
    {
        var resultado = await Despachante.EnviarAsync(new SalvarAreaAtuacao(_form.Id, _form.Nome));
        await ConcluirAsync(resultado, "Área salva.");
        if (resultado.Sucesso) _form = new Formulario();
    }

    private async Task AlternarAtivoAsync(AreaAtuacaoResumo area)
    {
        var resultado = await Despachante.EnviarAsync(new DefinirAtivoAreaAtuacao(area.Id, !area.Ativo));
        await ConcluirAsync(resultado, area.Ativo ? "Área inativada." : "Área ativada.");
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
    }
}
```

- [ ] **Step 7: Rodar e confirmar que passa**

Run: `dotnet test`
Expected: tudo aprovado, zero avisos.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "feat(cadastros): áreas de atuação"
```

---

### Task 3: Cliente no domínio

**Files:**
- Create em `src/AtividadesJuridicas.Domain/Cadastros/`: `Cpf.cs`, `Cnpj.cs`, `Endereco.cs`, `Cliente.cs`, `ClientePessoaFisica.cs`, `ClientePessoaJuridica.cs`
- Test em `tests/AtividadesJuridicas.UnitTests/Domain/`: `CpfTests.cs`, `CnpjTests.cs`, `EnderecoTests.cs`, `ClienteTests.cs`

**Interfaces:**
- Produces:
  - `Cpf.Criar(string?)` / `Cpf.TentarCriar(string?, out Cpf?)`; `Valor` (11 dígitos); `Formatado` (`000.000.000-00`).
  - `Cnpj.Criar` / `Cnpj.TentarCriar`; `Valor` (14 caracteres, maiúsculos); `Formatado` (`00.000.000/0000-00`).
  - `Endereco.Criar(logradouro, numero, complemento, bairro, cidade, uf, cep)` (todos `string?`), `Endereco.Vazio`, `EstaVazio`; propriedades `Logradouro`, `Numero`, `Complemento`, `Bairro`, `Cidade`, `Uf` (2 letras maiúsculas), `Cep` (8 dígitos).
  - `enum TipoCliente { PessoaFisica = 1, PessoaJuridica = 2 }`.
  - `record DadosDoCliente(string Nome, Email? Email, string? Telefone, Endereco Endereco, string? Observacoes)`.
  - `abstract class Cliente : Entidade` com `Tipo`, `Nome`, `Email`, `Telefone`, `Endereco`, `Observacoes`, `Ativo`, `Ativar()`, `Inativar()`.
  - `ClientePessoaFisica.Criar(DadosDoCliente, Cpf, string? rg, DateOnly? dataNascimento)` e `Alterar(...)` com os mesmos parâmetros; `Cpf`, `Rg`, `DataNascimento`, `NomeCompleto`.
  - `ClientePessoaJuridica.Criar(DadosDoCliente, Cnpj, string? nomeFantasia, string? nomeResponsavel, string? inscricaoEstadual, string? inscricaoMunicipal)` e `Alterar(...)`; `Cnpj`, `NomeFantasia`, `NomeResponsavel`, `InscricaoEstadual`, `InscricaoMunicipal`, `RazaoSocial`.
- O nome de exibição fica em `Cliente.Nome`: é o nome completo da pessoa física e a razão social da pessoa jurídica.

- [ ] **Step 1: Escrever os testes que falham**

`tests/AtividadesJuridicas.UnitTests/Domain/CpfTests.cs`:

```csharp
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.UnitTests.Domain;

public class CpfTests
{
    [Theory]
    [InlineData("52998224725")]
    [InlineData("529.982.247-25")]
    [InlineData(" 529 982 247 25 ")]
    public void Aceita_e_normaliza_para_onze_digitos(string texto)
    {
        var cpf = Cpf.Criar(texto);

        cpf.Valor.Should().Be("52998224725");
        cpf.Formatado.Should().Be("529.982.247-25");
    }

    [Fact]
    public void Mesmo_cpf_com_outra_pontuacao_e_igual()
    {
        Cpf.Criar("529.982.247-25").Should().Be(Cpf.Criar("52998224725"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("5299822472")]
    [InlineData("529982247250")]
    [InlineData("52998224724")]
    [InlineData("11111111111")]
    [InlineData("00000000000")]
    [InlineData("529.982.247-2a")]
    [InlineData("529/982/247-25")]
    public void Recusa_cpf_invalido(string? texto)
    {
        Cpf.TentarCriar(texto, out var cpf).Should().BeFalse();
        cpf.Should().BeNull();

        var acao = () => Cpf.Criar(texto);
        acao.Should().Throw<ExcecaoDeDominio>();
    }

    [Theory]
    [InlineData("11144477735")]
    [InlineData("12345678909")]
    public void Aceita_outros_cpfs_validos(string texto)
    {
        Cpf.TentarCriar(texto, out _).Should().BeTrue();
    }
}
```

`tests/AtividadesJuridicas.UnitTests/Domain/CnpjTests.cs`:

```csharp
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.UnitTests.Domain;

public class CnpjTests
{
    [Theory]
    [InlineData("11222333000181", "11222333000181", "11.222.333/0001-81")]
    [InlineData("11.222.333/0001-81", "11222333000181", "11.222.333/0001-81")]
    [InlineData("11.444.777/0001-61", "11444777000161", "11.444.777/0001-61")]
    public void Aceita_cnpj_numerico(string texto, string valor, string formatado)
    {
        var cnpj = Cnpj.Criar(texto);

        cnpj.Valor.Should().Be(valor);
        cnpj.Formatado.Should().Be(formatado);
    }

    [Theory]
    [InlineData("12ABC34501DE35")]
    [InlineData("12.ABC.345/01DE-35")]
    [InlineData("12.abc.345/01de-35")]
    public void Aceita_cnpj_alfanumerico_e_normaliza_para_maiusculas(string texto)
    {
        var cnpj = Cnpj.Criar(texto);

        cnpj.Valor.Should().Be("12ABC34501DE35");
        cnpj.Formatado.Should().Be("12.ABC.345/01DE-35");
    }

    [Fact]
    public void Mesmo_cnpj_com_outra_pontuacao_ou_caixa_e_igual()
    {
        Cnpj.Criar("12.abc.345/01de-35").Should().Be(Cnpj.Criar("12ABC34501DE35"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1122233300018")]
    [InlineData("112223330001811")]
    [InlineData("11222333000180")]
    [InlineData("00000000000000")]
    [InlineData("12ABC34501DE36")]
    [InlineData("12ABC34501DEA5")]
    [InlineData("12ÁBC34501DE35")]
    [InlineData("11.222.333-0001/81!")]
    public void Recusa_cnpj_invalido(string? texto)
    {
        Cnpj.TentarCriar(texto, out var cnpj).Should().BeFalse();
        cnpj.Should().BeNull();

        var acao = () => Cnpj.Criar(texto);
        acao.Should().Throw<ExcecaoDeDominio>();
    }
}
```

`tests/AtividadesJuridicas.UnitTests/Domain/EnderecoTests.cs`:

```csharp
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.UnitTests.Domain;

public class EnderecoTests
{
    [Fact]
    public void Apara_textos_e_normaliza_uf_e_cep()
    {
        var endereco = Endereco.Criar(" Rua A ", " 10 ", "  ", "Centro", "Maceió", "al", "57.020-000");

        endereco.Logradouro.Should().Be("Rua A");
        endereco.Numero.Should().Be("10");
        endereco.Complemento.Should().BeNull();
        endereco.Uf.Should().Be("AL");
        endereco.Cep.Should().Be("57020000");
        endereco.EstaVazio.Should().BeFalse();
    }

    [Fact]
    public void Endereco_sem_nenhum_campo_e_vazio_e_igual_ao_vazio()
    {
        var endereco = Endereco.Criar(null, " ", null, null, "", null, null);

        endereco.EstaVazio.Should().BeTrue();
        endereco.Should().Be(Endereco.Vazio);
    }

    [Fact]
    public void Enderecos_com_os_mesmos_valores_sao_iguais()
    {
        Endereco.Criar("Rua A", "10", null, null, "Maceió", "AL", "57020-000")
            .Should().Be(Endereco.Criar("Rua A", "10", null, null, "Maceió", "al", "57020000"));
    }

    [Theory]
    [InlineData("A")]
    [InlineData("ALA")]
    [InlineData("A1")]
    public void Recusa_uf_que_nao_tenha_duas_letras(string uf)
    {
        var acao = () => Endereco.Criar(null, null, null, null, null, uf, null);

        acao.Should().Throw<ExcecaoDeDominio>();
    }

    [Theory]
    [InlineData("5702000")]
    [InlineData("570200000")]
    [InlineData("57020-00a")]
    public void Recusa_cep_que_nao_tenha_oito_digitos(string cep)
    {
        var acao = () => Endereco.Criar(null, null, null, null, null, null, cep);

        acao.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void Recusa_logradouro_maior_que_200_caracteres()
    {
        var acao = () => Endereco.Criar(new string('a', 201), null, null, null, null, null, null);

        acao.Should().Throw<ExcecaoDeDominio>();
    }
}
```

`tests/AtividadesJuridicas.UnitTests/Domain/ClienteTests.cs`:

```csharp
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Comum;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.UnitTests.Domain;

public class ClienteTests
{
    private static DadosDoCliente Dados(string nome = "Maria Silva", string? email = "maria@x.com") => new(
        nome, email is null ? null : Email.Criar(email), " (82) 99999-0000 ",
        Endereco.Criar("Rua A", "10", null, "Centro", "Maceió", "AL", "57020-000"), " Cliente antigo ");

    [Fact]
    public void Pessoa_fisica_nasce_ativa_com_o_tipo_fixado()
    {
        var cliente = ClientePessoaFisica.Criar(Dados(), Cpf.Criar("529.982.247-25"), " 1234567 ", new DateOnly(1980, 5, 17));

        cliente.Tipo.Should().Be(TipoCliente.PessoaFisica);
        cliente.Nome.Should().Be("Maria Silva");
        cliente.NomeCompleto.Should().Be("Maria Silva");
        cliente.Email!.Valor.Should().Be("maria@x.com");
        cliente.Telefone.Should().Be("(82) 99999-0000");
        cliente.Endereco.Cidade.Should().Be("Maceió");
        cliente.Observacoes.Should().Be("Cliente antigo");
        cliente.Cpf.Valor.Should().Be("52998224725");
        cliente.Rg.Should().Be("1234567");
        cliente.DataNascimento.Should().Be(new DateOnly(1980, 5, 17));
        cliente.Ativo.Should().BeTrue();
    }

    [Fact]
    public void Pessoa_juridica_nasce_ativa_com_o_tipo_fixado()
    {
        var cliente = ClientePessoaJuridica.Criar(
            Dados("Acme Ltda", null), Cnpj.Criar("11.222.333/0001-81"), " Acme ", " João ", " 123 ", "  ");

        cliente.Tipo.Should().Be(TipoCliente.PessoaJuridica);
        cliente.Nome.Should().Be("Acme Ltda");
        cliente.RazaoSocial.Should().Be("Acme Ltda");
        cliente.Email.Should().BeNull();
        cliente.Cnpj.Valor.Should().Be("11222333000181");
        cliente.NomeFantasia.Should().Be("Acme");
        cliente.NomeResponsavel.Should().Be("João");
        cliente.InscricaoEstadual.Should().Be("123");
        cliente.InscricaoMunicipal.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Recusa_nome_vazio(string nome)
    {
        var acao = () => ClientePessoaFisica.Criar(Dados(nome), Cpf.Criar("52998224725"), null, null);

        acao.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void Recusa_textos_acima_do_limite()
    {
        var nomeLongo = () => ClientePessoaFisica.Criar(Dados(new string('a', 201)), Cpf.Criar("52998224725"), null, null);
        var rgLongo = () => ClientePessoaFisica.Criar(Dados(), Cpf.Criar("52998224725"), new string('1', 21), null);
        var fantasiaLonga = () => ClientePessoaJuridica.Criar(
            Dados("Acme"), Cnpj.Criar("11222333000181"), new string('a', 201), null, null, null);

        nomeLongo.Should().Throw<ExcecaoDeDominio>();
        rgLongo.Should().Throw<ExcecaoDeDominio>();
        fantasiaLonga.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void Alterar_substitui_os_dados_sem_mexer_no_estado_ativo()
    {
        var cliente = ClientePessoaFisica.Criar(Dados(), Cpf.Criar("52998224725"), "1", null);
        cliente.Inativar();

        cliente.Alterar(Dados("Maria S. Santos", null) with { Endereco = Endereco.Vazio },
            Cpf.Criar("111.444.777-35"), null, new DateOnly(1981, 1, 1));

        cliente.Nome.Should().Be("Maria S. Santos");
        cliente.Email.Should().BeNull();
        cliente.Endereco.EstaVazio.Should().BeTrue();
        cliente.Cpf.Valor.Should().Be("11144477735");
        cliente.Rg.Should().BeNull();
        cliente.Ativo.Should().BeFalse();
        cliente.Tipo.Should().Be(TipoCliente.PessoaFisica);
    }

    [Fact]
    public void Alteracao_invalida_nao_deixa_o_cliente_pela_metade()
    {
        var cliente = ClientePessoaFisica.Criar(Dados(), Cpf.Criar("52998224725"), "1", null);

        var acao = () => cliente.Alterar(Dados("Outro Nome"), Cpf.Criar("11144477735"), new string('1', 21), null);

        acao.Should().Throw<ExcecaoDeDominio>();
        cliente.Nome.Should().Be("Maria Silva");
        cliente.Cpf.Valor.Should().Be("52998224725");
    }
}
```

- [ ] **Step 2: Rodar e confirmar a falha**

Run: `dotnet test tests/AtividadesJuridicas.UnitTests`
Expected: erro de compilação (`Cpf`, `Cnpj`, `Endereco`, `Cliente` não existem).

- [ ] **Step 3: Implementar os objetos de valor**

`src/AtividadesJuridicas.Domain/Cadastros/Cpf.cs`:

```csharp
using System.Diagnostics.CodeAnalysis;
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Domain.Cadastros;

public sealed record Cpf
{
    /// <summary>Os onze dígitos, sem pontuação.</summary>
    public string Valor { get; }

    private Cpf(string valor) => Valor = valor;

    public string Formatado => $"{Valor[..3]}.{Valor[3..6]}.{Valor[6..9]}-{Valor[9..]}";

    public static Cpf Criar(string? texto) =>
        TentarCriar(texto, out var cpf) ? cpf : throw new ExcecaoDeDominio("CPF inválido.");

    public static bool TentarCriar(string? texto, [NotNullWhen(true)] out Cpf? cpf)
    {
        cpf = null;
        if (texto is null) return false;
        if (texto.Any(c => !char.IsAsciiDigit(c) && c is not ('.' or '-' or ' '))) return false;

        var digitos = new string([.. texto.Where(char.IsAsciiDigit)]);
        if (digitos.Length != 11 || digitos.Distinct().Count() == 1) return false;
        if (Digito(digitos, 9) != digitos[9] - '0' || Digito(digitos, 10) != digitos[10] - '0') return false;

        cpf = new Cpf(digitos);
        return true;
    }

    private static int Digito(string digitos, int quantidade)
    {
        var soma = 0;
        for (var i = 0; i < quantidade; i++) soma += (digitos[i] - '0') * (quantidade + 1 - i);
        var resto = soma * 10 % 11;
        return resto == 10 ? 0 : resto;
    }

    public override string ToString() => Formatado;
}
```

`src/AtividadesJuridicas.Domain/Cadastros/Cnpj.cs`:

```csharp
using System.Diagnostics.CodeAnalysis;
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Domain.Cadastros;

/// <summary>
/// CNPJ numérico ou alfanumérico: as doze primeiras posições aceitam dígitos e letras maiúsculas;
/// os dois dígitos verificadores são sempre numéricos.
/// </summary>
public sealed record Cnpj
{
    /// <summary>Os catorze caracteres, sem pontuação, em maiúsculas.</summary>
    public string Valor { get; }

    private Cnpj(string valor) => Valor = valor;

    public string Formatado => $"{Valor[..2]}.{Valor[2..5]}.{Valor[5..8]}/{Valor[8..12]}-{Valor[12..]}";

    public static Cnpj Criar(string? texto) =>
        TentarCriar(texto, out var cnpj) ? cnpj : throw new ExcecaoDeDominio("CNPJ inválido.");

    public static bool TentarCriar(string? texto, [NotNullWhen(true)] out Cnpj? cnpj)
    {
        cnpj = null;
        if (texto is null) return false;

        var limpo = new string([.. texto.Where(c => c is not ('.' or '/' or '-' or ' '))]).ToUpperInvariant();
        if (limpo.Length != 14) return false;
        if (!limpo[..12].All(c => char.IsAsciiDigit(c) || c is >= 'A' and <= 'Z')) return false;
        if (!char.IsAsciiDigit(limpo[12]) || !char.IsAsciiDigit(limpo[13])) return false;
        if (limpo.Distinct().Count() == 1) return false;

        if (Digito(limpo.AsSpan(0, 12)) != limpo[12] - '0' || Digito(limpo.AsSpan(0, 13)) != limpo[13] - '0')
            return false;

        cnpj = new Cnpj(limpo);
        return true;
    }

    // Módulo 11 com pesos de 2 a 9 a partir da direita; cada caractere vale o seu código ASCII menos 48.
    private static int Digito(ReadOnlySpan<char> caracteres)
    {
        var soma = 0;
        var peso = 2;
        for (var i = caracteres.Length - 1; i >= 0; i--)
        {
            soma += (caracteres[i] - '0') * peso;
            peso = peso == 9 ? 2 : peso + 1;
        }

        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }

    public override string ToString() => Formatado;
}
```

`src/AtividadesJuridicas.Domain/Cadastros/Endereco.cs`:

```csharp
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Domain.Cadastros;

/// <summary>Objeto de valor: todos os campos são opcionais e o endereço pode estar vazio.</summary>
public sealed record Endereco
{
    private Endereco() { }

    public string? Logradouro { get; private set; }
    public string? Numero { get; private set; }
    public string? Complemento { get; private set; }
    public string? Bairro { get; private set; }
    public string? Cidade { get; private set; }

    /// <summary>Duas letras maiúsculas.</summary>
    public string? Uf { get; private set; }

    /// <summary>Oito dígitos, sem pontuação.</summary>
    public string? Cep { get; private set; }

    public static Endereco Vazio => new();

    public bool EstaVazio =>
        Logradouro is null && Numero is null && Complemento is null && Bairro is null &&
        Cidade is null && Uf is null && Cep is null;

    public static Endereco Criar(
        string? logradouro, string? numero, string? complemento, string? bairro, string? cidade, string? uf, string? cep) =>
        new()
        {
            Logradouro = Texto(logradouro, 200, "logradouro"),
            Numero = Texto(numero, 20, "número"),
            Complemento = Texto(complemento, 100, "complemento"),
            Bairro = Texto(bairro, 100, "bairro"),
            Cidade = Texto(cidade, 100, "cidade"),
            Uf = NormalizarUf(uf),
            Cep = NormalizarCep(cep),
        };

    private static string? Texto(string? valor, int maximo, string campo)
    {
        if (string.IsNullOrWhiteSpace(valor)) return null;
        var aparado = valor.Trim();
        return aparado.Length <= maximo
            ? aparado
            : throw new ExcecaoDeDominio($"O campo {campo} do endereço deve ter até {maximo} caracteres.");
    }

    private static string? NormalizarUf(string? uf)
    {
        if (string.IsNullOrWhiteSpace(uf)) return null;
        var aparada = uf.Trim().ToUpperInvariant();
        return aparada.Length == 2 && aparada.All(c => c is >= 'A' and <= 'Z')
            ? aparada
            : throw new ExcecaoDeDominio("A UF deve ter duas letras.");
    }

    private static string? NormalizarCep(string? cep)
    {
        if (string.IsNullOrWhiteSpace(cep)) return null;
        var limpo = new string([.. cep.Where(c => c is not ('.' or '-' or ' '))]);
        return limpo.Length == 8 && limpo.All(char.IsAsciiDigit)
            ? limpo
            : throw new ExcecaoDeDominio("O CEP deve ter oito dígitos.");
    }
}
```

- [ ] **Step 4: Implementar o cliente**

`src/AtividadesJuridicas.Domain/Cadastros/Cliente.cs`:

```csharp
using AtividadesJuridicas.Domain.Comum;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.Domain.Cadastros;

public enum TipoCliente
{
    PessoaFisica = 1,
    PessoaJuridica = 2,
}

/// <summary>Dados comuns aos dois tipos de cliente.</summary>
public sealed record DadosDoCliente(string Nome, Email? Email, string? Telefone, Endereco Endereco, string? Observacoes);

public abstract class Cliente : Entidade
{
    public const int TamanhoMaximoNome = 200;
    public const int TamanhoMaximoTelefone = 30;
    public const int TamanhoMaximoObservacoes = 2000;

    protected Cliente(TipoCliente tipo)
    {
        Tipo = tipo;
        Ativo = true;
    }

    /// <summary>Fixado na criação; um cliente não muda de tipo.</summary>
    public TipoCliente Tipo { get; private set; }

    /// <summary>Nome completo da pessoa física ou razão social da pessoa jurídica.</summary>
    public string Nome { get; private set; } = null!;

    public Email? Email { get; private set; }
    public string? Telefone { get; private set; }
    public Endereco Endereco { get; private set; } = Endereco.Vazio;
    public string? Observacoes { get; private set; }
    public bool Ativo { get; private set; }

    public void Inativar() => Ativo = false;

    public void Ativar() => Ativo = true;

    /// <summary>Valida tudo antes de alterar qualquer coisa, para não deixar o cliente pela metade.</summary>
    protected static void Validar(DadosDoCliente dados)
    {
        ArgumentNullException.ThrowIfNull(dados);
        ArgumentNullException.ThrowIfNull(dados.Endereco);
        if (string.IsNullOrWhiteSpace(dados.Nome))
            throw new ExcecaoDeDominio("O nome do cliente é obrigatório.");
        Limite(dados.Nome, TamanhoMaximoNome, "nome");
        Limite(dados.Telefone, TamanhoMaximoTelefone, "telefone");
        Limite(dados.Observacoes, TamanhoMaximoObservacoes, "observações");
    }

    protected void Aplicar(DadosDoCliente dados)
    {
        Nome = dados.Nome.Trim();
        Email = dados.Email;
        Telefone = Opcional(dados.Telefone);
        Endereco = dados.Endereco;
        Observacoes = Opcional(dados.Observacoes);
    }

    protected static void Limite(string? valor, int maximo, string campo)
    {
        if (valor is not null && valor.Trim().Length > maximo)
            throw new ExcecaoDeDominio($"O campo {campo} deve ter até {maximo} caracteres.");
    }

    protected static string? Opcional(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
```

`src/AtividadesJuridicas.Domain/Cadastros/ClientePessoaFisica.cs`:

```csharp
namespace AtividadesJuridicas.Domain.Cadastros;

public sealed class ClientePessoaFisica : Cliente
{
    public const int TamanhoMaximoRg = 20;

    private ClientePessoaFisica() : base(TipoCliente.PessoaFisica) { }

    public Cpf Cpf { get; private set; } = null!;
    public string? Rg { get; private set; }
    public DateOnly? DataNascimento { get; private set; }

    public string NomeCompleto => Nome;

    public static ClientePessoaFisica Criar(DadosDoCliente dados, Cpf cpf, string? rg, DateOnly? dataNascimento)
    {
        var cliente = new ClientePessoaFisica();
        cliente.Alterar(dados, cpf, rg, dataNascimento);
        return cliente;
    }

    public void Alterar(DadosDoCliente dados, Cpf cpf, string? rg, DateOnly? dataNascimento)
    {
        ArgumentNullException.ThrowIfNull(cpf);
        Validar(dados);
        Limite(rg, TamanhoMaximoRg, "RG");

        Aplicar(dados);
        Cpf = cpf;
        Rg = Opcional(rg);
        DataNascimento = dataNascimento;
    }
}
```

`src/AtividadesJuridicas.Domain/Cadastros/ClientePessoaJuridica.cs`:

```csharp
namespace AtividadesJuridicas.Domain.Cadastros;

public sealed class ClientePessoaJuridica : Cliente
{
    public const int TamanhoMaximoInscricao = 30;

    private ClientePessoaJuridica() : base(TipoCliente.PessoaJuridica) { }

    public Cnpj Cnpj { get; private set; } = null!;
    public string? NomeFantasia { get; private set; }
    public string? NomeResponsavel { get; private set; }
    public string? InscricaoEstadual { get; private set; }
    public string? InscricaoMunicipal { get; private set; }

    public string RazaoSocial => Nome;

    public static ClientePessoaJuridica Criar(
        DadosDoCliente dados, Cnpj cnpj, string? nomeFantasia, string? nomeResponsavel,
        string? inscricaoEstadual, string? inscricaoMunicipal)
    {
        var cliente = new ClientePessoaJuridica();
        cliente.Alterar(dados, cnpj, nomeFantasia, nomeResponsavel, inscricaoEstadual, inscricaoMunicipal);
        return cliente;
    }

    public void Alterar(
        DadosDoCliente dados, Cnpj cnpj, string? nomeFantasia, string? nomeResponsavel,
        string? inscricaoEstadual, string? inscricaoMunicipal)
    {
        ArgumentNullException.ThrowIfNull(cnpj);
        Validar(dados);
        Limite(nomeFantasia, TamanhoMaximoNome, "nome fantasia");
        Limite(nomeResponsavel, TamanhoMaximoNome, "nome do responsável");
        Limite(inscricaoEstadual, TamanhoMaximoInscricao, "inscrição estadual");
        Limite(inscricaoMunicipal, TamanhoMaximoInscricao, "inscrição municipal");

        Aplicar(dados);
        Cnpj = cnpj;
        NomeFantasia = Opcional(nomeFantasia);
        NomeResponsavel = Opcional(nomeResponsavel);
        InscricaoEstadual = Opcional(inscricaoEstadual);
        InscricaoMunicipal = Opcional(inscricaoMunicipal);
    }
}
```

- [ ] **Step 5: Rodar e confirmar que passa**

Run: `dotnet test tests/AtividadesJuridicas.UnitTests`
Expected: tudo aprovado, incluindo `Domain_nao_referencia_nada_alem_da_biblioteca_base`.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "feat(cadastros): Cpf, Cnpj, Endereco e Cliente (PF e PJ) no domínio"
```

---

### Task 4: Clientes — casos de uso, persistência e auditoria de agregados

A tarefa tem duas partes, feitas em sequência e entregues juntas. A Parte A (casos de uso, testada com duplos) deixa `IClienteRepositorio` sem implementação registrada, e o host Web valida a injeção de dependências ao subir; por isso a suíte completa só volta a passar ao fim da Parte B.

#### Parte A — Casos de uso

**Files:**
- Create em `src/AtividadesJuridicas.Application/Cadastros/`: `PortasDeClientes.cs`, `ClienteMapeamento.cs`, `ListarClientes.cs`, `ObterCliente.cs`, `SalvarClientePessoaFisica.cs`, `SalvarClientePessoaJuridica.cs`, `DefinirAtivoCliente.cs`
- Modify: `tests/AtividadesJuridicas.UnitTests/Application/Falsos.cs`
- Test: `tests/AtividadesJuridicas.UnitTests/Application/ClientesTests.cs`

**Interfaces:**
- Consumes: `Cliente`, `ClientePessoaFisica`, `ClientePessoaJuridica`, `Cpf`, `Cnpj`, `Endereco`, `DadosDoCliente`, `Email`, `Pagina<T>`, `IConsultaAuditavel`, `IContextoDeAcesso`, `IUnidadeDeTrabalho`.
- Produces (namespace `AtividadesJuridicas.Application.Cadastros`):
  - `IClienteRepositorio`: `ObterPorIdAsync(Guid, CancellationToken)`; `ExisteCpfAsync(Cpf, Guid? excetoId, CancellationToken)`; `ExisteCnpjAsync(Cnpj, Guid? excetoId, CancellationToken)`; `ListarAsync(string? busca, bool incluirInativos, int pagina, int tamanho, CancellationToken)` devolvendo `Pagina<Cliente>` ordenada por nome; `Adicionar(Cliente)`.
  - Regra da busca: texto que é um CPF ou CNPJ válido procura pelo documento exato; qualquer outro texto procura no nome, sem diferenciar maiúsculas, tratando `%` e `_` como caracteres comuns.
  - Entradas: `record EnderecoEntrada(string? Logradouro = null, string? Numero = null, string? Complemento = null, string? Bairro = null, string? Cidade = null, string? Uf = null, string? Cep = null)`; `record DadosDoClienteEntrada(string Nome, string? Email, string? Telefone, EnderecoEntrada Endereco, string? Observacoes)`.
  - Leitura: `record ClienteResumo(Guid Id, TipoCliente Tipo, string Nome, string Documento, string? Email, string? Telefone, string? Cidade, bool Ativo)` (`Documento` formatado); `record ClienteDetalhe(...)` com todos os campos (ver Step 3).
  - `record ListarClientes(string? Busca = null, bool IncluirInativos = false, int Pagina = 1) : IRequisicao<Resultado<Pagina<ClienteResumo>>>, IConsultaAuditavel` — qualquer autenticado; página de 20 (`ClientesOpcoes.TamanhoDaPagina`).
  - `record ObterCliente(Guid Id) : IRequisicao<Resultado<ClienteDetalhe>>, IConsultaAuditavel` — qualquer autenticado; `Visualizou` com `RecursoId`.
  - `record SalvarClientePessoaFisica(Guid? Id, DadosDoClienteEntrada Dados, string Cpf, string? Rg, DateOnly? DataNascimento) : IRequisicao<Resultado<Guid>>` — Diretoria.
  - `record SalvarClientePessoaJuridica(Guid? Id, DadosDoClienteEntrada Dados, string Cnpj, string? NomeFantasia, string? NomeResponsavel, string? InscricaoEstadual, string? InscricaoMunicipal) : IRequisicao<Resultado<Guid>>` — Diretoria.
  - `record DefinirAtivoCliente(Guid Id, bool Ativo) : IRequisicao<Resultado>` — Diretoria.
- Produces (testes): `ClienteRepositorioFalso` e `Cenario.Clientes`.

- [ ] **Step 1: Atualizar os duplos e escrever os testes que falham**

Em `Falsos.cs`, acrescente:

```csharp
public sealed class ClienteRepositorioFalso : IClienteRepositorio
{
    public List<Cliente> Itens { get; } = [];

    public Task<Cliente?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Itens.FirstOrDefault(c => c.Id == id));

    public Task<bool> ExisteCpfAsync(Cpf cpf, Guid? excetoId, CancellationToken ct) =>
        Task.FromResult(Itens.OfType<ClientePessoaFisica>().Any(c => c.Cpf == cpf && c.Id != excetoId));

    public Task<bool> ExisteCnpjAsync(Cnpj cnpj, Guid? excetoId, CancellationToken ct) =>
        Task.FromResult(Itens.OfType<ClientePessoaJuridica>().Any(c => c.Cnpj == cnpj && c.Id != excetoId));

    public Task<Pagina<Cliente>> ListarAsync(string? busca, bool incluirInativos, int pagina, int tamanho, CancellationToken ct)
    {
        IEnumerable<Cliente> consulta = Itens.Where(c => incluirInativos || c.Ativo);

        if (Cpf.TentarCriar(busca, out var cpf))
            consulta = consulta.Where(c => c is ClientePessoaFisica pf && pf.Cpf == cpf);
        else if (Cnpj.TentarCriar(busca, out var cnpj))
            consulta = consulta.Where(c => c is ClientePessoaJuridica pj && pj.Cnpj == cnpj);
        else if (!string.IsNullOrWhiteSpace(busca))
            consulta = consulta.Where(c => c.Nome.Contains(busca.Trim(), StringComparison.OrdinalIgnoreCase));

        var todos = consulta.OrderBy(c => c.Nome).ToList();
        return Task.FromResult(new Pagina<Cliente>(
            [.. todos.Skip((pagina - 1) * tamanho).Take(tamanho)], todos.Count, pagina, tamanho));
    }

    public void Adicionar(Cliente cliente) => Itens.Add(cliente);
}
```

e em `Cenario`: `public ClienteRepositorioFalso Clientes { get; } = new();`.

`tests/AtividadesJuridicas.UnitTests/Application/ClientesTests.cs`:

```csharp
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.UnitTests.Application;

public class ClientesTests
{
    private readonly Cenario _c = new();

    private Usuario Producao() => _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);

    private static DadosDoClienteEntrada Dados(string nome = "Maria Silva", string? email = " Maria@X.com ") => new(
        nome, email, "(82) 99999-0000", new EnderecoEntrada("Rua A", "10", null, "Centro", "Maceió", "al", "57020-000"), null);

    private Task<Resultado<Guid>> SalvarPf(Usuario? ator, Guid? id, string cpf = "529.982.247-25", DadosDoClienteEntrada? dados = null) =>
        new SalvarClientePessoaFisicaManipulador(_c.Como(ator), _c.Clientes, _c.Unidade).ManipularAsync(
            new SalvarClientePessoaFisica(id, dados ?? Dados(), cpf, "123", new DateOnly(1980, 5, 17)), default);

    private Task<Resultado<Guid>> SalvarPj(Usuario? ator, Guid? id, string cnpj = "11.222.333/0001-81", DadosDoClienteEntrada? dados = null) =>
        new SalvarClientePessoaJuridicaManipulador(_c.Como(ator), _c.Clientes, _c.Unidade).ManipularAsync(
            new SalvarClientePessoaJuridica(id, dados ?? Dados("Acme Ltda", null), cnpj, "Acme", "João", null, null), default);

    private Task<Resultado<Pagina<ClienteResumo>>> Listar(Usuario? ator, string? busca = null, bool inativos = false, int pagina = 1) =>
        new ListarClientesManipulador(_c.Como(ator), _c.Clientes)
            .ManipularAsync(new ListarClientes(busca, inativos, pagina), default);

    private Task<Resultado<ClienteDetalhe>> Obter(Usuario? ator, Guid id) =>
        new ObterClienteManipulador(_c.Como(ator), _c.Clientes).ManipularAsync(new ObterCliente(id), default);

    private Task<Resultado> DefinirAtivo(Usuario? ator, Guid id, bool ativo) =>
        new DefinirAtivoClienteManipulador(_c.Como(ator), _c.Clientes, _c.Unidade)
            .ManipularAsync(new DefinirAtivoCliente(id, ativo), default);

    // ---- Salvar ----

    [Fact]
    public async Task Diretor_cria_pessoa_fisica_com_dados_normalizados()
    {
        var criado = await SalvarPf(_c.Diretor, null);

        criado.Sucesso.Should().BeTrue(criado.Mensagem);
        var cliente = _c.Clientes.Itens.OfType<ClientePessoaFisica>().Single();
        cliente.Id.Should().Be(criado.Valor);
        cliente.Cpf.Valor.Should().Be("52998224725");
        cliente.Email!.Valor.Should().Be("maria@x.com");
        cliente.Endereco.Uf.Should().Be("AL");
        cliente.Endereco.Cep.Should().Be("57020000");
        _c.Unidade.Salvamentos.Should().Be(1);
    }

    [Fact]
    public async Task Diretor_cria_pessoa_juridica_com_cnpj_alfanumerico()
    {
        var criado = await SalvarPj(_c.Diretor, null, "12.abc.345/01de-35");

        criado.Sucesso.Should().BeTrue(criado.Mensagem);
        _c.Clientes.Itens.OfType<ClientePessoaJuridica>().Single().Cnpj.Valor.Should().Be("12ABC34501DE35");
    }

    [Fact]
    public async Task Mesmo_documento_com_outra_pontuacao_e_conflito()
    {
        await SalvarPf(_c.Diretor, null, "529.982.247-25");
        await SalvarPj(_c.Diretor, null, "11.222.333/0001-81");

        (await SalvarPf(_c.Diretor, null, "52998224725")).Erro.Should().Be(TipoErro.Conflito);
        (await SalvarPj(_c.Diretor, null, "11222333000181")).Erro.Should().Be(TipoErro.Conflito);
        _c.Clientes.Itens.Should().HaveCount(2);
    }

    [Fact]
    public async Task Editar_mantendo_o_proprio_documento_nao_e_conflito()
    {
        var criado = await SalvarPf(_c.Diretor, null);

        var editado = await SalvarPf(_c.Diretor, criado.Valor, dados: Dados("Maria S. Santos"));

        editado.Sucesso.Should().BeTrue(editado.Mensagem);
        _c.Clientes.Itens.Single().Nome.Should().Be("Maria S. Santos");
    }

    [Theory]
    [InlineData("529.982.247-24", "Maria", "maria@x.com")]
    [InlineData("529.982.247-25", "", "maria@x.com")]
    [InlineData("529.982.247-25", "Maria", "nao-e-email")]
    public async Task Dados_invalidos_sao_erro_de_validacao(string cpf, string nome, string email)
    {
        var resultado = await SalvarPf(_c.Diretor, null, cpf, Dados(nome, email));

        resultado.Erro.Should().Be(TipoErro.Validacao);
        _c.Clientes.Itens.Should().BeEmpty();
        _c.Unidade.Salvamentos.Should().Be(0);
    }

    [Fact]
    public async Task Endereco_invalido_e_erro_de_validacao()
    {
        var dados = Dados() with { Endereco = new EnderecoEntrada(Uf: "ALA") };

        (await SalvarPf(_c.Diretor, null, dados: dados)).Erro.Should().Be(TipoErro.Validacao);
    }

    [Fact]
    public async Task Email_em_branco_vira_cliente_sem_email()
    {
        var criado = await SalvarPf(_c.Diretor, null, dados: Dados(email: "   "));

        criado.Sucesso.Should().BeTrue();
        _c.Clientes.Itens.Single().Email.Should().BeNull();
    }

    [Fact]
    public async Task Salvar_com_o_id_de_um_cliente_de_outro_tipo_e_nao_encontrado()
    {
        var pf = await SalvarPf(_c.Diretor, null);
        var pj = await SalvarPj(_c.Diretor, null);

        (await SalvarPj(_c.Diretor, pf.Valor, "11.444.777/0001-61")).Erro.Should().Be(TipoErro.NaoEncontrado);
        (await SalvarPf(_c.Diretor, pj.Valor, "111.444.777-35")).Erro.Should().Be(TipoErro.NaoEncontrado);
        (await SalvarPf(_c.Diretor, Guid.CreateVersion7())).Erro.Should().Be(TipoErro.NaoEncontrado);
        _c.Clientes.Itens.Single(c => c.Id == pf.Valor).Tipo.Should().Be(TipoCliente.PessoaFisica);
    }

    [Fact]
    public async Task Producao_e_anonimo_nao_criam_editam_nem_inativam()
    {
        var criado = await SalvarPf(_c.Diretor, null);
        var producao = Producao();

        (await SalvarPf(producao, null, "111.444.777-35")).Erro.Should().Be(TipoErro.AcessoNegado);
        (await SalvarPj(producao, null)).Erro.Should().Be(TipoErro.AcessoNegado);
        (await SalvarPf(producao, criado.Valor, dados: Dados("Outro"))).Erro.Should().Be(TipoErro.AcessoNegado);
        (await DefinirAtivo(producao, criado.Valor, false)).Erro.Should().Be(TipoErro.AcessoNegado);
        (await SalvarPf(null, null, "111.444.777-35")).Erro.Should().Be(TipoErro.AcessoNegado);

        var cliente = _c.Clientes.Itens.Should().ContainSingle().Subject;
        cliente.Nome.Should().Be("Maria Silva");
        cliente.Ativo.Should().BeTrue();
    }

    // ---- Ativar / inativar ----

    [Fact]
    public async Task Inativar_e_reativar()
    {
        var criado = await SalvarPf(_c.Diretor, null);

        (await DefinirAtivo(_c.Diretor, criado.Valor, false)).Sucesso.Should().BeTrue();
        _c.Clientes.Itens.Single().Ativo.Should().BeFalse();

        (await DefinirAtivo(_c.Diretor, criado.Valor, true)).Sucesso.Should().BeTrue();
        _c.Clientes.Itens.Single().Ativo.Should().BeTrue();

        (await DefinirAtivo(_c.Diretor, Guid.CreateVersion7(), false)).Erro.Should().Be(TipoErro.NaoEncontrado);
    }

    // ---- Consultar ----

    [Fact]
    public async Task Producao_lista_e_ve_o_documento_formatado()
    {
        await SalvarPf(_c.Diretor, null);
        await SalvarPj(_c.Diretor, null);

        var pagina = (await Listar(Producao())).Valor!;

        pagina.Total.Should().Be(2);
        pagina.Itens.Should().BeEquivalentTo(
        [
            new { Nome = "Acme Ltda", Tipo = TipoCliente.PessoaJuridica, Documento = "11.222.333/0001-81", Cidade = "Maceió" },
            new { Nome = "Maria Silva", Tipo = TipoCliente.PessoaFisica, Documento = "529.982.247-25", Cidade = "Maceió" },
        ], opcoes => opcoes.WithStrictOrdering());
    }

    [Fact]
    public async Task Inativos_so_aparecem_quando_pedidos()
    {
        var criado = await SalvarPf(_c.Diretor, null);
        await DefinirAtivo(_c.Diretor, criado.Valor, false);

        (await Listar(_c.Diretor)).Valor!.Itens.Should().BeEmpty();
        (await Listar(_c.Diretor, inativos: true)).Valor!.Itens.Should().ContainSingle();
    }

    [Fact]
    public async Task Busca_por_nome_parcial_ou_por_documento()
    {
        await SalvarPf(_c.Diretor, null);
        await SalvarPj(_c.Diretor, null);

        (await Listar(_c.Diretor, "mari")).Valor!.Itens.Should().ContainSingle().Which.Nome.Should().Be("Maria Silva");
        (await Listar(_c.Diretor, "52998224725")).Valor!.Itens.Should().ContainSingle().Which.Nome.Should().Be("Maria Silva");
        (await Listar(_c.Diretor, "11.222.333/0001-81")).Valor!.Itens.Should().ContainSingle().Which.Nome.Should().Be("Acme Ltda");
        (await Listar(_c.Diretor, "ninguém")).Valor!.Itens.Should().BeEmpty();
    }

    [Fact]
    public async Task Pagina_invalida_vira_a_primeira()
    {
        await SalvarPf(_c.Diretor, null);

        (await Listar(_c.Diretor, pagina: 0)).Valor!.Numero.Should().Be(1);
        (await Listar(_c.Diretor, pagina: -5)).Valor!.Itens.Should().ContainSingle();
    }

    [Fact]
    public async Task Producao_abre_a_ficha_completa()
    {
        var pf = await SalvarPf(_c.Diretor, null);
        var pj = await SalvarPj(_c.Diretor, null);
        var producao = Producao();

        var fisica = (await Obter(producao, pf.Valor)).Valor!;
        fisica.Should().BeEquivalentTo(new
        {
            Id = pf.Valor, Tipo = TipoCliente.PessoaFisica, Nome = "Maria Silva", Email = "maria@x.com",
            Cpf = "529.982.247-25", Rg = "123", DataNascimento = new DateOnly(1980, 5, 17),
            Cidade = "Maceió", Uf = "AL", Cep = "57020000", Ativo = true,
        });
        fisica.Cnpj.Should().BeNull();

        var juridica = (await Obter(producao, pj.Valor)).Valor!;
        juridica.Cnpj.Should().Be("11.222.333/0001-81");
        juridica.NomeFantasia.Should().Be("Acme");
        juridica.Cpf.Should().BeNull();
    }

    [Fact]
    public async Task Ficha_inexistente_e_nao_encontrada_e_anonimo_e_negado()
    {
        var pf = await SalvarPf(_c.Diretor, null);

        (await Obter(_c.Diretor, Guid.CreateVersion7())).Erro.Should().Be(TipoErro.NaoEncontrado);
        (await Obter(null, pf.Valor)).Erro.Should().Be(TipoErro.AcessoNegado);
        (await Listar(null)).Erro.Should().Be(TipoErro.AcessoNegado);
    }

    [Fact]
    public void Consultas_declaram_o_que_registram()
    {
        var id = Guid.CreateVersion7();
        IConsultaAuditavel listar = new ListarClientes("maria", true, 2);
        IConsultaAuditavel obter = new ObterCliente(id);

        listar.TipoRecurso.Should().Be("Cliente");
        listar.Acao.Should().Be(AcaoDeAcesso.Listou);
        listar.RecursoId.Should().BeNull();
        listar.Detalhe.Should().Contain("maria");

        obter.TipoRecurso.Should().Be("Cliente");
        obter.Acao.Should().Be(AcaoDeAcesso.Visualizou);
        obter.RecursoId.Should().Be(id);
    }
}
```

- [ ] **Step 2: Rodar e confirmar a falha**

Run: `dotnet test tests/AtividadesJuridicas.UnitTests`
Expected: erro de compilação (`IClienteRepositorio` e os casos de uso não existem).

- [ ] **Step 3: Implementar portas, entradas, modelos e mapeamento**

`src/AtividadesJuridicas.Application/Cadastros/PortasDeClientes.cs`:

```csharp
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Domain.Cadastros;

namespace AtividadesJuridicas.Application.Cadastros;

public static class ClientesOpcoes
{
    public const int TamanhoDaPagina = 20;
}

public interface IClienteRepositorio
{
    Task<Cliente?> ObterPorIdAsync(Guid id, CancellationToken ct);
    Task<bool> ExisteCpfAsync(Cpf cpf, Guid? excetoId, CancellationToken ct);
    Task<bool> ExisteCnpjAsync(Cnpj cnpj, Guid? excetoId, CancellationToken ct);

    /// <summary>
    /// Ordenado por nome. Texto que é um CPF ou CNPJ válido procura pelo documento exato; qualquer
    /// outro procura no nome, sem diferenciar maiúsculas, tratando % e _ como caracteres comuns.
    /// </summary>
    Task<Pagina<Cliente>> ListarAsync(string? busca, bool incluirInativos, int pagina, int tamanho, CancellationToken ct);

    void Adicionar(Cliente cliente);
}

public sealed record EnderecoEntrada(
    string? Logradouro = null, string? Numero = null, string? Complemento = null, string? Bairro = null,
    string? Cidade = null, string? Uf = null, string? Cep = null);

public sealed record DadosDoClienteEntrada(
    string Nome, string? Email, string? Telefone, EnderecoEntrada Endereco, string? Observacoes);

/// <summary><paramref name="Documento"/> vem formatado (CPF ou CNPJ).</summary>
public sealed record ClienteResumo(
    Guid Id, TipoCliente Tipo, string Nome, string Documento, string? Email, string? Telefone, string? Cidade, bool Ativo);

/// <summary>CPF e CNPJ vêm formatados; só os campos do tipo do cliente são preenchidos.</summary>
public sealed record ClienteDetalhe(
    Guid Id, TipoCliente Tipo, string Nome, string? Email, string? Telefone,
    string? Logradouro, string? Numero, string? Complemento, string? Bairro, string? Cidade, string? Uf, string? Cep,
    string? Observacoes, bool Ativo,
    string? Cpf, string? Rg, DateOnly? DataNascimento,
    string? Cnpj, string? NomeFantasia, string? NomeResponsavel, string? InscricaoEstadual, string? InscricaoMunicipal);
```

`src/AtividadesJuridicas.Application/Cadastros/ClienteMapeamento.cs`:

```csharp
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Comum;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.Application.Cadastros;

internal static class ClienteMapeamento
{
    /// <summary>Lança <see cref="ExcecaoDeDominio"/> quando o e-mail ou o endereço são inválidos.</summary>
    public static DadosDoCliente ParaDominio(DadosDoClienteEntrada entrada)
    {
        Email? email = null;
        if (!string.IsNullOrWhiteSpace(entrada.Email) && !Email.TentarCriar(entrada.Email, out email))
            throw new ExcecaoDeDominio("E-mail inválido.");

        var e = entrada.Endereco;
        var endereco = Endereco.Criar(e.Logradouro, e.Numero, e.Complemento, e.Bairro, e.Cidade, e.Uf, e.Cep);

        return new DadosDoCliente(entrada.Nome, email, entrada.Telefone, endereco, entrada.Observacoes);
    }

    public static string Documento(Cliente cliente) => cliente switch
    {
        ClientePessoaFisica pf => pf.Cpf.Formatado,
        ClientePessoaJuridica pj => pj.Cnpj.Formatado,
        _ => string.Empty,
    };

    public static ClienteResumo ParaResumo(Cliente c) => new(
        c.Id, c.Tipo, c.Nome, Documento(c), c.Email?.Valor, c.Telefone, c.Endereco.Cidade, c.Ativo);

    public static ClienteDetalhe ParaDetalhe(Cliente c)
    {
        var pf = c as ClientePessoaFisica;
        var pj = c as ClientePessoaJuridica;
        var e = c.Endereco;

        return new ClienteDetalhe(
            c.Id, c.Tipo, c.Nome, c.Email?.Valor, c.Telefone,
            e.Logradouro, e.Numero, e.Complemento, e.Bairro, e.Cidade, e.Uf, e.Cep,
            c.Observacoes, c.Ativo,
            pf?.Cpf.Formatado, pf?.Rg, pf?.DataNascimento,
            pj?.Cnpj.Formatado, pj?.NomeFantasia, pj?.NomeResponsavel, pj?.InscricaoEstadual, pj?.InscricaoMunicipal);
    }
}
```

- [ ] **Step 4: Implementar os casos de uso**

`src/AtividadesJuridicas.Application/Cadastros/ListarClientes.cs`:

```csharp
using System.Text.Json;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;

namespace AtividadesJuridicas.Application.Cadastros;

public sealed record ListarClientes(string? Busca = null, bool IncluirInativos = false, int Pagina = 1)
    : IRequisicao<Resultado<Pagina<ClienteResumo>>>, IConsultaAuditavel
{
    public string TipoRecurso => "Cliente";
    public AcaoDeAcesso Acao => AcaoDeAcesso.Listou;
    public string? Detalhe => JsonSerializer.Serialize(new { Busca, IncluirInativos, Pagina });
}

public sealed class ListarClientesManipulador(IContextoDeAcesso contexto, IClienteRepositorio clientes)
    : IManipulador<ListarClientes, Resultado<Pagina<ClienteResumo>>>
{
    public async Task<Resultado<Pagina<ClienteResumo>>> ManipularAsync(ListarClientes requisicao, CancellationToken ct)
    {
        // Diretoria e Produção consultam clientes; só a Diretoria altera.
        if (await contexto.ObterAsync(ct) is null)
            return Resultado<Pagina<ClienteResumo>>.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        var pagina = await clientes.ListarAsync(
            requisicao.Busca, requisicao.IncluirInativos, Math.Max(1, requisicao.Pagina), ClientesOpcoes.TamanhoDaPagina, ct);

        return Resultado<Pagina<ClienteResumo>>.Ok(new Pagina<ClienteResumo>(
            [.. pagina.Itens.Select(ClienteMapeamento.ParaResumo)], pagina.Total, pagina.Numero, pagina.Tamanho));
    }
}
```

`src/AtividadesJuridicas.Application/Cadastros/ObterCliente.cs`:

```csharp
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;

namespace AtividadesJuridicas.Application.Cadastros;

public sealed record ObterCliente(Guid Id) : IRequisicao<Resultado<ClienteDetalhe>>, IConsultaAuditavel
{
    public string TipoRecurso => "Cliente";
    public AcaoDeAcesso Acao => AcaoDeAcesso.Visualizou;
    public Guid? RecursoId => Id;
}

public sealed class ObterClienteManipulador(IContextoDeAcesso contexto, IClienteRepositorio clientes)
    : IManipulador<ObterCliente, Resultado<ClienteDetalhe>>
{
    public async Task<Resultado<ClienteDetalhe>> ManipularAsync(ObterCliente requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is null)
            return Resultado<ClienteDetalhe>.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        var cliente = await clientes.ObterPorIdAsync(requisicao.Id, ct);
        return cliente is null
            ? Resultado<ClienteDetalhe>.Falha(TipoErro.NaoEncontrado, "Cliente não encontrado.")
            : Resultado<ClienteDetalhe>.Ok(ClienteMapeamento.ParaDetalhe(cliente));
    }
}
```

`src/AtividadesJuridicas.Application/Cadastros/SalvarClientePessoaFisica.cs`:

```csharp
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Application.Cadastros;

/// <summary>Cria o cliente quando <paramref name="Id"/> é nulo; caso contrário, altera.</summary>
public sealed record SalvarClientePessoaFisica(
    Guid? Id, DadosDoClienteEntrada Dados, string Cpf, string? Rg, DateOnly? DataNascimento)
    : IRequisicao<Resultado<Guid>>;

public sealed class SalvarClientePessoaFisicaManipulador(
    IContextoDeAcesso contexto,
    IClienteRepositorio clientes,
    IUnidadeDeTrabalho unidade) : IManipulador<SalvarClientePessoaFisica, Resultado<Guid>>
{
    private const string CpfEmUso = "Já existe um cliente com este CPF.";

    public async Task<Resultado<Guid>> ManipularAsync(SalvarClientePessoaFisica requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Resultado<Guid>.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        ClientePessoaFisica? existente = null;
        if (requisicao.Id is { } id)
        {
            // Um Id de pessoa jurídica não é encontrado aqui: o tipo do cliente não muda.
            existente = await clientes.ObterPorIdAsync(id, ct) as ClientePessoaFisica;
            if (existente is null)
                return Resultado<Guid>.Falha(TipoErro.NaoEncontrado, "Cliente não encontrado.");
        }

        if (!Domain.Cadastros.Cpf.TentarCriar(requisicao.Cpf, out var cpf))
            return Resultado<Guid>.Falha(TipoErro.Validacao, "CPF inválido.");

        if (await clientes.ExisteCpfAsync(cpf, existente?.Id, ct))
            return Resultado<Guid>.Falha(TipoErro.Conflito, CpfEmUso);

        try
        {
            var dados = ClienteMapeamento.ParaDominio(requisicao.Dados);

            if (existente is null)
            {
                existente = ClientePessoaFisica.Criar(dados, cpf, requisicao.Rg, requisicao.DataNascimento);
                clientes.Adicionar(existente);
            }
            else
            {
                existente.Alterar(dados, cpf, requisicao.Rg, requisicao.DataNascimento);
            }

            await unidade.SalvarAsync(ct);
            return Resultado<Guid>.Ok(existente.Id);
        }
        catch (ExcecaoDeDominio ex)
        {
            return Resultado<Guid>.Falha(TipoErro.Validacao, ex.Message);
        }
        catch (ExcecaoDeConflito)
        {
            return Resultado<Guid>.Falha(TipoErro.Conflito, CpfEmUso);
        }
    }
}
```

`src/AtividadesJuridicas.Application/Cadastros/SalvarClientePessoaJuridica.cs`:

```csharp
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Application.Cadastros;

/// <summary>Cria o cliente quando <paramref name="Id"/> é nulo; caso contrário, altera.</summary>
public sealed record SalvarClientePessoaJuridica(
    Guid? Id, DadosDoClienteEntrada Dados, string Cnpj, string? NomeFantasia, string? NomeResponsavel,
    string? InscricaoEstadual, string? InscricaoMunicipal)
    : IRequisicao<Resultado<Guid>>;

public sealed class SalvarClientePessoaJuridicaManipulador(
    IContextoDeAcesso contexto,
    IClienteRepositorio clientes,
    IUnidadeDeTrabalho unidade) : IManipulador<SalvarClientePessoaJuridica, Resultado<Guid>>
{
    private const string CnpjEmUso = "Já existe um cliente com este CNPJ.";

    public async Task<Resultado<Guid>> ManipularAsync(SalvarClientePessoaJuridica requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Resultado<Guid>.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        ClientePessoaJuridica? existente = null;
        if (requisicao.Id is { } id)
        {
            // Um Id de pessoa física não é encontrado aqui: o tipo do cliente não muda.
            existente = await clientes.ObterPorIdAsync(id, ct) as ClientePessoaJuridica;
            if (existente is null)
                return Resultado<Guid>.Falha(TipoErro.NaoEncontrado, "Cliente não encontrado.");
        }

        if (!Domain.Cadastros.Cnpj.TentarCriar(requisicao.Cnpj, out var cnpj))
            return Resultado<Guid>.Falha(TipoErro.Validacao, "CNPJ inválido.");

        if (await clientes.ExisteCnpjAsync(cnpj, existente?.Id, ct))
            return Resultado<Guid>.Falha(TipoErro.Conflito, CnpjEmUso);

        try
        {
            var dados = ClienteMapeamento.ParaDominio(requisicao.Dados);

            if (existente is null)
            {
                existente = ClientePessoaJuridica.Criar(
                    dados, cnpj, requisicao.NomeFantasia, requisicao.NomeResponsavel,
                    requisicao.InscricaoEstadual, requisicao.InscricaoMunicipal);
                clientes.Adicionar(existente);
            }
            else
            {
                existente.Alterar(
                    dados, cnpj, requisicao.NomeFantasia, requisicao.NomeResponsavel,
                    requisicao.InscricaoEstadual, requisicao.InscricaoMunicipal);
            }

            await unidade.SalvarAsync(ct);
            return Resultado<Guid>.Ok(existente.Id);
        }
        catch (ExcecaoDeDominio ex)
        {
            return Resultado<Guid>.Falha(TipoErro.Validacao, ex.Message);
        }
        catch (ExcecaoDeConflito)
        {
            return Resultado<Guid>.Falha(TipoErro.Conflito, CnpjEmUso);
        }
    }
}
```

`src/AtividadesJuridicas.Application/Cadastros/DefinirAtivoCliente.cs`:

```csharp
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;

namespace AtividadesJuridicas.Application.Cadastros;

public sealed record DefinirAtivoCliente(Guid Id, bool Ativo) : IRequisicao<Resultado>;

public sealed class DefinirAtivoClienteManipulador(
    IContextoDeAcesso contexto,
    IClienteRepositorio clientes,
    IUnidadeDeTrabalho unidade) : IManipulador<DefinirAtivoCliente, Resultado>
{
    public async Task<Resultado> ManipularAsync(DefinirAtivoCliente requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Resultado.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        var cliente = await clientes.ObterPorIdAsync(requisicao.Id, ct);
        if (cliente is null)
            return Resultado.Falha(TipoErro.NaoEncontrado, "Cliente não encontrado.");

        if (cliente.Ativo == requisicao.Ativo) return Resultado.Ok();

        if (requisicao.Ativo) cliente.Ativar();
        else cliente.Inativar();

        await unidade.SalvarAsync(ct);
        return Resultado.Ok();
    }
}
```

- [ ] **Step 5: Rodar os testes unitários**

Run: `dotnet build` e `dotnet test tests/AtividadesJuridicas.UnitTests`
Expected: build sem avisos; unitários aprovados, incluindo `Toda_requisicao_tem_exatamente_um_manipulador_registrado`. Não rode a suíte de integração nem faça commit ainda: siga para a Parte B.

#### Parte B — Persistência e auditoria de agregados

**Files:**
- Create: `src/AtividadesJuridicas.Infrastructure/Persistencia/Configuracoes/ClienteConfiguracao.cs`, `Repositorios/ClienteRepositorio.cs`; migration `Clientes`
- Modify: `src/AtividadesJuridicas.Infrastructure/Persistencia/Interceptores/AuditoriaInterceptor.cs`, `AppDbContext.cs`, `Infrastructure/DependencyInjection.cs`
- Modify: `tests/AtividadesJuridicas.IntegrationTests/PropriedadesAuditadasTests.cs`
- Test: `tests/AtividadesJuridicas.IntegrationTests/ClientesTests.cs`

**Interfaces:**
- Consumes: `IClienteRepositorio`, os casos de uso da Parte A, `TesteComBanco`.
- Produces:
  - Tabela `clientes` (uma para os dois tipos, discriminada por `tipo`), com `cpf` e `cnpj` únicos entre os não excluídos e o endereço em colunas `endereco_*`.
  - `AppDbContext.Clientes`.
  - `AuditoriaInterceptor`: o registro usa o nome do **tipo raiz** da hierarquia (`"Cliente"` para PF e PJ); as propriedades de tipos complexos entram achatadas com o nome `Tipo.Propriedade` (`"Endereco.Cidade"`), em `Dados` e em `PropriedadesAuditadas`.

- [ ] **Step 6: Escrever os testes de integração que falham**

Em `PropriedadesAuditadasTests.cs`, acrescente ao dicionário esperado:

```csharp
            ["Cliente"] =
            [
                "Ativo", "Email", "Nome", "Observacoes", "Telefone", "Tipo",
                "Endereco.Bairro", "Endereco.Cep", "Endereco.Cidade", "Endereco.Complemento",
                "Endereco.Logradouro", "Endereco.Numero", "Endereco.Uf",
            ],
            ["ClientePessoaFisica"] =
            [
                "Ativo", "Email", "Nome", "Observacoes", "Telefone", "Tipo",
                "Endereco.Bairro", "Endereco.Cep", "Endereco.Cidade", "Endereco.Complemento",
                "Endereco.Logradouro", "Endereco.Numero", "Endereco.Uf",
                "Cpf", "DataNascimento", "Rg",
            ],
            ["ClientePessoaJuridica"] =
            [
                "Ativo", "Email", "Nome", "Observacoes", "Telefone", "Tipo",
                "Endereco.Bairro", "Endereco.Cep", "Endereco.Cidade", "Endereco.Complemento",
                "Endereco.Logradouro", "Endereco.Numero", "Endereco.Uf",
                "Cnpj", "InscricaoEstadual", "InscricaoMunicipal", "NomeFantasia", "NomeResponsavel",
            ],
```

`tests/AtividadesJuridicas.IntegrationTests/ClientesTests.cs`:

```csharp
using System.Text.Json;
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
        alteracao.Dados.Should().Be("""{"Endereco.Cidade":{"de":"Maceió","para":"Arapiraca"}}""");

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
```

- [ ] **Step 7: Rodar e confirmar a falha**

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --filter "FullyQualifiedName~ClientesTests|FullyQualifiedName~PropriedadesAuditadasTests"`
Expected: erro de compilação (`AppDbContext.Clientes` não existe).

- [ ] **Step 8: Implementar o mapeamento e o repositório**

`src/AtividadesJuridicas.Infrastructure/Persistencia/Configuracoes/ClienteConfiguracao.cs`:

```csharp
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Identidade;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AtividadesJuridicas.Infrastructure.Persistencia.Configuracoes;

public sealed class ClienteConfiguracao : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> b)
    {
        b.ToTable("clientes");

        // Uma tabela para os dois tipos; a coluna tipo decide a classe.
        b.HasDiscriminator(c => c.Tipo)
            .HasValue<ClientePessoaFisica>(TipoCliente.PessoaFisica)
            .HasValue<ClientePessoaJuridica>(TipoCliente.PessoaJuridica);
        b.Property(c => c.Tipo).HasConversion<int>();

        b.Property(c => c.Nome).HasMaxLength(Cliente.TamanhoMaximoNome).IsRequired();
        b.Property(c => c.Email)
            .HasConversion(e => e!.Valor, v => Email.Criar(v))
            .HasMaxLength(Email.TamanhoMaximo);
        b.Property(c => c.Telefone).HasMaxLength(Cliente.TamanhoMaximoTelefone);
        b.Property(c => c.Observacoes).HasMaxLength(Cliente.TamanhoMaximoObservacoes);

        // Objeto de valor: fica em colunas da própria tabela e é auditado campo a campo.
        b.ComplexProperty(c => c.Endereco, e =>
        {
            e.Property(x => x.Logradouro).HasMaxLength(200);
            e.Property(x => x.Numero).HasMaxLength(20);
            e.Property(x => x.Complemento).HasMaxLength(100);
            e.Property(x => x.Bairro).HasMaxLength(100);
            e.Property(x => x.Cidade).HasMaxLength(100);
            e.Property(x => x.Uf).HasMaxLength(2).IsFixedLength();
            e.Property(x => x.Cep).HasMaxLength(8).IsFixedLength();
        });

        b.HasIndex(c => c.Nome);
    }
}

public sealed class ClientePessoaFisicaConfiguracao : IEntityTypeConfiguration<ClientePessoaFisica>
{
    public void Configure(EntityTypeBuilder<ClientePessoaFisica> b)
    {
        b.Property(c => c.Cpf).HasConversion(c => c.Valor, v => Cpf.Criar(v)).HasMaxLength(11).IsFixedLength();
        b.Property(c => c.Rg).HasMaxLength(ClientePessoaFisica.TamanhoMaximoRg);
        b.HasIndex(c => c.Cpf).IsUnique().HasFilter("excluido_em IS NULL");
    }
}

public sealed class ClientePessoaJuridicaConfiguracao : IEntityTypeConfiguration<ClientePessoaJuridica>
{
    public void Configure(EntityTypeBuilder<ClientePessoaJuridica> b)
    {
        b.Property(c => c.Cnpj).HasConversion(c => c.Valor, v => Cnpj.Criar(v)).HasMaxLength(14).IsFixedLength();
        b.Property(c => c.NomeFantasia).HasMaxLength(Cliente.TamanhoMaximoNome);
        b.Property(c => c.NomeResponsavel).HasMaxLength(Cliente.TamanhoMaximoNome);
        b.Property(c => c.InscricaoEstadual).HasMaxLength(ClientePessoaJuridica.TamanhoMaximoInscricao);
        b.Property(c => c.InscricaoMunicipal).HasMaxLength(ClientePessoaJuridica.TamanhoMaximoInscricao);
        b.HasIndex(c => c.Cnpj).IsUnique().HasFilter("excluido_em IS NULL");
    }
}
```

`src/AtividadesJuridicas.Infrastructure/Persistencia/Repositorios/ClienteRepositorio.cs`:

```csharp
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Domain.Cadastros;
using Microsoft.EntityFrameworkCore;

namespace AtividadesJuridicas.Infrastructure.Persistencia.Repositorios;

public sealed class ClienteRepositorio(AppDbContext db) : IClienteRepositorio
{
    public Task<Cliente?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        db.Clientes.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<bool> ExisteCpfAsync(Cpf cpf, Guid? excetoId, CancellationToken ct) =>
        db.Clientes.OfType<ClientePessoaFisica>().AnyAsync(c => c.Cpf == cpf && c.Id != excetoId, ct);

    public Task<bool> ExisteCnpjAsync(Cnpj cnpj, Guid? excetoId, CancellationToken ct) =>
        db.Clientes.OfType<ClientePessoaJuridica>().AnyAsync(c => c.Cnpj == cnpj && c.Id != excetoId, ct);

    public async Task<Pagina<Cliente>> ListarAsync(
        string? busca, bool incluirInativos, int pagina, int tamanho, CancellationToken ct)
    {
        var consulta = db.Clientes.AsNoTracking();
        if (!incluirInativos) consulta = consulta.Where(c => c.Ativo);

        if (Cpf.TentarCriar(busca, out var cpf))
        {
            consulta = consulta.Where(c => c is ClientePessoaFisica && ((ClientePessoaFisica)c).Cpf == cpf);
        }
        else if (Cnpj.TentarCriar(busca, out var cnpj))
        {
            consulta = consulta.Where(c => c is ClientePessoaJuridica && ((ClientePessoaJuridica)c).Cnpj == cnpj);
        }
        else if (!string.IsNullOrWhiteSpace(busca))
        {
            // % e _ digitados pelo usuário são texto, não curingas.
            var padrao = "%" + busca.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            consulta = consulta.Where(c => EF.Functions.ILike(c.Nome, padrao, "\\"));
        }

        var total = await consulta.CountAsync(ct);
        var itens = await consulta
            .OrderBy(c => c.Nome).ThenBy(c => c.Id)
            .Skip((pagina - 1) * tamanho).Take(tamanho)
            .ToListAsync(ct);

        return new Pagina<Cliente>(itens, total, pagina, tamanho);
    }

    public void Adicionar(Cliente cliente) => db.Clientes.Add(cliente);
}
```

Em `AppDbContext.cs`: `public DbSet<Cliente> Clientes => Set<Cliente>();`.
Em `DependencyInjection.cs` (Infrastructure): `servicos.AddScoped<IClienteRepositorio, ClienteRepositorio>();`.

- [ ] **Step 9: Estender o interceptor de auditoria**

Em `AuditoriaInterceptor.cs`:

1. O nome gravado passa a ser o do tipo raiz da hierarquia. Em `Registrar`, troque `entrada.Metadata.ClrType.Name` por `entrada.Metadata.GetRootType().ClrType.Name`.

2. As propriedades passam a incluir as dos tipos complexos, achatadas. Substitua `Descrever`, `PropriedadesAuditadas`, `Auditavel` (as duas sobrecargas) e `Nome` por:

```csharp
    private static (OperacaoAuditada Operacao, string Dados)? Descrever(EntityEntry<Entidade> entrada)
    {
        var auditaveis = Folhas(entrada).ToList();

        switch (entrada.State)
        {
            case EntityState.Added:
                return (OperacaoAuditada.Criacao,
                    Serializar(auditaveis.ToDictionary(f => f.Nome, f => Valor(f.Entrada, f.Entrada.CurrentValue))));

            case EntityState.Modified:
                var exclusao = entrada.Property(nameof(Entidade.ExcluidoEm));
                if (exclusao is { IsModified: true, OriginalValue: null, CurrentValue: not null })
                    return (OperacaoAuditada.Exclusao,
                        Serializar(auditaveis.ToDictionary(f => f.Nome, f => Valor(f.Entrada, f.Entrada.OriginalValue))));

                var alteradas = auditaveis.Where(f => f.Entrada.IsModified).ToList();
                if (alteradas.Count == 0) return null;

                return (OperacaoAuditada.Alteracao, Serializar(alteradas.ToDictionary(
                    f => f.Nome,
                    f => (object?)new
                    {
                        de = Valor(f.Entrada, f.Entrada.OriginalValue),
                        para = Valor(f.Entrada, f.Entrada.CurrentValue),
                    })));

            default:
                return null;
        }
    }

    /// <summary>
    /// Propriedades escalares da entidade e, achatadas, as dos seus tipos complexos
    /// (por exemplo "Endereco.Cidade"), já sem as que ficam fora da auditoria.
    /// </summary>
    private static IEnumerable<(string Nome, PropertyEntry Entrada)> Folhas(EntityEntry entrada)
    {
        foreach (var propriedade in entrada.Properties.Where(p => Auditavel(p.Metadata)))
            yield return (propriedade.Metadata.Name, propriedade);

        foreach (var complexa in entrada.ComplexProperties)
            foreach (var folha in Folhas(complexa, complexa.Metadata.Name))
                yield return folha;
    }

    private static IEnumerable<(string Nome, PropertyEntry Entrada)> Folhas(ComplexPropertyEntry complexa, string prefixo)
    {
        foreach (var propriedade in complexa.Properties.Where(p => Auditavel(p.Metadata)))
            yield return ($"{prefixo}.{propriedade.Metadata.Name}", propriedade);

        foreach (var interna in complexa.ComplexProperties)
            foreach (var folha in Folhas(interna, $"{prefixo}.{interna.Metadata.Name}"))
                yield return folha;
    }

    /// <summary>Nomes das propriedades de <paramref name="tipo"/> que vão para os dados da auditoria.</summary>
    public static IReadOnlyList<string> PropriedadesAuditadas(IEntityType tipo) =>
        [.. tipo.GetProperties().Where(Auditavel).Select(p => p.Name),
         .. tipo.GetComplexProperties().SelectMany(c => PropriedadesAuditadas(c, c.Name))];

    private static IEnumerable<string> PropriedadesAuditadas(IComplexProperty complexa, string prefixo) =>
        complexa.ComplexType.GetProperties().Where(Auditavel).Select(p => $"{prefixo}.{p.Name}")
            .Concat(complexa.ComplexType.GetComplexProperties()
                .SelectMany(c => PropriedadesAuditadas(c, $"{prefixo}.{c.Name}")));

    private static bool Auditavel(IProperty propriedade) =>
        !ForaDosDados.Contains(propriedade.Name) &&
        propriedade.PropertyInfo?.GetCustomAttribute<NaoAuditarAttribute>() is null;
```

`Valor` e `Serializar` não mudam. Um `DateOnly` é serializado pelo `System.Text.Json` como `"1980-05-17"`.

- [ ] **Step 10: Gerar a migration**

Run:

```bash
dotnet ef migrations add Clientes --project src/AtividadesJuridicas.Infrastructure --startup-project src/AtividadesJuridicas.Infrastructure --output-dir Persistencia/Migrations
```

Expected: cria só a tabela `clientes`, com a coluna `tipo` (inteiro), as colunas `endereco_logradouro` … `endereco_cep`, `cpf` e `cnpj`, `data_nascimento` do tipo `date`, índices únicos `ix_clientes_cpf` e `ix_clientes_cnpj` filtrados por `excluido_em IS NULL`, e índice em `nome`. Registre no relatório os nomes de coluna gerados para o endereço.

- [ ] **Step 11: Rodar e confirmar que passa**

Run: `dotnet test`
Expected: tudo aprovado, zero avisos. Os testes de auditoria de usuários e perfis das fases anteriores continuam passando sem alteração.

- [ ] **Step 12: Commit**

```bash
git add -A
git commit -m "feat(cadastros): clientes — casos de uso, persistência e auditoria por tipo raiz e de tipos complexos"
```

---

### Task 5: Tela de clientes

**Files:**
- Create: `src/AtividadesJuridicas.Web/Components/Pages/Clientes.razor`
- Modify: `src/AtividadesJuridicas.Web/Components/Layout/MainLayout.razor`, `src/AtividadesJuridicas.Web/wwwroot/app.css`
- Test: `tests/AtividadesJuridicas.IntegrationTests/ClientesWebTests.cs`

**Interfaces:**
- Consumes: `IDespachante`, `ObterMeuAcesso`, `ListarClientes`, `ObterCliente`, `SalvarClientePessoaFisica`, `SalvarClientePessoaJuridica`, `DefinirAtivoCliente`, `ClienteResumo`, `ClienteDetalhe`, `DadosDoClienteEntrada`, `EnderecoEntrada`, `TipoCliente`, `Pagina<T>`.
- Produces: rota `/clientes`; link "Clientes" no menu para todo usuário autenticado.
- Comportamento: lista com busca, opção de incluir inativos e paginação. A Diretoria vê os botões de novo cliente (PF e PJ), editar e ativar/inativar, e o formulário. A Produção vê "Ver", que abre a ficha em modo leitura.

- [ ] **Step 1: Escrever os testes que falham**

`tests/AtividadesJuridicas.IntegrationTests/ClientesWebTests.cs`:

```csharp
using System.Net;
using AtividadesJuridicas.Application.Cadastros;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class ClientesWebTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco);

    public async Task InitializeAsync()
    {
        var adminId = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        var criado = await _app.ExecutarAsync(adminId, new SalvarClientePessoaFisica(
            null,
            new DadosDoClienteEntrada("Maria Silva", "maria@x.com", null, new EnderecoEntrada(Cidade: "Maceió"), null),
            "529.982.247-25", null, null));
        criado.Sucesso.Should().BeTrue(criado.Mensagem);
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    [Fact]
    public async Task Diretor_ve_a_lista_e_as_acoes_de_cadastro()
    {
        var cliente = await _app.ClienteAutenticadoAsync();

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/clientes"));

        html.Should().Contain("Clientes").And.Contain("Maria Silva").And.Contain("529.982.247-25").And.Contain("Maceió");
        html.Should().Contain("Nova pessoa física").And.Contain("Nova pessoa jurídica").And.Contain("Editar");
    }

    [Fact]
    public async Task Producao_ve_a_lista_mas_nenhuma_acao_de_cadastro()
    {
        await _app.CriarPedroAsync();
        var cliente = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/clientes"));

        html.Should().Contain("Maria Silva").And.Contain("Ver");
        html.Should().NotContain("Nova pessoa física").And.NotContain("Nova pessoa jurídica")
            .And.NotContain("Editar").And.NotContain("Inativar").And.NotContain("Acesso negado");
    }

    [Fact]
    public async Task Menu_mostra_clientes_para_todos_os_perfis()
    {
        await _app.CriarPedroAsync();
        var doDiretor = await _app.ClienteAutenticadoAsync();
        var doPedro = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        (await doDiretor.GetStringAsync("/")).Should().Contain("href=\"/clientes\"");
        (await doPedro.GetStringAsync("/")).Should().Contain("href=\"/clientes\"");
    }

    [Fact]
    public async Task Anonimo_e_redirecionado_ao_login()
    {
        var resposta = await _app.NovoCliente().GetAsync("/clientes");

        resposta.StatusCode.Should().Be(HttpStatusCode.Redirect);
        resposta.Headers.Location!.AbsolutePath.Should().Be("/conta/entrar");
    }
}
```

- [ ] **Step 2: Rodar e confirmar a falha**

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --filter "FullyQualifiedName~ClientesWebTests"`
Expected: FAIL. `/clientes` responde 404 e o menu não tem o link.

- [ ] **Step 3: Implementar**

Em `MainLayout.razor`, logo depois do link "Início" e fora do bloco da Diretoria: `<NavLink href="/clientes">Clientes</NavLink>`.

Acrescente a `wwwroot/app.css`:

```css
.grade { display: grid; grid-template-columns: repeat(auto-fit, minmax(14rem, 1fr)); gap: .75rem; }
.ficha { background: #fff; border: 1px solid var(--borda); border-radius: .5rem; padding: 1rem; margin-top: 1rem; }
.ficha dl { display: grid; grid-template-columns: 12rem 1fr; gap: .25rem .75rem; margin: 0; }
.ficha dt { font-weight: 600; }
textarea { padding: .5rem; border: 1px solid var(--borda); border-radius: .375rem; font: inherit; }
```

`src/AtividadesJuridicas.Web/Components/Pages/Clientes.razor`:

```razor
@page "/clientes"
@using System.ComponentModel.DataAnnotations
@inject IDespachante Despachante

<PageTitle>Clientes</PageTitle>

<h1>Clientes</h1>

@if (!_carregado)
{
    <p>Carregando…</p>
}
else if (_ator is null)
{
    <p class="erro">Seu acesso foi encerrado. Entre novamente.</p>
}
else
{
    <EditForm Model="_busca" OnSubmit="BuscarAsync" FormName="busca-clientes" class="formulario filtros">
        <label>Nome, CPF ou CNPJ <InputText @bind-Value="_busca.Texto" /></label>
        <label><InputCheckbox @bind-Value="_busca.IncluirInativos" /> Incluir inativos</label>
        <button type="submit">Buscar</button>
    </EditForm>

    @if (_mensagem is not null)
    {
        <p class="@(_falhou ? "erro" : "ok")" role="status">@_mensagem</p>
    }

    <table>
        <thead>
            <tr><th>Nome</th><th>Documento</th><th>E-mail</th><th>Telefone</th><th>Cidade</th><th>Situação</th><th></th></tr>
        </thead>
        <tbody>
            @foreach (var cliente in _pagina.Itens)
            {
                <tr class="@(cliente.Ativo ? null : "inativo")" @key="cliente.Id">
                    <td>@cliente.Nome</td>
                    <td>@cliente.Documento</td>
                    <td>@cliente.Email</td>
                    <td>@cliente.Telefone</td>
                    <td>@cliente.Cidade</td>
                    <td>@(cliente.Ativo ? "Ativo" : "Inativo")</td>
                    <td class="acoes">
                        @if (_ator.EhDiretoria)
                        {
                            <button type="button" class="secundario" @onclick="() => EditarAsync(cliente.Id)">Editar</button>
                            <button type="button" class="secundario" @onclick="() => AlternarAtivoAsync(cliente)">
                                @(cliente.Ativo ? "Inativar" : "Ativar")
                            </button>
                        }
                        else
                        {
                            <button type="button" class="secundario" @onclick="() => VerAsync(cliente.Id)">Ver</button>
                        }
                    </td>
                </tr>
            }
        </tbody>
    </table>

    <div class="acoes">
        <button type="button" class="secundario" disabled="@(_pagina.Numero <= 1)" @onclick="() => IrParaAsync(_pagina.Numero - 1)">Anterior</button>
        <span>Página @_pagina.Numero de @_pagina.TotalDePaginas (@_pagina.Total clientes)</span>
        <button type="button" class="secundario" disabled="@(_pagina.Numero >= _pagina.TotalDePaginas)" @onclick="() => IrParaAsync(_pagina.Numero + 1)">Próxima</button>
    </div>

    @if (_ator.EhDiretoria)
    {
        <div class="acoes">
            <button type="button" @onclick="() => Novo(TipoCliente.PessoaFisica)">Nova pessoa física</button>
            <button type="button" @onclick="() => Novo(TipoCliente.PessoaJuridica)">Nova pessoa jurídica</button>
        </div>

        @if (_form is not null)
        {
            <h2>@Titulo()</h2>

            <EditForm Model="_form" OnValidSubmit="SalvarAsync" FormName="cliente" class="formulario">
                <DataAnnotationsValidator />

                <div class="grade">
                    <label>
                        @(_form.Tipo == TipoCliente.PessoaFisica ? "Nome completo" : "Razão social")
                        <InputText @bind-Value="_form.Nome" />
                        <ValidationMessage For="() => _form.Nome" />
                    </label>

                    @if (_form.Tipo == TipoCliente.PessoaFisica)
                    {
                        <label>CPF <InputText @bind-Value="_form.Documento" /><ValidationMessage For="() => _form.Documento" /></label>
                        <label>RG <InputText @bind-Value="_form.Rg" /></label>
                        <label>Data de nascimento <InputDate @bind-Value="_form.DataNascimento" /></label>
                    }
                    else
                    {
                        <label>CNPJ <InputText @bind-Value="_form.Documento" /><ValidationMessage For="() => _form.Documento" /></label>
                        <label>Nome fantasia <InputText @bind-Value="_form.NomeFantasia" /></label>
                        <label>Responsável <InputText @bind-Value="_form.NomeResponsavel" /></label>
                        <label>Inscrição estadual <InputText @bind-Value="_form.InscricaoEstadual" /></label>
                        <label>Inscrição municipal <InputText @bind-Value="_form.InscricaoMunicipal" /></label>
                    }

                    <label>E-mail <InputText @bind-Value="_form.Email" type="email" /></label>
                    <label>Telefone <InputText @bind-Value="_form.Telefone" /></label>
                </div>

                <div class="grade">
                    <label>Logradouro <InputText @bind-Value="_form.Logradouro" /></label>
                    <label>Número <InputText @bind-Value="_form.Numero" /></label>
                    <label>Complemento <InputText @bind-Value="_form.Complemento" /></label>
                    <label>Bairro <InputText @bind-Value="_form.Bairro" /></label>
                    <label>Cidade <InputText @bind-Value="_form.Cidade" /></label>
                    <label>UF <InputText @bind-Value="_form.Uf" maxlength="2" /></label>
                    <label>CEP <InputText @bind-Value="_form.Cep" /></label>
                </div>

                <label>Observações <InputTextArea @bind-Value="_form.Observacoes" rows="3" /></label>

                <div class="acoes">
                    <button type="submit">Salvar</button>
                    <button type="button" class="secundario" @onclick="Fechar">Cancelar</button>
                </div>
            </EditForm>
        }
    }
    else if (_ficha is not null)
    {
        <section class="ficha">
            <h2>@_ficha.Nome</h2>
            <dl>
                @if (_ficha.Tipo == TipoCliente.PessoaFisica)
                {
                    <dt>CPF</dt><dd>@_ficha.Cpf</dd>
                    <dt>RG</dt><dd>@_ficha.Rg</dd>
                    <dt>Data de nascimento</dt><dd>@_ficha.DataNascimento?.ToString("dd/MM/yyyy")</dd>
                }
                else
                {
                    <dt>CNPJ</dt><dd>@_ficha.Cnpj</dd>
                    <dt>Nome fantasia</dt><dd>@_ficha.NomeFantasia</dd>
                    <dt>Responsável</dt><dd>@_ficha.NomeResponsavel</dd>
                    <dt>Inscrição estadual</dt><dd>@_ficha.InscricaoEstadual</dd>
                    <dt>Inscrição municipal</dt><dd>@_ficha.InscricaoMunicipal</dd>
                }
                <dt>E-mail</dt><dd>@_ficha.Email</dd>
                <dt>Telefone</dt><dd>@_ficha.Telefone</dd>
                <dt>Endereço</dt>
                <dd>@_ficha.Logradouro @_ficha.Numero @_ficha.Complemento — @_ficha.Bairro — @_ficha.Cidade/@_ficha.Uf — @_ficha.Cep</dd>
                <dt>Observações</dt><dd>@_ficha.Observacoes</dd>
                <dt>Situação</dt><dd>@(_ficha.Ativo ? "Ativo" : "Inativo")</dd>
            </dl>
            <div class="acoes"><button type="button" class="secundario" @onclick="Fechar">Fechar</button></div>
        </section>
    }
}

@code {
    private static readonly Pagina<ClienteResumo> Vazia = new([], 0, 1, ClientesOpcoes.TamanhoDaPagina);

    private bool _carregado;
    private bool _falhou;
    private string? _mensagem;
    private UsuarioAutenticado? _ator;
    private Busca _busca = new();
    private Pagina<ClienteResumo> _pagina = Vazia;
    private Formulario? _form;
    private ClienteDetalhe? _ficha;

    protected override async Task OnInitializedAsync()
    {
        _ator = await Despachante.EnviarAsync(new ObterMeuAcesso());
        if (_ator is not null) await CarregarAsync(1);
        _carregado = true;
    }

    private Task BuscarAsync() => CarregarAsync(1);

    private Task IrParaAsync(int pagina) => CarregarAsync(pagina);

    private async Task CarregarAsync(int pagina)
    {
        var resultado = await Despachante.EnviarAsync(
            new ListarClientes(_busca.Texto, _busca.IncluirInativos, pagina));

        if (resultado.Erro == TipoErro.AcessoNegado)
        {
            _ator = null;
            return;
        }

        _pagina = resultado.Valor ?? Vazia;
        if (!resultado.Sucesso) Avisar(resultado.Mensagem, falhou: true);
    }

    private void Novo(TipoCliente tipo)
    {
        _mensagem = null;
        _form = new Formulario { Tipo = tipo };
    }

    private async Task EditarAsync(Guid id)
    {
        var resultado = await Despachante.EnviarAsync(new ObterCliente(id));
        if (resultado.Valor is not { } c)
        {
            Avisar(resultado.Mensagem, falhou: true);
            return;
        }

        _mensagem = null;
        _form = new Formulario
        {
            Id = c.Id, Tipo = c.Tipo, Nome = c.Nome, Email = c.Email, Telefone = c.Telefone,
            Documento = c.Cpf ?? c.Cnpj ?? string.Empty, Rg = c.Rg, DataNascimento = c.DataNascimento,
            NomeFantasia = c.NomeFantasia, NomeResponsavel = c.NomeResponsavel,
            InscricaoEstadual = c.InscricaoEstadual, InscricaoMunicipal = c.InscricaoMunicipal,
            Logradouro = c.Logradouro, Numero = c.Numero, Complemento = c.Complemento, Bairro = c.Bairro,
            Cidade = c.Cidade, Uf = c.Uf, Cep = c.Cep, Observacoes = c.Observacoes,
        };
    }

    private async Task VerAsync(Guid id)
    {
        var resultado = await Despachante.EnviarAsync(new ObterCliente(id));
        _ficha = resultado.Valor;
        if (!resultado.Sucesso) Avisar(resultado.Mensagem, falhou: true);
    }

    private void Fechar()
    {
        _form = null;
        _ficha = null;
    }

    private string Titulo() => (_form!.Id is null ? "Nova " : "Editar ") +
        (_form.Tipo == TipoCliente.PessoaFisica ? "pessoa física" : "pessoa jurídica");

    private async Task SalvarAsync()
    {
        var f = _form!;
        var dados = new DadosDoClienteEntrada(
            f.Nome, f.Email, f.Telefone,
            new EnderecoEntrada(f.Logradouro, f.Numero, f.Complemento, f.Bairro, f.Cidade, f.Uf, f.Cep),
            f.Observacoes);

        var resultado = f.Tipo == TipoCliente.PessoaFisica
            ? await Despachante.EnviarAsync(new SalvarClientePessoaFisica(f.Id, dados, f.Documento, f.Rg, f.DataNascimento))
            : await Despachante.EnviarAsync(new SalvarClientePessoaJuridica(
                f.Id, dados, f.Documento, f.NomeFantasia, f.NomeResponsavel, f.InscricaoEstadual, f.InscricaoMunicipal));

        if (!resultado.Sucesso)
        {
            // O formulário permanece aberto com o que foi digitado.
            Avisar(resultado.Mensagem, falhou: true);
            return;
        }

        _form = null;
        await CarregarAsync(_pagina.Numero);
        Avisar("Cliente salvo.", falhou: false);
    }

    private async Task AlternarAtivoAsync(ClienteResumo cliente)
    {
        var resultado = await Despachante.EnviarAsync(new DefinirAtivoCliente(cliente.Id, !cliente.Ativo));
        await CarregarAsync(_pagina.Numero);
        Avisar(resultado.Sucesso ? (cliente.Ativo ? "Cliente inativado." : "Cliente ativado.") : resultado.Mensagem,
            falhou: !resultado.Sucesso);
    }

    private void Avisar(string? mensagem, bool falhou)
    {
        _mensagem = mensagem;
        _falhou = falhou;
    }

    private sealed class Busca
    {
        public string? Texto { get; set; }
        public bool IncluirInativos { get; set; }
    }

    private sealed class Formulario
    {
        public Guid? Id { get; set; }
        public TipoCliente Tipo { get; set; }

        [Required(ErrorMessage = "Informe o nome.")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe o documento.")]
        public string Documento { get; set; } = string.Empty;

        public string? Email { get; set; }
        public string? Telefone { get; set; }
        public string? Rg { get; set; }
        public DateOnly? DataNascimento { get; set; }
        public string? NomeFantasia { get; set; }
        public string? NomeResponsavel { get; set; }
        public string? InscricaoEstadual { get; set; }
        public string? InscricaoMunicipal { get; set; }
        public string? Logradouro { get; set; }
        public string? Numero { get; set; }
        public string? Complemento { get; set; }
        public string? Bairro { get; set; }
        public string? Cidade { get; set; }
        public string? Uf { get; set; }
        public string? Cep { get; set; }
        public string? Observacoes { get; set; }
    }
}
```

- [ ] **Step 4: Rodar e confirmar que passa**

Run: `dotnet test`
Expected: tudo aprovado, zero avisos.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat(web): tela de clientes com busca, ficha de leitura e cadastro pela Diretoria"
```
