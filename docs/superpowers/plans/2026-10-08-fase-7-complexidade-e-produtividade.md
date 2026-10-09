# Fase 7 — Complexidade de atividades e produtividade dos produtores: Plano de Implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Toda atividade passa a ter uma complexidade (1, 2 ou 3), e a Diretoria ganha uma tela que lista os produtores e mostra, para cada um, as atividades realizadas no mês, separadas por complexidade.

**Architecture:** `Complexidade` é um inteiro validado pelo agregado `Atividade`, informado junto com título, descrição e data de entrega. A produtividade não tem tabela: é uma consulta sobre o estado atual (atividade `Concluida`, `ConcluidaEm` no período, produtor entre os responsáveis), agrupada por complexidade no caso de uso. A tela converte o mês escolhido em um intervalo UTC, como a agenda faz com a semana, e abre o painel na própria página.

**Tech Stack:** .NET 10, Blazor Interactive Server, EF Core 10 + Npgsql, PostgreSQL 17, xUnit 2.9, FluentAssertions 7.2, Testcontainers.

**Spec:** `docs/superpowers/specs/2026-10-08-complexidade-e-produtividade-design.md`. Complementa `docs/superpowers/specs/2026-10-06-modelagem-dominio-design.md`.

## Decisões deste plano

- **`Complexidade` em `SalvarAtividade` é obrigatória e não tem valor padrão.** Por isso entra antes do parâmetro opcional `VersaoEsperada`, e não por último. O compilador aponta cada chamada que precisa informá-la. É a única exceção à regra "membro posicional novo entra por último".
- **Formulário sem escolha manda 0.** O seletor começa em "Selecione…" (valor 0). Não há validação própria na tela: o domínio recusa o 0 e a mensagem dele aparece no formulário.
- **A migration preenche e depois retira o valor padrão.** A coluna nasce com `DEFAULT 1` para as linhas existentes e o padrão é removido no mesmo `Up`. Um valor padrão no modelo faria o EF trocar um 0 por 1 em silêncio.
- **Sem índice novo.** A consulta parte do índice que já existe em `atividades_responsaveis.usuario_id`.
- **A lista de produtores reutiliza `ListarUsuarios`.** A página filtra os de tipo `Producao`. Não há caso de uso novo para a lista.
- **Ordem das recusas da consulta:** acesso negado, depois período inválido, depois produtor não encontrado.
- **Quem não é produtor responde `NaoEncontrado` sem evento de acesso negado.** A Diretoria pode ver qualquer produtor; o identificador pedido é que não é de um.
- **Painéis que só abrem por clique passam a ter teste.** Eles são desenhados fora da página com `HtmlRenderer`, que é do próprio ASP.NET Core (nenhum pacote novo), e um despachante de teste que executa os casos de uso contra o banco do teste. Cliques continuam sem teste automatizado e são conferidos por leitura.
- **A ficha da atividade não entra nesse desenho de teste.** Ela contém um `InputFile`, que pede um serviço de JavaScript inexistente fora da página. A linha de complexidade da ficha é conferida por leitura.
- **Na spec de domínio, "Produtividade" entra como subseção da seção 8.** Uma seção numerada nova renumeraria as seções que os planos anteriores citam.

## Como ler este plano

Cada tarefa traz os arquivos, o que ela consome e produz, e os passos com o código. Os trechos de arquivos existentes mostram só o que muda. Nunca apague um teste nem afrouxe uma asserção. Onde um passo manda corrigir chamadas, a correção é mecânica e está descrita no próprio passo.

Os testes de integração sobem um PostgreSQL em contêiner: o Docker precisa estar rodando.

Comandos usados ao longo do plano:

```bash
dotnet tool restore                                  # uma vez, para ter o dotnet-ef
dotnet build                                         # precisa terminar com zero avisos
dotnet test tests/AtividadesJuridicas.UnitTests      # unitários, sem banco
dotnet test tests/AtividadesJuridicas.IntegrationTests
dotnet test                                          # tudo
```

## Estado de partida

- Branch `feat/fase-7-produtividade`, criada a partir da `main` (fase 6 concluída), com a spec desta fase commitada.
- Domínio: `Atividade.Criar(DadosDaAtividade, VinculoDaAtividade, TipoAutor, Guid?, DateTime)` e `Atividade.Alterar(DadosDaAtividade, VinculoDaAtividade)`; `ConcluidaEm` é preenchido ao entrar em `Concluida` e limpo ao reabrir.
- Aplicação: `SalvarAtividade`, `ObterAtividade`, `ListarAtividades`; `IAtividadeRepositorio`; `IUsuarioRepositorio.ObterPorIdAsync`; `IPerfilRepositorio.ObterPorIdAsync`; `ListarUsuarios` (só Diretoria) devolve `UsuarioResumo` com `TipoPerfil` e `Ativo`.
- Web: `Atividades.razor` + `FichaDaAtividade.razor` + `FormularioDeAtividade.razor`; `FusoDoEscritorio` (`ParaLocal`, `InicioDoDiaEmUtc`, `Formatar`); `RotulosDeAgenda` é o modelo de classe estática de textos testada sem página.
- Antes de começar, rode `dotnet test` e anote as contagens: tudo precisa estar verde.

## Global Constraints

- TFM único `net10.0` em `Directory.Build.props`. `Nullable` e `TreatWarningsAsErrors=true`. Versões de pacote só em `Directory.Packages.props`. Nenhum pacote novo.
- `Domain` não referencia nada. `Application` não referencia EF Core, Npgsql, `Infrastructure` nem `Web`.
- Dentro da `Application`, `Atividades` usa `Identidade` e `Cadastros` e não usa `Agenda`.
- Código, nomes de tabela e mensagens em português. Tabelas e colunas em `snake_case`. Datas e horas em UTC no domínio, na aplicação e no banco.
- Toda propriedade mapeada nova é classificada em `PropriedadesAuditadasTests`. Nenhuma gravação por fora dos interceptores em `src` (as migrations são a exceção já prevista).
- Autorização verificada no manipulador; a interface apenas oculta. Consultas são `IConsultaAuditavel`.
- Um caso de uso por arquivo. Novas dependências de construtor e novos membros posicionais de registros entram por último, com a exceção declarada acima.
- Complexidade: inteiro, valores aceitos 1, 2 e 3. Mensagem do domínio: `A complexidade deve ser 1, 2 ou 3.`
- Atividades já existentes recebem complexidade 1 na migration.
- Consulta de produtividade: período `[DeUtc, AteUtc)` de no máximo 92 dias; recurso `Produtividade`, ação `Visualizou`.
- Rota `/produtores`. Item de menu "Produtores" no grupo da Diretoria, depois de "Usuários".
- Textos de tela, exatamente: `Produtores`, `Acompanhar produtividade`, `Produtividade — ` seguido do nome, `Complexidade 1`, `Complexidade 2`, `Complexidade 3`, `Total`, `Nenhuma atividade.`, `Acesso negado.`, `Tentar de novo`, `Selecione…`, `Concluída em`.
- Todo texto que um teste procura no HTML é escrito por **uma única** expressão Razor ou por marcação fixa. Texto fixo misturado com expressão na mesma célula pode vir separado por marcadores do Blazor.

## Review Focus

1. **Atividade concluída na última meia hora do mês, no horário do escritório** (30/09 23:30 em São Paulo já é 01/10 em UTC): conta em setembro, não em outubro. (Tarefas 3 e 4)
2. **Formulário salvo sem escolher a complexidade**: a gravação é recusada com a mensagem do domínio e nada é gravado. (Tarefas 1 e 2)
3. **Produtor que nunca concluiu nada**: os três grupos aparecem com zero, o seletor traz só o mês corrente e nada quebra. (Tarefas 3 e 4)
4. **Atividade com dois produtores**: aparece inteira para cada um, uma vez só. (Tarefa 3)
5. **Produtor inativado, e produtor que virou Diretoria**: o inativo continua consultável; quem mudou de tipo responde como inexistente. (Tarefa 3)
6. **Complexidade alterada depois da conclusão, e atividade reaberta**: a primeira muda de grupo; a segunda sai do mês e volta no mês da nova conclusão. (Tarefas 1 e 3)
7. **Banco que já tinha atividades**: a migration não falha e as deixa com complexidade 1. (Tarefa 1)
8. **Virada de ano e mês com mudança de horário** na lista de meses e no intervalo. (Tarefa 4)

---

### Task 1: Complexidade na atividade — domínio, casos de uso, banco e formulário

O formulário entra nesta tarefa porque a assinatura de `SalvarAtividade` muda e a solução precisa compilar.

**Files:**
- Modify: `src/AtividadesJuridicas.Domain/Atividades/Atividade.cs`
- Modify: `src/AtividadesJuridicas.Domain/Atividades/TiposDaAtividade.cs`
- Modify: `src/AtividadesJuridicas.Application/Atividades/SalvarAtividade.cs`
- Modify: `src/AtividadesJuridicas.Application/Atividades/PortasDeAtividades.cs`
- Modify: `src/AtividadesJuridicas.Application/Atividades/ObterAtividade.cs`
- Modify: `src/AtividadesJuridicas.Infrastructure/Persistencia/Repositorios/AtividadeRepositorio.cs`
- Create: `src/AtividadesJuridicas.Infrastructure/Persistencia/Migrations/<data>_ComplexidadeDaAtividade.cs` (gerada; mais o `.Designer.cs` e o `AppDbContextModelSnapshot.cs` atualizado)
- Modify: `src/AtividadesJuridicas.Web/Components/Atividades/FormularioDeAtividade.razor`
- Test: `tests/AtividadesJuridicas.UnitTests/Domain/AtividadeTests.cs`
- Test: `tests/AtividadesJuridicas.UnitTests/Application/AtividadesTests.cs`
- Test: `tests/AtividadesJuridicas.UnitTests/Application/Falsos.cs`
- Test: `tests/AtividadesJuridicas.IntegrationTests/AtividadesTests.cs`
- Test: `tests/AtividadesJuridicas.IntegrationTests/PropriedadesAuditadasTests.cs`
- Create: `tests/AtividadesJuridicas.IntegrationTests/MigracaoDeComplexidadeTests.cs`
- Modify: os demais arquivos de teste que constroem `SalvarAtividade` ou `DadosDaAtividade` (lista nos passos 7 e 9)

**Interfaces:**
- Consumes: nada de tarefas anteriores.
- Produces:
  - `Atividade.ComplexidadeMinima` (`const int` = 1), `Atividade.ComplexidadeMaxima` (`const int` = 3), `Atividade.ComplexidadeInvalida` (`const string`), `int Atividade.Complexidade { get; }`
  - `record DadosDaAtividade(string Titulo, string Descricao, DateOnly? DataEntrega, int Complexidade)`
  - `record SalvarAtividade(Guid? Id, string Titulo, string Descricao, DateOnly? DataEntrega, Guid? ClienteId, Guid? ProcessoId, Guid? AreaAtuacaoId, int Complexidade, uint? VersaoEsperada = null)`
  - `AtividadeResumo` e `AtividadeDetalhe` ganham, por último, `int Complexidade`

- [ ] **Step 1: Testes de domínio**

Em `tests/AtividadesJuridicas.UnitTests/Domain/AtividadeTests.cs`, troque o auxiliar `Dados` por:

```csharp
    private static DadosDaAtividade Dados(
        string titulo = "Elaborar contestação", DateOnly? entrega = null, bool semEntrega = false, int complexidade = 1) =>
        new(titulo, "Prazo de 15 dias.", semEntrega ? null : entrega ?? Entrega, complexidade);
```

Nas quatro construções diretas de `DadosDaAtividade` do arquivo (testes `Titulo_e_descricao_sao_obrigatorios` e `Titulo_e_descricao_tem_limite_e_sao_aparados`), acrescente `1` como quarto argumento. Exemplo: `new DadosDaAtividade(titulo, descricao, null, 1)`.

Acrescente ao final da classe:

```csharp
    // ---- Complexidade ----

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Complexidade_de_1_a_3_e_aceita_na_criacao(int complexidade)
    {
        var atividade = Atividade.Criar(
            Dados(complexidade: complexidade), VinculoDaAtividade.Avulsa, TipoAutor.Usuario, _diana.UsuarioId, T0);

        atividade.Complexidade.Should().Be(complexidade);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(-1)]
    public void Complexidade_fora_de_1_a_3_e_recusada_na_criacao_e_na_edicao(int complexidade)
    {
        var criar = () => Atividade.Criar(
            Dados(complexidade: complexidade), VinculoDaAtividade.Avulsa, TipoAutor.Usuario, _diana.UsuarioId, T0);
        var existente = Nova();
        var alterar = () => existente.Alterar(Dados(complexidade: complexidade), VinculoDaAtividade.Avulsa);

        criar.Should().Throw<ExcecaoDeDominio>().WithMessage("A complexidade deve ser 1, 2 ou 3.");
        alterar.Should().Throw<ExcecaoDeDominio>().WithMessage("A complexidade deve ser 1, 2 ou 3.");
        existente.Complexidade.Should().Be(1);
    }

    [Fact]
    public void Complexidade_pode_ser_alterada_depois_de_concluida_sem_mexer_no_status()
    {
        var atividade = Concluida();
        var concluidaEm = atividade.ConcluidaEm;

        atividade.Alterar(Dados(complexidade: 3), VinculoDaAtividade.Avulsa);

        atividade.Complexidade.Should().Be(3);
        atividade.Status.Should().Be(StatusAtividade.Concluida);
        atividade.ConcluidaEm.Should().Be(concluidaEm);
    }
```

- [ ] **Step 2: Testes de aplicação**

Em `tests/AtividadesJuridicas.UnitTests/Application/AtividadesTests.cs`, troque o auxiliar `Salvar` por:

```csharp
    private Task<Resultado<Guid>> Salvar(Usuario? ator, Guid? id = null, string titulo = "Elaborar contestação",
        DateOnly? entrega = null, bool semEntrega = false, Guid? cliente = null, Guid? processo = null, Guid? area = null,
        int complexidade = 1) =>
        new SalvarAtividadeManipulador(_c.Como(ator), _c.Atividades, _c.Clientes, _c.Processos, _c.Areas, _c.Relogio, _c.Unidade, _c.Documentos)
            .ManipularAsync(new SalvarAtividade(
                id, titulo, "Prazo de 15 dias.", semEntrega ? null : entrega ?? Entrega, cliente, processo, area, complexidade), default);
```

