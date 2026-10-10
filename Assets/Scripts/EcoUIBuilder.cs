#if UNITY_EDITOR
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// ECOBOT > UI > Construir ...  Crea el Menú Principal, el Game Over y la Victoria dentro de tus escenas,
/// ya conectados a tu MenuManager y a tu GameManager.
public static class EcoUIBuilder
{
    const string GenDir = "Assets/ECOBOT_UI/Generated";
    const string EscenaMenu = "MenuPrincipal";
    const string EscenaJuego = "Demo_Scene_1";

    static readonly Color Cian = new Color(0.25f, 0.88f, 1f, 1f);
    static readonly Color Amarillo = new Color(1f, 0.85f, 0.2f, 1f);
    static readonly Color Verde = new Color(0.5f, 1f, 0.4f, 1f);
    static readonly Color Tinta = new Color(0.03f, 0.06f, 0.08f, 0.86f);
    static readonly Color Blanco = new Color(0.96f, 0.98f, 1f, 1f);
    static readonly Vector2 C = new Vector2(0.5f, 0.5f);

    static Sprite sFill, sBorder, sVignette, sHazard, sStar, sFondo, sGameOver, sTitulo;
    static TMP_FontAsset font;

    // ───────────────────────── MENÚS ─────────────────────────
    [MenuItem("ECOBOT/UI/Construir TODO (Menú + Game Over + Victoria)", priority = 0)]
    public static void BuildAll()
    {
        if (!Prepare()) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (OpenScene(EscenaMenu)) BuildMenu();
        if (OpenScene(EscenaJuego)) BuildEndScreens();
        Fin("¡Listo! Se crearon el Menú Principal, el Game Over y la Victoria.\n\nPrueba el juego: en Play pulsa F8 (Game Over) o F9 (Victoria) para verlos.");
    }

    [MenuItem("ECOBOT/UI/Construir solo Menú Principal", priority = 1)]
    public static void BuildMenuOnly()
    {
        if (!Prepare()) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (OpenScene(EscenaMenu)) BuildMenu();
        Fin("¡Menú Principal listo!");
    }

    [MenuItem("ECOBOT/UI/Construir solo Game Over + Victoria", priority = 2)]
    public static void BuildEndOnly()
    {
        if (!Prepare()) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (OpenScene(EscenaJuego)) BuildEndScreens();
        Fin("¡Game Over y Victoria listos!\n\nEn Play: F8 = Game Over, F9 = Victoria.");
    }

    static void Fin(string msg) => EditorUtility.DisplayDialog("ECOBOT UI", msg, "OK");

    // ───────────────────────── MENÚ PRINCIPAL ─────────────────────────
    static void BuildMenu()
    {
        RemoveOld("UI_Eco_Menu");

        // Oculta (no borra) el canvas viejo para que no se sobreponga
        foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects())
            if (go.GetComponent<Canvas>() != null && !go.name.StartsWith("UI_Eco")) go.SetActive(false);

        EnsureEventSystem();

        var mm = Object.FindFirstObjectByType<MenuManager>(FindObjectsInactive.Include);
        if (mm == null) mm = new GameObject("MenuController").AddComponent<MenuManager>();

        Canvas canvas = NewCanvas("UI_Eco_Menu", 50);
        var opciones = canvas.gameObject.AddComponent<EcoOpciones>();

        var (root, cont, fx) = NewScreenPanel(canvas.transform, "PanelPrincipal", sFondo, new Color(0f, 0.06f, 0.14f, 0.18f), 0.8f);

        // Banda detrás del título
        var banda = NewImage(cont, "Banda", null, new Color(0, 0, 0, 0.38f));
        StripX(banda.rectTransform, 290, 380);

        RectTransform titulo;
        if (sTitulo != null)
        {
            var ti = NewImage(cont, "Titulo", sTitulo, Color.white);
            ti.preserveAspect = true;
            Anchor(ti.rectTransform, C, new Vector2(0, 300), new Vector2(1100, 280));
            titulo = ti.rectTransform;
        }
        else
        {
            var tt = NewText(cont, "Titulo", "ECOBOT", 190, Cian, FontStyles.Bold, TextAlignmentOptions.Center, 30);
            Anchor(tt.rectTransform, C, new Vector2(0, 325), new Vector2(1400, 230));
            titulo = tt.rectTransform;
        }
        var sub = NewText(cont, "Subtitulo", "RECICLA  ·  PROTEGE  ·  SOBREVIVE", 40, Blanco, FontStyles.Bold, TextAlignmentOptions.Center, 14);
        Anchor(sub.rectTransform, C, new Vector2(0, 190), new Vector2(1400, 70));

