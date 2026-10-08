# Fase 3B — Processos: Plano de Implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Cadastrar processos jurídicos vinculados a um ou mais clientes, com área de atuação, situação e número CNJ, auditando também a entrada e a saída de cada cliente.

**Architecture:** `Processo` é um agregado do contexto Cadastros com uma coleção de `ProcessoCliente`, entidade filha que deriva de `Entidade`: assim cada vínculo recebe carimbos, exclusão lógica e registro de auditoria próprios. O agregado referencia `Cliente` e `AreaAtuacao` só por Id. Quando os vínculos de um processo existente mudam, o caso de uso anota um evento no próprio processo, na mesma transação, para que a mudança apareça ao filtrar a auditoria por "Processo".

**Tech Stack:** .NET 10, Blazor Interactive Server, EF Core 10 + Npgsql, PostgreSQL 17 (`unaccent`), xUnit 2.9, FluentAssertions 7.2, Testcontainers.

**Spec:** `docs/superpowers/specs/2026-10-06-modelagem-dominio-design.md` (seções 4, 5, 6 — Processo —, 10, 11 e 12).

## Decisões deste plano

- **Vínculo como entidade filha.** `ProcessoCliente` deriva de `Entidade`, com Id próprio. Remover um cliente do processo é uma exclusão lógica do vínculo, auditada.
- **Mudança de vínculos visível no processo.** Além das linhas de auditoria de cada `ProcessoCliente`, o caso de uso anota o evento `processo_clientes_alterados` com a entidade `Processo`.
- **Área e cliente inativos.** Não entram em vínculos novos; um processo que já os tinha pode ser editado sem trocá-los.
- **Regras que dependem de atividades ficam para a fase 5:** impedir a remoção de um cliente com atividade vinculada ao processo, e propagar a troca de área para as atividades.
- **Sem exclusão de processo pela interface.** A situação (`Arquivado`, `Encerrado`) cobre a necessidade desta fase.

## Como ler este plano

Os **testes** de cada tarefa definem o comportamento exigido. O código de implementação é o desenho pretendido, escrito sem ter sido executado: quando um detalhe do EF Core 10, do Npgsql ou do Blazor exigir, ajuste o mínimo necessário para preservar o comportamento e registre o desvio no relatório, com a mensagem de erro que o motivou. Nunca apague um teste nem afrouxe uma asserção. Ajustar como um teste obtém suas entradas é permitido. O texto de `Dados` gravado em `jsonb` volta reformatado pelo PostgreSQL: compare JSON pela estrutura (`JsonNode.DeepEquals` ou `JsonDocument`), nunca por texto exato.

## Estado de partida (fase 3A, na branch `feat/fase-3-cadastros`)

- Domínio: `AreaAtuacao`, `Cliente` (PF/PJ), `Cpf`, `Cnpj`, `Endereco`.
- Aplicação: casos de uso de áreas e clientes; `IClienteRepositorio`, `IAreaAtuacaoRepositorio`; `DetalheDeConsulta.Serializar` para o detalhe das consultas; `Pagina<T>`; `IAuditoria` (`Anotar`, `RegistrarAsync`).
- Infraestrutura: interceptores de carimbos e de auditoria (tipo raiz, tipos complexos achatados); busca de clientes com `unaccent`; `ExcecaoDeConflito` lançada pela `UnidadeDeTrabalho` em violação de unicidade.
- Guardas: `PropriedadesAuditadasTests` (propriedades auditadas por entidade; todo tipo mapeado deriva de `Entidade`), `GravacoesPorForaTests`.
- Testes: 204 unitários e 139 de integração passando.

## Global Constraints

- TFM único `net10.0` em `Directory.Build.props`. `Nullable` e `TreatWarningsAsErrors=true`. Versões de pacote só em `Directory.Packages.props`. Nenhum pacote novo.
- `Domain` não referencia nada. `Application` não referencia EF Core, Npgsql, `Infrastructure` nem `Web`.
- Código, nomes de tabela e mensagens em português. Tabelas e colunas em `snake_case`. Datas em UTC.
- Exclusão sempre lógica, inclusive dos vínculos. Agregados se referenciam só por Id.
- Toda alteração de `Entidade` gera registro de auditoria na mesma transação. Toda propriedade nova é classificada em `PropriedadesAuditadasTests`. Todo tipo mapeado deriva de `Entidade`. Nenhuma gravação por fora dos interceptores.
- Matriz de acesso: processos — Diretoria tudo, Produção somente leitura.
- Autorização verificada no manipulador; a interface apenas oculta. Enquanto a consulta de acesso não termina, a página mostra "Carregando…".
- Consultas são `IConsultaAuditavel`: listagem registra `Listou`; abrir um registro registra `Visualizou` com o `RecursoId`. O `Detalhe` usa `DetalheDeConsulta.Serializar`.
- Um processo tem ao menos um cliente; um cliente não se repete no mesmo processo. O número CNJ é opcional e único quando informado.
- Buscas por texto não diferenciam maiúsculas nem acentos, e tratam `%` e `_` como caracteres comuns.
- Um caso de uso por arquivo. Novas dependências de construtor entram por último.
- Sem MediatR, sem FluentValidation, sem biblioteca de componentes de UI.

## Review Focus

1. **Remover um cliente de um processo**: o vínculo continua no banco com `excluido_em`, há um registro de exclusão, o processo recarregado não o traz de volta, e o mesmo cliente pode ser vinculado de novo. (Tarefa 2)
2. **Trocar só os clientes de um processo**: a mudança aparece ao filtrar a auditoria por "Processo". (Tarefa 2)
3. **Número CNJ com pontuação diferente** (`1234567-85.2024.8.02.0001` e `12345678520248020001`): é o mesmo número; o segundo cadastro é recusado. Dois processos sem número são aceitos. (Tarefas 1 e 2)
4. **Mesmo cliente informado duas vezes**, ou nenhum cliente: recusado como erro de validação, sem gravar nada. (Tarefas 1 e 2)
5. **Área ou cliente inativo**: recusado em vínculo novo; mantido em processo que já o tinha. (Tarefa 2)
6. **Usuário de Produção**: consulta e fica registrado; não cria, não edita e não muda a situação, e a tentativa fica registrada. (Tarefa 2)

---

## Estrutura de arquivos

```
src/AtividadesJuridicas.Domain/Cadastros/
  NumeroCnj.cs
  Processo.cs (com StatusProcesso, DadosDoProcesso, ClienteDoProcesso, AlteracaoDeClientes)
  ProcessoCliente.cs (com PoloProcessual)
src/AtividadesJuridicas.Application/Cadastros/
  PortasDeProcessos.cs, EventosDeCadastro.cs
  ListarProcessos.cs, ObterProcesso.cs, SalvarProcesso.cs, AlterarStatusProcesso.cs
src/AtividadesJuridicas.Infrastructure/Persistencia/
  Configuracoes/ProcessoConfiguracao.cs
  Repositorios/ProcessoRepositorio.cs
src/AtividadesJuridicas.Web/Components/Pages/
  Processos.razor
```

---

### Task 1: Processo no domínio

**Files:**
- Create em `src/AtividadesJuridicas.Domain/Cadastros/`: `NumeroCnj.cs`, `Processo.cs`, `ProcessoCliente.cs`
- Test em `tests/AtividadesJuridicas.UnitTests/Domain/`: `NumeroCnjTests.cs`, `ProcessoTests.cs`

**Interfaces:**
- Produces (namespace `AtividadesJuridicas.Domain.Cadastros`):
  - `NumeroCnj.Criar(string?)` / `NumeroCnj.TentarCriar(string?, out NumeroCnj?)`; `Valor` (20 dígitos); `Formatado` (`NNNNNNN-DD.AAAA.J.TR.OOOO`).
  - `enum StatusProcesso { Ativo = 1, Suspenso = 2, Arquivado = 3, Encerrado = 4 }`; `enum PoloProcessual { Ativo = 1, Passivo = 2, Terceiro = 3 }`.
  - `record DadosDoProcesso(NumeroCnj? NumeroCnj, string Titulo, Guid AreaAtuacaoId, string? Orgao, string? Comarca, string? ParteContraria, DateOnly? DataDistribuicao, decimal? ValorCausa, string? Observacoes)`.
  - `record ClienteDoProcesso(Guid ClienteId, PoloProcessual Polo)`.
  - `record AlteracaoDeClientes(IReadOnlyList<Guid> Adicionados, IReadOnlyList<Guid> Removidos, IReadOnlyList<Guid> PoloAlterado)` com `Houve`.
  - `Processo.Criar(DadosDoProcesso, IReadOnlyCollection<ClienteDoProcesso>)`, `Alterar(DadosDoProcesso)`, `DefinirClientes(IReadOnlyCollection<ClienteDoProcesso>)` devolvendo `AlteracaoDeClientes`, `AlterarStatus(StatusProcesso)`; propriedades dos dados, `Status` e `Clientes` (`IReadOnlyCollection<ProcessoCliente>`).
  - `ProcessoCliente : Entidade` com `ProcessoId`, `ClienteId`, `Polo`.

- [ ] **Step 1: Escrever os testes que falham**

`tests/AtividadesJuridicas.UnitTests/Domain/NumeroCnjTests.cs`:

```csharp
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.UnitTests.Domain;

public class NumeroCnjTests
{
    [Theory]
    [InlineData("1234567-85.2024.8.02.0001", "12345678520248020001")]
    [InlineData("12345678520248020001", "12345678520248020001")]
    [InlineData(" 0012345-22.2026.5.19.0005 ", "00123452220265190005")]
    [InlineData("7654321-31.2025.8.26.0100", "76543213120258260100")]
    public void Aceita_e_normaliza_para_vinte_digitos(string texto, string valor)
    {
        NumeroCnj.Criar(texto).Valor.Should().Be(valor);
    }

    [Fact]
    public void Formata_no_padrao_do_cnj()
    {
        NumeroCnj.Criar("12345678520248020001").Formatado.Should().Be("1234567-85.2024.8.02.0001");
    }

    [Fact]
    public void Mesmo_numero_com_outra_pontuacao_e_igual()
    {
        NumeroCnj.Criar("1234567-85.2024.8.02.0001").Should().Be(NumeroCnj.Criar("12345678520248020001"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1234567852024802000")]
    [InlineData("123456785202480200011")]
    [InlineData("1234567-86.2024.8.02.0001")]
    [InlineData("1234567-85.2024.8.02.0002")]
    [InlineData("1234567-85.2024.8.02.000a")]
    [InlineData("1234567/85.2024.8.02.0001")]
    public void Recusa_numero_invalido(string? texto)
    {
        NumeroCnj.TentarCriar(texto, out var numero).Should().BeFalse();
        numero.Should().BeNull();

        var acao = () => NumeroCnj.Criar(texto);
        acao.Should().Throw<ExcecaoDeDominio>();
    }
}
```

`tests/AtividadesJuridicas.UnitTests/Domain/ProcessoTests.cs`:

```csharp
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.UnitTests.Domain;

public class ProcessoTests
{
    private static readonly Guid Area = Guid.CreateVersion7();
    private static readonly Guid Maria = Guid.CreateVersion7();
    private static readonly Guid Acme = Guid.CreateVersion7();
    private static readonly Guid Jose = Guid.CreateVersion7();

    private static DadosDoProcesso Dados(string titulo = " Reclamação trabalhista ", decimal? valor = 1500.50m) => new(
        NumeroCnj.Criar("1234567-85.2024.8.02.0001"), titulo, Area, " 2ª Vara do Trabalho ", " Maceió ",
        " Empresa X ", new DateOnly(2024, 3, 10), valor, "  ");

    private static ClienteDoProcesso[] Clientes(params (Guid Id, PoloProcessual Polo)[] itens) =>
        [.. itens.Select(i => new ClienteDoProcesso(i.Id, i.Polo))];

    private static Processo Novo() => Processo.Criar(Dados(), Clientes((Maria, PoloProcessual.Ativo)));

    [Fact]
    public void Nasce_ativo_com_dados_aparados_e_os_clientes_vinculados()
    {
        var processo = Processo.Criar(Dados(), Clientes((Maria, PoloProcessual.Ativo), (Acme, PoloProcessual.Terceiro)));

        processo.Status.Should().Be(StatusProcesso.Ativo);
        processo.NumeroCnj!.Valor.Should().Be("12345678520248020001");
        processo.Titulo.Should().Be("Reclamação trabalhista");
        processo.AreaAtuacaoId.Should().Be(Area);
        processo.Orgao.Should().Be("2ª Vara do Trabalho");
        processo.Comarca.Should().Be("Maceió");
        processo.ParteContraria.Should().Be("Empresa X");
        processo.DataDistribuicao.Should().Be(new DateOnly(2024, 3, 10));
        processo.ValorCausa.Should().Be(1500.50m);
        processo.Observacoes.Should().BeNull();

        processo.Clientes.Should().HaveCount(2);
        processo.Clientes.Should().OnlyContain(c => c.ProcessoId == processo.Id && c.Id != Guid.Empty);
        processo.Clientes.Select(c => (c.ClienteId, c.Polo))
            .Should().BeEquivalentTo([(Maria, PoloProcessual.Ativo), (Acme, PoloProcessual.Terceiro)]);
    }

    [Fact]
    public void Numero_cnj_e_opcional()
    {
        var processo = Processo.Criar(Dados() with { NumeroCnj = null }, Clientes((Maria, PoloProcessual.Ativo)));

        processo.NumeroCnj.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Recusa_titulo_vazio(string titulo)
    {
        var acao = () => Processo.Criar(Dados(titulo), Clientes((Maria, PoloProcessual.Ativo)));

        acao.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void Recusa_area_vazia_valor_negativo_e_textos_acima_do_limite()
    {
        var semArea = () => Processo.Criar(Dados() with { AreaAtuacaoId = Guid.Empty }, Clientes((Maria, PoloProcessual.Ativo)));
        var valorNegativo = () => Processo.Criar(Dados(valor: -0.01m), Clientes((Maria, PoloProcessual.Ativo)));
        var tituloLongo = () => Processo.Criar(Dados(new string('a', 201)), Clientes((Maria, PoloProcessual.Ativo)));
        var comarcaLonga = () => Processo.Criar(Dados() with { Comarca = new string('a', 101) }, Clientes((Maria, PoloProcessual.Ativo)));

        semArea.Should().Throw<ExcecaoDeDominio>();
        valorNegativo.Should().Throw<ExcecaoDeDominio>();
        tituloLongo.Should().Throw<ExcecaoDeDominio>();
        comarcaLonga.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void Recusa_processo_sem_cliente_e_cliente_repetido()
    {
        var semCliente = () => Processo.Criar(Dados(), []);
        var repetido = () => Processo.Criar(Dados(), Clientes((Maria, PoloProcessual.Ativo), (Maria, PoloProcessual.Passivo)));
        var clienteVazio = () => Processo.Criar(Dados(), Clientes((Guid.Empty, PoloProcessual.Ativo)));
        var poloInvalido = () => Processo.Criar(Dados(), Clientes((Maria, (PoloProcessual)9)));

        semCliente.Should().Throw<ExcecaoDeDominio>();
        repetido.Should().Throw<ExcecaoDeDominio>();
        clienteVazio.Should().Throw<ExcecaoDeDominio>();
        poloInvalido.Should().Throw<ExcecaoDeDominio>();
    }

    [Fact]
    public void Alterar_substitui_os_dados_sem_mexer_em_status_nem_em_clientes()
    {
        var processo = Novo();
        processo.AlterarStatus(StatusProcesso.Suspenso);
        var outraArea = Guid.CreateVersion7();

        processo.Alterar(new DadosDoProcesso(null, "Ação de cobrança", outraArea, null, null, null, null, null, "Urgente"));

        processo.Titulo.Should().Be("Ação de cobrança");
        processo.NumeroCnj.Should().BeNull();
        processo.AreaAtuacaoId.Should().Be(outraArea);
        processo.Orgao.Should().BeNull();
        processo.ValorCausa.Should().BeNull();
        processo.Observacoes.Should().Be("Urgente");
        processo.Status.Should().Be(StatusProcesso.Suspenso);
        processo.Clientes.Should().ContainSingle();
    }

    [Fact]
    public void Alteracao_invalida_nao_deixa_o_processo_pela_metade()
    {
        var processo = Novo();

        var acao = () => processo.Alterar(Dados("Outro título", valor: -1m));

        acao.Should().Throw<ExcecaoDeDominio>();
        processo.Titulo.Should().Be("Reclamação trabalhista");
        processo.ValorCausa.Should().Be(1500.50m);
    }

    [Fact]
    public void Definir_clientes_adiciona_remove_e_troca_o_polo_preservando_os_que_ficam()
    {
        var processo = Processo.Criar(Dados(), Clientes((Maria, PoloProcessual.Ativo), (Acme, PoloProcessual.Ativo)));
        var vinculoDaMaria = processo.Clientes.Single(c => c.ClienteId == Maria);

        var alteracao = processo.DefinirClientes(Clientes((Maria, PoloProcessual.Passivo), (Jose, PoloProcessual.Terceiro)));

        alteracao.Houve.Should().BeTrue();
        alteracao.Adicionados.Should().Equal(Jose);
        alteracao.Removidos.Should().Equal(Acme);
        alteracao.PoloAlterado.Should().Equal(Maria);

        processo.Clientes.Select(c => (c.ClienteId, c.Polo)).Should().BeEquivalentTo(
            [(Maria, PoloProcessual.Passivo), (Jose, PoloProcessual.Terceiro)]);
        processo.Clientes.Single(c => c.ClienteId == Maria).Should().BeSameAs(vinculoDaMaria, "o vínculo que fica é o mesmo registro");
    }

    [Fact]
    public void Definir_os_mesmos_clientes_nao_altera_nada()
    {
        var processo = Processo.Criar(Dados(), Clientes((Maria, PoloProcessual.Ativo), (Acme, PoloProcessual.Passivo)));
        var antes = processo.Clientes.Select(c => c.Id).ToList();

        var alteracao = processo.DefinirClientes(Clientes((Acme, PoloProcessual.Passivo), (Maria, PoloProcessual.Ativo)));

        alteracao.Houve.Should().BeFalse();
        processo.Clientes.Select(c => c.Id).Should().BeEquivalentTo(antes);
    }

    [Fact]
    public void Definir_clientes_invalidos_nao_altera_os_vinculos()
    {
        var processo = Novo();

        var semCliente = () => processo.DefinirClientes([]);
        var repetido = () => processo.DefinirClientes(Clientes((Acme, PoloProcessual.Ativo), (Acme, PoloProcessual.Passivo)));

        semCliente.Should().Throw<ExcecaoDeDominio>();
        repetido.Should().Throw<ExcecaoDeDominio>();
        processo.Clientes.Should().ContainSingle().Which.ClienteId.Should().Be(Maria);
    }

    [Fact]
    public void Alterar_status_aceita_so_valores_do_enum()
    {
        var processo = Novo();

        processo.AlterarStatus(StatusProcesso.Encerrado);
        processo.Status.Should().Be(StatusProcesso.Encerrado);

        var acao = () => processo.AlterarStatus((StatusProcesso)99);
        acao.Should().Throw<ExcecaoDeDominio>();
    }
}
```

