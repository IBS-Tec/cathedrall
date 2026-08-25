using CathedrAll.Kernel.Application;
using CathedrAll.Kernel.Domain;

namespace CathedrAll.Api.Tests;

internal sealed record PipelineProbeCommand : ICommand<Result>;
