# Fase 5A — Atividades (núcleo): Plano de Implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Permitir que a Diretoria crie uma atividade, a direcione a um ou mais responsáveis com revisor e data de entrega, e que ela percorra o fluxo de status (concluir a parte, aprovar, devolver, reabrir) com todo o percurso registrado; a Produção vê só as atividades em que é responsável e só o seu direcionamento.

**Architecture:** `Atividade` é o agregado do contexto Atividades, com duas coleções de entidades filhas que derivam de `Entidade` (`AtividadeResponsavel`, `AtividadeHistoricoStatus`). Toda a máquina de status, a regra do revisor e a conclusão por responsável ficam no domínio; os casos de uso só carregam os dados que o domínio precisa (quem é ativo e de que tipo) e autorizam. O agregado tem controle de concorrência otimista (`xmin`) e um contador que faz toda mudança nas partes gravar também a linha da atividade, para que duas conclusões simultâneas não deixem a atividade presa.

**Tech Stack:** .NET 10, Blazor Interactive Server, EF Core 10 + Npgsql, PostgreSQL 17, xUnit 2.9, FluentAssertions 7.2, Testcontainers.

**Spec:** `docs/superpowers/specs/2026-10-06-modelagem-dominio-design.md` (seções 1, 4, 5, 8, 10, 11 e 12).

## Decisões deste plano

- **Fase 5 em duas partes.** 5A (este plano): o agregado, o fluxo de status, os casos de uso e a tela. 5B (plano seguinte): comentários, documentos de atividade (com a área "Documentos de Atividades" na ficha do cliente) e as regras de processo que dependem de atividades (não remover cliente com atividade; propagar a troca de área).
- **Autor "Sistema" só no domínio.** Nenhum caso de uso cria atividade pelo sistema nesta fase (fica para os recursos de IA); o domínio já aceita os dois tipos de autor.
- **Transição automática leva o usuário que a provocou.** Quando o último responsável conclui a parte, a linha do histórico (`Feita`, `Corrigida`) registra esse usuário; `UsuarioId` nulo fica reservado a transições feitas pelo próprio sistema.
- **Remover o último responsável pendente avança a atividade.** Se a Diretoria tira da atividade quem ainda devia, e todos os que ficaram já concluíram, a atividade avança (`Feita`/`Corrigida`) como se o último tivesse concluído. Sem isso, uma atividade ficaria presa quando o pendente deixa o escritório.
- **Atividade direcionada não fica sem responsável.** Depois de `Criada`, a lista de responsáveis não pode ficar vazia.
- **Travamento do revisor só conta diretor ativo.** "Exatamente um responsável de Diretoria" considera só responsáveis ativos com perfil de Diretoria; se esse responsável for inativado ou mudar de perfil, a escolha do revisor volta a ser livre.
- **Trocar o revisor é permitido também em `Feita` e `Corrigida`** (a spec lista só `Criada`, `Direcionada`, `ParaCorrecao` e `Reaberta`). Sem isso, uma atividade aguardando revisão ficaria presa se o revisor deixasse o escritório. Só não se troca o revisor de uma atividade `Concluida`.
- **Só o revisor aprova e devolve**, e ele precisa continuar sendo de Diretoria.
- **Quem não pode ver uma atividade recebe "não encontrada"**, e a tentativa fica registrada (como nos documentos).
- **Edições simultâneas**: a segunda gravação é recusada como conflito, com a mensagem "A atividade foi alterada por outra pessoa. Recarregue e tente novamente."
- **"Atrasada" usa a data do escritório**, não a data UTC: a aplicação ganha a porta `ICalendarioDoEscritorio`.
- **Cliente ou área inativos** não entram em vínculo novo, mas permanecem na atividade que já os tinha (mesma regra dos processos).

## Como ler este plano

Os **testes** de cada tarefa definem o comportamento exigido. O código de implementação é o desenho pretendido, escrito sem ter sido executado: quando um detalhe do C#, do EF Core 10, do Npgsql ou do Blazor exigir, ajuste o mínimo necessário para preservar o comportamento e registre o desvio no relatório, com a mensagem de erro que o motivou. Nunca apague um teste nem afrouxe uma asserção. Ajustar como um teste obtém suas entradas é permitido. O texto de `Dados` gravado em `jsonb` volta reformatado pelo PostgreSQL: compare JSON pela estrutura, nunca por texto exato.

## Estado de partida (fase 4, na `main`)

- Domínio: `Usuario`, `Perfil`; `AreaAtuacao`, `Cliente`, `Processo` (com `ProcessoCliente`, `Versao` em `xmin` e `VersaoDosVinculos`: o padrão de concorrência a seguir); `Documento`.
- Aplicação: `IContextoDeAcesso` → `UsuarioAutenticado(Id, Nome, TipoPerfil, PerfilId)`; `IUsuarioRepositorio.ObterVariosAsync`, `IPerfilRepositorio.ObterVariosAsync`; `IClienteRepositorio.ObterPorIdAsync`, `IProcessoRepositorio.ObterPorIdAsync`, `IAreaAtuacaoRepositorio.ObterPorIdAsync`; `IRelogio.AgoraUtc`; `IAuditoria`; `Pagina<T>`, `Paginacao.Normalizar`; `ExcecaoDeConcorrencia`, `ExcecaoDeConflito`.
- Infraestrutura: `ProcessoConfiguracao` (filhas com `ClientCascade` e acesso por campo; `xmin`); `AddInfrastructure(servicos, obterStringDeConexao, obterPastaDeArquivos, criarPastaDeArquivos)`.
- Web: `Processos.razor` e `Clientes.razor` como modelos de página; `FusoDoEscritorio` (fuso configurado em `Escritorio:FusoHorario`).
- Testes: 397 unitários e 259 de integração passando.

## Global Constraints

- TFM único `net10.0` em `Directory.Build.props`. `Nullable` e `TreatWarningsAsErrors=true`. Versões de pacote só em `Directory.Packages.props`. Nenhum pacote novo.
- `Domain` não referencia nada. `Application` não referencia EF Core, Npgsql, `Infrastructure` nem `Web`.
- Código, nomes de tabela e mensagens em português. Tabelas e colunas em `snake_case`. Datas e horas em UTC; `DataEntrega` é uma data (sem hora).
- Exclusão sempre lógica, inclusive das entidades filhas. Agregados se referenciam só por Id.
- Toda alteração de `Entidade` gera registro de auditoria na mesma transação. Toda propriedade nova é classificada em `PropriedadesAuditadasTests`. Todo tipo mapeado deriva de `Entidade`. Nenhuma gravação por fora dos interceptores.
- Matriz de acesso: atividades — Diretoria tudo; Produção somente as em que é responsável, e só o próprio direcionamento.
- Autorização verificada no manipulador; a interface apenas oculta. Enquanto a consulta de acesso não termina, a página mostra "Carregando…".
- Um recurso que o usuário não pode ver responde `NaoEncontrado`.
- Consultas são `IConsultaAuditavel`: listagem registra `Listou`; abrir uma atividade registra `Visualizou` com o `RecursoId`. O `Detalhe` usa `DetalheDeConsulta.Serializar`.
- Qualquer transição de status fora da tabela da spec é recusada pelo domínio. Toda transição insere uma linha em `AtividadeHistoricoStatus`.
- Um caso de uso por arquivo. Novas dependências de construtor entram por último.

## Review Focus

1. **Dois responsáveis concluem a parte ao mesmo tempo**: a atividade não fica presa em `Direcionada` com todas as partes concluídas; a segunda gravação é recusada como conflito e, repetida, avança a atividade. (Tarefa 2)
2. **O revisor é o único responsável**: concluir a parte leva direto a `Concluida`, com as duas linhas no histórico. (Tarefa 1)
3. **Devolução com só parte dos responsáveis selecionada**: só os selecionados voltam a ficar pendentes, e a atividade só vira `Corrigida` quando o último deles conclui. (Tarefa 1)
4. **Um usuário de Produção pede uma atividade em que não é responsável, ou o direcionamento de outro**: recebe "não encontrada" no primeiro caso e não recebe o texto no segundo. (Tarefa 2)
5. **Processo informado com cliente que não é do processo**: recusado; a área da atividade é sempre a do processo. (Tarefas 1 e 2)
6. **O responsável pendente é removido de uma atividade direcionada**: a atividade avança se todos os que ficaram já concluíram. (Tarefa 1)

---

### Task 1: `Atividade` no domínio

**Files:**
- Create: `src/AtividadesJuridicas.Domain/Atividades/Atividade.cs`
- Create: `src/AtividadesJuridicas.Domain/Atividades/AtividadeResponsavel.cs`
- Create: `src/AtividadesJuridicas.Domain/Atividades/AtividadeHistoricoStatus.cs`
- Create: `src/AtividadesJuridicas.Domain/Atividades/TiposDaAtividade.cs`
- Test: `tests/AtividadesJuridicas.UnitTests/Domain/AtividadeTests.cs`

**Interfaces:**
- Consumes: `Entidade`, `ExcecaoDeDominio`, `NaoAuditarAttribute`; `Usuario` (`Id`, `PerfilId`, `Ativo`), `Perfil` (`Id`, `Tipo`), `TipoPerfil`; `Processo` (`Id`, `AreaAtuacaoId`, `Clientes`).
- Produces (namespace `AtividadesJuridicas.Domain.Atividades`):
  - `enum StatusAtividade { Criada = 1, Direcionada = 2, Feita = 3, ParaCorrecao = 4, Corrigida = 5, Concluida = 6, Reaberta = 7 }`
  - `enum TipoAutor { Usuario = 1, Sistema = 2 }`
  - `record PessoaDaAtividade(Guid UsuarioId, TipoPerfil Tipo, bool Ativo)` com `bool EhDiretoriaAtiva` e `static PessoaDaAtividade De(Usuario usuario, Perfil perfil)`
  - `record ResponsavelInformado(PessoaDaAtividade Pessoa, string? Direcionamento)`
  - `record DadosDaAtividade(string Titulo, string Descricao, DateOnly? DataEntrega)`
  - `record VinculoDaAtividade` com `Guid? ClienteId`, `Guid? ProcessoId`, `Guid? AreaAtuacaoId` e `static VinculoDaAtividade Criar(Guid? clienteId, Processo? processo, Guid? areaAtuacaoId)`; `static VinculoDaAtividade Avulsa`
  - `record AlteracaoDeResponsaveis(IReadOnlyList<Guid> Adicionados, IReadOnlyList<Guid> Removidos, IReadOnlyList<Guid> DirecionamentoAlterado)` com `bool Houve`
  - `Atividade.Criar(DadosDaAtividade dados, VinculoDaAtividade vinculo, TipoAutor tipoAutor, Guid? autorId, DateTime agoraUtc)`
  - `atividade.Alterar(DadosDaAtividade dados, VinculoDaAtividade vinculo)`
  - `atividade.DefinirResponsaveis(IReadOnlyCollection<ResponsavelInformado> responsaveis, PessoaDaAtividade? revisor, DateTime agoraUtc, Guid? atorId): AlteracaoDeResponsaveis`
  - `atividade.TrocarRevisor(PessoaDaAtividade revisor, IReadOnlyCollection<PessoaDaAtividade> responsaveisAtuais)`
  - `atividade.ConcluirParte(Guid usuarioId, DateTime agoraUtc)`
  - `atividade.Aprovar(Guid revisorId, DateTime agoraUtc)`
  - `atividade.Devolver(Guid revisorId, string motivo, IReadOnlyCollection<Guid> responsaveis, DateTime agoraUtc)`
  - `atividade.Reabrir(string motivo, IReadOnlyCollection<Guid> responsaveis, DateTime agoraUtc, Guid? atorId)`
  - `atividade.EstaAtrasada(DateOnly hoje)`, `atividade.EhResponsavel(Guid usuarioId)`, `atividade.PodeAlterarResponsaveis`, `atividade.PodeTrocarRevisor`
  - Propriedades: `Titulo`, `Descricao`, `DataEntrega`, `Status`, `ConcluidaEm`, `ClienteId`, `ProcessoId`, `AreaAtuacaoId`, `TipoAutor`, `AutorId`, `RevisorId`, `Versao` (`uint`, `[NaoAuditar]`), `VersaoDasPartes` (`int`, `[NaoAuditar]`), `Responsaveis` (`IReadOnlyCollection<AtividadeResponsavel>`, campo `_responsaveis`), `Historico` (`IReadOnlyCollection<AtividadeHistoricoStatus>`, campo `_historico`)
  - `AtividadeResponsavel { AtividadeId, UsuarioId, Direcionamento, ConcluidoEm, bool Pendente }`, `AtividadeHistoricoStatus { AtividadeId, Ordem (int, 1 na criação e crescente a cada transição), StatusAnterior (StatusAtividade?), StatusNovo, UsuarioId (Guid?), DataHora, Motivo }`, ambos `: Entidade`
  - Constantes de mensagem públicas em `Atividade`: `SoORevisor`, `RevisorTravado`

- [ ] **Step 1: Escrever os testes**

`tests/AtividadesJuridicas.UnitTests/Domain/AtividadeTests.cs`:

