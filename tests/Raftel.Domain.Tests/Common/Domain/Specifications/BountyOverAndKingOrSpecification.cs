using Raftel.Domain.Specifications;

namespace Raftel.Domain.Tests.Common.Domain.Specifications;

public class BountyOverAndKingOrSpecification(int threshold)
    : OrSpecification<Pirate>(new IsKingSpecification(), new BountyOverSpecification(threshold));

public class BountyOverAndKingAndSpecification(int threshold)
    : AndSpecification<Pirate>(new IsKingSpecification(), new BountyOverSpecification(threshold));