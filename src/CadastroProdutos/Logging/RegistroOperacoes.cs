namespace CadastroProdutos.Logging;

public interface IRegistroOperacoes
{
    void Info(string mensagem);
    void Erro(string mensagem, Exception? ex = null);
}

/// <summary>Registra em arquivo texto as operações realizadas pela aplicação.</summary>
public class RegistroOperacoesArquivo : IRegistroOperacoes
{
    private readonly string _caminho;
    private readonly object _trava = new();

    public RegistroOperacoesArquivo(string caminho)
    {
        _caminho = Path.GetFullPath(caminho);
        Directory.CreateDirectory(Path.GetDirectoryName(_caminho)!);
    }

    public void Info(string mensagem) => Escrever("INFO", mensagem);

    public void Erro(string mensagem, Exception? ex = null) =>
        Escrever("ERRO", ex is null ? mensagem : $"{mensagem} | {ex.GetType().Name}: {ex.Message}");

    private void Escrever(string nivel, string mensagem)
    {
        try
        {
            lock (_trava)
            {
                File.AppendAllText(_caminho, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{nivel}] {mensagem}{Environment.NewLine}");
            }
        }
        catch (IOException)
        {
            // Falha ao gravar o log não deve derrubar a aplicação.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
