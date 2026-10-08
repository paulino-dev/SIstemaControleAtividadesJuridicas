# Fase 4A — Documentos: Plano de Implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Guardar os documentos de cada cliente ("Documentos Gerais" e "Contratos de Honorários") com o arquivo fora do banco, lista de acesso por perfil e por usuário, download controlado e trilha de auditoria.

**Architecture:** `Documento` é um agregado do contexto Documentos com duas coleções de entidades filhas (`DocumentoPerfilAcesso`, `DocumentoUsuarioAcesso`), que derivam de `Entidade` para terem carimbos, exclusão lógica e auditoria próprios. Uma única função de domínio decide quem acessa. O arquivo fica atrás da porta `IArmazenamentoDeArquivos` (primeira implementação: disco); o banco guarda metadados e o hash SHA-256. O download é um endpoint HTTP autenticado que executa o caso de uso pelo mesmo `ExecutorDeRequisicoes` das páginas.

**Tech Stack:** .NET 10, Blazor Interactive Server, EF Core 10 + Npgsql, PostgreSQL, xUnit 2.9, FluentAssertions 7.2, Testcontainers.

**Spec:** `docs/superpowers/specs/2026-10-06-modelagem-dominio-design.md` (seções 4, 5 — matriz de acesso —, 7, 10, 11 e 12).

## Decisões deste plano

- **Fase 4 em duas partes.** 4A (este plano): documentos de cliente. 4B (plano seguinte): gestão de perfis, incluindo a regra "perfil criado depois entra em todos os Documentos Gerais".
- **Documentos de atividade ficam para a fase 5.** O enum `CategoriaDocumento` já tem os três valores, mas `AtividadeId`, a fábrica de documento de atividade e a área "Documentos de Atividades" da ficha do cliente só entram com o agregado `Atividade`.
- **Contrato de Honorários é barrado duas vezes.** O domínio recusa incluir perfil ou usuário de Produção na lista de um contrato, e a função de acesso nega o contrato a qualquer usuário de Produção mesmo que ele conste na lista (cobre quem era da Diretoria quando foi incluído e depois mudou de perfil).
- **Perfil ou usuário inativo** não entra em lista de acesso nova, mas permanece na lista em que já estava.
- **Negado responde como não encontrado.** Baixar um documento sem ter acesso devolve `NaoEncontrado` (spec, seção 11) e grava o evento de segurança `acesso_negado` com o documento.
- **Mudança na lista de acesso** gera o evento de segurança `documento_acesso_alterado` no documento, na mesma transação.
- **Exclusão é lógica e o arquivo permanece no armazenamento.** Nada é apagado do disco ao excluir um documento.
- **Arquivo primeiro, banco depois.** O envio grava o arquivo, depois o registro; se o registro falhar, o arquivo é removido.
- **Limite de 50 MB por arquivo.** Arquivo vazio é recusado.
- **Download sempre como anexo**, com tipo `application/octet-stream` e `X-Content-Type-Options: nosniff`: o navegador nunca interpreta o conteúdo enviado.
- **A chave de armazenamento é gerada pelo servidor** (`aaaa/mm/<guid>`); o nome do arquivo enviado pelo usuário é só um metadado e nunca vira caminho.

## Como ler este plano

Os **testes** de cada tarefa definem o comportamento exigido. O código de implementação é o desenho pretendido, escrito sem ter sido executado: quando um detalhe do EF Core 10, do Npgsql ou do Blazor exigir, ajuste o mínimo necessário para preservar o comportamento e registre o desvio no relatório, com a mensagem de erro que o motivou. Nunca apague um teste nem afrouxe uma asserção. Ajustar como um teste obtém suas entradas é permitido. O texto de `Dados` gravado em `jsonb` volta reformatado pelo PostgreSQL: compare JSON pela estrutura (`JsonNode.DeepEquals` ou `JsonDocument`), nunca por texto exato.

## Estado de partida (fase 3, na `main`)

- Domínio: `Usuario`, `Perfil`; `AreaAtuacao`, `Cliente` (PF/PJ), `Processo` com `ProcessoCliente` (entidade filha: o padrão a seguir para as listas de acesso).
- Aplicação: `IContextoDeAcesso.ObterAsync` devolve `UsuarioAutenticado(Id, Nome, TipoPerfil)`; `ExecutorDeRequisicoes` grava `acesso_negado` quando o resultado é `AcessoNegado` e `RegistroAcesso` quando a requisição é `IConsultaAuditavel` e deu certo; `IAuditoria` (`Anotar`, `RegistrarAsync`); `Pagina<T>`, `Paginacao.Normalizar`; `DetalheDeConsulta.Serializar`.
- Infraestrutura: interceptores de carimbos (exclusão vira exclusão lógica) e de auditoria; `UnidadeDeTrabalho` traduz violação de unicidade em `ExcecaoDeConflito`; `AddInfrastructure(servicos, obterStringDeConexao)`.
- Web: `IdentidadeHttp.Definir(HttpContext)` para endpoints fora do Blazor; política padrão exige usuário autenticado em toda rota.
- Guardas: `PropriedadesAuditadasTests`, `GravacoesPorForaTests`, `ArquiteturaTests`, `RegistroDeManipuladoresTests`.
- Testes: 269 unitários e 162 de integração passando.

## Global Constraints

- TFM único `net10.0` em `Directory.Build.props`. `Nullable` e `TreatWarningsAsErrors=true`. Versões de pacote só em `Directory.Packages.props`. Nenhum pacote novo.
- `Domain` não referencia nada. `Application` não referencia EF Core, Npgsql, `Infrastructure` nem `Web`.
- Código, nomes de tabela e mensagens em português. Tabelas e colunas em `snake_case`. Datas em UTC.
- Exclusão sempre lógica, inclusive das entradas das listas de acesso. Agregados se referenciam só por Id.
- Toda alteração de `Entidade` gera registro de auditoria na mesma transação. Toda propriedade nova é classificada em `PropriedadesAuditadasTests`. Todo tipo mapeado deriva de `Entidade`. Nenhuma gravação por fora dos interceptores.
- Matriz de acesso: Documentos Gerais — Diretoria tudo, Produção leitura conforme lista de acesso; Contratos de Honorários — Diretoria tudo, Produção sem acesso, sem exceção.
- Autorização verificada no manipulador; a interface apenas oculta. Enquanto a consulta de acesso não termina, a página mostra "Carregando…".
- Um recurso que o usuário não pode ver responde `NaoEncontrado`.
- Consultas são `IConsultaAuditavel`: listagem registra `Listou`; ver a lista de acesso registra `Visualizou`; baixar registra `Baixou`, com o `RecursoId`. O `Detalhe` usa `DetalheDeConsulta.Serializar`.
- `NomeArquivoOriginal`, `TipoConteudo`, `TamanhoBytes`, `HashSha256`, `ChaveArmazenamento`, `Categoria` e `ClienteId` são imutáveis depois da criação.
- Um caso de uso por arquivo. Novas dependências de construtor entram por último.

## Review Focus

1. **Nome de arquivo com caminho** (`C:\pasta\contrato.pdf`, `../../etc/passwd`): só o último segmento vira `NomeArquivoOriginal`, e ele nunca é usado como caminho. (Tarefas 2 e 3)
2. **Usuário que estava na lista de um contrato e passou para Produção**: não acessa o contrato. (Tarefa 1)
3. **O banco falha depois de o arquivo ter sido gravado**: o arquivo não fica órfão no armazenamento. (Tarefa 3)
4. **Arquivo vazio ou acima do limite**: recusado com mensagem, sem deixar arquivo nem registro. (Tarefas 2 e 3)
5. **Nome com acentos e espaços no download** (`Procuração João.pdf`): o cabeçalho `Content-Disposition` entrega o nome correto. (Tarefa 4)
6. **Perfil retirado da lista e incluído de novo**: funciona; a entrada antiga continua no banco, excluída logicamente. (Tarefa 3)

---

### Task 1: `Documento` no domínio

**Files:**
- Create: `src/AtividadesJuridicas.Domain/Documentos/Documento.cs`
- Create: `src/AtividadesJuridicas.Domain/Documentos/DocumentoAcesso.cs`
- Test: `tests/AtividadesJuridicas.UnitTests/Domain/DocumentoTests.cs`

**Interfaces:**
- Consumes: `Entidade`, `ExcecaoDeDominio`, `Perfil` (`Id`, `Tipo`, `Ativo`), `Usuario` (`Id`, `PerfilId`, `Ativo`), `TipoPerfil`.
- Produces (namespace `AtividadesJuridicas.Domain.Documentos`):
  - `enum CategoriaDocumento { DocumentoGeral = 1, ContratoHonorarios = 2, DocumentoAtividade = 3 }`
  - `record ArquivoDoDocumento(string NomeOriginal, string TipoConteudo, long TamanhoBytes, string HashSha256, string ChaveArmazenamento)`
  - `record UsuarioComPerfil(Usuario Usuario, Perfil Perfil)`
  - `record AlteracaoDeAcesso(IReadOnlyList<Guid> PerfisAdicionados, IReadOnlyList<Guid> PerfisRemovidos, IReadOnlyList<Guid> UsuariosAdicionados, IReadOnlyList<Guid> UsuariosRemovidos)` com `bool Houve`
  - `Documento.CriarGeral(Guid clienteId, string? nome, ArquivoDoDocumento arquivo, IReadOnlyCollection<Perfil> perfisAtivos)`
  - `Documento.CriarContratoDeHonorarios(Guid clienteId, string? nome, ArquivoDoDocumento arquivo)`
  - `documento.Renomear(string nome)`, `documento.NomeParaBaixar()`, `documento.PodeSerAcessadoPor(Guid usuarioId, Guid perfilId, TipoPerfil tipoPerfil)`, `documento.DefinirAcesso(IReadOnlyCollection<Perfil> perfis, IReadOnlyCollection<UsuarioComPerfil> usuarios)`
  - `Documento.PerfisComAcesso` (`IReadOnlyCollection<DocumentoPerfilAcesso>`, campo `_perfisComAcesso`), `Documento.UsuariosComAcesso` (`IReadOnlyCollection<DocumentoUsuarioAcesso>`, campo `_usuariosComAcesso`)
  - `DocumentoPerfilAcesso { DocumentoId, PerfilId }`, `DocumentoUsuarioAcesso { DocumentoId, UsuarioId }`, ambos `: Entidade`

- [ ] **Step 1: Escrever os testes**

`tests/AtividadesJuridicas.UnitTests/Domain/DocumentoTests.cs`:

```csharp
using AtividadesJuridicas.Domain.Comum;
using AtividadesJuridicas.Domain.Documentos;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.UnitTests.Domain;

public class DocumentoTests
{
    private const string Hash = "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad";
    private const string Chave = "2026/10/0123456789abcdef0123456789abcdef";

    private static readonly Guid Cliente = Guid.CreateVersion7();

    private readonly Perfil _diretor = Perfil.Criar("Diretor", TipoPerfil.Diretoria);
    private readonly Perfil _producao = Perfil.Criar("Produção", TipoPerfil.Producao);
    private readonly Perfil _estagio = Perfil.Criar("Estagiário", TipoPerfil.Producao);

    private static ArquivoDoDocumento Arquivo(string nome = "procuracao.pdf", long tamanho = 1234, string hash = Hash) =>
        new(nome, "application/pdf", tamanho, hash, Chave);

    private static Usuario NovoUsuario(string nome, Perfil perfil) =>
        Usuario.Criar(nome, Email.Criar($"{Guid.NewGuid():N}@x.com"), "hash", perfil);

    private Documento Geral(params Perfil[] perfis) => Documento.CriarGeral(Cliente, null, Arquivo(), perfis);

    private static Documento Contrato() => Documento.CriarContratoDeHonorarios(Cliente, null, Arquivo("contrato.pdf"));

    // ---- Criação ----

    [Fact]
    public void Documento_geral_nasce_com_os_perfis_informados_e_o_nome_do_arquivo()
    {
        var documento = Geral(_diretor, _producao);

        documento.Categoria.Should().Be(CategoriaDocumento.DocumentoGeral);
        documento.ClienteId.Should().Be(Cliente);
        documento.Nome.Should().Be("procuracao.pdf");
        documento.NomeArquivoOriginal.Should().Be("procuracao.pdf");
        documento.TipoConteudo.Should().Be("application/pdf");
        documento.TamanhoBytes.Should().Be(1234);
        documento.HashSha256.Should().Be(Hash);
        documento.ChaveArmazenamento.Should().Be(Chave);
        documento.PerfisComAcesso.Select(a => a.PerfilId).Should().BeEquivalentTo([_diretor.Id, _producao.Id]);
        documento.PerfisComAcesso.Should().OnlyContain(a => a.DocumentoId == documento.Id);
        documento.UsuariosComAcesso.Should().BeEmpty();
    }

    [Fact]
    public void Nome_informado_prevalece_sobre_o_do_arquivo_e_e_aparado()
    {
        var documento = Documento.CriarGeral(Cliente, "  Procuração assinada  ", Arquivo(" scan001.pdf "), [_diretor]);

        documento.Nome.Should().Be("Procuração assinada");
        documento.NomeArquivoOriginal.Should().Be("scan001.pdf");
    }

    [Fact]
    public void Perfil_repetido_na_criacao_entra_uma_vez()
    {
        Geral(_producao, _producao).PerfisComAcesso.Should().ContainSingle();
    }

    [Fact]
    public void Contrato_de_honorarios_nasce_com_a_lista_de_acesso_vazia()
    {
        var contrato = Contrato();

        contrato.Categoria.Should().Be(CategoriaDocumento.ContratoHonorarios);
        contrato.PerfisComAcesso.Should().BeEmpty();
        contrato.UsuariosComAcesso.Should().BeEmpty();
    }

    [Fact]
    public void Cliente_e_obrigatorio()
    {
        var acao = () => Documento.CriarGeral(Guid.Empty, null, Arquivo(), [_diretor]);

        acao.Should().Throw<ExcecaoDeDominio>().WithMessage("O cliente do documento é obrigatório.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Arquivo_vazio_e_recusado(long tamanho)
    {
        var acao = () => Documento.CriarGeral(Cliente, null, Arquivo(tamanho: tamanho), [_diretor]);

        acao.Should().Throw<ExcecaoDeDominio>().WithMessage("O arquivo está vazio.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Nome_do_arquivo_e_obrigatorio(string nome)
    {
        var acao = () => Documento.CriarGeral(Cliente, null, Arquivo(nome), [_diretor]);

        acao.Should().Throw<ExcecaoDeDominio>().WithMessage("O nome do arquivo é obrigatório.");
    }

    [Fact]
    public void Nome_do_arquivo_tem_limite()
    {
        var acao = () => Documento.CriarGeral(Cliente, null, Arquivo(new string('a', 256)), [_diretor]);

        acao.Should().Throw<ExcecaoDeDominio>().WithMessage("O nome do arquivo deve ter até 255 caracteres.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD")]
    [InlineData("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015a")]
    [InlineData("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015az")]
    public void Hash_que_nao_e_sha256_em_hexadecimal_minusculo_e_erro_de_programacao(string hash)
    {
        var acao = () => Documento.CriarGeral(Cliente, null, Arquivo(hash: hash), [_diretor]);

        acao.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Perfil_inativo_nao_entra_na_lista_inicial()
    {
        _producao.Inativar();

        var acao = () => Geral(_diretor, _producao);

        acao.Should().Throw<ExcecaoDeDominio>().WithMessage("Perfil inativo não pode receber acesso a documento.");
    }

    // ---- Nome ----

    [Fact]
    public void Renomear_troca_so_o_nome()
    {
        var documento = Geral(_diretor);

        documento.Renomear("  Procuração 2026  ");

        documento.Nome.Should().Be("Procuração 2026");
        documento.NomeArquivoOriginal.Should().Be("procuracao.pdf");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Renomear_exige_nome(string nome)
    {
        var documento = Geral(_diretor);

        var acao = () => documento.Renomear(nome);

        acao.Should().Throw<ExcecaoDeDominio>().WithMessage("O nome do documento é obrigatório.");
    }

    [Fact]
    public void Renomear_tem_limite()
    {
        var documento = Geral(_diretor);

        var acao = () => documento.Renomear(new string('a', 256));

        acao.Should().Throw<ExcecaoDeDominio>().WithMessage("O nome do documento deve ter até 255 caracteres.");
    }

    [Theory]
    [InlineData("Procuração assinada", "Procuração assinada.pdf")]
    [InlineData("Procuração.PDF", "Procuração.PDF")]
    [InlineData("procuracao.pdf", "procuracao.pdf")]
    public void Nome_para_baixar_leva_a_extensao_do_arquivo_original(string nome, string esperado)
    {
        var documento = Geral(_diretor);
        documento.Renomear(nome);

        documento.NomeParaBaixar().Should().Be(esperado);
    }

    [Fact]
    public void Nome_para_baixar_sem_extensao_no_original_fica_como_esta()
    {
        var documento = Documento.CriarGeral(Cliente, "Anotações", Arquivo("LEIAME"), [_diretor]);

        documento.NomeParaBaixar().Should().Be("Anotações");
    }

    // ---- Regra de acesso ----

    [Fact]
    public void Diretoria_acessa_sempre()
    {
        var foraDaLista = NovoUsuario("Diana", _diretor);

        Geral().PodeSerAcessadoPor(foraDaLista.Id, _diretor.Id, TipoPerfil.Diretoria).Should().BeTrue();
        Contrato().PodeSerAcessadoPor(foraDaLista.Id, _diretor.Id, TipoPerfil.Diretoria).Should().BeTrue();
    }

    [Fact]
    public void Producao_acessa_pelo_perfil_ou_pelo_proprio_usuario()
    {
        var pedro = NovoUsuario("Pedro", _producao);
        var ester = NovoUsuario("Ester", _estagio);
        var documento = Geral(_producao);
        documento.DefinirAcesso([_producao], [new UsuarioComPerfil(ester, _estagio)]);

        documento.PodeSerAcessadoPor(pedro.Id, _producao.Id, TipoPerfil.Producao).Should().BeTrue("o perfil dele está na lista");
        documento.PodeSerAcessadoPor(ester.Id, _estagio.Id, TipoPerfil.Producao).Should().BeTrue("ela está na lista");
    }

    [Fact]
    public void Producao_fora_das_listas_nao_acessa()
    {
        var ester = NovoUsuario("Ester", _estagio);

        Geral(_producao).PodeSerAcessadoPor(ester.Id, _estagio.Id, TipoPerfil.Producao).Should().BeFalse();
    }

    [Fact]
    public void Contrato_nunca_e_acessado_por_producao_mesmo_que_o_usuario_conste_na_lista()
    {
        // Incluído quando era da Diretoria; depois passou para um perfil de Produção.
        var rebaixado = NovoUsuario("Rui", _diretor);
        var contrato = Contrato();
        contrato.DefinirAcesso([_diretor], [new UsuarioComPerfil(rebaixado, _diretor)]);

        contrato.PodeSerAcessadoPor(rebaixado.Id, _producao.Id, TipoPerfil.Producao).Should().BeFalse();
    }

    // ---- Lista de acesso ----

    [Fact]
    public void Definir_acesso_deixa_exatamente_o_que_foi_informado_e_conta_o_que_mudou()
    {
        var pedro = NovoUsuario("Pedro", _producao);
        var ester = NovoUsuario("Ester", _estagio);
        var documento = Geral(_diretor, _producao);
        documento.DefinirAcesso([_diretor, _producao], [new UsuarioComPerfil(pedro, _producao)]);
        var entradaDoDiretor = documento.PerfisComAcesso.Single(a => a.PerfilId == _diretor.Id);

        var alteracao = documento.DefinirAcesso([_diretor, _estagio], [new UsuarioComPerfil(ester, _estagio)]);

        alteracao.Houve.Should().BeTrue();
        alteracao.PerfisAdicionados.Should().Equal(_estagio.Id);
        alteracao.PerfisRemovidos.Should().Equal(_producao.Id);
        alteracao.UsuariosAdicionados.Should().Equal(ester.Id);
        alteracao.UsuariosRemovidos.Should().Equal(pedro.Id);
        documento.PerfisComAcesso.Select(a => a.PerfilId).Should().BeEquivalentTo([_diretor.Id, _estagio.Id]);
        documento.UsuariosComAcesso.Select(a => a.UsuarioId).Should().Equal(ester.Id);
        documento.PerfisComAcesso.Single(a => a.PerfilId == _diretor.Id).Should().BeSameAs(entradaDoDiretor,
            "quem permanece mantém a mesma entrada");
    }

    [Fact]
    public void Definir_o_mesmo_acesso_nao_muda_nada()
    {
        var documento = Geral(_diretor, _producao);

        var alteracao = documento.DefinirAcesso([_producao, _diretor, _diretor], []);

        alteracao.Houve.Should().BeFalse();
        documento.PerfisComAcesso.Should().HaveCount(2);
    }

    [Fact]
    public void Contrato_recusa_perfil_de_producao_e_nada_muda()
    {
        var contrato = Contrato();

        var acao = () => contrato.DefinirAcesso([_diretor, _producao], []);

        acao.Should().Throw<ExcecaoDeDominio>().WithMessage(Documento.ContratoSoParaDiretoria);
        contrato.PerfisComAcesso.Should().BeEmpty();
    }

    [Fact]
    public void Contrato_recusa_usuario_de_perfil_de_producao_e_nada_muda()
    {
        var pedro = NovoUsuario("Pedro", _producao);
        var diana = NovoUsuario("Diana", _diretor);
        var contrato = Contrato();

        var acao = () => contrato.DefinirAcesso(
            [], [new UsuarioComPerfil(diana, _diretor), new UsuarioComPerfil(pedro, _producao)]);

        acao.Should().Throw<ExcecaoDeDominio>().WithMessage(Documento.ContratoSoParaDiretoria);
        contrato.UsuariosComAcesso.Should().BeEmpty();
    }

    [Fact]
    public void Contrato_aceita_perfil_e_usuario_de_diretoria()
    {
        var diana = NovoUsuario("Diana", _diretor);
        var contrato = Contrato();

        contrato.DefinirAcesso([_diretor], [new UsuarioComPerfil(diana, _diretor)]);

        contrato.PerfisComAcesso.Should().ContainSingle();
        contrato.UsuariosComAcesso.Should().ContainSingle();
    }

    [Fact]
    public void Perfil_ou_usuario_inativo_nao_entra_em_lista_nova()
    {
        var ester = NovoUsuario("Ester", _estagio);
        ester.Inativar();
        _producao.Inativar();
        var documento = Geral(_diretor);

        var comPerfil = () => documento.DefinirAcesso([_diretor, _producao], []);
        var comUsuario = () => documento.DefinirAcesso([_diretor], [new UsuarioComPerfil(ester, _estagio)]);

        comPerfil.Should().Throw<ExcecaoDeDominio>().WithMessage("Perfil inativo não pode receber acesso a documento.");
        comUsuario.Should().Throw<ExcecaoDeDominio>().WithMessage("Usuário inativo não pode receber acesso a documento.");
        documento.PerfisComAcesso.Should().ContainSingle();
        documento.UsuariosComAcesso.Should().BeEmpty();
    }

    [Fact]
    public void Perfil_ou_usuario_que_ficou_inativo_permanece_na_lista_em_que_ja_estava()
    {
        var ester = NovoUsuario("Ester", _estagio);
        var documento = Geral(_diretor, _producao);
        documento.DefinirAcesso([_diretor, _producao], [new UsuarioComPerfil(ester, _estagio)]);
        _producao.Inativar();
        ester.Inativar();

        var alteracao = documento.DefinirAcesso([_diretor, _producao], [new UsuarioComPerfil(ester, _estagio)]);

        alteracao.Houve.Should().BeFalse();
    }

    [Fact]
    public void Perfil_informado_tem_de_ser_o_do_usuario()
    {
        var pedro = NovoUsuario("Pedro", _producao);
        var documento = Geral(_diretor);

        var acao = () => documento.DefinirAcesso([], [new UsuarioComPerfil(pedro, _diretor)]);

        acao.Should().Throw<ArgumentException>();
    }
}
```

