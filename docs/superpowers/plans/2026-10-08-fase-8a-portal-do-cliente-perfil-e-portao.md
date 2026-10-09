# Fase 8A — Portal do cliente: perfil Cliente e portão: Plano de Implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Permitir que a Diretoria crie usuários de perfil Cliente, vinculados a um cliente do cadastro, que fazem login, veem um início vazio, trocam a própria senha e não alcançam nenhum dado interno do escritório.

**Architecture:** O cliente é um `Usuario` comum com um terceiro `TipoPerfil` e um `ClienteId`. O isolamento não depende de cada caso de uso: o `ExecutorDeRequisicoes`, por onde toda requisição passa, nega ao ator de perfil Cliente tudo o que não estiver marcado com `IRequisicaoAbertaAoCliente`. O domínio recusa o usuário de cliente em todo lugar onde só a equipe cabe (responsável, participante, lista de acesso), e o layout fecha as páginas internas para ele.

**Tech Stack:** .NET 10, Blazor Interactive Server, EF Core 10 + Npgsql, PostgreSQL 17, xUnit 2.9, FluentAssertions 7.2, Testcontainers.

**Spec:** `docs/superpowers/specs/2026-10-08-portal-do-cliente-design.md` (seções 2, 3, 4 — só "Mudança nos Documentos Gerais" e o item 2 da regra de acesso —, 6 — menu, Usuários e a lista "Usuários do cliente" —, 7, 8, 9 e 10). As duas categorias de documento, o Andamento e as páginas do portal são da fase 8B.

## Decisões deste plano

- **O portão vem antes de existir um jeito de criar usuário de cliente.** A ordem das tarefas é domínio, portão, gestão de usuários, telas. Em nenhum commit intermediário existe um usuário de cliente criado pela aplicação com acesso de Produção.
- **O perfil "Cliente" é criado pelo semeador, não por migration.** A gravação passa pelos interceptores (carimbos e auditoria), usa Guid versão 7 e trata o nome já usado em C#. O semeador roda a cada inicialização, então um banco que já existe ganha o perfil na primeira subida depois da atualização.
- **`Identidade` não usa tipos de `Cadastros`.** O que a identidade precisa saber de um cliente (existe, nome, ativo) vem pela porta `IClientesDaIdentidade`, declarada em `Application/Identidade` e implementada na infraestrutura, como `IPendenciasDoUsuario`.
- **Nesta fase o portão abre três requisições:** `AutenticarUsuario`, `ObterMeuAcesso` e `AlterarMinhaSenha`. `BaixarDocumento` e as consultas do portal são marcadas na 8B. Um download pedido por um cliente nesta fase responde 403.
- **`AutenticarUsuario` precisa estar aberta.** Ela não usa o ator, mas o portão o consulta pela sessão que o navegador ainda tem: sem a marca, um cliente com sessão aberta não conseguiria entrar de novo.
- **Para o perfil Cliente, `Documento.PodeSerAcessadoPor` responde falso em todas as categorias que existem hoje**, mesmo que o usuário ou o perfil conste em alguma lista. A 8B abre as duas categorias novas.
- **Par perfil–cliente incoerente não tem acesso.** O banco não garante sozinho que só usuário de perfil Cliente tem `ClienteId`. Se o par estiver incoerente (edição manual no banco), o contexto de acesso devolve nulo e o login é recusado, em vez de tratar o usuário como equipe.
- **`ListarUsuarios` continua trazendo todos os usuários**, com o cliente de quem é de cliente: a auditoria filtra por qualquer usuário. As telas que escolhem gente da equipe (responsáveis, filtros de atividades e de agenda) deixam os usuários de cliente de fora. `ListarNomesDosAtivosAsync`, que alimenta os participantes de evento, deixa de trazê-los já no repositório.
- **A página interna não é montada para o cliente.** O layout decide antes de renderizar o corpo: para o cliente fora das rotas dele, mostra o conteúdo de "Página não encontrada" e a página nem é instanciada. Nenhuma requisição é enviada e nenhum `acesso_negado` é gerado pela visita.
- **Login de usuário cujo cliente está inativo conta como tentativa.** A tentativa já foi reservada antes de a senha ser conferida; não é devolvida. Cinco tentativas seguidas bloqueiam, como para qualquer usuário.
- **Manter o mesmo cliente na edição não o revalida.** Um usuário cujo cliente foi inativado continua editável (nome, e-mail) sem trocar de cliente. Trocar exige cliente existente e ativo.

## Como ler este plano

Cada tarefa traz as **regras** (o contrato), as **interfaces** que ela consome e produz e a lista de **testes obrigatórios** (nome e o que cada um confere). Onde o detalhe é sutil ou é a própria proteção (o portão, o contexto de acesso, o semeador, a guarda de rotas), o plano traz o código. O restante é escrito pelo implementador no estilo do que já existe. O código do plano não foi executado: quando o EF Core, o Npgsql ou o Blazor exigirem, ajuste o mínimo para preservar o comportamento e registre o desvio no relatório, com a mensagem de erro que o motivou.

Nunca apague um teste nem afrouxe uma asserção. Ajustar como um teste obtém as suas entradas é permitido. Quando um número mudar porque a regra mudou (o sistema passa a ter três perfis), atualize o número e liste o teste no relatório. O texto de `Dados` gravado em `jsonb` volta reformatado pelo PostgreSQL: compare JSON pela estrutura. Nos testes de integração, nunca escolha um registro de auditoria pela posição: selecione por operação, evento ou entidade.

## Estado de partida

- Worktree `.claude/worktrees/feat+fase-8-portal-do-cliente`, branch `worktree-feat+fase-8-portal-do-cliente`, criado a partir da `main` (`39c0527`, fase 6) mais o commit da spec do portal.
- A fase 7 (complexidade e produtividade) corre em paralelo no branch `feat/fase-7-produtividade` e **não está neste worktree**. As duas fases mexem em `MainLayout.razor`, no formulário de atividade, em `AppDbContextModelSnapshot.cs` e na modelagem de domínio. A que for integrada por último refaz a sua migration sobre a outra.
- Domínio: `TipoPerfil { Diretoria = 1, Producao = 2 }`; `Usuario` sem vínculo com cliente; `PessoaDaAtividade(UsuarioId, Tipo, Ativo)`; `PessoaDoEvento(UsuarioId, Ativo)`; `Documento.PodeSerAcessadoPor(usuarioId, perfilId, tipoPerfil, ehResponsavelDaAtividade)`.
- Aplicação: `UsuarioAutenticado(Id, Nome, TipoPerfil, PerfilId)` com `EhDiretoria`; `ExecutorDeRequisicoes(servicos, auditoria)`; os casos de uso testam `EhDiretoria` ou só "há ator".
- Web: `MainLayout.razor` carrega o ator e mostra o menu; cada página consulta `ObterMeuAcesso` por conta própria.
- Testes: 675 unitários passando. Antes da Tarefa 1, rode `dotnet test` inteiro (precisa do Docker) e anote no relatório o total de integração: é a linha de base.

## Global Constraints

- TFM único `net10.0` em `Directory.Build.props`. `Nullable` e `TreatWarningsAsErrors=true`. Versões de pacote só em `Directory.Packages.props`. Nenhum pacote novo.
- `Domain` não referencia nada. `Application` não referencia EF Core, Npgsql, `Infrastructure` nem `Web`.
- Dentro da `Application`, `Identidade` não usa tipos de `Cadastros`, `Documentos`, `Atividades` nem `Agenda`.
- Código, nomes de tabela e mensagens em português. Tabelas e colunas em `snake_case`. Datas e horas em UTC no domínio, na aplicação e no banco.
- Exclusão sempre lógica. Agregados se referenciam só por Id.
- Toda alteração de `Entidade` gera registro de auditoria na mesma transação. Toda propriedade nova é classificada em `PropriedadesAuditadasTests`. Nenhuma gravação por fora dos interceptores: `GravacoesPorForaTests` continua com a mesma lista.
- Autorização verificada no manipulador e, para o perfil Cliente, antes dele, no executor. A interface apenas oculta.
- Um caso de uso por arquivo. Novas dependências de construtor e novos membros posicionais de registros entram por último.
- Consultas são `IConsultaAuditavel`.
- Uma única migration nesta fase: `UsuarioDoCliente`.
- Mensagens exatas (copiadas da spec e deste plano):
  - `"Informe o cliente do usuário."`
  - `"Só um usuário de perfil Cliente é vinculado a um cliente."`
  - `"Um usuário de cliente não pode receber um perfil interno, nem um usuário interno o perfil de cliente."`
  - `"Cliente inválido."`, `"Cliente não encontrado."`, `"O cliente está inativo."`
  - `"Usuário de cliente não pode ser responsável por atividade."`
  - `"Usuário de cliente não pode participar de evento."`
  - `"Usuário ou perfil de cliente não entra em lista de acesso de documento."`
  - `"O perfil de cliente é único e já existe."`
  - `"Acesso negado."` (resposta do portão)

## Review Focus

1. **Cliente com sessão aberta volta à tela de login e entra de novo**: o login funciona; o portão não o trata como requisição interna. (Tarefa 2: `Cliente_com_sessao_aberta_consegue_entrar_de_novo`)
2. **O escritório inativa o cliente enquanto um usuário dele está com o portal aberto**: a próxima requisição HTTP cai no login, o login é recusado, e reativar o cliente devolve o acesso sem mexer no usuário. (Tarefa 2: `Inativar_o_cliente_derruba_a_sessao_aberta_do_usuario_dele`, `Usuario_de_cliente_inativo_nao_entra_e_perde_o_acesso_e_reativar_devolve`)
3. **Um banco que já está em uso sobe com a versão nova**: ganha o perfil "Cliente" uma única vez, mesmo com reinícios, e mesmo que já exista um perfil chamado "cliente". (Tarefa 3: `Banco_anterior_ao_portal_recebe_o_perfil_cliente_na_inicializacao`, `Nome_cliente_ja_usado_vira_cliente_portal`)
4. **A Diretoria envia um Documento Geral depois que o perfil Cliente existe**: o envio continua funcionando e a lista de acesso nasce só com os perfis internos. (Tarefa 2: `Documento_geral_continua_sendo_enviado_e_nasce_so_com_os_perfis_internos`)
5. **Um diretor edita um usuário da equipe e escolhe o perfil "Cliente" (ou o contrário)**: recusado com a mensagem que explica, e nada muda no usuário. (Tarefas 1 e 3: `Usuario_nao_troca_entre_perfil_de_cliente_e_perfil_interno`, `Editar_nao_troca_entre_perfil_de_cliente_e_perfil_interno`)
6. **O cliente digita o endereço de uma página interna**: vê "Página não encontrada", sem nenhum dado e sem que a página chegue a consultar nada. (Tarefa 4: `Cliente_em_pagina_interna_ve_nao_encontrada_e_nenhum_dado`)

