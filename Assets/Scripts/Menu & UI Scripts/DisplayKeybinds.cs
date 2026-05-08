using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class DisplayKeybinds: MonoBehaviour
{
    [Header("Left Column")] 
    [SerializeField] private TextMeshProUGUI upBindingText;
    [SerializeField] private TextMeshProUGUI downBindingText;
    [SerializeField] private TextMeshProUGUI leftBindingText;
    [SerializeField] private TextMeshProUGUI rightBindingText;
    [SerializeField] private TextMeshProUGUI rollLeftBindingText;
    
    [Header("Right Column")] 
    [SerializeField] private TextMeshProUGUI throttleUpBindingText;
    [SerializeField] private TextMeshProUGUI throttleDownBindingText;
    [SerializeField] private TextMeshProUGUI boostBindingText;
    [SerializeField] private TextMeshProUGUI pauseBindingText;
    [SerializeField] private TextMeshProUGUI rollRightBindingText;

    private PlayerInput _playerInput; 
    private void Awake()
    {
        _playerInput = new PlayerInput();
        
        if (PlayerPrefs.HasKey(KeyRebind.PLAYER_PREFS_BINDINGS))
        {
            _playerInput.LoadBindingOverridesFromJson(PlayerPrefs.GetString(KeyRebind.PLAYER_PREFS_BINDINGS));
            
        }
        
        upBindingText.text = _playerInput.Player.Pitch.bindings[1].ToDisplayString();   
        downBindingText.text = _playerInput.Player.Pitch.bindings[2].ToDisplayString(); 
        leftBindingText.text =  _playerInput.Player.Yaw.bindings[1].ToDisplayString();
        rightBindingText.text = _playerInput.Player.Yaw.bindings[2].ToDisplayString();
        rollLeftBindingText.text = _playerInput.Player.Roll.bindings[2].ToDisplayString(); 
        
        
        throttleUpBindingText.text = _playerInput.Player.ThrottleUp.bindings[0].ToDisplayString(); 
        throttleDownBindingText.text = _playerInput.Player.ThrottleDown.bindings[0].ToDisplayString(); 
        boostBindingText.text = _playerInput.Player.Boost.bindings[0].ToDisplayString();  
        pauseBindingText.text = _playerInput.Player.TogglePause.bindings[0].ToDisplayString(); 
        rollRightBindingText.text = _playerInput.Player.Roll.bindings[1].ToDisplayString(); 
    }

    
}
