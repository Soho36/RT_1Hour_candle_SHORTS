if (Position.MarketPosition != MarketPosition.Flat)
					{
						try
						{
							// Read the TP value from file
							string tpText = File.ReadAllText(TPOrdersFilePath);
							double takeProfitPrice = double.Parse(tpText, CultureInfo.InvariantCulture);
							
							if (tpOrder != null && tpOrder.OrderState == OrderState.Working)
							Print($"New TP read from file: {takeProfitPrice}");
							{
								CancelOrder(tpOrder);
							}
							// Submit new sell limit order for take profit
							tpOrder = ExitLongLimit(Position.Quantity, takeProfitPrice, "TP_Limit", "");


							Print($"Submitted TP Sell-Limit at {takeProfitPrice}");

						}
						catch (Exception ex)
						{
							Print($"Error reading TP file: {ex.Message}");
						}
					}