using RefaccionariaCuate.Domain.Entities;

namespace RefaccionariaCuate.Application.Abstractions.Auth;

public interface IJwtTokenGenerator
{
    string Generate(User user);
}
