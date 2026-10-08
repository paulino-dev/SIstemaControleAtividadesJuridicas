using AtividadesJuridicas.Domain.Comum;
using AtividadesJuridicas.Infrastructure.Persistencia;
using AtividadesJuridicas.Infrastructure.Persistencia.Interceptores;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// A auditoria é por exclusão: toda propriedade mapeada vai para o registro, que é somente inserção.
/// Este teste faz o esquecimento de um [NaoAuditar] virar um teste vermelho.
/// </summary>
public class PropriedadesAuditadasTests
{
    [Fact]
    public void Cada_entidade_audita_exatamente_as_propriedades_esperadas()
    {
        var esperado = new SortedDictionary<string, string[]>
        {
            ["AreaAtuacao"] = ["Ativo", "Nome"],
            ["Cliente"] =
            [
                "Ativo", "Email", "Nome", "Observacoes", "Telefone", "Tipo",
                "Endereco.Bairro", "Endereco.Cep", "Endereco.Cidade", "Endereco.Complemento",
                "Endereco.Logradouro", "Endereco.Numero", "Endereco.Uf",
            ],
            ["ClientePessoaFisica"] =
            [
                "Ativo", "Email", "Nome", "Observacoes", "Telefone", "Tipo",
                "Endereco.Bairro", "Endereco.Cep", "Endereco.Cidade", "Endereco.Complemento",
                "Endereco.Logradouro", "Endereco.Numero", "Endereco.Uf",
                "Cpf", "DataNascimento", "Rg",
            ],
            ["ClientePessoaJuridica"] =
            [
                "Ativo", "Email", "Nome", "Observacoes", "Telefone", "Tipo",
                "Endereco.Bairro", "Endereco.Cep", "Endereco.Cidade", "Endereco.Complemento",
                "Endereco.Logradouro", "Endereco.Numero", "Endereco.Uf",
                "Cnpj", "InscricaoEstadual", "InscricaoMunicipal", "NomeFantasia", "NomeResponsavel",
            ],
            ["Perfil"] = ["Ativo", "Nome", "Tipo"],
            ["Usuario"] = ["Ativo", "Email", "Nome", "NumeroOab", "PerfilId"],
        };

        using var db = new AppDbContextFabrica().CreateDbContext([]);
        var real = new SortedDictionary<string, string[]>(db.Model.GetEntityTypes()
            .Where(t => typeof(Entidade).IsAssignableFrom(t.ClrType))
            .ToDictionary(t => t.ClrType.Name, t => AuditoriaInterceptor.PropriedadesAuditadas(t).Order().ToArray()));

        real.Should().BeEquivalentTo(esperado,
            "uma entidade ou propriedade nova apareceu na auditoria. Se ela deve ser gravada na trilha de " +
            "auditoria (que é somente inserção e não pode ser corrigida depois), acrescente-a à lista esperada " +
            "neste teste; se for segredo ou controle técnico, marque a propriedade com [NaoAuditar]");
    }
}
