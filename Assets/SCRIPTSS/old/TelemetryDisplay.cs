// TelemetryDisplay.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TelemetryDisplay : MonoBehaviour
{
    public KartController kartController;

    public TMP_Text speedText;
    public TMP_Text rpmText;
    public TMP_Text torqueText;
    public TMP_Text fxRearText;
    public TMP_Text fyFrontText;
    public TMP_Text slipText;
    

    [System.Obsolete]
    void Update()
    {
        if (kartController == null) return;

        speedText.text = $"Speed: {kartController.GetSpeedMS():F2} m/s ({kartController.GetSpeedKMH():F2} km/h)";
        rpmText.text = $"RPM: {kartController.GetCurrentRPM():F0}";
        torqueText.text = $"Torque: {kartController.GetCurrentEngineTorque():F2} N·m";
        fxRearText.text = $"Fx Rear: {kartController.GetTotalFxRearAxle():F2} N";
        fyFrontText.text = $"Fy Front: {kartController.GetTotalFyFrontAxle():F2} N";

        float[] slip = kartController.GetWheelSlip();
        slipText.text = $"Slip: FL:{slip[0]:F2} FR:{slip[1]:F2} RL:{slip[2]:F2} RR:{slip[3]:F2}";
    }
}