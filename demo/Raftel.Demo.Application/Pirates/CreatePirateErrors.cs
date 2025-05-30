﻿using Raftel.Domain.Abstractions;

namespace Raftel.Demo.Application.Pirates;

public static class CreatePirateErrors
{
    public static readonly Error NameRequired = new("CreatePirate.NameRequired", "Pirate name is required.");

    public static readonly Error KingMustBeLuffy =
        new("CreatePirate.OnlyLuffyIsKing", "Only Luffy can be the Pirate King.");
}