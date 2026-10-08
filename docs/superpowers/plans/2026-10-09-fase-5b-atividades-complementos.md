# Fase 5B — Atividades (complementos): Plano de Implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Completar as atividades com o que depende do núcleo da fase 5A: as regras de processo que olham para as atividades, os comentários, e os documentos de atividade (anexados na atividade e visíveis também na ficha do cliente).

**Architecture:** As dependências entre contextos seguem a spec (Atividades depende de Cadastros e de Documentos, nunca o contrário): quando Cadastros ou Documentos precisam saber algo das atividades, pedem por uma porta própria, implementada na infraestrutura. O comentário é uma entidade com repositório próprio, para que comentar não regrave a linha da atividade (o que invalidaria formulários abertos). O acesso a um documento de atividade não é uma lista gravada: é derivado, a cada consulta, dos responsáveis atuais da atividade.

**Tech Stack:** .NET 10, Blazor Interactive Server, EF Core 10 + Npgsql, PostgreSQL 17, xUnit 2.9, FluentAssertions 7.2, Testcontainers.

**Spec:** `docs/superpowers/specs/2026-10-06-modelagem-dominio-design.md` (seções 6 — Processo —, 7 — Documentos de atividade —, 8, 10 e 11).

## Decisões deste plano

- **Acesso a documento de atividade é derivado, não gravado.** A spec fala em "lista de acesso inicial: responsáveis" e em incluir/retirar o acesso quando o responsável entra ou sai. Em vez de manter cópias, o acesso é calculado a cada consulta: Diretoria, ou responsável atual da atividade. O resultado é o mesmo, sem risco de a lista ficar desatualizada. A lista de acesso de um documento de atividade não é editável.
- **Quem anexa**: quem vê a atividade (Diretoria e responsáveis). Renomear e excluir: Diretoria.
- **Comentário é imutável e tem repositório próprio.** Comentam e leem os comentários quem vê a atividade, em qualquer status.
- **Troca de área do processo alcança todas as atividades dele**, inclusive as concluídas, na mesma transação e com registro de auditoria por atividade.
- **Não se retira de um processo um cliente que tenha atividade viva naquele processo** (atividade excluída não conta).
- **Excluir uma atividade não exclui os seus documentos nem os seus comentários.** Os documentos continuam na ficha do cliente para a Diretoria; para a Produção deixam de aparecer (não há mais responsáveis vivos). Os comentários ficam inacessíveis junto com a atividade.
- **Corridas aceitas**: uma atividade sendo ligada a um processo no mesmo instante em que o cliente é retirado dele, ou em que a área muda, pode ficar inconsistente; editar a atividade a corrige. Registrado como pendência.

## Como ler este plano

Cada tarefa traz as **regras** (o contrato) e a lista de **testes obrigatórios** (nome e o que cada um confere). O código é escrito pelo implementador no estilo do que já existe; onde um detalhe é sutil, o plano traz o trecho. Nunca apague um teste nem afrouxe uma asserção. O texto de `Dados` gravado em `jsonb` volta reformatado pelo PostgreSQL: compare JSON pela estrutura. Nos testes de integração, nunca escolha um registro de auditoria "pelo último": dois registros da mesma gravação não têm ordem definida — selecione por operação, evento ou entidade.

## Estado de partida (fase 5A, na branch `feat/fase-5-atividades`)

- Domínio: `Atividade` (responsáveis, histórico, fluxo de status, `Versao`/`VersaoDasPartes`); `Documento` (`CriarGeral`, `CriarContratoDeHonorarios`, `PodeSerAcessadoPor(usuarioId, perfilId, tipoPerfil)`, `DefinirAcesso`, enum `CategoriaDocumento` com `DocumentoAtividade = 3`, `ClienteId` anulável, sem `AtividadeId`); `Processo`.
- Aplicação: casos de uso de atividades em `Application/Atividades/` (`AcessoAAtividade.PodeVer`, `RegistrarNegadoAsync`, mensagens `NaoEncontrada`/`Alterada`); de documentos em `Application/Documentos/` (`EnviarDocumento`, `ListarDocumentosDoCliente`, `BaixarDocumento`, `RenomearDocumento`, `ExcluirDocumento`, `ObterAcessoDoDocumento`, `DefinirAcessoDoDocumento`; `DocumentoResumo`; `IArmazenamentoDeArquivos`); `SalvarProcesso`.
- Infraestrutura: tabela `documentos` com a restrição `categoria = 3 OR cliente_id IS NOT NULL`; `AtividadeRepositorio`; `DocumentoRepositorio`.
- Web: `FichaDaAtividade.razor`, `DocumentosDoCliente.razor`, `Clientes.razor`; endpoint `GET /documentos/{id}/baixar`.
- Testes: 521 unitários e 292 de integração passando.

