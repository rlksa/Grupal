using UnityEngine;

public class HealthManager : MonoBehaviour
{
    public int maxHealth = 3;
    public int currentHealth;

    // Arrastra aquí tus 3 objetos/imágenes de corazones en el Inspector (0, 1, 2)
    public GameObject[] hearts;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateHearts();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void UpdateHearts()
    {
        for (int i = 0; i < hearts.Length; i++)
        {
            if (i < currentHealth)
            {
                hearts[i].SetActive(true);
            }
            else
            {
                hearts[i].SetActive(false); // Desactiva el corazón según la vida restante
            }
        }
    }

    void Die()
    {
        Debug.Log("¡El jugador ha muerto!");
        // Aquí puedes reiniciar la escena o destruir al personaje
    }
}