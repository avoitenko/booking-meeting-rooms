using BookingMeetingRooms.Domain.Common;
using BookingMeetingRooms.Domain.Enums;

namespace BookingMeetingRooms.Domain.Entities;

public class User : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }

    private User() { }

    public User(string name, string email, UserRole role)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("User name cannot be empty", nameof(name));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("User email cannot be empty", nameof(email));
        if (!IsValidEmail(email))
            throw new ArgumentException("Invalid email format", nameof(email));

        Name = name;
        Email = email;
        Role = role;
    }

    public void Update(string name, string email, UserRole role)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("User name cannot be empty", nameof(name));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("User email cannot be empty", nameof(email));
        if (!IsValidEmail(email))
            throw new ArgumentException("Invalid email format", nameof(email));

        Name = name;
        Email = email;
        Role = role;
        UpdatedAt = DateTime.UtcNow;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
