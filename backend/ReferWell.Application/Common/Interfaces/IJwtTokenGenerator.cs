namespace ReferWell.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(ReferWell.Domain.Entities.ApplicationUser user);
}
