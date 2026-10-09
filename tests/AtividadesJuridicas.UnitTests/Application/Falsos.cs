using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using AtividadesJuridicas.Application.Agenda;
using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Documentos;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Agenda;
using AtividadesJuridicas.Domain.Atividades;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Documentos;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.UnitTests.Application;

public sealed class AreaAtuacaoRepositorioFalso : IAreaAtuacaoRepositorio
{
    public List<AreaAtuacao> Itens { get; } = [];

    public Task<AreaAtuacao?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Itens.FirstOrDefault(a => a.Id == id));

    public Task<bool> ExisteNomeAsync(string nome, Guid? excetoId, CancellationToken ct) =>
        Task.FromResult(Itens.Any(a =>
            a.Id != excetoId && string.Equals(a.Nome, nome.Trim(), StringComparison.OrdinalIgnoreCase)));

    public Task<IReadOnlyList<AreaAtuacaoResumo>> ListarAsync(bool somenteAtivas, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<AreaAtuacaoResumo>>(
            [.. Itens.Where(a => !somenteAtivas || a.Ativo).OrderBy(a => a.Nome)
                .Select(a => new AreaAtuacaoResumo(a.Id, a.Nome, a.Ativo))]);

    public void Adicionar(AreaAtuacao area) => Itens.Add(area);
}

public sealed class PerfilRepositorioFalso : IPerfilRepositorio
{
    public List<Perfil> Itens { get; } = [];

    public Task<Perfil?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Itens.FirstOrDefault(p => p.Id == id));

    public Task<IReadOnlyList<PerfilResumo>> ListarAtivosAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<PerfilResumo>>(
            [.. Itens.Where(p => p.Ativo).Select(p => new PerfilResumo(p.Id, p.Nome, p.Tipo))]);

    public Task<IReadOnlyList<Perfil>> ObterAtivosAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Perfil>>([.. Itens.Where(p => p.Ativo)]);

    public Task<IReadOnlyList<Perfil>> ObterVariosAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Perfil>>([.. Itens.Where(p => ids.Contains(p.Id))]);

    /// <summary>Preenchido pelo cenário: quantos usuários ativos têm o perfil.</summary>
    public Func<Guid, int> UsuariosAtivosDoPerfil { get; set; } = _ => 0;

    public Task<bool> ExisteNomeAsync(string nome, Guid? excetoId, CancellationToken ct) =>
        Task.FromResult(Itens.Any(p =>
            p.Id != excetoId && string.Equals(p.Nome, nome.Trim(), StringComparison.OrdinalIgnoreCase)));

    public Task<IReadOnlyList<PerfilDetalhe>> ListarAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<PerfilDetalhe>>(
            [.. Itens.OrderBy(p => p.Nome).Select(p => new PerfilDetalhe(p.Id, p.Nome, p.Tipo, p.Ativo, UsuariosAtivosDoPerfil(p.Id)))]);

    public void Adicionar(Perfil perfil) => Itens.Add(perfil);
}

public sealed class UsuarioRepositorioFalso(PerfilRepositorioFalso perfis, ClienteRepositorioFalso clientes) : IUsuarioRepositorio
{
    public List<Usuario> Itens { get; } = [];

