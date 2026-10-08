# Fase 4B — Gestão de perfis: Plano de Implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Permitir que a Diretoria crie, renomeie, inative e reative perfis, e garantir que um perfil novo passe a constar na lista de acesso de todos os Documentos Gerais existentes.

**Architecture:** Os casos de uso ficam no contexto Identidade. A inclusão do perfil novo nos Documentos Gerais é feita por uma porta da Identidade (`IAcessoInicialDePerfil`), implementada na infraestrutura com um único `INSERT ... SELECT`, dentro da mesma transação da criação do perfil. Como essa gravação não passa pelos interceptores, ela entra na lista permitida de `GravacoesPorForaTests` e é registrada por um evento de segurança com a quantidade de documentos alcançados.

**Tech Stack:** .NET 10, Blazor Interactive Server, EF Core 10 + Npgsql, PostgreSQL 17, xUnit 2.9, FluentAssertions 7.2, Testcontainers.

**Spec:** `docs/superpowers/specs/2026-10-06-modelagem-dominio-design.md` (seções 5 — Perfil e matriz de acesso —, 7 — "Um perfil criado depois é incluído automaticamente na lista de acesso de todos os Documentos Gerais existentes" — e 10).

## Decisões deste plano

- **O tipo do perfil não muda depois de criado.** As regras de negócio consultam o tipo; trocar o tipo de um perfil mudaria de uma vez o acesso de todos os seus usuários (inclusive a contratos de honorários).
- **Perfil com usuário ativo não pode ser inativado.** Primeiro troca-se o perfil desses usuários. Isso também protege a regra do último diretor.
- **Sem exclusão de perfil.** Inativar cobre a necessidade.
- **Inclusão em massa por SQL, com um evento.** Criar um perfil grava uma entrada por Documento Geral existente num único comando, sem um registro de auditoria por entrada; o evento `perfil_incluido_nos_documentos_gerais` guarda o perfil e a quantidade. As entradas levam `criado_em` e `criado_por`.
- **Reativar um perfil não o inclui** nos Documentos Gerais enviados enquanto ele esteve inativo: a regra da spec fala do perfil criado.
- **Nome único sem diferenciar maiúsculas**, verificado no caso de uso (o índice único do banco diferencia; a corrida rara vira conflito pelo índice quando os nomes são idênticos).
- **Um documento enviado no mesmo instante em que um perfil é criado** pode nascer sem o perfil novo (cada operação não enxerga a outra antes de gravar). Aceito: a Diretoria inclui o perfil pelo editor de acesso.

## Como ler este plano

Os **testes** de cada tarefa definem o comportamento exigido. O código de implementação é o desenho pretendido, escrito sem ter sido executado: quando um detalhe do EF Core 10, do Npgsql ou do Blazor exigir, ajuste o mínimo necessário para preservar o comportamento e registre o desvio no relatório, com a mensagem de erro que o motivou. Nunca apague um teste nem afrouxe uma asserção. Ajustar como um teste obtém suas entradas é permitido. O texto de `Dados` gravado em `jsonb` volta reformatado pelo PostgreSQL: compare JSON pela estrutura, nunca por texto exato.

## Estado de partida (fase 4A, na branch `feat/fase-4-documentos`)

- Domínio: `Perfil` (`Criar`, `Inativar`, `Ativar`; sem renomear); `Documento` com `DocumentoPerfilAcesso`.
- Aplicação: `IPerfilRepositorio` (`ObterPorIdAsync`, `ListarAtivosAsync`, `ObterAtivosAsync`, `ObterVariosAsync`); `ListarPerfisAtivos`; `IUnidadeDeTrabalho.SalvarAsync`; `IAuditoria.Anotar`; `EventosDeSeguranca`.
- Infraestrutura: tabelas `perfis` (índice único em `nome` filtrado por `excluido_em IS NULL`) e `documentos_perfis_acesso` (índice único em `(documento_id, perfil_id)` filtrado por `excluido_em IS NULL`).
- Web: `Areas.razor` é o modelo de página de cadastro simples; `DocumentosDoCliente.razor` tem o editor "Quem acessa", que hoje lista os perfis com `ListarPerfisAtivos`.
- Testes: 367 unitários e 231 de integração passando.

## Global Constraints

- TFM único `net10.0` em `Directory.Build.props`. `Nullable` e `TreatWarningsAsErrors=true`. Versões de pacote só em `Directory.Packages.props`. Nenhum pacote novo.
- `Domain` não referencia nada. `Application` não referencia EF Core, Npgsql, `Infrastructure` nem `Web`.
- Código, nomes de tabela e mensagens em português. Tabelas e colunas em `snake_case`. Datas em UTC.
- Matriz de acesso: usuários e perfis — Diretoria tudo, Produção sem acesso.
- Autorização verificada no manipulador; a interface apenas oculta. Enquanto a consulta de acesso não termina, a página mostra "Carregando…".
- Toda alteração de `Entidade` gera registro de auditoria na mesma transação. Gravação por fora dos interceptores só com o arquivo na lista permitida de `GravacoesPorForaTests` e com um evento de auditoria que a registre.
- Consultas são `IConsultaAuditavel`.
- Um caso de uso por arquivo. Novas dependências de construtor entram por último.
- Nenhuma migration nova: este plano não muda o modelo.

## Review Focus

1. **Criar um perfil com documentos já existentes**: o perfil passa a constar em todo Documento Geral vivo, e em nenhum contrato de honorários nem documento excluído; um usuário desse perfil passa a listar e baixar. (Tarefa 1)
2. **Falha no meio da criação**: se a inclusão nos documentos falhar, o perfil não fica criado. (Tarefa 1)
3. **Inativar um perfil que ainda tem usuário ativo**: recusado, com mensagem que diz o que fazer. (Tarefa 1)
4. **Nome que só difere em maiúsculas ou espaços**: conflito. (Tarefa 1)
5. **Perfil inativo que já constava na lista de um documento**: aparece no editor de acesso identificado como inativo e pode ser retirado. (Tarefa 2)

---

### Task 1: Casos de uso de perfis e inclusão do perfil novo nos Documentos Gerais

**Files:**
- Modify: `src/AtividadesJuridicas.Domain/Identidade/Perfil.cs` (`Renomear`)
- Modify: `src/AtividadesJuridicas.Application/Comum/Portas.cs` (`IUnidadeDeTrabalho.EmTransacaoAsync`)
- Modify: `src/AtividadesJuridicas.Application/Identidade/Portas.cs`
- Modify: `src/AtividadesJuridicas.Application/Auditoria/IAuditoria.cs` (evento)
- Create: `src/AtividadesJuridicas.Application/Identidade/ListarPerfis.cs`
- Create: `src/AtividadesJuridicas.Application/Identidade/SalvarPerfil.cs`
- Create: `src/AtividadesJuridicas.Application/Identidade/DefinirAtivoPerfil.cs`
- Create: `src/AtividadesJuridicas.Infrastructure/Persistencia/Repositorios/AcessoInicialDePerfil.cs`
- Modify: `src/AtividadesJuridicas.Infrastructure/Persistencia/Repositorios/PerfilRepositorio.cs`, `UsuarioRepositorio.cs`
- Modify: `src/AtividadesJuridicas.Infrastructure/Persistencia/UnidadeDeTrabalho.cs`
- Modify: `src/AtividadesJuridicas.Infrastructure/DependencyInjection.cs`
- Modify: `tests/AtividadesJuridicas.UnitTests/GravacoesPorForaTests.cs` (lista permitida)
- Modify: `tests/AtividadesJuridicas.UnitTests/Application/Falsos.cs`
- Modify: `tests/AtividadesJuridicas.UnitTests/Domain/PerfilTests.cs`
- Test: `tests/AtividadesJuridicas.UnitTests/Application/PerfisTests.cs`
- Test: `tests/AtividadesJuridicas.IntegrationTests/PerfisTests.cs`

