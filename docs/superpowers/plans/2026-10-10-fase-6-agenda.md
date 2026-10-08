# Fase 6 — Agenda e pendências: Plano de Implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Dar ao escritório uma agenda de eventos (audiências, reuniões, prazos) com participantes, vínculos opcionais com cliente, processo e atividade, e aviso de sobreposição; e fechar duas pendências das fases anteriores: avisar a Diretoria do que fica pendente ao inativar um usuário, e permitir seguir na auditoria tudo o que aconteceu com um registro.

**Architecture:** `Evento` é o agregado do contexto Agenda, com a coleção de `EventoParticipante` (entidade filha, derivada de `Entidade`) e o mesmo controle de concorrência dos outros agregados com filhas (`xmin` + contador). A sobreposição de horário é uma consulta: avisa e não impede. Horários são gravados em UTC; a tela converte do fuso do escritório.

**Tech Stack:** .NET 10, Blazor Interactive Server, EF Core 10 + Npgsql, PostgreSQL 17, xUnit 2.9, FluentAssertions 7.2, Testcontainers.

**Spec:** `docs/superpowers/specs/2026-10-06-modelagem-dominio-design.md` (seções 5 — matriz de acesso —, 9, 10 e 11).

## Decisões deste plano

- **Quem vê um evento**: a Diretoria vê todos; os demais veem os eventos em que participam **ou que criaram** (a spec diz "em que participa"; quem criou pode editar e excluir, então precisa continuar vendo mesmo se sair da lista de participantes).
- **O criador entra como participante na criação**, mesmo que não se inclua; depois pode ser retirado, desde que reste ao menos um participante.
- **Qualquer usuário pode incluir outros usuários ativos como participantes.** Para isso existe uma consulta que devolve só Id e nome dos usuários ativos, aberta a qualquer usuário autenticado.
- **Usuário inativo** não entra como participante novo; quem já participava permanece até ser retirado.
- **Vínculos**: cliente, processo e atividade são opcionais e independentes; a única regra cruzada é a da spec (com cliente e processo, o cliente tem de ser do processo). Para vincular uma atividade, quem cria ou edita precisa poder vê-la. Cliente inativo não entra em vínculo novo.
- **Sobreposição**: ao salvar, o caso de uso devolve os participantes que já têm outro evento no mesmo horário (nome e intervalo, sem o título do outro evento — o usuário pode não ter acesso a ele). É aviso; a gravação acontece.
- **Duração**: de 1 minuto a 7 dias.
- **Quem não pode ver um evento recebe "não encontrado"**, com a tentativa registrada, como nos documentos e nas atividades.
- **Edição e exclusão enviam a versão vista** (`VersaoEsperada`), como nas atividades.
- **A listagem é por período** (no máximo 92 dias), sem paginação.
- **Pendências do usuário a inativar**: a tela de usuários passa a mostrar, antes de inativar, quantas partes pendentes, revisões em aberto e eventos futuros o usuário tem. É aviso; a inativação continua permitida.
- **Auditoria por registro**: a consulta de auditoria ganha o filtro pelo identificador do registro e passa a mostrar o identificador de correlação (o que foi gravado junto).

## Como ler este plano

Cada tarefa traz as **regras** (o contrato) e a lista de **testes obrigatórios** (nome e o que cada um confere). O código é escrito pelo implementador no estilo do que já existe; onde um detalhe é sutil, o plano traz o trecho. Nunca apague um teste nem afrouxe uma asserção. O texto de `Dados` gravado em `jsonb` volta reformatado pelo PostgreSQL: compare JSON pela estrutura. Nos testes de integração, nunca escolha um registro de auditoria pela posição: selecione por operação, evento ou entidade.

## Estado de partida (fase 5, na `main`)

- Domínio: `Atividade` é o modelo de agregado com filhas, `Versao` (`xmin`) e contador; `VinculoDaAtividade.Criar(clienteId, processo, areaAtuacaoId)` é o modelo de vínculo validado.
- Aplicação: `AcessoAAtividade` (regra de visibilidade, resposta "não encontrada" + evento `acesso_negado`); comandos com `VersaoEsperada`; `IUsuarioRepositorio.ObterVariosAsync`; `IAtividadeRepositorio`; `ConsultarAuditoria` com `FiltroDeAuditoria(DeUtc, AteUtc, UsuarioId, Entidade, Operacao)`; `DefinirAtivoUsuario`.
- Web: `Atividades.razor` + `FichaDaAtividade.razor` (lista, ficha por clique, painéis recarregados antes de abrir); `Usuarios.razor`; `Auditoria.razor`; `FusoDoEscritorio` (`ParaLocal`, `InicioDoDiaEmUtc`, `Formatar`).
- Testes: 577 unitários e 319 de integração passando.

