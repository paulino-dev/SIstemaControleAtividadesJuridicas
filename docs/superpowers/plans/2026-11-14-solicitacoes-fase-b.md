# Solicitações do cliente — Fase B: a atividade a partir da solicitação: Plano de Implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A Diretoria cria, de dentro de uma solicitação, a atividade que vai atendê-la (cliente travado, descrição como comentário, documentos levados sem cópia), com a recusa de cancelamento, alteração e criação dupla conferida no servidor; excluir a atividade devolve a solicitação.

**Architecture:** `Atividade` ganha `SolicitacaoId` (definido só na criação; trava a troca de cliente). Um caso de uso novo, `CriarAtividadeDaSolicitacao`, grava numa só unidade a atividade, o vínculo na solicitação (pela versão dela), o comentário e os documentos, que apontam para o mesmo arquivo gravado. O formulário de atividade atual ganha o modo `?solicitacao={id}`; as duas fichas ganham a navegação entre si.

**Tech Stack:** .NET 10, Blazor Interactive Server, EF Core 10 + Npgsql, PostgreSQL 17, xUnit 2.9, FluentAssertions 7.2, Testcontainers.

**Spec:** `docs/superpowers/specs/2026-11-13-solicitacoes-do-cliente-design.md` — seções 3 (`Atividade` e `Documento`), 4 ("Mudanças nos casos de uso existentes"), 5, 6 (formulário e fichas), 8 (`atividades.solicitacao_id` e o índice da chave), 9 (o que a fase A deixou) e os três itens "Para a B" da seção 10.

## Decisões deste plano

- **Falha própria do cancelamento:** `TipoErro` ganha `ConfirmacaoNecessaria` (no fim do enum). Só `CriarAtividadeDaSolicitacao` a usa.
- **O vínculo recusa atividade de outro cliente no domínio:** `SolicitacaoDoCliente.VincularAtividade` passa a receber a `Atividade` (e não mais o Id) e confere `ClienteId` e `SolicitacaoId` dela.
- **Levar o documento para a atividade é um método do documento:** `Documento.LevarParaAtividade(...)` é o único caminho que cria documento com a chave de outro. Um teste por varredura fixa que só `CriarAtividadeDaSolicitacao.cs` o chama e que `new ArquivoDoDocumento(` só aparece em `ArquivoEnviado.cs` (chave nova) e em `Documento.cs`.
- **A validação de cliente, processo e área sai de `SalvarAtividade`** para `ValidacaoDoVinculo.ValidarAsync`, usada pelos dois casos de uso, sem mudar mensagens.
- **Gravação concorrente:** `ExcecaoDeConcorrencia` (a linha da solicitação mudou entre a leitura e a gravação) responde `Conflito` com a mensagem de "alterada"; `ExcecaoDeConflito` (índice único de `atividades.solicitacao_id`) responde `Conflito` com "já virou atividade". A tela relê a solicitação nos dois casos e mostra o que encontrar.
- **Formulário com solicitação que já virou atividade** (endereço aberto à mão): a página mostra o aviso "Esta solicitação já virou atividade." com "Ver atividade" e nenhum formulário. Solicitação cancelada carrega o formulário normalmente: o aviso só vem na gravação (spec, seção 6).
- **Cancelar e voltar do formulário com solicitação** vão para `/solicitacoes/{id}`, com a origem "Solicitação".
- **"Ver solicitação" na ficha da atividade** entra na faixa de ações, depois de "Trocar revisor" e antes de Excluir, como botão fantasma; no mobile, no "Mais ações", antes de Excluir.
- **O bloco da solicitação no formulário é um componente próprio**, `Components/Solicitacoes/SolicitacaoNoFormulario.razor`: o formulário de atividade já tem 436 linhas.

## Estado de partida

