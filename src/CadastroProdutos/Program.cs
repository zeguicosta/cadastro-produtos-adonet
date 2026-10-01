using System.Globalization;
using CadastroProdutos.Data;
using CadastroProdutos.Logging;
using CadastroProdutos.UI;
using Microsoft.Extensions.Configuration;

CultureInfo.CurrentCulture = new CultureInfo("pt-BR");
Console.OutputEncoding = System.Text.Encoding.UTF8;

var configuracao = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var connectionString = configuracao.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.WriteLine("[ERRO] Connection string \"DefaultConnection\" não encontrada no appsettings.json.");
    return 1;
}

var log = new RegistroOperacoesArquivo(configuracao["Log:Arquivo"] ?? "logs/operacoes.log");
var repositorio = new ProdutoRepository(connectionString, log);

try
{
    log.Info("Aplicação iniciada");
    repositorio.CriarTabela(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "criar_tabela.sql")));
    new Menu(repositorio).Executar();
}
catch (BancoDeDadosException ex)
{
    Console.WriteLine($"[ERRO] {ex.Message}");
    return 1;
}
catch (EndOfStreamException)
{
    // Entrada encerrada (Ctrl+D ou fim de arquivo redirecionado).
}
finally
{
    log.Info("Aplicação encerrada");
}

return 0;
