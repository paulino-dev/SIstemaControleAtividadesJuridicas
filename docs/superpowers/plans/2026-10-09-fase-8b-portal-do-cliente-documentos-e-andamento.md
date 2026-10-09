# Fase 8B — Portal do cliente: documentos e andamento: Plano de Implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** O cliente passa a baixar, pelo portal, os documentos que o escritório disponibiliza a ele e os documentos comuns a todos os clientes, e a acompanhar o andamento das atividades ligadas a ele. A Diretoria ganha as duas áreas de envio, com confirmação obrigatória nos Documentos comuns.

**Architecture:** As duas áreas são categorias novas do agregado `Documento`, sem lista de acesso; a regra única `PodeSerAcessadoPor` passa a receber o cliente do usuário. As consultas do portal não recebem Id de cliente: usam o do ator, lido do banco. O andamento é uma consulta sobre as atividades vivas do cliente que devolve só título, área, processo, situação e data de conclusão, sem identificadores. O portão do executor abre mais quatro requisições.

**Tech Stack:** .NET 10, Blazor Interactive Server, EF Core 10 + Npgsql, PostgreSQL 17, xUnit 2.9, FluentAssertions 7.2, Testcontainers.

**Spec:** `docs/superpowers/specs/2026-10-08-portal-do-cliente-design.md` (seções 4, 5, 6, 7, 8, 9, 10 e a lista "Registrado na revisão da 8A, para o plano da 8B" da seção 11). A modelagem `docs/superpowers/specs/2026-10-06-modelagem-dominio-design.md` é emendada na Tarefa 6.

## Decisões deste plano

- **O cliente recebe um tipo próprio de linha de documento**, `DocumentoDoPortal`: Id, nome, tamanho e data de envio. O nome do arquivo original e a categoria não vão. O autor do envio só é preenchido para a Diretoria.
- **Consultas próprias no repositório para o portal.** O filtro das listagens internas (`FiltroDeAcesso`, por perfil e por usuário) não conhece o tipo do perfil e não é reaproveitado para o cliente.
- **A linha do andamento não tem Id.** Não há ficha de atividade para o cliente, então nenhum identificador interno precisa sair.
- **`ListarDocumentosDoCliente` com a categoria nova:** `DocumentoDoCliente` pedido por quem não é da Diretoria responde `AcessoNegado`, como o Contrato de Honorários; `DocumentoComum` responde `Validacao`, porque documento comum não é de cliente.
- **A confirmação dos Documentos comuns vale em dois lugares.** Na tela, uma classe pura (`EnvioComConfirmacao`) guarda o estado do aviso e é testada sem página. No caso de uso, o envio sem `ConfirmouVisibilidade` é recusado antes de o arquivo ser gravado.
- **O componente `DocumentosDoCliente` passa a atender também os Documentos comuns da Diretoria**, com `ClienteId` opcional. Renomear e excluir já estão nele; repeti-los em outro componente duplicaria a lógica.
- **O cliente usa um componente novo e pequeno, só de leitura** (`DocumentosParaBaixar`), nas páginas "Meus documentos" e "Documentos comuns".
- **`/documentos-comuns` é compartilhada.** A Diretoria e o cliente a abrem; para a Produção, a própria página mostra "Página não encontrada" (o layout não distingue Diretoria de Produção antes de consultar o banco).
- **Guarda inversa no layout.** `/andamento` e `/meus-documentos` não são montadas para quem não é cliente, como as páginas internas não são montadas para o cliente.
- **Página interna aberta pelo cliente continua respondendo 200 com "Página não encontrada".** A revisão da 8A sugeriu 404; fica como está: só revela que a rota existe, e mudar depende de como o Blazor trata o status no meio da renderização, o mesmo tipo de dependência que travou a 8A.
- **A contagem do aviso de inativação do cliente é feita na tela**, com `ListarUsuariosDoCliente`, sem mudar o caso de uso `DefinirAtivoCliente`.
- **O início do cliente faz uma consulta só** (`ListarMeuAndamento`). Por isso o custo do contexto de acesso, lido uma vez a mais pelo portão, não é medido nesta fase.
- **Sem índice novo.** A lista de Documentos comuns filtra só pela categoria; o volume esperado é de dezenas de arquivos.

## Como ler este plano

Cada tarefa traz as **regras** (o contrato), as **interfaces** que ela consome e produz e a lista de **testes obrigatórios** (nome e o que cada um confere). Onde o detalhe é sutil ou é a própria proteção (a regra de acesso, as restrições do banco, a consulta do andamento, a confirmação, a guarda de rotas), o plano traz o código. O restante é escrito pelo implementador no estilo do que já existe. O código do plano não foi executado: quando o EF Core, o Npgsql ou o Blazor exigirem, ajuste o mínimo para preservar o comportamento e registre o desvio no relatório, com a mensagem de erro que o motivou.

Nunca apague um teste nem afrouxe uma asserção. Ajustar como um teste obtém as suas entradas é permitido. Quando uma resposta mudar porque a regra mudou (o download deixa de ser barrado pelo portão e passa a ser decidido pela regra do documento), atualize o teste e liste-o no relatório. O texto de `Dados` gravado em `jsonb` volta reformatado pelo PostgreSQL: compare JSON pela estrutura. Nos testes de integração, nunca escolha um registro de auditoria pela posição: selecione por operação, evento ou entidade.

## Estado de partida

- Worktree `.claude/worktrees/feat+fase-8-portal-do-cliente`, branch `worktree-feat+fase-8-portal-do-cliente`, no commit `9a5ce43`, que é a `main` com as fases 7 (complexidade e produtividade) e 8A (perfil Cliente e portão) mescladas.
- Domínio: `CategoriaDocumento { DocumentoGeral = 1, ContratoHonorarios = 2, DocumentoAtividade = 3 }`; `Documento.PodeSerAcessadoPor(usuarioId, perfilId, tipoPerfil, ehResponsavelDaAtividade = false)`, que responde falso para o perfil Cliente em todas as categorias; `Atividade` com `Complexidade`.
- Aplicação: `UsuarioAutenticado(Id, Nome, TipoPerfil, PerfilId, ClienteId = null)` com `EhDiretoria`, `EhCliente` e `EhInterno`; o portão do `ExecutorDeRequisicoes` barra todo ator que não é interno, salvo nas requisições marcadas com `IRequisicaoAbertaAoCliente` (hoje `AutenticarUsuario`, `ObterMeuAcesso` e `AlterarMinhaSenha`); `ListarUsuariosDoCliente`; `ArquivoEnviado.Validar` e `ArquivoEnviado.GravarAsync` (o que todo envio tem em comum).
- Web: `MainLayout.razor` decide pelo cookie (`atividades:portal`) se monta a página; `RotasDoCliente` lista as rotas do cliente; `DocumentosDoCliente.razor` é o componente das áreas de documentos da ficha do cliente; `Home.razor` mostra ao cliente a saudação e o link da senha.
- Testes: `TelaDeTeste.RenderizarAsync<TComponente>` desenha um componente fora da página (só o primeiro desenho; não serve a componente com `InputFile`); `AplicacaoDeTeste.CriarCarlaAsync()` cria a cliente "Maria Silva" e a usuária "Carla Cliente"; as constantes de login dela estão em `UsuariosDeClienteDeTeste`.
- Linha de base: rode `dotnet test` inteiro antes da Tarefa 1 e anote os totais no relatório. A suíte de integração leva de 15 a 35 minutos nesta máquina.

## Global Constraints

