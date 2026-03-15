using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Sistema de vida reutilizable para cualquier dinosaurio (jugador o enemigo).
///
/// Expone eventos de Unity para conectar el HUD y efectos sin dependencias duras:
///   OnHealthChanged(float percent) → conectar a la barra de vida
///   OnDeath()                      → conectar a GameManager / animación
/// </summary>
public class DinoHealth : MonoBehaviour
{
    [Header("Vida")]
    public float maxHealth = 100f;
    public float currentHealth { get; private set; }

    [Header("Efectos")]
    public ParticleSystem hitEffect;
    public ParticleSystem deathEffect;

    [Header("Eventos")]
    [Tooltip("Se dispara cuando cambia la vida. El float es el porcentaje (0–1).")]
    public UnityEvent<float> OnHealthChanged;

    [Tooltip("Se dispara cuando la vida llega a 0.")]
    public UnityEvent OnDeath;

    // ─── Estado ───────────────────────────────────────────────────
    public bool IsDead { get; private set; }
    public float HealthPercent => currentHealth / maxHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    // ══════════════════════════════════════════════════════════════
    // API pública
    // ══════════════════════════════════════════════════════════════

    /// <summary>Recibir daño. Llama a Die() si la vida llega a 0.</summary>
    public void TakeDamage(float amount)
    {
        if (IsDead) return;

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        OnHealthChanged?.Invoke(HealthPercent);

        if (hitEffect != null) hitEffect.Play();

        if (currentHealth <= 0f) Die();
    }

    /// <summary>Curar. No puede superar maxHealth.</summary>
    public void Heal(float amount)
    {
        if (IsDead) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(HealthPercent);
    }

    // ══════════════════════════════════════════════════════════════
    // Privado
    // ══════════════════════════════════════════════════════════════

    private void Die()
    {
        IsDead = true;
        OnDeath?.Invoke();

        if (deathEffect != null) deathEffect.Play();

        // Desactivar el objeto después de la animación de muerte (3 s)
        Invoke(nameof(Deactivate), 3f);
    }

    private void Deactivate() => gameObject.SetActive(false);
}
