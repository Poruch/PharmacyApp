using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmacyApp.Models;

[Table("USER")]
public class AppUser : EntityBase
{
    private int _userId;
    private string _lastName = null!;
    private string _firstName = null!;
    private string? _patronymic;
    private string _login = null!;
    private string _passwordHash = null!;
    private string _role = null!;

    [Key, Column("UserId")]
    public int UserId
    {
        get => _userId;
        set => _userId = value;
    }

    [Column("LastName")]
    public string LastName
    {
        get => _lastName;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Last name cannot be empty");
            _lastName = value;
        }
    }

    [Column("FirstName")]
    public string FirstName
    {
        get => _firstName;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("First name cannot be empty");
            _firstName = value;
        }
    }

    [Column("Patronymic")]
    public string? Patronymic
    {
        get => _patronymic;
        set => _patronymic = value;
    }

    [Column("Login")]
    public string Login
    {
        get => _login;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Login cannot be empty");
            _login = value;
        }
    }
    public string FullName => string.IsNullOrWhiteSpace(Patronymic)
        ? $"{LastName} {FirstName}"
        : $"{LastName} {FirstName} {Patronymic}"; 
    [Column("PasswordHash")]
    public string PasswordHash
    {
        get => _passwordHash;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Password hash cannot be empty");
            _passwordHash = value;
        }
    }

    [Column("Role")]
    public string Role
    {
        get => _role;
        set
        {
            var allowed = new[] { "provizor", "pharmacist", "manager", "admin", "cashier" };
            if (!allowed.Contains(value))
                throw new ArgumentException($"Role must be one of: {string.Join(", ", allowed)}");
            _role = value;
        }
    }

    [Column("Salt")]
    public string Salt { get; set; } = null!;

    [Column("IsActive")]
    public bool IsActive { get; set; } = true;

    [Column("RegistrationDate")]
    public DateTime RegistrationDate { get; set; } = DateTime.Now;

    public AppUser() { }

    public AppUser(string lastName, string firstName, string login, string passwordHash, string role, string? patronymic = null)
    {
        LastName = lastName;
        FirstName = firstName;
        Patronymic = patronymic;
        Login = login;
        PasswordHash = passwordHash;
        Role = role;
    }
}