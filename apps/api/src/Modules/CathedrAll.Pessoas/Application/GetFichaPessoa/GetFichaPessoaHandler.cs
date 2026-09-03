using CathedrAll.Kernel.Application;
using CathedrAll.Kernel.Domain;
using CathedrAll.Pessoas.Domain;
using CathedrAll.Pessoas.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CathedrAll.Pessoas.Application;

internal sealed class GetFichaPessoaHandler(
    ICurrentUser currentUser,
    PessoasDbContext context)
    : IRequestHandler<GetFichaPessoaQuery, Result<FichaPessoa>>
{
    public async Task<Result<FichaPessoa>> HandleAsync(
        GetFichaPessoaQuery request,
        CancellationToken cancellationToken)
    {
        var pessoaId = new PessoaId(request.PessoaId);

        bool podeLerMotivo = currentUser.Papel is Papel.Secretaria or Papel.Pastor;

        var linha = await context.Pessoas
            .Where(pessoa => pessoa.Id == pessoaId)
            .Select(pessoa => new
            {
                Id = pessoa.Id.Value,
                pessoa.Nome,
                Situacao = pessoa.Vinculos
                    .Where(vinculo => vinculo.DataFim == null)
                    .Select(vinculo => vinculo.Situacao)
                    .FirstOrDefault(),
                ConvidadoPor = context.Pessoas
                    .Where(convidou => convidou.Id == pessoa.ConvidadoPorId)
                    .Select(convidou => new PessoaRef(convidou.Id.Value, convidou.Nome))
                    .FirstOrDefault(),
                Celular = pessoa.Celular == null ? null : pessoa.Celular.Value,
                Email = pessoa.Email == null ? null : pessoa.Email.Value,
                pessoa.DataNascimento,
                pessoa.EstadoCivil,
                pessoa.DataCasamento,
                pessoa.Endereco,
                pessoa.Profissao,
                pessoa.DataBatismo,
                Vinculos = pessoa.Vinculos
                    .OrderBy(vinculo => vinculo.DataInicio)
                    .Select(vinculo => new VinculoDaFicha(
                        vinculo.Situacao,
                        vinculo.DataInicio,
                        vinculo.DataFim,
                        podeLerMotivo ? vinculo.Motivo : null))
                    .ToList(),
                FundidaEm = context.Pessoas
                    .Where(sobrevivente => sobrevivente.Id == pessoa.FundidaEmId)
                    .Select(sobrevivente => new PessoaRef(sobrevivente.Id.Value, sobrevivente.Nome))
                    .FirstOrDefault(),
                pessoa.Anonimizada,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (linha is null)
        {
            return PessoaErrors.NotFound;
        }

        Endereco? endereco = linha.Endereco;

        EnderecoDaFicha? enderecoDto = endereco is null
            ? null
            : new EnderecoDaFicha(
                endereco.Cep,
                endereco.Logradouro,
                endereco.Numero,
                endereco.Complemento,
                endereco.Bairro,
                endereco.Cidade,
                endereco.Uf);

        return new FichaPessoa(
            linha.Id,
            linha.Nome,
            linha.Situacao,
            linha.ConvidadoPor,
            linha.Celular,
            linha.Email,
            linha.DataNascimento,
            linha.EstadoCivil,
            linha.DataCasamento,
            enderecoDto,
            linha.Profissao,
            linha.DataBatismo,
            linha.Vinculos,
            linha.FundidaEm,
            linha.Anonimizada);
    }
}
