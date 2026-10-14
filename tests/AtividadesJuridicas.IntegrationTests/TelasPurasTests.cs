using System.Security.Claims;
using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Web.Components.Layout;
using AtividadesJuridicas.Web.Infra;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>O filtro que resulta de clicar num botão de linha da tela de auditoria.</summary>
public class FiltroDaTelaDeAuditoriaTests
{
    private static FiltroDaTelaDeAuditoria Cheio() => new()
    {
        De = new DateOnly(2026, 1, 1),
        Ate = new DateOnly(2026, 1, 31),
        UsuarioId = Guid.CreateVersion7(),
        Entidade = "Usuario",
        EntidadeId = Guid.CreateVersion7().ToString(),
        IdCorrelacao = Guid.CreateVersion7().ToString(),
        Operacao = OperacaoAuditada.Alteracao,
        TipoRecurso = "Usuario",
        Acao = AcaoDeAcesso.Listou,
    };

    [Fact]
    public void Clicar_no_registro_deixa_so_o_periodo_e_o_registro()
    {
        var atual = Cheio();
        var registro = Guid.CreateVersion7();

        var filtro = FiltroDaTelaDeAuditoria.PorRegistro(atual, registro);

        filtro.De.Should().Be(atual.De);
        filtro.Ate.Should().Be(atual.Ate);
        filtro.EntidadeId.Should().Be(registro.ToString());
        filtro.IdCorrelacao.Should().BeNull();
        filtro.Entidade.Should().BeNull();
        filtro.Operacao.Should().BeNull();
        filtro.UsuarioId.Should().BeNull();
    }

    [Fact]
    public void Clicar_na_correlacao_deixa_so_o_periodo_e_a_correlacao()
    {
        var atual = Cheio();
        var correlacao = Guid.CreateVersion7();

        var filtro = FiltroDaTelaDeAuditoria.PorCorrelacao(atual, correlacao);

        filtro.De.Should().Be(atual.De);
        filtro.Ate.Should().Be(atual.Ate);
        filtro.IdCorrelacao.Should().Be(correlacao.ToString());
        filtro.EntidadeId.Should().BeNull();
        filtro.Entidade.Should().BeNull();
        filtro.Operacao.Should().BeNull();
        filtro.UsuarioId.Should().BeNull();
    }

    [Fact]
    public void O_filtro_de_partida_nao_e_alterado()
    {
        var atual = Cheio();
        var entidadeId = atual.EntidadeId;

        _ = FiltroDaTelaDeAuditoria.PorRegistro(atual, Guid.CreateVersion7());
        _ = FiltroDaTelaDeAuditoria.PorCorrelacao(atual, Guid.CreateVersion7());

        atual.EntidadeId.Should().Be(entidadeId);
        atual.Entidade.Should().Be("Usuario");
        atual.Operacao.Should().Be(OperacaoAuditada.Alteracao);
        atual.UsuarioId.Should().NotBeNull();
    }
}

/// <summary>As páginas que o perfil Cliente pode abrir.</summary>
public class RotasDoClienteTests
{
    [Theory]
    [InlineData("")]
    [InlineData("/")]
    [InlineData("minha-senha")]
    [InlineData("minha-senha/")]
    [InlineData("Minha-Senha")]
    [InlineData("minha-senha?x=1")]
    [InlineData("minha-senha#topo")]
    [InlineData("nao-encontrada")]
    [InlineData("meus-documentos")]
    [InlineData("documentos-comuns")]
    [InlineData("andamento")]
    public void Rotas_do_portal_sao_permitidas(string caminho) =>
        RotasDoCliente.Permite(caminho).Should().BeTrue();

    [Theory]
    [InlineData("meus-documentos/")]
    [InlineData("Meus-Documentos")]
    [InlineData("meus-documentos?x=1")]
    [InlineData("meus-documentos#topo")]
    [InlineData("meus%2Ddocumentos")]
    [InlineData("andamento")]
    [InlineData("andamento/")]
    [InlineData("Andamento")]
    [InlineData("andamento?situacao=1")]
    [InlineData("andamento#topo")]
    [InlineData("andamen%74o")]
    public void Variantes_do_endereco_das_paginas_so_do_portal_valem_nas_duas_funcoes(string caminho)
    {
        RotasDoCliente.Exclusiva(caminho).Should().BeTrue();
        RotasDoCliente.Permite(caminho).Should().BeTrue();
    }

    [Fact]
    public void Endereco_duplamente_codificado_nao_e_exclusivo_nem_permitido()
    {
        RotasDoCliente.Exclusiva("meus%252Ddocumentos").Should().BeFalse();
        RotasDoCliente.Permite("meus%252Ddocumentos").Should().BeFalse();
    }