- TFM único `net10.0` em `Directory.Build.props`. `Nullable` e `TreatWarningsAsErrors=true`. Versões de pacote só em `Directory.Packages.props`. Nenhum pacote novo.
- `Domain` não referencia nada. `Application` não referencia EF Core, Npgsql, `Infrastructure` nem `Web`.
- Dentro da `Application`: `Identidade` não usa tipos de nenhum outro contexto; `Documentos` usa `Identidade` e `Cadastros` e não usa `Atividades`; `Atividades` usa `Identidade`, `Cadastros` e `Documentos`.
- Código, nomes de tabela e mensagens em português. Tabelas e colunas em `snake_case`. Datas e horas em UTC no domínio, na aplicação e no banco; a tela converte com `FusoDoEscritorio`.
- Exclusão sempre lógica. Agregados se referenciam só por Id.
- Toda alteração de `Entidade` gera registro de auditoria na mesma transação. Toda propriedade mapeada nova é classificada em `PropriedadesAuditadasTests`. Nenhuma gravação por fora dos interceptores: `GravacoesPorForaTests` continua com a mesma lista.
- Autorização verificada no manipulador e, para quem não é interno, antes dele, no executor. A interface apenas oculta.
- Um caso de uso por arquivo. Novas dependências de construtor e novos membros posicionais de registros entram por último.
- Consultas são `IConsultaAuditavel`.
- As consultas do portal não têm campo de Id de cliente: usam o `ClienteId` do ator.
- Uma única migration nesta fase: `DocumentosDoPortal`.
- Todo texto que um teste procura no HTML é escrito por **uma única** expressão Razor ou por marcação fixa. Texto fixo misturado com expressão na mesma célula pode vir separado por marcadores do Blazor.
- Rotas: `/andamento`, `/meus-documentos`, `/documentos-comuns`.
- Requisições abertas ao cliente ao fim da fase, exatamente: `AlterarMinhaSenha`, `AutenticarUsuario`, `BaixarDocumento`, `ListarDocumentosComuns`, `ListarMeuAndamento`, `ListarMeusDocumentos`, `ObterMeuAcesso`.
- Mensagens e textos exatos:
  - `"Confirme que o documento ficará visível para todos os clientes."`
  - `"Documentos desta área ficam visíveis para todos os clientes do escritório. Não envie aqui arquivos de um cliente específico."`
  - Botões `Confirmar envio` e `Cancelar`
  - `"Documento visível ao cliente não tem lista de acesso."`
  - `"Categoria inválida para envio."` (já existe) e `"Categoria inválida para esta consulta."`
  - `"Situação inválida."`
  - `"O título aparece para o cliente no portal."`
  - Área da ficha: `Documentos do cliente (visíveis ao cliente)`
  - Menu e títulos: `Andamento`, `Meus documentos`, `Documentos comuns`
  - Situações: `Em andamento` e `Concluída`
  - `Nenhuma atividade.`, `Nenhum documento.`, `Tentar de novo`, `Concluída em`
  - `"Cliente inativado."`, `"Cliente inativado. 1 usuário do portal ficou sem acesso."`, `"Cliente inativado. 3 usuários do portal ficaram sem acesso."` (com o número de usuários ativos)

## Review Focus

1. **O cliente cola no navegador o endereço de download de um documento de outro cliente, ou de um documento interno**: recebe 404, e a tentativa fica na auditoria com o documento. (Tarefa 2: `Cliente_A_nao_lista_nem_baixa_documento_do_cliente_B`, `Cliente_baixa_pelo_endereco_o_documento_dele_e_recebe_404_nos_demais`)
2. **A Diretoria corrige o cliente de um usuário**: ele passa a ver os documentos e o andamento do cliente novo e deixa de ver os do antigo. (Tarefas 2 e 3: `Trocar_o_cliente_do_usuario_troca_os_documentos_que_ele_ve`, `Trocar_o_cliente_do_usuario_troca_o_andamento_que_ele_ve`)
3. **O diretor escolhe um arquivo em Documentos comuns, clica em Enviar e desiste**: nada sobe e o arquivo continua escolhido; um envio sem a confirmação é recusado pelo caso de uso sem gravar arquivo nem registro. (Tarefas 2 e 4: `Documento_comum_sem_confirmacao_nao_grava_arquivo_nem_registro`, `Cancelar_fecha_o_aviso_e_o_proximo_pedido_avisa_de_novo`)
4. **Uma atividade concluída é reaberta, excluída ou muda de cliente**: volta a "Em andamento" sem data de conclusão, ou some do andamento do cliente. (Tarefa 3: `Atividade_reaberta_volta_a_em_andamento_sem_data_de_conclusao`, `Atividade_excluida_ou_que_muda_de_cliente_sai_do_andamento`)
5. **Cliente recém-cadastrado, sem atividade e sem documento**: as telas mostram zeros e "Nenhum…", sem erro. (Tarefas 4 e 5: `Cliente_sem_documentos_ve_nenhum_documento`, `Cliente_sem_atividades_ve_zeros_e_nenhuma_atividade`)
6. **Um usuário de Produção procura os documentos novos**: não os lista, não os baixa, não os vê em nenhuma listagem interna, e a página de Documentos comuns não existe para ele. (Tarefas 2 e 4: `Producao_nao_lista_nem_baixa_as_duas_categorias`, `Producao_em_documentos_comuns_ve_nao_encontrada`)
7. **Banco em uso, com documentos já gravados**: a migration troca as restrições sem falhar e sem tocar nos documentos. (Tarefa 1: `Migration_troca_a_restricao_do_cliente_num_banco_que_ja_tem_documentos`)

---

### Task 1: Categorias `DocumentoDoCliente` e `DocumentoComum` no domínio e no banco

**Files:**
- Modify: `src/AtividadesJuridicas.Domain/Documentos/Documento.cs`
- Modify: `src/AtividadesJuridicas.Infrastructure/Persistencia/Configuracoes/DocumentoConfiguracao.cs`
- Modify: `src/AtividadesJuridicas.Infrastructure/Persistencia/Repositorios/DocumentoRepositorio.cs`
- Create: migration `DocumentosDoPortal` em `src/AtividadesJuridicas.Infrastructure/Persistencia/Migrations/`
- Modify: `tests/AtividadesJuridicas.UnitTests/Application/Falsos.cs` (`DocumentoRepositorioFalso`)
- Test: `tests/AtividadesJuridicas.UnitTests/Domain/DocumentoTests.cs`; `tests/AtividadesJuridicas.IntegrationTests/DocumentosTests.cs`; `tests/AtividadesJuridicas.IntegrationTests/MigracaoDosDocumentosDoPortalTests.cs` (novo)

**Interfaces — Produces:**
- `enum CategoriaDocumento { DocumentoGeral = 1, ContratoHonorarios = 2, DocumentoAtividade = 3, DocumentoDoCliente = 4, DocumentoComum = 5 }`
- `Documento.CriarDoCliente(Guid clienteId, string? nome, ArquivoDoDocumento arquivo): Documento`
- `Documento.CriarComum(string? nome, ArquivoDoDocumento arquivo): Documento`
- `Documento.VisivelAoCliente` (`bool`, só leitura, não mapeada): verdadeiro nas duas categorias novas
- `Documento.PodeSerAcessadoPor(Guid usuarioId, Guid perfilId, TipoPerfil tipoPerfil, bool ehResponsavelDaAtividade = false, Guid? clienteDoUsuario = null): bool`
- `Documento.SemListaDeAcesso` (constante pública com a mensagem)

**Regras:**

1. **Criação.** `CriarDoCliente` exige cliente (a mensagem de cliente obrigatório que já existe; `Guid.Empty` também). `CriarComum` não recebe cliente: `ClienteId` e `AtividadeId` ficam nulos. As duas nascem com as listas de acesso vazias. As validações de nome e de arquivo são as de `Criar`, que já existem. No `Criar` privado, a exigência de cliente, que hoje vale para tudo que não é documento de atividade, passa a valer para Documento Geral, Contrato de Honorários e Documento do cliente. Atualize os comentários de `ClienteId` e da classe.

2. **Regra de acesso.** O método fica assim:

```csharp
    /// <summary>
    /// A única regra de acesso a documentos. <paramref name="ehResponsavelDaAtividade"/> só conta no documento de
    /// atividade, e quem a responde é quem consulta a atividade. <paramref name="clienteDoUsuario"/> é o cliente
    /// de um usuário de perfil Cliente (nulo para a equipe).
    /// </summary>
    public bool PodeSerAcessadoPor(
        Guid usuarioId, Guid perfilId, TipoPerfil tipoPerfil, bool ehResponsavelDaAtividade = false,
        Guid? clienteDoUsuario = null)
    {
        if (tipoPerfil == TipoPerfil.Diretoria) return true;

        // O cliente alcança só o que foi posto para ele: os documentos do próprio cliente e os comuns. Nenhuma
        // outra categoria, ainda que ele conste em alguma lista.
        if (tipoPerfil == TipoPerfil.Cliente)
            return Categoria == CategoriaDocumento.DocumentoComum ||
                   (Categoria == CategoriaDocumento.DocumentoDoCliente && clienteDoUsuario is { } cliente && ClienteId == cliente);

        // Daqui em diante só a Produção: um tipo de perfil desconhecido não acessa nada.
        if (tipoPerfil != TipoPerfil.Producao) return false;

        // O que é visível ao cliente não é da Produção.
        if (VisivelAoCliente) return false;

        // Sem exceção: nem quem consta na lista (por ter sido da Diretoria) alcança um contrato.
        if (Categoria == CategoriaDocumento.ContratoHonorarios) return false;

        // As listas não contam: o acesso acompanha os responsáveis de hoje.
        if (Categoria == CategoriaDocumento.DocumentoAtividade) return ehResponsavelDaAtividade;

        return _perfisComAcesso.Any(a => a.PerfilId == perfilId) ||
               _usuariosComAcesso.Any(a => a.UsuarioId == usuarioId);
    }
```

3. **Sem lista de acesso.** `DefinirAcesso` em documento com `VisivelAoCliente` lança `ExcecaoDeDominio(SemListaDeAcesso)`, com `SemListaDeAcesso = "Documento visível ao cliente não tem lista de acesso."`, logo depois da recusa do documento de atividade e antes de qualquer outra verificação.

