// KartController.cs
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class KartController : MonoBehaviour
{
    [Header("Конфигурация")]
    public KartConfig config;

    [Header("Колеса")]
    public Transform frontLeftWheel;
    public Transform frontRightWheel;
    public Transform rearLeftWheel;
    public Transform rearRightWheel;

    [Header("Входные данные")]
    public float steerInput = 0f; // От -1 до 1
    public float throttleInput = 0f; // От 0 до 1
    public bool handbrake = false;

    // Внутренние переменные
    private Rigidbody rb;
    private float currentRpm = 0f;
    private float currentEngineTorque = 0f;
    private float[] wheelSlip = new float[4]; // Slip Ratio для каждого колеса
    private Vector3[] wheelVelocity = new Vector3[4]; // Скорость каждого колеса
    private Vector3[] wheelPosition = new Vector3[4]; // Позиция точек контакта

    // Для расчета нормальных сил
    private float frontWeightShare = 0.4f; // Можно сделать полем в Inspector или взять из config
    private float rearWeightShare = 0.6f;


    [Header("Input System")]
    public InputActionReference steerAction;
    public InputActionReference throttleAction;
    public InputActionReference handbrakeAction;

    void Update()
    {
        if (steerAction != null) steerInput = steerAction.action.ReadValue<float>();
        if (throttleAction != null) throttleInput = throttleAction.action.ReadValue<float>();
        if (handbrakeAction != null) handbrake = handbrakeAction.action.IsPressed();
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("На объекте нет Rigidbody!");
            return;
        }

        // Инициализация позиций колес
        wheelPosition[0] = frontLeftWheel.position;
        wheelPosition[1] = frontRightWheel.position;
        wheelPosition[2] = rearLeftWheel.position;
        wheelPosition[3] = rearRightWheel.position;
    }

    [System.Obsolete]
    void FixedUpdate()
    {
        if (config == null) return;

        CalculateRPM();
        CalculateEngineTorque();
        ApplyForcesToWheels();
        ApplySteering();
        ApplyHandbrake();

        // Обновляем телеметрию (будет реализовано позже)
        UpdateTelemetry();
    }

    [System.Obsolete]
    void CalculateRPM()
    {
        // Рассчитываем угловую скорость колеса (в радианах в секунду)
        float wheelAngularVelocity = 0f;

        // Для простоты используем среднюю скорость задних колес
        Vector3 rearLeftVelocity = rb.GetPointVelocity(rearLeftWheel.position);
        Vector3 rearRightVelocity = rb.GetPointVelocity(rearRightWheel.position);

        // Проектируем скорость на направление движения колеса
        float rearLeftSpeed = Vector3.Dot(rearLeftVelocity, transform.forward);
        float rearRightSpeed = Vector3.Dot(rearRightVelocity, transform.forward);

        // Средняя скорость задних колес
        float averageRearSpeed = (rearLeftSpeed + rearRightSpeed) * 0.5f;

        // Угловая скорость колеса: ω = v / r
        wheelAngularVelocity = averageRearSpeed / config.wheelRadius;

        // Переводим в RPM: rpm = ω * (60 / 2π)
        currentRpm = wheelAngularVelocity * (60f / (2f * Mathf.PI)) * config.gearRatio;

        // Ограничитель оборотов
        if (currentRpm > config.maxRpm)
        {
            currentRpm = config.maxRpm;
        }
    }

    void CalculateEngineTorque()
    {
        // Получаем момент по кривой
        currentEngineTorque = config.engineTorqueCurve.Evaluate(currentRpm);

        // Если обороты выше максимума, момент падает до нуля
        if (currentRpm >= config.maxRpm)
        {
            currentEngineTorque *= Mathf.Clamp01((config.maxRpm + 1000f - currentRpm) / 1000f); // Плавный спад
        }
    }

    [System.Obsolete]
    void ApplyForcesToWheels()
    {
        // Расчет нормальных сил
        float totalWeight = config.mass * Physics.gravity.magnitude;
        float frontNormalForce = totalWeight * frontWeightShare;
        float rearNormalForce = totalWeight * rearWeightShare;

        // Делаем предположение, что вес распределяется поровну между левым и правым колесом на каждой оси
        float frontLeftNormal = frontNormalForce * 0.5f;
        float frontRightNormal = frontNormalForce * 0.5f;
        float rearLeftNormal = rearNormalForce * 0.5f;
        float rearRightNormal = rearNormalForce * 0.5f;

        // Применяем силы трения (упрощенная модель)
        Vector3 forwardDirection = transform.forward;
        Vector3 rightDirection = transform.right;

        for (int i = 0; i < 4; i++)
        {
            float normalForce = 0f;
            float lateralStiffness = 0f;
            Transform wheelTransform = null;

            switch (i)
            {
                case 0: normalForce = frontLeftNormal; lateralStiffness = config.frontLateralStiffness; wheelTransform = frontLeftWheel; break;
                case 1: normalForce = frontRightNormal; lateralStiffness = config.frontLateralStiffness; wheelTransform = frontRightWheel; break;
                case 2: normalForce = rearLeftNormal; lateralStiffness = config.rearLateralStiffness; wheelTransform = rearLeftWheel; break;
                case 3: normalForce = rearRightNormal; lateralStiffness = config.rearLateralStiffness; wheelTransform = rearRightWheel; break;
            }

            // Упрощенная боковая сила: Fy = -Cα * v_lat
            // v_lat - боковая скорость колеса относительно его направления
            Vector3 wheelVelocityLocal = transform.InverseTransformVector(rb.GetPointVelocity(wheelTransform.position));
            float vLat = Vector3.Dot(wheelVelocityLocal, rightDirection); // Боковая составляющая

            // Применяем фрикционный круг
            float maxFrictionForce = config.frictionCoefficient * normalForce;
            float fy = -lateralStiffness * vLat;

            // Продольная сила (Fx)
            float fx = 0f;

            if (i >= 2) // Задняя ось
            {
                // Применяем крутящий момент двигателя к задним колесам
                float torquePerWheel = currentEngineTorque / 2f; // Предполагаем, что момент делится поровну
                fx = torquePerWheel / config.wheelRadius; // Fx = Torque / Radius

                // Учитываем сопротивление качению
                fx -= config.rollingResistance;
            }

            // Ограничение по фрикционному кругу
            float forceMagnitude = Mathf.Sqrt(fx * fx + fy * fy);
            if (forceMagnitude > maxFrictionForce)
            {
                float ratio = maxFrictionForce / forceMagnitude;
                fx *= ratio;
                fy *= ratio;
            }

            // Применяем силу к центру масс
            Vector3 forceWorld = transform.TransformVector(new Vector3(fx, 0f, fy));
            rb.AddForceAtPosition(forceWorld, wheelTransform.position);

            // Расчет Slip Ratio (упрощенный)
            wheelSlip[i] = (fx > 0f) ? (rb.velocity.magnitude - (currentRpm * 2f * Mathf.PI / 60f) * config.wheelRadius) / (rb.velocity.magnitude + 0.01f) : 0f;
        }
    }

    void ApplySteering()
    {
        // Поворот передних колес
        float steerAngle = steerInput * config.maxSteerAngle;
        frontLeftWheel.localRotation = Quaternion.Euler(0f, steerAngle, 0f);
        frontRightWheel.localRotation = Quaternion.Euler(0f, steerAngle, 0f);
    }

    void ApplyHandbrake()
{
    if (handbrake)
    {
        // Устанавливаем Cα = 0 для задних колес
        // Это означает, что боковая сила для задних колес = 0
        // Также можно увеличить сопротивление качению
        // Мы можем просто не рассчитывать Fy для задних колес, когда ручник нажат
        // Это реализуется в ApplyForcesToWheels, где мы проверяем handbrake
    }
}

    void UpdateTelemetry()
    {
        // Этот метод будет вызываться из UI-скрипта
        // Здесь мы просто храним данные
    }

    // Методы для получения данных телеметрии
    [System.Obsolete]
    public float GetSpeedMS() => rb.velocity.magnitude;
    [System.Obsolete]
    public float GetSpeedKMH() => rb.velocity.magnitude * 3.6f;
    public float GetCurrentRPM() => currentRpm;
    public float GetCurrentEngineTorque() => currentEngineTorque;
    public float GetTotalFxRearAxle()
    {
        float total = 0f;
        for (int i = 2; i < 4; i++) // Задняя ось
        {
            // Здесь нужно хранить или пересчитывать Fx для задних колес
            // Для упрощения вернем текущий момент, преобразованный в силу
            total += currentEngineTorque / config.wheelRadius;
        }
        return total;
    }
    public float GetTotalFyFrontAxle()
    {
        float total = 0f;
        for (int i = 0; i < 2; i++) // Передняя ось
        {
            // Аналогично, нужно хранить Fy
            // Вернем грубое приближение
            Vector3 wheelVelocityLocal = transform.InverseTransformVector(rb.GetPointVelocity(wheelPosition[i]));
            float vLat = Vector3.Dot(wheelVelocityLocal, transform.right);
            float lateralStiffness = config.frontLateralStiffness;
            total += -lateralStiffness * vLat;
        }
        return total;
    }
    public float[] GetWheelSlip() => wheelSlip;
}