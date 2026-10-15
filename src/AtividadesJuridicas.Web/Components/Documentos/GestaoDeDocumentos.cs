using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Documentos;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Documentos;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Web.Infra;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace AtividadesJuridicas.Web.Components.Documentos;

/// <summary>
/// Estado e ações de uma lista de documentos que a Diretoria gerencia: listar, enviar (com confirmação nos
/// Documentos comuns), renomear, definir quem acessa e excluir. As telas que a herdam só desenham.
/// </summary>
public abstract class GestaoDeDocumentos : ComponentBase
{
    protected const long UmMegabyte = 1024 * 1024;

    private static readonly Pagina<Linha> Vazia = new([], 0, 1, DocumentosOpcoes.TamanhoDaPagina);

    [Inject] protected IDespachante Despachante { get; set; } = default!;

    /// <summary>Avisado quando um caso de uso responde acesso negado: a página decide o que mostrar.</summary>
    [Parameter] public EventCallback OnAcessoEncerrado { get; set; }

    /// <summary>A categoria dos documentos desta tela.</summary>
    protected abstract CategoriaDocumento CategoriaDosDocumentos { get; }

    /// <summary>O cliente dono dos documentos. Nulo só na categoria Documento comum.</summary>
    protected abstract Guid? ClienteDosDocumentos { get; }

    /// <summary>Leva o foco ao aviso de confirmação do envio, que cada tela desenha do seu jeito.</summary>
    protected abstract ValueTask FocarAvisoDeEnvioAsync();

    /// <summary>Documentos de Atividades: coluna "Atividade", sem envio e sem editor de acesso.</summary>
    protected bool DeAtividades => CategoriaDosDocumentos == CategoriaDocumento.DocumentoAtividade;

    protected bool _carregado;
    protected bool _falhou;
    protected bool _enviando;
    protected bool _envioAberto;
    protected bool _removendo;
    protected int _paginaPedida = 1;
    protected string? _mensagem;
    protected string? _erroDaLista;
    protected Pagina<Linha> _pagina = Vazia;

    // Documento comum fica visível a todos os clientes: o envio pede confirmação. Criado em OnInitializedAsync,
    // quando a categoria já chegou.
    protected EnvioComConfirmacao _confirmacao = new(exigeConfirmacao: false);
    private bool _focarAviso;

    // Um painel acabou de abrir e o campo dele ainda espera o foco: pedido uma vez por abertura, não a cada desenho.
    private bool _focarPainel;

    // O envio em curso foi confirmado no aviso de visibilidade (só os Documentos comuns pedem): o gancho padrão o repassa.
    private bool _envioConfirmado;

    protected IReadOnlyDictionary<CategoriaDocumento, int> _porCategoria = new Dictionary<CategoriaDocumento, int>();

    protected IBrowserFile? _arquivo;
    protected string? _nomeDoEnvio;
    protected int _versaoDoCampo;

    protected Guid? _excluindo;
    protected Linha? _renomeando;
    protected string _novoNome = string.Empty;

    protected AcessoDoDocumento? _acesso;
    protected IReadOnlyList<OpcaoDeAcesso> _perfis = [];
    protected IReadOnlyList<OpcaoDeAcesso> _usuarios = [];
    protected HashSet<Guid> _perfisMarcados = [];
    protected HashSet<Guid> _usuariosMarcados = [];

    /// <summary>Uma linha do editor de acesso: tudo o que consta na lista aparece aqui, com caixa de seleção.</summary>
    protected sealed record OpcaoDeAcesso(Guid Id, string Rotulo);

    /// <summary>Uma linha da tabela, venha a lista de documentos do cliente ou de documentos comuns.</summary>
    protected sealed record Linha(
        Guid Id, string Nome, long TamanhoBytes, DateTime EnviadoEm, string? EnviadoPor, string? AtividadeTitulo);

    protected override async Task OnInitializedAsync()
    {
        _confirmacao = new EnvioComConfirmacao(exigeConfirmacao: CategoriaDosDocumentos == CategoriaDocumento.DocumentoComum);
        await CarregarAsync(1);
        _carregado = true;
    }

    /// <summary>Troca de página ou nova tentativa: o aviso de uma ação anterior não vale mais.</summary>
    protected async Task IrParaAsync(int pagina)
    {
        _mensagem = null;
        await CarregarAsync(pagina);
    }

