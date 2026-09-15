using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace DaMaiDeparte.Web.Infrastructure;

/// <summary>Replaces the default English model binding messages with Romanian ones.</summary>
public static class RomanianModelBindingMessages
{
    public static void Apply(DefaultModelBindingMessageProvider provider)
    {
        provider.SetMissingBindRequiredValueAccessor(_ => "Acest câmp este obligatoriu.");
        provider.SetMissingKeyOrValueAccessor(() => "Acest câmp este obligatoriu.");
        provider.SetMissingRequestBodyRequiredValueAccessor(() => "Cererea nu conține datele necesare.");
        provider.SetValueMustNotBeNullAccessor(_ => "Acest câmp este obligatoriu.");
        provider.SetAttemptedValueIsInvalidAccessor((value, _) => $"Valoarea „{value}” nu este validă.");
        provider.SetNonPropertyAttemptedValueIsInvalidAccessor(value => $"Valoarea „{value}” nu este validă.");
        provider.SetUnknownValueIsInvalidAccessor(_ => "Valoarea introdusă nu este validă.");
        provider.SetNonPropertyUnknownValueIsInvalidAccessor(() => "Valoarea introdusă nu este validă.");
        provider.SetValueIsInvalidAccessor(value => $"Valoarea „{value}” nu este validă.");
        provider.SetValueMustBeANumberAccessor(_ => "Acest câmp trebuie să fie un număr.");
        provider.SetNonPropertyValueMustBeANumberAccessor(() => "Acest câmp trebuie să fie un număr.");
    }
}