Acrescente ao final da classe:

```csharp
    // ---- Complexidade ----

    [Fact]
    public async Task Salvar_grava_a_complexidade_e_obter_e_listar_a_trazem()
    {
        var id = (await Salvar(Diana, complexidade: 3)).Valor;

        NoRepositorio(id).Complexidade.Should().Be(3);
        (await Obter(Diana, id)).Valor!.Complexidade.Should().Be(3);
        (await Listar(Diana)).Valor!.Itens.Single().Complexidade.Should().Be(3);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public async Task Salvar_sem_complexidade_valida_e_recusado_e_nada_e_gravado(int complexidade)
    {
        var resultado = await Salvar(Diana, complexidade: complexidade);

        resultado.Erro.Should().Be(TipoErro.Validacao);
        resultado.Mensagem.Should().Be("A complexidade deve ser 1, 2 ou 3.");
        _c.Atividades.Itens.Should().BeEmpty();
        _c.Unidade.Salvamentos.Should().Be(0);
    }

    [Fact]
    public async Task Editar_troca_a_complexidade_inclusive_de_atividade_concluida()
    {
        var id = await FeitaAsync();
        (await Aprovar(Diana, id)).Sucesso.Should().BeTrue();

        var editada = await Salvar(Diana, id: id, complexidade: 2);

        editada.Sucesso.Should().BeTrue(editada.Mensagem);
        NoRepositorio(id).Complexidade.Should().Be(2);
        NoRepositorio(id).Status.Should().Be(StatusAtividade.Concluida);
    }
```

- [ ] **Step 3: Rodar e ver falhar**

Run: `dotnet test tests/AtividadesJuridicas.UnitTests`
Expected: FALHA de compilação — `DadosDaAtividade` não aceita quatro argumentos e `Atividade` não tem `Complexidade`.

- [ ] **Step 4: Domínio**

Em `src/AtividadesJuridicas.Domain/Atividades/TiposDaAtividade.cs`, troque o registro:

```csharp
public sealed record DadosDaAtividade(string Titulo, string Descricao, DateOnly? DataEntrega, int Complexidade);
```

Em `src/AtividadesJuridicas.Domain/Atividades/Atividade.cs`:

Depois de `public const int TamanhoMaximoDescricao = 10000;`:

```csharp
    public const int ComplexidadeMinima = 1;
    public const int ComplexidadeMaxima = 3;
    public const string ComplexidadeInvalida = "A complexidade deve ser 1, 2 ou 3.";
```

Depois da propriedade `DataEntrega`:

```csharp
    /// <summary>Nível de dificuldade: de <see cref="ComplexidadeMinima"/> a <see cref="ComplexidadeMaxima"/>.</summary>
    public int Complexidade { get; private set; }
```

No método `Aplicar`, depois de `DataEntrega = dados.DataEntrega;`:

```csharp
        Complexidade = dados.Complexidade;
```

No método `Validar`, imediatamente antes da verificação da data de entrega (`if (status != StatusAtividade.Criada && dados.DataEntrega is null)`):

```csharp
        if (dados.Complexidade is < ComplexidadeMinima or > ComplexidadeMaxima)
            throw new ExcecaoDeDominio(ComplexidadeInvalida);
```

- [ ] **Step 5: Aplicação**

Em `src/AtividadesJuridicas.Application/Atividades/SalvarAtividade.cs`, o registro passa a ser:

```csharp
public sealed record SalvarAtividade(
    Guid? Id, string Titulo, string Descricao, DateOnly? DataEntrega, Guid? ClienteId, Guid? ProcessoId, Guid? AreaAtuacaoId,
    int Complexidade, uint? VersaoEsperada = null)
    : IRequisicao<Resultado<Guid>>;
```

e, no manipulador, a linha que monta os dados:

```csharp
            var dados = new DadosDaAtividade(
                requisicao.Titulo, requisicao.Descricao, requisicao.DataEntrega, requisicao.Complexidade);
```

Em `src/AtividadesJuridicas.Application/Atividades/PortasDeAtividades.cs`, os dois registros ganham o último membro:

```csharp
public sealed record AtividadeResumo(
    Guid Id, string Titulo, StatusAtividade Status, DateOnly? DataEntrega, bool Atrasada, string? ClienteNome,
    string? ProcessoTitulo, string? AreaNome, IReadOnlyList<string> Responsaveis, string? RevisorNome, int Complexidade);
```

```csharp
public sealed record AtividadeDetalhe(
    Guid Id, string Titulo, string Descricao, DateOnly? DataEntrega, StatusAtividade Status, bool Atrasada,
    DateTime? ConcluidaEm, Guid? ClienteId, string? ClienteNome, Guid? ProcessoId, string? ProcessoTitulo,
    Guid? AreaAtuacaoId, string? AreaNome, TipoAutor TipoAutor, string? AutorNome, Guid? RevisorId, string? RevisorNome,
    IReadOnlyList<ResponsavelDetalhe> Responsaveis, IReadOnlyList<HistoricoDetalhe> Historico, AcoesDaAtividade Acoes,
    uint Versao, int Complexidade);
```

Em `src/AtividadesJuridicas.Application/Atividades/ObterAtividade.cs`, o final da construção do detalhe:

```csharp
            Acoes(ator, atividade), atividade.Versao, atividade.Complexidade));
```

- [ ] **Step 6: Repositório falso**

Em `tests/AtividadesJuridicas.UnitTests/Application/Falsos.cs`, na construção de `AtividadeResumo` dentro de `AtividadeRepositorioFalso.ListarAsync`, a última linha passa a ser:

```csharp
                Nome(a.RevisorId), a.Complexidade))],
```

- [ ] **Step 7: Corrigir as chamadas nos testes unitários**

Em cada chamada abaixo, o argumento novo entra **depois do argumento da área** (o sétimo). Use `1`.

- `tests/AtividadesJuridicas.UnitTests/Application/AtividadesTests.cs`: as três chamadas com `VersaoEsperada:` nomeado. Exemplo: `new SalvarAtividade(id, "Outro título", "Outra descrição", Entrega, null, null, null, 1, VersaoEsperada: 1u)`.
- `tests/AtividadesJuridicas.UnitTests/Application/ComentariosTests.cs`: uma chamada.
- `tests/AtividadesJuridicas.UnitTests/Application/AgendaTests.cs`: quatro chamadas.
- `tests/AtividadesJuridicas.UnitTests/Application/DocumentosDeAtividadeTests.cs`: duas chamadas.
- `tests/AtividadesJuridicas.UnitTests/Application/ProcessosTests.cs`: uma construção de `DadosDaAtividade`, que ganha `1` como quarto argumento.

- [ ] **Step 8: Rodar os unitários**

Run: `dotnet test tests/AtividadesJuridicas.UnitTests`
Expected: PASS, incluindo os testes novos de complexidade em `Domain/AtividadeTests` e em `Application/AtividadesTests`.

- [ ] **Step 9: Testes de integração**

Em `tests/AtividadesJuridicas.IntegrationTests/PropriedadesAuditadasTests.cs`, a lista esperada de `Atividade` ganha `"Complexidade"`:

```csharp
            ["Atividade"] =
            [
                "AreaAtuacaoId", "AutorId", "ClienteId", "Complexidade", "ConcluidaEm", "DataEntrega", "Descricao",
                "ProcessoId", "RevisorId", "Status", "TipoAutor", "Titulo",
            ],
```

Em `tests/AtividadesJuridicas.IntegrationTests/AtividadesTests.cs`, troque os auxiliares `CriarAsync` e `EditarAsync`:

```csharp
    private async Task<Guid> CriarAsync(
        string titulo = "Elaborar contestação", DateOnly? entrega = null, Guid? cliente = null, Guid? processo = null,
        int complexidade = 1)
    {
        var criada = await ExecutarAsync(_diana, new SalvarAtividade(
            null, titulo, "Prazo de 15 dias.", entrega ?? Entrega, cliente, processo, null, complexidade));
        criada.Sucesso.Should().BeTrue(criada.Mensagem);
        return criada.Valor;
    }
```

```csharp
    private Task<Resultado<Guid>> EditarAsync(
        Guid id, string titulo, DateOnly? entrega = null, uint? versao = null, int complexidade = 1) =>
        ExecutarAsync(_diana, new SalvarAtividade(
            id, titulo, "Prazo de 15 dias.", entrega ?? Entrega, null, null, null, complexidade, versao));
```

Acrescente o teste (antes da classe interna `RelogioDeTeste`):

```csharp
    [Fact]
    public async Task Complexidade_vai_e_volta_do_banco_e_a_alteracao_fica_na_auditoria()
    {
        var id = await CriarAsync(complexidade: 2);

        (await ExecutarAsync(_diana, new ObterAtividade(id))).Valor!.Complexidade.Should().Be(2);
        (await ExecutarAsync(_diana, new ListarAtividades())).Valor!.Itens.Single().Complexidade.Should().Be(2);

        var editada = await EditarAsync(id, "Elaborar contestação", complexidade: 3);

        editada.Sucesso.Should().BeTrue(editada.Mensagem);
        (await AtividadeNoBancoAsync(id)).Complexidade.Should().Be(3);
        var auditoria = await AuditoriaAsync();
        var criacao = auditoria.Single(r => r.Entidade == "Atividade" && r.EntidadeId == id
            && r.Operacao == OperacaoAuditada.Criacao);
        DadosDe(criacao).GetProperty("Complexidade").GetInt32().Should().Be(2);
        var alteracao = auditoria.Single(r => r.Entidade == "Atividade" && r.EntidadeId == id
            && r.Operacao == OperacaoAuditada.Alteracao);
        DadosDe(alteracao).GetProperty("Complexidade").GetProperty("de").GetInt32().Should().Be(2);
        DadosDe(alteracao).GetProperty("Complexidade").GetProperty("para").GetInt32().Should().Be(3);
    }
```

Crie `tests/AtividadesJuridicas.IntegrationTests/MigracaoDeComplexidadeTests.cs`:

```csharp
using AtividadesJuridicas.Application;
using AtividadesJuridicas.Infrastructure;
using AtividadesJuridicas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>A migration da complexidade aplicada a um banco que já tinha atividades.</summary>
[Collection(ColecaoBanco.Nome)]
public class MigracaoDeComplexidadeTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly string _conexao = banco.NovaConexao();
    private readonly string _pasta = Path.Combine(Path.GetTempPath(), "atividades-testes", Guid.NewGuid().ToString("N"));
    private ServiceProvider _raiz = null!;

    public Task InitializeAsync()
    {
        _raiz = new ServiceCollection()
            .AddApplication()
            .AddInfrastructure(_ => _conexao, _ => _pasta, criarPastaDeArquivos: true)
            .BuildServiceProvider();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        try
        {
            await _raiz.DisposeAsync();
            if (Directory.Exists(_pasta)) Directory.Delete(_pasta, recursive: true);
        }
        finally
        {
            await banco.RemoverAsync(_conexao);
        }
    }

    [Fact]
    public async Task Atividade_que_ja_existia_recebe_complexidade_1_e_a_coluna_nao_guarda_valor_padrao()
    {
        var id = Guid.CreateVersion7();

        await using (var escopo = _raiz.CreateAsyncScope())
        {
            var migrador = escopo.ServiceProvider.GetRequiredService<AppDbContext>().GetService<IMigrator>();

            // O banco como estava antes desta funcionalidade, já com uma atividade gravada.
            await migrador.MigrateAsync("IndicesDeAuditoria");
            await ComandoAsync(
                "INSERT INTO atividades (id, titulo, descricao, status, tipo_autor, criado_em) " +
                "VALUES (@id, 'Antiga', 'Criada antes da complexidade.', 1, 2, now())", id);

            await migrador.MigrateAsync();
        }

        (await ComandoAsync("SELECT complexidade FROM atividades WHERE id = @id", id)).Should().Be(1);
        (await ComandoAsync(
            "SELECT column_default FROM information_schema.columns " +
            "WHERE table_name = 'atividades' AND column_name = 'complexidade'")).Should().Be(DBNull.Value);
    }

    private async Task<object?> ComandoAsync(string sql, Guid? id = null)
    {
        await using var conexao = new NpgsqlConnection(_conexao);
        await conexao.OpenAsync();
        await using var comando = conexao.CreateCommand();
        comando.CommandText = sql;
        if (id is { } valor) comando.Parameters.AddWithValue("id", valor);
        return await comando.ExecuteScalarAsync();
    }
}
```

Corrija as demais chamadas de `SalvarAtividade` nos testes de integração. O argumento novo entra depois do da área; use `1`. **Onde a versão era passada por posição como oitavo argumento, ela passa a ser o nono.**

- `AgendaTests.cs` (1), `AtividadesWebTests.cs` (2), `AtividadesTests.cs` (as que restarem fora dos dois auxiliares: 2), `ComentariosTests.cs` (2), `DocumentosTests.cs` (1), `DocumentosDeAtividadeTests.cs` (7), `DocumentosWebTests.cs` (1), `PendenciasEAuditoriaPorRegistroTests.cs` (1), `ProcessosEAtividadesTests.cs` (1).

- [ ] **Step 10: Repositório e formulário**

Em `src/AtividadesJuridicas.Infrastructure/Persistencia/Repositorios/AtividadeRepositorio.cs`, na construção de `AtividadeResumo`, a última linha passa a ser:

```csharp
            Nome(usuarios, a.RevisorId), a.Complexidade)).ToList();
```

A propriedade é mapeada por convenção (coluna `complexidade`, `integer`, obrigatória): `AtividadeConfiguracao` não muda.

Em `src/AtividadesJuridicas.Web/Components/Atividades/FormularioDeAtividade.razor`:

A `div` com classe `grade` passa a ser:

```razor
            <div class="grade">
                <label>Título <input type="text" maxlength="@Atividade.TamanhoMaximoTitulo" @bind="_titulo" /></label>
                <label>Data de entrega <input type="date" @bind="_dataEntrega" /></label>
                <label>
                    Complexidade
                    <select @bind="_complexidade">
                        <option value="0">Selecione…</option>
                        @for (var nivel = Atividade.ComplexidadeMinima; nivel <= Atividade.ComplexidadeMaxima; nivel++)
                        {
                            <option value="@nivel">@nivel</option>
                        }
                    </select>
                </label>
            </div>
```

Nos campos, depois de `private DateOnly? _dataEntrega;`:

