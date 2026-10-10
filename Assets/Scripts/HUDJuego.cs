using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// HUD completo del juego, armado por código (no hay que dibujar nada a mano).
/// - Vidas (corazones) arriba a la izquierda
/// - Puntos con moneda arriba a la derecha
/// - Mochila con 3 slots abajo (muestra el NOMBRE de cada basura)
/// - Avisos (toast), aviso de "Presiona E" y flash rojo al recibir daño
///
/// Se crea solo cuando carga una escena que tenga un EcoBotInventario.
/// Si quieres, también puedes ponerlo manualmente en un objeto vacío.
/// </summary>
public class HUDJuego : MonoBehaviour
{
    public static HUDJuego Instance;

    [Header("Sprites (opcional - si están vacíos se buscan solos)")]
    public Sprite spriteCorazon;
    public Sprite spriteMonedas;
    public Sprite spriteMochila;
    public Sprite spriteManzana;

    [Header("Fuente (opcional - vacío = la de TextMeshPro por defecto)")]
    public TMP_FontAsset fuente;

    [Header("Opciones")]
    public bool ocultarHUDViejo = true;
    public string[] nombresHUDViejo = { "Panel_Inventario", "ContenedorVida", "HUD_Panel", "Button" };
    public int corazonesMaximos = 3;
    public float duracionAviso = 1.8f;

    // ---------- Colores del diseño ----------
    private readonly Color colFondo = new Color(0.07f, 0.12f, 0.10f, 0.88f);
    private readonly Color colBorde = new Color(0.45f, 0.85f, 0.60f, 1f);
    private readonly Color colBordeApagado = new Color(0.30f, 0.38f, 0.34f, 1f);
    private readonly Color colTextoSuave = new Color(0.75f, 0.92f, 0.82f, 1f);
    private readonly Color colCorazonPerdido = new Color(0.22f, 0.22f, 0.24f, 0.75f);

    // ---------- Referencias internas ----------
    private Canvas canvas;
    private CanvasGroup grupoRaiz;
    private Sprite sRedondo, sCirculo, sCorazonGen, sVineta;
    private bool corazonEsDelKit;

    private RectTransform panelVidas;
    private Image[] corazones;
    private Vector2[] posCorazones;

    private RectTransform panelPuntos;
    private TextMeshProUGUI textoPuntos;

    private CanvasGroup grupoInventario;
    private TextMeshProUGUI textoContador;
    private SlotUI[] slots;

    private RectTransform toastRect;
    private CanvasGroup grupoToast;
    private TextMeshProUGUI toastTexto;
    private Image toastBorde;
    private Vector2 toastPosBase;
    private Coroutine rutinaToast;

    private CanvasGroup grupoPrompt;
    private RectTransform promptRect;
    private TextMeshProUGUI promptTecla;
    private TextMeshProUGUI promptTexto;
    private float promptObjetivo = 0f;

    private Image vineta;
    private Coroutine rutinaVineta;

    private EcoBotInventario inv;
    private int vidasPrevias = -1;
    private int puntosObjetivoPrevio = int.MinValue;
    private float puntosMostrados = 0f;
    private bool inventarioVisible = true;
    private float inventarioAlfaObjetivo = 1f;

    private class SlotUI
    {
        public RectTransform raiz;
        public Image borde;
        public Image relleno;
        public Image circulo;
        public Image icono;
        public Image banda;
        public Image flash;
        public TextMeshProUGUI letra;
        public TextMeshProUGUI nombre;
        public TextMeshProUGUI categoria;
        public TextMeshProUGUI vacio;
        public string nombreActual = "";
        public string categoriaActual = "";
    }

