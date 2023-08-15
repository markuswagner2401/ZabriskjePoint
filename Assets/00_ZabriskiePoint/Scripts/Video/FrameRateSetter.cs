using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrameRateSetter : MonoBehaviour
{
    [SerializeField] int tagetFrameRate = 30;
    void Start()
    {
        Application.targetFrameRate = tagetFrameRate;
    }

    
}
