using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ShopDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ShopManager shopManager;

    [Header("Top Bar")]
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private string shopTitle = "Welcome To Dave's Plane Shop";
    [SerializeField] private string mainMenuSceneName = "Rhu_MainMenu";
    [SerializeField] private Button backButton;
    [SerializeField] private UnityEvent onBackPressed;

    [Header("Left Stats Panel")]
    [SerializeField] private TextMeshProUGUI planeNameText;
    [SerializeField] private TextMeshProUGUI gunTypeText;
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button actionButton;
    [SerializeField] private TextMeshProUGUI actionButtonText;

    [Header("Center Preview")]
    [SerializeField] private Transform centerDisplayParent;

    [Header("Right Scrollable Plane List")]
    [SerializeField] private Transform planeListContainer;
    [SerializeField] private Button planeListItemPrefab;
    [SerializeField] private bool includeGunTypeInList = true;
    [SerializeField] private Color selectedListItemColor = new Color(0.25f, 0.6f, 1f, 1f);
    [SerializeField] private Color defaultListItemColor = Color.white;

    private readonly List<Button> listButtons = new List<Button>();
    private readonly List<TextMeshProUGUI> listLabels = new List<TextMeshProUGUI>();
    private bool pendingRefreshFromValidate;

    private void Awake()
    {
        if (actionButton != null)
        {
            actionButton.onClick.AddListener(OnActionButtonPressed);
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackButtonPressed);
        }
    }

    private void OnEnable()
    {
        if (shopManager != null)
        {
            shopManager.ReloadMoneyFromPrefs();
            shopManager.OnShopChanged += Refresh;
        }

        BuildPlaneList();
        Refresh();
    }

    private void OnDisable()
    {
        if (shopManager != null)
        {
            shopManager.OnShopChanged -= Refresh;
        }
    }

    private void OnDestroy()
    {
        if (actionButton != null)
        {
            actionButton.onClick.RemoveListener(OnActionButtonPressed);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(OnBackButtonPressed);
        }
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        pendingRefreshFromValidate = true;
    }

    private void Update()
    {
        if (!pendingRefreshFromValidate)
        {
            return;
        }

        pendingRefreshFromValidate = false;
        Refresh();
    }

    private void BuildPlaneList()
    {
        ClearSpawnedPlaneListItems();

        if (shopManager == null || planeListContainer == null || planeListItemPrefab == null)
        {
            return;
        }

        IReadOnlyList<PlaneData> planes = shopManager.Planes;
        for (int i = 0; i < planes.Count; i++)
        {
            PlaneData plane = planes[i];
            if (plane == null)
            {
                continue;
            }

            Button button = Instantiate(planeListItemPrefab, planeListContainer);
            int capturedIndex = i;
            button.onClick.AddListener(() => shopManager.SelectPlane(capturedIndex));

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                string listLabel = includeGunTypeInList
                    ? plane.PlaneName + "\n" + plane.GunType
                    : plane.PlaneName;
                label.text = listLabel;
            }

            listButtons.Add(button);
            listLabels.Add(label);
        }
    }

    private void ClearSpawnedPlaneListItems()
    {
        for (int i = 0; i < listButtons.Count; i++)
        {
            if (listButtons[i] == null)
            {
                continue;
            }

            Destroy(listButtons[i].gameObject);
        }

        listButtons.Clear();
        listLabels.Clear();
    }

    private void Refresh()
    {
        if (shopManager == null)
        {
            Debug.LogWarning("[ShopDisplay] ShopManager is null in ShopDisplay!");
            return;
        }

        if (titleText != null)
        {
            titleText.text = shopTitle;
        }

        if (moneyText != null)
        {
            // Always update from PlayerPrefs to ensure latest value
            int money = ShopPersistence.LoadMoney();            
            moneyText.text = "$" + money;
        }
        else
        {
         
        }

        PlaneData selected = shopManager.GetSelectedPlane();
        RefreshStatsPanel(selected);
        RefreshActionButton(selected);
        RefreshPreview(selected);
        RefreshPlaneListVisuals();
    }

    private void RefreshStatsPanel(PlaneData selected)
    {
        if (planeNameText != null)
        {
            planeNameText.text = selected != null ? "Plane: " + selected.PlaneName : "Plane: -";
        }

        if (gunTypeText != null)
        {
            gunTypeText.text = selected != null ? "Gun Type: " + FormatGunType(selected.GunType) : "Gun Type: -";
        }

        if (speedText != null)
        {
            speedText.text = selected != null ? "Speed: " + selected.Speed + " MPH" : "Speed: -";
        }

        if (healthText != null)
        {
            healthText.text = selected != null ? "Health: " + selected.Health + " HP" : "Health: -";
        }

        if (damageText != null)
        {
            damageText.text = selected != null ? "Damage: " + selected.Damage + " DMG" : "Damage: -";
        }

        if (priceText != null)
        {
            priceText.text = selected != null ? "Price: $" + selected.Price : "Price: -";
        }

        if (statusText != null)
        {
            statusText.text = selected != null ? "Status: " + selected.State.ToString().ToUpperInvariant() : "Status: -";
        }
    }

    private string FormatGunType(GunType gunType)
    {
        switch (gunType)
        {
            case GunType.MachineGun:
                return "Machine Gun";
            case GunType.BurstGun:
                return "Burst Gun";
            case GunType.HeavyCannon:
                return "Heavy Cannon";
            case GunType.RailCannon:
                return "Rail Cannon";
            case GunType.LightGun:
                return "Light Gun";
            case GunType.Missiles:
                return "Missiles";
            default:
                return gunType.ToString();
        }
    }

    private void RefreshActionButton(PlaneData selected)
    {
        if (actionButtonText != null)
        {
            actionButtonText.text = shopManager.GetActionText(selected);
        }

        if (actionButton != null)
        {
            actionButton.interactable = shopManager.IsActionAvailable(selected);
        }
    }

    private void RefreshPreview(PlaneData selected)
    {
        if (shopManager == null || centerDisplayParent == null)
        {
            return;
        }

        // Destroy the previously displayed prefab
        for (int i = centerDisplayParent.childCount - 1; i >= 0; i--)
        {
            Destroy(centerDisplayParent.GetChild(i).gameObject);
        }

        // Instantiate the new plane prefab
        if (selected != null && selected.PlanePrefab != null)
        {
            GameObject instance = Instantiate(selected.PlanePrefab, centerDisplayParent, false);

            foreach (Transform child in instance.GetComponentsInChildren<Transform>(true))
            {
                if (child.CompareTag("Gun"))
                {
                    child.gameObject.SetActive(false);
                }
            }
        }
    }

    private void RefreshPlaneListVisuals()
    {
        int selectedIndex = shopManager != null ? shopManager.SelectedPlaneIndex : -1;

        for (int i = 0; i < listButtons.Count; i++)
        {
            Button button = listButtons[i];
            if (button == null)
            {
                continue;
            }

            ColorBlock colors = button.colors;
            Color target = i == selectedIndex ? selectedListItemColor : defaultListItemColor;
            colors.normalColor = target;
            colors.selectedColor = target;
            button.colors = colors;

            if (i < listLabels.Count && listLabels[i] != null && shopManager != null && i < shopManager.Planes.Count)
            {
                PlaneData plane = shopManager.Planes[i];
                if (plane != null)
                {
                    string label = includeGunTypeInList
                        ? plane.PlaneName + "\n" + plane.GunType
                        : plane.PlaneName;

                    if (plane.State == PlaneState.Locked)
                    {
                        label += "\nLOCKED";
                    }

                    if (plane.State == PlaneState.Equipped)
                    {
                        label += "\nEQUIPPED";
                    }

                    listLabels[i].text = label;
                }
            }
        }
    }

    private void OnActionButtonPressed()
    {
        if (shopManager == null)
        {
            return;
        }

        shopManager.HandleSelectedPlaneAction();
    }

    private void OnBackButtonPressed()
    {
        if (!string.IsNullOrWhiteSpace(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
            return;
        }

        onBackPressed?.Invoke();
    }
}