## Global Constraints

- TFM único `net10.0` em `Directory.Build.props`. `Nullable` e `TreatWarningsAsErrors=true`. Versões de pacote só em `Directory.Packages.props`. Nenhum pacote novo.
- `Domain` não referencia nada. `Application` não referencia EF Core, Npgsql, `Infrastructure` nem `Web`.
- Dentro da `Application`: `Agenda` pode usar `Identidade`, `Cadastros` e `Atividades`; nenhum deles usa `Agenda`.
- Código, nomes de tabela e mensagens em português. Tabelas e colunas em `snake_case`. Datas e horas em UTC no domínio, na aplicação e no banco.
- Exclusão sempre lógica, inclusive das entidades filhas. Agregados se referenciam só por Id.
- Toda alteração de `Entidade` gera registro de auditoria na mesma transação. Toda propriedade nova é classificada em `PropriedadesAuditadasTests`. Todo tipo mapeado deriva de `Entidade`. Nenhuma gravação por fora dos interceptores.
- Matriz de acesso: agenda — Diretoria todos os eventos; Produção os eventos em que participa (ou que criou); qualquer usuário cria os seus.
- Autorização verificada no manipulador; a interface apenas oculta. Um recurso que o usuário não pode ver responde `NaoEncontrado`, e a tentativa gera o evento `acesso_negado` com o recurso.
- Consultas são `IConsultaAuditavel`.
- Um caso de uso por arquivo. Novas dependências de construtor e novos membros posicionais de registros entram por último.

## Review Focus

1. **Um usuário de Produção pede a agenda de outra pessoa ou um evento em que não participa**: a lista traz só os dele, qualquer que seja o filtro; o evento responde "não encontrado". (Tarefa 2)
2. **Dois eventos do mesmo participante no mesmo horário**: o segundo é gravado e o aviso diz quem está em conflito e quando; eventos encostados (um termina quando o outro começa) não são conflito. (Tarefas 1 e 2)
3. **Evento marcado perto da meia-noite ou na virada do horário de verão**: aparece no dia certo no fuso do escritório. (Tarefa 3)
4. **O criador de Produção é retirado dos participantes por um diretor**: continua vendo, editando e excluindo o evento. (Tarefa 2)
5. **Cliente e processo informados, cliente fora do processo**: recusado. (Tarefas 1 e 2)
6. **Inativar um usuário com partes pendentes**: a tela mostra as contagens antes de confirmar. (Tarefa 4)

---

### Task 1: `Evento` no domínio

**Files:**
- Create: `src/AtividadesJuridicas.Domain/Agenda/Evento.cs`, `EventoParticipante.cs`, `TiposDoEvento.cs`
- Test: `tests/AtividadesJuridicas.UnitTests/Domain/EventoTests.cs`

**Interfaces — Produces (namespace `AtividadesJuridicas.Domain.Agenda`):**
- `enum TipoEvento { Audiencia = 1, Reuniao = 2, Prazo = 3, Outro = 4 }`
- `record DadosDoEvento(string Titulo, string? Descricao, string? Local, TipoEvento Tipo, DateTime InicioEm, int DuracaoEstimadaMinutos)`
- `record VinculoDoEvento` com `Guid? ClienteId`, `Guid? ProcessoId`, `Guid? AtividadeId`; `static VinculoDoEvento Nenhum`; `static VinculoDoEvento Criar(Guid? clienteId, Processo? processo, Guid? atividadeId)` (construtor privado, como `VinculoDaAtividade`)
- `record PessoaDoEvento(Guid UsuarioId, bool Ativo)`
- `record AlteracaoDeParticipantes(IReadOnlyList<Guid> Adicionados, IReadOnlyList<Guid> Removidos)` com `bool Houve`
- `Evento.Criar(DadosDoEvento dados, VinculoDoEvento vinculo, PessoaDoEvento criador, IReadOnlyCollection<PessoaDoEvento> participantes)`
- `evento.Alterar(DadosDoEvento dados, VinculoDoEvento vinculo)`
- `evento.DefinirParticipantes(IReadOnlyCollection<PessoaDoEvento> participantes): AlteracaoDeParticipantes`
- `evento.Participa(Guid usuarioId)`, `evento.FimEm` (`InicioEm` + duração), `evento.Sobrepoe(DateTime inicioUtc, DateTime fimUtc)`
- Propriedades: `Titulo`, `Descricao`, `Local`, `Tipo`, `InicioEm`, `DuracaoEstimadaMinutos`, `ClienteId`, `ProcessoId`, `AtividadeId`, `CriadorId` (`Guid`), `Versao` (`uint`, `[NaoAuditar]`), `VersaoDosParticipantes` (`int`, `[NaoAuditar]`), `Participantes` (`IReadOnlyCollection<EventoParticipante>`, campo `_participantes`)
- `EventoParticipante : Entidade { EventoId, UsuarioId }` com construtor `internal`
- Constantes: `TamanhoMaximoTitulo = 200`, `TamanhoMaximoDescricao = 4000`, `TamanhoMaximoLocal = 200`, `DuracaoMaximaMinutos = 10080`

