using UnityEngine;

public class MirarCamara : MonoBehaviour
{
    private Transform camaraTransform;

    void Start()
    {
        if (Camera.main != null)
        {
            camaraTransform = Camera.main.transform;
        }
    }

    void LateUpdate()
    {
        if (camaraTransform != null)
        {
            // Hace que el texto mire en la misma dirección que la cámara para que no se vea al revés
            transform.rotation = camaraTransform.rotation;
        }
    }
}