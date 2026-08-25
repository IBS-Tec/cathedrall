using CathedrAll.Kernel.Application;

namespace CathedrAll.Api.Tests;

internal sealed class FakeCurrentUser : ICurrentUser
{
    public Guid Id => Guid.Empty;

    public Papel Papel => Papel.Pastor;
}
