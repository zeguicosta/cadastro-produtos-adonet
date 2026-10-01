-- Script de criação da tabela Produtos (SQLite)
-- Ativo/DataExclusao dão suporte ao soft delete (exclusão lógica).

CREATE TABLE IF NOT EXISTS Produtos (
    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
    Nome         TEXT    NOT NULL,
    Preco        REAL    NOT NULL CHECK (Preco >= 0),
    Estoque      INTEGER NOT NULL CHECK (Estoque >= 0),
    Categoria    TEXT    NOT NULL,
    Ativo        INTEGER NOT NULL DEFAULT 1,
    DataExclusao TEXT    NULL
);