**Interfaces:**
- Consumes: `IContextoDeAcesso`, `IAuditoria`, `ExcecaoDeConflito`, `ExcecaoDeDominio`; `EnviarDocumento`, `ListarDocumentosDoCliente`, `BaixarDocumento`, `ExcluirDocumento`, `DefinirAcessoDoDocumento` (só nos testes de integração).
- Produces:
  - `Perfil.Renomear(string nome)`
  - `IUnidadeDeTrabalho.EmTransacaoAsync(Func<Task> acao, CancellationToken ct): Task` — roda `acao` numa transação; qualquer exceção desfaz tudo o que `acao` gravou.
  - `record PerfilDetalhe(Guid Id, string Nome, TipoPerfil Tipo, bool Ativo, int UsuariosAtivos)`
  - `IPerfilRepositorio.ExisteNomeAsync(string nome, Guid? excetoId, CancellationToken ct): Task<bool>`, `IPerfilRepositorio.ListarAsync(CancellationToken ct): Task<IReadOnlyList<PerfilDetalhe>>`, `IPerfilRepositorio.Adicionar(Perfil perfil)`
  - `IUsuarioRepositorio.ContarAtivosComPerfilAsync(Guid perfilId, CancellationToken ct): Task<int>`
  - `interface IAcessoInicialDePerfil { Task<int> IncluirNosDocumentosGeraisAsync(Guid perfilId, CancellationToken ct); }` (em `Application/Identidade/Portas.cs`)
  - `EventosDeSeguranca.PerfilIncluidoNosDocumentosGerais = "perfil_incluido_nos_documentos_gerais"`
  - `record ListarPerfis : IRequisicao<Resultado<IReadOnlyList<PerfilDetalhe>>>, IConsultaAuditavel`
  - `record SalvarPerfil(Guid? Id, string Nome, TipoPerfil Tipo) : IRequisicao<Resultado<Guid>>`
  - `record DefinirAtivoPerfil(Guid Id, bool Ativo) : IRequisicao<Resultado>`

#### Parte A — Domínio e aplicação

- [ ] **Step 1: Testes do domínio**

Acrescente a `tests/AtividadesJuridicas.UnitTests/Domain/PerfilTests.cs`:

```csharp
    [Fact]
    public void Renomear_troca_o_nome_aparado_e_mantem_tipo_e_situacao()
    {
        var perfil = Perfil.Criar("Estagiário", TipoPerfil.Producao);
        perfil.Inativar();

        perfil.Renomear("  Estagiário de Direito ");

        perfil.Nome.Should().Be("Estagiário de Direito");
        perfil.Tipo.Should().Be(TipoPerfil.Producao);
        perfil.Ativo.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Renomear_recusa_nome_vazio(string nome)
    {
        var perfil = Perfil.Criar("Estagiário", TipoPerfil.Producao);

        var acao = () => perfil.Renomear(nome);

        acao.Should().Throw<ExcecaoDeDominio>().WithMessage("O nome do perfil é obrigatório.");
        perfil.Nome.Should().Be("Estagiário");
    }

    [Fact]
    public void Renomear_respeita_o_tamanho_maximo()
    {
        var perfil = Perfil.Criar("Estagiário", TipoPerfil.Producao);

        var acao = () => perfil.Renomear(new string('a', Perfil.TamanhoMaximoNome + 1));

        acao.Should().Throw<ExcecaoDeDominio>().WithMessage("O nome do perfil deve ter até 100 caracteres.");
    }
```

- [ ] **Step 2: Falsos**

Em `tests/AtividadesJuridicas.UnitTests/Application/Falsos.cs`:

`PerfilRepositorioFalso` ganha:

```csharp
    /// <summary>Preenchido pelo cenário: quantos usuários ativos têm o perfil.</summary>
    public Func<Guid, int> UsuariosAtivosDoPerfil { get; set; } = _ => 0;

    public Task<bool> ExisteNomeAsync(string nome, Guid? excetoId, CancellationToken ct) =>
        Task.FromResult(Itens.Any(p =>
            p.Id != excetoId && string.Equals(p.Nome, nome.Trim(), StringComparison.OrdinalIgnoreCase)));

    public Task<IReadOnlyList<PerfilDetalhe>> ListarAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<PerfilDetalhe>>(
            [.. Itens.OrderBy(p => p.Nome).Select(p => new PerfilDetalhe(p.Id, p.Nome, p.Tipo, p.Ativo, UsuariosAtivosDoPerfil(p.Id)))]);

    public void Adicionar(Perfil perfil) => Itens.Add(perfil);
```

`UsuarioRepositorioFalso` ganha:

```csharp
    public Task<int> ContarAtivosComPerfilAsync(Guid perfilId, CancellationToken ct) =>
        Task.FromResult(Itens.Count(u => u.Ativo && u.PerfilId == perfilId));
```

`UnidadeDeTrabalhoFalsa` ganha:

```csharp
    public int Transacoes { get; private set; }

    /// <summary>Verdadeiro enquanto a ação de uma transação está rodando.</summary>
    public bool EmTransacao { get; private set; }

    public async Task EmTransacaoAsync(Func<Task> acao, CancellationToken ct)
    {
        Transacoes++;
        EmTransacao = true;
        try { await acao(); }
        finally { EmTransacao = false; }
    }
```

Novo falso:

```csharp
public sealed class AcessoInicialDePerfilFalso(UnidadeDeTrabalhoFalsa unidade) : IAcessoInicialDePerfil
{
    /// <summary>Quantos documentos gerais "existem" para receber o perfil.</summary>
    public int DocumentosGerais { get; set; }

    public Exception? Falha { get; set; }

    /// <summary>Perfil incluído e se a inclusão aconteceu dentro de uma transação.</summary>
    public List<(Guid PerfilId, bool EmTransacao)> Inclusoes { get; } = [];

    public Task<int> IncluirNosDocumentosGeraisAsync(Guid perfilId, CancellationToken ct)
    {
        if (Falha is not null) throw Falha;
        Inclusoes.Add((perfilId, unidade.EmTransacao));
        return Task.FromResult(DocumentosGerais);
    }
}
```

`Cenario` ganha `public AcessoInicialDePerfilFalso AcessoInicial { get; }`, criado no construtor com `new AcessoInicialDePerfilFalso(Unidade)`, e, no construtor, depois de criar `Usuarios`:

```csharp
        Perfis.UsuariosAtivosDoPerfil = id => Usuarios.Itens.Count(u => u.Ativo && u.PerfilId == id);
```

- [ ] **Step 3: Testes unitários dos casos de uso**

`tests/AtividadesJuridicas.UnitTests/Application/PerfisTests.cs`:

