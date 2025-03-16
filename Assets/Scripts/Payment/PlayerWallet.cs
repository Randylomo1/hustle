using UnityEngine;

namespace NairobiHustle.Payment
{
    public class PlayerWallet : MonoBehaviour
    {
        [SerializeField] private float initialBalance = 1000f;
        private float balance;

        private void Awake()
        {
            balance = PlayerPrefs.GetFloat("PlayerBalance", initialBalance);
        }

        public bool DeductFunds(float amount)
        {
            if (amount <= balance)
            {
                balance -= amount;
                PlayerPrefs.SetFloat("PlayerBalance", balance);
                PlayerPrefs.Save();
                return true;
            }
            return false;
        }

        public void AddFunds(float amount)
        {
            balance += amount;
            PlayerPrefs.SetFloat("PlayerBalance", balance);
            PlayerPrefs.Save();
        }

        public float GetBalance()
        {
            return balance;
        }
    }
}