namespace AtividadesJuridicas.Domain.Cadastros;

public sealed class ClientePessoaFisica : Cliente
{
    public const int TamanhoMaximoRg = 20;

    private ClientePessoaFisica() : base(TipoCliente.PessoaFisica) { }

    public Cpf Cpf { get; private set; } = null!;
    public string? Rg { get; private set; }
    public DateOnly? DataNascimento { get; private set; }

    public string NomeCompleto => Nome;

    public static ClientePessoaFisica Criar(DadosDoCliente dados, Cpf cpf, string? rg, DateOnly? dataNascimento)
    {
        var cliente = new ClientePessoaFisica();
        cliente.Alterar(dados, cpf, rg, dataNascimento);
        return cliente;
    }

    public void Alterar(DadosDoCliente dados, Cpf cpf, string? rg, DateOnly? dataNascimento)
    {
        ArgumentNullException.ThrowIfNull(cpf);
        Validar(dados);
        Limite(rg, TamanhoMaximoRg, "RG");

        Aplicar(dados);
        Cpf = cpf;
        Rg = Opcional(rg);
        DataNascimento = dataNascimento;
    }
}
