using Raftel.Domain.Specifications;

namespace Raftel.Demo.Domain.Pirates.Specifications;

public class BountyOverAndKingAndSpecification(int threshold)
    : AndSpecification<Pirate>(new IsKingSpecification(), new BountyOverSpecification(threshold));