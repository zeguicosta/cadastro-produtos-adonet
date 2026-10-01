using CadastroProdutos.Data;
using CadastroProdutos.Logging;
using CadastroProdutos.Models;
using Microsoft.Data.Sqlite;

namespace CadastroProdutos.Tests;

public class ProdutoRepositoryTests : IDisposable
{
    private readonly string _arquivoBanco = Path.Combine(Path.GetTempPath(), $"produtos_teste_{Guid.NewGuid():N}.db");
    private readonly string _connectionString;
    private readonly ProdutoRepository _repositorio;

    public ProdutoRepositoryTests()
    {
        _connectionString = $"Data Source={_arquivoBanco};Pooling=False";
        _repositorio = new ProdutoRepository(_connectionString, new RegistroNulo());
        _repositorio.CriarTabela(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "criar_tabela.sql")));
    }

    public void Dispose()
    {
        File.Delete(_arquivoBanco);
    }

    private static Produto NovoProduto(string nome = "Teclado") =>
        new() { Nome = nome, Preco = 199.90m, Estoque = 10, Categoria = "Informática" };

    [Fact]
    public void Inserir_GeraIdEPersisteOsDados()
    {
        var id = _repositorio.Inserir(NovoProduto());

        var salvo = _repositorio.BuscarPorId(id);
        Assert.NotNull(salvo);
        Assert.Equal("Teclado", salvo.Nome);
        Assert.Equal(199.90m, salvo.Preco);
        Assert.Equal(10, salvo.Estoque);
        Assert.Equal("Informática", salvo.Categoria);
    }

    [Fact]
    public void Listar_RetornaTodosOsProdutosAtivos()
    {
        _repositorio.Inserir(NovoProduto("A"));
        _repositorio.Inserir(NovoProduto("B"));

        var produtos = _repositorio.Listar();

        Assert.Equal(new[] { "A", "B" }, produtos.Select(p => p.Nome));
    }

    [Fact]
    public void BuscarPorId_Inexistente_RetornaNull()
    {
        Assert.Null(_repositorio.BuscarPorId(999));
    }

    [Fact]
    public void Atualizar_AlteraOsDados()
    {
        var produto = NovoProduto();
        _repositorio.Inserir(produto);

        produto.Nome = "Teclado Mecânico";
        produto.Preco = 349.50m;
        produto.Estoque = 3;
        Assert.True(_repositorio.Atualizar(produto));

        var salvo = _repositorio.BuscarPorId(produto.Id)!;
        Assert.Equal("Teclado Mecânico", salvo.Nome);
        Assert.Equal(349.50m, salvo.Preco);
        Assert.Equal(3, salvo.Estoque);
    }

    [Fact]
    public void Atualizar_Inexistente_RetornaFalse()
    {
        var produto = NovoProduto();
        produto.Id = 999;

        Assert.False(_repositorio.Atualizar(produto));
    }

    [Fact]
    public void Excluir_SoftDelete_OcultaMasMantemALinha()
    {
        var id = _repositorio.Inserir(NovoProduto());

        Assert.True(_repositorio.Excluir(id));

        Assert.Null(_repositorio.BuscarPorId(id));
        Assert.Empty(_repositorio.Listar());
        Assert.Equal(1, ContarLinhas());
    }

    [Fact]
    public void Excluir_Definitivo_RemoveALinha()
    {
        var id = _repositorio.Inserir(NovoProduto());

        Assert.True(_repositorio.Excluir(id, definitivo: true));

        Assert.Equal(0, ContarLinhas());
    }

    [Fact]
    public void Excluir_Inexistente_RetornaFalse()
    {
        Assert.False(_repositorio.Excluir(999));
    }

    [Fact]
    public void Inserir_TentativaDeSqlInjection_EhTratadaComoTexto()
    {
        const string ataque = "x'); DROP TABLE Produtos; --";
        _repositorio.Inserir(NovoProduto("Mouse"));

        var id = _repositorio.Inserir(NovoProduto(ataque));

        Assert.Equal(ataque, _repositorio.BuscarPorId(id)!.Nome);
        Assert.Equal(2, _repositorio.Listar().Count);
    }

    [Fact]
    public void Inserir_PrecoNegativo_LancaBancoDeDadosExceptionENaoGrava()
    {
        var produto = NovoProduto();
        produto.Preco = -1m;

        Assert.Throws<BancoDeDadosException>(() => _repositorio.Inserir(produto));
        Assert.Equal(0, ContarLinhas());
    }

    [Fact]
    public void Listar_SemTabela_LancaBancoDeDadosException()
    {
        var outroBanco = Path.Combine(Path.GetTempPath(), $"produtos_vazio_{Guid.NewGuid():N}.db");
        try
        {
            var repositorio = new ProdutoRepository($"Data Source={outroBanco};Pooling=False", new RegistroNulo());

            Assert.Throws<BancoDeDadosException>(() => repositorio.Listar());
        }
        finally
        {
            File.Delete(outroBanco);
        }
    }

    private long ContarLinhas()
    {
        using var conexao = new SqliteConnection(_connectionString);
        conexao.Open();
        using var comando = conexao.CreateCommand();
        comando.CommandText = "SELECT COUNT(*) FROM Produtos";
        return (long)comando.ExecuteScalar()!;
    }

    private sealed class RegistroNulo : IRegistroOperacoes
    {
        public void Info(string mensagem) { }
        public void Erro(string mensagem, Exception? ex = null) { }
    }
}
