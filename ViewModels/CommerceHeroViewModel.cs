namespace Project.ViewModels;

// Shared Razor equivalent of the supplied commerce-hero React component.
public sealed class CommerceHeroViewModel
{
    public bool IsAdmin { get; init; }
    public IEnumerable<Project.Models.Db.Category> Categories { get; init; } = Array.Empty<Project.Models.Db.Category>();
}
