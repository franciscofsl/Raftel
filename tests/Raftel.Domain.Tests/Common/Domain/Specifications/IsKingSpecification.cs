using System.Linq.Expressions;
using Raftel.Domain.Specifications;

namespace Raftel.Domain.Tests.Common.Domain.Specifications;

public class IsKingSpecification : Specification<Pirate>
{
    public override Expression<Func<Pirate, bool>> ToExpression()
    {
        return pirate => pirate.IsKing;
    }
}