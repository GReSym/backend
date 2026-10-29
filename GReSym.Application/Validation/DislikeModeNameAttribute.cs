using System.ComponentModel.DataAnnotations;
using GReSym.Core.Enums;

namespace GReSym.Application.Validation;

/// <summary>Null or one of <see cref="DislikeModes.AllNames"/> (case-insensitive).</summary>
[AttributeUsage(AttributeTargets.Property)]
public class DislikeModeNameAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null || DislikeModes.TryParse(value as string, out _))
            return ValidationResult.Success;

        return new ValidationResult(
            $"{validationContext.DisplayName} must be one of: {string.Join(", ", DislikeModes.AllNames)}");
    }
}
