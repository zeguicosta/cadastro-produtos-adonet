# Cadastro de Produtos — C# + ADO.NET

Aplicação de console em C# para cadastrar e gerenciar produtos, usando **ADO.NET puro** (`Microsoft.Data.Sqlite`) com banco **SQLite**. Sem ORM: conexão, comandos e mapeamento são feitos manualmente.

## O que o projeto demonstra

| Requisito | Onde está |
|---|---|
| Conexão com ADO.NET | `ProdutoRepository.Executar` (`SqliteConnection`) |
| CRUD completo | `Inserir`, `Listar`, `BuscarPorId`, `Atualizar`, `Excluir` em `ProdutoRepository` |
| `ExecuteNonQuery` em INSERT/UPDATE/DELETE | `ProdutoRepository` |
| `ExecuteReader` nos SELECT | `Listar`, `BuscarPorId` |
| Mapeamento manual DataReader → `Produto` | `ProdutoRepository.Mapear` |
| SQL parametrizado (anti SQL Injection) | todos os comandos usam `@parametros` |
| Connection string no `appsettings.json` | `src/CadastroProdutos/appsettings.json` |
| Tratamento de exceções de banco | `ProdutoRepository.Executar` → `BancoDeDadosException`, exibida pelo `Menu` |
| Registro das operações em arquivo | `RegistroOperacoesArquivo` → `logs/operacoes.log` |
| Script `.sql` | `database/criar_tabela.sql` |
| **Bônus:** testes unitários | `tests/CadastroProdutos.Tests` (xUnit, 11 testes) |
| **Bônus:** transações | `ProdutoRepository.Inserir` (commit/rollback) |
| **Bônus:** soft delete | `ProdutoRepository.Excluir` (colunas `Ativo` e `DataExclusao`) |

## Estrutura

```
CadastroProdutos/
├── database/criar_tabela.sql        # script de criação da tabela
├── src/CadastroProdutos/
│   ├── Program.cs                   # configuração e inicialização
│   ├── appsettings.json             # connection string
│   ├── Models/Produto.cs            # entidade
│   ├── Data/ProdutoRepository.cs    # acesso ao banco (todo o SQL fica aqui)
│   ├── Data/BancoDeDadosException.cs
│   ├── Logging/RegistroOperacoes.cs # log em arquivo
│   └── UI/Menu.cs                   # interface de console (sem SQL)
├── tests/CadastroProdutos.Tests/    # testes unitários do repository
└── prints/                          # prints das operações
```

A interface (`Menu`) só conversa com o `ProdutoRepository`; nenhum SQL existe fora do repository.

## Pré-requisitos

- [.NET SDK 9.0](https://dotnet.microsoft.com/download) ou superior

Não é preciso instalar nenhum servidor de banco: o SQLite é um arquivo local.

## Como configurar

A connection string fica em `src/CadastroProdutos/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=produtos.db"
  },
  "Log": {
    "Arquivo": "logs/operacoes.log"
  }
}
```

O arquivo `produtos.db` é criado automaticamente na pasta de onde a aplicação é executada.

### Criação da tabela

Ao iniciar, a aplicação executa `database/criar_tabela.sql` (que usa `CREATE TABLE IF NOT EXISTS`), então não é necessário nenhum passo manual. Se preferir criar a tabela à mão:

```bash
sqlite3 produtos.db < database/criar_tabela.sql
```

## Como executar

Na raiz do repositório:

```bash
dotnet run --project src/CadastroProdutos
```

Menu:

```
===== CADASTRO DE PRODUTOS =====
1. Inserir produto
2. Listar produtos
3. Buscar produto por ID
4. Atualizar produto
5. Excluir produto
6. Sair
```

- Em **Atualizar**, pressione Enter para manter o valor atual de um campo.
- Em **Excluir**, o padrão é a exclusão lógica (soft delete): o produto some das listagens, mas a linha continua no banco com `Ativo = 0`. Também é possível escolher a remoção definitiva (`DELETE`).
- As operações ficam registradas em `logs/operacoes.log`.

## Testes

```bash
dotnet test
```

Os testes usam um banco SQLite temporário e cobrem o CRUD, o soft delete, o tratamento de erros e uma tentativa de SQL Injection (`x'); DROP TABLE Produtos; --` é gravado como texto comum, sem afetar a tabela).

## Prints

Os prints das operações em funcionamento estão na pasta [`prints/`](prints/).
