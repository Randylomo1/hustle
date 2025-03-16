using System;

namespace NairobiHustle.Payment
{
    [System.Serializable]
    public class MPesaTransaction
    {
        public string transactionId;
        public float amount;
        public string phoneNumber;
        public DateTime timestamp;
        public string status;
        public string resultCode;
        public string resultDescription;
    }
}