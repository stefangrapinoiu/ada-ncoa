using System.ComponentModel.DataAnnotations;
using DaMaiDeparte.Web.Infrastructure;
using DaMaiDeparte.Web.Models;
using DaMaiDeparte.Web.Resources;
using DaMaiDeparte.Web.Services;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DaMaiDeparte.Web.Pages.Donator.Donations;

/// <summary>Form fields shared by the Create and Edit donation pages.</summary>
public class DonationFormInput
{
    [Required(ErrorMessage = UiText.Validation.TitleRequired)]
    [StringLength(100, MinimumLength = 3, ErrorMessage = UiText.Validation.TitleLength)]
    [Display(Name = "Titlu")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = UiText.Validation.DescriptionRequired)]
    [StringLength(2000, ErrorMessage = UiText.Validation.DescriptionLength)]
    [Display(Name = "Descriere")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = UiText.Validation.CategoryRequired)]
    [Display(Name = "Categorie")]
    public int? CategoryId { get; set; }

    [Required(ErrorMessage = UiText.Validation.ConditionRequired)]
    [Display(Name = "Starea produsului")]
    public ProductCondition? Condition { get; set; }

    [Required(ErrorMessage = UiText.Validation.PickupAreaRequired)]
    [StringLength(100, ErrorMessage = UiText.Validation.PickupAreaLength)]
    [Display(Name = "Zona de predare")]
    public string PickupArea { get; set; } = string.Empty;

    [Display(Name = "Imagine produs")]
    public IFormFile? Image { get; set; }

    [Display(Name = "Elimină imaginea")]
    public bool RemoveImage { get; set; }

    public DonationInput ToServiceInput() =>
        new(Title, Description, CategoryId ?? 0, Condition ?? default, PickupArea);

    public static DonationFormInput From(DonationItem item) => new()
    {
        Title = item.Title,
        Description = item.Description,
        CategoryId = item.CategoryId,
        Condition = item.Condition,
        PickupArea = item.PickupArea
    };
}

/// <summary>View model for the shared _DonationFormFields partial.</summary>
public sealed class DonationFormViewModel
{
    public required DonationFormInput Input { get; init; }

    public required IEnumerable<SelectListItem> Categories { get; init; }

    public required IEnumerable<SelectListItem> Conditions { get; init; }

    public string? CurrentImagePath { get; init; }

    public static async Task<(IEnumerable<SelectListItem> Categories, IEnumerable<SelectListItem> Conditions)> LoadOptionsAsync(
        IDonationService donations, CancellationToken cancellationToken)
    {
        var categories = await donations.GetCategoriesAsync(cancellationToken);
        return (
            categories.Select(c => new SelectListItem(c.Name, c.Id.ToString(RoDate.Culture))).ToList(),
            DisplayExtensions.ConditionOptions().ToList());
    }
}
