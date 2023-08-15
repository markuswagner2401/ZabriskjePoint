using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Events;

[RequireComponent(typeof(TMP_InputField))]
public class InputFieldSaverRestorer : MonoBehaviour
{
    [Header("Uses gameObject.name as unique identifier, so set an unique name in the hierarchy")]
    
    [SerializeField] TMP_InputField tMP_InputField = null;

    

    private void Start()
    {
        if (tMP_InputField == null)
        {
            tMP_InputField = GetComponent<TMP_InputField>();
        }

        tMP_InputField.onEndEdit.AddListener(SaveValue);

        RestoreValue();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            ClearPlayerPrefs();
        }
    }

    private void OnDisable()
    {
        tMP_InputField.onEndEdit.RemoveListener(SaveValue);
        SaveValue(tMP_InputField.text);
    }

    void RestoreValue()
    {
        string key = "InputFieldValueOfOf_" + gameObject.name;

        if (PlayerPrefs.HasKey(key))
        {
            tMP_InputField.text = PlayerPrefs.GetString(key);
        }
    }

    public void SaveValue(string value)
    {
        string key = "InputFieldValueOfOf_" + gameObject.name;

        PlayerPrefs.SetString(key, value);
    }

    public void ClearPlayerPrefs()
    {
        string key = "InputFieldValueOfOf_" + gameObject.name;
        PlayerPrefs.DeleteKey(key);
    }
}