```csharp
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.UnitTests.Application;

public class PerfisTests
{
    private readonly Cenario _c = new();

    private Usuario Producao() => _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);

    private Task<Resultado<Guid>> Salvar(Usuario? ator, Guid? id, string nome, TipoPerfil tipo = TipoPerfil.Producao) =>
        new SalvarPerfilManipulador(_c.Como(ator), _c.Perfis, _c.AcessoInicial, _c.Unidade, _c.Auditoria)
            .ManipularAsync(new SalvarPerfil(id, nome, tipo), default);

    private Task<Resultado> DefinirAtivo(Usuario? ator, Guid id, bool ativo) =>
        new DefinirAtivoPerfilManipulador(_c.Como(ator), _c.Perfis, _c.Usuarios, _c.Unidade)
            .ManipularAsync(new DefinirAtivoPerfil(id, ativo), default);

    private Task<Resultado<IReadOnlyList<PerfilDetalhe>>> Listar(Usuario? ator) =>
        new ListarPerfisManipulador(_c.Como(ator), _c.Perfis).ManipularAsync(new ListarPerfis(), default);

    // ---- Criar ----

    [Fact]
    public async Task Diretor_cria_perfil_e_ele_e_incluido_nos_documentos_gerais_na_mesma_transacao()
    {
        _c.AcessoInicial.DocumentosGerais = 7;

        var resultado = await Salvar(_c.Diretor, null, "  Estagiário ");

        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        var perfil = _c.Perfis.Itens.Single(p => p.Id == resultado.Valor);
        perfil.Nome.Should().Be("Estagiário");
        perfil.Tipo.Should().Be(TipoPerfil.Producao);
        perfil.Ativo.Should().BeTrue();
        _c.Unidade.Transacoes.Should().Be(1);
        _c.AcessoInicial.Inclusoes.Should().Equal((perfil.Id, true));

        var evento = _c.Auditoria.Eventos.Should().ContainSingle().Subject;
        evento.Evento.Should().Be(EventosDeSeguranca.PerfilIncluidoNosDocumentosGerais);
        evento.Entidade.Should().Be("Perfil");
        evento.EntidadeId.Should().Be(perfil.Id);
        evento.Imediato.Should().BeFalse("o evento é gravado na mesma transação");
        evento.Dados.Should().BeEquivalentTo(new { documentos = 7 });
    }

    [Fact]
    public async Task Sem_documentos_gerais_o_perfil_e_criado_sem_evento()
    {
        var resultado = await Salvar(_c.Diretor, null, "Estagiário");

        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        _c.AcessoInicial.Inclusoes.Should().ContainSingle();
        _c.Auditoria.Eventos.Should().BeEmpty();
    }

    [Fact]
    public async Task Perfil_de_diretoria_tambem_pode_ser_criado()
    {
        var resultado = await Salvar(_c.Diretor, null, "Sócio", TipoPerfil.Diretoria);

        _c.Perfis.Itens.Single(p => p.Id == resultado.Valor).Tipo.Should().Be(TipoPerfil.Diretoria);
    }

    [Fact]
    public async Task Falha_na_inclusao_sobe_para_a_transacao_ser_desfeita()
    {
        _c.AcessoInicial.Falha = new InvalidOperationException("banco fora do ar");

        var acao = () => Salvar(_c.Diretor, null, "Estagiário");

        await acao.Should().ThrowAsync<InvalidOperationException>().WithMessage("banco fora do ar");
        _c.Unidade.Transacoes.Should().Be(1, "a falha aconteceu dentro da transação, que a desfaz");
    }

    [Theory]
    [InlineData("Produção")]
    [InlineData("  PRODUÇÃO ")]
    [InlineData("produção")]
    public async Task Nome_repetido_e_conflito_sem_diferenciar_maiusculas(string nome)
    {
        var resultado = await Salvar(_c.Diretor, null, nome);

        resultado.Erro.Should().Be(TipoErro.Conflito);
        resultado.Mensagem.Should().Be("Já existe um perfil com este nome.");
        _c.Perfis.Itens.Should().HaveCount(2);
        _c.Unidade.Transacoes.Should().Be(0);
    }

    [Fact]
    public async Task Conflito_vindo_do_banco_tambem_e_nome_em_uso()
    {
        _c.Unidade.Falha = new ExcecaoDeConflito("duplicado");

        var resultado = await Salvar(_c.Diretor, null, "Estagiário");

        resultado.Erro.Should().Be(TipoErro.Conflito);
        resultado.Mensagem.Should().Be("Já existe um perfil com este nome.");
    }

    [Fact]
    public async Task Nome_vazio_ou_tipo_invalido_e_erro_de_validacao()
    {
        (await Salvar(_c.Diretor, null, "   ")).Erro.Should().Be(TipoErro.Validacao);
        (await Salvar(_c.Diretor, null, "Estagiário", (TipoPerfil)99)).Erro.Should().Be(TipoErro.Validacao);
        _c.Perfis.Itens.Should().HaveCount(2);
        _c.AcessoInicial.Inclusoes.Should().BeEmpty();
    }

    // ---- Renomear ----

    [Fact]
    public async Task Diretor_renomeia_sem_mexer_nos_documentos()
    {
        var resultado = await Salvar(_c.Diretor, _c.PerfilProducao.Id, "Advogados");

        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        _c.PerfilProducao.Nome.Should().Be("Advogados");
        _c.AcessoInicial.Inclusoes.Should().BeEmpty();
        _c.Unidade.Salvamentos.Should().Be(1);
    }

    [Fact]
    public async Task Salvar_mantendo_o_proprio_nome_nao_e_conflito()
    {
        (await Salvar(_c.Diretor, _c.PerfilProducao.Id, "produção")).Sucesso.Should().BeTrue();
    }

    [Fact]
    public async Task O_tipo_do_perfil_nao_muda()
    {
        var resultado = await Salvar(_c.Diretor, _c.PerfilProducao.Id, "Produção", TipoPerfil.Diretoria);

        resultado.Erro.Should().Be(TipoErro.Validacao);
        resultado.Mensagem.Should().Be("O tipo do perfil não pode ser alterado.");
        _c.PerfilProducao.Tipo.Should().Be(TipoPerfil.Producao);
        _c.Unidade.Salvamentos.Should().Be(0);
    }

    [Fact]
    public async Task Perfil_inexistente_e_nao_encontrado()
    {
        (await Salvar(_c.Diretor, Guid.CreateVersion7(), "X")).Erro.Should().Be(TipoErro.NaoEncontrado);
    }

    // ---- Ativar e inativar ----

    [Fact]
    public async Task Perfil_sem_usuario_ativo_e_inativado_e_reativado()
    {
        (await DefinirAtivo(_c.Diretor, _c.PerfilProducao.Id, false)).Sucesso.Should().BeTrue();
        _c.PerfilProducao.Ativo.Should().BeFalse();

        (await DefinirAtivo(_c.Diretor, _c.PerfilProducao.Id, true)).Sucesso.Should().BeTrue();
        _c.PerfilProducao.Ativo.Should().BeTrue();
        _c.Unidade.Salvamentos.Should().Be(2);
    }

    [Fact]
    public async Task Perfil_com_usuario_ativo_nao_e_inativado()
    {
        Producao();

        var resultado = await DefinirAtivo(_c.Diretor, _c.PerfilProducao.Id, false);

        resultado.Erro.Should().Be(TipoErro.Conflito);
        resultado.Mensagem.Should().Be("Há usuários ativos com este perfil. Troque o perfil deles antes de inativar.");
        _c.PerfilProducao.Ativo.Should().BeTrue();
    }

    [Fact]
    public async Task Perfil_so_com_usuarios_inativos_pode_ser_inativado()
    {
        Producao().Inativar();

        (await DefinirAtivo(_c.Diretor, _c.PerfilProducao.Id, false)).Sucesso.Should().BeTrue();
    }

    [Fact]
    public async Task O_perfil_do_diretor_em_uso_nao_e_inativado()
    {
        (await DefinirAtivo(_c.Diretor, _c.PerfilDiretor.Id, false)).Erro.Should().Be(TipoErro.Conflito);
        _c.PerfilDiretor.Ativo.Should().BeTrue();
    }

    [Fact]
    public async Task Definir_a_mesma_situacao_nao_grava()
    {
        (await DefinirAtivo(_c.Diretor, _c.PerfilProducao.Id, true)).Sucesso.Should().BeTrue();

        _c.Unidade.Salvamentos.Should().Be(0);
    }

    [Fact]
    public async Task Ativar_ou_inativar_perfil_inexistente_e_nao_encontrado()
    {
        (await DefinirAtivo(_c.Diretor, Guid.CreateVersion7(), false)).Erro.Should().Be(TipoErro.NaoEncontrado);
    }

    // ---- Acesso e listagem ----

    [Fact]
    public async Task Producao_e_anonimo_nao_gerenciam_nem_listam_perfis()
    {
        var pedro = Producao();

        (await Salvar(pedro, null, "Novo")).Erro.Should().Be(TipoErro.AcessoNegado);
        (await Salvar(null, null, "Novo")).Erro.Should().Be(TipoErro.AcessoNegado);
        (await Salvar(pedro, _c.PerfilProducao.Id, "Outro nome")).Erro.Should().Be(TipoErro.AcessoNegado);
        (await DefinirAtivo(pedro, _c.PerfilDiretor.Id, false)).Erro.Should().Be(TipoErro.AcessoNegado);
        (await DefinirAtivo(null, _c.PerfilDiretor.Id, false)).Erro.Should().Be(TipoErro.AcessoNegado);
        (await Listar(pedro)).Erro.Should().Be(TipoErro.AcessoNegado);
        (await Listar(null)).Erro.Should().Be(TipoErro.AcessoNegado);
        _c.Perfis.Itens.Should().HaveCount(2);
        _c.PerfilProducao.Nome.Should().Be("Produção");
    }

    [Fact]
    public async Task Listagem_traz_todos_os_perfis_com_a_contagem_de_usuarios_ativos()
    {
        Producao();
        _c.NovoUsuario("Paula", "paula@x.com", _c.PerfilProducao).Inativar();
        var estagio = (await Salvar(_c.Diretor, null, "Estagiário")).Valor;
        (await DefinirAtivo(_c.Diretor, estagio, false)).Sucesso.Should().BeTrue();

        var perfis = (await Listar(_c.Diretor)).Valor!;

        perfis.Should().BeEquivalentTo(
        [
            new PerfilDetalhe(_c.PerfilDiretor.Id, "Diretor", TipoPerfil.Diretoria, true, 1),
            new PerfilDetalhe(estagio, "Estagiário", TipoPerfil.Producao, false, 0),
            new PerfilDetalhe(_c.PerfilProducao.Id, "Produção", TipoPerfil.Producao, true, 1),
        ], opcoes => opcoes.WithStrictOrdering());
    }

    [Fact]
    public void Listagem_e_uma_consulta_auditavel()
    {
        IConsultaAuditavel consulta = new ListarPerfis();

        consulta.TipoRecurso.Should().Be("Perfil");
        consulta.Acao.Should().Be(AcaoDeAcesso.Listou);
    }
}
```

