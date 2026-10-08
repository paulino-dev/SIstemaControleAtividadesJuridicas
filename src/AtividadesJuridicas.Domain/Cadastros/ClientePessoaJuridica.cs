namespace AtividadesJuridicas.Domain.Cadastros;

public sealed class ClientePessoaJuridica : Cliente
{
    public const int TamanhoMaximoInscricao = 30;

    private ClientePessoaJuridica() : base(TipoCliente.PessoaJuridica) { }

    public Cnpj Cnpj { get; private set; } = null!;
    public string? NomeFantasia { get; private set; }
    public string? NomeResponsavel { get; private set; }
    public string? InscricaoEstadual { get; private set; }
    public string? InscricaoMunicipal { get; private set; }

    public string RazaoSocial => Nome;

    public static ClientePessoaJuridica Criar(
        DadosDoCliente dados, Cnpj cnpj, string? nomeFantasia, string? nomeResponsavel,
        string? inscricaoEstadual, string? inscricaoMunicipal)
    {
        var cliente = new ClientePessoaJuridica();
        cliente.Alterar(dados, cnpj, nomeFantasia, nomeResponsavel, inscricaoEstadual, inscricaoMunicipal);
        return cliente;
    }

    public void Alterar(
        DadosDoCliente dados, Cnpj cnpj, string? nomeFantasia, string? nomeResponsavel,
        string? inscricaoEstadual, string? inscricaoMunicipal)
    {
        ArgumentNullException.ThrowIfNull(cnpj);
        Validar(dados);
        Limite(nomeFantasia, TamanhoMaximoNome, "nome fantasia");
        Limite(nomeResponsavel, TamanhoMaximoNome, "nome do responsável");
        Limite(inscricaoEstadual, TamanhoMaximoInscricao, "inscrição estadual");
        Limite(inscricaoMunicipal, TamanhoMaximoInscricao, "inscrição municipal");

        Aplicar(dados);
        Cnpj = cnpj;
        NomeFantasia = Opcional(nomeFantasia);
        NomeResponsavel = Opcional(nomeResponsavel);
        InscricaoEstadual = Opcional(inscricaoEstadual);
        InscricaoMunicipal = Opcional(inscricaoMunicipal);
    }
}
