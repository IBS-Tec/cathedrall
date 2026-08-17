using Microsoft.AspNetCore.Http.Features;

namespace CathedrAll.Api.Tests;

internal sealed class StartedResponseFeature : HttpResponseFeature
{
    public override bool HasStarted => true;
}
