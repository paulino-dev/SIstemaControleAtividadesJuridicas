using System.Text;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Documentos;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Documentos;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.UnitTests.Application;

public class DocumentosDoPortalTests
{
    private readonly Cenario _c = new();
    private readonly Cliente _maria;
    private readonly Cliente _jose;
    private readonly Usuario _pedro;
    private readonly Usuario _carla;

    public DocumentosDoPortalTests()
    {
        _maria = _c.NovoCliente("Maria Silva", "529.982.247-25");
        _jose = _c.NovoCliente("José Souza", "111.444.777-35");
        _pedro = _c.NovoUsuario("Pedro", "pedro@x.com", _c.PerfilProducao);
        _carla = _c.NovoUsuarioDeCliente("Carla Cliente", "carla@x.com", _maria);
    }

    private static MemoryStream Conteudo(string texto = "conteúdo") => new(Encoding.UTF8.GetBytes(texto));

    private Task<Resultado<Guid>> Enviar(Usuario? ator, CategoriaDocumento categoria, Cliente? cliente = null,
        string arquivo = "laudo.pdf") =>
        new EnviarDocumentoManipulador(_c.Como(ator), _c.Documentos, _c.Clientes, _c.Perfis, _c.Armazenamento, _c.Unidade)
            .ManipularAsync(new EnviarDocumento(categoria, (cliente ?? _maria).Id, null, arquivo, "application/pdf", Conteudo()), default);

    private Task<Resultado<Guid>> EnviarComum(Usuario? ator, bool confirmou, string arquivo = "modelo.pdf") =>
        new EnviarDocumentoComumManipulador(_c.Como(ator), _c.Documentos, _c.Armazenamento, _c.Unidade)
            .ManipularAsync(new EnviarDocumentoComum(null, arquivo, "application/pdf", Conteudo(), confirmou), default);

    private Task<Resultado<Pagina<DocumentoDoPortal>>> MeusDocumentos(Usuario? ator) =>
        new ListarMeusDocumentosManipulador(_c.Como(ator), _c.Documentos).ManipularAsync(new ListarMeusDocumentos(), default);

    private Task<Resultado<Pagina<DocumentoDoPortal>>> Comuns(Usuario? ator) =>
        new ListarDocumentosComunsManipulador(_c.Como(ator), _c.Documentos).ManipularAsync(new ListarDocumentosComuns(), default);

    private Task<Resultado<Pagina<DocumentoResumo>>> NaFicha(Usuario? ator, CategoriaDocumento categoria) =>
        new ListarDocumentosDoClienteManipulador(_c.Como(ator), _c.Documentos)
            .ManipularAsync(new ListarDocumentosDoCliente(_maria.Id, categoria), default);

    private Task<Resultado<ArquivoParaBaixar>> Baixar(Usuario? ator, Guid id) =>
        new BaixarDocumentoManipulador(_c.Como(ator), _c.Documentos, _c.Armazenamento, _c.Auditoria, _c.ResponsaveisDeAtividade)
            .ManipularAsync(new BaixarDocumento(id), default);

    private async Task<Guid> Enviado(CategoriaDocumento categoria, Cliente? cliente = null, string arquivo = "laudo.pdf")
    {
        var resultado = await Enviar(_c.Diretor, categoria, cliente, arquivo);
        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        return resultado.Valor;
    }

    private async Task<Guid> ComumEnviado(string arquivo = "modelo.pdf")
    {
        var resultado = await EnviarComum(_c.Diretor, true, arquivo);
        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        return resultado.Valor;
    }

    [Fact]
    public async Task Diretor_envia_documento_do_cliente_sem_lista_de_acesso()
    {
        var resultado = await Enviar(_c.Diretor, CategoriaDocumento.DocumentoDoCliente);

        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        var documento = _c.Documentos.Itens.Should().ContainSingle().Subject;
        documento.Id.Should().Be(resultado.Valor);
        documento.Categoria.Should().Be(CategoriaDocumento.DocumentoDoCliente);
        documento.ClienteId.Should().Be(_maria.Id);
        documento.PerfisComAcesso.Should().BeEmpty();
        documento.UsuariosComAcesso.Should().BeEmpty();
        _c.Armazenamento.Arquivos.Should().ContainKey(documento.ChaveArmazenamento);
    }

