namespace Project.ViewModels;

// Uses actual catalog data; no placeholder prices, ratings or reviews.
public sealed class ProductRevealCardViewModel
{
    public Project.Models.Db.Product Product { get; init; } = null!;
    public bool IsAdmin { get; init; }
    public double? Rating { get; init; }
    public int ReviewCount { get; init; }
}
