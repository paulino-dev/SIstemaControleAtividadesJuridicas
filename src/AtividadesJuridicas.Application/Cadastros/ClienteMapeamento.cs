using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Comum;
using AtividadesJuridicas.Domain.Identidade;

namespace AtividadesJuridicas.Application.Cadastros;

internal static class ClienteMapeamento
{
    /// <summary>Lança <see cref="ExcecaoDeDominio"/> quando o e-mail ou o endereço são inválidos.</summary>
    public static DadosDoCliente ParaDominio(DadosDoClienteEntrada entrada)
    {
        Email? email = null;
        if (!string.IsNullOrWhiteSpace(entrada.Email) && !Email.TentarCriar(entrada.Email, out email))
            throw new ExcecaoDeDominio("E-mail inválido.");

        var e = entrada.Endereco;
        var endereco = Endereco.Criar(e.Logradouro, e.Numero, e.Complemento, e.Bairro, e.Cidade, e.Uf, e.Cep);

        return new DadosDoCliente(entrada.Nome, email, entrada.Telefone, endereco, entrada.Observacoes);
    }

    public static string Documento(Cliente cliente) => cliente switch
    {
        ClientePessoaFisica pf => pf.Cpf.Formatado,
        ClientePessoaJuridica pj => pj.Cnpj.Formatado,
        _ => string.Empty,
    };

    public static ClienteResumo ParaResumo(Cliente c) => new(
        c.Id, c.Tipo, c.Nome, Documento(c), c.Email?.Valor, c.Telefone, c.Endereco.Cidade, c.Ativo);

    public static ClienteDetalhe ParaDetalhe(Cliente c)
    {
        var pf = c as ClientePessoaFisica;
        var pj = c as ClientePessoaJuridica;
        var e = c.Endereco;

        return new ClienteDetalhe(
            c.Id, c.Tipo, c.Nome, c.Email?.Valor, c.Telefone,
            e.Logradouro, e.Numero, e.Complemento, e.Bairro, e.Cidade, e.Uf, e.Cep,
            c.Observacoes, c.Ativo,
            pf?.Cpf.Formatado, pf?.Rg, pf?.DataNascimento,
            pj?.Cnpj.Formatado, pj?.NomeFantasia, pj?.NomeResponsavel, pj?.InscricaoEstadual, pj?.InscricaoMunicipal);
    }
}