```csharp
    // 0 é "ainda não escolhida": o caso de uso recusa e a mensagem dele aparece no formulário.
    private int _complexidade;
```

Em `OnInitializedAsync`, depois de `_dataEntrega = atual.DataEntrega;`:

```csharp
            _complexidade = atual.Complexidade;
```

Em `SalvarAsync`, a chamada passa a ser:

```csharp
            var resultado = await Despachante.EnviarAsync(new SalvarAtividade(
                AtividadeId, _titulo, _descricao, _dataEntrega,
                _clienteId == Guid.Empty ? null : _clienteId,
                _processo?.Id,
                // Com processo, a área é a do processo (o caso de uso a ignora); sem processo, a escolhida.
                _processo is not null || _areaId == Guid.Empty ? null : _areaId,
                _complexidade,
                // Na edição, a versão com que o formulário foi aberto; atividade nova não tem.
                AtividadeId is null ? null : Atual?.Versao));
```

- [ ] **Step 11: Migration**

Run:

```bash
dotnet tool restore
dotnet ef migrations add ComplexidadeDaAtividade --project src/AtividadesJuridicas.Infrastructure --startup-project src/AtividadesJuridicas.Infrastructure --output-dir Persistencia/Migrations
```

No arquivo gerado `<data>_ComplexidadeDaAtividade.cs`, substitua os corpos de `Up` e `Down` por:

```csharp
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // As atividades que já existem recebem complexidade 1. O valor padrão serve só a esse preenchimento e é
            // retirado em seguida: toda atividade nova informa a sua.
            migrationBuilder.AddColumn<int>(
                name: "complexidade",
                table: "atividades",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql("ALTER TABLE atividades ALTER COLUMN complexidade DROP DEFAULT;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "complexidade",
                table: "atividades");
        }
```

A migration deve conter **só** a coluna `complexidade`. Se o EF tiver emitido qualquer outra operação (uma linha de `xmin`, por exemplo), apague-a e registre no relatório. Não edite à mão o `.Designer.cs` nem o snapshot.

Run: `dotnet ef migrations has-pending-model-changes --project src/AtividadesJuridicas.Infrastructure --startup-project src/AtividadesJuridicas.Infrastructure`
Expected: informa que não há mudanças pendentes no modelo.

- [ ] **Step 12: Rodar tudo**

Run: `dotnet build`
Expected: zero avisos, zero erros.

Run: `dotnet test`
Expected: PASS em tudo, incluindo `Complexidade_vai_e_volta_do_banco_e_a_alteracao_fica_na_auditoria`, `Atividade_que_ja_existia_recebe_complexidade_1_e_a_coluna_nao_guarda_valor_padrao` e `Cada_entidade_audita_exatamente_as_propriedades_esperadas`.

- [ ] **Step 13: Commit**

```bash
git add src tests
git commit -m "feat(atividades): complexidade obrigatória de 1 a 3 no domínio, nos casos de uso e no banco"
```

---

### Task 2: Complexidade nas telas de atividade

**Files:**
- Modify: `src/AtividadesJuridicas.Web/Components/Pages/Atividades.razor`
- Modify: `src/AtividadesJuridicas.Web/Components/Atividades/FichaDaAtividade.razor`
- Create: `tests/AtividadesJuridicas.IntegrationTests/TelaDeTeste.cs`
- Test: `tests/AtividadesJuridicas.IntegrationTests/AtividadesWebTests.cs`

**Interfaces:**
- Consumes: `AtividadeResumo.Complexidade`, `AtividadeDetalhe.Complexidade`, `SalvarAtividade(..., int Complexidade, uint? VersaoEsperada = null)` e o campo "Complexidade" de `FormularioDeAtividade` (Tarefa 1).
- Produces: `static Task<string> TelaDeTeste.RenderizarAsync<TComponente>(AplicacaoDeTeste app, Guid usuarioId, Dictionary<string, object?> parametros) where TComponente : IComponent` — desenha um componente até ele terminar de carregar e devolve o HTML. A Tarefa 4 usa.

- [ ] **Step 1: Auxiliar de desenho de componente**

Crie `tests/AtividadesJuridicas.IntegrationTests/TelaDeTeste.cs`:

```csharp
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Web.Infra;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// Desenha um componente fora da página, até ele terminar de carregar, e devolve o HTML. Serve aos painéis que só
/// abrem por clique e por isso não aparecem no HTML pré-renderizado. Os casos de uso rodam de verdade, contra o banco
/// do teste, como o usuário informado. Cliques ficam fora do alcance: só o primeiro desenho é verificado.
/// </summary>
public static class TelaDeTeste
{
    public static async Task<string> RenderizarAsync<TComponente>(
        AplicacaoDeTeste app, Guid usuarioId, Dictionary<string, object?> parametros)
        where TComponente : IComponent
    {
        // Só o que os componentes da aplicação injetam: o despachante e o fuso do escritório.
        await using var servicos = new ServiceCollection()
            .AddSingleton<IDespachante>(new DespachanteDeTeste(app, usuarioId))
            .AddSingleton(new FusoDoEscritorio("America/Sao_Paulo"))
            .BuildServiceProvider();
        await using var renderizador = new HtmlRenderer(servicos, NullLoggerFactory.Instance);

        return await renderizador.Dispatcher.InvokeAsync(async () =>
            (await renderizador.RenderComponentAsync<TComponente>(ParameterView.FromDictionary(parametros))).ToHtmlString());
    }

    private sealed class DespachanteDeTeste(AplicacaoDeTeste app, Guid usuarioId) : IDespachante
    {
        public Task<TResposta> EnviarAsync<TResposta>(IRequisicao<TResposta> requisicao, CancellationToken ct = default) =>
            app.ExecutarAsync(usuarioId, requisicao);
    }
}
```

É a primeira vez que o repositório usa `HtmlRenderer`. Um componente que injete outro serviço (por exemplo `IJSRuntime`, que o `InputFile` pede) falha ao ser desenhado aqui: é por isso que `FichaDaAtividade` não é testada por este caminho.

- [ ] **Step 2: Testes de tela**

Em `tests/AtividadesJuridicas.IntegrationTests/AtividadesWebTests.cs`:

Acrescente os `using`:

```csharp
using System.Text.RegularExpressions;
using AtividadesJuridicas.Web.Components.Atividades;
```

Acrescente o campo `private Guid _contestacao;` e, em `InitializeAsync`, dê complexidades diferentes às duas atividades e guarde o id da primeira:

```csharp
        var contestacao = await _app.ExecutarAsync(_admin, new SalvarAtividade(
            null, "Elaborar contestação", "Prazo de 15 dias.", new DateOnly(2020, 1, 15), null, null, null, 2));
        contestacao.Sucesso.Should().BeTrue(contestacao.Mensagem);
        _contestacao = contestacao.Valor;
```

```csharp
        var interno = await _app.ExecutarAsync(_admin, new SalvarAtividade(
            null, "Recurso interno", "Sem responsáveis.", null, null, null, null, 3));
```

Acrescente ao final da classe:

```csharp
    // ---- Complexidade ----

    /// <summary>O seletor de complexidade do formulário: do rótulo até o fim do primeiro select depois dele.</summary>
    private static string SeletorDeComplexidade(string html)
    {
        var inicio = Regex.Match(html, @"Complexidade\s*<select");
        inicio.Success.Should().BeTrue("o formulário deve ter o campo Complexidade");
        return html[inicio.Index..html.IndexOf("</select>", inicio.Index, StringComparison.Ordinal)];
    }

    private static List<string> Opcoes(string select, bool soASelecionada = false) =>
        [.. Regex.Matches(select, soASelecionada
                ? @"<option[^>]*selected[^>]*>\s*([^<]*?)\s*</option>"
                : @"<option[^>]*>\s*([^<]*?)\s*</option>")
            .Select(m => m.Groups[1].Value)];

    [Fact]
    public async Task Lista_mostra_a_complexidade_de_cada_atividade()
    {
        var cliente = await _app.ClienteAutenticadoAsync();

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/atividades"));

        html.Should().MatchRegex(@"<th>\s*Complexidade\s*</th>");
        html.Should().MatchRegex(@"<td>\s*2\s*</td>").And.MatchRegex(@"<td>\s*3\s*</td>");
    }

    [Fact]
    public async Task Formulario_de_atividade_nova_pede_a_complexidade_e_comeca_sem_escolha()
    {
        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarAsync<FormularioDeAtividade>(
            _app, _admin, new() { [nameof(FormularioDeAtividade.AtividadeId)] = null }));

        var select = SeletorDeComplexidade(html);
        Opcoes(select).Should().Equal("Selecione…", "1", "2", "3");
        Opcoes(select, soASelecionada: true).Should().Equal("Selecione…");
    }

    [Fact]
    public async Task Formulario_de_edicao_abre_com_a_complexidade_da_atividade()
    {
        var atual = (await _app.ExecutarAsync(_admin, new ObterAtividade(_contestacao))).Valor!;

        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarAsync<FormularioDeAtividade>(_app, _admin, new()
        {
            [nameof(FormularioDeAtividade.AtividadeId)] = _contestacao,
            [nameof(FormularioDeAtividade.Atual)] = atual,
        }));

        Opcoes(SeletorDeComplexidade(html), soASelecionada: true).Should().Equal("2");
    }
```

- [ ] **Step 3: Rodar e ver falhar**

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --filter "FullyQualifiedName~AtividadesWebTests"`
Expected: `Lista_mostra_a_complexidade_de_cada_atividade` FALHA (não há coluna). Os dois testes de formulário PASSAM, porque o campo entrou na Tarefa 1: eles ficam como rede para ele. Se um deles falhar por serviço não registrado, a mensagem diz qual componente pediu qual serviço; pare e registre no relatório em vez de registrar serviços a mais no auxiliar.

- [ ] **Step 4: Lista e ficha**

Em `src/AtividadesJuridicas.Web/Components/Pages/Atividades.razor`:

O cabeçalho da tabela passa a ser:

```razor
                <tr><th>Título</th><th>Situação</th><th>Entrega</th><th>Complexidade</th><th>Cliente</th><th>Processo</th><th>Responsáveis</th><th>Revisor</th><th></th></tr>
```

e, depois da célula da entrega (`<td>@RotulosDeAtividade.Data(atividade.DataEntrega)</td>`):

```razor
                        <td>@atividade.Complexidade</td>
```

Em `src/AtividadesJuridicas.Web/Components/Atividades/FichaDaAtividade.razor`, depois da linha `<dt>Entrega</dt><dd>@RotulosDeAtividade.Data(a.DataEntrega)</dd>`:

```razor
            <dt>Complexidade</dt><dd>@a.Complexidade</dd>
```

- [ ] **Step 5: Rodar tudo**

Run: `dotnet build`
Expected: zero avisos.

Run: `dotnet test`
Expected: PASS.

Por leitura, confira e descreva no relatório: a ficha mostra a complexidade entre a entrega e o cliente; um responsável de Produção vê a coluna e a linha da ficha (ele vê a atividade).

- [ ] **Step 6: Commit**

```bash
git add src tests
git commit -m "feat(web): complexidade na lista e na ficha da atividade, com teste de tela do formulário"
```

---

### Task 3: Consulta de produtividade

**Files:**
- Modify: `src/AtividadesJuridicas.Application/Atividades/PortasDeAtividades.cs`
- Create: `src/AtividadesJuridicas.Application/Atividades/ObterProdutividadeDoProdutor.cs`
- Modify: `src/AtividadesJuridicas.Infrastructure/Persistencia/Repositorios/AtividadeRepositorio.cs`
- Test: `tests/AtividadesJuridicas.UnitTests/Application/Falsos.cs`
- Create: `tests/AtividadesJuridicas.UnitTests/Application/ProdutividadeTests.cs`
- Create: `tests/AtividadesJuridicas.IntegrationTests/ProdutividadeTests.cs`

**Interfaces:**
- Consumes: `Atividade.ComplexidadeMinima`, `Atividade.ComplexidadeMaxima`, `Atividade.Complexidade`, `SalvarAtividade(..., int Complexidade, uint? VersaoEsperada = null)` (Tarefa 1).
- Produces (namespace `AtividadesJuridicas.Application.Atividades`):
  - `static class ProdutividadeOpcoes { const int PeriodoMaximoEmDias = 92; const string ProdutorNaoEncontrado = "Produtor não encontrado."; }`
  - `record AtividadeRealizada(Guid Id, string Titulo, int Complexidade, DateTime ConcluidaEm, string? ClienteNome)`
  - `record GrupoDeComplexidade(int Complexidade, IReadOnlyList<AtividadeRealizada> Atividades)` com `int Total`
  - `record ProdutividadeDoProdutor(Guid ProdutorId, string ProdutorNome, IReadOnlyList<GrupoDeComplexidade> Grupos, DateTime? PrimeiraConclusaoEm)` com `int Total`
  - `record ObterProdutividadeDoProdutor(Guid ProdutorId, DateTime DeUtc, DateTime AteUtc) : IRequisicao<Resultado<ProdutividadeDoProdutor>>, IConsultaAuditavel`
  - `IAtividadeRepositorio.ListarRealizadasAsync(Guid responsavelId, DateTime deUtc, DateTime ateUtc, CancellationToken ct)` → `Task<IReadOnlyList<AtividadeRealizada>>`
  - `IAtividadeRepositorio.ObterPrimeiraConclusaoAsync(Guid responsavelId, CancellationToken ct)` → `Task<DateTime?>`

- [ ] **Step 1: Testes unitários**

Crie `tests/AtividadesJuridicas.UnitTests/Application/ProdutividadeTests.cs`:

```csharp
using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.UnitTests.Application;

