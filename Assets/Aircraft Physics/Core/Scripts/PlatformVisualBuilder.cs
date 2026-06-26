using UnityEngine;

[RequireComponent(typeof(Platform3DOF))]
public class PlatformVisualBuilder : MonoBehaviour
{
    [Header("Platform Dimensions")]
    public float baseWidth = 2.5f;
    public float baseLength = 2.0f;
    public float platformWidth = 2.0f;
    public float platformLength = 1.6f;
    public float pistonRadius = 0.08f;
    public float pistonExtendedLength = 0.8f;
    public float baseHeight = 0.15f;
    public float platformHeight = 0.1f;

    [Header("Colors")]
    public Color baseColor = new Color(0.3f, 0.3f, 0.35f);
    public Color platformColor = new Color(0.2f, 0.3f, 0.5f);
    public Color pistonColor = new Color(0.7f, 0.7f, 0.7f);

    private Platform3DOF platform3DOF;
    private Transform root;
    private Transform basePlate;
    private Transform topPlate;
    private Transform[] pistons;
    private Vector3[] pistonBasePositions;

    void Start()
    {
        platform3DOF = GetComponent<Platform3DOF>();
        platform3DOF.directCockpitControl = false;
        BuildPlatform();
    }

    void BuildPlatform()
    {
        var aircraftPos = transform.position;

        root = new GameObject("3DOF_Platform_Root").transform;
        root.SetParent(null);
        root.position = aircraftPos - Vector3.up * 0.5f;
        root.rotation = Quaternion.identity;

        basePlate = CreateBox("Base", root, baseWidth, baseHeight, baseLength, baseColor);
        basePlate.localPosition = Vector3.zero;

        topPlate = CreateBox("TopPlatform", root, platformWidth, platformHeight, platformLength, platformColor);
        topPlate.localPosition = new Vector3(0, pistonExtendedLength + platformHeight * 0.5f, 0);

        float w = platformWidth * 0.35f;
        float l = platformLength * 0.35f;
        pistonBasePositions = new Vector3[]
        {
            new Vector3(0, 0, l),
            new Vector3(-w, 0, -l),
            new Vector3(w, 0, -l)
        };

        pistons = new Transform[3];
        for (int i = 0; i < 3; i++)
        {
            var p = CreatePiston("Piston_" + i, root, pistonBasePositions[i], pistonRadius, pistonExtendedLength, pistonColor);
            pistons[i] = p;
        }

        platform3DOF.platformVisual = topPlate;

        var cc = platform3DOF.cockpitContainer;
        if (cc != null)
        {
            cc.SetParent(topPlate, false);
            cc.localPosition = new Vector3(0, platformHeight * 0.5f, 0);
            cc.localRotation = Quaternion.identity;
        }
    }

    Transform CreateBox(string name, Transform parent, float w, float h, float d, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localScale = new Vector3(w, h, d);
        var mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        go.GetComponent<Renderer>().material = mat;
        return go.transform;
    }

    Transform CreatePiston(string name, Transform parent, Vector3 basePos, float radius, float length, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localScale = new Vector3(radius * 2, length, radius * 2);
        go.transform.localPosition = basePos + Vector3.up * length * 0.5f;

        var mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        go.GetComponent<Renderer>().material = mat;
        return go.transform;
    }

    void LateUpdate()
    {
        if (root == null || platform3DOF == null) return;

        float heave = platform3DOF.heaveOutputM;
        float pitch = platform3DOF.pitchOutputDeg;
        float roll = platform3DOF.rollOutputDeg;

        root.position = transform.position - Vector3.up * 0.5f;

        Vector3 topPos = new Vector3(0, pistonExtendedLength + platformHeight * 0.5f + heave, 0);
        topPlate.localPosition = topPos;
        topPlate.localRotation = Quaternion.Euler(pitch, 0, -roll);

        for (int i = 0; i < pistons.Length && i < pistonBasePositions.Length; i++)
        {
            if (pistons[i] == null) continue;
            Vector3 pWorld = topPlate.TransformPoint(pistonBasePositions[i]);
            Vector3 pLocal = root.InverseTransformPoint(pWorld);
            Vector3 baseLocal = pistonBasePositions[i];
            float dist = Vector3.Distance(baseLocal, pLocal);
            float pistonLen = Mathf.Max(dist, 0.1f);

            pistons[i].localScale = new Vector3(pistonRadius * 2, pistonLen * 0.5f, pistonRadius * 2);
            pistons[i].localPosition = (baseLocal + pLocal) * 0.5f;
            pistons[i].localRotation = Quaternion.FromToRotation(Vector3.up, (pLocal - baseLocal).normalized);
        }
    }

    void OnDestroy()
    {
        if (root != null) Destroy(root.gameObject);
    }
}
