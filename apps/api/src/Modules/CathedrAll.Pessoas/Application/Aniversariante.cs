namespace CathedrAll.Pessoas.Application;

internal sealed record Aniversariante(Guid Id, string Nome, TipoAniversario Tipo, DateOnly Data);