- [ ] **Step 2: Rodar e confirmar a falha**

Run: `dotnet test tests/AtividadesJuridicas.UnitTests`
Expected: erro de compilação (`NumeroCnj`, `Processo` não existem).

- [ ] **Step 3: Implementar**

`src/AtividadesJuridicas.Domain/Cadastros/NumeroCnj.cs`:

```csharp
using System.Diagnostics.CodeAnalysis;
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Domain.Cadastros;

/// <summary>Numeração única do CNJ: NNNNNNN-DD.AAAA.J.TR.OOOO, com dígito verificador módulo 97.</summary>
public sealed record NumeroCnj
{
    /// <summary>Os vinte dígitos, sem pontuação.</summary>
    public string Valor { get; }

    private NumeroCnj(string valor) => Valor = valor;

    public string Formatado =>
        $"{Valor[..7]}-{Valor[7..9]}.{Valor[9..13]}.{Valor[13]}.{Valor[14..16]}.{Valor[16..]}";

    public static NumeroCnj Criar(string? texto) =>
        TentarCriar(texto, out var numero) ? numero : throw new ExcecaoDeDominio("Número CNJ inválido.");

    public static bool TentarCriar(string? texto, [NotNullWhen(true)] out NumeroCnj? numero)
    {
        numero = null;
        if (texto is null) return false;
        if (texto.Any(c => !char.IsAsciiDigit(c) && c is not ('.' or '-' or ' '))) return false;

        var digitos = new string([.. texto.Where(char.IsAsciiDigit)]);
        if (digitos.Length != 20) return false;

        // O verificador é calculado sobre o número sem os dois dígitos, seguido de "00".
        var semVerificador = digitos[..7] + digitos[9..] + "00";
        var resto = 0;
        foreach (var c in semVerificador) resto = (resto * 10 + (c - '0')) % 97;

        if (98 - resto != int.Parse(digitos[7..9])) return false;

        numero = new NumeroCnj(digitos);
        return true;
    }

    public override string ToString() => Formatado;
}
```

`src/AtividadesJuridicas.Domain/Cadastros/ProcessoCliente.cs`:

```csharp
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Domain.Cadastros;

public enum PoloProcessual
{
    Ativo = 1,
    Passivo = 2,
    Terceiro = 3,
}

/// <summary>
/// Vínculo de um cliente com um processo. É uma entidade, com Id próprio, para que a entrada e a
/// saída do cliente tenham carimbos, exclusão lógica e registro de auditoria.
/// </summary>
public sealed class ProcessoCliente : Entidade
{
    private ProcessoCliente() { }

    internal ProcessoCliente(Guid processoId, Guid clienteId, PoloProcessual polo)
    {
        ProcessoId = processoId;
        ClienteId = clienteId;
        Polo = polo;
    }

    public Guid ProcessoId { get; private set; }
    public Guid ClienteId { get; private set; }
    public PoloProcessual Polo { get; private set; }

    internal void AlterarPolo(PoloProcessual polo) => Polo = polo;
}
```

`src/AtividadesJuridicas.Domain/Cadastros/Processo.cs`:

```csharp
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Domain.Cadastros;

public enum StatusProcesso
{
    Ativo = 1,
    Suspenso = 2,
    Arquivado = 3,
    Encerrado = 4,
}

public sealed record DadosDoProcesso(
    NumeroCnj? NumeroCnj, string Titulo, Guid AreaAtuacaoId, string? Orgao, string? Comarca,
    string? ParteContraria, DateOnly? DataDistribuicao, decimal? ValorCausa, string? Observacoes);

public sealed record ClienteDoProcesso(Guid ClienteId, PoloProcessual Polo);

/// <summary>O que mudou nos vínculos de um processo, pelos Ids dos clientes.</summary>
public sealed record AlteracaoDeClientes(
    IReadOnlyList<Guid> Adicionados, IReadOnlyList<Guid> Removidos, IReadOnlyList<Guid> PoloAlterado)
{
    public bool Houve => Adicionados.Count > 0 || Removidos.Count > 0 || PoloAlterado.Count > 0;
}

public sealed class Processo : Entidade
{
    public const int TamanhoMaximoTitulo = 200;
    public const int TamanhoMaximoOrgao = 200;
    public const int TamanhoMaximoComarca = 100;
    public const int TamanhoMaximoParteContraria = 200;
    public const int TamanhoMaximoObservacoes = 2000;

    private readonly List<ProcessoCliente> _clientes = [];

    private Processo() { }

    public NumeroCnj? NumeroCnj { get; private set; }
    public string Titulo { get; private set; } = null!;
    public Guid AreaAtuacaoId { get; private set; }
    public StatusProcesso Status { get; private set; }
    public string? Orgao { get; private set; }
    public string? Comarca { get; private set; }
    public string? ParteContraria { get; private set; }
    public DateOnly? DataDistribuicao { get; private set; }
    public decimal? ValorCausa { get; private set; }
    public string? Observacoes { get; private set; }

    public IReadOnlyCollection<ProcessoCliente> Clientes => _clientes;

    public static Processo Criar(DadosDoProcesso dados, IReadOnlyCollection<ClienteDoProcesso> clientes)
    {
        Validar(dados);
        Validar(clientes);

        var processo = new Processo { Status = StatusProcesso.Ativo };
        processo.Aplicar(dados);
        foreach (var cliente in clientes)
            processo._clientes.Add(new ProcessoCliente(processo.Id, cliente.ClienteId, cliente.Polo));
        return processo;
    }

    public void Alterar(DadosDoProcesso dados)
    {
        Validar(dados);
        Aplicar(dados);
    }

    /// <summary>
    /// Deixa o processo com exatamente os clientes informados. O vínculo de quem permanece é
    /// preservado (só o polo pode mudar); o de quem sai é removido da coleção.
    /// </summary>
    public AlteracaoDeClientes DefinirClientes(IReadOnlyCollection<ClienteDoProcesso> clientes)
    {
        Validar(clientes);

        var desejados = clientes.ToDictionary(c => c.ClienteId, c => c.Polo);
        var removidos = _clientes.Where(v => !desejados.ContainsKey(v.ClienteId)).ToList();
        var poloAlterado = new List<Guid>();
        var adicionados = new List<Guid>();

        foreach (var vinculo in removidos) _clientes.Remove(vinculo);

        foreach (var (clienteId, polo) in desejados)
        {
            var existente = _clientes.FirstOrDefault(v => v.ClienteId == clienteId);
            if (existente is null)
            {
                _clientes.Add(new ProcessoCliente(Id, clienteId, polo));
                adicionados.Add(clienteId);
            }
            else if (existente.Polo != polo)
            {
                existente.AlterarPolo(polo);
                poloAlterado.Add(clienteId);
            }
        }

        return new AlteracaoDeClientes(adicionados, [.. removidos.Select(v => v.ClienteId)], poloAlterado);
    }

    public void AlterarStatus(StatusProcesso status)
    {
        if (!Enum.IsDefined(status))
            throw new ExcecaoDeDominio("Situação de processo inválida.");
        Status = status;
    }

    private static void Validar(DadosDoProcesso dados)
    {
        ArgumentNullException.ThrowIfNull(dados);
        if (string.IsNullOrWhiteSpace(dados.Titulo))
            throw new ExcecaoDeDominio("O título do processo é obrigatório.");
        if (dados.AreaAtuacaoId == Guid.Empty)
            throw new ExcecaoDeDominio("A área de atuação é obrigatória.");
        if (dados.ValorCausa is < 0)
            throw new ExcecaoDeDominio("O valor da causa não pode ser negativo.");
        Limite(dados.Titulo, TamanhoMaximoTitulo, "título");
        Limite(dados.Orgao, TamanhoMaximoOrgao, "órgão");
        Limite(dados.Comarca, TamanhoMaximoComarca, "comarca");
        Limite(dados.ParteContraria, TamanhoMaximoParteContraria, "parte contrária");
        Limite(dados.Observacoes, TamanhoMaximoObservacoes, "observações");
    }

    private static void Validar(IReadOnlyCollection<ClienteDoProcesso> clientes)
    {
        ArgumentNullException.ThrowIfNull(clientes);
        if (clientes.Count == 0)
            throw new ExcecaoDeDominio("O processo precisa de ao menos um cliente.");
        if (clientes.Any(c => c.ClienteId == Guid.Empty))
            throw new ExcecaoDeDominio("Cliente inválido.");
        if (clientes.Any(c => !Enum.IsDefined(c.Polo)))
            throw new ExcecaoDeDominio("Polo inválido.");
        if (clientes.Select(c => c.ClienteId).Distinct().Count() != clientes.Count)
            throw new ExcecaoDeDominio("Um cliente não pode se repetir no mesmo processo.");
    }

    private void Aplicar(DadosDoProcesso dados)
    {
        NumeroCnj = dados.NumeroCnj;
        Titulo = dados.Titulo.Trim();
        AreaAtuacaoId = dados.AreaAtuacaoId;
        Orgao = Opcional(dados.Orgao);
        Comarca = Opcional(dados.Comarca);
        ParteContraria = Opcional(dados.ParteContraria);
        DataDistribuicao = dados.DataDistribuicao;
        ValorCausa = dados.ValorCausa;
        Observacoes = Opcional(dados.Observacoes);
    }

    private static void Limite(string? valor, int maximo, string campo)
    {
        if (valor is not null && valor.Trim().Length > maximo)
            throw new ExcecaoDeDominio($"O campo {campo} deve ter até {maximo} caracteres.");
    }

    private static string? Opcional(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
```

- [ ] **Step 4: Rodar e confirmar que passa**

