using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System;
using com.rfilkov.components;

public class InputReader : MonoBehaviour, ZabrPointControls.IMenueActions
{
    [SerializeField] GameObject[] displayManagerUIGos;

    [SerializeField] GameObject[] quitterGos;
    ZabrPointControls controls;

    


    
    void Start()
    {
        controls = new ZabrPointControls();
        controls.Menue.SetCallbacks(this);
        controls.Menue.Enable();

    }

    private void OnDestroy()
    {
        controls.Menue.Disable();
    }

    
    void Update()
    {

    }

    public void OnToggleDisplayManager(InputAction.CallbackContext context)
    {
        if(!context.performed) return;
        foreach (var item in displayManagerUIGos)
        {
            item.SetActive(!item.activeInHierarchy);
            if(item.activeInHierarchy)
            {
                GetComponent<DeviceManager>().Initialize();
            }
        }
    }

    public void OnQuit(InputAction.CallbackContext context)
    {
        if(!context.performed) return;

        GetComponent<GameManager>().ShowQuitUI(true);
    }

    public void OnCaptureDefaultTexture(InputAction.CallbackContext context)
    {
        if(!context.performed) return;

        print("on capture");

        FindObjectOfType<ColorCamUserImageProvider>()?.CaptureDefaultTexture();
    }
}
