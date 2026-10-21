using System.ComponentModel.DataAnnotations;
using LaMeridienneAPI.Models;

namespace LaMeridienneAPI.Infrastructure;

/// <summary>Montant en euros (R-05) : strictement positif, au plus 10 000 €, deux décimales au plus.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class AmountAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        if (value is not decimal amount) return ValidationResult.Success;
        var error = amount switch
        {
            <= 0 => "Le montant doit être supérieur à 0.",
            > CustomerEntry.MaxAmount => "Le montant ne peut pas dépasser 10 000 €.",
            _ when decimal.Round(amount, 2) != amount => "Le montant a au plus deux décimales.",
            _ => null,
        };
        return error is null ? ValidationResult.Success : new ValidationResult(error);
    }
}
