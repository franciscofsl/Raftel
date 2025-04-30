using Raftel.Domain.Specifications;

namespace Raftel.Demo.Domain.Pirates.Specifications;

public class BountyOverAndKingOrSpecification(int threshold)
    : OrSpecification<Pirate>(new IsKingSpecification(), new BountyOverSpecification(threshold));