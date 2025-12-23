// Модифицированный KartTelemetryUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class KartTelemetryUI : MonoBehaviour
{
    [Header("Ссылки")]
    public KartPhysics kartPhysics;

    [Header("Текстовые поля")]
    public TMP_Text speedText;
    public TMP_Text rpmText;
    public TMP_Text torqueText;
    public TMP_Text fxRearText;
    public TMP_Text fyFrontText;
    public TMP_Text slipText;
    public TMP_Text handbrakeText; // Добавляем отдельное поле

    [Header("Клавиши управления")]
    public KeyCode handbrakeKey = KeyCode.Space;
    public KeyCode altHandbrakeKey = KeyCode.LeftShift;

    void Update()
    {
        if (kartPhysics == null) return;

        // Основная телеметрия
        if (speedText != null)
            speedText.text = $"Скорость: {kartPhysics.SpeedKph:F1} км/ч ({kartPhysics.SpeedMps:F1} м/с)";

        if (rpmText != null)
            rpmText.text = $"RPM: {kartPhysics.CurrentRpm:F0}";

        if (torqueText != null)
            torqueText.text = $"Момент: {kartPhysics.CurrentTorque:F0} Н·м";

        if (fxRearText != null)
            fxRearText.text = $"Fx зад: {kartPhysics.TotalFxRear:F1} Н";

        if (fyFrontText != null)
            fyFrontText.text = $"Fy перед: {kartPhysics.TotalFyFront:F1} Н";

        if (slipText != null)
            slipText.text = $"Занос: {(Mathf.Abs(kartPhysics.TotalFyFront) > 100 ? "ДА" : "нет")}";

        // Отображение состояния ручного тормоза
        bool handbrakeActive = Input.GetKey(handbrakeKey) || Input.GetKey(altHandbrakeKey);
        if (handbrakeText != null)
            handbrakeText.text = $"Тормоз: {(handbrakeActive ? "РУЧНОЙ" : "нет")}";
    }

    // Упрощённый OnGUI без GUILayout (чтобы избежать ошибки)
    void OnGUI()
    {
        if (kartPhysics == null) return;

        // Используем простые GUI элементы вместо GUILayout
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 14;
        labelStyle.normal.textColor = Color.white;

        GUI.Label(new Rect(10, 10, 300, 25), "=== ТЕЛЕМЕТРИЯ КАРТИНГА ===", labelStyle);
        GUI.Label(new Rect(10, 40, 300, 25), $"Скорость: {kartPhysics.SpeedKph:F1} км/ч", labelStyle);
        GUI.Label(new Rect(10, 70, 300, 25), $"RPM: {kartPhysics.CurrentRpm:F0}", labelStyle);
        GUI.Label(new Rect(10, 100, 300, 25), $"Момент: {kartPhysics.CurrentTorque:F0} Н·м", labelStyle);
        GUI.Label(new Rect(10, 130, 300, 25), $"Fx зад: {kartPhysics.TotalFxRear:F1} Н", labelStyle);
        GUI.Label(new Rect(10, 160, 300, 25), $"Fy перед: {kartPhysics.TotalFyFront:F1} Н", labelStyle);

        bool handbrakeActive = Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.LeftShift);
        GUI.Label(new Rect(10, 190, 300, 25), $"Ручной тормоз: {(handbrakeActive ? "ВКЛ" : "выкл")}", labelStyle);
    }
}