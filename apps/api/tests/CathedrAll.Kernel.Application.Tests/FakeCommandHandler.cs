using CathedrAll.Kernel.Domain;

namespace CathedrAll.Kernel.Application.Tests;

internal sealed class FakeCommandHandler(List<string> trace) : IRequestHandler<FakeCommand, Result<string>>
{
    public Task<Result<string>> HandleAsync(FakeCommand request, CancellationToken cancellationToken)
    {
        trace.Add("handler");

        return Task.FromResult<Result<string>>(FakeHandler.Response);
    }
}