---

### Task 1: Tipo Cliente, vínculo do usuário e recusas no domínio

**Files:**
- Modify: `src/AtividadesJuridicas.Domain/Identidade/TipoPerfil.cs`, `Usuario.cs`
- Modify: `src/AtividadesJuridicas.Domain/Atividades/Atividade.cs`
- Modify: `src/AtividadesJuridicas.Domain/Agenda/TiposDoEvento.cs`, `Evento.cs`
- Modify: `src/AtividadesJuridicas.Domain/Documentos/Documento.cs`
- Modify: `src/AtividadesJuridicas.Infrastructure/Persistencia/Configuracoes/UsuarioConfiguracao.cs`
- Create: migration `UsuarioDoCliente` em `src/AtividadesJuridicas.Infrastructure/Persistencia/Migrations/`
- Modify: `tests/AtividadesJuridicas.IntegrationTests/PropriedadesAuditadasTests.cs`
- Test: `tests/AtividadesJuridicas.UnitTests/Domain/UsuarioTests.cs`, `AtividadeTests.cs`, `EventoTests.cs`, `DocumentoTests.cs`; `tests/AtividadesJuridicas.IntegrationTests/PersistenciaTests.cs`

**Interfaces — Produces:**
- `enum TipoPerfil { Diretoria = 1, Producao = 2, Cliente = 3 }`
- `Usuario.ClienteId` (`Guid?`), `Usuario.EhDeCliente` (`bool`, só leitura, não mapeada)
- `Usuario.Criar(string nome, Email email, string senhaHash, Perfil perfil, string? numeroOab = null, Guid? clienteId = null)`
- `Usuario.VincularAoCliente(Guid clienteId): bool` — verdadeiro quando o cliente mudou
- `Usuario.SoDeCliente`, `Usuario.TrocaEntreTipos` (constantes públicas com as mensagens)
- `record PessoaDoEvento(Guid UsuarioId, bool Ativo, bool EhCliente = false)`
- `Documento.ClienteForaDasListas` (constante pública com a mensagem)

**Regras:**

1. **`Usuario`.** O código abaixo é o desenho; as demais partes da classe não mudam.

```csharp
    public const string SoDeCliente = "Só um usuário de perfil Cliente é vinculado a um cliente.";
    public const string TrocaEntreTipos =
        "Um usuário de cliente não pode receber um perfil interno, nem um usuário interno o perfil de cliente.";

    /// <summary>Preenchido se e somente se o perfil do usuário é de tipo Cliente.</summary>
    public Guid? ClienteId { get; private set; }

    /// <summary>Usuário do portal. O vínculo com o cliente é o que diz de que lado o usuário está.</summary>
    public bool EhDeCliente => ClienteId is not null;

    public static Usuario Criar(
        string nome, Email email, string senhaHash, Perfil perfil, string? numeroOab = null, Guid? clienteId = null)
    {
        ArgumentNullException.ThrowIfNull(perfil);
        if (clienteId == Guid.Empty)
            throw new ExcecaoDeDominio("Cliente inválido.");
        if (perfil.Tipo == TipoPerfil.Cliente && clienteId is null)
            throw new ExcecaoDeDominio("Informe o cliente do usuário.");
        if (perfil.Tipo != TipoPerfil.Cliente && clienteId is not null)
            throw new ExcecaoDeDominio(SoDeCliente);

        var usuario = new Usuario { Ativo = true, ClienteId = clienteId };
        usuario.AlterarDados(nome, email, numeroOab);
        usuario.DefinirSenha(senhaHash);
        usuario.AlterarPerfil(perfil);
        return usuario;
    }

    public void AlterarPerfil(Perfil perfil)
    {
        ArgumentNullException.ThrowIfNull(perfil);
        if (!perfil.Ativo)
            throw new ExcecaoDeDominio("Não é possível atribuir um perfil inativo.");

        // Quem é de cliente não vira equipe, e vice-versa.
        if ((perfil.Tipo == TipoPerfil.Cliente) != EhDeCliente)
            throw new ExcecaoDeDominio(TrocaEntreTipos);

        PerfilId = perfil.Id;
    }

    /// <summary>
    /// Troca o cliente de um usuário de cliente. Devolve falso quando já era esse; ao trocar, as sessões
    /// abertas deixam de valer.
    /// </summary>
    public bool VincularAoCliente(Guid clienteId)
    {
        if (!EhDeCliente)
            throw new ExcecaoDeDominio(SoDeCliente);
        if (clienteId == Guid.Empty)
            throw new ExcecaoDeDominio("Cliente inválido.");
        if (ClienteId == clienteId) return false;

        ClienteId = clienteId;
        RenovarSessao();
        return true;
    }
```

   Em `AlterarDados`, a OAB de um usuário de cliente fica sempre nula: troque `NumeroOab = oab;` por `NumeroOab = EhDeCliente ? null : oab;` (a validação do tamanho continua antes).

2. **`Atividade.DefinirResponsaveis`.** Junto das verificações que antecedem qualquer mudança, logo depois da de usuário inativo: se algum dos responsáveis informados tem `Pessoa.Tipo == TipoPerfil.Cliente`, `ExcecaoDeDominio("Usuário de cliente não pode ser responsável por atividade.")`. Vale para todos os informados, não só os novos. O revisor já precisa ser de Diretoria ativa; nada muda nele.

3. **`Evento`.** `PessoaDoEvento` ganha `EhCliente` por último, com padrão falso. Em `Criar`, criador ou participante com `EhCliente`: `ExcecaoDeDominio("Usuário de cliente não pode participar de evento.")`. Em `DefinirParticipantes`, qualquer informado com `EhCliente`: a mesma exceção, antes de qualquer mudança.

4. **`Documento`.**
   - Constante pública `ClienteForaDasListas = "Usuário ou perfil de cliente não entra em lista de acesso de documento."`
   - `CriarGeral`: perfil de tipo `Cliente` entre os informados → `ExcecaoDeDominio(ClienteForaDasListas)`.
   - `DefinirAcesso`: perfil desejado de tipo `Cliente`, ou usuário desejado cujo perfil é de tipo `Cliente` → a mesma exceção, verificada com as demais, antes de qualquer mudança, em qualquer categoria.
   - `PodeSerAcessadoPor`: logo depois da linha da Diretoria, `if (tipoPerfil == TipoPerfil.Cliente) return false;`, com o comentário de que nenhuma das categorias de hoje é do cliente, ainda que ele conste em alguma lista. A assinatura não muda nesta fase.

5. **Persistência.** Em `UsuarioConfiguracao`: `b.HasOne<Cliente>().WithMany().HasForeignKey(u => u.ClienteId).OnDelete(DeleteBehavior.Restrict);` (o índice vem da chave estrangeira). Gere a migration:

```bash
dotnet ef migrations add UsuarioDoCliente --project src/AtividadesJuridicas.Infrastructure --startup-project src/AtividadesJuridicas.Infrastructure --output-dir Persistencia/Migrations
```

   Confira que ela só tem a coluna `cliente_id` (uuid, nula) em `usuarios`, o índice e a chave estrangeira para `clientes` com `Restrict`. Qualquer outra mudança que o EF emitir é sinal de modelo fora de sincronia: pare e registre.

6. **`PropriedadesAuditadasTests`:** `["Usuario"] = ["Ativo", "ClienteId", "Email", "Nome", "NumeroOab", "PerfilId"]`.

**Testes obrigatórios:**

`UsuarioTests` (acrescente; `Novo()` e `Diretor` já existem na classe):

```csharp
    private static readonly Perfil PerfilCliente = Perfil.Criar("Cliente", TipoPerfil.Cliente);
    private static readonly Guid Maria = Guid.CreateVersion7();

    private static Usuario NovoDeCliente() =>
        Usuario.Criar("Carla Cliente", Email.Criar("carla@x.com"), "hash", PerfilCliente, " AL 1 ", Maria);

    [Fact]
    public void Usuario_de_cliente_nasce_vinculado_ao_cliente_e_sem_oab()
    {
        var usuario = NovoDeCliente();

        usuario.ClienteId.Should().Be(Maria);
        usuario.EhDeCliente.Should().BeTrue();
        usuario.PerfilId.Should().Be(PerfilCliente.Id);
        usuario.NumeroOab.Should().BeNull("OAB é da equipe");
        Novo().EhDeCliente.Should().BeFalse();
        Novo().ClienteId.Should().BeNull();
    }

    [Fact]
    public void Perfil_de_cliente_exige_cliente_e_perfil_interno_o_recusa()
    {
        var semCliente = () => Usuario.Criar("Carla", Email.Criar("carla@x.com"), "hash", PerfilCliente);
        var internoComCliente = () => Usuario.Criar("Ana", Email.Criar("ana@x.com"), "hash", Diretor, null, Maria);
        var clienteVazio = () => Usuario.Criar("Carla", Email.Criar("carla@x.com"), "hash", PerfilCliente, null, Guid.Empty);

        semCliente.Should().Throw<ExcecaoDeDominio>().WithMessage("Informe o cliente do usuário.");
        internoComCliente.Should().Throw<ExcecaoDeDominio>().WithMessage(Usuario.SoDeCliente);
        clienteVazio.Should().Throw<ExcecaoDeDominio>().WithMessage("Cliente inválido.");
    }

    [Fact]
    public void Usuario_nao_troca_entre_perfil_de_cliente_e_perfil_interno()
    {
        var deCliente = NovoDeCliente();
        var interno = Novo();

        var paraInterno = () => deCliente.AlterarPerfil(Diretor);
        var paraCliente = () => interno.AlterarPerfil(PerfilCliente);

        paraInterno.Should().Throw<ExcecaoDeDominio>().WithMessage(Usuario.TrocaEntreTipos);
        paraCliente.Should().Throw<ExcecaoDeDominio>().WithMessage(Usuario.TrocaEntreTipos);
        deCliente.PerfilId.Should().Be(PerfilCliente.Id);
        interno.PerfilId.Should().Be(Diretor.Id);
    }

    [Fact]
    public void Trocar_o_cliente_renova_a_sessao_e_manter_o_mesmo_nao()
    {
        var usuario = NovoDeCliente();
        var versao = usuario.VersaoDeSessao;
        var outro = Guid.CreateVersion7();

        usuario.VincularAoCliente(Maria).Should().BeFalse();
        usuario.VersaoDeSessao.Should().Be(versao);

        usuario.VincularAoCliente(outro).Should().BeTrue();
        usuario.ClienteId.Should().Be(outro);
        usuario.VersaoDeSessao.Should().NotBe(versao);
    }

    [Fact]
    public void So_usuario_de_cliente_e_vinculado_e_o_cliente_precisa_ser_valido()
    {
        var interno = () => Novo().VincularAoCliente(Maria);
        var vazio = () => NovoDeCliente().VincularAoCliente(Guid.Empty);

        interno.Should().Throw<ExcecaoDeDominio>().WithMessage(Usuario.SoDeCliente);
        vazio.Should().Throw<ExcecaoDeDominio>().WithMessage("Cliente inválido.");
    }

    [Fact]
    public void Editar_os_dados_de_usuario_de_cliente_mantem_a_oab_nula()
    {
        var usuario = NovoDeCliente();

        usuario.AlterarDados("Carla C.", Email.Criar("carla.c@x.com"), "AL 9");

        usuario.Nome.Should().Be("Carla C.");
        usuario.NumeroOab.Should().BeNull();
    }
```

