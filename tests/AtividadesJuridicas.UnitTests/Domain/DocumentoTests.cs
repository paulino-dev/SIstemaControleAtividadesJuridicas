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
        documento.PerfisComAcesso.Select(a => a.PerfilId).Should().BeEquivalentTo([_diretor.Id, _producao.Id]);
        documento.UsuariosComAcesso.Select(a => a.UsuarioId).Should().Equal(ester.Id);
    }

    [Fact]
    public void Perfil_informado_tem_de_ser_o_do_usuario()
    {
        var pedro = NovoUsuario("Pedro", _producao);
        var documento = Geral(_diretor);

        var acao = () => documento.DefinirAcesso([], [new UsuarioComPerfil(pedro, _diretor)]);

        acao.Should().Throw<ArgumentException>();
    }

    // ---- Elementos nulos ----

    [Fact]
    public void Perfil_nulo_na_criacao_e_erro_de_programacao()
    {
        var acao = () => Documento.CriarGeral(Cliente, null, Arquivo(), [_diretor, null!]);

        acao.Should().Throw<ArgumentException>().WithParameterName("perfisAtivos");
    }

    [Fact]
    public void Perfil_nulo_em_definir_acesso_e_erro_de_programacao_e_nada_muda()
    {
        var documento = Geral(_diretor);
        var original = documento.PerfisComAcesso.ToList();

        var acao = () => documento.DefinirAcesso([_producao, null!], []);

        acao.Should().Throw<ArgumentException>().WithParameterName("perfis");
        documento.PerfisComAcesso.Should().Equal(original);
    }

    [Fact]
    public void Usuario_com_perfil_nulo_ou_incompleto_e_erro_de_programacao_e_nada_muda()
    {
        var pedro = NovoUsuario("Pedro", _producao);
        var documento = Geral(_diretor);
        documento.DefinirAcesso([_diretor], [new UsuarioComPerfil(pedro, _producao)]);
        var perfis = documento.PerfisComAcesso.ToList();
        var usuarios = documento.UsuariosComAcesso.ToList();

        var elementoNulo = () => documento.DefinirAcesso([], [null!]);
        var semUsuario = () => documento.DefinirAcesso([], [new UsuarioComPerfil(null!, _producao)]);
        var semPerfil = () => documento.DefinirAcesso([], [new UsuarioComPerfil(pedro, null!)]);

        elementoNulo.Should().Throw<ArgumentException>().WithParameterName("usuarios");
        semUsuario.Should().Throw<ArgumentException>().WithParameterName("usuarios");
        semPerfil.Should().Throw<ArgumentException>().WithParameterName("usuarios");
        documento.PerfisComAcesso.Should().Equal(perfis);
        documento.UsuariosComAcesso.Should().Equal(usuarios);
    }

    // ---- Atomicidade ----

    [Fact]
    public void Perfil_inativo_novo_com_remocoes_no_mesmo_pedido_nao_muda_nada()
    {
        var pedro = NovoUsuario("Pedro", _producao);
        var documento = Geral(_diretor, _producao);
        documento.DefinirAcesso([_diretor, _producao], [new UsuarioComPerfil(pedro, _producao)]);
        var perfis = documento.PerfisComAcesso.ToList();
        var usuarios = documento.UsuariosComAcesso.ToList();
        _estagio.Inativar();

        var acao = () => documento.DefinirAcesso([_diretor, _estagio], []);

        acao.Should().Throw<ExcecaoDeDominio>().WithMessage("Perfil inativo não pode receber acesso a documento.");
        documento.PerfisComAcesso.Should().HaveCount(2).And.Contain(perfis);
        documento.UsuariosComAcesso.Should().Equal(usuarios);
    }

    [Fact]
    public void Usuario_inativo_novo_com_remocoes_no_mesmo_pedido_nao_muda_nada()
    {
        var pedro = NovoUsuario("Pedro", _producao);
        var ester = NovoUsuario("Ester", _estagio);
        ester.Inativar();
        var documento = Geral(_diretor, _producao);
        documento.DefinirAcesso([_diretor, _producao], [new UsuarioComPerfil(pedro, _producao)]);
        var perfis = documento.PerfisComAcesso.ToList();
        var usuarios = documento.UsuariosComAcesso.ToList();

        var acao = () => documento.DefinirAcesso([_diretor], [new UsuarioComPerfil(ester, _estagio)]);

        acao.Should().Throw<ExcecaoDeDominio>().WithMessage("Usuário inativo não pode receber acesso a documento.");
        documento.PerfisComAcesso.Should().HaveCount(2).And.Contain(perfis);
        documento.UsuariosComAcesso.Should().HaveCount(1).And.Contain(usuarios);
    }

    // ---- Documento de atividade ----

    private static readonly Guid Atividade = Guid.CreateVersion7();

    private static Documento DeAtividade(Guid? cliente = null) =>
        Documento.CriarDeAtividade(Atividade, cliente, null, Arquivo("peticao.pdf"));

    [Fact]
    public void Documento_de_atividade_nasce_com_a_atividade_o_cliente_e_as_listas_vazias()
    {
        var documento = DeAtividade(Cliente);

        documento.Categoria.Should().Be(CategoriaDocumento.DocumentoAtividade);
        documento.AtividadeId.Should().Be(Atividade);
        documento.ClienteId.Should().Be(Cliente);
        documento.Nome.Should().Be("peticao.pdf");
        documento.PerfisComAcesso.Should().BeEmpty();
        documento.UsuariosComAcesso.Should().BeEmpty();
    }

    [Fact]
    public void Documento_de_atividade_avulsa_nasce_sem_cliente()
    {
        var documento = DeAtividade(null);

        documento.ClienteId.Should().BeNull();
        documento.AtividadeId.Should().Be(Atividade);
    }

    [Fact]
    public void Documento_de_atividade_exige_atividade_e_recusa_cliente_vazio()
    {
        var semAtividade = () => Documento.CriarDeAtividade(Guid.Empty, Cliente, null, Arquivo());
        var clienteVazio = () => Documento.CriarDeAtividade(Atividade, Guid.Empty, null, Arquivo());

        semAtividade.Should().Throw<ExcecaoDeDominio>().WithMessage("A atividade do documento é obrigatória.");
        clienteVazio.Should().Throw<ExcecaoDeDominio>().WithMessage("O cliente do documento é obrigatório.");
    }

    [Fact]
    public void Documentos_de_cliente_nao_tem_atividade()
    {
        Geral(_producao).AtividadeId.Should().BeNull();
        Contrato().AtividadeId.Should().BeNull();
    }

    [Fact]
    public void Acesso_a_documento_de_atividade_e_so_ser_diretoria_ou_responsavel()
    {
        var pedro = NovoUsuario("Pedro", _producao);
        var diana = NovoUsuario("Diana", _diretor);
        var documento = DeAtividade(Cliente);

        documento.PodeSerAcessadoPor(diana.Id, _diretor.Id, TipoPerfil.Diretoria).Should().BeTrue("a Diretoria acessa sempre");
        documento.PodeSerAcessadoPor(diana.Id, _diretor.Id, TipoPerfil.Diretoria, ehResponsavelDaAtividade: false).Should().BeTrue();
        documento.PodeSerAcessadoPor(pedro.Id, _producao.Id, TipoPerfil.Producao, ehResponsavelDaAtividade: true).Should().BeTrue();
        documento.PodeSerAcessadoPor(pedro.Id, _producao.Id, TipoPerfil.Producao, ehResponsavelDaAtividade: false).Should().BeFalse();
        documento.PodeSerAcessadoPor(pedro.Id, _producao.Id, TipoPerfil.Producao).Should().BeFalse("o padrão é não ser responsável");
    }

    private static T Construir<T>(params object[] argumentos) =>
        (T)Activator.CreateInstance(
            typeof(T), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, null, argumentos, null)!;

    [Fact]
    public void Constar_nas_listas_nao_da_acesso_a_documento_de_atividade()
    {
        var pedro = NovoUsuario("Pedro", _producao);
        var documento = DeAtividade(Cliente);
        // Não há caminho público para preencher as listas (DefinirAcesso recusa): monta-se por reflexão
        // para provar que, mesmo que algo as preencha, elas não contam.
        var perfis = (List<DocumentoPerfilAcesso>)typeof(Documento)
            .GetField("_perfisComAcesso", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(documento)!;
        var usuarios = (List<DocumentoUsuarioAcesso>)typeof(Documento)
            .GetField("_usuariosComAcesso", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(documento)!;
        perfis.Add(Construir<DocumentoPerfilAcesso>(documento.Id, _producao.Id));
        usuarios.Add(Construir<DocumentoUsuarioAcesso>(documento.Id, pedro.Id));

        documento.PodeSerAcessadoPor(pedro.Id, _producao.Id, TipoPerfil.Producao, ehResponsavelDaAtividade: false)
            .Should().BeFalse();
    }

    [Fact]
    public void Definir_acesso_de_documento_de_atividade_e_recusado()
    {
        var pedro = NovoUsuario("Pedro", _producao);
        var documento = DeAtividade(Cliente);

        var acao = () => documento.DefinirAcesso([_producao], [new UsuarioComPerfil(pedro, _producao)]);

        acao.Should().Throw<ExcecaoDeDominio>()
            .WithMessage("O acesso a um documento de atividade é o dos responsáveis da atividade.");
        documento.PerfisComAcesso.Should().BeEmpty();
        documento.UsuariosComAcesso.Should().BeEmpty();
    }

    [Fact]
    public void Documento_de_atividade_acompanha_a_troca_e_a_retirada_do_cliente()
    {
        var outro = Guid.CreateVersion7();
        var documento = DeAtividade(Cliente);

        documento.AcompanharClienteDaAtividade(outro);
        documento.ClienteId.Should().Be(outro);

        documento.AcompanharClienteDaAtividade(null);
        documento.ClienteId.Should().BeNull();

        documento.AcompanharClienteDaAtividade(Cliente);
        documento.ClienteId.Should().Be(Cliente);
    }

    [Fact]
    public void Documento_de_cliente_nao_acompanha_cliente_de_atividade()
    {
        var geral = () => Geral(_producao).AcompanharClienteDaAtividade(null);
        var contrato = () => Contrato().AcompanharClienteDaAtividade(Guid.CreateVersion7());

        geral.Should().Throw<InvalidOperationException>();
        contrato.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Acompanhar_recusa_cliente_vazio()
    {
        var acao = () => DeAtividade(Cliente).AcompanharClienteDaAtividade(Guid.Empty);

        acao.Should().Throw<ExcecaoDeDominio>().WithMessage("O cliente do documento é obrigatório.");
    }

    [Fact]
    public void Documento_geral_ignora_o_parametro_de_responsavel()
    {
        var ester = NovoUsuario("Ester", _estagio);
        var pedro = NovoUsuario("Pedro", _producao);
        var documento = Geral(_producao);

        documento.PodeSerAcessadoPor(ester.Id, _estagio.Id, TipoPerfil.Producao, ehResponsavelDaAtividade: true)
            .Should().BeFalse("estar fora da lista não é compensado por ser responsável de alguma atividade");
        documento.PodeSerAcessadoPor(pedro.Id, _producao.Id, TipoPerfil.Producao, ehResponsavelDaAtividade: false)
            .Should().BeTrue("a lista continua valendo");
    }

    [Fact]
    public void Contrato_continua_fechado_mesmo_com_responsavel_verdadeiro()
    {
        var pedro = NovoUsuario("Pedro", _producao);

        Contrato().PodeSerAcessadoPor(pedro.Id, _producao.Id, TipoPerfil.Producao, ehResponsavelDaAtividade: true)
            .Should().BeFalse();
    }
}
