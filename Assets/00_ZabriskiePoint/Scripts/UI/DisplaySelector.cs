using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Events;

[System.Serializable]
public class UnityEventInt : UnityEvent<int> {}


public class DisplaySelector : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI displayTMP;

    [SerializeField] Color displayActiveColor;

    [SerializeField] Color displayInactiveColor;

    [SerializeField] UnityEventInt onDisplayPatchChanged;

    
    void Start()
    {
        
    }


    void Update()
    {
        
    }

    public void SetDisplayActiveColor(bool value)
    {
        displayTMP.color = value ? displayActiveColor : displayInactiveColor;
    }

    public void OnDropdownValueChanged(int index)
    {
        onDisplayPatchChanged.Invoke(index);
    }
}