```csharp
using AtividadesJuridicas.Domain.Atividades;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Comum;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.UnitTests.Domain;

public class AtividadeTests
{
    private static readonly DateTime T0 = new(2026, 10, 9, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Entrega = new(2026, 10, 20);

    // Pessoas: dois diretores e três de Produção.
    private readonly PessoaDaAtividade _diana = new(Guid.CreateVersion7(), TipoPerfil.Diretoria, true);
    private readonly PessoaDaAtividade _davi = new(Guid.CreateVersion7(), TipoPerfil.Diretoria, true);
    private readonly PessoaDaAtividade _pedro = new(Guid.CreateVersion7(), TipoPerfil.Producao, true);
    private readonly PessoaDaAtividade _paula = new(Guid.CreateVersion7(), TipoPerfil.Producao, true);
    private readonly PessoaDaAtividade _ester = new(Guid.CreateVersion7(), TipoPerfil.Producao, true);

    private static DadosDaAtividade Dados(string titulo = "Elaborar contestação", DateOnly? entrega = null, bool semEntrega = false) =>
        new(titulo, "Prazo de 15 dias.", semEntrega ? null : entrega ?? Entrega);

    private Atividade Nova(bool semEntrega = false) =>
        Atividade.Criar(Dados(semEntrega: semEntrega), VinculoDaAtividade.Avulsa, TipoAutor.Usuario, _diana.UsuarioId, T0);

    private static ResponsavelInformado R(PessoaDaAtividade pessoa, string? direcionamento = null) => new(pessoa, direcionamento);

    /// <summary>Direcionada a Pedro e Paula, com Diana de revisora.</summary>
    private Atividade Direcionada()
    {
        var atividade = Nova();
        atividade.DefinirResponsaveis([R(_pedro, "Redigir"), R(_paula, "Juntar documentos")], _diana, T0.AddMinutes(1), _diana.UsuarioId);
        return atividade;
    }

    /// <summary>Feita: Pedro e Paula concluíram.</summary>
    private Atividade Feita()
    {
        var atividade = Direcionada();
        atividade.ConcluirParte(_pedro.UsuarioId, T0.AddHours(1));
        atividade.ConcluirParte(_paula.UsuarioId, T0.AddHours(2));
        return atividade;
    }

    private Atividade Concluida()
    {
        var atividade = Feita();
        atividade.Aprovar(_diana.UsuarioId, T0.AddHours(3));
        return atividade;
    }

    // O histórico é mantido na ordem das transições; Ordem numera cada linha dentro da atividade.
    private static (StatusAtividade? De, StatusAtividade Para)[] Percurso(Atividade atividade) =>
        [.. atividade.Historico.OrderBy(h => h.Ordem).Select(h => (h.StatusAnterior, h.StatusNovo))];

    // ---- Criação e dados ----

    [Fact]
    public void Nasce_criada_com_uma_linha_de_historico_e_sem_responsaveis()
    {
        var atividade = Nova();

        atividade.Status.Should().Be(StatusAtividade.Criada);
        atividade.Titulo.Should().Be("Elaborar contestação");
        atividade.Descricao.Should().Be("Prazo de 15 dias.");
        atividade.DataEntrega.Should().Be(Entrega);
        atividade.TipoAutor.Should().Be(TipoAutor.Usuario);
        atividade.AutorId.Should().Be(_diana.UsuarioId);
        atividade.RevisorId.Should().BeNull();
        atividade.ConcluidaEm.Should().BeNull();
        atividade.Responsaveis.Should().BeEmpty();
        var linha = atividade.Historico.Should().ContainSingle().Subject;
        linha.StatusAnterior.Should().BeNull();
        linha.StatusNovo.Should().Be(StatusAtividade.Criada);
        linha.UsuarioId.Should().Be(_diana.UsuarioId);
        linha.DataHora.Should().Be(T0);
        linha.Ordem.Should().Be(1);
        linha.AtividadeId.Should().Be(atividade.Id);
    }

    [Fact]
    public void Atividade_do_sistema_nao_tem_autor()
    {
        var atividade = Atividade.Criar(Dados(), VinculoDaAtividade.Avulsa, TipoAutor.Sistema, null, T0);

        atividade.TipoAutor.Should().Be(TipoAutor.Sistema);
        atividade.AutorId.Should().BeNull();
        atividade.Historico.Single().UsuarioId.Should().BeNull();
    }

    [Fact]
    public void Autor_e_coerente_com_o_tipo()
    {
        var semAutor = () => Atividade.Criar(Dados(), VinculoDaAtividade.Avulsa, TipoAutor.Usuario, null, T0);
        var sistemaComAutor = () => Atividade.Criar(Dados(), VinculoDaAtividade.Avulsa, TipoAutor.Sistema, _diana.UsuarioId, T0);

        semAutor.Should().Throw<ExcecaoDeDominio>().WithMessage("O autor da atividade é obrigatório.");
        sistemaComAutor.Should().Throw<ExcecaoDeDominio>().WithMessage("Atividade criada pelo sistema não tem autor.");
    }

    [Theory]
    [InlineData("", "Descrição", "O título da atividade é obrigatório.")]
    [InlineData("   ", "Descrição", "O título da atividade é obrigatório.")]
    [InlineData("Título", "", "A descrição da atividade é obrigatória.")]
    [InlineData("Título", "  ", "A descrição da atividade é obrigatória.")]
    public void Titulo_e_descricao_sao_obrigatorios(string titulo, string descricao, string mensagem)
    {
        var acao = () => Atividade.Criar(new DadosDaAtividade(titulo, descricao, null), VinculoDaAtividade.Avulsa, TipoAutor.Usuario, _diana.UsuarioId, T0);

        acao.Should().Throw<ExcecaoDeDominio>().WithMessage(mensagem);
    }

    [Fact]
    public void Titulo_e_descricao_tem_limite_e_sao_aparados()
    {
        var longoDemais = () => Atividade.Criar(new DadosDaAtividade(new string('a', 201), "d", null), VinculoDaAtividade.Avulsa, TipoAutor.Usuario, _diana.UsuarioId, T0);
        var descricaoLonga = () => Atividade.Criar(new DadosDaAtividade("t", new string('a', 10001), null), VinculoDaAtividade.Avulsa, TipoAutor.Usuario, _diana.UsuarioId, T0);
        var aparada = Atividade.Criar(new DadosDaAtividade("  Petição  ", "  Texto  ", null), VinculoDaAtividade.Avulsa, TipoAutor.Usuario, _diana.UsuarioId, T0);

        longoDemais.Should().Throw<ExcecaoDeDominio>().WithMessage("O título da atividade deve ter até 200 caracteres.");
        descricaoLonga.Should().Throw<ExcecaoDeDominio>().WithMessage("A descrição da atividade deve ter até 10000 caracteres.");
        aparada.Titulo.Should().Be("Petição");
        aparada.Descricao.Should().Be("Texto");
    }

    [Fact]
    public void Data_de_entrega_e_opcional_enquanto_criada_e_obrigatoria_depois()
    {
        var criada = Nova(semEntrega: true);
        criada.DataEntrega.Should().BeNull();

        var direcionada = Direcionada();
        var acao = () => direcionada.Alterar(Dados(semEntrega: true), VinculoDaAtividade.Avulsa);

        acao.Should().Throw<ExcecaoDeDominio>().WithMessage("A data de entrega é obrigatória depois que a atividade é direcionada.");
        direcionada.DataEntrega.Should().Be(Entrega);
    }

    [Fact]
    public void Alterar_troca_dados_e_vinculo_sem_mexer_no_status()
    {
        var atividade = Direcionada();
        var cliente = Guid.CreateVersion7();
        var area = Guid.CreateVersion7();

        atividade.Alterar(Dados("Novo título", new DateOnly(2026, 11, 1)), VinculoDaAtividade.Criar(cliente, null, area));

        atividade.Titulo.Should().Be("Novo título");
        atividade.DataEntrega.Should().Be(new DateOnly(2026, 11, 1));
        atividade.ClienteId.Should().Be(cliente);
        atividade.ProcessoId.Should().BeNull();
        atividade.AreaAtuacaoId.Should().Be(area);
        atividade.Status.Should().Be(StatusAtividade.Direcionada);
        atividade.Historico.Should().HaveCount(2);
    }

    // ---- Vínculo ----

    private static Processo NovoProcesso(Guid area, params Guid[] clientes) => Processo.Criar(
        new DadosDoProcesso(null, "Reclamação trabalhista", area, null, null, null, null, null, null),
        [.. clientes.Select(c => new ClienteDoProcesso(c, PoloProcessual.Ativo))]);

    [Fact]
    public void Atividade_avulsa_nao_tem_cliente_processo_nem_area()
    {
        VinculoDaAtividade.Avulsa.Should().Be(VinculoDaAtividade.Criar(null, null, null));
        VinculoDaAtividade.Avulsa.ClienteId.Should().BeNull();
        VinculoDaAtividade.Avulsa.ProcessoId.Should().BeNull();
        VinculoDaAtividade.Avulsa.AreaAtuacaoId.Should().BeNull();
    }

    [Fact]
    public void Cliente_sem_processo_aceita_area_informada_ou_nenhuma()
    {
        var cliente = Guid.CreateVersion7();
        var area = Guid.CreateVersion7();

        VinculoDaAtividade.Criar(cliente, null, area).Should().BeEquivalentTo(new { ClienteId = (Guid?)cliente, ProcessoId = (Guid?)null, AreaAtuacaoId = (Guid?)area });
        VinculoDaAtividade.Criar(cliente, null, null).AreaAtuacaoId.Should().BeNull();
        VinculoDaAtividade.Criar(null, null, area).AreaAtuacaoId.Should().Be(area, "uma atividade avulsa pode ter área");
    }

    [Fact]
    public void Com_processo_o_cliente_e_obrigatorio_e_tem_de_ser_do_processo()
    {
        var area = Guid.CreateVersion7();
        var maria = Guid.CreateVersion7();
        var processo = NovoProcesso(area, maria);

        var semCliente = () => VinculoDaAtividade.Criar(null, processo, null);
        var clienteDeFora = () => VinculoDaAtividade.Criar(Guid.CreateVersion7(), processo, null);

        semCliente.Should().Throw<ExcecaoDeDominio>().WithMessage("Com processo, o cliente da atividade é obrigatório.");
        clienteDeFora.Should().Throw<ExcecaoDeDominio>().WithMessage("O cliente da atividade deve ser um dos clientes do processo.");
    }

    [Fact]
    public void Com_processo_a_area_e_sempre_a_do_processo()
    {
        var area = Guid.CreateVersion7();
        var maria = Guid.CreateVersion7();
        var processo = NovoProcesso(area, maria);

        var vinculo = VinculoDaAtividade.Criar(maria, processo, Guid.CreateVersion7());

        vinculo.ClienteId.Should().Be(maria);
        vinculo.ProcessoId.Should().Be(processo.Id);
        vinculo.AreaAtuacaoId.Should().Be(area, "a área informada é ignorada quando há processo");
    }

    // ---- Direcionar ----

    [Fact]
    public void Definir_responsaveis_revisor_e_data_direciona_a_atividade()
    {
        var atividade = Nova();

        var alteracao = atividade.DefinirResponsaveis([R(_pedro, " Redigir "), R(_paula)], _diana, T0.AddMinutes(1), _diana.UsuarioId);

        atividade.Status.Should().Be(StatusAtividade.Direcionada);
        atividade.RevisorId.Should().Be(_diana.UsuarioId);
        atividade.Responsaveis.Select(r => (r.UsuarioId, r.Direcionamento, r.Pendente)).Should().BeEquivalentTo(
            [(_pedro.UsuarioId, (string?)"Redigir", true), (_paula.UsuarioId, (string?)null, true)]);
        atividade.Responsaveis.Should().OnlyContain(r => r.AtividadeId == atividade.Id);
        alteracao.Adicionados.Should().BeEquivalentTo([_pedro.UsuarioId, _paula.UsuarioId]);
        alteracao.Houve.Should().BeTrue();
        Percurso(atividade).Should().Equal((null, StatusAtividade.Criada), (StatusAtividade.Criada, StatusAtividade.Direcionada));
        atividade.Historico.Last().UsuarioId.Should().Be(_diana.UsuarioId);
        atividade.VersaoDasPartes.Should().Be(1);
    }

    [Fact]
    public void Sem_responsaveis_a_atividade_continua_criada_mas_guarda_o_revisor()
    {
        var atividade = Nova(semEntrega: true);

        atividade.DefinirResponsaveis([], _diana, T0, _diana.UsuarioId);

        atividade.Status.Should().Be(StatusAtividade.Criada);
        atividade.RevisorId.Should().Be(_diana.UsuarioId);
        atividade.Historico.Should().ContainSingle();
    }

    [Fact]
    public void Direcionar_exige_revisor_e_data_de_entrega()
    {
        var semData = Nova(semEntrega: true);
        var semRevisor = Nova();

        var acaoSemData = () => semData.DefinirResponsaveis([R(_pedro)], _diana, T0, _diana.UsuarioId);
        var acaoSemRevisor = () => semRevisor.DefinirResponsaveis([R(_pedro)], null, T0, _diana.UsuarioId);

        acaoSemData.Should().Throw<ExcecaoDeDominio>().WithMessage("Informe a data de entrega antes de direcionar a atividade.");
        acaoSemRevisor.Should().Throw<ExcecaoDeDominio>().WithMessage("Informe o revisor da atividade.");
        semData.Status.Should().Be(StatusAtividade.Criada);
        semData.Responsaveis.Should().BeEmpty();
        semData.RevisorId.Should().BeNull();
        semRevisor.Responsaveis.Should().BeEmpty();
    }

    [Fact]
    public void Um_usuario_nao_se_repete_entre_os_responsaveis()
    {
        var atividade = Nova();

        var acao = () => atividade.DefinirResponsaveis([R(_pedro), R(_pedro, "outra vez")], _diana, T0, _diana.UsuarioId);

        acao.Should().Throw<ExcecaoDeDominio>().WithMessage("Um usuário não pode se repetir entre os responsáveis.");
        atividade.Responsaveis.Should().BeEmpty();
    }

    [Fact]
    public void Usuario_inativo_nao_entra_como_responsavel_mas_quem_ja_estava_permanece()
    {
        var atividade = Direcionada();
        var pedroInativo = _pedro with { Ativo = false };
        var esterInativa = _ester with { Ativo = false };

        var incluir = () => atividade.DefinirResponsaveis([R(_pedro), R(_paula), R(esterInativa)], _diana, T0, _diana.UsuarioId);
        incluir.Should().Throw<ExcecaoDeDominio>().WithMessage("Usuário inativo não pode ser incluído como responsável.");

        var alteracao = atividade.DefinirResponsaveis([R(pedroInativo, "Redigir"), R(_paula, "Juntar documentos")], _diana, T0, _diana.UsuarioId);
        alteracao.Houve.Should().BeFalse();
        atividade.Responsaveis.Should().HaveCount(2);
    }

    [Fact]
    public void Direcionamento_tem_limite()
    {
        var atividade = Nova();

        var acao = () => atividade.DefinirResponsaveis([R(_pedro, new string('a', 4001))], _diana, T0, _diana.UsuarioId);

        acao.Should().Throw<ExcecaoDeDominio>().WithMessage("O direcionamento deve ter até 4000 caracteres.");
    }

    [Fact]
    public void Redefinir_responsaveis_preserva_quem_fica_e_conta_o_que_mudou()
    {
        var atividade = Direcionada();
        atividade.ConcluirParte(_pedro.UsuarioId, T0.AddHours(1));
        var entradaDoPedro = atividade.Responsaveis.Single(r => r.UsuarioId == _pedro.UsuarioId);

        var alteracao = atividade.DefinirResponsaveis([R(_pedro, "Redigir e revisar"), R(_ester, "Protocolar")], _diana, T0.AddHours(2), _diana.UsuarioId);

        alteracao.Adicionados.Should().Equal(_ester.UsuarioId);
        alteracao.Removidos.Should().Equal(_paula.UsuarioId);
        alteracao.DirecionamentoAlterado.Should().Equal(_pedro.UsuarioId);
        atividade.Responsaveis.Single(r => r.UsuarioId == _pedro.UsuarioId).Should().BeSameAs(entradaDoPedro);
        entradaDoPedro.Direcionamento.Should().Be("Redigir e revisar");
        entradaDoPedro.Pendente.Should().BeFalse("a conclusão de quem fica é preservada");
        atividade.Responsaveis.Single(r => r.UsuarioId == _ester.UsuarioId).Pendente.Should().BeTrue();
        atividade.Status.Should().Be(StatusAtividade.Direcionada);
    }

    [Fact]
    public void Atividade_direcionada_nao_fica_sem_responsavel()
    {
        var atividade = Direcionada();

        var acao = () => atividade.DefinirResponsaveis([], _diana, T0, _diana.UsuarioId);

        acao.Should().Throw<ExcecaoDeDominio>().WithMessage("A atividade precisa de ao menos um responsável.");
        atividade.Responsaveis.Should().HaveCount(2);
    }

    [Fact]
    public void Remover_o_ultimo_pendente_avanca_a_atividade()
    {
        var atividade = Direcionada();
        atividade.ConcluirParte(_pedro.UsuarioId, T0.AddHours(1));

        atividade.DefinirResponsaveis([R(_pedro, "Redigir")], _diana, T0.AddHours(2), _diana.UsuarioId);

        atividade.Status.Should().Be(StatusAtividade.Feita);
        atividade.Historico.Last().UsuarioId.Should().Be(_diana.UsuarioId);
    }

    [Theory]
    [InlineData(StatusAtividade.Feita)]
    [InlineData(StatusAtividade.Concluida)]
    public void Responsaveis_nao_mudam_enquanto_a_atividade_aguarda_revisao_ou_esta_concluida(StatusAtividade status)
    {
        var atividade = status == StatusAtividade.Feita ? Feita() : Concluida();

        var acao = () => atividade.DefinirResponsaveis([R(_pedro), R(_paula), R(_ester)], _diana, T0, _diana.UsuarioId);

        acao.Should().Throw<ExcecaoDeDominio>()
            .WithMessage("Os responsáveis só podem ser alterados enquanto a atividade está criada, direcionada, em correção ou reaberta.");
        atividade.PodeAlterarResponsaveis.Should().BeFalse();
        atividade.Responsaveis.Should().HaveCount(2);
    }

    [Fact]
    public void Elemento_nulo_e_erro_de_programacao()
    {
        var atividade = Nova();

        var acao = () => atividade.DefinirResponsaveis([R(_pedro), null!], _diana, T0, _diana.UsuarioId);

        acao.Should().Throw<ArgumentException>();
        atividade.Responsaveis.Should().BeEmpty();
    }

    // ---- Regra do revisor ----

    [Fact]
    public void Com_exatamente_um_responsavel_de_diretoria_ele_e_o_revisor_e_nao_pode_ser_outro()
    {
        var atividade = Nova();

        var outro = () => atividade.DefinirResponsaveis([R(_davi), R(_pedro)], _diana, T0, _diana.UsuarioId);
        outro.Should().Throw<ExcecaoDeDominio>().WithMessage(Atividade.RevisorTravado);

        atividade.DefinirResponsaveis([R(_davi), R(_pedro)], null, T0, _diana.UsuarioId);
        atividade.RevisorId.Should().Be(_davi.UsuarioId);

        var trocar = () => atividade.TrocarRevisor(_diana, [_davi, _pedro]);
        trocar.Should().Throw<ExcecaoDeDominio>().WithMessage(Atividade.RevisorTravado);
        atividade.RevisorId.Should().Be(_davi.UsuarioId);
    }

    [Fact]
    public void Com_dois_responsaveis_de_diretoria_o_revisor_e_escolhido()
    {
        var atividade = Nova();

        atividade.DefinirResponsaveis([R(_davi), R(_diana), R(_pedro)], _diana, T0, _diana.UsuarioId);
        atividade.RevisorId.Should().Be(_diana.UsuarioId);

        atividade.TrocarRevisor(_davi, [_davi, _diana, _pedro]);
        atividade.RevisorId.Should().Be(_davi.UsuarioId);
    }

    [Fact]
    public void Quando_a_reavaliacao_libera_a_escolha_o_revisor_atual_e_mantido()
    {
        var atividade = Nova();
        atividade.DefinirResponsaveis([R(_davi), R(_pedro)], null, T0, _diana.UsuarioId);

        // Davi sai dos responsáveis: nenhum diretor entre eles, a escolha fica livre e Davi continua revisor.
        atividade.DefinirResponsaveis([R(_pedro)], null, T0, _diana.UsuarioId);

        atividade.RevisorId.Should().Be(_davi.UsuarioId);
    }

    [Fact]
    public void Quando_a_reavaliacao_trava_o_revisor_passa_a_ser_o_responsavel_de_diretoria()
    {
        var atividade = Direcionada();   // revisora Diana, responsáveis Pedro e Paula

        atividade.DefinirResponsaveis([R(_pedro), R(_paula), R(_davi)], null, T0, _diana.UsuarioId);

        atividade.RevisorId.Should().Be(_davi.UsuarioId);
    }

    [Fact]
    public void Diretor_inativo_entre_os_responsaveis_nao_trava_o_revisor()
    {
        var atividade = Nova();
        atividade.DefinirResponsaveis([R(_davi), R(_pedro)], null, T0, _diana.UsuarioId);
        var daviInativo = _davi with { Ativo = false };

        atividade.TrocarRevisor(_diana, [daviInativo, _pedro]);

        atividade.RevisorId.Should().Be(_diana.UsuarioId);
    }

    [Fact]
    public void Revisor_tem_de_ser_diretor_ativo()
    {
        var atividade = Nova();

        var producao = () => atividade.DefinirResponsaveis([R(_paula)], _pedro, T0, _diana.UsuarioId);
        var inativo = () => atividade.DefinirResponsaveis([R(_paula)], _davi with { Ativo = false }, T0, _diana.UsuarioId);

        producao.Should().Throw<ExcecaoDeDominio>().WithMessage("O revisor deve ser um usuário ativo de Diretoria.");
        inativo.Should().Throw<ExcecaoDeDominio>().WithMessage("O revisor deve ser um usuário ativo de Diretoria.");
        atividade.RevisorId.Should().BeNull();
    }

    [Fact]
    public void Revisor_pode_ser_trocado_enquanto_aguarda_revisao_mas_nao_depois_de_concluida()
    {
        var feita = Feita();
        feita.TrocarRevisor(_davi, [_pedro, _paula]);
        feita.RevisorId.Should().Be(_davi.UsuarioId);

        var concluida = Concluida();
        var acao = () => concluida.TrocarRevisor(_davi, [_pedro, _paula]);

        acao.Should().Throw<ExcecaoDeDominio>().WithMessage("O revisor não pode ser trocado depois que a atividade foi concluída.");
        concluida.PodeTrocarRevisor.Should().BeFalse();
    }

    [Fact]
    public void Trocar_revisor_exige_a_lista_atual_de_responsaveis()
    {
        var atividade = Direcionada();

        var acao = () => atividade.TrocarRevisor(_davi, [_pedro]);

        acao.Should().Throw<ArgumentException>();
    }

    // ---- Concluir a parte ----

    [Fact]
    public void A_atividade_so_fica_feita_quando_o_ultimo_responsavel_conclui()
    {
        var atividade = Direcionada();

        atividade.ConcluirParte(_pedro.UsuarioId, T0.AddHours(1));
        atividade.Status.Should().Be(StatusAtividade.Direcionada);
        atividade.Responsaveis.Single(r => r.UsuarioId == _pedro.UsuarioId).ConcluidoEm.Should().Be(T0.AddHours(1));

        atividade.ConcluirParte(_paula.UsuarioId, T0.AddHours(2));
        atividade.Status.Should().Be(StatusAtividade.Feita);
        atividade.ConcluidaEm.Should().BeNull();
        var linha = atividade.Historico.Last();
        linha.StatusAnterior.Should().Be(StatusAtividade.Direcionada);
        linha.UsuarioId.Should().Be(_paula.UsuarioId, "a transição leva quem a provocou");
        linha.DataHora.Should().Be(T0.AddHours(2));
        atividade.VersaoDasPartes.Should().Be(3);
    }

    [Fact]
    public void Quem_nao_e_responsavel_ou_ja_concluiu_nao_conclui()
    {
        var atividade = Direcionada();
        atividade.ConcluirParte(_pedro.UsuarioId, T0.AddHours(1));

        var deFora = () => atividade.ConcluirParte(_ester.UsuarioId, T0.AddHours(2));
        var deNovo = () => atividade.ConcluirParte(_pedro.UsuarioId, T0.AddHours(2));

        deFora.Should().Throw<ExcecaoDeDominio>().WithMessage("Você não é responsável por esta atividade.");
        deNovo.Should().Throw<ExcecaoDeDominio>().WithMessage("Sua parte já está concluída.");
        atividade.EhResponsavel(_pedro.UsuarioId).Should().BeTrue();
        atividade.EhResponsavel(_ester.UsuarioId).Should().BeFalse();
    }

    [Theory]
    [InlineData(StatusAtividade.Criada)]
    [InlineData(StatusAtividade.Feita)]
    [InlineData(StatusAtividade.Concluida)]
    public void A_parte_so_e_concluida_em_atividade_direcionada_em_correcao_ou_reaberta(StatusAtividade status)
    {
        var atividade = status switch
        {
            StatusAtividade.Criada => Nova(),
            StatusAtividade.Feita => Feita(),
            _ => Concluida(),
        };

        var acao = () => atividade.ConcluirParte(_pedro.UsuarioId, T0.AddDays(1));

        acao.Should().Throw<ExcecaoDeDominio>()
            .WithMessage("A parte só pode ser concluída enquanto a atividade está direcionada, em correção ou reaberta.");
        atividade.Status.Should().Be(status);
    }

    [Fact]
    public void Quando_o_revisor_e_o_unico_responsavel_concluir_a_parte_conclui_a_atividade()
    {
        var atividade = Nova();
        atividade.DefinirResponsaveis([R(_diana)], null, T0.AddMinutes(1), _diana.UsuarioId);

        atividade.ConcluirParte(_diana.UsuarioId, T0.AddHours(1));

        atividade.Status.Should().Be(StatusAtividade.Concluida);
        atividade.ConcluidaEm.Should().Be(T0.AddHours(1));
        Percurso(atividade).Should().Equal(
            (null, StatusAtividade.Criada),
            (StatusAtividade.Criada, StatusAtividade.Direcionada),
            (StatusAtividade.Direcionada, StatusAtividade.Feita),
            (StatusAtividade.Feita, StatusAtividade.Concluida));
        atividade.Historico.Select(h => h.Ordem).Should().Equal(1, 2, 3, 4);
        atividade.Historico.TakeLast(2).Should().OnlyContain(h => h.DataHora == T0.AddHours(1) && h.UsuarioId == _diana.UsuarioId);
    }

    [Fact]
    public void Revisor_que_e_um_entre_varios_responsaveis_nao_conclui_a_atividade_sozinho()
    {
        var atividade = Nova();
        atividade.DefinirResponsaveis([R(_diana), R(_pedro)], null, T0, _diana.UsuarioId);

        atividade.ConcluirParte(_diana.UsuarioId, T0.AddHours(1));
        atividade.ConcluirParte(_pedro.UsuarioId, T0.AddHours(2));

        atividade.Status.Should().Be(StatusAtividade.Feita);
    }

    // ---- Aprovar e devolver ----

    [Fact]
    public void Revisor_aprova_e_a_atividade_fica_concluida()
    {
        var atividade = Feita();

        atividade.Aprovar(_diana.UsuarioId, T0.AddHours(3));

        atividade.Status.Should().Be(StatusAtividade.Concluida);
        atividade.ConcluidaEm.Should().Be(T0.AddHours(3));
        atividade.Historico.Last().Should().BeEquivalentTo(new
        {
            StatusAnterior = (StatusAtividade?)StatusAtividade.Feita, StatusNovo = StatusAtividade.Concluida,
            UsuarioId = (Guid?)_diana.UsuarioId, Motivo = (string?)null,
        });
    }

    [Fact]
    public void So_o_revisor_aprova_e_devolve()
    {
        var atividade = Feita();

        var aprovar = () => atividade.Aprovar(_davi.UsuarioId, T0.AddHours(3));
        var devolver = () => atividade.Devolver(_pedro.UsuarioId, "Faltou a procuração", [_pedro.UsuarioId], T0.AddHours(3));

        aprovar.Should().Throw<ExcecaoDeDominio>().WithMessage(Atividade.SoORevisor);
        devolver.Should().Throw<ExcecaoDeDominio>().WithMessage(Atividade.SoORevisor);
        atividade.Status.Should().Be(StatusAtividade.Feita);
    }

    [Theory]
    [InlineData(StatusAtividade.Criada)]
    [InlineData(StatusAtividade.Direcionada)]
    [InlineData(StatusAtividade.Concluida)]
    public void Aprovar_e_devolver_so_valem_para_atividade_feita_ou_corrigida(StatusAtividade status)
    {
        var atividade = status switch
        {
            StatusAtividade.Criada => Nova(),
            StatusAtividade.Direcionada => Direcionada(),
            _ => Concluida(),
        };

        var aprovar = () => atividade.Aprovar(_diana.UsuarioId, T0.AddDays(1));
        var devolver = () => atividade.Devolver(_diana.UsuarioId, "motivo", [_pedro.UsuarioId], T0.AddDays(1));

        aprovar.Should().Throw<ExcecaoDeDominio>().WithMessage("A atividade só pode ser aprovada ou devolvida quando está feita ou corrigida.");
        devolver.Should().Throw<ExcecaoDeDominio>().WithMessage("A atividade só pode ser aprovada ou devolvida quando está feita ou corrigida.");
        atividade.Status.Should().Be(status);
    }

    [Fact]
    public void Devolver_reabre_so_as_partes_selecionadas_e_grava_o_motivo()
    {
        var atividade = Feita();

        atividade.Devolver(_diana.UsuarioId, "  Faltou a procuração  ", [_paula.UsuarioId], T0.AddHours(3));

        atividade.Status.Should().Be(StatusAtividade.ParaCorrecao);
        atividade.Responsaveis.Single(r => r.UsuarioId == _paula.UsuarioId).Pendente.Should().BeTrue();
        atividade.Responsaveis.Single(r => r.UsuarioId == _pedro.UsuarioId).Pendente.Should().BeFalse();
        var linha = atividade.Historico.Last();
        linha.StatusNovo.Should().Be(StatusAtividade.ParaCorrecao);
        linha.Motivo.Should().Be("Faltou a procuração");
        linha.UsuarioId.Should().Be(_diana.UsuarioId);
    }

    [Fact]
    public void A_atividade_devolvida_fica_corrigida_quando_o_ultimo_selecionado_conclui()
    {
        var atividade = Feita();
        atividade.Devolver(_diana.UsuarioId, "Ajustar", [_pedro.UsuarioId, _paula.UsuarioId], T0.AddHours(3));

        atividade.ConcluirParte(_pedro.UsuarioId, T0.AddHours(4));
        atividade.Status.Should().Be(StatusAtividade.ParaCorrecao);

        atividade.ConcluirParte(_paula.UsuarioId, T0.AddHours(5));
        atividade.Status.Should().Be(StatusAtividade.Corrigida);

        atividade.Devolver(_diana.UsuarioId, "Ainda falta", [_paula.UsuarioId], T0.AddHours(6));
        atividade.Status.Should().Be(StatusAtividade.ParaCorrecao);
        atividade.ConcluirParte(_paula.UsuarioId, T0.AddHours(7));
        atividade.Aprovar(_diana.UsuarioId, T0.AddHours(8));

        atividade.Status.Should().Be(StatusAtividade.Concluida);
        Percurso(atividade).Select(p => p.Para).Should().Equal(
            StatusAtividade.Criada, StatusAtividade.Direcionada, StatusAtividade.Feita, StatusAtividade.ParaCorrecao,
            StatusAtividade.Corrigida, StatusAtividade.ParaCorrecao, StatusAtividade.Corrigida, StatusAtividade.Concluida);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Devolver_e_reabrir_exigem_motivo(string motivo)
    {
        var feita = Feita();
        var concluida = Concluida();

        var devolver = () => feita.Devolver(_diana.UsuarioId, motivo, [_pedro.UsuarioId], T0.AddHours(3));
        var reabrir = () => concluida.Reabrir(motivo, [_pedro.UsuarioId], T0.AddHours(4), _diana.UsuarioId);

        devolver.Should().Throw<ExcecaoDeDominio>().WithMessage("Informe o motivo.");
        reabrir.Should().Throw<ExcecaoDeDominio>().WithMessage("Informe o motivo.");
        feita.Status.Should().Be(StatusAtividade.Feita);
        concluida.Status.Should().Be(StatusAtividade.Concluida);
    }

    [Fact]
    public void Motivo_tem_limite()
    {
        var feita = Feita();

        var acao = () => feita.Devolver(_diana.UsuarioId, new string('a', 2001), [_pedro.UsuarioId], T0.AddHours(3));

        acao.Should().Throw<ExcecaoDeDominio>().WithMessage("O motivo deve ter até 2000 caracteres.");
    }

    [Fact]
    public void Devolver_e_reabrir_exigem_ao_menos_um_responsavel_da_atividade()
    {
        var feita = Feita();

        var nenhum = () => feita.Devolver(_diana.UsuarioId, "Ajustar", [], T0.AddHours(3));
        var deFora = () => feita.Devolver(_diana.UsuarioId, "Ajustar", [_pedro.UsuarioId, _ester.UsuarioId], T0.AddHours(3));

        nenhum.Should().Throw<ExcecaoDeDominio>().WithMessage("Selecione ao menos um responsável.");
        deFora.Should().Throw<ExcecaoDeDominio>().WithMessage("Responsável selecionado não faz parte da atividade.");
        feita.Status.Should().Be(StatusAtividade.Feita);
        feita.Responsaveis.Should().OnlyContain(r => !r.Pendente, "nada muda quando a devolução é recusada");
    }

    // ---- Reabrir ----

    [Fact]
    public void Reabrir_limpa_a_conclusao_e_as_partes_selecionadas()
    {
        var atividade = Concluida();

        atividade.Reabrir("Cliente pediu ajuste", [_pedro.UsuarioId], T0.AddDays(2), _davi.UsuarioId);

        atividade.Status.Should().Be(StatusAtividade.Reaberta);
        atividade.ConcluidaEm.Should().BeNull();
        atividade.Responsaveis.Single(r => r.UsuarioId == _pedro.UsuarioId).Pendente.Should().BeTrue();
        atividade.Responsaveis.Single(r => r.UsuarioId == _paula.UsuarioId).Pendente.Should().BeFalse();
        var linha = atividade.Historico.Last();
        linha.StatusAnterior.Should().Be(StatusAtividade.Concluida);
        linha.Motivo.Should().Be("Cliente pediu ajuste");
        linha.UsuarioId.Should().Be(_davi.UsuarioId);
        atividade.PodeAlterarResponsaveis.Should().BeTrue();
    }

    [Fact]
    public void Atividade_reaberta_volta_a_feita_quando_o_ultimo_selecionado_conclui()
    {
        var atividade = Concluida();
        atividade.Reabrir("Ajuste", [_pedro.UsuarioId], T0.AddDays(2), _diana.UsuarioId);

        atividade.ConcluirParte(_pedro.UsuarioId, T0.AddDays(3));

        atividade.Status.Should().Be(StatusAtividade.Feita);
        atividade.Historico.Last().StatusAnterior.Should().Be(StatusAtividade.Reaberta);
    }

    [Theory]
    [InlineData(StatusAtividade.Criada)]
    [InlineData(StatusAtividade.Direcionada)]
    [InlineData(StatusAtividade.Feita)]
    public void So_atividade_concluida_e_reaberta(StatusAtividade status)
    {
        var atividade = status switch
        {
            StatusAtividade.Criada => Nova(),
            StatusAtividade.Direcionada => Direcionada(),
            _ => Feita(),
        };

        var acao = () => atividade.Reabrir("motivo", [_pedro.UsuarioId], T0.AddDays(1), _diana.UsuarioId);

        acao.Should().Throw<ExcecaoDeDominio>().WithMessage("Só uma atividade concluída pode ser reaberta.");
        atividade.Status.Should().Be(status);
    }

    // ---- Atraso ----

    [Fact]
    public void Atrasada_e_data_de_entrega_passada_em_atividade_nao_concluida()
    {
        var direcionada = Direcionada();   // entrega em 20/10/2026
        var concluida = Concluida();
        var semData = Nova(semEntrega: true);

        direcionada.EstaAtrasada(new DateOnly(2026, 10, 20)).Should().BeFalse("no dia da entrega ainda não está atrasada");
        direcionada.EstaAtrasada(new DateOnly(2026, 10, 21)).Should().BeTrue();
        concluida.EstaAtrasada(new DateOnly(2026, 12, 1)).Should().BeFalse();
        semData.EstaAtrasada(new DateOnly(2026, 12, 1)).Should().BeFalse();
    }

    // ---- Pessoa ----

    [Fact]
    public void Pessoa_e_montada_do_usuario_e_do_seu_perfil()
    {
        var diretor = Perfil.Criar("Diretor", TipoPerfil.Diretoria);
        var producao = Perfil.Criar("Produção", TipoPerfil.Producao);
        var diana = Usuario.Criar("Diana", Email.Criar("diana@x.com"), "hash", diretor);
        var pedro = Usuario.Criar("Pedro", Email.Criar("pedro@x.com"), "hash", producao);
        pedro.Inativar();

        PessoaDaAtividade.De(diana, diretor).Should().Be(new PessoaDaAtividade(diana.Id, TipoPerfil.Diretoria, true));
        PessoaDaAtividade.De(diana, diretor).EhDiretoriaAtiva.Should().BeTrue();
        PessoaDaAtividade.De(pedro, producao).Should().Be(new PessoaDaAtividade(pedro.Id, TipoPerfil.Producao, false));
        PessoaDaAtividade.De(pedro, producao).EhDiretoriaAtiva.Should().BeFalse();

        var perfilErrado = () => PessoaDaAtividade.De(pedro, diretor);
        perfilErrado.Should().Throw<ArgumentException>();
    }
}
```

