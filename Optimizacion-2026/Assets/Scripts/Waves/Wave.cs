/// <summary>
/// Modelo de wave simple del prototipo inicial. El flujo principal actual usa WaveSystem y WaveConfig.
/// </summary>
public class Wave
{
    public bool IsWaveCompleted { get; private set; }

    /// <summary>
    /// Reinicia el estado de completitud de esta wave del prototipo inicial.
    /// </summary>
    public void Initialize()
    {
        IsWaveCompleted = false;
    }

    /// <summary>
    /// Punto de actualización reservado para lógica propia de esta wave del prototipo inicial.
    /// </summary>
    public void Update(float deltaTime)
    {
    }

    /// <summary>
    /// Marca esta wave del prototipo inicial como completada.
    /// </summary>
    public void Complete()
    {
        IsWaveCompleted = true;
    }
}