- [ ] **Step 2: Rodar e ver falhar**

Run: `dotnet test tests/AtividadesJuridicas.UnitTests --filter "FullyQualifiedName~DocumentoTests"`
Expected: FAIL na compilação (`Documento` não existe).

- [ ] **Step 3: Implementar**

`src/AtividadesJuridicas.Domain/Documentos/DocumentoAcesso.cs`:

```csharp
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Domain.Documentos;

/// <summary>
/// Um perfil na lista de acesso de um documento. É uma entidade, com Id próprio, para que a entrada
/// e a saída do perfil tenham carimbos, exclusão lógica e registro de auditoria.
/// </summary>
public sealed class DocumentoPerfilAcesso : Entidade
{
    private DocumentoPerfilAcesso() { }

    internal DocumentoPerfilAcesso(Guid documentoId, Guid perfilId)
    {
        DocumentoId = documentoId;
        PerfilId = perfilId;
    }

    public Guid DocumentoId { get; private set; }
    public Guid PerfilId { get; private set; }
}

/// <summary>Um usuário na lista de acesso de um documento.</summary>
public sealed class DocumentoUsuarioAcesso : Entidade
{
    private DocumentoUsuarioAcesso() { }

    internal DocumentoUsuarioAcesso(Guid documentoId, Guid usuarioId)
    {
        DocumentoId = documentoId;
        UsuarioId = usuarioId;
    }

    public Guid DocumentoId { get; private set; }
    public Guid UsuarioId { get; private set; }
}
```

`src/AtividadesJuridicas.Domain/Documentos/Documento.cs`:

```csharp
using AtividadesJuridicas.Domain.Comum;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.Domain.Documentos;

public enum CategoriaDocumento
{
    DocumentoGeral = 1,
    ContratoHonorarios = 2,
    DocumentoAtividade = 3,
}

/// <summary>O arquivo já gravado no armazenamento: nome enviado, tipo, tamanho, hash e onde ficou.</summary>
public sealed record ArquivoDoDocumento(
    string NomeOriginal, string TipoConteudo, long TamanhoBytes, string HashSha256, string ChaveArmazenamento);

/// <summary>Um usuário com o perfil que ele tem agora (o tipo do perfil decide a regra do contrato).</summary>
public sealed record UsuarioComPerfil(Usuario Usuario, Perfil Perfil);

/// <summary>O que mudou na lista de acesso de um documento, pelos Ids de perfis e de usuários.</summary>
public sealed record AlteracaoDeAcesso(
    IReadOnlyList<Guid> PerfisAdicionados, IReadOnlyList<Guid> PerfisRemovidos,
    IReadOnlyList<Guid> UsuariosAdicionados, IReadOnlyList<Guid> UsuariosRemovidos)
{
    public bool Houve =>
        PerfisAdicionados.Count > 0 || PerfisRemovidos.Count > 0 ||
        UsuariosAdicionados.Count > 0 || UsuariosRemovidos.Count > 0;
}

public sealed class Documento : Entidade
{
    public const int TamanhoMaximoNome = 255;
    public const int TamanhoMaximoTipoConteudo = 255;
    public const int TamanhoMaximoChave = 200;

    public const string ContratoSoParaDiretoria =
        "Contrato de Honorários não pode ser liberado para perfil ou usuário de Produção.";

    private readonly List<DocumentoPerfilAcesso> _perfisComAcesso = [];
    private readonly List<DocumentoUsuarioAcesso> _usuariosComAcesso = [];

    private Documento() { }

    public string Nome { get; private set; } = null!;
    public string NomeArquivoOriginal { get; private set; } = null!;
    public string TipoConteudo { get; private set; } = null!;
    public long TamanhoBytes { get; private set; }
    public string HashSha256 { get; private set; } = null!;
    public string ChaveArmazenamento { get; private set; } = null!;
    public CategoriaDocumento Categoria { get; private set; }

    /// <summary>Obrigatório em documento geral e em contrato de honorários.</summary>
    public Guid? ClienteId { get; private set; }

    public IReadOnlyCollection<DocumentoPerfilAcesso> PerfisComAcesso => _perfisComAcesso;
    public IReadOnlyCollection<DocumentoUsuarioAcesso> UsuariosComAcesso => _usuariosComAcesso;

    /// <summary>Documento geral: nasce liberado para todos os perfis ativos informados.</summary>
    public static Documento CriarGeral(
        Guid clienteId, string? nome, ArquivoDoDocumento arquivo, IReadOnlyCollection<Perfil> perfisAtivos)
    {
        ArgumentNullException.ThrowIfNull(perfisAtivos);
        if (perfisAtivos.Any(p => !p.Ativo))
            throw new ExcecaoDeDominio(PerfilInativo);

        var documento = Criar(CategoriaDocumento.DocumentoGeral, clienteId, nome, arquivo);
        foreach (var perfil in perfisAtivos.DistinctBy(p => p.Id))
            documento._perfisComAcesso.Add(new DocumentoPerfilAcesso(documento.Id, perfil.Id));
        return documento;
    }

    /// <summary>Contrato de honorários: nasce com a lista de acesso vazia; só a Diretoria o alcança.</summary>
    public static Documento CriarContratoDeHonorarios(Guid clienteId, string? nome, ArquivoDoDocumento arquivo) =>
        Criar(CategoriaDocumento.ContratoHonorarios, clienteId, nome, arquivo);

    public void Renomear(string nome) => Nome = NomeValido(nome);

    /// <summary>O nome do documento com a extensão do arquivo enviado, quando ele ainda não a tem.</summary>
    public string NomeParaBaixar()
    {
        var extensao = Path.GetExtension(NomeArquivoOriginal);
        return extensao.Length == 0 || Nome.EndsWith(extensao, StringComparison.OrdinalIgnoreCase)
            ? Nome
            : Nome + extensao;
    }

    /// <summary>A única regra de acesso a documentos.</summary>
    public bool PodeSerAcessadoPor(Guid usuarioId, Guid perfilId, TipoPerfil tipoPerfil)
    {
        if (tipoPerfil == TipoPerfil.Diretoria) return true;

        // Sem exceção: nem quem consta na lista (por ter sido da Diretoria) alcança um contrato.
        if (Categoria == CategoriaDocumento.ContratoHonorarios) return false;

        return _perfisComAcesso.Any(a => a.PerfilId == perfilId) ||
               _usuariosComAcesso.Any(a => a.UsuarioId == usuarioId);
    }

    /// <summary>
    /// Deixa a lista de acesso com exatamente os perfis e usuários informados. A entrada de quem
    /// permanece é preservada; a de quem sai é removida da coleção.
    /// </summary>
    public AlteracaoDeAcesso DefinirAcesso(
        IReadOnlyCollection<Perfil> perfis, IReadOnlyCollection<UsuarioComPerfil> usuarios)
    {
        ArgumentNullException.ThrowIfNull(perfis);
        ArgumentNullException.ThrowIfNull(usuarios);
        if (usuarios.Any(u => u.Usuario.PerfilId != u.Perfil.Id))
            throw new ArgumentException("O perfil informado não é o do usuário.", nameof(usuarios));

        var perfisDesejados = perfis.DistinctBy(p => p.Id).ToList();
        var usuariosDesejados = usuarios.DistinctBy(u => u.Usuario.Id).ToList();
        var perfisNovos = perfisDesejados.Where(p => _perfisComAcesso.All(a => a.PerfilId != p.Id)).ToList();
        var usuariosNovos = usuariosDesejados.Where(u => _usuariosComAcesso.All(a => a.UsuarioId != u.Usuario.Id)).ToList();

        // Tudo é verificado antes de qualquer mudança, para não deixar a lista pela metade.
        if (Categoria == CategoriaDocumento.ContratoHonorarios &&
            (perfisDesejados.Any(p => p.Tipo == TipoPerfil.Producao) ||
             usuariosDesejados.Any(u => u.Perfil.Tipo == TipoPerfil.Producao)))
            throw new ExcecaoDeDominio(ContratoSoParaDiretoria);
        if (perfisNovos.Any(p => !p.Ativo))
            throw new ExcecaoDeDominio(PerfilInativo);
        if (usuariosNovos.Any(u => !u.Usuario.Ativo))
            throw new ExcecaoDeDominio("Usuário inativo não pode receber acesso a documento.");

        var idsDePerfis = perfisDesejados.Select(p => p.Id).ToHashSet();
        var idsDeUsuarios = usuariosDesejados.Select(u => u.Usuario.Id).ToHashSet();
        var perfisRemovidos = _perfisComAcesso.Where(a => !idsDePerfis.Contains(a.PerfilId)).ToList();
        var usuariosRemovidos = _usuariosComAcesso.Where(a => !idsDeUsuarios.Contains(a.UsuarioId)).ToList();

        foreach (var entrada in perfisRemovidos) _perfisComAcesso.Remove(entrada);
        foreach (var entrada in usuariosRemovidos) _usuariosComAcesso.Remove(entrada);
        foreach (var perfil in perfisNovos) _perfisComAcesso.Add(new DocumentoPerfilAcesso(Id, perfil.Id));
        foreach (var usuario in usuariosNovos) _usuariosComAcesso.Add(new DocumentoUsuarioAcesso(Id, usuario.Usuario.Id));

        return new AlteracaoDeAcesso(
            [.. perfisNovos.Select(p => p.Id)], [.. perfisRemovidos.Select(a => a.PerfilId)],
            [.. usuariosNovos.Select(u => u.Usuario.Id)], [.. usuariosRemovidos.Select(a => a.UsuarioId)]);
    }

    private const string PerfilInativo = "Perfil inativo não pode receber acesso a documento.";

    private static Documento Criar(CategoriaDocumento categoria, Guid clienteId, string? nome, ArquivoDoDocumento arquivo)
    {
        ArgumentNullException.ThrowIfNull(arquivo);
        if (clienteId == Guid.Empty)
            throw new ExcecaoDeDominio("O cliente do documento é obrigatório.");
        if (string.IsNullOrWhiteSpace(arquivo.NomeOriginal))
            throw new ExcecaoDeDominio("O nome do arquivo é obrigatório.");

        var original = arquivo.NomeOriginal.Trim();
        if (original.Length > TamanhoMaximoNome)
            throw new ExcecaoDeDominio($"O nome do arquivo deve ter até {TamanhoMaximoNome} caracteres.");
        if (arquivo.TamanhoBytes <= 0)
            throw new ExcecaoDeDominio("O arquivo está vazio.");

        // O que vem do armazenamento e do servidor, não do usuário: erro aqui é de programação.
        if (string.IsNullOrWhiteSpace(arquivo.TipoConteudo) || arquivo.TipoConteudo.Trim().Length > TamanhoMaximoTipoConteudo)
            throw new ArgumentException("Tipo de conteúdo inválido.", nameof(arquivo));
        if (!HashValido(arquivo.HashSha256))
            throw new ArgumentException("O hash deve ser SHA-256 em hexadecimal minúsculo.", nameof(arquivo));
        if (string.IsNullOrWhiteSpace(arquivo.ChaveArmazenamento) || arquivo.ChaveArmazenamento.Length > TamanhoMaximoChave)
            throw new ArgumentException("Chave de armazenamento inválida.", nameof(arquivo));

        return new Documento
        {
            Categoria = categoria,
            ClienteId = clienteId,
            Nome = NomeValido(string.IsNullOrWhiteSpace(nome) ? original : nome),
            NomeArquivoOriginal = original,
            TipoConteudo = arquivo.TipoConteudo.Trim(),
            TamanhoBytes = arquivo.TamanhoBytes,
            HashSha256 = arquivo.HashSha256,
            ChaveArmazenamento = arquivo.ChaveArmazenamento,
        };
    }

    private static string NomeValido(string? nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ExcecaoDeDominio("O nome do documento é obrigatório.");
        if (nome.Trim().Length > TamanhoMaximoNome)
            throw new ExcecaoDeDominio($"O nome do documento deve ter até {TamanhoMaximoNome} caracteres.");
        return nome.Trim();
    }

    private static bool HashValido(string? hash) =>
        hash is { Length: 64 } && hash.All(c => c is (>= '0' and <= '9') or (>= 'a' and <= 'f'));
}
```

- [ ] **Step 4: Rodar e ver passar**

Run: `dotnet test tests/AtividadesJuridicas.UnitTests`
Expected: PASS, todos (269 anteriores + os novos), sem avisos. `ArquiteturaTests` continua verde (o domínio só usa a biblioteca base).

- [ ] **Step 5: Commit**

```bash
git add src/AtividadesJuridicas.Domain/Documentos tests/AtividadesJuridicas.UnitTests/Domain/DocumentoTests.cs
git commit -m "feat(documentos): Documento, listas de acesso e regra de acesso no domínio"
```

---

### Task 2: Armazenamento de arquivos em disco

**Files:**
- Create: `src/AtividadesJuridicas.Application/Documentos/PortasDeArmazenamento.cs`
- Create: `src/AtividadesJuridicas.Infrastructure/Armazenamento/ArmazenamentoEmDisco.cs`
- Modify: `src/AtividadesJuridicas.Infrastructure/DependencyInjection.cs`
- Modify: `src/AtividadesJuridicas.Web/Program.cs`
- Modify: `src/AtividadesJuridicas.Web/appsettings.json`
- Modify: `.gitignore`
- Modify: `tests/AtividadesJuridicas.IntegrationTests/TesteComBanco.cs`
- Modify: `tests/AtividadesJuridicas.IntegrationTests/AplicacaoDeTeste.cs`
- Test: `tests/AtividadesJuridicas.IntegrationTests/ArmazenamentoEmDiscoTests.cs`

**Interfaces:**
- Consumes: `IRelogio`.
- Produces (namespace `AtividadesJuridicas.Application.Documentos`):
  - `record ArquivoArmazenado(string Chave, long TamanhoBytes, string HashSha256)`
  - `interface IArmazenamentoDeArquivos { Task<ArquivoArmazenado> GravarAsync(Stream conteudo, long limiteBytes, CancellationToken ct); Task<Stream?> AbrirAsync(string chave, CancellationToken ct); Task RemoverAsync(string chave, CancellationToken ct); }`
  - `class ExcecaoDeArquivoGrandeDemais(long limiteBytes) : Exception` com `long LimiteBytes`
  - `AddInfrastructure(this IServiceCollection servicos, Func<IServiceProvider, string> obterStringDeConexao, Func<IServiceProvider, string> obterPastaDeArquivos)`
  - `TesteComBanco.PastaDeArquivos` (`protected string`), `AplicacaoDeTeste.PastaDeArquivos` (`public string`)

- [ ] **Step 1: Escrever os testes**

`tests/AtividadesJuridicas.IntegrationTests/ArmazenamentoEmDiscoTests.cs` (não usa banco; fica fora da coleção):