    private async Task CarregarAsync(int pagina)
    {
        _paginaPedida = pagina;
        _excluindo = null;
        var resultado = await ListarPaginaAsync(pagina);
        // Só a lista de um cliente traz a contagem por área.
        if (resultado.Valor is PaginaDeDocumentos lidos) _porCategoria = lidos.PorCategoria;

        if (await EncerradoAsync(resultado)) return;

        _pagina = resultado.Valor is { } p
            ? new Pagina<Linha>([.. p.Itens.Select(d => new Linha(d.Id, d.Nome, d.TamanhoBytes, d.EnviadoEm, d.EnviadoPor, d.AtividadeTitulo))],
                p.Total, p.Numero, p.Tamanho)
            : Vazia;
        // Falha na consulta não é "nenhum documento": mostra o erro no lugar da lista.
        _erroDaLista = resultado.Sucesso ? null : resultado.Mensagem ?? MensagemDaLista;

        // A página ficou além do fim (por exemplo, ao excluir o único item dela): volta à última.
        if (resultado.Sucesso && _pagina.Itens.Count == 0 && _pagina.Total > 0 && _pagina.Numero > _pagina.TotalDePaginas)
            await CarregarAsync(_pagina.TotalDePaginas);

        if (resultado.Sucesso) await AoCarregarAsync();
    }

    private const string MensagemDaLista = "Não foi possível consultar os documentos.";

    /// <summary>
    /// Lê uma página da lista. O padrão lê os Documentos comuns ou os do cliente na categoria da tela; a página do
    /// cliente chega como <see cref="PaginaDeDocumentos"/>, com a contagem por área. A lista de outra origem (os
    /// documentos de uma atividade) sobrescreve.
    /// </summary>
    protected virtual async Task<Resultado<Pagina<DocumentoResumo>>> ListarPaginaAsync(int pagina)
    {
        ConferirOrigemPadrao();
        if (CategoriaDosDocumentos == CategoriaDocumento.DocumentoComum)
        {
            var comuns = await Despachante.EnviarAsync(new ListarDocumentosComuns(pagina));
            if (!comuns.Sucesso) return MesmaFalha(comuns);

            // Documento comum não tem atividade: o título fica nulo. O autor vem preenchido para a Diretoria. A linha
            // não usa o nome original do arquivo; o resumo leva o nome do documento no lugar dele.
            var p = comuns.Valor!;
            return Resultado<Pagina<DocumentoResumo>>.Ok(new Pagina<DocumentoResumo>(
                [.. p.Itens.Select(d => new DocumentoResumo(
                    d.Id, d.Nome, d.Nome, d.TamanhoBytes, CategoriaDocumento.DocumentoComum, d.EnviadoEm, d.EnviadoPor, null, null))],
                p.Total, p.Numero, p.Tamanho));
        }

        var doCliente = await Despachante.EnviarAsync(
            new ListarDocumentosDoCliente(ClienteDosDocumentos!.Value, CategoriaDosDocumentos, pagina));
        // A página segue como PaginaDeDocumentos: a contagem por área vai junto.
        return doCliente.Sucesso ? Resultado<Pagina<DocumentoResumo>>.Ok(doCliente.Valor!) : MesmaFalha(doCliente);
    }

    /// <summary>
    /// Envia o arquivo escolhido. O padrão envia o Documento comum (com a confirmação de visibilidade que a tela
    /// colheu) ou o documento do cliente na categoria da tela. A lista de outra origem sobrescreve.
    /// </summary>
    protected virtual Task<Resultado<Guid>> EnviarArquivoAsync(string? nome, string nomeArquivo, string? tipoConteudo, Stream conteudo)
    {
        ConferirOrigemPadrao();
        return CategoriaDosDocumentos == CategoriaDocumento.DocumentoComum
            ? Despachante.EnviarAsync(new EnviarDocumentoComum(
                nome, nomeArquivo, tipoConteudo, conteudo, ConfirmouVisibilidade: _envioConfirmado))
            : Despachante.EnviarAsync(new EnviarDocumento(
                CategoriaDosDocumentos, ClienteDosDocumentos!.Value, nome, nomeArquivo, tipoConteudo, conteudo));
    }

    // A lista padrão é a dos Documentos comuns ou a de um cliente: qualquer outra combinação é erro de quem usa o
    // componente. A lista de outra origem sobrescreve os dois ganchos e não passa por aqui.
    private void ConferirOrigemPadrao()
    {
        if (CategoriaDosDocumentos != CategoriaDocumento.DocumentoComum && ClienteDosDocumentos is null)
            throw new InvalidOperationException("Informe o ClienteId: só os Documentos comuns dispensam o cliente.");
        if (CategoriaDosDocumentos == CategoriaDocumento.DocumentoComum && ClienteDosDocumentos is not null)
            throw new InvalidOperationException("Documentos comuns não pertencem a um cliente: não informe o ClienteId.");
    }

