namespace CadastroProdutos.Models;

public class Produto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public decimal Preco { get; set; }
    public int Estoque { get; set; }
    public string Categoria { get; set; } = string.Empty;

    public override string ToString() =>
        $"{Id,-5} {Nome,-30} {Preco,12:C2} {Estoque,8} {Categoria,-20}";
}
