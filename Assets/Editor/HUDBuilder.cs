using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Menú: GameObject > UI > Survival HUD
/// Crea el Canvas completo y conecta las referencias del HUDController.
/// </summary>
public static class HUDBuilder
{
    [MenuItem("GameObject/UI/Survival HUD", false, 10)]
    public static void CreateHUD()
    {
        var canvasGO = new GameObject("HUD_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create Survival HUD");

        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        Transform root = canvasGO.transform;

        // --- Barras (arriba izquierda)
        var healthLabel = NewText("HealthLabel", root, "SALUD", 22, TextAlignmentOptions.Left);
        Place(healthLabel.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -30), new Vector2(300, 28));
        var healthSlider = NewSlider("HealthBar", root, new Color(0f, 0f, 0f, 0.55f), new Color(0.25f, 0.85f, 0.35f));
        Place((RectTransform)healthSlider.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -60), new Vector2(340, 24));

        var nutritionLabel = NewText("NutritionLabel", root, "NUTRICIÓN", 22, TextAlignmentOptions.Left);
        Place(nutritionLabel.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -98), new Vector2(300, 28));
        var nutritionSlider = NewSlider("NutritionBar", root, new Color(0f, 0f, 0f, 0.55f), new Color(0.95f, 0.70f, 0.20f));
        Place((RectTransform)nutritionSlider.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -128), new Vector2(340, 24));

        // --- Munición (abajo derecha)
        var ammo = NewText("AmmoText", root, "30 / 90", 56, TextAlignmentOptions.Right);
        Place(ammo.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-40, 40), new Vector2(360, 80));

        // --- Botiquines (abajo izquierda)
        var medkit = NewText("MedkitText", root, "Botiquines: 0", 32, TextAlignmentOptions.Left);
        Place(medkit.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(40, 40), new Vector2(360, 50));

        // --- Mensajes (abajo centro)
        var message = NewText("MessageText", root, "", 30, TextAlignmentOptions.Center);
        Place(message.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 180), new Vector2(1000, 60));
        message.alpha = 0f;

        // --- Fila de objetos clave (arriba derecha)
        var keyRow = NewRect("KeyItems", root);
        Place(keyRow, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -30), new Vector2(600, 72));
        var layout = keyRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleRight;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        // --- Controlador + referencias
        var controller = canvasGO.AddComponent<HUDController>();
        var so = new SerializedObject(controller);
        so.FindProperty("healthSlider").objectReferenceValue = healthSlider;
        so.FindProperty("nutritionSlider").objectReferenceValue = nutritionSlider;
        so.FindProperty("ammoText").objectReferenceValue = ammo;
        so.FindProperty("medkitText").objectReferenceValue = medkit;
        so.FindProperty("messageText").objectReferenceValue = message;
        so.FindProperty("keyItemContainer").objectReferenceValue = keyRow;
        so.ApplyModifiedPropertiesWithoutUndo();

        Selection.activeGameObject = canvasGO;
    }

    // ------------------------------------------------------------- Helpers

    private static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static TextMeshProUGUI NewText(string name, Transform parent, string text, float size, TextAlignmentOptions align)
    {
        var rt = NewRect(name, parent);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.alignment = align;
        t.color = Color.white;
        t.raycastTarget = false;
        return t;
    }

    private static Slider NewSlider(string name, Transform parent, Color bgColor, Color fillColor)
    {
        var root = NewRect(name, parent);

        var bg = NewRect("Background", root);
        Stretch(bg);
        var bgImg = bg.gameObject.AddComponent<Image>();
        bgImg.color = bgColor;
        bgImg.raycastTarget = false;

        var fillArea = NewRect("Fill Area", root);
        Stretch(fillArea);

        var fill = NewRect("Fill", fillArea);
        fill.sizeDelta = Vector2.zero;
        var fillImg = fill.gameObject.AddComponent<Image>();
        fillImg.color = fillColor;
        fillImg.raycastTarget = false;

        var slider = root.gameObject.AddComponent<Slider>();
        slider.transition = Selectable.Transition.None;
        slider.targetGraphic = null;
        slider.interactable = false;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.fillRect = fill;
        slider.value = 1f;
        return slider;
    }
}