    [Fact]
    public void So_meus_documentos_e_andamento_sao_exclusivas_do_portal()
    {
        RotasDoCliente.Exclusiva("meus-documentos").Should().BeTrue();
        RotasDoCliente.Exclusiva("andamento").Should().BeTrue();
        RotasDoCliente.Exclusiva("").Should().BeFalse();
        RotasDoCliente.Exclusiva("minha-senha").Should().BeFalse();
        RotasDoCliente.Exclusiva("documentos-comuns").Should().BeFalse();
        RotasDoCliente.Exclusiva("clientes").Should().BeFalse();
    }

    [Theory]
    [InlineData("clientes")]
    [InlineData("processos")]
    [InlineData("atividades")]
    [InlineData("agenda")]
    [InlineData("areas")]
    [InlineData("usuarios")]
    [InlineData("perfis")]
    [InlineData("auditoria")]
    [InlineData("produtores")]
    [InlineData("minha-senha/outra")]
    [InlineData("qualquer-coisa")]
    public void Paginas_internas_e_desconhecidas_nao_sao_permitidas(string caminho) =>
        RotasDoCliente.Permite(caminho).Should().BeFalse();
}

public class EnvioComConfirmacaoTests
{
    [Fact]
    public void Sem_exigencia_o_pedido_ja_envia()
    {
        var envio = new EnvioComConfirmacao(exigeConfirmacao: false);

        envio.Pedir().Should().BeTrue();
        envio.AguardandoConfirmacao.Should().BeFalse();
    }

    [Fact]
    public void Com_exigencia_o_pedido_so_mostra_o_aviso_e_a_confirmacao_envia_uma_vez()
    {
        var envio = new EnvioComConfirmacao(exigeConfirmacao: true);

        envio.Pedir().Should().BeFalse();
        envio.AguardandoConfirmacao.Should().BeTrue();
        envio.Confirmar().Should().BeTrue();
        envio.AguardandoConfirmacao.Should().BeFalse();
        envio.Confirmar().Should().BeFalse();

        // O aviso vale para um envio só.
        envio.Pedir().Should().BeFalse();
        envio.AguardandoConfirmacao.Should().BeTrue();
    }

    [Fact]
    public void Cancelar_fecha_o_aviso_e_o_proximo_pedido_avisa_de_novo()
    {
        var envio = new EnvioComConfirmacao(exigeConfirmacao: true);
        envio.Pedir();

        envio.Cancelar();

        envio.AguardandoConfirmacao.Should().BeFalse();
        envio.Confirmar().Should().BeFalse();
        envio.Pedir().Should().BeFalse();
        envio.AguardandoConfirmacao.Should().BeTrue();
    }

    [Fact]
    public void Confirmar_sem_aviso_na_tela_nao_envia()
    {
        var envio = new EnvioComConfirmacao(exigeConfirmacao: true);

        envio.Confirmar().Should().BeFalse();
    }
}

public class TamanhoDeArquivoTests
{
    [Fact]
    public void Formata_bytes_quilobytes_e_megabytes()
    {
        TamanhoDeArquivo.Formatar(0).Should().Be("0 B");
        TamanhoDeArquivo.Formatar(1023).Should().Be("1023 B");
        TamanhoDeArquivo.Formatar(1024).Should().Be("1 KB");
        TamanhoDeArquivo.Formatar(1536).Should().Be("1,5 KB");
        TamanhoDeArquivo.Formatar(1024 * 1024).Should().Be("1 MB");
        TamanhoDeArquivo.Formatar(5 * 1024 * 1024 + 512 * 1024).Should().Be("5,5 MB");
    }

    [Fact]
    public void A_formatacao_nao_depende_da_cultura_da_thread()
    {
        var anterior = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            TamanhoDeArquivo.Formatar(1536).Should().Be("1,5 KB");
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = anterior;
        }
    }
}

public class IdentidadeHttpTests
{
    private static ClaimsPrincipal Com(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, "teste"));

    [Fact]
    public void Marca_do_portal_so_vale_com_a_claim()
    {
        IdentidadeHttp.EhDoPortal(Com(new Claim("atividades:portal", "1"))).Should().BeTrue();
        IdentidadeHttp.EhDoPortal(Com()).Should().BeFalse();
        IdentidadeHttp.EhDoPortal(Com(new Claim("atividades:portal", "0"))).Should().BeFalse();
        IdentidadeHttp.EhDoPortal(Com(new Claim("atividades:portal", "true"))).Should().BeFalse();
    }
}

