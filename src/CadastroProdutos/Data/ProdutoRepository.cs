using System.Data.Common;
using CadastroProdutos.Logging;
using CadastroProdutos.Models;
using Microsoft.Data.Sqlite;

namespace CadastroProdutos.Data;

/// <summary>
/// Único ponto de acesso ao banco. Todo SQL é parametrizado (nunca concatenado),
/// o que impede SQL Injection.
/// </summary>
public class ProdutoRepository
{
    private readonly string _connectionString;
    private readonly IRegistroOperacoes _log;

    public ProdutoRepository(string connectionString, IRegistroOperacoes log)
    {
        _connectionString = connectionString;
        _log = log;
    }

    /// <summary>Executa o script .sql de criação da tabela (idempotente).</summary>
    public void CriarTabela(string scriptSql)
    {
        Executar("criar tabela Produtos", conexao =>
        {
            using var comando = conexao.CreateCommand();
            comando.CommandText = scriptSql;
            comando.ExecuteNonQuery();
            return 0;
        });
    }

    /// <summary>Insere o produto e devolve o Id gerado. INSERT e leitura do Id rodam na mesma transação.</summary>
    public int Inserir(Produto produto)
    {
        var id = Executar("inserir produto", conexao =>
        {
            using var transacao = conexao.BeginTransaction();
            try
            {
                using var insert = conexao.CreateCommand();
                insert.Transaction = transacao;
                insert.CommandText =
                    "INSERT INTO Produtos (Nome, Preco, Estoque, Categoria) " +
                    "VALUES (@nome, @preco, @estoque, @categoria)";
                AdicionarParametrosDoProduto(insert, produto);
                insert.ExecuteNonQuery();

                using var consultaId = conexao.CreateCommand();
                consultaId.Transaction = transacao;
                consultaId.CommandText = "SELECT last_insert_rowid()";
                int novoId;
                using (var reader = consultaId.ExecuteReader())
                {
                    reader.Read();
                    novoId = reader.GetInt32(0);
                }

                transacao.Commit();
                return novoId;
            }
            catch
            {
                transacao.Rollback();
                throw;
            }
        });

        produto.Id = id;
        _log.Info($"INSERIR: Id={id}, Nome=\"{produto.Nome}\", Preco={produto.Preco}, Estoque={produto.Estoque}, Categoria=\"{produto.Categoria}\"");
        return id;
    }

    public List<Produto> Listar()
    {
        var produtos = Executar("listar produtos", conexao =>
        {
            using var comando = conexao.CreateCommand();
            comando.CommandText =
                "SELECT Id, Nome, Preco, Estoque, Categoria FROM Produtos WHERE Ativo = @ativo ORDER BY Id";
            comando.Parameters.AddWithValue("@ativo", 1);

            var lista = new List<Produto>();
            using var reader = comando.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(Mapear(reader));
            }
            return lista;
        });