Run: `dotnet build` e `dotnet test tests/AtividadesJuridicas.UnitTests`
Expected: build sem avisos; unitários aprovados, incluindo `Domain_nao_referencia_nada_alem_da_biblioteca_base`. Esta tarefa não usa banco: não rode a suíte de integração.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat(cadastros): NumeroCnj, Processo e ProcessoCliente no domínio"
```

---

### Task 2: Processos — casos de uso, persistência e auditoria dos vínculos

A tarefa tem duas partes, feitas em sequência e entregues em um commit só. A Parte A deixa `IProcessoRepositorio` sem implementação registrada, e o host Web valida a injeção de dependências ao subir; por isso a suíte completa só volta a passar ao fim da Parte B.

**Files:**
- Create em `src/AtividadesJuridicas.Application/Cadastros/`: `PortasDeProcessos.cs`, `EventosDeCadastro.cs`, `ListarProcessos.cs`, `ObterProcesso.cs`, `SalvarProcesso.cs`, `AlterarStatusProcesso.cs`
- Modify: `src/AtividadesJuridicas.Application/Comum/Portas.cs` (`ExcecaoDeConflito.Restricao`), `src/AtividadesJuridicas.Application/Cadastros/PortasDeClientes.cs` (`ObterVariosAsync`)
- Create em `src/AtividadesJuridicas.Infrastructure/Persistencia/`: `Configuracoes/ProcessoConfiguracao.cs`, `Repositorios/ProcessoRepositorio.cs`; migration `Processos`
- Modify: `Infrastructure/Persistencia/UnidadeDeTrabalho.cs`, `Repositorios/ClienteRepositorio.cs`, `AppDbContext.cs`, `Infrastructure/DependencyInjection.cs`
- Modify (testes): `tests/AtividadesJuridicas.UnitTests/Application/Falsos.cs`, `tests/AtividadesJuridicas.IntegrationTests/PropriedadesAuditadasTests.cs`
- Test: `tests/AtividadesJuridicas.UnitTests/Application/ProcessosTests.cs`, `tests/AtividadesJuridicas.IntegrationTests/ProcessosTests.cs`

**Interfaces:**
- Consumes: `Processo`, `ProcessoCliente`, `NumeroCnj`, enums e registros da Tarefa 1; `IAreaAtuacaoRepositorio`, `IClienteRepositorio`, `IAuditoria`, `IContextoDeAcesso`, `IUnidadeDeTrabalho`, `Pagina<T>`, `DetalheDeConsulta`, `ClienteMapeamento.Documento`.
- Produces (namespace `AtividadesJuridicas.Application.Cadastros`):
  - `ExcecaoDeConflito.Restricao` (`string?`): nome da restrição de unicidade violada.
  - `IClienteRepositorio.ObterVariosAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct)` devolvendo `IReadOnlyList<Cliente>`.
  - `EventosDeCadastro.ClientesDoProcessoAlterados = "processo_clientes_alterados"`.
  - `IProcessoRepositorio`: `ObterPorIdAsync` (com os vínculos), `ExisteNumeroCnjAsync(NumeroCnj, Guid? excetoId, ct)`, `ListarAsync(FiltroDeProcessos, int pagina, int tamanho, ct)` devolvendo `Pagina<ProcessoResumo>` ordenada por título, `ObterDetalheAsync(Guid, ct)`, `Adicionar(Processo)`.
  - Regra da busca: texto que é um número CNJ válido procura pelo número exato; qualquer outro procura no título e na parte contrária, sem diferenciar maiúsculas nem acentos, com `%` e `_` como caracteres comuns.
  - Entradas e modelos de leitura: ver Step 3.
  - `record ListarProcessos(string? Busca = null, Guid? ClienteId = null, StatusProcesso? Status = null, int Pagina = 1)` — qualquer autenticado; `Listou`.
  - `record ObterProcesso(Guid Id)` — qualquer autenticado; `Visualizou` com `RecursoId`.
  - `record SalvarProcesso(Guid? Id, DadosDoProcessoEntrada Dados, IReadOnlyList<ClienteDoProcessoEntrada> Clientes) : IRequisicao<Resultado<Guid>>` — Diretoria.
  - `record AlterarStatusProcesso(Guid Id, StatusProcesso Status) : IRequisicao<Resultado>` — Diretoria.
  - Manipuladores: `SalvarProcessoManipulador(IContextoDeAcesso, IProcessoRepositorio, IAreaAtuacaoRepositorio, IClienteRepositorio, IUnidadeDeTrabalho, IAuditoria)`; `AlterarStatusProcessoManipulador(IContextoDeAcesso, IProcessoRepositorio, IUnidadeDeTrabalho)`; `ListarProcessosManipulador(IContextoDeAcesso, IProcessoRepositorio)`; `ObterProcessoManipulador(IContextoDeAcesso, IProcessoRepositorio)`.
- Produces (testes): `ProcessoRepositorioFalso` e `Cenario.Processos`.
- Produces (banco): tabelas `processos` e `processos_clientes`; `AppDbContext.Processos`.

#### Parte A — Casos de uso

- [ ] **Step 1: Atualizar os duplos e escrever os testes unitários que falham**

Em `Falsos.cs`:

- Em `ClienteRepositorioFalso`, acrescente:

```csharp
    public Task<IReadOnlyList<Cliente>> ObterVariosAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Cliente>>([.. Itens.Where(c => ids.Contains(c.Id))]);
```

- Acrescente a classe:

```csharp
public sealed class ProcessoRepositorioFalso(AreaAtuacaoRepositorioFalso areas, ClienteRepositorioFalso clientes)
    : IProcessoRepositorio
{
    public List<Processo> Itens { get; } = [];

    public Task<Processo?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Itens.FirstOrDefault(p => p.Id == id));

    public Task<bool> ExisteNumeroCnjAsync(NumeroCnj numero, Guid? excetoId, CancellationToken ct) =>
        Task.FromResult(Itens.Any(p => p.NumeroCnj == numero && p.Id != excetoId));

    public Task<Pagina<ProcessoResumo>> ListarAsync(FiltroDeProcessos filtro, int pagina, int tamanho, CancellationToken ct)
    {
        IEnumerable<Processo> consulta = Itens;
        if (filtro.Status is { } status) consulta = consulta.Where(p => p.Status == status);
        if (filtro.ClienteId is { } clienteId) consulta = consulta.Where(p => p.Clientes.Any(c => c.ClienteId == clienteId));

        if (NumeroCnj.TentarCriar(filtro.Busca, out var numero))
            consulta = consulta.Where(p => p.NumeroCnj == numero);
        else if (!string.IsNullOrWhiteSpace(filtro.Busca))
            consulta = consulta.Where(p => Contem(p.Titulo, filtro.Busca) || Contem(p.ParteContraria, filtro.Busca));

        var todos = consulta.OrderBy(p => p.Titulo).ToList();
        return Task.FromResult(new Pagina<ProcessoResumo>(
            [.. todos.Skip((pagina - 1) * tamanho).Take(tamanho).Select(Resumo)], todos.Count, pagina, tamanho));
    }

    public Task<ProcessoDetalhe?> ObterDetalheAsync(Guid id, CancellationToken ct)
    {
        var p = Itens.FirstOrDefault(x => x.Id == id);
        if (p is null) return Task.FromResult<ProcessoDetalhe?>(null);

        return Task.FromResult<ProcessoDetalhe?>(new ProcessoDetalhe(
            p.Id, p.NumeroCnj?.Formatado, p.Titulo, p.AreaAtuacaoId, NomeDaArea(p.AreaAtuacaoId), p.Status,
            p.Orgao, p.Comarca, p.ParteContraria, p.DataDistribuicao, p.ValorCausa, p.Observacoes,
            [.. p.Clientes.Select(v =>
            {
                var c = clientes.Itens.First(x => x.Id == v.ClienteId);
                return new ClienteDoProcessoDetalhe(c.Id, c.Nome, Documento(c), v.Polo, c.Ativo);
            }).OrderBy(c => c.Nome)]));
    }

    public void Adicionar(Processo processo) => Itens.Add(processo);

    private ProcessoResumo Resumo(Processo p) => new(
        p.Id, p.NumeroCnj?.Formatado, p.Titulo, NomeDaArea(p.AreaAtuacaoId), p.Status,
        [.. p.Clientes.Select(v => clientes.Itens.First(c => c.Id == v.ClienteId).Nome).Order()], p.ParteContraria);

    private string NomeDaArea(Guid id) => areas.Itens.First(a => a.Id == id).Nome;

    private static string Documento(Cliente c) => c switch
    {
        ClientePessoaFisica pf => pf.Cpf.Formatado,
        ClientePessoaJuridica pj => pj.Cnpj.Formatado,
        _ => string.Empty,
    };

    // Mesma semântica da busca real: ignora maiúsculas e acentos.
    private static bool Contem(string? texto, string busca) =>
        texto is not null && SemAcentos(texto).Contains(SemAcentos(busca.Trim()), StringComparison.OrdinalIgnoreCase);

    private static string SemAcentos(string texto) => new([.. texto.Normalize(System.Text.NormalizationForm.FormD)
        .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)]);
}
```

- Em `Cenario`, acrescente a propriedade `public ProcessoRepositorioFalso Processos { get; }` e, no construtor (onde `Usuarios` já é criado), a linha `Processos = new ProcessoRepositorioFalso(Areas, Clientes);`.

`tests/AtividadesJuridicas.UnitTests/Application/ProcessosTests.cs`:

```csharp
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.UnitTests.Application;

public class ProcessosTests
{
    private const string Cnj = "1234567-85.2024.8.02.0001";

    private readonly Cenario _c = new();
    private readonly AreaAtuacao _trabalhista = AreaAtuacao.Criar("Trabalhista");
    private readonly AreaAtuacao _civel = AreaAtuacao.Criar("Cível");
    private readonly ClientePessoaFisica _maria;
    private readonly ClientePessoaJuridica _acme;
    private readonly ClientePessoaFisica _jose;

    public ProcessosTests()
    {
        _c.Areas.Itens.AddRange([_trabalhista, _civel]);
        _maria = ClientePessoaFisica.Criar(DadosDe("Maria Silva"), Cpf.Criar("529.982.247-25"), null, null);
        _acme = ClientePessoaJuridica.Criar(DadosDe("Acme Ltda"), Cnpj.Criar("11.222.333/0001-81"), null, null, null, null);
        _jose = ClientePessoaFisica.Criar(DadosDe("José Souza"), Cpf.Criar("111.444.777-35"), null, null);
        _c.Clientes.Itens.AddRange([_maria, _acme, _jose]);
    }

    private static DadosDoCliente DadosDe(string nome) => new(nome, null, null, Endereco.Vazio, null);

