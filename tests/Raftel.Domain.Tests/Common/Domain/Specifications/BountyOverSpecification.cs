using System.Linq.Expressions;
using Raftel.Domain.Specifications;

namespace Raftel.Domain.Tests.Common.Domain.Specifications;

public class BountyOverSpecification(int threshold) : Specification<Pirate>
{
    public override Expression<Func<Pirate, bool>> ToExpression()
    {
        return pirate => pirate.Bounty > threshold;
    }
}