`AtividadeTests` (use os ajudantes que a classe já tem para montar atividade e pessoas):
- `Usuario_de_cliente_nao_entra_como_responsavel` — em atividade `Criada` e em `Direcionada`; mensagem exata; a lista de responsáveis, o status e `VersaoDasPartes` ficam como estavam.
- `Usuario_de_cliente_nao_e_aceito_como_revisor` — `PessoaDaAtividade` de tipo `Cliente` como revisor informado é recusada (a mensagem é a que já existe para revisor que não é de Diretoria).

`EventoTests`:
- `Usuario_de_cliente_nao_cria_evento_nem_entra_como_participante_na_criacao` — criador com `EhCliente` e participante com `EhCliente`; mensagem exata.
- `Usuario_de_cliente_nao_entra_em_evento_existente_e_nada_muda` — `DefinirParticipantes` com um cliente junto de inclusões e remoções válidas; participantes e `VersaoDosParticipantes` ficam como estavam.

`DocumentoTests`:
- `Documento_geral_nao_nasce_com_perfil_de_cliente` — `CriarGeral` com o perfil Cliente entre os ativos lança `ClienteForaDasListas`.
- `Lista_de_acesso_recusa_perfil_e_usuario_de_cliente_em_qualquer_categoria` — teoria sobre `DocumentoGeral` e `ContratoHonorarios`; perfil de cliente e usuário de perfil de cliente; a lista fica como estava.
- `Perfil_de_cliente_nao_acessa_nenhuma_categoria_interna_mesmo_constando_na_lista` — teoria sobre as três categorias. Para o Documento Geral, ponha o perfil e o usuário de cliente na lista por reflexão sobre os campos privados `_perfisComAcesso` e `_usuariosComAcesso` (o domínio não deixa mais incluí-los) e confira que `PodeSerAcessadoPor(..., TipoPerfil.Cliente)` é falso. Para o documento de atividade, `ehResponsavelDaAtividade: true` também dá falso.

`PersistenciaTests` (integração):
- `Usuario_de_cliente_vai_e_volta_do_banco_com_o_cliente` — grava pelo `AppDbContext` um cliente, um perfil de tipo `Cliente` e um usuário dele; relê em outro escopo; `ClienteId` confere; o registro de `Criacao` de `Usuario` na auditoria traz `ClienteId`.
- `Banco_recusa_usuario_de_cliente_inexistente` — `ClienteId` aleatório falha ao salvar com violação de chave estrangeira.

- [ ] testes → falham → implementar → migration → `dotnet test` inteiro e `dotnet build` (zero avisos) → commit:

```bash
git add src tests
git commit -m "feat(identidade): tipo de perfil Cliente, vínculo do usuário com o cliente e recusas no domínio"
```

---

### Task 2: Portão do cliente, contexto de acesso e login

**Files:**
- Modify: `src/AtividadesJuridicas.Application/Comum/Requisicoes.cs`, `ExecutorDeRequisicoes.cs`
- Modify: `src/AtividadesJuridicas.Application/Identidade/ContextoDeAcesso.cs`, `Portas.cs`, `AutenticarUsuario.cs`, `ObterMeuAcesso.cs`, `AlterarMinhaSenha.cs`
- Modify: `src/AtividadesJuridicas.Application/Documentos/EnviarDocumento.cs`, `src/AtividadesJuridicas.Application/Agenda/SalvarEvento.cs`
- Create: `src/AtividadesJuridicas.Infrastructure/Persistencia/Repositorios/ClientesDaIdentidade.cs`
- Modify: `src/AtividadesJuridicas.Infrastructure/DependencyInjection.cs`
- Modify: `src/AtividadesJuridicas.Web/Infra/ValidacaoDeSessao.cs`
- Modify: `tests/AtividadesJuridicas.UnitTests/Application/Falsos.cs`, `ContextoDeAcessoTests.cs`, `AutenticarUsuarioTests.cs`, `ExecutorDeRequisicoesTests.cs`, `EventosDeSegurancaTests.cs`
- Test: `tests/AtividadesJuridicas.UnitTests/Application/PortaoDoClienteTests.cs` (novo)
- Test: `tests/AtividadesJuridicas.IntegrationTests/PortalDoClienteTests.cs`, `PortalDoClienteWebTests.cs` (novos)

**Interfaces:**
- Consumes (Tarefa 1): `TipoPerfil.Cliente`, `Usuario.ClienteId`, `Usuario.EhDeCliente`, `PessoaDoEvento(..., EhCliente)`.
- Produces:
  - `interface IRequisicaoAbertaAoCliente` (em `Application/Comum/Requisicoes.cs`, sem membros)
  - `record UsuarioAutenticado(Guid Id, string Nome, TipoPerfil TipoPerfil, Guid PerfilId, Guid? ClienteId = null)` com `EhDiretoria` e `EhCliente`
  - `record ClienteDoUsuario(Guid Id, string Nome, bool Ativo)` e `interface IClientesDaIdentidade { Task<ClienteDoUsuario?> ObterAsync(Guid clienteId, CancellationToken ct); }` (em `Application/Identidade/Portas.cs`)
  - `ExecutorDeRequisicoes(IServiceProvider servicos, IAuditoria auditoria, IContextoDeAcesso contexto)`
  - `ContextoDeAcesso(IIdentidadeAtual identidade, IUsuarioRepositorio usuarios, IPerfilRepositorio perfis, IClientesDaIdentidade clientes)`
  - `AutenticarUsuarioManipulador(usuarios, perfis, hasher, relogio, auditoria, IClientesDaIdentidade clientes)`
  - Nos falsos: `ClientesDaIdentidadeFalso`, `Cenario.PerfilCliente`, `Cenario.ClientesDaIdentidade`, `Cenario.NovoCliente(...)`, `Cenario.NovoUsuarioDeCliente(...)`

**Regras:**

1. **Marca.** Em `Requisicoes.cs`:

```csharp
/// <summary>
/// Marca a requisição que um ator de perfil Cliente pode executar. Tudo o que não tem esta marca é negado a ele
/// pelo executor, antes do caso de uso. Marcar mais uma é abrir uma porta do escritório ao cliente: a lista
/// completa está fixada em PortaoDoClienteTests.
/// </summary>
public interface IRequisicaoAbertaAoCliente;
```

   Marcadas nesta fase: `AutenticarUsuario`, `ObterMeuAcesso`, `AlterarMinhaSenha`. Nenhuma outra.

2. **Portão.** `ExecutorDeRequisicoes` fica assim (o bloco de auditoria do meio não muda):

```csharp
public sealed class ExecutorDeRequisicoes(IServiceProvider servicos, IAuditoria auditoria, IContextoDeAcesso contexto)
{
    public async Task<TResposta> ExecutarAsync<TResposta>(IRequisicao<TResposta> requisicao, CancellationToken ct)
    {
        // Portão do cliente: fechado por padrão. Um ator de perfil Cliente só roda o que foi marcado como aberto
        // a ele; para o resto, o caso de uso nem é procurado. Requisição aberta não consulta o ator aqui.
        var barrada = requisicao is not IRequisicaoAbertaAoCliente
                      && await contexto.ObterAsync(ct) is { EhCliente: true };

        try
        {
            var resposta = barrada ? NegarAoCliente(requisicao) : await ManipularAsync(requisicao, ct);

            // ... o registro do acesso negado e o registro da consulta continuam exatamente como estão ...

            return resposta;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            // Invoke embrulha lançamentos síncronos; relança a exceção real, com a pilha original.
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }

    private Task<TResposta> ManipularAsync<TResposta>(IRequisicao<TResposta> requisicao, CancellationToken ct)
    {
        var contrato = typeof(IManipulador<,>).MakeGenericType(requisicao.GetType(), typeof(TResposta));
        var manipulador = servicos.GetRequiredService(contrato);
        return (Task<TResposta>)contrato.GetMethod("ManipularAsync")!.Invoke(manipulador, [requisicao, ct])!;
    }

    /// <summary>A mesma resposta que um caso de uso dá a quem não tem acesso.</summary>
    private static TResposta NegarAoCliente<TResposta>(IRequisicao<TResposta> requisicao)
    {
        const string mensagem = "Acesso negado.";
        var tipo = typeof(TResposta);

        if (tipo == typeof(Resultado))
            return (TResposta)(object)Resultado.Falha(TipoErro.AcessoNegado, mensagem);

        if (tipo.IsGenericType && tipo.GetGenericTypeDefinition() == typeof(Resultado<>))
            return (TResposta)tipo
                .GetMethod(nameof(Resultado.Falha), BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)!
                .Invoke(null, [TipoErro.AcessoNegado, mensagem])!;

        // Requisição que não responde com Resultado tem de ser marcada como aberta: PortaoDoClienteTests cobra isso.
        throw new InvalidOperationException(
            $"{requisicao.GetType().Name} não responde com Resultado e não está aberta ao cliente: o portão não tem como negá-la.");
    }

    // DescartarAsync não muda.
}
```

   A recusa do portão segue pelo bloco que já existe: gera `acesso_negado` com o nome da requisição (e `recursoId` e `detalhe` quando é consulta) e não registra a consulta.

