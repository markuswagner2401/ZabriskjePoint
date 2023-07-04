using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MyConsole : MonoBehaviour
{
  
    public static MyConsole Instance { get; private set; }

   
    [SerializeField]
    private TextMeshProUGUI consoleText;

    private void Awake()
    {
        
        if (Instance == null)
        {
           
            Instance = this;
        }
        else if (Instance != this)
        {
            
            Destroy(gameObject);
        }

     
        DontDestroyOnLoad(gameObject);
    }

   
    public void Print(string message)
    {
       
        if (gameObject.activeInHierarchy)
        {
          
            consoleText.text = message + "\n" + consoleText.text;
        }
    }
}