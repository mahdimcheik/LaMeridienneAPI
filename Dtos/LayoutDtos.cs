using System.ComponentModel.DataAnnotations;
using LaMeridienneAPI.Models;

namespace LaMeridienneAPI.Dtos;

/// <summary>Disposition R-07 : chaque proportion entre 0,15 et 0,85.</summary>
public sealed record LayoutDto
{
    private const string OutOfRange = "Chaque case occupe entre 15 % et 85 % de la feuille.";

    [Required(ErrorMessage = OutOfRange), Range(SheetLayout.MinSplit, SheetLayout.MaxSplit, ErrorMessage = OutOfRange)]
    public double? TopSplit { get; init; }

    [Required(ErrorMessage = OutOfRange), Range(SheetLayout.MinSplit, SheetLayout.MaxSplit, ErrorMessage = OutOfRange)]
    public double? BottomSplit { get; init; }

    [Required(ErrorMessage = OutOfRange), Range(SheetLayout.MinSplit, SheetLayout.MaxSplit, ErrorMessage = OutOfRange)]
    public double? RowSplit { get; init; }

    public bool IsDefault { get; init; }

    public static LayoutDto From(SheetLayout? layout) => layout is null
        ? new() { TopSplit = SheetLayout.DefaultSplit, BottomSplit = SheetLayout.DefaultSplit, RowSplit = SheetLayout.DefaultSplit, IsDefault = true }
        : new() { TopSplit = layout.TopSplit, BottomSplit = layout.BottomSplit, RowSplit = layout.RowSplit };
}
