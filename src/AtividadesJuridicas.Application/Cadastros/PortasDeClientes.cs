using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Domain.Cadastros;

namespace AtividadesJuridicas.Application.Cadastros;

public static class ClientesOpcoes
{
    public const int TamanhoDaPagina = 20;
}

public interface IClienteRepositorio
{
    Task<Cliente?> ObterPorIdAsync(Guid id, CancellationToken ct);
    Task<bool> ExisteCpfAsync(Cpf cpf, Guid? excetoId, CancellationToken ct);
    Task<bool> ExisteCnpjAsync(Cnpj cnpj, Guid? excetoId, CancellationToken ct);

    /// <summary>
    /// Ordenado por nome. Texto que é um CPF ou CNPJ válido procura pelo documento exato; qualquer
    /// outro procura no nome, sem diferenciar maiúsculas, tratando % e _ como caracteres comuns.
    /// </summary>
    Task<Pagina<Cliente>> ListarAsync(string? busca, bool incluirInativos, int pagina, int tamanho, CancellationToken ct);

    /// <summary>Quantos clientes há no cadastro, ativos e inativos, e quantos deles estão inativos.</summary>
    Task<ContagemDeClientes> ContarAsync(CancellationToken ct);

    void Adicionar(Cliente cliente);

    /// <summary>Os clientes com os Ids informados; os inexistentes simplesmente não vêm.</summary>
    Task<IReadOnlyList<Cliente>> ObterVariosAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct);
}

public sealed record ContagemDeClientes(int Total, int Inativos);

/// <summary>
/// <paramref name="NoCadastro"/> são todos os clientes, ativos e inativos, e <paramref name="Inativos"/> os
/// inativos; nenhum dos dois olha a busca nem o filtro de inativos.
/// </summary>
public sealed record PaginaDeClientes(
    IReadOnlyList<ClienteResumo> Itens, int Total, int Numero, int Tamanho, int NoCadastro, int Inativos)
    : Pagina<ClienteResumo>(Itens, Total, Numero, Tamanho);

public sealed record EnderecoEntrada(
    string? Logradouro = null, string? Numero = null, string? Complemento = null, string? Bairro = null,
    string? Cidade = null, string? Uf = null, string? Cep = null);

public sealed record DadosDoClienteEntrada(
    string Nome, string? Email, string? Telefone, EnderecoEntrada Endereco, string? Observacoes);

/// <summary><paramref name="Documento"/> vem formatado (CPF ou CNPJ).</summary>
public sealed record ClienteResumo(
    Guid Id, TipoCliente Tipo, string Nome, string Documento, string? Email, string? Telefone, string? Cidade, bool Ativo);

/// <summary>CPF e CNPJ vêm formatados; só os campos do tipo do cliente são preenchidos.</summary>
public sealed record ClienteDetalhe(
    Guid Id, TipoCliente Tipo, string Nome, string? Email, string? Telefone,
    string? Logradouro, string? Numero, string? Complemento, string? Bairro, string? Cidade, string? Uf, string? Cep,
    string? Observacoes, bool Ativo,
    string? Cpf, string? Rg, DateOnly? DataNascimento,
    string? Cnpj, string? NomeFantasia, string? NomeResponsavel, string? InscricaoEstadual, string? InscricaoMunicipal);