- [ ] **Step 4: Rodar e ver falhar**

Run: `dotnet test tests/AtividadesJuridicas.UnitTests --filter "FullyQualifiedName~PerfisTests|FullyQualifiedName~PerfilTests"`
Expected: FAIL na compilação.

- [ ] **Step 5: Domínio e portas**

Em `Perfil.cs`, a validação do nome passa a ser compartilhada:

```csharp
    public static Perfil Criar(string nome, TipoPerfil tipo)
    {
        var nomeValido = NomeValido(nome);
        if (!Enum.IsDefined(tipo))
            throw new ExcecaoDeDominio("Tipo de perfil inválido.");

        return new Perfil { Nome = nomeValido, Tipo = tipo, Ativo = true };
    }

    /// <summary>O tipo não muda depois de criado: as regras de acesso dependem dele.</summary>
    public void Renomear(string nome) => Nome = NomeValido(nome);

    private static string NomeValido(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ExcecaoDeDominio("O nome do perfil é obrigatório.");
        if (nome.Trim().Length > TamanhoMaximoNome)
            throw new ExcecaoDeDominio($"O nome do perfil deve ter até {TamanhoMaximoNome} caracteres.");
        return nome.Trim();
    }
```

Em `Comum/Portas.cs`, `IUnidadeDeTrabalho` ganha:

```csharp
    /// <summary>
    /// Roda <paramref name="acao"/> numa transação: tudo o que ela gravar (inclusive por mais de um
    /// <see cref="SalvarAsync"/>) é confirmado junto, ou nada é, se ela lançar.
    /// </summary>
    Task EmTransacaoAsync(Func<Task> acao, CancellationToken ct);
```

Em `Identidade/Portas.cs`:

```csharp
/// <summary><paramref name="UsuariosAtivos"/>: quantos usuários ativos têm o perfil.</summary>
public sealed record PerfilDetalhe(Guid Id, string Nome, TipoPerfil Tipo, bool Ativo, int UsuariosAtivos);
```

`IUsuarioRepositorio` ganha `Task<int> ContarAtivosComPerfilAsync(Guid perfilId, CancellationToken ct);`.

`IPerfilRepositorio` ganha:

```csharp
    /// <summary>Compara sem diferenciar maiúsculas, desconsiderando <paramref name="excetoId"/>.</summary>
    Task<bool> ExisteNomeAsync(string nome, Guid? excetoId, CancellationToken ct);

    /// <summary>Todos os perfis, ativos e inativos, por nome.</summary>
    Task<IReadOnlyList<PerfilDetalhe>> ListarAsync(CancellationToken ct);

    void Adicionar(Perfil perfil);
```

E, no mesmo arquivo:

```csharp
/// <summary>O que um perfil recém-criado recebe: acesso a todos os Documentos Gerais existentes.</summary>
public interface IAcessoInicialDePerfil
{
    /// <summary>
    /// Inclui o perfil na lista de acesso de cada Documento Geral que ainda não o tem e devolve quantos
    /// foram. Grava por fora dos interceptores: quem chama registra o evento de auditoria.
    /// </summary>
    Task<int> IncluirNosDocumentosGeraisAsync(Guid perfilId, CancellationToken ct);
}
```

Em `Auditoria/IAuditoria.cs`, `EventosDeSeguranca` ganha `public const string PerfilIncluidoNosDocumentosGerais = "perfil_incluido_nos_documentos_gerais";`.

- [ ] **Step 6: Casos de uso**

`ListarPerfis.cs`:

```csharp
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Domain.Auditoria;

namespace AtividadesJuridicas.Application.Identidade;

/// <summary>Todos os perfis, ativos e inativos, para a gestão. Quem escolhe um perfil para um usuário usa ListarPerfisAtivos.</summary>
public sealed record ListarPerfis : IRequisicao<Resultado<IReadOnlyList<PerfilDetalhe>>>, IConsultaAuditavel
{
    public string TipoRecurso => "Perfil";
    public AcaoDeAcesso Acao => AcaoDeAcesso.Listou;
}

public sealed class ListarPerfisManipulador(IContextoDeAcesso contexto, IPerfilRepositorio perfis)
    : IManipulador<ListarPerfis, Resultado<IReadOnlyList<PerfilDetalhe>>>
{
    public async Task<Resultado<IReadOnlyList<PerfilDetalhe>>> ManipularAsync(ListarPerfis requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Resultado<IReadOnlyList<PerfilDetalhe>>.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        return Resultado<IReadOnlyList<PerfilDetalhe>>.Ok(await perfis.ListarAsync(ct));
    }
}
```

`SalvarPerfil.cs`:

```csharp
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Domain.Comum;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.Application.Identidade;

/// <summary>
/// Cria o perfil quando <paramref name="Id"/> é nulo; caso contrário, renomeia. O tipo só vale na
/// criação: num perfil existente ele tem de ser o mesmo.
/// </summary>
public sealed record SalvarPerfil(Guid? Id, string Nome, TipoPerfil Tipo) : IRequisicao<Resultado<Guid>>;

public sealed class SalvarPerfilManipulador(
    IContextoDeAcesso contexto,
    IPerfilRepositorio perfis,
    IAcessoInicialDePerfil acessoInicial,
    IUnidadeDeTrabalho unidade,
    IAuditoria auditoria) : IManipulador<SalvarPerfil, Resultado<Guid>>
{
    private const string NomeEmUso = "Já existe um perfil com este nome.";

    public async Task<Resultado<Guid>> ManipularAsync(SalvarPerfil requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Falha(TipoErro.AcessoNegado, "Acesso negado.");

        Perfil? existente = null;
        if (requisicao.Id is { } id)
        {
            existente = await perfis.ObterPorIdAsync(id, ct);
            if (existente is null) return Falha(TipoErro.NaoEncontrado, "Perfil não encontrado.");
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(requisicao.Nome) &&
                await perfis.ExisteNomeAsync(requisicao.Nome, existente?.Id, ct))
                return Falha(TipoErro.Conflito, NomeEmUso);

            if (existente is not null)
            {
                if (requisicao.Tipo != existente.Tipo)
                    return Falha(TipoErro.Validacao, "O tipo do perfil não pode ser alterado.");

                existente.Renomear(requisicao.Nome);
                await unidade.SalvarAsync(ct);
                return Resultado<Guid>.Ok(existente.Id);
            }

            var perfil = Perfil.Criar(requisicao.Nome, requisicao.Tipo);

            // O perfil e o acesso dele aos Documentos Gerais existentes entram juntos, ou nenhum entra.
            await unidade.EmTransacaoAsync(async () =>
            {
                perfis.Adicionar(perfil);
                await unidade.SalvarAsync(ct);

                var documentos = await acessoInicial.IncluirNosDocumentosGeraisAsync(perfil.Id, ct);
                if (documentos == 0) return;

                // A inclusão é uma gravação por fora dos interceptores: este evento é o registro dela.
                auditoria.Anotar(EventosDeSeguranca.PerfilIncluidoNosDocumentosGerais, "Perfil", perfil.Id, new { documentos });
                await unidade.SalvarAsync(ct);
            }, ct);

            return Resultado<Guid>.Ok(perfil.Id);
        }
        catch (ExcecaoDeDominio ex)
        {
            return Falha(TipoErro.Validacao, ex.Message);
        }
        catch (ExcecaoDeConflito)
        {
            return Falha(TipoErro.Conflito, NomeEmUso);
        }
    }

    private static Resultado<Guid> Falha(TipoErro erro, string mensagem) => Resultado<Guid>.Falha(erro, mensagem);
}
```

