using System.ComponentModel.DataAnnotations;

namespace LaBot.Api.Models.Dto;

public class RegisterDto
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = "";

    [Required, MinLength(6), MaxLength(100)]
    public string Password { get; set; } = "";

    [Required, Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = "";

    [Required, MinLength(3), MaxLength(50)]
    public string UserName { get; set; } = "";
}