## Global Constraints

- TFM único `net10.0` em `Directory.Build.props`. `Nullable` e `TreatWarningsAsErrors=true`. Versões de pacote só em `Directory.Packages.props`. Nenhum pacote novo.
- `Domain` não referencia nada. `Application` não referencia EF Core, Npgsql, `Infrastructure` nem `Web`.
- Dentro da `Application`: `Cadastros` e `Documentos` não usam tipos de `Application/Atividades`; `Atividades` pode usar os dois.
- Código, nomes de tabela e mensagens em português. Tabelas e colunas em `snake_case`. Datas em UTC.
- Exclusão sempre lógica. Agregados se referenciam só por Id.
- Toda alteração de `Entidade` gera registro de auditoria na mesma transação. Toda propriedade nova é classificada em `PropriedadesAuditadasTests`. Todo tipo mapeado deriva de `Entidade`. Nenhuma gravação por fora dos interceptores.
- Matriz de acesso: atividades — Diretoria tudo; Produção só as em que é responsável. Contratos de Honorários nunca para Produção. Documento de atividade: Diretoria, e os responsáveis atuais da atividade.
- Autorização verificada no manipulador; a interface apenas oculta. Um recurso que o usuário não pode ver responde `NaoEncontrado`, e a tentativa gera o evento `acesso_negado` com o recurso.
- Consultas são `IConsultaAuditavel`.
- Um caso de uso por arquivo. Novas dependências de construtor e novos membros posicionais de registros entram por último.

## Review Focus

1. **Um responsável é retirado da atividade**: no mesmo instante deixa de listar e de baixar os documentos dela, e de ler e escrever comentários. (Tarefas 2 e 3)
2. **A área do processo muda**: todas as atividades dele mudam junto, na mesma transação, cada uma com o seu registro de auditoria. (Tarefa 1)
3. **Tentar retirar do processo um cliente com atividade nele**: recusado com o nome do cliente; os demais dados do processo não mudam. (Tarefa 1)
4. **O cliente da atividade é trocado**: os documentos dela passam a aparecer na ficha do novo cliente e saem da do antigo. (Tarefa 3)
5. **Produção abre "Documentos de Atividades" na ficha de um cliente**: vê só os documentos das atividades em que é responsável; o total não revela os demais. (Tarefa 3)
6. **Documento de atividade avulsa** (sem cliente): pode ser anexado e baixado; não aparece em ficha de cliente. (Tarefa 3)

---

### Task 1: Regras de processo que dependem de atividades

**Files:**
- Modify: `src/AtividadesJuridicas.Domain/Atividades/Atividade.cs` (`AcompanharAreaDoProcesso`)
- Modify: `src/AtividadesJuridicas.Application/Cadastros/PortasDeProcessos.cs` (porta `IAtividadesDoProcesso`)
- Modify: `src/AtividadesJuridicas.Application/Cadastros/SalvarProcesso.cs`
- Create: `src/AtividadesJuridicas.Infrastructure/Persistencia/Repositorios/AtividadesDoProcesso.cs`
- Modify: `src/AtividadesJuridicas.Infrastructure/DependencyInjection.cs`
- Modify: `tests/AtividadesJuridicas.UnitTests/Application/Falsos.cs`, `Application/ProcessosTests.cs`, `Domain/AtividadeTests.cs`
- Modify: `tests/AtividadesJuridicas.IntegrationTests/ProcessosTests.cs` (ou um arquivo novo `ProcessosEAtividadesTests.cs`)