    public Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Itens.FirstOrDefault(u => u.Id == id));

    public Task<Usuario?> ObterPorEmailAsync(Email email, CancellationToken ct) =>
        Task.FromResult(Itens.FirstOrDefault(u => u.Email == email));

    public Task<bool> ExisteEmailAsync(Email email, Guid? excetoUsuarioId, CancellationToken ct) =>
        Task.FromResult(Itens.Any(u => u.Email == email && u.Id != excetoUsuarioId));

    public Task<int> ContarAtivosComPerfilAsync(Guid perfilId, CancellationToken ct) =>
        Task.FromResult(Itens.Count(u => u.Ativo && u.PerfilId == perfilId));

    public Task<int> ContarDiretoresAtivosAsync(Guid? excetoUsuarioId, CancellationToken ct) =>
        Task.FromResult(Itens.Count(u =>
            u.Ativo && u.Id != excetoUsuarioId &&
            perfis.Itens.Any(p => p.Id == u.PerfilId && p.Tipo == TipoPerfil.Diretoria)));

    public Task<IReadOnlyList<UsuarioResumo>> ListarAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<UsuarioResumo>>(
            [.. Itens.OrderBy(u => u.Nome).Select(u =>
            {
                var perfil = perfis.Itens.First(p => p.Id == u.PerfilId);
                var cliente = u.ClienteId is { } clienteId
                    ? clientes.Itens.FirstOrDefault(c => c.Id == clienteId)
                    : null;
                return new UsuarioResumo(u.Id, u.Nome, u.Email.Valor, perfil.Id, perfil.Nome,
                    perfil.Tipo, u.NumeroOab, u.Ativo, u.UltimoAcessoEm, u.ClienteId, cliente?.Nome, cliente?.Ativo);
            })]);

    public Task<IReadOnlyList<UsuarioDoCliente>> ListarDoClienteAsync(Guid clienteId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<UsuarioDoCliente>>(
            [.. Itens.Where(u => u.ClienteId == clienteId).OrderBy(u => u.Nome)
                .Select(u => new UsuarioDoCliente(u.Id, u.Nome, u.Email.Valor, u.Ativo, u.UltimoAcessoEm))]);

    public void Adicionar(Usuario usuario) => Itens.Add(usuario);

    public Task<IReadOnlyList<Usuario>> ObterVariosAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Usuario>>([.. Itens.Where(u => ids.Contains(u.Id))]);

    public Task<IReadOnlyList<(Guid Id, string Nome)>> ListarNomesDosAtivosAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<(Guid Id, string Nome)>>(
            [.. Itens.Where(u => u.Ativo && !u.EhDeCliente).OrderBy(u => u.Nome).Select(u => (u.Id, u.Nome))]);

    public int Reservas { get; private set; }

    public int LoginsRegistrados { get; private set; }

    public Task<TentativaDeLogin> ReservarTentativaDeLoginAsync(Guid usuarioId, DateTime agoraUtc, CancellationToken ct)
    {
        Reservas++;
        var usuario = Itens.First(u => u.Id == usuarioId);
        if (usuario.EstaBloqueado(agoraUtc)) return Task.FromResult(TentativaDeLogin.Recusada);
        return Task.FromResult(usuario.RegistrarFalhaDeLogin(agoraUtc)
            ? TentativaDeLogin.AdmitidaEBloqueou
            : TentativaDeLogin.Admitida);
    }

    public Task RegistrarLoginBemSucedidoAsync(Guid usuarioId, DateTime agoraUtc, CancellationToken ct)
    {
        LoginsRegistrados++;
        Itens.First(u => u.Id == usuarioId).RegistrarAcesso(agoraUtc);
        return Task.CompletedTask;
    }
}

public sealed class UnidadeDeTrabalhoFalsa : IUnidadeDeTrabalho
{
    public int Salvamentos { get; private set; }

    /// <summary>Quando definida, é lançada em cada gravação, como o banco faria numa violação.</summary>
    public Exception? Falha { get; set; }

    public Task SalvarAsync(CancellationToken ct)
    {
        if (Falha is not null) throw Falha;
        Salvamentos++;
        return Task.CompletedTask;
    }

    public int Transacoes { get; private set; }

    /// <summary>Verdadeiro enquanto a ação de uma transação está rodando.</summary>
    public bool EmTransacao { get; private set; }

    public async Task EmTransacaoAsync(Func<Task> acao, CancellationToken ct)
    {
        Transacoes++;
        EmTransacao = true;
        try { await acao(); }
        finally { EmTransacao = false; }
    }
}

public sealed class AcessoInicialDePerfilFalso(UnidadeDeTrabalhoFalsa unidade) : IAcessoInicialDePerfil
{
    /// <summary>Quantos documentos gerais "existem" para receber o perfil.</summary>
    public int DocumentosGerais { get; set; }

    public Exception? Falha { get; set; }

    /// <summary>Perfil incluído e se a inclusão aconteceu dentro de uma transação.</summary>
    public List<(Guid PerfilId, bool EmTransacao)> Inclusoes { get; } = [];

    public Task<int> IncluirNosDocumentosGeraisAsync(Guid perfilId, CancellationToken ct)
    {
        if (Falha is not null) throw Falha;
        Inclusoes.Add((perfilId, unidade.EmTransacao));
        return Task.FromResult(DocumentosGerais);
    }
}

public sealed class HasherFalso : IHasherDeSenha
{
    public string Gerar(string senha) => "hash:" + senha;

    public int Verificacoes { get; private set; }

    public string HashDeDescarte => "hash:descarte-que-ninguem-conhece";

    public bool Verificar(string senha, string hash)
    {
        Verificacoes++;
        return hash == "hash:" + senha;
    }
}

public sealed class RelogioFixo : IRelogio
{
    public DateTime AgoraUtc { get; set; } = new(2026, 10, 6, 12, 0, 0, DateTimeKind.Utc);
}

public sealed class ContextoDeAcessoFixo(UsuarioAutenticado? ator) : IContextoDeAcesso
{
    public Task<UsuarioAutenticado?> ObterAsync(CancellationToken ct) => Task.FromResult(ator);
}

public sealed class ClienteRepositorioFalso : IClienteRepositorio
{
    public List<Cliente> Itens { get; } = [];