    private Usuario Producao() => _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);

    private DadosDoProcessoEntrada Dados(string titulo = "Reclamação trabalhista", string? cnj = Cnj, Guid? area = null) => new(
        cnj, titulo, area ?? _trabalhista.Id, "2ª Vara", "Maceió", "Empresa X", new DateOnly(2024, 3, 10), 1500.50m, null);

    private static ClienteDoProcessoEntrada[] Vinculos(params (Cliente Cliente, PoloProcessual Polo)[] itens) =>
        [.. itens.Select(i => new ClienteDoProcessoEntrada(i.Cliente.Id, i.Polo))];

    private Task<Resultado<Guid>> Salvar(Usuario? ator, Guid? id, DadosDoProcessoEntrada? dados = null,
        ClienteDoProcessoEntrada[]? clientes = null) =>
        new SalvarProcessoManipulador(_c.Como(ator), _c.Processos, _c.Areas, _c.Clientes, _c.Unidade, _c.Auditoria)
            .ManipularAsync(new SalvarProcesso(
                id, dados ?? Dados(), clientes ?? Vinculos((_maria, PoloProcessual.Ativo))), default);

    private Task<Resultado> AlterarStatus(Usuario? ator, Guid id, StatusProcesso status) =>
        new AlterarStatusProcessoManipulador(_c.Como(ator), _c.Processos, _c.Unidade)
            .ManipularAsync(new AlterarStatusProcesso(id, status), default);

    private Task<Resultado<Pagina<ProcessoResumo>>> Listar(Usuario? ator, string? busca = null, Guid? cliente = null,
        StatusProcesso? status = null) =>
        new ListarProcessosManipulador(_c.Como(ator), _c.Processos)
            .ManipularAsync(new ListarProcessos(busca, cliente, status), default);

    private Task<Resultado<ProcessoDetalhe>> Obter(Usuario? ator, Guid id) =>
        new ObterProcessoManipulador(_c.Como(ator), _c.Processos).ManipularAsync(new ObterProcesso(id), default);

    // ---- Salvar ----

    [Fact]
    public async Task Diretor_cria_processo_com_numero_normalizado_e_clientes()
    {
        var criado = await Salvar(_c.Diretor, null,
            clientes: Vinculos((_maria, PoloProcessual.Ativo), (_acme, PoloProcessual.Passivo)));

        criado.Sucesso.Should().BeTrue(criado.Mensagem);
        var processo = _c.Processos.Itens.Should().ContainSingle().Subject;
        processo.Id.Should().Be(criado.Valor);
        processo.NumeroCnj!.Valor.Should().Be("12345678520248020001");
        processo.Status.Should().Be(StatusProcesso.Ativo);
        processo.Clientes.Should().HaveCount(2);
        _c.Unidade.Salvamentos.Should().Be(1);
        _c.Auditoria.Eventos.Should().BeEmpty("a criação já é auditada pelos registros do processo e dos vínculos");
    }

    [Fact]
    public async Task Numero_em_branco_vira_processo_sem_numero_e_dois_sem_numero_sao_aceitos()
    {
        (await Salvar(_c.Diretor, null, Dados("A", "  "))).Sucesso.Should().BeTrue();
        (await Salvar(_c.Diretor, null, Dados("B", null))).Sucesso.Should().BeTrue();

        _c.Processos.Itens.Should().HaveCount(2).And.OnlyContain(p => p.NumeroCnj == null);
    }

    [Fact]
    public async Task Mesmo_numero_com_outra_pontuacao_e_conflito()
    {
        await Salvar(_c.Diretor, null);

        var repetido = await Salvar(_c.Diretor, null, Dados("Outro", "12345678520248020001"));

        repetido.Erro.Should().Be(TipoErro.Conflito);
        _c.Processos.Itens.Should().ContainSingle();
    }

    [Fact]
    public async Task Editar_mantendo_o_proprio_numero_nao_e_conflito()
    {
        var criado = await Salvar(_c.Diretor, null);

        var editado = await Salvar(_c.Diretor, criado.Valor, Dados("Reclamação trabalhista — fase recursal"));

        editado.Sucesso.Should().BeTrue(editado.Mensagem);
        _c.Processos.Itens.Single().Titulo.Should().Be("Reclamação trabalhista — fase recursal");
    }

    [Theory]
    [InlineData("1234567-86.2024.8.02.0001", "Título")]
    [InlineData(Cnj, "")]
    public async Task Numero_ou_titulo_invalido_e_erro_de_validacao(string cnj, string titulo)
    {
        var resultado = await Salvar(_c.Diretor, null, Dados(titulo, cnj));

        resultado.Erro.Should().Be(TipoErro.Validacao);
        _c.Processos.Itens.Should().BeEmpty();
        _c.Unidade.Salvamentos.Should().Be(0);
    }

    [Fact]
    public async Task Sem_cliente_ou_com_cliente_repetido_e_erro_de_validacao()
    {
        var semCliente = await Salvar(_c.Diretor, null, clientes: []);
        var repetido = await Salvar(_c.Diretor, null,
            clientes: Vinculos((_maria, PoloProcessual.Ativo), (_maria, PoloProcessual.Passivo)));

        semCliente.Erro.Should().Be(TipoErro.Validacao);
        repetido.Erro.Should().Be(TipoErro.Validacao);
        _c.Processos.Itens.Should().BeEmpty();
        _c.Unidade.Salvamentos.Should().Be(0);
    }

    [Fact]
    public async Task Cliente_ou_area_inexistente_e_erro_de_validacao()
    {
        var clienteFantasma = await Salvar(_c.Diretor, null,
            clientes: [new ClienteDoProcessoEntrada(Guid.CreateVersion7(), PoloProcessual.Ativo)]);
        var areaFantasma = await Salvar(_c.Diretor, null, Dados(area: Guid.CreateVersion7()));

        clienteFantasma.Erro.Should().Be(TipoErro.Validacao);
        areaFantasma.Erro.Should().Be(TipoErro.Validacao);
        _c.Processos.Itens.Should().BeEmpty();
    }

    [Fact]
    public async Task Area_inativa_e_recusada_em_processo_novo_e_na_troca_mas_mantida_em_quem_ja_a_tinha()
    {
        var criado = await Salvar(_c.Diretor, null);
        _trabalhista.Inativar();
        _civel.Inativar();

        (await Salvar(_c.Diretor, null, Dados("Novo", null))).Erro.Should().Be(TipoErro.Validacao);
        (await Salvar(_c.Diretor, criado.Valor, Dados(area: _civel.Id))).Erro.Should().Be(TipoErro.Validacao);
        (await Salvar(_c.Diretor, criado.Valor, Dados("Título novo"))).Sucesso.Should().BeTrue();

        _c.Processos.Itens.Single().AreaAtuacaoId.Should().Be(_trabalhista.Id);
    }

    [Fact]
    public async Task Cliente_inativo_e_recusado_em_vinculo_novo_mas_mantido_em_quem_ja_o_tinha()
    {
        var criado = await Salvar(_c.Diretor, null);
        _maria.Inativar();
        _acme.Inativar();

        (await Salvar(_c.Diretor, null, Dados("Novo", null))).Erro.Should().Be(TipoErro.Validacao);
        (await Salvar(_c.Diretor, criado.Valor,
            clientes: Vinculos((_maria, PoloProcessual.Ativo), (_acme, PoloProcessual.Passivo)))).Erro.Should().Be(TipoErro.Validacao);
        (await Salvar(_c.Diretor, criado.Valor,
            clientes: Vinculos((_maria, PoloProcessual.Passivo), (_jose, PoloProcessual.Ativo)))).Sucesso.Should().BeTrue();

        _c.Processos.Itens.Single().Clientes.Select(c => c.ClienteId).Should().BeEquivalentTo([_maria.Id, _jose.Id]);
    }

    [Fact]
    public async Task Trocar_os_clientes_anota_o_evento_no_processo()
    {
        var criado = await Salvar(_c.Diretor, null,
            clientes: Vinculos((_maria, PoloProcessual.Ativo), (_acme, PoloProcessual.Ativo)));

        await Salvar(_c.Diretor, criado.Valor,
            clientes: Vinculos((_maria, PoloProcessual.Passivo), (_jose, PoloProcessual.Terceiro)));

        var evento = _c.Auditoria.Eventos.Should().ContainSingle().Subject;
        evento.Should().BeEquivalentTo(new
        {
            Evento = EventosDeCadastro.ClientesDoProcessoAlterados, Entidade = "Processo", EntidadeId = criado.Valor,
            Imediato = false,
        });
        evento.Dados.Should().BeEquivalentTo(new
        {
            adicionados = new[] { _jose.Id }, removidos = new[] { _acme.Id }, poloAlterado = new[] { _maria.Id },
        });
    }

    [Fact]
    public async Task Editar_sem_mexer_nos_clientes_nao_anota_evento()
    {
        var criado = await Salvar(_c.Diretor, null);

        await Salvar(_c.Diretor, criado.Valor, Dados("Título novo"));

        _c.Auditoria.Eventos.Should().BeEmpty();
    }

    [Fact]
    public async Task Edicao_recusada_nao_altera_o_processo()
    {
        var criado = await Salvar(_c.Diretor, null);

        var resultado = await Salvar(_c.Diretor, criado.Valor, Dados("Título novo"),
            [new ClienteDoProcessoEntrada(Guid.CreateVersion7(), PoloProcessual.Ativo)]);

        resultado.Erro.Should().Be(TipoErro.Validacao);
        var processo = _c.Processos.Itens.Single();
        processo.Titulo.Should().Be("Reclamação trabalhista");
        processo.Clientes.Should().ContainSingle().Which.ClienteId.Should().Be(_maria.Id);
        _c.Auditoria.Eventos.Should().BeEmpty();
    }

    [Fact]
    public async Task Producao_e_anonimo_nao_criam_editam_nem_mudam_a_situacao()
    {
        var criado = await Salvar(_c.Diretor, null);
        var producao = Producao();

        (await Salvar(producao, null, Dados("Outro", null))).Erro.Should().Be(TipoErro.AcessoNegado);
        (await Salvar(producao, criado.Valor, Dados("Invadido"))).Erro.Should().Be(TipoErro.AcessoNegado);
        (await AlterarStatus(producao, criado.Valor, StatusProcesso.Encerrado)).Erro.Should().Be(TipoErro.AcessoNegado);
        (await Salvar(null, null, Dados("Outro", null))).Erro.Should().Be(TipoErro.AcessoNegado);

        var processo = _c.Processos.Itens.Should().ContainSingle().Subject;
        processo.Titulo.Should().Be("Reclamação trabalhista");
        processo.Status.Should().Be(StatusProcesso.Ativo);
    }

    [Fact]
    public async Task Id_inexistente_e_nao_encontrado()
    {
        (await Salvar(_c.Diretor, Guid.CreateVersion7())).Erro.Should().Be(TipoErro.NaoEncontrado);
        (await AlterarStatus(_c.Diretor, Guid.CreateVersion7(), StatusProcesso.Suspenso)).Erro.Should().Be(TipoErro.NaoEncontrado);
        (await Obter(_c.Diretor, Guid.CreateVersion7())).Erro.Should().Be(TipoErro.NaoEncontrado);
    }

    // ---- Situação ----

    [Fact]
    public async Task Diretor_muda_a_situacao_e_definir_a_mesma_nao_grava()
    {
        var criado = await Salvar(_c.Diretor, null);
        var salvamentos = _c.Unidade.Salvamentos;

        (await AlterarStatus(_c.Diretor, criado.Valor, StatusProcesso.Arquivado)).Sucesso.Should().BeTrue();
        (await AlterarStatus(_c.Diretor, criado.Valor, StatusProcesso.Arquivado)).Sucesso.Should().BeTrue();

        _c.Processos.Itens.Single().Status.Should().Be(StatusProcesso.Arquivado);
        _c.Unidade.Salvamentos.Should().Be(salvamentos + 1);
    }

    // ---- Consultar ----

    [Fact]
    public async Task Producao_lista_com_area_clientes_e_numero_formatado()
    {
        await Salvar(_c.Diretor, null,
            clientes: Vinculos((_maria, PoloProcessual.Ativo), (_acme, PoloProcessual.Passivo)));

        var item = (await Listar(Producao())).Valor!.Itens.Should().ContainSingle().Subject;

        item.Should().BeEquivalentTo(new
        {
            NumeroCnj = Cnj, Titulo = "Reclamação trabalhista", AreaNome = "Trabalhista",
            Status = StatusProcesso.Ativo, ParteContraria = "Empresa X",
        });
        item.Clientes.Should().Equal("Acme Ltda", "Maria Silva");
    }

    [Fact]
    public async Task Filtra_por_cliente_situacao_e_texto_sem_acentos()
    {
        var a = await Salvar(_c.Diretor, null, Dados("Reclamação trabalhista", Cnj));
        await Salvar(_c.Diretor, null, Dados("Ação de cobrança", null, _civel.Id), Vinculos((_acme, PoloProcessual.Ativo)));
        await AlterarStatus(_c.Diretor, a.Valor, StatusProcesso.Suspenso);

        async Task<IEnumerable<string>> TitulosAsync(string? busca = null, Guid? cliente = null, StatusProcesso? status = null) =>
            (await Listar(_c.Diretor, busca, cliente, status)).Valor!.Itens.Select(p => p.Titulo);

        (await TitulosAsync()).Should().Equal("Ação de cobrança", "Reclamação trabalhista");
        (await TitulosAsync(cliente: _acme.Id)).Should().Equal("Ação de cobrança");
        (await TitulosAsync(status: StatusProcesso.Suspenso)).Should().Equal("Reclamação trabalhista");
        (await TitulosAsync("reclamacao")).Should().Equal("Reclamação trabalhista");
        (await TitulosAsync("EMPRESA x")).Should().Equal("Ação de cobrança", "Reclamação trabalhista");
        (await TitulosAsync("12345678520248020001")).Should().Equal("Reclamação trabalhista");
        (await TitulosAsync("inexistente")).Should().BeEmpty();
    }

    [Fact]
    public async Task Producao_abre_a_ficha_com_os_clientes_e_seus_documentos()
    {
        var criado = await Salvar(_c.Diretor, null,
            clientes: Vinculos((_maria, PoloProcessual.Ativo), (_acme, PoloProcessual.Passivo)));

        var ficha = (await Obter(Producao(), criado.Valor)).Valor!;

        ficha.Should().BeEquivalentTo(new
        {
            Id = criado.Valor, NumeroCnj = Cnj, Titulo = "Reclamação trabalhista", AreaAtuacaoId = _trabalhista.Id,
            AreaNome = "Trabalhista", Status = StatusProcesso.Ativo, Orgao = "2ª Vara", Comarca = "Maceió",
            ParteContraria = "Empresa X", DataDistribuicao = new DateOnly(2024, 3, 10), ValorCausa = 1500.50m,
        });
        ficha.Clientes.Should().BeEquivalentTo(
        [
            new ClienteDoProcessoDetalhe(_acme.Id, "Acme Ltda", "11.222.333/0001-81", PoloProcessual.Passivo, true),
            new ClienteDoProcessoDetalhe(_maria.Id, "Maria Silva", "529.982.247-25", PoloProcessual.Ativo, true),
        ], opcoes => opcoes.WithStrictOrdering());
    }

    [Fact]
    public async Task Anonimo_nao_consulta()
    {
        var criado = await Salvar(_c.Diretor, null);

        (await Listar(null)).Erro.Should().Be(TipoErro.AcessoNegado);
        (await Obter(null, criado.Valor)).Erro.Should().Be(TipoErro.AcessoNegado);
    }

    [Fact]
    public void Consultas_declaram_o_que_registram()
    {
        var id = Guid.CreateVersion7();
        IConsultaAuditavel listar = new ListarProcessos("Reclamação", id, StatusProcesso.Ativo, 2);
        IConsultaAuditavel obter = new ObterProcesso(id);

        listar.TipoRecurso.Should().Be("Processo");
        listar.Acao.Should().Be(AcaoDeAcesso.Listou);
        listar.RecursoId.Should().BeNull();
        listar.Detalhe.Should().Contain("Reclamação").And.NotContain("\\u00");

        obter.TipoRecurso.Should().Be("Processo");
        obter.Acao.Should().Be(AcaoDeAcesso.Visualizou);
        obter.RecursoId.Should().Be(id);
    }
}
```

- [ ] **Step 2: Rodar e confirmar a falha**

Run: `dotnet test tests/AtividadesJuridicas.UnitTests`
Expected: erro de compilação (`IProcessoRepositorio` e os casos de uso não existem).

- [ ] **Step 3: Implementar portas, eventos e modelos**

Em `src/AtividadesJuridicas.Application/Comum/Portas.cs`, `ExcecaoDeConflito` passa a ser:

```csharp
/// <summary>Outra operação gravou um valor que deveria ser único.</summary>
public sealed class ExcecaoDeConflito(string mensagem, Exception? interna = null, string? restricao = null)
    : Exception(mensagem, interna)
{
    /// <summary>Nome da restrição de unicidade violada, quando o banco o informa.</summary>
    public string? Restricao { get; } = restricao;
}
```

Em `PortasDeClientes.cs`, acrescente a `IClienteRepositorio`:

```csharp
    /// <summary>Os clientes com os Ids informados; os inexistentes simplesmente não vêm.</summary>
    Task<IReadOnlyList<Cliente>> ObterVariosAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct);
```

`src/AtividadesJuridicas.Application/Cadastros/EventosDeCadastro.cs`:

```csharp
namespace AtividadesJuridicas.Application.Cadastros;

public static class EventosDeCadastro
{
    /// <summary>
    /// Os clientes de um processo mudaram. Anotado no processo, para que a mudança apareça ao filtrar
    /// a auditoria por "Processo"; cada vínculo tem ainda o seu próprio registro.
    /// </summary>
    public const string ClientesDoProcessoAlterados = "processo_clientes_alterados";
}
```

`src/AtividadesJuridicas.Application/Cadastros/PortasDeProcessos.cs`:

```csharp
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Domain.Cadastros;

namespace AtividadesJuridicas.Application.Cadastros;

public static class ProcessosOpcoes
{
    public const int TamanhoDaPagina = 20;
}

public sealed record FiltroDeProcessos(string? Busca = null, Guid? ClienteId = null, StatusProcesso? Status = null);

public interface IProcessoRepositorio
{
    /// <summary>O processo com os seus vínculos de cliente.</summary>
    Task<Processo?> ObterPorIdAsync(Guid id, CancellationToken ct);

    Task<bool> ExisteNumeroCnjAsync(NumeroCnj numero, Guid? excetoId, CancellationToken ct);

    /// <summary>
    /// Ordenado por título. Texto que é um número CNJ válido procura pelo número exato; qualquer outro
    /// procura no título e na parte contrária, sem diferenciar maiúsculas nem acentos, tratando % e _
    /// como caracteres comuns.
    /// </summary>
    Task<Pagina<ProcessoResumo>> ListarAsync(FiltroDeProcessos filtro, int pagina, int tamanho, CancellationToken ct);

    Task<ProcessoDetalhe?> ObterDetalheAsync(Guid id, CancellationToken ct);

    void Adicionar(Processo processo);
}

public sealed record DadosDoProcessoEntrada(
    string? NumeroCnj, string Titulo, Guid AreaAtuacaoId, string? Orgao, string? Comarca, string? ParteContraria,
    DateOnly? DataDistribuicao, decimal? ValorCausa, string? Observacoes);

public sealed record ClienteDoProcessoEntrada(Guid ClienteId, PoloProcessual Polo);

/// <summary><paramref name="NumeroCnj"/> vem formatado; <paramref name="Clientes"/> são os nomes, em ordem alfabética.</summary>
public sealed record ProcessoResumo(
    Guid Id, string? NumeroCnj, string Titulo, string AreaNome, StatusProcesso Status,
    IReadOnlyList<string> Clientes, string? ParteContraria);

/// <summary><paramref name="Documento"/> vem formatado (CPF ou CNPJ).</summary>
public sealed record ClienteDoProcessoDetalhe(Guid ClienteId, string Nome, string Documento, PoloProcessual Polo, bool Ativo);

/// <summary><paramref name="NumeroCnj"/> vem formatado; <paramref name="Clientes"/> em ordem alfabética.</summary>
public sealed record ProcessoDetalhe(
    Guid Id, string? NumeroCnj, string Titulo, Guid AreaAtuacaoId, string AreaNome, StatusProcesso Status,
    string? Orgao, string? Comarca, string? ParteContraria, DateOnly? DataDistribuicao, decimal? ValorCausa,
    string? Observacoes, IReadOnlyList<ClienteDoProcessoDetalhe> Clientes);
```

- [ ] **Step 4: Implementar os casos de uso**

`src/AtividadesJuridicas.Application/Cadastros/ListarProcessos.cs`:

```csharp
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Cadastros;

namespace AtividadesJuridicas.Application.Cadastros;

public sealed record ListarProcessos(
    string? Busca = null, Guid? ClienteId = null, StatusProcesso? Status = null, int Pagina = 1)
    : IRequisicao<Resultado<Pagina<ProcessoResumo>>>, IConsultaAuditavel
{
    public string TipoRecurso => "Processo";
    public AcaoDeAcesso Acao => AcaoDeAcesso.Listou;
    public string? Detalhe => DetalheDeConsulta.Serializar(new { Busca, ClienteId, Status, Pagina });
}

public sealed class ListarProcessosManipulador(IContextoDeAcesso contexto, IProcessoRepositorio processos)
    : IManipulador<ListarProcessos, Resultado<Pagina<ProcessoResumo>>>
{
    public async Task<Resultado<Pagina<ProcessoResumo>>> ManipularAsync(ListarProcessos requisicao, CancellationToken ct)
    {
        // Diretoria e Produção consultam processos; só a Diretoria altera.
        if (await contexto.ObterAsync(ct) is null)
            return Resultado<Pagina<ProcessoResumo>>.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        return Resultado<Pagina<ProcessoResumo>>.Ok(await processos.ListarAsync(
            new FiltroDeProcessos(requisicao.Busca, requisicao.ClienteId, requisicao.Status),
            Math.Max(1, requisicao.Pagina), ProcessosOpcoes.TamanhoDaPagina, ct));
    }
}
```

`src/AtividadesJuridicas.Application/Cadastros/ObterProcesso.cs`:

```csharp
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;

