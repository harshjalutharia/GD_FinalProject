//------------------------------------------------------------------------------
// <auto-generated>
//     This code was auto-generated by com.unity.inputsystem:InputActionCodeGenerator
//     version 1.7.0
//     from Assets/Inputs/PlayerControls.inputactions
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

public partial class @PlayerControls: IInputActionCollection2, IDisposable
{
    public InputActionAsset asset { get; }
    public @PlayerControls()
    {
        asset = InputActionAsset.FromJson(@"{
    ""name"": ""PlayerControls"",
    ""maps"": [
        {
            ""name"": ""Player"",
            ""id"": ""9f7d7f2c-e102-458e-b516-c14a0612bec3"",
            ""actions"": [
                {
                    ""name"": ""Move"",
                    ""type"": ""Value"",
                    ""id"": ""d455a92d-debe-4657-a5fd-2f3b9be03d18"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": true
                },
                {
                    ""name"": ""View"",
                    ""type"": ""PassThrough"",
                    ""id"": ""ee4fe8bd-53a0-4cc4-af0f-871205a12788"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": true
                },
                {
                    ""name"": ""Jump"",
                    ""type"": ""Button"",
                    ""id"": ""a033dacd-cb7d-4bb0-bded-c53be332e39d"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""Fly"",
                    ""type"": ""Button"",
                    ""id"": ""1f02b888-5941-42e1-b773-4e4b4b735e48"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Hold"",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""SkipCutscene"",
                    ""type"": ""Button"",
                    ""id"": ""e1c1aede-bcde-4484-b990-aca992bc44db"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Hold(duration=0.5,pressPoint=0.5)"",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""Sprint"",
                    ""type"": ""Button"",
                    ""id"": ""5aade9b9-d97f-4907-a67d-831e529c8f2e"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""RingBell"",
                    ""type"": ""Button"",
                    ""id"": ""00c486d6-9ce8-44c2-94fb-60a4e830eeff"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""Menu"",
                    ""type"": ""Button"",
                    ""id"": ""389b4333-a538-4b91-93f0-221c796da526"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""Switch Boost"",
                    ""type"": ""Button"",
                    ""id"": ""f4d4341c-2527-4d10-9fe8-e61d5c7cd759"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""Boost"",
                    ""type"": ""Button"",
                    ""id"": ""8a4838fa-8b35-4ddc-aa3b-fd8a6bc13d66"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Hold(duration=0.1)"",
                    ""initialStateCheck"": false
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""c56445a2-22ca-4fdb-9991-241171287f6d"",
                    ""path"": ""<Gamepad>/leftStick"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""WASD [Keyboard]"",
                    ""id"": ""a4d9abe4-a077-4dba-84c7-640131b3fc40"",
                    ""path"": ""2DVector"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""up"",
                    ""id"": ""d30a9ab9-9fcf-4907-923a-12a7df490652"",
                    ""path"": ""<Keyboard>/w"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""down"",
                    ""id"": ""2ace8a0d-6b47-4c15-bf29-3a62b0013570"",
                    ""path"": ""<Keyboard>/s"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""left"",
                    ""id"": ""2e4b33eb-16bc-4407-bbdc-409a9fe94648"",
                    ""path"": ""<Keyboard>/a"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""right"",
                    ""id"": ""26310620-2474-488d-8633-1c19cf42dd71"",
                    ""path"": ""<Keyboard>/d"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": """",
                    ""id"": ""d59e05ac-fb46-4e75-83a9-2b4f5b363e94"",
                    ""path"": ""<Gamepad>/buttonSouth"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Jump"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""ac2f3d73-7157-424b-a30b-78d65e24f26d"",
                    ""path"": ""<Keyboard>/space"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Jump"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""29b54d5b-03e2-468c-adc7-0b7eab1b1fb0"",
                    ""path"": ""<Gamepad>/rightShoulder"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""RingBell"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""2a20a618-5cd6-49b7-a53f-2e1b0ae56981"",
                    ""path"": ""<Mouse>/rightButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""RingBell"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""dd88bcce-b07c-49fd-b97b-691db3219eca"",
                    ""path"": ""<Keyboard>/e"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""RingBell"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""12042164-3eb2-44f5-9c28-1f7299c905f4"",
                    ""path"": ""<Gamepad>/start"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Menu"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""7c73e443-97e1-4807-9527-de1c8eee3ef5"",
                    ""path"": ""<Keyboard>/m"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Menu"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""8cab654d-a508-4782-8268-d4eb0c30838b"",
                    ""path"": ""<Keyboard>/leftShift"",
                    ""interactions"": ""Hold(duration=0.1)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Sprint"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""8d323b34-0ea9-4080-aad8-a91fe25b7353"",
                    ""path"": ""<Gamepad>/buttonSouth"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""SkipCutscene"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""07975a17-fc59-4737-8b28-b5dbdf3e047b"",
                    ""path"": ""<Keyboard>/space"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""SkipCutscene"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""bbfd9bef-b4af-4d4e-ba26-15a88e258c67"",
                    ""path"": ""<Gamepad>/buttonSouth"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Fly"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""0d33c563-4c4e-4ea5-865a-8b43601254c7"",
                    ""path"": ""<Keyboard>/space"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Fly"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""4b1c76c3-cc6a-445d-9f97-3ff7adae8807"",
                    ""path"": ""<Mouse>/leftButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Boost"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""5448d421-f1b4-4c35-94bc-36c29712c08f"",
                    ""path"": ""<Gamepad>/leftShoulder"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Boost"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""3d9f2b39-77f9-4ac4-8933-c8854635ae04"",
                    ""path"": ""<Gamepad>/leftStickPress"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Switch Boost"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""aadb22f6-7c7a-464c-8190-37f0b2253b0b"",
                    ""path"": ""<Gamepad>/rightStick"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""View"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""2e6ed541-d02e-4453-8ae1-e46dc7ded0cd"",
                    ""path"": ""<Mouse>/delta"",
                    ""interactions"": """",
                    ""processors"": ""ScaleVector2(x=0.1,y=0.2)"",
                    ""groups"": """",
                    ""action"": ""View"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        },
        {
            ""name"": ""Debug"",
            ""id"": ""29e3d956-e2cc-4b92-b186-05c4e15570c6"",
            ""actions"": [
                {
                    ""name"": ""ActivateFlight"",
                    ""type"": ""Button"",
                    ""id"": ""a0313c3d-64e6-468a-8da9-38f86200c243"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""DebugUI"",
                    ""type"": ""Button"",
                    ""id"": ""4d9f9952-d8e1-4c18-a716-43908ce01d83"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""DebugFPS"",
                    ""type"": ""Button"",
                    ""id"": ""86225699-b534-4a64-a5cd-17328f5c6954"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""1099a2d5-f9be-4911-b968-7255a2317992"",
                    ""path"": ""<Keyboard>/c"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ActivateFlight"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""d453d612-227b-4b1c-b303-b8dc432b4be7"",
                    ""path"": ""<Gamepad>/dpad/down"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ActivateFlight"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""b786da59-f711-4a95-892d-c80ce43cf219"",
                    ""path"": ""<Keyboard>/1"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""DebugUI"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""e584e80e-3b6f-4996-9cd3-89db7b1667e8"",
                    ""path"": ""<Gamepad>/dpad/right"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""DebugUI"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""bb26bf2c-79af-45d7-be98-cc61149e7745"",
                    ""path"": ""<Keyboard>/2"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""DebugFPS"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""d18f26ef-249c-4799-991b-aa1fc598a190"",
                    ""path"": ""<Gamepad>/dpad/down"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""DebugFPS"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": []
}");
        // Player
        m_Player = asset.FindActionMap("Player", throwIfNotFound: true);
        m_Player_Move = m_Player.FindAction("Move", throwIfNotFound: true);
        m_Player_View = m_Player.FindAction("View", throwIfNotFound: true);
        m_Player_Jump = m_Player.FindAction("Jump", throwIfNotFound: true);
        m_Player_Fly = m_Player.FindAction("Fly", throwIfNotFound: true);
        m_Player_SkipCutscene = m_Player.FindAction("SkipCutscene", throwIfNotFound: true);
        m_Player_Sprint = m_Player.FindAction("Sprint", throwIfNotFound: true);
        m_Player_RingBell = m_Player.FindAction("RingBell", throwIfNotFound: true);
        m_Player_Menu = m_Player.FindAction("Menu", throwIfNotFound: true);
        m_Player_SwitchBoost = m_Player.FindAction("Switch Boost", throwIfNotFound: true);
        m_Player_Boost = m_Player.FindAction("Boost", throwIfNotFound: true);
        // Debug
        m_Debug = asset.FindActionMap("Debug", throwIfNotFound: true);
        m_Debug_ActivateFlight = m_Debug.FindAction("ActivateFlight", throwIfNotFound: true);
        m_Debug_DebugUI = m_Debug.FindAction("DebugUI", throwIfNotFound: true);
        m_Debug_DebugFPS = m_Debug.FindAction("DebugFPS", throwIfNotFound: true);
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

    // Player
    private readonly InputActionMap m_Player;
    private List<IPlayerActions> m_PlayerActionsCallbackInterfaces = new List<IPlayerActions>();
    private readonly InputAction m_Player_Move;
    private readonly InputAction m_Player_View;
    private readonly InputAction m_Player_Jump;
    private readonly InputAction m_Player_Fly;
    private readonly InputAction m_Player_SkipCutscene;
    private readonly InputAction m_Player_Sprint;
    private readonly InputAction m_Player_RingBell;
    private readonly InputAction m_Player_Menu;
    private readonly InputAction m_Player_SwitchBoost;
    private readonly InputAction m_Player_Boost;
    public struct PlayerActions
    {
        private @PlayerControls m_Wrapper;
        public PlayerActions(@PlayerControls wrapper) { m_Wrapper = wrapper; }
        public InputAction @Move => m_Wrapper.m_Player_Move;
        public InputAction @View => m_Wrapper.m_Player_View;
        public InputAction @Jump => m_Wrapper.m_Player_Jump;
        public InputAction @Fly => m_Wrapper.m_Player_Fly;
        public InputAction @SkipCutscene => m_Wrapper.m_Player_SkipCutscene;
        public InputAction @Sprint => m_Wrapper.m_Player_Sprint;
        public InputAction @RingBell => m_Wrapper.m_Player_RingBell;
        public InputAction @Menu => m_Wrapper.m_Player_Menu;
        public InputAction @SwitchBoost => m_Wrapper.m_Player_SwitchBoost;
        public InputAction @Boost => m_Wrapper.m_Player_Boost;
        public InputActionMap Get() { return m_Wrapper.m_Player; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(PlayerActions set) { return set.Get(); }
        public void AddCallbacks(IPlayerActions instance)
        {
            if (instance == null || m_Wrapper.m_PlayerActionsCallbackInterfaces.Contains(instance)) return;
            m_Wrapper.m_PlayerActionsCallbackInterfaces.Add(instance);
            @Move.started += instance.OnMove;
            @Move.performed += instance.OnMove;
            @Move.canceled += instance.OnMove;
            @View.started += instance.OnView;
            @View.performed += instance.OnView;
            @View.canceled += instance.OnView;
            @Jump.started += instance.OnJump;
            @Jump.performed += instance.OnJump;
            @Jump.canceled += instance.OnJump;
            @Fly.started += instance.OnFly;
            @Fly.performed += instance.OnFly;
            @Fly.canceled += instance.OnFly;
            @SkipCutscene.started += instance.OnSkipCutscene;
            @SkipCutscene.performed += instance.OnSkipCutscene;
            @SkipCutscene.canceled += instance.OnSkipCutscene;
            @Sprint.started += instance.OnSprint;
            @Sprint.performed += instance.OnSprint;
            @Sprint.canceled += instance.OnSprint;
            @RingBell.started += instance.OnRingBell;
            @RingBell.performed += instance.OnRingBell;
            @RingBell.canceled += instance.OnRingBell;
            @Menu.started += instance.OnMenu;
            @Menu.performed += instance.OnMenu;
            @Menu.canceled += instance.OnMenu;
            @SwitchBoost.started += instance.OnSwitchBoost;
            @SwitchBoost.performed += instance.OnSwitchBoost;
            @SwitchBoost.canceled += instance.OnSwitchBoost;
            @Boost.started += instance.OnBoost;
            @Boost.performed += instance.OnBoost;
            @Boost.canceled += instance.OnBoost;
        }

        private void UnregisterCallbacks(IPlayerActions instance)
        {
            @Move.started -= instance.OnMove;
            @Move.performed -= instance.OnMove;
            @Move.canceled -= instance.OnMove;
            @View.started -= instance.OnView;
            @View.performed -= instance.OnView;
            @View.canceled -= instance.OnView;
            @Jump.started -= instance.OnJump;
            @Jump.performed -= instance.OnJump;
            @Jump.canceled -= instance.OnJump;
            @Fly.started -= instance.OnFly;
            @Fly.performed -= instance.OnFly;
            @Fly.canceled -= instance.OnFly;
            @SkipCutscene.started -= instance.OnSkipCutscene;
            @SkipCutscene.performed -= instance.OnSkipCutscene;
            @SkipCutscene.canceled -= instance.OnSkipCutscene;
            @Sprint.started -= instance.OnSprint;
            @Sprint.performed -= instance.OnSprint;
            @Sprint.canceled -= instance.OnSprint;
            @RingBell.started -= instance.OnRingBell;
            @RingBell.performed -= instance.OnRingBell;
            @RingBell.canceled -= instance.OnRingBell;
            @Menu.started -= instance.OnMenu;
            @Menu.performed -= instance.OnMenu;
            @Menu.canceled -= instance.OnMenu;
            @SwitchBoost.started -= instance.OnSwitchBoost;
            @SwitchBoost.performed -= instance.OnSwitchBoost;
            @SwitchBoost.canceled -= instance.OnSwitchBoost;
            @Boost.started -= instance.OnBoost;
            @Boost.performed -= instance.OnBoost;
            @Boost.canceled -= instance.OnBoost;
        }

        public void RemoveCallbacks(IPlayerActions instance)
        {
            if (m_Wrapper.m_PlayerActionsCallbackInterfaces.Remove(instance))
                UnregisterCallbacks(instance);
        }

        public void SetCallbacks(IPlayerActions instance)
        {
            foreach (var item in m_Wrapper.m_PlayerActionsCallbackInterfaces)
                UnregisterCallbacks(item);
            m_Wrapper.m_PlayerActionsCallbackInterfaces.Clear();
            AddCallbacks(instance);
        }
    }
    public PlayerActions @Player => new PlayerActions(this);

    // Debug
    private readonly InputActionMap m_Debug;
    private List<IDebugActions> m_DebugActionsCallbackInterfaces = new List<IDebugActions>();
    private readonly InputAction m_Debug_ActivateFlight;
    private readonly InputAction m_Debug_DebugUI;
    private readonly InputAction m_Debug_DebugFPS;
    public struct DebugActions
    {
        private @PlayerControls m_Wrapper;
        public DebugActions(@PlayerControls wrapper) { m_Wrapper = wrapper; }
        public InputAction @ActivateFlight => m_Wrapper.m_Debug_ActivateFlight;
        public InputAction @DebugUI => m_Wrapper.m_Debug_DebugUI;
        public InputAction @DebugFPS => m_Wrapper.m_Debug_DebugFPS;
        public InputActionMap Get() { return m_Wrapper.m_Debug; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(DebugActions set) { return set.Get(); }
        public void AddCallbacks(IDebugActions instance)
        {
            if (instance == null || m_Wrapper.m_DebugActionsCallbackInterfaces.Contains(instance)) return;
            m_Wrapper.m_DebugActionsCallbackInterfaces.Add(instance);
            @ActivateFlight.started += instance.OnActivateFlight;
            @ActivateFlight.performed += instance.OnActivateFlight;
            @ActivateFlight.canceled += instance.OnActivateFlight;
            @DebugUI.started += instance.OnDebugUI;
            @DebugUI.performed += instance.OnDebugUI;
            @DebugUI.canceled += instance.OnDebugUI;
            @DebugFPS.started += instance.OnDebugFPS;
            @DebugFPS.performed += instance.OnDebugFPS;
            @DebugFPS.canceled += instance.OnDebugFPS;
        }

        private void UnregisterCallbacks(IDebugActions instance)
        {
            @ActivateFlight.started -= instance.OnActivateFlight;
            @ActivateFlight.performed -= instance.OnActivateFlight;
            @ActivateFlight.canceled -= instance.OnActivateFlight;
            @DebugUI.started -= instance.OnDebugUI;
            @DebugUI.performed -= instance.OnDebugUI;
            @DebugUI.canceled -= instance.OnDebugUI;
            @DebugFPS.started -= instance.OnDebugFPS;
            @DebugFPS.performed -= instance.OnDebugFPS;
            @DebugFPS.canceled -= instance.OnDebugFPS;
        }

        public void RemoveCallbacks(IDebugActions instance)
        {
            if (m_Wrapper.m_DebugActionsCallbackInterfaces.Remove(instance))
                UnregisterCallbacks(instance);
        }

        public void SetCallbacks(IDebugActions instance)
        {
            foreach (var item in m_Wrapper.m_DebugActionsCallbackInterfaces)
                UnregisterCallbacks(item);
            m_Wrapper.m_DebugActionsCallbackInterfaces.Clear();
            AddCallbacks(instance);
        }
    }
    public DebugActions @Debug => new DebugActions(this);
    public interface IPlayerActions
    {
        void OnMove(InputAction.CallbackContext context);
        void OnView(InputAction.CallbackContext context);
        void OnJump(InputAction.CallbackContext context);
        void OnFly(InputAction.CallbackContext context);
        void OnSkipCutscene(InputAction.CallbackContext context);
        void OnSprint(InputAction.CallbackContext context);
        void OnRingBell(InputAction.CallbackContext context);
        void OnMenu(InputAction.CallbackContext context);
        void OnSwitchBoost(InputAction.CallbackContext context);
        void OnBoost(InputAction.CallbackContext context);
    }
    public interface IDebugActions
    {
        void OnActivateFlight(InputAction.CallbackContext context);
        void OnDebugUI(InputAction.CallbackContext context);
        void OnDebugFPS(InputAction.CallbackContext context);
    }
}
