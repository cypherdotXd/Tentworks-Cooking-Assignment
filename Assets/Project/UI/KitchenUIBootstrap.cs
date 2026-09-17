using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Runtime bootstrapper for the Kitchen UI in the Main scene.
/// If the scene already has a Kitchen UI Canvas assembled via the Editor tool,
/// this bootstrapper cleanly preserves it. Otherwise, it dynamically constructs
/// the full HUD, stations, order rail, and overlays matching the target mockup.
/// </summary>
public static class KitchenUIBootstrap
{
    private static Sprite _borderLarge;
    private static Sprite _borderSmall;
    private static Sprite _lettuce;
    private static Sprite _cheese;
    private static Sprite _meat;
    private static Sprite _chefAvatar;
    private static Sprite _chefPlayer;
    private static Sprite _circleBadge;
    private static Sprite _circleDot;
    private static Sprite _burnerRing;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BuildForMainScene()
    {
        if (SceneManager.GetActiveScene().name != "Main")
            return;

        if (Object.FindAnyObjectByType<KitchenUIReferences>() != null)
            return;

        LoadResources();
        EnsureEventSystem();
        CreateLayout();
    }

    private static void LoadResources()
    {
        _borderLarge = Resources.Load<Sprite>("UI/white-rounded-border-large");
        _borderSmall = Resources.Load<Sprite>("UI/white-rounded-border-small");
        _lettuce = Resources.Load<Sprite>("UI/lettuce");
        _cheese = Resources.Load<Sprite>("UI/cheese");
        _meat = Resources.Load<Sprite>("UI/meat");
        _chefAvatar = Resources.Load<Sprite>("UI/chef_avatar");
        _chefPlayer = Resources.Load<Sprite>("UI/chef_player");
        _circleBadge = Resources.Load<Sprite>("UI/circle_badge");
        _circleDot = Resources.Load<Sprite>("UI/circle_dot");
        _burnerRing = Resources.Load<Sprite>("UI/burner_ring");
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null)
            return;