**Regras:**

1. Título obrigatório (`"O título do evento é obrigatório."`), aparado, até 200 (`"O título do evento deve ter até 200 caracteres."`). Descrição e local opcionais, aparados (vazio vira nulo), com os limites acima (`"A descrição do evento deve ter até 4000 caracteres."`, `"O local do evento deve ter até 200 caracteres."`). Tipo fora do enum: `"Tipo de evento inválido."`.
2. `InicioEm` precisa ser UTC (`ArgumentException` caso contrário). Duração de 1 a 10080 minutos (`"A duração deve ser maior que zero."`, `"A duração deve ser de no máximo 7 dias."`).
3. Vínculo: sem processo, cliente e atividade livres; com processo e cliente, o cliente tem de ser um dos clientes do processo (`"O cliente do evento deve ser um dos clientes do processo."`); com processo e sem cliente é permitido. `Guid.Empty` em cliente ou atividade é erro de domínio (`"Cliente inválido."`, `"Atividade inválida."`).
4. Criar: `criador` obrigatório e com `UsuarioId` não vazio; ele é sempre incluído entre os participantes, estando ou não na lista. Os demais participantes informados precisam estar ativos (`"Usuário inativo não pode ser incluído como participante."`); o criador precisa estar ativo. Usuário repetido na lista: `"Um usuário não pode se repetir entre os participantes."`. `Guid.Empty`: `"Usuário inválido."`. Elemento nulo: `ArgumentException`.
5. `DefinirParticipantes`: deixa exatamente os informados; ao menos um (`"O evento precisa de ao menos um participante."`); sem repetição; quem é novo precisa estar ativo, quem já participava pode permanecer mesmo inativo; a entrada de quem fica é preservada; tudo validado antes de qualquer mudança; `VersaoDosParticipantes` aumenta só quando algo muda. O criador pode ser retirado.
6. `Sobrepoe(inicio, fim)`: verdadeiro quando os intervalos `[InicioEm, FimEm)` e `[inicio, fim)` têm interseção; intervalos encostados não se sobrepõem.
7. `Alterar` não mexe em participantes nem em `CriadorId`.

**Testes obrigatórios (`EventoTests`):** criação com dados aparados e criador incluído (na lista ou fora dela, sem duplicar); cada validação da regra 1 e 2 (teoria com mensagens); `InicioEm` não UTC; vínculo (livre sem processo; cliente fora do processo recusado; processo sem cliente aceito; `Guid.Empty`); participante inativo novo recusado e inativo antigo mantido; repetição; lista vazia em `DefinirParticipantes`; alteração devolve adicionados/removidos e preserva a instância de quem fica; contador só muda quando há mudança; nada muda quando `DefinirParticipantes` lança (com lista já preenchida e remoções no mesmo pedido); criador pode ser retirado; `FimEm`; `Sobrepoe` (dentro, parcial antes, parcial depois, contendo, encostado antes, encostado depois, disjunto); `Alterar` não muda participantes nem criador.

- [ ] testes → falham → implementar → `dotnet test tests/AtividadesJuridicas.UnitTests` e `dotnet build` (zero avisos) → commit:

```bash
git add src/AtividadesJuridicas.Domain/Agenda tests/AtividadesJuridicas.UnitTests/Domain/EventoTests.cs
git commit -m "feat(agenda): Evento e participantes no domínio"
```

---

### Task 2: Casos de uso, persistência e auditoria da agenda