namespace AtividadesJuridicas.Application.Cadastros;

public sealed record ObterProcesso(Guid Id) : IRequisicao<Resultado<ProcessoDetalhe>>, IConsultaAuditavel
{
    public string TipoRecurso => "Processo";
    public AcaoDeAcesso Acao => AcaoDeAcesso.Visualizou;
    public Guid? RecursoId => Id;
}

public sealed class ObterProcessoManipulador(IContextoDeAcesso contexto, IProcessoRepositorio processos)
    : IManipulador<ObterProcesso, Resultado<ProcessoDetalhe>>
{
    public async Task<Resultado<ProcessoDetalhe>> ManipularAsync(ObterProcesso requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is null)
            return Resultado<ProcessoDetalhe>.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        var detalhe = await processos.ObterDetalheAsync(requisicao.Id, ct);
        return detalhe is null
            ? Resultado<ProcessoDetalhe>.Falha(TipoErro.NaoEncontrado, "Processo não encontrado.")
            : Resultado<ProcessoDetalhe>.Ok(detalhe);
    }
}
```

`src/AtividadesJuridicas.Application/Cadastros/SalvarProcesso.cs`:

```csharp
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Application.Cadastros;

/// <summary>Cria o processo quando <paramref name="Id"/> é nulo; caso contrário, altera os dados e os clientes.</summary>
public sealed record SalvarProcesso(
    Guid? Id, DadosDoProcessoEntrada Dados, IReadOnlyList<ClienteDoProcessoEntrada> Clientes)
    : IRequisicao<Resultado<Guid>>;

public sealed class SalvarProcessoManipulador(
    IContextoDeAcesso contexto,
    IProcessoRepositorio processos,
    IAreaAtuacaoRepositorio areas,
    IClienteRepositorio clientes,
    IUnidadeDeTrabalho unidade,
    IAuditoria auditoria) : IManipulador<SalvarProcesso, Resultado<Guid>>
{
    private const string NumeroEmUso = "Já existe um processo com este número.";

    public async Task<Resultado<Guid>> ManipularAsync(SalvarProcesso requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Falha(TipoErro.AcessoNegado, "Acesso negado.");

        Processo? existente = null;
        if (requisicao.Id is { } id)
        {
            existente = await processos.ObterPorIdAsync(id, ct);
            if (existente is null) return Falha(TipoErro.NaoEncontrado, "Processo não encontrado.");
        }

        // Tudo é verificado antes de qualquer alteração, para não deixar o processo pela metade.
        NumeroCnj? numero = null;
        if (!string.IsNullOrWhiteSpace(requisicao.Dados.NumeroCnj) &&
            !NumeroCnj.TentarCriar(requisicao.Dados.NumeroCnj, out numero))
            return Falha(TipoErro.Validacao, "Número CNJ inválido.");

        if (numero is not null && await processos.ExisteNumeroCnjAsync(numero, existente?.Id, ct))
            return Falha(TipoErro.Conflito, NumeroEmUso);

        var area = await areas.ObterPorIdAsync(requisicao.Dados.AreaAtuacaoId, ct);
        if (area is null) return Falha(TipoErro.Validacao, "Área de atuação não encontrada.");

        // Área inativa não entra em processo novo nem em troca; quem já a tinha pode mantê-la.
        if (!area.Ativo && existente?.AreaAtuacaoId != area.Id)
            return Falha(TipoErro.Validacao, "A área de atuação está inativa.");

        var pedidos = requisicao.Clientes ?? [];
        var ids = pedidos.Select(c => c.ClienteId).Distinct().ToList();
        var encontrados = await clientes.ObterVariosAsync(ids, ct);
        if (encontrados.Count != ids.Count) return Falha(TipoErro.Validacao, "Cliente não encontrado.");

        // Cliente inativo não entra em vínculo novo; quem já estava no processo pode permanecer.
        var jaVinculados = existente?.Clientes.Select(c => c.ClienteId).ToHashSet() ?? [];
        var inativoNovo = encontrados.FirstOrDefault(c => !c.Ativo && !jaVinculados.Contains(c.Id));
        if (inativoNovo is not null)
            return Falha(TipoErro.Validacao, $"O cliente {inativoNovo.Nome} está inativo.");

        try
        {
            var d = requisicao.Dados;
            var dados = new DadosDoProcesso(
                numero, d.Titulo, d.AreaAtuacaoId, d.Orgao, d.Comarca, d.ParteContraria,
                d.DataDistribuicao, d.ValorCausa, d.Observacoes);
            var vinculos = pedidos.Select(c => new ClienteDoProcesso(c.ClienteId, c.Polo)).ToList();

            if (existente is null)
            {
                existente = Processo.Criar(dados, vinculos);
                processos.Adicionar(existente);
            }
            else
            {
                // Cada chamada valida tudo antes de aplicar. Se a segunda falhar, o que a primeira aplicou
                // fica só na entidade em memória: nada é gravado, e o escopo da requisição é descartado.
                existente.Alterar(dados);
                var alteracao = existente.DefinirClientes(vinculos);

                if (alteracao.Houve)
                    auditoria.Anotar(EventosDeCadastro.ClientesDoProcessoAlterados, "Processo", existente.Id, new
                    {
                        adicionados = alteracao.Adicionados,
                        removidos = alteracao.Removidos,
                        poloAlterado = alteracao.PoloAlterado,
                    });
            }

            await unidade.SalvarAsync(ct);
            return Resultado<Guid>.Ok(existente.Id);
        }
        catch (ExcecaoDeDominio ex)
        {
            return Falha(TipoErro.Validacao, ex.Message);
        }
        catch (ExcecaoDeConflito ex)
        {
            return Falha(TipoErro.Conflito,
                ex.Restricao?.Contains("numero_cnj", StringComparison.OrdinalIgnoreCase) == true
                    ? NumeroEmUso
                    : "Os clientes do processo não podem se repetir.");
        }
    }

    private static Resultado<Guid> Falha(TipoErro erro, string mensagem) => Resultado<Guid>.Falha(erro, mensagem);
}
```

`src/AtividadesJuridicas.Application/Cadastros/AlterarStatusProcesso.cs`:

```csharp
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Application.Cadastros;

public sealed record AlterarStatusProcesso(Guid Id, StatusProcesso Status) : IRequisicao<Resultado>;

public sealed class AlterarStatusProcessoManipulador(
    IContextoDeAcesso contexto,
    IProcessoRepositorio processos,
    IUnidadeDeTrabalho unidade) : IManipulador<AlterarStatusProcesso, Resultado>
{
    public async Task<Resultado> ManipularAsync(AlterarStatusProcesso requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Resultado.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        var processo = await processos.ObterPorIdAsync(requisicao.Id, ct);
        if (processo is null)
            return Resultado.Falha(TipoErro.NaoEncontrado, "Processo não encontrado.");

        if (processo.Status == requisicao.Status) return Resultado.Ok();

        try
        {
            processo.AlterarStatus(requisicao.Status);
            await unidade.SalvarAsync(ct);
            return Resultado.Ok();
        }
        catch (ExcecaoDeDominio ex)
        {
            return Resultado.Falha(TipoErro.Validacao, ex.Message);
        }
    }
}
```

- [ ] **Step 5: Rodar os testes unitários**

Run: `dotnet build` e `dotnet test tests/AtividadesJuridicas.UnitTests`
Expected: build sem avisos; unitários aprovados, incluindo `Toda_requisicao_tem_exatamente_um_manipulador_registrado`. Não rode a suíte de integração nem faça commit ainda: siga para a Parte B.

#### Parte B — Persistência e auditoria dos vínculos

- [ ] **Step 6: Escrever os testes de integração que falham**

Em `PropriedadesAuditadasTests.cs`, acrescente ao dicionário esperado:

```csharp
            ["Processo"] =
            [
                "AreaAtuacaoId", "Comarca", "DataDistribuicao", "NumeroCnj", "Observacoes", "Orgao",
                "ParteContraria", "Status", "Titulo", "ValorCausa",
            ],
            ["ProcessoCliente"] = ["ClienteId", "Polo", "ProcessoId"],