**Interfaces:**
- Produces:
  - `atividade.AcompanharAreaDoProcesso(Guid areaAtuacaoId)` — troca `AreaAtuacaoId`; só vale para atividade com processo (`InvalidOperationException` caso contrário); `Guid.Empty` é `ArgumentException`. Vale em qualquer status e não gera linha de histórico.
  - Em `Application/Cadastros/PortasDeProcessos.cs`:
    ```csharp
    /// <summary>O que o cadastro de processos precisa saber das atividades e pedir a elas.</summary>
    public interface IAtividadesDoProcesso
    {
        /// <summary>Dentre os clientes informados, os que têm atividade viva neste processo.</summary>
        Task<IReadOnlyList<Guid>> ClientesComAtividadeAsync(Guid processoId, IReadOnlyCollection<Guid> clienteIds, CancellationToken ct);

        /// <summary>
        /// Faz as atividades do processo acompanharem a nova área. Só altera as entidades no contexto da
        /// requisição: quem grava é o SalvarAsync do caso de uso, na mesma transação. Devolve quantas mudaram.
        /// </summary>
        Task<int> AcompanharAreaAsync(Guid processoId, Guid areaAtuacaoId, CancellationToken ct);
    }
    ```
  - `SalvarProcessoManipulador` recebe `IAtividadesDoProcesso` como última dependência.

**Regras:**

1. Ao **editar** um processo, antes de qualquer alteração: se algum cliente que está no processo e não está na lista nova tem atividade viva nesse processo, responde `Validacao` com `"O cliente {Nome} tem atividade vinculada a este processo e não pode ser retirado."` (o primeiro, por nome), e nada muda. O nome vem do repositório de clientes.
2. Ao editar com **área diferente** da atual: depois de aplicar as alterações no processo e antes do `SalvarAsync`, chama `AcompanharAreaAsync`. Tudo é gravado junto. Uma falha de concorrência em qualquer atividade vira o mesmo `Conflito` que o processo já devolve.
3. Criar um processo não consulta atividades.
4. A implementação de `AcompanharAreaAsync` carrega as atividades do processo **inteiras** (com responsáveis e histórico, como `AtividadeRepositorio.ObterPorIdAsync`), inclusive as concluídas, chama `AcompanharAreaDoProcesso` nas que têm área diferente e não chama `SaveChanges`. Atividades excluídas não são tocadas.
5. `ClientesComAtividadeAsync` considera só atividades vivas (o filtro de exclusão lógica).

**Testes obrigatórios:**

Unitários de domínio (`AtividadeTests`):
- `Acompanhar_a_area_do_processo_troca_so_a_area` — status, histórico e `VersaoDasPartes` não mudam; funciona em atividade `Concluida`.
- `Atividade_sem_processo_nao_acompanha_area` — `InvalidOperationException`.

Unitários do caso de uso (`ProcessosTests`, com um `AtividadesDoProcessoFalso` em `Falsos.cs` apoiado em `Cenario.Atividades`):
- `Cliente_com_atividade_no_processo_nao_e_retirado` — mensagem com o nome; clientes e demais dados do processo intactos; nada salvo.
- `Cliente_sem_atividade_no_processo_pode_ser_retirado`.
- `Atividade_do_cliente_em_outro_processo_ou_sem_processo_nao_impede`.
- `Trocar_a_area_do_processo_leva_as_atividades_junto` — a atividade do processo passa à nova área; a de outro processo não.
- `Manter_a_area_nao_toca_nas_atividades` — o falso registra que `AcompanharAreaAsync` não foi chamado.
- `Criar_processo_nao_consulta_atividades`.

Integração (PostgreSQL):
- `Retirar_cliente_com_atividade_e_recusado_e_o_processo_fica_como_estava` — inclusive título e demais campos enviados na mesma requisição.
- `Atividade_excluida_nao_impede_retirar_o_cliente`.
- `Trocar_a_area_do_processo_atualiza_as_atividades_na_mesma_transacao` — duas atividades do processo (uma `Concluida`) e uma de outro processo; depois: as duas com a área nova, a terceira intacta; auditoria com uma `Alteracao` de `Atividade` por atividade alterada (JSON só com `AreaAtuacaoId`, `de`/`para`), todas com o mesmo `IdCorrelacao` da `Alteracao` do `Processo`; histórico das atividades sem linha nova.
- `Atividade_alterada_ao_mesmo_tempo_faz_a_troca_de_area_responder_conflito` — determinístico, com dois escopos como nos testes de corrida existentes: o escopo A carrega processo e atividades e troca a área; o escopo B conclui uma parte dessa atividade e grava; A, ao gravar, recebe `ExcecaoDeConcorrencia`; nada de A fica gravado (processo com a área antiga).
- `PropriedadesAuditadasTests` não muda (nenhuma propriedade nova).