4. **Banco.** Em `DocumentoConfiguracao`, a restrição do cliente é trocada (a da atividade não muda):

```csharp
        // O cliente é obrigatório no documento geral (1), no contrato (2) e no documento do cliente (4); opcional
        // no documento de atividade (3); e não existe no documento comum (5). Só o de atividade tem atividade.
        b.ToTable("documentos", t =>
        {
            t.HasCheckConstraint("ck_documentos_cliente_conforme_a_categoria",
                "(categoria IN (1, 2, 4) AND cliente_id IS NOT NULL) OR categoria = 3 OR (categoria = 5 AND cliente_id IS NULL)");
            t.HasCheckConstraint("ck_documentos_atividade_so_na_categoria_3", "(categoria = 3) = (atividade_id IS NOT NULL)");
        });
```

   Gere a migration:

```bash
dotnet ef migrations add DocumentosDoPortal --project src/AtividadesJuridicas.Infrastructure --startup-project src/AtividadesJuridicas.Infrastructure --output-dir Persistencia/Migrations
```

   Confira que ela só remove `ck_documentos_cliente_obrigatorio` e cria `ck_documentos_cliente_conforme_a_categoria`, e que o `Down` faz o inverso. Qualquer outra mudança é sinal de modelo fora de sincronia: pare e registre. Se algum teste existente citar o nome antigo da restrição, troque pelo novo e relate.

5. **Listagem interna.** Em `DocumentoRepositorio.ListarDoClienteAsync`, quando há `FiltroDeAcesso` (quem consulta não é da Diretoria), as duas categorias novas nunca vêm: acrescente `d.Categoria != CategoriaDocumento.DocumentoDoCliente && d.Categoria != CategoriaDocumento.DocumentoComum` à condição. O `PodeVer` de `DocumentoRepositorioFalso` responde falso para elas. O comentário da porta em `PortasDeDocumentos.cs` passa a citar as duas.

**Testes obrigatórios:**

`DocumentoTests` (unitário):
- `Documento_do_cliente_exige_cliente_e_nasce_sem_listas` — sem cliente e com `Guid.Empty` a mensagem é a de cliente obrigatório; com cliente, `Categoria`, `ClienteId`, `AtividadeId` nulo, listas vazias e `VisivelAoCliente` verdadeiro.
- `Documento_comum_nasce_sem_cliente_e_sem_listas` — `ClienteId` e `AtividadeId` nulos, listas vazias, `VisivelAoCliente` verdadeiro; as três categorias antigas têm `VisivelAoCliente` falso.
- `Regra_de_acesso_por_tipo_de_perfil_e_categoria` — teoria que cobre a matriz inteira. Para cada uma das cinco categorias, os atores: Diretoria; Produção que consta na lista (ou é responsável, no documento de atividade); Produção que não consta; Cliente dono do documento; Cliente de outro cliente; Cliente sem `clienteDoUsuario`; e um tipo desconhecido, `(TipoPerfil)99`. Resultado esperado:

| Categoria | Diretoria | Produção na lista / responsável | Produção fora | Cliente dono | Outro cliente | Cliente sem cliente | Tipo 99 |
|---|---|---|---|---|---|---|---|
| `DocumentoGeral` | sim | sim | não | não | não | não | não |
| `ContratoHonorarios` | sim | não | não | não | não | não | não |
| `DocumentoAtividade` | sim | sim | não | não | não | não | não |
| `DocumentoDoCliente` | sim | não | não | sim | não | não | não |
| `DocumentoComum` | sim | não | não | sim | sim | sim | não |

   Nas colunas de Cliente e do tipo 99, passe também `ehResponsavelDaAtividade: true` e o `perfilId`/`usuarioId` que constam na lista do Documento Geral: nada disso pode abrir o acesso.
- `Categorias_visiveis_ao_cliente_nao_tem_lista_de_acesso` — teoria sobre as duas: `DefinirAcesso` com um perfil interno lança `SemListaDeAcesso`, e as listas continuam vazias.

`DocumentosTests` (integração):
- `Documento_do_cliente_e_documento_comum_vao_e_voltam_do_banco` — gravados pelo `AppDbContext`, relidos em outro escopo.
- `Banco_recusa_documento_comum_com_cliente_e_documento_do_cliente_sem_cliente` — violação de `ck_documentos_cliente_conforme_a_categoria` (SqlState `23514`), provocada por `UPDATE` direto na linha, no teste.
- `Documentos_das_categorias_antigas_continuam_validos` — um Documento Geral, um Contrato e um documento de atividade avulsa (sem cliente) são gravados e relidos.

`MigracaoDosDocumentosDoPortalTests` (integração, novo; siga `MigracaoDeComplexidadeTests.cs`, que já faz o mesmo para a fase 7, com `IMigrator`):
- `Migration_troca_a_restricao_do_cliente_num_banco_que_ja_tem_documentos` — migre até `ComplexidadeDaAtividade` (a última antes desta fase); grave por SQL um cliente, uma atividade avulsa e um documento de cada categoria antiga (o de atividade, sem cliente); migre até o fim. A migration não falha; os três documentos continuam lá, iguais; inserir um documento de categoria 5 com cliente, ou de categoria 4 sem cliente, é recusado por `ck_documentos_cliente_conforme_a_categoria`; e inserir um de categoria 5 sem cliente é aceito.

- [ ] testes → falham → implementar → migration → `dotnet test` inteiro e `dotnet build` (zero avisos) → commit:

```bash
git add src tests
git commit -m "feat(documentos): categorias Documento do cliente e Documento comum, com a regra de acesso pelo cliente do usuário"
```

---

### Task 2: Casos de uso dos documentos do portal

**Files:**
- Modify: `src/AtividadesJuridicas.Application/Documentos/PortasDeDocumentos.cs`, `EnviarDocumento.cs`, `ListarDocumentosDoCliente.cs`, `BaixarDocumento.cs`
- Create: `src/AtividadesJuridicas.Application/Documentos/EnviarDocumentoComum.cs`, `ListarMeusDocumentos.cs`, `ListarDocumentosComuns.cs`
- Modify: `src/AtividadesJuridicas.Infrastructure/Persistencia/Repositorios/DocumentoRepositorio.cs`, `AcessoInicialDePerfil.cs`
- Modify: `tests/AtividadesJuridicas.UnitTests/Application/Falsos.cs`, `PortaoDoClienteTests.cs`
- Modify: `tests/AtividadesJuridicas.IntegrationTests/PortalDoClienteTests.cs`, `PortalDoClienteWebTests.cs`, `PerfisTests.cs`
- Test: `tests/AtividadesJuridicas.UnitTests/Application/DocumentosDoPortalTests.cs` (novo); `tests/AtividadesJuridicas.IntegrationTests/DocumentosDoPortalTests.cs` (novo)

**Interfaces:**
- Consumes (Tarefa 1): `CategoriaDocumento.DocumentoDoCliente`, `CategoriaDocumento.DocumentoComum`, `Documento.CriarDoCliente`, `Documento.CriarComum`, `Documento.PodeSerAcessadoPor(..., clienteDoUsuario)`.
- Produces (namespace `AtividadesJuridicas.Application.Documentos`):
  - `record DocumentoDoPortal(Guid Id, string Nome, long TamanhoBytes, DateTime EnviadoEm, string? EnviadoPor = null)`
  - `DocumentosOpcoes.ConfirmacaoExigida = "Confirme que o documento ficará visível para todos os clientes."`
  - `IDocumentoRepositorio.ListarDoPortalAsync(Guid clienteId, int pagina, int tamanho, CancellationToken ct): Task<Pagina<DocumentoDoPortal>>` — os `DocumentoDoCliente` daquele cliente, sem autor
  - `IDocumentoRepositorio.ListarComunsAsync(bool comAutor, int pagina, int tamanho, CancellationToken ct): Task<Pagina<DocumentoDoPortal>>` — todos os `DocumentoComum`
  - `record EnviarDocumentoComum(string? Nome, string NomeArquivo, string? TipoConteudo, Stream Conteudo, bool ConfirmouVisibilidade) : IRequisicao<Resultado<Guid>>`
  - `record ListarMeusDocumentos(int Pagina = 1) : IRequisicao<Resultado<Pagina<DocumentoDoPortal>>>, IConsultaAuditavel, IRequisicaoAbertaAoCliente`
  - `record ListarDocumentosComuns(int Pagina = 1) : IRequisicao<Resultado<Pagina<DocumentoDoPortal>>>, IConsultaAuditavel, IRequisicaoAbertaAoCliente`
  - `BaixarDocumento` passa a implementar `IRequisicaoAbertaAoCliente`

**Regras:**

1. **`EnviarDocumento`** aceita também `DocumentoDoCliente` e cria com `Documento.CriarDoCliente`. Continua só da Diretoria. `DocumentoComum` e `DocumentoAtividade` por esta requisição respondem `Validacao` `"Categoria inválida para envio."`. O cliente precisa existir; cliente inativo continua recebendo documentos.