- [ ] **Step 2: Rodar e ver falhar**

Run: `dotnet test tests/AtividadesJuridicas.UnitTests --filter "FullyQualifiedName~AtividadeTests"`
Expected: FAIL na compilação (`Atividade` não existe).

- [ ] **Step 3: Tipos de apoio e entidades filhas**

`src/AtividadesJuridicas.Domain/Atividades/TiposDaAtividade.cs`:

```csharp
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Comum;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.Domain.Atividades;

public enum StatusAtividade
{
    Criada = 1,
    Direcionada = 2,
    Feita = 3,
    ParaCorrecao = 4,
    Corrigida = 5,
    Concluida = 6,
    Reaberta = 7,
}

public enum TipoAutor
{
    Usuario = 1,
    Sistema = 2,
}

/// <summary>Um usuário como a atividade precisa conhecê-lo agora: quem é, se está ativo e de que tipo é o seu perfil.</summary>
public sealed record PessoaDaAtividade(Guid UsuarioId, TipoPerfil Tipo, bool Ativo)
{
    public bool EhDiretoriaAtiva => Ativo && Tipo == TipoPerfil.Diretoria;

    public static PessoaDaAtividade De(Usuario usuario, Perfil perfil)
    {
        ArgumentNullException.ThrowIfNull(usuario);
        ArgumentNullException.ThrowIfNull(perfil);
        if (usuario.PerfilId != perfil.Id)
            throw new ArgumentException("O perfil informado não é o do usuário.", nameof(perfil));

        return new PessoaDaAtividade(usuario.Id, perfil.Tipo, usuario.Ativo);
    }
}

public sealed record ResponsavelInformado(PessoaDaAtividade Pessoa, string? Direcionamento);

public sealed record DadosDaAtividade(string Titulo, string Descricao, DateOnly? DataEntrega);

/// <summary>Cliente, processo e área de uma atividade, já coerentes entre si.</summary>
public sealed record VinculoDaAtividade
{
    private VinculoDaAtividade(Guid? clienteId, Guid? processoId, Guid? areaAtuacaoId)
    {
        ClienteId = clienteId;
        ProcessoId = processoId;
        AreaAtuacaoId = areaAtuacaoId;
    }

    public Guid? ClienteId { get; }
    public Guid? ProcessoId { get; }
    public Guid? AreaAtuacaoId { get; }

    /// <summary>Sem cliente, sem processo e sem área.</summary>
    public static VinculoDaAtividade Avulsa { get; } = new(null, null, null);

    /// <summary>
    /// Com processo, o cliente é obrigatório, tem de ser um dos clientes do processo, e a área é a do
    /// processo (a informada é ignorada). Sem processo, cliente e área são opcionais.
    /// </summary>
    public static VinculoDaAtividade Criar(Guid? clienteId, Processo? processo, Guid? areaAtuacaoId)
    {
        if (processo is null) return new VinculoDaAtividade(clienteId, null, areaAtuacaoId);

        if (clienteId is null)
            throw new ExcecaoDeDominio("Com processo, o cliente da atividade é obrigatório.");
        if (processo.Clientes.All(c => c.ClienteId != clienteId))
            throw new ExcecaoDeDominio("O cliente da atividade deve ser um dos clientes do processo.");

        return new VinculoDaAtividade(clienteId, processo.Id, processo.AreaAtuacaoId);
    }
}

/// <summary>O que mudou nos responsáveis de uma atividade, pelos Ids dos usuários.</summary>
public sealed record AlteracaoDeResponsaveis(
    IReadOnlyList<Guid> Adicionados, IReadOnlyList<Guid> Removidos, IReadOnlyList<Guid> DirecionamentoAlterado)
{
    public bool Houve => Adicionados.Count > 0 || Removidos.Count > 0 || DirecionamentoAlterado.Count > 0;
}
```