3. **Contexto de acesso.** `UsuarioAutenticado` ganha `ClienteId` e `EhCliente`. `ContextoDeAcesso.ObterAsync`, depois de carregar o perfil:

```csharp
        // O par perfil–cliente incoerente (só possível por edição direta no banco) não tem acesso.
        if ((perfil.Tipo == TipoPerfil.Cliente) != usuario.EhDeCliente) return null;

        // Usuário de cliente só tem acesso enquanto o cliente dele existe e está ativo.
        if (usuario.ClienteId is { } clienteId &&
            await clientes.ObterAsync(clienteId, ct) is not { Ativo: true }) return null;

        return new UsuarioAutenticado(usuario.Id, usuario.Nome, perfil.Tipo, perfil.Id, usuario.ClienteId);
```

   Usuário interno não consulta clientes.

4. **Porta.** Em `Application/Identidade/Portas.cs`:

```csharp
/// <summary>O cliente de um usuário, como a identidade precisa conhecê-lo.</summary>
public sealed record ClienteDoUsuario(Guid Id, string Nome, bool Ativo);

/// <summary>O que a identidade pede ao cadastro de clientes. Implementada na infraestrutura: Identidade não usa tipos de Cadastros.</summary>
public interface IClientesDaIdentidade
{
    /// <summary>Nulo quando o cliente não existe.</summary>
    Task<ClienteDoUsuario?> ObterAsync(Guid clienteId, CancellationToken ct);
}
```

   Implementação `ClientesDaIdentidade(AppDbContext db)`: uma consulta `AsNoTracking` em `db.Clientes` que projeta `Id`, `Nome` e `Ativo`. Registre em `AddInfrastructure`.

5. **Login.** `AutenticarUsuarioManipulador` recebe `IClientesDaIdentidade` por último. Depois de o perfil ser carregado e antes de registrar o login bem-sucedido:

```csharp
        // Usuário de cliente só entra enquanto o cliente dele está ativo. A senha estava certa, mas a resposta é a
        // de credenciais inválidas, e a tentativa continua contada.
        if ((perfil.Tipo == TipoPerfil.Cliente) != usuario.EhDeCliente ||
            (usuario.ClienteId is { } clienteId && await clientes.ObterAsync(clienteId, ct) is not { Ativo: true }))
        {
            await auditoria.RegistrarAsync(EventosDeSeguranca.LoginFalha, "Usuario", usuario.Id,
                new { motivo = "cliente_inativo" }, ator: usuario.Id, ct: ct);
            return Invalido;
        }
```

6. **Cookie.** `ValidacaoDeSessao.ValidarAsync` passa a recusar também o cookie do usuário cujo cliente não está ativo:

```csharp
        var servicos = contexto.HttpContext.RequestServices;
        var usuarios = servicos.GetRequiredService<IUsuarioRepositorio>();
        var ct = contexto.HttpContext.RequestAborted;

        var principal = contexto.Principal;
        var valido =
            principal is not null &&
            IdentidadeHttp.VersaoDeSessao(principal) != Guid.Empty &&
            IdentidadeHttp.UsuarioId(principal) is { } id &&
            await usuarios.ObterPorIdAsync(id, ct) is { Ativo: true } usuario &&
            usuario.VersaoDeSessao == IdentidadeHttp.VersaoDeSessao(principal) &&
            // Usuário de cliente só mantém a sessão enquanto o cliente dele está ativo.
            (usuario.ClienteId is not { } clienteId ||
             await servicos.GetRequiredService<IClientesDaIdentidade>().ObterAsync(clienteId, ct) is { Ativo: true });
```

7. **Guardas da aplicação que acompanham o domínio da Tarefa 1.**
   - `EnviarDocumento`: o Documento Geral nasce com os perfis **internos** ativos: `(await perfis.ObterAtivosAsync(ct)).Where(p => p.Tipo != TipoPerfil.Cliente).ToList()`. Sem isso, o primeiro envio depois de existir o perfil Cliente seria recusado pelo domínio.
   - `SalvarEvento`: os participantes vão como `new PessoaDoEvento(p, encontrados[p].Ativo, encontrados[p].EhDeCliente)` e o criador como `new PessoaDoEvento(ator.Id, true, ator.EhCliente)`. A recusa do domínio já vira `Validacao` pelo `catch` existente.
   - `DefinirResponsaveisDaAtividade`, `SalvarAtividade` e `DefinirAcessoDoDocumento` não mudam: já passam o tipo do perfil ao domínio, e a recusa já vira `Validacao`.

8. **Falsos.** Em `Falsos.cs`:

```csharp
public sealed class ClientesDaIdentidadeFalso(ClienteRepositorioFalso clientes) : IClientesDaIdentidade
{
    public int Consultas { get; private set; }

    public Task<ClienteDoUsuario?> ObterAsync(Guid clienteId, CancellationToken ct)
    {
        Consultas++;
        var cliente = clientes.Itens.FirstOrDefault(c => c.Id == clienteId);
        return Task.FromResult(cliente is null ? null : new ClienteDoUsuario(cliente.Id, cliente.Nome, cliente.Ativo));
    }
}
```

   `Cenario` ganha:

```csharp
    /// <summary>Não entra em <see cref="Perfis"/> sozinho: os testes anteriores ao portal contam dois perfis.</summary>
    public Perfil PerfilCliente { get; } = Perfil.Criar("Cliente", TipoPerfil.Cliente);
    public ClientesDaIdentidadeFalso ClientesDaIdentidade { get; }   // criado no construtor: new(Clientes)

    public Cliente NovoCliente(string nome = "Maria Silva", string cpf = "529.982.247-25")
    {
        var cliente = ClientePessoaFisica.Criar(new DadosDoCliente(nome, null, null, Endereco.Vazio, null), Cpf.Criar(cpf), null, null);
        Clientes.Itens.Add(cliente);
        return cliente;
    }

    /// <summary>Usuário de perfil Cliente; o perfil entra no cenário na primeira vez em que é usado.</summary>
    public Usuario NovoUsuarioDeCliente(string nome, string email, Cliente cliente, string senha = "senha-segura")
    {
        if (!Perfis.Itens.Contains(PerfilCliente)) Perfis.Itens.Add(PerfilCliente);
        var usuario = Usuario.Criar(nome, Email.Criar(email), Hasher.Gerar(senha), PerfilCliente, null, cliente.Id);
        Usuarios.Itens.Add(usuario);
        return usuario;
    }
```

   `Cenario.Como` passa `usuario.ClienteId` como quinto argumento de `UsuarioAutenticado`. CPFs válidos já usados nos testes: `529.982.247-25`, `111.444.777-35`, `390.533.447-05`.

   Os testes que constroem `ExecutorDeRequisicoes`, `ContextoDeAcesso` e `AutenticarUsuarioManipulador` passam a informar o argumento novo: `new ContextoDeAcessoFixo(null)` para o executor (sem ator o portão não age) e `_cenario.ClientesDaIdentidade` para os outros dois.

**Testes obrigatórios:**

`PortaoDoClienteTests` (novo; o arquivo inteiro):

