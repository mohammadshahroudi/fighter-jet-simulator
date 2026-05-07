using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class WeaponFireInput : MonoBehaviour
{
    [Header("Weapons")]
    [SerializeField] private Transform weaponRoot;
    [SerializeField] private bool autoPopulateWeapons = true;
    [SerializeField] private List<GunLogic> guns = new List<GunLogic>();

    [Header("Devices")]
    [SerializeField] private bool enableGamepad = true;
    [SerializeField] private bool enableHotas = true;

    [Header("Gamepad")]
    [SerializeField] [Range(0f, 1f)] private float gamepadTriggerThreshold = 0.15f;
    [SerializeField] private bool useRightTrigger = true;
    [SerializeField] private bool useLeftTrigger;
    [SerializeField] private bool useWestButtonFallback = true;

    [Header("HOTAS")]
    [SerializeField] private string[] hotasFireButtons = { "trigger", "button0", "button1", "button2" };
    [SerializeField] [Range(0f, 1f)] private float hotasPressPoint = 0.5f;

    [Header("Firing")]
    [SerializeField] private float fireRate = 0.1f;

    private float nextFireTime;
    private static readonly MethodInfo ShootRaycastMethod = typeof(GunLogic).GetMethod("ShootRaycast", BindingFlags.NonPublic | BindingFlags.Instance);

    private void Awake()
    {
        if (weaponRoot == null)
        {
            PlayerController playerController = FindFirstObjectByType<PlayerController>();
            weaponRoot = playerController != null ? playerController.transform : transform;
        }

        if (autoPopulateWeapons)
        {
            RefreshWeaponList();
        }
    }

    private void OnValidate()
    {
        if (!autoPopulateWeapons) return;

        if (weaponRoot == null)
        {
            weaponRoot = transform;
        }

        RefreshWeaponList();
    }

    private void Update()
    {
        if (Time.time < nextFireTime) return;
        if (!ShouldFire()) return;

        Fire();
        nextFireTime = Time.time + Mathf.Max(0.01f, fireRate);
    }

    private bool ShouldFire()
    {
        if (enableGamepad)
        {
            Gamepad gamepad = Gamepad.current;
            if (gamepad != null && ShouldFireGamepad(gamepad))
            {
                return true;
            }
        }

        if (enableHotas)
        {
            Joystick joystick = Joystick.current;
            if (joystick != null && ShouldFireHotas(joystick))
            {
                return true;
            }
        }

        return false;
    }

    private bool ShouldFireGamepad(Gamepad gamepad)
    {
        if (useRightTrigger && gamepad.rightTrigger.ReadValue() >= gamepadTriggerThreshold) return true;
        if (useLeftTrigger && gamepad.leftTrigger.ReadValue() >= gamepadTriggerThreshold) return true;
        if (useWestButtonFallback && gamepad.buttonWest.isPressed) return true;
        return false;
    }

    private bool ShouldFireHotas(Joystick joystick)
    {
        if (joystick.trigger != null && joystick.trigger.isPressed)
        {
            return true;
        }

        if (hotasFireButtons == null || hotasFireButtons.Length == 0) return false;

        for (int i = 0; i < hotasFireButtons.Length; i++)
        {
            string controlName = hotasFireButtons[i];
            if (string.IsNullOrWhiteSpace(controlName)) continue;

            ButtonControl button = joystick.TryGetChildControl<ButtonControl>(controlName);
            if (button != null && button.ReadValue() >= hotasPressPoint)
            {
                return true;
            }
        }

        return false;
    }

    private void Fire()
    {
        if (autoPopulateWeapons && (guns == null || guns.Count == 0))
        {
            RefreshWeaponList();
        }

        if (guns == null || guns.Count == 0) return;

        for (int i = 0; i < guns.Count; i++)
        {
            GunLogic gun = guns[i];
            if (gun == null) continue;

            if (ShootRaycastMethod != null)
            {
                ShootRaycastMethod.Invoke(gun, null);
            }
            else
            {
                gun.TryFireFromAI();
            }
        }
    }

    private void RefreshWeaponList()
    {
        if (weaponRoot == null)
        {
            PlayerController playerController = FindFirstObjectByType<PlayerController>();
            weaponRoot = playerController != null ? playerController.transform : transform;
        }

        GunLogic[] foundGuns = weaponRoot.GetComponentsInChildren<GunLogic>(true);
        if (foundGuns.Length == 0)
        {
            PlayerController playerController = FindFirstObjectByType<PlayerController>();
            if (playerController != null && playerController.transform != weaponRoot)
            {
                weaponRoot = playerController.transform;
                foundGuns = weaponRoot.GetComponentsInChildren<GunLogic>(true);
            }
        }

        if (foundGuns.Length == 0)
        {
            foundGuns = FindObjectsByType<GunLogic>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }

        guns.Clear();

        for (int i = 0; i < foundGuns.Length; i++)
        {
            GunLogic foundGun = foundGuns[i];
            if (foundGun != null)
            {
                guns.Add(foundGun);
            }
        }
    }
}