public class ProdutividadeTests
{
    // Outubro de 2026 no fuso do escritório (UTC-3): de 01/10 00:00 a 01/11 00:00.
    private static readonly DateTime Outubro = new(2026, 10, 1, 3, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Novembro = new(2026, 11, 1, 3, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Entrega = new(2026, 12, 20);

    private readonly Cenario _c = new();
    private readonly Usuario _davi, _pedro, _paula;
    private readonly ClientePessoaFisica _maria;

    public ProdutividadeTests()
    {
        _davi = _c.NovoUsuario("Davi Diretor", "davi@x.com", _c.PerfilDiretor);
        _pedro = _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);
        _paula = _c.NovoUsuario("Paula", "paula@x.com", _c.PerfilProducao);
        _maria = ClientePessoaFisica.Criar(
            new DadosDoCliente("Maria Silva", null, null, Endereco.Vazio, null), Cpf.Criar("529.982.247-25"), null, null);
        _c.Clientes.Itens.Add(_maria);
    }

    private Usuario Diana => _c.Diretor;

    private static DateTime Em(int mes, int dia) => new(2026, mes, dia, 15, 0, 0, DateTimeKind.Utc);

    private Task<Resultado<ProdutividadeDoProdutor>> Obter(Usuario? ator, Guid produtor, DateTime? de = null, DateTime? ate = null) =>
        new ObterProdutividadeDoProdutorManipulador(_c.Como(ator), _c.Usuarios, _c.Perfis, _c.Atividades)
            .ManipularAsync(new ObterProdutividadeDoProdutor(produtor, de ?? Outubro, ate ?? Novembro), default);

    private Task<Resultado<Guid>> Salvar(Guid? id, string titulo, int complexidade, Guid? cliente) =>
        new SalvarAtividadeManipulador(_c.Como(Diana), _c.Atividades, _c.Clientes, _c.Processos, _c.Areas, _c.Relogio, _c.Unidade, _c.Documentos)
            .ManipularAsync(new SalvarAtividade(id, titulo, "Descrição.", Entrega, cliente, null, null, complexidade), default);

    private Task<Resultado> Concluir(Usuario quem, Guid id) =>
        new ConcluirMinhaParteManipulador(_c.Como(quem), _c.Atividades, _c.Relogio, _c.Unidade, _c.Auditoria)
            .ManipularAsync(new ConcluirMinhaParte(id), default);

    private Task<Resultado> Aprovar(Guid id) =>
        new AprovarAtividadeManipulador(_c.Como(Diana), _c.Atividades, _c.Relogio, _c.Unidade)
            .ManipularAsync(new AprovarAtividade(id), default);

    /// <summary>Direcionada aos responsáveis informados, com Diana de revisora.</summary>
    private async Task<Guid> DirecionadaAsync(string titulo, int complexidade, Guid? cliente, params Usuario[] responsaveis)
    {
        var salva = await Salvar(null, titulo, complexidade, cliente);
        salva.Sucesso.Should().BeTrue(salva.Mensagem);
        var definida = await new DefinirResponsaveisDaAtividadeManipulador(
                _c.Como(Diana), _c.Atividades, _c.Usuarios, _c.Perfis, _c.Relogio, _c.Unidade, _c.Auditoria)
            .ManipularAsync(new DefinirResponsaveisDaAtividade(
                salva.Valor, [.. responsaveis.Select(u => new ResponsavelEntrada(u.Id, null))], Diana.Id), default);
        definida.Sucesso.Should().BeTrue(definida.Mensagem);
        return salva.Valor;
    }

    /// <summary>Todo o percurso até a aprovação acontece no instante informado, que é o da conclusão.</summary>
    private async Task<Guid> ConcluidaAsync(string titulo, int complexidade, DateTime concluidaEm, Guid? cliente, params Usuario[] responsaveis)
    {
        _c.Relogio.AgoraUtc = concluidaEm;
        var id = await DirecionadaAsync(titulo, complexidade, cliente, responsaveis);
        foreach (var responsavel in responsaveis) (await Concluir(responsavel, id)).Sucesso.Should().BeTrue();
        (await Aprovar(id)).Sucesso.Should().BeTrue();
        return id;
    }

    private static (int Complexidade, int Total)[] Totais(ProdutividadeDoProdutor produtividade) =>
        [.. produtividade.Grupos.Select(g => (g.Complexidade, g.Total))];

    // ---- Acesso e validação ----

    [Fact]
    public async Task So_a_diretoria_consulta()
    {
        (await Obter(_pedro, _pedro.Id)).Erro.Should().Be(TipoErro.AcessoNegado);
        (await Obter(null, _pedro.Id)).Erro.Should().Be(TipoErro.AcessoNegado);
        (await Obter(Diana, _pedro.Id)).Sucesso.Should().BeTrue();
    }

    [Fact]
    public async Task Quem_nao_e_de_producao_responde_como_inexistente()
    {
        var diretor = await Obter(Diana, _davi.Id);
        var ninguem = await Obter(Diana, Guid.CreateVersion7());

        diretor.Erro.Should().Be(TipoErro.NaoEncontrado);
        diretor.Mensagem.Should().Be("Produtor não encontrado.");
        ninguem.Erro.Should().Be(TipoErro.NaoEncontrado);
        ninguem.Mensagem.Should().Be("Produtor não encontrado.");
    }

    [Fact]
    public async Task Periodo_invalido_e_recusado_antes_de_procurar_o_produtor()
    {
        var semUtc = await Obter(Diana, _pedro.Id, new DateTime(2026, 10, 1), Novembro);
        var invertido = await Obter(Diana, _pedro.Id, Novembro, Outubro);
        var vazio = await Obter(Diana, _pedro.Id, Outubro, Outubro);
        var longo = await Obter(Diana, _pedro.Id, Outubro, Outubro.AddDays(92).AddTicks(1));
        var noLimite = await Obter(Diana, _pedro.Id, Outubro, Outubro.AddDays(92));
        var invalidoDeQuemNaoExiste = await Obter(Diana, Guid.CreateVersion7(), Novembro, Outubro);

        semUtc.Erro.Should().Be(TipoErro.Validacao);
        semUtc.Mensagem.Should().Be("Período inválido.");
        invertido.Erro.Should().Be(TipoErro.Validacao);
        vazio.Erro.Should().Be(TipoErro.Validacao);
        longo.Erro.Should().Be(TipoErro.Validacao);
        longo.Mensagem.Should().Be("O período deve ter no máximo 92 dias.");
        noLimite.Sucesso.Should().BeTrue(noLimite.Mensagem);
        invalidoDeQuemNaoExiste.Erro.Should().Be(TipoErro.Validacao);
    }

    [Fact]
    public void E_consulta_auditavel_com_o_produtor_e_o_periodo()
    {
        IConsultaAuditavel consulta = new ObterProdutividadeDoProdutor(_pedro.Id, Outubro, Novembro);

        consulta.TipoRecurso.Should().Be("Produtividade");
        consulta.Acao.Should().Be(AcaoDeAcesso.Visualizou);
        consulta.RecursoId.Should().Be(_pedro.Id);
        consulta.Detalhe.Should().Contain("2026-10-01T03:00:00Z").And.Contain("2026-11-01T03:00:00Z");
    }

    // ---- O que conta ----

    [Fact]
    public async Task Sem_nada_concluido_vem_os_tres_grupos_vazios()
    {
        var produtividade = (await Obter(Diana, _pedro.Id)).Valor!;

        produtividade.ProdutorId.Should().Be(_pedro.Id);
        produtividade.ProdutorNome.Should().Be("Pedro");
        Totais(produtividade).Should().Equal((1, 0), (2, 0), (3, 0));
        produtividade.Total.Should().Be(0);
        produtividade.PrimeiraConclusaoEm.Should().BeNull();
    }

    [Fact]
    public async Task Agrupa_por_complexidade_as_concluidas_do_periodo_em_que_o_produtor_e_responsavel()
    {
        var simples = await ConcluidaAsync("Petição simples", 1, Em(10, 5), null, _pedro);
        var dosDois = await ConcluidaAsync("Recurso dos dois", 3, Em(10, 10), _maria.Id, _pedro, _paula);
        var daPaula = await ConcluidaAsync("Só da Paula", 2, Em(10, 7), null, _paula);
        await ConcluidaAsync("De setembro", 2, Em(9, 15), null, _pedro);
        await DirecionadaAsync("Ainda em andamento", 1, null, _pedro);

        var dePedro = (await Obter(Diana, _pedro.Id)).Valor!;
        var dePaula = (await Obter(Diana, _paula.Id)).Valor!;

        Totais(dePedro).Should().Equal((1, 1), (2, 0), (3, 1));
        dePedro.Total.Should().Be(2);
        dePedro.Grupos[0].Atividades.Should().Equal(new AtividadeRealizada(simples, "Petição simples", 1, Em(10, 5), null));
        dePedro.Grupos[2].Atividades.Should().Equal(new AtividadeRealizada(dosDois, "Recurso dos dois", 3, Em(10, 10), "Maria Silva"));
        dePedro.PrimeiraConclusaoEm.Should().Be(Em(9, 15));

        // A atividade dos dois aparece inteira para cada um, uma vez só.
        Totais(dePaula).Should().Equal((1, 0), (2, 1), (3, 1));
        dePaula.Grupos[1].Atividades.Select(a => a.Id).Should().Equal(daPaula);
        dePaula.Grupos[2].Atividades.Select(a => a.Id).Should().Equal(dosDois);
    }

    [Fact]
    public async Task Dentro_do_grupo_vem_da_conclusao_mais_antiga_para_a_mais_recente()
    {
        var depois = await ConcluidaAsync("Depois", 2, Em(10, 20), null, _pedro);
        var antes = await ConcluidaAsync("Antes", 2, Em(10, 2), null, _pedro);

        (await Obter(Diana, _pedro.Id)).Valor!.Grupos[1].Atividades.Select(a => a.Id).Should().Equal(antes, depois);
    }

    [Fact]
    public async Task O_periodo_e_semiaberto_o_inicio_conta_e_o_fim_nao()
    {
        var noInicio = await ConcluidaAsync("No primeiro instante", 1, Outubro, null, _pedro);
        await ConcluidaAsync("No primeiro instante do mês seguinte", 1, Novembro, null, _pedro);
        var noFim = await ConcluidaAsync("No último instante", 1, Novembro.AddTicks(-1), null, _pedro);

        (await Obter(Diana, _pedro.Id)).Valor!.Grupos[0].Atividades.Select(a => a.Id).Should().Equal(noInicio, noFim);
    }

    [Fact]
    public async Task Reaberta_deixa_de_contar_e_volta_no_mes_da_nova_conclusao()
    {
        var id = await ConcluidaAsync("Parecer", 2, Em(10, 5), null, _pedro);

        (await new ReabrirAtividadeManipulador(_c.Como(Diana), _c.Atividades, _c.Relogio, _c.Unidade)
            .ManipularAsync(new ReabrirAtividade(id, "Cliente pediu ajuste", [_pedro.Id]), default)).Sucesso.Should().BeTrue();

        var reaberta = (await Obter(Diana, _pedro.Id)).Valor!;
        reaberta.Total.Should().Be(0);
        reaberta.PrimeiraConclusaoEm.Should().BeNull();

        _c.Relogio.AgoraUtc = Em(11, 3);
        (await Concluir(_pedro, id)).Sucesso.Should().BeTrue();
        (await Aprovar(id)).Sucesso.Should().BeTrue();

        (await Obter(Diana, _pedro.Id)).Valor!.Total.Should().Be(0);
        (await Obter(Diana, _pedro.Id, Novembro, Novembro.AddDays(30))).Valor!
            .Grupos[1].Atividades.Select(a => a.Id).Should().Equal(id);
    }

    [Fact]
    public async Task Vale_a_complexidade_atual_da_atividade()
    {
        var id = await ConcluidaAsync("Parecer", 1, Em(10, 5), null, _pedro);

        (await Salvar(id, "Parecer", 3, null)).Sucesso.Should().BeTrue();

        Totais((await Obter(Diana, _pedro.Id)).Valor!).Should().Equal((1, 0), (2, 0), (3, 1));
    }

    [Fact]
    public async Task Excluida_deixa_de_contar()
    {
        var id = await ConcluidaAsync("Parecer", 1, Em(10, 5), null, _pedro);

        (await new ExcluirAtividadeManipulador(_c.Como(Diana), _c.Atividades, _c.Unidade)
            .ManipularAsync(new ExcluirAtividade(id), default)).Sucesso.Should().BeTrue();

        (await Obter(Diana, _pedro.Id)).Valor!.Total.Should().Be(0);
    }

    [Fact]
    public async Task Inativo_continua_consultavel_e_quem_virou_diretoria_deixa_de_ser_produtor()
    {
        await ConcluidaAsync("Parecer", 1, Em(10, 5), null, _pedro, _paula);

        _pedro.Inativar();
        _paula.AlterarPerfil(_c.PerfilDiretor);

        (await Obter(Diana, _pedro.Id)).Valor!.Total.Should().Be(1);
        (await Obter(Diana, _paula.Id)).Erro.Should().Be(TipoErro.NaoEncontrado);
    }
}
```

- [ ] **Step 2: Rodar e ver falhar**

Run: `dotnet test tests/AtividadesJuridicas.UnitTests`
Expected: FALHA de compilação — `ObterProdutividadeDoProdutor`, `ProdutividadeDoProdutor` e `AtividadeRealizada` não existem.

- [ ] **Step 3: Portas**

Em `src/AtividadesJuridicas.Application/Atividades/PortasDeAtividades.cs`, depois de `ClienteAtualDaAtividade`:

```csharp
public static class ProdutividadeOpcoes
{
    /// <summary>O mesmo limite da agenda. A tela pede um mês por vez.</summary>
    public const int PeriodoMaximoEmDias = 92;

    public const string ProdutorNaoEncontrado = "Produtor não encontrado.";
}

/// <summary>Uma atividade concluída em que o usuário é responsável.</summary>
public sealed record AtividadeRealizada(Guid Id, string Titulo, int Complexidade, DateTime ConcluidaEm, string? ClienteNome);

/// <summary>As atividades realizadas de uma complexidade, da conclusão mais antiga para a mais recente.</summary>
public sealed record GrupoDeComplexidade(int Complexidade, IReadOnlyList<AtividadeRealizada> Atividades)
{
    public int Total => Atividades.Count;
}

/// <summary>
/// <paramref name="Grupos"/>: um por complexidade, em ordem, sempre presentes mesmo vazios.
/// <paramref name="PrimeiraConclusaoEm"/>: a conclusão mais antiga entre as atividades hoje concluídas do produtor.
/// </summary>
public sealed record ProdutividadeDoProdutor(
    Guid ProdutorId, string ProdutorNome, IReadOnlyList<GrupoDeComplexidade> Grupos, DateTime? PrimeiraConclusaoEm)
{
    public int Total => Grupos.Sum(g => g.Total);
}
```

Em `IAtividadeRepositorio`, por último:

```csharp
    /// <summary>
    /// As atividades concluídas em <c>[deUtc, ateUtc)</c> em que o usuário é responsável, da conclusão mais antiga
    /// para a mais recente. O cliente excluído logicamente continua identificado pelo nome.
    /// </summary>
    Task<IReadOnlyList<AtividadeRealizada>> ListarRealizadasAsync(
        Guid responsavelId, DateTime deUtc, DateTime ateUtc, CancellationToken ct);

