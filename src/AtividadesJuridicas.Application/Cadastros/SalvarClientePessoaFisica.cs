using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Comum;

namespace AtividadesJuridicas.Application.Cadastros;

/// <summary>Cria o cliente quando <paramref name="Id"/> é nulo; caso contrário, altera.</summary>
public sealed record SalvarClientePessoaFisica(
    Guid? Id, DadosDoClienteEntrada Dados, string Cpf, string? Rg, DateOnly? DataNascimento)
    : IRequisicao<Resultado<Guid>>;

public sealed class SalvarClientePessoaFisicaManipulador(
    IContextoDeAcesso contexto,
    IClienteRepositorio clientes,
    IUnidadeDeTrabalho unidade) : IManipulador<SalvarClientePessoaFisica, Resultado<Guid>>
{
    private const string CpfEmUso = "Já existe um cliente com este CPF.";

    public async Task<Resultado<Guid>> ManipularAsync(SalvarClientePessoaFisica requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { EhDiretoria: true })
            return Resultado<Guid>.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        ClientePessoaFisica? existente = null;
        if (requisicao.Id is { } id)
        {
            // Um Id de pessoa jurídica não é encontrado aqui: o tipo do cliente não muda.
            existente = await clientes.ObterPorIdAsync(id, ct) as ClientePessoaFisica;
            if (existente is null)
                return Resultado<Guid>.Falha(TipoErro.NaoEncontrado, "Cliente não encontrado.");
        }

        if (!Domain.Cadastros.Cpf.TentarCriar(requisicao.Cpf, out var cpf))
            return Resultado<Guid>.Falha(TipoErro.Validacao, "CPF inválido.");

        if (await clientes.ExisteCpfAsync(cpf, existente?.Id, ct))
            return Resultado<Guid>.Falha(TipoErro.Conflito, CpfEmUso);

        try
        {
            var dados = ClienteMapeamento.ParaDominio(requisicao.Dados);

            if (existente is null)
            {
                existente = ClientePessoaFisica.Criar(dados, cpf, requisicao.Rg, requisicao.DataNascimento);
                clientes.Adicionar(existente);
            }
            else
            {
                existente.Alterar(dados, cpf, requisicao.Rg, requisicao.DataNascimento);
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
            return Resultado<Guid>.Falha(TipoErro.Conflito, CpfEmUso);
        }
    }
}