2. **`EnviarDocumentoComum`.** Só Diretoria (`AcessoNegado`). Sem `ConfirmouVisibilidade` verdadeiro, responde `Validacao` com `DocumentosOpcoes.ConfirmacaoExigida`, **antes** de validar ou gravar o arquivo: o armazenamento não é tocado. Depois segue como os outros envios: `ArquivoEnviado.Validar`, `ArquivoEnviado.GravarAsync` com `Documento.CriarComum`.

3. **`ListarDocumentosDoCliente`.** Depois da verificação do ator: categoria `DocumentoComum` → `Validacao` `"Categoria inválida para esta consulta."`; categoria `DocumentoDoCliente` pedida por quem não é da Diretoria → `AcessoNegado`, na mesma condição que hoje nega o Contrato de Honorários. Para a Diretoria, lista como as demais categorias.

4. **`ListarMeusDocumentos`.** O ator precisa ser `{ EhCliente: true, ClienteId: { } }`; qualquer outro, inclusive a Diretoria, recebe `AcessoNegado`. Lista por `ListarDoPortalAsync` com o `ClienteId` do ator. `TipoRecurso = "Documento"`, `Listou`, `Detalhe` com a página. Página normalizada com `Paginacao.Normalizar` e `DocumentosOpcoes.TamanhoDaPagina`, como as listagens existentes.

5. **`ListarDocumentosComuns`.** O ator precisa ser da Diretoria ou de perfil Cliente; a Produção recebe `AcessoNegado`. `comAutor` é `ator.EhDiretoria`: para o cliente, `EnviadoPor` vem nulo. Mesmos metadados de auditoria da regra 4.

6. **`BaixarDocumento`.** Ganha a marca `IRequisicaoAbertaAoCliente` e passa `clienteDoUsuario: ator.ClienteId` a `PodeSerAcessadoPor`. O resto não muda: sem acesso, a resposta é `NaoEncontrado` e a tentativa gera `acesso_negado` com o documento.

7. **Repositório.** As duas consultas novas ordenam do mais recente para o mais antigo (`CriadoEm`, depois `Id`, decrescentes), como `PaginaAsync`. O nome do autor é buscado só quando `comAutor`, com `IgnoreQueryFilters` em `usuarios`, como já se faz. Nenhuma das duas usa `FiltroDeAcesso`. Os falsos ganham as duas, com a mesma semântica.

8. **Porta do acesso inicial de perfil.** `AcessoInicialDePerfil.IncluirNosDocumentosGeraisAsync` passa a conferir o tipo do perfil no próprio SQL: um perfil de tipo `Cliente` não recebe nenhuma entrada. Acrescente à cláusula `WHERE`:

```sql
              AND EXISTS (
                  SELECT 1 FROM perfis p
                  WHERE p.id = CAST({perfilId} AS uuid) AND p.tipo <> {tipoCliente})
```

   com `var tipoCliente = (int)TipoPerfil.Cliente;`. Atualize o comentário da classe.

9. **Lista fixada do portão.** Em `PortaoDoClienteTests.As_requisicoes_abertas_ao_cliente_sao_exatamente_estas`, a lista passa a `["AlterarMinhaSenha", "AutenticarUsuario", "BaixarDocumento", "ListarDocumentosComuns", "ListarMeusDocumentos", "ObterMeuAcesso"]`. A Tarefa 3 acrescenta `ListarMeuAndamento`.

10. **Testes da 8A que mudam porque o download foi aberto.** Atualize e relate cada um:
    - `PortalDoClienteTests.Cliente_nao_alcanca_nenhum_caso_de_uso_interno_e_cada_tentativa_fica_registrada`: `BaixarDocumento` sai da lista de requisições barradas pelo portão. Aproveite para trocar o `ObterProcesso` de Id inexistente pelo processo da própria cliente (guarde o Id no `InitializeAsync`) e para deixar a conferência de "sem valor" estrita: nulo, ou o padrão de um tipo de **valor**.
    - `PortalDoClienteWebTests.Cliente_nao_baixa_documento_interno`: a resposta passa de 403 para 404, e o `acesso_negado` passa a levar o documento (`EntidadeId`) e `resposta` `nao_encontrado`. Ele é substituído pelo teste web da lista abaixo.
    - O campo `_contrato` de `PortalDoClienteTests`, hoje atribuído e nunca lido, passa a ser usado no teste de download negado.

**Testes obrigatórios:**

`DocumentosDoPortalTests` (unitário, com `Cenario`: a Diretora, o Pedro de Produção, as clientes Maria e José, a Carla usuária da Maria):
- `Diretor_envia_documento_do_cliente_sem_lista_de_acesso`
- `Producao_e_cliente_nao_enviam_documento_do_cliente_nem_documento_comum` — `AcessoNegado`, nada gravado.
- `Documento_comum_so_e_enviado_com_a_confirmacao_e_sem_ela_nada_e_gravado` — mensagem exata; `Armazenamento.Arquivos` vazio; `Unidade.Salvamentos` em zero; com a confirmação, o documento nasce sem cliente.
- `Documento_comum_nao_e_enviado_pelo_envio_de_cliente` — `"Categoria inválida para envio."`.
- `Cliente_lista_so_os_documentos_do_proprio_cliente_e_sem_autor` — com documentos do cliente da Maria e do José e um Documento Geral e um Contrato da Maria: a Carla recebe só os `DocumentoDoCliente` da Maria; `EnviadoPor` nulo.
- `Meus_documentos_e_so_para_o_perfil_cliente` — Diretoria, Produção e sem ator recebem `AcessoNegado`.
- `Documentos_comuns_chegam_ao_cliente_sem_autor_e_a_diretoria_com_autor_e_nunca_a_producao`
- `Na_ficha_o_documento_do_cliente_e_so_da_diretoria_e_o_comum_nao_e_de_cliente` — `ListarDocumentosDoCliente`: Produção recebe `AcessoNegado` na categoria `DocumentoDoCliente`; `DocumentoComum` responde `Validacao` com a mensagem exata, para a Diretoria também.
- `Cliente_baixa_o_documento_do_proprio_cliente_e_o_comum`
- `Cliente_nao_baixa_documento_de_outro_cliente_nem_documento_interno` — `NaoEncontrado`; evento `acesso_negado` imediato com a entidade `Documento` e o Id.
- `Producao_nao_baixa_documento_do_cliente_nem_documento_comum` — `NaoEncontrado`, com o evento.
- `Consultas_do_portal_sao_auditaveis_e_nao_tem_campo_de_cliente` — `TipoRecurso`, `Acao` e, por reflexão, nenhuma propriedade pública de `ListarMeusDocumentos` nem de `ListarDocumentosComuns` contém "Cliente" no nome.

`PortaoDoClienteTests`: a lista da regra 9.

`DocumentosDoPortalTests` (integração, `TesteComBanco`; no `InitializeAsync`: admin, Pedro de Produção, Maria com a usuária Carla, José com o usuário João, criados pelos casos de uso):
- `Cliente_A_nao_lista_nem_baixa_documento_do_cliente_B` — nos dois sentidos; o download negado é `NaoEncontrado` com `acesso_negado` do documento.
- `Producao_nao_lista_nem_baixa_as_duas_categorias` — `ListarMeusDocumentos` e `ListarDocumentosComuns` negados; `BaixarDocumento` `NaoEncontrado`; `ListarDocumentosDoCliente` de Documento Geral, como o Pedro, não traz os documentos novos da Maria.
- `Documentos_comuns_chegam_a_todo_cliente_e_o_autor_so_a_diretoria`
- `Documento_comum_sem_confirmacao_nao_grava_arquivo_nem_registro` — a quantidade de arquivos em `PastaDeArquivos` e a de documentos no banco não mudam.
- `Trocar_o_cliente_do_usuario_troca_os_documentos_que_ele_ve` — depois de `EditarUsuario` da Carla para o José: lista os do José; baixar um da Maria responde `NaoEncontrado`.
- `Cliente_inativo_continua_recebendo_documentos_da_diretoria` — com a Maria inativa, `EnviarDocumento` de `DocumentoDoCliente` tem sucesso e a Diretoria o lista; a Carla não tem acesso (regra da 8A).
- `Listas_do_portal_sao_paginadas_da_mais_recente_para_a_mais_antiga` — 21 documentos comuns: a primeira página traz 20, na ordem inversa do envio, com total 21.
- `Excluir_documento_comum_o_tira_de_todos_os_clientes` — some da lista da Carla e do João; o download passa a `NaoEncontrado`.
- `Listagens_e_downloads_do_cliente_ficam_no_registro_de_consultas` — dois `Listou` e um `Baixou` no recurso `Documento`, com o usuário da Carla.
- `Envio_renomeacao_e_exclusao_ficam_na_auditoria_do_documento` — registros de `Criacao`, `Alteracao` e `Exclusao` de `Documento`, sem nenhuma entrada de lista de acesso.

