using Raftel.Domain.Specifications;

namespace Raftel.Tests.Common.Domain.Specifications;

public class BountyOverAndKingOrSpecification(int threshold)
    : OrSpecification<Pirate>(new IsKingSpecification(), new BountyOverSpecification(threshold));