    [Fact]
    public async Task Producao_e_cliente_nao_enviam_documento_do_cliente_nem_documento_comum()
    {
        foreach (var ator in new[] { _pedro, _carla })
        {
            (await Enviar(ator, CategoriaDocumento.DocumentoDoCliente)).Erro.Should().Be(TipoErro.AcessoNegado);
            (await EnviarComum(ator, true)).Erro.Should().Be(TipoErro.AcessoNegado);
        }

        (await Enviar(null, CategoriaDocumento.DocumentoDoCliente)).Erro.Should().Be(TipoErro.AcessoNegado);
        (await EnviarComum(null, true)).Erro.Should().Be(TipoErro.AcessoNegado);
        _c.Documentos.Itens.Should().BeEmpty();
        _c.Armazenamento.Arquivos.Should().BeEmpty();
        _c.Unidade.Salvamentos.Should().Be(0);
    }

    [Fact]
    public async Task Documento_comum_so_e_enviado_com_a_confirmacao_e_sem_ela_nada_e_gravado()
    {
        var sem = await EnviarComum(_c.Diretor, false);

        sem.Erro.Should().Be(TipoErro.Validacao);
        sem.Mensagem.Should().Be("Confirme que o documento ficará visível para todos os clientes.");
        _c.Armazenamento.Arquivos.Should().BeEmpty();
        _c.Documentos.Itens.Should().BeEmpty();
        _c.Unidade.Salvamentos.Should().Be(0);

        var com = await EnviarComum(_c.Diretor, true);

        com.Sucesso.Should().BeTrue(com.Mensagem);
        var documento = _c.Documentos.Itens.Should().ContainSingle().Subject;
        documento.Categoria.Should().Be(CategoriaDocumento.DocumentoComum);
        documento.ClienteId.Should().BeNull();
        documento.PerfisComAcesso.Should().BeEmpty();
        _c.Armazenamento.Arquivos.Should().HaveCount(1);
        _c.Unidade.Salvamentos.Should().Be(1);
    }

    [Fact]
    public async Task Documento_comum_nao_e_enviado_pelo_envio_de_cliente()
    {
        foreach (var categoria in new[] { CategoriaDocumento.DocumentoComum, CategoriaDocumento.DocumentoAtividade })
        {
            var resultado = await Enviar(_c.Diretor, categoria);

            resultado.Erro.Should().Be(TipoErro.Validacao);
            resultado.Mensagem.Should().Be("Categoria inválida para envio.");
        }

        _c.Documentos.Itens.Should().BeEmpty();
        _c.Armazenamento.Arquivos.Should().BeEmpty();
    }

    [Fact]
    public async Task Cliente_lista_so_os_documentos_do_proprio_cliente_e_sem_autor()
    {
        _c.Documentos.AutorSimulado = "Diana Diretora";
        var daMaria1 = await Enviado(CategoriaDocumento.DocumentoDoCliente, _maria, "um.pdf");
        var daMaria2 = await Enviado(CategoriaDocumento.DocumentoDoCliente, _maria, "dois.pdf");
        await Enviado(CategoriaDocumento.DocumentoDoCliente, _jose, "do-jose.pdf");
        await Enviado(CategoriaDocumento.DocumentoGeral, _maria, "geral.pdf");
        await Enviado(CategoriaDocumento.ContratoHonorarios, _maria, "contrato.pdf");
        await ComumEnviado();

        var pagina = (await MeusDocumentos(_carla)).Valor!;

        pagina.Itens.Select(d => d.Id).Should().Equal(daMaria2, daMaria1);
        pagina.Total.Should().Be(2);
        pagina.Itens.Should().OnlyContain(d => d.EnviadoPor == null);
    }

    [Fact]
    public async Task Meus_documentos_e_so_para_o_perfil_cliente()
    {
        await Enviado(CategoriaDocumento.DocumentoDoCliente);

        foreach (var ator in new Usuario?[] { _c.Diretor, _pedro, null })
            (await MeusDocumentos(ator)).Erro.Should().Be(TipoErro.AcessoNegado);
    }

