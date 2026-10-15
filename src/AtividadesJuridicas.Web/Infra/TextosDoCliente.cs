using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Domain.Cadastros;

namespace AtividadesJuridicas.Web.Infra;

/// <summary>As frases da tela de clientes que dependem de quantos usuários do portal o cliente tem.</summary>
public static class TextosDoCliente
{
    public static string Tipo(TipoCliente tipo) => tipo == TipoCliente.PessoaFisica ? "Pessoa física" : "Pessoa jurídica";

    /// <summary>O documento com o nome dele: "CPF 529.982.247-25", "CNPJ 00.000.000/0002-00".</summary>
    public static string Documento(TipoCliente tipo, string documento) =>
        $"{(tipo == TipoCliente.PessoaFisica ? "CPF" : "CNPJ")} {documento}";

    /// <summary>O endereço numa linha, só com as partes preenchidas; nulo quando não há nenhuma.</summary>
    public static string? Endereco(ClienteDetalhe cliente)
    {
        static bool Tem(string? parte) => !string.IsNullOrWhiteSpace(parte);

        var cidadeUf = string.Join("/", new[] { cliente.Cidade, cliente.Uf }.Where(Tem));
        var partes = new[] { cliente.Logradouro, cliente.Numero, cliente.Complemento, cliente.Bairro, cidadeUf }
            .Where(Tem).ToList();
        if (Tem(cliente.Cep)) partes.Add($"CEP {cliente.Cep}");
        return partes.Count == 0 ? null : string.Join(", ", partes);
    }

    /// <summary>O que acontece ao inativar; <paramref name="usuariosAtivosDoPortal"/> nulo quando a contagem não veio.</summary>
    public static string ConsequenciaDaInativacao(int? usuariosAtivosDoPortal) => usuariosAtivosDoPortal switch
    {
        null => "Os usuários do portal deste cliente ficam sem acesso até ele ser reativado.",
        <= 0 => "O cliente pode ser reativado depois.",
        1 => "1 usuário do portal fica sem acesso até o cliente ser reativado.",
        var n => $"{n} usuários do portal ficam sem acesso até o cliente ser reativado.",
    };

    /// <summary>O aviso depois de inativar um cliente; <paramref name="usuariosAtivosDoPortal"/> perdem o acesso com ele.</summary>
    public static string Inativado(int usuariosAtivosDoPortal) => usuariosAtivosDoPortal switch
    {
        <= 0 => "Cliente inativado.",
        1 => "Cliente inativado. 1 usuário do portal ficou sem acesso.",
        _ => $"Cliente inativado. {usuariosAtivosDoPortal} usuários do portal ficaram sem acesso.",
    };
}
