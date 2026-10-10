using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI Puntuación del Asset")]
    public TextMeshProUGUI textoPuntosAsset;

    [Header("UI Game Over")]
    public GameObject panelGameOver;
    public TMP_Text textoPuntosGameOver;
    public TMP_Text textoRecordGameOver;

    [Header("UI Victoria")]
    public GameObject panelVictoria;
    public TMP_Text textoPuntosVictoria;
    public TMP_Text textoRecordVictoria;
    public Image[] estrellasVictoria;          // 3 estrellas (se pintan según las vidas que te quedan)

    [Tooltip("Puntos necesarios para ganar. 0 = no se gana por puntos (llama a Ganar() desde donde quieras).")]
    public int puntosParaGanar = 500;

    [Header("Sistema de Vidas")]
    public int vidasMaximas = 3;
    private int vidasActuales;

    [Header("Puntuación Total")]
    public int puntos = 0;

    public bool JuegoTerminado { get; private set; }

    // Mensaje flotante central automático
    private TextMeshProUGUI textoMensajeFlotante;
    private float temporizadorMensaje = 0f;
    private EcoBotInventario inventario;
    private float tiempoMuerte = -1f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        Time.timeScale = 1f;
    }

    private void Start()
    {
        vidasActuales = vidasMaximas;
        inventario = FindFirstObjectByType<EcoBotInventario>();

        CrearTextoFlotanteCentral();
        ActualizarTextoPuntosUI();

        if (panelGameOver != null) panelGameOver.SetActive(false);
        if (panelVictoria != null) panelVictoria.SetActive(false);

        if (SoundManager.Instance != null && SoundManager.Instance.musicaJuego != null)
        {
            SoundManager.Instance.CambiarMusica(SoundManager.Instance.musicaJuego);
        }
    }

    private void Update()
    {
        if (temporizadorMensaje > 0)
        {
            temporizadorMensaje -= Time.deltaTime;
            if (temporizadorMensaje <= 0 && textoMensajeFlotante != null)
            {
                textoMensajeFlotante.gameObject.SetActive(false);
            }
        }

        // Si el robot se queda sin vidas por cualquier causa, muestra el Game Over tras la animación de muerte
        if (!JuegoTerminado && inventario != null && inventario.vidasActuales <= 0)
        {
            if (tiempoMuerte < 0f) tiempoMuerte = Time.time;
            if (Time.time - tiempoMuerte >= 2.5f) MostrarGameOver();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Atajos para probar las pantallas: F8 = Game Over, F9 = Victoria
        if (Input.GetKeyDown(KeyCode.F8)) MostrarGameOver();
        if (Input.GetKeyDown(KeyCode.F9)) Ganar();
#endif
    }

    private void CrearTextoFlotanteCentral()
    {
        Canvas canvas = null;
        foreach (Canvas c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (c.name.StartsWith("UI_Eco")) continue;
            if (c.transform.parent != null) continue;
            canvas = c;
            break;
        }
        if (canvas == null) return;

        GameObject objMensaje = new GameObject("TextoMensaje_Flotante");
        objMensaje.transform.SetParent(canvas.transform, false);

        textoMensajeFlotante = objMensaje.AddComponent<TextMeshProUGUI>();
        textoMensajeFlotante.fontSize = 55;
        textoMensajeFlotante.alignment = TextAlignmentOptions.Center;
        textoMensajeFlotante.fontStyle = FontStyles.Bold;

        RectTransform rectMensaje = objMensaje.GetComponent<RectTransform>();
        rectMensaje.anchorMin = new Vector2(0.5f, 0.5f);
        rectMensaje.anchorMax = new Vector2(0.5f, 0.5f);
        rectMensaje.pivot = new Vector2(0.5f, 0.5f);
        rectMensaje.anchoredPosition = new Vector2(0, 50);
        rectMensaje.sizeDelta = new Vector2(800, 150);

        objMensaje.SetActive(false);
    }

    // 1. RECOGER BASURA (+10 PUNTOS)
    public void RecogerBasura()
    {
        if (JuegoTerminado) return;
        puntos += 10;
        ActualizarTextoPuntosUI();
        MostrarMensajeEnPantalla("+10", Color.cyan);

        if (SoundManager.Instance != null && SoundManager.Instance.sonidoAcierto != null)
            SoundManager.Instance.ReproducirSonido(SoundManager.Instance.sonidoAcierto);

        ComprobarVictoria();
    }

    // 2. DEPOSITAR EN CANECA (CORRECTO +50 / INCORRECTO -50)
    public void DepositarResiduo(bool esCorrecto)
    {
        if (JuegoTerminado) return;

        if (esCorrecto)
        {
            puntos += 50;
            MostrarMensajeEnPantalla("¡CORRECTO! +50", Color.green);

            if (SoundManager.Instance != null && SoundManager.Instance.sonidoAcierto != null)
                SoundManager.Instance.ReproducirSonido(SoundManager.Instance.sonidoAcierto);
        }
        else
        {
            puntos -= 50;
            if (puntos < 0) puntos = 0;

            MostrarMensajeEnPantalla("¡INCORRECTO! -50", Color.red);

            if (SoundManager.Instance != null && SoundManager.Instance.sonidoError != null)
                SoundManager.Instance.ReproducirSonido(SoundManager.Instance.sonidoError);
        }

        ActualizarTextoPuntosUI();
        ComprobarVictoria();
    }

    private void MostrarMensajeEnPantalla(string mensaje, Color color)
    {
        if (HUDJuego.Instance != null)
        {
            HUDJuego.Instance.MostrarMensaje(mensaje, color);
            return;
        }

        if (textoMensajeFlotante != null)
        {
            textoMensajeFlotante.text = mensaje;
            textoMensajeFlotante.color = color;
            textoMensajeFlotante.gameObject.SetActive(true);
            temporizadorMensaje = 1.5f;
        }
    }

    private void ActualizarTextoPuntosUI()
    {
        if (textoPuntosAsset != null)
        {
            textoPuntosAsset.text = "Puntos: " + puntos;
        }
    }

    public void RecibirDano(int cantidad)
    {
        if (JuegoTerminado) return;
        vidasActuales -= cantidad;

        if (SoundManager.Instance != null && SoundManager.Instance.sonidoGolpe != null)
        {
            SoundManager.Instance.ReproducirSonido(SoundManager.Instance.sonidoGolpe);
        }

        if (vidasActuales <= 0)
        {
            MostrarGameOver();
        }
    }

    // ───────────── VICTORIA ─────────────
    private void ComprobarVictoria()
    {
        if (puntosParaGanar > 0 && puntos >= puntosParaGanar) Ganar();
    }

    public void Ganar()
    {
        if (JuegoTerminado) return;
        JuegoTerminado = true;

        int record = GuardarRecord();
        if (textoPuntosVictoria != null) textoPuntosVictoria.text = "PUNTOS: " + puntos;
        if (textoRecordVictoria != null) textoRecordVictoria.text = "RÉCORD: " + record;

        int vidas = inventario != null ? inventario.vidasActuales : vidasActuales;
        int estrellas = Mathf.Clamp(vidas, 1, 3);
        if (estrellasVictoria != null)
        {
            for (int i = 0; i < estrellasVictoria.Length; i++)
            {
                if (estrellasVictoria[i] == null) continue;
                estrellasVictoria[i].color = i < estrellas ? new Color(1f, 0.85f, 0.2f) : new Color(0.25f, 0.28f, 0.3f);
            }
        }

        if (SoundManager.Instance != null && SoundManager.Instance.sonidoAcierto != null)
            SoundManager.Instance.ReproducirSonido(SoundManager.Instance.sonidoAcierto);

        AbrirPanelFinal(panelVictoria);
    }

    // ───────────── GAME OVER ─────────────
    private void MostrarGameOver()
    {
        if (JuegoTerminado) return;
        JuegoTerminado = true;

        if (SoundManager.Instance != null && SoundManager.Instance.sonidoGameOver != null)
        {
            SoundManager.Instance.ReproducirSonido(SoundManager.Instance.sonidoGameOver);
        }

        if (HUDJuego.Instance != null)
            HUDJuego.Instance.MostrarHUD(false);

        int record = GuardarRecord();
        if (textoPuntosGameOver != null) textoPuntosGameOver.text = "PUNTOS: " + puntos;
        if (textoRecordGameOver != null) textoRecordGameOver.text = "RÉCORD: " + record;

        AbrirPanelFinal(panelGameOver);
    }

    private int GuardarRecord()
    {
        int record = PlayerPrefs.GetInt("EcoRecord", 0);
        if (puntos > record)
        {
            record = puntos;
            PlayerPrefs.SetInt("EcoRecord", record);
            PlayerPrefs.Save();
        }
        return record;
    }

    private void AbrirPanelFinal(GameObject panel)
    {
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (panel != null) panel.SetActive(true);
    }

    // ───────────── BOTONES ─────────────
    public void Reintentar()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void IrAlMenuPrincipal()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }
}