`DefinirAtivoPerfil.cs`:

```csharp
using AtividadesJuridicas.Application.Comum;

namespace AtividadesJuridicas.Application.Identidade;

public sealed record DefinirAtivoPerfil(Guid Id, bool Ativo) : IRequisicao<Resultado>;

public sealed class DefinirAtivoPerfilManipulador(
    IContextoDeAcesso contexto,
    IPerfilRepositorio perfis,
    IUsuarioRepositorio usuarios,
    IUnidadeDeTrabalho unidade) : IManipulador<DefinirAtivoPerfil, Resultado>
{
    public async Task<Resultado> ManipularAsync(DefinirAtivoPerfil requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Resultado.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        var perfil = await perfis.ObterPorIdAsync(requisicao.Id, ct);
        if (perfil is null) return Resultado.Falha(TipoErro.NaoEncontrado, "Perfil não encontrado.");

        if (perfil.Ativo == requisicao.Ativo) return Resultado.Ok();

        if (requisicao.Ativo)
        {
            perfil.Ativar();
        }
        else
        {
            // Perfil inativo não pode estar atribuído a quem ainda usa o sistema.
            if (await usuarios.ContarAtivosComPerfilAsync(perfil.Id, ct) > 0)
                return Resultado.Falha(TipoErro.Conflito,
                    "Há usuários ativos com este perfil. Troque o perfil deles antes de inativar.");
            perfil.Inativar();
        }

        await unidade.SalvarAsync(ct);
        return Resultado.Ok();
    }
}
```

- [ ] **Step 7: Rodar os unitários**

Run: `dotnet test tests/AtividadesJuridicas.UnitTests`
Expected: PASS em tudo, menos `GravacoesPorForaTests` (que só muda na Parte B). A Infrastructure ainda não compila: é a Parte B.

#### Parte B — Persistência

- [ ] **Step 8: Testes de integração**

`tests/AtividadesJuridicas.IntegrationTests/PerfisTests.cs`:

