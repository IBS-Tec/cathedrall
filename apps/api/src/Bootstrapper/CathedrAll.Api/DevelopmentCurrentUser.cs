using CathedrAll.Kernel.Application;
using Microsoft.Extensions.Options;

namespace CathedrAll.Api;

internal sealed class DevelopmentCurrentUser(IOptionsSnapshot<DevelopmentCurrentUserOptions> options)
    : ICurrentUser
{
    public Guid Id => options.Value.Id;

    public Papel Papel => options.Value.Papel;
}
