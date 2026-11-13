# Solicitações do cliente — Fase A: a solicitação: Plano de Implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** O cliente abre, edita e cancela solicitações pelo portal, com descrição, urgência e documentos; a Diretoria lista todas, da mais antiga para a mais nova, e lê cada uma com os documentos e quem a abriu.

**Architecture:** Um agregado novo, `SolicitacaoDoCliente`, num contexto novo (`Solicitacoes`) no domínio e na aplicação. Os anexos são uma sexta categoria do agregado `Documento`, sem lista de acesso, que só a Diretoria e os usuários do próprio cliente alcançam. As requisições do portal usam o `ClienteId` do ator e entram na lista do portão. O vínculo com a atividade (`AtividadeId`) já existe no domínio e no banco nesta fase, mas só a Fase B o preenche.

**Tech Stack:** .NET 10, Blazor Interactive Server, EF Core 10 + Npgsql, PostgreSQL 17, xUnit 2.9, FluentAssertions 7.2, Testcontainers.

**Spec:** `docs/superpowers/specs/2026-11-13-solicitacoes-do-cliente-design.md` (seções 1 a 4, 6 a 9, e a Fase A da seção 10). A Fase B (criar atividade a partir da solicitação, `Atividade.SolicitacaoId`, índice da chave de armazenamento) fica para outro plano.

## Decisões deste plano

- **Contexto `Solicitacoes` na aplicação** usa `Identidade`, `Cadastros`, `Documentos` e `Atividades` (para a situação simplificada do andamento). Nenhum outro contexto usa `Solicitacoes`.
- **A situação vista pelo cliente é um tipo da aplicação**, `SituacaoParaOCliente { AguardandoAnalise = 1, Cancelada = 2, EmAndamento = 3, Concluida = 4 }`. As duas últimas vêm da atividade viva vinculada, por `Andamento.SituacaoDe`. Sem atividade viva vinculada, vale a situação do domínio.
- **Até 10 documentos vivos por solicitação** (`SolicitacoesOpcoes.MaximoDeDocumentos = 10`), contados na abertura e a cada anexo. O limite por arquivo é o de hoje (50 MB).
- **A abertura é uma gravação só.** Os arquivos são gravados no armazenamento um a um; se qualquer um for recusado ou a gravação no banco falhar, todos os arquivos já gravados são removidos e nada fica no banco. A lógica entra em `ArquivoEnviado`, ao lado da que já existe para um arquivo.
- **Mudança nos documentos mexe na versão da solicitação.** `SolicitacaoDoCliente.VersaoDosDocumentos` (inteiro, `[NaoAuditar]`) aumenta em todo anexo e remoção, como `VersaoDasPartes` na atividade: a linha da solicitação é regravada e o `xmin` muda.
- **Quem não abriu a solicitação recebe `Validacao`**, e não `AcessoNegado`, ao tentar alterá-la: nas telas, `AcessoNegado` significa "acesso encerrado".
- **Rotas do portal com Id.** `RotasDoCliente` passa a aceitar, além dos nomes fixos, o padrão `minhas-solicitacoes/{guid}` e `minhas-solicitacoes/{guid}/editar`, nas duas guardas (aberta ao cliente e só do portal).
- **A lista da Diretoria abre filtrada em "Aguardando"**; a requisição sem filtro traz todas. A ordem é sempre da mais antiga para a mais nova.
- **Na Fase A a ficha da Diretoria não tem "Criar atividade".** O botão entra na Fase B. Quando há atividade vinculada (só possível depois da Fase B), a ficha mostra o título dela.
- **O cliente vê o nome de quem abriu cada solicitação** (todos os usuários do cliente veem todas as solicitações do cliente). Não vê nome de quem enviou documento.
- **Trecho da descrição nas listas:** os primeiros 140 caracteres, com "…" quando cortado (`SolicitacoesOpcoes.TamanhoDoTrecho = 140`).

## Estado de partida

- Worktree `.claude/worktrees/solicitacoes-do-cliente`, branch `solicitacoes-do-cliente`, a partir de `origin/main` em `1276d89`. O spec ainda não tem commit: o primeiro commit desta fase o inclui (ver "Commits", abaixo).
- `CategoriaDocumento` vai de 1 a 5; `Documento.VisivelAoCliente` vale para 4 e 5; `Documento.PodeSerAcessadoPor(..., Guid? clienteDoUsuario)`.
- `ArquivoEnviado.Validar` e `ArquivoEnviado.GravarAsync` (um arquivo) em `Application/Documentos/ArquivoEnviado.cs`.
- Portão: `IRequisicaoAbertaAoCliente`; lista fixada em `PortaoDoClienteTests.As_requisicoes_abertas_ao_cliente_sao_exatamente_estas` (sete hoje).
- Manipuladores são registrados por varredura do assembly (`Application/DependencyInjection.cs`); repositórios, à mão, em `Infrastructure/DependencyInjection.cs`.
- Exclusão lógica por filtro global em `AppDbContext` para toda `Entidade`; concorrência otimista por `xmin` (ver `AtividadeConfiguracao`: `HasColumnName("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken()`).
- Testes: `TesteComBanco` (aplicação + banco), `AplicacaoDeTeste` (Web inteira), `UsuariosDeClienteDeTeste.GravarAsync(sp, clienteId, nome?, email?)`, `Falsos.cs` nos unitários.
- Linha de base: rode `dotnet test` inteiro antes da Tarefa 1 e anote os totais no relatório. A suíte de integração leva de 15 a 35 minutos nesta máquina; a máquina já reiniciou sozinha no meio de build e testes (ver memória do projeto): ao retomar, confira o estado do worktree antes de seguir.

## Commits

O git desta máquina está sem `user.name`/`user.email`. Antes do primeiro commit, pergunte ao usuário qual identidade usar (não escolha). Todo commit termina com:

