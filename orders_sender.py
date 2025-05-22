import winsound
import pandas as pd
from datetime import datetime
from data_handling_realtime import (
    save_order_parameters_to_file,
    # get_current_pending_order_direction,
    save_list_of_orders_to_file,
    get_position_state_longs,
    get_position_state_shorts
)
from colorama import Fore, Style, init


init(autoreset=True)


def last_candle_ohlc(output_df_with_levels):
    try:
        last_candle_high = output_df_with_levels['High'].iloc[-1]
        last_candle_low = output_df_with_levels['Low'].iloc[-1]
        last_candle_close = output_df_with_levels['Close'].iloc[-1]
        ticker = output_df_with_levels['Ticker'].iloc[-1]

        return last_candle_high, last_candle_low, last_candle_close, ticker
    except IndexError:
        print("Must be at least two rows in the source file")
        return


def send_buy_sell_orders(
        stop_market_price,
        last_signal,
        current_signal,
        n_index,
        buy_signal,
        sell_signal,
        last_candle_high,
        last_candle_low,
        stop_loss_offset,
        current_order_timestamp,
        last_order_timestamp,
        risk_reward
):

    current_time = pd.to_datetime(datetime.now())
    # current_time -= timedelta(hours=2)              # Sync with MT5 server time
    current_order_timestamp = pd.to_datetime(current_order_timestamp)
    time_difference_current_time_order = None

    if not pd.isna(current_order_timestamp):
        time_difference_current_time_order = ((current_time - current_order_timestamp).total_seconds() / 60)
    print()
    print('Order Sender: ')
    print(f'Last signal: {last_signal}'.upper())
    print(f'Current signal: {current_signal}'.upper())
    print(f'Last order timestamp: {last_order_timestamp}')
    print(f'Recent order timestamp: {current_order_timestamp}')
    print(f'Current time: {current_time}')
    print(f'Time difference: {time_difference_current_time_order}')

    # +------------------------------------------------------------------+
    # BUY ORDERS
    # +------------------------------------------------------------------+
    if get_position_state_longs() == '' or get_position_state_longs() == 'closed' or get_position_state_shorts() == 'opened_short':
        if not pd.isna(current_order_timestamp):
            if current_signal != last_signal:
                # If time difference between current and last order is positive then it's accepted:
                if time_difference_current_time_order < 1:
                    # If there is unique new signal and flag is True:
                    if current_signal == f'100+{n_index}' and buy_signal:  # If there is signal and flag is True:

                        winsound.PlaySound('chord.wav', winsound.SND_FILENAME)
                        print()
                        # print(f'{n_index} ▲ ▲ ▲ Buy order has been sent to NT8! ▲ ▲ ▲ {current_time}'.upper())
                        print(Fore.GREEN + Style.BRIGHT + f'{n_index} ▲ ▲ ▲ Buy order has been sent to NT8! ▲ ▲ ▲ {current_time}'.upper())

                        # ORDER PARAMETERS
                        entry_price = last_candle_high

                        # Stop Loss Price
                        stop_loss_price = round(last_candle_low - stop_loss_offset, 3)
                        risk = entry_price - stop_loss_price  # Distance between entry and stop loss

                        # Take Profit Prices (based on R:R ratios)
                        take_profit_price = round(entry_price + 1 * risk * risk_reward, 3)  # 1:1 R:R

                        line_order_parameters_nt8 = \
                            f'Buy, {stop_market_price}, {stop_loss_price}, {take_profit_price}'

                        print('Submitting new order: ', line_order_parameters_nt8)
                        save_order_parameters_to_file(line_order_parameters_nt8)
                        # line_order_parameters_to_order_list = f'{n_index},Buy,{t_price},{s_time}'
                        line_order_parameters_to_order_list = f'{current_order_timestamp}'
                        print('line_order_parameters_to_order_list: ', line_order_parameters_to_order_list)
                        save_list_of_orders_to_file(line_order_parameters_to_order_list)

                        # Reset buy_signal flag after processing order to allow the next unique signal
                        buy_signal = False  # Prevent repeated order for the same signal

                    # Reset flags here if a new unique signal occurs in consecutive candles
                    if current_signal != last_signal:
                        buy_signal, sell_signal = True, True  # Reset flags to allow the next unique signal

                else:
                    winsound.PlaySound('Windows Critical Stop.wav', winsound.SND_FILENAME)
                    print('Longs: Old signal. Rejected')
        else:
            print('Longs: No new orders')
    else:
        print('Longs: There is an open long position. No new long signals...'.upper())

    # +------------------------------------------------------------------+
    # SELL ORDERS
    # +------------------------------------------------------------------+
    if get_position_state_shorts() == '' or get_position_state_shorts() == 'closed' or get_position_state_longs() == 'opened_long':
        if not pd.isna(current_order_timestamp):

            if current_signal != last_signal:
                # If time difference between current and last order is positive then it's accepted:
                if time_difference_current_time_order < 1:
                    # Proceed if no last order exists or if the current order timestamp is newer
                    if current_signal == f'-100+{n_index}' and sell_signal:
                        # Play sound to indicate order sent
                        winsound.PlaySound('chord.wav', winsound.SND_FILENAME)
                        print()
                        # print(f'{n_index} ▼ ▼ ▼ Sell order has been sent to NT8! ▼ ▼ ▼ {current_time}'.upper())
                        print(Fore.RED + Style.BRIGHT + f'{n_index} ▼ ▼ ▼ Sell order has been sent to NT8! ▼ ▼ ▼ {current_time}'.upper())

                        # ORDER PARAMETERS
                        entry_price = last_candle_low

                        # Stop Loss Price
                        stop_loss_price = round(last_candle_high + stop_loss_offset, 3)
                        risk = stop_loss_price - entry_price

                        # Take Profit Prices (based on R:R ratios)
                        take_profit_price = round(entry_price - 1 * risk * risk_reward, 3)

                        line_order_parameters_nt8 = \
                            f'Sell, {stop_market_price}, {stop_loss_price}, {take_profit_price}'

                        print('Submitting new order: ', line_order_parameters_nt8)
                        save_order_parameters_to_file(line_order_parameters_nt8)

                        # line_order_parameters_to_order_list = f'{n_index},Sell,{t_price},{s_time}'
                        line_order_parameters_to_order_list = f'{current_order_timestamp}'
                        print('line_order_parameters_to_order_list: ', line_order_parameters_to_order_list)
                        save_list_of_orders_to_file(line_order_parameters_to_order_list)

                        # Reset sell_signal flag after processing order to allow the next unique signal
                        sell_signal = False  # Prevent repeated order for the same signal

                    # Reset flags here if a new unique signal occurs in consecutive candles
                    if current_signal != last_signal:
                        buy_signal, sell_signal = True, True  # Reset flags to allow the next unique signal
                else:
                    winsound.PlaySound('Windows Critical Stop.wav', winsound.SND_FILENAME)
                    print('Shorts: Old signal. Rejected')
        else:
            print('Shorts: No new orders')
    else:
        print('Shorts: There is an open short position. No new short signals...'.upper())
    return buy_signal, sell_signal