```csharp
using System.Text;
using System.Text.Json;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Documentos;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Documentos;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class PerfisTests(BancoFixture banco) : TesteComBanco(banco)
{
    private Guid _admin, _maria, _perfilDiretor, _perfilProducao;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _admin = await IdDoAdminAsync();
        _perfilDiretor = await IdDoPerfilAsync(TipoPerfil.Diretoria);
        _perfilProducao = await IdDoPerfilAsync(TipoPerfil.Producao);
        _maria = (await ExecutarAsync(_admin, new SalvarClientePessoaFisica(
            null, new DadosDoClienteEntrada("Maria Silva", null, null, new EnderecoEntrada(), null), "529.982.247-25", null, null))).Valor;
    }

    private async Task<Guid> EnviarAsync(CategoriaDocumento categoria, string arquivo)
    {
        var resultado = await ExecutarAsync(_admin, new EnviarDocumento(
            categoria, _maria, null, arquivo, "application/pdf", new MemoryStream(Encoding.UTF8.GetBytes("conteúdo"))));
        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        return resultado.Valor;
    }

    private async Task<Guid> CriarPerfilAsync(string nome, TipoPerfil tipo = TipoPerfil.Producao)
    {
        var resultado = await ExecutarAsync(_admin, new SalvarPerfil(null, nome, tipo));
        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        return resultado.Valor;
    }

    private async Task<Guid> CriarUsuarioAsync(string nome, string email, Guid perfil) =>
        (await ExecutarAsync(_admin, new CriarUsuario(nome, email, "senha-bem-segura", perfil, null))).Valor;

    private Task<List<DocumentoPerfilAcesso>> EntradasDoPerfilAsync(Guid perfil) => EmEscopoAsync(null, sp =>
        sp.GetRequiredService<AppDbContext>().Set<DocumentoPerfilAcesso>().IgnoreQueryFilters().AsNoTracking()
            .Where(a => a.PerfilId == perfil).ToListAsync());

    private Task<List<Perfil>> PerfisNoBancoAsync() => EmEscopoAsync(null, sp =>
        sp.GetRequiredService<AppDbContext>().Perfis.IgnoreQueryFilters().AsNoTracking().OrderBy(p => p.Nome).ToListAsync());

    [Fact]
    public async Task Perfil_novo_entra_em_todo_documento_geral_vivo_e_em_mais_nenhum()
    {
        var geral1 = await EnviarAsync(CategoriaDocumento.DocumentoGeral, "um.pdf");
        var geral2 = await EnviarAsync(CategoriaDocumento.DocumentoGeral, "dois.pdf");
        var excluido = await EnviarAsync(CategoriaDocumento.DocumentoGeral, "excluido.pdf");
        var contrato = await EnviarAsync(CategoriaDocumento.ContratoHonorarios, "contrato.pdf");
        (await ExecutarAsync(_admin, new ExcluirDocumento(excluido))).Sucesso.Should().BeTrue();

        var estagio = await CriarPerfilAsync("Estagiário");

        var entradas = await EntradasDoPerfilAsync(estagio);
        entradas.Select(a => a.DocumentoId).Should().BeEquivalentTo([geral1, geral2]);
        entradas.Should().OnlyContain(a =>
            a.CriadoPor == _admin && a.CriadoEm.Kind == DateTimeKind.Utc && a.CriadoEm > DateTime.UtcNow.AddMinutes(-5) &&
            a.ExcluidoEm == null && a.Id != Guid.Empty);
        entradas.Select(a => a.Id).Should().OnlyHaveUniqueItems();
        entradas.Should().NotContain(a => a.DocumentoId == contrato || a.DocumentoId == excluido);
    }

    [Fact]
    public async Task Usuario_do_perfil_novo_passa_a_listar_e_baixar_os_documentos_gerais_e_nunca_os_contratos()
    {
        var geral = await EnviarAsync(CategoriaDocumento.DocumentoGeral, "geral.pdf");
        var contrato = await EnviarAsync(CategoriaDocumento.ContratoHonorarios, "contrato.pdf");
        var estagio = await CriarPerfilAsync("Estagiário");
        var ester = await CriarUsuarioAsync("Ester", "ester@x.com", estagio);

        var lista = (await ExecutarAsync(ester, new ListarDocumentosDoCliente(_maria, CategoriaDocumento.DocumentoGeral))).Valor!;
        var baixou = await ExecutarAsync(ester, new BaixarDocumento(geral));
        var doContrato = await ExecutarAsync(ester, new BaixarDocumento(contrato));

        lista.Itens.Select(d => d.Id).Should().Equal(geral);
        baixou.Sucesso.Should().BeTrue(baixou.Mensagem);
        await baixou.Valor!.Conteudo.DisposeAsync();
        doContrato.Erro.Should().Be(TipoErro.NaoEncontrado);
    }

    [Fact]
    public async Task Criacao_fica_na_auditoria_com_o_registro_do_perfil_e_o_evento_da_inclusao()
    {
        await EnviarAsync(CategoriaDocumento.DocumentoGeral, "um.pdf");
        await EnviarAsync(CategoriaDocumento.DocumentoGeral, "dois.pdf");

        var estagio = await CriarPerfilAsync("Estagiário");

        var registros = (await AuditoriaAsync()).Where(r => r.EntidadeId == estagio).ToList();
        var criacao = registros.Should().ContainSingle(r => r.Operacao == OperacaoAuditada.Criacao).Subject;
        criacao.Entidade.Should().Be("Perfil");
        criacao.UsuarioId.Should().Be(_admin);
        var evento = registros.Should().ContainSingle(r => r.Evento == EventosDeSeguranca.PerfilIncluidoNosDocumentosGerais).Subject;
        evento.UsuarioId.Should().Be(_admin);
        evento.IdCorrelacao.Should().Be(criacao.IdCorrelacao);
        using var dados = JsonDocument.Parse(evento.Dados!);
        dados.RootElement.GetProperty("documentos").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task Sem_documentos_gerais_nao_ha_evento_de_inclusao()
    {
        var estagio = await CriarPerfilAsync("Estagiário");

        (await AuditoriaAsync()).Should().NotContain(r => r.Evento == EventosDeSeguranca.PerfilIncluidoNosDocumentosGerais);
        (await EntradasDoPerfilAsync(estagio)).Should().BeEmpty();
    }

    [Fact]
    public async Task Documento_enviado_depois_ja_nasce_com_o_perfil_novo_e_nao_com_o_inativo()
    {
        var estagio = await CriarPerfilAsync("Estagiário");
        var temporario = await CriarPerfilAsync("Temporário");
        (await ExecutarAsync(_admin, new DefinirAtivoPerfil(temporario, false))).Sucesso.Should().BeTrue();

        var documento = await EnviarAsync(CategoriaDocumento.DocumentoGeral, "novo.pdf");

        var acesso = (await ExecutarAsync(_admin, new ObterAcessoDoDocumento(documento))).Valor!;
        acesso.Perfis.Should().BeEquivalentTo([_perfilDiretor, _perfilProducao, estagio]);
    }

    [Fact]
    public async Task Transacao_desfaz_tudo_quando_a_acao_falha()
    {
        var acao = () => EmEscopoAsync(_admin, async sp =>
        {
            var unidade = sp.GetRequiredService<IUnidadeDeTrabalho>();
            await unidade.EmTransacaoAsync(async () =>
            {
                sp.GetRequiredService<IPerfilRepositorio>().Adicionar(Perfil.Criar("Fantasma", TipoPerfil.Producao));
                await unidade.SalvarAsync(default);
                throw new InvalidOperationException("falhou depois de gravar");
            }, default);
            return 0;
        });

        await acao.Should().ThrowAsync<InvalidOperationException>().WithMessage("falhou depois de gravar");
        (await PerfisNoBancoAsync()).Select(p => p.Nome).Should().Equal("Diretor", "Produção");
        (await AuditoriaAsync()).Should().NotContain(r => r.Dados != null && r.Dados.Contains("Fantasma"));
    }

    [Fact]
    public async Task Transacao_confirma_o_que_a_acao_gravou()
    {
        await EmEscopoAsync(_admin, async sp =>
        {
            var unidade = sp.GetRequiredService<IUnidadeDeTrabalho>();
            await unidade.EmTransacaoAsync(async () =>
            {
                sp.GetRequiredService<IPerfilRepositorio>().Adicionar(Perfil.Criar("Confirmado", TipoPerfil.Producao));
                await unidade.SalvarAsync(default);
            }, default);
            return 0;
        });

        (await PerfisNoBancoAsync()).Select(p => p.Nome).Should().Contain("Confirmado");
    }

    [Theory]
    [InlineData("Produção")]
    [InlineData(" produção ")]
    public async Task Nome_repetido_e_conflito_no_banco_real(string nome)
    {
        var resultado = await ExecutarAsync(_admin, new SalvarPerfil(null, nome, TipoPerfil.Producao));

        resultado.Erro.Should().Be(TipoErro.Conflito);
        (await PerfisNoBancoAsync()).Should().HaveCount(2);
    }

    [Fact]
    public async Task Renomear_gera_alteracao_so_do_nome_e_nao_mexe_nos_documentos()
    {
        await EnviarAsync(CategoriaDocumento.DocumentoGeral, "um.pdf");
        var antes = (await EntradasDoPerfilAsync(_perfilProducao)).Count;

        (await ExecutarAsync(_admin, new SalvarPerfil(_perfilProducao, "Advogados", TipoPerfil.Producao))).Sucesso.Should().BeTrue();

        var alteracao = (await AuditoriaAsync()).Should()
            .ContainSingle(r => r.Entidade == "Perfil" && r.Operacao == OperacaoAuditada.Alteracao).Subject;
        using var dados = JsonDocument.Parse(alteracao.Dados!);
        dados.RootElement.EnumerateObject().Select(p => p.Name).Should().Equal("Nome");
        dados.RootElement.GetProperty("Nome").GetProperty("para").GetString().Should().Be("Advogados");
        (await EntradasDoPerfilAsync(_perfilProducao)).Should().HaveCount(antes);
    }

    [Fact]
    public async Task Listagem_traz_ativos_e_inativos_com_a_contagem_de_usuarios_ativos()
    {
        await CriarUsuarioAsync("Pedro", "pedro@x.com", _perfilProducao);
        var paula = await CriarUsuarioAsync("Paula", "paula@x.com", _perfilProducao);
        (await ExecutarAsync(_admin, new DefinirAtivoUsuario(paula, false))).Sucesso.Should().BeTrue();
        var estagio = await CriarPerfilAsync("Estagiário");
        (await ExecutarAsync(_admin, new DefinirAtivoPerfil(estagio, false))).Sucesso.Should().BeTrue();

        var perfis = (await ExecutarAsync(_admin, new ListarPerfis())).Valor!;

        perfis.Should().BeEquivalentTo(
        [
            new PerfilDetalhe(_perfilDiretor, "Diretor", TipoPerfil.Diretoria, true, 1),
            new PerfilDetalhe(estagio, "Estagiário", TipoPerfil.Producao, false, 0),
            new PerfilDetalhe(_perfilProducao, "Produção", TipoPerfil.Producao, true, 1),
        ], opcoes => opcoes.WithStrictOrdering());
    }

    [Fact]
    public async Task Perfil_em_uso_nao_e_inativado_e_perfil_inativo_nao_pode_ser_atribuido()
    {
        var pedro = await CriarUsuarioAsync("Pedro", "pedro@x.com", _perfilProducao);
        var estagio = await CriarPerfilAsync("Estagiário");

        var emUso = await ExecutarAsync(_admin, new DefinirAtivoPerfil(_perfilProducao, false));
        var livre = await ExecutarAsync(_admin, new DefinirAtivoPerfil(estagio, false));
        var atribuir = await ExecutarAsync(_admin, new CriarUsuario("Ester", "ester@x.com", "senha-bem-segura", estagio, null));

        emUso.Erro.Should().Be(TipoErro.Conflito);
        livre.Sucesso.Should().BeTrue(livre.Mensagem);
        atribuir.Sucesso.Should().BeFalse("perfil inativo não pode ser atribuído a usuário");
        (await ExecutarAsync(_admin, new ListarPerfisAtivos())).Valor!.Select(p => p.Id).Should().NotContain(estagio);
        pedro.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Perfil_inativado_permanece_nas_listas_em_que_ja_estava_e_pode_ser_retirado()
    {
        var estagio = await CriarPerfilAsync("Estagiário");
        var documento = await EnviarAsync(CategoriaDocumento.DocumentoGeral, "um.pdf");
        (await ExecutarAsync(_admin, new DefinirAtivoPerfil(estagio, false))).Sucesso.Should().BeTrue();

        var manter = await ExecutarAsync(_admin, new DefinirAcessoDoDocumento(documento, [_perfilDiretor, estagio], []));
        var aposManter = (await ExecutarAsync(_admin, new ObterAcessoDoDocumento(documento))).Valor!.Perfis;
        var retirar = await ExecutarAsync(_admin, new DefinirAcessoDoDocumento(documento, [_perfilDiretor], []));
        var devolver = await ExecutarAsync(_admin, new DefinirAcessoDoDocumento(documento, [_perfilDiretor, estagio], []));

        manter.Sucesso.Should().BeTrue(manter.Mensagem);
        aposManter.Should().BeEquivalentTo([_perfilDiretor, estagio]);
        retirar.Sucesso.Should().BeTrue(retirar.Mensagem);
        devolver.Erro.Should().Be(TipoErro.Validacao, "perfil inativo não entra em lista nova");
    }

    [Fact]
    public async Task Producao_nao_gerencia_perfis_e_a_tentativa_fica_registrada()
    {
        var pedro = await CriarUsuarioAsync("Pedro", "pedro@x.com", _perfilProducao);

        var criar = await ExecutarAsync(pedro, new SalvarPerfil(null, "Meu perfil", TipoPerfil.Diretoria));
        var listar = await ExecutarAsync(pedro, new ListarPerfis());

        criar.Erro.Should().Be(TipoErro.AcessoNegado);
        listar.Erro.Should().Be(TipoErro.AcessoNegado);
        (await PerfisNoBancoAsync()).Should().HaveCount(2);
        (await AuditoriaAsync()).Count(r => r.Evento == EventosDeSeguranca.AcessoNegado && r.UsuarioId == pedro).Should().Be(2);
    }
}
```