        var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        Object.DontDestroyOnLoad(eventSystem);
    }

    private static void CreateLayout()
    {
        var canvasObject = new GameObject("Kitchen UI Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
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

        BuildKitchenFrame(root);
        BuildHeader(root, refs);
        BuildOrderRail(root, refs);
        BuildPlayerAndTooltip(root, refs);
        BuildStations(root, refs);
        BuildBottomLegend(root);
        BuildOverlays(root, refs);
    }

    private static void BuildKitchenFrame(RectTransform parent)
    {
        var bg = CreatePanel(parent, "Outer Dark Bezel", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.11f, 0.14f, 0.12f, 1f));
        bg.GetComponent<Image>().raycastTarget = false;

        var mat = CreatePanel(bg, "Kitchen Mat", Vector2.zero, Vector2.one, new Vector2(24f, 58f), new Vector2(-24f, -80f), new Color(0.44f, 0.39f, 0.33f, 1f));
        mat.GetComponent<Image>().raycastTarget = false;

        if (_borderLarge != null)
        {
            var border = CreateRect("Mat Border", mat, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var borderImg = border.gameObject.AddComponent<Image>();
            borderImg.sprite = _borderLarge;
            borderImg.type = Image.Type.Sliced;
            borderImg.color = new Color(0.23f, 0.20f, 0.17f, 1f);
            borderImg.raycastTarget = false;
        }

        var stripesContainer = CreateRect("Floor Grid Lines", mat, Vector2.zero, Vector2.one, new Vector2(20f, 20f), new Vector2(-20f, -20f));
        for (var i = 1; i <= 6; i++)
        {
            var frac = i / 7f;
            var line = CreateRect($"Stripe {i}", stripesContainer, new Vector2(frac, 0f), new Vector2(frac, 1f), new Vector2(-1f, 0f), new Vector2(1f, 0f));
            var img = line.gameObject.AddComponent<Image>();
            img.color = new Color(0.40f, 0.35f, 0.30f, 0.35f);
            img.raycastTarget = false;
        }
    }

    private static void BuildHeader(RectTransform parent, KitchenUIReferences refs)
    {
        var header = CreateRect("HUD Header Bar", parent, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -80f), Vector2.zero);
        var headerBg = header.gameObject.AddComponent<Image>();
        headerBg.color = new Color(0.11f, 0.14f, 0.12f, 1f);

        var brand = CreateRect("Branding", header, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(36f, -30f), new Vector2(380f, 30f));
        if (_chefAvatar != null)
        {
            var avatar = CreateRect("Chef Avatar", brand, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, -24f), new Vector2(48f, 24f));
            var img = avatar.gameObject.AddComponent<Image>();
            img.sprite = _chefAvatar;
            img.preserveAspect = true;
        }

        CreateLabel(brand, "App Title", "YES, CHEF!", 26, FontWeight.Bold, Color.white, TextAlignmentOptions.Left,
            new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(60f, 2f), new Vector2(0f, 28f));
        CreateLabel(brand, "App Subtitle", "Solo kitchen shift", 14, FontWeight.Regular, new Color(0.60f, 0.67f, 0.63f), TextAlignmentOptions.Left,
            new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(60f, -24f), new Vector2(0f, 0f));

        var stats = CreateRect("Stats & Controls", header, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-600f, -32f), new Vector2(-36f, 32f));

        refs.roundTimerLabel = CreateHeaderStat(stats, "Time Left Stat", "01:47", "TIME LEFT", 60f);
        refs.scoreLabel = CreateHeaderStat(stats, "Score Stat", "240", "SCORE", 210f);
        refs.highScoreLabel = CreateHeaderStat(stats, "High Score Stat", "510", "HIGH SCORE", 360f);

        var pauseBtnRect = CreateRect("Pause Button", stats, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(490f, -24f), new Vector2(538f, 24f));
        var pauseImg = pauseBtnRect.gameObject.AddComponent<Image>();
        pauseImg.color = new Color(0.12f, 0.17f, 0.15f, 1f);
        var pauseBtn = pauseBtnRect.gameObject.AddComponent<Button>();
        refs.pauseButton = pauseBtn;

        if (_borderSmall != null)
        {
            var pBorder = CreateRect("Border", pauseBtnRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var pBorderImg = pBorder.gameObject.AddComponent<Image>();
            pBorderImg.sprite = _borderSmall;
            pBorderImg.type = Image.Type.Sliced;
            pBorderImg.color = new Color(0.35f, 0.45f, 0.40f, 0.85f);
            pBorderImg.raycastTarget = false;
        }

        CreateLabel(pauseBtnRect, "Icon Text", "II", 18, FontWeight.Bold, Color.white, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
    }

    private static TextMeshProUGUI CreateHeaderStat(RectTransform parent, string name, string initialVal, string labelText, float posX)
    {
        var container = CreateRect(name, parent, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(posX - 60f, -28f), new Vector2(posX + 60f, 28f));
        var val = CreateLabel(container, "Value", initialVal, 28, FontWeight.Bold, Color.white, TextAlignmentOptions.Center,
            new Vector2(0f, 0.35f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        CreateLabel(container, "Label", labelText, 11, FontWeight.Bold, new Color(0.58f, 0.67f, 0.62f), TextAlignmentOptions.Center,
            new Vector2(0f, 0f), new Vector2(1f, 0.38f), Vector2.zero, Vector2.zero);
        return val;
    }

    private static void BuildOrderRail(RectTransform parent, KitchenUIReferences refs)
    {
        var rail = CreateRect("Order Rail", parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(50f, -730f), new Vector2(280f, -110f));

        var configs = new[]
        {
            new { Title = "WINDOW 1", Timer = "00:12", BorderCol = new Color(0.90f, 0.36f, 0.28f, 1f), Ingredients = new[] { _lettuce, _cheese } },
            new { Title = "WINDOW 2", Timer = "00:28", BorderCol = new Color(0.97f, 0.88f, 0.56f, 1f), Ingredients = new[] { _meat, _cheese } },
            new { Title = "WINDOW 3", Timer = "00:36", BorderCol = new Color(0.90f, 0.36f, 0.28f, 1f), Ingredients = new[] { _meat, _meat, _cheese } },
            new { Title = "WINDOW 4", Timer = "00:08", BorderCol = new Color(1.00f, 0.28f, 0.22f, 1f), Ingredients = new[] { _lettuce, _meat, _cheese } },
        };

        for (var i = 0; i < 4; i++)
        {
            var cfg = configs[i];
            var topOffset = -i * 152f;
            var bottomOffset = topOffset - 138f;

            var card = CreateRect($"Order Card {i + 1}", rail, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, bottomOffset), new Vector2(0f, topOffset));

            CreatePanel(card, "Shadow", Vector2.zero, Vector2.one, new Vector2(3f, -3f), new Vector2(3f, -3f), new Color(0.14f, 0.12f, 0.11f, 0.7f)).SetAsFirstSibling();
            CreatePanel(card, "Card Body", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.22f, 0.20f, 0.19f, 0.95f));

            Image borderImage = null;
            if (_borderLarge != null)
            {
                var borderObj = CreateRect("Card Border", card, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                borderImage = borderObj.gameObject.AddComponent<Image>();
                borderImage.sprite = _borderLarge;
                borderImage.type = Image.Type.Sliced;
                borderImage.color = cfg.BorderCol;
                borderImage.raycastTarget = false;

                if (i == 1)
                {
                    var glow = CreateRect("Selection Glow", card, Vector2.zero, Vector2.one, new Vector2(-3f, -3f), new Vector2(3f, 3f));
                    var glowImg = glow.gameObject.AddComponent<Image>();
                    glowImg.sprite = _borderLarge;
                    glowImg.type = Image.Type.Sliced;
                    glowImg.color = new Color(cfg.BorderCol.r, cfg.BorderCol.g, cfg.BorderCol.b, 0.45f);
                    glowImg.raycastTarget = false;
                }
            }

            var title = CreateLabel(card, "Window Label", cfg.Title, 13, FontWeight.Bold, new Color(0.60f, 0.57f, 0.55f), TextAlignmentOptions.Left,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -30f), new Vector2(-16f, -10f));

            var ingContainer = CreateRect("Ingredients Row", card, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(16f, -18f), new Vector2(-16f, 24f));
            for (var j = 0; j < cfg.Ingredients.Length; j++)
            {
                var sprite = cfg.Ingredients[j];
                var badgeRect = CreateRect($"Badge {j + 1}", ingContainer, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(j * 44f, -19f), new Vector2(j * 44f + 38f, 19f));
                var badgeImg = badgeRect.gameObject.AddComponent<Image>();
                badgeImg.sprite = _circleBadge;
                badgeImg.color = Color.white;

                if (sprite != null)
                {
                    var iconRect = CreateRect("Icon", badgeRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-13f, -13f), new Vector2(13f, 13f));
                    var iconImg = iconRect.gameObject.AddComponent<Image>();
                    iconImg.sprite = sprite;
                    iconImg.preserveAspect = true;
                    iconImg.raycastTarget = false;
                }
            }

            var timer = CreateLabel(card, "Timer Label", cfg.Timer, 22, FontWeight.Bold, cfg.BorderCol, TextAlignmentOptions.Left,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(16f, 10f), new Vector2(-16f, 36f));

            refs.orderCards[i] = new OrderCardUI
            {
                cardRoot = card.gameObject,
                borderImage = borderImage,
                windowTitle = title,
                ingredientsContainer = ingContainer,
                timerLabel = timer
            };
        }
    }

    private static void BuildPlayerAndTooltip(RectTransform parent, KitchenUIReferences refs)
    {
        var playerNode = CreateRect("Player Avatar Marker", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-70f, -80f), new Vector2(30f, 20f));
        if (_chefPlayer != null)
        {
            var pImg = playerNode.gameObject.AddComponent<Image>();
            pImg.sprite = _chefPlayer;
            pImg.preserveAspect = true;
            pImg.raycastTarget = false;
        }

        if (_cheese != null)
        {
            var heldCheese = CreateRect("Held Cheese Item", playerNode, new Vector2(1f, 0.25f), new Vector2(1f, 0.25f), new Vector2(-6f, -16f), new Vector2(26f, 16f));
            var cImg = heldCheese.gameObject.AddComponent<Image>();
            cImg.sprite = _cheese;
            cImg.preserveAspect = true;
            cImg.raycastTarget = false;
        }

        var tooltip = CreateRect("In Hand Tooltip", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-135f, -170f), new Vector2(95f, -126f));
        refs.heldItemPill = tooltip.gameObject;

        var pillBg = CreatePanel(tooltip, "Pill Background", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.22f, 0.21f, 0.19f, 0.95f));
        if (_borderSmall != null)
        {
            var b = CreateRect("Border", pillBg, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var bi = b.gameObject.AddComponent<Image>();
            bi.sprite = _borderSmall;
            bi.type = Image.Type.Sliced;
            bi.color = new Color(0.35f, 0.33f, 0.30f, 0.8f);
            bi.raycastTarget = false;
        }

        refs.heldItemLabel = CreateLabel(tooltip, "Tooltip Text", "In hand:        cheese", 15, FontWeight.Bold, new Color(0.92f, 0.92f, 0.92f), TextAlignmentOptions.Center,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        if (_cheese != null)
        {
            var tipIcon = CreateRect("Cheese Icon", tooltip, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-4f, -10f), new Vector2(16f, 10f));
            var tiImg = tipIcon.gameObject.AddComponent<Image>();
            tiImg.sprite = _cheese;
            tiImg.preserveAspect = true;
            tiImg.raycastTarget = false;
            refs.heldItemIcon = tiImg;
        }
    }

    private static void BuildStations(RectTransform parent, KitchenUIReferences refs)
    {
        var stationsRoot = CreateRect("Kitchen Stations HUD", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // PREP TABLE
        var prep = CreateStationCard(stationsRoot, "PREP TABLE Station", new Vector2(0.5f, 0.5f), new Vector2(170f, 60f), new Vector2(430f, 170f));
        refs.prepTablePanel = prep.gameObject;
        CreateLabel(prep, "Header", "PREP TABLE", 17, FontWeight.Bold, new Color(0.94f, 0.94f, 0.94f), TextAlignmentOptions.Center,
            new Vector2(0f, 0.5f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

        var prepStatus = CreateRect("Status Row", prep, new Vector2(0f, 0f), new Vector2(1f, 0.55f), Vector2.zero, Vector2.zero);
        if (_lettuce != null)
        {
            var pIcon = CreateRect("Lettuce Icon", prepStatus, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-80f, -11f), new Vector2(-58f, 11f));
            var pImg = pIcon.gameObject.AddComponent<Image>();
            pImg.sprite = _lettuce;
            pImg.preserveAspect = true;
        }
        refs.prepTableStatusLabel = CreateLabel(prepStatus, "Status Text", "chopping · 1.2s", 14, FontWeight.Regular, new Color(0.85f, 0.85f, 0.85f), TextAlignmentOptions.Left,
            new Vector2(0.5f, 0f), new Vector2(1f, 1f), new Vector2(-52f, 0f), Vector2.zero);

        // FRIDGE
        var fridge = CreateRect("FRIDGE Station", stationsRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(510f, 60f), new Vector2(670f, 270f));
        refs.fridgePanel = fridge.gameObject;
        CreatePanel(fridge, "Shadow", Vector2.zero, Vector2.one, new Vector2(3f, -3f), new Vector2(3f, -3f), new Color(0.12f, 0.18f, 0.22f, 0.7f)).SetAsFirstSibling();
        CreatePanel(fridge, "Body", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.29f, 0.45f, 0.55f, 1f));

        if (_borderLarge != null)
        {
            var fb = CreateRect("Border", fridge, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var fbi = fb.gameObject.AddComponent<Image>();
            fbi.sprite = _borderLarge;
            fbi.type = Image.Type.Sliced;
            fbi.color = new Color(0.18f, 0.29f, 0.36f, 1f);
            fbi.raycastTarget = false;
        }

        CreateLabel(fridge, "Title", "FRIDGE", 19, FontWeight.Bold, new Color(0.88f, 0.94f, 0.98f), TextAlignmentOptions.Center,
            new Vector2(0f, 0.72f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

        var handle = CreateRect("Door Handle", fridge, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-2f, -50f), new Vector2(2f, 50f));
        var handleImg = handle.gameObject.AddComponent<Image>();
        handleImg.color = new Color(0.88f, 0.94f, 0.98f, 0.95f);

        CreateLabel(fridge, "Subtitle", "raw ingredients", 13, FontWeight.Regular, new Color(0.68f, 0.78f, 0.85f), TextAlignmentOptions.Center,
            new Vector2(0f, 0f), new Vector2(1f, 0.25f), Vector2.zero, Vector2.zero);

        // STOVE
        var stove = CreateStationCard(stationsRoot, "STOVE Station", new Vector2(0.5f, 0.5f), new Vector2(160f, -250f), new Vector2(440f, -90f));
        refs.stovePanel = stove.gameObject;

        CreateLabel(stove, "Header", "STOVE", 17, FontWeight.Bold, new Color(0.94f, 0.94f, 0.94f), TextAlignmentOptions.Center,
            new Vector2(0f, 0.65f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

        var burnerL = CreateRect("Burner Left", stove, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-54f, -10f), new Vector2(-6f, 38f));
        var bImgL = burnerL.gameObject.AddComponent<Image>();
        bImgL.sprite = _burnerRing;
        bImgL.color = new Color(0.90f, 0.49f, 0.28f, 1f);
        refs.stoveBurnerLeft = bImgL;

        var burnerR = CreateRect("Burner Right", stove, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(6f, -10f), new Vector2(54f, 38f));
        var bImgR = burnerR.gameObject.AddComponent<Image>();
        bImgR.sprite = _burnerRing;
        bImgR.color = new Color(0.31f, 0.73f, 0.44f, 1f);
        refs.stoveBurnerRight = bImgR;

        var stoveStatus = CreateRect("Status Row", stove, new Vector2(0f, 0f), new Vector2(1f, 0.35f), Vector2.zero, Vector2.zero);
        if (_meat != null)
        {
            var mIcon = CreateRect("Meat Icon", stoveStatus, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-66f, -11f), new Vector2(-44f, 11f));
            var mImg = mIcon.gameObject.AddComponent<Image>();
            mImg.sprite = _meat;
            mImg.preserveAspect = true;
        }
        refs.stoveStatusLabel = CreateLabel(stoveStatus, "Status Text", "3.5s · ready", 14, FontWeight.Regular, new Color(0.85f, 0.85f, 0.85f), TextAlignmentOptions.Left,
            new Vector2(0.5f, 0f), new Vector2(1f, 1f), new Vector2(-38f, 0f), Vector2.zero);

        // TRASH
        var trash = CreateStationCard(stationsRoot, "TRASH Station", new Vector2(0.5f, 0.5f), new Vector2(530f, -250f), new Vector2(670f, -110f), new Color(0.28f, 0.25f, 0.22f, 0.95f));
        refs.trashPanel = trash.gameObject;
        CreateLabel(trash, "Header", "TRASH", 18, FontWeight.Bold, new Color(0.82f, 0.77f, 0.72f), TextAlignmentOptions.Center,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // Helper Banner
        var helper = CreateRect("Helper Banner", stationsRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(180f, -325f), new Vector2(670f, -265f));
        refs.helperMessagePanel = helper.gameObject;

        var helperBg = CreatePanel(helper, "Background", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.21f, 0.19f, 0.18f, 0.92f));
        if (_borderSmall != null)
        {
            var hb = CreateRect("Border", helperBg, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var hbi = hb.gameObject.AddComponent<Image>();
            hbi.sprite = _borderSmall;
            hbi.type = Image.Type.Sliced;
            hbi.color = new Color(0.35f, 0.32f, 0.30f, 0.8f);
            hbi.raycastTarget = false;
        }

        refs.helperMessageLabel = CreateLabel(helper, "Message Text", "Window 2 selected — deliver cheese next for a quick 12-point finish.", 14, FontWeight.Regular,
            new Color(0.84f, 0.84f, 0.84f), TextAlignmentOptions.Left, Vector2.zero, Vector2.one, new Vector2(16f, 0f), new Vector2(-16f, 0f));
    }

    private static RectTransform CreateStationCard(RectTransform parent, string name, Vector2 anchor, Vector2 offsetMin, Vector2 offsetMax, Color? cardColor = null)
    {
        var card = CreateRect(name, parent, anchor, anchor, offsetMin, offsetMax);
        CreatePanel(card, "Shadow", Vector2.zero, Vector2.one, new Vector2(3f, -3f), new Vector2(3f, -3f), new Color(0.14f, 0.12f, 0.11f, 0.7f)).SetAsFirstSibling();
        CreatePanel(card, "Body", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, cardColor ?? new Color(0.22f, 0.20f, 0.19f, 0.95f));

        if (_borderLarge != null)
        {
            var b = CreateRect("Border", card, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var bi = b.gameObject.AddComponent<Image>();
            bi.sprite = _borderLarge;
            bi.type = Image.Type.Sliced;
            bi.color = new Color(0.35f, 0.32f, 0.30f, 0.85f);
            bi.raycastTarget = false;
        }
        return card;
    }

    private static void BuildBottomLegend(RectTransform parent)
    {
        var legend = CreateRect("Bottom Legend Bar", parent, new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 54f));
        var lBg = legend.gameObject.AddComponent<Image>();
        lBg.color = new Color(0.11f, 0.14f, 0.12f, 1f);

        var row = CreateRect("Legend Row", legend, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(36f, -16f), new Vector2(-36f, 16f));

        var pos = 0f;
        AddLegendItem(row, ref pos, _circleDot, new Color(0.90f, 0.36f, 0.28f, 1f), "Urgent order timer", 180f);
        AddLegendItem(row, ref pos, _circleDot, new Color(0.31f, 0.73f, 0.44f, 1f), "Cooked and ready", 180f);
        AddLegendItem(row, ref pos, _lettuce, Color.white, "needs prep  ·", 135f);
        AddLegendItem(row, ref pos, _meat, Color.white, "needs stove  ·", 140f);
        AddLegendItem(row, ref pos, _cheese, Color.white, "serve directly", 140f);
    }

    private static void AddLegendItem(RectTransform parent, ref float curX, Sprite icon, Color iconColor, string text, float width)
    {
        var item = CreateRect("Item", parent, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(curX, -14f), new Vector2(curX + width, 14f));
        if (icon != null)
        {
            var iRect = CreateRect("Icon", item, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, -8f), new Vector2(16f, 8f));
            var img = iRect.gameObject.AddComponent<Image>();
            img.sprite = icon;
            img.color = iconColor;
            img.preserveAspect = true;
        }
        CreateLabel(item, "Text", text, 13, FontWeight.Regular, new Color(0.72f, 0.79f, 0.75f), TextAlignmentOptions.Left,
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(24f, 0f), Vector2.zero);

        curX += width + 20f;
    }

    private static void BuildOverlays(RectTransform parent, KitchenUIReferences refs)
    {
        // 1. Start Screen
        var startOverlay = CreateOverlay(parent, "Start Screen Overlay");
        refs.startPanel = startOverlay.gameObject;
        startOverlay.gameObject.SetActive(false);

        var startCard = CreateStationCard(startOverlay, "Start Card", new Vector2(0.5f, 0.5f), new Vector2(-320f, -200f), new Vector2(320f, 200f));
        CreateLabel(startCard, "Title", "YES, CHEF!", 48, FontWeight.Bold, Color.white, TextAlignmentOptions.Center, new Vector2(0f, 0.65f), new Vector2(1f, 0.95f), Vector2.zero, Vector2.zero);
        CreateLabel(startCard, "Subtitle", "RUN THE KITCHEN. FILL THE ORDERS. BEAT THE CLOCK.", 16, FontWeight.Bold, new Color(0.60f, 0.67f, 0.63f), TextAlignmentOptions.Center, new Vector2(0.05f, 0.52f), new Vector2(0.95f, 0.65f), Vector2.zero, Vector2.zero);
        CreateLabel(startCard, "Instructions", "Collect ingredients from the fridge.\nChop vegetables, cook meat, then deliver the right ingredients to each window.", 16, FontWeight.Regular, new Color(0.85f, 0.90f, 0.88f), TextAlignmentOptions.Center, new Vector2(0.1f, 0.28f), new Vector2(0.9f, 0.52f), Vector2.zero, Vector2.zero);

        var startBtn = CreateButton(startCard, "Start Button", "START SERVICE", new Vector2(0.5f, 0.14f), new Vector2(280f, 54f), new Color(0.16f, 0.55f, 0.38f, 1f));
        startBtn.onClick.AddListener(() => startOverlay.gameObject.SetActive(false));

        // 2. Pause Screen
        var pauseOverlay = CreateOverlay(parent, "Pause Screen Overlay");
        refs.pausePanel = pauseOverlay.gameObject;
        pauseOverlay.gameObject.SetActive(false);

        var pauseCard = CreateStationCard(pauseOverlay, "Pause Card", new Vector2(0.5f, 0.5f), new Vector2(-220f, -170f), new Vector2(220f, 170f));
        CreateLabel(pauseCard, "Title", "PAUSED", 38, FontWeight.Bold, Color.white, TextAlignmentOptions.Center, new Vector2(0f, 0.70f), new Vector2(1f, 0.92f), Vector2.zero, Vector2.zero);

        var resumeBtn = CreateButton(pauseCard, "Resume Button", "RESUME", new Vector2(0.5f, 0.50f), new Vector2(260f, 50f), new Color(0.16f, 0.55f, 0.38f, 1f));
        resumeBtn.onClick.AddListener(() => pauseOverlay.gameObject.SetActive(false));

        var restartBtn = CreateButton(pauseCard, "Restart Button", "RESTART ROUND", new Vector2(0.5f, 0.30f), new Vector2(260f, 44f), new Color(0.22f, 0.32f, 0.42f, 1f));
        restartBtn.onClick.AddListener(() => SceneManager.LoadScene("Main"));

        var quitBtn = CreateButton(pauseCard, "Quit Button", "QUIT GAME", new Vector2(0.5f, 0.12f), new Vector2(260f, 40f), new Color(0.48f, 0.20f, 0.22f, 1f));
        quitBtn.onClick.AddListener(Application.Quit);

        if (refs.pauseButton != null)
        {
            refs.pauseButton.onClick.AddListener(() => pauseOverlay.gameObject.SetActive(true));
        }

        // 3. Round Complete Screen
        var endOverlay = CreateOverlay(parent, "Round Complete Overlay");
        refs.endPanel = endOverlay.gameObject;
        endOverlay.gameObject.SetActive(false);

        var endCard = CreateStationCard(endOverlay, "Complete Card", new Vector2(0.5f, 0.5f), new Vector2(-250f, -170f), new Vector2(250f, 170f));
        CreateLabel(endCard, "Title", "SERVICE COMPLETE", 32, FontWeight.Bold, Color.white, TextAlignmentOptions.Center, new Vector2(0f, 0.68f), new Vector2(1f, 0.90f), Vector2.zero, Vector2.zero);
        refs.finalScoreLabel = CreateLabel(endCard, "Final Score", "FINAL SCORE  0000", 26, FontWeight.Bold, new Color(0.97f, 0.88f, 0.56f), TextAlignmentOptions.Center, new Vector2(0f, 0.48f), new Vector2(1f, 0.66f), Vector2.zero, Vector2.zero);
        refs.newHighScoreLabel = CreateLabel(endCard, "New High Score", "NEW HIGH SCORE!", 17, FontWeight.Bold, new Color(0.35f, 0.85f, 0.50f), TextAlignmentOptions.Center, new Vector2(0f, 0.34f), new Vector2(1f, 0.48f), Vector2.zero, Vector2.zero);

        var playAgainBtn = CreateButton(endCard, "Play Again Button", "PLAY AGAIN", new Vector2(0.5f, 0.14f), new Vector2(260f, 52f), new Color(0.16f, 0.55f, 0.38f, 1f));
        playAgainBtn.onClick.AddListener(() => SceneManager.LoadScene("Main"));
    }

    private static Button CreateButton(RectTransform parent, string name, string label, Vector2 anchor, Vector2 size, Color color)
    {
        var btnRect = CreatePanel(parent, name, anchor, anchor, -size * 0.5f, size * 0.5f, color);
        var btn = btnRect.gameObject.AddComponent<Button>();
        btn.targetGraphic = btnRect.GetComponent<Image>();
        CreateLabel(btnRect, "Label", label, 16, FontWeight.Bold, Color.white, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return btn;
    }

    private static RectTransform CreateOverlay(RectTransform parent, string name)
    {
        var overlay = CreateRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var img = overlay.gameObject.AddComponent<Image>();
        img.color = new Color(0.02f, 0.03f, 0.03f, 0.82f);
        return overlay;
    }

    private static RectTransform CreatePanel(RectTransform parent, string name, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax, Color color)
    {
        var panel = CreateRect(name, parent, min, max, offsetMin, offsetMax);
        var img = panel.gameObject.AddComponent<Image>();
        img.color = color;
        return panel;
    }

    private static TextMeshProUGUI CreateLabel(RectTransform parent, string name, string text, float size, FontWeight weight, Color color,
        TextAlignmentOptions alignment, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    {
        var node = CreateRect(name, parent, min, max, offsetMin, offsetMax);
        var tmp = node.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.font = TMP_Settings.defaultFontAsset;
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

[System.Serializable]
public class OrderCardUI
{
    public GameObject cardRoot;
    public Image borderImage;
    public TextMeshProUGUI windowTitle;
    public Transform ingredientsContainer;
    public TextMeshProUGUI timerLabel;
}

public sealed class KitchenUIReferences : MonoBehaviour
{
    [Header("HUD Header")]
    public TextMeshProUGUI scoreLabel;
    public TextMeshProUGUI highScoreLabel;
    public TextMeshProUGUI roundTimerLabel;
    public Button pauseButton;

    [Header("Orders")]
    public OrderCardUI[] orderCards = new OrderCardUI[4];

    [Header("Held Item")]
    public GameObject heldItemPill;
    public TextMeshProUGUI heldItemLabel;
    public Image heldItemIcon;

    [Header("Stations HUD")]
    public GameObject prepTablePanel;
    public TextMeshProUGUI prepTableStatusLabel;
    public GameObject fridgePanel;
    public GameObject stovePanel;
    public TextMeshProUGUI stoveStatusLabel;
    public Image stoveBurnerLeft;
    public Image stoveBurnerRight;
    public GameObject trashPanel;
    public GameObject helperMessagePanel;
    public TextMeshProUGUI helperMessageLabel;

    [Header("Overlays")]
    public GameObject startPanel;
    public GameObject pausePanel;
    public GameObject endPanel;
    public TextMeshProUGUI finalScoreLabel;
    public TextMeshProUGUI newHighScoreLabel;
}
