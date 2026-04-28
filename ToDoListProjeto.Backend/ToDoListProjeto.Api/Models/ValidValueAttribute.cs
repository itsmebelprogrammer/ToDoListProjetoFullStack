using System.ComponentModel.DataAnnotations;

namespace ToDoListProjeto.Api.Models;

[AttributeUsage(AttributeTargets.Property)]
public class ValidValueAttribute : ValidationAttribute
{
    private readonly string[] _validValues;

    public ValidValueAttribute(params string[] validValues)
    {
        _validValues = validValues;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        if (value is null) return ValidationResult.Success;

        if (value is string str && _validValues.Contains(str))
            return ValidationResult.Success;

        return new ValidationResult(
            $"Valor inválido para '{context.DisplayName}'. Permitidos: {string.Join(", ", _validValues)}.",
            [context.MemberName!]);
    }
}