```

`tests/AtividadesJuridicas.IntegrationTests/ProcessosTests.cs`:

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
public class ProcessosTests(BancoFixture banco) : TesteComBanco(banco)
{
    private const string Cnj = "1234567-85.2024.8.02.0001";

    private Guid _admin, _trabalhista, _civel, _maria, _acme, _jose;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _admin = await IdDoAdminAsync();
        _trabalhista = (await ExecutarAsync(_admin, new SalvarAreaAtuacao(null, "Trabalhista"))).Valor;
        _civel = (await ExecutarAsync(_admin, new SalvarAreaAtuacao(null, "Cível"))).Valor;
        _maria = await PfAsync("Maria Silva", "529.982.247-25");
        _jose = await PfAsync("José Souza", "111.444.777-35");
        _acme = (await ExecutarAsync(_admin, new SalvarClientePessoaJuridica(
            null, DadosDe("Acme Ltda"), "11.222.333/0001-81", null, null, null, null))).Valor;
    }

    private static DadosDoClienteEntrada DadosDe(string nome) => new(nome, null, null, new EnderecoEntrada(), null);

    private async Task<Guid> PfAsync(string nome, string cpf) =>
        (await ExecutarAsync(_admin, new SalvarClientePessoaFisica(null, DadosDe(nome), cpf, null, null))).Valor;

    private DadosDoProcessoEntrada Dados(string titulo = "Reclamação trabalhista", string? cnj = Cnj, Guid? area = null,
        string? parte = "Empresa X") => new(
        cnj, titulo, area ?? _trabalhista, "2ª Vara do Trabalho", "Maceió", parte, new DateOnly(2024, 3, 10), 1500.50m, "Urgente");

    private static ClienteDoProcessoEntrada[] Vinculos(params (Guid Id, PoloProcessual Polo)[] itens) =>
        [.. itens.Select(i => new ClienteDoProcessoEntrada(i.Id, i.Polo))];

    private async Task<Guid> CriarAsync(DadosDoProcessoEntrada? dados = null, ClienteDoProcessoEntrada[]? clientes = null)
    {
        var criado = await ExecutarAsync(_admin, new SalvarProcesso(
            null, dados ?? Dados(), clientes ?? Vinculos((_maria, PoloProcessual.Ativo), (_acme, PoloProcessual.Passivo))));
        criado.Sucesso.Should().BeTrue(criado.Mensagem);
        return criado.Valor;
    }

    private async Task<Guid> ProducaoAsync()
    {
        var perfil = await IdDoPerfilAsync(TipoPerfil.Producao);
        return (await ExecutarAsync(_admin, new CriarUsuario("Pedro", "pedro@x.com", "senha-do-pedro", perfil, null))).Valor;
    }

    private Task<List<ProcessoCliente>> VinculosNoBancoAsync(Guid processoId) => EmEscopoAsync(null, sp =>
        sp.GetRequiredService<AppDbContext>().Set<ProcessoCliente>().IgnoreQueryFilters().AsNoTracking()
            .Where(v => v.ProcessoId == processoId).OrderBy(v => v.CriadoEm).ThenBy(v => v.Id).ToListAsync());

    [Fact]
    public async Task Processo_vai_e_volta_do_banco_inteiro_com_os_clientes()
    {
        var id = await CriarAsync();

        var ficha = (await ExecutarAsync(_admin, new ObterProcesso(id))).Valor!;

        ficha.Should().BeEquivalentTo(new
        {
            Id = id, NumeroCnj = Cnj, Titulo = "Reclamação trabalhista", AreaAtuacaoId = _trabalhista, AreaNome = "Trabalhista",
            Status = StatusProcesso.Ativo, Orgao = "2ª Vara do Trabalho", Comarca = "Maceió", ParteContraria = "Empresa X",
            DataDistribuicao = new DateOnly(2024, 3, 10), ValorCausa = 1500.50m, Observacoes = "Urgente",
        });
        ficha.Clientes.Should().BeEquivalentTo(
        [
            new ClienteDoProcessoDetalhe(_acme, "Acme Ltda", "11.222.333/0001-81", PoloProcessual.Passivo, true),
            new ClienteDoProcessoDetalhe(_maria, "Maria Silva", "529.982.247-25", PoloProcessual.Ativo, true),
        ], opcoes => opcoes.WithStrictOrdering());
    }

    [Fact]
    public async Task Criacao_audita_o_processo_e_cada_vinculo()
    {
        var id = await CriarAsync();
        var vinculos = await VinculosNoBancoAsync(id);

        var auditoria = await AuditoriaAsync();
        var doProcesso = auditoria.Single(r => r.Entidade == "Processo" && r.EntidadeId == id);
        doProcesso.Operacao.Should().Be(OperacaoAuditada.Criacao);
        var dados = JsonDocument.Parse(doProcesso.Dados!).RootElement;
        dados.GetProperty("NumeroCnj").GetString().Should().Be("12345678520248020001");
        dados.GetProperty("Status").GetString().Should().Be("Ativo");
        dados.GetProperty("AreaAtuacaoId").GetGuid().Should().Be(_trabalhista);
        dados.GetProperty("ValorCausa").GetDecimal().Should().Be(1500.50m);

        var dosVinculos = auditoria.Where(r => r.Entidade == "ProcessoCliente").ToList();
        dosVinculos.Should().HaveCount(2).And.OnlyContain(r => r.Operacao == OperacaoAuditada.Criacao);
        dosVinculos.Select(r => r.EntidadeId).Should().BeEquivalentTo(vinculos.Select(v => (Guid?)v.Id));
        dosVinculos.Should().OnlyContain(r => r.IdCorrelacao == doProcesso.IdCorrelacao);
        var doVinculoDaMaria = JsonDocument.Parse(
            dosVinculos.Single(r => r.EntidadeId == vinculos.Single(v => v.ClienteId == _maria).Id).Dados!).RootElement;
        doVinculoDaMaria.GetProperty("ProcessoId").GetGuid().Should().Be(id);
        doVinculoDaMaria.GetProperty("ClienteId").GetGuid().Should().Be(_maria);
        doVinculoDaMaria.GetProperty("Polo").GetString().Should().Be("Ativo");
    }

    [Fact]
    public async Task Remover_trocar_o_polo_e_adicionar_clientes_e_exclusao_logica_auditada()
    {
        var id = await CriarAsync();
        var antes = await VinculosNoBancoAsync(id);
        var daMaria = antes.Single(v => v.ClienteId == _maria);
        var daAcme = antes.Single(v => v.ClienteId == _acme);

        var editado = await ExecutarAsync(_admin, new SalvarProcesso(
            id, Dados(), Vinculos((_maria, PoloProcessual.Terceiro), (_jose, PoloProcessual.Ativo))));
        editado.Sucesso.Should().BeTrue(editado.Mensagem);

        // O vínculo removido continua no banco, excluído logicamente; o que ficou é o mesmo registro.
        var depois = await VinculosNoBancoAsync(id);
        depois.Should().HaveCount(3);
        var removido = depois.Single(v => v.Id == daAcme.Id);
        removido.ExcluidoEm.Should().NotBeNull();
        removido.ExcluidoPor.Should().Be(_admin);
        depois.Single(v => v.Id == daMaria.Id).Polo.Should().Be(PoloProcessual.Terceiro);
        depois.Single(v => v.Id == daMaria.Id).ExcluidoEm.Should().BeNull();

        // Recarregado, o processo não traz o vínculo removido de volta.
        var ficha = (await ExecutarAsync(_admin, new ObterProcesso(id))).Valor!;
        ficha.Clientes.Select(c => (c.Nome, c.Polo)).Should().Equal(
            ("José Souza", PoloProcessual.Ativo), ("Maria Silva", PoloProcessual.Terceiro));

        // Um registro por vínculo afetado, e o evento no processo.
        var auditoria = await AuditoriaAsync();
        auditoria.Single(r => r.EntidadeId == daAcme.Id && r.Operacao != OperacaoAuditada.Criacao)
            .Operacao.Should().Be(OperacaoAuditada.Exclusao);
        var troca = auditoria.Single(r => r.EntidadeId == daMaria.Id && r.Operacao == OperacaoAuditada.Alteracao);
        var polo = JsonDocument.Parse(troca.Dados!).RootElement.GetProperty("Polo");
        polo.GetProperty("de").GetString().Should().Be("Ativo");
        polo.GetProperty("para").GetString().Should().Be("Terceiro");
        auditoria.Count(r => r.Entidade == "ProcessoCliente" && r.Operacao == OperacaoAuditada.Criacao).Should().Be(3);

        var evento = auditoria.Single(r => r.Evento == EventosDeCadastro.ClientesDoProcessoAlterados);
        evento.Entidade.Should().Be("Processo");
        evento.EntidadeId.Should().Be(id);
        evento.UsuarioId.Should().Be(_admin);
        var dadosDoEvento = JsonDocument.Parse(evento.Dados!).RootElement;
        dadosDoEvento.GetProperty("adicionados").EnumerateArray().Select(e => e.GetGuid()).Should().Equal(_jose);
        dadosDoEvento.GetProperty("removidos").EnumerateArray().Select(e => e.GetGuid()).Should().Equal(_acme);
        dadosDoEvento.GetProperty("poloAlterado").EnumerateArray().Select(e => e.GetGuid()).Should().Equal(_maria);
    }

    [Fact]
    public async Task Cliente_removido_pode_ser_vinculado_de_novo()
    {
        var id = await CriarAsync();
        await ExecutarAsync(_admin, new SalvarProcesso(id, Dados(), Vinculos((_maria, PoloProcessual.Ativo))));

        var deVolta = await ExecutarAsync(_admin, new SalvarProcesso(
            id, Dados(), Vinculos((_maria, PoloProcessual.Ativo), (_acme, PoloProcessual.Terceiro))));

        deVolta.Sucesso.Should().BeTrue(deVolta.Mensagem);
        var daAcme = (await VinculosNoBancoAsync(id)).Where(v => v.ClienteId == _acme).ToList();
        daAcme.Should().HaveCount(2);
        daAcme.Count(v => v.ExcluidoEm == null).Should().Be(1);
    }

    [Fact]
    public async Task Salvar_sem_mudar_nada_nao_gera_registro_nem_evento()
    {
        var id = await CriarAsync();
        var antes = (await AuditoriaAsync()).Count;

        await ExecutarAsync(_admin, new SalvarProcesso(
            id, Dados(cnj: "12345678520248020001"), Vinculos((_acme, PoloProcessual.Passivo), (_maria, PoloProcessual.Ativo))));

        (await AuditoriaAsync()).Should().HaveCount(antes);
    }

    [Fact]
    public async Task Edicao_com_dados_invalidos_nao_muda_os_clientes()
    {
        var id = await CriarAsync();

        var resultado = await ExecutarAsync(_admin, new SalvarProcesso(
            id, Dados(titulo: ""), Vinculos((_jose, PoloProcessual.Ativo))));

        resultado.Erro.Should().Be(TipoErro.Validacao);
        var ficha = (await ExecutarAsync(_admin, new ObterProcesso(id))).Valor!;
        ficha.Titulo.Should().Be("Reclamação trabalhista");
        ficha.Clientes.Select(c => c.Nome).Should().Equal("Acme Ltda", "Maria Silva");
        (await VinculosNoBancoAsync(id)).Should().HaveCount(2).And.OnlyContain(v => v.ExcluidoEm == null);
    }

    [Fact]
    public async Task Numero_duplicado_e_recusado_pelo_caso_de_uso_e_pelo_banco_e_sem_numero_pode_repetir()
    {
        await CriarAsync();

        (await ExecutarAsync(_admin, new SalvarProcesso(null, Dados("Outro", "12345678520248020001"),
            Vinculos((_maria, PoloProcessual.Ativo))))).Erro.Should().Be(TipoErro.Conflito);

        await CriarAsync(Dados("Sem número 1", null));
        await CriarAsync(Dados("Sem número 2", null));

        var direto = () => EmEscopoAsync(_admin, async sp =>
        {
            sp.GetRequiredService<AppDbContext>().Add(Processo.Criar(
                new DadosDoProcesso(NumeroCnj.Criar(Cnj), "Direto", _trabalhista, null, null, null, null, null, null),
                [new ClienteDoProcesso(_maria, PoloProcessual.Ativo)]));
            await sp.GetRequiredService<IUnidadeDeTrabalho>().SalvarAsync(default);
            return 0;
        });
        (await direto.Should().ThrowAsync<ExcecaoDeConflito>()).Which.Restricao.Should().Contain("numero_cnj");
    }

    [Fact]
    public async Task Area_e_cliente_inativos_nao_entram_em_vinculo_novo_mas_ficam_em_quem_ja_os_tinha()
    {
        var id = await CriarAsync();
        await ExecutarAsync(_admin, new DefinirAtivoAreaAtuacao(_trabalhista, false));
        await ExecutarAsync(_admin, new DefinirAtivoCliente(_acme, false));
        await ExecutarAsync(_admin, new DefinirAtivoCliente(_jose, false));

        (await ExecutarAsync(_admin, new SalvarProcesso(null, Dados("Novo", null), Vinculos((_maria, PoloProcessual.Ativo)))))
            .Erro.Should().Be(TipoErro.Validacao, "área inativa não entra em processo novo");
        (await ExecutarAsync(_admin, new SalvarProcesso(null, Dados("Novo", null, _civel), Vinculos((_acme, PoloProcessual.Ativo)))))
            .Erro.Should().Be(TipoErro.Validacao, "cliente inativo não entra em processo novo");
        (await ExecutarAsync(_admin, new SalvarProcesso(id, Dados(), Vinculos((_maria, PoloProcessual.Ativo), (_jose, PoloProcessual.Ativo)))))
            .Erro.Should().Be(TipoErro.Validacao, "cliente inativo não entra em vínculo novo");

        var mantido = await ExecutarAsync(_admin, new SalvarProcesso(
            id, Dados("Título novo"), Vinculos((_maria, PoloProcessual.Ativo), (_acme, PoloProcessual.Terceiro))));
        mantido.Sucesso.Should().BeTrue(mantido.Mensagem);

        var ficha = (await ExecutarAsync(_admin, new ObterProcesso(id))).Valor!;
        ficha.AreaNome.Should().Be("Trabalhista");
        ficha.Clientes.Single(c => c.ClienteId == _acme).Ativo.Should().BeFalse();
    }

    [Fact]
    public async Task Listagem_filtra_por_cliente_situacao_numero_e_texto_sem_acentos_nem_curingas()
    {
        var reclamacao = await CriarAsync();
        await CriarAsync(Dados("Ação de cobrança 100%", null, _civel, "Banco_Y"), Vinculos((_acme, PoloProcessual.Ativo)));
        await ExecutarAsync(_admin, new AlterarStatusProcesso(reclamacao, StatusProcesso.Suspenso));

        async Task<IEnumerable<string>> TitulosAsync(string? busca = null, Guid? cliente = null, StatusProcesso? status = null) =>
            (await ExecutarAsync(_admin, new ListarProcessos(busca, cliente, status))).Valor!.Itens.Select(p => p.Titulo);

        (await TitulosAsync()).Should().Equal("Ação de cobrança 100%", "Reclamação trabalhista");
        (await TitulosAsync(cliente: _maria)).Should().Equal("Reclamação trabalhista");
        (await TitulosAsync(cliente: _acme)).Should().Equal("Ação de cobrança 100%", "Reclamação trabalhista");
        (await TitulosAsync(status: StatusProcesso.Suspenso)).Should().Equal("Reclamação trabalhista");
        (await TitulosAsync("RECLAMACAO")).Should().Equal("Reclamação trabalhista");
        (await TitulosAsync("cobranca")).Should().Equal("Ação de cobrança 100%");
        (await TitulosAsync("empresa")).Should().Equal("Reclamação trabalhista");
        (await TitulosAsync("%")).Should().Equal("Ação de cobrança 100%");
        (await TitulosAsync("_")).Should().Equal("Ação de cobrança 100%");
        (await TitulosAsync(Cnj)).Should().Equal("Reclamação trabalhista");
        (await TitulosAsync("12345678520248020001")).Should().Equal("Reclamação trabalhista");
        (await TitulosAsync("xyz")).Should().BeEmpty();

        var item = (await ExecutarAsync(_admin, new ListarProcessos("reclamacao"))).Valor!.Itens.Single();
        item.Should().BeEquivalentTo(new
        {
            NumeroCnj = Cnj, AreaNome = "Trabalhista", Status = StatusProcesso.Suspenso, ParteContraria = "Empresa X",
        });
        item.Clientes.Should().Equal("Acme Ltda", "Maria Silva");
    }

    [Fact]
    public async Task Cliente_removido_do_processo_some_do_filtro_por_cliente_e_da_listagem()
    {
        var id = await CriarAsync();
        await ExecutarAsync(_admin, new SalvarProcesso(id, Dados(), Vinculos((_maria, PoloProcessual.Ativo))));

        (await ExecutarAsync(_admin, new ListarProcessos(ClienteId: _acme))).Valor!.Itens.Should().BeEmpty();
        (await ExecutarAsync(_admin, new ListarProcessos())).Valor!.Itens.Single().Clientes.Should().Equal("Maria Silva");
    }

    [Fact]
    public async Task Mudanca_de_situacao_e_auditada()
    {
        var id = await CriarAsync();

        (await ExecutarAsync(_admin, new AlterarStatusProcesso(id, StatusProcesso.Encerrado))).Sucesso.Should().BeTrue();

        var alteracao = (await AuditoriaAsync()).Single(r => r.EntidadeId == id && r.Operacao == OperacaoAuditada.Alteracao);
        var status = JsonDocument.Parse(alteracao.Dados!).RootElement.GetProperty("Status");
        status.GetProperty("de").GetString().Should().Be("Ativo");
        status.GetProperty("para").GetString().Should().Be("Encerrado");
    }

    [Fact]
    public async Task Producao_consulta_e_fica_registrado_mas_nao_altera()
    {
        var id = await CriarAsync();
        var pedro = await ProducaoAsync();

        (await ExecutarAsync(pedro, new ListarProcessos("reclamação"))).Valor!.Itens.Should().ContainSingle();
        (await ExecutarAsync(pedro, new ObterProcesso(id))).Sucesso.Should().BeTrue();
        (await ExecutarAsync(pedro, new SalvarProcesso(id, Dados("Invadido"), Vinculos((_maria, PoloProcessual.Ativo)))))
            .Erro.Should().Be(TipoErro.AcessoNegado);
        (await ExecutarAsync(pedro, new AlterarStatusProcesso(id, StatusProcesso.Encerrado))).Erro.Should().Be(TipoErro.AcessoNegado);

        var acessos = await EmEscopoAsync(null, sp => sp.GetRequiredService<AppDbContext>().RegistrosAcesso
            .AsNoTracking().Where(a => a.UsuarioId == pedro && a.TipoRecurso == "Processo").ToListAsync());
        acessos.Should().Contain(a => a.Acao == AcaoDeAcesso.Listou && a.Detalhe!.Contains("reclamação"));
        acessos.Should().Contain(a => a.Acao == AcaoDeAcesso.Visualizou && a.RecursoId == id);

        (await AuditoriaAsync()).Count(r => r.Evento == EventosDeSeguranca.AcessoNegado && r.UsuarioId == pedro).Should().Be(2);
        var ficha = (await ExecutarAsync(_admin, new ObterProcesso(id))).Valor!;
        ficha.Titulo.Should().Be("Reclamação trabalhista");
        ficha.Status.Should().Be(StatusProcesso.Ativo);
    }

    [Fact]
    public async Task Excluir_o_processo_exclui_logicamente_tambem_os_vinculos()
    {
        var id = await CriarAsync();

        await EmEscopoAsync(_admin, async sp =>
        {
            var processo = await sp.GetRequiredService<IProcessoRepositorio>().ObterPorIdAsync(id, default);
            sp.GetRequiredService<AppDbContext>().Remove(processo!);
            await sp.GetRequiredService<IUnidadeDeTrabalho>().SalvarAsync(default);
            return 0;
        });

        (await VinculosNoBancoAsync(id)).Should().HaveCount(2).And.OnlyContain(v => v.ExcluidoEm != null);
        (await ExecutarAsync(_admin, new ObterProcesso(id))).Erro.Should().Be(TipoErro.NaoEncontrado);
        var auditoria = await AuditoriaAsync();
        auditoria.Count(r => r.Operacao == OperacaoAuditada.Exclusao && r.Entidade == "ProcessoCliente").Should().Be(2);
        auditoria.Count(r => r.Operacao == OperacaoAuditada.Exclusao && r.Entidade == "Processo").Should().Be(1);
        await CriarAsync();
    }
}
```

- [ ] **Step 7: Rodar e confirmar a falha**

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --filter "FullyQualifiedName~ProcessosTests|FullyQualifiedName~PropriedadesAuditadasTests"`
Expected: erro de compilação (`IClienteRepositorio.ObterVariosAsync` sem implementação em `ClienteRepositorio`; `Set<ProcessoCliente>` fora do modelo).

- [ ] **Step 8: Implementar o mapeamento, os repositórios e a restrição no conflito**

`src/AtividadesJuridicas.Infrastructure/Persistencia/Configuracoes/ProcessoConfiguracao.cs`:

```csharp
using AtividadesJuridicas.Domain.Cadastros;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AtividadesJuridicas.Infrastructure.Persistencia.Configuracoes;

