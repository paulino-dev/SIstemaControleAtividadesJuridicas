using System.Text;
using System.Text.Json.Nodes;
using AtividadesJuridicas.Application.Agenda;
using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Documentos;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Agenda;
using AtividadesJuridicas.Domain.Atividades;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Documentos;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class PortalDoClienteTests(BancoFixture banco) : TesteComBanco(banco)
{
    private const string Invalido = "E-mail ou senha inválidos.";
    private const string NovaSenha = "nova-senha-da-carla";

    private static readonly DateTime Inicio = new(2090, 6, 15, 13, 0, 0, DateTimeKind.Utc);

    private Guid _admin, _pedro, _carla, _maria, _jose, _processo, _atividade, _evento, _geral, _contrato;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _admin = await IdDoAdminAsync();
        var producao = await IdDoPerfilAsync(TipoPerfil.Producao);
        _pedro = (await ExecutarAsync(_admin, new CriarUsuario("Pedro", "pedro@x.com", "senha-do-pedro", producao, null))).Valor;
        _maria = await ClienteAsync("Maria Silva", "529.982.247-25");
        _jose = await ClienteAsync("José Souza", "111.444.777-35");

        var area = (await ExecutarAsync(_admin, new SalvarAreaAtuacao(null, "Trabalhista"))).Valor;
        var processo = await ExecutarAsync(_admin, new SalvarProcesso(
            null, new DadosDoProcessoEntrada(null, "Reclamação trabalhista", area, null, null, null, null, null, null),
            [new ClienteDoProcessoEntrada(_maria, PoloProcessual.Ativo)]));
        processo.Sucesso.Should().BeTrue(processo.Mensagem);
        _processo = processo.Valor;

        var atividade = await ExecutarAsync(_admin, new SalvarAtividade(
            null, "Elaborar contestação", "Prazo de 15 dias.", new DateOnly(2090, 6, 20), _maria, null, null, 1));
        atividade.Sucesso.Should().BeTrue(atividade.Mensagem);
        _atividade = atividade.Valor;
        (await ExecutarAsync(_admin, new DefinirResponsaveisDaAtividade(
            _atividade, [new ResponsavelEntrada(_pedro, "Redigir")], _admin))).Sucesso.Should().BeTrue();

        _evento = await EventoAsync();
        _geral = await EnviarAsync(CategoriaDocumento.DocumentoGeral, "procuracao.pdf");
        _contrato = await EnviarAsync(CategoriaDocumento.ContratoHonorarios, "contrato.pdf");

        _carla = await EmEscopoAsync(_admin, sp => UsuariosDeClienteDeTeste.GravarAsync(sp, _maria));
    }

    private async Task<Guid> ClienteAsync(string nome, string cpf) =>
        (await ExecutarAsync(_admin, new SalvarClientePessoaFisica(
            null, new DadosDoClienteEntrada(nome, null, null, new EnderecoEntrada(), null), cpf, null, null))).Valor;

    private async Task<Guid> EventoAsync()
    {
        var criado = await ExecutarAsync(_admin, new SalvarEvento(
            null, "Audiência de instrução", "Levar os documentos.", "Fórum", TipoEvento.Audiencia, Inicio, 60,
            null, null, null, [], null));
        criado.Sucesso.Should().BeTrue(criado.Mensagem);
        return criado.Valor!.Id;
    }

    private async Task<Guid> EnviarAsync(CategoriaDocumento categoria, string arquivo)
    {
        var resultado = await ExecutarAsync(_admin, new EnviarDocumento(
            categoria, _maria, null, arquivo, "application/pdf", new MemoryStream(Encoding.UTF8.GetBytes("conteúdo"))));
        resultado.Sucesso.Should().BeTrue(resultado.Mensagem);
        return resultado.Valor;
    }

    /// <summary>Executa como a Carla e devolve o resultado com o valor, se houver.</summary>
    private async Task<(string Requisicao, Resultado Resultado)> ComoCarlaAsync<TResultado>(IRequisicao<TResultado> requisicao)
        where TResultado : Resultado =>
        (requisicao.GetType().Name, await ExecutarAsync(_carla, requisicao));

    private Task<List<RegistroAuditoria>> EventosAsync(string evento) =>
        EmEscopoAsync(null, sp => sp.GetRequiredService<AppDbContext>().RegistrosAuditoria.AsNoTracking()
            .Where(r => r.Evento == evento).OrderBy(r => r.Id).ToListAsync());

    private Task<Documento> DocumentoNoBancoAsync(Guid id) => EmEscopoAsync(null, sp =>
        sp.GetRequiredService<AppDbContext>().Documentos.IgnoreQueryFilters().AsNoTracking()
            .Include(d => d.PerfisComAcesso).Include(d => d.UsuariosComAcesso).SingleAsync(d => d.Id == id));

    [Fact]
    public async Task Cliente_nao_alcanca_nenhum_caso_de_uso_interno_e_cada_tentativa_fica_registrada()
    {
        var pedidos = new Func<Task<(string, Resultado)>>[]
        {
            () => ComoCarlaAsync(new ListarClientes()),
            () => ComoCarlaAsync(new ObterCliente(_maria)),
            () => ComoCarlaAsync(new ObterCliente(_jose)),
            () => ComoCarlaAsync(new ListarProcessos()),
            () => ComoCarlaAsync(new ObterProcesso(_processo)),
            () => ComoCarlaAsync(new ListarAreasAtuacao()),
            () => ComoCarlaAsync(new ListarAtividades()),
            () => ComoCarlaAsync(new ObterAtividade(_atividade)),
            () => ComoCarlaAsync(new ListarEventos(Inicio.Date, Inicio.Date.AddDays(1))),
            () => ComoCarlaAsync(new ObterEvento(_evento)),
            () => ComoCarlaAsync(new ListarUsuariosParaAgenda()),
            () => ComoCarlaAsync(new ListarDocumentosDoCliente(_maria, CategoriaDocumento.DocumentoGeral)),
            () => ComoCarlaAsync(new ListarUsuarios()),
            () => ComoCarlaAsync(new ConsultarAuditoria(new FiltroDeAuditoria())),
            () => ComoCarlaAsync(new SalvarEvento(
                null, "Reunião", null, null, TipoEvento.Reuniao, Inicio, 30, null, null, null, [], null)),
            () => ComoCarlaAsync(new ComentarAtividade(_atividade, "Oi")),
        };
        var tentativas = new List<(string Requisicao, Resultado Resultado)>();
        foreach (var tentar in pedidos) tentativas.Add(await tentar());

        foreach (var (requisicao, resultado) in tentativas)
        {
            resultado.Erro.Should().Be(TipoErro.AcessoNegado, requisicao);
            // Sem valor: nulo, ou o padrão de um tipo de valor (ComentarAtividade responde com Resultado<Guid>).
            var valor = resultado.GetType().GetProperty("Valor")?.GetValue(resultado);
            (valor is null || (valor.GetType().IsValueType && valor.Equals(Activator.CreateInstance(valor.GetType()))))
                .Should().BeTrue(requisicao);
        }

        var negados = (await EventosAsync(EventosDeSeguranca.AcessoNegado)).Where(e => e.UsuarioId == _carla).ToList();
        negados.Should().HaveCount(tentativas.Count);
        negados.Select(e => (string?)JsonNode.Parse(e.Dados!)!["requisicao"])
            .Should().BeEquivalentTo(tentativas.Select(t => t.Requisicao));

        var acessos = await EmEscopoAsync(null, sp => sp.GetRequiredService<AppDbContext>().RegistrosAcesso.AsNoTracking()
            .Where(r => r.UsuarioId == _carla).ToListAsync());
        acessos.Should().BeEmpty();
    }

    [Fact]
    public async Task Download_de_documento_interno_pelo_cliente_e_negado_como_nao_encontrado_e_fica_registrado()
    {
        foreach (var documento in new[] { _geral, _contrato })
        {
            var resultado = await ExecutarAsync(_carla, new BaixarDocumento(documento));

            resultado.Erro.Should().Be(TipoErro.NaoEncontrado);
            resultado.Valor.Should().BeNull();
        }

        var negados = (await EventosAsync(EventosDeSeguranca.AcessoNegado)).Where(e => e.UsuarioId == _carla).ToList();
        negados.Select(e => e.EntidadeId).Should().BeEquivalentTo([_geral, _contrato]);
        negados.Should().OnlyContain(e => e.Entidade == "Documento");
        negados.Select(e => (string?)JsonNode.Parse(e.Dados!)!["resposta"]).Should().OnlyContain(r => r == "nao_encontrado");
    }

    [Fact]
    public async Task Cliente_consulta_o_proprio_acesso_e_troca_a_propria_senha()
    {
        var acesso = await ExecutarAsync(_carla, new ObterMeuAcesso());
        acesso.Should().NotBeNull();
        acesso!.EhCliente.Should().BeTrue();
        acesso.ClienteId.Should().Be(_maria);

        (await ExecutarAsync(_carla, new AlterarMinhaSenha(UsuariosDeClienteDeTeste.SenhaDaCarla, NovaSenha)))
            .Sucesso.Should().BeTrue();

        (await ExecutarAsync(null, new AutenticarUsuario(UsuariosDeClienteDeTeste.EmailDaCarla, NovaSenha)))
            .Sucesso.Should().BeTrue();
        (await ExecutarAsync(null, new AutenticarUsuario(UsuariosDeClienteDeTeste.EmailDaCarla, UsuariosDeClienteDeTeste.SenhaDaCarla)))
            .Sucesso.Should().BeFalse();
    }

    [Fact]
    public async Task Usuario_de_cliente_inativo_nao_entra_e_perde_o_acesso_e_reativar_devolve()
    {
        (await ExecutarAsync(_admin, new DefinirAtivoCliente(_maria, false))).Sucesso.Should().BeTrue();

        var login = await ExecutarAsync(null, new AutenticarUsuario(
            UsuariosDeClienteDeTeste.EmailDaCarla, UsuariosDeClienteDeTeste.SenhaDaCarla));
        login.Erro.Should().Be(TipoErro.AcessoNegado);
        login.Mensagem.Should().Be(Invalido);
        var falha = (await EventosAsync(EventosDeSeguranca.LoginFalha)).Should().ContainSingle().Subject;
        falha.Entidade.Should().Be("Usuario");
        falha.EntidadeId.Should().Be(_carla);
        ((string?)JsonNode.Parse(falha.Dados!)!["motivo"]).Should().Be("cliente_inativo");

        (await ExecutarAsync(_carla, new ObterMeuAcesso())).Should().BeNull();
        (await ExecutarAsync(_carla, new AlterarMinhaSenha(UsuariosDeClienteDeTeste.SenhaDaCarla, NovaSenha)))
            .Erro.Should().Be(TipoErro.AcessoNegado);

        (await ExecutarAsync(_admin, new DefinirAtivoCliente(_maria, true))).Sucesso.Should().BeTrue();

        (await ExecutarAsync(null, new AutenticarUsuario(
            UsuariosDeClienteDeTeste.EmailDaCarla, UsuariosDeClienteDeTeste.SenhaDaCarla))).Sucesso.Should().BeTrue();
        (await ExecutarAsync(_carla, new ObterMeuAcesso())).Should().NotBeNull();
    }

    [Fact]
    public async Task Documento_geral_continua_sendo_enviado_e_nasce_so_com_os_perfis_internos()
    {
        var id = await EnviarAsync(CategoriaDocumento.DocumentoGeral, "outro.pdf");

        var documento = await DocumentoNoBancoAsync(id);

        documento.PerfisComAcesso.Select(a => a.PerfilId).Should().BeEquivalentTo(
            [await IdDoPerfilAsync(TipoPerfil.Diretoria), await IdDoPerfilAsync(TipoPerfil.Producao)]);
    }

    [Fact]
    public async Task Evento_recusa_usuario_de_cliente_como_participante()
    {
        var resultado = await ExecutarAsync(_admin, new SalvarEvento(
            null, "Reunião com a cliente", null, null, TipoEvento.Reuniao, Inicio, 30, null, null, null, [_carla], null));

        resultado.Erro.Should().Be(TipoErro.Validacao);
        resultado.Mensagem.Should().Be("Usuário de cliente não pode participar de evento.");
        var eventos = await EmEscopoAsync(null, sp => sp.GetRequiredService<AppDbContext>().Eventos.AsNoTracking().ToListAsync());
        eventos.Select(e => e.Id).Should().Equal(_evento);
    }

    [Fact]
    public async Task Atividade_recusa_usuario_de_cliente_como_responsavel()
    {
        var resultado = await ExecutarAsync(_admin, new DefinirResponsaveisDaAtividade(
            _atividade, [new ResponsavelEntrada(_carla, "Redigir")], _admin));

        resultado.Erro.Should().Be(TipoErro.Validacao);
        resultado.Mensagem.Should().Be("Usuário de cliente não pode ser responsável por atividade.");
        var responsaveis = await EmEscopoAsync(null, sp => sp.GetRequiredService<AppDbContext>()
            .Set<AtividadeResponsavel>().AsNoTracking().Where(r => r.AtividadeId == _atividade).ToListAsync());
        responsaveis.Select(r => r.UsuarioId).Should().Equal(_pedro);
    }

    [Fact]
    public async Task Lista_de_acesso_recusa_usuario_e_perfil_de_cliente()
    {
        const string mensagem = "Usuário ou perfil de cliente não entra em lista de acesso de documento.";
        var antes = (await DocumentoNoBancoAsync(_geral)).PerfisComAcesso.Select(a => a.PerfilId).ToList();
        var perfilCliente = await IdDoPerfilAsync(TipoPerfil.Cliente);

        var comPerfil = await ExecutarAsync(_admin, new DefinirAcessoDoDocumento(_geral, [perfilCliente], []));
        var comUsuario = await ExecutarAsync(_admin, new DefinirAcessoDoDocumento(_geral, [.. antes], [_carla]));

        comPerfil.Erro.Should().Be(TipoErro.Validacao);
        comPerfil.Mensagem.Should().Be(mensagem);
        comUsuario.Erro.Should().Be(TipoErro.Validacao);
        comUsuario.Mensagem.Should().Be(mensagem);
        var depois = await DocumentoNoBancoAsync(_geral);
        depois.PerfisComAcesso.Select(a => a.PerfilId).Should().BeEquivalentTo(antes);
        depois.UsuariosComAcesso.Should().BeEmpty();
    }

    private Task<Guid> VersaoDeSessaoAsync(Guid usuario) => EmEscopoAsync(null, async sp =>
        (await sp.GetRequiredService<AppDbContext>().Usuarios.AsNoTracking().SingleAsync(u => u.Id == usuario)).VersaoDeSessao);

    [Fact]
    public async Task Usuario_de_cliente_e_criado_pelo_caso_de_uso_e_aparece_na_lista_do_cliente()
    {
        var lista = (await ExecutarAsync(_admin, new ListarUsuarios())).Valor!;
        var daCarla = lista.Single(u => u.Id == _carla);
        daCarla.ClienteId.Should().Be(_maria);
        daCarla.ClienteNome.Should().Be("Maria Silva");
        daCarla.TipoPerfil.Should().Be(TipoPerfil.Cliente);
        lista.Where(u => u.Id != _carla).Should().OnlyContain(u => u.ClienteId == null && u.ClienteNome == null);

        var doCliente = (await ExecutarAsync(_admin, new ListarUsuariosDoCliente(_maria))).Valor!;
        doCliente.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            Id = _carla, Nome = "Carla Cliente", Email = UsuariosDeClienteDeTeste.EmailDaCarla, Ativo = true,
        });
        (await ExecutarAsync(_admin, new ListarUsuariosDoCliente(_jose))).Valor.Should().BeEmpty();

        var acessos = await EmEscopoAsync(null, sp => sp.GetRequiredService<AppDbContext>().RegistrosAcesso.AsNoTracking()
            .Where(r => r.UsuarioId == _admin && r.TipoRecurso == "Usuario" && r.Detalhe != null && r.Detalhe.Contains(_maria.ToString()))
            .ToListAsync());
        acessos.Should().ContainSingle().Which.Acao.Should().Be(AcaoDeAcesso.Listou);
    }

    [Fact]
    public async Task Lista_de_usuarios_mostra_o_cliente_inativo_no_banco()
    {
        (await ExecutarAsync(_admin, new ListarUsuarios())).Valor!.Single(u => u.Id == _carla)
            .ClienteAtivo.Should().BeTrue();
        (await ExecutarAsync(_admin, new DefinirAtivoCliente(_maria, false))).Sucesso.Should().BeTrue();

        var lista = (await ExecutarAsync(_admin, new ListarUsuarios())).Valor!;

        var daCarla = lista.Single(u => u.Id == _carla);
        daCarla.ClienteAtivo.Should().BeFalse();
        daCarla.ClienteNome.Should().Be("Maria Silva");
        lista.Where(u => u.Id != _carla).Should().OnlyContain(u => u.ClienteAtivo == null);
    }

    [Fact]
    public async Task Par_perfil_cliente_e_recusado_pelos_casos_de_uso_no_banco()
    {
        var perfilCliente = await IdDoPerfilAsync(TipoPerfil.Cliente);
        var perfilProducao = await IdDoPerfilAsync(TipoPerfil.Producao);
        var usuariosAntes = (await ExecutarAsync(_admin, new ListarUsuarios())).Valor!;

        var semCliente = await ExecutarAsync(_admin, new CriarUsuario(
            "Ana Cliente", "ana@x.com", "senha-da-ana", perfilCliente, null));
        var internoComCliente = await ExecutarAsync(_admin, new CriarUsuario(
            "Beto", "beto@x.com", "senha-do-beto", perfilProducao, null, _maria));
        var pedroParaCliente = await ExecutarAsync(_admin, new EditarUsuario(
            _pedro, "Pedro", "pedro@x.com", perfilCliente, null, null));
        var carlaParaProducao = await ExecutarAsync(_admin, new EditarUsuario(
            _carla, "Carla Cliente", UsuariosDeClienteDeTeste.EmailDaCarla, perfilProducao, null, null));

        semCliente.Erro.Should().Be(TipoErro.Validacao);
        semCliente.Mensagem.Should().Be("Informe o cliente do usuário.");
        internoComCliente.Erro.Should().Be(TipoErro.Validacao);
        internoComCliente.Mensagem.Should().Be("Só um usuário de perfil Cliente é vinculado a um cliente.");
        const string troca = "Um usuário de cliente não pode receber um perfil interno, nem um usuário interno o perfil de cliente.";
        pedroParaCliente.Erro.Should().Be(TipoErro.Validacao);
        pedroParaCliente.Mensagem.Should().Be(troca);
        carlaParaProducao.Erro.Should().Be(TipoErro.Validacao);
        carlaParaProducao.Mensagem.Should().Be(troca);

        var usuarios = await EmEscopoAsync(null, sp =>
            sp.GetRequiredService<AppDbContext>().Usuarios.AsNoTracking().ToListAsync());
        usuarios.Select(u => u.Id).Should().BeEquivalentTo(usuariosAntes.Select(u => u.Id), "nenhum usuário novo");
        var pedro = usuarios.Single(u => u.Id == _pedro);
        pedro.PerfilId.Should().Be(perfilProducao);
        pedro.ClienteId.Should().BeNull();
        var carla = usuarios.Single(u => u.Id == _carla);
        carla.PerfilId.Should().Be(perfilCliente);
        carla.ClienteId.Should().Be(_maria);
    }

    [Fact]
    public async Task Trocar_o_cliente_derruba_a_sessao_e_fica_auditado()
    {
        var sessaoAntes = await VersaoDeSessaoAsync(_carla);
        var perfilCliente = await IdDoPerfilAsync(TipoPerfil.Cliente);

        var troca = await ExecutarAsync(_admin, new EditarUsuario(
            _carla, "Carla Cliente", UsuariosDeClienteDeTeste.EmailDaCarla, perfilCliente, null, _jose));

        troca.Sucesso.Should().BeTrue(troca.Mensagem);
        (await VersaoDeSessaoAsync(_carla)).Should().NotBe(sessaoAntes);
        var evento = (await EventosAsync(EventosDeSeguranca.UsuarioClienteAlterado)).Should().ContainSingle().Subject;
        evento.Entidade.Should().Be("Usuario");
        evento.EntidadeId.Should().Be(_carla);
        evento.UsuarioId.Should().Be(_admin);
        var dados = JsonNode.Parse(evento.Dados!)!;
        ((Guid)dados["de"]!).Should().Be(_maria);
        ((Guid)dados["para"]!).Should().Be(_jose);
        var alteracao = (await AuditoriaAsync()).Should().ContainSingle(r =>
            r.Entidade == "Usuario" && r.EntidadeId == _carla && r.Operacao == OperacaoAuditada.Alteracao).Subject;
        evento.IdCorrelacao.Should().NotBeNull().And.Be(alteracao.IdCorrelacao);

        var acesso = await ExecutarAsync(_carla, new ObterMeuAcesso());
        acesso!.ClienteId.Should().Be(_jose);
    }

    [Fact]
    public async Task Usuario_de_cliente_nao_conta_como_diretor_nem_aparece_para_a_agenda()
    {
        var inativar = await ExecutarAsync(_admin, new DefinirAtivoUsuario(_admin, false));

        inativar.Erro.Should().Be(TipoErro.Conflito);
        inativar.Mensagem.Should().Be("O sistema precisa de ao menos um usuário ativo de Diretoria.");
        var agenda = (await ExecutarAsync(_admin, new ListarUsuariosParaAgenda())).Valor!;
        agenda.Select(u => u.Id).Should().NotContain(_carla);
        agenda.Select(u => u.Id).Should().BeEquivalentTo([_admin, _pedro]);
    }

    [Fact]
    public async Task Criar_perfil_de_tipo_cliente_e_recusado_no_banco()
    {
        var resultado = await ExecutarAsync(_admin, new SalvarPerfil(null, "Portal", TipoPerfil.Cliente));

        resultado.Erro.Should().Be(TipoErro.Validacao);
        resultado.Mensagem.Should().Be("O perfil de cliente é único e já existe.");
        var perfis = await EmEscopoAsync(null, sp =>
            sp.GetRequiredService<AppDbContext>().Perfis.IgnoreQueryFilters().AsNoTracking().ToListAsync());
        perfis.Should().ContainSingle(p => p.Tipo == TipoPerfil.Cliente);
        perfis.Should().NotContain(p => p.Nome == "Portal");
    }
}
