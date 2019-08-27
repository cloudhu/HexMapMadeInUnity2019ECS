using Unity.Entities;
using UnityEngine;

public class Sample : MonoBehaviour
{
    private CreateHexCellSystem hexCellSystem;

    // Start is called before the first frame update
    void Start()
    {
        hexCellSystem = World.Active.CreateSystem<CreateHexCellSystem>();
    }

    // Update is called once per frame
    void Update()
    {
        hexCellSystem.Update();
    }
}