public sealed class ProcessoConfiguracao : IEntityTypeConfiguration<Processo>
{
    public void Configure(EntityTypeBuilder<Processo> b)
    {
        b.ToTable("processos");

        b.Property(p => p.NumeroCnj)
            .HasConversion(n => n!.Valor, v => NumeroCnj.Criar(v))
            .HasMaxLength(20).IsFixedLength();
        b.Property(p => p.Titulo).HasMaxLength(Processo.TamanhoMaximoTitulo).IsRequired();
        b.Property(p => p.Status).HasConversion<int>();
        b.Property(p => p.Orgao).HasMaxLength(Processo.TamanhoMaximoOrgao);
        b.Property(p => p.Comarca).HasMaxLength(Processo.TamanhoMaximoComarca);
        b.Property(p => p.ParteContraria).HasMaxLength(Processo.TamanhoMaximoParteContraria);
        b.Property(p => p.ValorCausa).HasPrecision(18, 2);
        b.Property(p => p.Observacoes).HasMaxLength(Processo.TamanhoMaximoObservacoes);

        // Único só entre os processos que têm número e não foram excluídos.
        b.HasIndex(p => p.NumeroCnj).IsUnique().HasFilter("numero_cnj IS NOT NULL AND excluido_em IS NULL");
        b.HasIndex(p => p.Titulo);

        b.HasOne<AreaAtuacao>().WithMany().HasForeignKey(p => p.AreaAtuacaoId).OnDelete(DeleteBehavior.Restrict);

        // Os vínculos são gravados pelo campo privado da coleção. O comportamento Cascade faz o EF
        // marcar como excluído o vínculo retirado da coleção (ou o de um processo excluído); o
        // interceptor de carimbos converte essa exclusão em exclusão lógica.
        b.HasMany(p => p.Clientes).WithOne().HasForeignKey(v => v.ProcessoId).OnDelete(DeleteBehavior.Cascade);
        b.Navigation(p => p.Clientes).HasField("_clientes").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class ProcessoClienteConfiguracao : IEntityTypeConfiguration<ProcessoCliente>
{
    public void Configure(EntityTypeBuilder<ProcessoCliente> b)
    {
        b.ToTable("processos_clientes");
        b.Property(v => v.Polo).HasConversion<int>();

        b.HasOne<Cliente>().WithMany().HasForeignKey(v => v.ClienteId).OnDelete(DeleteBehavior.Restrict);

        // Um cliente não se repete entre os vínculos vivos do mesmo processo.
        b.HasIndex(v => new { v.ProcessoId, v.ClienteId }).IsUnique().HasFilter("excluido_em IS NULL");
        b.HasIndex(v => v.ClienteId);
    }
}
```

`src/AtividadesJuridicas.Infrastructure/Persistencia/Repositorios/ProcessoRepositorio.cs`:

```csharp
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Domain.Cadastros;
using Microsoft.EntityFrameworkCore;

namespace AtividadesJuridicas.Infrastructure.Persistencia.Repositorios;

public sealed class ProcessoRepositorio(AppDbContext db) : IProcessoRepositorio
{
    public Task<Processo?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        db.Processos.Include(p => p.Clientes).FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<bool> ExisteNumeroCnjAsync(NumeroCnj numero, Guid? excetoId, CancellationToken ct) =>
        db.Processos.AnyAsync(p => p.NumeroCnj == numero && p.Id != excetoId, ct);

    public async Task<Pagina<ProcessoResumo>> ListarAsync(
        FiltroDeProcessos filtro, int pagina, int tamanho, CancellationToken ct)
    {
        var consulta = db.Processos.AsNoTracking();

        if (filtro.Status is { } status) consulta = consulta.Where(p => p.Status == status);
        if (filtro.ClienteId is { } clienteId) consulta = consulta.Where(p => p.Clientes.Any(v => v.ClienteId == clienteId));

        if (NumeroCnj.TentarCriar(filtro.Busca, out var numero))
        {
            consulta = consulta.Where(p => p.NumeroCnj == numero);
        }
        else if (!string.IsNullOrWhiteSpace(filtro.Busca))
        {
            // % e _ digitados pelo usuário são texto, não curingas.
            var padrao = "%" + filtro.Busca.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            consulta = consulta.Where(p =>
                EF.Functions.ILike(EF.Functions.Unaccent(p.Titulo), EF.Functions.Unaccent(padrao), "\\") ||
                (p.ParteContraria != null &&
                 EF.Functions.ILike(EF.Functions.Unaccent(p.ParteContraria), EF.Functions.Unaccent(padrao), "\\")));
        }

        var total = await consulta.CountAsync(ct);
        var processos = await consulta
            .OrderBy(p => p.Titulo).ThenBy(p => p.Id)
            .Skip((pagina - 1) * tamanho).Take(tamanho)
            .Include(p => p.Clientes)
            .ToListAsync(ct);

        var areas = await NomesDasAreasAsync(processos.Select(p => p.AreaAtuacaoId), ct);
        var clientes = await ClientesAsync(processos.SelectMany(p => p.Clientes).Select(v => v.ClienteId), ct);

        var itens = processos.Select(p => new ProcessoResumo(
            p.Id, p.NumeroCnj?.Formatado, p.Titulo, areas.GetValueOrDefault(p.AreaAtuacaoId, string.Empty), p.Status,
            [.. p.Clientes.Select(v => clientes[v.ClienteId].Nome).Order(StringComparer.CurrentCultureIgnoreCase)],
            p.ParteContraria)).ToList();

        return new Pagina<ProcessoResumo>(itens, total, pagina, tamanho);
    }

    public async Task<ProcessoDetalhe?> ObterDetalheAsync(Guid id, CancellationToken ct)
    {
        var p = await db.Processos.AsNoTracking().Include(x => x.Clientes).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return null;

        var areas = await NomesDasAreasAsync([p.AreaAtuacaoId], ct);
        var clientes = await ClientesAsync(p.Clientes.Select(v => v.ClienteId), ct);

        return new ProcessoDetalhe(
            p.Id, p.NumeroCnj?.Formatado, p.Titulo, p.AreaAtuacaoId, areas.GetValueOrDefault(p.AreaAtuacaoId, string.Empty),
            p.Status, p.Orgao, p.Comarca, p.ParteContraria, p.DataDistribuicao, p.ValorCausa, p.Observacoes,
            [.. p.Clientes
                .Select(v => (Vinculo: v, Cliente: clientes[v.ClienteId]))
                .Select(x => new ClienteDoProcessoDetalhe(
                    x.Cliente.Id, x.Cliente.Nome, Documento(x.Cliente), x.Vinculo.Polo, x.Cliente.Ativo))
                .OrderBy(c => c.Nome, StringComparer.CurrentCultureIgnoreCase)]);
    }

    public void Adicionar(Processo processo) => db.Processos.Add(processo);

    // Áreas e clientes excluídos logicamente continuam identificados pelo nome nos processos que os citam.
    private async Task<Dictionary<Guid, string>> NomesDasAreasAsync(IEnumerable<Guid> ids, CancellationToken ct)
    {
        var lista = ids.Distinct().ToList();
        return await db.AreasAtuacao.IgnoreQueryFilters().AsNoTracking()
            .Where(a => lista.Contains(a.Id)).ToDictionaryAsync(a => a.Id, a => a.Nome, ct);
    }

    private async Task<Dictionary<Guid, Cliente>> ClientesAsync(IEnumerable<Guid> ids, CancellationToken ct)
    {
        var lista = ids.Distinct().ToList();
        return await db.Clientes.IgnoreQueryFilters().AsNoTracking()
            .Where(c => lista.Contains(c.Id)).ToDictionaryAsync(c => c.Id, ct);
    }

    private static string Documento(Cliente cliente) => cliente switch
    {
        ClientePessoaFisica pf => pf.Cpf.Formatado,
        ClientePessoaJuridica pj => pj.Cnpj.Formatado,
        _ => string.Empty,
    };
}
```

Em `ClienteRepositorio.cs`, acrescente:

```csharp
    public async Task<IReadOnlyList<Cliente>> ObterVariosAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct) =>
        await db.Clientes.Where(c => ids.Contains(c.Id)).ToListAsync(ct);
```

Em `UnidadeDeTrabalho.cs`, o `catch` passa a guardar o nome da restrição:

```csharp
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg)
        {
            throw new ExcecaoDeConflito("Já existe um registro com o mesmo valor único.", ex, pg.ConstraintName);
        }
```

Em `AppDbContext.cs`: `public DbSet<Processo> Processos => Set<Processo>();`.
Em `DependencyInjection.cs` (Infrastructure): `servicos.AddScoped<IProcessoRepositorio, ProcessoRepositorio>();`.

- [ ] **Step 9: Gerar a migration**

Run:

```bash
dotnet ef migrations add Processos --project src/AtividadesJuridicas.Infrastructure --startup-project src/AtividadesJuridicas.Infrastructure --output-dir Persistencia/Migrations
```

Expected: cria só as tabelas `processos` e `processos_clientes`. Confira e registre no relatório: `numero_cnj` `character(20)` anulável; `valor_causa` `numeric(18,2)`; `data_distribuicao` `date`; índice único `ix_processos_numero_cnj` com o filtro `numero_cnj IS NOT NULL AND excluido_em IS NULL`; índice único de `processos_clientes` em `(processo_id, cliente_id)` filtrado por `excluido_em IS NULL`; FKs para `areas_atuacao` e `clientes` com `Restrict`.

- [ ] **Step 10: Rodar e confirmar que passa**

Run: `dotnet test`
Expected: tudo aprovado, zero avisos.

Se o teste `Remover_trocar_o_polo_e_adicionar_clientes_e_exclusao_logica_auditada` mostrar que o vínculo retirado da coleção foi removido fisicamente, ou que o EF lança ao romper o relacionamento obrigatório, o defeito está na combinação do relacionamento com o `CarimbosInterceptor`: ajuste o mapeamento do relacionamento (ou, se necessário, o momento em que o interceptor converte a exclusão) preservando o comportamento que os testes fixam, e descreva o que encontrou.

- [ ] **Step 11: Commit**

```bash
git add -A
git commit -m "feat(cadastros): processos — casos de uso, persistência e auditoria dos vínculos com clientes"
```

---

### Task 3: Tela de processos

**Files:**
- Create: `src/AtividadesJuridicas.Web/Components/Pages/Processos.razor`
- Modify: `src/AtividadesJuridicas.Web/Components/Layout/MainLayout.razor`, `src/AtividadesJuridicas.Web/Components/Pages/Clientes.razor`
- Test: `tests/AtividadesJuridicas.IntegrationTests/ProcessosWebTests.cs`

**Interfaces:**
- Consumes: `IDespachante`, `ObterMeuAcesso`, `ListarProcessos`, `ObterProcesso`, `SalvarProcesso`, `AlterarStatusProcesso`, `ListarAreasAtuacao`, `ListarClientes`, e os registros de `PortasDeProcessos.cs`.
- Produces: rota `/processos`; link "Processos" no menu para todo usuário autenticado; na tela de clientes, a lista dos processos do cliente aberto.
- Comportamento:
  - Lista com busca por texto ou número, filtro por situação e paginação.
  - Diretoria: novo processo, editar, mudar a situação. O formulário escolhe a área entre as ativas (mais a área atual do processo, se estiver inativa), e monta a lista de clientes buscando por nome ou documento, com o polo de cada um.
  - Produção: "Ver" abre a ficha em modo leitura.
  - Tela de clientes: ao abrir a ficha (Produção) ou o formulário de edição (Diretoria) de um cliente, mostra os processos dele (título, número, situação), consultados com `ListarProcessos(ClienteId: …)`.

- [ ] **Step 1: Escrever os testes que falham**

`tests/AtividadesJuridicas.IntegrationTests/ProcessosWebTests.cs`:

```csharp
using System.Net;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Domain.Cadastros;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class ProcessosWebTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco);

    public async Task InitializeAsync()
    {
        var admin = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        var area = await _app.ExecutarAsync(admin, new SalvarAreaAtuacao(null, "Trabalhista"));
        var maria = await _app.ExecutarAsync(admin, new SalvarClientePessoaFisica(
            null, new DadosDoClienteEntrada("Maria Silva", null, null, new EnderecoEntrada(), null), "529.982.247-25", null, null));
        var criado = await _app.ExecutarAsync(admin, new SalvarProcesso(
            null,
            new DadosDoProcessoEntrada("1234567-85.2024.8.02.0001", "Reclamação trabalhista", area.Valor, null, "Maceió",
                "Empresa X", null, null, null),
            [new ClienteDoProcessoEntrada(maria.Valor, PoloProcessual.Ativo)]));
        criado.Sucesso.Should().BeTrue(criado.Mensagem);
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    [Fact]
    public async Task Diretor_ve_a_lista_e_as_acoes_de_cadastro()
    {
        var cliente = await _app.ClienteAutenticadoAsync();

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/processos"));

        html.Should().Contain("Processos").And.Contain("Reclamação trabalhista")
            .And.Contain("1234567-85.2024.8.02.0001").And.Contain("Trabalhista").And.Contain("Maria Silva");
        html.Should().Contain("Novo processo").And.Contain("Editar");
    }

    [Fact]
    public async Task Producao_ve_a_lista_mas_nenhuma_acao_de_cadastro()
    {
        await _app.CriarPedroAsync();
        var cliente = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/processos"));

        html.Should().Contain("Reclamação trabalhista").And.Contain("Ver");
        html.Should().NotContain("Novo processo").And.NotContain("Editar").And.NotContain("Seu acesso foi encerrado");
    }

    [Fact]
    public async Task Menu_mostra_processos_para_todos_os_perfis()
    {
        await _app.CriarPedroAsync();
        var doDiretor = await _app.ClienteAutenticadoAsync();
        var doPedro = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        (await doDiretor.GetStringAsync("/")).Should().Contain("href=\"/processos\"");
        (await doPedro.GetStringAsync("/")).Should().Contain("href=\"/processos\"");
    }

    [Fact]
    public async Task Anonimo_e_redirecionado_ao_login()
    {
        var resposta = await _app.NovoCliente().GetAsync("/processos");

        resposta.StatusCode.Should().Be(HttpStatusCode.Redirect);
        resposta.Headers.Location!.AbsolutePath.Should().Be("/conta/entrar");
    }
}
```

- [ ] **Step 2: Rodar e confirmar a falha**

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --filter "FullyQualifiedName~ProcessosWebTests"`
Expected: FAIL. `/processos` responde 404 e o menu não tem o link.

- [ ] **Step 3: Implementar a página**

Em `MainLayout.razor`, logo depois do link "Clientes" e fora do bloco da Diretoria: `<NavLink href="/processos">Processos</NavLink>`.

`src/AtividadesJuridicas.Web/Components/Pages/Processos.razor`:

