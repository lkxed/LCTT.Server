using System.ComponentModel.DataAnnotations;

namespace LCTT.Server.Models;

public class URL
{
    public int Id { get; set; }
    [Required]
    public string Value { get; set; } = default!;
}