```csharp
using System.Runtime.CompilerServices;
using AtividadesJuridicas.Application;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.UnitTests.Application;

/// <summary>
/// O portão do executor é o que separa o cliente do que é interno. Estes testes percorrem todas as requisições
/// da aplicação: uma requisição nova nasce fechada ao cliente, e abri-la exige mexer na lista fixada aqui.
/// </summary>
public class PortaoDoClienteTests
{
    private static readonly UsuarioAutenticado Cliente =
        new(Guid.CreateVersion7(), "Carla Cliente", TipoPerfil.Cliente, Guid.CreateVersion7(), Guid.CreateVersion7());

    private static IReadOnlyList<(Type Tipo, Type Resposta)> Requisicoes() =>
        [.. typeof(DependencyInjection).Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequisicao<>))
                .Select(i => (t, i.GetGenericArguments()[0])))];

    private static bool Aberta(Type tipo) => typeof(IRequisicaoAbertaAoCliente).IsAssignableFrom(tipo);

    /// <summary>Executa a requisição sem construí-la: o portão decide pelo tipo, antes de olhar o conteúdo.</summary>
    private static async Task<object?> ExecutarAsync(ExecutorDeRequisicoes executor, Type tipo, Type resposta)
    {
        var requisicao = RuntimeHelpers.GetUninitializedObject(tipo);
        var tarefa = (Task)typeof(ExecutorDeRequisicoes).GetMethod(nameof(ExecutorDeRequisicoes.ExecutarAsync))!
            .MakeGenericMethod(resposta).Invoke(executor, [requisicao, CancellationToken.None])!;
        await tarefa;
        return tarefa.GetType().GetProperty("Result")!.GetValue(tarefa);
    }

    [Fact]
    public void As_requisicoes_abertas_ao_cliente_sao_exatamente_estas()
    {
        Requisicoes().Where(r => Aberta(r.Tipo)).Select(r => r.Tipo.Name).Order().Should().Equal(
            ["AlterarMinhaSenha", "AutenticarUsuario", "ObterMeuAcesso"],
            "abrir uma requisição ao cliente é uma decisão de segurança: acrescente-a aqui e na spec do portal");
    }

    [Fact]
    public void Toda_requisicao_fechada_ao_cliente_responde_com_Resultado()
    {
        Requisicoes().Where(r => !Aberta(r.Tipo) && !typeof(Resultado).IsAssignableFrom(r.Resposta))
            .Select(r => r.Tipo.Name).Should().BeEmpty(
                "o portão só sabe negar com Resultado; uma requisição com outra resposta precisa ser aberta ao cliente ou passar a responder com Resultado");
    }

    [Fact]
    public async Task Toda_requisicao_fechada_e_negada_ao_cliente_sem_chegar_ao_caso_de_uso()
    {
        var auditoria = new AuditoriaFalsa();
        // Nenhum manipulador registrado: se o portão deixasse passar, o executor lançaria ao procurar o caso de uso.
        var executor = new ExecutorDeRequisicoes(new ProvedorFalso(), auditoria, new ContextoDeAcessoFixo(Cliente));
        var fechadas = Requisicoes().Where(r => !Aberta(r.Tipo)).ToList();

        fechadas.Should().HaveCountGreaterThan(40, "a aplicação tem dezenas de casos de uso internos");
        foreach (var (tipo, resposta) in fechadas)
        {
            var obtido = await ExecutarAsync(executor, tipo, resposta);

            obtido.Should().BeAssignableTo<Resultado>($"{tipo.Name} deve ser negada com Resultado")
                .Which.Erro.Should().Be(TipoErro.AcessoNegado, $"{tipo.Name} não está aberta ao cliente");
        }

        auditoria.Eventos.Should().HaveCount(fechadas.Count)
            .And.OnlyContain(e => e.Evento == EventosDeSeguranca.AcessoNegado && e.Imediato);
        auditoria.Acessos.Should().BeEmpty("consulta negada não é consulta feita");
    }

    [Fact]
    public async Task Requisicao_aberta_passa_pelo_portao()
    {
        var executor = new ExecutorDeRequisicoes(new ProvedorFalso(), new AuditoriaFalsa(), new ContextoDeAcessoFixo(Cliente));

        foreach (var (tipo, resposta) in Requisicoes().Where(r => Aberta(r.Tipo)))
        {
            var acao = () => ExecutarAsync(executor, tipo, resposta);

            // Passou pelo portão e parou onde se espera: não há manipulador registrado neste teste.
            await acao.Should().ThrowAsync<InvalidOperationException>($"{tipo.Name} deve chegar ao caso de uso");
        }
    }

    private sealed class ContextoQueConta(UsuarioAutenticado? ator) : IContextoDeAcesso
    {
        public int Consultas { get; private set; }

        public Task<UsuarioAutenticado?> ObterAsync(CancellationToken ct)
        {
            Consultas++;
            return Task.FromResult(ator);
        }
    }

    private sealed class ObterMeuAcessoFalso(UsuarioAutenticado? ator) : IManipulador<ObterMeuAcesso, UsuarioAutenticado?>
    {
        public Task<UsuarioAutenticado?> ManipularAsync(ObterMeuAcesso requisicao, CancellationToken ct) => Task.FromResult(ator);
    }

    [Fact]
    public async Task Requisicao_aberta_nao_faz_o_portao_consultar_o_ator()
    {
        var contexto = new ContextoQueConta(Cliente);
        var executor = new ExecutorDeRequisicoes(
            new ProvedorFalso().Com<IManipulador<ObterMeuAcesso, UsuarioAutenticado?>>(new ObterMeuAcessoFalso(Cliente)),
            new AuditoriaFalsa(), contexto);

        (await executor.ExecutarAsync(new ObterMeuAcesso(), default)).Should().Be(Cliente);

        contexto.Consultas.Should().Be(0);
    }

    private sealed class ListarClientesFalso : IManipulador<ListarClientes, Resultado<Pagina<ClienteResumo>>>
    {
        public int Chamadas { get; private set; }

        public Task<Resultado<Pagina<ClienteResumo>>> ManipularAsync(ListarClientes requisicao, CancellationToken ct)
        {
            Chamadas++;
            return Task.FromResult(Resultado<Pagina<ClienteResumo>>.Ok(new Pagina<ClienteResumo>([], 0, 1, 20)));
        }
    }

    public static TheoryData<UsuarioAutenticado?> QuemNaoEBarrado() => new()
    {
        new UsuarioAutenticado(Guid.CreateVersion7(), "Diana", TipoPerfil.Diretoria, Guid.CreateVersion7()),
        new UsuarioAutenticado(Guid.CreateVersion7(), "Pedro", TipoPerfil.Producao, Guid.CreateVersion7()),
        null,
    };

    [Theory]
    [MemberData(nameof(QuemNaoEBarrado))]
    public async Task Diretoria_producao_e_anonimo_nao_sao_barrados_pelo_portao(UsuarioAutenticado? ator)
    {
        var manipulador = new ListarClientesFalso();
        var executor = new ExecutorDeRequisicoes(
            new ProvedorFalso().Com<IManipulador<ListarClientes, Resultado<Pagina<ClienteResumo>>>>(manipulador),
            new AuditoriaFalsa(), new ContextoDeAcessoFixo(ator));

        (await executor.ExecutarAsync(new ListarClientes(), default)).Sucesso.Should().BeTrue();

        manipulador.Chamadas.Should().Be(1, "o portão só fecha para o perfil Cliente; cada caso de uso mantém a sua regra");
    }

    [Fact]
    public async Task A_recusa_do_portao_fica_registrada_com_a_requisicao_e_o_alvo()
    {
        var auditoria = new AuditoriaFalsa();
        var manipulador = new ListarClientesFalso();
        var executor = new ExecutorDeRequisicoes(
            new ProvedorFalso().Com<IManipulador<ListarClientes, Resultado<Pagina<ClienteResumo>>>>(manipulador),
            auditoria, new ContextoDeAcessoFixo(Cliente));
        var pedido = new ListarClientes("Maria", Pagina: 2);

        var resultado = await executor.ExecutarAsync(pedido, default);

        resultado.Erro.Should().Be(TipoErro.AcessoNegado);
        resultado.Mensagem.Should().Be("Acesso negado.");
        resultado.Valor.Should().BeNull();
        manipulador.Chamadas.Should().Be(0);
        var evento = auditoria.Eventos.Should().ContainSingle().Subject;
        evento.Evento.Should().Be(EventosDeSeguranca.AcessoNegado);
        evento.Dados.Should().BeEquivalentTo(new { requisicao = "ListarClientes", recursoId = (Guid?)null, detalhe = pedido.Detalhe });
        auditoria.Acessos.Should().BeEmpty();
    }
}
```

   Se `TheoryData<UsuarioAutenticado?>` não serializar nesta versão do xUnit, troque por três `[Fact]`; o que cada um confere não muda.

`ContextoDeAcessoTests` (acrescente):
- `Usuario_de_cliente_vem_com_o_cliente_e_e_reconhecido_como_cliente` — `EhCliente` verdadeiro, `EhDiretoria` falso, `ClienteId` do cliente.
- `Usuario_de_cliente_inativo_devolve_nulo_e_volta_ao_reativar` — o mesmo contexto, consultado antes, depois de `cliente.Inativar()` e depois de `cliente.Ativar()`.
- `Usuario_de_cliente_que_nao_existe_devolve_nulo` — o cliente é retirado do repositório falso.
- `Par_perfil_cliente_incoerente_devolve_nulo` — por reflexão, um usuário interno recebe `ClienteId` e um usuário de cliente fica sem ele; os dois devolvem nulo.
- `Usuario_interno_nao_consulta_clientes` — `ClientesDaIdentidade.Consultas` fica em zero.

`AutenticarUsuarioTests` (acrescente):
- `Usuario_de_cliente_ativo_autentica` — `SessaoIniciada` com `TipoPerfil.Cliente`.
- `Usuario_de_cliente_inativo_recebe_credenciais_invalidas_mesmo_com_a_senha_certa` — mensagem `"E-mail ou senha inválidos."`; `LoginsRegistrados` em zero; último evento `login_falha` com a entidade do usuário e `Dados` equivalente a `new { motivo = "cliente_inativo" }`; a reserva da tentativa aconteceu (`Reservas` em 1).

`PortalDoClienteTests` (novo, integração, `TesteComBanco`). Ainda não há caso de uso que crie usuário de cliente; o ajudante grava direto:

```csharp
    /// <summary>Até a Tarefa 3 não há caso de uso que crie usuário de cliente: grava direto, reaproveitando o perfil se ele já existir.</summary>
    private Task<Guid> CriarUsuarioDeClienteAsync(Guid clienteId, string nome = "Carla Cliente", string email = "carla@x.com") =>
        EmEscopoAsync(_admin, async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var perfil = await db.Perfis.FirstOrDefaultAsync(p => p.Tipo == TipoPerfil.Cliente);
            if (perfil is null) db.Perfis.Add(perfil = Perfil.Criar("Cliente", TipoPerfil.Cliente));

            var usuario = Usuario.Criar(nome, Email.Criar(email),
                sp.GetRequiredService<IHasherDeSenha>().Gerar(SenhaDaCarla), perfil, null, clienteId);
            db.Usuarios.Add(usuario);
            await db.SaveChangesAsync();
            return usuario.Id;
        });
```

   Monte no `InitializeAsync`: o admin, os clientes Maria e José (`SalvarClientePessoaFisica`), um usuário de Produção (Pedro), a Carla (da Maria), um processo e uma atividade da Maria, um evento, um Documento Geral e um Contrato de Honorários da Maria.

- `Cliente_nao_alcanca_nenhum_caso_de_uso_interno_e_cada_tentativa_fica_registrada` — como a Carla: `ListarClientes`, `ObterCliente` (da própria Maria e do José), `ListarProcessos`, `ObterProcesso`, `ListarAreasAtuacao`, `ListarAtividades`, `ObterAtividade`, `ListarEventos`, `ObterEvento`, `ListarUsuariosParaAgenda`, `ListarDocumentosDoCliente` (Documento Geral da própria Maria), `BaixarDocumento` (o Documento Geral da Maria), `ListarUsuarios`, `ConsultarAuditoria`, `SalvarEvento`, `ComentarAtividade`. Todas respondem `AcessoNegado` com valor nulo. Há um `acesso_negado` por tentativa, com `UsuarioId` da Carla e o nome da requisição em `Dados`. Não há nenhum `RegistroAcesso` da Carla.
- `Cliente_consulta_o_proprio_acesso_e_troca_a_propria_senha` — `ObterMeuAcesso` devolve `EhCliente` e o `ClienteId` da Maria; `AlterarMinhaSenha` funciona; com a senha nova o login passa e com a antiga não.
- `Usuario_de_cliente_inativo_nao_entra_e_perde_o_acesso_e_reativar_devolve` — `DefinirAtivoCliente(maria, false)`: `AutenticarUsuario` responde `AcessoNegado` com `"E-mail ou senha inválidos."` e gera `login_falha` com `motivo` `cliente_inativo`; `ObterMeuAcesso` devolve nulo; `AlterarMinhaSenha` responde `AcessoNegado`. Depois de reativar, login e `ObterMeuAcesso` voltam, sem tocar no usuário.
- `Documento_geral_continua_sendo_enviado_e_nasce_so_com_os_perfis_internos` — com o perfil Cliente já no banco, `EnviarDocumento` de Documento Geral tem sucesso e `PerfisComAcesso` é exatamente Diretor e Produção.
- `Evento_recusa_usuario_de_cliente_como_participante` — `SalvarEvento` pelo admin com a Carla entre os participantes: `Validacao` com a mensagem exata, nada gravado.
- `Atividade_recusa_usuario_de_cliente_como_responsavel` — `DefinirResponsaveisDaAtividade` com a Carla: `Validacao` com a mensagem exata; a atividade fica como estava.
- `Lista_de_acesso_recusa_usuario_e_perfil_de_cliente` — `DefinirAcessoDoDocumento` com o perfil Cliente e, em outra chamada, com a Carla: `Validacao` com a mensagem exata; a lista fica como estava.