- [ ] **Step 1:** escrever os testes; **Step 2:** vê-los falhar; **Step 3:** implementar (domínio, porta, manipulador, implementação da porta, DI, falso); **Step 4:** `dotnet test` inteiro, sem avisos; **Step 5:** commit:

```bash
git add src tests
git commit -m "feat(cadastros): processo não perde cliente com atividade e a troca de área alcança as atividades"
```

---

### Task 2: Comentários

**Files:**
- Create: `src/AtividadesJuridicas.Domain/Atividades/AtividadeComentario.cs`
- Create: `src/AtividadesJuridicas.Application/Atividades/PortasDeComentarios.cs`, `ComentarAtividade.cs`, `ListarComentariosDaAtividade.cs`
- Create: `src/AtividadesJuridicas.Infrastructure/Persistencia/Configuracoes/AtividadeComentarioConfiguracao.cs`, `Repositorios/ComentarioRepositorio.cs`
- Modify: `AppDbContext.cs` (não é obrigatório um `DbSet`), `DependencyInjection.cs`
- Create: migration `Comentarios`
- Modify: `tests/AtividadesJuridicas.UnitTests/Application/Falsos.cs`; `tests/AtividadesJuridicas.IntegrationTests/PropriedadesAuditadasTests.cs`
- Test: `tests/AtividadesJuridicas.UnitTests/Domain/AtividadeComentarioTests.cs`, `Application/ComentariosTests.cs`; `tests/AtividadesJuridicas.IntegrationTests/ComentariosTests.cs`

**Interfaces:**
- Produces:
  - `AtividadeComentario : Entidade` com `AtividadeId`, `UsuarioId`, `Texto`, `DataHora`; `const int TamanhoMaximoTexto = 4000`; `static AtividadeComentario Criar(Guid atividadeId, Guid usuarioId, string texto, DateTime agoraUtc)`. Sem nenhum método que altere.
  - `record ComentarioDetalhe(Guid Id, Guid UsuarioId, string? UsuarioNome, string Texto, DateTime DataHora)`
  - `interface IComentarioRepositorio { void Adicionar(AtividadeComentario comentario); Task<IReadOnlyList<ComentarioDetalhe>> ListarDaAtividadeAsync(Guid atividadeId, CancellationToken ct); }`
  - `record ComentarAtividade(Guid AtividadeId, string Texto) : IRequisicao<Resultado<Guid>>`
  - `record ListarComentariosDaAtividade(Guid AtividadeId) : IRequisicao<Resultado<IReadOnlyList<ComentarioDetalhe>>>, IConsultaAuditavel` (`TipoRecurso = "AtividadeComentario"`, `Acao = Listou`, `Detalhe` com o `AtividadeId`)

**Regras:**

1. Domínio: texto obrigatório (`"Escreva o comentário."`), aparado, até 4000 caracteres (`"O comentário deve ter até 4000 caracteres."`); `atividadeId`/`usuarioId` vazios e `agoraUtc` fora de UTC são `ArgumentException`. Quebras de linha no meio do texto são preservadas.
2. Os dois casos de uso: sem usuário → `AcessoNegado`. Atividade inexistente (ou excluída) → `NaoEncontrado` com `AcessoAAtividade.NaoEncontrada`. Usuário que não vê a atividade (`AcessoAAtividade.PodeVer`) → o mesmo `NaoEncontrado`, com o evento `acesso_negado` da atividade (`AcessoAAtividade.RegistrarNegadoAsync`).
3. `ComentarAtividade` vale em qualquer status. O autor é o ator; `DataHora` é `IRelogio.AgoraUtc`. Não altera a atividade (a linha da atividade não é regravada: a `Versao` dela não muda).
4. A listagem vem em ordem cronológica (`DataHora`, depois `Id`), com o nome do autor (mesmo de usuário excluído; `null` se não for encontrado).
5. Mapeamento: tabela `atividades_comentarios`; `Texto` (4000, obrigatório); FKs `Restrict` para `atividades` e `usuarios`; índice em `(atividade_id, data_hora)`. Não é filha do agregado `Atividade` no EF (sem navegação a partir de `Atividade`).
6. `PropriedadesAuditadasTests`: `["AtividadeComentario"] = ["AtividadeId", "DataHora", "Texto", "UsuarioId"]`.

**Testes obrigatórios:**

