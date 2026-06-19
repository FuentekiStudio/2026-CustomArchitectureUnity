using System.Collections.Generic;
using UnityEngine;

public class WavesController
{
    private int maxWaves;
    private int waveCount;

    private Wave wave;
    private List<Wave> waves;

    private float newWaveTime;
    private float currentTime;
    private float textTime;
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
        //Level.LevelController.OnLevelWin += OnLevelWinHandler;

        waveCount = 0;

        foreach (Wave wave in waves)
        {
            wave.Initialize();
        }
        currentTime = 0;
    }

    public void WaveIncomingWarning() // originalmente mostraba un texto diciendo "wave incoming" en la pantalla. Lo dejo por si lo queremos implementar
    {
        if (showText)
        {
            currentTime += Time.deltaTime;

            if (currentTime < textTime)
            {
                //mostrar el texto por un tiempo

            }
            else
            {
                //dejar de mostrarlo
                showText = false;
                currentTime = 0;
            }
        }
    }

    public void Update()
    {
        if (waveCount < maxWaves)
        {
            if (wave == null)
            {
                SetNewWave();
            }
            else
            {
                wave.Update();

                if (WaveFinished())
                {
                    wave = null;
                    waveCount++;
                }
            }
        }
        else
        {
            // Llamado a "ganaste"
        }
    }

    private bool WaveFinished()
    {
        return wave.IsWaveCompleted;
    }

    private void SetNewWave()
    {
        // Setear wave con un tiempo de espera
        currentTime += Time.deltaTime;

        if (currentTime > newWaveTime)
        {
            wave = waves[waveCount];
            showText = true;
            currentTime = 0;
        }
    }
    private void OnLevelWinHandler()
    {
        //GameManager.Instance.SetGameState(GameState.Win);
    }
}
