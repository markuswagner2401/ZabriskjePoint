using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Events;

[System.Serializable]
public class UnityEventInt : UnityEvent<int> {}


public class DeviceSelector : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI displayTMP;

    [SerializeField] TMP_Dropdown dropdown;

    [SerializeField] Color displayActiveColor;

    [SerializeField] Color displayInactiveColor;

    //[SerializeField] UnityEventInt onDisplayPatchChanged;

    
    void Start()
    {
        
    }


    void Update()
    {
        
    }

    public int GetDropdownValue()
    {
        if(dropdown == null) return -1;
        return dropdown.value;
    }

    public void SetDeviceActiveColor(bool value)
    {
        displayTMP.color = value ? displayActiveColor : displayInactiveColor;
    }

    // public void OnDropdownValueChanged(int index)
    // {
    //     onDisplayPatchChanged.Invoke(index);
    // }
}