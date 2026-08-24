using CathedrAll.Pessoas.Domain;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CathedrAll.Pessoas.Infrastructure;

internal sealed class EmailConverter()
    : ValueConverter<Email, string>(email => email.Value, value => new Email(value));
