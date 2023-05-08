namespace AuthenticationMicroservice.Models.Entities;

using System.ComponentModel.DataAnnotations.Schema;
using BaseObjects;

public class RefreshToken : BaseGuidEntity
{
    public string? Token { get; set; }
    public bool IsValid { get; set; } = true;
    public long Expires { get; set; }
    public string? IpAddress { get; set; }
    public bool Deleted { get; set; }
    public string? Reason { get; set; }

    [ForeignKey("User")] public int UserId { get; set; }

    [ForeignKey("UserId")] public virtual User? User { get; set; }

    public void Invalidate(string reason)
    {
        IsValid = false;
        Reason = reason;
    }
}

public class AccessToken
{
    public string? Token { get; set; }
    public long ExpiresIn { get; set; }
}