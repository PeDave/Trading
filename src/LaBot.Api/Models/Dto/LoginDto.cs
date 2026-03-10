using System.ComponentModel.DataAnnotations;

namespace LaBot.Api.Models.Dto;

public class LoginDto
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = "";

    [Required, MinLength(6)]
    public string Password { get; set; } = "";
}
