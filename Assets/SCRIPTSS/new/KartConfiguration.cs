// Assets/Scripts/Data/KartConfiguration.cs
using UnityEngine;

[CreateAssetMenu(fileName = "NewKartConfiguration", menuName = "Kart/Configuration")]
public class KartConfiguration : ScriptableObject
{
    [Header("Общие настройки")]
    [Range(300f, 1000f)] public float mass = 500f;
    [Range(0.5f, 1.5f)] public float frictionCoefficient = 1f;
    [Range(0.2f, 0.8f)] public float frontWeightShare = 0.4f;

    [Header("Шины")]
    [Range(5000f, 20000f)] public float frontLateralStiffness = 15000f;
    [Range(5000f, 20000f)] public float rearLateralStiffness = 12000f;
    [Range(0.01f, 0.1f)] public float rollingResistance = 0.03f;
    [Range(0.2f, 0.5f)] public float wheelRadius = 0.3f;
    [Range(10f, 45f)] public float maxSteerAngle = 30f;

    [Header("Двигатель")]
    public AnimationCurve engineTorqueCurve = AnimationCurve.Linear(0, 100, 1, 150);
    [Range(0.1f, 2f)] public float engineInertia = 0.5f;
    [Range(3000f, 10000f)] public float maxRpm = 8000f;
    [Range(3f, 10f)] public float gearRatio = 6f;

    [Header("Аэродинамика (опционально)")]
    [Range(0f, 2f)] public float downforceCoefficient = 0.5f;
}