`PortalDoClienteWebTests` (novo, `AplicacaoDeTeste`; o mesmo ajudante, sobre `_app.EmEscopoAsync`):
- `Cliente_entra_pela_tela_de_login` — `EntrarAsync` responde redirecionamento e `GET /` responde 200.
- `Cliente_com_sessao_aberta_consegue_entrar_de_novo` — o mesmo `HttpClient` já autenticado como Carla faz o POST de login de novo e recebe redirecionamento, não a tela com erro.
- `Inativar_o_cliente_derruba_a_sessao_aberta_do_usuario_dele` — `GET /` 200; `DefinirAtivoCliente(false)`; `GET /` redireciona para `/conta/entrar`; um login novo responde 200 com `"E-mail ou senha inválidos."`.
- `Cliente_nao_baixa_documento_interno` — `GET /documentos/{id}/baixar` de um Documento Geral do próprio cliente responde 403, e há um `acesso_negado` com `requisicao` `BaixarDocumento`.

- [ ] testes → falham → implementar (Parte A: marca, portão, contexto, login e unitários; Parte B: porta na infraestrutura, cookie, guardas da aplicação e integração) → `dotnet test` inteiro → commit:

```bash
git add src tests
git commit -m "feat(identidade): portão do cliente no executor, contexto de acesso e login pelo cliente ativo"
```

---

### Task 3: Usuários de cliente, perfil Cliente semeado e listas só da equipe

**Files:**
- Modify: `src/AtividadesJuridicas.Application/Identidade/CriarUsuario.cs`, `EditarUsuario.cs`, `SalvarPerfil.cs`, `Portas.cs`
- Create: `src/AtividadesJuridicas.Application/Identidade/ListarUsuariosDoCliente.cs`
- Modify: `src/AtividadesJuridicas.Application/Auditoria/IAuditoria.cs` (evento)
- Modify: `src/AtividadesJuridicas.Infrastructure/Persistencia/Repositorios/UsuarioRepositorio.cs`, `src/AtividadesJuridicas.Infrastructure/Persistencia/SemeadorInicial.cs`
- Modify: `tests/AtividadesJuridicas.UnitTests/Application/Falsos.cs`, `GestaoDeUsuariosTests.cs`, `PerfisTests.cs`, `AgendaTests.cs`
- Modify: `tests/AtividadesJuridicas.IntegrationTests/PersistenciaTests.cs`, `PerfisTests.cs`, `AuditoriaDeAlteracoesTests.cs`, `PortalDoClienteTests.cs`, `PortalDoClienteWebTests.cs`, `AplicacaoDeTeste.cs`

**Interfaces:**
- Consumes (Tarefas 1 e 2): `Usuario.Criar(..., clienteId)`, `Usuario.VincularAoCliente`, `Usuario.EhDeCliente`, `Usuario.SoDeCliente`, `Usuario.TrocaEntreTipos`, `IClientesDaIdentidade`, `ClienteDoUsuario`.
- Produces:
  - `record CriarUsuario(string Nome, string Email, string Senha, Guid PerfilId, string? NumeroOab, Guid? ClienteId = null) : IRequisicao<Resultado<Guid>>`
  - `record EditarUsuario(Guid Id, string Nome, string Email, Guid PerfilId, string? NumeroOab, Guid? ClienteId = null) : IRequisicao<Resultado>`
  - `CriarUsuarioManipulador(contexto, usuarios, perfis, hasher, unidade, IClientesDaIdentidade clientes)`; `EditarUsuarioManipulador(contexto, usuarios, perfis, unidade, auditoria, IClientesDaIdentidade clientes)`
  - `record UsuarioResumo(..., DateTime? UltimoAcessoEm, Guid? ClienteId = null, string? ClienteNome = null)`
  - `record UsuarioDoCliente(Guid Id, string Nome, string Email, bool Ativo, DateTime? UltimoAcessoEm)`
  - `record ListarUsuariosDoCliente(Guid ClienteId) : IRequisicao<Resultado<IReadOnlyList<UsuarioDoCliente>>>, IConsultaAuditavel` (`TipoRecurso = "Usuario"`, `Listou`, `Detalhe` com o `ClienteId`)
  - `IUsuarioRepositorio.ListarDoClienteAsync(Guid clienteId, CancellationToken ct): Task<IReadOnlyList<UsuarioDoCliente>>`
  - `EventosDeSeguranca.UsuarioClienteAlterado = "usuario_cliente_alterado"`
  - `AplicacaoDeTeste.CriarCarlaAsync(): Task<(Guid UsuarioId, Guid ClienteId)>`, com as constantes `EmailDaCarla = "carla@x.com"` e `SenhaDaCarla = "senha-da-carla"`

**Regras:**

1. **`CriarUsuario`.** Só Diretoria, como hoje. Depois de carregar o perfil:
   - perfil de tipo `Cliente` sem `ClienteId` → `Validacao` `"Informe o cliente do usuário."`;
   - perfil de tipo `Cliente` com cliente que não existe → `Validacao` `"Cliente não encontrado."`; inativo → `Validacao` `"O cliente está inativo."`;
   - perfil interno com `ClienteId` → `Validacao` com `Usuario.SoDeCliente`;
   - a OAB informada para um usuário de cliente é ignorada.

2. **`EditarUsuario`.** Antes de qualquer alteração e de qualquer anotação de auditoria:
   - `(perfil.Tipo == TipoPerfil.Cliente) != usuario.EhDeCliente` → `Validacao` com `Usuario.TrocaEntreTipos`;
   - usuário interno com `ClienteId` informado → `Validacao` com `Usuario.SoDeCliente`;
   - usuário de cliente sem `ClienteId` → `Validacao` `"Informe o cliente do usuário."`;
   - usuário de cliente com `ClienteId` diferente do atual: o cliente novo precisa existir (`"Cliente não encontrado."`) e estar ativo (`"O cliente está inativo."`). O mesmo cliente não é revalidado.

   Na gravação, quando `usuario.VincularAoCliente(novo)` devolve verdadeiro: `auditoria.Anotar(EventosDeSeguranca.UsuarioClienteAlterado, "Usuario", usuario.Id, new { de = anterior, para = novo })`, na mesma transação. A regra do último diretor não muda.

3. **`ListarUsuarios`.** O manipulador não muda. `UsuarioRepositorio.ListarAsync` passa a trazer `ClienteId` e, por junção externa com `clientes`, `ClienteNome` (nulo para a equipe; o nome vem mesmo com o cliente inativo).

4. **`ListarUsuariosDoCliente`.** Só Diretoria (`AcessoNegado`). Cliente que não existe (`IClientesDaIdentidade`) → `NaoEncontrado` `"Cliente não encontrado."`. Devolve os usuários daquele cliente, ativos e inativos, por nome.

5. **`ListarNomesDosAtivosAsync`.** Só a equipe: `u.Ativo && u.ClienteId == null`. Atualize o comentário da porta. `ListarUsuariosParaAgenda` não muda.

6. **`SalvarPerfil`.** Criar com `Tipo == TipoPerfil.Cliente` → `Validacao` `"O perfil de cliente é único e já existe."`, antes da verificação do nome. Renomear o perfil Cliente continua permitido (o tipo informado é o do perfil).

7. **Semeador.** O perfil Cliente é garantido a cada execução:

```csharp
    public async Task ExecutarAsync(AdminInicialOpcoes? admin, CancellationToken ct = default)
    {
        await db.Database.MigrateAsync(ct);

        if (!await db.Perfis.IgnoreQueryFilters().AnyAsync(ct))
        {
            if (admin is null)
                throw new InvalidOperationException(
                    "Banco vazio: configure AdminInicial:Nome, AdminInicial:Email e AdminInicial:Senha para criar o primeiro diretor.");
            if (PoliticaDeSenha.Validar(admin.Senha) is { } erro)
                throw new InvalidOperationException("AdminInicial:Senha inválida. " + erro);

            var diretor = Perfil.Criar("Diretor", TipoPerfil.Diretoria);
            var producao = Perfil.Criar("Produção", TipoPerfil.Producao);
            var usuario = Usuario.Criar(admin.Nome, Email.Criar(admin.Email), hasher.Gerar(admin.Senha), diretor);

            db.AddRange(diretor, producao, usuario);
        }

        // Banco anterior ao portal do cliente não tem o perfil; em banco novo ele nasce junto dos outros dois.
        if (!await db.Perfis.IgnoreQueryFilters().AnyAsync(p => p.Tipo == TipoPerfil.Cliente, ct))
        {
            var nomeEmUso = await db.Perfis.AnyAsync(p => p.Nome.ToLower() == "cliente", ct);
            db.Add(Perfil.Criar(nomeEmUso ? "Cliente (portal)" : "Cliente", TipoPerfil.Cliente));
        }

        await db.SaveChangesAsync(ct);
    }
```

   Atualize o comentário da classe: o semeador cria os perfis padrão e o primeiro diretor em banco vazio e garante o perfil Cliente em qualquer banco.

