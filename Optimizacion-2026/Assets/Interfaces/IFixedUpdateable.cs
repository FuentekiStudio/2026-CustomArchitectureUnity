using UnityEngine;

/// <summary>
/// Contrato para sistemas que CustomUpdateManager ejecuta en el loop fijo de simulación.
/// </summary>
public interface IFixedUpdateable
{
    /// <summary>
    /// Actualiza lógica de simulación fija usando el fixed delta time recibido.
    /// </summary>
    public void FixedUpdate(float tick);
}