    public Task<Cliente?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Itens.FirstOrDefault(c => c.Id == id));

    public Task<bool> ExisteCpfAsync(Cpf cpf, Guid? excetoId, CancellationToken ct) =>
        Task.FromResult(Itens.OfType<ClientePessoaFisica>().Any(c => c.Cpf == cpf && c.Id != excetoId));

    public Task<bool> ExisteCnpjAsync(Cnpj cnpj, Guid? excetoId, CancellationToken ct) =>
        Task.FromResult(Itens.OfType<ClientePessoaJuridica>().Any(c => c.Cnpj == cnpj && c.Id != excetoId));

    public Task<Pagina<Cliente>> ListarAsync(string? busca, bool incluirInativos, int pagina, int tamanho, CancellationToken ct)
    {
        IEnumerable<Cliente> consulta = Itens.Where(c => incluirInativos || c.Ativo);

        if (Cpf.TentarCriar(busca, out var cpf))
            consulta = consulta.Where(c => c is ClientePessoaFisica pf && pf.Cpf == cpf);
        else if (Cnpj.TentarCriar(busca, out var cnpj))
            consulta = consulta.Where(c => c is ClientePessoaJuridica pj && pj.Cnpj == cnpj);
        else if (!string.IsNullOrWhiteSpace(busca))
            consulta = consulta.Where(c => SemAcentos(c.Nome).Contains(SemAcentos(busca.Trim()), StringComparison.OrdinalIgnoreCase));

        var todos = consulta.OrderBy(c => c.Nome).ToList();
        return Task.FromResult(new Pagina<Cliente>(
            [.. todos.Skip((pagina - 1) * tamanho).Take(tamanho)], todos.Count, pagina, tamanho));
    }

    public void Adicionar(Cliente cliente) => Itens.Add(cliente);

    public Task<IReadOnlyList<Cliente>> ObterVariosAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Cliente>>([.. Itens.Where(c => ids.Contains(c.Id))]);

    // Mesma semântica do unaccent do banco: remove os diacríticos antes de comparar.
    private static string SemAcentos(string texto) => new(
        [.. texto.Normalize(NormalizationForm.FormD)
            .Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)]);
}

public sealed class ClientesDaIdentidadeFalso(ClienteRepositorioFalso clientes) : IClientesDaIdentidade
{
    public int Consultas { get; private set; }

    public Task<ClienteDoUsuario?> ObterAsync(Guid clienteId, CancellationToken ct)
    {
        Consultas++;
        var cliente = clientes.Itens.FirstOrDefault(c => c.Id == clienteId);
        return Task.FromResult(cliente is null ? null : new ClienteDoUsuario(cliente.Id, cliente.Nome, cliente.Ativo));
    }
}

public sealed class ProcessoRepositorioFalso(AreaAtuacaoRepositorioFalso areas, ClienteRepositorioFalso clientes)
    : IProcessoRepositorio
{
    public List<Processo> Itens { get; } = [];

    public Task<Processo?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Itens.FirstOrDefault(p => p.Id == id));

    public Task<bool> ExisteNumeroCnjAsync(NumeroCnj numero, Guid? excetoId, CancellationToken ct) =>
        Task.FromResult(Itens.Any(p => p.NumeroCnj == numero && p.Id != excetoId));

    public Task<Pagina<ProcessoResumo>> ListarAsync(FiltroDeProcessos filtro, int pagina, int tamanho, CancellationToken ct)
    {
        IEnumerable<Processo> consulta = Itens;
        if (filtro.Status is { } status) consulta = consulta.Where(p => p.Status == status);
        if (filtro.ClienteId is { } clienteId) consulta = consulta.Where(p => p.Clientes.Any(c => c.ClienteId == clienteId));

        if (NumeroCnj.TentarCriar(filtro.Busca, out var numero))
            consulta = consulta.Where(p => p.NumeroCnj == numero);
        else if (!string.IsNullOrWhiteSpace(filtro.Busca))
            consulta = consulta.Where(p => Contem(p.Titulo, filtro.Busca) || Contem(p.ParteContraria, filtro.Busca));

        var todos = consulta.OrderBy(p => p.Titulo).ToList();
        return Task.FromResult(new Pagina<ProcessoResumo>(
            [.. todos.Skip((pagina - 1) * tamanho).Take(tamanho).Select(Resumo)], todos.Count, pagina, tamanho));
    }

    public Task<ProcessoDetalhe?> ObterDetalheAsync(Guid id, CancellationToken ct)
    {
        var p = Itens.FirstOrDefault(x => x.Id == id);
        if (p is null) return Task.FromResult<ProcessoDetalhe?>(null);

        return Task.FromResult<ProcessoDetalhe?>(new ProcessoDetalhe(
            p.Id, p.NumeroCnj?.Formatado, p.Titulo, p.AreaAtuacaoId, NomeDaArea(p.AreaAtuacaoId), p.Status,
            p.Orgao, p.Comarca, p.ParteContraria, p.DataDistribuicao, p.ValorCausa, p.Observacoes,
            [.. p.Clientes.Select(v =>
            {
                var c = clientes.Itens.First(x => x.Id == v.ClienteId);
                return new ClienteDoProcessoDetalhe(c.Id, c.Nome, Documento(c), v.Polo, c.Ativo);
            }).OrderBy(c => c.Nome)]));
    }

    public void Adicionar(Processo processo) => Itens.Add(processo);

    private ProcessoResumo Resumo(Processo p) => new(
        p.Id, p.NumeroCnj?.Formatado, p.Titulo, NomeDaArea(p.AreaAtuacaoId), p.Status,
        [.. p.Clientes.Select(v => clientes.Itens.First(c => c.Id == v.ClienteId).Nome).Order()], p.ParteContraria);

    private string NomeDaArea(Guid id) => areas.Itens.First(a => a.Id == id).Nome;

    private static string Documento(Cliente c) => c switch
    {
        ClientePessoaFisica pf => pf.Cpf.Formatado,
        ClientePessoaJuridica pj => pj.Cnpj.Formatado,
        _ => string.Empty,
    };

    // Mesma semântica da busca real: ignora maiúsculas e acentos.
    private static bool Contem(string? texto, string busca) =>
        texto is not null && SemAcentos(texto).Contains(SemAcentos(busca.Trim()), StringComparison.OrdinalIgnoreCase);

    private static string SemAcentos(string texto) => new([.. texto.Normalize(System.Text.NormalizationForm.FormD)
        .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)]);
}

