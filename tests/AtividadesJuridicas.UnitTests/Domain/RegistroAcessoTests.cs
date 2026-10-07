using AtividadesJuridicas.Domain.Auditoria;

namespace AtividadesJuridicas.UnitTests.Domain;

public class RegistroAcessoTests
{
    private static readonly DateTime Agora = new(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Detalhe_acima_do_limite_e_truncado()
    {
        var registro = RegistroAcesso.Criar(
            Agora, Guid.CreateVersion7(), "RegistroAuditoria", null, AcaoDeAcesso.Listou, new string('x', 3000), null);

        registro.Detalhe.Should().HaveLength(RegistroAcesso.TamanhoMaximoDetalhe);
        RegistroAcesso.TamanhoMaximoDetalhe.Should().Be(2000);
    }

    [Fact]
    public void Detalhe_dentro_do_limite_fica_intacto()
    {
        var registro = RegistroAcesso.Criar(
            Agora, Guid.CreateVersion7(), "RegistroAuditoria", null, AcaoDeAcesso.Listou, "curto", null);

        registro.Detalhe.Should().Be("curto");
    }
}
