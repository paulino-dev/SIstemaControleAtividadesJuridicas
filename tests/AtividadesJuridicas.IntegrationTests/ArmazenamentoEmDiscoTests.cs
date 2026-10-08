using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Documentos;
using AtividadesJuridicas.Infrastructure.Armazenamento;

namespace AtividadesJuridicas.IntegrationTests;

public sealed class ArmazenamentoEmDiscoTests : IDisposable
{
    private sealed class RelogioDeOutubro : IRelogio
    {
        public DateTime AgoraUtc => new(2026, 10, 8, 12, 0, 0, DateTimeKind.Utc);
    }

    private readonly string _pasta = Path.Combine(Path.GetTempPath(), "atividades-testes", Guid.NewGuid().ToString("N"));
    private readonly ArmazenamentoEmDisco _armazenamento;

    public ArmazenamentoEmDiscoTests() => _armazenamento = new ArmazenamentoEmDisco(_pasta, new RelogioDeOutubro());

    public void Dispose()
    {
        if (Directory.Exists(_pasta)) Directory.Delete(_pasta, recursive: true);
    }

    private static MemoryStream Conteudo(string texto) => new(Encoding.UTF8.GetBytes(texto));

    private string[] ArquivosNoDisco() =>
        Directory.Exists(_pasta) ? Directory.GetFiles(_pasta, "*", SearchOption.AllDirectories) : [];

    [Fact]
    public async Task Grava_e_devolve_chave_tamanho_e_hash()
    {
        var gravado = await _armazenamento.GravarAsync(Conteudo("abc"), 1024, default);

        gravado.TamanhoBytes.Should().Be(3);
        gravado.HashSha256.Should().Be("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad");
        gravado.Chave.Should().MatchRegex("^2026/10/[0-9a-f]{32}$");
        ArquivosNoDisco().Should().ContainSingle().Which.Should().NotEndWith(".tmp");
    }

    [Fact]
    public async Task O_que_foi_gravado_volta_igual()
    {
        var gravado = await _armazenamento.GravarAsync(Conteudo("conteúdo com acentuação"), 1024, default);

        await using var aberto = await _armazenamento.AbrirAsync(gravado.Chave, default);

        aberto.Should().NotBeNull();
        using var leitor = new StreamReader(aberto!, Encoding.UTF8);
        (await leitor.ReadToEndAsync()).Should().Be("conteúdo com acentuação");
    }

    [Fact]
    public async Task Cada_gravacao_tem_a_sua_chave()
    {
        var primeiro = await _armazenamento.GravarAsync(Conteudo("igual"), 1024, default);
        var segundo = await _armazenamento.GravarAsync(Conteudo("igual"), 1024, default);

        segundo.Chave.Should().NotBe(primeiro.Chave);
        segundo.HashSha256.Should().Be(primeiro.HashSha256);
        ArquivosNoDisco().Should().HaveCount(2);
    }

    [Fact]
    public async Task Arquivo_no_limite_e_aceito_e_um_byte_a_mais_e_recusado_sem_deixar_nada_no_disco()
    {
        (await _armazenamento.GravarAsync(Conteudo("12345"), 5, default)).TamanhoBytes.Should().Be(5);

        var acao = () => _armazenamento.GravarAsync(Conteudo("123456"), 5, default);

        (await acao.Should().ThrowAsync<ExcecaoDeArquivoGrandeDemais>()).Which.LimiteBytes.Should().Be(5);
        ArquivosNoDisco().Should().ContainSingle("só o primeiro arquivo ficou; o recusado não deixa resto, nem temporário");
    }

    [Fact]
    public async Task Arquivo_vazio_e_gravado_com_tamanho_zero()
    {
        // Quem recusa arquivo vazio é o domínio; o armazenamento só informa o tamanho.
        (await _armazenamento.GravarAsync(Conteudo(""), 1024, default)).TamanhoBytes.Should().Be(0);
    }

    [Fact]
    public async Task Abrir_chave_que_nao_existe_devolve_nulo()
    {
        (await _armazenamento.AbrirAsync("2026/10/00000000000000000000000000000000", default)).Should().BeNull();
    }

