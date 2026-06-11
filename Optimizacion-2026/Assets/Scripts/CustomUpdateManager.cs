using UnityEngine;

public class CustomUpdateManager : MonoBehaviour
{
    public IUpdateable[] updateableList;
    public IFixedUpdateable[] fixedUpdateableList;
    public ILateUpdateable[] lateUpdateableList;

    

    void Start()
    {
        CLogger.Log("CustomUpdateManager");
    }

    // Ordenamiento de listas de updateables segun el orden de prioridad determinado en la interface
    
    void Update()
    {
        foreach (var updateable in updateableList)
        {
            updateable.Update(Time.deltaTime);
        }
    }

    private void FixedUpdate()
    {
        foreach(var fixedUpdateable in fixedUpdateableList)
        {
            fixedUpdateable.FixedUpdate(Time.deltaTime);
        }
    }

    private void LateUpdate()
    {
        foreach (var lateUpdateable in lateUpdateableList)
        {
            lateUpdateable.LateUpdate(Time.deltaTime);
        }
    }
    
    

    public void Register()
    {
        // Logica de registro. Lo hacemos directo por lista o por eventos?
    }

    public void Unregister() 
    {

    }
    
    
    // Agregar metodos para activar o desactivar Updateables sin desuscribir
}