**Files:**
- Create em `src/AtividadesJuridicas.Application/Agenda/`: `PortasDeAgenda.cs`, `AcessoAoEvento.cs`, `SalvarEvento.cs`, `ExcluirEvento.cs`, `ListarEventos.cs`, `ObterEvento.cs`, `ListarUsuariosParaAgenda.cs`
- Create: `src/AtividadesJuridicas.Infrastructure/Persistencia/Configuracoes/EventoConfiguracao.cs`, `Repositorios/EventoRepositorio.cs`
- Modify: `AppDbContext.cs`, `DependencyInjection.cs`; `src/AtividadesJuridicas.Application/Identidade/Portas.cs` (consulta de usuários ativos)
- Create: migration `Agenda`
- Modify: `tests/AtividadesJuridicas.UnitTests/Application/Falsos.cs`; `tests/AtividadesJuridicas.IntegrationTests/PropriedadesAuditadasTests.cs`
- Test: `tests/AtividadesJuridicas.UnitTests/Application/AgendaTests.cs`; `tests/AtividadesJuridicas.IntegrationTests/AgendaTests.cs`

**Interfaces — Produces (namespace `AtividadesJuridicas.Application.Agenda`):**
- `static class AgendaOpcoes { const int PeriodoMaximoEmDias = 92; }`
- `record ParticipanteDetalhe(Guid UsuarioId, string? Nome, bool Ativo)`
- `record EventoResumo(Guid Id, string Titulo, TipoEvento Tipo, DateTime InicioEm, DateTime FimEm, string? Local, string? ClienteNome, string? ProcessoTitulo, IReadOnlyList<string> Participantes)`
- `record AcoesDoEvento(bool Editar, bool Excluir)`
- `record EventoDetalhe(Guid Id, string Titulo, string? Descricao, string? Local, TipoEvento Tipo, DateTime InicioEm, int DuracaoEstimadaMinutos, DateTime FimEm, Guid? ClienteId, string? ClienteNome, Guid? ProcessoId, string? ProcessoTitulo, Guid? AtividadeId, string? AtividadeTitulo, Guid CriadorId, string? CriadorNome, IReadOnlyList<ParticipanteDetalhe> Participantes, AcoesDoEvento Acoes, uint Versao)`
- `record SobreposicaoDeAgenda(Guid UsuarioId, string? UsuarioNome, DateTime InicioEm, DateTime FimEm)`
- `record EventoSalvo(Guid Id, IReadOnlyList<SobreposicaoDeAgenda> Sobreposicoes)`
- `record FiltroDeEventos(DateTime DeUtc, DateTime AteUtc, Guid? ParticipanteId, TipoEvento? Tipo, Guid? VisivelPara)` — `VisivelPara` não nulo restringe aos eventos em que esse usuário participa ou que ele criou
- `interface IEventoRepositorio { Task<Evento?> ObterPorIdAsync(Guid id, CancellationToken ct); Task<IReadOnlyList<EventoResumo>> ListarAsync(FiltroDeEventos filtro, CancellationToken ct); Task<IReadOnlyList<SobreposicaoDeAgenda>> SobreposicoesAsync(Guid? excetoEventoId, IReadOnlyCollection<Guid> usuarioIds, DateTime inicioUtc, DateTime fimUtc, CancellationToken ct); void Adicionar(Evento evento); void Remover(Evento evento); }`
- `record SalvarEvento(Guid? Id, string Titulo, string? Descricao, string? Local, TipoEvento Tipo, DateTime InicioEm, int DuracaoEstimadaMinutos, Guid? ClienteId, Guid? ProcessoId, Guid? AtividadeId, IReadOnlyList<Guid> Participantes, uint? VersaoEsperada = null) : IRequisicao<Resultado<EventoSalvo>>`
- `record ExcluirEvento(Guid Id, uint? VersaoEsperada = null) : IRequisicao<Resultado>`
- `record ListarEventos(DateTime DeUtc, DateTime AteUtc, Guid? ParticipanteId = null, TipoEvento? Tipo = null) : IRequisicao<Resultado<IReadOnlyList<EventoResumo>>>, IConsultaAuditavel` (`TipoRecurso = "Evento"`, `Listou`, `Detalhe` com os filtros)
- `record ObterEvento(Guid Id) : IRequisicao<Resultado<EventoDetalhe>>, IConsultaAuditavel` (`"Evento"`, `Visualizou`, `RecursoId`)
- `record UsuarioDaAgenda(Guid Id, string Nome)`; `record ListarUsuariosParaAgenda : IRequisicao<Resultado<IReadOnlyList<UsuarioDaAgenda>>>` — para qualquer usuário autenticado; devolve só Id e nome dos usuários ativos, por nome. Não é consulta auditável (apoio de formulário).
- Em `Application/Identidade/Portas.cs`, `IUsuarioRepositorio` ganha `Task<IReadOnlyList<(Guid Id, string Nome)>> ListarNomesDosAtivosAsync(CancellationToken ct)` (a `Identidade` não usa tipos da `Agenda`: o caso de uso é que monta `UsuarioDaAgenda`).

