using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Builds the presentation-only UI for the Main kitchen scene. Gameplay code can later
/// update the public labels and order cards through the KitchenUIReferences component.
/// </summary>
public static class KitchenUIBootstrap
{
    private static Sprite _panelSprite;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BuildForMainScene()
    {
        if (SceneManager.GetActiveScene().name != "Main" || Object.FindFirstObjectByType<KitchenUIReferences>() != null)
            return;

        EnsureEventSystem();
        CreateLayout();
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
            return;

        var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        Object.DontDestroyOnLoad(eventSystem);
    }

    private static void CreateLayout()
    {
        var canvasObject = new GameObject("Kitchen UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        var refs = canvasObject.AddComponent<KitchenUIReferences>();
        var root = CreateRect("Safe Area", canvasObject.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        BuildHeader(root, refs);
        BuildOrderRail(root, refs);
        BuildHeldItem(root, refs);
        BuildStartPanel(root, refs);
        BuildPausePanel(root, refs);
        BuildEndPanel(root, refs);
    }

    private static void BuildHeader(RectTransform parent, KitchenUIReferences refs)
    {
        var header = CreateRect("HUD Header", parent, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -22f), new Vector2(-24f, -106f));
        var score = CreateStat(header, "Score", "SCORE", "0000", new Vector2(0f, 0f), new Vector2(0.25f, 1f));
        refs.scoreLabel = score.value;

        var timer = CreateStat(header, "Round Timer", "TIME LEFT", "03:00", new Vector2(0.375f, 0f), new Vector2(0.625f, 1f), new Color(1f, 0.75f, 0.25f));
        refs.roundTimerLabel = timer.value;

        var highScore = CreateStat(header, "High Score", "HIGH SCORE", "0000", new Vector2(0.75f, 0f), new Vector2(1f, 1f));
        refs.highScoreLabel = highScore.value;

        var pause = CreateButton(header, "Pause Button", "II", new Vector2(1f, 0.5f), new Vector2(72f, 72f), new Color(0.12f, 0.18f, 0.25f, 0.96f));
        pause.GetComponent<RectTransform>().anchoredPosition = new Vector2(-32f, 0f);
        pause.onClick.AddListener(() => refs.pausePanel.SetActive(true));
    }

    private static void BuildOrderRail(RectTransform parent, KitchenUIReferences refs)
    {
        var rail = CreateRect("Order Rail", parent, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(24f, -282f), new Vector2(378f, 282f));
        CreateLabel(rail, "Order Rail Title", "ACTIVE ORDERS", 22, TextAlignmentOptions.Left, Color.white,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, -38f), new Vector2(-12f, -6f));

        for (var index = 0; index < 4; index++)
        {
            var card = CreatePanel(rail, $"Order Card {index + 1}", new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -58f - index * 125f), new Vector2(0f, -172f - index * 125f), new Color(0.08f, 0.13f, 0.19f, 0.92f));
            refs.orderCards[index] = card.gameObject;

            CreateLabel(card, "Window", $"WINDOW {index + 1:00}", 16, TextAlignmentOptions.Left, new Color(0.63f, 0.82f, 1f),
                new Vector2(0f, 1f), new Vector2(0.55f, 1f), new Vector2(14f, -32f), new Vector2(0f, -8f));
            refs.orderTimers[index] = CreateLabel(card, "Age", "0:00", 18, TextAlignmentOptions.Right, new Color(1f, 0.75f, 0.25f),
                new Vector2(0.58f, 1f), new Vector2(1f, 1f), new Vector2(0f, -34f), new Vector2(-14f, -8f));

            var ingredients = CreateRect("Ingredients", card, new Vector2(0f, 0f), new Vector2(1f, 0.58f), new Vector2(14f, 8f), new Vector2(-14f, -6f));
            CreateIngredientChip(ingredients, "Vegetable", "VEG", new Vector2(0f, 0f), new Color(0.25f, 0.78f, 0.39f));
            CreateIngredientChip(ingredients, "Cheese", "CHEESE", new Vector2(0.34f, 0f), new Color(1f, 0.78f, 0.18f));
            CreateIngredientChip(ingredients, "Meat", "MEAT", new Vector2(0.68f, 0f), new Color(0.9f, 0.31f, 0.27f));
        }
    }

    private static void BuildHeldItem(RectTransform parent, KitchenUIReferences refs)
    {
        var panel = CreatePanel(parent, "Held Item", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-210f, 28f), new Vector2(210f, 112f), new Color(0.08f, 0.13f, 0.19f, 0.92f));
        CreateLabel(panel, "Holding Caption", "IN HAND", 15, TextAlignmentOptions.Center, new Color(0.63f, 0.82f, 1f), new Vector2(0f, 0.55f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        refs.heldItemLabel = CreateLabel(panel, "Held Item Value", "EMPTY", 25, TextAlignmentOptions.Center, Color.white, new Vector2(0f, 0f), new Vector2(1f, 0.65f), Vector2.zero, Vector2.zero);
        CreateLabel(parent, "Controls Hint", "WASD  MOVE     E  INTERACT     ESC  PAUSE", 16, TextAlignmentOptions.Center, new Color(0.87f, 0.92f, 0.98f, 0.86f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-300f, 4f), new Vector2(300f, 26f));
    }

    private static void BuildStartPanel(RectTransform parent, KitchenUIReferences refs)
    {
        var overlay = CreateOverlay(parent, "Start Screen");
        refs.startPanel = overlay.gameObject;
        var panel = CreatePanel(overlay, "Start Card", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-330f, -210f), new Vector2(330f, 210f), new Color(0.07f, 0.11f, 0.16f, 0.98f));
        CreateLabel(panel, "Title", "YES, CHEF!", 52, TextAlignmentOptions.Center, Color.white, new Vector2(0f, 0.66f), new Vector2(1f, 0.92f), Vector2.zero, Vector2.zero);
        CreateLabel(panel, "Subtitle", "RUN THE KITCHEN. FILL THE ORDERS. BEAT THE CLOCK.", 17, TextAlignmentOptions.Center, new Color(0.63f, 0.82f, 1f), new Vector2(0.08f, 0.51f), new Vector2(0.92f, 0.66f), Vector2.zero, Vector2.zero);
        CreateLabel(panel, "Instructions", "Collect ingredients from the fridge.\nChop vegetables, cook meat, then deliver the right ingredients to each window.", 18, TextAlignmentOptions.Center, new Color(0.9f, 0.93f, 0.98f), new Vector2(0.12f, 0.26f), new Vector2(0.88f, 0.51f), Vector2.zero, Vector2.zero);
        var start = CreateButton(panel, "Start Service", "START SERVICE", new Vector2(0.5f, 0.12f), new Vector2(300f, 64f), new Color(0.14f, 0.56f, 0.42f, 1f));
        start.onClick.AddListener(() => overlay.gameObject.SetActive(false));
    }

    private static void BuildPausePanel(RectTransform parent, KitchenUIReferences refs)
    {
        var overlay = CreateOverlay(parent, "Pause Screen");
        overlay.gameObject.SetActive(false);
        refs.pausePanel = overlay.gameObject;
        var panel = CreatePanel(overlay, "Pause Card", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-220f, -185f), new Vector2(220f, 185f), new Color(0.07f, 0.11f, 0.16f, 0.98f));
        CreateLabel(panel, "Pause Title", "PAUSED", 42, TextAlignmentOptions.Center, Color.white, new Vector2(0f, 0.7f), new Vector2(1f, 0.92f), Vector2.zero, Vector2.zero);
        var resume = CreateButton(panel, "Resume", "RESUME", new Vector2(0.5f, 0.5f), new Vector2(280f, 58f), new Color(0.14f, 0.56f, 0.42f, 1f));
        resume.onClick.AddListener(() => overlay.gameObject.SetActive(false));
        var restart = CreateButton(panel, "Restart", "RESTART ROUND", new Vector2(0.5f, 0.31f), new Vector2(280f, 50f), new Color(0.17f, 0.27f, 0.39f, 1f));
        restart.onClick.AddListener(() => SceneManager.LoadScene("Main"));
        CreateButton(panel, "Quit", "QUIT GAME", new Vector2(0.5f, 0.14f), new Vector2(280f, 44f), new Color(0.42f, 0.18f, 0.2f, 1f)).onClick.AddListener(Application.Quit);
    }

    private static void BuildEndPanel(RectTransform parent, KitchenUIReferences refs)
    {
        var overlay = CreateOverlay(parent, "Round Complete Screen");
        overlay.gameObject.SetActive(false);
        refs.endPanel = overlay.gameObject;
        var panel = CreatePanel(overlay, "Round Complete Card", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-260f, -180f), new Vector2(260f, 180f), new Color(0.07f, 0.11f, 0.16f, 0.98f));
        CreateLabel(panel, "Complete Title", "SERVICE COMPLETE", 36, TextAlignmentOptions.Center, Color.white, new Vector2(0f, 0.68f), new Vector2(1f, 0.9f), Vector2.zero, Vector2.zero);
        refs.finalScoreLabel = CreateLabel(panel, "Final Score", "FINAL SCORE  0000", 28, TextAlignmentOptions.Center, new Color(1f, 0.75f, 0.25f), new Vector2(0f, 0.48f), new Vector2(1f, 0.66f), Vector2.zero, Vector2.zero);
        refs.newHighScoreLabel = CreateLabel(panel, "New High Score", "NEW HIGH SCORE!", 18, TextAlignmentOptions.Center, new Color(0.35f, 0.9f, 0.55f), new Vector2(0f, 0.34f), new Vector2(1f, 0.48f), Vector2.zero, Vector2.zero);
        CreateButton(panel, "Play Again", "PLAY AGAIN", new Vector2(0.5f, 0.14f), new Vector2(280f, 58f), new Color(0.14f, 0.56f, 0.42f, 1f)).onClick.AddListener(() => SceneManager.LoadScene("Main"));
    }

    private static (TextMeshProUGUI caption, TextMeshProUGUI value) CreateStat(RectTransform parent, string name, string caption, string value, Vector2 min, Vector2 max, Color? accent = null)
    {
        var panel = CreatePanel(parent, name, min, max, new Vector2(8f, 0f), new Vector2(-8f, 0f), new Color(0.08f, 0.13f, 0.19f, 0.92f));
        var captionLabel = CreateLabel(panel, "Caption", caption, 14, TextAlignmentOptions.Center, accent ?? new Color(0.63f, 0.82f, 1f), new Vector2(0f, 0.5f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        var valueLabel = CreateLabel(panel, "Value", value, 29, TextAlignmentOptions.Center, Color.white, new Vector2(0f, 0f), new Vector2(1f, 0.62f), Vector2.zero, Vector2.zero);
        return (captionLabel, valueLabel);
    }

    private static void CreateIngredientChip(RectTransform parent, string name, string text, Vector2 anchor, Color colour)
    {
        var chip = CreatePanel(parent, name, anchor, anchor, new Vector2(0f, 0f), new Vector2(88f, 42f), new Color(colour.r, colour.g, colour.b, 0.2f));
        chip.anchoredPosition = new Vector2(anchor.x * (parent.rect.width - 88f), 0f);
        CreateLabel(chip, "Text", text, 12, TextAlignmentOptions.Center, colour, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
    }

    private static Button CreateButton(RectTransform parent, string name, string label, Vector2 anchor, Vector2 size, Color colour)
    {
        var button = CreatePanel(parent, name, anchor, anchor, -size * 0.5f, size * 0.5f, colour);
        var component = button.gameObject.AddComponent<Button>();
        component.targetGraphic = button.GetComponent<Image>();
        var colors = component.colors;
        colors.highlightedColor = Color.Lerp(colour, Color.white, 0.15f);
        colors.pressedColor = Color.Lerp(colour, Color.black, 0.2f);
        component.colors = colors;
        CreateLabel(button, "Label", label, 18, TextAlignmentOptions.Center, Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return component;
    }

    private static RectTransform CreateOverlay(RectTransform parent, string name)
    {
        var overlay = CreateRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var image = overlay.gameObject.AddComponent<Image>();
        image.color = new Color(0.01f, 0.02f, 0.04f, 0.76f);
        return overlay;
    }

    private static RectTransform CreatePanel(RectTransform parent, string name, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax, Color colour)
    {
        var panel = CreateRect(name, parent, min, max, offsetMin, offsetMax);
        var image = panel.gameObject.AddComponent<Image>();
        image.color = colour;

        var border = CreateRect("White Rounded Border", panel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var borderImage = border.gameObject.AddComponent<Image>();
        borderImage.sprite = GetPanelSprite();
        borderImage.type = Image.Type.Sliced;
        borderImage.color = new Color(1f, 1f, 1f, 0.72f);
        borderImage.raycastTarget = false;
        return panel;
    }

    private static TextMeshProUGUI CreateLabel(RectTransform parent, string name, string text, float size, TextAlignmentOptions alignment, Color colour, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    {
        var label = CreateRect(name, parent, min, max, offsetMin, offsetMax);
        var tmp = label.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.font = TMP_Settings.defaultFontAsset;
        tmp.fontSize = size;
        tmp.alignment = alignment;
        tmp.color = colour;
        tmp.enableWordWrapping = true;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    {
        var node = new GameObject(name, typeof(RectTransform));
        node.transform.SetParent(parent, false);
        var rect = node.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        return rect;
    }

    private static Sprite GetPanelSprite()
    {
        if (_panelSprite != null)
            return _panelSprite;

        const int size = 64;
        const int radius = 12;
        const int thickness = 3;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
        {
            var dx = Mathf.Max(radius - 1 - x, 0, x - (size - radius));
            var dy = Mathf.Max(radius - 1 - y, 0, y - (size - radius));
            var distance = Mathf.Sqrt(dx * dx + dy * dy);
            var outer = distance <= radius ? 1f : 0f;
            var innerDistance = Mathf.Sqrt(Mathf.Max(dx - thickness, 0) * Mathf.Max(dx - thickness, 0) + Mathf.Max(dy - thickness, 0) * Mathf.Max(dy - thickness, 0));
            var inner = (x >= thickness && x < size - thickness && y >= thickness && y < size - thickness && innerDistance <= radius - thickness) ? 1f : 0f;
            texture.SetPixel(x, y, new Color(1f, 1f, 1f, outer - inner));
        }
        texture.Apply();
        _panelSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        return _panelSprite;
    }
}

public sealed class KitchenUIReferences : MonoBehaviour
{
    public TextMeshProUGUI scoreLabel;
    public TextMeshProUGUI highScoreLabel;
    public TextMeshProUGUI roundTimerLabel;
    public TextMeshProUGUI heldItemLabel;
    public TextMeshProUGUI finalScoreLabel;
    public TextMeshProUGUI newHighScoreLabel;
    public readonly GameObject[] orderCards = new GameObject[4];
    public readonly TextMeshProUGUI[] orderTimers = new TextMeshProUGUI[4];
    public GameObject startPanel;
    public GameObject pausePanel;
    public GameObject endPanel;
}