    [Theory]
    [InlineData("../../segredo")]
    [InlineData("2026/10/../../../segredo")]
    [InlineData("2026/10/0123456789abcdef0123456789abcdef.tmp")]
    [InlineData("C:\\Windows\\win.ini")]
    [InlineData("/etc/passwd")]
    [InlineData("")]
    [InlineData("2026/10/0123456789abcdef0123456789abcdef\n")]
    [InlineData("٢٠٢٦/١٠/0123456789abcdef0123456789abcdef")]
    [InlineData("2026/10/0123456789ABCDEF0123456789abcdef")]
    [InlineData("2026\\10\\0123456789abcdef0123456789abcdef")]
    public async Task Chave_fora_do_formato_e_recusada(string chave)
    {
        var abrir = () => _armazenamento.AbrirAsync(chave, default);
        var remover = () => _armazenamento.RemoverAsync(chave, default);

        await abrir.Should().ThrowAsync<ArgumentException>();
        await remover.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Remover_apaga_o_arquivo_e_repetir_nao_e_erro()
    {
        var gravado = await _armazenamento.GravarAsync(Conteudo("abc"), 1024, default);

        await _armazenamento.RemoverAsync(gravado.Chave, default);
        await _armazenamento.RemoverAsync(gravado.Chave, default);

        ArquivosNoDisco().Should().BeEmpty();
        (await _armazenamento.AbrirAsync(gravado.Chave, default)).Should().BeNull();
    }

    [Fact]
    public async Task Falha_na_leitura_do_conteudo_nao_deixa_temporario()
    {
        var acao = () => _armazenamento.GravarAsync(new FluxoQueFalha(), 1024, default);

        await acao.Should().ThrowAsync<IOException>();
        ArquivosNoDisco().Should().BeEmpty();
    }

    [Fact]
    public async Task Pasta_raiz_removida_nao_passa_por_arquivo_inexistente()
    {
        var gravado = await _armazenamento.GravarAsync(Conteudo("abc"), 1024, default);
        Directory.Delete(_pasta, recursive: true);

        var abrir = () => _armazenamento.AbrirAsync(gravado.Chave, default);
        var remover = () => _armazenamento.RemoverAsync(gravado.Chave, default);

        await abrir.Should().ThrowAsync<IOException>().WithMessage("*não está acessível*");
        await remover.Should().ThrowAsync<IOException>().WithMessage("*não está acessível*");
    }

    [Fact]
    public async Task Pasta_do_mes_inexistente_com_raiz_presente_e_arquivo_inexistente()
    {
        const string chave = "2020/01/00000000000000000000000000000000";

        (await _armazenamento.AbrirAsync(chave, default)).Should().BeNull();
        await _armazenamento.RemoverAsync(chave, default);
    }

    [Fact]
    public async Task Chave_usa_cultura_invariante()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("th-TH");
            var gravado = await _armazenamento.GravarAsync(Conteudo("abc"), 1024, default);
            gravado.Chave.Should().StartWith("2026/10/");
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public async Task Remover_com_o_arquivo_aberto_nao_falha()
    {
        var gravado = await _armazenamento.GravarAsync(Conteudo("abc"), 1024, default);

        var aberto = await _armazenamento.AbrirAsync(gravado.Chave, default);
        aberto.Should().NotBeNull();
        await using (aberto)
        {
            await _armazenamento.RemoverAsync(gravado.Chave, default);
        }

        (await _armazenamento.AbrirAsync(gravado.Chave, default)).Should().BeNull();
    }

    private static byte[] ConteudoGrande()
    {
        var bytes = new byte[200_000];
        for (var i = 0; i < bytes.Length; i++) bytes[i] = (byte)(i % 251);
        return bytes;
    }

    [Fact]
    public async Task Conteudo_maior_que_o_buffer_com_leituras_curtas_e_gravado_inteiro()
    {
        var bytes = ConteudoGrande();

        var gravado = await _armazenamento.GravarAsync(new FluxoDeLeiturasCurtas(bytes), 1_000_000, default);

        gravado.TamanhoBytes.Should().Be(bytes.Length);
        gravado.HashSha256.Should().Be(Convert.ToHexStringLower(SHA256.HashData(bytes)));
        await using var aberto = await _armazenamento.AbrirAsync(gravado.Chave, default);
        using var copia = new MemoryStream();
        await aberto!.CopyToAsync(copia);
        copia.ToArray().Should().Equal(bytes);
    }

    [Fact]
    public async Task Limite_ultrapassado_em_um_bloco_posterior_nao_deixa_nada_no_disco()
    {
        var acao = () => _armazenamento.GravarAsync(new FluxoDeLeiturasCurtas(ConteudoGrande()), 100_000, default);

        (await acao.Should().ThrowAsync<ExcecaoDeArquivoGrandeDemais>()).Which.LimiteBytes.Should().Be(100_000);
        ArquivosNoDisco().Should().BeEmpty();
    }

    private sealed class FluxoDeLeiturasCurtas(byte[] bytes) : MemoryStream(bytes)
    {
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            base.ReadAsync(buffer[..Math.Min(buffer.Length, 1000)], cancellationToken);
    }

    private sealed class FluxoQueFalha : MemoryStream
    {
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            throw new IOException("conexão caiu");
    }
}