**Regras:**

1. **Ver**: `AcessoAoEvento.PodeVer(ator, evento)` = Diretoria, ou participa, ou é o criador. `PodeGerenciar` (editar/excluir) = Diretoria ou criador. Para quem não pode ver, toda requisição com o Id de um evento responde `NaoEncontrado` com `"Evento não encontrado."` e grava o evento `acesso_negado` (entidade `"Evento"`, o Id, `{ requisicao, resposta = "nao_encontrado" }`, `CancellationToken.None`). Evento inexistente: o mesmo `NaoEncontrado`, sem evento de segurança. Quem vê mas não gerencia e tenta editar ou excluir: `AcessoNegado`.
2. **Sem usuário**: `AcessoNegado` em tudo.
3. **`SalvarEvento` — criar**: qualquer usuário autenticado. O ator é o criador. Carrega os participantes informados (usuário inexistente → `Validacao` `"Usuário não encontrado."`). Cliente informado tem de existir (`"Cliente não encontrado."`) e estar ativo (`"O cliente está inativo."`); processo tem de existir (`"Processo não encontrado."`); atividade tem de existir **e ser visível ao ator** pela regra das atividades (`AcessoAAtividade.PodeVer`) — caso contrário `Validacao` `"Atividade não encontrada."` (a mesma mensagem para inexistente e invisível).
4. **`SalvarEvento` — editar**: regra 1; `VersaoEsperada` diferente da versão carregada → `Conflito` `"O evento foi alterado por outra pessoa. Recarregue e tente novamente."`, antes de qualquer validação. Cliente e atividade só são revalidados (existência, ativo, visibilidade) quando mudam em relação ao que o evento já tinha. Aplica `Alterar` e `DefinirParticipantes`.
5. **Sobreposição**: depois de validar e antes de responder, consulta `SobreposicoesAsync` para os participantes finais no intervalo do evento, desconsiderando o próprio evento; o resultado vai em `EventoSalvo.Sobreposicoes` (uma entrada por participante e evento em conflito, ordenada por nome e início). Não impede a gravação. Eventos excluídos não contam.
6. **`ExcluirEvento`**: regra 1 + `PodeGerenciar` + `VersaoEsperada`. Exclusão lógica do evento e dos participantes.
7. **`ListarEventos`**: `DeUtc`/`AteUtc` em UTC, `De < Ate`, período de no máximo 92 dias (`Validacao`: `"Período inválido."` / `"O período deve ter no máximo 92 dias."`). Traz os eventos que **tocam** o período (começam antes do fim e terminam depois do início), em ordem de início (desempate por Id). Para quem não é da Diretoria, `VisivelPara` é sempre o próprio usuário, qualquer que seja o `ParticipanteId` pedido (que continua valendo como filtro adicional).
8. **`ObterEvento`**: regra 1. Devolve nomes (cliente, processo, atividade, criador, participantes com `Ativo`) e `Acoes`. O título da atividade vinculada só é devolvido se o ator puder ver a atividade; caso contrário `AtividadeTitulo` vem nulo (o `AtividadeId` continua).
9. **Erros**: `ExcecaoDeDominio` → `Validacao`; `ExcecaoDeConcorrencia` e `ExcecaoDeConflito` → `Conflito` com a mensagem da regra 4.
10. **Mapeamento**: tabela `eventos` (`Titulo` 200, `Descricao` 4000, `Local` 200, `Tipo` int, `InicioEm` timestamptz, `Versao` como `xmin`, `VersaoDosParticipantes` default 0; coluna calculada não é necessária — `FimEm` não é mapeado); FKs `Restrict` para `clientes`, `processos`, `atividades`, `usuarios` (criador); índice em `inicio_em`. Tabela `eventos_participantes` (FK `Restrict` para `usuarios`; índice único `(evento_id, usuario_id)` filtrado por `excluido_em IS NULL`; índice em `usuario_id`), filha com `ClientCascade` e acesso por campo. Na consulta de sobreposição e na listagem, o fim é calculado no banco a partir de `inicio_em` e da duração (ou filtre por um intervalo ampliado em 7 dias e refine em memória — explique no relatório a opção e por que está correta).
11. **`PropriedadesAuditadasTests`**: `Evento` = `AtividadeId`, `ClienteId`, `CriadorId`, `Descricao`, `DuracaoEstimadaMinutos`, `InicioEm`, `Local`, `ProcessoId`, `Tipo`, `Titulo`; `EventoParticipante` = `EventoId`, `UsuarioId`.

