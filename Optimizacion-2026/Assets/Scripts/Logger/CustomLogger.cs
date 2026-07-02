using UnityEngine;

/// <summary>
/// Logger condicional usado para mensajes de depuración sin costo cuando ENABLE_LOG no está definido.
/// </summary>
public static class CLogger
{
    [System.Diagnostics.Conditional("ENABLE_LOG")]
    /// <summary>
    /// Escribe un mensaje en consola solo si el símbolo ENABLE_LOG está activo.
    /// </summary>
    public static void Log(object message)
    {
        Debug.Log(message);
    }
}
