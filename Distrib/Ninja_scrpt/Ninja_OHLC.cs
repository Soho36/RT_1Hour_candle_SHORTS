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

// The namespace and class name must match NinjaTrader conventions
namespace NinjaTrader.NinjaScript.Strategies
{
    public class SaveOHLCVToFile : Strategy
    {	
		public enum OHLCPathOption
		{
			Shorts,
			Longs
		}
		[NinjaScriptProperty]
		[Display(Name = "Choose OHLC Path", Order = 1, GroupName = "File Paths")]
		public OHLCPathOption SelectedPathOption { get; set; }
		
        private bool isLiveData = false;
		private string OHLCFilePath;
		
        // OnStateChange is used to initialize the strategy
        protected override void OnStateChange()
        {
            if (State == State.Configure)
			{
				switch (SelectedPathOption)
				{
					case OHLCPathOption.Shorts:
						OHLCFilePath = @"C:\Users\Liikurserv\PycharmProjects\RT_1Hour_candle_SHORTS\OHLCVData_1.csv";
						break;
					case OHLCPathOption.Longs:
						OHLCFilePath = @"C:\Users\Liikurserv\PycharmProjects\RT_1Hour_candle\OHLCVData_1.csv";
						break;
				}
			}
            
            else if (State == State.Realtime)
            {
                // Transitioning to real-time
                isLiveData = true;
            }
        }

        // OnBarUpdate is called for each new bar
        protected override void OnBarUpdate()
        {
            // Skip historical bars
            if (!isLiveData)
                return;

            // Extract OHLCV data from the previous bar
            double open = Open[0];
            double high = High[0];
            double low = Low[0];
            double close = Close[0];
			double volume = Volume[0];

            // Get current date and time
            DateTime now = Time[0];
            string currentDate = now.ToString("yyyy.MM.dd");
            string currentTime = now.ToString("HH:mm");

            // Write data to the file
            string dataLine = string.Join(";",
                Instrument.FullName,
                BarsPeriod.Value + BarsPeriod.BarsPeriodType.ToString(),
                currentDate,
                currentTime,
                open.ToString("F2"),
                high.ToString("F2"),
                low.ToString("F2"),
                close.ToString("F2"),
				volume.ToString("F2"));

            Print("New line saved to file: " + dataLine);
            File.AppendAllText(OHLCFilePath, dataLine + Environment.NewLine);
        }
    }
}