```csharp
using System.Text;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Documentos;
using AtividadesJuridicas.Infrastructure.Armazenamento;

namespace AtividadesJuridicas.IntegrationTests;

public sealed class ArmazenamentoEmDiscoTests : IDisposable
{
    private sealed class RelogioDeOutubro : IRelogio
    {
        public DateTime AgoraUtc => new(2026, 10, 8, 12, 0, 0, DateTimeKind.Utc);
    }

    private readonly string _pasta = Path.Combine(Path.GetTempPath(), "atividades-testes", Guid.NewGuid().ToString("N"));
    private readonly ArmazenamentoEmDisco _armazenamento;

    public ArmazenamentoEmDiscoTests() => _armazenamento = new ArmazenamentoEmDisco(_pasta, new RelogioDeOutubro());

    public void Dispose()
    {
        if (Directory.Exists(_pasta)) Directory.Delete(_pasta, recursive: true);
    }

    private static MemoryStream Conteudo(string texto) => new(Encoding.UTF8.GetBytes(texto));

    private string[] ArquivosNoDisco() =>
        Directory.Exists(_pasta) ? Directory.GetFiles(_pasta, "*", SearchOption.AllDirectories) : [];

    [Fact]
    public async Task Grava_e_devolve_chave_tamanho_e_hash()
    {
        var gravado = await _armazenamento.GravarAsync(Conteudo("abc"), 1024, default);

        gravado.TamanhoBytes.Should().Be(3);
        gravado.HashSha256.Should().Be("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad");
        gravado.Chave.Should().MatchRegex("^2026/10/[0-9a-f]{32}$");
        ArquivosNoDisco().Should().ContainSingle().Which.Should().NotEndWith(".tmp");
    }

    [Fact]
    public async Task O_que_foi_gravado_volta_igual()
    {
        var gravado = await _armazenamento.GravarAsync(Conteudo("conteúdo com acentuação"), 1024, default);

        await using var aberto = await _armazenamento.AbrirAsync(gravado.Chave, default);

        aberto.Should().NotBeNull();
        using var leitor = new StreamReader(aberto!, Encoding.UTF8);
        (await leitor.ReadToEndAsync()).Should().Be("conteúdo com acentuação");
    }

    [Fact]
    public async Task Cada_gravacao_tem_a_sua_chave()
    {
        var primeiro = await _armazenamento.GravarAsync(Conteudo("igual"), 1024, default);
        var segundo = await _armazenamento.GravarAsync(Conteudo("igual"), 1024, default);

        segundo.Chave.Should().NotBe(primeiro.Chave);
        segundo.HashSha256.Should().Be(primeiro.HashSha256);
        ArquivosNoDisco().Should().HaveCount(2);
    }

    [Fact]
    public async Task Arquivo_no_limite_e_aceito_e_um_byte_a_mais_e_recusado_sem_deixar_nada_no_disco()
    {
        (await _armazenamento.GravarAsync(Conteudo("12345"), 5, default)).TamanhoBytes.Should().Be(5);

        var acao = () => _armazenamento.GravarAsync(Conteudo("123456"), 5, default);

        (await acao.Should().ThrowAsync<ExcecaoDeArquivoGrandeDemais>()).Which.LimiteBytes.Should().Be(5);
        ArquivosNoDisco().Should().ContainSingle("só o primeiro arquivo ficou; o recusado não deixa resto, nem temporário");
    }

    [Fact]
    public async Task Arquivo_vazio_e_gravado_com_tamanho_zero()
    {
        // Quem recusa arquivo vazio é o domínio; o armazenamento só informa o tamanho.
        (await _armazenamento.GravarAsync(Conteudo(""), 1024, default)).TamanhoBytes.Should().Be(0);
    }

    [Fact]
    public async Task Abrir_chave_que_nao_existe_devolve_nulo()
    {
        (await _armazenamento.AbrirAsync("2026/10/00000000000000000000000000000000", default)).Should().BeNull();
    }

    [Theory]
    [InlineData("../../segredo")]
    [InlineData("2026/10/../../../segredo")]
    [InlineData("2026/10/0123456789abcdef0123456789abcdef.tmp")]
    [InlineData("C:\\Windows\\win.ini")]
    [InlineData("/etc/passwd")]
    [InlineData("")]
    public async Task Chave_fora_do_formato_e_recusada(string chave)
    {
        var abrir = () => _armazenamento.AbrirAsync(chave, default);
        var remover = () => _armazenamento.RemoverAsync(chave, default);

        await abrir.Should().ThrowAsync<ArgumentException>();
        await remover.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Remover_apaga_o_arquivo_e_repetir_nao_e_erro()
    {
        var gravado = await _armazenamento.GravarAsync(Conteudo("abc"), 1024, default);

        await _armazenamento.RemoverAsync(gravado.Chave, default);
        await _armazenamento.RemoverAsync(gravado.Chave, default);

        ArquivosNoDisco().Should().BeEmpty();
        (await _armazenamento.AbrirAsync(gravado.Chave, default)).Should().BeNull();
    }

    [Fact]
    public async Task Falha_na_leitura_do_conteudo_nao_deixa_temporario()
    {
        var acao = () => _armazenamento.GravarAsync(new FluxoQueFalha(), 1024, default);

        await acao.Should().ThrowAsync<IOException>();
        ArquivosNoDisco().Should().BeEmpty();
    }

    private sealed class FluxoQueFalha : MemoryStream
    {
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            throw new IOException("conexão caiu");
    }
}
```

- [ ] **Step 2: Rodar e ver falhar**

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --filter "FullyQualifiedName~ArmazenamentoEmDiscoTests"`
Expected: FAIL na compilação (`ArmazenamentoEmDisco` não existe).

- [ ] **Step 3: Porta na aplicação**

`src/AtividadesJuridicas.Application/Documentos/PortasDeArmazenamento.cs`:

```csharp
namespace AtividadesJuridicas.Application.Documentos;

/// <summary><paramref name="HashSha256"/> em hexadecimal minúsculo.</summary>
public sealed record ArquivoArmazenado(string Chave, long TamanhoBytes, string HashSha256);

/// <summary>Onde os arquivos ficam, fora do banco. A chave é sempre gerada pelo armazenamento.</summary>
public interface IArmazenamentoDeArquivos
{
    /// <summary>
    /// Grava o conteúdo e devolve onde ficou, o tamanho e o hash. Lança
    /// <see cref="ExcecaoDeArquivoGrandeDemais"/> quando o conteúdo passa de <paramref name="limiteBytes"/>;
    /// em qualquer falha, nada fica gravado.
    /// </summary>
    Task<ArquivoArmazenado> GravarAsync(Stream conteudo, long limiteBytes, CancellationToken ct);

    /// <summary>Nulo quando o arquivo não existe. Quem chama fecha o fluxo.</summary>
    Task<Stream?> AbrirAsync(string chave, CancellationToken ct);

    /// <summary>Remover o que não existe não é erro.</summary>
    Task RemoverAsync(string chave, CancellationToken ct);
}

public sealed class ExcecaoDeArquivoGrandeDemais(long limiteBytes)
    : Exception($"O arquivo passa do limite de {limiteBytes} bytes.")
{
    public long LimiteBytes { get; } = limiteBytes;
}
```

- [ ] **Step 4: Implementação em disco**

`src/AtividadesJuridicas.Infrastructure/Armazenamento/ArmazenamentoEmDisco.cs`:

```csharp
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Documentos;

namespace AtividadesJuridicas.Infrastructure.Armazenamento;

/// <summary>
/// Guarda cada arquivo em <c>pasta/aaaa/mm/guid</c>. A chave nunca vem do usuário e só é aceita
/// no formato que este armazenamento gera, o que impede sair da pasta.
/// </summary>
public sealed partial class ArmazenamentoEmDisco(string pasta, IRelogio relogio) : IArmazenamentoDeArquivos
{
    private const int TamanhoDoBuffer = 81920;

    private readonly string _raiz = Path.GetFullPath(pasta);

    [GeneratedRegex(@"^\d{4}/\d{2}/[0-9a-f]{32}$")]
    private static partial Regex FormatoDaChave();

    public async Task<ArquivoArmazenado> GravarAsync(Stream conteudo, long limiteBytes, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(conteudo);

        var agora = relogio.AgoraUtc;
        var chave = $"{agora:yyyy}/{agora:MM}/{Guid.NewGuid():N}";
        var destino = Caminho(chave);
        var temporario = destino + ".tmp";
        Directory.CreateDirectory(Path.GetDirectoryName(destino)!);

        try
        {
            long total = 0;
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

            await using (var arquivo = new FileStream(
                temporario, FileMode.CreateNew, FileAccess.Write, FileShare.None, TamanhoDoBuffer, useAsync: true))
            {
                var buffer = new byte[TamanhoDoBuffer];
                int lidos;
                while ((lidos = await conteudo.ReadAsync(buffer, ct)) > 0)
                {
                    total += lidos;
                    if (total > limiteBytes) throw new ExcecaoDeArquivoGrandeDemais(limiteBytes);

                    hash.AppendData(buffer, 0, lidos);
                    await arquivo.WriteAsync(buffer.AsMemory(0, lidos), ct);
                }
            }

            // Só aparece com o nome definitivo depois de completo.
            File.Move(temporario, destino);
            return new ArquivoArmazenado(chave, total, Convert.ToHexStringLower(hash.GetHashAndReset()));
        }
        catch
        {
            File.Delete(temporario);
            throw;
        }
    }

    public Task<Stream?> AbrirAsync(string chave, CancellationToken ct)
    {
        var caminho = Caminho(chave);
        return Task.FromResult<Stream?>(File.Exists(caminho)
            ? new FileStream(caminho, FileMode.Open, FileAccess.Read, FileShare.Read, TamanhoDoBuffer, useAsync: true)
            : null);
    }

    public Task RemoverAsync(string chave, CancellationToken ct)
    {
        File.Delete(Caminho(chave));
        return Task.CompletedTask;
    }

    private string Caminho(string chave)
    {
        if (chave is null || !FormatoDaChave().IsMatch(chave))
            throw new ArgumentException("Chave de armazenamento fora do formato.", nameof(chave));

        return Path.Combine(_raiz, chave.Replace('/', Path.DirectorySeparatorChar));
    }
}
```

`File.Delete` não lança quando o arquivo não existe, desde que a pasta exista. Em `RemoverAsync` a pasta do mês pode não existir (chave válida que nunca foi gravada): se `File.Delete` lançar `DirectoryNotFoundException` nesse caso, trate-o como "não existe" — o teste `Remover_apaga_o_arquivo_e_repetir_nao_e_erro` e o de chave inexistente definem o comportamento.

- [ ] **Step 5: Composição**

Em `src/AtividadesJuridicas.Infrastructure/DependencyInjection.cs`, a assinatura ganha o segundo parâmetro e o registro do armazenamento (acrescente `using AtividadesJuridicas.Application.Documentos;` e `using AtividadesJuridicas.Infrastructure.Armazenamento;`):

```csharp
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection servicos,
        Func<IServiceProvider, string> obterStringDeConexao,
        Func<IServiceProvider, string> obterPastaDeArquivos)
    {
        // ... registros existentes, sem mudança ...

        servicos.AddSingleton<IArmazenamentoDeArquivos>(sp =>
            new ArmazenamentoEmDisco(obterPastaDeArquivos(sp), sp.GetRequiredService<IRelogio>()));

        return servicos;
    }
```

Em `src/AtividadesJuridicas.Web/Program.cs`, a chamada passa a ser:

```csharp
builder.Services.AddInfrastructure(
    sp => sp.GetRequiredService<IConfiguration>().GetConnectionString("Padrao")
          ?? throw new InvalidOperationException("ConnectionStrings:Padrao não configurada."),
    sp =>
    {
        // Caminho relativo é resolvido a partir da pasta da aplicação.
        var pasta = sp.GetRequiredService<IConfiguration>()["Armazenamento:Pasta"]
                    ?? throw new InvalidOperationException("Armazenamento:Pasta não configurada.");
        return Path.GetFullPath(pasta, sp.GetRequiredService<IWebHostEnvironment>().ContentRootPath);
    });
```

Em `src/AtividadesJuridicas.Web/appsettings.json`, acrescente a seção (ao lado de `Escritorio`):

```json
  "Armazenamento": {
    "Pasta": "dados/arquivos"
  }
```

No fim de `.gitignore`:

```
# Arquivos enviados pelos usuários (armazenamento em disco)
dados/
```

- [ ] **Step 6: Apoio de teste**

Em `tests/AtividadesJuridicas.IntegrationTests/TesteComBanco.cs`:

```csharp
    /// <summary>Pasta de arquivos só deste teste; apagada ao final.</summary>
    protected string PastaDeArquivos { get; } =
        Path.Combine(Path.GetTempPath(), "atividades-testes", Guid.NewGuid().ToString("N"));
```

Na montagem dos serviços: `.AddInfrastructure(_ => conexao, _ => PastaDeArquivos)`. Em `DisposeAsync`, depois de descartar a raiz:

```csharp
        if (Directory.Exists(PastaDeArquivos)) Directory.Delete(PastaDeArquivos, recursive: true);
```

Em `tests/AtividadesJuridicas.IntegrationTests/AplicacaoDeTeste.cs`:

```csharp
    public string PastaDeArquivos { get; } =
        Path.Combine(Path.GetTempPath(), "atividades-testes", Guid.NewGuid().ToString("N"));
```

Acrescente `["Armazenamento:Pasta"] = PastaDeArquivos,` à configuração em memória e, em `DisposeAsync`, depois de `base.DisposeAsync()`, apague a pasta se existir.

- [ ] **Step 7: Rodar e ver passar**

Run: `dotnet test` (suíte inteira, da raiz)
Expected: PASS — 269 unitários + os da Tarefa 1; integração 162 + 14 novos (9 testes, um deles com 6 casos); sem avisos. A aplicação Web sobe nos testes de página (a pasta configurada não precisa existir até a primeira gravação).

- [ ] **Step 8: Commit**

```bash
git add src tests .gitignore
git commit -m "feat(documentos): armazenamento de arquivos em disco atrás de uma porta da aplicação"
```

---

### Task 3: Casos de uso, persistência e auditoria dos documentos

Uma tarefa só: a aplicação Web valida a injeção de dependências ao subir, então os manipuladores e as implementações das portas precisam entrar juntos.

**Files:**
- Modify: `src/AtividadesJuridicas.Application/Identidade/ContextoDeAcesso.cs` (`UsuarioAutenticado` ganha `PerfilId`)
- Modify: `src/AtividadesJuridicas.Application/Identidade/Portas.cs` (`ObterAtivosAsync`, `ObterVariosAsync`)
- Modify: `src/AtividadesJuridicas.Application/Auditoria/IAuditoria.cs` (evento `documento_acesso_alterado`)
- Create: `src/AtividadesJuridicas.Application/Documentos/PortasDeDocumentos.cs`
- Create: `src/AtividadesJuridicas.Application/Documentos/EnviarDocumento.cs`
- Create: `src/AtividadesJuridicas.Application/Documentos/ListarDocumentosDoCliente.cs`
- Create: `src/AtividadesJuridicas.Application/Documentos/BaixarDocumento.cs`
- Create: `src/AtividadesJuridicas.Application/Documentos/RenomearDocumento.cs`
- Create: `src/AtividadesJuridicas.Application/Documentos/ExcluirDocumento.cs`
- Create: `src/AtividadesJuridicas.Application/Documentos/ObterAcessoDoDocumento.cs`
- Create: `src/AtividadesJuridicas.Application/Documentos/DefinirAcessoDoDocumento.cs`
- Create: `src/AtividadesJuridicas.Infrastructure/Persistencia/Configuracoes/DocumentoConfiguracao.cs`
- Create: `src/AtividadesJuridicas.Infrastructure/Persistencia/Repositorios/DocumentoRepositorio.cs`
- Modify: `src/AtividadesJuridicas.Infrastructure/Persistencia/AppDbContext.cs` (`DbSet<Documento> Documentos`)
- Modify: `src/AtividadesJuridicas.Infrastructure/Persistencia/Repositorios/PerfilRepositorio.cs`, `UsuarioRepositorio.cs`
- Modify: `src/AtividadesJuridicas.Infrastructure/DependencyInjection.cs`
- Create: migration `Documentos`
- Modify: `tests/AtividadesJuridicas.UnitTests/Application/Falsos.cs`, `ContextoDeAcessoTests.cs`
- Modify: `tests/AtividadesJuridicas.IntegrationTests/PropriedadesAuditadasTests.cs`
- Test: `tests/AtividadesJuridicas.UnitTests/Application/DocumentosTests.cs`
- Test: `tests/AtividadesJuridicas.IntegrationTests/DocumentosTests.cs`

**Interfaces:**
- Consumes: tudo o que as Tarefas 1 e 2 produzem; `IClienteRepositorio.ObterPorIdAsync`; `IUnidadeDeTrabalho`; `IAuditoria`; `IContextoDeAcesso`.
- Produces:
  - `record UsuarioAutenticado(Guid Id, string Nome, TipoPerfil TipoPerfil, Guid PerfilId)`
  - `IPerfilRepositorio.ObterAtivosAsync(CancellationToken): Task<IReadOnlyList<Perfil>>`, `IPerfilRepositorio.ObterVariosAsync(IReadOnlyCollection<Guid>, CancellationToken): Task<IReadOnlyList<Perfil>>`, `IUsuarioRepositorio.ObterVariosAsync(IReadOnlyCollection<Guid>, CancellationToken): Task<IReadOnlyList<Usuario>>`
  - `EventosDeSeguranca.DocumentoAcessoAlterado = "documento_acesso_alterado"`
  - namespace `AtividadesJuridicas.Application.Documentos`:
    - `static class DocumentosOpcoes { const long TamanhoMaximoBytes = 50L * 1024 * 1024; const int TamanhoDaPagina = 20; const string TipoDeConteudoPadrao = "application/octet-stream"; }`
    - `record FiltroDeAcesso(Guid UsuarioId, Guid PerfilId)`
    - `record DocumentoResumo(Guid Id, string Nome, string NomeArquivoOriginal, long TamanhoBytes, CategoriaDocumento Categoria, DateTime EnviadoEm, string? EnviadoPor)`
    - `record ArquivoParaBaixar(string NomeArquivo, long TamanhoBytes, Stream Conteudo)`
    - `record AcessoDoDocumento(Guid DocumentoId, string Nome, CategoriaDocumento Categoria, IReadOnlyList<Guid> Perfis, IReadOnlyList<Guid> Usuarios)`
    - `interface IDocumentoRepositorio { Task<Documento?> ObterPorIdAsync(Guid id, CancellationToken ct); Task<Pagina<DocumentoResumo>> ListarDoClienteAsync(Guid clienteId, CategoriaDocumento categoria, FiltroDeAcesso? acesso, int pagina, int tamanho, CancellationToken ct); void Adicionar(Documento documento); void Remover(Documento documento); }`
    - `record EnviarDocumento(CategoriaDocumento Categoria, Guid ClienteId, string? Nome, string NomeArquivo, string? TipoConteudo, Stream Conteudo) : IRequisicao<Resultado<Guid>>`
    - `record ListarDocumentosDoCliente(Guid ClienteId, CategoriaDocumento Categoria, int Pagina = 1) : IRequisicao<Resultado<Pagina<DocumentoResumo>>>, IConsultaAuditavel`
    - `record BaixarDocumento(Guid Id) : IRequisicao<Resultado<ArquivoParaBaixar>>, IConsultaAuditavel`
    - `record RenomearDocumento(Guid Id, string Nome) : IRequisicao<Resultado>`
    - `record ExcluirDocumento(Guid Id) : IRequisicao<Resultado>`
    - `record ObterAcessoDoDocumento(Guid Id) : IRequisicao<Resultado<AcessoDoDocumento>>, IConsultaAuditavel`
    - `record DefinirAcessoDoDocumento(Guid Id, IReadOnlyList<Guid> Perfis, IReadOnlyList<Guid> Usuarios) : IRequisicao<Resultado>`

#### Parte A — Aplicação

- [ ] **Step 1: Falsos e cenário**

Em `tests/AtividadesJuridicas.UnitTests/Application/Falsos.cs` (acrescente os `using` de `System.Security.Cryptography`, `AtividadesJuridicas.Application.Documentos` e `AtividadesJuridicas.Domain.Documentos`):

`PerfilRepositorioFalso` ganha:

```csharp
    public Task<IReadOnlyList<Perfil>> ObterAtivosAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Perfil>>([.. Itens.Where(p => p.Ativo)]);

    public Task<IReadOnlyList<Perfil>> ObterVariosAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Perfil>>([.. Itens.Where(p => ids.Contains(p.Id))]);
