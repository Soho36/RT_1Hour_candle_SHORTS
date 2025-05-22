#region Using declarations
using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

namespace NinjaTrader.NinjaScript.Strategies
{
    public class MyCustomStrategyLongOnly : Strategy
    {
        private bool executeLongTrade = false;
        
		private double entryPriceLongOnly = 0;
		private double stopPrice = 0;
		
        private double targetPrice1 = 0;
        private double targetPrice2 = 0;
		private double targetPrice3 = 0;
		
		private Order longOrder1;
		private Order longOrder2;
		private Order longOrder3;
		
		private string lastPositionState = "closed"; // Tracks the last written position state
		
		// private string positionStateFilePath = "C:\\Users\\Liikurserv\\PycharmProjects\\RT_Ninja\\position_state.txt";		
		private string activeOrdersFilePath = "C:\\Users\\Liikurserv\\PycharmProjects\\RT_Ninja\\current_order_direction.txt";
		private string positionStateFilePath = "C:\\Users\\Liikurserv\\PycharmProjects\\RT_Ninja\\position_state_longs.txt";
		
		// Declare a Dictionary to Track Order Ages
		private Dictionary<string, int> orderCreationCandle = new Dictionary<string, int>();

		// Track whether stop losses have been moved to breakeven
		bool stopLossMovedToBreakevenLong1 = false;
		bool stopLossMovedToBreakevenShort1 = false;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "Filetransmit_LongOnly";
                Calculate = Calculate.OnEachTick;
                EntriesPerDirection = 1; // Allow entries
                EntryHandling = EntryHandling.UniqueEntries;
            }
        }

        protected override void OnBarUpdate()
		{
			// Cancel orders older than 5 candles
			CancelOldOrders(CurrentBar, 5);
			if (CurrentBars[0] < BarsRequiredToTrade)
				return;

			string signalFilePath = "C:\\Users\\Liikurserv\\PycharmProjects\\RT_Ninja\\trade_signal.txt";

			if (File.Exists(signalFilePath))
			{
				try
				{
					string signal = File.ReadAllText(signalFilePath).Trim();
					string[] parts = signal.Split(',');
					// Handle Cancel signal
					if (signal.Equals("Cancel", StringComparison.OrdinalIgnoreCase))
					{
						CancelAllOrders();
						Print("Received Cancel signal. All active orders have been cancelled.");
						File.WriteAllText(signalFilePath, string.Empty); // Clear the signal file
						return; // Exit early as no further action is needed
					}
					if (parts.Length == 6)
					{
						string tradeDirection = parts[0].Trim();                    // Direction
						if (
							double.TryParse(parts[1].Trim(), out entryPriceLongOnly) &&     // entryPriceLongOnly
							double.TryParse(parts[2].Trim(), out stopPrice) &&      // Stop-loss price
							double.TryParse(parts[3].Trim(), out targetPrice1) &&   // Take-profit1 price
							double.TryParse(parts[4].Trim(), out targetPrice2) &&	// Take-profit2 price
							double.TryParse(parts[5].Trim(), out targetPrice3)		// Take-profit3 price
							)             
						{
							if (tradeDirection.Equals("Buy", StringComparison.OrdinalIgnoreCase))
							{
								if (Position.MarketPosition == MarketPosition.Flat)
								{
									executeLongTrade = true;
									File.WriteAllText(signalFilePath, string.Empty);
								}
							}
						}
					}
				}
				catch (Exception ex)
				{
					Print($"Error reading signal file: {ex.Message}");
				}
			}

			// Handle long positions
			if (executeLongTrade)
			{
				try
				{
					if (longOrder1 == null || longOrder1.OrderState != OrderState.Working)
					{
						if (entryPriceLongOnly <= GetCurrentAsk())
						{
							Print("Error: Buy stop order price must be above the current market price.");
							executeLongTrade = false; // Reset flag
							return; // Exit without placing the order
						}

						longOrder1 = EnterLongStopMarket(0, true, 2, entryPriceLongOnly, "Long1");
						orderCreationCandle[longOrder1.OrderId] = CurrentBar; // Track candle index for the order
						SetStopLoss("Long1", CalculationMode.Price, stopPrice, false);
						SetProfitTarget("Long1", CalculationMode.Price, targetPrice1);
						Print($"1-st LONG stop-market order placed at {entryPriceLongOnly} with TP1: {targetPrice1}, SL: {stopPrice}");
					}

					if (longOrder2 == null || longOrder2.OrderState != OrderState.Working)
					{
						if (entryPriceLongOnly <= GetCurrentAsk())
						{
							Print("Error: Buy stop order price must be above the current market price.");
							executeLongTrade = false; // Reset flag
							return; // Exit without placing the order
						}

						longOrder2 = EnterLongStopMarket(0, true, 1, entryPriceLongOnly, "Long2");
						orderCreationCandle[longOrder2.OrderId] = CurrentBar; // Track candle index for the order
						SetStopLoss("Long2", CalculationMode.Price, stopPrice, false);
						SetProfitTarget("Long2", CalculationMode.Price, targetPrice2);
						Print($"2-nd LONG stop-market order placed at {entryPriceLongOnly} with TP2: {targetPrice2}, SL: {stopPrice}");
					}
					if (longOrder3 == null || longOrder3.OrderState != OrderState.Working)
					{
						if (entryPriceLongOnly <= GetCurrentAsk())
						{
							Print("Error: Buy stop order price must be above the current market price.");
							executeLongTrade = false; // Reset flag
							return; // Exit without placing the order
						}

						longOrder3 = EnterLongStopMarket(0, true, 1, entryPriceLongOnly, "Long3");
						orderCreationCandle[longOrder3.OrderId] = CurrentBar; // Track candle index for the order
						SetStopLoss("Long3", CalculationMode.Price, stopPrice, false);
						SetProfitTarget("Long3", CalculationMode.Price, targetPrice3);
						Print($"3-rd LONG stop-market order placed at {entryPriceLongOnly} with TP3: {targetPrice3}, SL: {stopPrice}");
					}
				}
				catch (Exception ex)
				{
					Print($"Error placing long orders: {ex.Message}");
				}
				executeLongTrade = false; // Reset flag
			}


			// Move stop to breakeven for long positions once TP1 is reached
		    if (Position.MarketPosition == MarketPosition.Long && !stopLossMovedToBreakevenLong1)
		    {
		        if (Close[0] >= targetPrice1 - 1 * TickSize)
		        {
		            // Move stop loss to breakeven (entry price) for Long2
		            SetStopLoss("Long2", CalculationMode.Price, Position.AveragePrice, false);
		            Print($"Stop loss moved to breakeven for Long2 at price: {Position.AveragePrice}");
					
					// Move stop loss to breakeven (entry price) for Long3
		            SetStopLoss("Long3", CalculationMode.Price, Position.AveragePrice, false);
		            Print($"Stop loss moved to breakeven for Long3 at price: {Position.AveragePrice}");
					
					stopLossMovedToBreakevenLong1 = true; // Set flag to prevent repeated execution					            
		        }
		    }
		}
    

		protected override void OnExecutionUpdate(Cbi.Execution execution, string executionId, double price, int quantity, MarketPosition marketPosition, string orderId, DateTime time)
		{
			base.OnExecutionUpdate(execution, executionId, price, quantity, marketPosition, orderId, time);

			// Determine the position state with direction
			string currentPositionState;

			if (Position.MarketPosition == MarketPosition.Flat)
				currentPositionState = "closed";
			else if (Position.MarketPosition == MarketPosition.Long)
				currentPositionState = "opened_long";
			/* else if (Position.MarketPosition == MarketPosition.Short)
				currentPositionState = "opened_short"; */
			else
				return; // Safety check, no action for unknown market position

			// Write to the file only if the state has changed
			if (currentPositionState != lastPositionState)
			{
				try
				{
					File.WriteAllText(positionStateFilePath, currentPositionState);
					Print($"Position state updated: {currentPositionState}");
					lastPositionState = currentPositionState; // Update the tracked state
				}
				catch (Exception ex)
				{
					Print($"Error writing position state to file: {ex.Message}");
				}
			}
		}

		protected override void OnOrderUpdate(Order order, double limitPrice, double stopPrice, int quantity, int filled, double averageFillPrice, OrderState orderState, DateTime time, ErrorCode error, string comment)
		{
			try
			{
				// Get all active orders (working or accepted)
				var activeOrders = Account.Orders.Where(o => o.OrderState == OrderState.Working || o.OrderState == OrderState.Accepted).ToList();

				// Check if there are active buy or sell orders
				if (activeOrders.Any(o => o.OrderAction == OrderAction.Buy))
				{
					File.WriteAllText(activeOrdersFilePath, "buy");
				}
				else if (activeOrders.Any(o => o.OrderAction == OrderAction.SellShort))
				{
					File.WriteAllText(activeOrdersFilePath, "sell");
				}
				else
				{
					// No active orders
					File.WriteAllText(activeOrdersFilePath, "");
				}
			}
			catch (Exception ex)
			{
				Print($"Error updating order direction file: {ex.Message}");
			}
		}


		private void CancelAllOrders()
		{
			try
			{
				foreach (Order order in Account.Orders)
				{
					// Cancel only orders associated with this account and in working/accepted state
					if ((order.OrderState == OrderState.Working || order.OrderState == OrderState.Accepted))
					{
						CancelOrder(order);
						Print($"Cancelled order: {order.Name}");
					}
				}
			}
			catch (Exception ex)
			{
				Print($"Error canceling orders: {ex.Message}");
			}
		}
		
		private void CancelOldOrders(int currentCandleIndex, int maxCandleAge)
		{
			try
			{
				// List to track orders to cancel
				List<string> ordersToCancel = new List<string>();

				foreach (Order order in Account.Orders)
				{
					// Skip null or inactive orders
					if (order == null || string.IsNullOrEmpty(order.OrderId))
						continue;

					// Check only orders in working or accepted state
					if (order.OrderState == OrderState.Working || order.OrderState == OrderState.Accepted)
					{	
						// Check if the order is being tracked in the dictionary
						if (orderCreationCandle.TryGetValue(order.OrderId, out int orderCandleIndex))
						{
							// Calculate the age of the order in candles
							int candleAge = currentCandleIndex - orderCandleIndex;
							if (candleAge > maxCandleAge)
							{
								ordersToCancel.Add(order.OrderId);
								Print($"Order {order.Name} is {candleAge} candles old and will be canceled.");
							}
						}
					}
				}

				// Cancel identified orders
				foreach (string orderId in ordersToCancel)
				{
					Order orderToCancel = Account.Orders.FirstOrDefault(o => o.OrderId == orderId);
					if (orderToCancel != null)
					{
						CancelOrder(orderToCancel);
						Print($"Cancelled order: {orderToCancel.Name} due to time limit threshold");
						orderCreationCandle.Remove(orderId); // Remove from tracking
					}
				}
			}
			catch (Exception ex)
			{
				Print($"Error in CancelOldOrders: {ex.Message}");
			}
		}
    }
}