    // A falha da consulta no tipo do gancho, com o erro e a mensagem de quem respondeu.
    private static Resultado<Pagina<DocumentoResumo>> MesmaFalha(Resultado resultado) =>
        Resultado<Pagina<DocumentoResumo>>.Falha(resultado.Erro ?? TipoErro.Validacao, resultado.Mensagem ?? MensagemDaLista);

    private async Task<bool> EncerradoAsync(Resultado resultado)
    {
        if (resultado.Erro != TipoErro.AcessoNegado) return false;

        _pagina = Vazia;
        _acesso = null;
        _renomeando = null;
        _confirmacao.Cancelar();
        await OnAcessoEncerrado.InvokeAsync();
        return true;
    }

    protected void Escolher(InputFileChangeEventArgs e)
    {
        // Cancelar o diálogo de arquivos dispara a troca com zero arquivos.
        _arquivo = e.FileCount > 0 ? e.File : null;
        _mensagem = null;
        // O aviso era sobre o arquivo anterior.
        _confirmacao.Cancelar();
    }

    /// <summary>Clique em "Enviar": nos Documentos comuns só abre o aviso; nas demais categorias envia direto.</summary>
    protected async Task EnviarAsync()
    {
        if (_arquivo is not { } arquivo || _enviando || _confirmacao.AguardandoConfirmacao) return;

        _mensagem = null;
        // O aviso só abre para um envio que pode seguir. O arquivo não muda com o aviso aberto (escolher outro o
        // fecha), então a confirmação não repete esta verificação.
        if (arquivo.Size > DocumentosOpcoes.TamanhoMaximoBytes)
        {
            Avisar($"O arquivo passa do limite de {DocumentosOpcoes.TamanhoMaximoBytes / UmMegabyte} MB.", falhou: true);
            return;
        }

        if (!_confirmacao.Pedir())
        {
            _focarAviso = true;
            return;
        }

        await EnviarEscolhidoAsync(confirmou: false);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_focarAviso)
        {
            _focarAviso = false;
            await TentarFocarAsync(FocarAvisoDeEnvioAsync);
        }

