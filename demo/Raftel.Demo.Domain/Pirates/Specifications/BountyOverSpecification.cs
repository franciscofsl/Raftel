using System.Linq.Expressions;
using Raftel.Domain.Specifications;

namespace Raftel.Demo.Domain.Pirates.Specifications;

public class BountyOverSpecification(int threshold) : Specification<Pirate>
{
    public override Expression<Func<Pirate, bool>> ToExpression()
    {
        return pirate => pirate.Bounty > threshold;
    }
}