using UnityEngine;
using UnityEngine.UI;
using NairobiHustle.Payment;

namespace NairobiHustle.UI
{
    public class UIPaymentPanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MPesaService mpesaService;
        [SerializeField] private PlayerWallet playerWallet;
        
        [Header("UI Elements")]
        [SerializeField] private InputField phoneNumberField;
        [SerializeField] private InputField amountField;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Text statusText;

        private void OnEnable()
        {
            confirmButton.onClick.AddListener(HandlePayment);
            cancelButton.onClick.AddListener(ClosePanel);
            mpesaService.OnSuccess.AddListener(OnPaymentSuccess);
            mpesaService.OnFailure.AddListener(OnPaymentFailed);
        }

        private void OnDisable()
        {
            confirmButton.onClick.RemoveListener(HandlePayment);
            cancelButton.onClick.RemoveListener(ClosePanel);
            mpesaService.OnSuccess.RemoveListener(OnPaymentSuccess);
            mpesaService.OnFailure.RemoveListener(OnPaymentFailed);
        }

        private async void HandlePayment()
        {
            if(float.TryParse(amountField.text, out float amount))
            {
                statusText.text = "Processing payment...";
                PlayerPrefs.SetString("UserPhoneNumber", phoneNumberField.text);
                bool success = await mpesaService.ProcessPayment(amount);
                
                if(!success)
                {
                    statusText.text = "Payment processing failed";
                }
            }
        }

        private void OnPaymentSuccess(float amount)
        {
            statusText.text = $"Payment of {amount} KES successful!";
            playerWallet.DeductFunds(amount);
            ClosePanel();
        }

        private void OnPaymentFailed(string error)
        {
            statusText.text = $"Payment failed: {error}";
        }

        private void ClosePanel()
        {
            gameObject.SetActive(false);
        }
    }
}