- Worktree `.claude/worktrees/solicitacoes-fase-b`, branch `solicitacoes-fase-b`, a partir de `origin/main` em `c47c4a2` (o merge do PR #4, a fase A).
- Git: identidade já configurada no repositório. Todo commit termina com `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- `SolicitacaoDoCliente` (`Domain/Solicitacoes`): `Situacao`, `VincularAtividade(Guid, bool)`, `DesvincularAtividade()`, `Versao` (`xmin`), constantes `JaVirouAtividade` e `CanceladaPeloCliente`.
- `ObterSolicitacao` (Diretoria) devolve `SolicitacaoDetalhe` com `Versao`, `Documentos` (`DocumentoDoPortal`), `AtividadeId` e `AtividadeTitulo`.
- `SalvarAtividade` cria e altera; `ExcluirAtividade` remove; `ObterAtividade` monta `AtividadeDetalhe` (record posicional em `PortasDeAtividades.cs`).
- `ListarProcessos(Busca, ClienteId, Status, Pagina)` já filtra por cliente.
- `ix_documentos_chave_armazenamento` é único (`DocumentoConfiguracao`, linha 34).
- Testes: `Falsos.cs` e `Cenario` nos unitários; `TesteComBanco`, `AplicacaoDeTeste`, `UsuariosDeClienteDeTeste` e `SolicitacoesInterativasTests` (modelo de teste de tela) nos de integração. `PropriedadesAuditadasTests` classifica toda propriedade mapeada.
- A suíte de integração leva de 15 a 35 minutos; rode-a em segundo plano e com `--logger "trx;LogFileName=..."`. A máquina já reiniciou sozinha no meio de build: ao retomar, confira o estado do worktree.

## Global Constraints

- Mensagens exatas:
  - Troca de cliente: "O cliente de uma atividade criada a partir de uma solicitação não pode ser trocado."
  - Já em atividade: "Esta solicitação já virou atividade." (`SolicitacaoDoCliente.JaVirouAtividade`)
  - Cancelada: "Cliente cancelou a solicitação, mesmo assim deseja criar a atividade?" (`SolicitacaoDoCliente.CanceladaPeloCliente`)
  - Alterada durante a criação: "O cliente alterou a solicitação enquanto você criava a atividade. Confira os dados atualizados." (`SolicitacoesOpcoes.AlteradaDuranteACriacao`)
  - Documento marcado inválido: "Um documento marcado não está mais nesta solicitação." (`SolicitacoesOpcoes.DocumentoForaDaSolicitacao`)
  - Atividade de outro cliente no vínculo: "A atividade precisa ser do cliente da solicitação." (`SolicitacaoDoCliente.AtividadeDeOutroCliente`)
  - Cliente inativo: "O cliente está inativo." (a de hoje)
- Ordem das verificações de `CriarAtividadeDaSolicitacao` (spec, seção 5): Diretoria → existe (`NaoEncontrado`, `SolicitacoesOpcoes.NaoEncontrada`) → já em atividade (`Conflito`) → cancelada sem confirmação (`ConfirmacaoNecessaria`) → versão (`Conflito`, vale também com a confirmação) → documentos (`Validacao`) → vínculo (cliente, processo, área: `Validacao`).
- Rótulos da tela: bloco "Solicitação do cliente"; caixa "Anexar a descrição da solicitação como comentário da atividade" (marcada); lista "Documentos da solicitação" (todas marcadas) com "Marcar todos" e "Desmarcar todos"; botões do aviso de cancelamento "Criar mesmo assim" e "Voltar à solicitação"; ficha da atividade "Ver solicitação"; ficha da solicitação "Criar atividade".
- A Produção nunca vê "Ver solicitação" nem `SolicitacaoId` (`ObterAtividade` só o devolve à Diretoria).
- Nenhuma requisição nova é aberta ao cliente: a lista do portão continua com catorze.
- Banco: `atividades.solicitacao_id` anulável, chave estrangeira sem cascata para `solicitacoes_do_cliente`, índice único parcial `excluido_em IS NULL`; `ix_documentos_chave_armazenamento` passa a índice comum. Uma migration só, `AtividadeDaSolicitacao`, só de DDL.
- Classes `aj-*` do design system; nada de estilo inline.

## Review Focus

1. **Dois diretores (ou duas abas) criam a atividade da mesma solicitação ao mesmo tempo:** uma passa, a outra recebe `Conflito` e nada dela fica gravado (nem atividade, nem comentário, nem documento). Teste de integração na Tarefa 3.
2. **O cliente remove um documento depois de o diretor abrir o formulário, e o diretor salva com ele marcado:** a versão mudou, então a resposta é `Conflito` (alterada), não `Validacao`; a tela recarrega e o documento some da lista. Teste de integração na Tarefa 3 e de tela na Tarefa 5.
3. **Id de documento de outra solicitação (ou de outra categoria) enviado como marcado:** `Validacao`, nada gravado. Teste unitário na Tarefa 3.
4. **Processo escolhido que não tem o cliente da solicitação:** `Validacao` pela regra do vínculo, nada gravado. Teste unitário na Tarefa 3.
5. **Atividade excluída e a mesma solicitação usada de novo:** a solicitação volta a aguardar, e uma atividade nova pode ser criada dela (o índice parcial ignora a excluída). Teste de integração na Tarefa 4.

---

### Task 1: Domínio — atividade com solicitação, vínculo conferido e documento levado

**Files:**
- Modify: `src/AtividadesJuridicas.Domain/Atividades/Atividade.cs`
- Modify: `src/AtividadesJuridicas.Domain/Solicitacoes/SolicitacaoDoCliente.cs`
- Modify: `src/AtividadesJuridicas.Domain/Documentos/Documento.cs`
- Test: `tests/AtividadesJuridicas.UnitTests/Domain/AtividadeTests.cs`, `SolicitacaoDoClienteTests.cs`, `DocumentoTests.cs`

**Interfaces:**
- Produces:
  - `Atividade.SolicitacaoId : Guid?` (setter privado).
  - `Atividade.Criar(DadosDaAtividade dados, VinculoDaAtividade vinculo, TipoAutor tipoAutor, Guid? autorId, DateTime agoraUtc, Guid? solicitacaoId = null)`.
  - `Atividade.ClienteDaSolicitacaoTravado` (const string, a mensagem de troca de cliente).
  - `SolicitacaoDoCliente.VincularAtividade(Atividade atividade, bool confirmouCancelada)` (substitui a versão com `Guid`).
  - `SolicitacaoDoCliente.AtividadeDeOutroCliente` (const string).
  - `Documento.LevarParaAtividade(Guid atividadeId, Guid clienteDaAtividade) : Documento`.

- [ ] **Step 1: Escrever os testes que falham.**
  - `AtividadeTests`:
    - `Criar_com_solicitacao_guarda_o_Id`: `SolicitacaoId` igual ao informado; sem o parâmetro, nulo.
    - `Criar_com_solicitacao_exige_cliente`: vínculo sem cliente → `ExcecaoDeDominio`.
    - `Criar_com_solicitacao_vazia_e_erro_de_programacao`: `Guid.Empty` → `ArgumentException`.
    - `Alterar_atividade_com_solicitacao_recusa_trocar_o_cliente`: para outro cliente e para nenhum → `ExcecaoDeDominio` com `ClienteDaSolicitacaoTravado`, e nada muda.
    - `Alterar_atividade_com_solicitacao_aceita_o_mesmo_cliente`: título e processo mudam.
    - `Alterar_atividade_sem_solicitacao_troca_o_cliente_como_antes`.
  - `SolicitacaoDoClienteTests`:
    - Os testes de vínculo existentes passam a montar uma `Atividade` do cliente da solicitação com `solicitacaoId: solicitacao.Id`.
    - `Vincular_recusa_atividade_de_outro_cliente`: `AtividadeDeOutroCliente`, e `AtividadeId` continua nulo.
    - `Vincular_recusa_atividade_criada_de_outra_solicitacao`: `ArgumentException`, erro de programação.
  - `DocumentoTests`:
    - `LevarParaAtividade_cria_documento_de_atividade_com_o_mesmo_arquivo`. O documento novo tem categoria `DocumentoAtividade`, `AtividadeId` e `ClienteId` informados, o mesmo `Nome`, `NomeArquivoOriginal`, `TipoConteudo`, `TamanhoBytes`, `HashSha256` e `ChaveArmazenamento`, e `Id` diferente do original. O original fica inalterado.
    - `LevarParaAtividade_so_vale_para_documento_da_solicitacao`: para outras categorias, `InvalidOperationException`.
    - `LevarParaAtividade_exige_o_cliente_do_documento`: cliente diferente do `ClienteId` do documento → `ExcecaoDeDominio`.
- [ ] **Step 2: Rodar e ver falhar.** `dotnet test tests/AtividadesJuridicas.UnitTests --filter "FullyQualifiedName~AtividadeTests|FullyQualifiedName~SolicitacaoDoClienteTests|FullyQualifiedName~DocumentoTests"` — não compila.
- [ ] **Step 3: Implementar.**
  - **`Atividade.Alterar`:** com `SolicitacaoId`, recusa `vinculo.ClienteId != ClienteId` antes de aplicar qualquer coisa.
  - **`VincularAtividade`:** confere nesta ordem:
    1. `atividade` nula → `ArgumentNullException`;
    2. `atividade.SolicitacaoId != Id` → `ArgumentException`;
    3. `atividade.ClienteId != ClienteId` → `ExcecaoDeDominio(AtividadeDeOutroCliente)`;
    4. as regras de hoje (já em atividade, cancelada sem confirmação).
  - **`LevarParaAtividade`:** usa `CriarDeAtividade` com `new ArquivoDoDocumento(...)` montado dos campos do próprio documento e o `Nome` dele.
  - **Comentário em `Documento.cs`:** registra a regra que substitui o índice único (spec, seção 3).
- [ ] **Step 4: Rodar e ver passar.** Mesmo comando, mais `dotnet build` da solução: os chamadores de `VincularAtividade(Guid, …)` em testes param de compilar e passam a usar a atividade.
- [ ] **Step 5: Commit.** `feat(domain): atividade com solicitação, vínculo conferido pelo cliente e documento levado para a atividade`.

### Task 2: Persistência — `atividades.solicitacao_id` e a chave de armazenamento compartilhável

**Files:**
- Modify: `src/AtividadesJuridicas.Infrastructure/Persistencia/Configuracoes/AtividadeConfiguracao.cs`
- Modify: `src/AtividadesJuridicas.Infrastructure/Persistencia/Configuracoes/DocumentoConfiguracao.cs:34`
- Create: migration `AtividadeDaSolicitacao` (em `Persistencia/Migrations`, pelo `dotnet ef migrations add AtividadeDaSolicitacao --project src/AtividadesJuridicas.Infrastructure --startup-project src/AtividadesJuridicas.Infrastructure --output-dir Persistencia/Migrations`)
- Test: `tests/AtividadesJuridicas.IntegrationTests/PropriedadesAuditadasTests.cs`, `SolicitacoesPersistenciaTests.cs`, `MigracaoDaAtividadeDaSolicitacaoTests.cs` (novo, no molde de `MigracaoDasSolicitacoesTests`)

**Interfaces:**
- Consumes: `Atividade.SolicitacaoId`, `VincularAtividade(Atividade, bool)`, `LevarParaAtividade` (Tarefa 1).
- Produces: a coluna, o índice único parcial `ix_atividades_solicitacao_id` e o índice comum `ix_documentos_chave_armazenamento`.

- [ ] **Step 1: Escrever os testes que falham.**
  - **`PropriedadesAuditadasTests`:** `SolicitacaoId` entra na lista de `Atividade`, como auditada.
  - **`SolicitacoesPersistenciaTests`:**
    - `Atividade_criada_e_vinculada_na_mesma_gravacao`: uma `SalvarAsync` só, com a atividade nova (com `SolicitacaoId`) e a solicitação existente vinculada. Relida, a atividade tem `SolicitacaoId` e a solicitação tem `AtividadeId`.
    - `Duas_atividades_vivas_da_mesma_solicitacao_sao_recusadas`: `ExcecaoDeConflito` com a restrição `ix_atividades_solicitacao_id`.
    - `Atividade_excluida_nao_impede_outra_da_mesma_solicitacao`.
    - `Dois_documentos_com_a_mesma_chave_gravam`: o documento da solicitação e o levado para a atividade.
  - **`MigracaoDaAtividadeDaSolicitacaoTests`:**
    - Aplicada a um banco migrado até `SolicitacoesDoCliente` e com uma atividade, a migration deixa `solicitacao_id` nulo nela.
    - O índice da chave deixa de ser único (consultar `pg_indexes.indexdef`).
    - O `Down` sobre um banco sem chave compartilhada volta o índice único.
- [ ] **Step 2: Rodar e ver falhar.** `dotnet test tests/AtividadesJuridicas.IntegrationTests --filter "FullyQualifiedName~PropriedadesAuditadasTests|FullyQualifiedName~SolicitacoesPersistenciaTests|FullyQualifiedName~MigracaoDaAtividadeDaSolicitacaoTests"`.
- [ ] **Step 3: Implementar.**
  - **Configuração:**
    - `HasOne<SolicitacaoDoCliente>().WithMany().HasForeignKey(a => a.SolicitacaoId).OnDelete(DeleteBehavior.Restrict)`;
    - `HasIndex(a => a.SolicitacaoId).IsUnique().HasFilter("excluido_em IS NULL")`;
    - no documento, `HasIndex(d => d.ChaveArmazenamento)` sem `IsUnique`.
  - **Migration:** gerar e conferir que só tem DDL. No `Down`, um comentário diz que recriar o índice único falha se já houver chave compartilhada (spec, seção 8).
  - **Se o EF acusar dependência circular** entre `atividades` e `solicitacoes_do_cliente` ao gravar o par, registre no relatório: a atividade é inserida e a solicitação, que já existe, é só atualizada.
- [ ] **Step 4: Rodar e ver passar.** Mesmo comando.
- [ ] **Step 5: Commit.** `feat(infra): atividade ligada à solicitação e chave de armazenamento compartilhável`.

### Task 3: Aplicação — `CriarAtividadeDaSolicitacao`

**Files:**
- Create: `src/AtividadesJuridicas.Application/Atividades/ValidacaoDoVinculo.cs`
- Create: `src/AtividadesJuridicas.Application/Solicitacoes/CriarAtividadeDaSolicitacao.cs`
- Modify: `src/AtividadesJuridicas.Application/Atividades/SalvarAtividade.cs` (usa `ValidacaoDoVinculo`)
- Modify: `src/AtividadesJuridicas.Application/Comum/Resultado.cs` (`TipoErro.ConfirmacaoNecessaria`)
- Modify: `src/AtividadesJuridicas.Application/Solicitacoes/PortasDeSolicitacoes.cs` (duas mensagens em `SolicitacoesOpcoes`)
- Test: `tests/AtividadesJuridicas.UnitTests/Application/AtividadeDaSolicitacaoTests.cs` (novo), `tests/AtividadesJuridicas.UnitTests/ChaveCompartilhadaTests.cs` (novo, no molde de `GravacoesPorForaTests`), `tests/AtividadesJuridicas.IntegrationTests/AtividadeDaSolicitacaoTests.cs` (novo)

**Interfaces:**
- Consumes: Tarefas 1 e 2; `ISolicitacaoRepositorio.ObterPorIdAsync` (rastreada); `IDocumentoRepositorio.ObterPorIdAsync`; `IComentarioRepositorio.Adicionar`; `AtividadeComentario.Criar(atividadeId, autorId, texto, agoraUtc)`.
- Produces:

```csharp
public sealed record CriarAtividadeDaSolicitacao(
    Guid SolicitacaoId, uint VersaoDaSolicitacao, string Titulo, string Descricao, DateOnly? DataEntrega,
    Guid? ProcessoId, Guid? AreaAtuacaoId, int Complexidade, bool AnexarDescricaoComoComentario,
    IReadOnlyList<Guid> DocumentosDaSolicitacao, bool ConfirmouSolicitacaoCancelada = false)
    : IRequisicao<Resultado<Guid>>;   // o valor é o Id da atividade nova

internal static class ValidacaoDoVinculo
{
    /// Erro: a mensagem de Validacao (nulo se válido). Processo: o carregado, quando informado.
    public static Task<(string? Erro, Processo? Processo)> ValidarAsync(
        IClienteRepositorio clientes, IProcessoRepositorio processos, IAreaAtuacaoRepositorio areas,
        Guid? clienteId, Guid? processoId, Guid? areaId, Atividade? existente, CancellationToken ct);
}
```

- [ ] **Step 1: Escrever os testes unitários que falham** (`AtividadeDaSolicitacaoTests`, com `Cenario`; uma solicitação aguardando, de cliente ativo, com dois documentos):
  - Diretoria cria: a atividade tem o cliente da solicitação e `SolicitacaoId`; a solicitação fica `EmAtividade` com o `AtividadeId`. Devolve o Id. `Unidade` salvou uma vez.
  - `Producao_e_Cliente_recebem_AcessoNegado`, sem gravar.
  - As cinco verificações, uma por teste, cada uma sem gravar nada, com o tipo de erro e a mensagem das Global Constraints:
    - inexistente;
    - já em atividade;
    - cancelada sem confirmação → `ConfirmacaoNecessaria`;
    - versão diferente;
    - versão diferente com confirmação → continua `Conflito`.
  - Cancelada com confirmação e versão certa: cria, e `CanceladaEm` se mantém.
  - Comentário: com a caixa, um comentário com o texto exato da descrição da solicitação e o diretor como autor; sem a caixa, nenhum.
  - Documentos:
    - só os marcados viram documentos da atividade, com a mesma `ChaveArmazenamento` e o mesmo `HashSha256`, a categoria `DocumentoAtividade` e o cliente da atividade;
    - lista vazia não leva nenhum;
    - Id repetido leva um só.
  - **Review Focus 3:**
    - documento de outra solicitação marcado → `Validacao` `DocumentoForaDaSolicitacao`, sem gravar;
    - documento de categoria comum marcado → idem;
    - documento removido (excluído) marcado → idem.
  - **Review Focus 4:** processo sem o cliente da solicitação → `Validacao`, sem gravar.
  - Cliente inativo → `Validacao` "O cliente está inativo.".
  - Na gravação, `ExcecaoDeConcorrencia` → `Conflito` com `AlteradaDuranteACriacao`, e `ExcecaoDeConflito` → `Conflito` com `JaVirouAtividade`. Use a `UnidadeDeTrabalhoFalsa`; se ela não sabe lançar, acrescente o gancho.
  - `SalvarAtividade` mantém as mensagens de hoje (os testes existentes de `AtividadesTests` passam sem mudança).
  - `SalvarAtividade_nao_troca_o_cliente_de_atividade_da_solicitacao`: a edição com outro cliente (ou sem cliente) dá `Validacao` com `ClienteDaSolicitacaoTravado` e não grava.
  - **`ChaveCompartilhadaTests`:**
    - `So_CriarAtividadeDaSolicitacao_leva_documento_para_atividade`: em `src`, `.LevarParaAtividade(` só aparece em `CriarAtividadeDaSolicitacao.cs` (sem contar `Documento.cs`, onde é declarado).
    - `Arquivo_de_documento_so_e_montado_com_chave_nova_ou_pelo_proprio_documento`: `new ArquivoDoDocumento(` só aparece em `ArquivoEnviado.cs` e em `Documento.cs`.
    - A mensagem de falha manda registrar o caminho novo na spec (seção 3) antes de acrescentá-lo.
- [ ] **Step 2: Rodar e ver falhar.** `dotnet test tests/AtividadesJuridicas.UnitTests --filter "FullyQualifiedName~AtividadeDaSolicitacaoTests|FullyQualifiedName~ChaveCompartilhadaTests|FullyQualifiedName~AtividadesTests"`.
- [ ] **Step 3: Implementar.**
  - **`ValidacaoDoVinculo`:** extrair de `SalvarAtividade` as linhas 44–65 sem mudar comportamento.
  - **Ordem no manipulador:**
    1. as verificações das Global Constraints;
    2. `Atividade.Criar(..., TipoAutor.Usuario, ator.Id, relogio.AgoraUtc, solicitacao.Id)` com `VinculoDaAtividade.Criar(solicitacao.ClienteId, processo, areaId)`;
    3. `atividades.Adicionar`;
    4. `solicitacao.VincularAtividade(atividade, ConfirmouSolicitacaoCancelada)`;
    5. o comentário;
    6. os documentos (`LevarParaAtividade(atividade.Id, solicitacao.ClienteId)`);
    7. uma `SalvarAsync`.
  - **`ExcecaoDeDominio`** → `Validacao` com a mensagem.
- [ ] **Step 4: Rodar e ver passar.** Mesmo comando.
- [ ] **Step 5: Escrever os testes de integração** (`AtividadeDaSolicitacaoTests`, com `TesteComBanco` e `UsuariosDeClienteDeTeste`):
  - **`Cliente_cancela_depois_de_o_diretor_carregar`:** o diretor lê a versão por `ObterSolicitacao` e o cliente cancela por `CancelarMinhaSolicitacao`.
    - Criar com a versão lida dá `ConfirmacaoNecessaria`.
    - Com a confirmação e a versão antiga, dá `Conflito`.
    - Com a confirmação e a versão relida, cria.
  - **`Cliente_remove_documento_depois_de_o_diretor_carregar` (Review Focus 2):** criar com o documento marcado e a versão antiga dá `Conflito` com `AlteradaDuranteACriacao`, e nada fica gravado.
  - **`Duas_criacoes_simultaneas_so_uma_passa` (Review Focus 1):** dois escopos com a mesma versão, gravando em paralelo (`Task.WhenAll`).
    - Exatamente um `Ok` e um `Conflito`.
    - Uma atividade viva com `solicitacao_id`.
    - O número de comentários e de documentos de atividade é o de uma criação só.
  - **`Documento_levado_baixa_o_mesmo_conteudo`:**
    - `BaixarDocumento` do documento da solicitação e do da atividade devolvem os mesmos bytes.
    - Excluir o da atividade (`ExcluirDocumento`) não impede baixar o da solicitação.
  - **`Producao_responsavel_baixa_o_levado_e_nao_o_da_solicitacao`:**
    - Atividade criada da solicitação, com um usuário de Produção como responsável (`DefinirResponsaveisDaAtividade`).
    - Ele baixa o documento da atividade e recebe a recusa de hoje no da solicitação.
  - **`Usuario_do_cliente_B_nao_alcanca_o_que_veio_do_cliente_A`:** `ObterMinhaSolicitacao` responde `NaoEncontrado` e `BaixarDocumento` do documento levado recusa.
- [ ] **Step 6: Rodar e ver passar.** `dotnet test tests/AtividadesJuridicas.IntegrationTests --filter FullyQualifiedName~AtividadeDaSolicitacaoTests`.
- [ ] **Step 7: Commit.** `feat(app): criar atividade a partir da solicitação, com cancelamento, versão e documentos conferidos no servidor`.

### Task 4: Aplicação — exclusão que desvincula e `SolicitacaoId` na ficha

**Files:**
- Modify: `src/AtividadesJuridicas.Application/Atividades/ExcluirAtividade.cs`
- Modify: `src/AtividadesJuridicas.Application/Atividades/ObterAtividade.cs`
- Modify: `src/AtividadesJuridicas.Application/Atividades/PortasDeAtividades.cs` (`AtividadeDetalhe`)
- Test: `tests/AtividadesJuridicas.UnitTests/Application/AtividadesTests.cs`, `tests/AtividadesJuridicas.IntegrationTests/AtividadeDaSolicitacaoTests.cs`

**Interfaces:**
- Consumes: `ISolicitacaoRepositorio.ObterPorIdAsync`, `SolicitacaoDoCliente.DesvincularAtividade()`.
- Produces: `AtividadeDetalhe` ganha o último parâmetro `Guid? SolicitacaoId = null`.

- [ ] **Step 1: Escrever os testes que falham.**
  - **Unitários:**
    - `Excluir_atividade_da_solicitacao_desvincula_na_mesma_gravacao`: a solicitação volta a `Aguardando`; uma que tinha sido cancelada volta a `Cancelada`; `Unidade` salvou uma vez.
    - `Excluir_atividade_sem_solicitacao_nao_mexe_em_solicitacao`.
    - `Excluir_nao_desvincula_solicitacao_ligada_a_outra_atividade`: a solicitação aponta para outra atividade e continua apontando.
    - `ObterAtividade_traz_SolicitacaoId_so_para_a_Diretoria`: a Produção responsável recebe nulo.
  - **Integração (Review Focus 5):** `Excluir_a_atividade_devolve_a_solicitacao_e_permite_criar_outra`.
    - Criar, excluir a atividade (`ExcluirAtividade`) e conferir por `ObterSolicitacao` a situação `Aguardando` e `AtividadeId` nulo.
    - Pelo portal, `ObterMinhaSolicitacao` mostra "Aguardando análise".
    - Criar de novo dá certo.
- [ ] **Step 2: Rodar e ver falhar.** `dotnet test tests/AtividadesJuridicas.UnitTests --filter FullyQualifiedName~AtividadesTests`.
- [ ] **Step 3: Implementar.**
  - **`ExcluirAtividade`:** recebe `ISolicitacaoRepositorio`. Com `atividade.SolicitacaoId`, carrega a solicitação e, se `AtividadeId == atividade.Id`, chama `DesvincularAtividade()` antes da mesma `SalvarAsync`.
  - **`ObterAtividade`:** passa `ator.EhDiretoria ? atividade.SolicitacaoId : null`.
- [ ] **Step 4: Rodar e ver passar.** Mesmo comando e `dotnet test tests/AtividadesJuridicas.IntegrationTests --filter FullyQualifiedName~AtividadeDaSolicitacaoTests`.
- [ ] **Step 5: Commit.** `feat(app): excluir a atividade devolve a solicitação; a ficha da Diretoria sabe de onde a atividade veio`.

### Task 5: Tela — formulário de atividade com `?solicitacao={id}`

**Files:**
- Create: `src/AtividadesJuridicas.Web/Components/Solicitacoes/SolicitacaoNoFormulario.razor`
- Modify: `src/AtividadesJuridicas.Web/Components/Pages/AtividadeFormulario.razor`
- Modify: `src/AtividadesJuridicas.Web/Components/_Imports.razor` (namespace novo, se preciso)
- Test: `tests/AtividadesJuridicas.IntegrationTests/AtividadeDaSolicitacaoInterativaTests.cs` (novo, no molde de `SolicitacoesInterativasTests` e `AtividadeFormularioInterativoTests`)

**Interfaces:**
- Consumes:
  - `ObterSolicitacao` / `SolicitacaoDetalhe` (fase A);
  - `CriarAtividadeDaSolicitacao`, `TipoErro.ConfirmacaoNecessaria` (Tarefa 3);
  - `AtividadeDetalhe.SolicitacaoId` (Tarefa 4).
- Produces: `SolicitacaoNoFormulario` com os parâmetros:
  - `SolicitacaoDetalhe Solicitacao`;
  - `bool AnexarComentario` + `EventCallback<bool> AnexarComentarioChanged`;
  - `IReadOnlySet<Guid> Marcados` + `EventCallback<IReadOnlySet<Guid>> MarcadosChanged`;
  - `bool Desabilitado`.

- [ ] **Step 1: Escrever os testes de tela que falham.**
  - **`Abre_com_o_cliente_travado_e_a_descricao_preenchida`:**
    - Mostra o bloco "Solicitação do cliente" (quem abriu, quando, urgência, descrição) e o nome do cliente, sem busca de cliente nem "Sem cliente".
    - A descrição da atividade vem igual à da solicitação e o título vem vazio.
    - A caixa do comentário e todos os documentos vêm marcados.
  - **`Sem_documentos_a_lista_nao_aparece`.**
  - **`Marcar_todos_e_desmarcar_todos`.**
  - **`Busca_de_processo_so_traz_processos_do_cliente`:** dois processos, um de outro cliente, e a busca mostra só o do cliente.
  - **`Salvar_cria_e_abre_a_ficha_da_atividade_nova`:** o recado "Atividade salva." e a solicitação vinculada.
  - **`Cancelada_durante_a_criacao_pede_confirmacao`:**
    - O cliente cancela depois da carga e Salvar mostra o aviso com a mensagem exata, "Criar mesmo assim" e "Voltar à solicitação".
    - "Criar mesmo assim" cria a atividade.
    - Se a descrição também mudou, o aviso diz que a solicitação foi alterada.
  - **`Alterada_durante_a_criacao_recarrega_o_bloco_e_mantem_o_digitado`:**
    - O cliente altera a descrição e anexa um documento.
    - Salvar mostra `AlteradaDuranteACriacao`, o bloco com a descrição nova e o documento novo marcado.
    - O título digitado continua no campo.
  - **`Documento_removido_some_depois_do_conflito` (Review Focus 2).**
  - **`Ja_virou_atividade_mostra_o_link`:**
    - Na carga, a página mostra "Esta solicitação já virou atividade." e "Ver atividade", sem formulário.
    - Na gravação (outra aba criou antes), o mesmo aviso aparece acima do formulário.
  - **`Edicao_de_atividade_da_solicitacao_trava_o_cliente`:** sem busca de cliente nem "Sem cliente", e a busca de processo filtrada pelo cliente.
  - **`Producao_nao_abre_o_formulario_com_solicitacao`:** o "Acesso negado." de hoje.
- [ ] **Step 2: Rodar e ver falhar.** `dotnet test tests/AtividadesJuridicas.IntegrationTests --filter FullyQualifiedName~AtividadeDaSolicitacaoInterativaTests`.
- [ ] **Step 3: Implementar.**
  - **Parâmetro novo:** `[SupplyParameterFromQuery(Name = "solicitacao")] Guid? Solicitacao` (só vale sem `Id`).
  - **Carga:** depois do acesso, `ObterSolicitacao`.
    - `NaoEncontrado` → estado "não encontrada", com o texto "Solicitação não encontrada.".
    - `EmAtividade` → o aviso com o link, sem formulário.
    - Senão:
      - cliente, nome e `_versaoDaSolicitacao` vêm da solicitação, e a descrição vem preenchida;
      - todos os documentos vêm marcados.
  - **Cliente travado:** quando há solicitação (nova) ou `SolicitacaoId` (edição).
    - `EscolherProcessoAsync` não troca o cliente.
    - `RemoverProcesso` e `RemoverCliente` não o limpam.
    - O seletor "Cliente do processo" vira texto.
    - A busca passa `ClienteId` a `ListarProcessos`.
  - **Gravação:** com solicitação, envia `CriarAtividadeDaSolicitacao`; senão, `SalvarAtividade`, como hoje.
  - **Respostas:**
    - **`ConfirmacaoNecessaria`:** relê a solicitação (bloco, documentos e versão; os documentos novos entram marcados e os removidos saem) e mostra o aviso de confirmação. "Criar mesmo assim" reenvia com `ConfirmouSolicitacaoCancelada: true` e a versão relida; "Voltar à solicitação" leva a `/solicitacoes/{id}`.
    - **`Conflito`:** relê a solicitação do mesmo jeito.
      - Se ela está `EmAtividade`, mostra "Esta solicitação já virou atividade." com o link.
      - Senão, mostra a mensagem do servidor com o bloco recarregado.
    - **Demais erros:** como hoje.
  - **Cancelar e voltar:** `/solicitacoes/{id}`, com a origem "Solicitação".
- [ ] **Step 4: Rodar e ver passar.** Mesmo comando e `dotnet test tests/AtividadesJuridicas.IntegrationTests --filter "FullyQualifiedName~AtividadeFormulario"` (o formulário sem solicitação não muda).
- [ ] **Step 5: Commit.** `feat(web): formulário de atividade a partir da solicitação, com cliente travado, comentário e documentos`.

### Task 6: Telas — navegação entre as fichas e emendas da documentação

**Files:**
- Modify: `src/AtividadesJuridicas.Web/Components/Pages/SolicitacaoFicha.razor`
- Modify: `src/AtividadesJuridicas.Web/Components/Pages/AtividadeFicha.razor`
- Modify: `docs/superpowers/specs/2026-11-13-solicitacoes-do-cliente-design.md` (estado e seção 10), `docs/superpowers/specs/2026-10-08-portal-do-cliente-design.md` e `docs/superpowers/specs/2026-10-06-modelagem-dominio-design.md` (o que a fase B mudou)
- Test: `tests/AtividadesJuridicas.IntegrationTests/AtividadeDaSolicitacaoInterativaTests.cs`, `SolicitacoesWebTests.cs`, `AtividadeFichaWebTests.cs`

- [ ] **Step 1: Escrever os testes que falham.**
  - **Ficha da solicitação:**
    - Em `Aguardando`, mostra "Criar atividade" com o endereço `/atividades/nova?solicitacao={id}`.
    - Em `Cancelada` e com atividade, não mostra.
  - **Ficha da atividade criada da solicitação:**
    - A Diretoria vê "Ver solicitação" para `/solicitacoes/{id}` na faixa e no "Mais ações".
    - A Produção responsável não vê.
    - A atividade sem solicitação não mostra o botão.
- [ ] **Step 2: Rodar e ver falhar.** `dotnet test tests/AtividadesJuridicas.IntegrationTests --filter "FullyQualifiedName~AtividadeDaSolicitacaoInterativaTests|FullyQualifiedName~SolicitacoesWebTests|FullyQualifiedName~AtividadeFichaWebTests"`.
- [ ] **Step 3: Implementar.**
  - **`SolicitacaoFicha`:** o botão primário "Criar atividade" acima das colunas, só em `Aguardando`.
  - **`AtividadeFicha`:** o botão conforme as Decisões. `TemAcaoDeCadastro` passa a contar `SolicitacaoId` para a faixa e o menu aparecerem.
- [ ] **Step 4: Emendar a documentação.**
  - **Spec das solicitações:**
    - estado "fases A e B implementadas";
    - seção 10 com um bloco "Registrado ao fim da fase B", no formato do da fase A: o que foi feito e as decisões deste plano;
    - a pergunta da auditoria continua "Em aberto".
  - **Portal e modelagem de domínio:**
    - `Atividade.SolicitacaoId` e a trava do cliente;
    - `LevarParaAtividade` e a regra da chave compartilhada;
    - `TipoErro.ConfirmacaoNecessaria`.
- [ ] **Step 5: Rodar a suíte inteira** (unitários e integração, em segundo plano, com trx) e anotar os totais.
- [ ] **Step 6: Commit.** `feat(web): navegação entre a ficha da atividade e a da solicitação; docs: emendas da fase B`.

Ao fim da Tarefa 6, o controlador confere no navegador, num ambiente descartável (memória do projeto):
- criar a partir da solicitação com e sem documentos;
- o cliente cancela no meio e o diretor cria mesmo assim;
- o cliente altera no meio;
- duas abas;
- excluir a atividade e ver a solicitação voltar;
- as larguras de tablet e celular do formulário.