**Testes obrigatórios:**

Unitários (`AgendaTests`, com `EventoRepositorioFalso` de mesma semântica: toca o período; `VisivelPara`; sobreposição com intervalos semiabertos):
- `Qualquer_usuario_cria_evento_e_entra_como_participante`
- `Producao_ve_so_os_eventos_em_que_participa_ou_que_criou_qualquer_que_seja_o_filtro`
- `Diretoria_ve_todos_e_filtra_por_participante_e_tipo`
- `Evento_que_o_usuario_nao_ve_responde_nao_encontrado_e_fica_registrado` (obter, editar, excluir)
- `Participante_que_nao_criou_ve_mas_nao_edita_nem_exclui` (`AcessoNegado`)
- `Criador_retirado_dos_participantes_continua_vendo_e_gerenciando`
- `Sobreposicao_e_avisada_e_nao_impede` — e `Eventos_encostados_nao_sao_sobreposicao`, `Editar_nao_conflita_consigo_mesmo`, `Evento_excluido_nao_gera_aviso`
- `Aviso_de_sobreposicao_nao_revela_o_titulo_do_outro_evento` (o tipo nem tem o campo: teste que o registro só carrega usuário e intervalo)
- `Vinculos_sao_validados` — cliente inexistente/inativo, processo inexistente, cliente fora do processo, atividade inexistente, atividade que o ator não vê (mesma mensagem), cliente inativo que já estava no evento permanece
- `Participante_inexistente_ou_inativo_e_recusado`
- `Versao_esperada_diferente_e_conflito_em_editar_e_excluir`
- `Periodo_invalido_ou_longo_demais_e_recusado`
- `Listagem_traz_os_eventos_que_tocam_o_periodo_em_ordem`
- `Ficha_do_evento_esconde_o_titulo_da_atividade_que_o_usuario_nao_ve`
- `Acoes_do_evento_seguem_o_papel`
- `Lista_de_usuarios_para_a_agenda_traz_so_ativos_com_id_e_nome_para_qualquer_usuario`
- `Consultas_sao_auditaveis`

Integração (`AgendaTests`, PostgreSQL):
- `Evento_vai_e_volta_do_banco_com_participantes_e_vinculos`
- `Listagem_por_periodo_no_banco` — evento que começa antes e termina dentro, que começa dentro e termina depois, que contém o período, encostado antes e encostado depois (estes dois de fora)
- `Producao_so_lista_e_abre_os_seus_no_banco` — `acesso_negado` com entidade `Evento`; sem `Visualizou` para a tentativa negada
- `Sobreposicao_e_calculada_no_banco` — conflito parcial, encostado (sem aviso), evento excluído (sem aviso), o próprio evento na edição (sem aviso)
- `Trocar_participantes_e_logico_e_auditado` — entrada antiga com `excluido_em`, `Exclusao`/`Criacao` de `EventoParticipante`; participante removido pode voltar
- `Duas_edicoes_simultaneas_a_segunda_recebe_conflito` — dois escopos, determinístico
- `Formulario_desatualizado_e_recusado` — `VersaoEsperada` antiga em editar e excluir; com a atual, sucesso
- `Excluir_e_logico_para_o_evento_e_os_participantes`
- `Banco_recusa_apagar_fisicamente_evento_com_participantes` e `Banco_recusa_participante_repetido_vivo`
- `Acessos_ficam_no_registro_de_consultas`

- [ ] testes → falham → implementar (Parte A aplicação + unitários; Parte B persistência + integração) → migration `Agenda` (só as duas tabelas; apague a linha `xmin` se o EF a emitir, como nas migrations `Processos` e `Atividades`; confira e registre) → `dotnet test` inteiro → commit:

```bash
git add src tests
git commit -m "feat(agenda): casos de uso, persistência e auditoria da agenda"
```

---

### Task 3: Tela da agenda

