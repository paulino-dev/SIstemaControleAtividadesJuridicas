using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Comum;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.Domain.Atividades;

public enum StatusAtividade
{
    Criada = 1,
    Direcionada = 2,
    Feita = 3,
    ParaCorrecao = 4,
    Corrigida = 5,
    Concluida = 6,
    Reaberta = 7,
}

public enum TipoAutor
{
    Usuario = 1,
    Sistema = 2,
}

/// <summary>Um usuário como a atividade precisa conhecê-lo agora: quem é, se está ativo e de que tipo é o seu perfil.</summary>
public sealed record PessoaDaAtividade(Guid UsuarioId, TipoPerfil Tipo, bool Ativo)
{
    public bool EhDiretoriaAtiva => Ativo && Tipo == TipoPerfil.Diretoria;

    public static PessoaDaAtividade De(Usuario usuario, Perfil perfil)
    {
        ArgumentNullException.ThrowIfNull(usuario);
        ArgumentNullException.ThrowIfNull(perfil);
        if (usuario.PerfilId != perfil.Id)
            throw new ArgumentException("O perfil informado não é o do usuário.", nameof(perfil));

        return new PessoaDaAtividade(usuario.Id, perfil.Tipo, usuario.Ativo);
    }
}

public sealed record ResponsavelInformado(PessoaDaAtividade Pessoa, string? Direcionamento);

public sealed record DadosDaAtividade(string Titulo, string Descricao, DateOnly? DataEntrega);

/// <summary>Cliente, processo e área de uma atividade, já coerentes entre si.</summary>
public sealed record VinculoDaAtividade
{
    private VinculoDaAtividade(Guid? clienteId, Guid? processoId, Guid? areaAtuacaoId)
    {
        ClienteId = clienteId;
        ProcessoId = processoId;
        AreaAtuacaoId = areaAtuacaoId;
    }

    public Guid? ClienteId { get; }
    public Guid? ProcessoId { get; }
    public Guid? AreaAtuacaoId { get; }

    /// <summary>Sem cliente, sem processo e sem área.</summary>
    public static VinculoDaAtividade Avulsa { get; } = new(null, null, null);

    /// <summary>
    /// Com processo, o cliente é obrigatório, tem de ser um dos clientes do processo, e a área é a do
    /// processo (a informada é ignorada). Sem processo, cliente e área são opcionais.
    /// </summary>
    public static VinculoDaAtividade Criar(Guid? clienteId, Processo? processo, Guid? areaAtuacaoId)
    {
        if (clienteId == Guid.Empty)
            throw new ExcecaoDeDominio("Cliente inválido.");
        if (areaAtuacaoId == Guid.Empty)
            throw new ExcecaoDeDominio("Área de atuação inválida.");

        if (processo is null) return new VinculoDaAtividade(clienteId, null, areaAtuacaoId);

        if (clienteId is null)
            throw new ExcecaoDeDominio("Com processo, o cliente da atividade é obrigatório.");
        if (processo.Clientes.All(c => c.ClienteId != clienteId))
            throw new ExcecaoDeDominio("O cliente da atividade deve ser um dos clientes do processo.");

        return new VinculoDaAtividade(clienteId, processo.Id, processo.AreaAtuacaoId);
    }
}

/// <summary>O que mudou nos responsáveis de uma atividade, pelos Ids dos usuários.</summary>
public sealed record AlteracaoDeResponsaveis(
    IReadOnlyList<Guid> Adicionados, IReadOnlyList<Guid> Removidos, IReadOnlyList<Guid> DirecionamentoAlterado)
{
    public bool Houve => Adicionados.Count > 0 || Removidos.Count > 0 || DirecionamentoAlterado.Count > 0;
}
