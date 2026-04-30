using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class KeyRebind : MonoBehaviour
{
    
    // [Header("Down")] 
    // [SerializeField] private Button downButton;
    // [SerializeField] private TextMeshProUGUI downBindingText;
    //
    // [Header("Up")] 
    // [SerializeField] private Button upButton;
    // [SerializeField] private TextMeshProUGUI upBindingText;
    //
    // [Header("Left")] 
    // [SerializeField] private Button leftButton;
    // [SerializeField] private TextMeshProUGUI leftBindingText;
    //
    // [Header("Right")] 
    // [SerializeField] private Button rightButton;
    // [SerializeField] private TextMeshProUGUI rightBindingText;
    //
    [Header("Roll Left")] 
    [SerializeField] private Button rollLeftButton;
    [SerializeField] private TextMeshProUGUI rollLeftBindingText;
    
    // [Header("Throttle Up")] 
    // [SerializeField] private Button throttleUpButton;
    // [SerializeField] private TextMeshProUGUI throttleUpBindingText;
    //
    // [Header("Throttle Down")] 
    // [SerializeField] private Button throttleDownButton;
    // [SerializeField] private TextMeshProUGUI throttleDownBindingText;
    //
    // [Header("Boost")] 
    // [SerializeField] private Button boostButton;
    // [SerializeField] private TextMeshProUGUI boostBindingText;
    //
    // [Header("Pause")] 
    // [SerializeField] private Button pauseButton;
    // [SerializeField] private TextMeshProUGUI pauseBindingText;
    //
    [Header("Roll Right")] 
    [SerializeField] private Button rollRightButton;
    [SerializeField] private TextMeshProUGUI rollRightBindingText;


    [Header("Misc.")] [SerializeField] private GameObject rebindOverlay; 
    
    
    // [PRIVATE FIELDS] 
    private const string PLAYER_PREFS_BINDINGS = "InputBindings";
    
    private PlayerInput _playerInput; 
    public enum Binding
    {
        Roll_Right, 
        Roll_Left,
        Pitch, 
        Yaw, 
        ThrottleUp, 
        ThrottleDown, 
        TogglePause, 
    }

    public string GetBindingText(Binding binding)
    {
        switch (binding)
        {
            default:
                case Binding.Roll_Right:
                    return _playerInput.Player.Roll.bindings[1].ToDisplayString(); 
                case Binding.Roll_Left:
                    return _playerInput.Player.Roll.bindings[2].ToDisplayString(); 
        }
    }

    private void Awake()
    {
        _playerInput = new PlayerInput();
        
        if (PlayerPrefs.HasKey(PLAYER_PREFS_BINDINGS))
        {
            _playerInput.LoadBindingOverridesFromJson(PlayerPrefs.GetString(PLAYER_PREFS_BINDINGS));
        }
        
        _playerInput.Player.Enable();

        
        // [LISTENERS]
        rollRightButton.onClick.AddListener(() => {RebindAndDisplay(Binding.Roll_Right);});

        
    }

    private void Start()
    {
        UpdateBindings(); 
        
    }

    private void UpdateBindings()
    {
        rollRightBindingText.text = GetBindingText(Binding.Roll_Right); 
    }

    private void RebindBinding(Binding binding, Action onActionRebound)
    {
        _playerInput.Player.Disable();

        InputAction inputAction;
        int bindingIndex;

        switch (binding)
        {
            default:
            case Binding.Roll_Right:
                inputAction = _playerInput.Player.Roll;
                bindingIndex = 1;
                break; 
        }

        inputAction.PerformInteractiveRebinding(bindingIndex)
            .OnComplete(callback =>
            {
                Debug.Log(callback.action.bindings[1].path);
                Debug.Log(callback.action.bindings[1].overridePath);

                callback.Dispose();
                _playerInput.Player.Enable();

                onActionRebound();

                _playerInput.SaveBindingOverridesAsJson();
                PlayerPrefs.SetString(PLAYER_PREFS_BINDINGS, _playerInput.SaveBindingOverridesAsJson()); 
                
                

            }) 
            .Start();
    }

    private void RebindAndDisplay(Binding binding)
    {
        ShowRebindOverlay();
        RebindBinding(binding, () =>
        { 
            HideRebindOverlay();
            UpdateBindings();
        }); 
        
        
    }
    private void ShowRebindOverlay()
    {
        rebindOverlay.SetActive(true);
    }
    private void HideRebindOverlay()
    {
        rebindOverlay.SetActive(false);

    }
}