public sealed class CalendarioFixo : ICalendarioDoEscritorio
{
    public DateOnly Hoje { get; set; } = new(2026, 10, 9);
}

public sealed class AtividadeRepositorioFalso(
    UsuarioRepositorioFalso usuarios, ClienteRepositorioFalso clientes, ProcessoRepositorioFalso processos,
    AreaAtuacaoRepositorioFalso areas) : IAtividadeRepositorio
{
    public List<Atividade> Itens { get; } = [];

    public Task<Atividade?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Itens.FirstOrDefault(a => a.Id == id));

    /// <summary>Simula o que acontece com a atividade entre a primeira leitura e a releitura do cliente atual.</summary>
    public Action<Guid>? AntesDeLerOClienteAtual { get; set; }

    /// <summary>Cliente "do banco" por atividade, no lugar da instância em memória (que aqui é sempre a mesma).</summary>
    public Dictionary<Guid, Guid?> ClienteAtualForcado { get; } = [];

    public Task<ClienteAtualDaAtividade?> ObterClienteAtualAsync(Guid atividadeId, CancellationToken ct)
    {
        AntesDeLerOClienteAtual?.Invoke(atividadeId);
        var atividade = Itens.FirstOrDefault(a => a.Id == atividadeId);
        if (atividade is null) return Task.FromResult<ClienteAtualDaAtividade?>(null);
        return Task.FromResult<ClienteAtualDaAtividade?>(new ClienteAtualDaAtividade(
            ClienteAtualForcado.TryGetValue(atividadeId, out var forcado) ? forcado : atividade.ClienteId));
    }

    public Task<Pagina<AtividadeResumo>> ListarAsync(FiltroDeAtividades filtro, int pagina, int tamanho, CancellationToken ct)
    {
        // Mesma regra da consulta real. A ordem de inserção faz o papel de "mais recentes primeiro".
        var todos = Enumerable.Reverse(Itens)
            .Where(a => filtro.Status is null || a.Status == filtro.Status)
            .Where(a => filtro.ResponsavelId is null || a.EhResponsavel(filtro.ResponsavelId.Value))
            .Where(a => filtro.RevisorId is null || a.RevisorId == filtro.RevisorId)
            .Where(a => filtro.ClienteId is null || a.ClienteId == filtro.ClienteId)
            .Where(a => filtro.ProcessoId is null || a.ProcessoId == filtro.ProcessoId)
            .Where(a => !filtro.SomenteAtrasadas || a.EstaAtrasada(filtro.Hoje))
            .Where(a => string.IsNullOrWhiteSpace(filtro.Busca) ||
                        a.Titulo.Contains(filtro.Busca.Trim(), StringComparison.OrdinalIgnoreCase))
            .OrderBy(a => a.DataEntrega is null).ThenBy(a => a.DataEntrega)
            .ToList();

        string? Nome(Guid? id) => id is null ? null : usuarios.Itens.FirstOrDefault(u => u.Id == id)?.Nome;

        return Task.FromResult(new Pagina<AtividadeResumo>(
            [.. todos.Skip((pagina - 1) * tamanho).Take(tamanho).Select(a => new AtividadeResumo(
                a.Id, a.Titulo, a.Status, a.DataEntrega, a.EstaAtrasada(filtro.Hoje),
                clientes.Itens.FirstOrDefault(c => c.Id == a.ClienteId)?.Nome,
                processos.Itens.FirstOrDefault(p => p.Id == a.ProcessoId)?.Titulo,
                areas.Itens.FirstOrDefault(x => x.Id == a.AreaAtuacaoId)?.Nome,
                [.. a.Responsaveis.Select(r => Nome(r.UsuarioId) ?? string.Empty).Order()],
                Nome(a.RevisorId), a.Complexidade))],
            todos.Count, pagina, tamanho));
    }

    // Mesma regra da consulta real: concluída, no intervalo semiaberto, com o usuário entre os responsáveis.
    public Task<IReadOnlyList<AtividadeRealizada>> ListarRealizadasAsync(
        Guid responsavelId, DateTime deUtc, DateTime ateUtc, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<AtividadeRealizada>>([.. Itens
            .Where(a => a.Status == StatusAtividade.Concluida && a.ConcluidaEm >= deUtc && a.ConcluidaEm < ateUtc
                        && a.EhResponsavel(responsavelId))
            .OrderBy(a => a.ConcluidaEm).ThenBy(a => a.Id)
            .Select(a => new AtividadeRealizada(
                a.Id, a.Titulo, a.Complexidade, a.ConcluidaEm!.Value,
                clientes.Itens.FirstOrDefault(c => c.Id == a.ClienteId)?.Nome))]);

    public Task<DateTime?> ObterPrimeiraConclusaoAsync(Guid responsavelId, CancellationToken ct) =>
        Task.FromResult(Itens
            .Where(a => a.Status == StatusAtividade.Concluida && a.EhResponsavel(responsavelId))
            .Min(a => a.ConcluidaEm));

    public void Adicionar(Atividade atividade) => Itens.Add(atividade);

    public void Remover(Atividade atividade) => Itens.Remove(atividade);
}

