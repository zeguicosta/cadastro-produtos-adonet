using System.Globalization;
using CadastroProdutos.Data;
using CadastroProdutos.Models;

namespace CadastroProdutos.UI;

/// <summary>Interface de console. Não contém SQL: só conversa com o ProdutoRepository.</summary>
public class Menu
{
    private readonly ProdutoRepository _repositorio;

    public Menu(ProdutoRepository repositorio)
    {
        _repositorio = repositorio;
    }

    public void Executar()
    {
        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("===== CADASTRO DE PRODUTOS =====");
            Console.WriteLine("1. Inserir produto");
            Console.WriteLine("2. Listar produtos");
            Console.WriteLine("3. Buscar produto por ID");
            Console.WriteLine("4. Atualizar produto");
            Console.WriteLine("5. Excluir produto");
            Console.WriteLine("6. Sair");
            Console.Write("Escolha uma opção: ");

            var opcao = Console.ReadLine();
            if (opcao is null)
            {
                return; // entrada encerrada
            }

            Console.WriteLine();
            try
            {
                switch (opcao.Trim())
                {
                    case "1": Inserir(); break;
                    case "2": Listar(); break;
                    case "3": BuscarPorId(); break;
                    case "4": Atualizar(); break;
                    case "5": Excluir(); break;
                    case "6":
                        Console.WriteLine("Até logo!");
                        return;
                    default:
                        Console.WriteLine("Opção inválida. Digite um número de 1 a 6.");
                        break;
                }
            }
            catch (BancoDeDadosException ex)
            {
                Console.WriteLine($"[ERRO] {ex.Message}");
            }
        }
    }

    private void Inserir()
    {
        Console.WriteLine("--- Inserir produto ---");
        var produto = new Produto
        {
            Nome = LerTexto("Nome: "),
            Preco = LerDecimal("Preço: "),
            Estoque = LerInteiro("Estoque: "),
            Categoria = LerTexto("Categoria: ")
        };

        var id = _repositorio.Inserir(produto);
        Console.WriteLine($"Produto inserido com sucesso! Id = {id}");
    }

    private void Listar()
    {
        Console.WriteLine("--- Lista de produtos ---");
        var produtos = _repositorio.Listar();
        if (produtos.Count == 0)
        {
            Console.WriteLine("Nenhum produto cadastrado.");
            return;
        }

        ImprimirCabecalho();
        foreach (var produto in produtos)
        {
            Console.WriteLine(produto);
        }
        Console.WriteLine($"Total: {produtos.Count} produto(s)");
    }

    private void BuscarPorId()
    {
        Console.WriteLine("--- Buscar produto por ID ---");
        var id = LerInteiro("Id: ");
        var produto = _repositorio.BuscarPorId(id);
        if (produto is null)
        {
            Console.WriteLine($"Produto com Id {id} não encontrado.");
            return;
        }

        ImprimirCabecalho();
        Console.WriteLine(produto);
    }

    private void Atualizar()
    {
        Console.WriteLine("--- Atualizar produto ---");
        var id = LerInteiro("Id: ");
        var produto = _repositorio.BuscarPorId(id);
        if (produto is null)
        {
            Console.WriteLine($"Produto com Id {id} não encontrado.");
            return;
        }

        Console.WriteLine("Pressione Enter para manter o valor atual.");
        produto.Nome = LerTexto($"Nome [{produto.Nome}]: ", produto.Nome);
        produto.Preco = LerDecimal($"Preço [{produto.Preco:F2}]: ", produto.Preco);
        produto.Estoque = LerInteiro($"Estoque [{produto.Estoque}]: ", produto.Estoque);
        produto.Categoria = LerTexto($"Categoria [{produto.Categoria}]: ", produto.Categoria);

        Console.WriteLine(_repositorio.Atualizar(produto)
            ? "Produto atualizado com sucesso!"
            : "Nenhum produto foi atualizado.");
    }

    private void Excluir()
    {
        Console.WriteLine("--- Excluir produto ---");
        var id = LerInteiro("Id: ");
        var produto = _repositorio.BuscarPorId(id);
        if (produto is null)
        {
            Console.WriteLine($"Produto com Id {id} não encontrado.");
            return;
        }

        Console.Write($"Confirma a exclusão de \"{produto.Nome}\"? (s/N): ");
        if (!RespondeuSim())
        {
            Console.WriteLine("Exclusão cancelada.");
            return;
        }

        Console.Write("Remover definitivamente do banco? (s/N = exclusão lógica): ");
        var definitivo = RespondeuSim();

        Console.WriteLine(_repositorio.Excluir(id, definitivo)
            ? "Produto excluído com sucesso!"
            : "Nenhum produto foi excluído.");
    }

    private static void ImprimirCabecalho()
    {
        Console.WriteLine($"{"Id",-5} {"Nome",-30} {"Preço",12} {"Estoque",8} {"Categoria",-20}");
        Console.WriteLine(new string('-', 79));
    }

    private static bool RespondeuSim() =>
        (Console.ReadLine() ?? string.Empty).Trim().Equals("s", StringComparison.OrdinalIgnoreCase);

    // As leituras repetem a pergunta até receber um valor válido.
    // "padrao" (quando informado) é usado se o usuário só apertar Enter.

    private static string LerTexto(string rotulo, string? padrao = null)
    {
        while (true)
        {
            Console.Write(rotulo);
            var entrada = LerLinha().Trim();
            if (entrada.Length > 0)
            {
                return entrada;
            }
            if (padrao is not null)
            {
                return padrao;
            }
            Console.WriteLine("Valor obrigatório.");
        }
    }

    private static decimal LerDecimal(string rotulo, decimal? padrao = null)
    {
        while (true)
        {
            Console.Write(rotulo);
            var entrada = LerLinha().Trim();
            if (entrada.Length == 0 && padrao.HasValue)
            {
                return padrao.Value;
            }
            // Aceita tanto 10,50 quanto 10.50.
            if (decimal.TryParse(entrada.Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var valor))
            {
                return valor;
            }
            Console.WriteLine("Digite um número não negativo (ex.: 19,90).");
        }
    }

    private static int LerInteiro(string rotulo, int? padrao = null)
    {
        while (true)
        {
            Console.Write(rotulo);
            var entrada = LerLinha().Trim();
            if (entrada.Length == 0 && padrao.HasValue)
            {
                return padrao.Value;
            }
            if (int.TryParse(entrada, NumberStyles.None, CultureInfo.InvariantCulture, out var valor))
            {
                return valor;
            }
            Console.WriteLine("Digite um número inteiro não negativo.");
        }
    }

    private static string LerLinha() =>
        Console.ReadLine() ?? throw new EndOfStreamException("Entrada encerrada.");
}
