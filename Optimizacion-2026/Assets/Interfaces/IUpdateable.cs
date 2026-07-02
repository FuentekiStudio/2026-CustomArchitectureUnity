using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// Contrato para sistemas que CustomUpdateManager ejecuta una vez por frame.
/// </summary>
public interface IUpdateable
{
    /// <summary>
    /// Actualiza lógica dependiente del frame usando el delta time recibido.
    /// </summary>
    public void Update(float tick);
}
