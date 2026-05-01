using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class KeyRebind : MonoBehaviour
{
    
    [Header("Down")] 
    [SerializeField] private Button downButton;
    [SerializeField] private TextMeshProUGUI downBindingText;
    
    [Header("Up")] 
    [SerializeField] private Button upButton;
    [SerializeField] private TextMeshProUGUI upBindingText;
    
    [Header("Left")] 
    [SerializeField] private Button leftButton;
    [SerializeField] private TextMeshProUGUI leftBindingText;
    
    [Header("Right")] 
    [SerializeField] private Button rightButton;
    [SerializeField] private TextMeshProUGUI rightBindingText;
    
    [Header("Roll Left")] 
    [SerializeField] private Button rollLeftButton;
    [SerializeField] private TextMeshProUGUI rollLeftBindingText;
    
    [Header("Throttle Up")] 
    [SerializeField] private Button throttleUpButton;
    [SerializeField] private TextMeshProUGUI throttleUpBindingText;
    
    [Header("Throttle Down")] 
    [SerializeField] private Button throttleDownButton;
    [SerializeField] private TextMeshProUGUI throttleDownBindingText;
    
    [Header("Boost")] 
    [SerializeField] private Button boostButton;
    [SerializeField] private TextMeshProUGUI boostBindingText;
    
    [Header("Pause")] 
    [SerializeField] private Button pauseButton;
    [SerializeField] private TextMeshProUGUI pauseBindingText;
    
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
        Pitch_Up,
        Pitch_Down, 
        Yaw_Right,
        Yaw_Left,
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
                case Binding.Pitch_Up:
                    return _playerInput.Player.Pitch.bindings[1].ToDisplayString(); 
                case Binding.Pitch_Down:
                    return _playerInput.Player.Pitch.bindings[2].ToDisplayString(); 
                case Binding.Yaw_Left:
                    return _playerInput.Player.Yaw.bindings[1].ToDisplayString();
                case Binding.Yaw_Right: 
                    return _playerInput.Player.Yaw.bindings[2].ToDisplayString();
                case Binding.ThrottleUp:
                    return _playerInput.Player.ThrottleUp.bindings[0].ToDisplayString();
                case Binding.ThrottleDown:
                    return _playerInput.Player.ThrottleDown.bindings[0].ToDisplayString();
                case Binding.TogglePause:
                    return _playerInput.Player.TogglePause.bindings[0].ToDisplayString(); 
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
        rollLeftButton.onClick.AddListener(() => { RebindAndDisplay(Binding.Roll_Left); });
        upButton.onClick.AddListener(() => {RebindAndDisplay(Binding.Pitch_Up);});
        downButton.onClick.AddListener(() => {RebindAndDisplay(Binding.Pitch_Down);});
        leftButton.onClick.AddListener(() => {RebindAndDisplay(Binding.Yaw_Left);});
        rightButton.onClick.AddListener(() => {RebindAndDisplay(Binding.Yaw_Right);});
        throttleUpButton.onClick.AddListener(() => {RebindAndDisplay(Binding.ThrottleUp);});
        throttleDownButton.onClick.AddListener(() => {RebindAndDisplay(Binding.ThrottleDown);});
        pauseButton.onClick.AddListener(() => {RebindAndDisplay(Binding.TogglePause);});
        
    }

    private void Start()
    {
        UpdateBindings(); 
        
    }

    private void UpdateBindings()
    {
        rollRightBindingText.text = GetBindingText(Binding.Roll_Right); 
        rollLeftBindingText.text = GetBindingText(Binding.Roll_Left);
        upBindingText.text = GetBindingText(Binding.Pitch_Up); 
        downBindingText.text = GetBindingText(Binding.Pitch_Down); 
        leftBindingText.text = GetBindingText(Binding.Yaw_Left); 
        rightBindingText.text = GetBindingText(Binding.Yaw_Right); 
        throttleUpBindingText.text = GetBindingText(Binding.ThrottleUp); 
        throttleDownBindingText.text = GetBindingText(Binding.ThrottleDown); 
        pauseBindingText.text = GetBindingText(Binding.TogglePause);

        
        // TODO: boost isn't connected to the unity input system, ask for clarification
        boostBindingText.text = "Space"; 

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
            case Binding.Roll_Left:
                inputAction = _playerInput.Player.Roll;
                bindingIndex = 2;
                break; 
            case Binding.Pitch_Up:
                inputAction = _playerInput.Player.Pitch;
                bindingIndex = 1;
                break;
            case Binding.Pitch_Down:
                inputAction = _playerInput.Player.Pitch;
                bindingIndex = 2;
                break;
            case Binding.Yaw_Left:
                inputAction = _playerInput.Player.Yaw;
                bindingIndex = 1;
                break;
            case Binding.Yaw_Right:
                inputAction = _playerInput.Player.Yaw;
                bindingIndex = 2;
                break;
            case Binding.ThrottleUp:
                inputAction = _playerInput.Player.ThrottleUp;
                bindingIndex = 0;
                break;
            case Binding.ThrottleDown:
                inputAction = _playerInput.Player.ThrottleDown;
                bindingIndex = 0;
                break;
            case Binding.TogglePause:
                inputAction = _playerInput.Player.TogglePause;
                bindingIndex = 0;
                break;
        }

        inputAction.PerformInteractiveRebinding(bindingIndex)
            .OnComplete(callback =>
            {
                Debug.Log(callback.action.bindings[bindingIndex].path);
                Debug.Log(callback.action.bindings[bindingIndex].overridePath);

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