    // =====================================================================
    //  CREACIÓN AUTOMÁTICA
    // =====================================================================
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void IniciarSistema()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
        SceneManager.sceneLoaded += AlCargarEscena;
        CrearSiHaceFalta();
    }

    private static void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        CrearSiHaceFalta();
    }

    private static void CrearSiHaceFalta()
    {
        if (Instance != null) return;
        if (FindFirstObjectByType<EcoBotInventario>() == null) return;

        GameObject go = new GameObject("HUD_Juego");
        go.AddComponent<HUDJuego>();
    }

    // =====================================================================
    //  CICLO DE VIDA
    // =====================================================================
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        CargarSprites();
        GenerarSpritesPropios();
        Construir();

        if (ocultarHUDViejo) OcultarHUDViejo();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;

        if (inv == null) inv = FindFirstObjectByType<EcoBotInventario>();

        if (inv != null)
        {
            ActualizarVidas();
            ActualizarSlots();
        }

        ActualizarPuntos(dt);

        // Mostrar / ocultar mochila y aviso con fundido suave
        if (grupoInventario != null)
            grupoInventario.alpha = Mathf.MoveTowards(grupoInventario.alpha, inventarioAlfaObjetivo, dt * 6f);

        if (grupoPrompt != null)
        {
            grupoPrompt.alpha = Mathf.MoveTowards(grupoPrompt.alpha, promptObjetivo, dt * 8f);
        }
    }

    // =====================================================================
    //  API PÚBLICA (la usan los otros scripts)
    // =====================================================================
    public void MostrarMensaje(string texto, Color color)
    {
        if (toastRect == null) return;
        if (rutinaToast != null) StopCoroutine(rutinaToast);
        rutinaToast = StartCoroutine(RutinaToast(texto, color));
    }

    public void MostrarPrompt(string tecla, string texto)
    {
        if (promptTexto == null) return;
        promptTecla.text = tecla;
        promptTexto.text = texto;
        promptObjetivo = 1f;
    }

    public void OcultarPrompt()
    {
        promptObjetivo = 0f;
    }

    public void ToggleInventario()
    {
        inventarioVisible = !inventarioVisible;
        inventarioAlfaObjetivo = inventarioVisible ? 1f : 0f;
    }

    public void MostrarHUD(bool visible)
    {
        if (grupoRaiz != null) grupoRaiz.alpha = visible ? 1f : 0f;
    }

    // =====================================================================
    //  ACTUALIZACIÓN DESDE EL JUEGO
    // =====================================================================
    private void ActualizarVidas()
    {
        int v = inv.vidasActuales;
        if (v == vidasPrevias) return;

        bool perdioVida = vidasPrevias >= 0 && v < vidasPrevias;
        int anterior = vidasPrevias;
        vidasPrevias = v;

        for (int i = 0; i < corazones.Length; i++)
        {
            bool vivo = i < v;
            corazones[i].color = vivo ? ColorCorazonVivo() : colCorazonPerdido;
        }

        if (perdioVida)
        {
            int indicePerdido = Mathf.Clamp(v, 0, corazones.Length - 1);
            StartCoroutine(Sacudir(corazones[indicePerdido].rectTransform, posCorazones[indicePerdido], 0.45f, 10f));
            StartCoroutine(Rebote(panelVidas, 1.08f, 0.35f));
            DestelloRojo();
        }
        else if (anterior >= 0 && v > anterior)
        {
            StartCoroutine(Rebote(panelVidas, 1.08f, 0.35f));
        }
    }

    private Color ColorCorazonVivo()
    {
        // El corazón del kit ya viene rojo; el que genero por código es blanco y lo pinto
        return corazonEsDelKit ? Color.white : new Color(1f, 0.28f, 0.35f, 1f);
    }

    private void ActualizarSlots()
    {
        int cuenta = inv.listaInventario.Count;
        int limite = Mathf.Max(1, inv.limiteInventario);

        textoContador.text = cuenta + " / " + limite;
        textoContador.color = cuenta >= limite ? new Color(1f, 0.45f, 0.40f, 1f) : Color.white;

        for (int i = 0; i < slots.Length; i++)
        {
            SlotUI s = slots[i];
            string nombre = "";
            string cat = "";

            if (i < cuenta)
            {
                nombre = inv.listaInventario[i].nombreMostrado;
                cat = inv.listaInventario[i].tipoCategoria;
            }

            if (nombre == s.nombreActual && cat == s.categoriaActual) continue;

            bool eraVacio = string.IsNullOrEmpty(s.nombreActual);
            s.nombreActual = nombre;
            s.categoriaActual = cat;

            PintarSlot(s, nombre, cat);

            if (eraVacio && !string.IsNullOrEmpty(nombre))
            {
                StartCoroutine(Rebote(s.raiz, 1.18f, 0.35f));
                StartCoroutine(Destello(s.flash, 0.55f, 0.4f));
            }
        }
    }

    private void PintarSlot(SlotUI s, string nombre, string cat)
    {
        bool lleno = !string.IsNullOrEmpty(nombre);

        s.vacio.gameObject.SetActive(!lleno);
        s.nombre.gameObject.SetActive(lleno);
        s.categoria.transform.parent.gameObject.SetActive(lleno);
        s.circulo.gameObject.SetActive(lleno);

        if (!lleno)
        {
            s.borde.color = colBordeApagado;
            s.relleno.color = new Color(0.05f, 0.09f, 0.08f, 0.85f);
            s.icono.gameObject.SetActive(false);
            s.letra.gameObject.SetActive(false);
            return;
        }

        Color c = ColorCategoria(cat);
        s.borde.color = c;
        s.relleno.color = new Color(c.r * 0.18f + 0.04f, c.g * 0.18f + 0.06f, c.b * 0.18f + 0.05f, 0.95f);
        s.circulo.color = new Color(c.r, c.g, c.b, 0.35f);

        s.nombre.text = nombre;
        s.categoria.text = TituloCategoria(cat);
        s.banda.color = c;

        bool esOrganico = (cat ?? "").Trim().ToLowerInvariant().StartsWith("org");
        if (esOrganico && spriteManzana != null)
        {
            s.icono.sprite = spriteManzana;
            s.icono.gameObject.SetActive(true);
            s.letra.gameObject.SetActive(false);
        }
        else
        {
            s.icono.gameObject.SetActive(false);
            s.letra.gameObject.SetActive(true);
            s.letra.text = nombre.Substring(0, 1).ToUpper();
        }
    }

    private void ActualizarPuntos(float dt)
    {
        int objetivo = 0;
        if (GameManager.Instance != null) objetivo = GameManager.Instance.puntos;
        else if (inv != null) objetivo = inv.puntosTotales;

        if (puntosObjetivoPrevio == int.MinValue)
        {
            puntosObjetivoPrevio = objetivo;
            puntosMostrados = objetivo;
        }

        if (objetivo != puntosObjetivoPrevio)
        {
            int delta = objetivo - puntosObjetivoPrevio;
            puntosObjetivoPrevio = objetivo;

            StartCoroutine(Rebote(panelPuntos, 1.10f, 0.30f));
            StartCoroutine(PuntosFlotantes(delta));
        }

        float velocidad = Mathf.Max(80f, Mathf.Abs(objetivo - puntosMostrados) * 6f);
        puntosMostrados = Mathf.MoveTowards(puntosMostrados, objetivo, velocidad * dt);
        textoPuntos.text = Mathf.RoundToInt(puntosMostrados).ToString();
    }

    // =====================================================================
    //  CONSTRUCCIÓN DE LA INTERFAZ
    // =====================================================================
    private void Construir()
    {
        GameObject cgo = new GameObject("HUD_Canvas");
        cgo.transform.SetParent(transform, false);

        canvas = cgo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -1; // detrás del Canvas viejo para que el Game Over quede encima

        CanvasScaler escalador = cgo.AddComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);
        escalador.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        escalador.matchWidthOrHeight = 0.5f;

        grupoRaiz = cgo.AddComponent<CanvasGroup>();
        grupoRaiz.blocksRaycasts = false;
        grupoRaiz.interactable = false;

        Transform raiz = cgo.transform;

        ConstruirVineta(raiz);
        ConstruirVidas(raiz);
        ConstruirPuntos(raiz);
        ConstruirInventario(raiz);
        ConstruirToast(raiz);
        ConstruirPrompt(raiz);
        ConstruirAyudaControles(raiz);
    }

    private void ConstruirVineta(Transform raiz)
    {
        RectTransform rt = NuevoRect("Vineta", raiz);
        Estirar(rt, 0, 0, 0, 0);
        vineta = rt.gameObject.AddComponent<Image>();
        vineta.sprite = sVineta;
        vineta.color = new Color(1f, 0.1f, 0.1f, 0f);
        vineta.raycastTarget = false;
    }

    private void ConstruirVidas(Transform raiz)
    {
        panelVidas = Panel("PanelVidas", raiz, new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(30, -30), new Vector2(330, 128), colFondo, colBorde, out Image borde, out Image relleno);

        TextMeshProUGUI etiqueta = Texto("Etiqueta", panelVidas, "VIDAS", 24, colTextoSuave,
            FontStyles.Bold, TextAlignmentOptions.TopLeft);
        Anclar(etiqueta.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(26, -12), new Vector2(200, 32));

        corazones = new Image[corazonesMaximos];
        posCorazones = new Vector2[corazonesMaximos];

        Sprite sprite = spriteCorazon != null ? spriteCorazon : sCorazonGen;
        corazonEsDelKit = spriteCorazon != null;

        for (int i = 0; i < corazonesMaximos; i++)
        {
            RectTransform rt = NuevoRect("Corazon" + (i + 1), panelVidas);
            Vector2 pos = new Vector2(24 + i * 94, 16);
            Anclar(rt, Vector2.zero, Vector2.zero, Vector2.zero, pos, new Vector2(80, 76));
            posCorazones[i] = pos;

            Image im = rt.gameObject.AddComponent<Image>();
            im.sprite = sprite;
            im.preserveAspect = true;
            im.raycastTarget = false;
            im.color = ColorCorazonVivo();
            corazones[i] = im;
        }
    }

    private void ConstruirPuntos(Transform raiz)
    {
        panelPuntos = Panel("PanelPuntos", raiz, new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-30, -30), new Vector2(420, 128), colFondo, new Color(1f, 0.82f, 0.30f, 1f),
            out Image borde, out Image relleno);

        // Moneda
        RectTransform rtMoneda = NuevoRect("Moneda", panelPuntos);
        Anclar(rtMoneda, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            new Vector2(22, 0), new Vector2(92, 92));
        Image moneda = rtMoneda.gameObject.AddComponent<Image>();
        moneda.sprite = spriteMonedas != null ? spriteMonedas : sCirculo;
        moneda.color = spriteMonedas != null ? Color.white : new Color(1f, 0.82f, 0.25f, 1f);
        moneda.preserveAspect = true;
        moneda.raycastTarget = false;

        TextMeshProUGUI etiqueta = Texto("Etiqueta", panelPuntos, "PUNTOS", 24, new Color(1f, 0.90f, 0.55f, 1f),
            FontStyles.Bold, TextAlignmentOptions.TopLeft);
        Anclar(etiqueta.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(130, -12), new Vector2(240, 32));

        textoPuntos = Texto("Numero", panelPuntos, "0", 64, Color.white,
            FontStyles.Bold, TextAlignmentOptions.Left);
        textoPuntos.enableAutoSizing = true;
        textoPuntos.fontSizeMin = 30;
        textoPuntos.fontSizeMax = 64;
        Anclar(textoPuntos.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0),
            new Vector2(130, 12), new Vector2(270, 80));
    }

    private void ConstruirInventario(Transform raiz)
    {
        RectTransform contenedor = NuevoRect("Inventario", raiz);
        Anclar(contenedor, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 28), new Vector2(860, 210));
        grupoInventario = contenedor.gameObject.AddComponent<CanvasGroup>();
        grupoInventario.blocksRaycasts = false;
        grupoInventario.interactable = false;

        // ----- Bloque de la mochila (icono + contador) -----
        RectTransform bloque = Panel("BloqueMochila", contenedor, new Vector2(0, 0), new Vector2(0, 0),
            new Vector2(0, 0), new Vector2(170, 200), colFondo, colBorde, out Image b1, out Image r1);

        RectTransform rtIcono = NuevoRect("IconoMochila", bloque);
        Anclar(rtIcono, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -18), new Vector2(120, 90));
        Image iconoMochila = rtIcono.gameObject.AddComponent<Image>();
        iconoMochila.sprite = spriteMochila != null ? spriteMochila : sCirculo;
        iconoMochila.color = spriteMochila != null ? Color.white : new Color(0.7f, 0.55f, 0.35f, 1f);
        iconoMochila.preserveAspect = true;
        iconoMochila.raycastTarget = false;

        TextMeshProUGUI lblMochila = Texto("Etiqueta", bloque, "MOCHILA", 22, colTextoSuave,
            FontStyles.Bold, TextAlignmentOptions.Center);
        Anclar(lblMochila.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 62), new Vector2(160, 30));

        textoContador = Texto("Contador", bloque, "0 / 3", 40, Color.white,
            FontStyles.Bold, TextAlignmentOptions.Center);
        Anclar(textoContador.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 14), new Vector2(160, 48));

        // ----- Slots -----
        int cantidad = 3;
        slots = new SlotUI[cantidad];
        for (int i = 0; i < cantidad; i++)
        {
            slots[i] = ConstruirSlot(contenedor, i, new Vector2(190 + i * 222, 0));
            PintarSlot(slots[i], "", "");
        }
    }

    private SlotUI ConstruirSlot(Transform padre, int indice, Vector2 pos)
    {
        SlotUI s = new SlotUI();

        // Pivote al centro para que el rebote crezca desde el medio del slot
        s.raiz = Panel("Slot" + (indice + 1), padre, new Vector2(0, 0), new Vector2(0.5f, 0.5f),
            pos + new Vector2(100f, 100f), new Vector2(200, 200), colFondo, colBordeApagado,
            out s.borde, out s.relleno);

        // Círculo detrás del icono
        RectTransform rtCirculo = NuevoRect("Circulo", s.raiz);
        Anclar(rtCirculo, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -22), new Vector2(76, 76));
        s.circulo = rtCirculo.gameObject.AddComponent<Image>();
        s.circulo.sprite = sCirculo;
        s.circulo.raycastTarget = false;

        // Icono (manzana para orgánicos)
        RectTransform rtIcono = NuevoRect("Icono", rtCirculo);
        Estirar(rtIcono, 8, 8, 8, 8);
        s.icono = rtIcono.gameObject.AddComponent<Image>();
        s.icono.preserveAspect = true;
        s.icono.raycastTarget = false;

        // Letra inicial
        s.letra = Texto("Letra", rtCirculo, "", 44, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
        Estirar(s.letra.rectTransform, 0, 0, 0, 0);

        // Nombre del objeto
        s.nombre = Texto("Nombre", s.raiz, "", 30, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
        s.nombre.enableAutoSizing = true;
        s.nombre.fontSizeMin = 16;
        s.nombre.fontSizeMax = 30;
        Anclar(s.nombre.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0),
            new Vector2(0, 54), new Vector2(-18, 50));

        // Banda con la categoría
        RectTransform rtBanda = NuevoRect("BandaCategoria", s.raiz);
        Anclar(rtBanda, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0),
            new Vector2(0, 14), new Vector2(-28, 34));
        s.banda = rtBanda.gameObject.AddComponent<Image>();
        s.banda.sprite = sRedondo;
        s.banda.type = Image.Type.Sliced;
        s.banda.raycastTarget = false;

        s.categoria = Texto("Categoria", rtBanda, "", 22, new Color(0.05f, 0.08f, 0.07f, 1f),
            FontStyles.Bold, TextAlignmentOptions.Center);
        Estirar(s.categoria.rectTransform, 0, 0, 0, 0);

        // Texto "VACÍO"
        s.vacio = Texto("Vacio", s.raiz, "VACÍO", 26, new Color(0.45f, 0.55f, 0.50f, 1f),
            FontStyles.Bold, TextAlignmentOptions.Center);
        Estirar(s.vacio.rectTransform, 0, 0, 0, 0);

        // Número del slot
        RectTransform rtNum = NuevoRect("Numero", s.raiz);
        Anclar(rtNum, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(10, -10), new Vector2(34, 34));
        Image fondoNum = rtNum.gameObject.AddComponent<Image>();
        fondoNum.sprite = sCirculo;
        fondoNum.color = new Color(0f, 0f, 0f, 0.55f);
        fondoNum.raycastTarget = false;
        TextMeshProUGUI txtNum = Texto("Texto", rtNum, (indice + 1).ToString(), 22, Color.white,
            FontStyles.Bold, TextAlignmentOptions.Center);
        Estirar(txtNum.rectTransform, 0, 0, 0, 0);

        // Destello al recoger
        RectTransform rtFlash = NuevoRect("Flash", s.raiz);
        Estirar(rtFlash, 3, 3, 3, 3);
        s.flash = rtFlash.gameObject.AddComponent<Image>();
        s.flash.sprite = sRedondo;
        s.flash.type = Image.Type.Sliced;
        s.flash.color = new Color(1f, 1f, 1f, 0f);
        s.flash.raycastTarget = false;

        return s;
    }

    private void ConstruirToast(Transform raiz)
    {
        toastRect = Panel("Aviso", raiz, new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 300), new Vector2(900, 92), new Color(0.05f, 0.08f, 0.07f, 0.92f), Color.white,
            out toastBorde, out Image relleno);
        toastPosBase = toastRect.anchoredPosition;

        grupoToast = toastRect.gameObject.AddComponent<CanvasGroup>();
        grupoToast.alpha = 0f;
        grupoToast.blocksRaycasts = false;
        grupoToast.interactable = false;

        toastTexto = Texto("Texto", toastRect, "", 44, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
        toastTexto.enableAutoSizing = true;
        toastTexto.fontSizeMin = 22;
        toastTexto.fontSizeMax = 44;
        Estirar(toastTexto.rectTransform, 24, 8, 24, 8);
    }

    private void ConstruirPrompt(Transform raiz)
    {
        promptRect = Panel("Prompt", raiz, new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 410), new Vector2(620, 84), colFondo, colBorde, out Image borde, out Image relleno);

        grupoPrompt = promptRect.gameObject.AddComponent<CanvasGroup>();
        grupoPrompt.alpha = 0f;
        grupoPrompt.blocksRaycasts = false;
        grupoPrompt.interactable = false;

        // "Tecla" estilo botón
        RectTransform rtTecla = Panel("Tecla", promptRect, new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            new Vector2(18, 0), new Vector2(58, 58), new Color(0.95f, 0.95f, 0.90f, 1f),
            new Color(0.45f, 0.45f, 0.40f, 1f), out Image bTecla, out Image rTecla);
        promptTecla = Texto("Letra", rtTecla, "E", 38, new Color(0.10f, 0.12f, 0.10f, 1f),
            FontStyles.Bold, TextAlignmentOptions.Center);
        Estirar(promptTecla.rectTransform, 0, 0, 0, 0);

        promptTexto = Texto("Texto", promptRect, "", 34, Color.white, FontStyles.Bold, TextAlignmentOptions.Left);
        promptTexto.enableAutoSizing = true;
        promptTexto.fontSizeMin = 18;
        promptTexto.fontSizeMax = 34;
        Estirar(promptTexto.rectTransform, 96, 6, 20, 6);
    }

    private void ConstruirAyudaControles(Transform raiz)
    {
        RectTransform rt = Panel("Controles", raiz, new Vector2(0, 0), new Vector2(0, 0),
            new Vector2(30, 28), new Vector2(300, 150), new Color(0.05f, 0.08f, 0.07f, 0.55f),
            new Color(0.30f, 0.38f, 0.34f, 0.8f), out Image borde, out Image relleno);

        TextMeshProUGUI t = Texto("Texto", rt,
            "<b>WASD</b>  Mover\n<b>SHIFT</b>  Correr\n<b>E</b>  Interactuar\n<b>I</b>  Mochila",
            24, new Color(1f, 1f, 1f, 0.85f), FontStyles.Normal, TextAlignmentOptions.Left);
        Estirar(t.rectTransform, 22, 10, 10, 10);
    }

    // =====================================================================
    //  RUTINAS DE ANIMACIÓN (usan tiempo "real" para funcionar en pausa)
    // =====================================================================
    private IEnumerator RutinaToast(string texto, Color color)
    {
        toastTexto.text = texto;
        toastTexto.color = color;
        toastBorde.color = color;

        float t = 0f;
        const float entrada = 0.18f;
        while (t < entrada)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / entrada);
            k = 1f - (1f - k) * (1f - k);
            grupoToast.alpha = k;
            toastRect.anchoredPosition = toastPosBase + new Vector2(0, (1f - k) * -30f);
            yield return null;
        }
        grupoToast.alpha = 1f;
        toastRect.anchoredPosition = toastPosBase;

        float espera = duracionAviso;
        while (espera > 0f)
        {
            espera -= Time.unscaledDeltaTime;
            yield return null;
        }

        t = 0f;
        const float salida = 0.3f;
        while (t < salida)
        {
            t += Time.unscaledDeltaTime;
            grupoToast.alpha = 1f - Mathf.Clamp01(t / salida);
            yield return null;
        }
        grupoToast.alpha = 0f;
        rutinaToast = null;
    }

    private IEnumerator Rebote(RectTransform rt, float maximo, float duracion)
    {
        float t = 0f;
        while (t < duracion)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duracion);
            float s = 1f + (maximo - 1f) * Mathf.Sin(k * Mathf.PI);
            rt.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    private IEnumerator Sacudir(RectTransform rt, Vector2 origen, float duracion, float fuerza)
    {
        float t = 0f;
        while (t < duracion)
        {
            t += Time.unscaledDeltaTime;
            float k = 1f - Mathf.Clamp01(t / duracion);
            rt.anchoredPosition = origen + new Vector2(Mathf.Sin(t * 70f) * fuerza * k, 0f);
            yield return null;
        }
        rt.anchoredPosition = origen;
    }

    private IEnumerator Destello(Image img, float alfaInicial, float duracion)
    {
        float t = 0f;
        while (t < duracion)
        {
            t += Time.unscaledDeltaTime;
            float a = alfaInicial * (1f - Mathf.Clamp01(t / duracion));
            img.color = new Color(1f, 1f, 1f, a);
            yield return null;
        }
        img.color = new Color(1f, 1f, 1f, 0f);
    }

    private void DestelloRojo()
    {
        if (vineta == null) return;
        if (rutinaVineta != null) StopCoroutine(rutinaVineta);
        rutinaVineta = StartCoroutine(RutinaVineta());
    }

    private IEnumerator RutinaVineta()
    {
        float t = 0f;
        const float duracion = 0.6f;
        while (t < duracion)
        {
            t += Time.unscaledDeltaTime;
            float a = 0.85f * (1f - Mathf.Clamp01(t / duracion));
            vineta.color = new Color(1f, 0.1f, 0.1f, a);
            yield return null;
        }
        vineta.color = new Color(1f, 0.1f, 0.1f, 0f);
    }

    private IEnumerator PuntosFlotantes(int delta)
    {
        string texto = (delta > 0 ? "+" : "") + delta;
        Color col = delta > 0 ? new Color(0.45f, 1f, 0.55f, 1f) : new Color(1f, 0.40f, 0.40f, 1f);

        TextMeshProUGUI t = Texto("PuntosFlotantes", canvas.transform, texto, 52, col,
            FontStyles.Bold, TextAlignmentOptions.Right);
        Anclar(t.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-40, -170), new Vector2(300, 70));

        Vector2 inicio = t.rectTransform.anchoredPosition;
        float dur = 1.2f;
        float tiempo = 0f;
        while (tiempo < dur)
        {
            tiempo += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(tiempo / dur);
            t.rectTransform.anchoredPosition = inicio + new Vector2(0, -60f * k);
            t.color = new Color(col.r, col.g, col.b, 1f - k * k);
            yield return null;
        }
        Destroy(t.gameObject);
    }

    // =====================================================================
    //  OCULTAR EL HUD VIEJO
    // =====================================================================
    private void OcultarHUDViejo()
    {
        Canvas[] todos = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Canvas c in todos)
        {
            if (c == canvas) continue;
            foreach (string nombre in nombresHUDViejo)
            {
                Transform hijo = c.transform.Find(nombre);
                if (hijo != null) hijo.gameObject.SetActive(false);
            }
        }
    }

    // =====================================================================
    //  CARGA DE SPRITES Y GENERACIÓN POR CÓDIGO
    // =====================================================================
    private void CargarSprites()
    {
        spriteCorazon = Cargar(spriteCorazon, "UI_Indicator_Heart",
            "Assets/Pixel_HUD_UI_FreeKit/Sprites/UI Elements/UI_Indicator_Heart.png");
        spriteMonedas = Cargar(spriteMonedas, "coins",
            "Assets/Pixel_HUD_UI_FreeKit/Sprites/Icons/coins.png");
        spriteMochila = Cargar(spriteMochila, "mochila",
            "Assets/Pixel_HUD_UI_FreeKit/Sprites/Icons/mochila.png");
        spriteManzana = Cargar(spriteManzana, "Icon_Consumable_Apple",
            "Assets/Pixel_HUD_UI_FreeKit/Sprites/Icons/Icon_Consumable_Apple.png");
    }

    private Sprite Cargar(Sprite actual, string nombreResources, string rutaEditor)
    {
        if (actual != null) return actual;

        Sprite s = Resources.Load<Sprite>("HUD/" + nombreResources);
#if UNITY_EDITOR
        if (s == null) s = AssetDatabase.LoadAssetAtPath<Sprite>(rutaEditor);
#endif
        return s;
    }

    private void GenerarSpritesPropios()
    {
        sRedondo = CrearSpriteRedondeado(64, 22);
        sCirculo = CrearSpriteRedondeado(128, 64);
        sCorazonGen = CrearSpriteCorazon(128);
        sVineta = CrearSpriteVineta(128);
    }

    private static Sprite CrearSpriteRedondeado(int tam, int radio)
    {
        Texture2D tex = new Texture2D(tam, tam, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color32[] px = new Color32[tam * tam];
        for (int y = 0; y < tam; y++)
        {
            for (int x = 0; x < tam; x++)
            {
                float fx = x + 0.5f;
                float fy = y + 0.5f;
                float cx = Mathf.Clamp(fx, radio, tam - radio);
                float cy = Mathf.Clamp(fy, radio, tam - radio);
                float d = Vector2.Distance(new Vector2(fx, fy), new Vector2(cx, cy));
                float a = Mathf.Clamp01(radio - d + 0.5f);
                px[y * tam + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }
        tex.SetPixels32(px);
        tex.Apply();

        Vector4 borde = new Vector4(radio, radio, radio, radio);
        return Sprite.Create(tex, new Rect(0, 0, tam, tam), new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, borde);
    }

    private static Sprite CrearSpriteCorazon(int tam)
    {
        Texture2D tex = new Texture2D(tam, tam, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color32[] px = new Color32[tam * tam];
        const int sub = 3;
        for (int y = 0; y < tam; y++)
        {
            for (int x = 0; x < tam; x++)
            {
                int dentro = 0;
                for (int sy = 0; sy < sub; sy++)
                {
                    for (int sx = 0; sx < sub; sx++)
                    {
                        float u = (x + (sx + 0.5f) / sub) / tam;
                        float v = (y + (sy + 0.5f) / sub) / tam;
                        float hx = u * 2.6f - 1.3f;
                        float hy = v * 2.6f - 1.15f;
                        float a = hx * hx + hy * hy - 1f;
                        float f = a * a * a - hx * hx * hy * hy * hy;
                        if (f <= 0f) dentro++;
                    }
                }
                byte alfa = (byte)(255f * dentro / (sub * sub));
                px[y * tam + x] = new Color32(255, 255, 255, alfa);
            }
        }
        tex.SetPixels32(px);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, tam, tam), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite CrearSpriteVineta(int tam)
    {
        Texture2D tex = new Texture2D(tam, tam, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color32[] px = new Color32[tam * tam];
        for (int y = 0; y < tam; y++)
        {
            for (int x = 0; x < tam; x++)
            {
                float nx = (x + 0.5f) / tam * 2f - 1f;
                float ny = (y + 0.5f) / tam * 2f - 1f;
                float d = Mathf.Sqrt(nx * nx + ny * ny) / 1.4142f;
                float a = Mathf.SmoothStep(0.35f, 1f, d);
                px[y * tam + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }
        tex.SetPixels32(px);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, tam, tam), new Vector2(0.5f, 0.5f), 100f);
    }

    // =====================================================================
    //  HERRAMIENTAS PARA ARMAR UI
    // =====================================================================
    private static RectTransform NuevoRect(string nombre, Transform padre)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(padre, false);
        return rt;
    }

    private static void Anclar(RectTransform rt, Vector2 anclaMin, Vector2 anclaMax, Vector2 pivote,
        Vector2 pos, Vector2 tam)
    {
        rt.anchorMin = anclaMin;
        rt.anchorMax = anclaMax;
        rt.pivot = pivote;
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;
    }

    private static void Estirar(RectTransform rt, float izq, float abajo, float der, float arriba)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(izq, abajo);
        rt.offsetMax = new Vector2(-der, -arriba);
    }

    private TextMeshProUGUI Texto(string nombre, Transform padre, string texto, float tam, Color color,
        FontStyles estilo, TextAlignmentOptions alineacion)
    {
        RectTransform rt = NuevoRect(nombre, padre);
        TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (fuente != null) t.font = fuente;
        t.text = texto;
        t.fontSize = tam;
        t.color = color;
        t.fontStyle = estilo;
        t.alignment = alineacion;
        t.raycastTarget = false;
        return t;
    }

    /// <summary>
    /// Panel con borde de color, relleno, brillo suave y sombra.
    /// Devuelve el RectTransform exterior; los hijos que agregues van encima del relleno.
    /// </summary>
    private RectTransform Panel(string nombre, Transform padre, Vector2 ancla, Vector2 pivote,
        Vector2 pos, Vector2 tam, Color relleno, Color borde, out Image imgBorde, out Image imgRelleno)
    {
        RectTransform exterior = NuevoRect(nombre, padre);
        Anclar(exterior, ancla, ancla, pivote, pos, tam);

        imgBorde = exterior.gameObject.AddComponent<Image>();
        imgBorde.sprite = sRedondo;
        imgBorde.type = Image.Type.Sliced;
        imgBorde.color = borde;
        imgBorde.raycastTarget = false;

        Shadow sombra = exterior.gameObject.AddComponent<Shadow>();
        sombra.effectColor = new Color(0f, 0f, 0f, 0.45f);
        sombra.effectDistance = new Vector2(0f, -6f);

        RectTransform interior = NuevoRect("Relleno", exterior);
        Estirar(interior, 4, 4, 4, 4);
        imgRelleno = interior.gameObject.AddComponent<Image>();
        imgRelleno.sprite = sRedondo;
        imgRelleno.type = Image.Type.Sliced;
        imgRelleno.color = relleno;
        imgRelleno.raycastTarget = false;

        RectTransform brillo = NuevoRect("Brillo", interior);
        brillo.anchorMin = new Vector2(0, 0.5f);
        brillo.anchorMax = new Vector2(1, 1);
        brillo.offsetMin = new Vector2(6, 0);
        brillo.offsetMax = new Vector2(-6, -6);
        Image imgBrillo = brillo.gameObject.AddComponent<Image>();
        imgBrillo.sprite = sRedondo;
        imgBrillo.type = Image.Type.Sliced;
        imgBrillo.color = new Color(1f, 1f, 1f, 0.06f);
        imgBrillo.raycastTarget = false;

        return exterior;
    }

    // =====================================================================
    //  CATEGORÍAS
    // =====================================================================
    private static Color ColorCategoria(string cat)
    {
        string c = (cat ?? "").Trim().ToLowerInvariant();
        if (c.StartsWith("org")) return new Color(0.40f, 0.80f, 0.35f, 1f);   // verde
        if (c.StartsWith("plas")) return new Color(0.25f, 0.60f, 0.95f, 1f);  // azul
        if (c.StartsWith("pap")) return new Color(0.95f, 0.80f, 0.25f, 1f);   // amarillo
        if (c.StartsWith("vid")) return new Color(0.30f, 0.85f, 0.80f, 1f);   // turquesa
        return new Color(0.70f, 0.75f, 0.78f, 1f);
    }

    private static string TituloCategoria(string cat)
    {
        string c = (cat ?? "").Trim().ToLowerInvariant();
        if (c.StartsWith("org")) return "ORGÁNICO";
        if (c.StartsWith("plas")) return "PLÁSTICO";
        if (c.StartsWith("pap")) return "PAPEL";
        if (c.StartsWith("vid")) return "VIDRIO";
        return (cat ?? "").Trim().ToUpperInvariant();
    }
}