`PerfisTests` (integração): `Porta_do_acesso_inicial_nao_inclui_perfil_de_cliente` — com um Documento Geral no banco, chamar `IAcessoInicialDePerfil.IncluirNosDocumentosGeraisAsync` com o Id do perfil Cliente devolve 0 e não cria entrada; com um perfil de Produção recém-criado por `SalvarPerfil`, a entrada continua existindo (teste que já existe).

`PortalDoClienteWebTests`: `Cliente_baixa_pelo_endereco_o_documento_dele_e_recebe_404_nos_demais` — `GET /documentos/{id}/baixar` como a Carla: 200 e anexo no documento do cliente da Maria e no comum; 404 no documento de outro cliente, no Documento Geral e no Contrato da própria Maria; um `acesso_negado` com o documento para cada 404.

- [ ] testes → falham → implementar (Parte A: aplicação e unitários; Parte B: repositório, SQL da porta e integração) → `dotnet test` inteiro → commit:

```bash
git add src tests
git commit -m "feat(documentos): envio e consultas dos documentos do cliente e dos documentos comuns, com download aberto ao cliente"
```

---

### Task 3: Andamento das atividades do cliente

**Files:**
- Modify: `src/AtividadesJuridicas.Application/Atividades/PortasDeAtividades.cs`
- Create: `src/AtividadesJuridicas.Application/Atividades/ListarMeuAndamento.cs`
- Modify: `src/AtividadesJuridicas.Infrastructure/Persistencia/Repositorios/AtividadeRepositorio.cs`
- Modify: `tests/AtividadesJuridicas.UnitTests/Application/Falsos.cs`, `PortaoDoClienteTests.cs`
- Test: `tests/AtividadesJuridicas.UnitTests/Application/AndamentoTests.cs` (novo); `tests/AtividadesJuridicas.IntegrationTests/AndamentoTests.cs` (novo)

**Interfaces:**
- Consumes: `UsuarioAutenticado.EhCliente`, `UsuarioAutenticado.ClienteId`, `IRequisicaoAbertaAoCliente`, `AtividadesOpcoes.TamanhoDaPagina`.
- Produces (namespace `AtividadesJuridicas.Application.Atividades`, em `PortasDeAtividades.cs`):
  - `enum SituacaoDoAndamento { EmAndamento = 1, Concluida = 2 }`
  - `static class Andamento { public static SituacaoDoAndamento SituacaoDe(StatusAtividade status); }` — `Concluida` só para `StatusAtividade.Concluida`
  - `record AtividadeDoAndamento(string Titulo, string? AreaNome, string? Processo, SituacaoDoAndamento Situacao, DateTime? ConcluidaEm)`
  - `record AndamentoDoCliente(Pagina<AtividadeDoAndamento> Atividades, int EmAndamento, int Concluidas)`
  - `IAtividadeRepositorio.ListarAndamentoAsync(Guid clienteId, SituacaoDoAndamento? situacao, int pagina, int tamanho, CancellationToken ct): Task<AndamentoDoCliente>`
- Produces (em `ListarMeuAndamento.cs`):
  - `record ListarMeuAndamento(SituacaoDoAndamento? Situacao = null, int Pagina = 1) : IRequisicao<Resultado<AndamentoDoCliente>>, IConsultaAuditavel, IRequisicaoAbertaAoCliente`

**Regras:**

1. **Quem.** O ator precisa ser `{ EhCliente: true, ClienteId: { } }`; qualquer outro, inclusive a Diretoria, recebe `AcessoNegado`. A consulta usa só o `ClienteId` do ator. `TipoRecurso = "Atividade"`, `Listou`, `Detalhe` com a situação e a página.
2. **Entrada.** `Situacao` informada e fora do enum → `Validacao` `"Situação inválida."`. Página normalizada com `Paginacao.Normalizar` e `AtividadesOpcoes.TamanhoDaPagina`.
3. **O que entra.** As atividades vivas cujo `ClienteId` é o do ator, em qualquer status, desde a criação. Atividade avulsa não tem cliente e não entra; atividade excluída sai pelo filtro de exclusão lógica.
4. **Cada linha** tem exatamente cinco campos: título; nome da área de atuação (nulo sem área); processo (o número CNJ formatado; na falta dele, o título do processo; nulo sem processo); situação; e a data de conclusão, só quando a situação é `Concluida`.
5. **Totais.** `EmAndamento` e `Concluidas` contam todas as atividades do cliente, qualquer que seja o filtro pedido.
6. **Ordem.** Em andamento primeiro, pela criação mais recente; depois as concluídas, da conclusão mais recente para a mais antiga. A consulta:

```csharp
    public async Task<AndamentoDoCliente> ListarAndamentoAsync(
        Guid clienteId, SituacaoDoAndamento? situacao, int pagina, int tamanho, CancellationToken ct)
    {
        // Só as atividades vivas do cliente: a avulsa não tem cliente, e a excluída sai pelo filtro de exclusão lógica.
        var doCliente = db.Atividades.AsNoTracking().Where(a => a.ClienteId == clienteId);

        // Os totais são do cliente, não do filtro.
        var concluidas = await doCliente.CountAsync(a => a.Status == StatusAtividade.Concluida, ct);
        var emAndamento = await doCliente.CountAsync(a => a.Status != StatusAtividade.Concluida, ct);

        var (consulta, total) = situacao switch
        {
            SituacaoDoAndamento.Concluida => (doCliente.Where(a => a.Status == StatusAtividade.Concluida), concluidas),
            SituacaoDoAndamento.EmAndamento => (doCliente.Where(a => a.Status != StatusAtividade.Concluida), emAndamento),
            _ => (doCliente, concluidas + emAndamento),
        };

        var linhas = await consulta
            .OrderBy(a => a.Status == StatusAtividade.Concluida)
            .ThenByDescending(a => a.Status == StatusAtividade.Concluida ? a.ConcluidaEm : (DateTime?)a.CriadoEm)
            .ThenByDescending(a => a.Id)
            .Skip((pagina - 1) * tamanho).Take(tamanho)
            .Select(a => new { a.Titulo, a.Status, a.ConcluidaEm, a.ProcessoId, a.AreaAtuacaoId })
            .ToListAsync(ct);

        // Área e processo excluídos logicamente continuam identificados, como nas outras listagens.
        // ... carregar por Id, com IgnoreQueryFilters, o nome das áreas e o número CNJ e o título dos processos;
        //     o número CNJ é objeto de valor: leia .Formatado depois de materializar ...

        // ... montar AtividadeDoAndamento com Andamento.SituacaoDe(status) e ConcluidaEm só quando concluída ...
    }
```

   Os dois trechos comentados seguem o padrão de `ListarAsync`, logo acima no mesmo arquivo.
7. **Portão.** `PortaoDoClienteTests`: a lista fixada ganha `"ListarMeuAndamento"` e fica com os sete nomes das restrições globais.
8. **Falso.** `AtividadeRepositorioFalso.ListarAndamentoAsync` com a mesma semântica; como o falso não tem relógio, a ordem de inserção faz o papel da criação.

**Testes obrigatórios:**

`AndamentoTests` (unitário):
- `Situacao_do_andamento_para_cada_status` — teoria com os sete status: só `Concluida` dá `Concluida`.
- `Cliente_ve_so_as_atividades_do_proprio_cliente_com_os_totais`
- `Andamento_e_so_para_o_perfil_cliente` — Diretoria, Produção e sem ator recebem `AcessoNegado`.
- `Filtro_por_situacao_nao_muda_os_totais`
- `Situacao_invalida_e_recusada`
- `Linha_do_andamento_tem_exatamente_os_cinco_campos` — por reflexão, as propriedades públicas de `AtividadeDoAndamento` são `AreaNome`, `ConcluidaEm`, `Processo`, `Situacao` e `Titulo`. O teste existe para que acrescentar um campo ao que o cliente lê seja uma decisão visível.
- `Consulta_e_auditavel_e_nao_tem_campo_de_cliente`

`AndamentoTests` (integração, `TesteComBanco`; Maria com a usuária Carla, José, Pedro de Produção; conclua atividades pelo fluxo real: direcionar, concluir a parte, aprovar):
- `Andamento_traz_so_as_atividades_vivas_do_cliente_com_os_totais` — da Maria: uma criada, uma direcionada, uma concluída, uma excluída; do José: uma; avulsa: uma. A Carla recebe três linhas, com totais 2 e 1.
- `Ordem_em_andamento_primeiro_e_concluidas_da_mais_recente_para_a_mais_antiga`
- `Atividade_reaberta_volta_a_em_andamento_sem_data_de_conclusao`
- `Atividade_excluida_ou_que_muda_de_cliente_sai_do_andamento`
- `Processo_aparece_pelo_numero_cnj_e_na_falta_dele_pelo_titulo` — e nulo na atividade sem processo.
- `Trocar_o_cliente_do_usuario_troca_o_andamento_que_ele_ve`
- `Cliente_sem_atividades_recebe_lista_vazia_e_totais_zerados`
- `Descricao_responsaveis_e_direcionamentos_nao_saem_na_resposta` — serialize a resposta em JSON e confira que o texto da descrição, o nome do Pedro e o direcionamento dado a ele não aparecem.
- `Consulta_fica_no_registro_de_consultas` — `Listou` no recurso `Atividade`, com o usuário da Carla.