public sealed class ComentarioRepositorioFalso(UsuarioRepositorioFalso usuarios) : IComentarioRepositorio
{
    public List<AtividadeComentario> Itens { get; } = [];

    public void Adicionar(AtividadeComentario comentario) => Itens.Add(comentario);

    public Task<IReadOnlyList<ComentarioDetalhe>> ListarDaAtividadeAsync(Guid atividadeId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<ComentarioDetalhe>>([.. Itens
            .Where(c => c.AtividadeId == atividadeId)
            .OrderBy(c => c.DataHora).ThenBy(c => c.Id)
            .Select(c => new ComentarioDetalhe(
                c.Id, c.UsuarioId, usuarios.Itens.FirstOrDefault(u => u.Id == c.UsuarioId)?.Nome, c.Texto, c.DataHora))]);
}

public sealed class AtividadesDoProcessoFalso(AtividadeRepositorioFalso atividades) : IAtividadesDoProcesso
{
    public int ConsultasDeClientes { get; private set; }

    /// <summary>Cada chamada a AcompanharAreaAsync: processo e área pedidos.</summary>
    public List<(Guid ProcessoId, Guid AreaAtuacaoId)> Acompanhamentos { get; } = [];

    public Task<IReadOnlyList<Guid>> ClientesComAtividadeAsync(
        Guid processoId, IReadOnlyCollection<Guid> clienteIds, CancellationToken ct)
    {
        ConsultasDeClientes++;
        return Task.FromResult<IReadOnlyList<Guid>>([.. atividades.Itens
            .Where(a => a.ProcessoId == processoId && a.ClienteId is { } c && clienteIds.Contains(c))
            .Select(a => a.ClienteId!.Value).Distinct()]);
    }

    public Task<int> AcompanharAreaAsync(Guid processoId, Guid areaAtuacaoId, CancellationToken ct)
    {
        Acompanhamentos.Add((processoId, areaAtuacaoId));
        var alteradas = atividades.Itens.Where(a => a.ProcessoId == processoId && a.AreaAtuacaoId != areaAtuacaoId).ToList();
        foreach (var atividade in alteradas) atividade.AcompanharAreaDoProcesso(areaAtuacaoId);
        return Task.FromResult(alteradas.Count);
    }
}

/// <summary>Mesma semântica da consulta real: intervalos semiabertos, evento excluído sai do repositório.</summary>
public sealed class EventoRepositorioFalso(
    UsuarioRepositorioFalso usuarios, ClienteRepositorioFalso clientes, ProcessoRepositorioFalso processos)
    : IEventoRepositorio
{
    public List<Evento> Itens { get; } = [];

    public Task<Evento?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Itens.FirstOrDefault(e => e.Id == id));

