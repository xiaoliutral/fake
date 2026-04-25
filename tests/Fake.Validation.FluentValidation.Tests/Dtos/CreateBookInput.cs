using System.ComponentModel.DataAnnotations;

namespace Fake.Validation.FluentValidation.Tests.Dtos;

public class CreateBookInput
{
    [Required]
    public string Name { get; set; }

    public int Numbers { get; set; }
}