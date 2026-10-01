namespace CadastroProdutos.Data;

/// <summary>Erro de acesso ao banco já traduzido para uma mensagem amigável.</summary>
public class BancoDeDadosException : Exception
{
    public BancoDeDadosException(string mensagem, Exception interna) : base(mensagem, interna)
    {
    }
}
