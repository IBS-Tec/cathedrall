namespace CathedrAll.Kernel.Application.Tests;

internal sealed record RequisicaoFalsa(string Valor) : IRequest<string>;
