namespace AuthenticationMicroservice.Models.BaseObjects;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class BaseGuidEntity : IBaseEntity, IGuidId, ITrackable
{
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DeletedAt { get; set; }

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}

public class BaseIntEntity : IBaseEntity, IIntId, ITrackable
{
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DeletedAt { get; set; }
    [Key] public int Id { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

internal interface IBaseEntity
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
}

internal interface IGuidId
{
    Guid Id { get; set; }
}

internal interface IIntId
{
    int Id { get; set; }
}

internal interface ITrackable
{
    DateTimeOffset? UpdatedAt { get; set; }
}