- [ ] testes → falham → implementar → `dotnet test` inteiro → commit:

```bash
git add src tests
git commit -m "feat(atividades): andamento das atividades do cliente, com situação simplificada e totais"
```

---

### Task 4: Telas de documentos — ficha do cliente, Documentos comuns e Meus documentos

**Files:**
- Create: `src/AtividadesJuridicas.Web/Infra/EnvioComConfirmacao.cs`, `src/AtividadesJuridicas.Web/Infra/TamanhoDeArquivo.cs`
- Create: `src/AtividadesJuridicas.Web/Components/Documentos/DocumentosParaBaixar.razor`
- Create: `src/AtividadesJuridicas.Web/Components/Pages/DocumentosComuns.razor`, `MeusDocumentos.razor`
- Modify: `src/AtividadesJuridicas.Web/Components/Documentos/DocumentosDoCliente.razor`
- Modify: `src/AtividadesJuridicas.Web/Components/Pages/Clientes.razor`, `src/AtividadesJuridicas.Web/Components/Layout/MainLayout.razor`, `src/AtividadesJuridicas.Web/Infra/RotasDoCliente.cs`
- Test: `tests/AtividadesJuridicas.IntegrationTests/TelasPurasTests.cs`, `PortalDoClienteWebTests.cs`; `tests/AtividadesJuridicas.IntegrationTests/DocumentosDoPortalWebTests.cs` (novo)

**Interfaces:**
- Consumes (Tarefas 1 e 2): `CategoriaDocumento.DocumentoDoCliente`, `CategoriaDocumento.DocumentoComum`, `EnviarDocumentoComum`, `ListarMeusDocumentos`, `ListarDocumentosComuns`, `DocumentoDoPortal`, `DocumentosOpcoes.ConfirmacaoExigida`.
- Produces: `EnvioComConfirmacao`; `TamanhoDeArquivo.Formatar(long bytes): string`; `RotasDoCliente.Exclusiva(string caminhoRelativo): bool`; o componente `DocumentosParaBaixar` com os parâmetros `Titulo` (`string`), `Listar` (`Func<int, Task<Resultado<Pagina<DocumentoDoPortal>>>>`) e `OnAcessoEncerrado` (`EventCallback`).

**O que as telas fazem:**

1. **A confirmação, como classe pura** (`Web/Infra/EnvioComConfirmacao.cs`):

```csharp
/// <summary>
/// O envio em dois passos de uma área cujo conteúdo fica visível a todos os clientes: o primeiro clique só mostra o
/// aviso; o envio acontece na confirmação. O aviso vale para um envio só.
/// </summary>
public sealed class EnvioComConfirmacao(bool exigeConfirmacao)
{
    public bool AguardandoConfirmacao { get; private set; }

    /// <summary>Clique em "Enviar". Verdadeiro quando o envio segue já; falso quando o aviso passou a ser exibido.</summary>
    public bool Pedir()
    {
        if (!exigeConfirmacao) return true;

        AguardandoConfirmacao = true;
        return false;
    }

    /// <summary>Clique em "Confirmar envio". Verdadeiro só quando o aviso estava na tela.</summary>
    public bool Confirmar()
    {
        if (!AguardandoConfirmacao) return false;

        AguardandoConfirmacao = false;
        return true;
    }

    /// <summary>Clique em "Cancelar", ou troca do arquivo escolhido: o próximo envio avisa de novo.</summary>
    public void Cancelar() => AguardandoConfirmacao = false;
}
```

2. **`TamanhoDeArquivo.Formatar`** recebe a função `Tamanho` que hoje é privada em `DocumentosDoCliente.razor` (B, KB, MB, com a mesma formatação). O componente antigo e o novo passam a usá-la.

3. **`DocumentosDoCliente.razor`** passa a atender os Documentos comuns da Diretoria:
   - `ClienteId` vira `Guid?` e deixa de ser `EditorRequired`; só é nulo na categoria `DocumentoComum`.
   - A lista vem de `ListarDocumentosComuns` quando a categoria é `DocumentoComum`, e de `ListarDocumentosDoCliente` nas demais. Como as duas respostas têm tipos diferentes (`DocumentoDoPortal` e `DocumentoResumo`), o componente passa a guardar as linhas num registro privado próprio, com o que a tabela usa: Id, nome, tamanho, data de envio, autor e título da atividade.
   - O envio usa `EnviarDocumentoComum` na categoria `DocumentoComum` e `EnviarDocumento` nas demais.
   - Na categoria `DocumentoComum`, o botão "Enviar" chama `Pedir()`; se o retorno for falso, nada é enviado e aparece o aviso, com os textos exatos das restrições globais:

```razor
        @if (_confirmacao.AguardandoConfirmacao)
        {
            <div class="confirmacao" role="alertdialog" aria-label="Confirmar envio">
                <p>Documentos desta área ficam visíveis para todos os clientes do escritório. Não envie aqui arquivos de um cliente específico.</p>
                <div class="acoes">
                    <button type="button" disabled="@_enviando" @onclick="ConfirmarEnvioAsync">Confirmar envio</button>
                    <button type="button" class="secundario" disabled="@_enviando" @onclick="CancelarEnvio">Cancelar</button>
                </div>
            </div>
        }
```

     "Confirmar envio" chama `Confirmar()` e só envia se o retorno for verdadeiro, com `ConfirmouVisibilidade: true`. "Cancelar" chama `Cancelar()` e mantém o arquivo e o nome escolhidos. Escolher outro arquivo chama `Cancelar()`. Depois de um envio, o próximo avisa de novo.
   - O botão "Acesso" e o editor de acesso continuam só na categoria `DocumentoGeral`.
   - Nas outras categorias o comportamento do componente não muda.

4. **`Clientes.razor`.** No formulário de edição da Diretoria, uma quarta área depois de "Documentos de Atividades": `Categoria="CategoriaDocumento.DocumentoDoCliente"`, `Titulo="Documentos do cliente (visíveis ao cliente)"`, `PodeGerenciar="true"`. Não aparece na ficha de leitura da Produção.

5. **`DocumentosParaBaixar.razor`** (só leitura, para o cliente): título em `<h3>`; "Carregando…" até a primeira resposta; tabela com Nome (o mesmo link de download de `DocumentosDoCliente`, com `download` e `data-enhance-nav="false"`), Tamanho e "Enviado em"; `Nenhum documento.` quando vazia; a falha da consulta mostra o erro e o botão `Tentar de novo` no lugar da lista; paginação como a do componente existente; acesso negado chama `OnAcessoEncerrado`. Sem coluna de autor, sem envio, sem renomear e sem excluir.

6. **`MeusDocumentos.razor`** (`@page "/meus-documentos"`): `<h1>Meus documentos</h1>` e o componente acima com `ListarMeusDocumentos`. Com o acesso encerrado, a mensagem "Seu acesso foi encerrado. Entre novamente.", como nas outras páginas.

7. **`DocumentosComuns.razor`** (`@page "/documentos-comuns"`): `<h1>Documentos comuns</h1>`; "Carregando…" até `ObterMeuAcesso` responder; depois:
   - Diretoria: um parágrafo dizendo que os arquivos desta área ficam visíveis para todos os clientes, e `<DocumentosDoCliente Categoria="CategoriaDocumento.DocumentoComum" Titulo="Documentos comuns" PodeGerenciar="true" />`;
   - perfil Cliente: `DocumentosParaBaixar` com `ListarDocumentosComuns`;
   - qualquer outro (Produção): `<NaoEncontrada />`;
   - sem ator: a mensagem de acesso encerrado.

8. **`RotasDoCliente`** e a guarda inversa:

```csharp
public static class RotasDoCliente
{
    // A Tarefa 5 acrescenta "andamento" às duas listas.
    private static readonly HashSet<string> Abertas =
        new(StringComparer.OrdinalIgnoreCase) { "", "minha-senha", "nao-encontrada", "meus-documentos", "documentos-comuns" };

    // Só do portal: não são montadas para quem não é cliente. Documentos comuns fica de fora, porque a Diretoria usa.
    private static readonly HashSet<string> SoDoPortal =
        new(StringComparer.OrdinalIgnoreCase) { "meus-documentos" };

    /// <summary><paramref name="caminhoRelativo"/>: o endereço sem a base, como em <c>NavigationManager.ToBaseRelativePath</c>.</summary>
    public static bool Permite(string caminhoRelativo) => Abertas.Contains(Normalizar(caminhoRelativo));

    /// <summary>Verdadeiro para a página que só o cliente abre.</summary>
    public static bool Exclusiva(string caminhoRelativo) => SoDoPortal.Contains(Normalizar(caminhoRelativo));

    private static string Normalizar(string caminhoRelativo) => caminhoRelativo.Split('?', '#')[0].Trim('/');
}
```

   Em `MainLayout.razor`, a condição que hoje mostra `<NaoEncontrada />` para o cliente em rota interna passa a valer também no sentido inverso. Calcule a rota uma vez e use:

