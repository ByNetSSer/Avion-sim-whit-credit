using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PlatformPanel : MonoBehaviour
{
    public Platform3DOF platform;
    public AirplaneController controller;

    private Text infoText;
    private Transform horizonContainer;
    private RectTransform horizonRect;
    private RectTransform skyRect;
    private RectTransform groundRect;
    private List<PitchMark> pitchMarks;

    struct PitchMark
    {
        public Text label;
        public int angle;
    }

    void Start()
    {
        CreateCanvas();
    }

    void CreateCanvas()
    {
        var canvasGO = new GameObject("PlatformCanvas");
        canvasGO.layer = 5;
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.AddComponent<GraphicRaycaster>();

        var bg = new GameObject("PanelBG", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(canvasGO.transform, false);
        var bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0, 0.5f);
        bgRect.anchorMax = new Vector2(0, 1);
        bgRect.pivot = new Vector2(0, 1);
        bgRect.sizeDelta = new Vector2(280, 0);
        bgRect.offsetMin = new Vector2(10, 10);
        bgRect.offsetMax = new Vector2(-10, -10);
        bg.GetComponent<Image>().color = new Color(0, 0, 0, 0.75f);

        CreateHorizon(bg.transform);
        CreateInfoText(bg.transform);
    }

    void CreateHorizon(Transform parent)
    {
        horizonContainer = new GameObject("Horizon", typeof(RectTransform)).transform;
        horizonContainer.SetParent(parent, false);
        horizonRect = horizonContainer.GetComponent<RectTransform>();
        horizonRect.anchorMin = new Vector2(0, 0);
        horizonRect.anchorMax = new Vector2(1, 0);
        horizonRect.pivot = new Vector2(0.5f, 0);
        horizonRect.sizeDelta = new Vector2(0, 130);
        horizonRect.anchoredPosition = new Vector2(0, 5);

        var bg = new GameObject("H_BG", typeof(RawImage)).GetComponent<RectTransform>();
        bg.SetParent(horizonContainer, false);
        bg.anchorMin = Vector2.zero;
        bg.anchorMax = Vector2.one;
        bg.offsetMin = Vector2.zero;
        bg.offsetMax = Vector2.zero;
        bg.GetComponent<RawImage>().color = new Color(0.08f, 0.08f, 0.12f, 1);

        var sky = new GameObject("Sky", typeof(RawImage)).GetComponent<RectTransform>();
        sky.SetParent(horizonContainer, false);
        sky.GetComponent<RawImage>().color = new Color(0.2f, 0.5f, 0.8f, 1);
        sky.anchorMin = new Vector2(0, 0.5f);
        sky.anchorMax = Vector2.one;
        sky.pivot = new Vector2(0.5f, 0.5f);
        skyRect = sky;

        var ground = new GameObject("Ground", typeof(RawImage)).GetComponent<RectTransform>();
        ground.SetParent(horizonContainer, false);
        ground.GetComponent<RawImage>().color = new Color(0.5f, 0.3f, 0.1f, 1);
        ground.anchorMin = Vector2.zero;
        ground.anchorMax = new Vector2(1, 0.5f);
        ground.pivot = new Vector2(0.5f, 0.5f);
        groundRect = ground;

        var cl = new GameObject("CenterLine", typeof(RawImage)).GetComponent<RectTransform>();
        cl.SetParent(horizonContainer, false);
        cl.GetComponent<RawImage>().color = Color.yellow;
        cl.sizeDelta = new Vector2(60, 2);
        cl.anchorMin = new Vector2(0.5f, 0.5f);
        cl.anchorMax = new Vector2(0.5f, 0.5f);
        cl.pivot = new Vector2(0.5f, 0.5f);
        cl.anchoredPosition = Vector2.zero;

        pitchMarks = new List<PitchMark>();
        for (int i = 0; i < 9; i++)
        {
            int angle = (i - 4) * 10;
            var mark = new GameObject("PM_" + angle, typeof(RectTransform), typeof(Text)).GetComponent<RectTransform>();
            mark.SetParent(horizonContainer, false);
            mark.anchorMin = new Vector2(0.5f, 0);
            mark.anchorMax = new Vector2(0.5f, 0);
            mark.pivot = new Vector2(0.5f, 0.5f);
            mark.sizeDelta = new Vector2(80, 14);

            var txt = mark.GetComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 10;
            txt.color = new Color(1, 1, 1, 0.6f);
            txt.alignment = TextAnchor.MiddleCenter;
            txt.text = angle == 0 ? "\u2014" : angle.ToString();

            pitchMarks.Add(new PitchMark { label = txt, angle = angle });
        }
    }

    void CreateInfoText(Transform parent)
    {
        var textGO = new GameObject("InfoText", typeof(RectTransform), typeof(Text));
        textGO.transform.SetParent(parent, false);
        var textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(5, 140);
        textRect.offsetMax = new Vector2(-5, 0);

        infoText = textGO.GetComponent<Text>();
        infoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        infoText.fontSize = 12;
        infoText.color = Color.white;
        infoText.alignment = TextAnchor.UpperLeft;
        infoText.supportRichText = true;
    }

    void Update()
    {
        if (infoText == null || platform == null) return;

        float pp = platform.pitchOutputDeg;
        float rp = platform.rollOutputDeg;
        float hp = platform.heaveOutputM;

        string camStatus = platform.cockpitContainer != null && Camera.main != null
            && Camera.main.transform.parent == platform.cockpitContainer
            ? "ACTIVE" : "DISABLED";

        infoText.text = string.Format(
            "<b>=== 3DOF MOTION PLATFORM ===</b>\n" +
            "Immersion: <color=#88ff88>{0}</color>\n" +
            "\n" +
            "<b>Platform Output</b>\n" +
            "Pitch: <color=#66ccff>{1,7:F1}\u00b0</color>\n" +
            "Roll:  <color=#66ccff>{2,7:F1}\u00b0</color>\n" +
            "Heave: <color=#66ccff>{3,7:F2}m</color>\n" +
            "\n" +
            "<b>Inputs</b>\n" +
            "Throttle: <color=#ffcc66>{4,7:F0}%</color>\n" +
            "Pitch:    <color=#ffcc66>{5,7:F2}</color>\n" +
            "Roll:     <color=#ffcc66>{6,7:F2}</color>\n" +
            "Yaw:      <color=#ffcc66>{7,7:F2}</color>\n" +
            "\n" +
            "<b>Aircraft</b>\n" +
            "Speed: <color=#88ff88>{8,7:F0} m/s</color>\n" +
            "Alt:   <color=#88ff88>{9,7:F0} m</color>",
            camStatus,
            pp, rp, hp,
            (controller != null ? controller.ThrustPercent * 100 : 0),
            (controller != null ? controller.Pitch : 0),
            (controller != null ? controller.Roll : 0),
            (controller != null ? controller.Yaw : 0),
            (platform.TryGetComponent(out Rigidbody rb) ? rb.linearVelocity.magnitude : 0),
            platform.transform.position.y
        );

        UpdateHorizon();
    }

    void UpdateHorizon()
    {
        if (horizonRect == null || platform == null) return;

        float pitch = platform.pitchOutputDeg;
        float roll = platform.rollOutputDeg;
        float pixelsPerDegree = 2.2f;
        float pitchOffset = pitch * pixelsPerDegree;

        if (skyRect != null) skyRect.anchoredPosition = new Vector2(0, pitchOffset);
        if (groundRect != null) groundRect.anchoredPosition = new Vector2(0, pitchOffset);

        horizonContainer.localRotation = Quaternion.Euler(0, 0, roll);

        float h = horizonRect.rect.height;
        foreach (var mark in pitchMarks)
        {
            if (mark.label == null) continue;
            float y = h * 0.5f + (mark.angle - pitch) * pixelsPerDegree;
            mark.label.rectTransform.anchoredPosition = new Vector2(0, y);
            mark.label.gameObject.SetActive(y > -10 && y < h + 10);
        }
    }
}
