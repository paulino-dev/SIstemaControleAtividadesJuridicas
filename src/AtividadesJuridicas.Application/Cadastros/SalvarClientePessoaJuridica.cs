using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Application.Cadastros;

/// <summary>Cria o cliente quando <paramref name="Id"/> é nulo; caso contrário, altera.</summary>
public sealed record SalvarClientePessoaJuridica(
    Guid? Id, DadosDoClienteEntrada Dados, string Cnpj, string? NomeFantasia, string? NomeResponsavel,
    string? InscricaoEstadual, string? InscricaoMunicipal)
    : IRequisicao<Resultado<Guid>>;

public sealed class SalvarClientePessoaJuridicaManipulador(
    IContextoDeAcesso contexto,
    IClienteRepositorio clientes,
    IUnidadeDeTrabalho unidade) : IManipulador<SalvarClientePessoaJuridica, Resultado<Guid>>
{
    private const string CnpjEmUso = "Já existe um cliente com este CNPJ.";

    public async Task<Resultado<Guid>> ManipularAsync(SalvarClientePessoaJuridica requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Resultado<Guid>.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        ClientePessoaJuridica? existente = null;
        if (requisicao.Id is { } id)
        {
            // Um Id de pessoa física não é encontrado aqui: o tipo do cliente não muda.
            existente = await clientes.ObterPorIdAsync(id, ct) as ClientePessoaJuridica;
            if (existente is null)
                return Resultado<Guid>.Falha(TipoErro.NaoEncontrado, "Cliente não encontrado.");
        }

        if (!Domain.Cadastros.Cnpj.TentarCriar(requisicao.Cnpj, out var cnpj))
            return Resultado<Guid>.Falha(TipoErro.Validacao, "CNPJ inválido.");

        if (await clientes.ExisteCnpjAsync(cnpj, existente?.Id, ct))
            return Resultado<Guid>.Falha(TipoErro.Conflito, CnpjEmUso);

        try
        {
            var dados = ClienteMapeamento.ParaDominio(requisicao.Dados);

            if (existente is null)
            {
                existente = ClientePessoaJuridica.Criar(
                    dados, cnpj, requisicao.NomeFantasia, requisicao.NomeResponsavel,
                    requisicao.InscricaoEstadual, requisicao.InscricaoMunicipal);
                clientes.Adicionar(existente);
            }
            else
            {
                existente.Alterar(
                    dados, cnpj, requisicao.NomeFantasia, requisicao.NomeResponsavel,
                    requisicao.InscricaoEstadual, requisicao.InscricaoMunicipal);
            }

            await unidade.SalvarAsync(ct);
            return Resultado<Guid>.Ok(existente.Id);
        }
        catch (ExcecaoDeDominio ex)
        {
            return Resultado<Guid>.Falha(TipoErro.Validacao, ex.Message);
        }
        catch (ExcecaoDeConflito)
        {
            return Resultado<Guid>.Falha(TipoErro.Conflito, CnpjEmUso);
        }
    }
}
