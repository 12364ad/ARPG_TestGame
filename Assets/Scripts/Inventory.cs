using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public GameObject EquipPanel;
    public GameObject InventoryPanel;
    bool isOpen = false;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        OpenWindow();
    }

    void OpenWindow()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            isOpen = !isOpen;
            EquipPanel.SetActive(isOpen);
        }
    }
}