`src/AtividadesJuridicas.Domain/Atividades/AtividadeResponsavel.cs`:

```csharp
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Domain.Atividades;

/// <summary>Um responsável da atividade, com o direcionamento que só ele (e a Diretoria) vê e a conclusão da sua parte.</summary>
public sealed class AtividadeResponsavel : Entidade
{
    public const int TamanhoMaximoDirecionamento = 4000;

    private AtividadeResponsavel() { }

    internal AtividadeResponsavel(Guid atividadeId, Guid usuarioId, string? direcionamento)
    {
        AtividadeId = atividadeId;
        UsuarioId = usuarioId;
        Direcionamento = direcionamento;
    }

    public Guid AtividadeId { get; private set; }
    public Guid UsuarioId { get; private set; }
    public string? Direcionamento { get; private set; }

    /// <summary>Nulo enquanto a parte está pendente.</summary>
    public DateTime? ConcluidoEm { get; private set; }

    public bool Pendente => ConcluidoEm is null;

    internal void AlterarDirecionamento(string? direcionamento) => Direcionamento = direcionamento;

    internal void Concluir(DateTime agoraUtc) => ConcluidoEm = agoraUtc;

    internal void Reabrir() => ConcluidoEm = null;
}
```

`src/AtividadesJuridicas.Domain/Atividades/AtividadeHistoricoStatus.cs`:

```csharp
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Domain.Atividades;

/// <summary>
/// Uma transição de status. É dado de negócio, exibido na atividade; nunca é alterado depois de criado.
/// <see cref="UsuarioId"/> nulo indica transição feita pelo sistema.
/// </summary>
public sealed class AtividadeHistoricoStatus : Entidade
{
    public const int TamanhoMaximoMotivo = 2000;

    private AtividadeHistoricoStatus() { }

    internal AtividadeHistoricoStatus(
        Guid atividadeId, int ordem, StatusAtividade? statusAnterior, StatusAtividade statusNovo, Guid? usuarioId,
        DateTime dataHoraUtc, string? motivo)
    {
        AtividadeId = atividadeId;
        Ordem = ordem;
        StatusAnterior = statusAnterior;
        StatusNovo = statusNovo;
        UsuarioId = usuarioId;
        DataHora = dataHoraUtc;
        Motivo = motivo;
    }

    public Guid AtividadeId { get; private set; }

    /// <summary>Posição da transição dentro da atividade: duas transições do mesmo instante continuam ordenadas.</summary>
    public int Ordem { get; private set; }

    public StatusAtividade? StatusAnterior { get; private set; }
    public StatusAtividade StatusNovo { get; private set; }
    public Guid? UsuarioId { get; private set; }
    public DateTime DataHora { get; private set; }
    public string? Motivo { get; private set; }
}
```

- [ ] **Step 4: O agregado**

`src/AtividadesJuridicas.Domain/Atividades/Atividade.cs`:

```csharp
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Domain.Atividades;

public sealed class Atividade : Entidade
{
    public const int TamanhoMaximoTitulo = 200;
    public const int TamanhoMaximoDescricao = 10000;

    public const string SoORevisor = "Somente o revisor da atividade pode aprová-la ou devolvê-la.";
    public const string RevisorTravado =
        "O revisor desta atividade é o seu único responsável de Diretoria e não pode ser outro.";

    private const string SoAlteraResponsaveis =
        "Os responsáveis só podem ser alterados enquanto a atividade está criada, direcionada, em correção ou reaberta.";
    private const string SoAguardandoRevisao =
        "A atividade só pode ser aprovada ou devolvida quando está feita ou corrigida.";

    private readonly List<AtividadeResponsavel> _responsaveis = [];
    private readonly List<AtividadeHistoricoStatus> _historico = [];

    private Atividade() { }

    public string Titulo { get; private set; } = null!;
    public string Descricao { get; private set; } = null!;

    /// <summary>Obrigatória a partir de Direcionada.</summary>
    public DateOnly? DataEntrega { get; private set; }

    public StatusAtividade Status { get; private set; }

    /// <summary>Preenchido ao entrar em Concluida; limpo ao reabrir.</summary>
    public DateTime? ConcluidaEm { get; private set; }

    public Guid? ClienteId { get; private set; }
    public Guid? ProcessoId { get; private set; }
    public Guid? AreaAtuacaoId { get; private set; }
    public TipoAutor TipoAutor { get; private set; }
    public Guid? AutorId { get; private set; }
    public Guid? RevisorId { get; private set; }

    /// <summary>Versão da linha, mantida pelo banco (controle de concorrência otimista).</summary>
    [NaoAuditar]
    public uint Versao { get; private set; }

    /// <summary>
    /// Aumenta a cada mudança nos responsáveis ou nas partes. Faz essa mudança gravar também a linha da
    /// atividade, acionando a verificação de concorrência: duas conclusões simultâneas não passam as duas.
    /// </summary>
    [NaoAuditar]
    public int VersaoDasPartes { get; private set; }

    public IReadOnlyCollection<AtividadeResponsavel> Responsaveis => _responsaveis;
    public IReadOnlyCollection<AtividadeHistoricoStatus> Historico => _historico;

    public bool PodeAlterarResponsaveis => Status is
        StatusAtividade.Criada or StatusAtividade.Direcionada or StatusAtividade.ParaCorrecao or StatusAtividade.Reaberta;

    public bool PodeTrocarRevisor => Status != StatusAtividade.Concluida;

    private bool AguardaPartes => Status is
        StatusAtividade.Direcionada or StatusAtividade.ParaCorrecao or StatusAtividade.Reaberta;

    private bool AguardaRevisao => Status is StatusAtividade.Feita or StatusAtividade.Corrigida;

    public static Atividade Criar(
        DadosDaAtividade dados, VinculoDaAtividade vinculo, TipoAutor tipoAutor, Guid? autorId, DateTime agoraUtc)
    {
        Validar(dados, StatusAtividade.Criada);
        ArgumentNullException.ThrowIfNull(vinculo);
        if (!Enum.IsDefined(tipoAutor))
            throw new ExcecaoDeDominio("Tipo de autor inválido.");
        if (tipoAutor == TipoAutor.Usuario && (autorId is null || autorId == Guid.Empty))
            throw new ExcecaoDeDominio("O autor da atividade é obrigatório.");
        if (tipoAutor == TipoAutor.Sistema && autorId is not null)
            throw new ExcecaoDeDominio("Atividade criada pelo sistema não tem autor.");

        var atividade = new Atividade { TipoAutor = tipoAutor, AutorId = autorId, Status = StatusAtividade.Criada };
        atividade.Aplicar(dados, vinculo);
        atividade._historico.Add(new AtividadeHistoricoStatus(
            atividade.Id, 1, null, StatusAtividade.Criada, autorId, agoraUtc, null));
        return atividade;
    }

    public void Alterar(DadosDaAtividade dados, VinculoDaAtividade vinculo)
    {
        Validar(dados, Status);
        ArgumentNullException.ThrowIfNull(vinculo);
        Aplicar(dados, vinculo);
    }

    /// <summary>
    /// Deixa a atividade com exatamente os responsáveis informados e reavalia o revisor. A entrada de quem
    /// permanece é preservada (com a conclusão da sua parte). Em atividade criada, o primeiro responsável a
    /// direciona; nos demais status, se todos os que ficaram já concluíram, a atividade avança.
    /// </summary>
    public AlteracaoDeResponsaveis DefinirResponsaveis(
        IReadOnlyCollection<ResponsavelInformado> responsaveis, PessoaDaAtividade? revisor, DateTime agoraUtc, Guid? atorId)
    {
        ArgumentNullException.ThrowIfNull(responsaveis);
        if (responsaveis.Any(r => r is null || r.Pessoa is null))
            throw new ArgumentException("A lista de responsáveis não pode conter elemento nulo.", nameof(responsaveis));

        // Tudo é verificado antes de qualquer mudança, para não deixar a atividade pela metade.
        if (!PodeAlterarResponsaveis)
            throw new ExcecaoDeDominio(SoAlteraResponsaveis);
        if (responsaveis.Select(r => r.Pessoa.UsuarioId).Distinct().Count() != responsaveis.Count)
            throw new ExcecaoDeDominio("Um usuário não pode se repetir entre os responsáveis.");
        if (responsaveis.Any(r => Texto(r.Direcionamento) is { Length: > AtividadeResponsavel.TamanhoMaximoDirecionamento }))
            throw new ExcecaoDeDominio(
                $"O direcionamento deve ter até {AtividadeResponsavel.TamanhoMaximoDirecionamento} caracteres.");

        var novos = responsaveis.Where(r => _responsaveis.All(e => e.UsuarioId != r.Pessoa.UsuarioId)).ToList();
        if (novos.Any(r => !r.Pessoa.Ativo))
            throw new ExcecaoDeDominio("Usuário inativo não pode ser incluído como responsável.");
        if (Status != StatusAtividade.Criada && responsaveis.Count == 0)
            throw new ExcecaoDeDominio("A atividade precisa de ao menos um responsável.");

        var revisorId = ResolverRevisor([.. responsaveis.Select(r => r.Pessoa)], revisor);
        if (responsaveis.Count > 0)
        {
            if (revisorId is null)
                throw new ExcecaoDeDominio("Informe o revisor da atividade.");
            if (DataEntrega is null)
                throw new ExcecaoDeDominio("Informe a data de entrega antes de direcionar a atividade.");
        }

        var ids = responsaveis.Select(r => r.Pessoa.UsuarioId).ToHashSet();
        var removidos = _responsaveis.Where(e => !ids.Contains(e.UsuarioId)).ToList();
        var direcionamentoAlterado = new List<Guid>();

        foreach (var entrada in removidos) _responsaveis.Remove(entrada);
        foreach (var informado in responsaveis)
        {
            var direcionamento = Texto(informado.Direcionamento);
            var existente = _responsaveis.FirstOrDefault(e => e.UsuarioId == informado.Pessoa.UsuarioId);
            if (existente is null)
            {
                _responsaveis.Add(new AtividadeResponsavel(Id, informado.Pessoa.UsuarioId, direcionamento));
            }
            else if (existente.Direcionamento != direcionamento)
            {
                existente.AlterarDirecionamento(direcionamento);
                direcionamentoAlterado.Add(existente.UsuarioId);
            }
        }

        RevisorId = revisorId;

        var alteracao = new AlteracaoDeResponsaveis(
            [.. novos.Select(r => r.Pessoa.UsuarioId)], [.. removidos.Select(e => e.UsuarioId)], direcionamentoAlterado);
        if (alteracao.Houve) VersaoDasPartes++;

        if (Status == StatusAtividade.Criada && _responsaveis.Count > 0)
            Transicionar(StatusAtividade.Direcionada, atorId, agoraUtc, null);
        else
            AvancarSeTodosConcluiram(atorId, agoraUtc);

        return alteracao;
    }

    /// <summary><paramref name="responsaveisAtuais"/>: os responsáveis de agora, para reavaliar o travamento.</summary>
    public void TrocarRevisor(PessoaDaAtividade revisor, IReadOnlyCollection<PessoaDaAtividade> responsaveisAtuais)
    {
        ArgumentNullException.ThrowIfNull(revisor);
        ArgumentNullException.ThrowIfNull(responsaveisAtuais);
        if (!responsaveisAtuais.Select(p => p.UsuarioId).ToHashSet().SetEquals(_responsaveis.Select(e => e.UsuarioId)))
            throw new ArgumentException("A lista informada não é a dos responsáveis atuais.", nameof(responsaveisAtuais));
        if (!PodeTrocarRevisor)
            throw new ExcecaoDeDominio("O revisor não pode ser trocado depois que a atividade foi concluída.");

        RevisorId = ResolverRevisor(responsaveisAtuais, revisor);
    }

    public void ConcluirParte(Guid usuarioId, DateTime agoraUtc)
    {
        if (!AguardaPartes)
            throw new ExcecaoDeDominio(
                "A parte só pode ser concluída enquanto a atividade está direcionada, em correção ou reaberta.");

        var parte = _responsaveis.FirstOrDefault(e => e.UsuarioId == usuarioId)
            ?? throw new ExcecaoDeDominio("Você não é responsável por esta atividade.");
        if (!parte.Pendente)
            throw new ExcecaoDeDominio("Sua parte já está concluída.");

        parte.Concluir(agoraUtc);
        VersaoDasPartes++;
        AvancarSeTodosConcluiram(usuarioId, agoraUtc);
    }

    public void Aprovar(Guid revisorId, DateTime agoraUtc)
    {
        ExigirRevisao(revisorId);
        Transicionar(StatusAtividade.Concluida, revisorId, agoraUtc, null);
    }

    /// <summary>Devolve para correção: só as partes dos responsáveis selecionados voltam a ficar pendentes.</summary>
    public void Devolver(Guid revisorId, string motivo, IReadOnlyCollection<Guid> responsaveis, DateTime agoraUtc)
    {
        ExigirRevisao(revisorId);
        var motivoValido = MotivoValido(motivo);
        ReabrirPartes(responsaveis);
        Transicionar(StatusAtividade.ParaCorrecao, revisorId, agoraUtc, motivoValido);
    }

    public void Reabrir(string motivo, IReadOnlyCollection<Guid> responsaveis, DateTime agoraUtc, Guid? atorId)
    {
        if (Status != StatusAtividade.Concluida)
            throw new ExcecaoDeDominio("Só uma atividade concluída pode ser reaberta.");

        var motivoValido = MotivoValido(motivo);
        ReabrirPartes(responsaveis);
        Transicionar(StatusAtividade.Reaberta, atorId, agoraUtc, motivoValido);
    }

    /// <summary>"Atrasada" não é status: é a data de entrega anterior a hoje em atividade não concluída.</summary>
    public bool EstaAtrasada(DateOnly hoje) => Status != StatusAtividade.Concluida && DataEntrega < hoje;

    public bool EhResponsavel(Guid usuarioId) => _responsaveis.Any(e => e.UsuarioId == usuarioId);

    // Exatamente um responsável ativo de Diretoria: ele é o revisor e não pode ser outro. Caso contrário,
    // vale o informado (que precisa ser diretor ativo) ou, sem informado, o revisor atual é mantido.
    private Guid? ResolverRevisor(IReadOnlyCollection<PessoaDaAtividade> responsaveis, PessoaDaAtividade? informado)
    {
        var diretores = responsaveis.Where(p => p.EhDiretoriaAtiva).ToList();
        if (diretores.Count == 1)
        {
            var travado = diretores[0].UsuarioId;
            if (informado is not null && informado.UsuarioId != travado)
                throw new ExcecaoDeDominio(RevisorTravado);
            return travado;
        }

        if (informado is null || informado.UsuarioId == RevisorId) return RevisorId;
        if (!informado.EhDiretoriaAtiva)
            throw new ExcecaoDeDominio("O revisor deve ser um usuário ativo de Diretoria.");
        return informado.UsuarioId;
    }

    private void ExigirRevisao(Guid revisorId)
    {
        if (!AguardaRevisao)
            throw new ExcecaoDeDominio(SoAguardandoRevisao);
        if (RevisorId != revisorId)
            throw new ExcecaoDeDominio(SoORevisor);
    }

    // Valida tudo antes de reabrir a primeira parte.
    private void ReabrirPartes(IReadOnlyCollection<Guid> responsaveis)
    {
        ArgumentNullException.ThrowIfNull(responsaveis);
        var selecionados = responsaveis.Distinct().ToList();
        if (selecionados.Count == 0)
            throw new ExcecaoDeDominio("Selecione ao menos um responsável.");
        if (selecionados.Any(id => _responsaveis.All(e => e.UsuarioId != id)))
            throw new ExcecaoDeDominio("Responsável selecionado não faz parte da atividade.");

        foreach (var parte in _responsaveis.Where(e => selecionados.Contains(e.UsuarioId))) parte.Reabrir();
        VersaoDasPartes++;
    }

    private void AvancarSeTodosConcluiram(Guid? usuarioId, DateTime agoraUtc)
    {
        if (!AguardaPartes || _responsaveis.Count == 0 || _responsaveis.Any(e => e.Pendente)) return;

        Transicionar(
            Status == StatusAtividade.ParaCorrecao ? StatusAtividade.Corrigida : StatusAtividade.Feita,
            usuarioId, agoraUtc, null);

        // O revisor é o único responsável: não há a quem submeter, a atividade se conclui.
        if (_responsaveis.Count == 1 && _responsaveis[0].UsuarioId == RevisorId)
            Transicionar(StatusAtividade.Concluida, usuarioId, agoraUtc, null);
    }

    private void Transicionar(StatusAtividade novo, Guid? usuarioId, DateTime agoraUtc, string? motivo)
    {
        _historico.Add(new AtividadeHistoricoStatus(Id, _historico.Count + 1, Status, novo, usuarioId, agoraUtc, motivo));
        Status = novo;
        ConcluidaEm = novo == StatusAtividade.Concluida ? agoraUtc : null;
    }

    private void Aplicar(DadosDaAtividade dados, VinculoDaAtividade vinculo)
    {
        Titulo = dados.Titulo.Trim();
        Descricao = dados.Descricao.Trim();
        DataEntrega = dados.DataEntrega;
        ClienteId = vinculo.ClienteId;
        ProcessoId = vinculo.ProcessoId;
        AreaAtuacaoId = vinculo.AreaAtuacaoId;
    }

    private static void Validar(DadosDaAtividade dados, StatusAtividade status)
    {
        ArgumentNullException.ThrowIfNull(dados);
        if (string.IsNullOrWhiteSpace(dados.Titulo))
            throw new ExcecaoDeDominio("O título da atividade é obrigatório.");
        if (dados.Titulo.Trim().Length > TamanhoMaximoTitulo)
            throw new ExcecaoDeDominio($"O título da atividade deve ter até {TamanhoMaximoTitulo} caracteres.");
        if (string.IsNullOrWhiteSpace(dados.Descricao))
            throw new ExcecaoDeDominio("A descrição da atividade é obrigatória.");
        if (dados.Descricao.Trim().Length > TamanhoMaximoDescricao)
            throw new ExcecaoDeDominio($"A descrição da atividade deve ter até {TamanhoMaximoDescricao} caracteres.");
        if (status != StatusAtividade.Criada && dados.DataEntrega is null)
            throw new ExcecaoDeDominio("A data de entrega é obrigatória depois que a atividade é direcionada.");
    }

    private static string MotivoValido(string? motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo))
            throw new ExcecaoDeDominio("Informe o motivo.");
        if (motivo.Trim().Length > AtividadeHistoricoStatus.TamanhoMaximoMotivo)
            throw new ExcecaoDeDominio($"O motivo deve ter até {AtividadeHistoricoStatus.TamanhoMaximoMotivo} caracteres.");
        return motivo.Trim();
    }

    private static string? Texto(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
```

