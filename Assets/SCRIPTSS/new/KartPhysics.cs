// Assets/Scripts/Kart/KartPhysics.cs
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class KartPhysics : MonoBehaviour
{
    [Header("Конфигурация")]
    public KartConfiguration config;

    [Header("Колёса")]
    public Transform frontLeftWheel;
    public Transform frontRightWheel;
    public Transform rearLeftWheel;
    public Transform rearRightWheel;

    [Header("Настройки отладки")]
    public bool showDebugInfo = true;
    public bool drawForceVectors = true;

    // Физические параметры
    private Rigidbody rb;
    private float currentSteerAngle;
    private float currentRpm;
    private float engineTorque;
    private bool handbrakeActive;

    // Силы на колёсах
    private struct WheelForces
    {
        public float normalForce;
        public float fx;
        public float fy;
        public float slipRatio;
        public float vLat;
    }

    private WheelForces fl, fr, rl, rr;

    // Входные данные
    private float throttleInput;
    private float steerInput;
    private float brakeInput;

    // Телеметрия
    public float SpeedMps { get; private set; }
    public float SpeedKph { get; private set; }
    public float CurrentRpm { get => currentRpm; }
    public float CurrentTorque { get => engineTorque; }
    public float TotalFxRear { get => rl.fx + rr.fx; }
    public float TotalFyFront { get => fl.fy + fr.fy; }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = config.mass;
        rb.centerOfMass = new Vector3(0, -0.3f, 0); // Низкий центр тяжести
    }

    void Update()
    {
        // Обработка ввода
        throttleInput = Input.GetAxis("Vertical");
        steerInput = Input.GetAxis("Horizontal");
        handbrakeActive = Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.LeftShift);

        // Поворот колёс
        currentSteerAngle = steerInput * config.maxSteerAngle;

        // Визуальный поворот колёс
        if (frontLeftWheel && frontRightWheel)
        {
            frontLeftWheel.localRotation = Quaternion.Euler(0, currentSteerAngle, 0);
            frontRightWheel.localRotation = Quaternion.Euler(0, currentSteerAngle, 0);
        }
    }

    [System.Obsolete]
    void FixedUpdate()
    {
        // 1. Расчёт нормальных сил
        CalculateNormalForces();

        // 2. Расчёт двигателя
        CalculateEngine();

        // 3. Расчёт сил на колёсах
        CalculateWheelForces();

        // 4. Применение сил
        ApplyForces();

        // 5. Телеметрия
        UpdateTelemetry();
        Debug.Log($"Throttle: {throttleInput}, RPM: {currentRpm}, Torque: {engineTorque}, Velocity: {rb.velocity.magnitude}");
    }

    void CalculateNormalForces()
    {
        float totalWeight = config.mass * 9.81f;
        float frontWeight = totalWeight * config.frontWeightShare;
        float rearWeight = totalWeight * (1 - config.frontWeightShare);

        // Распределение по осям (упрощённо)
        fl.normalForce = frontWeight / 2f;
        fr.normalForce = frontWeight / 2f;
        rl.normalForce = rearWeight / 2f;
        rr.normalForce = rearWeight / 2f;
    }

    [System.Obsolete]
    void CalculateEngine()
    {
        // УПРОЩЕННАЯ РАБОЧАЯ ВЕРСИЯ
        float throttle = Mathf.Max(0, throttleInput); // Только газ, тормоз отдельно

        // 1. Текущая скорость
        SpeedMps = rb.velocity.magnitude;

        // 2. Минимальные RPM когда газ нажат
        float minRpm = 800f; // Холостой ход
        if (throttle > 0.1f)
            minRpm = 1500f;

        // 3. RPM от скорости (реальные)
        float wheelRpm = (SpeedMps / (2 * Mathf.PI * config.wheelRadius)) * 60f;
        float realRpm = wheelRpm * config.gearRatio;

        // 4. Целевые RPM = большее из (минимальные при газе, реальные от скорости)
        float targetRpm = Mathf.Max(minRpm, realRpm);

        // 5. Плавное изменение RPM
        float rpmChangeSpeed = 300f; // Насколько быстро меняются RPM
        if (currentRpm < targetRpm)
            currentRpm += rpmChangeSpeed * Time.fixedDeltaTime;
        else
            currentRpm = Mathf.Lerp(currentRpm, targetRpm, 0.05f);

        // 6. ОГРАНИЧИТЕЛЬ (мягкий!)
        if (currentRpm > config.maxRpm)
        {
            // Мягкое снижение момента, а не обнуление
            float overRevFactor = 1.0f - ((currentRpm - config.maxRpm) / 1000f);
            overRevFactor = Mathf.Clamp01(overRevFactor);

            // Получаем момент из кривой
            float normalizedRpm = Mathf.Clamp01(currentRpm / config.maxRpm);
            float maxTorque = config.engineTorqueCurve.Evaluate(normalizedRpm);
            engineTorque = maxTorque * throttle * config.gearRatio * overRevFactor;

            Debug.Log($"Soft limiter: RPM={currentRpm:F0}, Factor={overRevFactor:F2}");
        }
        else
        {
            // Нормальный расчёт
            float normalizedRpm = currentRpm / config.maxRpm;
            float maxTorque = config.engineTorqueCurve.Evaluate(normalizedRpm);
            engineTorque = maxTorque * throttle * config.gearRatio;
        }

        // 7. ГАРАНТИРУЕМ минимальный момент
        if (throttle > 0.1f && engineTorque < 50f)
            engineTorque = 50f;

        Debug.Log($"ENGINE: RPM={currentRpm:F0}, Target={targetRpm:F0}, Torque={engineTorque:F0}, Throttle={throttle:F2}");
    }

    [System.Obsolete]
    void CalculateWheelForces()
    {
        // Локальная скорость в системе координат картинга
        Vector3 localVelocity = transform.InverseTransformDirection(rb.velocity);

        // Боковая скорость для расчёта угла скольжения
        float vLat = localVelocity.x;

        // Для всех колёс
        CalculateWheelForce(ref fl, vLat, true, false);
        CalculateWheelForce(ref fr, vLat, true, false);
        CalculateWheelForce(ref rl, vLat, false, handbrakeActive);
        CalculateWheelForce(ref rr, vLat, false, handbrakeActive);
    }

    void CalculateWheelForce(ref WheelForces wheel, float vLat, bool isFront, bool isLocked)
    {
        // Максимальная сила трения
        float maxFriction = config.frictionCoefficient * wheel.normalForce;

        // Продольная сила (ускорение/торможение)
        float desiredFx = 0;

        if (!isLocked)
        {
            if (isFront)
            {
                // Передние колёса - только торможение
                desiredFx = -Mathf.Clamp(brakeInput, 0, 1) * maxFriction;
            }
            else
            {
                // Задние колёса - привод
                desiredFx = engineTorque / config.wheelRadius / 2f; // Делим на 2 колеса

                // Торможение двигателем
                if (throttleInput < 0)
                    desiredFx *= throttleInput;
            }
        }

        // Боковая сила
        float desiredFy = 0;

        if (!isLocked)
        {
            float Ca = isFront ? config.frontLateralStiffness : config.rearLateralStiffness;
            desiredFy = -Ca * vLat * 0.001f; // Масштабируем
        }

        // Фрикционный круг
        Vector2 force = new Vector2(desiredFx, desiredFy);
        if (force.magnitude > maxFriction)
        {
            force = force.normalized * maxFriction;
        }

        wheel.fx = force.x;
        wheel.fy = force.y;
        wheel.vLat = vLat;
        wheel.slipRatio = Mathf.Abs(vLat / Mathf.Max(0.1f, SpeedMps));
    }

    void ApplyForces()
    {
        // Суммарная сила в локальных координатах
        Vector3 totalForceLocal = Vector3.zero;
        totalForceLocal.x = fl.fy + fr.fy + rl.fy + rr.fy;
        totalForceLocal.z = fl.fx + fr.fx + rl.fx + rr.fx;

        // Сопротивление качению
        totalForceLocal.z -= config.rollingResistance * rb.mass * 9.81f * Mathf.Sign(SpeedMps);

        // Преобразование в мировые координаты
        Vector3 totalForceWorld = transform.TransformDirection(totalForceLocal);

        // Прижимная сила (аэродинамика)
        if (config.downforceCoefficient > 0)
        {
            float downforce = config.downforceCoefficient * SpeedMps * SpeedMps;
            totalForceWorld.y -= downforce;
        }

        // Применение силы
        rb.AddForce(totalForceWorld);

        // Момент для поворота (упрощённо)
        float turnTorque = (fl.fy - fr.fy) * 0.5f; // Разница в боковых силах
        rb.AddTorque(transform.up * turnTorque);
    }

    [System.Obsolete]
    void UpdateTelemetry()
    {
        SpeedMps = rb.velocity.magnitude;
        SpeedKph = SpeedMps * 3.6f;
    }

    [System.Obsolete]
    void OnDrawGizmos()
    {
        if (!showDebugInfo || !Application.isPlaying) return;

        // Отображение сил
        if (drawForceVectors)
        {
            DrawWheelForce(frontLeftWheel.position, fl.fx, fl.fy);
            DrawWheelForce(frontRightWheel.position, fr.fx, fr.fy);
            DrawWheelForce(rearLeftWheel.position, rl.fx, rl.fy);
            DrawWheelForce(rearRightWheel.position, rr.fx, rr.fy);
        }

        // Отображение скорости
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, rb.velocity.normalized * 2f);
    }

    void DrawWheelForce(Vector3 position, float fx, float fy)
    {
        Vector3 force = new Vector3(fy * 0.01f, 0, fx * 0.01f);
        Gizmos.color = Color.red;
        Gizmos.DrawRay(position, transform.TransformDirection(force));
    }
}