8. **Falsos.** `UsuarioRepositorioFalso`: `ListarAsync` preenche `ClienteId` e `ClienteNome` (o falso recebe o `ClienteRepositorioFalso` por último no construtor; ajuste o `Cenario`); `ListarDoClienteAsync`; `ListarNomesDosAtivosAsync` sem usuários de cliente.

9. **Ajudantes de teste.** Em `PortalDoClienteTests` e `PortalDoClienteWebTests`, troque o corpo de `CriarUsuarioDeClienteAsync` pelo caso de uso `CriarUsuario` com o perfil de tipo `Cliente` semeado. Em `AplicacaoDeTeste`, `CriarCarlaAsync` cria a cliente "Maria Silva" (CPF `529.982.247-25`) e a usuária "Carla Cliente" pelos casos de uso.

10. **Testes existentes que mudam porque agora há três perfis.** Atualize o número ou a lista, sem tirar a asserção, e relate cada um:
    - `PersistenciaTests.Semeador_cria_os_dois_perfis_e_o_administrador` → renomeie para `Semeador_cria_os_tres_perfis_e_o_administrador`; a lista passa a `("Cliente", Cliente), ("Diretor", Diretoria), ("Produção", Producao)`.
    - `PersistenciaTests.Semeador_executado_de_novo_nao_duplica` → `(3, 1)`.
    - Onde um teste pega um perfil qualquer com `db.Perfis.FirstAsync()` para criar um usuário (`PersistenciaTests`, `AuditoriaDeAlteracoesTests`), peça um perfil de tipo `Producao`: com o perfil Cliente o domínio exigiria um cliente.
    - `PerfisTests` (integração): as listas e contagens de perfis ganham o "Cliente".
    - Qualquer outro teste que conte perfis, ou registros de auditoria da semeadura, e falhar.
    - As listas de acesso de Documento Geral (`[_perfilDiretor, _perfilProducao]`) **não** mudam: se um desses testes falhar, o erro é da implementação.

**Testes obrigatórios:**

`GestaoDeUsuariosTests` (acrescente; os ajudantes `Criar` e `Editar` passam a informar `_c.ClientesDaIdentidade`):
- `Diretor_cria_usuario_de_cliente_vinculado_ao_cliente_e_sem_oab`
- `Perfil_de_cliente_sem_cliente_com_cliente_inexistente_ou_inativo_e_recusado` — as três mensagens; nada gravado.
- `Perfil_interno_com_cliente_e_recusado`
- `Editar_nao_troca_entre_perfil_de_cliente_e_perfil_interno` — nos dois sentidos; perfil, cliente e sessão como estavam; nenhum evento; `Salvamentos` em zero.
- `Trocar_o_cliente_do_usuario_gera_evento_e_renova_a_sessao` — evento `usuario_cliente_alterado`, não imediato, entidade `Usuario`, `Dados` equivalente a `new { de, para }`.
- `Manter_o_cliente_nao_gera_evento_nem_renova_a_sessao_mesmo_com_o_cliente_inativo` — nome e e-mail mudam.
- `Trocar_para_cliente_inexistente_ou_inativo_e_recusado`
- `Editar_usuario_de_cliente_sem_cliente_e_recusado` e `Editar_usuario_interno_com_cliente_e_recusado`
- `Lista_de_usuarios_traz_o_cliente_de_quem_e_de_cliente`
- `Usuarios_do_cliente_so_para_a_diretoria_e_so_os_daquele_cliente` — Produção recebe `AcessoNegado`; cliente inexistente, `NaoEncontrado`; ativos e inativos vêm, por nome; os de outro cliente e os da equipe não vêm; a consulta é auditável.

`PerfisTests` (unitário): `Criar_perfil_de_tipo_cliente_e_recusado` (mensagem exata, nenhum perfil criado, nenhuma inclusão em documentos) e `Perfil_de_cliente_pode_ser_renomeado`.

`AgendaTests` (unitário): `Lista_de_usuarios_para_a_agenda_nao_traz_usuario_de_cliente`.

`PersistenciaTests` (integração):
- `Banco_anterior_ao_portal_recebe_o_perfil_cliente_na_inicializacao` — apague a linha do perfil Cliente (`ExecuteDeleteAsync` no teste), rode o semeador duas vezes: há exatamente um perfil de tipo `Cliente`, chamado "Cliente", com registro de `Criacao` na auditoria e `UsuarioId` nulo; o admin e os outros perfis não foram tocados.
- `Nome_cliente_ja_usado_vira_cliente_portal` — sem o perfil Cliente e com um perfil de Produção chamado "cliente", o semeador cria "Cliente (portal)".

`PortalDoClienteTests` (integração, acrescente):
- `Usuario_de_cliente_e_criado_pelo_caso_de_uso_e_aparece_na_lista_do_cliente` — `ListarUsuarios` traz o nome da cliente; `ListarUsuariosDoCliente` traz a Carla; o registro de consulta é gravado.
- `Trocar_o_cliente_derruba_a_sessao_e_fica_auditado` — `VersaoDeSessao` muda no banco; há um `usuario_cliente_alterado` com `de` e `para`, com o mesmo `IdCorrelacao` do registro de alteração do usuário; depois da troca `ObterMeuAcesso` traz o cliente novo.
- `Usuario_de_cliente_nao_conta_como_diretor_nem_aparece_para_a_agenda` — com um só diretor e um usuário de cliente ativo, inativar o diretor continua recusado; `ListarUsuariosParaAgenda` não traz a Carla.
- `Criar_perfil_de_tipo_cliente_e_recusado_no_banco` — continua havendo um único perfil de tipo `Cliente`.

`PortalDoClienteWebTests`: `Trocar_o_cliente_derruba_a_sessao_web` — `GET /` 200; `EditarUsuario` com outro cliente; `GET /` redireciona.

- [ ] testes → falham → implementar → `dotnet test` inteiro → commit:

```bash
git add src tests
git commit -m "feat(identidade): usuários de cliente, perfil Cliente semeado e listas só da equipe"
```

---

### Task 4: Telas — usuários de cliente, menu do portal e páginas internas fechadas

**Files:**
- Create: `src/AtividadesJuridicas.Web/Infra/RotulosDePerfil.cs`, `src/AtividadesJuridicas.Web/Infra/RotasDoCliente.cs`
- Modify: `src/AtividadesJuridicas.Web/Components/Layout/MainLayout.razor`
- Modify: `src/AtividadesJuridicas.Web/Components/Pages/Home.razor`, `Usuarios.razor`, `Perfis.razor`, `Clientes.razor`, `Atividades.razor`, `Agenda.razor`
- Modify: `src/AtividadesJuridicas.Web/Components/Documentos/DocumentosDoCliente.razor`, `src/AtividadesJuridicas.Web/Components/Atividades/EditorDeResponsaveis.razor`
- Test: `tests/AtividadesJuridicas.IntegrationTests/PortalDoClienteWebTests.cs`, `TelasPurasTests.cs`, `PaginasProtegidasTests.cs`

**Interfaces:**
- Consumes (Tarefas 2 e 3): `UsuarioAutenticado.EhCliente`, `UsuarioResumo.ClienteId`, `UsuarioResumo.ClienteNome`, `CriarUsuario(..., ClienteId)`, `EditarUsuario(..., ClienteId)`, `ListarUsuariosDoCliente`, `UsuarioDoCliente`, `AplicacaoDeTeste.CriarCarlaAsync`.
- Produces: `RotulosDePerfil.DoTipo(TipoPerfil): string`; `RotasDoCliente.Permite(string caminhoRelativo): bool`.

**O que as telas fazem:**

1. **Funções puras** (em `Web/Infra`, testadas em `TelasPurasTests`):

```csharp
public static class RotulosDePerfil
{
    public static string DoTipo(TipoPerfil tipo) => tipo switch
    {
        TipoPerfil.Diretoria => "Diretoria",
        TipoPerfil.Producao => "Produção",
        TipoPerfil.Cliente => "Cliente",
        _ => tipo.ToString(),
    };
}

/// <summary>As páginas que um usuário de perfil Cliente pode abrir. Fechado por padrão: página nova é interna.</summary>
public static class RotasDoCliente
{
    // A fase 8B acrescenta andamento, meus-documentos e documentos-comuns.
    private static readonly HashSet<string> Abertas =
        new(StringComparer.OrdinalIgnoreCase) { "", "minha-senha", "nao-encontrada" };

    /// <summary><paramref name="caminhoRelativo"/>: o endereço sem a base, como em <c>NavigationManager.ToBaseRelativePath</c>.</summary>
    public static bool Permite(string caminhoRelativo) =>
        Abertas.Contains(caminhoRelativo.Split('?', '#')[0].Trim('/'));
}
```

   Todos os lugares que hoje escrevem `tipo == TipoPerfil.Diretoria ? "Diretoria" : "Produção"` (`Usuarios.razor`, `Perfis.razor`, `Home.razor`) passam a usar `RotulosDePerfil.DoTipo`.

2. **`MainLayout.razor`.** O corpo só é montado depois de se saber quem é o usuário, e não é montado para o cliente fora das rotas dele:

```razor
@inherits LayoutComponentBase
@using AtividadesJuridicas.Web.Components.Pages
@inject IDespachante Despachante
@inject NavigationManager Navegacao

<header class="topo">
    <a class="marca" href="/">Atividades Jurídicas</a>
    <nav>
        @if (_carregado)
        {
            <NavLink href="/" Match="NavLinkMatch.All">Início</NavLink>
            @if (_ator is not { EhCliente: true })
            {
                <NavLink href="/atividades">Atividades</NavLink>
                <NavLink href="/agenda">Agenda</NavLink>
                <NavLink href="/clientes">Clientes</NavLink>
                <NavLink href="/processos">Processos</NavLink>
                @if (_ator is { EhDiretoria: true })
                {
                    <NavLink href="/areas">Áreas</NavLink>
                    <NavLink href="/usuarios">Usuários</NavLink>
                    <NavLink href="/perfis">Perfis</NavLink>
                    <NavLink href="/auditoria">Auditoria</NavLink>
                }
            }
            <NavLink href="/minha-senha">Minha senha</NavLink>
        }
    </nav>
    <span class="quem">@_ator?.Nome</span>
    <form method="post" action="/conta/sair">
        <AntiforgeryToken />
        <button type="submit">Sair</button>
    </form>
</header>

<main class="conteudo">
    @if (!_carregado)
    {
        <p>Carregando…</p>
    }
    else if (_ator is { EhCliente: true } && !RotasDoCliente.Permite(Navegacao.ToBaseRelativePath(Navegacao.Uri)))
    {
        @* A página interna não é montada para o cliente: ela não chega a consultar nada. *@
        <NaoEncontrada />
    }
    else
    {
        @Body
    }
</main>

@code {
    private UsuarioAutenticado? _ator;
    private bool _carregado;

    protected override async Task OnInitializedAsync()
    {
        _ator = await Despachante.EnviarAsync(new ObterMeuAcesso());
        _carregado = true;
    }
}
```

   O HTML pré-renderizado continua completo: o servidor espera a consulta do layout e depois a da página. Rode a suíte de páginas inteira para confirmar que nenhum teste de página passou a ver "Carregando…".

