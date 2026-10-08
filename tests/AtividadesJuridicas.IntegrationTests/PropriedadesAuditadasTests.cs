using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Comum;
using AtividadesJuridicas.Infrastructure.Persistencia;
using AtividadesJuridicas.Infrastructure.Persistencia.Interceptores;
using Microsoft.EntityFrameworkCore;

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
            ["Documento"] =
            [
                "Categoria", "ChaveArmazenamento", "ClienteId", "HashSha256", "Nome", "NomeArquivoOriginal",
                "TamanhoBytes", "TipoConteudo",
            ],
            ["DocumentoPerfilAcesso"] = ["DocumentoId", "PerfilId"],
            ["DocumentoUsuarioAcesso"] = ["DocumentoId", "UsuarioId"],
            ["Perfil"] = ["Ativo", "Nome", "Tipo"],
            ["Processo"] =
            [
                "AreaAtuacaoId", "Comarca", "DataDistribuicao", "NumeroCnj", "Observacoes", "Orgao",
                "ParteContraria", "Status", "Titulo", "ValorCausa",
            ],
            ["ProcessoCliente"] = ["ClienteId", "Polo", "ProcessoId"],
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

    [Fact]
    public void Todo_tipo_mapeado_deriva_de_Entidade_e_nenhum_e_dependente()
    {
        // Os registros de auditoria e de acesso são de inserção e gravam a própria trilha: ficam de fora.
        var permitidos = new[] { typeof(RegistroAuditoria), typeof(RegistroAcesso) };

        using var db = new AppDbContextFabrica().CreateDbContext([]);
        var tipos = db.Model.GetEntityTypes().ToList();

        var foraDeEntidade = tipos
            .Select(t => t.ClrType)
            .Where(t => !typeof(Entidade).IsAssignableFrom(t) && !permitidos.Contains(t))
            .Select(t => t.Name).ToList();
        var dependentes = tipos.Where(t => t.IsOwned()).Select(t => t.ClrType.Name).ToList();

        foraDeEntidade.Should().BeEmpty(
            "um tipo mapeado fora de Entidade não recebe carimbos, não tem exclusão lógica e não é auditado. " +
            "Faça-o derivar de Entidade ou, se realmente não puder, acrescente-o à lista de exceções deste teste com o motivo");
        dependentes.Should().BeEmpty(
            "um tipo dependente (owned) não é Entidade: seria excluído fisicamente e não seria auditado. " +
            "Mapeie-o como Entidade ou como propriedade complexa");
    }
}