        var bJugar = NewButton(cont, "BotonJugar", "JUGAR", Cian, new Vector2(0, 20), new Vector2(580, 96), null, 50);
        var bOpc = NewButton(cont, "BotonOpciones", "OPCIONES", Cian, new Vector2(0, -105), new Vector2(580, 96), null, 50);
        var bSalir = NewButton(cont, "BotonSalir", "SALIR", Cian, new Vector2(0, -230), new Vector2(580, 96), null, 50);

        var pie = NewText(cont, "Pie", "E: RECOGER / DEPOSITAR   ·   ESPACIO: SALTAR   ·   RECICLA BIEN PARA SUMAR PUNTOS", 26, new Color(1, 1, 1, 0.65f), FontStyles.Bold, TextAlignmentOptions.Center, 4);
        Anchor(pie.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 50), new Vector2(1700, 50));

        fx.titulo = titulo;
        fx.elementosEscalonados = new[] { titulo, sub.rectTransform, bJugar.GetComponent<RectTransform>(), bOpc.GetComponent<RectTransform>(), bSalir.GetComponent<RectTransform>() };

        // ── Panel de opciones ──
        var opts = BuildOptions(canvas.transform, opciones);

        UnityEventTools.AddPersistentListener(bJugar.onClick, mm.CargarJuego);
        UnityEventTools.AddPersistentListener(bOpc.onClick, mm.AbrirOpciones);
        UnityEventTools.AddPersistentListener(bSalir.onClick, mm.SalirDelJuego);

        mm.panelOpciones = opts;
        opts.SetActive(false);
        EditorUtility.SetDirty(mm);

        GuardarEscena();
    }

    static GameObject BuildOptions(Transform parent, EcoOpciones opciones)
    {
        var root = NewRT("PanelOpciones", parent);
        Stretch(root);
        root.gameObject.AddComponent<CanvasGroup>();
        var fx = root.gameObject.AddComponent<EcoUIPanel>();

        var dim = NewImage(root, "Oscurecer", null, new Color(0, 0, 0, 0.72f), false, 1, true);
        Stretch(dim.rectTransform);

        var win = NewImage(root, "Ventana", sFill, Tinta, true, 0.5f, true);
        Anchor(win.rectTransform, C, Vector2.zero, new Vector2(920, 660));
        var bord = NewImage(win.transform, "Borde", sBorder, Cian, true, 0.5f);
        Stretch(bord.rectTransform);
        fx.contenido = win.rectTransform;

        var tit = NewText(win.transform, "Titulo", "OPCIONES", 66, Cian, FontStyles.Bold, TextAlignmentOptions.Center, 10);
        Anchor(tit.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -78), new Vector2(800, 90));

        // Fila 1: Volumen
        var l1 = NewText(win.transform, "EtVolumen", "VOLUMEN", 40, Blanco, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, 4);
        Anchor(l1.rectTransform, C, new Vector2(-250, 110), new Vector2(320, 60));
        var slider = NewSlider(win.transform, new Vector2(110, 110), new Vector2(380, 44), Cian);
        var pct = NewText(win.transform, "TextoVolumen", "100%", 38, Cian, FontStyles.Bold, TextAlignmentOptions.Midline, 2);
        Anchor(pct.rectTransform, C, new Vector2(355, 110), new Vector2(110, 60));

        // Fila 2: Calidad
        var l2 = NewText(win.transform, "EtCalidad", "CALIDAD", 40, Blanco, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, 4);
        Anchor(l2.rectTransform, C, new Vector2(-250, 0), new Vector2(320, 60));
        var bMenos = NewButton(win.transform, "CalidadMenos", "<", Cian, new Vector2(-50, 0), new Vector2(72, 72), null, 44);
        var calTxt = NewText(win.transform, "TextoCalidad", "ALTA", 36, Cian, FontStyles.Bold, TextAlignmentOptions.Midline, 2);
        Anchor(calTxt.rectTransform, C, new Vector2(150, 0), new Vector2(250, 60));
        var bMas = NewButton(win.transform, "CalidadMas", ">", Cian, new Vector2(345, 0), new Vector2(72, 72), null, 44);

        // Fila 3: Pantalla completa
        var l3 = NewText(win.transform, "EtPantalla", "PANTALLA COMPLETA", 40, Blanco, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, 4);
        Anchor(l3.rectTransform, C, new Vector2(-170, -110), new Vector2(520, 60));
        var tog = NewToggle(win.transform, new Vector2(345, -110), Cian);

        // Volver
        var bVolver = NewButton(win.transform, "BotonVolver", "VOLVER", Cian, new Vector2(0, -235), new Vector2(420, 90), null, 46);

        fx.elementosEscalonados = new RectTransform[0];

        opciones.volumen = slider;
        opciones.textoVolumen = pct;
        opciones.textoCalidad = calTxt;
        opciones.pantallaCompleta = tog;
        EditorUtility.SetDirty(opciones);

        UnityEventTools.AddPersistentListener(bMenos.onClick, opciones.CalidadMenos);
        UnityEventTools.AddPersistentListener(bMas.onClick, opciones.CalidadMas);

        var mm = Object.FindFirstObjectByType<MenuManager>(FindObjectsInactive.Include);
        if (mm != null) UnityEventTools.AddPersistentListener(bVolver.onClick, mm.CerrarOpciones);

        return root.gameObject;
    }

    // ───────────────────────── GAME OVER + VICTORIA ─────────────────────────
    static void BuildEndScreens()
    {
        RemoveOld("UI_Eco_FinJuego");
        EnsureEventSystem();

        var gm = Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
        if (gm == null) gm = new GameObject("GameManager").AddComponent<GameManager>();

        Canvas canvas = NewCanvas("UI_Eco_FinJuego", 200);

        // ═══ GAME OVER ═══
        {
            var (root, cont, fx) = NewScreenPanel(canvas.transform, "PanelGameOver", sGameOver, new Color(0.12f, 0.26f, 0f, 0.30f), 0.9f);

            var banda = NewImage(cont, "Banda", null, new Color(0, 0, 0, 0.45f));
            StripX(banda.rectTransform, 255, 250);

            var t1 = NewText(cont, "Titulo", "¡HEMOS PERDIDO EL CORE!", 96, new Color(1f, 0.97f, 0.85f), FontStyles.Bold, TextAlignmentOptions.Center, 4);
            Anchor(t1.rectTransform, C, new Vector2(0, 300), new Vector2(1800, 130));
            var t2 = NewText(cont, "Subtitulo", "HAS SIDO DERROTADO", 62, Amarillo, FontStyles.Bold, TextAlignmentOptions.Center, 10);
            Anchor(t2.rectTransform, C, new Vector2(0, 205), new Vector2(1400, 90));

            var plate = NewStatsPlate(cont, new Vector2(0, 50), new Vector2(620, 160), Amarillo, out var pts, out var rec);
            var b1 = NewButton(cont, "BotonReintentar", "REINTENTAR", Amarillo, new Vector2(0, -120), new Vector2(620, 98), sHazard, 50);
            var b2 = NewButton(cont, "BotonMenu", "MENÚ PRINCIPAL", Amarillo, new Vector2(0, -240), new Vector2(620, 98), sHazard, 50);

            UnityEventTools.AddPersistentListener(b1.onClick, gm.Reintentar);
            UnityEventTools.AddPersistentListener(b2.onClick, gm.IrAlMenuPrincipal);

            fx.titulo = t1.rectTransform;
            fx.elementosEscalonados = new[] { t1.rectTransform, t2.rectTransform, plate, b1.GetComponent<RectTransform>(), b2.GetComponent<RectTransform>() };

            gm.panelGameOver = root.gameObject;
            gm.textoPuntosGameOver = pts;
            gm.textoRecordGameOver = rec;
            root.gameObject.SetActive(false);
        }

        // ═══ VICTORIA ═══
        {
            var (root, cont, fx) = NewScreenPanel(canvas.transform, "PanelVictoria", sFondo, new Color(0.05f, 0.45f, 0.25f, 0.22f), 0.75f);

            var banda = NewImage(cont, "Banda", null, new Color(0, 0.08f, 0.04f, 0.5f));
            StripX(banda.rectTransform, 290, 250);

            var t1 = NewText(cont, "Titulo", "¡MISIÓN CUMPLIDA!", 112, Verde, FontStyles.Bold, TextAlignmentOptions.Center, 8);
            Anchor(t1.rectTransform, C, new Vector2(0, 330), new Vector2(1800, 150));
            var t2 = NewText(cont, "Subtitulo", "LA CIUDAD ESTÁ MÁS LIMPIA GRACIAS A ECOBOT", 44, Blanco, FontStyles.Bold, TextAlignmentOptions.Center, 6);
            Anchor(t2.rectTransform, C, new Vector2(0, 245), new Vector2(1700, 70));

            var estrellas = new Image[3];
            var rects = new System.Collections.Generic.List<RectTransform> { t1.rectTransform, t2.rectTransform };
            for (int i = 0; i < 3; i++)
            {
                bool centro = i == 1;
                var s = NewImage(cont, "Estrella" + (i + 1), sStar, Amarillo);
                Anchor(s.rectTransform, C, new Vector2((i - 1) * 160, centro ? 135 : 118), centro ? new Vector2(150, 150) : new Vector2(120, 120));
                estrellas[i] = s;
                rects.Add(s.rectTransform);
            }

            var plate = NewStatsPlate(cont, new Vector2(0, -35), new Vector2(620, 150), Verde, out var pts, out var rec);
            var b1 = NewButton(cont, "BotonJugarDeNuevo", "JUGAR DE NUEVO", Verde, new Vector2(0, -190), new Vector2(620, 98), sStar, 50);
            var b2 = NewButton(cont, "BotonMenu", "MENÚ PRINCIPAL", Verde, new Vector2(0, -310), new Vector2(620, 98), sStar, 50);

            UnityEventTools.AddPersistentListener(b1.onClick, gm.Reintentar);
            UnityEventTools.AddPersistentListener(b2.onClick, gm.IrAlMenuPrincipal);

            rects.Add(plate);
            rects.Add(b1.GetComponent<RectTransform>());
            rects.Add(b2.GetComponent<RectTransform>());
            fx.titulo = t1.rectTransform;
            fx.elementosEscalonados = rects.ToArray();

            gm.panelVictoria = root.gameObject;
            gm.textoPuntosVictoria = pts;
            gm.textoRecordVictoria = rec;
            gm.estrellasVictoria = estrellas;
            root.gameObject.SetActive(false);
        }

        EditorUtility.SetDirty(gm);
        GuardarEscena();
    }

    static RectTransform NewStatsPlate(Transform parent, Vector2 pos, Vector2 size, Color accent, out TextMeshProUGUI puntos, out TextMeshProUGUI record)
    {
        var plate = NewImage(parent, "PlacaPuntos", sFill, Tinta, true, 0.5f);
        Anchor(plate.rectTransform, C, pos, size);
        var b = NewImage(plate.transform, "Borde", sBorder, Dim(accent, 0.8f), true, 0.5f);
        Stretch(b.rectTransform);
        puntos = NewText(plate.transform, "TextoPuntos", "PUNTOS: 0", 56, Blanco, FontStyles.Bold, TextAlignmentOptions.Center, 6);
        Anchor(puntos.rectTransform, C, new Vector2(0, size.y * 0.17f), new Vector2(size.x - 40, 70));
        record = NewText(plate.transform, "TextoRecord", "RÉCORD: 0", 36, accent, FontStyles.Bold, TextAlignmentOptions.Center, 6);
        Anchor(record.rectTransform, C, new Vector2(0, -size.y * 0.22f), new Vector2(size.x - 40, 50));
        return plate.rectTransform;
    }

    // ───────────────────────── PIEZAS DE UI ─────────────────────────
    static (RectTransform root, RectTransform cont, EcoUIPanel fx) NewScreenPanel(Transform parent, string name, Sprite bg, Color overlay, float vignette)
    {
        var root = NewRT(name, parent);
        Stretch(root);
        root.gameObject.AddComponent<CanvasGroup>();
        var fx = root.gameObject.AddComponent<EcoUIPanel>();
        NewBackground(root, bg, overlay, vignette);
        var cont = NewRT("Contenido", root);
        Stretch(cont);
        fx.contenido = cont;
        return (root, cont, fx);
    }

    static void NewBackground(Transform parent, Sprite bg, Color overlay, float vignetteAlpha)
    {
        var holder = NewRT("Fondo", parent);
        Stretch(holder);
        holder.gameObject.AddComponent<RectMask2D>();

        if (bg != null)
        {
            var img = NewImage(holder, "Imagen", bg, Color.white);
            var r = img.rectTransform;
            r.anchorMin = r.anchorMax = C; r.pivot = C; r.anchoredPosition = Vector2.zero; r.sizeDelta = new Vector2(1920, 1080);
            var fit = img.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = bg.rect.width / bg.rect.height;
        }
        else
        {
            var solid = NewImage(holder, "Color", null, new Color(0.04f, 0.08f, 0.1f));
            Stretch(solid.rectTransform);
        }
        var ov = NewImage(holder, "Tinte", null, overlay);
        Stretch(ov.rectTransform);
        var vg = NewImage(holder, "Vineta", sVignette, new Color(0, 0, 0, vignetteAlpha));
        Stretch(vg.rectTransform);
    }

    static Button NewButton(Transform parent, string name, string label, Color accent, Vector2 pos, Vector2 size, Sprite icon, float fontSize)
    {
        var fill = NewImage(parent, name, sFill, Tinta, true, 0.5f, true);
        Anchor(fill.rectTransform, C, pos, size);

        var btn = fill.gameObject.AddComponent<Button>();
        btn.targetGraphic = fill;
        btn.transition = Selectable.Transition.None;

        var borde = NewImage(fill.transform, "Borde", sBorder, Dim(accent, 0.85f), true, 0.5f);
        Stretch(borde.rectTransform);

        var txt = NewText(fill.transform, "Texto", label, fontSize, accent, FontStyles.Bold, TextAlignmentOptions.Center, 6);
        Stretch(txt.rectTransform, 12, 4, 12, 4);

        if (icon != null)
        {
            var iz = NewImage(fill.transform, "IconoIzq", icon, accent);
            Anchor(iz.rectTransform, new Vector2(0f, 0.5f), new Vector2(-52, 0), new Vector2(60, 60));
            var de = NewImage(fill.transform, "IconoDer", icon, accent);
            Anchor(de.rectTransform, new Vector2(1f, 0.5f), new Vector2(52, 0), new Vector2(60, 60));
        }

        var fx = fill.gameObject.AddComponent<EcoButtonFX>();
        fx.borde = borde; fx.bordeNormal = Dim(accent, 0.85f); fx.bordeHover = Color.white;
        fx.fondo = fill; fx.fondoNormal = Tinta;
        fx.fondoHover = new Color(accent.r * 0.30f + 0.03f, accent.g * 0.30f + 0.03f, accent.b * 0.30f + 0.03f, 0.95f);
        fx.texto = txt; fx.textoNormal = accent; fx.textoHover = Color.white;
        return btn;
    }

    static Slider NewSlider(Transform parent, Vector2 pos, Vector2 size, Color accent)
    {
        var root = NewRT("Slider", parent);
        Anchor(root, C, pos, size);

        var bg = NewImage(root, "Fondo", null, new Color(0.10f, 0.16f, 0.18f, 1f), false, 1, true);
        bg.rectTransform.anchorMin = new Vector2(0, 0.5f);
        bg.rectTransform.anchorMax = new Vector2(1, 0.5f);
        bg.rectTransform.sizeDelta = new Vector2(0, 16);
        bg.rectTransform.anchoredPosition = Vector2.zero;

        var fillArea = NewRT("FillArea", root);
        fillArea.anchorMin = new Vector2(0, 0.5f);
        fillArea.anchorMax = new Vector2(1, 0.5f);
        fillArea.sizeDelta = new Vector2(-20, 16);
        fillArea.anchoredPosition = Vector2.zero;
        var fill = NewImage(fillArea, "Fill", null, accent);
        fill.rectTransform.anchorMin = Vector2.zero;
        fill.rectTransform.anchorMax = Vector2.one;
        fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;

        var handleArea = NewRT("HandleArea", root);
        Stretch(handleArea, 12, 0, 12, 0);
        var handle = NewImage(handleArea, "Handle", null, Color.white, false, 1, true);
        handle.rectTransform.anchorMin = new Vector2(0, 0);
        handle.rectTransform.anchorMax = new Vector2(0, 1);
        handle.rectTransform.sizeDelta = new Vector2(24, 0);

        var s = root.gameObject.AddComponent<Slider>();
        s.fillRect = fill.rectTransform;
        s.handleRect = handle.rectTransform;
        s.targetGraphic = handle;
        s.direction = Slider.Direction.LeftToRight;
        s.minValue = 0f; s.maxValue = 1f; s.value = 1f;
        return s;
    }

    static Toggle NewToggle(Transform parent, Vector2 pos, Color accent)
    {
        var bg = NewImage(parent, "Toggle", sFill, new Color(0.05f, 0.09f, 0.11f, 1f), true, 0.5f, true);
        Anchor(bg.rectTransform, C, pos, new Vector2(72, 72));
        var bord = NewImage(bg.transform, "Borde", sBorder, accent, true, 0.5f);
        Stretch(bord.rectTransform);
        var chk = NewImage(bg.transform, "Check", null, accent);
        Stretch(chk.rectTransform, 20, 20, 20, 20);

        var t = bg.gameObject.AddComponent<Toggle>();
        t.targetGraphic = bg;
        t.graphic = chk;
        t.transition = Selectable.Transition.None;
        t.isOn = true;
        return t;
    }

    // ───────────────────────── HELPERS ─────────────────────────
    static RectTransform NewRT(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    static void Stretch(RectTransform rt, float l = 0, float b = 0, float r = 0, float t = 0)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
    }

    static void Anchor(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = anchor; rt.pivot = C;
        rt.anchoredPosition = pos; rt.sizeDelta = size;
    }

    /// Franja horizontal que ocupa todo el ancho, centrada en y con alto h.
    static void StripX(RectTransform rt, float y, float h)
    {
        rt.anchorMin = new Vector2(0, 0.5f); rt.anchorMax = new Vector2(1, 0.5f); rt.pivot = C;
        rt.anchoredPosition = new Vector2(0, y); rt.sizeDelta = new Vector2(0, h);
    }

    static Image NewImage(Transform parent, string name, Sprite s, Color c, bool sliced = false, float ppuMul = 1f, bool raycast = false)
    {
        var rt = NewRT(name, parent);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = s; img.color = c; img.raycastTarget = raycast;
        if (sliced && s != null) { img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = ppuMul; }
        return img;
    }

    static TextMeshProUGUI NewText(Transform parent, string name, string text, float size, Color c, FontStyles style, TextAlignmentOptions al, float spacing)
    {
        var rt = NewRT(name, parent);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.font = font; t.text = text; t.fontSize = size; t.color = c;
        t.fontStyle = style; t.alignment = al; t.characterSpacing = spacing;
        t.raycastTarget = false;
        return t;
    }

    static Color Dim(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, 1f);

    static Canvas NewCanvas(string name, int order)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.layer = LayerMask.NameToLayer("UI");
        var c = go.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = order;
        var s = go.GetComponent<CanvasScaler>();
        s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        s.referenceResolution = new Vector2(1920, 1080);
        s.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        s.matchWidthOrHeight = 0.5f;
        return c;
    }

    static void RemoveOld(string prefix)
    {
        var olds = SceneManager.GetActiveScene().GetRootGameObjects().Where(g => g.name.StartsWith(prefix)).ToArray();
        foreach (var g in olds) Object.DestroyImmediate(g);
    }

    static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;
        var go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        var m = go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        m.AssignDefaultActions();
