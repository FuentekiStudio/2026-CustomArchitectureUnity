using UnityEngine;

/// <summary>
/// Contrato para entidades que pueden recibir daño desde CombatSystem.
/// </summary>
public interface IDamageable
{
    int Id { get; }
    Vector3 Position { get; }
    bool IsDead { get; }

    /// <summary>
    /// Descuenta vida o resistencia según el daño recibido.
    /// </summary>
    void TakeDamage(int amount);
}

/// <summary>
/// Datos comunes de entidades runtime asociadas a un GameObject reciclado del pool.
/// </summary>
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

/// <summary>
/// Modelo runtime de un enemigo. EnemySystem lo mueve y CombatSystem le aplica daño.
/// </summary>
public sealed class EnemyEntity : EntityBase, IDamageable
{
    public int health;
    public float speed;
    public EnemyType type;

    public int Id => id;
    public Vector3 Position => position;
    public bool IsDead => health <= 0;

    /// <summary>
    /// Resta vida al enemigo cuando recibe impacto de un proyectil.
    /// </summary>
    public void TakeDamage(int amount)
    {
        health -= amount;
    }
}

/// <summary>
/// Modelo runtime de una pared de buff. BuffWallSystem la mueve y CombatSystem la destruye.
/// </summary>
public sealed class BuffWallEntity : EntityBase, IDamageable
{
    public int health;
    public float speed;
    public BuffData buff;

    public int Id => id;
    public Vector3 Position => position;
    public bool IsDead => health <= 0;

    /// <summary>
    /// Resta vida a la pared cuando recibe impacto de un proyectil.
    /// </summary>
    public void TakeDamage(int amount)
    {
        health -= amount;
    }
}

/// <summary>
/// Modelo runtime de un proyectil activo. ProjectileSystem lo mueve y CollisionSystem detecta impactos.
/// </summary>
public sealed class ProjectileEntity : EntityBase
{
    public Vector3 direction;
    public int damage;
    public float lifetime;
    public object owner;
}

/// <summary>
/// Estado mutable del jugador usado por PlayerSystem y mostrado parcialmente por la UI.
/// </summary>
public sealed class PlayerState
{
    public Vector3 position;
    public int damage;
    public int damageBonus;
    public int projectileCount;
    public float fireRate;
    public float shootCooldown;
}

/// <summary>
/// Clasificación de entidades usada por PhysicsRegistry para resolver colliders.
/// </summary>
public enum EntityKind
{
    Enemy,
    BuffWall,
    Projectile
}

/// <summary>
/// Referencia liviana que vincula un Collider de Unity con una entidad de gameplay.
/// </summary>
public readonly struct EntityRef
{
    public readonly EntityKind Kind;
    public readonly EntityBase Entity;

    /// <summary>
    /// Crea una referencia tipada hacia una entidad registrada en física.
    /// </summary>
    public EntityRef(EntityKind kind, EntityBase entity)
    {
        Kind = kind;
        Entity = entity;
    }
}
