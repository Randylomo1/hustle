using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace NairobiHustle.Payment
{
    public class MPesaPaymentUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_InputField phoneNumberInput;
        [SerializeField] private TMP_InputField amountInput;
        [SerializeField] private Button payButton;
        [SerializeField] private TextMeshProUGUI statusText;

        private MPesaService mpesaService;

        private void Awake()
        {
            mpesaService = GetComponent<MPesaService>();
            payButton.onClick.AddListener(ProcessPayment);
        }

        private async void ProcessPayment()
        {
            if (!ValidateInputs())
                return;

            payButton.interactable = false;
            statusText.text = "Processing payment...";

            float amount = float.Parse(amountInput.text);
            bool success = await mpesaService.ProcessPayment(amount);

            statusText.text = success ? "Payment successful!" : "Payment failed!";
            payButton.interactable = true;
        }

        private bool ValidateInputs()
        {
            if (string.IsNullOrEmpty(phoneNumberInput.text) || phoneNumberInput.text.Length != 10)
            {
                statusText.text = "Invalid phone number!";
                return false;
            }

            if (!float.TryParse(amountInput.text, out float amount) || amount <= 0)
            {
                statusText.text = "Invalid amount!";
                return false;
            }

            return true;
        }
    }
}