Atenção a dois pontos que os testes fixam: `Transicionar` limpa `ConcluidaEm` em toda transição que não seja para `Concluida` (só `Reaberta` sai de `Concluida`, então é o mesmo que "limpo ao reabrir"); e duas transições da mesma chamada (`Feita` e `Concluida`, quando o revisor é o único responsável) têm a mesma `DataHora` — é `Ordem` que as mantém em sequência. `Ordem` conta as linhas do histórico carregado: o histórico de uma atividade é sempre carregado inteiro junto com ela.

- [ ] **Step 5: Rodar e ver passar**

Run: `dotnet test tests/AtividadesJuridicas.UnitTests` e `dotnet build` na raiz
Expected: PASS, todos (397 anteriores + os novos), sem avisos. `ArquiteturaTests` continua verde.

- [ ] **Step 6: Commit**

```bash
git add src/AtividadesJuridicas.Domain/Atividades tests/AtividadesJuridicas.UnitTests/Domain/AtividadeTests.cs
git commit -m "feat(atividades): Atividade, responsáveis, histórico e fluxo de status no domínio"
```

---

### Task 2: Casos de uso, persistência e auditoria das atividades

Uma tarefa só: a aplicação Web valida a injeção de dependências ao subir, então os manipuladores e as implementações das portas precisam entrar juntos.

**Files:**
- Create: `src/AtividadesJuridicas.Application/Atividades/PortasDeAtividades.cs`
- Create: `src/AtividadesJuridicas.Application/Atividades/EventosDeAtividade.cs`
- Create: `src/AtividadesJuridicas.Application/Atividades/PessoasDaAtividade.cs`
- Create: `src/AtividadesJuridicas.Application/Atividades/SalvarAtividade.cs`
- Create: `src/AtividadesJuridicas.Application/Atividades/DefinirResponsaveisDaAtividade.cs`
- Create: `src/AtividadesJuridicas.Application/Atividades/TrocarRevisorDaAtividade.cs`
- Create: `src/AtividadesJuridicas.Application/Atividades/ConcluirMinhaParte.cs`
- Create: `src/AtividadesJuridicas.Application/Atividades/AprovarAtividade.cs`
- Create: `src/AtividadesJuridicas.Application/Atividades/DevolverAtividade.cs`
- Create: `src/AtividadesJuridicas.Application/Atividades/ReabrirAtividade.cs`
- Create: `src/AtividadesJuridicas.Application/Atividades/ExcluirAtividade.cs`
- Create: `src/AtividadesJuridicas.Application/Atividades/ListarAtividades.cs`
- Create: `src/AtividadesJuridicas.Application/Atividades/ObterAtividade.cs`
- Modify: `src/AtividadesJuridicas.Application/Comum/Portas.cs` (`ICalendarioDoEscritorio`)
- Create: `src/AtividadesJuridicas.Infrastructure/Comum/CalendarioDoEscritorio.cs`
- Create: `src/AtividadesJuridicas.Infrastructure/Persistencia/Configuracoes/AtividadeConfiguracao.cs`
- Create: `src/AtividadesJuridicas.Infrastructure/Persistencia/Repositorios/AtividadeRepositorio.cs`
- Modify: `src/AtividadesJuridicas.Infrastructure/Persistencia/AppDbContext.cs`, `DependencyInjection.cs`
- Modify: `src/AtividadesJuridicas.Web/Program.cs` (passa o fuso do escritório)
- Create: migration `Atividades`
- Modify: `tests/AtividadesJuridicas.UnitTests/Application/Falsos.cs`
- Modify: `tests/AtividadesJuridicas.IntegrationTests/PropriedadesAuditadasTests.cs`
- Test: `tests/AtividadesJuridicas.UnitTests/Application/AtividadesTests.cs`
- Test: `tests/AtividadesJuridicas.IntegrationTests/AtividadesTests.cs`

**Interfaces:**
- Consumes: tudo o que a Tarefa 1 produz; `IContextoDeAcesso`; `IUsuarioRepositorio.ObterVariosAsync`, `IPerfilRepositorio.ObterVariosAsync`; `IClienteRepositorio.ObterPorIdAsync`; `IProcessoRepositorio.ObterPorIdAsync`; `IAreaAtuacaoRepositorio.ObterPorIdAsync`; `IRelogio`; `IUnidadeDeTrabalho`; `IAuditoria`.
- Produces:
  - `interface ICalendarioDoEscritorio { DateOnly Hoje { get; } }` (em `Application/Comum/Portas.cs`)
  - `AddInfrastructure(..., bool criarPastaDeArquivos, string fusoDoEscritorio = "America/Sao_Paulo")`
  - namespace `AtividadesJuridicas.Application.Atividades`:
    - `static class AtividadesOpcoes { const int TamanhoDaPagina = 20; }`
    - `static class EventosDeAtividade { const string ResponsaveisAlterados = "atividade_responsaveis_alterados"; }`
    - `record FiltroDeAtividades(StatusAtividade? Status, Guid? ResponsavelId, Guid? ClienteId, Guid? ProcessoId, bool SomenteAtrasadas, string? Busca, DateOnly Hoje)`
    - `record AtividadeResumo(Guid Id, string Titulo, StatusAtividade Status, DateOnly? DataEntrega, bool Atrasada, string? ClienteNome, string? ProcessoTitulo, string? AreaNome, IReadOnlyList<string> Responsaveis, string? RevisorNome)`
    - `record ResponsavelDetalhe(Guid UsuarioId, string Nome, bool DirecionamentoVisivel, string? Direcionamento, DateTime? ConcluidoEm)`
    - `record HistoricoDetalhe(StatusAtividade? StatusAnterior, StatusAtividade StatusNovo, string? UsuarioNome, DateTime DataHora, string? Motivo)`
    - `record AcoesDaAtividade(bool Editar, bool DefinirResponsaveis, bool TrocarRevisor, bool ConcluirMinhaParte, bool AprovarOuDevolver, bool Reabrir, bool Excluir)`
    - `record AtividadeDetalhe(Guid Id, string Titulo, string Descricao, DateOnly? DataEntrega, StatusAtividade Status, bool Atrasada, DateTime? ConcluidaEm, Guid? ClienteId, string? ClienteNome, Guid? ProcessoId, string? ProcessoTitulo, Guid? AreaAtuacaoId, string? AreaNome, TipoAutor TipoAutor, string? AutorNome, Guid? RevisorId, string? RevisorNome, IReadOnlyList<ResponsavelDetalhe> Responsaveis, IReadOnlyList<HistoricoDetalhe> Historico, AcoesDaAtividade Acoes)`
    - `interface IAtividadeRepositorio { Task<Atividade?> ObterPorIdAsync(Guid id, CancellationToken ct); Task<Pagina<AtividadeResumo>> ListarAsync(FiltroDeAtividades filtro, int pagina, int tamanho, CancellationToken ct); void Adicionar(Atividade atividade); void Remover(Atividade atividade); }`
    - `record ResponsavelEntrada(Guid UsuarioId, string? Direcionamento)`
    - `record SalvarAtividade(Guid? Id, string Titulo, string Descricao, DateOnly? DataEntrega, Guid? ClienteId, Guid? ProcessoId, Guid? AreaAtuacaoId) : IRequisicao<Resultado<Guid>>`
    - `record DefinirResponsaveisDaAtividade(Guid Id, IReadOnlyList<ResponsavelEntrada> Responsaveis, Guid? RevisorId) : IRequisicao<Resultado>`
    - `record TrocarRevisorDaAtividade(Guid Id, Guid RevisorId) : IRequisicao<Resultado>`
    - `record ConcluirMinhaParte(Guid Id) : IRequisicao<Resultado>`
    - `record AprovarAtividade(Guid Id) : IRequisicao<Resultado>`
    - `record DevolverAtividade(Guid Id, string Motivo, IReadOnlyList<Guid> Responsaveis) : IRequisicao<Resultado>`
    - `record ReabrirAtividade(Guid Id, string Motivo, IReadOnlyList<Guid> Responsaveis) : IRequisicao<Resultado>`
    - `record ExcluirAtividade(Guid Id) : IRequisicao<Resultado>`
    - `record ListarAtividades(StatusAtividade? Status = null, Guid? ResponsavelId = null, Guid? ClienteId = null, Guid? ProcessoId = null, bool SomenteAtrasadas = false, string? Busca = null, int Pagina = 1) : IRequisicao<Resultado<Pagina<AtividadeResumo>>>, IConsultaAuditavel`
    - `record ObterAtividade(Guid Id) : IRequisicao<Resultado<AtividadeDetalhe>>, IConsultaAuditavel`

#### Regras dos casos de uso (valem para o código e para os testes)

Mensagens comuns: `"Acesso negado."`, `"Atividade não encontrada."`, e, para conflito de concorrência, `"A atividade foi alterada por outra pessoa. Recarregue e tente novamente."`.

1. **Quem vê.** A Diretoria vê todas; um usuário de Produção vê só as atividades em que é responsável. Para quem não pode ver, toda requisição que recebe o Id de uma atividade (`ObterAtividade`, `ConcluirMinhaParte`) responde `NaoEncontrado` com `"Atividade não encontrada."`, e grava imediatamente o evento `acesso_negado` com a entidade `"Atividade"`, o Id e `{ requisicao = <nome>, resposta = "nao_encontrado" }` (com `CancellationToken.None`). Atividade inexistente responde o mesmo `NaoEncontrado`, sem evento.
2. **Comandos da Diretoria** (`SalvarAtividade`, `DefinirResponsaveisDaAtividade`, `TrocarRevisorDaAtividade`, `ReabrirAtividade`, `ExcluirAtividade`): quem não é da Diretoria recebe `AcessoNegado` antes de qualquer leitura.
3. **`AprovarAtividade` e `DevolverAtividade`**: exigem que o ator seja da Diretoria **e** seja o revisor. Quem não é da Diretoria recebe `AcessoNegado`; um diretor que não é o revisor recebe `Validacao` com `Atividade.SoORevisor` (a recusa vem do domínio).
4. **`ConcluirMinhaParte`**: qualquer usuário autenticado; aplica a regra 1 e depois o domínio (`Validacao` com a mensagem do domínio quando a parte já está concluída ou o status não permite).
5. **`SalvarAtividade`**: cria (autor = o diretor que cria, `TipoAutor.Usuario`) ou altera. Antes de chamar o domínio: cliente informado tem de existir (`Validacao` "Cliente não encontrado.") e, se for vínculo novo (diferente do que a atividade já tinha), estar ativo ("O cliente está inativo."); processo informado tem de existir ("Processo não encontrado."); área informada sem processo tem de existir ("Área de atuação não encontrada.") e, se for nova na atividade, estar ativa ("A área de atuação está inativa."). O vínculo é montado com `VinculoDaAtividade.Criar(clienteId, processo, areaAtuacaoId)`.
6. **`DefinirResponsaveisDaAtividade` e `TrocarRevisorDaAtividade`**: carregam os usuários informados (e os responsáveis atuais, no caso da troca) com os seus perfis e montam `PessoaDaAtividade.De`; usuário inexistente → `Validacao` "Usuário não encontrado.". Quando os responsáveis mudam, anotam o evento `atividade_responsaveis_alterados` na atividade com `{ adicionados, removidos, direcionamentoAlterado }` (mesma transação).
7. **Erros do domínio** (`ExcecaoDeDominio`) viram `Validacao` com a mensagem; `ExcecaoDeConcorrencia` vira `Conflito` com a mensagem de conflito.
8. **`ListarAtividades`**: para quem não é da Diretoria, o filtro de responsável é sempre o próprio usuário, qualquer que seja o valor pedido. `Hoje` vem de `ICalendarioDoEscritorio`. Ordenação: data de entrega crescente (sem data por último), depois as mais recentes primeiro. A busca procura no título, sem diferenciar maiúsculas nem acentos, tratando `%` e `_` como texto.
9. **`ObterAtividade`**: o direcionamento de um responsável só é devolvido (`DirecionamentoVisivel = true`) para a Diretoria e para o próprio responsável; para os demais, `DirecionamentoVisivel = false` e `Direcionamento = null`. Nomes (cliente, processo, área, autor, revisor, responsáveis, usuários do histórico) vêm dos respectivos repositórios; um registro que não seja mais encontrado aparece como `null`. O histórico vem em ordem cronológica.
10. **`AcoesDaAtividade`** diz o que o ator pode fazer agora: `Editar`, `Excluir` = Diretoria; `DefinirResponsaveis` = Diretoria e `atividade.PodeAlterarResponsaveis`; `TrocarRevisor` = Diretoria e `atividade.PodeTrocarRevisor`; `ConcluirMinhaParte` = o ator é responsável com parte pendente e o status é `Direcionada`, `ParaCorrecao` ou `Reaberta`; `AprovarOuDevolver` = o ator é da Diretoria, é o revisor e o status é `Feita` ou `Corrigida`; `Reabrir` = Diretoria e status `Concluida`.

- [ ] **Step 1: Falsos e cenário**

Em `tests/AtividadesJuridicas.UnitTests/Application/Falsos.cs` (com os `using` de `AtividadesJuridicas.Application.Atividades` e `AtividadesJuridicas.Domain.Atividades`):

```csharp
public sealed class CalendarioFixo : ICalendarioDoEscritorio
{
    public DateOnly Hoje { get; set; } = new(2026, 10, 9);
}

public sealed class AtividadeRepositorioFalso(
    UsuarioRepositorioFalso usuarios, ClienteRepositorioFalso clientes, ProcessoRepositorioFalso processos,
    AreaAtuacaoRepositorioFalso areas) : IAtividadeRepositorio
{
    public List<Atividade> Itens { get; } = [];

    public Task<Atividade?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Itens.FirstOrDefault(a => a.Id == id));

    public Task<Pagina<AtividadeResumo>> ListarAsync(FiltroDeAtividades filtro, int pagina, int tamanho, CancellationToken ct)
    {
        // Mesma regra da consulta real. A ordem de inserção faz o papel de "mais recentes primeiro".
        var todos = Enumerable.Reverse(Itens)
            .Where(a => filtro.Status is null || a.Status == filtro.Status)
            .Where(a => filtro.ResponsavelId is null || a.EhResponsavel(filtro.ResponsavelId.Value))
            .Where(a => filtro.ClienteId is null || a.ClienteId == filtro.ClienteId)
            .Where(a => filtro.ProcessoId is null || a.ProcessoId == filtro.ProcessoId)
            .Where(a => !filtro.SomenteAtrasadas || a.EstaAtrasada(filtro.Hoje))
            .Where(a => string.IsNullOrWhiteSpace(filtro.Busca) ||
                        a.Titulo.Contains(filtro.Busca.Trim(), StringComparison.OrdinalIgnoreCase))
            .OrderBy(a => a.DataEntrega is null).ThenBy(a => a.DataEntrega)
            .ToList();

        string? Nome(Guid? id) => id is null ? null : usuarios.Itens.FirstOrDefault(u => u.Id == id)?.Nome;

        return Task.FromResult(new Pagina<AtividadeResumo>(
            [.. todos.Skip((pagina - 1) * tamanho).Take(tamanho).Select(a => new AtividadeResumo(
                a.Id, a.Titulo, a.Status, a.DataEntrega, a.EstaAtrasada(filtro.Hoje),
                clientes.Itens.FirstOrDefault(c => c.Id == a.ClienteId)?.Nome,
                processos.Itens.FirstOrDefault(p => p.Id == a.ProcessoId)?.Titulo,
                areas.Itens.FirstOrDefault(x => x.Id == a.AreaAtuacaoId)?.Nome,
                [.. a.Responsaveis.Select(r => Nome(r.UsuarioId) ?? string.Empty).Order()],
                Nome(a.RevisorId)))],
            todos.Count, pagina, tamanho));
    }

    public void Adicionar(Atividade atividade) => Itens.Add(atividade);

    public void Remover(Atividade atividade) => Itens.Remove(atividade);
}
```

`Cenario` ganha `public CalendarioFixo Calendario { get; } = new();` e `public AtividadeRepositorioFalso Atividades { get; }` (criado no construtor com `new AtividadeRepositorioFalso(Usuarios, Clientes, Processos, Areas)`, depois de `Processos`).

- [ ] **Step 2: Testes unitários dos casos de uso**

`tests/AtividadesJuridicas.UnitTests/Application/AtividadesTests.cs`:

