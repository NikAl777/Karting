// KartConfig.cs
using UnityEngine;

[CreateAssetMenu(fileName = "NewKartConfig", menuName = "Karting/Kart Config")]
public class KartConfig : ScriptableObject
{
    public float mass = 150f; // Масса картинга
    public float frictionCoefficient = 1.0f; // Коэффициент трения
    public float frontLateralStiffness = 10000f; // Жесткость передних шин
    public float rearLateralStiffness = 8000f; // Жесткость задних шин
    public float rollingResistance = 5f; // Сопротивление качению
    public float maxSteerAngle = 30f; // Максимальный угол поворота
    public AnimationCurve engineTorqueCurve; // Кривая момента двигателя
    public float engineInertia = 0.1f; // Инерция маховика
    public float maxRpm = 10000f; // Максимальные обороты
    public float gearRatio = 3.5f; // Передаточное отношение
    public float wheelRadius = 0.3f; // Радиус колеса
    internal float frontWeightShare;
    internal int downforceCoefficient;
}