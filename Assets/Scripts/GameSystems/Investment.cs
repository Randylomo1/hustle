using UnityEngine;
using System;

namespace NairobiHustle.GameSystems
{
    [System.Serializable]
    public class Investment
    {
        public string id;
        public InvestmentOption option;
        public float amount;
        
        [SerializeField] private string startDateString;
        [SerializeField] private string maturityDateString;
        
        [System.NonSerialized]
        public DateTime startDate;
        
        [System.NonSerialized]
        public DateTime maturityDate;
        
        public void OnBeforeSerialize()
        {
            startDateString = startDate.ToString("o");
            maturityDateString = maturityDate.ToString("o");
        }
        
        public void OnAfterDeserialize()
        {
            startDate = DateTime.Parse(startDateString);
            maturityDate = DateTime.Parse(maturityDateString);
        }
    }
}