```razor
    else if (EhCliente ? !RotasDoCliente.Permite(rota) : RotasDoCliente.Exclusiva(rota))
    {
        @* A página não é montada: ela não chega a consultar nada. *@
        <NaoEncontrada />
    }
```

9. **Menu.** No bloco do cliente (hoje vazio, entre "Início" e "Minha senha"): `Meus documentos` e `Documentos comuns`, nessa ordem. No bloco da Diretoria: `Documentos comuns`, como primeiro item, antes de "Áreas". A Produção não ganha item.

**Testes obrigatórios:**

`TelasPurasTests`:
- `EnvioComConfirmacaoTests.Sem_exigencia_o_pedido_ja_envia`
- `EnvioComConfirmacaoTests.Com_exigencia_o_pedido_so_mostra_o_aviso_e_a_confirmacao_envia_uma_vez` — `Pedir()` falso e aviso na tela; `Confirmar()` verdadeiro e aviso fechado; um segundo `Confirmar()` é falso.
- `EnvioComConfirmacaoTests.Cancelar_fecha_o_aviso_e_o_proximo_pedido_avisa_de_novo`
- `EnvioComConfirmacaoTests.Confirmar_sem_aviso_na_tela_nao_envia`
- `TamanhoDeArquivoTests.Formata_bytes_quilobytes_e_megabytes`
- `RotasDoClienteTests`: as duas rotas novas entram em `Rotas_do_portal_sao_permitidas`; e `So_meus_documentos_e_exclusiva_do_portal` (`"meus-documentos"` verdadeiro; `""`, `"minha-senha"`, `"documentos-comuns"` e `"clientes"` falsos).

`DocumentosDoPortalWebTests` (novo, HTML pré-renderizado; `InitializeAsync` com `CriarCarlaAsync`, `CriarPedroAsync`, um documento do cliente da Maria e um documento comum, enviados pelos casos de uso):
- `Diretor_ve_documentos_comuns_com_o_envio_e_sem_o_aviso_aberto` — contém "Documentos comuns", o nome do documento, "Enviar documento", "Renomear" e "Excluir"; não contém "Confirmar envio" nem "Página não encontrada".
- `Cliente_ve_documentos_comuns_e_meus_documentos_so_com_download` — nas duas páginas: o nome do documento, o link `/documentos/{id}/baixar`; não contém "Enviar documento", "Renomear", "Excluir", `<th>Por</th>` nem "Diana Diretora".
- `Cliente_sem_documentos_ve_nenhum_documento` — um segundo cliente, sem documento do cliente: "Nenhum documento." em `/meus-documentos`.
- `Producao_em_documentos_comuns_ve_nao_encontrada` — contém "Página não encontrada"; não contém o nome do documento; nenhum `acesso_negado` novo.
- `Equipe_em_meus_documentos_ve_nao_encontrada_sem_consultar` — o admin e o Pedro: "Página não encontrada"; a quantidade de `acesso_negado` e de registros de consulta não muda.
- `Menu_da_diretoria_tem_documentos_comuns_e_o_da_producao_nao`

`PortalDoClienteWebTests.Cliente_ve_o_inicio_com_o_menu_do_portal`: a lista exata dos `href` do menu passa a `/`, `/meus-documentos`, `/documentos-comuns` e `/minha-senha`.

O comportamento interativo não tem teste automatizado (o componente com `InputFile` não é desenhado fora da página). Descreva no relatório, caminho por caminho, como verificou por leitura:
- "Enviar" em Documentos comuns não envia e abre o aviso; "Confirmar envio" envia uma vez; "Cancelar" fecha o aviso e mantém o arquivo e o nome; trocar o arquivo com o aviso aberto fecha o aviso;
- depois de um envio confirmado, o envio seguinte avisa de novo;
- nas áreas da ficha do cliente (inclusive a nova), "Enviar" envia direto, sem aviso;
- a quarta área da ficha não tem o botão "Acesso" e não aparece para a Produção;
- renomear e excluir funcionam nos Documentos comuns.

- [ ] testes → falham → implementar → `dotnet test` inteiro → commit:

```bash
git add src tests
git commit -m "feat(web): documentos do cliente na ficha, Documentos comuns com confirmação e Meus documentos no portal"
```

---

### Task 5: Andamento na tela, início do cliente e acabamento

**Files:**
- Create: `src/AtividadesJuridicas.Web/Components/Pages/Andamento.razor`
- Create: `src/AtividadesJuridicas.Web/Components/Portal/TotaisDoAndamento.razor`
- Create: `src/AtividadesJuridicas.Web/Infra/RotulosDoAndamento.cs`, `src/AtividadesJuridicas.Web/Infra/TextosDoCliente.cs`
- Modify: `src/AtividadesJuridicas.Web/Components/Pages/Home.razor`, `Clientes.razor`, `Usuarios.razor`
- Modify: `src/AtividadesJuridicas.Web/Components/Atividades/FormularioDeAtividade.razor`
- Modify: `src/AtividadesJuridicas.Web/Components/Layout/MainLayout.razor`, `src/AtividadesJuridicas.Web/Infra/RotasDoCliente.cs`, `src/AtividadesJuridicas.Web/Components/_Imports.razor`
- Create: `tests/AtividadesJuridicas.UnitTests/WebSoPeloDespachanteTests.cs`
- Test: `tests/AtividadesJuridicas.IntegrationTests/TelasPurasTests.cs`, `PortalDoClienteWebTests.cs`, `AtividadesWebTests.cs`

**Interfaces:**
- Consumes (Tarefa 3): `ListarMeuAndamento`, `AndamentoDoCliente`, `AtividadeDoAndamento`, `SituacaoDoAndamento`. Da 8A: `ListarUsuariosDoCliente`, `UsuarioDoCliente`.
- Produces: `RotulosDoAndamento.Da(SituacaoDoAndamento): string`; `TextosDoCliente.Inativado(int usuariosAtivosDoPortal): string`; o componente `TotaisDoAndamento` com os parâmetros `EmAndamento` e `Concluidas` (`int`).

**O que as telas fazem:**

1. **Funções puras.** `RotulosDoAndamento.Da`: `Em andamento` e `Concluída`. `TextosDoCliente.Inativado`: zero → `Cliente inativado.`; um → `Cliente inativado. 1 usuário do portal ficou sem acesso.`; mais de um → `Cliente inativado. 3 usuários do portal ficaram sem acesso.` (com o número).

2. **`TotaisDoAndamento.razor`**, usado no início e na página de andamento, com marcação fixa para os testes:

```razor
<dl class="totais">
    <dt>Em andamento</dt><dd>@EmAndamento</dd>
    <dt>Concluídas</dt><dd>@Concluidas</dd>
</dl>
```

3. **`Andamento.razor`** (`@page "/andamento"`): `<h1>Andamento</h1>`; "Carregando…" até a primeira resposta; os totais; um seletor de situação (`Todas`, `Em andamento`, `Concluídas`) que refaz a consulta na primeira página; tabela com Título, Área, Processo, Situação e "Concluída em" (data no fuso do escritório; vazia quando em andamento); `Nenhuma atividade.` quando a lista vem vazia; a falha da consulta mostra o erro e `Tentar de novo` no lugar da lista; paginação como as outras listas; acesso encerrado mostra a mensagem das outras páginas. Cada célula de texto é uma única expressão Razor. Como a linha não tem Id, use o índice como `@key`.

4. **`Home.razor`**, para o cliente: a saudação, os totais (uma consulta `ListarMeuAndamento`, só os totais são usados) e os atalhos `Andamento`, `Meus documentos`, `Documentos comuns` e `Minha senha`. Se a consulta do andamento falhar, os atalhos aparecem e, no lugar dos totais, a mensagem do erro. Para os demais perfis nada muda.

5. **Menu e rotas.** No bloco do cliente do `MainLayout`, `Andamento` entra antes de `Meus documentos`. Em `RotasDoCliente`, `"andamento"` entra em `Abertas` e em `SoDoPortal`; o comentário "A Tarefa 5 acrescenta…" sai.

6. **`FormularioDeAtividade.razor`.** Quando há cliente escolhido (`_clienteId != Guid.Empty`), logo abaixo do campo do título: `<p class="aviso">O título aparece para o cliente no portal.</p>`. Sem cliente, o lembrete não aparece.

