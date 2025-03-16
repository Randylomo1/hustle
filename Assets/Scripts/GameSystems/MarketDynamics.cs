using UnityEngine;
using System;
using System.Collections.Generic;
using NairobiHustle.Payment;

namespace NairobiHustle.GameSystems
{
    public class MarketDynamics : MonoBehaviour
    {
        [Header("Market Conditions")]
        [SerializeField] private float fuelPrice = 150f; // KES per liter
        [SerializeField] private float fuelPriceVolatility = 0.1f;
        [SerializeField] private float inflationRate = 0.06f; // 6% annual inflation
        [SerializeField] private float competitionIntensity = 0.7f; // 0-1 scale

        [Header("Route Economics")]
        [SerializeField] private RouteEconomics[] routeEconomics = new RouteEconomics[]
        {
            new RouteEconomics {
                routeName = "Eastlands Express",
                baseFare = 80f,
                distance = 15f, // km
                competitorCount = 8,
                peakHourMultiplier = 1.5f,
                offPeakDiscount = 0.8f,
                studentDiscount = 0.7f
            },
            new RouteEconomics {
                routeName = "Westlands Connect",
                baseFare = 100f,
                distance = 12f,
                competitorCount = 6,
                peakHourMultiplier = 1.8f,
                offPeakDiscount = 0.7f,
                studentDiscount = 0.8f
            },
            new RouteEconomics {
                routeName = "Thika Superhighway",
                baseFare = 150f,
                distance = 25f,
                competitorCount = 10,
                peakHourMultiplier = 1.6f,
                offPeakDiscount = 0.75f,
                studentDiscount = 0.8f
            }
        };

        [Header("Vehicle Economics")]
        [SerializeField] private VehicleEconomics[] vehicleEconomics = new VehicleEconomics[]
        {
            new VehicleEconomics {
                type = "14-Seater",
                fuelConsumption = 12f, // km/l
                maintenanceCost = 2000f, // per week
                insuranceCost = 15000f, // per month
                licenseRenewal = 8000f, // per year
                resaleValue = 0.85f // percentage after 1 year
            },
            new VehicleEconomics {
                type = "33-Seater",
                fuelConsumption = 8f,
                maintenanceCost = 4000f,
                insuranceCost = 25000f,
                licenseRenewal = 12000f,
                resaleValue = 0.80f
            }
        };

        [Header("SACCO Economics")]
        [SerializeField] private SACCOEconomics[] saccoEconomics = new SACCOEconomics[]
        {
            new SACCOEconomics {
                name = "Metro Trans",
                membershipFee = 50000f,
                monthlyDues = 2000f,
                savingsInterest = 0.08f,
                loanInterest = 0.12f,
                benefitsMultiplier = 1.2f
            },
            new SACCOEconomics {
                name = "City Hoppa",
                membershipFee = 75000f,
                monthlyDues = 3000f,
                savingsInterest = 0.10f,
                loanInterest = 0.14f,
                benefitsMultiplier = 1.3f
            }
        };

        private AfricanAuthenticity africanAuthenticity;
        private CareerProgression careerProgression;
        private Dictionary<string, float> routePrices;
        private Dictionary<string, int> routePassengers;
        private float currentFuelPrice;
        private DateTime lastUpdate;

        private void Awake()
        {
            africanAuthenticity = GetComponent<AfricanAuthenticity>();
            careerProgression = GetComponent<CareerProgression>();
            routePrices = new Dictionary<string, float>();
            routePassengers = new Dictionary<string, int>();
            InitializeMarket();
        }

        private void InitializeMarket()
        {
            currentFuelPrice = fuelPrice;
            lastUpdate = DateTime.Now;
            
            foreach (RouteEconomics route in routeEconomics)
            {
                routePrices[route.routeName] = route.baseFare;
                routePassengers[route.routeName] = 0;
            }

            InvokeRepeating("UpdateMarketDynamics", 0f, 3600f); // Update every hour
        }

        private void UpdateMarketDynamics()
        {
            UpdateFuelPrice();
            UpdateRoutePrices();
            UpdateCompetition();
            UpdateSACCOBenefits();
        }

        private void UpdateFuelPrice()
        {
            // Simulate fuel price fluctuations
            float volatility = UnityEngine.Random.Range(-fuelPriceVolatility, fuelPriceVolatility);
            currentFuelPrice = Mathf.Max(fuelPrice * (1f + volatility), fuelPrice * 0.8f);

            // Apply inflation
            TimeSpan timeSinceLastUpdate = DateTime.Now - lastUpdate;
            float yearFraction = (float)timeSinceLastUpdate.TotalDays / 365f;
            currentFuelPrice *= (1f + (inflationRate * yearFraction));
        }