    public Task<IReadOnlyList<EventoResumo>> ListarAsync(FiltroDeEventos filtro, CancellationToken ct)
    {
        string? NomeDoUsuario(Guid id) => usuarios.Itens.FirstOrDefault(u => u.Id == id)?.Nome;

        var eventos = Itens
            .Where(e => e.Sobrepoe(filtro.DeUtc, filtro.AteUtc))
            .Where(e => filtro.Tipo is null || e.Tipo == filtro.Tipo)
            .Where(e => filtro.ParticipanteId is null || e.Participa(filtro.ParticipanteId.Value))
            .Where(e => filtro.VisivelPara is not { } v || e.CriadorId == v || e.Participa(v))
            .OrderBy(e => e.InicioEm).ThenBy(e => e.Id)
            .Select(e => new EventoResumo(
                e.Id, e.Titulo, e.Tipo, e.InicioEm, e.FimEm, e.Local,
                clientes.Itens.FirstOrDefault(c => c.Id == e.ClienteId)?.Nome,
                processos.Itens.FirstOrDefault(p => p.Id == e.ProcessoId)?.Titulo,
                [.. e.Participantes.Select(p => NomeDoUsuario(p.UsuarioId) ?? string.Empty)
                    .Order(StringComparer.CurrentCultureIgnoreCase)]))
            .ToList();
        return Task.FromResult<IReadOnlyList<EventoResumo>>(eventos);
    }

    public Task<IReadOnlyList<SobreposicaoDeAgenda>> SobreposicoesAsync(
        Guid? excetoEventoId, IReadOnlyCollection<Guid> usuarioIds, DateTime inicioUtc, DateTime fimUtc, CancellationToken ct)
    {
        var avisos = Itens
            .Where(e => e.Id != excetoEventoId && e.Sobrepoe(inicioUtc, fimUtc))
            .SelectMany(e => e.Participantes.Where(p => usuarioIds.Contains(p.UsuarioId)).Select(p =>
                new SobreposicaoDeAgenda(
                    p.UsuarioId, usuarios.Itens.FirstOrDefault(u => u.Id == p.UsuarioId)?.Nome, e.InicioEm, e.FimEm)))
            .OrderBy(a => a.UsuarioNome, StringComparer.CurrentCultureIgnoreCase).ThenBy(a => a.InicioEm)
            .ToList();
        return Task.FromResult<IReadOnlyList<SobreposicaoDeAgenda>>(avisos);
    }

    public void Adicionar(Evento evento) => Itens.Add(evento);

    public void Remover(Evento evento) => Itens.Remove(evento);
}

/// <summary>Monta um cenário com os dois perfis padrão e um diretor ativo.</summary>
public sealed class Cenario
{
    public PerfilRepositorioFalso Perfis { get; } = new();
    public UsuarioRepositorioFalso Usuarios { get; }
    public UnidadeDeTrabalhoFalsa Unidade { get; } = new();
    public AreaAtuacaoRepositorioFalso Areas { get; } = new();
    public ClienteRepositorioFalso Clientes { get; } = new();
    public ProcessoRepositorioFalso Processos { get; }
    public AtividadeRepositorioFalso Atividades { get; }
    public EventoRepositorioFalso Eventos { get; }
    public AtividadesDoProcessoFalso AtividadesDoProcesso { get; }
    public ComentarioRepositorioFalso Comentarios { get; }
    public CalendarioFixo Calendario { get; } = new();
    public HasherFalso Hasher { get; } = new();
    public RelogioFixo Relogio { get; } = new();
    public AuditoriaFalsa Auditoria { get; } = new();
    public DocumentoRepositorioFalso Documentos { get; }
    public ResponsaveisDeAtividadeFalso ResponsaveisDeAtividade { get; }
    public ArmazenamentoFalso Armazenamento { get; } = new();
    public Perfil PerfilDiretor { get; } = Perfil.Criar("Diretor", TipoPerfil.Diretoria);
    public Perfil PerfilProducao { get; } = Perfil.Criar("Produção", TipoPerfil.Producao);
    /// <summary>Não entra em <see cref="Perfis"/> sozinho: os testes anteriores ao portal contam dois perfis.</summary>
    public Perfil PerfilCliente { get; } = Perfil.Criar("Cliente", TipoPerfil.Cliente);
    public ClientesDaIdentidadeFalso ClientesDaIdentidade { get; }
    public Usuario Diretor { get; }
    public AcessoInicialDePerfilFalso AcessoInicial { get; }