(A política de senha pode exigir outro formato: use uma senha que `PoliticaDeSenha` aceite. `DefinirAtivoUsuario` e `CriarUsuario` estão em `Application.Identidade`.)

Em `tests/AtividadesJuridicas.UnitTests/GravacoesPorForaTests.cs`, a lista permitida ganha:

```csharp
        ["AcessoInicialDePerfil.cs"] = "perfil novo incluído em todos os Documentos Gerais num só comando: " +
                                      "registrado pelo evento perfil_incluido_nos_documentos_gerais",
```

- [ ] **Step 9: Infraestrutura**

`src/AtividadesJuridicas.Infrastructure/Persistencia/Repositorios/AcessoInicialDePerfil.cs`:

```csharp
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Documentos;
using Microsoft.EntityFrameworkCore;

namespace AtividadesJuridicas.Infrastructure.Persistencia.Repositorios;

/// <summary>
/// Inclui um perfil novo na lista de acesso de todos os Documentos Gerais com um único comando. É uma
/// gravação por fora dos interceptores (uma linha por documento, sem registro de auditoria por linha):
/// o caso de uso a registra com o evento perfil_incluido_nos_documentos_gerais, na mesma transação.
/// </summary>
public sealed class AcessoInicialDePerfil(AppDbContext db, IIdentidadeAtual identidade, IRelogio relogio)
    : IAcessoInicialDePerfil
{
    public Task<int> IncluirNosDocumentosGeraisAsync(Guid perfilId, CancellationToken ct)
    {
        var agora = relogio.AgoraUtc;
        var autor = identidade.UsuarioId;
        var geral = (int)CategoriaDocumento.DocumentoGeral;

        // Os valores entram como parâmetros; os CAST dão tipo ao parâmetro mesmo quando ele é nulo.
        return db.Database.ExecuteSqlAsync($"""
            INSERT INTO documentos_perfis_acesso (id, documento_id, perfil_id, criado_em, criado_por)
            SELECT gen_random_uuid(), d.id, CAST({perfilId} AS uuid), CAST({agora} AS timestamptz), CAST({autor} AS uuid)
            FROM documentos d
            WHERE d.categoria = {geral}
              AND d.excluido_em IS NULL
              AND NOT EXISTS (
                  SELECT 1 FROM documentos_perfis_acesso a
                  WHERE a.documento_id = d.id AND a.perfil_id = CAST({perfilId} AS uuid) AND a.excluido_em IS NULL)
            """, ct);
    }
}
```

Os Ids dessas entradas são gerados pelo banco (`gen_random_uuid`, versão 4), ao contrário dos demais, gerados no domínio (versão 7): são linhas sem ordem relevante.

`UnidadeDeTrabalho` ganha:

```csharp
    public async Task EmTransacaoAsync(Func<Task> acao, CancellationToken ct)
    {
        await using var transacao = await db.Database.BeginTransactionAsync(ct);
        await acao();
        await transacao.CommitAsync(ct);
    }
```

(Sem `Commit`, o descarte da transação desfaz tudo.)

`PerfilRepositorio` ganha:

```csharp
    public Task<bool> ExisteNomeAsync(string nome, Guid? excetoId, CancellationToken ct)
    {
        var procurado = nome.Trim().ToLowerInvariant();
        return db.Perfis.AnyAsync(p => p.Id != excetoId && p.Nome.ToLower() == procurado, ct);
    }

    public async Task<IReadOnlyList<PerfilDetalhe>> ListarAsync(CancellationToken ct) =>
        await db.Perfis.AsNoTracking()
            .OrderBy(p => p.Nome)
            .Select(p => new PerfilDetalhe(
                p.Id, p.Nome, p.Tipo, p.Ativo, db.Usuarios.Count(u => u.Ativo && u.PerfilId == p.Id)))
            .ToListAsync(ct);

    public void Adicionar(Perfil perfil) => db.Perfis.Add(perfil);
```

`UsuarioRepositorio` ganha:

```csharp
    public Task<int> ContarAtivosComPerfilAsync(Guid perfilId, CancellationToken ct) =>
        db.Usuarios.CountAsync(u => u.Ativo && u.PerfilId == perfilId, ct);
```

Em `DependencyInjection.cs`: `servicos.AddScoped<IAcessoInicialDePerfil, AcessoInicialDePerfil>();`.

- [ ] **Step 10: Rodar tudo**

Run: `dotnet test` (suíte inteira, da raiz)
Expected: PASS, tudo, sem avisos. `GravacoesPorForaTests` verde com o arquivo novo na lista. Nenhuma migration nova (`dotnet ef migrations has-pending-model-changes --project src/AtividadesJuridicas.Infrastructure --startup-project src/AtividadesJuridicas.Infrastructure` deve dizer que não há mudanças; se o comando não existir nesta versão, gere uma migration de teste, confirme que veio vazia e apague-a).

- [ ] **Step 11: Commit**

```bash
git add src tests
git commit -m "feat(identidade): gestão de perfis e inclusão do perfil novo em todos os documentos gerais"
```

---

### Task 2: Tela de perfis e perfis inativos no editor de acesso

**Files:**
- Create: `src/AtividadesJuridicas.Web/Components/Pages/Perfis.razor`
- Modify: `src/AtividadesJuridicas.Web/Components/Layout/MainLayout.razor`
- Modify: `src/AtividadesJuridicas.Web/Components/Documentos/DocumentosDoCliente.razor`
- Test: `tests/AtividadesJuridicas.IntegrationTests/PerfisWebTests.cs`

**Interfaces:**
- Consumes: `ListarPerfis`, `SalvarPerfil`, `DefinirAtivoPerfil`, `PerfilDetalhe`, `ObterMeuAcesso`; `AplicacaoDeTeste`.
- Produces: rota `/perfis`; item "Perfis" no menu da Diretoria.

- [ ] **Step 1: Testes de página**

`tests/AtividadesJuridicas.IntegrationTests/PerfisWebTests.cs`:

```csharp
using System.Net;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class PerfisWebTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco);

    public async Task InitializeAsync()
    {
        var admin = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        var estagio = await _app.ExecutarAsync(admin, new SalvarPerfil(null, "Estagiário", TipoPerfil.Producao));
        estagio.Sucesso.Should().BeTrue(estagio.Mensagem);
        (await _app.ExecutarAsync(admin, new DefinirAtivoPerfil(estagio.Valor, false))).Sucesso.Should().BeTrue();
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    [Fact]
    public async Task Diretor_ve_os_perfis_ativos_e_inativos_com_as_acoes()
    {
        var cliente = await _app.ClienteAutenticadoAsync();

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/perfis"));

        html.Should().Contain("Perfis").And.Contain("Diretor").And.Contain("Produção").And.Contain("Estagiário");
        html.Should().Contain("Diretoria").And.Contain("Inativo");
        html.Should().Contain("Novo perfil").And.Contain("Renomear").And.Contain("Inativar").And.Contain("Ativar");
        html.Should().NotContain("Acesso negado");
    }

    [Fact]
    public async Task Producao_nao_ve_a_gestao_de_perfis()
    {
        await _app.CriarPedroAsync();
        var cliente = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/perfis"));

        html.Should().Contain("Acesso negado.");
        html.Should().NotContain("Estagiário").And.NotContain("Novo perfil").And.NotContain("Renomear");
    }

    [Fact]
    public async Task Menu_mostra_perfis_so_para_a_diretoria()
    {
        await _app.CriarPedroAsync();
        var doDiretor = await _app.ClienteAutenticadoAsync();
        var doPedro = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        (await doDiretor.GetStringAsync("/")).Should().Contain("href=\"/perfis\"");
        (await doPedro.GetStringAsync("/")).Should().NotContain("href=\"/perfis\"");
    }

    [Fact]
    public async Task Anonimo_e_redirecionado_ao_login()
    {
        var resposta = await _app.NovoCliente().GetAsync("/perfis");

        resposta.StatusCode.Should().Be(HttpStatusCode.Redirect);
        resposta.Headers.Location!.AbsolutePath.Should().Be("/conta/entrar");
    }
}
```

Se houver um teste que enumera as páginas protegidas (`PaginasProtegidasTests`), acrescente `/perfis` a ele.

- [ ] **Step 2: Rodar e ver falhar**

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --filter "FullyQualifiedName~PerfisWebTests"`
Expected: FAIL — a página não existe.

- [ ] **Step 3: Página**

`src/AtividadesJuridicas.Web/Components/Pages/Perfis.razor`:

```razor
@page "/perfis"
@using System.ComponentModel.DataAnnotations
@inject IDespachante Despachante

<PageTitle>Perfis</PageTitle>

<h1>Perfis</h1>

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
            <tr><th>Nome</th><th>Tipo</th><th>Usuários ativos</th><th>Situação</th><th></th></tr>
        </thead>
        <tbody>
            @foreach (var perfil in _perfis)
            {
                <tr class="@(perfil.Ativo ? null : "inativo")" @key="perfil.Id">
                    <td>@perfil.Nome</td>
                    <td>@NomeDoTipo(perfil.Tipo)</td>
                    <td>@perfil.UsuariosAtivos</td>
                    <td>@(perfil.Ativo ? "Ativo" : "Inativo")</td>
                    <td class="acoes">
                        <button type="button" class="secundario" @onclick="() => Editar(perfil)">Renomear</button>
                        <button type="button" class="secundario" @onclick="() => AlternarAtivoAsync(perfil)">
                            @(perfil.Ativo ? "Inativar" : "Ativar")
                        </button>
                    </td>
                </tr>
            }
        </tbody>
    </table>

    <h2>@(_form.Id is null ? "Novo perfil" : "Renomear perfil")</h2>

    <EditForm Model="_form" OnValidSubmit="SalvarAsync" FormName="perfil" class="formulario">
        <DataAnnotationsValidator />
        <label>
            Nome
            <InputText @bind-Value="_form.Nome" />
            <ValidationMessage For="() => _form.Nome" />
        </label>
        <label>
            Tipo
            <InputSelect @bind-Value="_form.Tipo" disabled="@(_form.Id is not null)">
                <option value="@TipoPerfil.Producao">Produção</option>
                <option value="@TipoPerfil.Diretoria">Diretoria</option>
            </InputSelect>
        </label>
        @if (_form.Id is null)
        {
            <p>O tipo não pode ser mudado depois. Um perfil novo passa a acessar todos os Documentos Gerais já existentes; um perfil de Diretoria acessa tudo, inclusive os Contratos de Honorários.</p>
        }
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
    private IReadOnlyList<PerfilDetalhe> _perfis = [];
    private Formulario _form = new();

    protected override async Task OnInitializedAsync()
    {
        _autorizado = await Despachante.EnviarAsync(new ObterMeuAcesso()) is { EhDiretoria: true };
        if (_autorizado) await RecarregarAsync();
        _carregado = true;
    }

    private static string NomeDoTipo(TipoPerfil tipo) => tipo == TipoPerfil.Diretoria ? "Diretoria" : "Produção";

    // Falso quando a recarga falhou; a mensagem do motivo já ficou na tela.
    private async Task<bool> RecarregarAsync()
    {
        var resultado = await Despachante.EnviarAsync(new ListarPerfis());
        if (resultado.Erro == TipoErro.AcessoNegado)
        {
            _autorizado = false;
            return false;
        }

        _perfis = resultado.Valor ?? [];
        if (resultado.Sucesso) return true;

        // Falha que não é de acesso: mostra o motivo em vez de uma lista vazia sem explicação.
        _falhou = true;
        _mensagem = resultado.Mensagem;
        return false;
    }

    private void Limpar()
    {
        _form = new Formulario();
        _mensagem = null;
    }

    private void Editar(PerfilDetalhe perfil)
    {
        _mensagem = null;
        _form = new Formulario { Id = perfil.Id, Nome = perfil.Nome, Tipo = perfil.Tipo };
    }

    private async Task SalvarAsync()
    {
        var resultado = await Despachante.EnviarAsync(new SalvarPerfil(_form.Id, _form.Nome, _form.Tipo));
        if (await ConcluirAsync(resultado, "Perfil salvo.")) _form = new Formulario();
    }

    private async Task AlternarAtivoAsync(PerfilDetalhe perfil)
    {
        var resultado = await Despachante.EnviarAsync(new DefinirAtivoPerfil(perfil.Id, !perfil.Ativo));
        await ConcluirAsync(resultado, perfil.Ativo ? "Perfil inativado." : "Perfil ativado.");
    }

    // Verdadeiro quando a ação deu certo.
    private async Task<bool> ConcluirAsync(Resultado resultado, string mensagemDeSucesso)
    {
        // Perdeu o acesso no meio do uso: some a tela de gestão, o servidor já recusou a gravação.
        if (resultado.Erro == TipoErro.AcessoNegado)
        {
            _autorizado = false;
            return false;
        }

        if (!resultado.Sucesso)
        {
            _falhou = true;
            _mensagem = resultado.Mensagem;
            return false;
        }

        // A mensagem de sucesso não pode esconder uma falha na recarga da lista.
        if (await RecarregarAsync())
        {
            _falhou = false;
            _mensagem = mensagemDeSucesso;
        }
        return true;
    }

    private sealed class Formulario
    {
        public Guid? Id { get; set; }

        [Required(ErrorMessage = "Informe o nome.")]
        public string Nome { get; set; } = string.Empty;

        public TipoPerfil Tipo { get; set; } = TipoPerfil.Producao;
    }
}
```

Em `MainLayout.razor`, dentro do bloco da Diretoria, entre "Usuários" e "Auditoria": `<NavLink href="/perfis">Perfis</NavLink>`.

- [ ] **Step 4: Perfis inativos no editor de acesso dos documentos**

Em `DocumentosDoCliente.razor`, `AbrirAcessoAsync` passa a consultar `ListarPerfis` (todos, com `Ativo`) no lugar de `ListarPerfisAtivos`:

- opções para incluir: os perfis **ativos** (os de Diretoria com o rótulo "(Diretoria — acessa sempre)", como hoje);
- um perfil que **já consta** na lista do documento e está inativo aparece com o nome e o rótulo "(inativo)", com a caixa funcionando, para poder ser retirado;
- um perfil inativo que **não** consta na lista não aparece (o domínio recusaria incluí-lo);
- "Perfil não identificado" continua existindo para um Id que `ListarPerfis` não devolva.

Nada mais muda no componente. Descreva no relatório como verificou cada caso por leitura (o projeto não tem teste de componente).

- [ ] **Step 5: Rodar tudo**

Run: `dotnet test` (suíte inteira, da raiz)
Expected: PASS, tudo, sem avisos.

- [ ] **Step 6: Commit**

```bash
git add src tests
git commit -m "feat(web): tela de perfis e perfis inativos identificados no editor de acesso"
```
