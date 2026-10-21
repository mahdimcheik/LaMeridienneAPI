namespace LaMeridienneAPI.Models;

/// <summary>Titre vendu au comptoir (E05) ; jamais supprimé, seulement désactivé (R-10).</summary>
public class Newspaper
{
    public const int NameMaxLength = 100;

    public static readonly Guid SudOuestId = Guid.Parse("01920000-0000-7000-8000-000000000001");
    public static readonly Guid LeRepublicainId = Guid.Parse("01920000-0000-7000-8000-000000000002");

    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Name { get; set; } = "";

    /// <summary>Nom sans casse ni accents, unique : voir <see cref="Services.CustomerNames"/>.</summary>
    public string NormalizedName { get; set; } = "";

    public bool IsActive { get; set; } = true;

    /// <summary>Position dans la case Journaux, à partir de 1.</summary>
    public int SortOrder { get; set; }
}
