using EggLedger.Models.Enums;

namespace EggLedger.DTO.User;

public class UserUpdateDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }

    /// <summary>
    /// Only an Admin caller may set this; the controller rejects the request otherwise.
    /// </summary>
    public UserRoles? Role { get; set; }
}
