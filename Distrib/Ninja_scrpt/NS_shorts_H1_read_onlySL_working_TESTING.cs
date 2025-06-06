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
using System.Globalization;
using System.Threading;
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
    public class MyCustomStrategyShortOnly : Strategy
	
    {
		[NinjaScriptProperty]
		[Display(Name = "Signal File Path", Order = 1, GroupName = "File Paths")]
		public string SignalFilePath { get; set; }
        
		[NinjaScriptProperty]
		[Display(Name = "Position State File Path", Order = 2, GroupName = "File Paths")]
		public string PositionStateFilePath { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Position Entry Price File Path", Order = 3, GroupName = "File Paths")]
		public string PositionEntryPriceFilePath { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "SL orders file path", Order = 4, GroupName = "File Paths")]
		public string SLOrdersFilePath { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "OB Candle H/L File Path", Order = 5, GroupName = "File Paths")]
		public string OBCandleHighLowPath { get; set; }	
		
		private bool executeShortTrade = false;

		private double entryPriceShortOnly = 0;
		private double stopPrice = 0;

        private double targetPrice1 = 0;

		private Order shortOrder1;
		
		private Order slOrder = null;

		private string lastPositionState = "closed"; // Tracks the last written position state
		private bool hasPrintedEmptySignalMessage = false; // Flag to track if the empty signal message has been printed
		private bool hasPrintedExceptionMessage = false; // Flag to track if the empty signal message has been printed
		private bool hasPrintedSLSubmittedMessage = false;
		private bool hasPrintedSLFailureMessage = false;
		private bool hasPrintedSLWarningMessage = false;
		private bool hasPrintedFileErrorMessage = false;
		private double lastSubmittedSL = 0;

		
		// Declare a Dictionary to Track Order Ages
		private Dictionary<string, int> orderCreationCandle = new Dictionary<string, int>();

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)	// DEFAULT PATHS
            {	
				SignalFilePath = @"C:\Users\Liikurserv\PycharmProjects\RT_1Hour_candle_SHORTS\trade_signal.txt";
				PositionStateFilePath = @"C:\Users\Liikurserv\PycharmProjects\RT_1Hour_candle_SHORTS\active_position.csv";
				PositionEntryPriceFilePath = @"C:\Users\Liikurserv\PycharmProjects\RT_1Hour_candle_SHORTS\entry_price.csv";
				SLOrdersFilePath = @"C:\Users\Liikurserv\PycharmProjects\RT_1Hour_candle_SHORTS\sl_orders.csv";
				OBCandleHighLowPath = @"C:\Users\Liikurserv\PycharmProjects\RT_1Hour_candle_SHORTS\OB_candle_HL.csv";
	
                Name = "Filetransmit_ShortOnly";
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

			if (File.Exists(SignalFilePath))
			{
				try
				{
					string signal = File.ReadAllText(SignalFilePath).Trim();
					if (string.IsNullOrEmpty(signal))
					{
						if (!hasPrintedEmptySignalMessage)
						{
							Print($"[{DateTime.Now:HH:mm:ss}] Signal file is empty. No action will be taken.");
							hasPrintedEmptySignalMessage = true; // Set the flag to true after printing
						}
						return; // Exit early if the file is empty
					}
					hasPrintedEmptySignalMessage = false; // Reset the flag if the file is not empty
					string[] parts = signal.Split(',');

					// Handle Cancel signal
					if (signal.Equals("Cancel", StringComparison.OrdinalIgnoreCase))
					{
						CancelAllOrders();
						Print($"[{DateTime.Now:HH:mm:ss}] Received Cancel signal. All active orders have been cancelled.");
						File.WriteAllText(SignalFilePath, string.Empty); // Clear the signal file
						return; // Exit early as no further action is needed
					}
					if (parts.Length == 6)
						Print($"[{DateTime.Now:HH:mm:ss}] Signal file content: {signal}");
					{
						string tradeDirection = parts[0].Trim();                    // Direction
						if (
							double.TryParse(parts[1].Trim(), out entryPriceShortOnly) &&     // entryPriceShortOnly
							double.TryParse(parts[2].Trim(), out stopPrice) &&      // Stop-loss price
							double.TryParse(parts[3].Trim(), out targetPrice1)   // Take-profit1 price
							)
						{
							if (tradeDirection.Equals("Sell", StringComparison.OrdinalIgnoreCase))
							{
								if (Position.MarketPosition == MarketPosition.Flat)
								{
									executeShortTrade = true;
									File.WriteAllText(SignalFilePath, string.Empty);
								}
							}
						}
					}
					// Only proceed if a position is open
					if (Position.MarketPosition != MarketPosition.Flat)
					{
						for (int i = 0; i < 5; i++)
						{
							try
							{
								string slText = File.ReadAllText(SLOrdersFilePath);
								double stopLossPrice = double.Parse(slText, CultureInfo.InvariantCulture);

								if (slOrder != null && slOrder.OrderState == OrderState.Working)
									CancelOrder(slOrder);

								// Reset flags if SL changed
								if (stopLossPrice != lastSubmittedSL)
								{
									lastSubmittedSL = stopLossPrice;
									hasPrintedSLSubmittedMessage = false;
									hasPrintedSLFailureMessage = false;
									hasPrintedSLWarningMessage = false;
								}

								if (stopLossPrice > GetCurrentAsk())
								{
									slOrder = ExitShortStopMarket(Position.Quantity, stopLossPrice, "SL_Stop", "");

									if (slOrder != null)
									{
										if (!hasPrintedSLSubmittedMessage)
										{
											Print($"[{DateTime.Now:HH:mm:ss}][✅ SL SUBMITTED] {stopLossPrice}");
											hasPrintedSLSubmittedMessage = true;
										}
									}
									else
									{
										if (!hasPrintedSLFailureMessage)
										{
											Print($"[{DateTime.Now:HH:mm:ss}][❌ ERROR] SL order was not submitted. Value: {stopLossPrice}, Qty: {Position.Quantity}");
											hasPrintedSLFailureMessage = true;
										}
									}
								}
								
							}
							catch (Exception ex)
							{
								if (!hasPrintedFileErrorMessage)
								{
									Print($"[{DateTime.Now:HH:mm:ss}][❌ ERROR] Reading TP/SL file: {ex.Message}");
									hasPrintedFileErrorMessage = true;
								}
								// Optional small delay to give Python time
								Thread.Sleep(100);
							}
						}
					}
					else
					{
						// Reset all flags when flat
						hasPrintedSLSubmittedMessage = false;
						hasPrintedSLFailureMessage = false;
						hasPrintedSLWarningMessage = false;
						hasPrintedFileErrorMessage = false;
						lastSubmittedSL = 0;
					}
				}
				catch (Exception ex)
				{
					if (!hasPrintedExceptionMessage)
						{
							Print($"[{DateTime.Now:HH:mm:ss}]Error reading signal file: {ex.Message}");
							hasPrintedExceptionMessage = true; // Set the flag to true after printing
						}
						return; // Exit early if the file is empty
				}
				hasPrintedExceptionMessage = false; // Reset the flag if the file is not empty

			}

			// Handle short positions
			if (executeShortTrade)
			{
				try
				{
					if (shortOrder1 == null || shortOrder1.OrderState != OrderState.Working)
					{
						if (entryPriceShortOnly >= GetCurrentBid())
						{
							Print($"[{DateTime.Now:HH:mm:ss}] Error: Sell stop order price must be above the current market price.");
							executeShortTrade = false; // Reset flag
							return; // Exit without placing the order
						}

						shortOrder1 = EnterShortStopMarket(0, true, 1, entryPriceShortOnly, "Short1"); // Third parameter is an order size
                        orderCreationCandle[shortOrder1.OrderId] = CurrentBar; // Track candle index for the order
						// SetStopLoss("Short1", CalculationMode.Price, stopPrice, false);
						// SetProfitTarget("Short1", CalculationMode.Price, targetPrice1);
						Print($"[{DateTime.Now:HH:mm:ss}] 1-st SHORT stop-market order placed at {entryPriceShortOnly} with TP1: no TP, SL: {stopPrice}");
					}
				}
				catch (Exception ex)
				{
					Print($"[{DateTime.Now:HH:mm:ss}] Error placing short orders: {ex.Message}");
				}
				executeShortTrade = false; // Reset flag
			}
		}


		protected override void OnExecutionUpdate(Cbi.Execution execution, string executionId, double price, int quantity, MarketPosition marketPosition, string orderId, DateTime time)
		{
			base.OnExecutionUpdate(execution, executionId, price, quantity, marketPosition, orderId, time);

			// Determine the position state with direction
			string currentPositionState;

			if (Position.MarketPosition == MarketPosition.Flat)
				currentPositionState = "closed";
			else if (Position.MarketPosition == MarketPosition.Short)
				currentPositionState = "opened_short";
			/* else if (Position.MarketPosition == MarketPosition.Short)
				currentPositionState = "opened_short"; */
			else
				return; // Skip if unknown state

			// Write to the file only if the state has changed
			if (currentPositionState != lastPositionState)
			{
				try
				{	// Write position state to file (closed or opened_short)
					File.WriteAllText(PositionStateFilePath, currentPositionState);
					Print($"[{DateTime.Now:HH:mm:ss}] Position state updated: {currentPositionState}");

					// Write actual entry price to file (Culture-invariant formatting)
		            string entryPriceText = Position.AveragePrice.ToString(CultureInfo.InvariantCulture);
		            File.WriteAllText(PositionEntryPriceFilePath, entryPriceText);
		            Print($"[{DateTime.Now:HH:mm:ss}] [ENTRY PRICE] Written to file: {entryPriceText}");	

					lastPositionState = currentPositionState; // Update the tracked state


					// ✅ Place SL when short is opened
					if (currentPositionState == "opened_short")
					{
						double slPrice = GetLastGreenCandleHigh();

						if (slOrder != null && slOrder.OrderState == OrderState.Working)
							CancelOrder(slOrder);

						slOrder = ExitShortStopMarket(Position.Quantity, slPrice, "SL_LastGreen", "");
						Print($"[{DateTime.Now:HH:mm:ss}] [SL SET] Stop-loss placed at last green candle high: {slPrice}");
					}

					// ✅ Clear OB candle file when position is closed
			        if (currentPositionState == "closed")
			        {
			            File.WriteAllText(OBCandleHighLowPath, "");
			            Print($"[{DateTime.Now:HH:mm:ss}] [INFO] OB Candle file {OBCandleHighLowPath} cleared.");
			        }

			        // ✅ Clear sl_orders.csv file when position is closed
			        if (currentPositionState == "closed")
			        {
			            File.WriteAllText(SLOrdersFilePath, "");
			            Print($"[{DateTime.Now:HH:mm:ss}] [INFO] sl_orders.csv file {SLOrdersFilePath} cleared.");
			        }		

				}

				catch (Exception ex)
				{
					Print($"[{DateTime.Now:HH:mm:ss}] Error writing position state to file: {ex.Message}");
				}
			}
		}


		private double GetLastGreenCandleHigh(int lookbackBars = 10)
		{
			for (int i = 1; i <= lookbackBars; i++) // Start from 1 to avoid the current forming bar
			{
				if (Close[i] > Open[i])
				{
					Print($"[{DateTime.Now:HH:mm:ss}] [INFO] Found green candle at bar index {i}, high: {High[i]}");
					return High[i];
				}
			}
			Print($"[{DateTime.Now:HH:mm:ss}] [WARN] No green candle found in lookback range");
			return High[1]; // Fallback if no green candle is found
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
						Print($"[{DateTime.Now:HH:mm:ss}] Cancelled order: {order.Name}");
					}
				}
			}
			catch (Exception ex)
			{
				Print($"[{DateTime.Now:HH:mm:ss}] Error canceling orders: {ex.Message}");
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
								Print($"[{DateTime.Now:HH:mm:ss}] [{DateTime.Now:HH:mm:ss}] Order {order.Name} is {candleAge} candles old and will be canceled.");
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
						Print($"[{DateTime.Now:HH:mm:ss}] Cancelled order: {orderToCancel.Name} due to time limit threshold");
						orderCreationCandle.Remove(orderId); // Remove from tracking
					}
				}
			}
			catch (Exception ex)
			{
				Print($"[{DateTime.Now:HH:mm:ss}] Error in CancelOldOrders: {ex.Message}");
			}
		}
    }
}
