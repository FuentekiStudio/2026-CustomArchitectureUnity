using System.Collections.Generic;

/// <summary>
/// Controlador de waves del prototipo inicial. El flujo principal actual usa WaveSystem.
/// </summary>
public class WavesController : IUpdateable
{
    private readonly int maxWaves;
    private readonly List<Wave> waves;
    private readonly float newWaveTime;
    private readonly float textTime;

    private int waveCount;
    private Wave wave;
    private float currentTime;
    private bool showText;

    public int WaveCount => waveCount;

    /// <summary>
    /// Recibe waves del prototipo inicial, cantidad máxima y timers de aviso e inicio.
    /// </summary>
    public WavesController(int maxWaves, List<Wave> wavesList, float newWaveTime, float textTime)
    {
        this.maxWaves = maxWaves;
        waves = wavesList;
        this.newWaveTime = newWaveTime;
        this.textTime = textTime;
        showText = false;
        wave = null;
    }

    /// <summary>
    /// Reinicia el contador y el estado de todas las waves del prototipo inicial.
    /// </summary>
    public void Initialize()
    {
        waveCount = 0;

        for (int i = 0; i < waves.Count; i++)
        {
            waves[i].Initialize();
        }

        currentTime = 0f;
    }

    /// <summary>
    /// Controla la duración del aviso visual de próxima wave.
    /// </summary>
    public void WaveIncomingWarning(float deltaTime)
    {
        if (!showText)
        {
            return;
        }

        currentTime += deltaTime;
        if (currentTime >= textTime)
        {
            showText = false;
            currentTime = 0f;
        }
    }

    /// <summary>
    /// Actualiza la wave activa del prototipo inicial o espera para iniciar la siguiente.
    /// </summary>
    public void Update(float deltaTime)
    {
        WaveIncomingWarning(deltaTime);

        if (waveCount >= maxWaves)
        {
            return;
        }

        if (wave == null)
        {
            SetNewWave(deltaTime);
            return;
        }

        wave.Update(deltaTime);

        if (wave.IsWaveCompleted)
        {
            wave = null;
            waveCount++;
        }
    }

    /// <summary>
    /// Asigna una nueva wave cuando vence el tiempo entre waves.
    /// </summary>
    private void SetNewWave(float deltaTime)
    {
        currentTime += deltaTime;

        if (currentTime > newWaveTime)
        {
            wave = waves[waveCount];
            showText = true;
            currentTime = 0f;
        }
    }
}
