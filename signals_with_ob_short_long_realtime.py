from data_handling_realtime import active_position, write_sl_tp, get_entry_price, get_initial_sl, save_ob_candle_ohlc
from colorama import Fore, Style, init


init(autoreset=True)
"""
Main function analyzing price interaction with levels and long/short signals generation logics
"""


def hourly_engulf_signals(
        output_df,
        # max_time_waiting_for_entry,
        current_candle_max_size,
        current_candle_min_size,
        # ob_candle_max_size,
        # ob_candle_min_size
):

    n_index = None
    s_signal = None
    t_price = None
    s_time = None
    candle_counter = 0
    signals_counter = 0

    output_df.reset_index(inplace=True)

    """
    Function to check if the time difference has exceeded the time limit and print the necessary information.
    Returns True if the time limit is exceeded, otherwise False.
    """

    """
    Print triggered signals
    """
    def signal_triggered_output(
            nn_index,
            sig_time,
            tt_price,
            t_side,
            ss_signal
    ):

        print(
            "++++++++++++++++++++++++++\n"
            f"+ {t_side.capitalize()} triggered at index {nn_index}, "
            f"Time: {sig_time}, "
            f"Stop-market price: {tt_price}\n"
            f"+ s_signal: {ss_signal}\n"
            "++++++++++++++++++++++++++"
        )
        print('-----------------------------------------------------------------------------------------------------')
        return ss_signal, nn_index, tt_price, sig_time     # RETURNS SIGNAL FOR send_buy_sell_orders()

    # Start looping through dataframe
    for index, row in output_df.iterrows():
        candle_counter += 1

        current_candle_open = row['Open']
        current_candle_close = row['Close']
        current_candle_high = row['High']
        current_candle_low = row['Low']
        current_candle_time = row['Time']
        current_candle_range = abs(current_candle_high - current_candle_low)
        # trailing_sl_for_longs = current_candle_low
        flatten_at_candle_close = current_candle_close

        if active_position() == 'opened_short':
            print(Fore.GREEN + Style.DIM + "Active position is open (shorts)")
            entry_price = get_entry_price()
            initial_sl_shorts = get_initial_sl()
            print(f'entry_price: {entry_price}'.upper())
            print(f'initial_sl_shorts: {initial_sl_shorts}'.upper())

            try:
                risk_points = abs(initial_sl_shorts - entry_price)

            except TypeError:
                write_sl_tp(flatten_at_candle_close)
                print(f"TypeError: {entry_price} or {initial_sl_shorts} is None")
                print(Fore.RED + Style.DIM + "Flattening position because of error".upper())
                risk_points = 0

            print(f'risk_points: {risk_points}'.upper())

            if entry_price - current_candle_close > risk_points:
                write_sl_tp(flatten_at_candle_close)

            else:
                write_sl_tp(initial_sl_shorts)
                print("SL and TP orders are written to file (shorts)")

        # +------------------------------------------------------------------+
        # LONGS
        # +------------------------------------------------------------------+

        if current_candle_close < current_candle_open:  # Check if current candle is RED:
            if current_candle_max_size >= current_candle_range >= current_candle_min_size:
                print(
                    f"○ RED candle found at index {index}, candle size is OK, "
                    f"Time: {current_candle_time}\n"
                    f"Min: {current_candle_min_size}, Body: {current_candle_range}, Max: {current_candle_max_size}"
                )

                print(Fore.GREEN + Style.BRIGHT + 'SEND BUY_STOP')
                signal = f'100+{index}'
                signals_counter += 1
                side = 'long'

                # Save the candle data to a file

                # This part is commented out because otherwise it will save the OB for longs every time
                # but this script is for shorts only

                # if active_position() != 'opened_long':
                #     print("There is no active position, OB candle low is saved")
                #     save_ob_candle_ohlc(current_candle_low)

                s_signal, n_index, t_price, s_time = signal_triggered_output(
                    index,
                    current_candle_time,
                    current_candle_high,
                    side,
                    signal
                )

            else:
                print("RED candle size is not OK")
                print(Fore.YELLOW + Style.BRIGHT + "RED candle size is not OK")

        # +------------------------------------------------------------------+
        # SHORTS
        # +------------------------------------------------------------------+

        if current_candle_close > current_candle_open:  # Check if current candle is GREEN
            if current_candle_max_size >= current_candle_range >= current_candle_min_size:
                print(
                    f"○ GREEN candle found at index {index}, candle size is OK, "
                    f"Time: {current_candle_time}\n"
                    f"Min: {current_candle_min_size}, Body: {current_candle_range}, Max: {current_candle_max_size}"
                )

                print(Fore.RED + Style.BRIGHT + 'SEND SELL_STOP')
                signal = f'-100+{index}'

                signals_counter += 1
                side = 'short'

                # Save the candle data to a file
                if active_position() != 'opened_short':
                    print("There is no active position, OB candle high is saved")
                    save_ob_candle_ohlc(current_candle_high)

                s_signal, n_index, t_price, s_time = signal_triggered_output(
                    index,
                    current_candle_time,
                    current_candle_low,
                    side,
                    signal
                )

            else:
                print("GREEN candle size is not OK")
                print(Fore.YELLOW + Style.BRIGHT + "GREEN candle size is not OK")

    return (
            s_signal,   # signal 100 or -100
            n_index,    # index
            t_price,    # stop-market order price
            candle_counter,
            s_time,
            signals_counter
    )
