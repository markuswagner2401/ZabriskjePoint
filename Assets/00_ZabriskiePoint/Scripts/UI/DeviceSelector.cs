using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Events;

[System.Serializable]
public class UnityEventInt : UnityEvent<int> {}


public class DeviceSelector : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI TitelTMP;

    [SerializeField] TMP_Dropdown displayDropdown;

    [SerializeField] TextMeshProUGUI displayDropdownLabel;

    [SerializeField] TMP_Dropdown sensorDropdown;

    [SerializeField] TextMeshProUGUI sensorDropdownLabel;

    [SerializeField] Color validColor;

    [SerializeField] Color invalidColor;

    //[SerializeField] UnityEventInt onDisplayPatchChanged;

    
    void Start()
    {
        
    }


    void Update()
    {
        
    }

    ////

    public int GetDisplayDropdownValue()
    {
        if(displayDropdown == null) return -1;
        return displayDropdown.value;
    }

    public void SetDisplayActiveColor(bool value)
    {
        displayDropdownLabel.color = value ? validColor : invalidColor;
    }

    ////

    public int GetSensorDropdownValue()
    {
        if(displayDropdown == null) return -1;
        return displayDropdown.value;
    }

    public void SetSensorActiveColor(bool value)
    {
        sensorDropdownLabel.color = value ? validColor : invalidColor;
    }

    ////

    public void SetSetupCompleteColor(bool value)
    {
        TitelTMP.color = value ? validColor : invalidColor;
    }

    

    // public void OnDropdownValueChanged(int index)
    // {
    //     onDisplayPatchChanged.Invoke(index);
    // }
}