7. **`Clientes.razor`, aviso de inativação.** Depois de uma inativação bem-sucedida, a tela consulta `ListarUsuariosDoCliente` do cliente, conta os usuários ativos e mostra `TextosDoCliente.Inativado(n)`. Se essa consulta falhar, mostra `Cliente inativado.`. A ativação continua com `Cliente ativado.`.

8. **`Usuarios.razor`, busca de cliente.** `BuscarClientesAsync` ganha a guarda contra reentrância que as outras ações da tela já têm (`_emAndamento`): dois Enter seguidos não disparam duas consultas.

9. **Teste de arquitetura** (`tests/AtividadesJuridicas.UnitTests/WebSoPeloDespachanteTests.cs`), no modelo de `GravacoesPorForaTests`: percorre os `.cs` e `.razor` de `src/AtividadesJuridicas.Web` (fora `bin` e `obj`) e falha se algum arquivo, além dos permitidos, citar repositório, contexto do banco ou manipulador:

```csharp
    // Arquivo → motivo pelo qual chegar aos dados sem passar pelo executor é aceito.
    private static readonly Dictionary<string, string> Permitidos = new()
    {
        ["ValidacaoDeSessao.cs"] = "valida o cookie a cada requisição HTTP: lê só o usuário e o cliente dele, não executa caso de uso",
    };

    [GeneratedRegex(@"\b(I\w+Repositorio|AppDbContext|IUnidadeDeTrabalho|IClientesDaIdentidade|IManipulador\s*<)")]
    private static partial Regex AcessoPorFora();
```

   A mensagem da falha explica: o portão do cliente só protege o que passa pelo `ExecutorDeRequisicoes`; uma tela que lê por outro caminho fica fora dele. A busca da raiz do repositório já existe em `GravacoesPorForaTests` (`RaizDoRepositorio`): torne-a acessível às duas classes em vez de copiá-la.

**Testes obrigatórios:**

`TelasPurasTests`: `RotulosDoAndamentoTests.Cada_situacao_tem_o_seu_rotulo`; `TextosDoClienteTests.Aviso_de_inativacao_conta_os_usuarios_do_portal` (zero, um e três); `RotasDoClienteTests` com `"andamento"` permitida e exclusiva.

`PortalDoClienteWebTests` (HTML pré-renderizado; acrescente ao cenário, pelos casos de uso, um processo com número CNJ e três atividades da Maria: uma em andamento com processo, uma em andamento sem processo e uma concluída):
- `Cliente_ve_o_andamento_com_os_totais_e_a_situacao` — os três títulos, o nome da área, o número do processo, `<td>Em andamento</td>`, `<td>Concluída</td>`, `<dt>Em andamento</dt><dd>2</dd>` e `<dt>Concluídas</dt><dd>1</dd>`; não contém a descrição das atividades, o nome do Pedro nem o direcionamento dele.
- `Cliente_sem_atividades_ve_zeros_e_nenhuma_atividade` — um segundo cliente com usuário: `<dd>0</dd>` duas vezes e "Nenhuma atividade.".
- `Inicio_do_cliente_mostra_os_totais_e_os_atalhos` — os dois totais e os `href` `/andamento`, `/meus-documentos`, `/documentos-comuns` e `/minha-senha` no corpo da página.
- `Equipe_em_andamento_ve_nao_encontrada_sem_consultar` — o admin e o Pedro; nenhum `acesso_negado` nem registro de consulta novo.
- `Cliente_ve_o_inicio_com_o_menu_do_portal`: a lista exata passa a `/`, `/andamento`, `/meus-documentos`, `/documentos-comuns` e `/minha-senha`.
- `Cliente_em_pagina_interna_ve_nao_encontrada_e_nenhum_dado` continua passando sem alteração.

`AtividadesWebTests` (com `TelaDeTeste`): `Formulario_de_atividade_lembra_que_o_titulo_aparece_para_o_cliente` — desenhado com uma atividade que tem cliente, contém a frase exata; desenhado para atividade nova, não contém.

`WebSoPeloDespachanteTests.A_web_so_chega_aos_dados_pelo_despachante` — e prove que ele discrimina: injete temporariamente um repositório numa página, veja falhar e desfaça.

Por leitura, descreva no relatório: trocar a situação refaz a consulta na primeira página e mantém os totais; a paginação do andamento; a mensagem de inativação com zero, um e vários usuários, e com a consulta de usuários falhando; a guarda da busca de cliente.

- [ ] testes → falham → implementar → `dotnet test` inteiro → commit:

```bash
git add src tests
git commit -m "feat(web): andamento e início do portal, lembrete do título, aviso de inativação com os usuários do portal"
```

---

### Task 6: Emendar a modelagem de domínio e fechar a spec do portal

**Files:**
- Modify: `docs/superpowers/specs/2026-10-06-modelagem-dominio-design.md`
- Modify: `docs/superpowers/specs/2026-10-08-portal-do-cliente-design.md`

Tarefa só de documentação. O texto segue o estilo de cada documento: frases curtas, regras em lista, tabelas, sem código.

- [ ] **Step 1: Modelagem, seção 5 (Identidade)**
  - Requisições abertas ao cliente: a lista passa a ter as sete das restrições globais deste plano.
  - Matriz de acesso: duas linhas novas, "Documentos do cliente" (Diretoria: tudo; Produção: sem acesso; Cliente: leitura dos do próprio cliente) e "Documentos comuns" (Diretoria: tudo; Produção: sem acesso; Cliente: leitura); e, na linha "Atividades", a coluna Cliente passa a "Andamento das do próprio cliente".
  - O que o cliente abre: início com os totais e os atalhos, Andamento, Meus documentos, Documentos comuns e a troca de senha. A página só do portal não é montada para a equipe.

- [ ] **Step 2: Modelagem, seções 6, 7 e 8**
  - Seção 6: a ficha do cliente tem a quarta área, "Documentos do cliente (visíveis ao cliente)", só para a Diretoria; inativar o cliente avisa quantos usuários do portal ficam sem acesso.
  - Seção 7: as duas categorias na tabela do `Documento` e na de comportamento por categoria (cliente obrigatório no Documento do cliente e ausente no Documento comum; sem lista de acesso; só a Diretoria envia, renomeia e exclui); a regra de acesso reescrita na ordem do código; o envio de Documento comum com a confirmação exigida na tela e no caso de uso; as consultas do portal (o que o cliente recebe de cada documento, sem autor); as duas categorias nunca aparecem nas listagens da Produção.
  - Seção 8: subseção "Andamento" (o que entra, os cinco campos, as duas situações, os totais, a ordem, e a lista do que não chega ao cliente); o lembrete do formulário de atividade.

- [ ] **Step 3: Modelagem, seções 10, 12 e 13, e os três trechos apontados na revisão da 8A**
  - Seção 10: as consultas do portal registram `Listou` (recursos `Documento` e `Atividade`); o download do cliente registra `Baixou`; o download negado gera `acesso_negado` com o documento.
  - Seção 12: a lista fixada das sete requisições abertas; o teste de arquitetura da camada Web; a matriz da regra de acesso por tipo de perfil e categoria.
  - Seção 13: a frase que remetia os documentos e o andamento à fase 8B sai; "envio de arquivos pelo cliente" e os demais itens da seção 12 da spec do portal entram como fora do escopo.
  - "Bloqueio de login": uma frase remetendo à regra do usuário de cliente inativo, cuja tentativa conta mesmo com a senha certa.
  - "Reativar o cliente devolve o acesso": passa a dizer que o acesso volta no próximo login.
  - O termo "sem ator" é trocado por texto corrente ("nega o acesso, como a quem não está autenticado").

- [ ] **Step 4: Spec do portal**
  - Seção 4: o cliente recebe Id, nome, tamanho e data de envio; `ListarDocumentosDoCliente` com `DocumentoComum` responde validação.
  - Seção 5: a linha do andamento não leva identificador.
  - Seção 6: `/documentos-comuns` mostra "não encontrada" à Produção pela própria página; `/andamento` e `/meus-documentos` não são montadas para a equipe.
  - Seção 11: a lista "Registrado na revisão da 8A, para o plano da 8B" vira o registro do que foi feito: cada item com "feito na 8B" ou com a decisão tomada (status 200 mantido para o cliente em página interna; custo do contexto não medido, porque o início faz uma consulta só).

- [ ] **Step 5: Reler e conferir**

Releia cada seção alterada contra o código: cada regra escrita tem o trecho que a implementa, e nenhuma frase antiga ficou contradizendo a nova. Procure nos dois documentos por "três", "Sem acesso" na coluna Cliente, "fase 8B", "8B" e "não é desta fase", e trate cada ocorrência.

- [ ] **Step 6: Commit**

```bash
git add docs/superpowers/specs
git commit -m "docs(spec): regras dos documentos do portal e do andamento fixadas na fase 8B"
```
