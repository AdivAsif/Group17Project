namespace Group17profile.Models.DefaultObjects;

using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

public class DefaultGuidEntity : IDefaultEntity, IGuidId, ITrackable
{
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? DeletedAt { get; set; }

    [JsonProperty(PropertyName = "id")] public Guid Id { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}

public class DefaultIntEntity : IDefaultEntity, IIntId, ITrackable
{
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DeletedAt { get; set; }

    [JsonProperty(PropertyName = "id")] public int Id { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}

internal interface IDefaultEntity
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