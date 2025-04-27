using Raftel.Domain.Specifications;

namespace Raftel.Tests.Common.Domain.Specifications;

public class BountyOverAndKingAndSpecification(int threshold)
    : AndSpecification<Pirate>(new IsKingSpecification(), new BountyOverSpecification(threshold));