using UnityEngine;

public class InventoryManager : MonoBehaviour, Instance_interface
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    /*
        <summary>接口实现部分
    */
    public void SwitchOn()
    {
        this.enabled = true;
    }

    public void SwitchOff()
    {
        this.enabled = false;
    }
}
