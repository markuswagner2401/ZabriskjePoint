//------------------------------------------------------------------------------
// <auto-generated>
//     This code was auto-generated by com.unity.inputsystem:InputActionCodeGenerator
//     version 1.6.1
//     from Assets/ZabrPointControls.inputactions
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public partial class @ZabrPointControls: IInputActionCollection2, IDisposable
{
    public InputActionAsset asset { get; }
    public @ZabrPointControls()
    {
        asset = InputActionAsset.FromJson(@"{
    ""name"": ""ZabrPointControls"",
    ""maps"": [
        {
            ""name"": ""Menue"",
            ""id"": ""8c521ee3-5722-4aaf-b899-0e3603496c8a"",
            ""actions"": [
                {
                    ""name"": ""ToggleDisplayManager"",
                    ""type"": ""Button"",
                    ""id"": ""8178d524-da2f-44b9-9ff1-29fb615509de"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""Quit"",
                    ""type"": ""Button"",
                    ""id"": ""3645d5c9-0458-4161-80e7-5b3d4fe93e6d"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""CaptureDefaultTexture"",
                    ""type"": ""Button"",
                    ""id"": ""14218cf1-6887-4032-93c4-25b4ddf26cf4"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""Reset"",
                    ""type"": ""Button"",
                    ""id"": ""98ef32a0-a2c0-4d46-ac33-0ae8349b2782"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""fcf9e7f6-1e87-4b5a-a3f7-7d878f51b0e3"",
                    ""path"": ""<Keyboard>/m"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ToggleDisplayManager"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""60616e0a-60fb-4bda-acad-b5c0851ee2c4"",
                    ""path"": ""<Keyboard>/q"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Mouse&Keyboard"",
                    ""action"": ""Quit"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""ed794bc8-1134-4db7-82a8-6dc7df204272"",
                    ""path"": ""<Keyboard>/c"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Mouse&Keyboard"",
                    ""action"": ""CaptureDefaultTexture"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""6ad872cb-0ace-4814-9075-0cd448b485c6"",
                    ""path"": ""<Keyboard>/r"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Reset"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": [
        {
            ""name"": ""Mouse&Keyboard"",
            ""bindingGroup"": ""Mouse&Keyboard"",
            ""devices"": [
                {
                    ""devicePath"": ""<Keyboard>"",
                    ""isOptional"": false,
                    ""isOR"": false
                },
                {
                    ""devicePath"": ""<Mouse>"",
                    ""isOptional"": false,
                    ""isOR"": false
                }
            ]
        }
    ]
}");
        // Menue
        m_Menue = asset.FindActionMap("Menue", throwIfNotFound: true);
        m_Menue_ToggleDisplayManager = m_Menue.FindAction("ToggleDisplayManager", throwIfNotFound: true);
        m_Menue_Quit = m_Menue.FindAction("Quit", throwIfNotFound: true);
        m_Menue_CaptureDefaultTexture = m_Menue.FindAction("CaptureDefaultTexture", throwIfNotFound: true);
        m_Menue_Reset = m_Menue.FindAction("Reset", throwIfNotFound: true);
    }

    public void Dispose()
    {
        UnityEngine.Object.Destroy(asset);
    }

    public InputBinding? bindingMask
    {
        get => asset.bindingMask;
        set => asset.bindingMask = value;
    }

    public ReadOnlyArray<InputDevice>? devices
    {
        get => asset.devices;
        set => asset.devices = value;
    }

    public ReadOnlyArray<InputControlScheme> controlSchemes => asset.controlSchemes;

    public bool Contains(InputAction action)
    {
        return asset.Contains(action);
    }

    public IEnumerator<InputAction> GetEnumerator()
    {
        return asset.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Enable()
    {
        asset.Enable();
    }

    public void Disable()
    {
        asset.Disable();
    }

    public IEnumerable<InputBinding> bindings => asset.bindings;

    public InputAction FindAction(string actionNameOrId, bool throwIfNotFound = false)
    {
        return asset.FindAction(actionNameOrId, throwIfNotFound);
    }

    public int FindBinding(InputBinding bindingMask, out InputAction action)
    {
        return asset.FindBinding(bindingMask, out action);
    }

    // Menue
    private readonly InputActionMap m_Menue;
    private List<IMenueActions> m_MenueActionsCallbackInterfaces = new List<IMenueActions>();
    private readonly InputAction m_Menue_ToggleDisplayManager;
    private readonly InputAction m_Menue_Quit;
    private readonly InputAction m_Menue_CaptureDefaultTexture;
    private readonly InputAction m_Menue_Reset;
    public struct MenueActions
    {
        private @ZabrPointControls m_Wrapper;
        public MenueActions(@ZabrPointControls wrapper) { m_Wrapper = wrapper; }
        public InputAction @ToggleDisplayManager => m_Wrapper.m_Menue_ToggleDisplayManager;
        public InputAction @Quit => m_Wrapper.m_Menue_Quit;
        public InputAction @CaptureDefaultTexture => m_Wrapper.m_Menue_CaptureDefaultTexture;
        public InputAction @Reset => m_Wrapper.m_Menue_Reset;
        public InputActionMap Get() { return m_Wrapper.m_Menue; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(MenueActions set) { return set.Get(); }
        public void AddCallbacks(IMenueActions instance)
        {
            if (instance == null || m_Wrapper.m_MenueActionsCallbackInterfaces.Contains(instance)) return;
            m_Wrapper.m_MenueActionsCallbackInterfaces.Add(instance);
            @ToggleDisplayManager.started += instance.OnToggleDisplayManager;
            @ToggleDisplayManager.performed += instance.OnToggleDisplayManager;
            @ToggleDisplayManager.canceled += instance.OnToggleDisplayManager;
            @Quit.started += instance.OnQuit;
            @Quit.performed += instance.OnQuit;
            @Quit.canceled += instance.OnQuit;
            @CaptureDefaultTexture.started += instance.OnCaptureDefaultTexture;
            @CaptureDefaultTexture.performed += instance.OnCaptureDefaultTexture;
            @CaptureDefaultTexture.canceled += instance.OnCaptureDefaultTexture;
            @Reset.started += instance.OnReset;
            @Reset.performed += instance.OnReset;
            @Reset.canceled += instance.OnReset;
        }

        private void UnregisterCallbacks(IMenueActions instance)
        {
            @ToggleDisplayManager.started -= instance.OnToggleDisplayManager;
            @ToggleDisplayManager.performed -= instance.OnToggleDisplayManager;
            @ToggleDisplayManager.canceled -= instance.OnToggleDisplayManager;
            @Quit.started -= instance.OnQuit;
            @Quit.performed -= instance.OnQuit;
            @Quit.canceled -= instance.OnQuit;
            @CaptureDefaultTexture.started -= instance.OnCaptureDefaultTexture;
            @CaptureDefaultTexture.performed -= instance.OnCaptureDefaultTexture;
            @CaptureDefaultTexture.canceled -= instance.OnCaptureDefaultTexture;
            @Reset.started -= instance.OnReset;
            @Reset.performed -= instance.OnReset;
            @Reset.canceled -= instance.OnReset;
        }

        public void RemoveCallbacks(IMenueActions instance)
        {
            if (m_Wrapper.m_MenueActionsCallbackInterfaces.Remove(instance))
                UnregisterCallbacks(instance);
        }

        public void SetCallbacks(IMenueActions instance)
        {
            foreach (var item in m_Wrapper.m_MenueActionsCallbackInterfaces)
                UnregisterCallbacks(item);
            m_Wrapper.m_MenueActionsCallbackInterfaces.Clear();
            AddCallbacks(instance);
        }
    }
    public MenueActions @Menue => new MenueActions(this);
    private int m_MouseKeyboardSchemeIndex = -1;
    public InputControlScheme MouseKeyboardScheme
    {
        get
        {
            if (m_MouseKeyboardSchemeIndex == -1) m_MouseKeyboardSchemeIndex = asset.FindControlSchemeIndex("Mouse&Keyboard");
            return asset.controlSchemes[m_MouseKeyboardSchemeIndex];
        }
    }
    public interface IMenueActions
    {
        void OnToggleDisplayManager(InputAction.CallbackContext context);
        void OnQuit(InputAction.CallbackContext context);
        void OnCaptureDefaultTexture(InputAction.CallbackContext context);
        void OnReset(InputAction.CallbackContext context);
    }
}
