using CathedrAll.Pessoas.Domain;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CathedrAll.Pessoas.Infrastructure;

internal sealed class PessoaIdConverter()
    : ValueConverter<PessoaId, Guid>(id => id.Value, value => new PessoaId(value));
