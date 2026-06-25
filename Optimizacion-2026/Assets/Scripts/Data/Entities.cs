using UnityEngine;

public interface IDamageable
{
    int Id { get; }
    Vector3 Position { get; }
    bool IsDead { get; }
    void TakeDamage(int amount);
}

public abstract class EntityBase
{
    public int id;
    public Vector3 position;
    public bool isActive;
    public GameObject view;
    public Collider collider;
    public IPoolable poolable;
    public PoolId poolId;
}

public sealed class EnemyEntity : EntityBase, IDamageable
{
    public int health;
    public float speed;
    public EnemyType type;

    public int Id => id;
    public Vector3 Position => position;
    public bool IsDead => health <= 0;

    public void TakeDamage(int amount)
    {
        health -= amount;
    }
}

public sealed class BuffWallEntity : EntityBase, IDamageable
{
    public int health;
    public float speed;
    public BuffData buff;

    public int Id => id;
    public Vector3 Position => position;
    public bool IsDead => health <= 0;

    public void TakeDamage(int amount)
    {
        health -= amount;
    }
}

public sealed class ProjectileEntity : EntityBase
{
    public Vector3 direction;
    public int damage;
    public float lifetime;
    public object owner;
}

public sealed class PlayerState
{
    public Vector3 position;
    public int damage;
    public int damageBonus;
    public int projectileCount;
    public float fireRate;
    public float shootCooldown;
}

public enum EntityKind
{
    Enemy,
    BuffWall,
    Projectile
}

public readonly struct EntityRef
{
    public readonly EntityKind Kind;
    public readonly EntityBase Entity;

    public EntityRef(EntityKind kind, EntityBase entity)
    {
        Kind = kind;
        Entity = entity;
    }
}
