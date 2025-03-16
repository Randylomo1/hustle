using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using PayPalCheckoutSdk.Core;
using PayPalCheckoutSdk.Orders;
using Stripe;
using Newtonsoft.Json;

namespace NairobiHustle.Services
{
    public class PaymentGatewayService : MonoBehaviour
    {
        [Header("Payment Gateway Configuration")]
        [SerializeField] private bool enableMPesa = true;
        [SerializeField] private bool enablePayPal = true;
        [SerializeField] private bool enableCardPayments = true;
        
        [Header("PayPal Settings")]
        [SerializeField] private string paypalClientId;
        [SerializeField] private string paypalClientSecret;
        [SerializeField] private bool useSandbox = true;

        [Header("Stripe Settings")]
        [SerializeField] private string stripePublishableKey;
        [SerializeField] private string stripeSecretKey;
        
        private SecureMPesaService mpesaService;
        private PayPalHttpClient paypalClient;
        private StripeClient stripeClient;
        private PaymentProcessor paymentProcessor;

        private void Awake()
        {
            InitializePaymentGateways();
        }

        private void InitializePaymentGateways()
        {
            // Initialize M-Pesa
            if (enableMPesa)
            {
                mpesaService = GetComponent<SecureMPesaService>();
                if (mpesaService == null)
                {
                    mpesaService = gameObject.AddComponent<SecureMPesaService>();
                }
            }

            // Initialize PayPal
            if (enablePayPal)
            {
                var environment = useSandbox ? 
                    new SandboxEnvironment(paypalClientId, paypalClientSecret) :
                    new LiveEnvironment(paypalClientId, paypalClientSecret);
                paypalClient = new PayPalHttpClient(environment);
            }

            // Initialize Stripe
            if (enableCardPayments)
            {
                StripeConfiguration.ApiKey = stripeSecretKey;
                stripeClient = new StripeClient(stripeSecretKey);
            }

            paymentProcessor = new PaymentProcessor();
        }

