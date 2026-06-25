public class Wave
{
    public bool IsWaveCompleted { get; private set; }

    public void Initialize()
    {
        IsWaveCompleted = false;
    }

    public void Update(float deltaTime)
    {
    }

    public void Complete()
    {
        IsWaveCompleted = true;
    }
}
