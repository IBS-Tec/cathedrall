using CathedrAll.Pessoas.Domain;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CathedrAll.Pessoas.Infrastructure;

internal sealed class VinculoIgrejaIdConverter()
    : ValueConverter<VinculoIgrejaId, Guid>(id => id.Value, value => new VinculoIgrejaId(value));