    [Fact]
    public async Task Documentos_comuns_chegam_ao_cliente_sem_autor_e_a_diretoria_com_autor_e_nunca_a_producao()
    {
        _c.Documentos.AutorSimulado = "Diana Diretora";
        var primeiro = await ComumEnviado("um.pdf");
        var segundo = await ComumEnviado("dois.pdf");
        await Enviado(CategoriaDocumento.DocumentoDoCliente);

        var doCliente = (await Comuns(_carla)).Valor!;
        var daDiretoria = (await Comuns(_c.Diretor)).Valor!;

        doCliente.Itens.Select(d => d.Id).Should().Equal(segundo, primeiro);
        doCliente.Itens.Should().OnlyContain(d => d.EnviadoPor == null);
        daDiretoria.Itens.Select(d => d.Id).Should().Equal(segundo, primeiro);
        daDiretoria.Itens.Should().OnlyContain(d => d.EnviadoPor == "Diana Diretora");
        (await Comuns(_pedro)).Erro.Should().Be(TipoErro.AcessoNegado);
        (await Comuns(null)).Erro.Should().Be(TipoErro.AcessoNegado);
    }

    [Fact]
    public async Task Na_ficha_o_documento_do_cliente_e_so_da_diretoria_e_o_comum_nao_e_de_cliente()
    {
        var id = await Enviado(CategoriaDocumento.DocumentoDoCliente);

        (await NaFicha(_c.Diretor, CategoriaDocumento.DocumentoDoCliente)).Valor!.Itens
            .Select(d => d.Id).Should().Equal(id);
        (await NaFicha(_pedro, CategoriaDocumento.DocumentoDoCliente)).Erro.Should().Be(TipoErro.AcessoNegado);

        foreach (var ator in new[] { _c.Diretor, _pedro })
        {
            var comum = await NaFicha(ator, CategoriaDocumento.DocumentoComum);

            comum.Erro.Should().Be(TipoErro.Validacao);
            comum.Mensagem.Should().Be("Categoria inválida para esta consulta.");
        }

        (await NaFicha(null, CategoriaDocumento.DocumentoComum)).Erro.Should().Be(TipoErro.AcessoNegado);
    }

    [Fact]
    public async Task Cliente_baixa_o_documento_do_proprio_cliente_e_o_comum()
    {
        var doCliente = await Enviado(CategoriaDocumento.DocumentoDoCliente);
        var comum = await ComumEnviado();

        foreach (var id in new[] { doCliente, comum })
        {
            var resultado = await Baixar(_carla, id);

            resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
            await using var arquivo = resultado.Valor!;
            arquivo.Conteudo.Should().NotBeNull();
        }

        _c.Auditoria.Eventos.Should().BeEmpty();
    }

    [Fact]
    public async Task Cliente_nao_baixa_documento_de_outro_cliente_nem_documento_interno()
    {
        var doJose = await Enviado(CategoriaDocumento.DocumentoDoCliente, _jose);
        var geral = await Enviado(CategoriaDocumento.DocumentoGeral, _maria);
        var contrato = await Enviado(CategoriaDocumento.ContratoHonorarios, _maria);

        foreach (var id in new[] { doJose, geral, contrato })
        {
            var resultado = await Baixar(_carla, id);

            resultado.Erro.Should().Be(TipoErro.NaoEncontrado);
            var evento = _c.Auditoria.Eventos.Last();
            evento.Evento.Should().Be(EventosDeSeguranca.AcessoNegado);
            evento.Entidade.Should().Be("Documento");
            evento.EntidadeId.Should().Be(id);
            evento.Imediato.Should().BeTrue();
        }

        _c.Auditoria.Eventos.Should().HaveCount(3);
    }

    [Fact]
    public async Task Producao_nao_baixa_documento_do_cliente_nem_documento_comum()
    {
        var doCliente = await Enviado(CategoriaDocumento.DocumentoDoCliente);
        var comum = await ComumEnviado();

        foreach (var id in new[] { doCliente, comum })
        {
            var resultado = await Baixar(_pedro, id);

            resultado.Erro.Should().Be(TipoErro.NaoEncontrado);
            var evento = _c.Auditoria.Eventos.Last();
            evento.Evento.Should().Be(EventosDeSeguranca.AcessoNegado);
            evento.EntidadeId.Should().Be(id);
        }

        _c.Auditoria.Eventos.Should().HaveCount(2);
    }

    [Fact]
    public void Consultas_do_portal_sao_auditaveis_e_nao_tem_campo_de_cliente()
    {
        IConsultaAuditavel[] consultas = [new ListarMeusDocumentos(), new ListarDocumentosComuns()];

        foreach (var consulta in consultas)
        {
            consulta.TipoRecurso.Should().Be("Documento");
            consulta.Acao.Should().Be(AcaoDeAcesso.Listou);
            consulta.GetType().GetProperties().Select(p => p.Name)
                .Should().NotContain(n => n.Contains("Cliente"), consulta.GetType().Name);
        }
    }
}
