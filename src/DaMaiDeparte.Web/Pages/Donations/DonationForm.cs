using System.ComponentModel.DataAnnotations;
using DaMaiDeparte.Web.Infrastructure;
using DaMaiDeparte.Web.Models;
using DaMaiDeparte.Web.Resources;
using DaMaiDeparte.Web.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace DaMaiDeparte.Web.Pages.Donations;

/// <summary>
/// Fields shared by the Create and Edit pages: name, category, expiry, location, photos and the
/// handover details. There is no free-text description.
/// </summary>
public class DonationFormInput
{
    [Required(ErrorMessage = UiText.Validation.TitleRequired)]
    [StringLength(FoodRules.MaxTitleLength, MinimumLength = 3, ErrorMessage = UiText.Validation.TitleLength)]
    [Display(Name = "Denumire aliment")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = UiText.Validation.CategoryRequired)]
    [Display(Name = "Categorie")]
    public int? FoodCategoryId { get; set; }

    /// <summary>
    /// Bound as text and parsed as dd/MM/yyyy. A native &lt;input type="date"&gt; renders in the
    /// browser's locale (mm/dd/yyyy on a US system) and that cannot be overridden from the page,
    /// so the field is plain text and the format is enforced here.
    /// </summary>
    [Required(ErrorMessage = UiText.Validation.ExpirationRequired)]
    [Display(Name = "Data expirării")]
    public string? ExpirationDate { get; set; }

    [Display(Name = "Țara")]
    public int? CountryId { get; set; }

    /// <summary>UI-only: narrows the localitate dropdown, not persisted on the donation itself
    /// (the city already implies its județ).</summary>
    [Display(Name = "Județul")]
    public int? CountyId { get; set; }

    [Display(Name = "Localitatea")]
    public int? CityId { get; set; }

    [Display(Name = "Cartierul")]
    public int? NeighborhoodId { get; set; }

    [Required(ErrorMessage = UiText.Validation.PickupLocationRequired)]
    [StringLength(FoodRules.MaxPickupLocationLength, ErrorMessage = UiText.Validation.PickupLocationLength)]
    [Display(Name = "Locul predării")]
    public string PickupLocation { get; set; } = string.Empty;

    [StringLength(FoodRules.MaxPickupNotesLength, ErrorMessage = UiText.Validation.PickupNotesLength)]
    [Display(Name = "Detalii despre predare")]
    public string? PickupNotes { get; set; }

    /// <summary>
    /// A single multi-select file field: the user picks one to three photos at once instead of
    /// filling three separate inputs.
    /// </summary>
    [Display(Name = "Fotografii")]
    public List<IFormFile> Images { get; set; } = new();

    /// <summary>Ids of already stored photos the user ticked for removal (Edit only).</summary>
    public List<int> RemoveImageIds { get; set; } = new();

    // ----- Food-safety confirmations (all three are mandatory) -----

    [Display(Name = "Ambalaj original")]
    public bool ConfirmPackaging { get; set; }

    [Display(Name = "Fără carne și lactate")]
    public bool ConfirmNoMeatDairy { get; set; }

    [Display(Name = "Data expirării vizibilă")]
    public bool ConfirmExpiryVisible { get; set; }

    /// <summary>Non-empty uploads, in the order the browser supplied them.</summary>
    public IReadOnlyList<IFormFile> UploadedImages() =>
        Images.Where(f => f is { Length: > 0 }).ToList();

    /// <summary>
    /// Validates the parts a data annotation cannot express: the three mandatory tick boxes,
    /// the date format and the location. Runs on the server on every post.
    /// </summary>
    public DateOnly? ValidateAndParse(ModelStateDictionary modelState)
    {
        if (!ConfirmPackaging)
        {
            modelState.AddModelError($"Input.{nameof(ConfirmPackaging)}", UiText.Validation.SafetyPackagingRequired);
        }

        if (!ConfirmNoMeatDairy)
        {
            modelState.AddModelError($"Input.{nameof(ConfirmNoMeatDairy)}", UiText.Validation.SafetyNoMeatDairyRequired);
        }

        if (!ConfirmExpiryVisible)
        {
            modelState.AddModelError($"Input.{nameof(ConfirmExpiryVisible)}", UiText.Validation.SafetyExpiryVisibleRequired);
        }

        var expiration = RoDate.ParseDate(ExpirationDate);
        if (expiration is null && !string.IsNullOrWhiteSpace(ExpirationDate))
        {
            modelState.AddModelError($"Input.{nameof(ExpirationDate)}", UiText.Validation.ExpirationInvalid);
        }

        if (UploadedImages().Count > FoodRules.MaxImages)
        {
            modelState.AddModelError($"Input.{nameof(Images)}", UiText.Validation.TooManyImages);
        }

        if (CountryId is null)
        {
            modelState.AddModelError($"Input.{nameof(CountryId)}", UiText.Validation.CountryRequired);
        }

        if (CountyId is null)
        {
            modelState.AddModelError($"Input.{nameof(CountyId)}", UiText.Validation.CountyRequired);
        }

        if (CityId is null)
        {
            modelState.AddModelError($"Input.{nameof(CityId)}", UiText.Validation.LocalityRequired);
        }

        return expiration;
    }

    public DonationInput ToServiceInput(DateOnly expirationDate) =>
        new(Title,
            FoodCategoryId ?? 0,
            expirationDate,
            CountryId ?? 0,
            CityId ?? 0,
            NeighborhoodId,
            PickupLocation,
            PickupNotes);

    public static DonationFormInput From(DonationItem item) => new()
    {
        Title = item.Title,
        FoodCategoryId = item.FoodCategoryId,
        ExpirationDate = RoDate.ToDateInput(item.ExpirationDate),
        CountryId = item.CountryId,
        CityId = item.CityId,
        NeighborhoodId = item.NeighborhoodId,
        PickupLocation = item.PickupLocation,
        PickupNotes = item.PickupNotes,
        // Already confirmed when the listing was first published.
        ConfirmPackaging = true,
        ConfirmNoMeatDairy = true,
        ConfirmExpiryVisible = true
    };
}

/// <summary>View model for the shared _DonationFormFields partial.</summary>
public sealed class DonationFormViewModel
{
    public required DonationFormInput Input { get; init; }

    public required IReadOnlyList<FoodCategory> Categories { get; init; }

    public required LocationPickerModel Picker { get; init; }

    /// <summary>Photos already stored on the listing (Edit only).</summary>
    public IReadOnlyList<DonationImage> ExistingImages { get; init; } = Array.Empty<DonationImage>();

    /// <summary>Earliest date the form accepts, mirroring the server-side rule.</summary>
    public string MinExpirationDate => RoDate.ToDateInput(RoDate.EarliestAllowedExpiry);
}
