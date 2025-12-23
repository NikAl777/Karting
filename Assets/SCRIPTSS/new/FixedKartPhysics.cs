using UnityEngine;

public class FixedKartPhysics : MonoBehaviour
{
    [Header("Колёса (перетащите 4 сферы)")]
    public Transform wheelFL;
    public Transform wheelFR;
    public Transform wheelRL;
    public Transform wheelRR;

    [Header("Двигатель")]
    public float maxSpeed = 50f; // км/ч
    public float acceleration = 8000f;
    public float brakePower = 10000f;

    [Header("Управление")]
    public float maxSteerAngle = 30f;
    public float steerSpeed = 200f; // Скорость поворота
    public float highSpeedSteer = 100f; // Поворот на высокой скорости

    [Header("Физика")]
    public float downForce = 100f; // Прижимная сила

    // Приватные
    private Rigidbody rb;
    private float throttle, steer;
    private bool handbrake;
    private float currentSteerAngle;

    // Телеметрия
    public float SpeedKph { get; private set; }

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Настраиваем центр масс ниже для устойчивости
        rb.centerOfMass = new Vector3(0, -0.5f, 0);

        Debug.Log("Картинг готов к работе!");
    }

    [System.Obsolete]
    void Update()
    {
        // Ввод
        throttle = Input.GetAxis("Vertical");
        steer = Input.GetAxis("Horizontal");
        handbrake = Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.LeftShift);

        // Плавный поворот руля
        float targetSteerAngle = steer * maxSteerAngle;
        currentSteerAngle = Mathf.Lerp(currentSteerAngle, targetSteerAngle, Time.deltaTime * 10f);

        // Визуальный поворот передних колёс
        if (wheelFL) wheelFL.localRotation = Quaternion.Euler(0, currentSteerAngle, 0);
        if (wheelFR) wheelFR.localRotation = Quaternion.Euler(0, currentSteerAngle, 0);

        // Вращение всех колёс
        SpeedKph = rb.velocity.magnitude * 3.6f;
        float wheelRotation = SpeedKph * 10f * Time.deltaTime;
        RotateWheel(wheelFL, wheelRotation);
        RotateWheel(wheelFR, wheelRotation);
        RotateWheel(wheelRL, wheelRotation);
        RotateWheel(wheelRR, wheelRotation);

        // Дебаг
        if (Input.GetKeyDown(KeyCode.W))
            Debug.Log($"W pressed! Throttle: {throttle}, Speed: {SpeedKph:F1} km/h");
    }

    void RotateWheel(Transform wheel, float rotation)
    {
        if (wheel)
            wheel.Rotate(rotation, 0, 0);
    }

    [System.Obsolete]
    void FixedUpdate()
    {
        // 1. ДВИЖЕНИЕ ВПЕРЁД/НАЗАД
        Vector3 moveForce = Vector3.zero;

        if (throttle > 0.1f) // Газ
        {
            // Ограничение максимальной скорости
            if (SpeedKph < maxSpeed)
            {
                moveForce = transform.forward * acceleration * throttle;
            }
        }
        else if (throttle < -0.1f) // Тормоз
        {
            moveForce = transform.forward * brakePower * throttle;
        }

        rb.AddForce(moveForce);

        // 2. ПОВОРОТ (РАБОЧАЯ ВЕРСИЯ)
        if (Mathf.Abs(steer) > 0.1f)
        {
            // Разные коэффициенты для разных скоростей
            float speedFactor = Mathf.Clamp01(SpeedKph / 30f); // 0 на месте, 1 на 30 км/ч
            float currentSteerPower = Mathf.Lerp(steerSpeed, highSpeedSteer, speedFactor);

            // Поворачиваем в зависимости от скорости
            if (SpeedKph > 1f) // Только если движемся
            {
                // Метод 1: AddTorque (физический)
                float turnTorque = steer * currentSteerPower * rb.mass * 0.01f;
                rb.AddTorque(transform.up * turnTorque);

                // Метод 2: Смещение силы (дополнительно)
                // Vector3 turnForce = transform.right * steer * currentSteerPower * SpeedKph * 0.1f;
                // rb.AddForce(turnForce);
            }
            else
            {
                // На месте - просто поворачиваем
                transform.Rotate(0, steer * 100f * Time.fixedDeltaTime, 0, Space.World);
            }
        }

        // 3. РУЧНОЙ ТОРМОЗ (ЗАНОС)
        if (handbrake)
        {
            // Резкое торможение
            rb.AddForce(-rb.velocity.normalized * brakePower * 2f);

            // Уменьшаем сцепление для заноса
            rb.AddTorque(transform.up * steer * 5000f);

            Debug.Log("HANDBRAKE! Дрифт!");
        }

        // 4. ПРИЖИМНАЯ СИЛА (чтобы не переворачиваться)
        rb.AddForce(Vector3.down * downForce * SpeedKph * 0.1f);

        // 5. СОПРОТИВЛЕНИЕ ВОЗДУХА
        if (SpeedKph > 10f)
        {
            Vector3 drag = -rb.velocity.normalized * SpeedKph * 5f;
            rb.AddForce(drag);
        }
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 22;
        style.normal.textColor = Color.cyan;

        int y = 10;
        int lineHeight = 30;

        GUI.Label(new Rect(10, y, 400, lineHeight), $"Скорость: {SpeedKph:F1} км/ч", style); y += lineHeight;
        GUI.Label(new Rect(10, y, 400, lineHeight), $"Газ: {throttle:F2}", style); y += lineHeight;
        GUI.Label(new Rect(10, y, 400, lineHeight), $"Руль: {steer:F2}", style); y += lineHeight;
        GUI.Label(new Rect(10, y, 400, lineHeight), $"Поворот: {currentSteerAngle:F1}°", style); y += lineHeight;
        GUI.Label(new Rect(10, y, 400, lineHeight), $"Тормоз: {(handbrake ? "РУЧНОЙ" : "нет")}", style);

        // Подсказки управления
        GUI.Label(new Rect(Screen.width - 250, 10, 240, 100),
            "Управление:\n" +
            "W/S - Газ/Тормоз\n" +
            "A/D - Поворот\n" +
            "SPACE - Ручной тормоз\n" +
            "R - Сброс");
    }

    
}