        if (_focarPainel)
        {
            _focarPainel = false;
            await TentarFocarAsync(FocarPainelAbertoAsync);
        }
    }

    // Sem foco (circuito caindo, por exemplo) o que abriu continua visível e funcional.
    private static async Task TentarFocarAsync(Func<ValueTask> focar)
    {
        try
        {
            await focar();
        }
        catch (Exception ex) when (ex is InvalidOperationException or JSException or JSDisconnectedException)
        {
        }
    }

    protected async Task ConfirmarEnvioAsync()
    {
        if (_arquivo is null || _enviando) return;

        if (!_confirmacao.Confirmar()) return;

        await EnviarEscolhidoAsync(confirmou: true);
    }

    // O arquivo e o nome escolhidos ficam: o usuário pode enviar depois.
    protected void CancelarEnvio() => _confirmacao.Cancelar();

    // No máximo uma coisa aberta por vez entre o envio, o renomear, o acesso e a confirmação de exclusão: cada abertura
    // fecha as outras. Com um envio ou uma exclusão em curso nada se abre, para não desmontar o que a ação usa.
    protected bool Ocupada => _enviando || _removendo;

    protected void AbrirEnvio()
    {
        if (Ocupada) return;

        CancelarExclusao();
        _mensagem = null;
        _renomeando = null;
        _acesso = null;
        _envioAberto = true;
        _focarPainel = true;
    }

    protected void RenomearLinha(Linha documento)
    {
        if (Ocupada) return;

        CancelarExclusao();
        if (_envioAberto) DescartarEnvio();
        AbrirRenomear(documento);   // já fecha o acesso
        _focarPainel = true;
    }

    protected void ExcluirLinha(Guid id)
    {
        if (Ocupada) return;

        if (_envioAberto) DescartarEnvio();
        FecharRenomear();
        FecharAcesso();
        PedirExclusao(id);
    }

    protected async Task AbrirAcessoDaLinhaAsync(Guid id)
    {
        if (Ocupada) return;

        CancelarExclusao();
        if (_envioAberto) DescartarEnvio();
        await AbrirAcessoAsync(id);   // já fecha o renomear
        if (_acesso is not null) _focarPainel = true;
    }

    // O arquivo escolhido não é guardado: reabrir o envio começa do zero.
    protected void DescartarEnvio()
    {
        _confirmacao.Cancelar();
        _arquivo = null;
        _nomeDoEnvio = null;
        _versaoDoCampo++;
        _envioAberto = false;
    }

    /// <summary>Leva o foco ao campo do painel que acabou de abrir. As telas que têm campo a focar sobrescrevem.</summary>
    protected virtual ValueTask FocarPainelAbertoAsync() => ValueTask.CompletedTask;

    /// <summary>Chamado no fim de cada leitura bem-sucedida da lista.</summary>
    protected virtual Task AoCarregarAsync() => Task.CompletedTask;

    /// <summary>Um envio acabou de dar certo: fecha limpando (outro arquivo escolhido enquanto este subia não fica).</summary>
    protected virtual void AoConcluirEnvio() => DescartarEnvio();

    private async Task EnviarEscolhidoAsync(bool confirmou)
    {
        if (_arquivo is not { } arquivo || _enviando) return;

        _mensagem = null;
        _enviando = true;
        try
        {
            await using var conteudo = arquivo.OpenReadStream(DocumentosOpcoes.TamanhoMaximoBytes);
            _envioConfirmado = confirmou;
            var resultado = await EnviarArquivoAsync(_nomeDoEnvio, arquivo.Name, arquivo.ContentType, conteudo);
            if (await EncerradoAsync(resultado)) return;

            if (!resultado.Sucesso)
            {
                Avisar(resultado.Mensagem, falhou: true);
                return;
            }

            // Se outro arquivo foi escolhido enquanto este subia, a escolha nova é preservada.
            if (ReferenceEquals(_arquivo, arquivo))
            {
                _arquivo = null;
                _nomeDoEnvio = null;
                _versaoDoCampo++;   // recria o campo de arquivo, vazio
            }

            AoConcluirEnvio();
            await CarregarAsync(1);
            if (_erroDaLista is null) Avisar("Documento enviado.", falhou: false);
            else Avisar("Documento enviado, mas não foi possível atualizar a lista.", falhou: true);
        }
        catch (Exception ex) when (ex is IOException or JSException or TimeoutException or OperationCanceledException)
        {
            // A leitura do arquivo no navegador foi interrompida (conexão caiu, arquivo removido, tempo esgotado).
            Avisar("Não foi possível enviar o arquivo. Escolha-o de novo e tente outra vez.", falhou: true);
        }
        finally
        {
            _enviando = false;
        }
    }

    protected void AbrirRenomear(Linha documento)
    {
        _mensagem = null;
        _acesso = null;
        _renomeando = documento;
        _novoNome = documento.Nome;
    }

    protected async Task RenomearAsync()
    {
        if (_renomeando is not { } documento) return;

        _mensagem = null;
        var resultado = await Despachante.EnviarAsync(new RenomearDocumento(documento.Id, _novoNome));
        if (await EncerradoAsync(resultado)) return;

        if (!resultado.Sucesso)
        {
            Avisar(resultado.Mensagem, falhou: true);
            if (resultado.Erro == TipoErro.NaoEncontrado)
            {
                // O documento já não existe: fecha o painel e atualiza a lista, mantendo a mensagem.
                if (ReferenceEquals(_renomeando, documento)) _renomeando = null;
                await CarregarAsync(_pagina.Numero);
            }

            return;
        }

        if (ReferenceEquals(_renomeando, documento)) _renomeando = null;
        await CarregarAsync(_pagina.Numero);
        if (_erroDaLista is null) Avisar("Nome alterado.", falhou: false);
        else Avisar("Nome alterado, mas não foi possível atualizar a lista.", falhou: true);
    }

    // Métodos, e não lambdas de atribuição, nos @onclick: a exclusão pede um segundo clique.
    protected void PedirExclusao(Guid id) => _excluindo = id;

    protected void CancelarExclusao() => _excluindo = null;

    protected void FecharRenomear() => _renomeando = null;

    protected void FecharAcesso() => _acesso = null;

    protected async Task ExcluirAsync(Guid id)
    {
        // Um segundo clique em "Confirmar exclusão" receberia "não encontrado" e apagaria o aviso de sucesso.
        if (_removendo) return;

        _removendo = true;
        _mensagem = null;
        try
        {
            var resultado = await Despachante.EnviarAsync(new ExcluirDocumento(id));
            if (await EncerradoAsync(resultado)) return;

            if (!resultado.Sucesso)
            {
                Avisar(resultado.Mensagem, falhou: true);
                // O documento pode já não existir: a lista é atualizada para refletir o servidor.
                if (resultado.Erro == TipoErro.NaoEncontrado) await CarregarAsync(_pagina.Numero);
                return;
            }

            if (_acesso?.DocumentoId == id) _acesso = null;
            if (_renomeando?.Id == id) _renomeando = null;
            await CarregarAsync(_pagina.Numero);
            if (_erroDaLista is null) Avisar("Documento excluído.", falhou: false);
            else Avisar("Documento excluído, mas não foi possível atualizar a lista.", falhou: true);
        }
        finally
        {
            _removendo = false;
            _excluindo = null;
        }
    }

    protected async Task AbrirAcessoAsync(Guid id)
    {
        _mensagem = null;
        _renomeando = null;

        var acesso = await Despachante.EnviarAsync(new ObterAcessoDoDocumento(id));
        if (await EncerradoAsync(acesso)) return;
        var perfis = await Despachante.EnviarAsync(new ListarPerfis());
        if (await EncerradoAsync(perfis)) return;
        var usuarios = await Despachante.EnviarAsync(new ListarUsuarios());
        if (await EncerradoAsync(usuarios)) return;

        if (acesso.Valor is null || perfis.Valor is null || usuarios.Valor is null)
        {
            Avisar(acesso.Mensagem ?? perfis.Mensagem ?? usuarios.Mensagem, falhou: true);
            return;
        }

        // Tudo o que consta na lista aparece com uma caixa que funciona, para poder ser retirado; e só o que
        // aparece é enviado ao salvar. Perfis ativos e usuários ativos de Produção são as opções para incluir;
        // um perfil inativo só aparece se já consta na lista (o domínio recusaria incluí-lo), identificado como tal.
        var naLista = acesso.Valor.Perfis.ToHashSet();
        var perfisDaTela = new List<OpcaoDeAcesso>(perfis.Valor.Where(p => p.Tipo != TipoPerfil.Cliente && (p.Ativo || naLista.Contains(p.Id)))
            .Select(p => new OpcaoDeAcesso(p.Id, RotuloDoPerfil(p))));
        perfisDaTela.AddRange(naLista.Where(perfilId => perfisDaTela.All(o => o.Id != perfilId))
            .Select(perfilId => new OpcaoDeAcesso(perfilId, "Perfil não identificado")));

        var usuariosDaTela = new List<OpcaoDeAcesso>(usuarios.Valor.Where(u => u.Ativo && u.TipoPerfil == TipoPerfil.Producao)
            .Select(u => new OpcaoDeAcesso(u.Id, $"{u.Nome} ({u.PerfilNome})")));
        foreach (var usuarioId in acesso.Valor.Usuarios.Where(uid => usuariosDaTela.All(o => o.Id != uid)))
        {
            var usuario = usuarios.Valor.FirstOrDefault(u => u.Id == usuarioId);
            usuariosDaTela.Add(new OpcaoDeAcesso(usuarioId, usuario is null
                ? "Usuário não identificado"
                : $"{usuario.Nome} ({(usuario.Ativo ? "Diretoria — acessa sempre" : "inativo")})"));
        }

        _perfis = perfisDaTela;
        _usuarios = usuariosDaTela;
        _perfisMarcados = [.. acesso.Valor.Perfis];
        _usuariosMarcados = [.. acesso.Valor.Usuarios];
        _acesso = acesso.Valor;
    }

    private static string RotuloDoPerfil(PerfilDetalhe perfil) =>
        !perfil.Ativo ? $"{perfil.Nome} (inativo)"
        : perfil.Tipo == TipoPerfil.Diretoria ? $"{perfil.Nome} (Diretoria — acessa sempre)"
        : perfil.Nome;

    protected static void Marcar(HashSet<Guid> marcados, Guid id, ChangeEventArgs e)
    {
        if (e.Value is true) marcados.Add(id);
        else marcados.Remove(id);
    }

    protected async Task SalvarAcessoAsync()
    {
        if (_acesso is not { } acesso) return;

        _mensagem = null;
        var resultado = await Despachante.EnviarAsync(
            new DefinirAcessoDoDocumento(acesso.DocumentoId, [.. _perfisMarcados], [.. _usuariosMarcados]));
        if (await EncerradoAsync(resultado)) return;

        if (!resultado.Sucesso)
        {
            Avisar(resultado.Mensagem, falhou: true);
            return;
        }

        if (_acesso?.DocumentoId == acesso.DocumentoId) _acesso = null;
        Avisar("Acesso atualizado.", falhou: false);
    }

    private void Avisar(string? mensagem, bool falhou)
    {
        _mensagem = mensagem;
        _falhou = falhou;
    }
}