        private void UpdateRoutePrices()
        {
            foreach (RouteEconomics route in routeEconomics)
            {
                float basePrice = CalculateBasePrice(route);
                float competitionFactor = CalculateCompetitionFactor(route);
                float demandFactor = CalculateDemandFactor(route);
                float weatherFactor = africanAuthenticity.GetRouteMultiplier(route.routeName);

                float finalPrice = basePrice * competitionFactor * demandFactor * weatherFactor;
                routePrices[route.routeName] = Mathf.Round(finalPrice / 10f) * 10f; // Round to nearest 10 KES
            }
        }

        private float CalculateBasePrice(RouteEconomics route)
        {
            // Calculate cost-based price
            float fuelCost = (route.distance / vehicleEconomics[0].fuelConsumption) * currentFuelPrice;
            float maintenanceCost = vehicleEconomics[0].maintenanceCost / (7f * 10f); // Per trip estimate
            float operatingCost = fuelCost + maintenanceCost;

            return Mathf.Max(route.baseFare, operatingCost * 1.3f); // Minimum 30% margin
        }

        private float CalculateCompetitionFactor(RouteEconomics route)
        {
            // More competitors = lower prices
            return 1f - (route.competitorCount * 0.02f * competitionIntensity);
        }

        private float CalculateDemandFactor(RouteEconomics route)
        {
            DateTime now = DateTime.Now;
            bool isPeakHour = IsPeakHour(now.Hour);
            bool isWeekend = now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday;

            if (isPeakHour && !isWeekend)
            {
                return route.peakHourMultiplier;
            }
            else if (isWeekend || now.Hour < 6 || now.Hour > 21)
            {
                return route.offPeakDiscount;
            }

            return 1.0f;
        }

        private bool IsPeakHour(int hour)
        {
            return (hour >= 6 && hour <= 9) || (hour >= 17 && hour <= 19);
        }

        private void UpdateCompetition()
        {
            foreach (RouteEconomics route in routeEconomics)
            {
                // Simulate competitor behavior
                float competitorPrice = routePrices[route.routeName] * UnityEngine.Random.Range(0.9f, 1.1f);
                
                // Adjust our price if competitors are significantly cheaper
                if (competitorPrice < routePrices[route.routeName] * 0.9f)
                {
                    routePrices[route.routeName] = Mathf.Lerp(
                        routePrices[route.routeName],
                        competitorPrice * 1.05f,
                        competitionIntensity
                    );
                }
            }
        }

        private void UpdateSACCOBenefits()
        {
            foreach (SACCOEconomics sacco in saccoEconomics)
            {
                // Update SACCO benefits based on market conditions
                float marketMultiplier = 1f + (currentFuelPrice / fuelPrice - 1f) * 0.5f;
                sacco.currentBenefitsMultiplier = sacco.benefitsMultiplier * marketMultiplier;

                // Adjust interest rates based on market
                sacco.currentSavingsInterest = Mathf.Max(
                    sacco.savingsInterest * marketMultiplier,
                    sacco.savingsInterest * 0.8f
                );
            }
        }

        public float GetCurrentFare(string routeName, bool isStudent = false)
        {
            if (!routePrices.ContainsKey(routeName))
            {
                return 0f;
            }

            RouteEconomics route = Array.Find(
                routeEconomics,
                r => r.routeName == routeName
            );

            float fare = routePrices[routeName];
            
            if (isStudent && route != null)
            {
                fare *= route.studentDiscount;
            }

            return Mathf.Round(fare / 10f) * 10f; // Round to nearest 10 KES
        }

        public float GetOperatingCosts(string routeName)
        {
            RouteEconomics route = Array.Find(
                routeEconomics,
                r => r.routeName == routeName
            );

            if (route == null)
            {
                return 0f;
            }

            float fuelCost = (route.distance / vehicleEconomics[0].fuelConsumption) * currentFuelPrice;
            float maintenanceCost = vehicleEconomics[0].maintenanceCost / (7f * 10f);
            float insuranceCost = vehicleEconomics[0].insuranceCost / (30f * 10f);
            float licenseCost = vehicleEconomics[0].licenseRenewal / (365f * 10f);

            return fuelCost + maintenanceCost + insuranceCost + licenseCost;
        }

        public float GetSACCOBenefits(string saccoName)
        {
            SACCOEconomics sacco = Array.Find(
                saccoEconomics,
                s => s.name == saccoName
            );

            return sacco != null ? sacco.currentBenefitsMultiplier : 1f;
        }
    }

    [System.Serializable]
    public class RouteEconomics
    {
        public string routeName;
        public float baseFare;
        public float distance;
        public int competitorCount;
        public float peakHourMultiplier;
        public float offPeakDiscount;
        public float studentDiscount;
    }

    [System.Serializable]
    public class VehicleEconomics
    {
        public string type;
        public float fuelConsumption;
        public float maintenanceCost;
        public float insuranceCost;
        public float licenseRenewal;
        public float resaleValue;
    }

    [System.Serializable]
    public class SACCOEconomics
    {
        public string name;
        public float membershipFee;
        public float monthlyDues;
        public float savingsInterest;
        public float loanInterest;
        public float benefitsMultiplier;
        public float currentBenefitsMultiplier;
        public float currentSavingsInterest;
    }
}