```csharp
using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Domain.Atividades;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.UnitTests.Application;

public class AtividadesTests
{
    private const string NaoEncontrada = "Atividade não encontrada.";
    private static readonly DateOnly Entrega = new(2026, 10, 20);

    private readonly Cenario _c = new();
    private readonly Usuario _davi, _pedro, _paula, _ester;
    private readonly ClientePessoaFisica _maria, _jose;
    private readonly AreaAtuacao _trabalhista = AreaAtuacao.Criar("Trabalhista");
    private readonly AreaAtuacao _civel = AreaAtuacao.Criar("Cível");
    private readonly Processo _processo;

    public AtividadesTests()
    {
        _davi = _c.NovoUsuario("Davi Diretor", "davi@x.com", _c.PerfilDiretor);
        _pedro = _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);
        _paula = _c.NovoUsuario("Paula", "paula@x.com", _c.PerfilProducao);
        _ester = _c.NovoUsuario("Ester", "ester@x.com", _c.PerfilProducao);
        _maria = ClientePessoaFisica.Criar(new DadosDoCliente("Maria Silva", null, null, Endereco.Vazio, null), Cpf.Criar("529.982.247-25"), null, null);
        _jose = ClientePessoaFisica.Criar(new DadosDoCliente("José Souza", null, null, Endereco.Vazio, null), Cpf.Criar("111.444.777-35"), null, null);
        _c.Clientes.Itens.AddRange([_maria, _jose]);
        _c.Areas.Itens.AddRange([_trabalhista, _civel]);
        _processo = Processo.Criar(
            new DadosDoProcesso(null, "Reclamação trabalhista", _trabalhista.Id, null, null, null, null, null, null),
            [new ClienteDoProcesso(_maria.Id, PoloProcessual.Ativo)]);
        _c.Processos.Itens.Add(_processo);
    }

    private Usuario Diana => _c.Diretor;

    private Task<Resultado<Guid>> Salvar(Usuario? ator, Guid? id = null, string titulo = "Elaborar contestação",
        DateOnly? entrega = null, bool semEntrega = false, Guid? cliente = null, Guid? processo = null, Guid? area = null) =>
        new SalvarAtividadeManipulador(_c.Como(ator), _c.Atividades, _c.Clientes, _c.Processos, _c.Areas, _c.Relogio, _c.Unidade)
            .ManipularAsync(new SalvarAtividade(id, titulo, "Prazo de 15 dias.", semEntrega ? null : entrega ?? Entrega, cliente, processo, area), default);

    private Task<Resultado> Definir(Usuario? ator, Guid id, Guid? revisor, params (Usuario Usuario, string? Direcionamento)[] responsaveis) =>
        new DefinirResponsaveisDaAtividadeManipulador(_c.Como(ator), _c.Atividades, _c.Usuarios, _c.Perfis, _c.Relogio, _c.Unidade, _c.Auditoria)
            .ManipularAsync(new DefinirResponsaveisDaAtividade(
                id, [.. responsaveis.Select(r => new ResponsavelEntrada(r.Usuario.Id, r.Direcionamento))], revisor), default);

    private Task<Resultado> Concluir(Usuario? ator, Guid id) =>
        new ConcluirMinhaParteManipulador(_c.Como(ator), _c.Atividades, _c.Relogio, _c.Unidade, _c.Auditoria)
            .ManipularAsync(new ConcluirMinhaParte(id), default);

    private Task<Resultado> Aprovar(Usuario? ator, Guid id) =>
        new AprovarAtividadeManipulador(_c.Como(ator), _c.Atividades, _c.Relogio, _c.Unidade)
            .ManipularAsync(new AprovarAtividade(id), default);

    private Task<Resultado> Devolver(Usuario? ator, Guid id, string motivo, params Usuario[] responsaveis) =>
        new DevolverAtividadeManipulador(_c.Como(ator), _c.Atividades, _c.Relogio, _c.Unidade)
            .ManipularAsync(new DevolverAtividade(id, motivo, [.. responsaveis.Select(u => u.Id)]), default);

    private Task<Resultado> Reabrir(Usuario? ator, Guid id, string motivo, params Usuario[] responsaveis) =>
        new ReabrirAtividadeManipulador(_c.Como(ator), _c.Atividades, _c.Relogio, _c.Unidade)
            .ManipularAsync(new ReabrirAtividade(id, motivo, [.. responsaveis.Select(u => u.Id)]), default);

    private Task<Resultado> TrocarRevisor(Usuario? ator, Guid id, Usuario revisor) =>
        new TrocarRevisorDaAtividadeManipulador(_c.Como(ator), _c.Atividades, _c.Usuarios, _c.Perfis, _c.Unidade)
            .ManipularAsync(new TrocarRevisorDaAtividade(id, revisor.Id), default);

    private Task<Resultado> Excluir(Usuario? ator, Guid id) =>
        new ExcluirAtividadeManipulador(_c.Como(ator), _c.Atividades, _c.Unidade).ManipularAsync(new ExcluirAtividade(id), default);

    private Task<Resultado<AtividadeDetalhe>> Obter(Usuario? ator, Guid id) =>
        new ObterAtividadeManipulador(_c.Como(ator), _c.Atividades, _c.Usuarios, _c.Clientes, _c.Processos, _c.Areas, _c.Calendario, _c.Auditoria)
            .ManipularAsync(new ObterAtividade(id), default);

    private Task<Resultado<Pagina<AtividadeResumo>>> Listar(Usuario? ator, ListarAtividades? consulta = null) =>
        new ListarAtividadesManipulador(_c.Como(ator), _c.Atividades, _c.Calendario)
            .ManipularAsync(consulta ?? new ListarAtividades(), default);

    private Atividade NoRepositorio(Guid id) => _c.Atividades.Itens.Single(a => a.Id == id);

    private async Task<Guid> CriadaAsync(string titulo = "Elaborar contestação", DateOnly? entrega = null)
    {
        var resultado = await Salvar(Diana, titulo: titulo, entrega: entrega);
        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        return resultado.Valor;
    }

    /// <summary>Direcionada a Pedro e Paula, com Diana de revisora.</summary>
    private async Task<Guid> DirecionadaAsync(string titulo = "Elaborar contestação", DateOnly? entrega = null)
    {
        var id = await CriadaAsync(titulo, entrega);
        var definido = await Definir(Diana, id, Diana.Id, (_pedro, "Redigir"), (_paula, "Juntar documentos"));
        definido.Sucesso.Should().BeTrue(definido.Mensagem);
        return id;
    }

    private async Task<Guid> FeitaAsync()
    {
        var id = await DirecionadaAsync();
        (await Concluir(_pedro, id)).Sucesso.Should().BeTrue();
        (await Concluir(_paula, id)).Sucesso.Should().BeTrue();
        return id;
    }

    // ---- Salvar ----

    [Fact]
    public async Task Diretor_cria_atividade_avulsa_e_e_o_autor()
    {
        var resultado = await Salvar(Diana);

        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        var atividade = NoRepositorio(resultado.Valor);
        atividade.Status.Should().Be(StatusAtividade.Criada);
        atividade.TipoAutor.Should().Be(TipoAutor.Usuario);
        atividade.AutorId.Should().Be(Diana.Id);
        atividade.ClienteId.Should().BeNull();
        atividade.Historico.Single().DataHora.Should().Be(_c.Relogio.AgoraUtc);
        _c.Unidade.Salvamentos.Should().Be(1);
    }

    [Fact]
    public async Task Com_processo_a_area_e_a_do_processo_e_o_cliente_tem_de_ser_dele()
    {
        var certo = await Salvar(Diana, cliente: _maria.Id, processo: _processo.Id, area: _civel.Id);
        var clienteDeFora = await Salvar(Diana, cliente: _jose.Id, processo: _processo.Id);
        var semCliente = await Salvar(Diana, processo: _processo.Id);

        certo.Sucesso.Should().BeTrue(certo.Mensagem);
        NoRepositorio(certo.Valor).AreaAtuacaoId.Should().Be(_trabalhista.Id);
        clienteDeFora.Erro.Should().Be(TipoErro.Validacao);
        clienteDeFora.Mensagem.Should().Be("O cliente da atividade deve ser um dos clientes do processo.");
        semCliente.Erro.Should().Be(TipoErro.Validacao);
        _c.Atividades.Itens.Should().ContainSingle();
    }

    [Fact]
    public async Task Cliente_processo_e_area_informados_tem_de_existir()
    {
        (await Salvar(Diana, cliente: Guid.CreateVersion7())).Mensagem.Should().Be("Cliente não encontrado.");
        (await Salvar(Diana, cliente: _maria.Id, processo: Guid.CreateVersion7())).Mensagem.Should().Be("Processo não encontrado.");
        (await Salvar(Diana, area: Guid.CreateVersion7())).Mensagem.Should().Be("Área de atuação não encontrada.");
        _c.Atividades.Itens.Should().BeEmpty();
    }

    [Fact]
    public async Task Cliente_e_area_inativos_nao_entram_em_vinculo_novo_mas_permanecem_no_que_ja_existia()
    {
        var id = (await Salvar(Diana, cliente: _maria.Id, area: _civel.Id)).Valor;
        // Use os métodos reais de inativação de Cliente e de AreaAtuacao (confira os nomes no domínio).
        _maria.Inativar();
        _civel.Inativar();

        var manter = await Salvar(Diana, id, titulo: "Título novo", cliente: _maria.Id, area: _civel.Id);
        var novaComInativo = await Salvar(Diana, cliente: _maria.Id);
        var novaComAreaInativa = await Salvar(Diana, area: _civel.Id);

        manter.Sucesso.Should().BeTrue(manter.Mensagem);
        NoRepositorio(id).Titulo.Should().Be("Título novo");
        novaComInativo.Mensagem.Should().Be("O cliente está inativo.");
        novaComAreaInativa.Mensagem.Should().Be("A área de atuação está inativa.");
    }

    [Fact]
    public async Task Editar_valida_e_nao_encontra_o_que_nao_existe()
    {
        var id = await DirecionadaAsync();

        (await Salvar(Diana, id, semEntrega: true)).Erro.Should().Be(TipoErro.Validacao);
        (await Salvar(Diana, id, titulo: "  ")).Erro.Should().Be(TipoErro.Validacao);
        (await Salvar(Diana, Guid.CreateVersion7())).Erro.Should().Be(TipoErro.NaoEncontrado);
        NoRepositorio(id).Titulo.Should().Be("Elaborar contestação");
    }

    // ---- Direcionar ----

    [Fact]
    public async Task Definir_responsaveis_direciona_e_anota_o_evento()
    {
        var id = await CriadaAsync();

        var resultado = await Definir(Diana, id, Diana.Id, (_pedro, "Redigir"), (_paula, null));

        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        var atividade = NoRepositorio(id);
        atividade.Status.Should().Be(StatusAtividade.Direcionada);
        atividade.RevisorId.Should().Be(Diana.Id);
        var evento = _c.Auditoria.Eventos.Should().ContainSingle().Subject;
        evento.Evento.Should().Be(EventosDeAtividade.ResponsaveisAlterados);
        evento.Entidade.Should().Be("Atividade");
        evento.EntidadeId.Should().Be(id);
        evento.Imediato.Should().BeFalse();
        evento.Dados.Should().BeEquivalentTo(new
        {
            adicionados = new[] { _pedro.Id, _paula.Id },
            removidos = Array.Empty<Guid>(),
            direcionamentoAlterado = Array.Empty<Guid>(),
        });
    }

    [Fact]
    public async Task Definir_os_mesmos_responsaveis_nao_anota_evento()
    {
        var id = await DirecionadaAsync();
        _c.Auditoria.Eventos.Clear();

        (await Definir(Diana, id, Diana.Id, (_pedro, "Redigir"), (_paula, "Juntar documentos"))).Sucesso.Should().BeTrue();

        _c.Auditoria.Eventos.Should().BeEmpty();
    }

    [Fact]
    public async Task Responsavel_de_diretoria_unico_vira_o_revisor_e_outro_e_recusado()
    {
        var id = await CriadaAsync();

        var outro = await Definir(Diana, id, Diana.Id, (_davi, null), (_pedro, null));
        var travado = await Definir(Diana, id, null, (_davi, null), (_pedro, null));

        outro.Erro.Should().Be(TipoErro.Validacao);
        outro.Mensagem.Should().Be(Atividade.RevisorTravado);
        travado.Sucesso.Should().BeTrue(travado.Mensagem);
        NoRepositorio(id).RevisorId.Should().Be(_davi.Id);
    }

    [Fact]
    public async Task Usuario_inexistente_ou_inativo_nao_entra_como_responsavel_nem_revisor()
    {
        var id = await CriadaAsync();
        _ester.Inativar();
        var fantasma = Usuario.Criar("Fantasma", Email.Criar("f@x.com"), "hash", _c.PerfilProducao);

        (await Definir(Diana, id, Diana.Id, (fantasma, null))).Mensagem.Should().Be("Usuário não encontrado.");
        (await Definir(Diana, id, Diana.Id, (_ester, null))).Mensagem.Should().Be("Usuário inativo não pode ser incluído como responsável.");
        (await Definir(Diana, id, _pedro.Id, (_paula, null))).Mensagem.Should().Be("O revisor deve ser um usuário ativo de Diretoria.");
        (await Definir(Diana, id, fantasma.Id, (_paula, null))).Mensagem.Should().Be("Usuário não encontrado.");
        NoRepositorio(id).Status.Should().Be(StatusAtividade.Criada);
    }

    [Fact]
    public async Task Trocar_revisor_respeita_a_regra_e_o_acesso()
    {
        var id = await FeitaAsync();

        (await TrocarRevisor(_pedro, id, _davi)).Erro.Should().Be(TipoErro.AcessoNegado);
        (await TrocarRevisor(Diana, id, _pedro)).Mensagem.Should().Be("O revisor deve ser um usuário ativo de Diretoria.");
        (await TrocarRevisor(Diana, id, _davi)).Sucesso.Should().BeTrue();
        NoRepositorio(id).RevisorId.Should().Be(_davi.Id);
    }

    // ---- Concluir, aprovar, devolver, reabrir ----

    [Fact]
    public async Task Responsaveis_concluem_revisor_aprova_e_o_percurso_fica_no_historico()
    {
        var id = await FeitaAsync();
        NoRepositorio(id).Status.Should().Be(StatusAtividade.Feita);

        (await Aprovar(Diana, id)).Sucesso.Should().BeTrue();

        var atividade = NoRepositorio(id);
        atividade.Status.Should().Be(StatusAtividade.Concluida);
        atividade.ConcluidaEm.Should().Be(_c.Relogio.AgoraUtc);
        atividade.Historico.Select(h => h.StatusNovo).Should().Equal(
            StatusAtividade.Criada, StatusAtividade.Direcionada, StatusAtividade.Feita, StatusAtividade.Concluida);
    }

    [Fact]
    public async Task So_o_revisor_de_diretoria_aprova_e_devolve()
    {
        var id = await FeitaAsync();

        (await Aprovar(_pedro, id)).Erro.Should().Be(TipoErro.AcessoNegado);
        (await Devolver(_pedro, id, "motivo", _pedro)).Erro.Should().Be(TipoErro.AcessoNegado);
        var outroDiretor = await Aprovar(_davi, id);
        outroDiretor.Erro.Should().Be(TipoErro.Validacao);
        outroDiretor.Mensagem.Should().Be(Atividade.SoORevisor);
        NoRepositorio(id).Status.Should().Be(StatusAtividade.Feita);
    }

    [Fact]
    public async Task Devolver_e_reabrir_seguem_o_fluxo()
    {
        var id = await FeitaAsync();

        (await Devolver(Diana, id, "Faltou a procuração", _paula)).Sucesso.Should().BeTrue();
        NoRepositorio(id).Status.Should().Be(StatusAtividade.ParaCorrecao);
        (await Concluir(_pedro, id)).Mensagem.Should().Be("Sua parte já está concluída.");
        (await Concluir(_paula, id)).Sucesso.Should().BeTrue();
        NoRepositorio(id).Status.Should().Be(StatusAtividade.Corrigida);
        (await Aprovar(Diana, id)).Sucesso.Should().BeTrue();

        (await Reabrir(_pedro, id, "Ajuste", _pedro)).Erro.Should().Be(TipoErro.AcessoNegado);
        (await Reabrir(_davi, id, "  ", _pedro)).Mensagem.Should().Be("Informe o motivo.");
        (await Reabrir(_davi, id, "Cliente pediu ajuste", _pedro)).Sucesso.Should().BeTrue();
        var atividade = NoRepositorio(id);
        atividade.Status.Should().Be(StatusAtividade.Reaberta);
        atividade.Historico.Last().UsuarioId.Should().Be(_davi.Id);
        atividade.Historico.Last().Motivo.Should().Be("Cliente pediu ajuste");
    }

    [Fact]
    public async Task Concluir_a_parte_de_atividade_que_nao_e_sua_responde_nao_encontrada_e_fica_registrado()
    {
        var id = await DirecionadaAsync();
        _c.Auditoria.Eventos.Clear();

        var resultado = await Concluir(_ester, id);

        resultado.Erro.Should().Be(TipoErro.NaoEncontrado);
        resultado.Mensagem.Should().Be(NaoEncontrada);
        var evento = _c.Auditoria.Eventos.Should().ContainSingle().Subject;
        evento.Evento.Should().Be(EventosDeSeguranca.AcessoNegado);
        evento.Entidade.Should().Be("Atividade");
        evento.EntidadeId.Should().Be(id);
        evento.Imediato.Should().BeTrue();
    }

    [Fact]
    public async Task Diretor_que_nao_e_responsavel_nao_conclui_parte_alguma()
    {
        var id = await DirecionadaAsync();

        var resultado = await Concluir(_davi, id);

        resultado.Erro.Should().Be(TipoErro.Validacao);
        resultado.Mensagem.Should().Be("Você não é responsável por esta atividade.");
    }

    [Fact]
    public async Task Conflito_de_concorrencia_vira_mensagem_de_recarregar()
    {
        var id = await DirecionadaAsync();
        _c.Unidade.Falha = new ExcecaoDeConcorrencia("alterado");

        var resultado = await Concluir(_pedro, id);

        resultado.Erro.Should().Be(TipoErro.Conflito);
        resultado.Mensagem.Should().Be("A atividade foi alterada por outra pessoa. Recarregue e tente novamente.");
    }

    // ---- Excluir ----

    [Fact]
    public async Task So_a_diretoria_exclui()
    {
        var id = await DirecionadaAsync();

        (await Excluir(_pedro, id)).Erro.Should().Be(TipoErro.AcessoNegado);
        (await Excluir(Diana, Guid.CreateVersion7())).Erro.Should().Be(TipoErro.NaoEncontrado);
        (await Excluir(Diana, id)).Sucesso.Should().BeTrue();
        _c.Atividades.Itens.Should().BeEmpty();
    }

    // ---- Acesso dos comandos ----

    [Fact]
    public async Task Producao_e_anonimo_nao_criam_editam_nem_direcionam()
    {
        var id = await DirecionadaAsync();

        (await Salvar(_pedro)).Erro.Should().Be(TipoErro.AcessoNegado);
        (await Salvar(null)).Erro.Should().Be(TipoErro.AcessoNegado);
        (await Salvar(_pedro, id, titulo: "Meu título")).Erro.Should().Be(TipoErro.AcessoNegado);
        (await Definir(_pedro, id, _pedro.Id, (_pedro, "tudo comigo"))).Erro.Should().Be(TipoErro.AcessoNegado);
        (await Concluir(null, id)).Erro.Should().Be(TipoErro.AcessoNegado);
        NoRepositorio(id).Titulo.Should().Be("Elaborar contestação");
        _c.Atividades.Itens.Should().ContainSingle();
    }

    // ---- Ver ----

    [Fact]
    public async Task Diretor_ve_a_atividade_inteira_com_todos_os_direcionamentos_e_as_acoes()
    {
        var id = (await Salvar(Diana, cliente: _maria.Id, processo: _processo.Id)).Valor;
        (await Definir(Diana, id, Diana.Id, (_pedro, "Redigir"), (_paula, "Juntar documentos"))).Sucesso.Should().BeTrue();

        var detalhe = (await Obter(Diana, id)).Valor!;

        detalhe.Should().BeEquivalentTo(new
        {
            Id = id, Titulo = "Elaborar contestação", Descricao = "Prazo de 15 dias.", DataEntrega = (DateOnly?)Entrega,
            Status = StatusAtividade.Direcionada, Atrasada = false, ClienteNome = "Maria Silva",
            ProcessoTitulo = "Reclamação trabalhista", AreaNome = "Trabalhista", TipoAutor = TipoAutor.Usuario,
            AutorNome = "Diana Diretora", RevisorNome = "Diana Diretora",
        });
        detalhe.Responsaveis.Should().BeEquivalentTo(
        [
            new ResponsavelDetalhe(_paula.Id, "Paula", true, "Juntar documentos", null),
            new ResponsavelDetalhe(_pedro.Id, "Pedro", true, "Redigir", null),
        ], opcoes => opcoes.WithStrictOrdering());
        detalhe.Historico.Select(h => (h.StatusNovo, h.UsuarioNome)).Should().Equal(
            (StatusAtividade.Criada, "Diana Diretora"), (StatusAtividade.Direcionada, "Diana Diretora"));
        detalhe.Acoes.Should().Be(new AcoesDaAtividade(
            Editar: true, DefinirResponsaveis: true, TrocarRevisor: true, ConcluirMinhaParte: false,
            AprovarOuDevolver: false, Reabrir: false, Excluir: true));
    }

    [Fact]
    public async Task Responsavel_de_producao_ve_os_nomes_dos_colegas_mas_so_o_proprio_direcionamento()
    {
        var id = await DirecionadaAsync();

        var detalhe = (await Obter(_pedro, id)).Valor!;

        detalhe.Responsaveis.Should().BeEquivalentTo(
        [
            new ResponsavelDetalhe(_paula.Id, "Paula", false, null, null),
            new ResponsavelDetalhe(_pedro.Id, "Pedro", true, "Redigir", null),
        ], opcoes => opcoes.WithStrictOrdering());
        detalhe.Acoes.Should().Be(new AcoesDaAtividade(
            Editar: false, DefinirResponsaveis: false, TrocarRevisor: false, ConcluirMinhaParte: true,
            AprovarOuDevolver: false, Reabrir: false, Excluir: false));
    }

    [Fact]
    public async Task Producao_que_nao_e_responsavel_recebe_nao_encontrada_e_a_tentativa_fica_registrada()
    {
        var id = await DirecionadaAsync();
        _c.Auditoria.Eventos.Clear();

        var resultado = await Obter(_ester, id);
        var inexistente = await Obter(_ester, Guid.CreateVersion7());

        resultado.Erro.Should().Be(TipoErro.NaoEncontrado);
        resultado.Mensagem.Should().Be(NaoEncontrada);
        inexistente.Erro.Should().Be(TipoErro.NaoEncontrado);
        inexistente.Mensagem.Should().Be(NaoEncontrada);
        _c.Auditoria.Eventos.Should().ContainSingle(e => e.Evento == EventosDeSeguranca.AcessoNegado && e.EntidadeId == id);
        (await Obter(null, id)).Erro.Should().Be(TipoErro.AcessoNegado);
    }

    [Fact]
    public async Task Acoes_acompanham_o_status_e_o_papel()
    {
        var id = await FeitaAsync();

        (await Obter(Diana, id)).Valor!.Acoes.Should().Be(new AcoesDaAtividade(
            Editar: true, DefinirResponsaveis: false, TrocarRevisor: true, ConcluirMinhaParte: false,
            AprovarOuDevolver: true, Reabrir: false, Excluir: true));
        (await Obter(_davi, id)).Valor!.Acoes.AprovarOuDevolver.Should().BeFalse("Davi é diretor, mas não é o revisor");
        (await Obter(_pedro, id)).Valor!.Acoes.ConcluirMinhaParte.Should().BeFalse("a parte dele já está concluída");

        (await Aprovar(Diana, id)).Sucesso.Should().BeTrue();
        (await Obter(_davi, id)).Valor!.Acoes.Should().Be(new AcoesDaAtividade(
            Editar: true, DefinirResponsaveis: false, TrocarRevisor: false, ConcluirMinhaParte: false,
            AprovarOuDevolver: false, Reabrir: true, Excluir: true));
    }

    [Fact]
    public async Task Atrasada_usa_a_data_do_escritorio()
    {
        var id = await DirecionadaAsync(entrega: new DateOnly(2026, 10, 9));

        _c.Calendario.Hoje = new DateOnly(2026, 10, 9);
        (await Obter(Diana, id)).Valor!.Atrasada.Should().BeFalse();

        _c.Calendario.Hoje = new DateOnly(2026, 10, 10);
        (await Obter(Diana, id)).Valor!.Atrasada.Should().BeTrue();
    }

    [Fact]
    public void Ver_e_consulta_auditavel_da_atividade()
    {
        var id = Guid.CreateVersion7();
        IConsultaAuditavel consulta = new ObterAtividade(id);

        consulta.TipoRecurso.Should().Be("Atividade");
        consulta.Acao.Should().Be(AcaoDeAcesso.Visualizou);
        consulta.RecursoId.Should().Be(id);
    }

    // ---- Listar ----

    [Fact]
    public async Task Diretoria_lista_todas_e_producao_so_as_suas_qualquer_que_seja_o_filtro_pedido()
    {
        var dosDois = await DirecionadaAsync("De Pedro e Paula");
        var daEster = await CriadaAsync("Da Ester");
        (await Definir(Diana, daEster, Diana.Id, (_ester, null))).Sucesso.Should().BeTrue();
        var semNinguem = await CriadaAsync("Sem responsável");

        (await Listar(Diana)).Valor!.Itens.Select(a => a.Id).Should().BeEquivalentTo([dosDois, daEster, semNinguem]);
        (await Listar(_pedro)).Valor!.Itens.Select(a => a.Id).Should().Equal(dosDois);
        (await Listar(_pedro, new ListarAtividades(ResponsavelId: _ester.Id))).Valor!.Itens.Select(a => a.Id)
            .Should().Equal(dosDois, "Produção não lista as atividades de outro");
        (await Listar(Diana, new ListarAtividades(ResponsavelId: _ester.Id))).Valor!.Itens.Select(a => a.Id).Should().Equal(daEster);
        (await Listar(null)).Erro.Should().Be(TipoErro.AcessoNegado);
    }

    [Fact]
    public async Task Listagem_filtra_por_status_cliente_atraso_e_busca_e_traz_os_nomes()
    {
        var atrasada = await DirecionadaAsync("Contestação", new DateOnly(2026, 10, 1));
        var noPrazo = (await Salvar(Diana, titulo: "Recurso", cliente: _maria.Id)).Valor;
        _c.Calendario.Hoje = new DateOnly(2026, 10, 9);

        (await Listar(Diana, new ListarAtividades(SomenteAtrasadas: true))).Valor!.Itens.Select(a => a.Id).Should().Equal(atrasada);
        (await Listar(Diana, new ListarAtividades(Status: StatusAtividade.Criada))).Valor!.Itens.Select(a => a.Id).Should().Equal(noPrazo);
        (await Listar(Diana, new ListarAtividades(ClienteId: _maria.Id))).Valor!.Itens.Select(a => a.Id).Should().Equal(noPrazo);
        (await Listar(Diana, new ListarAtividades(Busca: "contest"))).Valor!.Itens.Select(a => a.Id).Should().Equal(atrasada);

        var item = (await Listar(Diana, new ListarAtividades(SomenteAtrasadas: true))).Valor!.Itens.Single();
        item.Atrasada.Should().BeTrue();
        item.Responsaveis.Should().Equal("Paula", "Pedro");
        item.RevisorNome.Should().Be("Diana Diretora");
    }

    [Fact]
    public void Listagem_e_consulta_auditavel_com_os_filtros_no_detalhe()
    {
        IConsultaAuditavel consulta = new ListarAtividades(Status: StatusAtividade.Feita, Busca: "contestação", Pagina: 2);

        consulta.TipoRecurso.Should().Be("Atividade");
        consulta.Acao.Should().Be(AcaoDeAcesso.Listou);
        consulta.RecursoId.Should().BeNull();
        consulta.Detalhe.Should().Contain("contestação").And.Contain("\"Pagina\":2");
    }
}
```