    /// <summary>
    /// A conclusão mais antiga entre as atividades hoje concluídas em que o usuário é responsável; nulo se não há
    /// nenhuma.
    /// </summary>
    Task<DateTime?> ObterPrimeiraConclusaoAsync(Guid responsavelId, CancellationToken ct);
```

- [ ] **Step 4: Caso de uso**

Crie `src/AtividadesJuridicas.Application/Atividades/ObterProdutividadeDoProdutor.cs`:

```csharp
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Atividades;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.Application.Atividades;

/// <summary>
/// As atividades que o produtor realizou no período <c>[DeUtc, AteUtc)</c>, em UTC, separadas por complexidade.
/// Realizada é a atividade concluída em que ele é responsável; o período olha para a data da conclusão.
/// </summary>
public sealed record ObterProdutividadeDoProdutor(Guid ProdutorId, DateTime DeUtc, DateTime AteUtc)
    : IRequisicao<Resultado<ProdutividadeDoProdutor>>, IConsultaAuditavel
{
    public string TipoRecurso => "Produtividade";
    public AcaoDeAcesso Acao => AcaoDeAcesso.Visualizou;
    public Guid? RecursoId => ProdutorId;
    public string? Detalhe => DetalheDeConsulta.Serializar(new { DeUtc, AteUtc });
}

public sealed class ObterProdutividadeDoProdutorManipulador(
    IContextoDeAcesso contexto, IUsuarioRepositorio usuarios, IPerfilRepositorio perfis, IAtividadeRepositorio atividades)
    : IManipulador<ObterProdutividadeDoProdutor, Resultado<ProdutividadeDoProdutor>>
{
    public async Task<Resultado<ProdutividadeDoProdutor>> ManipularAsync(
        ObterProdutividadeDoProdutor requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Falha(TipoErro.AcessoNegado, "Acesso negado.");

        if (requisicao.DeUtc.Kind != DateTimeKind.Utc || requisicao.AteUtc.Kind != DateTimeKind.Utc
            || requisicao.DeUtc >= requisicao.AteUtc)
            return Falha(TipoErro.Validacao, "Período inválido.");
        if (requisicao.AteUtc - requisicao.DeUtc > TimeSpan.FromDays(ProdutividadeOpcoes.PeriodoMaximoEmDias))
            return Falha(TipoErro.Validacao, $"O período deve ter no máximo {ProdutividadeOpcoes.PeriodoMaximoEmDias} dias.");

        // Produtor é quem tem hoje um perfil de Produção, ativo ou não. Qualquer outro Id responde como inexistente.
        var produtor = await usuarios.ObterPorIdAsync(requisicao.ProdutorId, ct);
        var perfil = produtor is null ? null : await perfis.ObterPorIdAsync(produtor.PerfilId, ct);
        if (produtor is null || perfil is not { Tipo: TipoPerfil.Producao })
            return Falha(TipoErro.NaoEncontrado, ProdutividadeOpcoes.ProdutorNaoEncontrado);

        var realizadas = await atividades.ListarRealizadasAsync(produtor.Id, requisicao.DeUtc, requisicao.AteUtc, ct);

        // Um grupo por complexidade, mesmo vazio, para a tela mostrar sempre as três colunas.
        var grupos = Enumerable
            .Range(Atividade.ComplexidadeMinima, Atividade.ComplexidadeMaxima - Atividade.ComplexidadeMinima + 1)
            .Select(complexidade => new GrupoDeComplexidade(
                complexidade, [.. realizadas.Where(a => a.Complexidade == complexidade)]))
            .ToList();

        return Resultado<ProdutividadeDoProdutor>.Ok(new ProdutividadeDoProdutor(
            produtor.Id, produtor.Nome, grupos, await atividades.ObterPrimeiraConclusaoAsync(produtor.Id, ct)));
    }

    private static Resultado<ProdutividadeDoProdutor> Falha(TipoErro erro, string mensagem) =>
        Resultado<ProdutividadeDoProdutor>.Falha(erro, mensagem);
}
```

O manipulador é registrado sozinho: `AddApplication` varre os `IManipulador<,>` do assembly, e `RegistroDeManipuladoresTests` confere.

- [ ] **Step 5: Repositório falso**

Em `tests/AtividadesJuridicas.UnitTests/Application/Falsos.cs`, em `AtividadeRepositorioFalso`, antes de `Adicionar`:

```csharp
    // Mesma regra da consulta real: concluída, no intervalo semiaberto, com o usuário entre os responsáveis.
    public Task<IReadOnlyList<AtividadeRealizada>> ListarRealizadasAsync(
        Guid responsavelId, DateTime deUtc, DateTime ateUtc, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<AtividadeRealizada>>([.. Itens
            .Where(a => a.Status == StatusAtividade.Concluida && a.ConcluidaEm >= deUtc && a.ConcluidaEm < ateUtc
                        && a.EhResponsavel(responsavelId))
            .OrderBy(a => a.ConcluidaEm).ThenBy(a => a.Id)
            .Select(a => new AtividadeRealizada(
                a.Id, a.Titulo, a.Complexidade, a.ConcluidaEm!.Value,
                clientes.Itens.FirstOrDefault(c => c.Id == a.ClienteId)?.Nome))]);

    public Task<DateTime?> ObterPrimeiraConclusaoAsync(Guid responsavelId, CancellationToken ct) =>
        Task.FromResult(Itens
            .Where(a => a.Status == StatusAtividade.Concluida && a.EhResponsavel(responsavelId))
            .Min(a => a.ConcluidaEm));
```

- [ ] **Step 6: Rodar os unitários**

Run: `dotnet test tests/AtividadesJuridicas.UnitTests`
Expected: PASS, incluindo os doze testes de `ProdutividadeTests` e `Toda_requisicao_tem_exatamente_um_manipulador_registrado`.

- [ ] **Step 7: Testes de integração**

Crie `tests/AtividadesJuridicas.IntegrationTests/ProdutividadeTests.cs`:

```csharp
using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class ProdutividadeTests(BancoFixture banco) : TesteComBanco(banco)
{
    // Meses de 2026 no fuso do escritório (UTC-3): cada um começa às 03:00 UTC do dia 1.
    private static readonly DateTime Setembro = new(2026, 9, 1, 3, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Outubro = new(2026, 10, 1, 3, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Novembro = new(2026, 11, 1, 3, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Entrega = new(2090, 6, 15);

    private Guid _diana, _davi, _pedro, _paula, _maria;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _diana = await IdDoAdminAsync();
        _davi = await NovoUsuarioAsync("Davi Diretor", "davi@x.com", TipoPerfil.Diretoria);
        _pedro = await NovoUsuarioAsync("Pedro", "pedro@x.com", TipoPerfil.Producao);
        _paula = await NovoUsuarioAsync("Paula", "paula@x.com", TipoPerfil.Producao);
        _maria = (await ExecutarAsync(_diana, new SalvarClientePessoaFisica(
            null, new DadosDoClienteEntrada("Maria Silva", null, null, new EnderecoEntrada(), null),
            "529.982.247-25", null, null))).Valor;
    }

    private static DateTime Em(int mes, int dia) => new(2026, mes, dia, 15, 0, 0, DateTimeKind.Utc);

    private async Task<Guid> NovoUsuarioAsync(string nome, string email, TipoPerfil tipo)
    {
        var perfil = await IdDoPerfilAsync(tipo);
        var criado = await ExecutarAsync(_diana, new CriarUsuario(nome, email, "senha-do-" + nome[..3].ToLowerInvariant(), perfil, null));
        criado.Sucesso.Should().BeTrue(criado.Mensagem);
        return criado.Valor;
    }

    /// <summary>Direcionada aos responsáveis informados, com Diana de revisora.</summary>
    private async Task<Guid> DirecionadaAsync(string titulo, int complexidade, Guid? cliente, params Guid[] responsaveis)
    {
        var criada = await ExecutarAsync(_diana, new SalvarAtividade(
            null, titulo, "Descrição.", Entrega, cliente, null, null, complexidade));
        criada.Sucesso.Should().BeTrue(criada.Mensagem);
        var definida = await ExecutarAsync(_diana, new DefinirResponsaveisDaAtividade(
            criada.Valor, [.. responsaveis.Select(r => new ResponsavelEntrada(r, null))], _diana));
        definida.Sucesso.Should().BeTrue(definida.Mensagem);
        return criada.Valor;
    }

    /// <summary>
    /// Conclui pelos casos de uso (o relógio da infraestrutura é o real) e depois acerta <c>ConcluidaEm</c> para o
    /// instante do cenário, pelo contexto e com os interceptores.
    /// </summary>
    private async Task<Guid> ConcluidaAsync(string titulo, int complexidade, DateTime concluidaEm, Guid? cliente, params Guid[] responsaveis)
    {
        var id = await DirecionadaAsync(titulo, complexidade, cliente, responsaveis);
        foreach (var responsavel in responsaveis)
            (await ExecutarAsync(responsavel, new ConcluirMinhaParte(id))).Sucesso.Should().BeTrue();
        (await ExecutarAsync(_diana, new AprovarAtividade(id))).Sucesso.Should().BeTrue();

        await EmEscopoAsync(null, async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var atividade = await db.Atividades.SingleAsync(a => a.Id == id);
            db.Entry(atividade).Property(a => a.ConcluidaEm).CurrentValue = concluidaEm;
            return await db.SaveChangesAsync();
        });
        return id;
    }

    private async Task<ProdutividadeDoProdutor> ProdutividadeAsync(Guid produtor, DateTime? de = null, DateTime? ate = null)
    {
        var resultado = await ExecutarAsync(_diana, new ObterProdutividadeDoProdutor(produtor, de ?? Outubro, ate ?? Novembro));
        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        return resultado.Valor!;
    }

    private static (int Complexidade, int Total)[] Totais(ProdutividadeDoProdutor produtividade) =>
        [.. produtividade.Grupos.Select(g => (g.Complexidade, g.Total))];

    [Fact]
    public async Task Traz_so_as_concluidas_do_periodo_em_que_o_produtor_e_responsavel()
    {
        var simples = await ConcluidaAsync("Petição simples", 1, Em(10, 5), null, _pedro);
        var dosDois = await ConcluidaAsync("Recurso dos dois", 3, Em(10, 10), _maria, _pedro, _paula);
        var daPaula = await ConcluidaAsync("Só da Paula", 2, Em(10, 7), null, _paula);
        await ConcluidaAsync("De setembro", 2, Em(9, 15), null, _pedro);
        await DirecionadaAsync("Em andamento", 1, null, _pedro);

        var dePedro = await ProdutividadeAsync(_pedro);
        var dePaula = await ProdutividadeAsync(_paula);

        dePedro.ProdutorNome.Should().Be("Pedro");
        Totais(dePedro).Should().Equal((1, 1), (2, 0), (3, 1));
        dePedro.Total.Should().Be(2);
        dePedro.Grupos[0].Atividades.Should().Equal(new AtividadeRealizada(simples, "Petição simples", 1, Em(10, 5), null));
        dePedro.Grupos[2].Atividades.Should().Equal(new AtividadeRealizada(dosDois, "Recurso dos dois", 3, Em(10, 10), "Maria Silva"));
        dePedro.Grupos[2].Atividades.Single().ConcluidaEm.Kind.Should().Be(DateTimeKind.Utc);
        dePedro.PrimeiraConclusaoEm.Should().Be(Em(9, 15));

        // A atividade dos dois aparece inteira para cada um, uma vez só.
        Totais(dePaula).Should().Equal((1, 0), (2, 1), (3, 1));
        dePaula.Grupos[1].Atividades.Select(a => a.Id).Should().Equal(daPaula);
        dePaula.Grupos[2].Atividades.Select(a => a.Id).Should().Equal(dosDois);
    }

    [Fact]
    public async Task Periodo_e_semiaberto_e_cada_grupo_vem_em_ordem_de_conclusao()
    {
        // 30/09 23:59:59 no escritório é 01/10 02:59:59 em UTC: ainda setembro.
        var fimDeSetembro = await ConcluidaAsync("Último segundo de setembro", 1, Outubro.AddSeconds(-1), null, _pedro);
        var fimDeOutubro = await ConcluidaAsync("Último segundo de outubro", 1, Novembro.AddSeconds(-1), null, _pedro);
        var meioDeOutubro = await ConcluidaAsync("Meio de outubro", 1, Em(10, 15), null, _pedro);
        var inicioDeOutubro = await ConcluidaAsync("Primeiro instante de outubro", 1, Outubro, null, _pedro);
        await ConcluidaAsync("Primeiro instante de novembro", 1, Novembro, null, _pedro);

        (await ProdutividadeAsync(_pedro)).Grupos[0].Atividades.Select(a => a.Id)
            .Should().Equal(inicioDeOutubro, meioDeOutubro, fimDeOutubro);
        (await ProdutividadeAsync(_pedro, Setembro, Outubro)).Grupos[0].Atividades.Select(a => a.Id)
            .Should().Equal(fimDeSetembro);
    }

    [Fact]
    public async Task Reaberta_e_excluida_deixam_de_contar()
    {
        var reaberta = await ConcluidaAsync("Será reaberta", 1, Em(10, 5), null, _pedro);
        var excluida = await ConcluidaAsync("Será excluída", 2, Em(10, 6), null, _pedro);
        var fica = await ConcluidaAsync("Fica", 3, Em(10, 7), null, _pedro);

        (await ExecutarAsync(_diana, new ReabrirAtividade(reaberta, "Cliente pediu ajuste", [_pedro]))).Sucesso.Should().BeTrue();
        (await ExecutarAsync(_diana, new ExcluirAtividade(excluida))).Sucesso.Should().BeTrue();

        var produtividade = await ProdutividadeAsync(_pedro);
        Totais(produtividade).Should().Equal((1, 0), (2, 0), (3, 1));
        produtividade.Grupos[2].Atividades.Select(a => a.Id).Should().Equal(fica);
        produtividade.PrimeiraConclusaoEm.Should().Be(Em(10, 7));
    }

    [Fact]
    public async Task Sem_nada_concluido_vem_os_tres_grupos_vazios_e_nenhuma_primeira_conclusao()
    {
        await DirecionadaAsync("Em andamento", 1, null, _pedro);

        var produtividade = await ProdutividadeAsync(_pedro);

        Totais(produtividade).Should().Equal((1, 0), (2, 0), (3, 0));
        produtividade.PrimeiraConclusaoEm.Should().BeNull();
    }

    [Fact]
    public async Task So_a_diretoria_consulta_e_so_quem_e_de_producao_e_produtor()
    {
        await ConcluidaAsync("Parecer", 1, Em(10, 5), null, _pedro);

        var dePedro = await ExecutarAsync(_pedro, new ObterProdutividadeDoProdutor(_pedro, Outubro, Novembro));
        var doDiretor = await ExecutarAsync(_diana, new ObterProdutividadeDoProdutor(_davi, Outubro, Novembro));
        var deNinguem = await ExecutarAsync(_diana, new ObterProdutividadeDoProdutor(Guid.CreateVersion7(), Outubro, Novembro));

        dePedro.Erro.Should().Be(TipoErro.AcessoNegado);
        doDiretor.Erro.Should().Be(TipoErro.NaoEncontrado);
        doDiretor.Mensagem.Should().Be("Produtor não encontrado.");
        deNinguem.Erro.Should().Be(TipoErro.NaoEncontrado);

        // Só a recusa por perfil é tentativa de acesso negada, e fica registrada com o produtor pedido.
        var negado = (await AuditoriaAsync()).Where(r => r.Evento == EventosDeSeguranca.AcessoNegado)
            .Should().ContainSingle().Subject;
        negado.UsuarioId.Should().Be(_pedro);
        negado.Dados.Should().Contain(_pedro.ToString());
    }

    [Fact]
    public async Task Consulta_fica_no_registro_de_acessos_com_o_produtor_e_o_periodo()
    {
        await ProdutividadeAsync(_pedro);
        await ExecutarAsync(_diana, new ObterProdutividadeDoProdutor(_davi, Outubro, Novembro));   // recusada: não registra

        var acessos = await EmEscopoAsync(null, sp => sp.GetRequiredService<AppDbContext>().RegistrosAcesso
            .AsNoTracking().Where(a => a.TipoRecurso == "Produtividade").ToListAsync());

        var acesso = acessos.Should().ContainSingle().Subject;
        acesso.Acao.Should().Be(AcaoDeAcesso.Visualizou);
        acesso.RecursoId.Should().Be(_pedro);
        acesso.UsuarioId.Should().Be(_diana);
        acesso.Detalhe.Should().Contain("2026-10-01T03:00:00Z").And.Contain("2026-11-01T03:00:00Z");
    }

    [Fact]
    public async Task Inativo_continua_consultavel_e_quem_vira_diretoria_deixa_de_ser_produtor()
    {
        await ConcluidaAsync("Parecer", 1, Em(10, 5), null, _pedro, _paula);
        (await ExecutarAsync(_diana, new DefinirAtivoUsuario(_pedro, false))).Sucesso.Should().BeTrue();
        var diretoria = await IdDoPerfilAsync(TipoPerfil.Diretoria);
        (await ExecutarAsync(_diana, new EditarUsuario(_paula, "Paula", "paula@x.com", diretoria, null))).Sucesso.Should().BeTrue();

        (await ProdutividadeAsync(_pedro)).Total.Should().Be(1);
        (await ExecutarAsync(_diana, new ObterProdutividadeDoProdutor(_paula, Outubro, Novembro)))
            .Erro.Should().Be(TipoErro.NaoEncontrado);
    }
}
```

- [ ] **Step 8: Rodar e ver falhar**

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --filter "FullyQualifiedName~ProdutividadeTests"`
Expected: FALHA de compilação — `AtividadeRepositorio` não implementa os dois membros novos de `IAtividadeRepositorio`.

- [ ] **Step 9: Repositório**

Em `src/AtividadesJuridicas.Infrastructure/Persistencia/Repositorios/AtividadeRepositorio.cs`, antes de `Adicionar`:

```csharp
    public async Task<IReadOnlyList<AtividadeRealizada>> ListarRealizadasAsync(
        Guid responsavelId, DateTime deUtc, DateTime ateUtc, CancellationToken ct)
    {
        // Pelas entidades mapeadas: o filtro de exclusão lógica vale para a atividade e para a entrada do responsável.
        var atividades = await db.Atividades.AsNoTracking()
            .Where(a => a.Status == StatusAtividade.Concluida && a.ConcluidaEm >= deUtc && a.ConcluidaEm < ateUtc
                        && a.Responsaveis.Any(r => r.UsuarioId == responsavelId))
            .OrderBy(a => a.ConcluidaEm).ThenBy(a => a.Id)
            .Select(a => new { a.Id, a.Titulo, a.Complexidade, a.ConcluidaEm, a.ClienteId })
            .ToListAsync(ct);

        // O cliente excluído logicamente continua identificado pelo nome.
        var idsDeClientes = atividades.Where(a => a.ClienteId != null).Select(a => a.ClienteId!.Value).Distinct().ToList();
        var clientes = await db.Clientes.IgnoreQueryFilters().AsNoTracking()
            .Where(c => idsDeClientes.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Nome, ct);

        return [.. atividades.Select(a => new AtividadeRealizada(
            a.Id, a.Titulo, a.Complexidade, a.ConcluidaEm!.Value,
            a.ClienteId is { } clienteId && clientes.TryGetValue(clienteId, out var nome) ? nome : null))];
    }

    public Task<DateTime?> ObterPrimeiraConclusaoAsync(Guid responsavelId, CancellationToken ct) =>
        db.Atividades.AsNoTracking()
            .Where(a => a.Status == StatusAtividade.Concluida && a.Responsaveis.Any(r => r.UsuarioId == responsavelId))
            .MinAsync(a => a.ConcluidaEm, ct);
```

- [ ] **Step 10: Rodar tudo**

Run: `dotnet build`
Expected: zero avisos.

Run: `dotnet test`
Expected: PASS, incluindo os sete testes de integração de `ProdutividadeTests`. Nenhuma migration nova: `dotnet ef migrations has-pending-model-changes --project src/AtividadesJuridicas.Infrastructure --startup-project src/AtividadesJuridicas.Infrastructure` continua sem mudanças pendentes.

- [ ] **Step 11: Commit**

```bash
git add src tests
git commit -m "feat(atividades): consulta da produtividade de um produtor por período e por complexidade"
```

---

### Task 4: Tela de produtores e painel de produtividade

**Files:**
- Create: `src/AtividadesJuridicas.Web/Components/Produtores/RotulosDeProdutividade.cs`
- Create: `src/AtividadesJuridicas.Web/Components/Produtores/PainelDeProdutividade.razor`
- Create: `src/AtividadesJuridicas.Web/Components/Pages/Produtores.razor`
- Modify: `src/AtividadesJuridicas.Web/Components/_Imports.razor`
- Modify: `src/AtividadesJuridicas.Web/Components/Layout/MainLayout.razor`
- Create: `tests/AtividadesJuridicas.IntegrationTests/ProdutoresWebTests.cs`

O componente se chama `PainelDeProdutividade` porque `ProdutividadeDoProdutor` já é o registro da aplicação.

**Interfaces:**
- Consumes: `ObterProdutividadeDoProdutor(Guid ProdutorId, DateTime DeUtc, DateTime AteUtc)`, `ProdutividadeDoProdutor`, `GrupoDeComplexidade`, `ProdutividadeOpcoes.PeriodoMaximoEmDias` (Tarefa 3); `TelaDeTeste.RenderizarAsync<TComponente>` (Tarefa 2); `SalvarAtividade(..., int Complexidade, ...)` (Tarefa 1); `ListarUsuarios`, `UsuarioResumo`, `ObterMeuAcesso`, `FichaDaAtividade`, `FusoDoEscritorio` (existentes).
- Produces (namespace `AtividadesJuridicas.Web.Components.Produtores`): `static class RotulosDeProdutividade` com `MesDe(DateOnly)`, `MesDe(FusoDoEscritorio, DateTime)`, `Lista(DateOnly hoje, DateOnly maisAntigo)`, `Intervalo(FusoDoEscritorio, DateOnly)`, `Mes(DateOnly)`, `Chave(DateOnly)`, `TentarLer(string?, out DateOnly)`, `Titulo(string)`, `Coluna(int)`, `Grupo(GrupoDeComplexidade)`. Componente `PainelDeProdutividade` com os parâmetros `ProdutorId`, `ProdutorNome`, `OnAcessoEncerrado`, `OnFechar`.

- [ ] **Step 1: Testes dos textos e do cálculo de mês**

Crie `tests/AtividadesJuridicas.IntegrationTests/ProdutoresWebTests.cs` começando pela classe sem página:

```csharp
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Infrastructure.Persistencia;
using AtividadesJuridicas.Web.Components.Produtores;
using AtividadesJuridicas.Web.Infra;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>Os meses que o painel oferece, o intervalo que cada um vira na consulta e os textos do painel.</summary>
public class RotulosDeProdutividadeTests
{
    private readonly FusoDoEscritorio _fuso = new("America/Sao_Paulo");

    [Fact]
    public void Lista_vai_do_mes_corrente_ate_o_mais_antigo_atravessando_a_virada_do_ano()
    {
        RotulosDeProdutividade.Lista(new DateOnly(2027, 2, 14), new DateOnly(2026, 11, 30)).Should().Equal(
            new DateOnly(2027, 2, 1), new DateOnly(2027, 1, 1), new DateOnly(2026, 12, 1), new DateOnly(2026, 11, 1));
    }

    [Fact]
    public void Lista_sem_historico_ou_com_mais_antigo_no_futuro_traz_so_o_mes_corrente()
    {
        var hoje = new DateOnly(2026, 10, 8);

        RotulosDeProdutividade.Lista(hoje, hoje).Should().Equal(new DateOnly(2026, 10, 1));
        RotulosDeProdutividade.Lista(hoje, new DateOnly(2026, 12, 1)).Should().Equal(new DateOnly(2026, 10, 1));
    }

    [Fact]
    public void Intervalo_e_o_mes_inteiro_no_fuso_do_escritorio_em_utc()
    {
        var (de, ate) = RotulosDeProdutividade.Intervalo(_fuso, new DateOnly(2026, 10, 17));

        de.Should().Be(new DateTime(2026, 10, 1, 3, 0, 0, DateTimeKind.Utc));
        ate.Should().Be(new DateTime(2026, 11, 1, 3, 0, 0, DateTimeKind.Utc));
        de.Kind.Should().Be(DateTimeKind.Utc);
        ate.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Conclusao_no_fim_da_noite_do_ultimo_dia_pertence_ao_mes_que_termina()
    {
        // 30/09 23:30 em São Paulo já é 01/10 02:30 em UTC.
        var conclusao = new DateTime(2026, 10, 1, 2, 30, 0, DateTimeKind.Utc);
        var (deSetembro, ateSetembro) = RotulosDeProdutividade.Intervalo(_fuso, new DateOnly(2026, 9, 1));
        var (deOutubro, _) = RotulosDeProdutividade.Intervalo(_fuso, new DateOnly(2026, 10, 1));

        RotulosDeProdutividade.MesDe(_fuso, conclusao).Should().Be(new DateOnly(2026, 9, 1));
        (conclusao >= deSetembro && conclusao < ateSetembro).Should().BeTrue();
        (conclusao >= deOutubro).Should().BeFalse();
    }

    [Fact]
    public void Mes_com_mudanca_de_horario_tem_o_intervalo_certo_e_cabe_no_limite_da_consulta()
    {
        var novaYork = new FusoDoEscritorio("America/New_York");

        // Em Nova York o relógio volta em 01/11/2026: novembro tem uma hora a mais.
        var (de, ate) = RotulosDeProdutividade.Intervalo(novaYork, new DateOnly(2026, 11, 1));

        de.Should().Be(new DateTime(2026, 11, 1, 4, 0, 0, DateTimeKind.Utc));
        ate.Should().Be(new DateTime(2026, 12, 1, 5, 0, 0, DateTimeKind.Utc));
        (ate - de).Should().BeLessThan(TimeSpan.FromDays(ProdutividadeOpcoes.PeriodoMaximoEmDias));
    }

    [Fact]
    public void Textos_nao_dependem_da_cultura_da_thread()
    {
        var antes = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ar-SA");   // outros algarismos e outro calendário

            RotulosDeProdutividade.Mes(new DateOnly(2026, 10, 1)).Should().Be("outubro/2026");
            RotulosDeProdutividade.Chave(new DateOnly(2026, 3, 1)).Should().Be("2026-03");
            RotulosDeProdutividade.Coluna(2).Should().Be("Complexidade 2");
            RotulosDeProdutividade.Grupo(new GrupoDeComplexidade(3, [])).Should().Be("Complexidade 3 (0)");
        }
        finally
        {
            CultureInfo.CurrentCulture = antes;
        }
    }

    [Fact]
    public void Chave_vai_e_volta_e_texto_estranho_e_recusado()
    {
        RotulosDeProdutividade.TentarLer("2026-03", out var mes).Should().BeTrue();
        mes.Should().Be(new DateOnly(2026, 3, 1));

        RotulosDeProdutividade.TentarLer("março", out _).Should().BeFalse();
        RotulosDeProdutividade.TentarLer("2026-13", out _).Should().BeFalse();
        RotulosDeProdutividade.TentarLer(null, out _).Should().BeFalse();
    }

    [Fact]
    public void Titulo_do_painel_leva_o_nome_do_produtor()
    {
        RotulosDeProdutividade.Titulo("Pedro Produção").Should().Be("Produtividade — Pedro Produção");
    }
}
```

- [ ] **Step 2: Rodar e ver falhar**

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --filter "FullyQualifiedName~RotulosDeProdutividadeTests"`
Expected: FALHA de compilação — `RotulosDeProdutividade` não existe.

- [ ] **Step 3: Textos e cálculo de mês**

Crie `src/AtividadesJuridicas.Web/Components/Produtores/RotulosDeProdutividade.cs`:

```csharp
using System.Globalization;
using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Web.Infra;

namespace AtividadesJuridicas.Web.Components.Produtores;

/// <summary>Os textos em português e o cálculo de mês que o painel de produtividade mostra.</summary>
public static class RotulosDeProdutividade
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("pt-BR");

    /// <summary>Um mês é representado pelo seu primeiro dia.</summary>
    public static DateOnly MesDe(DateOnly dia) => new(dia.Year, dia.Month, 1);

    /// <summary>O mês em que o instante cai, no fuso do escritório.</summary>
    public static DateOnly MesDe(FusoDoEscritorio fuso, DateTime utc) => MesDe(DateOnly.FromDateTime(fuso.ParaLocal(utc)));

    /// <summary>
    /// Do mês de <paramref name="hoje"/> para trás, mês a mês, até o de <paramref name="maisAntigo"/>. A lista sempre
    /// traz o mês corrente e nunca um mês futuro.
    /// </summary>
    public static IReadOnlyList<DateOnly> Lista(DateOnly hoje, DateOnly maisAntigo)
    {
        var limite = MesDe(maisAntigo);
        var meses = new List<DateOnly>();
        for (var mes = MesDe(hoje); meses.Count == 0 || mes >= limite; mes = mes.AddMonths(-1)) meses.Add(mes);
        return meses;
    }

    /// <summary>O mês inteiro, do primeiro instante dele ao primeiro do mês seguinte, em UTC.</summary>
    public static (DateTime DeUtc, DateTime AteUtc) Intervalo(FusoDoEscritorio fuso, DateOnly mes)
    {
        var primeiroDia = MesDe(mes);
        return (fuso.InicioDoDiaEmUtc(primeiroDia), fuso.InicioDoDiaEmUtc(primeiroDia.AddMonths(1)));
    }

    /// <summary>"outubro/2026", com algarismos e calendário fixos, qualquer que seja a cultura da conexão.</summary>
    public static string Mes(DateOnly mes) => mes.ToString("MMMM'/'yyyy", Cultura);

    /// <summary>"2026-10": o valor da opção no seletor.</summary>
    public static string Chave(DateOnly mes) => mes.ToString("yyyy-MM", CultureInfo.InvariantCulture);

    /// <summary>Lê de volta uma chave escrita por <see cref="Chave"/>.</summary>
    public static bool TentarLer(string? chave, out DateOnly mes) =>
        DateOnly.TryParseExact(chave, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out mes);

    public static string Titulo(string produtorNome) => $"Produtividade — {produtorNome}";

    /// <summary>"Complexidade 2": o cabeçalho da coluna de totais.</summary>
    public static string Coluna(int complexidade) =>
        "Complexidade " + complexidade.ToString(CultureInfo.InvariantCulture);

    /// <summary>"Complexidade 2 (5)": o título do bloco, com a quantidade.</summary>
    public static string Grupo(GrupoDeComplexidade grupo) =>
        $"{Coluna(grupo.Complexidade)} ({grupo.Total.ToString(CultureInfo.InvariantCulture)})";
}
```

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --filter "FullyQualifiedName~RotulosDeProdutividadeTests"`
Expected: PASS nos oito testes.

- [ ] **Step 4: Testes da página e do painel**

No mesmo arquivo `ProdutoresWebTests.cs`, acrescente a classe:

```csharp
[Collection(ColecaoBanco.Nome)]
public class ProdutoresWebTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco);
    private readonly FusoDoEscritorio _fuso = new("America/Sao_Paulo");
    private Guid _admin;
    private Guid _pedro;

    public async Task InitializeAsync()
    {
        _admin = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        _pedro = await _app.CriarPedroAsync();
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    private DateOnly Hoje() => DateOnly.FromDateTime(_fuso.ParaLocal(DateTime.UtcNow));

    /// <summary>Uma atividade do Pedro concluída agora (o relógio é o real): cai no mês corrente.</summary>
    private async Task<Guid> ConcluidaAgoraAsync(string titulo, int complexidade)
    {
        var criada = await _app.ExecutarAsync(_admin, new SalvarAtividade(
            null, titulo, "Descrição.", new DateOnly(2090, 6, 15), null, null, null, complexidade));
        criada.Sucesso.Should().BeTrue(criada.Mensagem);
        (await _app.ExecutarAsync(_admin, new DefinirResponsaveisDaAtividade(
            criada.Valor, [new ResponsavelEntrada(_pedro, null)], _admin))).Sucesso.Should().BeTrue();
        (await _app.ExecutarAsync(_pedro, new ConcluirMinhaParte(criada.Valor))).Sucesso.Should().BeTrue();
        (await _app.ExecutarAsync(_admin, new AprovarAtividade(criada.Valor))).Sucesso.Should().BeTrue();
        return criada.Valor;
    }

    private async Task<string> PainelAsync(Guid produtor, string nome) => WebUtility.HtmlDecode(
        await TelaDeTeste.RenderizarAsync<PainelDeProdutividade>(_app, _admin, new()
        {
            [nameof(PainelDeProdutividade.ProdutorId)] = produtor,
            [nameof(PainelDeProdutividade.ProdutorNome)] = nome,
        }));

    private static List<string> Textos(string html, string padrao) =>
        [.. Regex.Matches(html, padrao).Select(m => m.Groups[1].Value)];

    private static List<string> Meses(string html) => Textos(html, @"<option[^>]*>\s*([^<]*?)\s*</option>");

    // ---- Página ----

    [Fact]
    public async Task Diretor_ve_so_quem_e_de_producao_em_ordem_de_nome_com_os_inativos_marcados()
    {
        var producao = await _app.EmEscopoAsync(null, async sp =>
            (await sp.GetRequiredService<AppDbContext>().Perfis.AsNoTracking()
                .FirstAsync(p => p.Tipo == TipoPerfil.Producao && p.Ativo)).Id);
        var iara = await _app.ExecutarAsync(_admin, new CriarUsuario("Iara Inativa", "iara@x.com", "senha-da-iara", producao, null));
        iara.Sucesso.Should().BeTrue(iara.Mensagem);
        (await _app.ExecutarAsync(_admin, new DefinirAtivoUsuario(iara.Valor, false))).Sucesso.Should().BeTrue();
        var cliente = await _app.ClienteAutenticadoAsync();

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/produtores"));

        html.Should().Contain("Produtores").And.Contain("Acompanhar produtividade").And.NotContain("Acesso negado");
        // Só as células de dados (a das ações tem classe): nome, perfil, OAB e situação de cada linha, em ordem.
        // A diretora não está entre elas, embora o nome dela apareça no cabeçalho da página.
        Textos(html, @"<td>\s*([^<]*?)\s*</td>").Should().Equal(
            "Iara Inativa", "Produção", "", "Inativo",
            "Pedro Produção", "Produção", "", "Ativo");
        Regex.Matches(html, "<tr class=\"inativo\"").Count.Should().Be(1);
        // O painel só abre por clique.
        html.Should().NotContain("Produtividade —");
    }

    [Fact]
    public async Task Producao_recebe_acesso_negado_e_nao_ve_a_lista()
    {
        var cliente = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/produtores"));

        html.Should().Contain("Acesso negado").And.NotContain("Acompanhar produtividade");
        (await _app.EventosAsync(EventosDeSeguranca.AcessoNegado)).Should().BeEmpty(
            "a página confere o perfil antes de pedir a lista de usuários");
    }

    [Fact]
    public async Task Menu_mostra_produtores_so_para_a_diretoria()
    {
        var doDiretor = await _app.ClienteAutenticadoAsync();
        var doPedro = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        (await doDiretor.GetStringAsync("/")).Should().Contain("href=\"/produtores\"");
        (await doPedro.GetStringAsync("/")).Should().NotContain("href=\"/produtores\"");
    }

    [Fact]
    public async Task Anonimo_e_redirecionado_ao_login()
    {
        var resposta = await _app.NovoCliente().GetAsync("/produtores");

        resposta.StatusCode.Should().Be(HttpStatusCode.Redirect);
        resposta.Headers.Location!.AbsolutePath.Should().Be("/conta/entrar");
    }

    // ---- Painel ----

    [Fact]
    public async Task Painel_abre_no_mes_corrente_com_os_totais_e_as_atividades_de_cada_complexidade()
    {
        await ConcluidaAgoraAsync("Petição simples", 1);
        await ConcluidaAgoraAsync("Parecer difícil", 3);
        await ConcluidaAgoraAsync("Recurso difícil", 3);
        var mesCorrente = RotulosDeProdutividade.Mes(RotulosDeProdutividade.MesDe(Hoje()));

        var html = await PainelAsync(_pedro, "Pedro Produção");

        html.Should().Contain("Produtividade — Pedro Produção");
        // Sem conclusão mais antiga, o seletor traz só o mês corrente, e ele está escolhido.
        Meses(html).Should().Equal(mesCorrente);
        Textos(html, @"<option[^>]*selected[^>]*>\s*([^<]*?)\s*</option>").Should().Equal(mesCorrente);

        // Totais, na ordem das colunas: complexidade 1, 2, 3 e o total. Só a linha de totais tem células numéricas.
        Textos(html, @"<th>\s*(Complexidade \d|Total)\s*</th>")
            .Should().Equal("Complexidade 1", "Complexidade 2", "Complexidade 3", "Total");
        Textos(html, @"<td>\s*(\d+)\s*</td>").Should().Equal("1", "0", "2", "3");

        Textos(html, @"<h3>\s*([^<]*?)\s*</h3>")
            .Should().Equal("Complexidade 1 (1)", "Complexidade 2 (0)", "Complexidade 3 (2)");
        html.Should().MatchRegex(@"<td>\s*Petição simples\s*</td>");
        // Dentro do grupo, da conclusão mais antiga para a mais recente.
        html.IndexOf("Parecer difícil", StringComparison.Ordinal).Should().BePositive()
            .And.BeLessThan(html.IndexOf("Recurso difícil", StringComparison.Ordinal));
        // Só o grupo vazio diz que está vazio.
        Regex.Matches(html, "Nenhuma atividade\\.").Count.Should().Be(1);
        html.Should().Contain("Concluída em").And.Contain("Abrir");
        html.Should().NotContain("Tentar de novo").And.NotContain("Carregando");
    }

    [Fact]
    public async Task Painel_de_quem_nunca_concluiu_mostra_zeros_e_so_o_mes_corrente()
    {
        var html = await PainelAsync(_pedro, "Pedro Produção");

        Textos(html, @"<td>\s*(\d+)\s*</td>").Should().Equal("0", "0", "0", "0");
        Regex.Matches(html, "Nenhuma atividade\\.").Count.Should().Be(3);
        Meses(html).Should().Equal(RotulosDeProdutividade.Mes(RotulosDeProdutividade.MesDe(Hoje())));
    }

    [Fact]
    public async Task Painel_oferece_os_meses_desde_a_primeira_conclusao()
    {
        var antiga = await ConcluidaAgoraAsync("De três meses atrás", 2);
        var haTresMeses = DateTime.UtcNow.AddMonths(-3);
        await _app.EmEscopoAsync(null, async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var atividade = await db.Atividades.SingleAsync(a => a.Id == antiga);
            db.Entry(atividade).Property(a => a.ConcluidaEm).CurrentValue = haTresMeses;
            return await db.SaveChangesAsync();
        });
        var esperados = RotulosDeProdutividade.Lista(Hoje(), RotulosDeProdutividade.MesDe(_fuso, haTresMeses));

        var html = await PainelAsync(_pedro, "Pedro Produção");

        esperados.Count.Should().BeGreaterThan(1);
        Meses(html).Should().Equal(esperados.Select(RotulosDeProdutividade.Mes));
        // O painel abre no mês corrente, e a atividade antiga não é dele.
        Textos(html, @"<option[^>]*selected[^>]*>\s*([^<]*?)\s*</option>")
            .Should().Equal(RotulosDeProdutividade.Mes(esperados[0]));
        html.Should().NotContain("De três meses atrás");
    }

    [Fact]
    public async Task Abrir_o_painel_fica_no_registro_de_consultas()
    {
        await PainelAsync(_pedro, "Pedro Produção");

        var acessos = await _app.EmEscopoAsync(null, sp =>
            sp.GetRequiredService<AppDbContext>().RegistrosAcesso.AsNoTracking().ToListAsync());
        acessos.Should().Contain(a => a.TipoRecurso == "Produtividade" && a.Acao == AcaoDeAcesso.Visualizou
            && a.RecursoId == _pedro && a.UsuarioId == _admin);
    }

    [Fact]
    public async Task Painel_de_quem_nao_e_produtor_mostra_o_erro_no_lugar_dos_numeros()
    {
        var html = await PainelAsync(_admin, "Diana Diretora");

        html.Should().Contain("Produtor não encontrado.").And.Contain("Tentar de novo");
        html.Should().NotContain("Complexidade 1").And.NotContain("Nenhuma atividade.");
    }
}
```

- [ ] **Step 5: Rodar e ver falhar**

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --filter "FullyQualifiedName~ProdutoresWebTests"`
Expected: FALHA de compilação — `PainelDeProdutividade` não existe.

- [ ] **Step 6: Painel**

Crie `src/AtividadesJuridicas.Web/Components/Produtores/PainelDeProdutividade.razor`:

```razor
@inject IDespachante Despachante
@inject FusoDoEscritorio Fuso

@* Todo texto que os testes procuram sai de uma única expressão: texto fixo junto de expressão pode vir separado. *@
<section class="ficha">
    <h2>@RotulosDeProdutividade.Titulo(ProdutorNome)</h2>

    <div class="acoes">
        <label>
            Mês
            <select value="@RotulosDeProdutividade.Chave(_mes)" disabled="@_carregando" @onchange="MudarMesAsync">
                @foreach (var mes in _meses)
                {
                    <option value="@RotulosDeProdutividade.Chave(mes)">@RotulosDeProdutividade.Mes(mes)</option>
                }
            </select>
        </label>
        <button type="button" class="secundario" @onclick="FecharAsync">Fechar</button>
    </div>

    @if (_erro is not null)
    {
        @* Falha na consulta não é "nenhuma atividade": mostra o erro no lugar dos números. *@
        <p class="erro">@_erro</p>
        <div class="acoes">
            <button type="button" class="secundario" disabled="@_carregando" @onclick="TentarDeNovoAsync">Tentar de novo</button>
        </div>
    }
    else if (_produtividade is not { } p)
    {
        <p>Carregando…</p>
    }
    else
    {
        <table>
            <thead>
                <tr>
                    @foreach (var grupo in p.Grupos)
                    {
                        <th>@RotulosDeProdutividade.Coluna(grupo.Complexidade)</th>
                    }
                    <th>Total</th>
                </tr>
            </thead>
            <tbody>
                <tr>
                    @foreach (var grupo in p.Grupos)
                    {
                        <td>@grupo.Total</td>
                    }
                    <td>@p.Total</td>
                </tr>
            </tbody>
        </table>

        @foreach (var grupo in p.Grupos)
        {
            <h3>@RotulosDeProdutividade.Grupo(grupo)</h3>
            @if (grupo.Total == 0)
            {
                <p>Nenhuma atividade.</p>
            }
            else
            {
                <table>
                    <thead><tr><th>Título</th><th>Cliente</th><th>Concluída em</th><th></th></tr></thead>
                    <tbody>
                        @foreach (var atividade in grupo.Atividades)
                        {
                            <tr @key="atividade.Id">
                                <td>@atividade.Titulo</td>
                                <td>@atividade.ClienteNome</td>
                                <td>@Fuso.Formatar(atividade.ConcluidaEm)</td>
                                <td class="acoes">
                                    <button type="button" class="secundario" @onclick="() => Abrir(atividade.Id)">Abrir</button>
                                </td>
                            </tr>
                        }
                    </tbody>
                </table>
            }
        }
    }
</section>

@if (_atividadeAberta is { } id)
{
    @* A mesma ficha da tela de atividades. O que mudar nela (uma reabertura, a complexidade) muda os números. *@
    <FichaDaAtividade @key="id" AtividadeId="id" OnMudou="RecarregarAsync" OnAcessoEncerrado="EncerrarAcessoAsync"
                      OnFechar="() => FecharFicha(id)" />
}

@code {
    [Parameter, EditorRequired] public Guid ProdutorId { get; set; }

    [Parameter, EditorRequired] public string ProdutorNome { get; set; } = string.Empty;

    /// <summary>Avisado quando a consulta responde acesso negado: a página decide o que mostrar.</summary>
    [Parameter] public EventCallback OnAcessoEncerrado { get; set; }

    [Parameter] public EventCallback OnFechar { get; set; }

    private bool _carregando;
    private int _geracao;
    private string? _erro;
    private DateOnly _mes;
    private IReadOnlyList<DateOnly> _meses = [];
    private ProdutividadeDoProdutor? _produtividade;
    private Guid? _atividadeAberta;

    protected override async Task OnInitializedAsync()
    {
        // Abre no mês corrente. Os meses anteriores entram no seletor quando a consulta diz desde quando há histórico.
        _mes = RotulosDeProdutividade.MesDe(Hoje());
        _meses = [_mes];
        await CarregarAsync();
    }

    /// <summary>O dia de hoje no fuso do escritório.</summary>
    private DateOnly Hoje() => DateOnly.FromDateTime(Fuso.ParaLocal(DateTime.UtcNow));

    private async Task MudarMesAsync(ChangeEventArgs e)
    {
        // Só vale um mês que o seletor ofereceu.
        if (_carregando || !RotulosDeProdutividade.TentarLer(e.Value as string, out var mes) || !_meses.Contains(mes)) return;

        _mes = mes;
        _atividadeAberta = null;   // a ficha aberta era de uma atividade do mês que saiu da tela
        await CarregarAsync();
    }

    private Task TentarDeNovoAsync() => _carregando ? Task.CompletedTask : CarregarAsync();

    /// <summary>A ficha avisou que algo mudou na atividade: os números do mês são consultados de novo.</summary>
    private Task RecarregarAsync() => CarregarAsync();

    private async Task CarregarAsync()
    {
        // Uma resposta que chega depois de outra consulta mais nova é descartada.
        var geracao = ++_geracao;
        var mes = _mes;
        _carregando = true;
        try
        {
            var (deUtc, ateUtc) = RotulosDeProdutividade.Intervalo(Fuso, mes);
            var resultado = await Despachante.EnviarAsync(new ObterProdutividadeDoProdutor(ProdutorId, deUtc, ateUtc));
            if (geracao != _geracao) return;

            if (resultado.Erro == TipoErro.AcessoNegado)
            {
                await EncerrarAcessoAsync();
                return;
            }

            if (resultado.Valor is not { } produtividade)
            {
                // Números antigos não ficam na tela depois de uma falha.
                _produtividade = null;
                _erro = resultado.Mensagem ?? "Não foi possível consultar a produtividade.";
                return;
            }

            _erro = null;
            _produtividade = produtividade;

            // O seletor vai do mês corrente até o da primeira conclusão; o mês em exibição nunca sai dele.
            var maisAntigo = produtividade.PrimeiraConclusaoEm is { } primeira
                ? RotulosDeProdutividade.MesDe(Fuso, primeira)
                : mes;
            _meses = RotulosDeProdutividade.Lista(Hoje(), maisAntigo < mes ? maisAntigo : mes);
        }
        finally
        {
            if (geracao == _geracao) _carregando = false;
        }
    }

    private void Abrir(Guid atividadeId) => _atividadeAberta = atividadeId;

    private void FecharFicha(Guid idDaFicha)
    {
        if (_atividadeAberta == idDaFicha) _atividadeAberta = null;
    }

    private async Task EncerrarAcessoAsync()
    {
        _produtividade = null;
        _atividadeAberta = null;
        _erro = null;
        await OnAcessoEncerrado.InvokeAsync();
    }

    private async Task FecharAsync() => await OnFechar.InvokeAsync();
}
```

- [ ] **Step 7: Página, importação e menu**

Em `src/AtividadesJuridicas.Web/Components/_Imports.razor`, acrescente por último:

```razor
@using AtividadesJuridicas.Web.Components.Produtores
```

Crie `src/AtividadesJuridicas.Web/Components/Pages/Produtores.razor`:

```razor
@page "/produtores"
@inject IDespachante Despachante

<PageTitle>Produtores</PageTitle>

<h1>Produtores</h1>

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
    @if (_produtores.Count == 0)
    {
        <p>Nenhum produtor.</p>
    }
    else
    {
        <table>
            <thead>
                <tr><th>Nome</th><th>Perfil</th><th>OAB</th><th>Situação</th><th></th></tr>
            </thead>
            <tbody>
                @foreach (var produtor in _produtores)
                {
                    <tr class="@(produtor.Ativo ? null : "inativo")" @key="produtor.Id">
                        <td>@produtor.Nome</td>
                        <td>@produtor.PerfilNome</td>
                        <td>@produtor.NumeroOab</td>
                        <td>@(produtor.Ativo ? "Ativo" : "Inativo")</td>
                        <td class="acoes">
                            <button type="button" class="secundario" @onclick="() => Abrir(produtor)">Acompanhar produtividade</button>
                        </td>
                    </tr>
                }
            </tbody>
        </table>
    }

    @if (_aberto is { } aberto)
    {
        @* O aviso de fechar leva o id do painel que o enviou: o de um painel que já não é o aberto é ignorado. *@
        <PainelDeProdutividade @key="aberto.Id" ProdutorId="aberto.Id" ProdutorNome="@aberto.Nome"
                               OnAcessoEncerrado="EncerrarAcesso" OnFechar="() => Fechar(aberto.Id)" />
    }
}

@code {
    private bool _carregado;
    private bool _autorizado;
    private IReadOnlyList<UsuarioResumo> _produtores = [];
    private UsuarioResumo? _aberto;

    protected override async Task OnInitializedAsync()
    {
        // Quem não é da Diretoria nem chega a pedir a lista de usuários: abrir a página não vira acesso negado na auditoria.
        _autorizado = await Despachante.EnviarAsync(new ObterMeuAcesso()) is { EhDiretoria: true };
        if (_autorizado)
        {
            var resultado = await Despachante.EnviarAsync(new ListarUsuarios());
            _autorizado = resultado.Sucesso;

            // Produtor é quem tem hoje um perfil de Produção. Os inativos ficam, para a consulta de meses passados.
            _produtores = [.. (resultado.Valor ?? [])
                .Where(u => u.TipoPerfil == TipoPerfil.Producao)
                .OrderBy(u => u.Nome, StringComparer.CurrentCultureIgnoreCase)];
        }

        _carregado = true;
    }

    private void Abrir(UsuarioResumo produtor) => _aberto = produtor;

    private void Fechar(Guid idDoPainel)
    {
        if (_aberto?.Id == idDoPainel) _aberto = null;
    }

    /// <summary>Uma resposta de acesso negado encerra a tela: nada do que estava aberto continua visível.</summary>
    private void EncerrarAcesso()
    {
        _autorizado = false;
        _produtores = [];
        _aberto = null;
    }
}
```

Em `src/AtividadesJuridicas.Web/Components/Layout/MainLayout.razor`, depois de `<NavLink href="/usuarios">Usuários</NavLink>`:

```razor
            <NavLink href="/produtores">Produtores</NavLink>
```

- [ ] **Step 8: Rodar tudo**

Run: `dotnet build`
Expected: zero avisos.

Run: `dotnet test`
Expected: PASS, incluindo os nove testes de `ProdutoresWebTests` e os oito de `RotulosDeProdutividadeTests`.

- [ ] **Step 9: Conferência por leitura**

O que depende de clique não tem teste automatizado. Confira lendo o código e descreva no relatório, caminho por caminho:

- "Acompanhar produtividade" abre o painel abaixo da tabela; abrir outro produtor troca o painel (a chave do painel é o id do produtor, então ele recomeça do mês corrente).
- Trocar o mês consulta de novo, com o seletor desabilitado enquanto carrega, e fecha a ficha que estivesse aberta.
- "Abrir" mostra a ficha da atividade abaixo do painel; uma mudança feita nela (reabrir, editar a complexidade) refaz a consulta do mês; "Fechar" da ficha a retira.
- Uma falha de consulta troca os números pelo erro e por "Tentar de novo"; acesso negado encerra a página inteira.
- "Fechar" do painel o retira da página.

Se o aplicativo puder ser aberto no navegador, percorra esses mesmos caminhos nele e diga no relatório o que foi visto.

- [ ] **Step 10: Commit**

```bash
git add src tests
git commit -m "feat(web): tela de produtores com painel de produtividade por mês e por complexidade"
```

---

### Task 5: Emendar a modelagem de domínio

**Files:**
- Modify: `docs/superpowers/specs/2026-10-06-modelagem-dominio-design.md`
- Modify: `docs/superpowers/specs/2026-10-08-complexidade-e-produtividade-design.md`

**Interfaces:**
- Consumes: as regras implementadas nas Tarefas 1 a 4. Antes de escrever, confira cada frase abaixo contra o código; se o que foi implementado difere, pare e registre a diferença no relatório em vez de adaptar o texto.
- Produces: nada para outras tarefas.

- [ ] **Step 1: Matriz de acesso (seção 5)**

Na tabela "Matriz de acesso", depois da linha de "Auditoria":

```markdown
| Produtividade | Consulta, por produtor e por mês | Sem acesso |
```

- [ ] **Step 2: Campo e regras da complexidade (seção 8)**

Na tabela de campos de "Atividade (raiz do agregado)", depois da linha de `DataEntrega`:

```markdown
| `Complexidade` | Obrigatória; inteiro de 1 a 3 |
```

Depois do bloco "Regras de vínculo", acrescente:

```markdown
Regras da complexidade:

- É informada junto com título, descrição e data de entrega, na criação e na edição, pela Diretoria.
- Um valor fora de 1 a 3 é recusado pelo domínio ("A complexidade deve ser 1, 2 ou 3.").
- Pode ser alterada em qualquer status, inclusive em atividade concluída. A alteração fica no registro de alterações da atividade, sem evento próprio.
- As atividades que existiam antes do campo receberam complexidade 1.
```

- [ ] **Step 3: Subseção de produtividade (final da seção 8)**

Ao final da seção 8, depois da lista que encerra "Permissões":

```markdown
### Produtividade

A Diretoria acompanha, por produtor e por mês, as atividades realizadas, separadas por complexidade.

- Produtor é o usuário cujo perfil atual é de tipo `Producao`, ativo ou inativo. Um usuário de Diretoria não é produtor, mesmo sendo responsável em atividades. Vale o perfil atual: quem muda de tipo entra ou sai da lista.
- Uma atividade é realizada por um produtor em um período quando, no momento da consulta, está em `Concluida`, não foi excluída, `ConcluidaEm` cai no período e o produtor é um dos responsáveis.
- Uma atividade com vários responsáveis conta uma vez para cada um.
- Os números são calculados a cada consulta, sobre o estado atual. Uma atividade reaberta deixa de contar e volta a contar no mês da nova conclusão; vale a complexidade atual. Por isso os números de um mês passado mudam quando uma atividade dele é reaberta, excluída ou tem a complexidade alterada.
- A consulta recebe o produtor e um período em UTC de no máximo 92 dias; a tela pede um mês por vez, no fuso do escritório. Ela devolve um grupo por complexidade, sempre os três, cada um com as atividades da conclusão mais antiga para a mais recente, e a data da primeira conclusão do produtor.
- Quem não é da Diretoria recebe `AcessoNegado`. Um identificador que não é de um produtor responde `NaoEncontrado`, sem evento de acesso negado: a Diretoria pode ver qualquer produtor.
- A tela lista os produtores e abre a produtividade em um painel na própria página, com o seletor de mês indo do mês corrente até o da primeira conclusão do produtor.
```

- [ ] **Step 4: Registro de consultas (seção 10)**

Em "RegistroAcesso", depois do item "Na agenda: …":

```markdown
- Na produtividade: consultar a produtividade de um produtor registra `Visualizou` (recurso `Produtividade`), com o produtor em `RecursoId` e o período em `Detalhe`. A lista de produtores fica registrada pela listagem de usuários, que a tela reutiliza.
```

- [ ] **Step 5: Fora do escopo (seção 13)**

Ao final do parágrafo da seção 13, antes do ponto final, acrescente:

```markdown
; pontuação ponderada, metas e fechamento mensal de produtividade; complexidade configurável
```

- [ ] **Step 6: Estado da spec desta fase**

Em `docs/superpowers/specs/2026-10-08-complexidade-e-produtividade-design.md`, a linha de estado passa a ser:

```markdown
Estado: implementada
```

- [ ] **Step 7: Verificação final**

Run: `dotnet build`
Expected: zero avisos.

Run: `dotnet test`
Expected: PASS em tudo. Compare as contagens com as anotadas no início: os testes novos somam, nenhum sumiu.

Run: `git status --short`
Expected: só os dois arquivos de documentação alterados (e a pasta `.serena/`, que não é rastreada e não entra no commit).

- [ ] **Step 8: Commit**

```bash
git add docs/superpowers/specs
git commit -m "docs: especificação emendada com a complexidade de atividades e a produtividade dos produtores"
```
