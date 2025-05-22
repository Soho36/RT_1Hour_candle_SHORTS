#region Using declarations
using System;
using NinjaTrader.NinjaScript;
using NinjaTrader.Cbi;
using System.ComponentModel.DataAnnotations;
#endregion

namespace NinjaTrader.NinjaScript.Strategies
{
    public class CustomAtmStrategy : Strategy
    {
        private string atmStrategyId = string.Empty;
        private string orderId = string.Empty;
        private bool isAtmStrategyCreated = false;

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Risk Reward Ratio", Order = 1, GroupName = "Parameters")]
        public double RiskRewardRatio { get; set; } = 1;

        private double stopLossPrice;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "ATM strategy with SL at candle low and TP at high if R:R > 1:1.";
                Name = "DynamicTP_ATM";
                Calculate = Calculate.OnEachTick;
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 2 || State == State.Historical)
                return;

            // 1. Place ATM order if not already placed
            if (!isAtmStrategyCreated && string.IsNullOrEmpty(atmStrategyId))
            {
                if (Close[0] > Open[0])
                {
                    atmStrategyId = GetAtmStrategyUniqueId();
                    orderId = GetAtmStrategyUniqueId();
                    stopLossPrice = Low[1];

                    AtmStrategyCreate(OrderAction.Buy, OrderType.Market, 0, 0, TimeInForce.Day,
                        orderId, "AtmStrategyTemplate", atmStrategyId,
                        (error, id) =>
                        {
                            if (error == ErrorCode.NoError && id == atmStrategyId)
                                isAtmStrategyCreated = true;
                        });
                }
            }

            // 2. If ATM strategy is active and position is long, monitor for TP adjustment
            if (isAtmStrategyCreated)
            {
                var position = GetAtmStrategyMarketPosition(atmStrategyId);
                if (position == MarketPosition.Long)
                {
                    double entryPrice = GetAtmStrategyPositionAveragePrice(atmStrategyId);
                    double rrDistance = entryPrice - stopLossPrice;
                    double minTargetPrice = entryPrice + rrDistance * RiskRewardRatio;

                    if (Close[0] >= minTargetPrice)
                    {
                        double tpPrice = High[0];
                        AtmStrategyChangeStopTarget(0, stopLossPrice, "STOP1", atmStrategyId);
                        AtmStrategyChangeStopTarget(tpPrice, 0, "TP1", atmStrategyId);
                    }
                }
                else if (position == MarketPosition.Flat)
                {
                    // Reset if strategy finished
                    atmStrategyId = string.Empty;
                    orderId = string.Empty;
                    isAtmStrategyCreated = false;
                }
            }
        }
    }
}