```razor
@page "/processos"
@using System.ComponentModel.DataAnnotations
@inject IDespachante Despachante

<PageTitle>Processos</PageTitle>

<h1>Processos</h1>

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
    <EditForm Model="_busca" OnSubmit="BuscarAsync" FormName="busca-processos" class="formulario filtros">
        <label>Título, parte contrária ou número <InputText @bind-Value="_busca.Texto" /></label>
        <label>
            Situação
            <InputSelect @bind-Value="_busca.Status">
                <option value="">Todas</option>
                @foreach (var status in Enum.GetValues<StatusProcesso>())
                {
                    <option value="@status">@status</option>
                }
            </InputSelect>
        </label>
        <button type="submit">Buscar</button>
    </EditForm>

    @if (_mensagem is not null)
    {
        <p class="@(_falhou ? "erro" : "ok")" role="status">@_mensagem</p>
    }

    <table>
        <thead>
            <tr><th>Título</th><th>Número</th><th>Área</th><th>Clientes</th><th>Parte contrária</th><th>Situação</th><th></th></tr>
        </thead>
        <tbody>
            @foreach (var processo in _pagina.Itens)
            {
                <tr @key="processo.Id">
                    <td>@processo.Titulo</td>
                    <td>@processo.NumeroCnj</td>
                    <td>@processo.AreaNome</td>
                    <td>@string.Join(", ", processo.Clientes)</td>
                    <td>@processo.ParteContraria</td>
                    <td>@processo.Status</td>
                    <td class="acoes">
                        @if (_ator.EhDiretoria)
                        {
                            <button type="button" class="secundario" @onclick="() => EditarAsync(processo.Id)">Editar</button>
                        }
                        else
                        {
                            <button type="button" class="secundario" @onclick="() => VerAsync(processo.Id)">Ver</button>
                        }
                    </td>
                </tr>
            }
        </tbody>
    </table>

    <div class="acoes">
        <button type="button" class="secundario" disabled="@(_pagina.Numero <= 1)" @onclick="() => CarregarAsync(_pagina.Numero - 1)">Anterior</button>
        <span>Página @_pagina.Numero de @_pagina.TotalDePaginas (@_pagina.Total processos)</span>
        <button type="button" class="secundario" disabled="@(_pagina.Numero >= _pagina.TotalDePaginas)" @onclick="() => CarregarAsync(_pagina.Numero + 1)">Próxima</button>
    </div>

    @if (_ator.EhDiretoria)
    {
        <div class="acoes">
            <button type="button" @onclick="NovoAsync">Novo processo</button>
        </div>

        @if (_form is not null)
        {
            <h2>@(_form.Id is null ? "Novo processo" : "Editar processo")</h2>

            <EditForm Model="_form" OnValidSubmit="SalvarAsync" FormName="processo" class="formulario">
                <DataAnnotationsValidator />

                <div class="grade">
                    <label>Título <InputText @bind-Value="_form.Titulo" /><ValidationMessage For="() => _form.Titulo" /></label>
                    <label>Número CNJ <InputText @bind-Value="_form.NumeroCnj" placeholder="0000000-00.0000.0.00.0000" /></label>
                    <label>
                        Área de atuação
                        <InputSelect @bind-Value="_form.AreaAtuacaoId">
                            <option value="@Guid.Empty">Selecione…</option>
                            @foreach (var area in _areas)
                            {
                                <option value="@area.Id">@area.Nome@(area.Ativo ? "" : " (inativa)")</option>
                            }
                        </InputSelect>
                    </label>
                    <label>Órgão <InputText @bind-Value="_form.Orgao" /></label>
                    <label>Comarca <InputText @bind-Value="_form.Comarca" /></label>
                    <label>Parte contrária <InputText @bind-Value="_form.ParteContraria" /></label>
                    <label>Data de distribuição <InputDate @bind-Value="_form.DataDistribuicao" /></label>
                    <label>Valor da causa <InputNumber @bind-Value="_form.ValorCausa" /></label>
                </div>

                <label>Observações <InputTextArea @bind-Value="_form.Observacoes" rows="3" /></label>

                <h3>Clientes do processo</h3>
                @if (_form.Clientes.Count == 0)
                {
                    <p>Nenhum cliente vinculado.</p>
                }
                else
                {
                    <table>
                        <thead><tr><th>Cliente</th><th>Polo</th><th></th></tr></thead>
                        <tbody>
                            @foreach (var vinculo in _form.Clientes)
                            {
                                <tr @key="vinculo.ClienteId">
                                    <td>@vinculo.Nome</td>
                                    <td>
                                        <InputSelect @bind-Value="vinculo.Polo">
                                            @foreach (var polo in Enum.GetValues<PoloProcessual>())
                                            {
                                                <option value="@polo">@polo</option>
                                            }
                                        </InputSelect>
                                    </td>
                                    <td><button type="button" class="secundario" @onclick="() => Desvincular(vinculo)">Remover</button></td>
                                </tr>
                            }
                        </tbody>
                    </table>
                }

                <div class="filtros formulario">
                    <label>Adicionar cliente (nome, CPF ou CNPJ) <InputText @bind-Value="_buscaDeCliente" /></label>
                    <button type="button" class="secundario" @onclick="ProcurarClientesAsync">Procurar</button>
                </div>
                @foreach (var candidato in _candidatos)
                {
                    <div class="acoes" @key="candidato.Id">
                        <span>@candidato.Nome — @candidato.Documento</span>
                        <button type="button" class="secundario" @onclick="() => Vincular(candidato)">Adicionar</button>
                    </div>
                }

                <div class="acoes">
                    <button type="submit">Salvar</button>
                    <button type="button" class="secundario" @onclick="Fechar">Cancelar</button>
                </div>
            </EditForm>

            @if (_form.Id is { } idEmEdicao)
            {
                <div class="acoes">
                    <span>Situação atual: @_form.Status</span>
                    @foreach (var status in Enum.GetValues<StatusProcesso>().Where(s => s != _form.Status))
                    {
                        <button type="button" class="secundario" @onclick="() => AlterarStatusAsync(idEmEdicao, status)">Marcar como @status</button>
                    }
                </div>
            }
        }
    }
    else if (_ficha is not null)
    {
        <section class="ficha">
            <h2>@_ficha.Titulo</h2>
            <dl>
                <dt>Número</dt><dd>@_ficha.NumeroCnj</dd>
                <dt>Área</dt><dd>@_ficha.AreaNome</dd>
                <dt>Situação</dt><dd>@_ficha.Status</dd>
                <dt>Órgão</dt><dd>@_ficha.Orgao</dd>
                <dt>Comarca</dt><dd>@_ficha.Comarca</dd>
                <dt>Parte contrária</dt><dd>@_ficha.ParteContraria</dd>
                <dt>Distribuição</dt><dd>@_ficha.DataDistribuicao?.ToString("dd/MM/yyyy")</dd>
                <dt>Valor da causa</dt><dd>@_ficha.ValorCausa?.ToString("C", Cultura)</dd>
                <dt>Observações</dt><dd>@_ficha.Observacoes</dd>
                <dt>Clientes</dt>
                <dd>
                    @foreach (var cliente in _ficha.Clientes)
                    {
                        <div>@cliente.Nome (@cliente.Documento) — polo @cliente.Polo</div>
                    }
                </dd>
            </dl>
            <div class="acoes"><button type="button" class="secundario" @onclick="Fechar">Fechar</button></div>
        </section>
    }
}

@code {
    private static readonly System.Globalization.CultureInfo Cultura = System.Globalization.CultureInfo.GetCultureInfo("pt-BR");
    private static readonly Pagina<ProcessoResumo> Vazia = new([], 0, 1, ProcessosOpcoes.TamanhoDaPagina);

    private bool _carregado;
    private bool _falhou;
    private string? _mensagem;
    private UsuarioAutenticado? _ator;
    private Busca _busca = new();
    private Pagina<ProcessoResumo> _pagina = Vazia;
    private IReadOnlyList<AreaAtuacaoResumo> _areas = [];
    private Formulario? _form;
    private ProcessoDetalhe? _ficha;
    private string? _buscaDeCliente;
    private IReadOnlyList<ClienteResumo> _candidatos = [];

    protected override async Task OnInitializedAsync()
    {
        _ator = await Despachante.EnviarAsync(new ObterMeuAcesso());
        if (_ator is not null) await CarregarAsync(1);
        _carregado = true;
    }

    private Task BuscarAsync() => CarregarAsync(1);

    private async Task CarregarAsync(int pagina)
    {
        var resultado = await Despachante.EnviarAsync(new ListarProcessos(_busca.Texto, null, _busca.Status, pagina));
        if (AcessoEncerrado(resultado)) return;

        _pagina = resultado.Valor ?? Vazia;
        if (!resultado.Sucesso)
        {
            Avisar(resultado.Mensagem, falhou: true);
            return;
        }

        // Depois de mudar um filtro ou a situação, a página atual pode ter deixado de existir.
        if (_pagina.Itens.Count == 0 && _pagina.Total > 0 && pagina > _pagina.TotalDePaginas)
            await CarregarAsync(_pagina.TotalDePaginas);
    }

    private async Task NovoAsync()
    {
        if (!await CarregarAreasAsync(areaAtual: null)) return;
        AbrirFormulario(new Formulario());
    }

    private async Task EditarAsync(Guid id)
    {
        var resultado = await Despachante.EnviarAsync(new ObterProcesso(id));
        if (AcessoEncerrado(resultado)) return;
        if (resultado.Valor is not { } p)
        {
            Avisar(resultado.Mensagem, falhou: true);
            return;
        }

        if (!await CarregarAreasAsync(p.AreaAtuacaoId)) return;

        AbrirFormulario(new Formulario
        {
            Id = p.Id, Titulo = p.Titulo, NumeroCnj = p.NumeroCnj, AreaAtuacaoId = p.AreaAtuacaoId, Status = p.Status,
            Orgao = p.Orgao, Comarca = p.Comarca, ParteContraria = p.ParteContraria,
            DataDistribuicao = p.DataDistribuicao, ValorCausa = p.ValorCausa, Observacoes = p.Observacoes,
            Clientes = [.. p.Clientes.Select(c => new Vinculo { ClienteId = c.ClienteId, Nome = c.Nome, Polo = c.Polo })],
        });
    }

    /// <summary>Áreas ativas, mais a área atual do processo quando ela está inativa.</summary>
    private async Task<bool> CarregarAreasAsync(Guid? areaAtual)
    {
        var resultado = await Despachante.EnviarAsync(new ListarAreasAtuacao());
        if (AcessoEncerrado(resultado)) return false;
        if (!resultado.Sucesso)
        {
            Avisar(resultado.Mensagem, falhou: true);
            return false;
        }

        _areas = [.. resultado.Valor!.Where(a => a.Ativo || a.Id == areaAtual)];
        return true;
    }

    private void AbrirFormulario(Formulario form)
    {
        _mensagem = null;
        _buscaDeCliente = null;
        _candidatos = [];
        _form = form;
    }

    private async Task ProcurarClientesAsync()
    {
        var resultado = await Despachante.EnviarAsync(new ListarClientes(_buscaDeCliente));
        if (AcessoEncerrado(resultado)) return;

        var jaVinculados = _form!.Clientes.Select(c => c.ClienteId).ToHashSet();
        _candidatos = [.. (resultado.Valor?.Itens ?? []).Where(c => !jaVinculados.Contains(c.Id))];
        if (!resultado.Sucesso) Avisar(resultado.Mensagem, falhou: true);
    }

    private void Vincular(ClienteResumo cliente)
    {
        if (_form!.Clientes.All(c => c.ClienteId != cliente.Id))
            _form.Clientes.Add(new Vinculo { ClienteId = cliente.Id, Nome = cliente.Nome, Polo = PoloProcessual.Ativo });
        _candidatos = [.. _candidatos.Where(c => c.Id != cliente.Id)];
    }

    private void Desvincular(Vinculo vinculo) => _form!.Clientes.Remove(vinculo);

    private async Task VerAsync(Guid id)
    {
        var resultado = await Despachante.EnviarAsync(new ObterProcesso(id));
        if (AcessoEncerrado(resultado)) return;

        _ficha = resultado.Valor;
        if (!resultado.Sucesso) Avisar(resultado.Mensagem, falhou: true);
    }

    private void Fechar()
    {
        _form = null;
        _ficha = null;
        _mensagem = null;
    }

    private async Task SalvarAsync()
    {
        var f = _form!;
        var resultado = await Despachante.EnviarAsync(new SalvarProcesso(
            f.Id,
            new DadosDoProcessoEntrada(f.NumeroCnj, f.Titulo, f.AreaAtuacaoId, f.Orgao, f.Comarca, f.ParteContraria,
                f.DataDistribuicao, f.ValorCausa, f.Observacoes),
            [.. f.Clientes.Select(c => new ClienteDoProcessoEntrada(c.ClienteId, c.Polo))]));

        if (AcessoEncerrado(resultado)) return;
        if (!resultado.Sucesso)
        {
            // O formulário permanece aberto com o que foi digitado.
            Avisar(resultado.Mensagem, falhou: true);
            return;
        }

        _form = null;
        _falhou = false;
        await CarregarAsync(_pagina.Numero);
        if (_ator is not null && !_falhou) Avisar("Processo salvo.", falhou: false);
    }

    private async Task AlterarStatusAsync(Guid id, StatusProcesso status)
    {
        var resultado = await Despachante.EnviarAsync(new AlterarStatusProcesso(id, status));
        if (AcessoEncerrado(resultado)) return;
        if (!resultado.Sucesso)
        {
            Avisar(resultado.Mensagem, falhou: true);
            return;
        }

        _form!.Status = status;
        _falhou = false;
        await CarregarAsync(_pagina.Numero);
        if (_ator is not null && !_falhou) Avisar($"Situação alterada para {status}.", falhou: false);
    }

    /// <summary>Uma resposta de acesso negado encerra a tela: nada do que estava aberto continua visível.</summary>
    private bool AcessoEncerrado(Resultado resultado)
    {
        if (resultado.Erro != TipoErro.AcessoNegado) return false;

        _ator = null;
        _pagina = Vazia;
        _form = null;
        _ficha = null;
        _candidatos = [];
        return true;
    }

    private void Avisar(string? mensagem, bool falhou)
    {
        _mensagem = mensagem;
        _falhou = falhou;
    }

    private sealed class Busca
    {
        public string? Texto { get; set; }
        public StatusProcesso? Status { get; set; }
    }

    private sealed class Vinculo
    {
        public Guid ClienteId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public PoloProcessual Polo { get; set; }
    }

    private sealed class Formulario
    {
        public Guid? Id { get; set; }

        [Required(ErrorMessage = "Informe o título.")]
        public string Titulo { get; set; } = string.Empty;

        public string? NumeroCnj { get; set; }
        public Guid AreaAtuacaoId { get; set; }
        public StatusProcesso Status { get; set; } = StatusProcesso.Ativo;
        public string? Orgao { get; set; }
        public string? Comarca { get; set; }
        public string? ParteContraria { get; set; }
        public DateOnly? DataDistribuicao { get; set; }
        public decimal? ValorCausa { get; set; }
        public string? Observacoes { get; set; }
        public List<Vinculo> Clientes { get; set; } = [];
    }
}
```

Uma área não selecionada chega ao caso de uso como `Guid.Empty`, que ele recusa com "Área de atuação não encontrada."; um processo sem clientes é recusado com a mensagem do domínio. A página mostra a mensagem e mantém o formulário aberto.

- [ ] **Step 4: Mostrar os processos do cliente na tela de clientes**

Em `Clientes.razor`:

- Acrescente o campo `private IReadOnlyList<ProcessoResumo> _processosDoCliente = [];`.
- Em `EditarAsync` e em `VerAsync`, depois de obter o cliente com sucesso, carregue os processos dele:

```csharp
        var processos = await Despachante.EnviarAsync(new ListarProcessos(ClienteId: id));
        if (AcessoEncerrado(processos)) return;
        _processosDoCliente = processos.Valor?.Itens ?? [];
```

- Em `Fechar`, em `Novo` e no `AcessoEncerrado`, esvazie a lista (`_processosDoCliente = [];`).
- Logo abaixo do formulário de edição (quando `_form.Id` não é nulo) e dentro da ficha de leitura, mostre:

```razor
            <h3>Processos do cliente</h3>
            @if (_processosDoCliente.Count == 0)
            {
                <p>Nenhum processo vinculado.</p>
            }
            else
            {
                <ul>
                    @foreach (var processo in _processosDoCliente)
                    {
                        <li @key="processo.Id">@processo.Titulo — @(processo.NumeroCnj ?? "sem número") — @processo.Status</li>
                    }
                </ul>
            }
```

A lista traz até vinte processos (a primeira página). Se houver mais, acrescente abaixo dela a linha `<p>Há mais processos; veja todos em Processos.</p>` quando o total da consulta for maior que a quantidade exibida; para isso guarde também o total devolvido.

- [ ] **Step 5: Rodar e confirmar que passa**

Run: `dotnet test`
Expected: tudo aprovado, zero avisos.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "feat(web): tela de processos e processos na ficha do cliente"
```
