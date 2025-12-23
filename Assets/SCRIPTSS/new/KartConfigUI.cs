// Assets/Scripts/UI/KartConfigUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class KartConfigUI : MonoBehaviour
{
    [Header("Ссылки")]
    public KartPhysics kartPhysics;
    public KartConfiguration currentConfig;

    [Header("UI элементы")]
    public TMP_InputField massInput;
    public Slider frictionSlider;
    public TMP_Text frictionValue;
    public Slider frontStiffnessSlider;
    public TMP_Text frontStiffnessValue;
    public Slider rearStiffnessSlider;
    public TMP_Text rearStiffnessValue;
    public Slider steerAngleSlider;
    public TMP_Text steerAngleValue;
    public Slider maxRpmSlider;
    public TMP_Text maxRpmValue;

    [Header("Панель")]
    public GameObject configPanel;
    public Button applyButton;
    public Button resetButton;

    void Start()
    {
        if (kartPhysics != null && kartPhysics.config != null)
        {
            currentConfig = kartPhysics.config;
            LoadConfigToUI();
        }

        // Подписка на события
        if (applyButton != null)
            applyButton.onClick.AddListener(ApplyConfig);

        if (resetButton != null)
            resetButton.onClick.AddListener(ResetConfig);

        // Скрываем панель по умолчанию
        if (configPanel != null)
            configPanel.SetActive(false);
    }

    void Update()
    {
        // Открытие/закрытие панели по клавише
        if (Input.GetKeyDown(KeyCode.F1))
        {
            if (configPanel != null)
                configPanel.SetActive(!configPanel.activeSelf);
        }

        // Обновление значений на слайдерах
        UpdateSliderValues();
    }

    void LoadConfigToUI()
    {
        if (currentConfig == null) return;

        if (massInput != null)
            massInput.text = currentConfig.mass.ToString("F0");

        if (frictionSlider != null)
            frictionSlider.value = currentConfig.frictionCoefficient;

        if (frontStiffnessSlider != null)
            frontStiffnessSlider.value = currentConfig.frontLateralStiffness;

        if (rearStiffnessSlider != null)
            rearStiffnessSlider.value = currentConfig.rearLateralStiffness;

        if (steerAngleSlider != null)
            steerAngleSlider.value = currentConfig.maxSteerAngle;

        if (maxRpmSlider != null)
            maxRpmSlider.value = currentConfig.maxRpm;
    }

    void UpdateSliderValues()
    {
        if (frictionValue != null && frictionSlider != null)
            frictionValue.text = frictionSlider.value.ToString("F2");

        if (frontStiffnessValue != null && frontStiffnessSlider != null)
            frontStiffnessValue.text = frontStiffnessSlider.value.ToString("F0");

        if (rearStiffnessValue != null && rearStiffnessSlider != null)
            rearStiffnessValue.text = rearStiffnessSlider.value.ToString("F0");

        if (steerAngleValue != null && steerAngleSlider != null)
            steerAngleValue.text = steerAngleSlider.value.ToString("F0");

        if (maxRpmValue != null && maxRpmSlider != null)
            maxRpmValue.text = maxRpmSlider.value.ToString("F0");
    }

    void ApplyConfig()
    {
        if (currentConfig == null || kartPhysics == null) return;

        // Применение значений из UI
        if (massInput != null)
            currentConfig.mass = float.Parse(massInput.text);

        if (frictionSlider != null)
            currentConfig.frictionCoefficient = frictionSlider.value;

        if (frontStiffnessSlider != null)
            currentConfig.frontLateralStiffness = frontStiffnessSlider.value;

        if (rearStiffnessSlider != null)
            currentConfig.rearLateralStiffness = rearStiffnessSlider.value;

        if (steerAngleSlider != null)
            currentConfig.maxSteerAngle = steerAngleSlider.value;

        if (maxRpmSlider != null)
            currentConfig.maxRpm = maxRpmSlider.value;

        // Обновление физики
        kartPhysics.config = currentConfig;

        Debug.Log("Конфигурация применена!");
    }

    void ResetConfig()
    {
        // Создание новой конфигурации с значениями по умолчанию
        currentConfig = ScriptableObject.CreateInstance<KartConfiguration>();
        LoadConfigToUI();
    }
}