- [ ] **Step 3: Rodar e ver falhar**

Run: `dotnet test tests/AtividadesJuridicas.UnitTests --filter "FullyQualifiedName~AtividadesTests"`
Expected: FAIL na compilação.

- [ ] **Step 4: Portas e apoio**

Em `Application/Comum/Portas.cs`:

```csharp
/// <summary>A data de hoje no fuso do escritório (o dia vira à meia-noite de lá, não à meia-noite UTC).</summary>
public interface ICalendarioDoEscritorio
{
    DateOnly Hoje { get; }
}
```

`Application/Atividades/EventosDeAtividade.cs`:

```csharp
namespace AtividadesJuridicas.Application.Atividades;

public static class EventosDeAtividade
{
    /// <summary>Os responsáveis de uma atividade mudaram. Anotado na atividade; cada entrada tem ainda o seu próprio registro.</summary>
    public const string ResponsaveisAlterados = "atividade_responsaveis_alterados";
}
```

`Application/Atividades/PortasDeAtividades.cs`: as constantes, os registros e a interface exatamente como em **Produces** (com comentários de documentação curtos; `IAtividadeRepositorio.ObterPorIdAsync` devolve a atividade com responsáveis e histórico; `Remover` é exclusão lógica da atividade e das filhas).

`Application/Atividades/PessoasDaAtividade.cs`:

```csharp
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Atividades;

namespace AtividadesJuridicas.Application.Atividades;

/// <summary>Carrega usuários com os seus perfis, como o domínio de atividades precisa conhecê-los.</summary>
internal static class PessoasDaAtividade
{
    /// <summary>Nulo quando algum dos Ids não corresponde a um usuário (ou o perfil dele não é encontrado).</summary>
    public static async Task<IReadOnlyDictionary<Guid, PessoaDaAtividade>?> CarregarAsync(
        IUsuarioRepositorio usuarios, IPerfilRepositorio perfis, IReadOnlyCollection<Guid> ids, CancellationToken ct)
    {
        var distintos = ids.Distinct().ToList();
        var encontrados = await usuarios.ObterVariosAsync(distintos, ct);
        if (encontrados.Count != distintos.Count) return null;

        var perfisDosUsuarios = (await perfis.ObterVariosAsync([.. encontrados.Select(u => u.PerfilId).Distinct()], ct))
            .ToDictionary(p => p.Id);
        if (encontrados.Any(u => !perfisDosUsuarios.ContainsKey(u.PerfilId))) return null;

        return encontrados.ToDictionary(u => u.Id, u => PessoaDaAtividade.De(u, perfisDosUsuarios[u.PerfilId]));
    }
}
```

- [ ] **Step 5: Casos de uso**

Escreva um arquivo por caso de uso, no estilo de `SalvarProcesso.cs` / `AlterarStatusProcesso.cs` (verificação de acesso primeiro; `try` em volta do domínio e do `SalvarAsync`; `catch (ExcecaoDeDominio)` → `Validacao`; `catch (ExcecaoDeConcorrencia)` → `Conflito`), seguindo as dez **Regras dos casos de uso** acima. Os construtores recebem as dependências na ordem usada pelos testes do Step 2. Dois deles, por inteiro, como referência de forma:

`ConcluirMinhaParte.cs`:

```csharp
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Application.Atividades;

public sealed record ConcluirMinhaParte(Guid Id) : IRequisicao<Resultado>;

public sealed class ConcluirMinhaParteManipulador(
    IContextoDeAcesso contexto,
    IAtividadeRepositorio atividades,
    IRelogio relogio,
    IUnidadeDeTrabalho unidade,
    IAuditoria auditoria) : IManipulador<ConcluirMinhaParte, Resultado>
{
    public async Task<Resultado> ManipularAsync(ConcluirMinhaParte requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { } ator)
            return Resultado.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        var atividade = await atividades.ObterPorIdAsync(requisicao.Id, ct);
        if (atividade is null) return Resultado.Falha(TipoErro.NaoEncontrado, AcessoAAtividade.NaoEncontrada);

        if (!AcessoAAtividade.PodeVer(ator, atividade))
        {
            await AcessoAAtividade.RegistrarNegadoAsync(auditoria, atividade.Id, nameof(ConcluirMinhaParte));
            return Resultado.Falha(TipoErro.NaoEncontrado, AcessoAAtividade.NaoEncontrada);
        }

        try
        {
            atividade.ConcluirParte(ator.Id, relogio.AgoraUtc);
            await unidade.SalvarAsync(ct);
            return Resultado.Ok();
        }
        catch (ExcecaoDeDominio ex)
        {
            return Resultado.Falha(TipoErro.Validacao, ex.Message);
        }
        catch (ExcecaoDeConcorrencia)
        {
            return Resultado.Falha(TipoErro.Conflito, AcessoAAtividade.Alterada);
        }
    }
}
```

com o apoio (no mesmo diretório, `AcessoAAtividade.cs` — acrescente-o à lista de arquivos no relatório):

```csharp
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Atividades;

namespace AtividadesJuridicas.Application.Atividades;

/// <summary>A regra de quem vê uma atividade e as respostas comuns dos casos de uso.</summary>
internal static class AcessoAAtividade
{
    public const string NaoEncontrada = "Atividade não encontrada.";
    public const string Alterada = "A atividade foi alterada por outra pessoa. Recarregue e tente novamente.";

    /// <summary>A Diretoria vê todas; os demais, só as atividades em que são responsáveis.</summary>
    public static bool PodeVer(UsuarioAutenticado ator, Atividade atividade) =>
        ator.EhDiretoria || atividade.EhResponsavel(ator.Id);

    /// <summary>Para quem pediu, a atividade não existe; para a Diretoria, a tentativa fica registrada.</summary>
    public static Task RegistrarNegadoAsync(IAuditoria auditoria, Guid atividadeId, string requisicao) =>
        auditoria.RegistrarAsync(
            EventosDeSeguranca.AcessoNegado, "Atividade", atividadeId,
            new { requisicao, resposta = "nao_encontrado" }, ct: CancellationToken.None);
}
```

`DefinirResponsaveisDaAtividade.cs`:

```csharp
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Atividades;
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Application.Atividades;

public sealed record ResponsavelEntrada(Guid UsuarioId, string? Direcionamento);

/// <summary>Deixa a atividade com exatamente os responsáveis informados; <paramref name="RevisorId"/> nulo mantém ou deduz o revisor.</summary>
public sealed record DefinirResponsaveisDaAtividade(Guid Id, IReadOnlyList<ResponsavelEntrada> Responsaveis, Guid? RevisorId)
    : IRequisicao<Resultado>;

public sealed class DefinirResponsaveisDaAtividadeManipulador(
    IContextoDeAcesso contexto,
    IAtividadeRepositorio atividades,
    IUsuarioRepositorio usuarios,
    IPerfilRepositorio perfis,
    IRelogio relogio,
    IUnidadeDeTrabalho unidade,
    IAuditoria auditoria) : IManipulador<DefinirResponsaveisDaAtividade, Resultado>
{
    public async Task<Resultado> ManipularAsync(DefinirResponsaveisDaAtividade requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true } ator)
            return Resultado.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        var atividade = await atividades.ObterPorIdAsync(requisicao.Id, ct);
        if (atividade is null) return Resultado.Falha(TipoErro.NaoEncontrado, AcessoAAtividade.NaoEncontrada);

        var pedidos = requisicao.Responsaveis ?? [];
        var ids = pedidos.Select(r => r.UsuarioId).ToList();
        if (requisicao.RevisorId is { } revisorId) ids.Add(revisorId);

        var pessoas = await PessoasDaAtividade.CarregarAsync(usuarios, perfis, ids, ct);
        if (pessoas is null) return Resultado.Falha(TipoErro.Validacao, "Usuário não encontrado.");

        try
        {
            var alteracao = atividade.DefinirResponsaveis(
                [.. pedidos.Select(r => new ResponsavelInformado(pessoas[r.UsuarioId], r.Direcionamento))],
                requisicao.RevisorId is { } id ? pessoas[id] : null,
                relogio.AgoraUtc, ator.Id);

            if (alteracao.Houve)
                auditoria.Anotar(EventosDeAtividade.ResponsaveisAlterados, "Atividade", atividade.Id, new
                {
                    adicionados = alteracao.Adicionados,
                    removidos = alteracao.Removidos,
                    direcionamentoAlterado = alteracao.DirecionamentoAlterado,
                });

            await unidade.SalvarAsync(ct);
            return Resultado.Ok();
        }
        catch (ExcecaoDeDominio ex)
        {
            return Resultado.Falha(TipoErro.Validacao, ex.Message);
        }
        catch (ExcecaoDeConcorrencia)
        {
            return Resultado.Falha(TipoErro.Conflito, AcessoAAtividade.Alterada);
        }
    }
}
```

Observação: `pedidos` com o mesmo usuário repetido chega ao domínio como dois `ResponsavelInformado` do mesmo usuário, e é o domínio que recusa ("Um usuário não pode se repetir entre os responsáveis.").

Os demais (`SalvarAtividade`, `TrocarRevisorDaAtividade`, `AprovarAtividade`, `DevolverAtividade`, `ReabrirAtividade`, `ExcluirAtividade`, `ListarAtividades`, `ObterAtividade`) seguem as regras e as assinaturas dos testes. Em `ObterAtividade`, os responsáveis vêm ordenados pelo nome e o histórico pela `Ordem`.

- [ ] **Step 6: Rodar os unitários**