public class RotulosDePerfilTests
{
    [Fact]
    public void Cada_tipo_tem_o_seu_rotulo()
    {
        RotulosDePerfil.DoTipo(TipoPerfil.Diretoria).Should().Be("Diretoria");
        RotulosDePerfil.DoTipo(TipoPerfil.Producao).Should().Be("Produção");
        RotulosDePerfil.DoTipo(TipoPerfil.Cliente).Should().Be("Cliente");
    }
}

public class RotulosDoAndamentoTests
{
    [Fact]
    public void Cada_situacao_tem_o_seu_rotulo()
    {
        RotulosDoAndamento.Da(SituacaoDoAndamento.EmAndamento).Should().Be("Em andamento");
        RotulosDoAndamento.Da(SituacaoDoAndamento.Concluida).Should().Be("Concluída");
    }
}

public class TextosDoClienteTests
{
    [Fact]
    public void Aviso_de_inativacao_conta_os_usuarios_do_portal()
    {
        TextosDoCliente.Inativado(0).Should().Be("Cliente inativado.");
        TextosDoCliente.Inativado(1).Should().Be("Cliente inativado. 1 usuário do portal ficou sem acesso.");
        TextosDoCliente.Inativado(3).Should().Be("Cliente inativado. 3 usuários do portal ficaram sem acesso.");
    }
}

/// <summary>As frases da confirmação de inativação de um usuário.</summary>
public class TextosDaInativacaoTests
{
    [Fact]
    public void So_partes()
    {
        TextosDaInativacao.Pendencias("Pedro", new PendenciasDoUsuario(2, 0, 0)).Should()
            .Be("Este usuário tem 2 partes pendentes. Esses itens continuam atribuídos a Pedro até serem redistribuídos.");
        TextosDaInativacao.Pendencias("Pedro", new PendenciasDoUsuario(1, 0, 0)).Should()
            .Be("Este usuário tem 1 parte pendente. Esses itens continuam atribuídos a Pedro até serem redistribuídos.");
    }

    [Fact]
    public void So_revisoes()
    {
        TextosDaInativacao.Pendencias("Davi", new PendenciasDoUsuario(0, 1, 0)).Should()
            .Be("Este usuário tem 1 revisão em aberto. Esses itens continuam atribuídos a Davi até serem redistribuídos.");
        TextosDaInativacao.Pendencias("Davi", new PendenciasDoUsuario(0, 3, 0)).Should()
            .Be("Este usuário tem 3 revisões em aberto. Esses itens continuam atribuídos a Davi até serem redistribuídos.");
    }

    [Fact]
    public void So_eventos()
    {
        TextosDaInativacao.Pendencias("Paula", new PendenciasDoUsuario(0, 0, 1)).Should()
            .Be("Este usuário tem 1 evento futuro. Esses itens continuam atribuídos a Paula até serem redistribuídos.");
        TextosDaInativacao.Pendencias("Paula", new PendenciasDoUsuario(0, 0, 4)).Should()
            .Be("Este usuário tem 4 eventos futuros. Esses itens continuam atribuídos a Paula até serem redistribuídos.");
    }

    [Fact]
    public void Os_tres_e_dois_a_dois()
    {
        TextosDaInativacao.Pendencias("Ana", new PendenciasDoUsuario(1, 2, 3)).Should()
            .Be("Este usuário tem 1 parte pendente, 2 revisões em aberto e 3 eventos futuros. " +
                "Esses itens continuam atribuídos a Ana até serem redistribuídos.");
        TextosDaInativacao.Pendencias("Ana", new PendenciasDoUsuario(1, 0, 3)).Should()
            .Be("Este usuário tem 1 parte pendente e 3 eventos futuros. " +
                "Esses itens continuam atribuídos a Ana até serem redistribuídos.");
    }

    [Fact]
    public void Sem_pendencias_nao_ha_frase_de_pendencias_so_a_pergunta()
    {
        TextosDaInativacao.Pendencias("Ana", new PendenciasDoUsuario(0, 0, 0)).Should().BeNull();
        TextosDaInativacao.Pergunta("Ana").Should().Be("Confirmar a inativação de Ana?");
    }
}

public class ContagemTests
{
    [Theory]
    [InlineData(0, "0 documentos")]
    [InlineData(1, "1 documento")]
    [InlineData(8, "8 documentos")]
    public void Contagem_usa_o_singular_so_para_um(int quantidade, string esperado)
    {
        Contagem.De(quantidade, "documento", "documentos").Should().Be(esperado);
    }