        _log.Info($"LISTAR: {produtos.Count} produto(s) retornado(s)");
        return produtos;
    }

    public Produto? BuscarPorId(int id)
    {
        var produto = Executar("buscar produto", conexao =>
        {
            using var comando = conexao.CreateCommand();
            comando.CommandText =
                "SELECT Id, Nome, Preco, Estoque, Categoria FROM Produtos WHERE Id = @id AND Ativo = @ativo";
            comando.Parameters.AddWithValue("@id", id);
            comando.Parameters.AddWithValue("@ativo", 1);

            using var reader = comando.ExecuteReader();
            return reader.Read() ? Mapear(reader) : null;
        });

        _log.Info($"BUSCAR: Id={id} -> {(produto is null ? "não encontrado" : "encontrado")}");
        return produto;
    }

    /// <summary>Devolve false quando não existe produto ativo com o Id informado.</summary>
    public bool Atualizar(Produto produto)
    {
        var linhas = Executar("atualizar produto", conexao =>
        {
            using var comando = conexao.CreateCommand();
            comando.CommandText =
                "UPDATE Produtos SET Nome = @nome, Preco = @preco, Estoque = @estoque, Categoria = @categoria " +
                "WHERE Id = @id AND Ativo = @ativo";
            AdicionarParametrosDoProduto(comando, produto);
            comando.Parameters.AddWithValue("@id", produto.Id);
            comando.Parameters.AddWithValue("@ativo", 1);
            return comando.ExecuteNonQuery();
        });

        _log.Info($"ATUALIZAR: Id={produto.Id} -> {linhas} linha(s) afetada(s)");
        return linhas > 0;
    }

    /// <summary>
    /// Por padrão faz soft delete (marca Ativo = 0 e mantém o registro).
    /// Com <paramref name="definitivo"/> = true remove a linha com DELETE.
    /// </summary>
    public bool Excluir(int id, bool definitivo = false)
    {
        var linhas = Executar("excluir produto", conexao =>
        {
            using var comando = conexao.CreateCommand();
            if (definitivo)
            {
                comando.CommandText = "DELETE FROM Produtos WHERE Id = @id";
            }
            else
            {
                comando.CommandText =
                    "UPDATE Produtos SET Ativo = 0, DataExclusao = @dataExclusao WHERE Id = @id AND Ativo = 1";
                comando.Parameters.AddWithValue("@dataExclusao", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            comando.Parameters.AddWithValue("@id", id);
            return comando.ExecuteNonQuery();
        });

        _log.Info($"EXCLUIR ({(definitivo ? "definitivo" : "soft delete")}): Id={id} -> {linhas} linha(s) afetada(s)");
        return linhas > 0;
    }

    private static void AdicionarParametrosDoProduto(SqliteCommand comando, Produto produto)
    {
        comando.Parameters.AddWithValue("@nome", produto.Nome);
        comando.Parameters.AddWithValue("@preco", produto.Preco);
        comando.Parameters.AddWithValue("@estoque", produto.Estoque);
        comando.Parameters.AddWithValue("@categoria", produto.Categoria);
    }

    // Mapeamento manual DataReader -> Produto.
    private static Produto Mapear(SqliteDataReader reader) => new()
    {
        Id = reader.GetInt32(reader.GetOrdinal("Id")),
        Nome = reader.GetString(reader.GetOrdinal("Nome")),
        Preco = reader.GetDecimal(reader.GetOrdinal("Preco")),
        Estoque = reader.GetInt32(reader.GetOrdinal("Estoque")),
        Categoria = reader.GetString(reader.GetOrdinal("Categoria"))
    };

    // Abre a conexão, executa a operação e centraliza o tratamento das exceções de banco.
    private T Executar<T>(string operacao, Func<SqliteConnection, T> acao)
    {
        try
        {
            using var conexao = new SqliteConnection(_connectionString);
            conexao.Open();
            return acao(conexao);
        }
        catch (SqliteException ex)
        {
            _log.Erro($"Falha ao {operacao} (SQLite código {ex.SqliteErrorCode})", ex);
            throw new BancoDeDadosException(TraduzirErro(operacao, ex), ex);
        }
        catch (DbException ex)
        {
            _log.Erro($"Falha ao {operacao}", ex);
            throw new BancoDeDadosException($"Erro de banco de dados ao {operacao}.", ex);
        }
        catch (InvalidOperationException ex)
        {
            _log.Erro($"Falha ao {operacao}", ex);
            throw new BancoDeDadosException($"Não foi possível usar a conexão com o banco ao {operacao}.", ex);
        }
    }

    private static string TraduzirErro(string operacao, SqliteException ex) => ex.SqliteErrorCode switch
    {
        1 => $"Erro de SQL ao {operacao}. Verifique se a tabela Produtos foi criada.",
        5 or 6 => $"O banco de dados está ocupado. Tente {operacao} novamente em instantes.",
        8 => "O banco de dados está em modo somente leitura.",
        14 => "Não foi possível abrir o arquivo do banco. Verifique a connection string no appsettings.json.",
        19 => "Os dados violam uma restrição da tabela (ex.: preço ou estoque negativo, campo obrigatório vazio).",
        _ => $"Erro de banco de dados ao {operacao}: {ex.Message}"
    };
}
