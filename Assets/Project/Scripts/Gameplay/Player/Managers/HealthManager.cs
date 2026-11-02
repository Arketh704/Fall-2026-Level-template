using UnityEngine;

namespace LevelDesign
{
    public class HealthManager : MonoBehaviour
    {
        public float currentHealth;
        public float maxHealth;
        public int lives;

        public void TakeDamage(float damage)
        {
            currentHealth -= damage;
            if(currentHealth <= 0) {
                Die();
            }
        }

        public void Heal(float health)
        {
            currentHealth += health;
            if(currentHealth > maxHealth) { currentHealth = maxHealth; }
        }

        public void AddLife(int increase)
        {
            lives += increase;
        }

        public void RemoveLife(int removal) 
        {
            lives -= removal;
            if(lives == 0) { 
                Defeated();
            }
        }

        public void Die()
        {
            
        }

        public void Defeated()
        {
            
        }
    }
}