```
Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

Antes de cada commit, `git branch --show-current` deve responder `solicitacoes-do-cliente`. Git só pelo PowerShell.

## Global Constraints

- TFM único `net10.0`. `Nullable` e `TreatWarningsAsErrors=true`. Versões de pacote só em `Directory.Packages.props`. Nenhum pacote novo.
- `Domain` não referencia nada. `Application` não referencia EF Core, Npgsql, `Infrastructure` nem `Web`. A camada Web só chega aos dados pelo despachante (`WebSoPeloDespachanteTests`).
- Código, nomes de tabela e mensagens em português. Tabelas e colunas em `snake_case`. Datas em UTC no domínio, na aplicação e no banco; a tela converte com `FusoDoEscritorio`.
- Exclusão sempre lógica. Agregados se referenciam só por Id. Toda alteração de `Entidade` gera auditoria na mesma transação; toda propriedade mapeada nova é classificada em `PropriedadesAuditadasTests`; `GravacoesPorForaTests` continua com a mesma lista.
- Autorização verificada no manipulador e, para o cliente, também no portão. A interface apenas oculta.
- Um caso de uso por arquivo. Consultas são `IConsultaAuditavel` com `TipoRecurso => "Solicitacao"`.
- As requisições do portal não têm campo de Id de cliente.
- Uma única migration nesta fase: `SolicitacoesDoCliente`.
- Todo texto que um teste procura no HTML é escrito por uma única expressão Razor ou por marcação fixa.
- Telas no design system: siga `Andamento.razor`, `AtividadeFormulario.razor` e `AtividadeFicha.razor` como modelo de lista, formulário e ficha; `TelasNoDesignSystemTests` continua verde.
- Requisições abertas ao cliente ao fim da fase, exatamente: `AbrirSolicitacao`, `AlterarMinhaSenha`, `AlterarMinhaSolicitacao`, `AnexarDocumentoASolicitacao`, `AutenticarUsuario`, `BaixarDocumento`, `CancelarMinhaSolicitacao`, `ListarDocumentosComuns`, `ListarMeuAndamento`, `ListarMeusDocumentos`, `ListarMinhasSolicitacoes`, `ObterMeuAcesso`, `ObterMinhaSolicitacao`, `RemoverDocumentoDaSolicitacao`.
- Rotas: `/minhas-solicitacoes`, `/minhas-solicitacoes/nova`, `/minhas-solicitacoes/{id}`, `/minhas-solicitacoes/{id}/editar` (só do portal); `/solicitacoes`, `/solicitacoes/{id}` (Diretoria).
- Mensagens e textos exatos:
  - Aviso fixo: `A equipe verifica sua solicitação em até 24 horas úteis.`
  - Depois de abrir: `Solicitação enviada. A equipe verifica sua solicitação em até 24 horas úteis.`
  - `A descrição da solicitação é obrigatória.` / `A descrição da solicitação deve ter até 4000 caracteres.` / `Urgência inválida.`
  - `Esta solicitação não pode mais ser alterada.`
  - `Só quem abriu a solicitação pode alterá-la.`
  - `Solicitação não encontrada.`
  - `A solicitação foi alterada. Recarregue e tente de novo.`
  - `Anexe até 10 documentos por solicitação.`
  - `Esta solicitação já virou atividade.` e `Cliente cancelou a solicitação, mesmo assim deseja criar a atividade?` (constantes do domínio; a segunda só é usada na Fase B)
  - `Categoria inválida para esta consulta.` (já existe)
  - Urgências: `Baixa`, `Média`, `Alta`
  - Situações para o cliente: `Aguardando análise`, `Cancelada`, `Em andamento`, `Concluída`; para a Diretoria: `Aguardando`, `Cancelada`, `Virou atividade`
  - Menu e títulos: `Solicitações` (menu do cliente e da Diretoria), `Minhas solicitações` (título da lista do portal), `Nova solicitação`, `Editar solicitação`, `Solicitação`
  - Botões: `Nova solicitação`, `Enviar solicitação`, `Salvar alterações`, `Editar`, `Cancelar solicitação`, `Sim, cancelar`, `Voltar`, `Remover`, `Anexar documento`
  - Confirmação do cancelamento: `Cancelar esta solicitação? Ela não poderá ser reaberta.`
  - Lista vazia: `Nenhuma solicitação.`
  - Atalho do início do cliente: título `Nova solicitação`, descrição `Peça algo ao escritório e anexe documentos.`

## Review Focus

1. **O cliente edita a mesma solicitação em duas abas**: a segunda gravação responde conflito e não sobrescreve a primeira. (Tarefa 4: `Alterar_com_versao_antiga_responde_conflito_e_mantem_a_primeira`)
2. **Um dos vários arquivos da abertura é recusado (grande demais ou vazio)**: nada é gravado, nem solicitação, nem documento, nem arquivo no disco. (Tarefa 4: `Abrir_com_um_arquivo_recusado_nao_grava_nada_nem_deixa_arquivo`)
3. **Outro usuário do mesmo cliente abre a solicitação de um colega**: vê, mas não edita, não anexa, não remove documento e não cancela. (Tarefa 4: `Colega_do_mesmo_cliente_ve_mas_nao_altera`)
4. **A Produção cola o endereço de download de um documento de solicitação, ou abre `/solicitacoes`**: download responde não encontrado com registro de acesso negado; a página mostra "Página não encontrada". (Tarefas 2 e 6: `Producao_nao_baixa_documento_de_solicitacao`, `Producao_em_solicitacoes_ve_nao_encontrada`)
5. **O cliente cancela e depois tenta editar, anexar ou remover documento (por outra aba)**: recebe "Esta solicitação não pode mais ser alterada." (ou conflito de versão), e nada muda. (Tarefa 4: `Depois_de_cancelada_nada_mais_muda`)

---

### Task 1: Agregado `SolicitacaoDoCliente` no domínio

**Files:**
- Create: `src/AtividadesJuridicas.Domain/Solicitacoes/SolicitacaoDoCliente.cs`
- Create: `src/AtividadesJuridicas.Domain/Solicitacoes/TiposDaSolicitacao.cs`
- Test: `tests/AtividadesJuridicas.UnitTests/Domain/SolicitacaoDoClienteTests.cs`

**Interfaces — Produces:**
- `enum UrgenciaDaSolicitacao { Baixa = 1, Media = 2, Alta = 3 }`
- `enum SituacaoDaSolicitacao { Aguardando = 1, Cancelada = 2, EmAtividade = 3 }`
- `sealed class SolicitacaoDoCliente : Entidade` com:
  - `const int TamanhoMaximoDescricao = AtividadeComentario.TamanhoMaximoTexto` (4000)
  - `const string NaoPodeMaisSerAlterada = "Esta solicitação não pode mais ser alterada."`
  - `const string JaVirouAtividade = "Esta solicitação já virou atividade."`
  - `const string CanceladaPeloCliente = "Cliente cancelou a solicitação, mesmo assim deseja criar a atividade?"`
  - `Guid ClienteId`, `Guid SolicitanteId`, `string Descricao`, `UrgenciaDaSolicitacao Urgencia`, `DateTime? CanceladaEm`, `Guid? AtividadeId` (todos `private set`)
  - `[NaoAuditar] uint Versao`, `[NaoAuditar] int VersaoDosDocumentos`
  - `SituacaoDaSolicitacao Situacao` (calculada: `AtividadeId` ⇒ `EmAtividade`; senão `CanceladaEm` ⇒ `Cancelada`; senão `Aguardando`)
  - `static SolicitacaoDoCliente Abrir(Guid clienteId, Guid solicitanteId, string descricao, UrgenciaDaSolicitacao urgencia)`
  - `void Alterar(string descricao, UrgenciaDaSolicitacao urgencia)`
  - `void Cancelar(DateTime agoraUtc)`
  - `void RegistrarMudancaNosDocumentos()`
  - `void VincularAtividade(Guid atividadeId, bool confirmouCancelada)`
  - `void DesvincularAtividade()`
  - `bool EhDoSolicitante(Guid usuarioId)`

**Regras:**
1. `Abrir`: `clienteId` e `solicitanteId` não vazios (`ArgumentException`: erro de programação). Descrição aparada, obrigatória e até 4000 caracteres; urgência definida no enum. Mensagens exatas das Global Constraints, como `ExcecaoDeDominio`.
2. `Alterar`, `Cancelar` e `RegistrarMudancaNosDocumentos` só em `Aguardando`; fora dela, `ExcecaoDeDominio(NaoPodeMaisSerAlterada)`. `Cancelar` exige `agoraUtc` em UTC (`ArgumentException`).
3. `VincularAtividade`: `atividadeId` não vazio; em `EmAtividade`, `ExcecaoDeDominio(JaVirouAtividade)`; em `Cancelada` sem `confirmouCancelada`, `ExcecaoDeDominio(CanceladaPeloCliente)`. `CanceladaEm` é preservado.
4. `DesvincularAtividade`: limpa `AtividadeId`; a situação volta a `Aguardando` ou `Cancelada` conforme `CanceladaEm`. Sem atividade, não faz nada.

- [ ] **Step 1: Escrever os testes que falham** em `SolicitacaoDoClienteTests`:
  - `Abrir_apara_a_descricao_e_nasce_aguardando` — `Situacao == Aguardando`, `Descricao` sem espaços nas pontas, `CanceladaEm` e `AtividadeId` nulos.
  - `Abrir_recusa_descricao_vazia_longa_e_urgencia_invalida` — teoria com `""`, `"   "`, 4001 caracteres e `(UrgenciaDaSolicitacao)9`; mensagens exatas. 4000 caracteres passa.
  - `Alterar_troca_descricao_e_urgencia_so_enquanto_aguarda`
  - `Cancelar_deixa_cancelada_e_nada_mais_muda` — depois de cancelar, `Alterar`, `Cancelar` e `RegistrarMudancaNosDocumentos` lançam com `NaoPodeMaisSerAlterada`.
  - `RegistrarMudancaNosDocumentos_aumenta_a_versao_dos_documentos`
  - `Vincular_em_aguardando_vira_em_atividade`
  - `Vincular_cancelada_exige_confirmacao` — sem confirmação lança com `CanceladaPeloCliente`; com ela vira `EmAtividade` e mantém `CanceladaEm`.
  - `Vincular_duas_vezes_e_recusado` — mensagem `JaVirouAtividade`.
  - `Desvincular_volta_ao_estado_anterior` — de aguardando volta a `Aguardando`; de cancelada-confirmada volta a `Cancelada`.
  - `Em_atividade_nao_se_altera_nem_cancela`
- [ ] **Step 2: Rodar e ver falhar.** `dotnet test tests/AtividadesJuridicas.UnitTests --filter FullyQualifiedName~SolicitacaoDoClienteTests` — não compila (tipos inexistentes).
- [ ] **Step 3: Implementar** os dois arquivos com as interfaces acima, no estilo de `Atividade.cs` (construtor privado, `private set`, validação antes de qualquer mudança).
- [ ] **Step 4: Rodar e ver passar.** Mesmo comando: todos verdes.
- [ ] **Step 5: Commit** (inclui o spec): `feat(domain): solicitação do cliente`.

---

### Task 2: Categoria `DocumentoDaSolicitacao`

**Files:**
- Modify: `src/AtividadesJuridicas.Domain/Documentos/Documento.cs`
- Modify: `src/AtividadesJuridicas.Infrastructure/Persistencia/Repositorios/DocumentoRepositorio.cs` (`ComAcesso`)
- Modify: `src/AtividadesJuridicas.Application/Documentos/ListarDocumentosDoCliente.cs`
- Modify: `src/AtividadesJuridicas.Application/Documentos/PortasDeDocumentos.cs`
- Modify: `tests/AtividadesJuridicas.UnitTests/Application/Falsos.cs` (`DocumentoRepositorioFalso`)
- Test: `tests/AtividadesJuridicas.UnitTests/Domain/DocumentoTests.cs`; `tests/AtividadesJuridicas.UnitTests/Application/DocumentosTests.cs`

**Interfaces — Produces:**
- `CategoriaDocumento.DocumentoDaSolicitacao = 6`
- `Documento.SolicitacaoId` (`Guid?`, `private set`): obrigatório na categoria 6 e só nela
- `Documento.CriarDaSolicitacao(Guid solicitacaoId, Guid clienteId, string? nome, ArquivoDoDocumento arquivo): Documento`
- `IDocumentoRepositorio.ListarDaSolicitacaoAsync(Guid solicitacaoId, CancellationToken ct): Task<IReadOnlyList<DocumentoDoPortal>>` — vivos, do mais antigo para o mais novo, sem autor
- `IDocumentoRepositorio.ContarDaSolicitacaoAsync(Guid solicitacaoId, CancellationToken ct): Task<int>` — vivos

**Regras:**
1. `CriarDaSolicitacao`: `solicitacaoId` não vazio (`ExcecaoDeDominio("A solicitação do documento é obrigatória.")`); cliente obrigatório (a exigência de cliente do `Criar` privado passa a valer também para a categoria 6). Nasce sem listas de acesso.
2. `VisivelAoCliente` passa a valer também para a categoria 6. Com isso a Produção já não a alcança e `DefinirAcesso` a recusa com `SemListaDeAcesso`, sem regra nova.
3. Em `PodeSerAcessadoPor`, no ramo do perfil `Cliente`, acrescente: categoria 6 com `ClienteId == clienteDoUsuario`. Atualize os comentários de `ClienteId` e da regra.
4. `DocumentoRepositorio.ComAcesso`: acrescente `d.Categoria != CategoriaDocumento.DocumentoDaSolicitacao` às categorias excluídas. `ContarDoClienteAsync` não muda (a categoria 6 não está entre as áreas lidas da ficha).
5. `ListarDocumentosDoCliente` com a categoria 6 responde `Validacao` com `"Categoria inválida para esta consulta."`, como a 5. `EnviarDocumento` já recusa a 6 (`"Categoria inválida para envio."`): fixe em teste.

- [ ] **Step 1: Testes que falham.**
  - Domínio: `Documento_da_solicitacao_exige_solicitacao_e_cliente`; `Documento_da_solicitacao_e_visivel_ao_cliente_e_sem_lista`; e acrescente a categoria 6 à matriz de acesso existente (três tipos de perfil; cliente dono lê; cliente de outro, Produção mesmo constando em lista e tipo desconhecido não leem; Diretoria lê).
  - Aplicação: `ListarDocumentosDoCliente_recusa_a_categoria_da_solicitacao` (`Validacao`, mensagem exata); `EnviarDocumento_recusa_a_categoria_da_solicitacao`.
- [ ] **Step 2: Rodar e ver falhar.** `dotnet test tests/AtividadesJuridicas.UnitTests --filter "FullyQualifiedName~DocumentoTests|FullyQualifiedName~DocumentosTests"`.
- [ ] **Step 3: Implementar** as regras 1 a 5 e os dois métodos novos da porta, em `DocumentoRepositorio` (consulta por `SolicitacaoId`, sem autor, como `DoPortalAsync`) e no falso (filtrar a lista em memória). A coluna só passa a existir com a migration da Tarefa 3; os testes desta tarefa são unitários.
- [ ] **Step 4: Rodar e ver passar.** Mesmo comando.
- [ ] **Step 5: Commit** `feat(domain): documento da solicitação`.

O mapeamento e a migration da coluna `solicitacao_id` ficam na Tarefa 3; os testes de integração da categoria, na Tarefa 4.

---

### Task 3: Persistência e repositório das solicitações

**Files:**
- Create: `src/AtividadesJuridicas.Application/Solicitacoes/PortasDeSolicitacoes.cs`
- Create: `src/AtividadesJuridicas.Infrastructure/Persistencia/Configuracoes/SolicitacaoDoClienteConfiguracao.cs`
- Create: `src/AtividadesJuridicas.Infrastructure/Persistencia/Repositorios/SolicitacaoRepositorio.cs`
- Create: migration `SolicitacoesDoCliente` em `src/AtividadesJuridicas.Infrastructure/Persistencia/Migrations/`
- Modify: `src/AtividadesJuridicas.Infrastructure/Persistencia/AppDbContext.cs` (`DbSet<SolicitacaoDoCliente> SolicitacoesDoCliente`)
- Modify: `src/AtividadesJuridicas.Infrastructure/Persistencia/Configuracoes/DocumentoConfiguracao.cs`
- Modify: `src/AtividadesJuridicas.Infrastructure/DependencyInjection.cs`
- Modify: `tests/AtividadesJuridicas.IntegrationTests/PropriedadesAuditadasTests.cs`
- Modify: `tests/AtividadesJuridicas.UnitTests/Application/Falsos.cs` (`SolicitacaoRepositorioFalso`)
- Test: `tests/AtividadesJuridicas.IntegrationTests/SolicitacoesPersistenciaTests.cs` (novo); `tests/AtividadesJuridicas.IntegrationTests/MigracaoDasSolicitacoesTests.cs` (novo)

**Interfaces — Produces** (em `PortasDeSolicitacoes.cs`, namespace `AtividadesJuridicas.Application.Solicitacoes`):
- `static class SolicitacoesOpcoes { TamanhoDaPagina = 20; MaximoDeDocumentos = 10; TamanhoDoTrecho = 140; NaoEncontrada = "Solicitação não encontrada."; Alterada = "A solicitação foi alterada. Recarregue e tente de novo."; SoQuemAbriu = "Só quem abriu a solicitação pode alterá-la."; LimiteDeDocumentos = "Anexe até 10 documentos por solicitação."; static string Trecho(string descricao) }`
- `enum SituacaoParaOCliente { AguardandoAnalise = 1, Cancelada = 2, EmAndamento = 3, Concluida = 4 }`
- `sealed record SolicitacaoDoPortal(Guid Id, DateTime AbertaEm, string Trecho, UrgenciaDaSolicitacao Urgencia, SituacaoParaOCliente Situacao, string SolicitanteNome, int Documentos)`
- `sealed record SolicitacaoResumo(Guid Id, DateTime AbertaEm, Guid ClienteId, string ClienteNome, string SolicitanteNome, UrgenciaDaSolicitacao Urgencia, string Trecho, int Documentos, SituacaoDaSolicitacao Situacao)`
- `sealed record AtividadeDaSolicitacao(Guid Id, string Titulo, StatusAtividade Status, DateTime? ConcluidaEm)`
- `interface ISolicitacaoRepositorio`:
  - `Task<SolicitacaoDoCliente?> ObterPorIdAsync(Guid id, CancellationToken ct)` (rastreada)
  - `void Adicionar(SolicitacaoDoCliente solicitacao)`
  - `Task<Pagina<SolicitacaoDoPortal>> ListarDoClienteAsync(Guid clienteId, SituacaoParaOCliente? situacao, int pagina, int tamanho, CancellationToken ct)` — da mais recente para a mais antiga, desempate por Id decrescente
  - `Task<Pagina<SolicitacaoResumo>> ListarAsync(SituacaoDaSolicitacao? situacao, Guid? clienteId, int pagina, int tamanho, CancellationToken ct)` — da mais antiga para a mais nova (`CriadoEm`, depois Id), todas as solicitações vivas
  - `Task<AtividadeDaSolicitacao?> ObterAtividadeAsync(Guid atividadeId, CancellationToken ct)` — só atividade viva
  - `Task<string?> NomeDoUsuarioAsync(Guid usuarioId, CancellationToken ct)` e `Task<string?> NomeDoClienteAsync(Guid clienteId, CancellationToken ct)` — mesmo de excluídos (`IgnoreQueryFilters`)

**Regras:**
1. Tabela `solicitacoes_do_cliente`: `descricao` até 4000, obrigatória; `urgencia` como inteiro, com `ck_solicitacoes_urgencia` (`urgencia IN (1, 2, 3)`); `versao` mapeada em `xmin` como em `AtividadeConfiguracao`; `versao_dos_documentos` com padrão 0. Chaves estrangeiras sem cascata (`Restrict`) para `clientes` (`cliente_id`), `usuarios` (`solicitante_id`) e `atividades` (`atividade_id`). Índices: `(cliente_id, criado_em)`, `criado_em`, `atividade_id`.
2. `documentos.solicitacao_id`: anulável, chave estrangeira `Restrict` para `solicitacoes_do_cliente`, com índice. Restrições reescritas na mesma migration:
   - `ck_documentos_cliente_conforme_a_categoria`: `(categoria IN (1, 2, 4, 6) AND cliente_id IS NOT NULL) OR categoria = 3 OR (categoria = 5 AND cliente_id IS NULL)`
   - nova `ck_documentos_solicitacao_so_na_categoria_6`: `(categoria = 6) = (solicitacao_id IS NOT NULL)`
   - O índice único da chave de armazenamento **não** muda nesta fase.
3. Situação para o cliente no banco, numa consulta só: atividade viva vinculada concluída ⇒ `Concluida`; viva e não concluída ⇒ `EmAndamento`; sem atividade viva e com `cancelada_em` ⇒ `Cancelada`; senão ⇒ `AguardandoAnalise`. O filtro usa a mesma expressão. "Documentos" conta os documentos vivos da solicitação.
4. Situação da Diretoria (`ListarAsync`) é a do domínio, por `atividade_id` e `cancelada_em`.
5. `PropriedadesAuditadasTests`: `["SolicitacaoDoCliente"] = ["AtividadeId", "CanceladaEm", "ClienteId", "Descricao", "SolicitanteId", "Urgencia"]`; `Documento` ganha `"SolicitacaoId"`.

- [ ] **Step 1: Testes que falham.**
  - `SolicitacoesPersistenciaTests`:
    - `Solicitacao_grava_e_volta_com_todos_os_campos`
    - `Alterar_com_versao_antiga_falha_por_concorrencia` — duas instâncias em escopos diferentes; a segunda gravação lança a exceção de concorrência que `UnidadeDeTrabalho` já traduz.
    - `Mudanca_nos_documentos_muda_a_versao`
    - `Listar_do_cliente_traz_so_as_do_cliente_da_mais_recente_para_a_mais_antiga`
    - `Situacao_para_o_cliente_segue_a_atividade_viva` — vincule pelo domínio (`VincularAtividade`) uma atividade criada por `SalvarAtividade`: `EmAndamento`; conclua-a (responsável único diretor): `Concluida`; exclua-a por `ExcluirAtividade`: volta a `AguardandoAnalise`. Filtro por cada uma das quatro situações.
    - `Listar_da_diretoria_vai_da_mais_antiga_para_a_mais_nova_e_filtra`
    - `Banco_recusa_documento_da_solicitacao_sem_solicitacao_e_solicitacao_fora_da_categoria_6` (SQL direto, como os testes de restrição existentes).
  - `MigracaoDasSolicitacoesTests.Migration_troca_a_restricao_num_banco_que_ja_tem_documentos` — modelo: `MigracaoDosDocumentosDoPortalTests`.
  - `PropriedadesAuditadasTests` com a lista nova.
- [ ] **Step 2: Rodar e ver falhar.** `dotnet test tests/AtividadesJuridicas.IntegrationTests --filter "FullyQualifiedName~SolicitacoesPersistenciaTests|FullyQualifiedName~MigracaoDasSolicitacoesTests|FullyQualifiedName~PropriedadesAuditadasTests"`.
- [ ] **Step 3: Implementar** configuração, `DbSet`, repositório, registro em `DependencyInjection` e o falso. Gerar a migration: `dotnet ef migrations add SolicitacoesDoCliente --project src/AtividadesJuridicas.Infrastructure --startup-project src/AtividadesJuridicas.Infrastructure --output-dir Persistencia/Migrations` (confira o comando usado nas fases anteriores em `docs/superpowers/plans/2026-10-09-fase-8b-*.md` e use o mesmo). Revise o SQL gerado: as restrições antigas são removidas e recriadas, e nada toca nos dados.
- [ ] **Step 4: Rodar e ver passar.** Mesmo comando.
- [ ] **Step 5: Commit** `feat(infra): persistência das solicitações do cliente`.

---

### Task 4: Casos de uso do portal

**Files:**
- Modify: `src/AtividadesJuridicas.Application/Documentos/ArquivoEnviado.cs`
- Create em `src/AtividadesJuridicas.Application/Solicitacoes/`: `AbrirSolicitacao.cs`, `ListarMinhasSolicitacoes.cs`, `ObterMinhaSolicitacao.cs`, `AlterarMinhaSolicitacao.cs`, `AnexarDocumentoASolicitacao.cs`, `RemoverDocumentoDaSolicitacao.cs`, `CancelarMinhaSolicitacao.cs`, `MinhaSolicitacao.cs` (o que os cinco comandos têm em comum)
- Modify: `tests/AtividadesJuridicas.UnitTests/Application/PortaoDoClienteTests.cs`
- Test: `tests/AtividadesJuridicas.UnitTests/Application/SolicitacoesDoPortalTests.cs` (novo); `tests/AtividadesJuridicas.IntegrationTests/SolicitacoesDoPortalTests.cs` (novo)

**Interfaces — Consumes:** Tarefas 1 a 3.

**Interfaces — Produces** (todas `IRequisicaoAbertaAoCliente`):
- `sealed record ArquivoParaEnviar(string NomeArquivo, string? TipoConteudo, Stream Conteudo)` (em `Documentos`)
- `ArquivoEnviado.GravarVariosAsync(IArmazenamentoDeArquivos armazenamento, IUnidadeDeTrabalho unidade, IReadOnlyList<ArquivoParaEnviar> arquivos, Func<IReadOnlyList<ArquivoDoDocumento>, Task> registrar, CancellationToken ct): Task<Resultado>` — valida todos (`Validar`) antes de gravar o primeiro; grava um a um; chama `registrar` e `SalvarAsync`; em qualquer recusa ou exceção, remove todos os já gravados (`TentarRemoverAsync`).
- `AbrirSolicitacao(string Descricao, UrgenciaDaSolicitacao Urgencia, IReadOnlyList<ArquivoParaEnviar> Arquivos) : IRequisicao<Resultado<Guid>>`
- `ListarMinhasSolicitacoes(SituacaoParaOCliente? Situacao = null, int Pagina = 1) : IRequisicao<Resultado<Pagina<SolicitacaoDoPortal>>>`, `IConsultaAuditavel` (`Listou`, `Detalhe` com situação e página)
- `ObterMinhaSolicitacao(Guid Id) : IRequisicao<Resultado<MinhaSolicitacaoDetalhe>>`, `IConsultaAuditavel` (`Visualizou`, `RecursoId => Id`)
- `sealed record MinhaSolicitacaoDetalhe(Guid Id, DateTime AbertaEm, string Descricao, UrgenciaDaSolicitacao Urgencia, SituacaoParaOCliente Situacao, string SolicitanteNome, bool PodeAlterar, IReadOnlyList<DocumentoDoPortal> Documentos, string? AtividadeTitulo, DateTime? AtividadeConcluidaEm, uint Versao)`
- `AlterarMinhaSolicitacao(Guid Id, string Descricao, UrgenciaDaSolicitacao Urgencia, uint VersaoEsperada) : IRequisicao<Resultado>`
- `AnexarDocumentoASolicitacao(Guid Id, string NomeArquivo, string? TipoConteudo, Stream Conteudo, uint VersaoEsperada) : IRequisicao<Resultado<Guid>>`
- `RemoverDocumentoDaSolicitacao(Guid DocumentoId, uint VersaoEsperada) : IRequisicao<Resultado>`
- `CancelarMinhaSolicitacao(Guid Id, uint VersaoEsperada) : IRequisicao<Resultado>`

**Regras:**
1. Ator: `{ EhCliente: true, ClienteId: { } clienteId }`; senão `AcessoNegado` ("Acesso negado.").
2. Solicitação inexistente, excluída ou de outro cliente: `NaoEncontrado` com `SolicitacoesOpcoes.NaoEncontrada`. Em `RemoverDocumentoDaSolicitacao`, o mesmo para documento inexistente, de outra categoria ou cuja solicitação é de outro cliente.
3. Nos cinco comandos, nesta ordem: solicitante (`Validacao`, `SoQuemAbriu`) → versão (`Conflito`, `Alterada`) → domínio (`Validacao` com a mensagem da exceção) → limite de documentos, quando houver anexo (`Validacao`, `LimiteDeDocumentos`) → gravação. `ExcecaoDeConcorrencia`/`ExcecaoDeConflito` na gravação também viram `Conflito` com `Alterada`.
4. `AbrirSolicitacao`: mais de 10 arquivos recusa antes de gravar qualquer um. A solicitação é criada pelo domínio **antes** de gravar arquivos (descrição inválida não grava nada). Cada documento: `Documento.CriarDaSolicitacao(solicitacao.Id, clienteId, null, arquivo)`. Zero arquivos é válido.
5. `AnexarDocumentoASolicitacao` e `RemoverDocumentoDaSolicitacao` chamam `RegistrarMudancaNosDocumentos()` na mesma gravação. O anexo usa `ArquivoEnviado.GravarAsync` (um arquivo).
6. `ObterMinhaSolicitacao.PodeAlterar` = ator é o solicitante e a situação do domínio é `Aguardando`. Os documentos vêm de `ListarDaSolicitacaoAsync`. Com atividade viva vinculada, `AtividadeTitulo` e, se concluída, `AtividadeConcluidaEm`; a situação segue a regra 3 da Tarefa 3.
7. `ListarMinhasSolicitacoes` com situação fora do enum: `Validacao` ("Situação inválida.").
8. `PortaoDoClienteTests.As_requisicoes_abertas_ao_cliente_sao_exatamente_estas`: a lista das Global Constraints (14 nomes).

- [ ] **Step 1: Testes unitários que falham** (`SolicitacoesDoPortalTests`, com os falsos): um teste por regra de 1 a 7, entre eles:
  - `Equipe_recebe_acesso_negado_nas_requisicoes_do_portal` (Diretoria e Produção, as sete).
  - `Abrir_com_onze_arquivos_e_recusado_sem_gravar_arquivo`
  - `Abrir_com_descricao_invalida_nao_grava_arquivo`
  - `Comandos_conferem_solicitante_antes_da_versao` — colega com versão errada recebe `SoQuemAbriu`.
  - `Anexar_alem_do_limite_e_recusado`
  - `Remover_documento_de_outra_categoria_responde_nao_encontrado`
  - e a lista do portão atualizada.
- [ ] **Step 2: Rodar e ver falhar.** `dotnet test tests/AtividadesJuridicas.UnitTests --filter "FullyQualifiedName~SolicitacoesDoPortalTests|FullyQualifiedName~PortaoDoClienteTests"`.
- [ ] **Step 3: Implementar** `GravarVariosAsync` e os sete casos de uso. `MinhaSolicitacao.cs` traz a carga e as conferências comuns das regras 1 a 3 (por exemplo, `static Task<(SolicitacaoDoCliente?, Resultado?)> CarregarParaAlterarAsync(...)`); nome e forma ficam a critério do implementador.
- [ ] **Step 4: Rodar e ver passar.** Mesmo comando.
- [ ] **Step 5: Testes de integração que falham** (`IntegrationTests/SolicitacoesDoPortalTests`, base `TesteComBanco`; clientes Maria e José, usuárias Carla e Ana de Maria, João de José, Pedro de Produção, como em `DocumentosDoPortalTests`):
  - `Carla_abre_com_dois_arquivos_e_ve_na_lista_e_na_ficha` — `AbertaEm`, trecho, urgência, `SolicitanteNome == "Carla Cliente"`, `Documentos == 2`, baixa os dois.
  - `Abrir_com_um_arquivo_recusado_nao_grava_nada_nem_deixa_arquivo` — o segundo arquivo passa de 50 MB (stream que gera bytes sem alocar tudo, como nos testes de limite existentes); zero solicitações, zero documentos e zero arquivos no disco.
  - `Cliente_A_nao_lista_nao_abre_e_nao_baixa_nada_do_cliente_B` — João: lista sem as de Maria; `ObterMinhaSolicitacao` responde `NaoEncontrado`; download responde `NaoEncontrado` com registro de acesso negado.
  - `Colega_do_mesmo_cliente_ve_mas_nao_altera` — Ana vê a de Carla (com `PodeAlterar == false`); alterar, anexar, remover e cancelar respondem `SoQuemAbriu`.
  - `Alterar_com_versao_antiga_responde_conflito_e_mantem_a_primeira`
  - `Anexar_e_remover_mudam_a_versao` — a versão lida antes do anexo passa a responder conflito.
  - `Depois_de_cancelada_nada_mais_muda` — com a versão nova, alterar, anexar e remover respondem `NaoPodeMaisSerAlterada`; a situação é `Cancelada`.
  - `Producao_nao_baixa_documento_de_solicitacao` — `NaoEncontrado` e registro de acesso negado.
  - `Consultas_do_portal_ficam_registradas` — `Listou` e `Visualizou` no recurso `Solicitacao`.
  - `Gravacoes_ficam_na_auditoria` — abertura, alteração, cancelamento e documentos geram registros de entidade.
- [ ] **Step 6: Rodar e ver falhar, implementar o que faltar, rodar e ver passar.** `dotnet test tests/AtividadesJuridicas.IntegrationTests --filter FullyQualifiedName~SolicitacoesDoPortalTests`.
- [ ] **Step 7: Commit** `feat(app): solicitações no portal do cliente`.

---

### Task 5: Consultas da Diretoria

**Files:**
- Create em `src/AtividadesJuridicas.Application/Solicitacoes/`: `ListarSolicitacoes.cs`, `ObterSolicitacao.cs`
- Test: `tests/AtividadesJuridicas.UnitTests/Application/SolicitacoesDaDiretoriaTests.cs`; `tests/AtividadesJuridicas.IntegrationTests/SolicitacoesDaDiretoriaTests.cs`

**Interfaces — Produces:**
- `ListarSolicitacoes(SituacaoDaSolicitacao? Situacao = null, Guid? ClienteId = null, int Pagina = 1) : IRequisicao<Resultado<Pagina<SolicitacaoResumo>>>`, `IConsultaAuditavel` (`Listou`, `Detalhe` com os três campos)
- `ObterSolicitacao(Guid Id) : IRequisicao<Resultado<SolicitacaoDetalhe>>`, `IConsultaAuditavel` (`Visualizou`, `RecursoId => Id`)
- `sealed record SolicitacaoDetalhe(Guid Id, DateTime AbertaEm, Guid ClienteId, string ClienteNome, Guid SolicitanteId, string SolicitanteNome, UrgenciaDaSolicitacao Urgencia, string Descricao, SituacaoDaSolicitacao Situacao, DateTime? CanceladaEm, IReadOnlyList<DocumentoDoPortal> Documentos, Guid? AtividadeId, string? AtividadeTitulo, uint Versao)`

**Regras:** só `EhDiretoria`; Produção e Cliente recebem `AcessoNegado` (o cliente já é barrado pelo portão). Situação fora do enum: `Validacao` ("Situação inválida."). Solicitação inexistente ou excluída: `NaoEncontrado`. Nomes de cliente e de solicitante aparecem mesmo depois de excluídos. `Versao` é devolvida para a Fase B.

- [ ] **Step 1: Testes que falham.** Unitários: `Producao_recebe_acesso_negado_nas_duas`; `Situacao_invalida_e_recusada`. Integração: `Diretoria_lista_da_mais_antiga_para_a_mais_nova_com_filtros` (três solicitações de dois clientes, uma cancelada; filtro por situação e por cliente; sem filtro, todas); `Diretoria_le_a_ficha_com_documentos_e_quem_abriu`; `Diretoria_baixa_documento_da_solicitacao`; `Consultas_da_diretoria_ficam_registradas`.
- [ ] **Step 2: Rodar e ver falhar.** `dotnet test --filter FullyQualifiedName~SolicitacoesDaDiretoriaTests`.
- [ ] **Step 3: Implementar.**
- [ ] **Step 4: Rodar e ver passar.**
- [ ] **Step 5: Commit** `feat(app): solicitações para a Diretoria`.

---

### Task 6: Telas

**Files:**
- Modify: `src/AtividadesJuridicas.Web/Infra/RotasDoCliente.cs`
- Modify: `src/AtividadesJuridicas.Web/Components/Layout/MainLayout.razor`
- Modify: `src/AtividadesJuridicas.Web/Components/Pages/Home.razor`
- Create: `src/AtividadesJuridicas.Web/Infra/RotulosDeSolicitacao.cs`
- Create em `src/AtividadesJuridicas.Web/Components/Pages/`: `MinhasSolicitacoes.razor`, `MinhaSolicitacaoFormulario.razor`, `MinhaSolicitacaoFicha.razor`, `Solicitacoes.razor`, `SolicitacaoFicha.razor`
- Test: `tests/AtividadesJuridicas.UnitTests/` — `RotasDoCliente` (se já houver teste dela, acrescente lá); `tests/AtividadesJuridicas.IntegrationTests/SolicitacoesWebTests.cs` e `SolicitacoesInterativasTests.cs` (novos); `LayoutWebTests.cs` / `MainLayoutInterativoTests.cs` (menu)

**Interfaces — Produces:**
- `RotulosDeSolicitacao.Urgencia(UrgenciaDaSolicitacao): string` → `Baixa`/`Média`/`Alta`; `.ParaOCliente(SituacaoParaOCliente): string` → `Aguardando análise`/`Cancelada`/`Em andamento`/`Concluída`; `.ParaADiretoria(SituacaoDaSolicitacao): string` → `Aguardando`/`Cancelada`/`Virou atividade`

**Regras:**
1. **Guardas de rota.** Em `RotasDoCliente`, `minhas-solicitacoes` e `minhas-solicitacoes/nova` entram nos dois conjuntos (aberta e só do portal), e um padrão `^minhas-solicitacoes/[0-9a-fA-F-]{36}(/editar)?$` vale nos dois métodos, sobre o caminho já normalizado. `/solicitacoes` não entra: é interna, e a própria página responde "Página não encontrada" para a Produção, como `/documentos-comuns`.
2. **Menu.** Cliente: "Solicitações" (`/minhas-solicitacoes`, ícone `enviar`) depois de Andamento, no menu lateral e no trilho; na barra inferior fica em "Mais". Diretoria: "Solicitações" (`/solicitacoes`, ícone `enviar`) depois de Atividades, só no menu lateral (no trilho e na barra, por "Mais"). Atualize as listas `MenuDoCliente`, `TrilhoDoCliente` e `MenuDaDiretoria` (o comentário delas pede os dois lugares) e o comentário do topo do layout.
3. **Início do cliente:** quarto `CartaoAtalho` para `/minhas-solicitacoes/nova`, ícone `enviar`, com o título e a descrição das Global Constraints.
4. **`/minhas-solicitacoes`:** título "Minhas solicitações", botão "Nova solicitação", filtro por situação (quatro), lista com data de abertura (fuso do escritório), trecho, urgência, situação e quem abriu; cada linha leva à ficha. Paginada; volta à última página se a pedida deixou de existir. Vazia: "Nenhuma solicitação.". Acesso negado: "Seu acesso foi encerrado. Entre novamente.", como em `MeusDocumentos.razor`.
5. **`/minhas-solicitacoes/nova`:** descrição (área de texto, até 4000), urgência (três opções, nenhuma pré-marcada; obrigatória), `InputFile multiple` com a lista dos escolhidos e "Remover" em cada um, recusa na tela de arquivo acima de 50 MB e de mais de 10 arquivos antes de enviar; o aviso fixo das 24 horas úteis visível no formulário. "Enviar solicitação" lê os arquivos com `OpenReadStream(DocumentosOpcoes.TamanhoMaximoBytes)` e envia `AbrirSolicitacao`; sucesso navega para a ficha com `?enviada=1`.
6. **`/minhas-solicitacoes/{id}`:** descrição, urgência, situação, quem abriu, data, documentos com link de download (`/documentos/{id}/baixar`). Com `?enviada=1`, `Aviso` de sucesso com o texto "Solicitação enviada. …". Com atividade, título da atividade e a situação. Com `PodeAlterar`, "Editar" e "Cancelar solicitação"; o cancelamento abre confirmação dentro da página ("Cancelar esta solicitação? Ela não poderá ser reaberta.", "Sim, cancelar", "Voltar"), sem diálogo do navegador. Conflito recarrega a ficha e mostra a mensagem.
7. **`/minhas-solicitacoes/{id}/editar`:** descrição e urgência com "Salvar alterações" (`AlterarMinhaSolicitacao`); documentos atuais com "Remover" e "Anexar documento" (um por vez, enviado na hora). Depois de cada operação de documento, recarrega a solicitação para ter a versão nova. Conflito mostra a mensagem e recarrega. Sem `PodeAlterar`, volta para a ficha.
8. **`/solicitacoes`:** título "Solicitações", filtro por situação que **abre em "Aguardando"** (com a opção "Todas") e busca de cliente (o mesmo componente/padrão de busca de cliente do formulário de atividade), lista com data, cliente, quem abriu, urgência, trecho, nº de documentos e situação, da mais antiga para a mais nova; linha leva à ficha. Para a Produção, "Página não encontrada" pela própria página (como `DocumentosComuns.razor`).
9. **`/solicitacoes/{id}`:** todos os campos do `SolicitacaoDetalhe`, documentos com download, cliente com link para a ficha do cliente. Com atividade, link "Ver atividade". Sem botões de ação nesta fase.

- [ ] **Step 1: Testes que falham.**
  - Rotas: `Rotas_das_solicitacoes_do_portal_abrem_ao_cliente_e_sao_exclusivas` (as quatro, com Id em maiúsculas e minúsculas e com `?enviada=1`); `Rotas_parecidas_nao_abrem` (`minhas-solicitacoes/abc`, `minhas-solicitacoes/{guid}/excluir`, `solicitacoes`).
  - Web (`SolicitacoesWebTests`, base `AplicacaoDeTeste`): `Equipe_em_rota_do_portal_ve_nao_encontrada`; `Cliente_em_solicitacoes_da_diretoria_ve_nao_encontrada`; `Producao_em_solicitacoes_ve_nao_encontrada`; `Menu_do_cliente_e_da_diretoria_tem_solicitacoes` e `Menu_da_producao_nao_tem`; `Inicio_do_cliente_tem_o_atalho_nova_solicitacao`; `Formulario_novo_mostra_o_aviso_das_24_horas_uteis`.
  - Interativos (`SolicitacoesInterativasTests`, padrão de `DocumentosComunsInterativosTests`): `Cliente_abre_solicitacao_com_arquivo_e_cai_na_ficha_com_o_aviso`; `Cancelar_pede_confirmacao_e_voltar_nao_cancela`; `Ficha_de_colega_nao_mostra_editar_nem_cancelar`; `Lista_da_diretoria_abre_em_aguardando_e_traz_todas_com_o_filtro_todas`.
- [ ] **Step 2: Rodar e ver falhar.** `dotnet test --filter "FullyQualifiedName~Solicitacoes|FullyQualifiedName~RotasDoCliente|FullyQualifiedName~Layout"`.
- [ ] **Step 3: Implementar** as regras 1 a 9.
- [ ] **Step 4: Rodar e ver passar**, e rodar `TelasNoDesignSystemTests` e `WebSoPeloDespachanteTests`.
- [ ] **Step 5: Verificar no navegador** em ambiente descartável (ver memória do projeto "Verificação no navegador em ambiente descartável"): abrir como cliente com dois anexos, editar, remover um anexo, cancelar; abrir como Diretoria a lista e a ficha; abrir como Produção `/solicitacoes`. Desktop, tablet e mobile. Registrar no relatório o que foi visto.
- [ ] **Step 6: Commit** `feat(web): telas das solicitações do cliente`.

---

### Task 7: Emendas das specs e verificação final

**Files:**
- Modify: `docs/superpowers/specs/2026-11-13-solicitacoes-do-cliente-design.md` (Estado; registro do que a Fase A fixou)
- Modify: `docs/superpowers/specs/2026-10-08-portal-do-cliente-design.md` (lista das requisições abertas na seção 3; menu e páginas na seção 6; "Envio de arquivos pelo cliente" sai da seção 12)
- Modify: `docs/superpowers/specs/2026-10-06-modelagem-dominio-design.md` (agregado novo, categoria 6 na matriz de acesso, lista do portão)

- [ ] **Step 1: Emendar as três specs** com o que a fase fixou, incluindo as decisões deste plano que não estavam no spec (limite de 10 documentos, quem altera, trecho de 140 caracteres, lista da Diretoria abrindo em "Aguardando").
- [ ] **Step 2: Suíte inteira.** `dotnet test` — compare com a linha de base do início: só testes novos e os alterados listados no relatório.
- [ ] **Step 3: Commit** `docs: emendas da fase A das solicitações`.
