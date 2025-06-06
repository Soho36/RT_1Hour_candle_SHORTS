import pandas as pd
from datetime import datetime
import time
import csv

# log_file_reading_interval = 1       # File reading interval (sec)

# +------------------------------------------------------------------+
# FILE TRANSMIT PATHS
# +------------------------------------------------------------------+

mt5_account_number = 828    # LAST 3 DIGITS OF MT5 ACCOUNT. MUST BE CHANGED BEFORE BUILDING EXE

# MT5 directory with OHLC log file (logging on active timeframe):

# OHLC saved by NT8
nt8_logging_file_path = f'OHLCVData_1.csv'

# PATH TO FILES
nt8_buy_sell_signals_for_path = f'trade_signal.txt'
list_of_orders_path = 'list_of_orders.csv'
position_state_longs_path = 'position_state_shorts.csv'
position_state_shorts_path = 'position_state_shorts.txt'
current_order_direction_path = 'current_order_direction.txt'
current_time = datetime.now().strftime('%Y-%m-%d %H:%M:%S')
active_position_file_path = f'active_position.csv'
tp_orders_file_path = 'tp_orders.csv'
sl_initial_order_file_path = 'sl_order_initial.csv'
sl_in_position_orders_file_path = 'sl_orders_inposition.csv'
entry_price_file_path = 'entry_price.csv'


def leave_only_last_line():     # Clear file before starting the script
    with open(nt8_logging_file_path, 'r', encoding='utf-8') as file:
        lines = file.readlines()
        # Check if there's at least one line to keep
        if lines:
            with open(nt8_logging_file_path, 'w', encoding='utf-8') as file:
                file.write(lines[-1])  # Write only last several lines back to file
        else:
            print('Reading OHLC. File is empty...')

# +------------------------------------------------------------------+
# |                      CLEAR FILES BEFORE START                    |
# +------------------------------------------------------------------+


def clear_all_files_before_start(state):  # Called from orders_sender.py
    # with open(position_state_shorts_path, 'w', encoding='utf-8') as file:
    #     file.write(state)
    #     print(f"Position state shorts is set to {state} before starting script")
    #
    # with open(position_state_longs_path, 'w', encoding='utf-8') as file:
    #     file.write(state)
    #     print(f"Position state longs is set to {state} before starting script")

    with open(active_position_file_path, 'w', encoding='utf-8') as file:
        file.write('closed')
        print(f"Clear active_position.csv before starting script")

    with open(tp_orders_file_path, 'w', encoding='utf-8') as file:
        file.write('')
        print(f"Clear tp_orders.csv before starting script")

    with open(sl_in_position_orders_file_path, 'w', encoding='utf-8') as file:
        file.write('')
        print(f"Clear sl_orders_inposition.csv before starting script")

    with open(sl_initial_order_file_path, 'w', encoding='utf-8') as file:
        file.write('')
        print(f"Clear sl_orders_initial.csv before starting script")

    with open(entry_price_file_path, 'w', encoding='utf-8') as file:
        file.write('')
        print(f"Clear entry_price.csv before starting script")

    with open(nt8_buy_sell_signals_for_path, 'w', encoding='utf-8') as file:
        file.write('')
        print(f"Clear trade_signal.txt before starting script")


def get_dataframe_from_file(max_time_waiting_for_entry):

    log_df = pd.read_csv(
        nt8_logging_file_path,
        sep=';',
        encoding='utf-8',
        engine='python'
    )
    new_column_names = ['Ticker', 'Timeframe', 'Date', 'Time', 'Open', 'High', 'Low', 'Close', 'Volume']
    log_df.columns = new_column_names
    log_df['Datetime'] = pd.to_datetime(log_df['Date'] + ' ' + log_df['Time'], format='ISO8601')
    log_df.set_index('Datetime', inplace=True)
    dataframe_from_log = log_df.loc[:, ['Ticker', 'Date', 'Time', 'Open', 'High', 'Low', 'Close']]
    datetime_index = log_df.index
    last_date = str(datetime_index[-1])     # Get datetime of the first row of dataframe to pass along with levels

    return dataframe_from_log, last_date


# Tracks if there is an open position
# def get_position_state_shorts():
#     with open(position_state_shorts_path, 'r', encoding='utf-8') as file:
#         state = file.read()
#         return state


# def get_position_state_longs():
#     with open(position_state_longs_path, 'r', encoding='utf-8') as file:
#         state = file.read()
#         return state


def get_current_pending_order_direction():
    with open(current_order_direction_path, 'r', encoding='utf-8') as file:
        current_pending_order = file.read().strip()
        return current_pending_order


def save_order_parameters_to_file(line_order_parameters):   # Called from orders_sender.py
    retries = 5
    for attempt in range(retries):
        try:
            with open(nt8_buy_sell_signals_for_path, 'w', encoding='utf-8') as file:
                file.writelines(line_order_parameters)
                print(f"NEW ORDER IS SUCCESSFULLY SAVED TO FILE")
            break
        except PermissionError:
            print(f"Attempt {attempt + 1} failed. Retrying...")
            time.sleep(1)
    else:
        print("Failed to write to the file after multiple attempts.")


# Create orders list file to track orders
def save_list_of_orders_to_file(line_order_parameters_to_order_list):
    with open(list_of_orders_path, 'w', encoding='utf-8') as file:
        file.writelines(line_order_parameters_to_order_list)


def get_last_order_time_from_file():
    with open(list_of_orders_path, 'r', encoding='utf-8') as file:
        last_order_timestamp = pd.to_datetime(file.read())
        if pd.isna(last_order_timestamp):
            last_order_timestamp = pd.to_datetime('2024-01-01 00:00:00')  # Default value while the file is empty

        return last_order_timestamp


def active_position():  # Read entry price from the active position file
    with open(active_position_file_path, 'r') as file:
        reader = csv.reader(file)
        rows = list(reader)
        if rows:
            return rows[-1][0]  # last entry
    return False


def get_entry_price():  # Read entry price from entry_price.csv
    with open(entry_price_file_path, 'r') as file:
        reader = csv.reader(file)
        rows = list(reader)
        if rows:
            return float(rows[-1][0])  # last entry
    return None


def get_initial_sl():  # Read initial stop loss from sl_order_initial.csv
    with open(sl_initial_order_file_path, 'r') as file:
        reader = csv.reader(file)
        rows = list(reader)
        if rows:
            return float(rows[-1][0])  # last entry
    return None


def write_in_position_sl(sl):    # Write stop loss to file once position is opened
    with open(sl_in_position_orders_file_path, 'w', newline='') as file:
        writer = csv.writer(file)
        writer.writerow([sl])
    print(f"[INFO] SL(position): {sl} written to {sl_in_position_orders_file_path}")


def write_initial_sl(sl):    # Write initial stop loss to file before position is opened
    with open(sl_initial_order_file_path, 'w', newline='') as file:
        writer = csv.writer(file)
        writer.writerow([sl])
    print(f"[INFO] SL(initial): {sl} written to {sl_in_position_orders_file_path}")
