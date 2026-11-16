
using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public event Action<float> OnHealthChanged;
    public event Action OnPlayerDied;

    [Header("Health Settings")]
    [SerializeField] float maxHealth = 100f;
    [SerializeField] float currentHealth = 100f;

    [Header("Damage Settings")]
    [SerializeField] float damageMultiplier = 1f;

    [Header("Death Settings")]
    [SerializeField] GameObject deathPrefab;
    [SerializeField] float destroyDelay = 0f;

    bool isDead;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public float HealthPercentage => currentHealth / maxHealth;
    public bool IsDead => isDead;

    void Awake()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
    }

    public void TakeDamage(float damage)
    {
        if (isDead || damage <= 0f)
            return;

        float finalDamage = damage * damageMultiplier;

        currentHealth = Mathf.Max(0f, currentHealth - finalDamage);

        OnHealthChanged?.Invoke(currentHealth);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (isDead || amount <= 0f)
            return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);

        OnHealthChanged?.Invoke(currentHealth);
    }

    public void RestoreHealth()
    {
        if (isDead)
            return;

        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth);
    }

    void Die()
    {
        if (isDead)
            return;

        isDead = true;

        OnPlayerDied?.Invoke();

        if (deathPrefab != null)
        {
            Instantiate(
                deathPrefab,
                transform.position,
                transform.rotation
            );
        }

        Destroy(gameObject, destroyDelay);
    }
}
