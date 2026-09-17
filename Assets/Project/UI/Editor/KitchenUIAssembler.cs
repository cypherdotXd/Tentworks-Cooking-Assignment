using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class KitchenUIAssembler
{
    private static Sprite _roundedSolid;
    private static Sprite _roundedOutline;
    private static Sprite _roundedGlow;
    private static Sprite _lettuce;
    private static Sprite _cheese;
    private static Sprite _meat;
    private static Sprite _chefAvatar;
    private static Sprite _chefPlayer;
    private static Sprite _circleBadge;
    private static Sprite _circleDot;
    private static Sprite _burnerRing;
    private static Sprite _pauseIcon;

    private static int _frameCounter;

    static KitchenUIAssembler()
    {
        _frameCounter = 0;
        EditorApplication.update += AutoAssembleUpdate;
    }

    private static void AutoAssembleUpdate()
    {
        _frameCounter++;
        if (_frameCounter < 3) return;
        EditorApplication.update -= AutoAssembleUpdate;

        AssembleUI();
    }

    [MenuItem("Tools/Assemble Kitchen UI")]
    public static void AssembleUI()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name != "Main")
        {
            if (File.Exists("Assets/Project/Scenes/Main.unity"))
                scene = EditorSceneManager.OpenScene("Assets/Project/Scenes/Main.unity");
            else
                return;
        }

        LoadSprites();
        EnsureEventSystem();

        // Clean up old canvases or previous attempts
        var allCanvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        foreach (var c in allCanvases)
        {
            if (c.gameObject.name.Contains("Kitchen") || c.gameObject.name == "Canvas")
            {
                Undo.DestroyObjectImmediate(c.gameObject);
            }
        }

        // 1. Root Canvas
        var canvasObj = new GameObject("Kitchen UI Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasObj, "Create Kitchen UI Canvas");

        var canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        var refs = canvasObj.AddComponent<KitchenUIReferences>();

        // Safe Area Root
        var root = CreateRect("Safe Area", canvasObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // 2. Kitchen Bezel & Mat
        BuildKitchenFrame(root);

        // 3. HUD Header Bar
        BuildHeader(root, refs);

        // 4. Order Rail (Left side)
        BuildOrderRail(root, refs);

        // 5. Center Player & Tooltip
        BuildPlayerAndTooltip(root, refs);

        // 6. Stations Overlay (Right side)
        BuildStations(root, refs);

        // 7. Bottom Legend Bar
        BuildBottomLegend(root);

        // 8. Menus (Overlays)
        BuildOverlays(root, refs);

        EditorUtility.SetDirty(canvasObj);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("<color=green>Kitchen UI Canvas assembled and serialized successfully into Main scene!</color>");
    }

    private static void LoadSprites()
    {
        _roundedSolid = LoadSprite("rounded_rect_solid");
        _roundedOutline = LoadSprite("rounded_rect_outline");
        _roundedGlow = LoadSprite("rounded_rect_glow");
        _lettuce = LoadSprite("lettuce");
        _cheese = LoadSprite("cheese");
        _meat = LoadSprite("meat");
        _chefAvatar = LoadSprite("chef_avatar");
        _chefPlayer = LoadSprite("chef_player");
        _circleBadge = LoadSprite("circle_badge");
        _circleDot = LoadSprite("circle_dot");
        _burnerRing = LoadSprite("burner_ring");
        _pauseIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/UI/icons/pause.svg");
    }

    private static Sprite LoadSprite(string name)
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Project/UI/Sprites/{name}.png");
        if (s == null)
            s = Resources.Load<Sprite>($"UI/{name}");
        return s;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
    }

    private static void BuildKitchenFrame(RectTransform parent)
    {
        // Dark outer bezel background
        var bg = CreatePanel(parent, "Outer Dark Bezel", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.10f, 0.13f, 0.11f, 1f));
        bg.GetComponent<Image>().raycastTarget = false;

        // Rounded Taupe Kitchen Counter Mat
        var mat = CreatePanel(bg, "Kitchen Mat", Vector2.zero, Vector2.one, new Vector2(24f, 54f), new Vector2(-24f, -78f),
            new Color(0.43f, 0.38f, 0.32f, 1f), _roundedSolid);
        mat.GetComponent<Image>().raycastTarget = false;

        // Dark Brown Mat Outline
        if (_roundedOutline != null)
        {
            var border = CreatePanel(mat, "Mat Border", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                new Color(0.21f, 0.18f, 0.15f, 1f), _roundedOutline);
            border.GetComponent<Image>().raycastTarget = false;
        }

        // Decorative subtle vertical floor lines
        var grid = CreateRect("Floor Grid Lines", mat, Vector2.zero, Vector2.one, new Vector2(20f, 20f), new Vector2(-20f, -20f));
        for (var i = 1; i <= 6; i++)
        {
            var frac = i / 7f;
            var line = CreateRect($"Line {i}", grid, new Vector2(frac, 0f), new Vector2(frac, 1f), new Vector2(-1f, 0f), new Vector2(1f, 0f));
            var img = line.gameObject.AddComponent<Image>();
            img.color = new Color(0.38f, 0.33f, 0.27f, 0.28f);
            img.raycastTarget = false;
        }
    }

    private static void BuildHeader(RectTransform parent, KitchenUIReferences refs)
    {
        var header = CreateRect("HUD Header Bar", parent, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -74f), Vector2.zero);
        var headerBg = header.gameObject.AddComponent<Image>();
        headerBg.color = new Color(0.10f, 0.13f, 0.11f, 1f);

        // Left Branding
        var brand = CreateRect("Branding", header, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(28f, -26f), new Vector2(360f, 26f));
        if (_chefAvatar != null)
        {
            var av = CreateRect("Chef Avatar", brand, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, -24f), new Vector2(48f, 24f));
            var img = av.gameObject.AddComponent<Image>();
            img.sprite = _chefAvatar;
            img.preserveAspect = true;
        }

        CreateLabel(brand, "App Title", "YES, CHEF!", 24, FontWeight.Bold, Color.white, TextAlignmentOptions.Left,
            new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(58f, 2f), new Vector2(0f, 26f));
        CreateLabel(brand, "App Subtitle", "Solo kitchen shift", 13, FontWeight.Regular, new Color(0.61f, 0.67f, 0.64f), TextAlignmentOptions.Left,
            new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(58f, -22f), new Vector2(0f, 0f));

        // Right Stats
        var stats = CreateRect("Stats & Controls", header, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-540f, -30f), new Vector2(-28f, 30f));

        refs.roundTimerLabel = CreateHeaderStat(stats, "Time Left", "01:47", "TIME LEFT", 50f);
        refs.scoreLabel = CreateHeaderStat(stats, "Score", "240", "SCORE", 180f);
        refs.highScoreLabel = CreateHeaderStat(stats, "High Score", "510", "HIGH SCORE", 310f);

        // Pause Button
        var pauseRect = CreateRect("Pause Button", stats, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(430f, -23f), new Vector2(476f, 23f));
        CreatePanel(pauseRect, "Body", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.12f, 0.16f, 0.14f, 1f), _roundedSolid);
        CreatePanel(pauseRect, "Border", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.32f, 0.40f, 0.36f, 1f), _roundedOutline);

        var btn = pauseRect.gameObject.AddComponent<Button>();
        btn.targetGraphic = pauseRect.GetComponentInChildren<Image>();
        refs.pauseButton = btn;

        if (_pauseIcon != null)
        {
            var pIcon = CreateRect("Icon", pauseRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-11f, -11f), new Vector2(11f, 11f));
            var pImg = pIcon.gameObject.AddComponent<Image>();
            pImg.sprite = _pauseIcon;
            pImg.color = Color.white;
            pImg.preserveAspect = true;
            pImg.raycastTarget = false;
        }
        else
        {
            CreateLabel(pauseRect, "Text Icon", "II", 16, FontWeight.Bold, Color.white, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }
    }

    private static TextMeshProUGUI CreateHeaderStat(RectTransform parent, string name, string valText, string labelText, float posX)
    {
        var container = CreateRect(name, parent, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(posX - 55f, -26f), new Vector2(posX + 55f, 26f));
        var val = CreateLabel(container, "Val", valText, 26, FontWeight.Bold, Color.white, TextAlignmentOptions.Center,
            new Vector2(0f, 0.35f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        CreateLabel(container, "Lbl", labelText, 11, FontWeight.Bold, new Color(0.58f, 0.65f, 0.61f), TextAlignmentOptions.Center,
            new Vector2(0f, 0f), new Vector2(1f, 0.38f), Vector2.zero, Vector2.zero);
        return val;
    }

    private static void BuildOrderRail(RectTransform parent, KitchenUIReferences refs)
    {
        var rail = CreateRect("Order Rail", parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(46f, -700f), new Vector2(236f, -100f));

        var configs = new[]
        {
            new { Title = "WINDOW 1", Timer = "00:12", IsSelected = false, TimerCol = new Color(0.88f, 0.35f, 0.28f, 1f), Ing = new[] { _lettuce, _cheese } },
            new { Title = "WINDOW 2", Timer = "00:28", IsSelected = true,  TimerCol = new Color(0.90f, 0.38f, 0.30f, 1f), Ing = new[] { _meat, _cheese } },
            new { Title = "WINDOW 3", Timer = "00:36", IsSelected = false, TimerCol = new Color(0.88f, 0.35f, 0.28f, 1f), Ing = new[] { _meat, _meat, _cheese } },
            new { Title = "WINDOW 4", Timer = "00:08", IsSelected = false, TimerCol = new Color(1.00f, 0.24f, 0.19f, 1f), Ing = new[] { _lettuce, _meat, _cheese } },
        };

        for (var i = 0; i < 4; i++)
        {
            var cfg = configs[i];
            var top = -i * 146f;
            var bottom = top - 132f;

            var card = CreateRect($"Order Card {i + 1}", rail, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, bottom), new Vector2(0f, top));

            // Shadow
            CreatePanel(card, "Shadow", Vector2.zero, Vector2.one, new Vector2(3f, -3f), new Vector2(3f, -3f), new Color(0.12f, 0.10f, 0.09f, 0.6f), _roundedSolid);

            // Card Body
            CreatePanel(card, "Body", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.22f, 0.20f, 0.19f, 0.98f), _roundedSolid);

            // Border
            Image borderImg = null;
            if (cfg.IsSelected)
            {
                var borderObj = CreatePanel(card, "Glow Border", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.96f, 0.88f, 0.54f, 1f), _roundedGlow);
                borderImg = borderObj.GetComponent<Image>();
            }
            else
            {
                var borderObj = CreatePanel(card, "Border", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, cfg.TimerCol, _roundedOutline);
                borderImg = borderObj.GetComponent<Image>();
            }

            // Window Title
            var title = CreateLabel(card, "Title", cfg.Title, 13, FontWeight.Bold, new Color(0.56f, 0.54f, 0.52f), TextAlignmentOptions.Left,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(14f, -28f), new Vector2(-14f, -8f));

            // Ingredients Row
            var row = CreateRect("Ingredients Row", card, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(14f, -16f), new Vector2(-14f, 22f));
            for (var j = 0; j < cfg.Ing.Length; j++)
            {
                var sprite = cfg.Ing[j];
                var badge = CreateRect($"Badge {j + 1}", row, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(j * 40f, -17f), new Vector2(j * 40f + 34f, 17f));
                var bImg = badge.gameObject.AddComponent<Image>();
                bImg.sprite = _circleBadge;
                bImg.color = Color.white;

                if (sprite != null)
                {
                    var icon = CreateRect("Icon", badge, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-12f, -12f), new Vector2(12f, 12f));
                    var iImg = icon.gameObject.AddComponent<Image>();
                    iImg.sprite = sprite;
                    iImg.preserveAspect = true;
                    iImg.raycastTarget = false;
                }
            }

            // Countdown Timer
            var timer = CreateLabel(card, "Timer", cfg.Timer, 20, FontWeight.Bold, cfg.TimerCol, TextAlignmentOptions.Left,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(14f, 8f), new Vector2(-14f, 32f));

            refs.orderCards[i] = new OrderCardUI
            {
                cardRoot = card.gameObject,
                borderImage = borderImg,
                windowTitle = title,
                ingredientsContainer = row,
                timerLabel = timer
            };
        }
    }

    private static void BuildPlayerAndTooltip(RectTransform parent, KitchenUIReferences refs)
    {
        // Center Player Token
        var player = CreateRect("Player Avatar Token", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-60f, -70f), new Vector2(40f, 30f));
        if (_chefPlayer != null)
        {
            var pImg = player.gameObject.AddComponent<Image>();
            pImg.sprite = _chefPlayer;
            pImg.preserveAspect = true;
            pImg.raycastTarget = false;
        }

        if (_cheese != null)
        {
            var held = CreateRect("Held Item", player, new Vector2(1f, 0.25f), new Vector2(1f, 0.25f), new Vector2(-8f, -16f), new Vector2(24f, 16f));
            var hImg = held.gameObject.AddComponent<Image>();
            hImg.sprite = _cheese;
            hImg.preserveAspect = true;
            hImg.raycastTarget = false;
        }

        // In-Hand Tooltip Pill
        var tooltip = CreateRect("In Hand Tooltip", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-115f, -150f), new Vector2(95f, -112f));
        refs.heldItemPill = tooltip.gameObject;

        CreatePanel(tooltip, "Body", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.22f, 0.21f, 0.20f, 0.95f), _roundedSolid);
        CreatePanel(tooltip, "Border", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.35f, 0.33f, 0.30f, 0.8f), _roundedOutline);

        refs.heldItemLabel = CreateLabel(tooltip, "Text", "In hand:        cheese", 14, FontWeight.Bold, new Color(0.92f, 0.92f, 0.92f), TextAlignmentOptions.Center,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        if (_cheese != null)
        {
            var cIcon = CreateRect("Cheese Icon", tooltip, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-6f, -9f), new Vector2(12f, 9f));
            var ci = cIcon.gameObject.AddComponent<Image>();
            ci.sprite = _cheese;
            ci.preserveAspect = true;
            ci.raycastTarget = false;
            refs.heldItemIcon = ci;
        }
    }

    private static void BuildStations(RectTransform parent, KitchenUIReferences refs)
    {
        var stations = CreateRect("Kitchen Stations HUD", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // 1. PREP TABLE
        var prep = CreateStationCard(stations, "PREP TABLE Station", new Vector2(0.5f, 0.5f), new Vector2(200f, 70f), new Vector2(460f, 170f));
        refs.prepTablePanel = prep.gameObject;

        CreateLabel(prep, "Header", "PREP TABLE", 17, FontWeight.Bold, Color.white, TextAlignmentOptions.Center,
            new Vector2(0f, 0.5f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

        var prepRow = CreateRect("Status Row", prep, new Vector2(0f, 0f), new Vector2(1f, 0.55f), Vector2.zero, Vector2.zero);
        if (_lettuce != null)
        {
            var lIcon = CreateRect("Icon", prepRow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-74f, -10f), new Vector2(-54f, 10f));
            var li = lIcon.gameObject.AddComponent<Image>();
            li.sprite = _lettuce;
            li.preserveAspect = true;
        }
        refs.prepTableStatusLabel = CreateLabel(prepRow, "Status Text", "chopping · 1.2s", 14, FontWeight.Regular, new Color(0.85f, 0.85f, 0.85f), TextAlignmentOptions.Left,
            new Vector2(0.5f, 0f), new Vector2(1f, 1f), new Vector2(-48f, 0f), Vector2.zero);

        // 2. FRIDGE
        var fridge = CreateRect("FRIDGE Station", stations, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(510f, 60f), new Vector2(670f, 260f));
        refs.fridgePanel = fridge.gameObject;

        CreatePanel(fridge, "Shadow", Vector2.zero, Vector2.one, new Vector2(3f, -3f), new Vector2(3f, -3f), new Color(0.10f, 0.16f, 0.19f, 0.7f), _roundedSolid);
        CreatePanel(fridge, "Body", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.29f, 0.45f, 0.55f, 1f), _roundedSolid);
        CreatePanel(fridge, "Border", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.18f, 0.28f, 0.35f, 1f), _roundedOutline);

        CreateLabel(fridge, "Title", "FRIDGE", 19, FontWeight.Bold, new Color(0.88f, 0.94f, 0.98f), TextAlignmentOptions.Center,
            new Vector2(0f, 0.70f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

        var handle = CreateRect("Handle", fridge, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-2f, -44f), new Vector2(2f, 44f));
        var hImg = handle.gameObject.AddComponent<Image>();
        hImg.color = new Color(0.88f, 0.94f, 0.98f, 0.95f);

        CreateLabel(fridge, "Subtitle", "raw ingredients", 13, FontWeight.Regular, new Color(0.68f, 0.78f, 0.85f), TextAlignmentOptions.Center,
            new Vector2(0f, 0f), new Vector2(1f, 0.25f), Vector2.zero, Vector2.zero);

        // 3. STOVE
        var stove = CreateStationCard(stations, "STOVE Station", new Vector2(0.5f, 0.5f), new Vector2(190f, -230f), new Vector2(470f, -80f));
        refs.stovePanel = stove.gameObject;

        CreateLabel(stove, "Header", "STOVE", 17, FontWeight.Bold, Color.white, TextAlignmentOptions.Center,
            new Vector2(0f, 0.65f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

        var bLeft = CreateRect("Left Burner", stove, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-50f, -8f), new Vector2(-6f, 36f));
        var bli = bLeft.gameObject.AddComponent<Image>();
        bli.sprite = _burnerRing;
        bli.color = new Color(0.90f, 0.49f, 0.28f, 1f);
        refs.stoveBurnerLeft = bli;

        var bRight = CreateRect("Right Burner", stove, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(6f, -8f), new Vector2(50f, 36f));
        var bri = bRight.gameObject.AddComponent<Image>();
        bri.sprite = _burnerRing;
        bri.color = new Color(0.31f, 0.73f, 0.44f, 1f);
        refs.stoveBurnerRight = bri;

        var stoveRow = CreateRect("Status Row", stove, new Vector2(0f, 0f), new Vector2(1f, 0.35f), Vector2.zero, Vector2.zero);
        if (_meat != null)
        {
            var mIcon = CreateRect("Icon", stoveRow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-62f, -10f), new Vector2(-42f, 10f));
            var mi = mIcon.gameObject.AddComponent<Image>();
            mi.sprite = _meat;
            mi.preserveAspect = true;
        }
        refs.stoveStatusLabel = CreateLabel(stoveRow, "Status Text", "3.5s · ready", 14, FontWeight.Regular, new Color(0.85f, 0.85f, 0.85f), TextAlignmentOptions.Left,
            new Vector2(0.5f, 0f), new Vector2(1f, 1f), new Vector2(-36f, 0f), Vector2.zero);

        // 4. TRASH
        var trash = CreateStationCard(stations, "TRASH Station", new Vector2(0.5f, 0.5f), new Vector2(520f, -230f), new Vector2(660f, -100f), new Color(0.28f, 0.25f, 0.22f, 0.98f));
        refs.trashPanel = trash.gameObject;
        CreateLabel(trash, "Header", "TRASH", 18, FontWeight.Bold, new Color(0.80f, 0.75f, 0.70f), TextAlignmentOptions.Center,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // 5. Guidance Banner
        var banner = CreateRect("Guidance Banner", stations, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(210f, -300f), new Vector2(670f, -244f));
        refs.helperMessagePanel = banner.gameObject;

        CreatePanel(banner, "Body", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.21f, 0.19f, 0.18f, 0.92f), _roundedSolid);
        CreatePanel(banner, "Border", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.35f, 0.32f, 0.30f, 0.8f), _roundedOutline);

        refs.helperMessageLabel = CreateLabel(banner, "Text", "Window 2 selected — deliver cheese next for a quick 12-point finish.", 13, FontWeight.Regular,
            new Color(0.84f, 0.84f, 0.84f), TextAlignmentOptions.Left, Vector2.zero, Vector2.one, new Vector2(14f, 0f), new Vector2(-14f, 0f));
    }

    private static RectTransform CreateStationCard(RectTransform parent, string name, Vector2 anchor, Vector2 offsetMin, Vector2 offsetMax, Color? cardColor = null)
    {
        var card = CreateRect(name, parent, anchor, anchor, offsetMin, offsetMax);
        CreatePanel(card, "Shadow", Vector2.zero, Vector2.one, new Vector2(3f, -3f), new Vector2(3f, -3f), new Color(0.12f, 0.10f, 0.09f, 0.6f), _roundedSolid);
        CreatePanel(card, "Body", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, cardColor ?? new Color(0.22f, 0.20f, 0.19f, 0.98f), _roundedSolid);
        CreatePanel(card, "Border", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.35f, 0.32f, 0.30f, 0.85f), _roundedOutline);
        return card;
    }

    private static void BuildBottomLegend(RectTransform parent)
    {
        var legend = CreateRect("Bottom Legend Bar", parent, new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 48f));
        var lBg = legend.gameObject.AddComponent<Image>();
        lBg.color = new Color(0.10f, 0.13f, 0.11f, 1f);

        var row = CreateRect("Legend Row", legend, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(28f, -14f), new Vector2(-28f, 14f));

        var pos = 0f;
        AddLegendItem(row, ref pos, _circleDot, new Color(0.90f, 0.36f, 0.28f, 1f), "Urgent order timer", 175f);
        AddLegendItem(row, ref pos, _circleDot, new Color(0.31f, 0.73f, 0.44f, 1f), "Cooked and ready", 175f);
        AddLegendItem(row, ref pos, _lettuce, Color.white, "needs prep  ·", 130f);
        AddLegendItem(row, ref pos, _meat, Color.white, "needs stove  ·", 135f);
        AddLegendItem(row, ref pos, _cheese, Color.white, "serve directly", 135f);
    }

    private static void AddLegendItem(RectTransform parent, ref float curX, Sprite icon, Color iconColor, string text, float width)
    {
        var item = CreateRect("Item", parent, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(curX, -12f), new Vector2(curX + width, 12f));
        if (icon != null)
        {
            var iRect = CreateRect("Icon", item, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, -8f), new Vector2(16f, 8f));
            var img = iRect.gameObject.AddComponent<Image>();
            img.sprite = icon;
            img.color = iconColor;
            img.preserveAspect = true;
            img.raycastTarget = false;
        }
        CreateLabel(item, "Text", text, 13, FontWeight.Regular, new Color(0.70f, 0.77f, 0.73f), TextAlignmentOptions.Left,
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(22f, 0f), Vector2.zero);

        curX += width + 18f;
    }

    private static void BuildOverlays(RectTransform parent, KitchenUIReferences refs)
    {
        // Start Screen
        var startOverlay = CreateOverlay(parent, "Start Screen Overlay");
        refs.startPanel = startOverlay.gameObject;
        startOverlay.gameObject.SetActive(false);

        var startCard = CreateStationCard(startOverlay, "Start Card", new Vector2(0.5f, 0.5f), new Vector2(-300f, -190f), new Vector2(300f, 190f));
        CreateLabel(startCard, "Title", "YES, CHEF!", 46, FontWeight.Bold, Color.white, TextAlignmentOptions.Center, new Vector2(0f, 0.65f), new Vector2(1f, 0.95f), Vector2.zero, Vector2.zero);
        CreateLabel(startCard, "Subtitle", "RUN THE KITCHEN. FILL THE ORDERS. BEAT THE CLOCK.", 15, FontWeight.Bold, new Color(0.60f, 0.67f, 0.63f), TextAlignmentOptions.Center, new Vector2(0.05f, 0.52f), new Vector2(0.95f, 0.65f), Vector2.zero, Vector2.zero);
        CreateLabel(startCard, "Instructions", "Collect ingredients from the fridge.\nChop vegetables, cook meat, then deliver the right ingredients to each window.", 15, FontWeight.Regular, new Color(0.85f, 0.90f, 0.88f), TextAlignmentOptions.Center, new Vector2(0.08f, 0.28f), new Vector2(0.92f, 0.52f), Vector2.zero, Vector2.zero);

        var startBtn = CreateButton(startCard, "Start Button", "START SERVICE", new Vector2(0.5f, 0.14f), new Vector2(260f, 50f), new Color(0.16f, 0.55f, 0.38f, 1f));
        startBtn.onClick.AddListener(() => startOverlay.gameObject.SetActive(false));

        // Pause Screen
        var pauseOverlay = CreateOverlay(parent, "Pause Screen Overlay");
        refs.pausePanel = pauseOverlay.gameObject;
        pauseOverlay.gameObject.SetActive(false);

        var pauseCard = CreateStationCard(pauseOverlay, "Pause Card", new Vector2(0.5f, 0.5f), new Vector2(-200f, -160f), new Vector2(200f, 160f));
        CreateLabel(pauseCard, "Title", "PAUSED", 36, FontWeight.Bold, Color.white, TextAlignmentOptions.Center, new Vector2(0f, 0.70f), new Vector2(1f, 0.92f), Vector2.zero, Vector2.zero);

        var resumeBtn = CreateButton(pauseCard, "Resume Button", "RESUME", new Vector2(0.5f, 0.50f), new Vector2(240f, 46f), new Color(0.16f, 0.55f, 0.38f, 1f));
        resumeBtn.onClick.AddListener(() => pauseOverlay.gameObject.SetActive(false));

        var restartBtn = CreateButton(pauseCard, "Restart Button", "RESTART ROUND", new Vector2(0.5f, 0.30f), new Vector2(240f, 42f), new Color(0.22f, 0.32f, 0.42f, 1f));
        restartBtn.onClick.AddListener(() => SceneManager.LoadScene("Main"));

        var quitBtn = CreateButton(pauseCard, "Quit Button", "QUIT GAME", new Vector2(0.5f, 0.12f), new Vector2(240f, 38f), new Color(0.48f, 0.20f, 0.22f, 1f));
        quitBtn.onClick.AddListener(Application.Quit);

        if (refs.pauseButton != null)
        {
            refs.pauseButton.onClick.AddListener(() => pauseOverlay.gameObject.SetActive(true));
        }

        // Round Complete Screen
        var endOverlay = CreateOverlay(parent, "Round Complete Overlay");
        refs.endPanel = endOverlay.gameObject;
        endOverlay.gameObject.SetActive(false);

        var endCard = CreateStationCard(endOverlay, "Complete Card", new Vector2(0.5f, 0.5f), new Vector2(-240f, -160f), new Vector2(240f, 160f));
        CreateLabel(endCard, "Title", "SERVICE COMPLETE", 30, FontWeight.Bold, Color.white, TextAlignmentOptions.Center, new Vector2(0f, 0.68f), new Vector2(1f, 0.90f), Vector2.zero, Vector2.zero);
        refs.finalScoreLabel = CreateLabel(endCard, "Final Score", "FINAL SCORE  0000", 24, FontWeight.Bold, new Color(0.97f, 0.88f, 0.56f), TextAlignmentOptions.Center, new Vector2(0f, 0.48f), new Vector2(1f, 0.66f), Vector2.zero, Vector2.zero);
        refs.newHighScoreLabel = CreateLabel(endCard, "New High Score", "NEW HIGH SCORE!", 16, FontWeight.Bold, new Color(0.35f, 0.85f, 0.50f), TextAlignmentOptions.Center, new Vector2(0f, 0.34f), new Vector2(1f, 0.48f), Vector2.zero, Vector2.zero);

        var playAgainBtn = CreateButton(endCard, "Play Again Button", "PLAY AGAIN", new Vector2(0.5f, 0.14f), new Vector2(240f, 48f), new Color(0.16f, 0.55f, 0.38f, 1f));
        playAgainBtn.onClick.AddListener(() => SceneManager.LoadScene("Main"));
    }

    private static Button CreateButton(RectTransform parent, string name, string label, Vector2 anchor, Vector2 size, Color color)
    {
        var btnRect = CreatePanel(parent, name, anchor, anchor, -size * 0.5f, size * 0.5f, color, _roundedSolid);
        CreatePanel(btnRect, "Border", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.white * 0.3f, _roundedOutline);
        var btn = btnRect.gameObject.AddComponent<Button>();
        btn.targetGraphic = btnRect.GetComponent<Image>();
        CreateLabel(btnRect, "Label", label, 15, FontWeight.Bold, Color.white, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return btn;
    }

    private static RectTransform CreateOverlay(RectTransform parent, string name)
    {
        var overlay = CreateRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var img = overlay.gameObject.AddComponent<Image>();
        img.color = new Color(0.02f, 0.03f, 0.03f, 0.82f);
        return overlay;
    }

    private static RectTransform CreatePanel(RectTransform parent, string name, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax, Color color, Sprite sprite = null)
    {
        var panel = CreateRect(name, parent, min, max, offsetMin, offsetMax);
        var img = panel.gameObject.AddComponent<Image>();
        img.color = color;
        if (sprite != null)
        {
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
        }
        return panel;
    }

    private static TextMeshProUGUI CreateLabel(RectTransform parent, string name, string text, float size, FontWeight weight, Color color,
        TextAlignmentOptions alignment, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    {
        var node = CreateRect(name, parent, min, max, offsetMin, offsetMax);
        var tmp = node.gameObject.AddComponent<TextMeshProUGUI>();

        try
        {
            if (TMP_Settings.defaultFontAsset != null)
                tmp.font = TMP_Settings.defaultFontAsset;
        }
        catch {}

        if (tmp.font == null)
        {
            var fallback = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (fallback != null)
                tmp.font = fallback;
        }

        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontWeight = weight;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        return rt;
    }
}