Domínio: criação com texto aparado e quebras de linha internas preservadas; texto vazio; texto longo; ids vazios; instante não UTC; a classe não expõe método público que altere o estado (teste por reflexão: nenhum método público de instância declarado na classe além dos acessores de leitura).

Casos de uso (falso `ComentarioRepositorioFalso`):
- `Diretor_e_responsavel_comentam_e_leem_em_ordem`.
- `Producao_que_nao_e_responsavel_nao_comenta_nem_le_e_a_tentativa_fica_registrada` — `NaoEncontrado` nos dois, dois eventos `acesso_negado` com a atividade.
- `Atividade_inexistente_e_nao_encontrada_sem_evento`.
- `Sem_usuario_e_acesso_negado`.
- `Comentario_vazio_ou_longo_e_validacao_e_nada_e_gravado`.
- `Comenta_se_em_qualquer_status` (inclusive `Concluida`).
- `Listagem_e_consulta_auditavel`.

Integração:
- `Comentario_vai_e_volta_do_banco_com_autor_e_ordem`.
- `Comentar_nao_muda_a_versao_da_atividade` — `ObterAtividade` antes e depois: mesma `Versao`; e um `SalvarAtividade` com a `VersaoEsperada` lida antes do comentário ainda tem sucesso.
- `Comentario_gera_registro_de_criacao_e_a_listagem_fica_no_registro_de_consultas`.
- `Responsavel_removido_deixa_de_ler_e_de_comentar`.
- `Atividade_excluida_esconde_os_comentarios` — listar e comentar respondem `NaoEncontrado`; as linhas continuam no banco, sem `excluido_em`.
- `Banco_recusa_apagar_fisicamente_atividade_com_comentario` (FK).

- [ ] **Step 1:** testes; **Step 2:** falham; **Step 3:** implementar; **Step 4:** migration `Comentarios` (só a tabela `atividades_comentarios`; confira e registre no relatório) ; **Step 5:** `dotnet test` inteiro; **Step 6:** commit:

```bash
git add src tests
git commit -m "feat(atividades): comentários das atividades"
```

---

### Task 3: Documentos de atividade

**Files:**
- Modify: `src/AtividadesJuridicas.Domain/Documentos/Documento.cs`
- Modify: `src/AtividadesJuridicas.Application/Documentos/PortasDeDocumentos.cs`, `BaixarDocumento.cs`, `ListarDocumentosDoCliente.cs`, `EnviarDocumento.cs` (extrair a validação do arquivo para reuso)
- Create: `src/AtividadesJuridicas.Application/Documentos/ArquivoEnviado.cs` (apoio compartilhado)
- Create: `src/AtividadesJuridicas.Application/Atividades/EnviarDocumentoDaAtividade.cs`, `ListarDocumentosDaAtividade.cs`
- Modify: `src/AtividadesJuridicas.Application/Atividades/SalvarAtividade.cs` (documentos acompanham o cliente)
- Modify: `src/AtividadesJuridicas.Infrastructure/Persistencia/Configuracoes/DocumentoConfiguracao.cs`, `Repositorios/DocumentoRepositorio.cs`, `DependencyInjection.cs`
- Create: `src/AtividadesJuridicas.Infrastructure/Persistencia/Repositorios/ResponsaveisDeAtividade.cs`
- Create: migration `DocumentosDeAtividade`
- Modify: fakes e `PropriedadesAuditadasTests.cs`
- Test: `tests/AtividadesJuridicas.UnitTests/Domain/DocumentoTests.cs` (acréscimos), `Application/DocumentosDeAtividadeTests.cs`; `tests/AtividadesJuridicas.IntegrationTests/DocumentosDeAtividadeTests.cs`, `DocumentosWebTests.cs` (acréscimos)