```

`UsuarioRepositorioFalso` ganha:

```csharp
    public Task<IReadOnlyList<Usuario>> ObterVariosAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Usuario>>([.. Itens.Where(u => ids.Contains(u.Id))]);
```

Novos falsos:

```csharp
public sealed class ArmazenamentoFalso : IArmazenamentoDeArquivos
{
    public Dictionary<string, byte[]> Arquivos { get; } = [];

    /// <summary>Simula um arquivo acima do limite, sem precisar de um conteúdo enorme.</summary>
    public bool RecusarPorTamanho { get; set; }

    public async Task<ArquivoArmazenado> GravarAsync(Stream conteudo, long limiteBytes, CancellationToken ct)
    {
        if (RecusarPorTamanho) throw new ExcecaoDeArquivoGrandeDemais(limiteBytes);

        using var memoria = new MemoryStream();
        await conteudo.CopyToAsync(memoria, ct);
        var bytes = memoria.ToArray();
        var chave = $"2026/10/{Guid.NewGuid():N}";
        Arquivos[chave] = bytes;
        return new ArquivoArmazenado(chave, bytes.LongLength, Convert.ToHexStringLower(SHA256.HashData(bytes)));
    }

    public Task<Stream?> AbrirAsync(string chave, CancellationToken ct) =>
        Task.FromResult<Stream?>(Arquivos.TryGetValue(chave, out var bytes) ? new MemoryStream(bytes) : null);

    public Task RemoverAsync(string chave, CancellationToken ct)
    {
        Arquivos.Remove(chave);
        return Task.CompletedTask;
    }
}

public sealed class DocumentoRepositorioFalso : IDocumentoRepositorio
{
    public List<Documento> Itens { get; } = [];

    public Task<Documento?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Itens.FirstOrDefault(d => d.Id == id));

    public Task<Pagina<DocumentoResumo>> ListarDoClienteAsync(
        Guid clienteId, CategoriaDocumento categoria, FiltroDeAcesso? acesso, int pagina, int tamanho, CancellationToken ct)
    {
        // Mesma regra da consulta real: sem filtro (Diretoria) vê tudo; com filtro, nunca contrato.
        var todos = Itens
            .Where(d => d.ClienteId == clienteId && d.Categoria == categoria)
            .Where(d => acesso is null ||
                (d.Categoria != CategoriaDocumento.ContratoHonorarios &&
                 (d.PerfisComAcesso.Any(a => a.PerfilId == acesso.PerfilId) ||
                  d.UsuariosComAcesso.Any(a => a.UsuarioId == acesso.UsuarioId))))
            .OrderBy(d => d.Nome)
            .ToList();

        return Task.FromResult(new Pagina<DocumentoResumo>(
            [.. todos.Skip((pagina - 1) * tamanho).Take(tamanho).Select(d => new DocumentoResumo(
                d.Id, d.Nome, d.NomeArquivoOriginal, d.TamanhoBytes, d.Categoria, d.CriadoEm, null))],
            todos.Count, pagina, tamanho));
    }

    public void Adicionar(Documento documento) => Itens.Add(documento);

    public void Remover(Documento documento) => Itens.Remove(documento);
}
```

`Cenario` ganha `public DocumentoRepositorioFalso Documentos { get; } = new();` e `public ArmazenamentoFalso Armazenamento { get; } = new();`, e `Como` passa o perfil:

```csharp
        return new ContextoDeAcessoFixo(new UsuarioAutenticado(usuario.Id, usuario.Nome, perfil.Tipo, perfil.Id));
```

Em `ContextoDeAcessoTests.cs`, a asserção existente passa a incluir o perfil:

```csharp
        ator.Should().Be(new UsuarioAutenticado(
            _cenario.Diretor.Id, "Diana Diretora", TipoPerfil.Diretoria, _cenario.PerfilDiretor.Id));
```

- [ ] **Step 2: Testes unitários dos casos de uso**

`tests/AtividadesJuridicas.UnitTests/Application/DocumentosTests.cs`:

```csharp
using System.Text;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Documentos;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Documentos;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.UnitTests.Application;

public class DocumentosTests
{
    private readonly Cenario _c = new();
    private readonly ClientePessoaFisica _maria;
    private readonly Perfil _estagio = Perfil.Criar("Estagiário", TipoPerfil.Producao);
    private readonly Usuario _pedro;
    private readonly Usuario _ester;

    public DocumentosTests()
    {
        _maria = ClientePessoaFisica.Criar(
            new DadosDoCliente("Maria Silva", null, null, Endereco.Vazio, null), Cpf.Criar("529.982.247-25"), null, null);
        _c.Clientes.Itens.Add(_maria);
        _c.Perfis.Itens.Add(_estagio);
        _pedro = _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);
        _ester = _c.NovoUsuario("Ester", "ester@x.com", _estagio);
    }

    private static MemoryStream Conteudo(string texto = "conteúdo") => new(Encoding.UTF8.GetBytes(texto));

    private Task<Resultado<Guid>> Enviar(Usuario? ator, CategoriaDocumento categoria = CategoriaDocumento.DocumentoGeral,
        string arquivo = "procuracao.pdf", string? nome = null, Guid? cliente = null, Stream? conteudo = null,
        string? tipo = "application/pdf") =>
        new EnviarDocumentoManipulador(_c.Como(ator), _c.Documentos, _c.Clientes, _c.Perfis, _c.Armazenamento, _c.Unidade)
            .ManipularAsync(new EnviarDocumento(categoria, cliente ?? _maria.Id, nome, arquivo, tipo, conteudo ?? Conteudo()), default);

    private async Task<Documento> Enviado(CategoriaDocumento categoria = CategoriaDocumento.DocumentoGeral, string arquivo = "procuracao.pdf")
    {
        var resultado = await Enviar(_c.Diretor, categoria, arquivo);
        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        return _c.Documentos.Itens.Single(d => d.Id == resultado.Valor);
    }

    private Task<Resultado<Pagina<DocumentoResumo>>> Listar(Usuario? ator,
        CategoriaDocumento categoria = CategoriaDocumento.DocumentoGeral, int pagina = 1) =>
        new ListarDocumentosDoClienteManipulador(_c.Como(ator), _c.Documentos)
            .ManipularAsync(new ListarDocumentosDoCliente(_maria.Id, categoria, pagina), default);

    private Task<Resultado<ArquivoParaBaixar>> Baixar(Usuario? ator, Guid id) =>
        new BaixarDocumentoManipulador(_c.Como(ator), _c.Documentos, _c.Armazenamento, _c.Auditoria)
            .ManipularAsync(new BaixarDocumento(id), default);

    private Task<Resultado> DefinirAcesso(Usuario? ator, Guid id, Guid[] perfis, Guid[] usuarios) =>
        new DefinirAcessoDoDocumentoManipulador(_c.Como(ator), _c.Documentos, _c.Perfis, _c.Usuarios, _c.Unidade, _c.Auditoria)
            .ManipularAsync(new DefinirAcessoDoDocumento(id, perfis, usuarios), default);

    // ---- Enviar ----

    [Fact]
    public async Task Diretor_envia_documento_geral_liberado_para_os_perfis_ativos()
    {
        _estagio.Inativar();

        var resultado = await Enviar(_c.Diretor, conteudo: Conteudo("abc"));

        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        var documento = _c.Documentos.Itens.Should().ContainSingle().Subject;
        documento.Id.Should().Be(resultado.Valor);
        documento.Categoria.Should().Be(CategoriaDocumento.DocumentoGeral);
        documento.ClienteId.Should().Be(_maria.Id);
        documento.Nome.Should().Be("procuracao.pdf");
        documento.TipoConteudo.Should().Be("application/pdf");
        documento.TamanhoBytes.Should().Be(3);
        documento.HashSha256.Should().Be("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad");
        documento.PerfisComAcesso.Select(a => a.PerfilId)
            .Should().BeEquivalentTo([_c.PerfilDiretor.Id, _c.PerfilProducao.Id], "o perfil inativo fica de fora");
        _c.Armazenamento.Arquivos.Should().ContainKey(documento.ChaveArmazenamento);
        _c.Unidade.Salvamentos.Should().Be(1);
    }

    [Fact]
    public async Task Contrato_de_honorarios_nasce_sem_ninguem_na_lista()
    {
        var contrato = await Enviado(CategoriaDocumento.ContratoHonorarios, "contrato.pdf");

        contrato.Categoria.Should().Be(CategoriaDocumento.ContratoHonorarios);
        contrato.PerfisComAcesso.Should().BeEmpty();
    }

    [Theory]
    [InlineData(@"C:\Users\diana\Documents\contrato.pdf", "contrato.pdf")]
    [InlineData("../../etc/passwd", "passwd")]
    [InlineData("/tmp/laudo.pdf", "laudo.pdf")]
    [InlineData("  laudo final.pdf  ", "laudo final.pdf")]
    public async Task So_o_ultimo_segmento_do_nome_enviado_e_guardado(string enviado, string esperado)
    {
        var documento = await Enviado(arquivo: enviado);

        documento.NomeArquivoOriginal.Should().Be(esperado);
        documento.ChaveArmazenamento.Should().NotContain("passwd").And.NotContain("contrato").And.NotContain("laudo");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("..")]
    [InlineData("pasta/")]
    public async Task Envio_sem_nome_de_arquivo_e_recusado_antes_de_gravar(string arquivo)
    {
        var resultado = await Enviar(_c.Diretor, arquivo: arquivo);

        resultado.Erro.Should().Be(TipoErro.Validacao);
        resultado.Mensagem.Should().Be("Informe o arquivo.");
        _c.Armazenamento.Arquivos.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Tipo_de_conteudo_ausente_vira_o_padrao(string? tipo)
    {
        var resultado = await Enviar(_c.Diretor, tipo: tipo);

        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        _c.Documentos.Itens.Single().TipoConteudo.Should().Be("application/octet-stream");
    }

    [Fact]
    public async Task Tipo_de_conteudo_longo_demais_vira_o_padrao()
    {
        (await Enviar(_c.Diretor, tipo: new string('x', 300))).Sucesso.Should().BeTrue();

        _c.Documentos.Itens.Single().TipoConteudo.Should().Be("application/octet-stream");
    }

    [Fact]
    public async Task Producao_nao_envia_e_nada_e_gravado()
    {
        var resultado = await Enviar(_pedro);

        resultado.Erro.Should().Be(TipoErro.AcessoNegado);
        _c.Armazenamento.Arquivos.Should().BeEmpty();
        _c.Documentos.Itens.Should().BeEmpty();
    }

    [Fact]
    public async Task Documento_de_atividade_nao_e_enviado_por_aqui()
    {
        var resultado = await Enviar(_c.Diretor, CategoriaDocumento.DocumentoAtividade);

        resultado.Erro.Should().Be(TipoErro.Validacao);
        resultado.Mensagem.Should().Be("Categoria inválida para envio.");
        _c.Armazenamento.Arquivos.Should().BeEmpty();
    }

    [Fact]
    public async Task Cliente_inexistente_e_recusado_antes_de_gravar()
    {
        var resultado = await Enviar(_c.Diretor, cliente: Guid.CreateVersion7());

        resultado.Erro.Should().Be(TipoErro.Validacao);
        resultado.Mensagem.Should().Be("Cliente não encontrado.");
        _c.Armazenamento.Arquivos.Should().BeEmpty();
    }

    [Fact]
    public async Task Arquivo_vazio_e_recusado_e_nao_fica_no_armazenamento()
    {
        var resultado = await Enviar(_c.Diretor, conteudo: Conteudo(""));

        resultado.Erro.Should().Be(TipoErro.Validacao);
        resultado.Mensagem.Should().Be("O arquivo está vazio.");
        _c.Armazenamento.Arquivos.Should().BeEmpty();
        _c.Documentos.Itens.Should().BeEmpty();
        _c.Unidade.Salvamentos.Should().Be(0);
    }

    [Fact]
    public async Task Arquivo_acima_do_limite_e_recusado_com_o_limite_na_mensagem()
    {
        _c.Armazenamento.RecusarPorTamanho = true;

        var resultado = await Enviar(_c.Diretor);

        resultado.Erro.Should().Be(TipoErro.Validacao);
        resultado.Mensagem.Should().Be("O arquivo passa do limite de 50 MB.");
        _c.Documentos.Itens.Should().BeEmpty();
    }

    [Fact]
    public async Task Falha_ao_gravar_no_banco_remove_o_arquivo_e_sobe()
    {
        _c.Unidade.Falha = new InvalidOperationException("banco fora do ar");

        var acao = () => Enviar(_c.Diretor);

        await acao.Should().ThrowAsync<InvalidOperationException>().WithMessage("banco fora do ar");
        _c.Armazenamento.Arquivos.Should().BeEmpty("o arquivo não pode ficar sem registro");
    }

    // ---- Listar ----

    [Fact]
    public async Task Diretoria_lista_todos_e_producao_so_os_que_pode_acessar()
    {
        var liberado = await Enviado(arquivo: "a-liberado.pdf");
        var restrito = await Enviado(arquivo: "b-restrito.pdf");
        var soDaEster = await Enviado(arquivo: "c-da-ester.pdf");
        (await DefinirAcesso(_c.Diretor, restrito.Id, [_c.PerfilDiretor.Id], [])).Sucesso.Should().BeTrue();
        (await DefinirAcesso(_c.Diretor, soDaEster.Id, [], [_ester.Id])).Sucesso.Should().BeTrue();

        (await Listar(_c.Diretor)).Valor!.Itens.Select(d => d.Id)
            .Should().Equal(liberado.Id, restrito.Id, soDaEster.Id);
        (await Listar(_pedro)).Valor!.Itens.Select(d => d.Id).Should().Equal(liberado.Id);
        // O perfil da Ester estava ativo quando "liberado" foi enviado: ela o alcança pelo perfil.
        (await Listar(_ester)).Valor!.Itens.Select(d => d.Id).Should().Equal(liberado.Id, soDaEster.Id);
    }

    [Fact]
    public async Task Producao_nao_lista_contratos_de_honorarios()
    {
        await Enviado(CategoriaDocumento.ContratoHonorarios, "contrato.pdf");

        (await Listar(_pedro, CategoriaDocumento.ContratoHonorarios)).Erro.Should().Be(TipoErro.AcessoNegado);
        (await Listar(_c.Diretor, CategoriaDocumento.ContratoHonorarios)).Valor!.Itens.Should().ContainSingle();
    }

    [Fact]
    public async Task Sem_usuario_nao_lista()
    {
        (await Listar(null)).Erro.Should().Be(TipoErro.AcessoNegado);
    }

    [Fact]
    public async Task Pagina_absurda_devolve_lista_vazia_com_o_total()
    {
        await Enviado();

        var pagina = (await Listar(_c.Diretor, pagina: int.MaxValue)).Valor!;

        pagina.Itens.Should().BeEmpty();
        pagina.Total.Should().Be(1);
    }

    [Fact]
    public void Listagem_e_consulta_auditavel_com_os_filtros_no_detalhe()
    {
        IConsultaAuditavel consulta = new ListarDocumentosDoCliente(_maria.Id, CategoriaDocumento.ContratoHonorarios, 2);

        consulta.TipoRecurso.Should().Be("Documento");
        consulta.Acao.Should().Be(AcaoDeAcesso.Listou);
        consulta.RecursoId.Should().BeNull();
        consulta.Detalhe.Should().Contain(_maria.Id.ToString()).And.Contain("\"Pagina\":2");
    }

    // ---- Baixar ----

    [Fact]
    public async Task Diretor_baixa_o_conteudo_com_o_nome_do_documento()
    {
        var documento = await Enviado();
        documento.Renomear("Procuração assinada");

        var resultado = await Baixar(_c.Diretor, documento.Id);

        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        resultado.Valor!.NomeArquivo.Should().Be("Procuração assinada.pdf");
        resultado.Valor.TamanhoBytes.Should().Be(documento.TamanhoBytes);
        using var leitor = new StreamReader(resultado.Valor.Conteudo, Encoding.UTF8);
        (await leitor.ReadToEndAsync()).Should().Be("conteúdo");
    }

    [Fact]
    public async Task Producao_com_acesso_baixa()
    {
        var documento = await Enviado();

        (await Baixar(_pedro, documento.Id)).Sucesso.Should().BeTrue();
    }

    [Fact]
    public async Task Producao_sem_acesso_recebe_nao_encontrado_e_a_tentativa_fica_registrada()
    {
        var documento = await Enviado();
        (await DefinirAcesso(_c.Diretor, documento.Id, [_c.PerfilDiretor.Id], [])).Sucesso.Should().BeTrue();
        _c.Auditoria.Eventos.Clear();

        var resultado = await Baixar(_pedro, documento.Id);

        resultado.Erro.Should().Be(TipoErro.NaoEncontrado);
        resultado.Mensagem.Should().Be("Documento não encontrado.");
        var evento = _c.Auditoria.Eventos.Should().ContainSingle().Subject;
        evento.Evento.Should().Be(EventosDeSeguranca.AcessoNegado);
        evento.Entidade.Should().Be("Documento");
        evento.EntidadeId.Should().Be(documento.Id);
        evento.Imediato.Should().BeTrue();
    }

    [Fact]
    public async Task Contrato_responde_nao_encontrado_para_producao()
    {
        var contrato = await Enviado(CategoriaDocumento.ContratoHonorarios, "contrato.pdf");

        var resultado = await Baixar(_pedro, contrato.Id);

        resultado.Erro.Should().Be(TipoErro.NaoEncontrado);
        _c.Auditoria.Eventos.Should().ContainSingle(e => e.Evento == EventosDeSeguranca.AcessoNegado);
    }

    [Fact]
    public async Task Documento_inexistente_e_nao_encontrado_sem_evento()
    {
        var resultado = await Baixar(_pedro, Guid.CreateVersion7());

        resultado.Erro.Should().Be(TipoErro.NaoEncontrado);
        resultado.Mensagem.Should().Be("Documento não encontrado.");
        _c.Auditoria.Eventos.Should().BeEmpty();
    }

    [Fact]
    public async Task Sem_usuario_nao_baixa()
    {
        var documento = await Enviado();

        (await Baixar(null, documento.Id)).Erro.Should().Be(TipoErro.AcessoNegado);
    }

    [Fact]
    public async Task Arquivo_sumido_do_armazenamento_e_avisado_como_nao_encontrado()
    {
        var documento = await Enviado();
        _c.Armazenamento.Arquivos.Clear();

        var resultado = await Baixar(_c.Diretor, documento.Id);

        resultado.Erro.Should().Be(TipoErro.NaoEncontrado);
        resultado.Mensagem.Should().Be("O arquivo deste documento não está no armazenamento.");
    }

    [Fact]
    public void Baixar_e_consulta_auditavel_do_documento()
    {
        var id = Guid.CreateVersion7();
        IConsultaAuditavel consulta = new BaixarDocumento(id);

        consulta.TipoRecurso.Should().Be("Documento");
        consulta.Acao.Should().Be(AcaoDeAcesso.Baixou);
        consulta.RecursoId.Should().Be(id);
    }

    // ---- Renomear e excluir ----

    [Fact]
    public async Task Diretor_renomeia()
    {
        var documento = await Enviado();

        var resultado = await new RenomearDocumentoManipulador(_c.Como(_c.Diretor), _c.Documentos, _c.Unidade)
            .ManipularAsync(new RenomearDocumento(documento.Id, " Procuração 2026 "), default);

        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        documento.Nome.Should().Be("Procuração 2026");
    }

    [Fact]
    public async Task Renomear_valida_e_respeita_o_acesso()
    {
        var documento = await Enviado();
        Task<Resultado> Renomear(Usuario? ator, Guid id, string nome) =>
            new RenomearDocumentoManipulador(_c.Como(ator), _c.Documentos, _c.Unidade)
                .ManipularAsync(new RenomearDocumento(id, nome), default);

        (await Renomear(_pedro, documento.Id, "x")).Erro.Should().Be(TipoErro.AcessoNegado);
        (await Renomear(_c.Diretor, documento.Id, " ")).Erro.Should().Be(TipoErro.Validacao);
        (await Renomear(_c.Diretor, Guid.CreateVersion7(), "x")).Erro.Should().Be(TipoErro.NaoEncontrado);
        documento.Nome.Should().Be("procuracao.pdf");
    }

    [Fact]
    public async Task Diretor_exclui_o_documento_e_o_arquivo_permanece_no_armazenamento()
    {
        var documento = await Enviado();

        var resultado = await new ExcluirDocumentoManipulador(_c.Como(_c.Diretor), _c.Documentos, _c.Unidade)
            .ManipularAsync(new ExcluirDocumento(documento.Id), default);

        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        _c.Documentos.Itens.Should().BeEmpty();
        _c.Armazenamento.Arquivos.Should().ContainKey(documento.ChaveArmazenamento);
    }

    [Fact]
    public async Task Producao_nao_exclui()
    {
        var documento = await Enviado();

        var resultado = await new ExcluirDocumentoManipulador(_c.Como(_pedro), _c.Documentos, _c.Unidade)
            .ManipularAsync(new ExcluirDocumento(documento.Id), default);

        resultado.Erro.Should().Be(TipoErro.AcessoNegado);
        _c.Documentos.Itens.Should().ContainSingle();
    }

    // ---- Lista de acesso ----

    [Fact]
    public async Task Diretor_ve_a_lista_de_acesso()
    {
        var documento = await Enviado();
        (await DefinirAcesso(_c.Diretor, documento.Id, [_c.PerfilDiretor.Id], [_ester.Id])).Sucesso.Should().BeTrue();

        var resultado = await new ObterAcessoDoDocumentoManipulador(_c.Como(_c.Diretor), _c.Documentos)
            .ManipularAsync(new ObterAcessoDoDocumento(documento.Id), default);

        resultado.Valor.Should().BeEquivalentTo(new AcessoDoDocumento(
            documento.Id, "procuracao.pdf", CategoriaDocumento.DocumentoGeral, [_c.PerfilDiretor.Id], [_ester.Id]));
    }

    [Fact]
    public async Task Producao_nao_ve_a_lista_de_acesso()
    {
        var documento = await Enviado();

        var resultado = await new ObterAcessoDoDocumentoManipulador(_c.Como(_pedro), _c.Documentos)
            .ManipularAsync(new ObterAcessoDoDocumento(documento.Id), default);

        resultado.Erro.Should().Be(TipoErro.AcessoNegado);
    }

    [Fact]
    public async Task Definir_acesso_anota_o_evento_com_o_que_mudou()
    {
        // O perfil Estagiário estava inativo no envio: o documento nasce só com Diretor e Produção.
        _estagio.Inativar();
        var documento = await Enviado();
        _estagio.Ativar();

        var resultado = await DefinirAcesso(_c.Diretor, documento.Id, [_c.PerfilDiretor.Id, _estagio.Id], [_pedro.Id]);

        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        var evento = _c.Auditoria.Eventos.Should().ContainSingle().Subject;
        evento.Evento.Should().Be(EventosDeSeguranca.DocumentoAcessoAlterado);
        evento.Entidade.Should().Be("Documento");
        evento.EntidadeId.Should().Be(documento.Id);
        evento.Imediato.Should().BeFalse("o evento é gravado na mesma transação da mudança");
        evento.Dados.Should().BeEquivalentTo(new
        {
            perfisAdicionados = new[] { _estagio.Id },
            perfisRemovidos = new[] { _c.PerfilProducao.Id },
            usuariosAdicionados = new[] { _pedro.Id },
            usuariosRemovidos = Array.Empty<Guid>(),
        });
    }

    [Fact]
    public async Task Definir_o_mesmo_acesso_nao_anota_evento()
    {
        var documento = await Enviado();

        (await DefinirAcesso(_c.Diretor, documento.Id, [_c.PerfilDiretor.Id, _c.PerfilProducao.Id, _estagio.Id], []))
            .Sucesso.Should().BeTrue();

        _c.Auditoria.Eventos.Should().BeEmpty();
    }

    [Fact]
    public async Task Contrato_nao_e_liberado_para_producao_nem_por_perfil_nem_por_usuario()
    {
        var contrato = await Enviado(CategoriaDocumento.ContratoHonorarios, "contrato.pdf");

        var porPerfil = await DefinirAcesso(_c.Diretor, contrato.Id, [_c.PerfilProducao.Id], []);
        var porUsuario = await DefinirAcesso(_c.Diretor, contrato.Id, [], [_pedro.Id]);

        porPerfil.Erro.Should().Be(TipoErro.Validacao);
        porPerfil.Mensagem.Should().Be(Documento.ContratoSoParaDiretoria);
        porUsuario.Erro.Should().Be(TipoErro.Validacao);
        contrato.PerfisComAcesso.Should().BeEmpty();
        contrato.UsuariosComAcesso.Should().BeEmpty();
        _c.Auditoria.Eventos.Should().BeEmpty();
    }

    [Fact]
    public async Task Perfil_ou_usuario_inexistente_e_recusado()
    {
        var documento = await Enviado();

        (await DefinirAcesso(_c.Diretor, documento.Id, [Guid.CreateVersion7()], [])).Mensagem.Should().Be("Perfil não encontrado.");
        (await DefinirAcesso(_c.Diretor, documento.Id, [], [Guid.CreateVersion7()])).Mensagem.Should().Be("Usuário não encontrado.");
        documento.PerfisComAcesso.Should().HaveCount(3, "Diretor, Produção e Estagiário, como no envio");
    }

    [Fact]
    public async Task Producao_nao_define_acesso()
    {
        var documento = await Enviado();

        (await DefinirAcesso(_pedro, documento.Id, [], [_pedro.Id])).Erro.Should().Be(TipoErro.AcessoNegado);
        documento.UsuariosComAcesso.Should().BeEmpty();
    }

    [Fact]
    public async Task Conflito_ao_gravar_a_lista_vira_mensagem_de_recarregar()
    {
        var documento = await Enviado();
        _c.Unidade.Falha = new ExcecaoDeConflito("duplicado");

        var resultado = await DefinirAcesso(_c.Diretor, documento.Id, [_c.PerfilDiretor.Id], []);

        resultado.Erro.Should().Be(TipoErro.Conflito);
        resultado.Mensagem.Should().Be("A lista de acesso foi alterada por outra pessoa. Recarregue e tente novamente.");
    }
}
```

- [ ] **Step 3: Rodar e ver falhar**

Run: `dotnet test tests/AtividadesJuridicas.UnitTests --filter "FullyQualifiedName~DocumentosTests"`
Expected: FAIL na compilação.

- [ ] **Step 4: Portas e ajustes na identidade**

Em `ContextoDeAcesso.cs`:

```csharp
public sealed record UsuarioAutenticado(Guid Id, string Nome, TipoPerfil TipoPerfil, Guid PerfilId)
{
    public bool EhDiretoria => TipoPerfil == TipoPerfil.Diretoria;
}
```

e, no fim de `ObterAsync`: `return new UsuarioAutenticado(usuario.Id, usuario.Nome, perfil.Tipo, perfil.Id);`.

Em `Identidade/Portas.cs`, `IUsuarioRepositorio` ganha (depois de `ListarAsync`):

```csharp
    /// <summary>Os usuários com os Ids informados; os inexistentes simplesmente não vêm.</summary>
    Task<IReadOnlyList<Usuario>> ObterVariosAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct);