Run: `dotnet test tests/AtividadesJuridicas.UnitTests`
Expected: PASS, todos; `RegistroDeManipuladoresTests` verde. A Infrastructure ainda não tem as implementações: é a Parte B.

- [ ] **Step 7: Testes de integração**

`tests/AtividadesJuridicas.IntegrationTests/AtividadesTests.cs` — escreva estes testes (nomes e asserções obrigatórios; os auxiliares seguem `ProcessosTests.cs` e `DocumentosTests.cs`). Preparação comum: admin (Diana), um segundo diretor (Davi), três usuários de Produção (Pedro, Paula, Ester), o cliente Maria, a área Trabalhista e um processo de Maria.

1. `Atividade_vai_e_volta_do_banco_inteira_com_responsaveis_e_historico` — cria com cliente e processo, direciona a Pedro e Paula com Diana revisora; `ObterAtividade` (como Diana) devolve todos os campos, os dois responsáveis com direcionamento, e o histórico `Criada`, `Direcionada` com os nomes.
2. `Fluxo_completo_fica_no_historico_e_na_auditoria` — conclui as duas partes, devolve para Paula com motivo, Paula conclui, Diana aprova, Davi reabre para Pedro com motivo. No banco (`IgnoreQueryFilters`): histórico com `Criada, Direcionada, Feita, ParaCorrecao, Corrigida, Concluida, Reaberta`, motivos gravados, `UsuarioId` de cada transição (Paula em `Feita` e em `Corrigida`; Diana em `ParaCorrecao` e `Concluida`; Davi em `Reaberta`); `concluida_em` nulo ao final. Na auditoria: uma linha `Criacao` de `Atividade`; linhas `Alteracao` de `Atividade` cujo JSON contém `Status` com `de`/`para`; uma linha `Criacao` de `AtividadeHistoricoStatus` por transição; linhas `Alteracao` de `AtividadeResponsavel` com `ConcluidoEm`.
3. `Concluir_so_a_parte_carimba_a_atividade_sem_gerar_alteracao_dela` — depois de Pedro concluir (Paula pendente): `AtualizadoEm`/`AtualizadoPor` da atividade preenchidos com Pedro, `VersaoDasPartes` aumentou, e **nenhuma** linha `Alteracao` de `Atividade` foi criada por essa operação (só a de `AtividadeResponsavel`).
4. `Duas_conclusoes_simultaneas_nao_deixam_a_atividade_presa` — determinístico, com dois escopos aninhados como em `ProcessosTests` (a corrida do processo): os dois carregam a atividade direcionada; o escopo de Paula grava primeiro; o de Pedro, ao gravar, recebe `ExcecaoDeConcorrencia` (pela `IUnidadeDeTrabalho`). Depois, `ConcluirMinhaParte` de Pedro pelo caso de uso tem sucesso e a atividade fica `Feita`. Um segundo teste, `Conclusao_concorrente_pelo_caso_de_uso_responde_conflito`, só se for possível montar de forma determinística; caso contrário, registre no relatório que o mapeamento para `Conflito` está coberto pelo teste unitário.
5. `Producao_so_lista_e_abre_as_atividades_em_que_e_responsavel` — três atividades (de Pedro e Paula; de Ester; sem responsável). Pedro lista só a primeira, mesmo pedindo `ResponsavelId` de Ester; `Total` é 1; `ObterAtividade` da de Ester responde `NaoEncontrado` e gera `acesso_negado` com `Entidade = "Atividade"`, o Id, e Pedro como usuário; não há `RegistroAcesso` `Visualizou` para essa tentativa. Diana lista as três.
6. `Responsavel_ve_so_o_proprio_direcionamento` — Pedro abre a atividade: o direcionamento de Paula vem nulo com `DirecionamentoVisivel = false`; o dele vem preenchido.
7. `Listagem_ordena_filtra_e_pagina_no_banco` — 21 atividades com datas de entrega distintas mais uma sem data: página 1 tem 20, em ordem crescente de entrega; página 2 tem as 2 restantes, a sem data por último; filtro `SomenteAtrasadas` com o calendário do escritório (use uma data de entrega no passado distante e outra no futuro distante para não depender do dia em que o teste roda); busca "contestacao" encontra "Contestação"; filtros por cliente, processo e status.
8. `Remover_responsavel_e_logico_e_o_evento_fica_na_mesma_transacao` — tira Paula e inclui Ester: a entrada de Paula continua no banco com `excluido_em`; há `Exclusao` de `AtividadeResponsavel`, `Criacao` da nova, e um evento `atividade_responsaveis_alterados` com o mesmo `IdCorrelacao`, cujos dados trazem `adicionados` e `removidos`.
9. `Responsavel_removido_pode_voltar` — remove Paula e a inclui de novo: duas entradas dela no banco, uma excluída.
10. `Excluir_e_logico_para_a_atividade_os_responsaveis_e_o_historico` — depois de excluir: tudo com `excluido_em`; a listagem não traz; `ObterAtividade` responde `NaoEncontrado`.
11. `Banco_recusa_apagar_fisicamente_uma_atividade_com_filhas` — `DELETE FROM atividades` falha com violação de FK.
12. `Banco_recusa_dois_responsaveis_vivos_iguais_na_mesma_atividade` — `INSERT` bruto de uma segunda entrada viva do mesmo usuário falha com violação de unicidade.
13. `Acessos_ficam_no_registro_de_consultas` — listar e abrir geram `Listou` e `Visualizou` (com o `RecursoId`) para `TipoRecurso = "Atividade"`.
14. `Producao_nao_cria_nem_direciona_e_a_tentativa_fica_registrada` — `AcessoNegado` e dois eventos `acesso_negado` do usuário.
15. `Calendario_do_escritorio_usa_o_fuso_configurado` — resolva `ICalendarioDoEscritorio` e confira que `Hoje` é a data de agora convertida para `America/Sao_Paulo` (calcule o esperado no teste com `TimeZoneInfo`); e, construindo `CalendarioDoEscritorio` diretamente com um relógio fixo em `2026-10-10T02:00:00Z`, `Hoje` é `2026-10-09` (em São Paulo ainda são 23h do dia 9).

Em `PropriedadesAuditadasTests.cs`, acrescente à lista esperada:

```csharp
            ["Atividade"] =
            [
                "AreaAtuacaoId", "AutorId", "ClienteId", "ConcluidaEm", "DataEntrega", "Descricao", "ProcessoId",
                "RevisorId", "Status", "TipoAutor", "Titulo",
            ],
            ["AtividadeHistoricoStatus"] = ["AtividadeId", "DataHora", "Motivo", "Ordem", "StatusAnterior", "StatusNovo", "UsuarioId"],
            ["AtividadeResponsavel"] = ["AtividadeId", "ConcluidoEm", "Direcionamento", "UsuarioId"],
```

- [ ] **Step 8: Mapeamento, repositório, calendário e composição**

`AtividadeConfiguracao.cs` — três classes de configuração, espelhando `ProcessoConfiguracao`:

- `atividades`: `Titulo` (200, obrigatório), `Descricao` (10000, obrigatório), `Status` e `TipoAutor` como `int`, `DataEntrega` (`date`), `Versao` como `xmin` (`HasColumnName("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken()`), `VersaoDasPartes` (`HasDefaultValue(0)`); FKs com `Restrict` para `clientes`, `processos`, `areas_atuacao` e, duas vezes, `usuarios` (`AutorId`, `RevisorId`); índices em `status`, `data_entrega`, `cliente_id`, `processo_id`; as duas coleções com `ClientCascade` e acesso por campo (`_responsaveis`, `_historico`).
- `atividades_responsaveis`: `Direcionamento` (4000); FK `Restrict` para `usuarios`; índice único `(atividade_id, usuario_id)` filtrado por `excluido_em IS NULL`; índice em `usuario_id`.
- `atividades_historico_status`: `StatusAnterior` e `StatusNovo` como `int`, `Motivo` (2000); FK `Restrict` para `usuarios` (`UsuarioId`, anulável); índice único em `(atividade_id, ordem)` filtrado por `excluido_em IS NULL`.

`AtividadeRepositorio.cs`: `ObterPorIdAsync` com os dois `Include`; `ListarAsync` aplicando os filtros no banco (responsável: `a.Responsaveis.Any(r => r.UsuarioId == id)`; atrasadas: `a.Status != Concluida && a.DataEntrega < hoje`; busca com `unaccent` + `ILike` + escape, como `ProcessoRepositorio`), ordenando por `DataEntrega == null`, `DataEntrega`, `CriadoEm` decrescente, `Id` decrescente, paginando e depois buscando os nomes (clientes, processos, áreas e usuários com `IgnoreQueryFilters`, como os outros repositórios); `Adicionar`; `Remover` (`db.Remove`).

`Infrastructure/Comum/CalendarioDoEscritorio.cs`:

```csharp
using AtividadesJuridicas.Application.Comum;

namespace AtividadesJuridicas.Infrastructure.Comum;

public sealed class CalendarioDoEscritorio(TimeZoneInfo fuso, IRelogio relogio) : ICalendarioDoEscritorio
{
    public DateOnly Hoje => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(relogio.AgoraUtc, fuso));
}
```

Em `DependencyInjection.cs`: novo parâmetro opcional `string fusoDoEscritorio = "America/Sao_Paulo"` (último), `servicos.AddSingleton<ICalendarioDoEscritorio>(sp => new CalendarioDoEscritorio(TimeZoneInfo.FindSystemTimeZoneById(fusoDoEscritorio), sp.GetRequiredService<IRelogio>()));`, `servicos.AddScoped<IAtividadeRepositorio, AtividadeRepositorio>();`. Em `Program.cs`, passe `fusoDoEscritorio: builder.Configuration["Escritorio:FusoHorario"] ?? "America/Sao_Paulo"` (o mesmo valor usado para `FusoDoEscritorio`).

Em `AppDbContext.cs`: `public DbSet<Atividade> Atividades => Set<Atividade>();`.

- [ ] **Step 9: Migration**

```bash
dotnet ef migrations add Atividades --project src/AtividadesJuridicas.Infrastructure --startup-project src/AtividadesJuridicas.Infrastructure --output-dir Persistencia/Migrations
```

Expected: cria só `atividades`, `atividades_responsaveis` e `atividades_historico_status`, com os índices e FKs descritos, as FKs `atividade_id` das filhas **sem** cascata, e sem coluna física `xmin` (se o EF emitir a linha da coluna `xmin` na migration, apague só essa linha, como foi feito na migration `Processos`, e registre no relatório). Nenhuma alteração em tabelas existentes.

- [ ] **Step 10: Rodar tudo**

Run: `dotnet test` (suíte inteira, da raiz)
Expected: PASS, tudo, sem avisos. `GravacoesPorForaTests` verde (nenhum SQL bruto novo em `src/`).

- [ ] **Step 11: Commit**

```bash
git add src tests
git commit -m "feat(atividades): casos de uso, persistência e auditoria das atividades"
```

---

### Task 3: Tela de atividades

**Files:**
- Create: `src/AtividadesJuridicas.Web/Components/Pages/Atividades.razor`
- Create: `src/AtividadesJuridicas.Web/Components/Atividades/FichaDaAtividade.razor`
- Modify: `src/AtividadesJuridicas.Web/Components/Layout/MainLayout.razor`
- Modify: `src/AtividadesJuridicas.Web/Components/_Imports.razor`
- Modify: `src/AtividadesJuridicas.Web/Components/Pages/Home.razor`
- Test: `tests/AtividadesJuridicas.IntegrationTests/AtividadesWebTests.cs`

**Interfaces:**
- Consumes: todos os casos de uso e modelos da Tarefa 2; `ListarUsuarios` (só Diretoria), `ListarClientes`, `ListarProcessos`, `ListarAreasAtuacao`; `ObterMeuAcesso`; `FusoDoEscritorio.Formatar`.
- Produces: rota `/atividades`; item "Atividades" no menu, para todos os perfis; componente `FichaDaAtividade` (parâmetros `AtividadeId` (`Guid`), `OnMudou` (`EventCallback`), `OnAcessoEncerrado` (`EventCallback`), `OnFechar` (`EventCallback`)).

#### O que a tela faz

**Página `/atividades`** (segue `Processos.razor` no estilo e no tratamento de perda de acesso):

- "Carregando…" até a consulta de acesso terminar; sem usuário, "Seu acesso foi encerrado. Entre novamente."
- Filtros (formulário próprio, com `FormName`): busca por título, situação (lista com "Todas" e os sete status, com os nomes em português: Criada, Direcionada, Feita, Para correção, Corrigida, Concluída, Reaberta), "Só atrasadas". Para a Diretoria, também um filtro de responsável (usuários ativos).
- Tabela: Título, Situação (com o marcador "Atrasada" quando for o caso), Entrega (dd/MM/aaaa), Cliente, Processo, Responsáveis, Revisor, e um botão "Abrir" por linha. Paginação como nas outras listas; lista vazia mostra "Nenhuma atividade."; falha na consulta nunca aparece como lista vazia.
- Diretoria: botão "Nova atividade", que abre o formulário (Título, Descrição, Data de entrega, Cliente, Processo, Área). Cliente e processo são escolhidos por busca (como o seletor de clientes de `Processos.razor`, fora de qualquer `EditForm`, com Enter fazendo a busca); ao escolher um processo, o cliente passa a ser escolhido entre os clientes daquele processo e a área fica fixa na do processo. Ao salvar uma atividade nova, a ficha dela é aberta.
- "Abrir" mostra o componente `FichaDaAtividade` (com `@key` pelo Id) abaixo da lista; quando a ficha avisa que algo mudou, a lista é recarregada.

**Componente `FichaDaAtividade`**:

- Carrega `ObterAtividade`. `NaoEncontrado` mostra "Atividade não encontrada." e um botão "Fechar"; `AcessoNegado` em qualquer chamada dispara `OnAcessoEncerrado`.
- Mostra: título, situação (com "Atrasada"), descrição, entrega, cliente, processo, área, autor, revisor; a lista de responsáveis (nome, "Concluída em …" ou "Pendente", e o direcionamento **só quando `DirecionamentoVisivel`**); o histórico (data e hora no fuso do escritório, de → para, quem, motivo).
- Mostra só as ações que `Acoes` permite:
  - **Concluir minha parte** — com confirmação em dois cliques.
  - **Aprovar** (dois cliques) e **Devolver** — a devolução pede o motivo e a seleção de responsáveis, todos marcados por padrão, exigindo ao menos um.
  - **Reabrir** — motivo e seleção de responsáveis, como a devolução.
  - **Editar** — o mesmo formulário da criação, preenchido.
  - **Responsáveis** — editor: lista de usuários ativos com caixa de seleção e um campo de direcionamento para cada marcado; quem já é responsável e está inativo aparece identificado "(inativo)" e pode ser mantido ou retirado; seletor de revisor entre os diretores ativos com a opção "Manter / deduzir" (envia `RevisorId` nulo). Tudo o que está na lista aparece e só o que aparece é enviado.
  - **Trocar revisor** — seletor de diretores ativos.
  - **Excluir** — dois cliques.
- Cada ação: guarda contra clique duplo (um indicador "em andamento" que desabilita os botões), limpa a mensagem anterior ao começar, mostra a mensagem de erro do caso de uso quando falha (inclusive o conflito "A atividade foi alterada por outra pessoa…", caso em que a ficha é recarregada), e, quando dá certo, recarrega a ficha e dispara `OnMudou`. Depois de excluir, dispara `OnMudou` e `OnFechar`.
- Nenhum `EditForm` envolve campos de busca; lambdas de `@onclick` que só atribuem valor viram métodos.

**Menu e início**: "Atividades" aparece para todos, logo depois de "Início". Em `Home.razor`, um link "Minhas atividades" para `/atividades`.

- [ ] **Step 1: Testes de página**

`tests/AtividadesJuridicas.IntegrationTests/AtividadesWebTests.cs` — preparação: admin, Pedro (`CriarPedroAsync`), uma atividade "Elaborar contestação" direcionada a Pedro com o admin de revisor e entrega em 2020-01-15 (atrasada), e uma atividade "Recurso interno" sem responsáveis.

1. `Diretor_ve_todas_as_atividades_e_as_acoes_de_cadastro` — `/atividades` contém "Atividades", "Elaborar contestação", "Recurso interno", "Direcionada", "Criada", "Atrasada", "15/01/2020", "Pedro Produção", "Nova atividade", "Abrir".
2. `Producao_ve_so_as_suas_e_nenhuma_acao_de_cadastro` — contém "Elaborar contestação" e "Abrir"; não contém "Recurso interno", "Nova atividade" nem "Seu acesso foi encerrado".
3. `Menu_mostra_atividades_para_todos_os_perfis` — `href="/atividades"` na página inicial dos dois.
4. `Anonimo_e_redirecionado_ao_login`.
5. `Listagem_fica_no_registro_de_consultas` — depois do GET do Pedro, existe um `RegistroAcesso` `Listou` de `Atividade` para ele.

Se houver o teste que enumera as páginas protegidas, acrescente `/atividades`.

- [ ] **Step 2: Rodar e ver falhar**

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --filter "FullyQualifiedName~AtividadesWebTests"`
Expected: FAIL — a página não existe.

- [ ] **Step 3: Implementar a página, o componente, o menu e o início**

Conforme "O que a tela faz". Em `_Imports.razor`, acrescente `@using AtividadesJuridicas.Application.Atividades`, `@using AtividadesJuridicas.Domain.Atividades` e `@using AtividadesJuridicas.Web.Components.Atividades`.

- [ ] **Step 4: Rodar tudo**

Run: `dotnet test` (suíte inteira, da raiz)
Expected: PASS, tudo, sem avisos.

O comportamento interativo não tem teste automatizado (lacuna conhecida; não acrescente pacote). Leia cada caminho e descreva no relatório como verificou, em especial: o direcionamento de outro responsável nunca é renderizado para a Produção (o dado nem chega: confira que a ficha usa só o que `ObterAtividade` devolve); uma falha de consulta nunca aparece como "Nenhuma atividade."; `AcessoNegado` em qualquer ação encerra a tela; cada botão de ação só aparece quando `Acoes` permite; o editor de responsáveis envia exatamente o que mostra; clique duplo não repete a ação.

- [ ] **Step 5: Commit**

```bash
git add src tests
git commit -m "feat(web): tela de atividades com ficha, direcionamento e fluxo de status"
```