    [Theory]
    [InlineData(5, 1, "5 áreas · 1 inativa")]
    [InlineData(5, 0, "5 áreas")]
    [InlineData(1, 1, "1 área · 1 inativa")]
    [InlineData(7, 2, "7 áreas · 2 inativas")]
    public void Contagem_com_inativos_so_fala_deles_quando_ha(int total, int inativos, string esperado)
    {
        Contagem.ComInativos(total, inativos, "área", "áreas", "inativa", "inativas").Should().Be(esperado);
    }
}

public class IdentificadorCurtoTests
{
    [Fact]
    public void Identificador_curto_traz_os_oito_primeiros_caracteres_e_reticencias()
    {
        IdentificadorCurto.De(Guid.Parse("0199a3f2-7c1e-7000-8000-000000000001")).Should().Be("0199a3f2…");
    }
}

public class RecadosTests
{
    [Fact]
    public void Recado_e_entregue_uma_vez_so_e_vale_o_ultimo()
    {
        var recados = new Recados();
        recados.Retirar().Should().BeNull();

        recados.Deixar("Usuário salvo.");
        recados.Deixar("Usuário inativado.", TipoDeAviso.Sucesso);

        recados.Retirar().Should().Be(new Recado("Usuário inativado.", TipoDeAviso.Sucesso));
        recados.Retirar().Should().BeNull();
    }
}

public class ResumoDeAuditoriaTests
{
    private readonly FusoDoEscritorio _fuso = new("America/Sao_Paulo");

    [Theory]
    [InlineData("{\"Status\":{\"de\":\"Feita\",\"para\":\"Concluida\"}}", "Status: Feita → Concluida")]
    [InlineData("{\"De\":\"Feita\",\"Para\":\"Concluida\"}", "De: Feita · Para: Concluida")]
    [InlineData("{\"ConcluidoEm\":{\"de\":null,\"para\":\"2026-10-08T21:11:00Z\"}}", "ConcluidoEm: null → 08/10/2026 18:11")]
    [InlineData("{\"email\":\"desconhecido@correio.example\"}", "email: desconhecido@correio.example")]
    [InlineData("{\"DataEntrega\":\"2090-06-15\",\"Complexidade\":2,\"Ativo\":true}", "DataEntrega: 15/06/2090 · Complexidade: 2 · Ativo: true")]
    [InlineData("{\"Partes\":[1,2],\"Endereco\":{\"Cidade\":\"Maceió\"}}", "Partes: [1,2] · Endereco: {\"Cidade\":\"Maceió\"}")]
    [InlineData("{\"Quando\":\"2026-10-08T21:11:00\"}", "Quando: 08/10/2026 18:11")]
    public void Resumo_le_os_campos_gravados(string dados, string esperado)
    {
        ResumoDeAuditoria.De(dados, _fuso).Should().Be(esperado);
    }

    [Theory]
    [InlineData(null, "—")]
    [InlineData("", "—")]
    [InlineData("{}", "—")]
    [InlineData("texto solto", "texto solto")]
    [InlineData("[1,2]", "[1,2]")]
    [InlineData("{\"quebrado\":", "{\"quebrado\":")]
    public void Resumo_de_dados_fora_da_forma_mostra_o_que_veio(string? dados, string esperado)
    {
        ResumoDeAuditoria.De(dados, _fuso).Should().Be(esperado);
    }
}

public class TextosDeDocumentoTests
{
    private readonly FusoDoEscritorio _fuso = new("America/Sao_Paulo");

    [Fact]
    public void Endereco_leva_ao_download()
    {
        TextosDeDocumento.Endereco(Guid.Parse("0199a3f2-7c1e-7000-8000-000000000001"))
            .Should().Be("/documentos/0199a3f2-7c1e-7000-8000-000000000001/baixar");
    }

    [Fact]
    public void Detalhe_junta_tamanho_data_e_autor_sem_separador_solto()
    {
        var quando = new DateTime(2026, 9, 2, 13, 14, 0, DateTimeKind.Utc);

        TextosDeDocumento.Detalhe(412 * 1024, quando, "Marina Couto", _fuso).Should().Be("412 KB · 02/09/2026 10:14 · Marina Couto");
        TextosDeDocumento.Detalhe(412 * 1024, quando, null, _fuso).Should().Be("412 KB · 02/09/2026 10:14");
    }
}