    public Cenario()
    {
        AcessoInicial = new AcessoInicialDePerfilFalso(Unidade);
        Usuarios = new UsuarioRepositorioFalso(Perfis, Clientes);
        Perfis.UsuariosAtivosDoPerfil = id => Usuarios.Itens.Count(u => u.Ativo && u.PerfilId == id);
        ClientesDaIdentidade = new ClientesDaIdentidadeFalso(Clientes);
        Processos = new ProcessoRepositorioFalso(Areas, Clientes);
        Atividades = new AtividadeRepositorioFalso(Usuarios, Clientes, Processos, Areas);
        Eventos = new EventoRepositorioFalso(Usuarios, Clientes, Processos);
        AtividadesDoProcesso = new AtividadesDoProcessoFalso(Atividades);
        Documentos = new DocumentoRepositorioFalso(Atividades);
        ResponsaveisDeAtividade = new ResponsaveisDeAtividadeFalso(Atividades);
        Comentarios = new ComentarioRepositorioFalso(Usuarios);
        Perfis.Itens.AddRange([PerfilDiretor, PerfilProducao]);
        Diretor = NovoUsuario("Diana Diretora", "diana@x.com", PerfilDiretor);
    }

    public Usuario NovoUsuario(string nome, string email, Perfil perfil, string senha = "senha-segura")
    {
        var usuario = Usuario.Criar(nome, Email.Criar(email), Hasher.Gerar(senha), perfil);
        Usuarios.Itens.Add(usuario);
        return usuario;
    }

    public Cliente NovoCliente(string nome = "Maria Silva", string cpf = "529.982.247-25")
    {
        var cliente = ClientePessoaFisica.Criar(new DadosDoCliente(nome, null, null, Endereco.Vazio, null), Cpf.Criar(cpf), null, null);
        Clientes.Itens.Add(cliente);
        return cliente;
    }

    /// <summary>Usuário de perfil Cliente; o perfil entra no cenário na primeira vez em que é usado.</summary>
    public Usuario NovoUsuarioDeCliente(string nome, string email, Cliente cliente, string senha = "senha-segura")
    {
        if (!Perfis.Itens.Contains(PerfilCliente)) Perfis.Itens.Add(PerfilCliente);
        var usuario = Usuario.Criar(nome, Email.Criar(email), Hasher.Gerar(senha), PerfilCliente, null, cliente.Id);
        Usuarios.Itens.Add(usuario);
        return usuario;
    }

    public IContextoDeAcesso Como(Usuario? usuario)
    {
        if (usuario is null) return new ContextoDeAcessoFixo(null);
        var perfil = Perfis.Itens.First(p => p.Id == usuario.PerfilId);
        return new ContextoDeAcessoFixo(new UsuarioAutenticado(usuario.Id, usuario.Nome, perfil.Tipo, perfil.Id, usuario.ClienteId));
    }
}

public sealed class ArmazenamentoFalso : IArmazenamentoDeArquivos
{
    public Dictionary<string, byte[]> Arquivos { get; } = [];

    /// <summary>Simula um arquivo acima do limite, sem precisar de um conteúdo enorme.</summary>
    public bool RecusarPorTamanho { get; set; }

    /// <summary>Simula um armazenamento em que a remoção do arquivo falha.</summary>
    public bool FalharAoRemover { get; set; }

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
        if (FalharAoRemover) throw new IOException("disco indisponível");

        Arquivos.Remove(chave);
        return Task.CompletedTask;
    }
}

public sealed class DocumentoRepositorioFalso(AtividadeRepositorioFalso? atividades = null) : IDocumentoRepositorio
{
    public List<Documento> Itens { get; } = [];

    /// <summary>Quantas vezes os documentos de uma atividade foram pedidos para serem alterados.</summary>
    public int ConsultasPorAtividade { get; set; }

