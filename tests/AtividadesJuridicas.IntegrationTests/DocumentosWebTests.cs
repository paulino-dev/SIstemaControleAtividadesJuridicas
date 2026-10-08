using System.Net;
using System.Text;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Cadastros;
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

    [Fact]
    public async Task Arquivo_que_sumiu_do_disco_e_erro_de_servidor_registrado_e_nao_um_404()
    {
        var chave = await _app.EmEscopoAsync(null, async sp =>
            (await sp.GetRequiredService<AppDbContext>().Documentos.AsNoTracking().SingleAsync(d => d.Id == _geral)).ChaveArmazenamento);
        File.Delete(Path.Combine(_app.PastaDeArquivos, chave.Replace('/', Path.DirectorySeparatorChar)));
        var cliente = await _app.ClienteAutenticadoAsync();

        var resposta = await cliente.GetAsync($"/documentos/{_geral}/baixar");

        resposta.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        (await resposta.Content.ReadAsStringAsync()).Should().NotContain(Conteudo);
        (await DownloadsAsync()).Should().BeEmpty("o download não aconteceu");
        var evento = (await _app.EventosAsync(EventosDeDocumento.ArquivoAusente)).Should().ContainSingle().Subject;
        evento.Entidade.Should().Be("Documento");
        evento.EntidadeId.Should().Be(_geral);
        evento.UsuarioId.Should().Be(_admin);
    }

    [Fact]
    public async Task Download_informa_o_tamanho_do_arquivo()
    {
        var cliente = await _app.ClienteAutenticadoAsync();

        var resposta = await cliente.GetAsync($"/documentos/{_geral}/baixar");

        resposta.Content.Headers.ContentLength.Should().Be(Encoding.UTF8.GetByteCount(Conteudo));
    }
}
