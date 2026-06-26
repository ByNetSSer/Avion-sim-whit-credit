using UnityEngine;

[System.Serializable]
public class MotorServo
{
    public string NameMotor;
    public float ValueMotorRotation;   // Angulo real del brazo, en grados (-45 a 45)
    public int ComandoMotor;           // Valor que se le envia al motor/servo real (0 a 250)
    public Transform Brazo;
}

public class MotoresServo : MonoBehaviour
{
    public Transform vehicle;

    public MotorServo MotorA; // Frente
    public MotorServo MotorB; // Trasero Izquierdo
    public MotorServo MotorC; // Trasero Derecho

    [Header("Configuracion")]
    public float limiteAngulo = 45f; // Rango -45 a 45

    [Header("Rango de comando del motor real")]
    public int comandoMin = 0;
    public int comandoMax = 250;

    [Header("Suavizado (simula velocidad real del servo)")]
    public float velocidadServo = 90f; // grados por segundo que puede girar el servo

    // Guardamos el angulo actual de cada brazo para poder interpolar hacia el objetivo
    private float anguloActualA;
    private float anguloActualB;
    private float anguloActualC;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        // 1) Obtenemos los angulos de Euler del vehiculo, normalizados a -180/180
        float anguloX = NormalizarAngulo(vehicle.eulerAngles.x);
        float anguloZ = NormalizarAngulo(vehicle.eulerAngles.z);

        // 2) Calculamos el angulo objetivo de cada motor (-45 a 45), igual que antes
        float anguloObjetivoA = Mathf.Clamp(anguloX, -limiteAngulo, limiteAngulo);
        float anguloObjetivoB = Mathf.Clamp(anguloZ, -limiteAngulo, limiteAngulo);
        float anguloObjetivoC = Mathf.Clamp(-anguloZ, -limiteAngulo, limiteAngulo);

        // 3) Convertimos ese angulo objetivo a un comando de motor (0 a 250)
        //    Este es el valor que en el mundo real se le enviaria al servo por PWM/serial
        MotorA.ComandoMotor = AnguloAComando(anguloObjetivoA);
        MotorB.ComandoMotor = AnguloAComando(anguloObjetivoB);
        MotorC.ComandoMotor = AnguloAComando(anguloObjetivoC);

        // 4) El motor "interpreta" su comando y lo vuelve a convertir a un angulo real (-45 a 45)
        //    Este paso simula lo que haria el firmware del servo al recibir el comando
        float anguloDesdeComandoA = ComandoAAngulo(MotorA.ComandoMotor);
        float anguloDesdeComandoB = ComandoAAngulo(MotorB.ComandoMotor);
        float anguloDesdeComandoC = ComandoAAngulo(MotorC.ComandoMotor);

        // 5) Avanzamos el angulo actual del brazo hacia ese angulo a velocidad fija (grados/segundo)
        anguloActualA = Mathf.MoveTowards(anguloActualA, anguloDesdeComandoA, velocidadServo * Time.deltaTime);
        anguloActualB = Mathf.MoveTowards(anguloActualB, anguloDesdeComandoB, velocidadServo * Time.deltaTime);
        anguloActualC = Mathf.MoveTowards(anguloActualC, anguloDesdeComandoC, velocidadServo * Time.deltaTime);

        // Guardamos el angulo final (ya suavizado) en ValueMotorRotation, por si se quiere leer/depurar
        MotorA.ValueMotorRotation = anguloActualA;
        MotorB.ValueMotorRotation = anguloActualB;
        MotorC.ValueMotorRotation = anguloActualC;

        // 6) Aplicamos la rotacion local en Z a cada Brazo
        AplicarRotacionBrazo(MotorA, anguloActualA);
        AplicarRotacionBrazo(MotorB, anguloActualB);
        AplicarRotacionBrazo(MotorC, anguloActualC);
    }

    // Convierte un angulo en grados (-limiteAngulo a +limiteAngulo) a un comando de motor (comandoMin a comandoMax)
    // Ejemplo con limites -45/45 y comando 0/250: -45 -> 0, 0 -> 125, 45 -> 250
    int AnguloAComando(float angulo)
    {
        float t = Mathf.InverseLerp(-limiteAngulo, limiteAngulo, angulo); // t va de 0 a 1
        float comando = Mathf.Lerp(comandoMin, comandoMax, t);
        return Mathf.RoundToInt(comando);
    }

    // Convierte un comando de motor (comandoMin a comandoMax) de vuelta a un angulo en grados
    // Ejemplo con limites -45/45 y comando 0/250: 0 -> -45, 125 -> 0, 250 -> 45
    float ComandoAAngulo(int comando)
    {
        float t = Mathf.InverseLerp(comandoMin, comandoMax, comando); // t va de 0 a 1
        return Mathf.Lerp(-limiteAngulo, limiteAngulo, t);
    }

    void AplicarRotacionBrazo(MotorServo motor, float anguloSuavizado)
    {
        if (motor.Brazo == null) return;

        Vector3 rotacionLocal = motor.Brazo.localEulerAngles;

        motor.Brazo.localRotation = Quaternion.Euler(
            rotacionLocal.x,
            rotacionLocal.y,
            anguloSuavizado
        );
    }

    // Convierte un angulo 0-360 a su equivalente -180/180
    float NormalizarAngulo(float angulo)
    {
        angulo %= 360f;
        if (angulo > 180f) angulo -= 360f;
        return angulo;
    }

    // ---------------------------------------------------------------
    // GIZMOS: dibuja en la escena, solo visible en el Editor (no en build)
    // ---------------------------------------------------------------
    void OnDrawGizmos()
    {
        DibujarGizmoMotor(MotorA, Color.red);
        DibujarGizmoMotor(MotorB, Color.green);
        DibujarGizmoMotor(MotorC, Color.blue);
    }

    void DibujarGizmoMotor(MotorServo motor, Color color)
    {
        if (motor.Brazo == null) return;

        Gizmos.color = color;

        // 1) Esfera en la posicion del motor (origen del Brazo)
        Gizmos.DrawSphere(motor.Brazo.position, 0.03f);

        // 2) Linea mostrando la direccion actual del Brazo (su eje hacia adelante local)
        Vector3 direccion = motor.Brazo.up * 0.2f; // usamos "up" porque el Brazo rota en Z, su eje Y local apunta hacia donde "sube/baja"
        Gizmos.DrawLine(motor.Brazo.position, motor.Brazo.position + direccion);

        // Pequeña punta de flecha para distinguir la direccion
        Gizmos.DrawSphere(motor.Brazo.position + direccion, 0.015f);

#if UNITY_EDITOR
        // 3) Texto flotante con nombre, angulo actual y comando (0-250)
        string texto = $"{motor.NameMotor}\nAng: {motor.ValueMotorRotation:F1}°\nCmd: {motor.ComandoMotor}";
        UnityEditor.Handles.color = color;
        UnityEditor.Handles.Label(motor.Brazo.position + direccion + Vector3.up * 0.05f, texto);
#endif
    }
}