    public Task<Documento?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Itens.FirstOrDefault(d => d.Id == id));

    public Task<Pagina<DocumentoResumo>> ListarDoClienteAsync(
        Guid clienteId, CategoriaDocumento categoria, FiltroDeAcesso? acesso, int pagina, int tamanho, CancellationToken ct)
    {
        // Mesma regra da consulta real: sem filtro (Diretoria) vê tudo; com filtro, nunca contrato nem as categorias do cliente, e o
        // documento de atividade só para quem é responsável hoje de uma atividade viva.
        // O fake não tem relógio: a ordem de inserção faz as vezes da data, do mais recente para o mais antigo.
        var todos = Enumerable.Reverse(Itens)
            .Where(d => d.ClienteId == clienteId && d.Categoria == categoria)
            .Where(d => acesso is null || PodeVer(d, acesso))
            .ToList();

        return Task.FromResult(Pagina(todos, pagina, tamanho));
    }

    public Task<Pagina<DocumentoResumo>> ListarDaAtividadeAsync(Guid atividadeId, int pagina, int tamanho, CancellationToken ct) =>
        Task.FromResult(Pagina([.. Enumerable.Reverse(Itens).Where(d => d.AtividadeId == atividadeId)], pagina, tamanho));

    public Task<IReadOnlyList<Documento>> ObterDaAtividadeAsync(Guid atividadeId, CancellationToken ct)
    {
        ConsultasPorAtividade++;
        return Task.FromResult<IReadOnlyList<Documento>>([.. Itens.Where(d => d.AtividadeId == atividadeId)]);
    }

    /// <summary>O fake não grava o autor: o nome que o repositório real buscaria do usuário que enviou.</summary>
    public string? AutorSimulado { get; set; }

    public Task<Pagina<DocumentoDoPortal>> ListarDoPortalAsync(Guid clienteId, int pagina, int tamanho, CancellationToken ct) =>
        Task.FromResult(DoPortal(
            Enumerable.Reverse(Itens).Where(d => d.Categoria == CategoriaDocumento.DocumentoDoCliente && d.ClienteId == clienteId),
            comAutor: false, pagina, tamanho));

    public Task<Pagina<DocumentoDoPortal>> ListarComunsAsync(bool comAutor, int pagina, int tamanho, CancellationToken ct) =>
        Task.FromResult(DoPortal(
            Enumerable.Reverse(Itens).Where(d => d.Categoria == CategoriaDocumento.DocumentoComum), comAutor, pagina, tamanho));

    private Pagina<DocumentoDoPortal> DoPortal(IEnumerable<Documento> documentos, bool comAutor, int pagina, int tamanho)
    {
        var todos = documentos.ToList();
        return new([.. todos.Skip((pagina - 1) * tamanho).Take(tamanho).Select(d => new DocumentoDoPortal(
                d.Id, d.Nome, d.TamanhoBytes, d.CriadoEm, comAutor ? AutorSimulado : null))],
            todos.Count, pagina, tamanho);
    }

    public void Adicionar(Documento documento) => Itens.Add(documento);

    public void Remover(Documento documento) => Itens.Remove(documento);

    private bool PodeVer(Documento d, FiltroDeAcesso acesso)
    {
        if (d.Categoria is CategoriaDocumento.ContratoHonorarios or CategoriaDocumento.DocumentoDoCliente
            or CategoriaDocumento.DocumentoComum) return false;
        if (d.Categoria == CategoriaDocumento.DocumentoAtividade)
            return atividades?.Itens.Any(a => a.Id == d.AtividadeId && a.EhResponsavel(acesso.UsuarioId)) ?? false;
        return d.PerfisComAcesso.Any(a => a.PerfilId == acesso.PerfilId) ||
               d.UsuariosComAcesso.Any(a => a.UsuarioId == acesso.UsuarioId);
    }

    private Pagina<DocumentoResumo> Pagina(List<Documento> todos, int pagina, int tamanho) =>
        new([.. todos.Skip((pagina - 1) * tamanho).Take(tamanho).Select(d => new DocumentoResumo(
                d.Id, d.Nome, d.NomeArquivoOriginal, d.TamanhoBytes, d.Categoria, d.CriadoEm, null,
                d.AtividadeId, atividades?.Itens.FirstOrDefault(a => a.Id == d.AtividadeId)?.Titulo))],
            todos.Count, pagina, tamanho);
}

/// <summary>Responsável atual de atividade viva: a atividade ainda está no repositório e o usuário consta nela.</summary>
public sealed class ResponsaveisDeAtividadeFalso(AtividadeRepositorioFalso atividades) : IResponsaveisDeAtividade
{
    public Task<bool> EhResponsavelAsync(Guid atividadeId, Guid usuarioId, CancellationToken ct) =>
        Task.FromResult(atividades.Itens.Any(a => a.Id == atividadeId && a.EhResponsavel(usuarioId)));
}

public sealed record EventoAnotado(string Evento, string? Entidade, Guid? EntidadeId, object? Dados, bool Imediato, Guid? Ator = null);

public sealed class AuditoriaFalsa : IAuditoria
{
    public List<EventoAnotado> Eventos { get; } = [];

    public List<IConsultaAuditavel> Acessos { get; } = [];

    public Task RegistrarAcessoAsync(IConsultaAuditavel consulta, CancellationToken ct = default)
    {
        Acessos.Add(consulta);
        return Task.CompletedTask;
    }

    public void Anotar(string evento, string? entidade = null, Guid? entidadeId = null, object? dados = null) =>
        Eventos.Add(new EventoAnotado(evento, entidade, entidadeId, dados, Imediato: false));

    public Task RegistrarAsync(string evento, string? entidade = null, Guid? entidadeId = null, object? dados = null,
        Guid? ator = null, CancellationToken ct = default)
    {
        Eventos.Add(new EventoAnotado(evento, entidade, entidadeId, dados, Imediato: true, ator));
        return Task.CompletedTask;
    }
}