**Interfaces:**
- Produces:
  - Domínio:
    - `Documento.AtividadeId` (`Guid?`).
    - `Documento.CriarDeAtividade(Guid atividadeId, Guid? clienteId, string? nome, ArquivoDoDocumento arquivo)` — categoria `DocumentoAtividade`, listas de acesso vazias; `atividadeId` vazio é erro de domínio (`"A atividade do documento é obrigatória."`); `clienteId` pode ser nulo (atividade avulsa) e, quando informado, não pode ser `Guid.Empty`.
    - `documento.AcompanharClienteDaAtividade(Guid? clienteId)` — só para documento de atividade (`InvalidOperationException` caso contrário).
    - `documento.PodeSerAcessadoPor(Guid usuarioId, Guid perfilId, TipoPerfil tipoPerfil, bool ehResponsavelDaAtividade = false)` — Diretoria: sempre. Contrato: nunca para os demais. Documento de atividade: exatamente `ehResponsavelDaAtividade` (as listas não contam). Documento geral: as listas (o novo parâmetro é ignorado).
    - `DefinirAcesso` em documento de atividade → `ExcecaoDeDominio` `"O acesso a um documento de atividade é o dos responsáveis da atividade."`.
    - `CriarGeral`/`CriarContratoDeHonorarios` continuam deixando `AtividadeId` nulo.
  - Em `Application/Documentos/PortasDeDocumentos.cs`:
    - `interface IResponsaveisDeAtividade { Task<bool> EhResponsavelAsync(Guid atividadeId, Guid usuarioId, CancellationToken ct); }` — verdadeiro só para responsável atual de atividade viva.
    - `DocumentoResumo` ganha, por último, `Guid? AtividadeId` e `string? AtividadeTitulo`.
    - `IDocumentoRepositorio` ganha `Task<Pagina<DocumentoResumo>> ListarDaAtividadeAsync(Guid atividadeId, int pagina, int tamanho, CancellationToken ct)` e `Task<IReadOnlyList<Documento>> ObterDaAtividadeAsync(Guid atividadeId, CancellationToken ct)` (rastreados, para o cliente acompanhar).
  - Em `Application/Atividades/`:
    - `record EnviarDocumentoDaAtividade(Guid AtividadeId, string? Nome, string NomeArquivo, string? TipoConteudo, Stream Conteudo) : IRequisicao<Resultado<Guid>>`
    - `record ListarDocumentosDaAtividade(Guid AtividadeId, int Pagina = 1) : IRequisicao<Resultado<Pagina<DocumentoResumo>>>, IConsultaAuditavel` (`TipoRecurso = "Documento"`, `Listou`, `Detalhe` com `AtividadeId` e `Pagina`)

**Regras:**

1. **Enviar**: sem usuário → `AcessoNegado`; atividade inexistente → `NaoEncontrado` (`AcessoAAtividade.NaoEncontrada`); quem não vê a atividade → o mesmo `NaoEncontrado` + evento `acesso_negado` da atividade. As validações do arquivo e do nome, o limite de tamanho, a ordem "arquivo primeiro, banco depois" e a remoção do arquivo em qualquer saída sem registro são **as mesmas** de `EnviarDocumento`: extraia essa parte para `ArquivoEnviado` (ou equivalente) e use nos dois, sem mudar o comportamento nem as mensagens de `EnviarDocumento` (os testes existentes continuam passando sem alteração). O `ClienteId` do documento é o da atividade no momento do envio.
2. **Listar da atividade**: mesma regra de visibilidade (com evento quando negado). Do mais recente para o mais antigo.
3. **Listar do cliente** com categoria `DocumentoAtividade`: Diretoria vê todos os documentos de atividade do cliente; os demais, só os de atividades vivas em que são responsáveis hoje — filtro no banco, e o `Total` conta só esses. Cada item traz `AtividadeId` e `AtividadeTitulo` (o título mesmo de atividade excluída, para a Diretoria). Para as outras categorias, os dois campos vêm nulos e nada muda.
4. **Baixar**: para documento de atividade, o manipulador pergunta a `IResponsaveisDeAtividade` (só quando o ator não é da Diretoria) e passa a resposta a `PodeSerAcessadoPor`. O restante do fluxo (não encontrado + evento, arquivo ausente) não muda.
5. **Renomear e excluir**: continuam da Diretoria, para qualquer categoria. `ObterAcessoDoDocumento` de um documento de atividade devolve listas vazias; `DefinirAcessoDoDocumento` responde `Validacao` com a mensagem do domínio.
6. **`EnviarDocumento`** (o de cliente) continua recusando a categoria `DocumentoAtividade`.
7. **`SalvarAtividade`**: quando a edição muda o cliente da atividade (inclusive de/para nenhum), os documentos dela acompanham (`ObterDaAtividadeAsync` + `AcompanharClienteDaAtividade`), na mesma gravação.
8. **Excluir a atividade** não toca nos documentos.
9. Mapeamento: coluna `atividade_id` (anulável) com FK `Restrict` para `atividades` e índice; a restrição de verificação passa a ser dupla: a existente (`categoria = 3 OR cliente_id IS NOT NULL`) e `(categoria = 3) = (atividade_id IS NOT NULL)`.
10. `PropriedadesAuditadasTests`: `Documento` ganha `"AtividadeId"`.