public class RotulosDeProcessoTests
{
    [Theory]
    [InlineData(StatusProcesso.Ativo, "Ativo", "Ativos")]
    [InlineData(StatusProcesso.Suspenso, "Suspenso", "Suspensos")]
    [InlineData(StatusProcesso.Arquivado, "Arquivado", "Arquivados")]
    [InlineData(StatusProcesso.Encerrado, "Encerrado", "Encerrados")]
    public void Situacao_no_singular_e_no_plural(StatusProcesso status, string singular, string plural)
    {
        RotulosDeProcesso.Situacao(status).Should().Be(singular);
        RotulosDeProcesso.SituacaoNoPlural(status).Should().Be(plural);
    }

    [Fact]
    public void Numero_vazio_e_sem_numero()
    {
        RotulosDeProcesso.Numero(null).Should().Be("Sem número");
        RotulosDeProcesso.Numero("0000000-00.2026.5.19.0002").Should().Be("0000000-00.2026.5.19.0002");
        RotulosDeProcesso.Polo(PoloProcessual.Terceiro).Should().Be("Terceiro");
    }
}

public class TextosDoClienteDaFichaTests
{
    private static ClienteDetalhe Cliente(string? logradouro = null, string? numero = null, string? complemento = null,
        string? bairro = null, string? cidade = null, string? uf = null, string? cep = null) =>
        new(Guid.NewGuid(), TipoCliente.PessoaJuridica, "Transportes Litoral Ltda.", null, null,
            logradouro, numero, complemento, bairro, cidade, uf, cep, null, true,
            null, null, null, "00.000.000/0002-00", null, null, null, null);

    [Fact]
    public void Tipo_e_documento()
    {
        TextosDoCliente.Tipo(TipoCliente.PessoaFisica).Should().Be("Pessoa física");
        TextosDoCliente.Documento(TipoCliente.PessoaJuridica, "00.000.000/0002-00").Should().Be("CNPJ 00.000.000/0002-00");
        TextosDoCliente.Documento(TipoCliente.PessoaFisica, "529.982.247-25").Should().Be("CPF 529.982.247-25");
    }

    [Fact]
    public void Endereco_so_com_o_que_ha()
    {
        TextosDoCliente.Endereco(Cliente("Av. do Porto", "1200", null, "Jaraguá", "Maceió", "AL"))
            .Should().Be("Av. do Porto, 1200, Jaraguá, Maceió/AL");
        TextosDoCliente.Endereco(Cliente("Rua A", null, "sala 4", null, "Maceió", null, "57000-000"))
            .Should().Be("Rua A, sala 4, Maceió, CEP 57000-000");
        TextosDoCliente.Endereco(Cliente(uf: "AL")).Should().Be("AL");
        TextosDoCliente.Endereco(Cliente()).Should().BeNull();
    }

    [Theory]
    [InlineData(null, "Os usuários do portal deste cliente ficam sem acesso até ele ser reativado.")]
    [InlineData(0, "O cliente pode ser reativado depois.")]
    [InlineData(1, "1 usuário do portal fica sem acesso até o cliente ser reativado.")]
    [InlineData(3, "3 usuários do portal ficam sem acesso até o cliente ser reativado.")]
    public void Consequencia_da_inativacao(int? usuarios, string esperado)
    {
        TextosDoCliente.ConsequenciaDaInativacao(usuarios).Should().Be(esperado);
    }
}

public class ValorEmReaisTests
{
    [Theory]
    [InlineData("84.000,00", 84000)]
    [InlineData("84000", 84000)]
    [InlineData("84000,5", 84000.5)]
    [InlineData("1.234.567,89", 1234567.89)]
    [InlineData("0,50", 0.5)]
    [InlineData(" 84.000,00 ", 84000)]
    public void Le_o_formato_brasileiro(string texto, double esperado)
    {
        ValorEmReais.TentarLer(texto, out var valor).Should().BeTrue();
        valor.Should().Be((decimal)esperado);
    }

    [Theory]
    [InlineData("84000.50")]
    [InlineData("1.2.3")]
    [InlineData("84.00")]
    [InlineData("abc")]
    [InlineData("-10")]
    [InlineData("10,555")]
    public void Recusa_o_que_nao_e_o_formato_brasileiro(string texto)
    {
        ValorEmReais.TentarLer(texto, out _).Should().BeFalse();
    }

    [Fact]
    public void Vazio_e_nulo_e_valido_e_escreve_com_duas_casas()
    {
        ValorEmReais.TentarLer("  ", out var vazio).Should().BeTrue();
        vazio.Should().BeNull();
        ValorEmReais.Escrever(84000m).Should().Be("84.000,00");
        ValorEmReais.Escrever(null).Should().BeNull();
    }
}