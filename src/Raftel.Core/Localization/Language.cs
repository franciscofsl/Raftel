using Raftel.Core.BaseTypes;
using Raftel.Core.Localization.ValueObjects;
using Raftel.Shared.Results;

namespace Raftel.Core.Localization;

public sealed class Language : AggregateRoot<LanguageId>
{
    public string Name { get; private set; }

    public string IsoCode { get; private set; }

    private readonly List<TranslationResource> _resources = new();

    public IReadOnlyCollection<TranslationResource> Resources => _resources.AsReadOnly();

    private Language()
    {
    }

    public static Language Create(string name, string isoCode)
    {
        return new Language
        {
            Id = EntityIdGenerator.Create<LanguageId>(),
            Name = name,
            IsoCode = isoCode
        };
    }

    public Result<TranslationResource> AddTranslationResource(string key, string value)
    {
        if (Resources.Any(_ => _.Key == key))
        {
            return Result.Failure<TranslationResource>(LocalizationErrors.DuplicatedResource);
        }

        var resource = TranslationResource.Create(key, value);
        _resources.Add(resource);
        return Result.Ok(resource);
    }
}