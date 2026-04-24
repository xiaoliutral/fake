using System.ComponentModel.DataAnnotations;

namespace Fake.Validation.Tests.Dtos;

public class CreateBookInput
{
    [Required] public string Name { get; set; }

    [Range(10, 1000)] public int Numbers { get; set; }
}