#else
        go.AddComponent<StandaloneInputModule>();
#endif
    }

    static void GuardarEscena()
    {
        var sc = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(sc);
        EditorSceneManager.SaveScene(sc);
    }

    static bool OpenScene(string name)
    {
        foreach (var guid in AssetDatabase.FindAssets(name + " t:Scene"))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(p) == name)
            {
                EditorSceneManager.OpenScene(p, OpenSceneMode.Single);
                return true;
            }
        }
        EditorUtility.DisplayDialog("ECOBOT UI", "No encontré la escena '" + name + "'. Revisa los nombres al inicio de EcoUIBuilder.cs", "OK");
        return false;
    }

    // ───────────────────────── RECURSOS ─────────────────────────
    static bool Prepare()
    {
        font = TMP_Settings.defaultFontAsset;
        if (font == null)
        {
            EditorUtility.DisplayDialog("ECOBOT UI", "Falta TextMeshPro: Window > TextMeshPro > Import TMP Essential Resources.", "OK");
            return false;
        }
        Directory.CreateDirectory(GenDir);
        sFill = MakeSprite("eco_fill", 64, FillPx, new Vector4(16, 16, 16, 16));
        sBorder = MakeSprite("eco_border", 64, BorderPx, new Vector4(16, 16, 16, 16));
        sVignette = MakeSprite("eco_vineta", 256, VignettePx, Vector4.zero);
        sHazard = MakeSprite("eco_peligro", 64, HazardPx, Vector4.zero);
        sStar = MakeSprite("eco_estrella", 64, StarPx, Vector4.zero);

        sFondo = FindSprite("fondo");
        sGameOver = FindSprite("game over");
        if (sGameOver == null) sGameOver = sFondo;
        sTitulo = FindSprite("titulo");
        return true;
    }

    static Sprite FindSprite(string fileName)
    {
        foreach (var guid in AssetDatabase.FindAssets(fileName + " t:Texture2D"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.StartsWith("Assets/")) continue;
            if (Path.GetFileNameWithoutExtension(path).ToLower() != fileName.ToLower()) continue;

            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null && imp.textureType != TextureImporterType.Sprite)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.SaveAndReimport();
            }
            var sp = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
            if (sp != null) return sp;
        }
        return null;
    }

    static Sprite MakeSprite(string name, int size, System.Func<int, int, int, int, Color> fn, Vector4 border)
    {
        string path = GenDir + "/" + name + ".png";
        if (!File.Exists(path))
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    px[y * size + x] = fn(x, y, size, size);
            tex.SetPixels(px);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spriteBorder = border;
            imp.filterMode = FilterMode.Bilinear;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // Rectángulo con esquinas cortadas (estilo sci-fi)
    static bool InChamfer(int x, int y, int w, int h, int c)
    {
        int rx = w - 1 - x, ry = h - 1 - y;
        return x + y >= c && rx + y >= c && x + ry >= c && rx + ry >= c;
    }

    static Color FillPx(int x, int y, int w, int h) => InChamfer(x, y, w, h, 14) ? Color.white : Color.clear;

    static Color BorderPx(int x, int y, int w, int h)
    {
        const int t = 4;
        if (!InChamfer(x, y, w, h, 14)) return Color.clear;
        bool inner = x >= t && y >= t && x < w - t && y < h - t && InChamfer(x, y, w, h, 20);
        return inner ? Color.clear : Color.white;
    }

    static Color VignettePx(int x, int y, int w, int h)
    {
        float dx = (x + 0.5f) / w * 2f - 1f, dy = (y + 0.5f) / h * 2f - 1f;
        float d = Mathf.Sqrt(dx * dx + dy * dy);
        float a = Mathf.Clamp01((d - 0.45f) / 0.9f);
        return new Color(1, 1, 1, a * a);
    }

    // Triángulo de advertencia con "!"
    static Color HazardPx(int x, int y, int w, int h)
    {
        float sx = x + 0.5f, sy = (h - 1 - y) + 0.5f;
        float half = (sy - 6f) * (28f / 50f);
        bool outer = sy >= 6f && sy <= 56f && Mathf.Abs(sx - 32f) <= half;
        if (!outer) return Color.clear;
        bool inner = sy >= 14f && sy <= 50f && Mathf.Abs(sx - 32f) <= half - 7f;
        if (inner)
        {
            bool barra = Mathf.Abs(sx - 32f) <= 2.7f && sy >= 28f && sy <= 42f;
            bool punto = Mathf.Abs(sx - 32f) <= 2.7f && sy >= 46f && sy <= 50f;
            return (barra || punto) ? Color.white : Color.clear;
        }
        return Color.white;
    }

    // Estrella de 5 puntas
    static Color StarPx(int x, int y, int w, int h)
    {
        float sx = x + 0.5f, sy = (h - 1 - y) + 0.5f;
        var p = new Vector2[10];
        for (int i = 0; i < 10; i++)
        {
            float a = -Mathf.PI / 2f + i * Mathf.PI / 5f;
            float rad = (i % 2 == 0) ? w * 0.48f : w * 0.20f;
            p[i] = new Vector2(w / 2f + Mathf.Cos(a) * rad, h / 2f + 2f + Mathf.Sin(a) * rad);
        }
        bool inside = false;
        for (int i = 0, j = p.Length - 1; i < p.Length; j = i++)
        {
            if ((p[i].y > sy) != (p[j].y > sy) &&
                sx < (p[j].x - p[i].x) * (sy - p[i].y) / (p[j].y - p[i].y) + p[i].x)
                inside = !inside;
        }
        return inside ? Color.white : Color.clear;
    }
}
#endif