**Files:**
- Create: `src/AtividadesJuridicas.Web/Components/Pages/Agenda.razor`; em `Components/Agenda/`: `FichaDoEvento.razor`, `FormularioDeEvento.razor`
- Modify: `src/AtividadesJuridicas.Web/Infra/FusoDoEscritorio.cs` (conversão de horário local para UTC), `Components/Layout/MainLayout.razor`, `Components/_Imports.razor`, `Components/Pages/Home.razor`
- Test: `tests/AtividadesJuridicas.IntegrationTests/AgendaWebTests.cs`, `FusoDoEscritorioTests.cs` (acréscimos)

**O que a tela faz:**

- **`FusoDoEscritorio.ParaUtc(DateTime local)`**: converte uma data e hora do escritório para UTC. Horário inexistente (salto do horário de verão) é empurrado para o primeiro instante válido; horário ambíguo (hora repetida) vale a primeira ocorrência. Testes com um fuso que tem horário de verão (por exemplo `America/New_York`) e com `America/Sao_Paulo`.
- **Página `/agenda`** (para todos os perfis; item "Agenda" no menu depois de "Atividades"; link em `Home.razor`):
  - "Carregando…" até a consulta de acesso terminar; perda de acesso encerra a tela.
  - Mostra uma **semana** (segunda a domingo, no fuso do escritório), com "Semana anterior", "Hoje" e "Próxima semana". A semana inicial é a de hoje. O período pedido ao servidor é do início da segunda ao início da segunda seguinte (`InicioDoDiaEmUtc`).
  - Eventos agrupados por dia (cabeçalho com dia da semana e data); cada evento: hora de início e de fim (no fuso do escritório), título, tipo (nomes em português: Audiência, Reunião, Prazo, Outro), local, cliente, processo, participantes, botão "Abrir". Um evento que atravessa a meia-noite aparece em cada dia que toca. Dia sem evento mostra "Sem eventos."; falha de consulta nunca aparece como semana vazia.
  - Filtros: tipo; e, para a Diretoria, participante (usuários de `ListarUsuariosParaAgenda`).
  - "Novo evento" (para todos) abre `FormularioDeEvento`: título, tipo, data, hora (campos separados, no fuso do escritório), duração em minutos, local, descrição, cliente (busca, como no formulário de atividade), processo (busca; ao escolher um processo com cliente escolhido fora dele, o servidor recusa e a mensagem aparece), atividade (busca por título em `ListarAtividades` — o usuário só encontra as que vê), participantes (caixas de seleção sobre `ListarUsuariosParaAgenda`, com o próprio usuário já marcado). Sem `EditForm` em volta de campos de busca; `maxlength` pelos limites do domínio.
  - Ao salvar: se `EventoSalvo.Sobreposicoes` não estiver vazio, mostra um aviso (não um erro) listando "Fulano já tem evento das HH:mm às HH:mm (dd/MM)" — o evento **foi** gravado e a mensagem diz isso.
- **`FichaDoEvento`** (parâmetros `EventoId`, `OnMudou`, `OnFechar`, `OnAcessoEncerrado`; aberta por "Abrir", com `@key`): mostra todos os campos, criador, participantes (inativos marcados), vínculos (o título da atividade só quando vier); "Editar" e "Excluir" só quando `Acoes` permite; antes de abrir a edição a ficha é recarregada; editar e excluir enviam `VersaoEsperada`; conflito ou "não encontrado" fecha o painel, recarrega e mostra a mensagem; excluir em dois cliques; guarda de ação em andamento; depois de excluir, `OnMudou` e `OnFechar`.

**Testes obrigatórios:**
- `FusoDoEscritorioTests`: `ParaUtc` em horário normal, inexistente e ambíguo; ida e volta com `ParaLocal`.
- `AgendaWebTests` (HTML pré-renderizado da semana atual; crie os eventos com horários relativos ao "agora" do teste, dentro da semana corrente no fuso do escritório, e calcule no teste o que deve aparecer): `Diretor_ve_os_eventos_da_semana`, `Producao_ve_so_os_seus`, `Menu_mostra_agenda_para_todos`, `Anonimo_e_redirecionado_ao_login`, `Listagem_fica_no_registro_de_consultas`.
- O comportamento interativo (navegar semanas, formulário, aviso de sobreposição, ficha) não tem teste automatizado: descreva no relatório, caminho por caminho, como verificou por leitura — em especial o agrupamento por dia de um evento perto da meia-noite, e que o aviso de sobreposição deixa claro que o evento foi gravado.

- [ ] testes → falham → implementar → `dotnet test` inteiro → commit:

```bash
git add src tests
git commit -m "feat(web): agenda semanal com eventos, participantes e aviso de sobreposição"
```

---

