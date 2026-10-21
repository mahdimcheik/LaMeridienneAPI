namespace LaMeridienneAPI.Models;

/// <summary>
/// Disposition des quatre cases propre à un utilisateur (R-07), en proportions de la feuille :
/// largeur de la case de gauche de chaque rangée, hauteur de la rangée du haut.
/// </summary>
public class SheetLayout
{
    public const double MinSplit = 0.15;
    public const double MaxSplit = 0.85;
    public const double DefaultSplit = 0.5;

    public Guid UserId { get; set; }
    public double TopSplit { get; set; } = DefaultSplit;
    public double BottomSplit { get; set; } = DefaultSplit;
    public double RowSplit { get; set; } = DefaultSplit;
}
