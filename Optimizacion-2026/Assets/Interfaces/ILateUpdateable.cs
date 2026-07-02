using UnityEngine;

/// <summary>
/// Contrato para sistemas que CustomUpdateManager ejecuta en LateUpdate de Unity.
/// </summary>
public interface ILateUpdateable
{
    /// <summary>
    /// Actualiza lógica tardía, normalmente sincronización visual, usando delta time.
    /// </summary>
    public void LateUpdate(float tick);
}
