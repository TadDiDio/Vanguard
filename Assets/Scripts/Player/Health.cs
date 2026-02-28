using System;
using UnityEngine;

namespace Vanguard
{
    public interface IHealth
    {
        public void TakeDamage(int amount);
    }
    
    public class Health : MonoBehaviour, IHealth
    {
        [SerializeField] private int maxHealth = 100;
        
        private int _health;

        private void Awake()
        {
            _health = maxHealth;
        }

        public void TakeDamage(int amount)
        {
            
        }
    }
}