        public async Task<PaymentResult> ProcessPayment(
            PaymentRequest request,
            PaymentMethod method,
            UserData userData)
        {
            try
            {
                // Validate request
                if (!ValidatePaymentRequest(request))
                {
                    return new PaymentResult
                    {
                        Success = false,
                        Error = "Invalid payment request"
                    };
                }

                // Process payment based on method
                switch (method)
                {
                    case PaymentMethod.MPesa:
                        return await ProcessMPesaPayment(request, userData);
                    case PaymentMethod.PayPal:
                        return await ProcessPayPalPayment(request, userData);
                    case PaymentMethod.Card:
                        return await ProcessCardPayment(request, userData);
                    default:
                        return new PaymentResult
                        {
                            Success = false,
                            Error = "Unsupported payment method"
                        };
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Payment processing failed: {e.Message}");
                return new PaymentResult
                {
                    Success = false,
                    Error = "Payment processing error"
                };
            }
        }

        private async Task<PaymentResult> ProcessMPesaPayment(
            PaymentRequest request,
            UserData userData)
        {
            if (!enableMPesa || mpesaService == null)
            {
                return new PaymentResult
                {
                    Success = false,
                    Error = "M-Pesa payments not enabled"
                };
            }

            var result = await mpesaService.ProcessSecurePayment(
                userData.PhoneNumber,
                request.Amount,
                request.Description,
                userData.BiometricData,
                userData.LocationData,
                userData.DeviceData
            );

            return new PaymentResult
            {
                Success = result.Success,
                Error = result.Error,
                TransactionId = result.TransactionId,
                PaymentMethod = PaymentMethod.MPesa
            };
        }

        private async Task<PaymentResult> ProcessPayPalPayment(
            PaymentRequest request,
            UserData userData)
        {
            if (!enablePayPal || paypalClient == null)
            {
                return new PaymentResult
                {
                    Success = false,
                    Error = "PayPal payments not enabled"
                };
            }

            try
            {
                var orderRequest = new OrdersCreateRequest();
                orderRequest.Prefer("return=representation");
                orderRequest.RequestBody(BuildPayPalOrderRequest(request));

                var response = await paypalClient.Execute(orderRequest);
                var order = response.Result<Order>();

                return new PaymentResult
                {
                    Success = true,
                    TransactionId = order.Id,
                    PaymentMethod = PaymentMethod.PayPal,
                    PaymentUrl = GetPayPalApprovalUrl(order)
                };
            }
            catch (Exception e)
            {
                Debug.LogError($"PayPal payment failed: {e.Message}");
                return new PaymentResult
                {
                    Success = false,
                    Error = "PayPal payment processing error"
                };
            }
        }

        private async Task<PaymentResult> ProcessCardPayment(
            PaymentRequest request,
            UserData userData)
        {
            if (!enableCardPayments || stripeClient == null)
            {
                return new PaymentResult
                {
                    Success = false,
                    Error = "Card payments not enabled"
                };
            }

            try
            {
                var paymentIntentService = new PaymentIntentService(stripeClient);
                var paymentIntent = await paymentIntentService.CreateAsync(
                    new PaymentIntentCreateOptions
                    {
                        Amount = (long)(request.Amount * 100), // Convert to cents
                        Currency = "kes",
                        PaymentMethodTypes = new List<string> { "card" },
                        Description = request.Description,
                        Metadata = new Dictionary<string, string>
                        {
                            { "UserID", userData.UserId },
                            { "GameTransaction", request.TransactionId }
                        }
                    }
                );

                return new PaymentResult
                {
                    Success = true,
                    TransactionId = paymentIntent.Id,
                    PaymentMethod = PaymentMethod.Card,
                    ClientSecret = paymentIntent.ClientSecret
                };
            }
            catch (Exception e)
            {
                Debug.LogError($"Card payment failed: {e.Message}");
                return new PaymentResult
                {
                    Success = false,
                    Error = "Card payment processing error"
                };
            }
        }

        private bool ValidatePaymentRequest(PaymentRequest request)
        {
            return request != null &&
                   request.Amount > 0 &&
                   !string.IsNullOrEmpty(request.Description) &&
                   !string.IsNullOrEmpty(request.TransactionId);
        }

        private OrderRequest BuildPayPalOrderRequest(PaymentRequest request)
        {
            return new OrderRequest()
            {
                CheckoutPaymentIntent = "CAPTURE",
                PurchaseUnits = new List<PurchaseUnitRequest>
                {
                    new PurchaseUnitRequest
                    {
                        AmountWithBreakdown = new AmountWithBreakdown
                        {
                            CurrencyCode = "USD",
                            Value = request.Amount.ToString("F2")
                        },
                        Description = request.Description
                    }
                },
                ApplicationContext = new ApplicationContext
                {
                    ReturnUrl = "https://nairobihustle.com/payment/success",
                    CancelUrl = "https://nairobihustle.com/payment/cancel"
                }
            };
        }

        private string GetPayPalApprovalUrl(Order order)
        {
            foreach (var link in order.Links)
            {
                if (link.Rel.Equals("approve", StringComparison.OrdinalIgnoreCase))
                {
                    return link.Href;
                }
            }
            return null;
        }

        public enum PaymentMethod
        {
            MPesa,
            PayPal,
            Card
        }

        public class PaymentRequest
        {
            public float Amount { get; set; }
            public string Description { get; set; }
            public string TransactionId { get; set; }
            public string Currency { get; set; } = "KES";
        }

        public class PaymentResult
        {
            public bool Success { get; set; }
            public string Error { get; set; }
            public string TransactionId { get; set; }
            public PaymentMethod PaymentMethod { get; set; }
            public string PaymentUrl { get; set; }
            public string ClientSecret { get; set; }
        }

        public class UserData
        {
            public string UserId { get; set; }
            public string PhoneNumber { get; set; }
            public BiometricData BiometricData { get; set; }
            public LocationData LocationData { get; set; }
            public DeviceData DeviceData { get; set; }
        }
    }
} 