3. **`Home.razor`.** Para o cliente: a saudação ("Olá, **nome**.") e o link "Minha senha". Sem os links de atividades e de agenda e sem a frase do perfil. Para os demais, como hoje, com o tipo vindo de `RotulosDePerfil.DoTipo`.

4. **`Usuarios.razor`.**
   - Tabela: coluna "Cliente" depois de "Perfil", com `ClienteNome` (vazia para a equipe). O rótulo do perfil usa `RotulosDePerfil.DoTipo`.
   - Formulário: quando o perfil escolhido é de tipo `Cliente`, o campo da OAB some e aparece o bloco "Cliente": campo de busca e botão "Buscar" (`ListarClientes`, primeira página), a lista dos encontrados com "Escolher", e, com um cliente escolhido, "Cliente: nome" e o botão "Trocar". Siga a busca de cliente do `FormularioDeAtividade.razor`: Enter no campo de busca não envia o formulário do usuário.
   - Ao editar um usuário de cliente, o cliente atual já vem escolhido (`ClienteId` e `ClienteNome` da linha).
   - Opções de perfil: em usuário novo, todos os perfis ativos; ao editar um usuário de cliente, só os de tipo `Cliente`; ao editar um usuário da equipe, só os internos. O perfil inativo atual continua aparecendo, como hoje.
   - Salvar: perfil de tipo `Cliente` sem cliente escolhido mostra "Informe o cliente do usuário." sem enviar. `CriarUsuario` e `EditarUsuario` recebem o `ClienteId` (nulo para a equipe).
   - Usuário novo continua começando em um perfil de Produção.

5. **`Perfis.razor`.** `NomeDoTipo` usa `RotulosDePerfil.DoTipo`. O seletor de tipo do perfil novo continua só com Produção e Diretoria. O perfil Cliente aparece na tabela e pode ser renomeado.

6. **`Clientes.razor`.** No formulário de edição da Diretoria, depois de "Processos do cliente", a seção "Usuários do cliente": carregada com `ListarUsuariosDoCliente` ao abrir a edição; tabela com Nome, E-mail, Situação e Último acesso; vazia mostra "Nenhum usuário. Crie o acesso do cliente em Usuários."; falha na consulta mostra o erro no lugar da lista, nunca a lista vazia; acesso negado encerra a tela como as outras consultas da página. Não aparece na ficha de leitura da Produção.

7. **Telas que escolhem gente da equipe** deixam de listar usuários de cliente (`u.TipoPerfil != TipoPerfil.Cliente`):
   - `EditorDeResponsaveis.razor`: os usuários que podem ser incluídos;
   - `Atividades.razor`: o filtro "Responsável";
   - `Agenda.razor`: o filtro de participante;
   - `DocumentosDoCliente.razor`, editor "Quem acessa": os perfis oferecidos (os usuários já são só os de Produção).

   `Auditoria.razor` não muda: o filtro de usuário continua com todos, inclusive os de cliente.

**Testes obrigatórios:**

`TelasPurasTests`:
- `RotasDoClienteTests.Rotas_do_portal_sao_permitidas` — teoria: `""`, `"/"`, `"minha-senha"`, `"minha-senha/"`, `"Minha-Senha"`, `"minha-senha?x=1"`, `"minha-senha#topo"`, `"nao-encontrada"`.
- `RotasDoClienteTests.Paginas_internas_e_desconhecidas_nao_sao_permitidas` — teoria: `"clientes"`, `"processos"`, `"atividades"`, `"agenda"`, `"areas"`, `"usuarios"`, `"perfis"`, `"auditoria"`, `"produtores"`, `"minha-senha/outra"`, `"qualquer-coisa"`.
- `RotulosDePerfilTests.Cada_tipo_tem_o_seu_rotulo`.

`PortalDoClienteWebTests` (HTML pré-renderizado, com `CriarCarlaAsync`):
- `Cliente_ve_o_inicio_com_o_menu_do_portal` — contém "Carla Cliente", `href="/minha-senha"` e "Sair"; não contém `href="/atividades"`, `href="/agenda"`, `href="/clientes"`, `href="/processos"`, `href="/areas"`, `href="/usuarios"`, `href="/perfis"`, `href="/auditoria"`, nem "Carregando".
- `Cliente_em_pagina_interna_ve_nao_encontrada_e_nenhum_dado` — teoria sobre `/clientes`, `/processos`, `/atividades`, `/agenda`, `/areas`, `/usuarios`, `/perfis`, `/auditoria`: contém "Página não encontrada"; não contém "Maria Silva", "diana@x.com" nem "Acesso negado"; a quantidade de eventos `acesso_negado` e de registros de consulta da Carla é a mesma de antes da visita.
- `Cliente_abre_a_troca_de_senha` — `/minha-senha` contém "Senha atual".
- `Diretoria_e_producao_continuam_abrindo_as_paginas_internas` — o admin em `/clientes` e o Pedro em `/atividades`: sem "Página não encontrada" e sem "Carregando".
- `Cadastro_de_usuarios_mostra_o_cliente_e_o_tipo_cliente` — `/usuarios` contém "Cliente (Cliente)", `<th>Cliente</th>` e, na linha da Carla, "Maria Silva".
- `Tela_de_perfis_mostra_o_perfil_cliente_e_nao_o_oferece_ao_criar` — `/perfis` contém `<td>Cliente</td>` na linha do perfil; o `<select>` de tipo não tem a opção "Cliente".

`PaginasProtegidasTests.Cadastro_de_usuarios_mostra_o_tipo_do_perfil_e_marca_o_perfil_inativo`: acrescente que o `<select>` contém `>Cliente (Cliente)</option>`. O restante do teste continua valendo, inclusive a única opção selecionada ser a de Produção.

O comportamento interativo não tem teste automatizado. Descreva no relatório, caminho por caminho, como verificou por leitura:
- escolher o perfil Cliente mostra a busca e esconde a OAB; voltar a um perfil interno desfaz as duas coisas e limpa o cliente escolhido;
- Enter na busca de cliente não salva o usuário;
- editar um usuário de cliente traz o cliente e restringe os perfis; editar um usuário da equipe não oferece o perfil Cliente;
- a seção "Usuários do cliente" aparece só na edição da Diretoria, e a falha da consulta não vira lista vazia;
- navegar pelo menu, dentro do circuito já aberto, de uma rota do portal para uma interna (endereço digitado) troca o corpo pelo "não encontrada".

- [ ] testes → falham → implementar → `dotnet test` inteiro → commit:

```bash
git add src tests
git commit -m "feat(web): usuários de cliente, menu do portal e páginas internas fechadas ao cliente"
```

---

### Task 5: Emendar a modelagem de domínio

**Files:**
- Modify: `docs/superpowers/specs/2026-10-06-modelagem-dominio-design.md`

Emende só o que esta fase fixou; o que é da 8B (categorias de documento, Andamento, páginas do portal) fica para a emenda da 8B.

- [ ] **Step 1: Seção 5 — Identidade**
  - `Perfil`: `Tipo` passa a `Diretoria`, `Producao` ou `Cliente`; existe um único perfil de tipo `Cliente`, garantido pelo semeador, que não pode ser criado pela tela; pode ser renomeado.
  - `Usuario`: campo `ClienteId` (preenchido se e somente se o perfil é de tipo `Cliente`); o usuário não troca entre o tipo `Cliente` e os internos; a troca de cliente renova a sessão e gera `usuario_cliente_alterado`; usuário de cliente não é responsável, revisor, participante nem entra em lista de acesso; o nome dos usuários ativos visível a todos passa a ser o dos usuários ativos da equipe.
  - "Autenticação e autorização": o portão do executor, as três requisições abertas e a regra do cliente inativo (login, cookie e operação seguinte); o par perfil–cliente incoerente não tem acesso.
  - Matriz de acesso: coluna "Cliente", com "Sem acesso" em todas as linhas existentes.

- [ ] **Step 2: Seções 7, 8 e 9**
  - Seção 7: o Documento Geral nasce liberado para todos os perfis **internos** ativos; a regra de acesso ganha o item "perfil de tipo `Cliente` não acessa nenhuma destas categorias, ainda que conste em lista"; o domínio recusa perfil e usuário de cliente em qualquer lista.
  - Seção 8: usuário de cliente não entra como responsável.
  - Seção 9: usuário de cliente não participa de evento nem aparece na escolha de participantes.

- [ ] **Step 3: Seções 10, 12 e 13**
  - Seção 10: o evento `usuario_cliente_alterado`; a recusa do portão gera `acesso_negado` com o nome da requisição; `login_falha` com `motivo` `cliente_inativo`.
  - Seção 12: o teste que percorre todas as requisições e a lista fixada das abertas ao cliente.
  - Seção 13: "portal do cliente" sai da lista; no lugar, uma frase dizendo que os documentos e o andamento do portal estão em `2026-10-08-portal-do-cliente-design.md`, fase 8B.

- [ ] **Step 4: Reler e conferir**

Releia as seções alteradas contra o código: cada regra escrita tem um teste, e nenhuma frase antiga da modelagem ficou contradizendo a nova (procure por "todos os perfis ativos", "qualquer usuário autenticado" e "`Diretoria` ou `Producao`").

- [ ] **Step 5: Commit**

```bash
git add docs/superpowers/specs/2026-10-06-modelagem-dominio-design.md
git commit -m "docs(spec): regras do perfil Cliente e do portão fixadas na fase 8A"
```