### Task 4: Pendências — aviso ao inativar usuário e auditoria por registro

**Files:**
- Create: `src/AtividadesJuridicas.Application/Identidade/ObterPendenciasDoUsuario.cs`
- Modify: `src/AtividadesJuridicas.Application/Identidade/Portas.cs` (porta `IPendenciasDoUsuario`), `src/AtividadesJuridicas.Infrastructure/...` (implementação e DI)
- Modify: `src/AtividadesJuridicas.Application/Auditoria/ConsultaDeAuditoria.cs`, `ConsultarAuditoria.cs`; `src/AtividadesJuridicas.Infrastructure/Persistencia/Repositorios/ConsultaDeAuditoria.cs`
- Modify: `src/AtividadesJuridicas.Web/Components/Pages/Usuarios.razor`, `Auditoria.razor`
- Test: unitários e de integração correspondentes

**Regras:**

1. **Pendências do usuário.** Porta em `Identidade` (que não usa tipos de `Atividades` nem de `Agenda`): `interface IPendenciasDoUsuario { Task<PendenciasDoUsuario> ContarAsync(Guid usuarioId, DateTime agoraUtc, CancellationToken ct); }` com `record PendenciasDoUsuario(int PartesPendentes, int RevisoesEmAberto, int EventosFuturos)`, implementada na infraestrutura:
   - `PartesPendentes`: entradas vivas de responsável com `ConcluidoEm` nulo em atividades vivas nos status `Direcionada`, `ParaCorrecao` ou `Reaberta`;
   - `RevisoesEmAberto`: atividades vivas não concluídas (`Status != Concluida`) em que o usuário é o revisor;
   - `EventosFuturos`: eventos vivos em que participa e cujo fim é posterior a agora.
   Caso de uso `ObterPendenciasDoUsuario(Guid Id) : IRequisicao<Resultado<PendenciasDoUsuario>>`, só Diretoria (`AcessoNegado`), usuário inexistente → `NaoEncontrado`. Não é consulta auditável (apoio de tela).
2. **Tela de usuários.** "Inativar" passa a ter dois cliques: o primeiro busca as pendências e mostra, junto do botão "Confirmar inativação", o texto com as contagens que não forem zero ("Este usuário tem 3 partes pendentes, 1 revisão em aberto e 2 eventos futuros. Elas continuam com ele até serem redistribuídas." ou "Este usuário não tem pendências."). "Ativar" continua com um clique. Guarda de ação em andamento.
3. **Auditoria por registro.** `FiltroDeAuditoria` ganha, por último, `Guid? EntidadeId = null` e `Guid? IdCorrelacao = null`; a consulta filtra por eles no banco. `RegistroAuditoriaResumo` ganha, por último, `Guid? IdCorrelacao`. A tela de auditoria ganha os dois campos de filtro (texto; um valor que não seja um Guid mostra "Identificador inválido." e não consulta) e, em cada linha, o identificador do registro e o de correlação como botões que aplicam o filtro correspondente ("ver tudo deste registro", "ver o que foi gravado junto").

**Testes obrigatórios:**
- Unitários: `ObterPendenciasDoUsuario` — só Diretoria; usuário inexistente; devolve o que a porta contou. `ConsultarAuditoria` — o `Detalhe` da consulta inclui os filtros novos.
- Integração: `Pendencias_do_usuario_contam_partes_revisoes_e_eventos` — monta: uma atividade direcionada com parte pendente dele (conta), outra em que ele já concluiu (não conta), uma `Feita` aguardando revisão em que ele é o revisor (conta), uma `Concluida` em que ele é o revisor (não conta), uma excluída (não conta), um evento futuro (conta), um passado (não conta), um futuro excluído (não conta), um futuro de que foi retirado (não conta). `Auditoria_filtra_por_registro_e_por_correlacao` — depois de direcionar uma atividade: o filtro por `EntidadeId` da atividade traz só as linhas dela; o filtro pelo `IdCorrelacao` de uma delas traz também as linhas dos responsáveis e do histórico gravadas junto; os dois filtros combinam com os existentes.
- Página: `Auditoria` continua passando nos testes existentes; acrescente o que o HTML pré-renderizado permitir (os dois campos de filtro presentes).
- Leitura: os dois cliques da inativação; o identificador inválido no filtro.

- [ ] testes → falham → implementar → `dotnet test` inteiro → commit:

```bash
git add src tests
git commit -m "feat(identidade): pendências antes de inativar usuário e auditoria por registro e por correlação"
```
