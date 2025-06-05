using System;
using System.IO;
using NinjaTrader.NinjaScript;

// The namespace and class name must match NinjaTrader conventions
namespace NinjaTrader.NinjaScript.Strategies
{
    public class SaveOHLCVToFile : Strategy
    {
        // private string filePath = "E:\\YandexDisk\\Documents\\55\\OHLCVData_1.csv";
		// private string filePath = "C:\\Users\\Liikurserv\\PycharmProjects\\RT_Ninja\\OHLCVData_1.csv";
		private string filePath = "C:\\Users\\Liikurserv\\PycharmProjects\\RT_1Hour_candle_SHORTS\\OHLCVData_1.csv";
        private bool isLiveData = false;

        // OnStateChange is used to initialize the strategy
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "SaveOHLCVToFile";
                Calculate = Calculate.OnBarClose; // Trigger logic on bar close
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
            File.AppendAllText(filePath, dataLine + Environment.NewLine);
        }
    }
}