using CathedrAll.Kernel.Application;

namespace CathedrAll.Api;

internal sealed class DevelopmentCurrentUserOptions
{
    public const string SectionName = "CurrentUser";

    public Guid Id { get; set; } = new("11111111-1111-1111-1111-111111111111");

    public Papel Papel { get; set; } = Papel.Pastor;
}
