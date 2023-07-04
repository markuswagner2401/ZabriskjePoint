using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using System;

public class MyConsole : MonoBehaviour
{
  
    public static MyConsole Instance { get; private set; }

   
    [SerializeField]
    private TextMeshProUGUI consoleText;

    [SerializeField] int maxLines = 5;

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
        string newText = message + "\n" + consoleText.text;
        string[] lines = newText.Split(new[] { "\n" }, StringSplitOptions.None);

        if (lines.Length > maxLines)
        {
            lines = lines.Take(maxLines).ToArray();
        }

        consoleText.text = String.Join("\n", lines);
    }
}
}