```

e `IPerfilRepositorio`:

```csharp
    Task<IReadOnlyList<Perfil>> ObterAtivosAsync(CancellationToken ct);

    /// <summary>Os perfis com os Ids informados; os inexistentes simplesmente não vêm.</summary>
    Task<IReadOnlyList<Perfil>> ObterVariosAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct);
```

Em `Auditoria/IAuditoria.cs`, `EventosDeSeguranca` ganha `public const string DocumentoAcessoAlterado = "documento_acesso_alterado";`.

`src/AtividadesJuridicas.Application/Documentos/PortasDeDocumentos.cs`:

```csharp
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Domain.Documentos;

namespace AtividadesJuridicas.Application.Documentos;

public static class DocumentosOpcoes
{
    public const long TamanhoMaximoBytes = 50L * 1024 * 1024;
    public const int TamanhoDaPagina = 20;
    public const string TipoDeConteudoPadrao = "application/octet-stream";
}

/// <summary>Quem está consultando, quando a consulta precisa respeitar a lista de acesso.</summary>
public sealed record FiltroDeAcesso(Guid UsuarioId, Guid PerfilId);

public interface IDocumentoRepositorio
{
    /// <summary>O documento com as suas listas de acesso.</summary>
    Task<Documento?> ObterPorIdAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Do mais recente para o mais antigo. Sem <paramref name="acesso"/>, todos os documentos; com ele,
    /// só os que o perfil ou o usuário alcançam pela lista de acesso, e nunca um contrato de honorários.
    /// </summary>
    Task<Pagina<DocumentoResumo>> ListarDoClienteAsync(
        Guid clienteId, CategoriaDocumento categoria, FiltroDeAcesso? acesso, int pagina, int tamanho, CancellationToken ct);

    void Adicionar(Documento documento);

    /// <summary>Exclusão lógica do documento e das entradas da lista de acesso; o arquivo permanece.</summary>
    void Remover(Documento documento);
}

/// <summary><paramref name="EnviadoPor"/> é o nome de quem enviou, quando conhecido.</summary>
public sealed record DocumentoResumo(
    Guid Id, string Nome, string NomeArquivoOriginal, long TamanhoBytes, CategoriaDocumento Categoria,
    DateTime EnviadoEm, string? EnviadoPor);

/// <summary>Quem recebe fecha <paramref name="Conteudo"/>.</summary>
public sealed record ArquivoParaBaixar(string NomeArquivo, long TamanhoBytes, Stream Conteudo);

public sealed record AcessoDoDocumento(
    Guid DocumentoId, string Nome, CategoriaDocumento Categoria, IReadOnlyList<Guid> Perfis, IReadOnlyList<Guid> Usuarios);
```

- [ ] **Step 5: Casos de uso**

`EnviarDocumento.cs`:

```csharp
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Comum;
using AtividadesJuridicas.Domain.Documentos;

namespace AtividadesJuridicas.Application.Documentos;

/// <summary>
/// Envia um documento geral ou um contrato de honorários de um cliente. <paramref name="NomeArquivo"/> é o
/// nome que o navegador informou: só o último segmento é guardado, e ele nunca é usado como caminho.
/// </summary>
public sealed record EnviarDocumento(
    CategoriaDocumento Categoria, Guid ClienteId, string? Nome, string NomeArquivo, string? TipoConteudo, Stream Conteudo)
    : IRequisicao<Resultado<Guid>>;

public sealed class EnviarDocumentoManipulador(
    IContextoDeAcesso contexto,
    IDocumentoRepositorio documentos,
    IClienteRepositorio clientes,
    IPerfilRepositorio perfis,
    IArmazenamentoDeArquivos armazenamento,
    IUnidadeDeTrabalho unidade) : IManipulador<EnviarDocumento, Resultado<Guid>>
{
    private const long UmMegabyte = 1024 * 1024;

    public async Task<Resultado<Guid>> ManipularAsync(EnviarDocumento requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Falha(TipoErro.AcessoNegado, "Acesso negado.");

        if (requisicao.Categoria is not (CategoriaDocumento.DocumentoGeral or CategoriaDocumento.ContratoHonorarios))
            return Falha(TipoErro.Validacao, "Categoria inválida para envio.");

        var nomeDoArquivo = SoONome(requisicao.NomeArquivo);
        if (nomeDoArquivo.Length == 0 || requisicao.Conteudo is null)
            return Falha(TipoErro.Validacao, "Informe o arquivo.");

        if (await clientes.ObterPorIdAsync(requisicao.ClienteId, ct) is null)
            return Falha(TipoErro.Validacao, "Cliente não encontrado.");

        ArquivoArmazenado gravado;
        try
        {
            gravado = await armazenamento.GravarAsync(requisicao.Conteudo, DocumentosOpcoes.TamanhoMaximoBytes, ct);
        }
        catch (ExcecaoDeArquivoGrandeDemais)
        {
            return Falha(TipoErro.Validacao,
                $"O arquivo passa do limite de {DocumentosOpcoes.TamanhoMaximoBytes / UmMegabyte} MB.");
        }

        // Daqui em diante o arquivo já está gravado: qualquer saída sem registro no banco o remove.
        try
        {
            var arquivo = new ArquivoDoDocumento(
                nomeDoArquivo, TipoDe(requisicao.TipoConteudo), gravado.TamanhoBytes, gravado.HashSha256, gravado.Chave);

            var documento = requisicao.Categoria == CategoriaDocumento.DocumentoGeral
                ? Documento.CriarGeral(requisicao.ClienteId, requisicao.Nome, arquivo, await perfis.ObterAtivosAsync(ct))
                : Documento.CriarContratoDeHonorarios(requisicao.ClienteId, requisicao.Nome, arquivo);

            documentos.Adicionar(documento);
            await unidade.SalvarAsync(ct);
            return Resultado<Guid>.Ok(documento.Id);
        }
        catch (ExcecaoDeDominio ex)
        {
            await armazenamento.RemoverAsync(gravado.Chave, CancellationToken.None);
            return Falha(TipoErro.Validacao, ex.Message);
        }
        catch
        {
            await armazenamento.RemoverAsync(gravado.Chave, CancellationToken.None);
            throw;
        }
    }

    /// <summary>O último segmento do caminho informado, com qualquer um dos separadores; vazio quando não há nome.</summary>
    private static string SoONome(string? caminho)
    {
        if (string.IsNullOrWhiteSpace(caminho)) return string.Empty;

        var corte = caminho.LastIndexOfAny(['/', '\\']);
        var nome = (corte >= 0 ? caminho[(corte + 1)..] : caminho).Trim();
        return nome is "." or ".." ? string.Empty : nome;
    }

    private static string TipoDe(string? tipo) =>
        string.IsNullOrWhiteSpace(tipo) || tipo.Trim().Length > Documento.TamanhoMaximoTipoConteudo
            ? DocumentosOpcoes.TipoDeConteudoPadrao
            : tipo.Trim();

    private static Resultado<Guid> Falha(TipoErro erro, string mensagem) => Resultado<Guid>.Falha(erro, mensagem);
}
```

`ListarDocumentosDoCliente.cs`:

```csharp
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Documentos;

namespace AtividadesJuridicas.Application.Documentos;

public sealed record ListarDocumentosDoCliente(Guid ClienteId, CategoriaDocumento Categoria, int Pagina = 1)
    : IRequisicao<Resultado<Pagina<DocumentoResumo>>>, IConsultaAuditavel
{
    public string TipoRecurso => "Documento";
    public AcaoDeAcesso Acao => AcaoDeAcesso.Listou;
    public string? Detalhe => DetalheDeConsulta.Serializar(new { ClienteId, Categoria, Pagina });
}

public sealed class ListarDocumentosDoClienteManipulador(IContextoDeAcesso contexto, IDocumentoRepositorio documentos)
    : IManipulador<ListarDocumentosDoCliente, Resultado<Pagina<DocumentoResumo>>>
{
    public async Task<Resultado<Pagina<DocumentoResumo>>> ManipularAsync(ListarDocumentosDoCliente requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { } ator)
            return Falha("Acesso negado.");

        // Contrato de honorários: Produção não tem acesso, nem à lista.
        if (requisicao.Categoria == CategoriaDocumento.ContratoHonorarios && !ator.EhDiretoria)
            return Falha("Acesso negado.");

        var acesso = ator.EhDiretoria ? null : new FiltroDeAcesso(ator.Id, ator.PerfilId);

        return Resultado<Pagina<DocumentoResumo>>.Ok(await documentos.ListarDoClienteAsync(
            requisicao.ClienteId, requisicao.Categoria, acesso,
            Paginacao.Normalizar(requisicao.Pagina, DocumentosOpcoes.TamanhoDaPagina), DocumentosOpcoes.TamanhoDaPagina, ct));
    }

    private static Resultado<Pagina<DocumentoResumo>> Falha(string mensagem) =>
        Resultado<Pagina<DocumentoResumo>>.Falha(TipoErro.AcessoNegado, mensagem);
}
```

`BaixarDocumento.cs`:

```csharp
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;

namespace AtividadesJuridicas.Application.Documentos;

public sealed record BaixarDocumento(Guid Id) : IRequisicao<Resultado<ArquivoParaBaixar>>, IConsultaAuditavel
{
    public string TipoRecurso => "Documento";
    public AcaoDeAcesso Acao => AcaoDeAcesso.Baixou;
    public Guid? RecursoId => Id;
}

public sealed class BaixarDocumentoManipulador(
    IContextoDeAcesso contexto,
    IDocumentoRepositorio documentos,
    IArmazenamentoDeArquivos armazenamento,
    IAuditoria auditoria) : IManipulador<BaixarDocumento, Resultado<ArquivoParaBaixar>>
{
    private const string NaoEncontrado = "Documento não encontrado.";

    public async Task<Resultado<ArquivoParaBaixar>> ManipularAsync(BaixarDocumento requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { } ator)
            return Falha(TipoErro.AcessoNegado, "Acesso negado.");

        var documento = await documentos.ObterPorIdAsync(requisicao.Id, ct);
        if (documento is null) return Falha(TipoErro.NaoEncontrado, NaoEncontrado);

        if (!documento.PodeSerAcessadoPor(ator.Id, ator.PerfilId, ator.TipoPerfil))
        {
            // Para quem pediu, o documento não existe; para a Diretoria, a tentativa fica registrada.
            await auditoria.RegistrarAsync(
                EventosDeSeguranca.AcessoNegado, "Documento", documento.Id,
                new { requisicao = nameof(BaixarDocumento), resposta = "nao_encontrado" }, ct: ct);
            return Falha(TipoErro.NaoEncontrado, NaoEncontrado);
        }

        var conteudo = await armazenamento.AbrirAsync(documento.ChaveArmazenamento, ct);
        return conteudo is null
            ? Falha(TipoErro.NaoEncontrado, "O arquivo deste documento não está no armazenamento.")
            : Resultado<ArquivoParaBaixar>.Ok(new ArquivoParaBaixar(documento.NomeParaBaixar(), documento.TamanhoBytes, conteudo));
    }

    private static Resultado<ArquivoParaBaixar> Falha(TipoErro erro, string mensagem) =>
        Resultado<ArquivoParaBaixar>.Falha(erro, mensagem);
}
```

`RenomearDocumento.cs`:

```csharp
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Application.Documentos;

public sealed record RenomearDocumento(Guid Id, string Nome) : IRequisicao<Resultado>;

