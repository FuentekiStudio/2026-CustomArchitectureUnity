using System.Collections.Generic;

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

    public WavesController(int maxWaves, List<Wave> wavesList, float newWaveTime, float textTime)
    {
        this.maxWaves = maxWaves;
        waves = wavesList;
        this.newWaveTime = newWaveTime;
        this.textTime = textTime;
        showText = false;
        wave = null;
    }

    public void Initialize()
    {
        waveCount = 0;

        for (int i = 0; i < waves.Count; i++)
        {
            waves[i].Initialize();
        }

        currentTime = 0f;
    }

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