**Testes obrigatórios:**

Domínio (`DocumentoTests`): criação de documento de atividade (com e sem cliente; listas vazias); `PodeSerAcessadoPor` para os quatro casos (Diretoria; responsável; não responsável; e que constar nas listas não dá acesso — monte por reflexão ou confirme que `DefinirAcesso` recusa e que não há outro caminho para preencher as listas); `DefinirAcesso` recusado; `AcompanharClienteDaAtividade` (troca, para nulo, recusa em documento geral); documento geral ignora o parâmetro novo; contrato continua fechado mesmo com `ehResponsavelDaAtividade = true`.

Casos de uso (`DocumentosDeAtividadeTests`, unitário):
- `Responsavel_e_diretor_anexam_e_o_documento_leva_a_atividade_e_o_cliente_dela`.
- `Atividade_avulsa_aceita_documento_sem_cliente`.
- `Quem_nao_ve_a_atividade_nao_anexa_nem_lista_e_a_tentativa_fica_registrada`.
- `Validacoes_do_arquivo_sao_as_mesmas_do_documento_de_cliente` — vazio, nome ausente, nome com caminho reduzido ao último segmento, limite; armazenamento vazio depois de cada recusa.
- `Falha_ao_gravar_no_banco_remove_o_arquivo`.
- `Baixar_documento_de_atividade_depende_de_ser_responsavel_hoje` — responsável baixa; depois de removido da atividade, `NaoEncontrado` + evento; diretor baixa sempre.
- `Lista_do_cliente_traz_os_documentos_de_atividade_com_o_titulo_e_respeita_os_responsaveis`.
- `Trocar_o_cliente_da_atividade_leva_os_documentos` e `Tirar_o_cliente_da_atividade_deixa_os_documentos_sem_cliente`.
- `Definir_acesso_de_documento_de_atividade_e_recusado`.
- `Enviar_documento_de_cliente_continua_recusando_a_categoria_de_atividade`.

Integração (`DocumentosDeAtividadeTests`):
- `Documento_de_atividade_vai_para_o_disco_e_para_o_banco_com_atividade_e_cliente`.
- `Responsavel_lista_e_baixa_e_quem_nao_e_responsavel_nao` — incluindo `Total` da listagem do cliente para a Produção.
- `Responsavel_removido_perde_os_documentos_na_hora_e_o_incluido_ganha` (Review Focus 1).
- `Trocar_o_cliente_da_atividade_move_os_documentos_de_ficha_e_audita` — `Alteracao` de `Documento` só com `ClienteId`, mesmo `IdCorrelacao` da `Alteracao` da `Atividade`.
- `Documento_de_atividade_avulsa_nao_aparece_em_ficha_de_cliente_mas_e_listado_e_baixado_pela_atividade`.
- `Excluir_a_atividade_mantem_os_documentos_para_a_diretoria_e_os_esconde_da_producao` — a listagem do cliente (Diretoria) ainda traz o documento com o título da atividade.
- `Banco_exige_atividade_em_documento_de_atividade_e_so_nele` — dois `INSERT` brutos: categoria 3 sem `atividade_id`, e categoria 1 com `atividade_id`; ambos falham com violação de verificação.
- `Documentos_de_cliente_e_contratos_nao_mudaram` — um documento geral e um contrato continuam com o comportamento da fase 4 (listas, contrato fechado), com `AtividadeId` nulo.
- Em `DocumentosWebTests`: download por HTTP de um documento de atividade — 200 para o responsável (Pedro), 404 para outro usuário de Produção, 404 para Pedro depois de removido da atividade.

- [ ] **Step 1:** testes; **Step 2:** falham; **Step 3:** implementar; **Step 4:** migration `DocumentosDeAtividade` (só a coluna, a FK, o índice e a troca da restrição de verificação na tabela `documentos`; confira e registre); **Step 5:** `dotnet test` inteiro; **Step 6:** commit:

```bash
git add src tests
git commit -m "feat(documentos): documentos de atividade com acesso pelos responsáveis atuais"
```

---

### Task 4: Comentários e documentos na ficha da atividade; "Documentos de Atividades" na ficha do cliente