public sealed class RenomearDocumentoManipulador(
    IContextoDeAcesso contexto, IDocumentoRepositorio documentos, IUnidadeDeTrabalho unidade)
    : IManipulador<RenomearDocumento, Resultado>
{
    public async Task<Resultado> ManipularAsync(RenomearDocumento requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Resultado.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        var documento = await documentos.ObterPorIdAsync(requisicao.Id, ct);
        if (documento is null) return Resultado.Falha(TipoErro.NaoEncontrado, "Documento não encontrado.");

        try
        {
            documento.Renomear(requisicao.Nome);
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

`ExcluirDocumento.cs`:

```csharp
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;

namespace AtividadesJuridicas.Application.Documentos;

/// <summary>Exclusão lógica. O arquivo permanece no armazenamento.</summary>
public sealed record ExcluirDocumento(Guid Id) : IRequisicao<Resultado>;

public sealed class ExcluirDocumentoManipulador(
    IContextoDeAcesso contexto, IDocumentoRepositorio documentos, IUnidadeDeTrabalho unidade)
    : IManipulador<ExcluirDocumento, Resultado>
{
    public async Task<Resultado> ManipularAsync(ExcluirDocumento requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Resultado.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        var documento = await documentos.ObterPorIdAsync(requisicao.Id, ct);
        if (documento is null) return Resultado.Falha(TipoErro.NaoEncontrado, "Documento não encontrado.");

        documentos.Remover(documento);
        await unidade.SalvarAsync(ct);
        return Resultado.Ok();
    }
}
```

`ObterAcessoDoDocumento.cs`:

```csharp
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;

namespace AtividadesJuridicas.Application.Documentos;

public sealed record ObterAcessoDoDocumento(Guid Id) : IRequisicao<Resultado<AcessoDoDocumento>>, IConsultaAuditavel
{
    public string TipoRecurso => "Documento";
    public AcaoDeAcesso Acao => AcaoDeAcesso.Visualizou;
    public Guid? RecursoId => Id;
}

public sealed class ObterAcessoDoDocumentoManipulador(IContextoDeAcesso contexto, IDocumentoRepositorio documentos)
    : IManipulador<ObterAcessoDoDocumento, Resultado<AcessoDoDocumento>>
{
    public async Task<Resultado<AcessoDoDocumento>> ManipularAsync(ObterAcessoDoDocumento requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Resultado<AcessoDoDocumento>.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        var documento = await documentos.ObterPorIdAsync(requisicao.Id, ct);
        return documento is null
            ? Resultado<AcessoDoDocumento>.Falha(TipoErro.NaoEncontrado, "Documento não encontrado.")
            : Resultado<AcessoDoDocumento>.Ok(new AcessoDoDocumento(
                documento.Id, documento.Nome, documento.Categoria,
                [.. documento.PerfisComAcesso.Select(a => a.PerfilId)],
                [.. documento.UsuariosComAcesso.Select(a => a.UsuarioId)]));
    }
}
```

`DefinirAcessoDoDocumento.cs`:

```csharp
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Comum;
using AtividadesJuridicas.Domain.Documentos;

namespace AtividadesJuridicas.Application.Documentos;

/// <summary>Deixa a lista de acesso do documento com exatamente os perfis e usuários informados.</summary>
public sealed record DefinirAcessoDoDocumento(Guid Id, IReadOnlyList<Guid> Perfis, IReadOnlyList<Guid> Usuarios)
    : IRequisicao<Resultado>;

public sealed class DefinirAcessoDoDocumentoManipulador(
    IContextoDeAcesso contexto,
    IDocumentoRepositorio documentos,
    IPerfilRepositorio perfis,
    IUsuarioRepositorio usuarios,
    IUnidadeDeTrabalho unidade,
    IAuditoria auditoria) : IManipulador<DefinirAcessoDoDocumento, Resultado>
{
    public async Task<Resultado> ManipularAsync(DefinirAcessoDoDocumento requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Resultado.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        var documento = await documentos.ObterPorIdAsync(requisicao.Id, ct);
        if (documento is null) return Resultado.Falha(TipoErro.NaoEncontrado, "Documento não encontrado.");

        var idsDePerfis = (requisicao.Perfis ?? []).Distinct().ToList();
        var perfisPedidos = await perfis.ObterVariosAsync(idsDePerfis, ct);
        if (perfisPedidos.Count != idsDePerfis.Count)
            return Resultado.Falha(TipoErro.Validacao, "Perfil não encontrado.");

        var idsDeUsuarios = (requisicao.Usuarios ?? []).Distinct().ToList();
        var usuariosPedidos = await usuarios.ObterVariosAsync(idsDeUsuarios, ct);
        if (usuariosPedidos.Count != idsDeUsuarios.Count)
            return Resultado.Falha(TipoErro.Validacao, "Usuário não encontrado.");

        // O tipo do perfil de cada usuário decide a regra do contrato de honorários.
        var perfisDosUsuarios = (await perfis.ObterVariosAsync([.. usuariosPedidos.Select(u => u.PerfilId).Distinct()], ct))
            .ToDictionary(p => p.Id);
        if (usuariosPedidos.Any(u => !perfisDosUsuarios.ContainsKey(u.PerfilId)))
            return Resultado.Falha(TipoErro.Validacao, "Perfil não encontrado.");

        try
        {
            var alteracao = documento.DefinirAcesso(
                perfisPedidos, [.. usuariosPedidos.Select(u => new UsuarioComPerfil(u, perfisDosUsuarios[u.PerfilId]))]);
            if (!alteracao.Houve) return Resultado.Ok();

            auditoria.Anotar(EventosDeSeguranca.DocumentoAcessoAlterado, "Documento", documento.Id, new
            {
                perfisAdicionados = alteracao.PerfisAdicionados,
                perfisRemovidos = alteracao.PerfisRemovidos,
                usuariosAdicionados = alteracao.UsuariosAdicionados,
                usuariosRemovidos = alteracao.UsuariosRemovidos,
            });

            await unidade.SalvarAsync(ct);
            return Resultado.Ok();
        }
        catch (ExcecaoDeDominio ex)
        {
            return Resultado.Falha(TipoErro.Validacao, ex.Message);
        }
        catch (ExcecaoDeConflito)
        {
            return Resultado.Falha(TipoErro.Conflito,
                "A lista de acesso foi alterada por outra pessoa. Recarregue e tente novamente.");
        }
    }
}
```

- [ ] **Step 6: Rodar os unitários**

Run: `dotnet test tests/AtividadesJuridicas.UnitTests`
Expected: PASS, todos; `RegistroDeManipuladoresTests` verde (cada requisição nova tem um manipulador). A solução inteira ainda não compila na Infrastructure (as portas ganharam métodos): é a Parte B.

#### Parte B — Persistência

- [ ] **Step 7: Testes de integração**

`tests/AtividadesJuridicas.IntegrationTests/DocumentosTests.cs`:

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
using Npgsql;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class DocumentosTests(BancoFixture banco) : TesteComBanco(banco)
{
    private Guid _admin, _maria, _jose, _perfilDiretor, _perfilProducao, _pedro;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _admin = await IdDoAdminAsync();
        _perfilDiretor = await IdDoPerfilAsync(TipoPerfil.Diretoria);
        _perfilProducao = await IdDoPerfilAsync(TipoPerfil.Producao);
        _maria = await PfAsync("Maria Silva", "529.982.247-25");
        _jose = await PfAsync("José Souza", "111.444.777-35");
        _pedro = (await ExecutarAsync(_admin,
            new CriarUsuario("Pedro", "pedro@x.com", "senha-do-pedro", _perfilProducao, null))).Valor;
    }

    private async Task<Guid> PfAsync(string nome, string cpf) =>
        (await ExecutarAsync(_admin, new SalvarClientePessoaFisica(
            null, new DadosDoClienteEntrada(nome, null, null, new EnderecoEntrada(), null), cpf, null, null))).Valor;

    private async Task<Guid> EnviarAsync(CategoriaDocumento categoria = CategoriaDocumento.DocumentoGeral,
        string arquivo = "procuracao.pdf", string conteudo = "conteúdo do arquivo", Guid? cliente = null, string? nome = null)
    {
        var resultado = await ExecutarAsync(_admin, new EnviarDocumento(
            categoria, cliente ?? _maria, nome, arquivo, "application/pdf", new MemoryStream(Encoding.UTF8.GetBytes(conteudo))));
        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        return resultado.Valor;
    }

    private async Task<string?> BaixarTextoAsync(Guid usuario, Guid documento)
    {
        var resultado = await ExecutarAsync(usuario, new BaixarDocumento(documento));
        if (resultado.Valor is not { } arquivo) return null;

        await using var conteudo = arquivo.Conteudo;
        using var leitor = new StreamReader(conteudo, Encoding.UTF8);
        return await leitor.ReadToEndAsync();
    }

    private Task<Documento> NoBancoAsync(Guid id) => EmEscopoAsync(null, sp =>
        sp.GetRequiredService<AppDbContext>().Documentos.IgnoreQueryFilters().AsNoTracking()
            .Include(d => d.PerfisComAcesso).Include(d => d.UsuariosComAcesso).SingleAsync(d => d.Id == id));

    private Task<List<DocumentoPerfilAcesso>> EntradasDePerfilAsync(Guid documento) => EmEscopoAsync(null, sp =>
        sp.GetRequiredService<AppDbContext>().Set<DocumentoPerfilAcesso>().IgnoreQueryFilters().AsNoTracking()
            .Where(a => a.DocumentoId == documento).OrderBy(a => a.CriadoEm).ThenBy(a => a.Id).ToListAsync());

    private Task<List<RegistroAcesso>> AcessosAsync() => EmEscopoAsync(null, sp =>
        sp.GetRequiredService<AppDbContext>().RegistrosAcesso.AsNoTracking().OrderBy(r => r.Id).ToListAsync());

    private string[] ArquivosNoDisco() =>
        Directory.Exists(PastaDeArquivos) ? Directory.GetFiles(PastaDeArquivos, "*", SearchOption.AllDirectories) : [];

    private async Task<IReadOnlyList<Guid>> IdsListadosAsync(Guid usuario, CategoriaDocumento categoria = CategoriaDocumento.DocumentoGeral) =>
        [.. (await ExecutarAsync(usuario, new ListarDocumentosDoCliente(_maria, categoria))).Valor!.Itens.Select(d => d.Id)];

    [Fact]
    public async Task Documento_enviado_vai_para_o_disco_e_os_metadados_para_o_banco()
    {
        var id = await EnviarAsync(conteudo: "abc", nome: "Procuração assinada");

        var documento = await NoBancoAsync(id);
        documento.Should().BeEquivalentTo(new
        {
            Nome = "Procuração assinada", NomeArquivoOriginal = "procuracao.pdf", TipoConteudo = "application/pdf",
            TamanhoBytes = 3L, HashSha256 = "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad",
            Categoria = CategoriaDocumento.DocumentoGeral, ClienteId = (Guid?)_maria, CriadoPor = (Guid?)_admin,
        });
        documento.PerfisComAcesso.Select(a => a.PerfilId).Should().BeEquivalentTo([_perfilDiretor, _perfilProducao]);
        ArquivosNoDisco().Should().ContainSingle();
        (await BaixarTextoAsync(_admin, id)).Should().Be("abc");
    }

    [Fact]
    public async Task Envio_gera_registros_de_criacao_do_documento_e_de_cada_entrada_da_lista()
    {
        var id = await EnviarAsync();

        var registros = (await AuditoriaAsync()).Where(r => r.Entidade is "Documento" or "DocumentoPerfilAcesso").ToList();

        var doDocumento = registros.Should().ContainSingle(r => r.Entidade == "Documento").Subject;
        doDocumento.Operacao.Should().Be(OperacaoAuditada.Criacao);
        doDocumento.EntidadeId.Should().Be(id);
        doDocumento.UsuarioId.Should().Be(_admin);
        using var dados = JsonDocument.Parse(doDocumento.Dados!);
        dados.RootElement.GetProperty("Nome").GetString().Should().Be("procuracao.pdf");
        dados.RootElement.GetProperty("Categoria").GetString().Should().Be("DocumentoGeral");
        dados.RootElement.GetProperty("ClienteId").GetGuid().Should().Be(_maria);
        registros.Where(r => r.Entidade == "DocumentoPerfilAcesso").Should().HaveCount(2)
            .And.OnlyContain(r => r.Operacao == OperacaoAuditada.Criacao && r.IdCorrelacao == doDocumento.IdCorrelacao);
    }

    [Fact]
    public async Task Listagem_respeita_a_lista_de_acesso_no_banco()
    {
        var liberado = await EnviarAsync(arquivo: "liberado.pdf");
        var restrito = await EnviarAsync(arquivo: "restrito.pdf");
        var doPedro = await EnviarAsync(arquivo: "do-pedro.pdf");
        var deOutroCliente = await EnviarAsync(arquivo: "outro.pdf", cliente: _jose);
        (await ExecutarAsync(_admin, new DefinirAcessoDoDocumento(restrito, [_perfilDiretor], []))).Sucesso.Should().BeTrue();
        (await ExecutarAsync(_admin, new DefinirAcessoDoDocumento(doPedro, [], [_pedro]))).Sucesso.Should().BeTrue();

        (await IdsListadosAsync(_admin)).Should().BeEquivalentTo([liberado, restrito, doPedro]);
        (await IdsListadosAsync(_pedro)).Should().BeEquivalentTo([liberado, doPedro]);
        (await IdsListadosAsync(_pedro)).Should().NotContain(deOutroCliente);
    }

    [Fact]
    public async Task Listagem_vem_do_mais_recente_para_o_mais_antigo_com_quem_enviou()
    {
        var primeiro = await EnviarAsync(arquivo: "primeiro.pdf");
        var segundo = await EnviarAsync(arquivo: "segundo.pdf");

        var pagina = (await ExecutarAsync(_admin, new ListarDocumentosDoCliente(_maria, CategoriaDocumento.DocumentoGeral))).Valor!;

        pagina.Total.Should().Be(2);
        pagina.Itens.Select(d => d.Id).Should().Equal(segundo, primeiro);
        pagina.Itens.Should().OnlyContain(d => d.EnviadoPor == "Diana Diretora" && d.EnviadoEm.Kind == DateTimeKind.Utc);
    }

    [Fact]
    public async Task Retirar_o_perfil_tira_o_documento_da_producao_e_devolver_o_perfil_o_traz_de_volta()
    {
        var id = await EnviarAsync();

        (await ExecutarAsync(_admin, new DefinirAcessoDoDocumento(id, [_perfilDiretor], []))).Sucesso.Should().BeTrue();
        (await IdsListadosAsync(_pedro)).Should().BeEmpty();
        (await BaixarTextoAsync(_pedro, id)).Should().BeNull();

        (await ExecutarAsync(_admin, new DefinirAcessoDoDocumento(id, [_perfilDiretor, _perfilProducao], []))).Sucesso.Should().BeTrue();
        (await IdsListadosAsync(_pedro)).Should().Equal(id);
        (await BaixarTextoAsync(_pedro, id)).Should().Be("conteúdo do arquivo");

        // A entrada antiga do perfil continua no banco, excluída logicamente; a nova é outra linha.
        var entradas = (await EntradasDePerfilAsync(id)).Where(a => a.PerfilId == _perfilProducao).ToList();
        entradas.Should().HaveCount(2);
        entradas.Count(a => a.ExcluidoEm is not null).Should().Be(1);
    }

    [Fact]
    public async Task Mudanca_na_lista_de_acesso_grava_o_evento_e_os_registros_na_mesma_transacao()
    {
        var id = await EnviarAsync();

        (await ExecutarAsync(_admin, new DefinirAcessoDoDocumento(id, [_perfilDiretor], [_pedro]))).Sucesso.Should().BeTrue();

        var registros = await AuditoriaAsync();
        var evento = registros.Should().ContainSingle(r => r.Evento == EventosDeSeguranca.DocumentoAcessoAlterado).Subject;
        evento.Entidade.Should().Be("Documento");
        evento.EntidadeId.Should().Be(id);
        evento.UsuarioId.Should().Be(_admin);
        using var dados = JsonDocument.Parse(evento.Dados!);
        dados.RootElement.GetProperty("perfisRemovidos").EnumerateArray().Select(e => e.GetGuid()).Should().Equal(_perfilProducao);
        dados.RootElement.GetProperty("usuariosAdicionados").EnumerateArray().Select(e => e.GetGuid()).Should().Equal(_pedro);
        registros.Should().ContainSingle(r =>
            r.Entidade == "DocumentoPerfilAcesso" && r.Operacao == OperacaoAuditada.Exclusao && r.IdCorrelacao == evento.IdCorrelacao);
        registros.Should().ContainSingle(r =>
            r.Entidade == "DocumentoUsuarioAcesso" && r.Operacao == OperacaoAuditada.Criacao && r.IdCorrelacao == evento.IdCorrelacao);
    }

    [Fact]
    public async Task Contrato_de_honorarios_e_invisivel_para_a_producao()
    {
        var contrato = await EnviarAsync(CategoriaDocumento.ContratoHonorarios, "contrato.pdf");

        var listagem = await ExecutarAsync(_pedro, new ListarDocumentosDoCliente(_maria, CategoriaDocumento.ContratoHonorarios));
        var download = await ExecutarAsync(_pedro, new BaixarDocumento(contrato));
        var liberar = await ExecutarAsync(_admin, new DefinirAcessoDoDocumento(contrato, [_perfilProducao], [_pedro]));

        listagem.Erro.Should().Be(TipoErro.AcessoNegado);
        download.Erro.Should().Be(TipoErro.NaoEncontrado);
        liberar.Erro.Should().Be(TipoErro.Validacao);
        (await NoBancoAsync(contrato)).PerfisComAcesso.Should().BeEmpty();
        (await IdsListadosAsync(_admin, CategoriaDocumento.ContratoHonorarios)).Should().Equal(contrato);
        (await BaixarTextoAsync(_admin, contrato)).Should().Be("conteúdo do arquivo");

        // As duas tentativas do Pedro ficam registradas: a listagem (negada) e o download (respondido como não encontrado).
        var negados = (await AuditoriaAsync()).Where(r => r.Evento == EventosDeSeguranca.AcessoNegado).ToList();
        negados.Should().HaveCount(2).And.OnlyContain(r => r.UsuarioId == _pedro);
        negados.Should().ContainSingle(r => r.Entidade == "Documento" && r.EntidadeId == contrato);
    }

    [Fact]
    public async Task Listar_baixar_e_ver_a_lista_de_acesso_ficam_no_registro_de_consultas()
    {
        var id = await EnviarAsync();

        await ExecutarAsync(_pedro, new ListarDocumentosDoCliente(_maria, CategoriaDocumento.DocumentoGeral));
        (await BaixarTextoAsync(_pedro, id)).Should().NotBeNull();
        await ExecutarAsync(_admin, new ObterAcessoDoDocumento(id));

        var acessos = (await AcessosAsync()).Where(a => a.TipoRecurso == "Documento").ToList();
        acessos.Select(a => (a.UsuarioId, a.Acao, a.RecursoId)).Should().Equal(
            (_pedro, AcaoDeAcesso.Listou, (Guid?)null),
            (_pedro, AcaoDeAcesso.Baixou, (Guid?)id),
            (_admin, AcaoDeAcesso.Visualizou, (Guid?)id));
        acessos[0].Detalhe.Should().Contain(_maria.ToString());
    }

    [Fact]
    public async Task Download_negado_nao_entra_no_registro_de_consultas()
    {
        var id = await EnviarAsync();
        (await ExecutarAsync(_admin, new DefinirAcessoDoDocumento(id, [_perfilDiretor], []))).Sucesso.Should().BeTrue();

        (await BaixarTextoAsync(_pedro, id)).Should().BeNull();

        (await AcessosAsync()).Should().NotContain(a => a.Acao == AcaoDeAcesso.Baixou);
    }

    [Fact]
    public async Task Excluir_e_logico_para_o_documento_e_para_a_lista_e_o_arquivo_fica_no_disco()
    {
        var id = await EnviarAsync();

        (await ExecutarAsync(_admin, new ExcluirDocumento(id))).Sucesso.Should().BeTrue();

        var documento = await NoBancoAsync(id);
        documento.ExcluidoEm.Should().NotBeNull();
        documento.ExcluidoPor.Should().Be(_admin);
        (await EntradasDePerfilAsync(id)).Should().HaveCount(2).And.OnlyContain(a => a.ExcluidoEm != null);
        (await IdsListadosAsync(_admin)).Should().BeEmpty();
        (await ExecutarAsync(_admin, new BaixarDocumento(id))).Erro.Should().Be(TipoErro.NaoEncontrado);
        ArquivosNoDisco().Should().ContainSingle("o arquivo de um documento excluído permanece no armazenamento");
        (await AuditoriaAsync()).Where(r => r.Operacao == OperacaoAuditada.Exclusao)
            .Select(r => r.Entidade).Should().BeEquivalentTo(["Documento", "DocumentoPerfilAcesso", "DocumentoPerfilAcesso"]);
    }

    [Fact]
    public async Task Renomear_gera_registro_de_alteracao_so_do_nome()
    {
        var id = await EnviarAsync();

        (await ExecutarAsync(_admin, new RenomearDocumento(id, "Procuração 2026"))).Sucesso.Should().BeTrue();

        var alteracao = (await AuditoriaAsync()).Should()
            .ContainSingle(r => r.Entidade == "Documento" && r.Operacao == OperacaoAuditada.Alteracao).Subject;
        using var dados = JsonDocument.Parse(alteracao.Dados!);
        dados.RootElement.EnumerateObject().Select(p => p.Name).Should().Equal("Nome");
        dados.RootElement.GetProperty("Nome").GetProperty("para").GetString().Should().Be("Procuração 2026");
        (await NoBancoAsync(id)).NomeArquivoOriginal.Should().Be("procuracao.pdf");
    }

    [Fact]
    public async Task Envio_recusado_nao_deixa_arquivo_no_disco_nem_registro_no_banco()
    {
        var vazio = await ExecutarAsync(_admin, new EnviarDocumento(
            CategoriaDocumento.DocumentoGeral, _maria, null, "vazio.pdf", "application/pdf", new MemoryStream()));
        var semCliente = await ExecutarAsync(_admin, new EnviarDocumento(
            CategoriaDocumento.DocumentoGeral, Guid.CreateVersion7(), null, "x.pdf", "application/pdf", new MemoryStream([1, 2, 3])));
        var doPedro = await ExecutarAsync(_pedro, new EnviarDocumento(
            CategoriaDocumento.DocumentoGeral, _maria, null, "x.pdf", "application/pdf", new MemoryStream([1, 2, 3])));

        vazio.Erro.Should().Be(TipoErro.Validacao);
        semCliente.Erro.Should().Be(TipoErro.Validacao);
        doPedro.Erro.Should().Be(TipoErro.AcessoNegado);
        ArquivosNoDisco().Should().BeEmpty();
        (await IdsListadosAsync(_admin)).Should().BeEmpty();
    }

    [Fact]
    public async Task Banco_recusa_apagar_fisicamente_um_documento_com_lista_de_acesso()
    {
        var id = await EnviarAsync();

        var acao = () => EmEscopoAsync(null, sp =>
            sp.GetRequiredService<AppDbContext>().Database.ExecuteSqlAsync($"DELETE FROM documentos WHERE id = {id}"));

        var erro = (await acao.Should().ThrowAsync<PostgresException>()).Which;
        erro.SqlState.Should().Be(PostgresErrorCodes.ForeignKeyViolation);
        (await EntradasDePerfilAsync(id)).Should().HaveCount(2);
    }

    [Fact]
    public async Task Banco_recusa_duas_entradas_vivas_do_mesmo_perfil_no_mesmo_documento()
    {
        var id = await EnviarAsync();
        var novoId = Guid.CreateVersion7();
        var agora = DateTime.UtcNow;

        var acao = () => EmEscopoAsync(null, sp => sp.GetRequiredService<AppDbContext>().Database.ExecuteSqlAsync(
            $"INSERT INTO documentos_perfis_acesso (id, documento_id, perfil_id, criado_em) VALUES ({novoId}, {id}, {_perfilProducao}, {agora})"));

        (await acao.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
    }
}
```

Se `ExecuteSqlAsync` embrulhar a `PostgresException` em outra exceção, siga o que `ProcessosTests` faz (o auxiliar `PostgresDe`, que percorre as `InnerException`): a asserção exigida é o `SqlState`.

Em `PropriedadesAuditadasTests.cs`, acrescente à lista esperada (em ordem alfabética das chaves):

```csharp
            ["Documento"] =
            [
                "Categoria", "ChaveArmazenamento", "ClienteId", "HashSha256", "Nome", "NomeArquivoOriginal",
                "TamanhoBytes", "TipoConteudo",
            ],
            ["DocumentoPerfilAcesso"] = ["DocumentoId", "PerfilId"],
            ["DocumentoUsuarioAcesso"] = ["DocumentoId", "UsuarioId"],
```

- [ ] **Step 8: Mapeamento**

`src/AtividadesJuridicas.Infrastructure/Persistencia/Configuracoes/DocumentoConfiguracao.cs`:

```csharp
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Documentos;
using AtividadesJuridicas.Domain.Identidade;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AtividadesJuridicas.Infrastructure.Persistencia.Configuracoes;

public sealed class DocumentoConfiguracao : IEntityTypeConfiguration<Documento>
{
    public void Configure(EntityTypeBuilder<Documento> b)
    {
        b.ToTable("documentos");

        b.Property(d => d.Nome).HasMaxLength(Documento.TamanhoMaximoNome).IsRequired();
        b.Property(d => d.NomeArquivoOriginal).HasMaxLength(Documento.TamanhoMaximoNome).IsRequired();
        b.Property(d => d.TipoConteudo).HasMaxLength(Documento.TamanhoMaximoTipoConteudo).IsRequired();
        b.Property(d => d.HashSha256).HasMaxLength(64).IsFixedLength().IsRequired();
        b.Property(d => d.ChaveArmazenamento).HasMaxLength(Documento.TamanhoMaximoChave).IsRequired();
        b.Property(d => d.Categoria).HasConversion<int>();

        // Cada arquivo do armazenamento pertence a um único documento, mesmo depois de excluído.
        b.HasIndex(d => d.ChaveArmazenamento).IsUnique();
        b.HasIndex(d => new { d.ClienteId, d.Categoria });

        b.HasOne<Cliente>().WithMany().HasForeignKey(d => d.ClienteId).OnDelete(DeleteBehavior.Restrict);

        // As listas são gravadas pelos campos privados. ClientCascade (sem cascata no banco) faz o EF marcar
        // como excluída a entrada retirada da coleção (ou a de um documento excluído); o interceptor de
        // carimbos converte essa exclusão em exclusão lógica.
        b.HasMany(d => d.PerfisComAcesso).WithOne().HasForeignKey(a => a.DocumentoId).OnDelete(DeleteBehavior.ClientCascade);
        b.Navigation(d => d.PerfisComAcesso).HasField("_perfisComAcesso").UsePropertyAccessMode(PropertyAccessMode.Field);

        b.HasMany(d => d.UsuariosComAcesso).WithOne().HasForeignKey(a => a.DocumentoId).OnDelete(DeleteBehavior.ClientCascade);
        b.Navigation(d => d.UsuariosComAcesso).HasField("_usuariosComAcesso").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class DocumentoPerfilAcessoConfiguracao : IEntityTypeConfiguration<DocumentoPerfilAcesso>
{
    public void Configure(EntityTypeBuilder<DocumentoPerfilAcesso> b)
    {
        b.ToTable("documentos_perfis_acesso");

        b.HasOne<Perfil>().WithMany().HasForeignKey(a => a.PerfilId).OnDelete(DeleteBehavior.Restrict);

        // Um perfil não se repete entre as entradas vivas do mesmo documento.
        b.HasIndex(a => new { a.DocumentoId, a.PerfilId }).IsUnique().HasFilter("excluido_em IS NULL");
        b.HasIndex(a => a.PerfilId);
    }
}

public sealed class DocumentoUsuarioAcessoConfiguracao : IEntityTypeConfiguration<DocumentoUsuarioAcesso>
{
    public void Configure(EntityTypeBuilder<DocumentoUsuarioAcesso> b)
    {
        b.ToTable("documentos_usuarios_acesso");

        b.HasOne<Usuario>().WithMany().HasForeignKey(a => a.UsuarioId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(a => new { a.DocumentoId, a.UsuarioId }).IsUnique().HasFilter("excluido_em IS NULL");
        b.HasIndex(a => a.UsuarioId);
    }
}
```

Em `AppDbContext.cs`: `public DbSet<Documento> Documentos => Set<Documento>();` (com o `using AtividadesJuridicas.Domain.Documentos;`).

- [ ] **Step 9: Repositórios e composição**

`src/AtividadesJuridicas.Infrastructure/Persistencia/Repositorios/DocumentoRepositorio.cs`:

```csharp
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Documentos;
using AtividadesJuridicas.Domain.Documentos;
using Microsoft.EntityFrameworkCore;

namespace AtividadesJuridicas.Infrastructure.Persistencia.Repositorios;

public sealed class DocumentoRepositorio(AppDbContext db) : IDocumentoRepositorio
{
    public Task<Documento?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        db.Documentos
            .Include(d => d.PerfisComAcesso)
            .Include(d => d.UsuariosComAcesso)
            .FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task<Pagina<DocumentoResumo>> ListarDoClienteAsync(
        Guid clienteId, CategoriaDocumento categoria, FiltroDeAcesso? acesso, int pagina, int tamanho, CancellationToken ct)
    {
        var consulta = db.Documentos.AsNoTracking().Where(d => d.ClienteId == clienteId && d.Categoria == categoria);

        if (acesso is not null)
        {
            // A mesma regra de Documento.PodeSerAcessadoPor para quem não é da Diretoria, aplicada no banco.
            var perfilId = acesso.PerfilId;
            var usuarioId = acesso.UsuarioId;
            consulta = consulta.Where(d =>
                d.Categoria != CategoriaDocumento.ContratoHonorarios &&
                (d.PerfisComAcesso.Any(a => a.PerfilId == perfilId) ||
                 d.UsuariosComAcesso.Any(a => a.UsuarioId == usuarioId)));
        }

        var total = await consulta.CountAsync(ct);
        var documentos = await consulta
            .OrderByDescending(d => d.CriadoEm).ThenByDescending(d => d.Id)
            .Skip((pagina - 1) * tamanho).Take(tamanho)
            .ToListAsync(ct);

        // Quem enviou continua identificado pelo nome mesmo depois de excluído.
        var autores = documentos.Where(d => d.CriadoPor.HasValue).Select(d => d.CriadoPor!.Value).Distinct().ToList();
        var nomes = await db.Usuarios.IgnoreQueryFilters().AsNoTracking()
            .Where(u => autores.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Nome, ct);

        var itens = documentos.Select(d => new DocumentoResumo(
            d.Id, d.Nome, d.NomeArquivoOriginal, d.TamanhoBytes, d.Categoria, d.CriadoEm,
            d.CriadoPor is { } autor ? nomes.GetValueOrDefault(autor) : null)).ToList();

        return new Pagina<DocumentoResumo>(itens, total, pagina, tamanho);
    }

    public void Adicionar(Documento documento) => db.Documentos.Add(documento);

    public void Remover(Documento documento) => db.Documentos.Remove(documento);
}
```

`PerfilRepositorio` ganha:

```csharp
    public async Task<IReadOnlyList<Perfil>> ObterAtivosAsync(CancellationToken ct) =>
        await db.Perfis.Where(p => p.Ativo).OrderBy(p => p.Nome).ToListAsync(ct);

    public async Task<IReadOnlyList<Perfil>> ObterVariosAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct) =>
        await db.Perfis.Where(p => ids.Contains(p.Id)).ToListAsync(ct);
```

`UsuarioRepositorio` ganha:

```csharp
    public async Task<IReadOnlyList<Usuario>> ObterVariosAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct) =>
        await db.Usuarios.Where(u => ids.Contains(u.Id)).ToListAsync(ct);
```

Em `DependencyInjection.cs` da Infrastructure: `servicos.AddScoped<IDocumentoRepositorio, DocumentoRepositorio>();` (ao lado dos outros repositórios).

- [ ] **Step 10: Migration**

```bash
dotnet ef migrations add Documentos --project src/AtividadesJuridicas.Infrastructure --startup-project src/AtividadesJuridicas.Infrastructure --output-dir Persistencia/Migrations
```

Expected: cria só as tabelas `documentos`, `documentos_perfis_acesso` e `documentos_usuarios_acesso`. Confira e registre no relatório: `hash_sha256` `character(64)`; `tamanho_bytes` `bigint`; `cliente_id` `uuid` anulável; índice único em `chave_armazenamento` (sem filtro); índices únicos das duas listas em `(documento_id, perfil_id)` e `(documento_id, usuario_id)` filtrados por `excluido_em IS NULL`; as FKs `documento_id` das listas **sem** `onDelete: Cascade` (NO ACTION/RESTRICT); FKs para `clientes`, `perfis` e `usuarios` com `Restrict`. Nenhuma alteração em tabelas existentes. Se a migration contiver qualquer outra coisa, pare e relate.

- [ ] **Step 11: Rodar tudo**

Run: `dotnet test` (suíte inteira, da raiz)
Expected: PASS, tudo, sem avisos. `GravacoesPorForaTests` verde (nenhum SQL bruto em `src/`). `PropriedadesAuditadasTests` verde com as três entidades novas.

- [ ] **Step 12: Commit**

```bash
git add src tests
git commit -m "feat(documentos): envio, listagem, download, lista de acesso e persistência dos documentos de cliente"
```

---

### Task 4: Download por HTTP e documentos na ficha do cliente

**Files:**
- Create: `src/AtividadesJuridicas.Web/Infra/DocumentosEndpoints.cs`
- Create: `src/AtividadesJuridicas.Web/Components/Documentos/DocumentosDoCliente.razor`
- Modify: `src/AtividadesJuridicas.Web/Program.cs` (`app.MapDocumentos();`)
- Modify: `src/AtividadesJuridicas.Web/Components/_Imports.razor`
- Modify: `src/AtividadesJuridicas.Web/Components/Pages/Clientes.razor`
- Test: `tests/AtividadesJuridicas.IntegrationTests/DocumentosWebTests.cs`

**Interfaces:**
- Consumes: `BaixarDocumento`, `ListarDocumentosDoCliente`, `EnviarDocumento`, `RenomearDocumento`, `ExcluirDocumento`, `ObterAcessoDoDocumento`, `DefinirAcessoDoDocumento`, `DocumentosOpcoes`; `ListarPerfisAtivos`, `ListarUsuarios` (ambos só da Diretoria); `ExecutorDeRequisicoes`; `IdentidadeHttp.Definir`; `FusoDoEscritorio.Formatar`; `AplicacaoDeTeste` (`ExecutarAsync`, `ClienteAutenticadoAsync`, `CriarPedroAsync`, `EventosAsync`, `IpDeTeste`).
- Produces: rota `GET /documentos/{id:guid}/baixar`; componente `DocumentosDoCliente` com os parâmetros `ClienteId` (`Guid`), `Categoria` (`CategoriaDocumento`), `Titulo` (`string`), `PodeGerenciar` (`bool`), `OnAcessoEncerrado` (`EventCallback`).

- [ ] **Step 1: Testes do endpoint**

`tests/AtividadesJuridicas.IntegrationTests/DocumentosWebTests.cs`:

```csharp
using System.Net;
using System.Text;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Documentos;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Documentos;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class DocumentosWebTests(BancoFixture banco) : IAsyncLifetime
{
    private const string Conteudo = "conteúdo do arquivo, com acentuação";

    private readonly AplicacaoDeTeste _app = new(banco);
    private Guid _admin, _pedro, _maria, _geral, _contrato;

    public async Task InitializeAsync()
    {
        _admin = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        _pedro = await _app.CriarPedroAsync();
        _maria = (await _app.ExecutarAsync(_admin, new SalvarClientePessoaFisica(
            null, new DadosDoClienteEntrada("Maria Silva", null, null, new EnderecoEntrada(), null), "529.982.247-25", null, null))).Valor;
        _geral = await EnviarAsync(CategoriaDocumento.DocumentoGeral, "Procuração João.pdf");
        _contrato = await EnviarAsync(CategoriaDocumento.ContratoHonorarios, "contrato.pdf");
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    private async Task<Guid> EnviarAsync(CategoriaDocumento categoria, string arquivo)
    {
        var resultado = await _app.ExecutarAsync(_admin, new EnviarDocumento(
            categoria, _maria, null, arquivo, "text/html", new MemoryStream(Encoding.UTF8.GetBytes(Conteudo))));
        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        return resultado.Valor;
    }

    private Task<HttpClient> DoPedroAsync() =>
        _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

    private Task<List<RegistroAcesso>> DownloadsAsync() => _app.EmEscopoAsync(null, sp =>
        sp.GetRequiredService<AppDbContext>().RegistrosAcesso.AsNoTracking()
            .Where(r => r.Acao == AcaoDeAcesso.Baixou).OrderBy(r => r.Id).ToListAsync());

    [Fact]
    public async Task Diretor_baixa_o_arquivo_como_anexo_com_o_nome_certo()
    {
        var cliente = await _app.ClienteAutenticadoAsync();

        var resposta = await cliente.GetAsync($"/documentos/{_geral}/baixar");

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        (await resposta.Content.ReadAsStringAsync()).Should().Be(Conteudo);
        // Nunca o tipo enviado pelo usuário (aqui, text/html): o navegador não interpreta o conteúdo.
        resposta.Content.Headers.ContentType!.MediaType.Should().Be("application/octet-stream");
        resposta.Content.Headers.ContentDisposition!.DispositionType.Should().Be("attachment");
        resposta.Content.Headers.ContentDisposition.FileNameStar.Should().Be("Procuração João.pdf");
        resposta.Headers.GetValues("X-Content-Type-Options").Should().Equal("nosniff");
        resposta.Headers.CacheControl!.NoStore.Should().BeTrue();
    }

    [Fact]
    public async Task Download_fica_no_registro_de_consultas_com_usuario_e_ip()
    {
        var cliente = await DoPedroAsync();

        (await cliente.GetAsync($"/documentos/{_geral}/baixar")).StatusCode.Should().Be(HttpStatusCode.OK);

        var registro = (await DownloadsAsync()).Should().ContainSingle().Subject;
        registro.UsuarioId.Should().Be(_pedro);
        registro.TipoRecurso.Should().Be("Documento");
        registro.RecursoId.Should().Be(_geral);
        registro.Ip.Should().Be(AplicacaoDeTeste.IpDeTeste);
    }

    [Fact]
    public async Task Producao_sem_acesso_recebe_404_e_a_tentativa_fica_registrada()
    {
        var diretor = await _app.IdDoPerfilDiretorAsync();
        (await _app.ExecutarAsync(_admin, new DefinirAcessoDoDocumento(_geral, [diretor], []))).Sucesso.Should().BeTrue();
        var cliente = await DoPedroAsync();

        var resposta = await cliente.GetAsync($"/documentos/{_geral}/baixar");

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await resposta.Content.ReadAsStringAsync()).Should().NotContain(Conteudo);
        var negado = (await _app.EventosAsync(EventosDeSeguranca.AcessoNegado)).Should().ContainSingle().Subject;
        negado.UsuarioId.Should().Be(_pedro);
        negado.Entidade.Should().Be("Documento");
        negado.EntidadeId.Should().Be(_geral);
        negado.Ip.Should().Be(AplicacaoDeTeste.IpDeTeste);
        (await DownloadsAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Contrato_de_honorarios_e_404_para_a_producao_e_200_para_o_diretor()
    {
        var doPedro = await DoPedroAsync();
        var doDiretor = await _app.ClienteAutenticadoAsync();

        (await doPedro.GetAsync($"/documentos/{_contrato}/baixar")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await doDiretor.GetAsync($"/documentos/{_contrato}/baixar")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Documento_inexistente_ou_id_malformado_e_404()
    {
        var cliente = await _app.ClienteAutenticadoAsync();

        (await cliente.GetAsync($"/documentos/{Guid.CreateVersion7()}/baixar")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await cliente.GetAsync("/documentos/nao-e-um-id/baixar")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Anonimo_e_redirecionado_ao_login_sem_ver_o_arquivo()
    {
        var resposta = await _app.NovoCliente().GetAsync($"/documentos/{_geral}/baixar");

        resposta.StatusCode.Should().Be(HttpStatusCode.Redirect);
        resposta.Headers.Location!.AbsolutePath.Should().Be("/conta/entrar");
        (await DownloadsAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Usuario_inativado_depois_de_entrar_nao_baixa()
    {
        var cliente = await DoPedroAsync();
        (await _app.ExecutarAsync(_admin, new DefinirAtivoUsuario(_pedro, false))).Sucesso.Should().BeTrue();

        var resposta = await cliente.GetAsync($"/documentos/{_geral}/baixar");

        resposta.StatusCode.Should().Be(HttpStatusCode.Redirect);
        (await DownloadsAsync()).Should().BeEmpty();
    }
}
```

Acrescente a `AplicacaoDeTeste` o auxiliar usado acima:

```csharp
    public Task<Guid> IdDoPerfilDiretorAsync() => EmEscopoAsync(null, async sp =>
        (await sp.GetRequiredService<AppDbContext>().Perfis.AsNoTracking()
            .SingleAsync(p => p.Tipo == TipoPerfil.Diretoria)).Id);
```

(`DefinirAtivoUsuario` fica em `AtividadesJuridicas.Application.Identidade`: acrescente o `using`.)

- [ ] **Step 2: Rodar e ver falhar**

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --filter "FullyQualifiedName~DocumentosWebTests"`
Expected: FAIL — a rota não existe (404 onde se espera 200).

- [ ] **Step 3: Endpoint**

`src/AtividadesJuridicas.Web/Infra/DocumentosEndpoints.cs`:

```csharp
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Documentos;

namespace AtividadesJuridicas.Web.Infra;

public static class DocumentosEndpoints
{
    /// <summary>
    /// Download de documento. A rota exige usuário autenticado (política padrão); quem decide o acesso
    /// é o caso de uso, que também registra a consulta ou a tentativa negada.
    /// </summary>
    public static IEndpointRouteBuilder MapDocumentos(this IEndpointRouteBuilder rotas)
    {
        rotas.MapGet("/documentos/{id:guid}/baixar",
            async (Guid id, HttpContext contexto, ExecutorDeRequisicoes executor) =>
            {
                IdentidadeHttp.Definir(contexto);

                var resultado = await executor.ExecutarAsync(new BaixarDocumento(id), contexto.RequestAborted);
                if (resultado.Valor is not { } arquivo)
                    return resultado.Erro == TipoErro.AcessoNegado
                        ? Results.StatusCode(StatusCodes.Status403Forbidden)
                        : Results.NotFound();

                // Sempre anexo, com tipo neutro: o conteúdo enviado nunca é interpretado pelo navegador.
                contexto.Response.Headers.XContentTypeOptions = "nosniff";
                contexto.Response.Headers.CacheControl = "no-store";
                return Results.File(arquivo.Conteudo, DocumentosOpcoes.TipoDeConteudoPadrao, arquivo.NomeArquivo);
            });

        return rotas;
    }
}
```

Em `Program.cs`, depois do `MapPost("/conta/sair", ...)`: `app.MapDocumentos();`.

- [ ] **Step 4: Componente dos documentos do cliente**

Em `_Imports.razor`, acrescente:

```razor
@using AtividadesJuridicas.Application.Documentos
@using AtividadesJuridicas.Domain.Documentos
@using AtividadesJuridicas.Web.Components.Documentos
```

`src/AtividadesJuridicas.Web/Components/Documentos/DocumentosDoCliente.razor`:

```razor
@inject IDespachante Despachante
@inject FusoDoEscritorio Fuso

<section class="ficha">
    <h3>@Titulo</h3>

    @if (_mensagem is not null)
    {
        <p class="@(_falhou ? "erro" : "ok")" role="status">@_mensagem</p>
    }

    @if (!_carregado)
    {
        <p>Carregando…</p>
    }
    else if (_erroDaLista is not null)
    {
        <p class="erro">@_erroDaLista</p>
    }
    else if (_pagina.Itens.Count == 0)
    {
        <p>Nenhum documento.</p>
    }
    else
    {
        <table>
            <thead>
                <tr><th>Nome</th><th>Tamanho</th><th>Enviado em</th><th>Por</th><th></th></tr>
            </thead>
            <tbody>
                @foreach (var documento in _pagina.Itens)
                {
                    <tr @key="documento.Id">
                        <td>
                            @* Link fora da navegação do Blazor: é um download, não uma página. *@
                            <a href="@($"/documentos/{documento.Id}/baixar")" download data-enhance-nav="false">@documento.Nome</a>
                        </td>
                        <td>@Tamanho(documento.TamanhoBytes)</td>
                        <td>@Fuso.Formatar(documento.EnviadoEm)</td>
                        <td>@documento.EnviadoPor</td>
                        <td class="acoes">
                            @if (PodeGerenciar)
                            {
                                <button type="button" class="secundario" @onclick="() => AbrirRenomear(documento)">Renomear</button>
                                @if (Categoria == CategoriaDocumento.DocumentoGeral)
                                {
                                    <button type="button" class="secundario" @onclick="() => AbrirAcessoAsync(documento.Id)">Acesso</button>
                                }
                                @if (_excluindo == documento.Id)
                                {
                                    <button type="button" @onclick="() => ExcluirAsync(documento.Id)">Confirmar exclusão</button>
                                    <button type="button" class="secundario" @onclick="CancelarExclusao">Cancelar</button>
                                }
                                else
                                {
                                    <button type="button" class="secundario" @onclick="() => PedirExclusao(documento.Id)">Excluir</button>
                                }
                            }
                        </td>
                    </tr>
                }
            </tbody>
        </table>

        @if (_pagina.TotalDePaginas > 1)
        {
            <div class="acoes">
                <button type="button" class="secundario" disabled="@(_pagina.Numero <= 1)" @onclick="() => CarregarAsync(_pagina.Numero - 1)">Anterior</button>
                <span>Página @_pagina.Numero de @_pagina.TotalDePaginas</span>
                <button type="button" class="secundario" disabled="@(_pagina.Numero >= _pagina.TotalDePaginas)" @onclick="() => CarregarAsync(_pagina.Numero + 1)">Próxima</button>
            </div>
        }
    }

    @if (PodeGerenciar && _carregado)
    {
        @if (_renomeando is not null)
        {
            <div class="formulario">
                <label>Novo nome <input type="text" @bind="_novoNome" /></label>
                <div class="acoes">
                    <button type="button" @onclick="RenomearAsync">Salvar nome</button>
                    <button type="button" class="secundario" @onclick="FecharRenomear">Cancelar</button>
                </div>
            </div>
        }

        @if (_acesso is not null)
        {
            <div class="formulario">
                <strong>Quem acessa "@_acesso.Nome"</strong>
                <p>A Diretoria acessa sempre. Marque os perfis e os usuários de Produção que também podem ver este documento.</p>
                <fieldset>
                    <legend>Perfis</legend>
                    @foreach (var perfil in _perfis)
                    {
                        <label @key="perfil.Id">
                            <input type="checkbox" checked="@_perfisMarcados.Contains(perfil.Id)"
                                   @onchange="e => Marcar(_perfisMarcados, perfil.Id, e)" /> @perfil.Nome
                        </label>
                    }
                </fieldset>
                <fieldset>
                    <legend>Usuários</legend>
                    @foreach (var usuario in _usuarios)
                    {
                        <label @key="usuario.Id">
                            <input type="checkbox" checked="@_usuariosMarcados.Contains(usuario.Id)"
                                   @onchange="e => Marcar(_usuariosMarcados, usuario.Id, e)" /> @usuario.Nome (@usuario.PerfilNome)
                        </label>
                    }
                </fieldset>
                <div class="acoes">
                    <button type="button" @onclick="SalvarAcessoAsync">Salvar acesso</button>
                    <button type="button" class="secundario" @onclick="FecharAcesso">Cancelar</button>
                </div>
            </div>
        }

        <div class="formulario">
            <strong>Enviar documento</strong>
            <label>Arquivo (até @(DocumentosOpcoes.TamanhoMaximoBytes / UmMegabyte) MB) <InputFile @key="_versaoDoCampo" OnChange="Escolher" /></label>
            <label>Nome (opcional) <input type="text" @bind="_nomeDoEnvio" /></label>
            <div class="acoes">
                <button type="button" disabled="@(_arquivo is null || _enviando)" @onclick="EnviarAsync">
                    @(_enviando ? "Enviando…" : "Enviar")
                </button>
            </div>
        </div>
    }
</section>

@code {
    private const long UmMegabyte = 1024 * 1024;

    private static readonly Pagina<DocumentoResumo> Vazia = new([], 0, 1, DocumentosOpcoes.TamanhoDaPagina);

    [Parameter, EditorRequired] public Guid ClienteId { get; set; }
    [Parameter, EditorRequired] public CategoriaDocumento Categoria { get; set; }
    [Parameter, EditorRequired] public string Titulo { get; set; } = string.Empty;
    [Parameter] public bool PodeGerenciar { get; set; }

    /// <summary>Avisado quando um caso de uso responde acesso negado: a página decide o que mostrar.</summary>
    [Parameter] public EventCallback OnAcessoEncerrado { get; set; }

    private bool _carregado;
    private bool _falhou;
    private bool _enviando;
    private string? _mensagem;
    private string? _erroDaLista;
    private Pagina<DocumentoResumo> _pagina = Vazia;

    private IBrowserFile? _arquivo;
    private string? _nomeDoEnvio;
    private int _versaoDoCampo;

    private Guid? _excluindo;
    private DocumentoResumo? _renomeando;
    private string _novoNome = string.Empty;

    private AcessoDoDocumento? _acesso;
    private IReadOnlyList<PerfilResumo> _perfis = [];
    private IReadOnlyList<UsuarioResumo> _usuarios = [];
    private HashSet<Guid> _perfisMarcados = [];
    private HashSet<Guid> _usuariosMarcados = [];

    protected override async Task OnInitializedAsync()
    {
        await CarregarAsync(1);
        _carregado = true;
    }

    private async Task CarregarAsync(int pagina)
    {
        var resultado = await Despachante.EnviarAsync(new ListarDocumentosDoCliente(ClienteId, Categoria, pagina));
        if (await EncerradoAsync(resultado)) return;

        _pagina = resultado.Valor ?? Vazia;
        // Falha na consulta não é "nenhum documento": mostra o erro no lugar da lista.
        _erroDaLista = resultado.Sucesso ? null : resultado.Mensagem ?? "Não foi possível consultar os documentos.";

        // A página ficou além do fim (por exemplo, ao excluir o único item dela): volta à última.
        if (resultado.Sucesso && _pagina.Itens.Count == 0 && _pagina.Total > 0 && _pagina.Numero > _pagina.TotalDePaginas)
            await CarregarAsync(_pagina.TotalDePaginas);
    }

    private async Task<bool> EncerradoAsync(Resultado resultado)
    {
        if (resultado.Erro != TipoErro.AcessoNegado) return false;

        _pagina = Vazia;
        _acesso = null;
        _renomeando = null;
        await OnAcessoEncerrado.InvokeAsync();
        return true;
    }

    private void Escolher(InputFileChangeEventArgs e)
    {
        _arquivo = e.File;
        _mensagem = null;
    }

    private async Task EnviarAsync()
    {
        if (_arquivo is not { } arquivo || _enviando) return;

        if (arquivo.Size > DocumentosOpcoes.TamanhoMaximoBytes)
        {
            Avisar($"O arquivo passa do limite de {DocumentosOpcoes.TamanhoMaximoBytes / UmMegabyte} MB.", falhou: true);
            return;
        }

        _enviando = true;
        try
        {
            await using var conteudo = arquivo.OpenReadStream(DocumentosOpcoes.TamanhoMaximoBytes);
            var resultado = await Despachante.EnviarAsync(new EnviarDocumento(
                Categoria, ClienteId, _nomeDoEnvio, arquivo.Name, arquivo.ContentType, conteudo));
            if (await EncerradoAsync(resultado)) return;

            if (!resultado.Sucesso)
            {
                Avisar(resultado.Mensagem, falhou: true);
                return;
            }

            _arquivo = null;
            _nomeDoEnvio = null;
            _versaoDoCampo++;   // recria o campo de arquivo, vazio
            await CarregarAsync(1);
            if (_erroDaLista is null) Avisar("Documento enviado.", falhou: false);
        }
        catch (IOException)
        {
            // A leitura do arquivo no navegador foi interrompida (conexão caiu, arquivo removido).
            Avisar("Não foi possível ler o arquivo. Escolha-o de novo e tente outra vez.", falhou: true);
        }
        finally
        {
            _enviando = false;
        }
    }

    private void AbrirRenomear(DocumentoResumo documento)
    {
        _mensagem = null;
        _acesso = null;
        _renomeando = documento;
        _novoNome = documento.Nome;
    }

    private async Task RenomearAsync()
    {
        if (_renomeando is not { } documento) return;

        var resultado = await Despachante.EnviarAsync(new RenomearDocumento(documento.Id, _novoNome));
        if (await EncerradoAsync(resultado)) return;

        if (!resultado.Sucesso)
        {
            Avisar(resultado.Mensagem, falhou: true);
            return;
        }

        _renomeando = null;
        await CarregarAsync(_pagina.Numero);
        if (_erroDaLista is null) Avisar("Nome alterado.", falhou: false);
    }

    // Métodos, e não lambdas de atribuição, nos @onclick: a exclusão pede um segundo clique.
    private void PedirExclusao(Guid id) => _excluindo = id;

    private void CancelarExclusao() => _excluindo = null;

    private void FecharRenomear() => _renomeando = null;

    private void FecharAcesso() => _acesso = null;

    private async Task ExcluirAsync(Guid id)
    {
        _excluindo = null;
        var resultado = await Despachante.EnviarAsync(new ExcluirDocumento(id));
        if (await EncerradoAsync(resultado)) return;

        if (!resultado.Sucesso)
        {
            Avisar(resultado.Mensagem, falhou: true);
            return;
        }

        if (_acesso?.DocumentoId == id) _acesso = null;
        if (_renomeando?.Id == id) _renomeando = null;
        await CarregarAsync(_pagina.Numero);
        if (_erroDaLista is null) Avisar("Documento excluído.", falhou: false);
    }

    private async Task AbrirAcessoAsync(Guid id)
    {
        _mensagem = null;
        _renomeando = null;

        var acesso = await Despachante.EnviarAsync(new ObterAcessoDoDocumento(id));
        if (await EncerradoAsync(acesso)) return;
        var perfis = await Despachante.EnviarAsync(new ListarPerfisAtivos());
        if (await EncerradoAsync(perfis)) return;
        var usuarios = await Despachante.EnviarAsync(new ListarUsuarios());
        if (await EncerradoAsync(usuarios)) return;

        if (acesso.Valor is null || perfis.Valor is null || usuarios.Valor is null)
        {
            Avisar(acesso.Mensagem ?? perfis.Mensagem ?? usuarios.Mensagem, falhou: true);
            return;
        }

        // A Diretoria acessa sempre: só perfis e usuários de Produção fazem diferença na lista. Quem já
        // consta e não aparece aqui (inativo, ou da Diretoria) continua marcado e é preservado ao salvar.
        _perfis = [.. perfis.Valor.Where(p => p.Tipo == TipoPerfil.Producao)];
        _usuarios = [.. usuarios.Valor.Where(u => u.Ativo && u.TipoPerfil == TipoPerfil.Producao)];
        _perfisMarcados = [.. acesso.Valor.Perfis];
        _usuariosMarcados = [.. acesso.Valor.Usuarios];
        _acesso = acesso.Valor;
    }

    private static void Marcar(HashSet<Guid> marcados, Guid id, ChangeEventArgs e)
    {
        if (e.Value is true) marcados.Add(id);
        else marcados.Remove(id);
    }

    private async Task SalvarAcessoAsync()
    {
        if (_acesso is not { } acesso) return;

        var resultado = await Despachante.EnviarAsync(
            new DefinirAcessoDoDocumento(acesso.DocumentoId, [.. _perfisMarcados], [.. _usuariosMarcados]));
        if (await EncerradoAsync(resultado)) return;

        if (!resultado.Sucesso)
        {
            Avisar(resultado.Mensagem, falhou: true);
            return;
        }

        _acesso = null;
        Avisar("Acesso atualizado.", falhou: false);
    }

    private void Avisar(string? mensagem, bool falhou)
    {
        _mensagem = mensagem;
        _falhou = falhou;
    }

    private static string Tamanho(long bytes) => bytes switch
    {
        >= UmMegabyte => $"{bytes / (double)UmMegabyte:0.#} MB",
        >= 1024 => $"{bytes / 1024d:0.#} KB",
        _ => $"{bytes} B",
    };
}
```

Confira em `App.razor` e `Routes.razor` como o modo de renderização interativo é aplicado e garanta que o link de download faça uma requisição HTTP comum (não uma navegação interceptada pelo Blazor); `download` e `data-enhance-nav="false"` são o caminho pretendido. Descreva no relatório como verificou.

- [ ] **Step 5: Ficha do cliente**

Em `Clientes.razor`:

1. Na edição pela Diretoria, logo depois de `@ProcessosDoCliente` (dentro do `@if (_form.Id is not null)`):

```razor
                <DocumentosDoCliente @key="@($"gerais-{_form.Id}")" ClienteId="_form.Id.Value"
                                     Categoria="CategoriaDocumento.DocumentoGeral" Titulo="Documentos Gerais"
                                     PodeGerenciar="true" OnAcessoEncerrado="EncerrarAcesso" />
                <DocumentosDoCliente @key="@($"contratos-{_form.Id}")" ClienteId="_form.Id.Value"
                                     Categoria="CategoriaDocumento.ContratoHonorarios" Titulo="Contratos de Honorários"
                                     PodeGerenciar="true" OnAcessoEncerrado="EncerrarAcesso" />
```

2. Na ficha de leitura (Produção), logo depois de `@ProcessosDoCliente`:

```razor
            <DocumentosDoCliente @key="@($"gerais-{_ficha.Id}")" ClienteId="_ficha.Id"
                                 Categoria="CategoriaDocumento.DocumentoGeral" Titulo="Documentos Gerais"
                                 PodeGerenciar="false" OnAcessoEncerrado="EncerrarAcesso" />
```

A Produção não recebe a área "Contratos de Honorários": o componente nem é renderizado para ela (e o caso de uso negaria a listagem).

3. Extraia de `AcessoEncerrado(Resultado)` o que limpa a tela para um método `EncerrarAcesso()` e chame-o dos dois lugares:

```csharp
    private bool AcessoEncerrado(Resultado resultado)
    {
        if (resultado.Erro != TipoErro.AcessoNegado) return false;

        EncerrarAcesso();
        return true;
    }

    // Perdeu o acesso no meio do uso: some tudo que estava na tela, nada de dados antigos.
    private void EncerrarAcesso()
    {
        _ator = null;
        _pagina = Vazia;
        _form = null;
        _ficha = null;
        LimparProcessosDoCliente();
    }
```

- [ ] **Step 6: Rodar tudo**

Run: `dotnet test` (suíte inteira, da raiz)
Expected: PASS, tudo, sem avisos. Os testes de página existentes (`ClientesWebTests`) continuam verdes.

O comportamento interativo do componente (envio, renomear, excluir, lista de acesso, perda de acesso no meio do uso) não tem teste automatizado — lacuna conhecida do projeto, sem pacote de teste de componentes. Leia cada caminho e descreva no relatório como verificou: em especial, que uma falha de listagem nunca aparece como "Nenhum documento."; que `AcessoNegado` em qualquer ação chama `OnAcessoEncerrado`; que o botão "Enviar" fica desabilitado sem arquivo e durante o envio; que a mensagem de sucesso não sobrescreve um erro de recarga.

- [ ] **Step 7: Commit**

```bash
git add src tests
git commit -m "feat(web): download de documentos e documentos gerais e contratos de honorários na ficha do cliente"
```
