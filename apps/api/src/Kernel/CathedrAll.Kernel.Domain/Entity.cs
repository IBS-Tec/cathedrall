namespace CathedrAll.Kernel.Domain;

public abstract class Entity<TId>(TId id)
    where TId : notnull
{
    public TId Id { get; } = id;

#pragma warning disable S3875
    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) =>
        left is null ? right is null : left.Equals(right);
#pragma warning restore S3875

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) =>
        !(left == right);

    public sealed override bool Equals(object? obj) =>
        obj is Entity<TId> other
        && GetType() == other.GetType()
        && Id.Equals(other.Id);

    public sealed override int GetHashCode() => HashCode.Combine(GetType(), Id);
}
