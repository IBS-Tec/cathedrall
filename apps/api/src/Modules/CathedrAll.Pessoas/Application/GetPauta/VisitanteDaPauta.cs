namespace CathedrAll.Pessoas.Application;

internal sealed record VisitanteDaPauta(Guid Id, string Nome, PessoaRef? ConvidadoPor);