**Files:**
- Create: `src/AtividadesJuridicas.Web/Components/Atividades/ComentariosDaAtividade.razor`
- Create: `src/AtividadesJuridicas.Web/Components/Atividades/DocumentosDaAtividade.razor`
- Modify: `src/AtividadesJuridicas.Web/Components/Atividades/FichaDaAtividade.razor`
- Modify: `src/AtividadesJuridicas.Web/Components/Documentos/DocumentosDoCliente.razor`
- Modify: `src/AtividadesJuridicas.Web/Components/Pages/Clientes.razor`
- Test: `tests/AtividadesJuridicas.IntegrationTests/ClientesWebTests.cs` e/ou `AtividadesWebTests.cs` (o que o HTML pré-renderizado permitir)

**O que a tela faz:**

- **Ficha da atividade** ganha duas seções, abaixo do histórico, para quem vê a ficha:
  - **Comentários** (`ComentariosDaAtividade`, parâmetros `AtividadeId`, `OnAcessoEncerrado`): lista em ordem cronológica (autor, data e hora no fuso do escritório, texto com as quebras de linha preservadas — classe `texto-livre`); campo de texto (textarea) e botão "Comentar", com guarda de envio duplo; depois de comentar, o campo é limpo e a lista recarregada. Lista vazia: "Nenhum comentário." Falha de consulta nunca aparece como lista vazia. `NaoEncontrado` (perdeu a visibilidade) mostra a mensagem do caso de uso no lugar da seção.
  - **Documentos** (`DocumentosDaAtividade`, parâmetros `AtividadeId`, `PodeGerenciar` (= `Acoes.Editar`), `OnAcessoEncerrado`): lista com link de download (o mesmo padrão de `DocumentosDoCliente`: `<a href download data-enhance-nav="false">`), tamanho, data e quem enviou; envio de arquivo para todos que veem a ficha (mesma mecânica de `DocumentosDoCliente`: limite, guarda de envio, `InputFile` recriado depois do envio, tratamento de `IOException`); renomear e excluir (em dois cliques) só quando `PodeGerenciar`. Sem editor de acesso.
  - As duas seções são componentes próprios, com `@key` pelo Id da atividade; ações nelas não disparam `OnMudou` (não mudam a atividade).
- **Ficha do cliente** ganha a terceira área, "Documentos de Atividades", tanto na edição (Diretoria) quanto na ficha de leitura (Produção): `DocumentosDoCliente` com `Categoria = DocumentoAtividade`. Para essa categoria o componente: mostra a coluna "Atividade" (o título de origem); **não** mostra o envio de arquivo nem o botão "Acesso"; renomear e excluir continuam para quem gerencia (Diretoria). Texto de lista vazia: "Nenhum documento."

**Regras de implementação:** as mesmas lições já registradas (sem `EditForm` em volta de campos soltos; lambdas de atribuição viram métodos; guarda de ação em andamento; mensagem limpa ao começar; "ação concluída, mas não foi possível atualizar a lista" quando a recarga falha; `AcessoNegado` em qualquer chamada dispara `OnAcessoEncerrado`). Se a duplicação entre `DocumentosDaAtividade` e `DocumentosDoCliente` for grande, extraia o que é comum para um componente ou classe de apoio e diga no relatório; não mude o comportamento de `DocumentosDoCliente` para as categorias existentes.

**Testes obrigatórios (HTML pré-renderizado; o que não for alcançável por GET fica para leitura do código):**
- A ficha de leitura de cliente não é pré-renderizada (abre por clique): não há teste de página para as áreas. Em vez disso, um teste de integração pelos casos de uso já cobre a listagem (Tarefa 3).
- `AtividadesWebTests`: nada novo é pré-renderizado (a ficha abre por clique). Mantenha os testes existentes verdes.
- Descreva no relatório, caminho por caminho, como verificou por leitura: comentário enviado duas vezes por clique duplo; falha ao listar; perda de visibilidade no meio do uso; envio de arquivo acima do limite; link de download; coluna "Atividade" só na categoria de atividade; ausência do envio e do "Acesso" nessa categoria; `DocumentosDoCliente` inalterado para Documentos Gerais e Contratos.

- [ ] **Step 1:** implementar; **Step 2:** `dotnet test` inteiro, sem avisos; **Step 3:** commit:

```bash
git add src tests
git commit -m "feat(web): comentários e documentos na ficha da atividade e documentos de atividades na ficha do cliente"
```
