using Unity.VisualScripting;
using UnityEngine;

public interface IUpdateable
{
    // Orden de prioridad del script
    public void Update(float tick);
    
}
