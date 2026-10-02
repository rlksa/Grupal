using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI Puntuación del Asset")]
    public TextMeshProUGUI textoPuntosAsset;

    [Header("UI Game Over")]
    public GameObject panelGameOver;

    [Header("Sistema de Vidas")]
    public int vidasMaximas = 3;
    private int vidasActuales;

    [Header("Puntuación Total")]
    public int puntos = 0;

    // Mensaje flotante central automático
    private TextMeshProUGUI textoMensajeFlotante;
    private float temporizadorMensaje = 0f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        vidasActuales = vidasMaximas;

        CrearTextoFlotanteCentral();
        ActualizarTextoPuntosUI();

        if (panelGameOver != null)
            panelGameOver.SetActive(false);

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
    }

    private void CrearTextoFlotanteCentral()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
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
        puntos += 10;
        ActualizarTextoPuntosUI();
        MostrarMensajeEnPantalla("+10", Color.cyan);

        if (SoundManager.Instance != null && SoundManager.Instance.sonidoAcierto != null)
            SoundManager.Instance.ReproducirSonido(SoundManager.Instance.sonidoAcierto);
    }

    // 2. DEPOSITAR EN CANECA (CORRECTO +50 / INCORRECTO -50)
    public void DepositarResiduo(bool esCorrecto)
    {
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
    }

    private void MostrarMensajeEnPantalla(string mensaje, Color color)
    {
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

    private void MostrarGameOver()
    {
        if (SoundManager.Instance != null && SoundManager.Instance.sonidoGameOver != null)
        {
            SoundManager.Instance.ReproducirSonido(SoundManager.Instance.sonidoGameOver);
        }

        Time.timeScale = 0f;

        if (panelGameOver != null)
            panelGameOver.SetActive(true);
    }

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