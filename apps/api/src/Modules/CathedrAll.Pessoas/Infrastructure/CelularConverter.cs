using CathedrAll.Pessoas.Domain;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CathedrAll.Pessoas.Infrastructure;

internal sealed class CelularConverter()
    : ValueConverter<Celular, string>(celular => celular.Value, value => new Celular(value));
