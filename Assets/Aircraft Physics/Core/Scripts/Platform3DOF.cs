using UnityEngine;

public class Platform3DOF : MonoBehaviour
{
    public enum HeaveSource { VerticalVelocity, VerticalAcceleration, Altitude }

    [Header("Platform Response")]
    [SerializeField] float maxPitchAngle = 25f;
    [SerializeField] float maxRollAngle = 25f;
    [SerializeField] float maxHeaveDistance = 0.6f;
    [SerializeField] HeaveSource heaveSource = HeaveSource.VerticalAcceleration;
    [SerializeField] float responseSmoothing = 4f;
    [SerializeField] float heaveWashoutRate = 0.3f;

    [Header("Raw Aircraft State")]
    public float rawPitch;
    public float rawRoll;
    public float rawHeave;

    [Header("Target Platform State (-1 to 1)")]
    public float targetPitch;
    public float targetRoll;
    public float targetHeave;

    [Header("Current Smoothed State (-1 to 1)")]
    public float currentPitch;
    public float currentRoll;
    public float currentHeave;

    [Header("Immersive Cockpit (camera parent)")]
    public Transform cockpitContainer;

    [Header("3D Platform Visual")]
    public Transform platformVisual;

    [Header("Readout")]
    public float pitchOutputDeg;
    public float rollOutputDeg;
    public float heaveOutputM;

    public bool directCockpitControl = true;

    [SerializeField]private Rigidbody rb;
    private float prevVerticalVelocity;

    public float MaxPitchAngle => maxPitchAngle;
    public float MaxRollAngle => maxRollAngle;
    public float MaxHeaveDistance => maxHeaveDistance;

    void Awake()
    {
       //    rb = GetComponent<Rigidbody>();
        prevVerticalVelocity = rb.linearVelocity.y;
        /*
        if (cockpitContainer == null)
        {
            var go = new GameObject("CockpitContainer");
            cockpitContainer = go.transform;
            cockpitContainer.SetParent(null);
            //cockpitContainer.position = transform.position + Vector3.up * 1.2f;
        }
        */
    }

    void FixedUpdate()
    {
        ExtractRawState();
        ComputeTarget();
        ApplySmoothing();
        if (directCockpitControl) ApplyToCockpit();
        if (platformVisual != null) UpdateVisual();
    }

    void ExtractRawState()
    {
        Vector3 euler = transform.localEulerAngles;
        float p = euler.x; if (p > 180) p -= 360;
        float r = euler.z; if (r > 180) r -= 360;

        rawPitch = Mathf.Clamp(p / maxPitchAngle, -1f, 1f);
        rawRoll = Mathf.Clamp(r / maxRollAngle, -1f, 1f);

        switch (heaveSource)
        {
            case HeaveSource.VerticalVelocity:
                rawHeave = Mathf.Clamp(rb.linearVelocity.y * 0.12f, -1f, 1f);
                break;
            case HeaveSource.VerticalAcceleration:
                float accel = (rb.linearVelocity.y - prevVerticalVelocity) / Time.fixedDeltaTime;
                rawHeave = Mathf.Clamp(accel * 0.015f, -1f, 1f);
                prevVerticalVelocity = rb.linearVelocity.y;
                break;
            case HeaveSource.Altitude:
                rawHeave = Mathf.Clamp((transform.position.y - 10f) / 80f, -1f, 1f);
                break;
        }
    }

    void ComputeTarget()
    {
        targetPitch = rawPitch;
        targetRoll = rawRoll;

        targetHeave = Mathf.Lerp(targetHeave, rawHeave, Time.fixedDeltaTime * 2f);
        targetHeave -= targetHeave * heaveWashoutRate * Time.fixedDeltaTime;
        targetHeave = Mathf.Clamp(targetHeave, -1f, 1f);
    }

    void ApplySmoothing()
    {
        currentPitch = Mathf.Lerp(currentPitch, targetPitch, Time.fixedDeltaTime * responseSmoothing);
        currentRoll = Mathf.Lerp(currentRoll, targetRoll, Time.fixedDeltaTime * responseSmoothing);
        currentHeave = Mathf.Lerp(currentHeave, targetHeave, Time.fixedDeltaTime * responseSmoothing);

        pitchOutputDeg = currentPitch * maxPitchAngle;
        rollOutputDeg = currentRoll * maxRollAngle;
        heaveOutputM = currentHeave * maxHeaveDistance;
    }

    public void ApplyToCockpit()
    {
        if (cockpitContainer == null) return;
        cockpitContainer.position = transform.position + Vector3.up * (1.2f + heaveOutputM);
        cockpitContainer.rotation = Quaternion.Euler(pitchOutputDeg, transform.eulerAngles.y, -rollOutputDeg);
    }

    void UpdateVisual()
    {
        if (platformVisual == null) return;
        platformVisual.localRotation = Quaternion.Euler(pitchOutputDeg, 0, -rollOutputDeg);
        platformVisual.localPosition = new Vector3(0, heaveOutputM, 0);
    }
}
