using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.IO.Ports;
using System.Diagnostics;
using System.Management;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using System.Windows.Threading;
using System.Globalization;
using System.Windows.Interop;


namespace SimpleSerialMonitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private StringBuilder recv_buff = new StringBuilder();
        private const int max_lines = 10000;


        public MainWindow()
        {
            InitializeComponent();
            //sel_port();   // 重くなるので廃止

            DispatcherTimer ui_update_timer = new DispatcherTimer(DispatcherPriority.Background);
            ui_update_timer.Tick += new EventHandler(update_ui);
            ui_update_timer.Interval = new TimeSpan(0, 0, 0, 0, 35);
            ui_update_timer.Start();

            parity_bit.ItemsSource = Enum.GetValues(typeof(Parity));
            //stop_bit.ItemsSource = Enum.GetValues(typeof(StopBits));


            // プルダウンメニューのリスト処理
            StopBits[] stopbit = (StopBits[])Enum.GetValues(typeof(StopBits));
            string[] display_stopbit = { "0", "1", "2" , "1.5" };
            var StopBitsList = new ObservableCollection<ComPort>();

            for (int i = 0; i < stopbit.Length; i++)
            {
                //if (display_stopbit[i] != "0" && display_stopbit[i] != "1.5")
                if (display_stopbit[i] != "0")
                {
                    StopBitsList.Add(new ComPort { DisplayStopBits = display_stopbit[i], SelectStopBits = stopbit[i].ToString() });
                }
            }
            stop_bit.ItemsSource = StopBitsList;
            stop_bit.SelectedValuePath = "SelectStopBits";
            stop_bit.DisplayMemberPath = "DisplayStopBits";


            // 送信テキストボックスでPreviewTextInputイベントでスペースが拾えるようにする
            sendText.InputBindings.Add(new KeyBinding(ApplicationCommands.NotACommand, Key.Space, ModifierKeys.None));
            sendText.InputBindings.Add(new KeyBinding(ApplicationCommands.NotACommand, Key.Space, ModifierKeys.Shift));



            // 設定値の読み込み
            baud_rate.SelectedIndex = Properties.Settings.Default.baund_rate_index;
            data_bit.SelectedIndex = Properties.Settings.Default.date_length_index;
            parity_bit.SelectedIndex = Properties.Settings.Default.parity_index;
            stop_bit.SelectedIndex = Properties.Settings.Default.stop_bit_index;
            check_cr.IsChecked = Properties.Settings.Default.cr_ischeck;
            check_lf.IsChecked = Properties.Settings.Default.lf_ischeck;
            hex_mode.IsChecked = Properties.Settings.Default.hexmode_ischeck;
            check_send_log.IsChecked = Properties.Settings.Default.loggingsend_ischeck;
            check_autoscroll.IsChecked = Properties.Settings.Default.autoscroll_ischeck;
            check_timestamp.IsChecked = Properties.Settings.Default.timestamp_ischeck;
            check_lineWrap.IsChecked = Properties.Settings.Default.linewrap_ischeck;


            // 読み込んだ設定値の反映
            if (check_lineWrap.IsChecked == true)
            {
                receiveText.TextWrapping = TextWrapping.Wrap;
            }
            else
            {
                receiveText.TextWrapping = TextWrapping.NoWrap;
            }

        }


        public class ComPort
        {
            public string DeviceID { get; set; }
            public string Description { get; set; }
            public string SelectStopBits { get; set; }
            public string DisplayStopBits { get; set; }

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            HwndSource hwndSource = PresentationSource.FromVisual(this) as HwndSource;
            if (hwndSource != null)
                hwndSource.AddHook(new HwndSourceHook(DeveiceChanged));
        }

        private const int WM_DEVICECHANGE = 0x219;
        private const int DBT_DEVICEARRIVAL = 0x8000;
        private const int DBT_DEVICEREMOVECOMPLETE = 0x8004;

        private string current_opening_port = string.Empty;

        private IntPtr DeveiceChanged(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            string[] PortNames;
            if (msg == WM_DEVICECHANGE)
            {
                switch (wParam.ToInt32())
                {
                    case DBT_DEVICEARRIVAL:
                        PortNames = SerialPort.GetPortNames();
                        if (current_opening_port != string.Empty && PortNames.Contains(current_opening_port))
                        {
                            //MessageBox.Show("シリアルポートが接続されました");
                        }
                        break;
                    case DBT_DEVICEREMOVECOMPLETE:
                        PortNames = SerialPort.GetPortNames();
                        if (current_opening_port != string.Empty && !PortNames.Contains(current_opening_port))
                        {
                            //MessageBox.Show(current_opening_port + " が切断されました");
                            Debug.WriteLine(current_opening_port + " が切断されました");
                            if (serialPort != null)
                            {
                                serialPort.Close();
                                serialPort = null;
                                disconnect.IsEnabled = false;
                                connect.IsEnabled = true;
                                send.IsEnabled = false;
                                cmb.SelectedIndex = -1;
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
            return IntPtr.Zero;
        }


        // UIの更新（受信テキストボックス）
        private void update_ui(object sender, EventArgs e)
        {
            //Debug.WriteLine(FastDateTime.Now.ToString("HH:mm:ss.fff "));

            /*receiveText.Dispatcher.BeginInvoke(
                DispatcherPriority.Background, new Action(() =>
                {
                }
            ));*/

            // 10000行を超えたら古いものから消していく
            /*if (receiveText.LineCount >= max_lines)
            {
                //receiveText.Text = receiveText.Text.TrimStart();
                StringBuilder sb = new StringBuilder();
                sb.Append(receiveText.Text.Substring(receiveText.Text.IndexOf("\r\n") + 2));
                receiveText.Clear();
                receiveText.AppendText(sb.ToString());
            }*/

            if (!String.IsNullOrEmpty(recv_buff.ToString()))
            {
                receiveText.AppendText(recv_buff.ToString());
                recv_buff.Clear();
            }

            
        }

        SerialPort serialPort = null;
        private List<string> _history = new List<string>();
        private int _historyIndex = -1;

        
        // シリアルポート列挙
        private void sel_port()
        {
            // シリアルポートの列挙
            string[] PortList = SortComNumber(SerialPort.GetPortNames());
            string[] PortName = SortComNumber(GetDeviceNames());
            var PortNumList = new ObservableCollection<ComPort>();

            // COM番号とラベルの紐付け
            for (int i = 0; i < PortList.Length;i++)
            {
                bool add_flg = false;
                for (int j = 0; j < PortName.Length; j++)
                {
                    if (PortName[j].Contains(PortList[i]))
                    {
                        PortNumList.Add(new ComPort { DeviceID = PortList[i], Description = PortName[j] });
                        add_flg = true;
                    }
                }
                // ラベルがない場合はそのまま出力
                if (!add_flg)
                {
                    PortNumList.Add(new ComPort { DeviceID = PortList[i], Description = PortList[i] });
                }
            }

            cmb.ItemsSource = PortNumList;
            cmb.SelectedValuePath = "DeviceID";
            cmb.DisplayMemberPath = "Description";
        }


        private string[] GetDeviceNames()
        {
            var deviceNameList = new System.Collections.ArrayList();
            var check = new Regex("(COM[1-9][0-9]?[0-9]?)");

            ManagementClass mcPnPEntity = new ManagementClass("Win32_PnPEntity");
            ManagementObjectCollection manageObjCol = mcPnPEntity.GetInstances();

            //全てのPnPデバイスを探索しシリアル通信が行われるデバイスを随時追加する
            foreach (ManagementObject manageObj in manageObjCol)
            {
                //Nameプロパティを取得
                var namePropertyValue = manageObj.GetPropertyValue("Caption");
                var status = manageObj.GetPropertyValue("Status");
                if (namePropertyValue == null)
                {
                    continue;
                }

                //Nameプロパティ文字列の一部が"(COM1)～(COM999)"と一致するときリストに追加"
                string name = namePropertyValue.ToString();
                if (check.IsMatch(name) && status.ToString() == "OK")
                {
                    deviceNameList.Add(name);
                }
            }

            //戻り値作成
            if (deviceNameList.Count > 0)
            {
                string[] deviceNames = new string[deviceNameList.Count];
                int index = 0;
                foreach (var name in deviceNameList)
                {
                    Match com_num  = Regex.Match(name.ToString(), @"\((.*?)\)");
                    Match com_name = Regex.Match(name.ToString(), @"(.*)(?=\s\()");
                    deviceNames[index++] = com_num.Groups[1].Value.ToString() + " (" + com_name.Groups[1].Value.ToString() + ")";
                }
                return deviceNames;
            }
            else
            {
                return null;
            }
        }


        public string[] SortComNumber(string[] sort_array)
        {
            List<string> vlist;
            if (sort_array != null && sort_array.Length > 0)
            {
                vlist = new List<string>(sort_array);
            }
            else {
                vlist = new List<string>();
            }

            // {Name,Index}という匿名クラスのリストを作り、Indexに従ってソートする。
            var list = vlist.Select(title => new { Name = title, Index = COM_number_toInt(title) })
                .OrderBy(title => title.Index).Select(title => title.Name);

            return list.ToArray();
        }


        private int COM_number_toInt(string txt)
        {
            Match match = Regex.Match(txt, @"(?<=COM)[0-9]+");
            //return int.Parse(Regex.Replace(txt, @"[^0-9]", ""));
            return int.Parse(match.Value);
        }

        /*----------------------------
        * 接続ボタンを押した時の処理
        *---------------------------*/
        private void connect_Click(object sender, RoutedEventArgs e)
        {
            // ポートが選択されている場合
            if (cmb.SelectedValue != null)
            {
                // ポート名を取得
                var port = cmb.SelectedValue.ToString();
                // まだポートに繋がっていない場合
                if (serialPort == null)
                {
                    // serialPortの設定
                    serialPort = new SerialPort();
                    serialPort.PortName = port;
                    serialPort.BaudRate = Convert.ToInt32(baud_rate.Text);
                    serialPort.DataBits = Convert.ToInt32(data_bit.Text);
                    serialPort.Parity = (Parity)Enum.Parse(typeof(Parity), parity_bit.SelectedValue.ToString());
                    serialPort.StopBits = (StopBits)Enum.Parse(typeof(StopBits), stop_bit.SelectedValue.ToString());
                    serialPort.Encoding = Encoding.UTF8;
                    serialPort.WriteTimeout = 100000;
                    serialPort.DtrEnable = true;
                    //serialPort.RtsEnable = true;
                    //serialPort.ReadTimeout = 50;

                    //データ受信時のイベントを設定 関数は後述のserialPort_DataReceived
                    serialPort.DataReceived += new SerialDataReceivedEventHandler(serialPort_DataReceived);

                    // シリアルポートに接続
                    try
                    {
                        // ポートオープン
                        serialPort.Open();
                        connect.IsEnabled = false;
                        disconnect.IsEnabled = true;
                        send.IsEnabled = true;
                        Debug.WriteLine(string.Format("Opened serial port:\t{0}", serialPort.PortName));
                        Debug.WriteLine(string.Format("Baud Rate:\t{0}", serialPort.BaudRate));
                        Debug.WriteLine(string.Format("Data Bits:\t{0}", serialPort.DataBits));
                        Debug.WriteLine(string.Format("Parity:\t{0}", serialPort.Parity));
                        Debug.WriteLine(string.Format("Stop Bits:\t{0}", serialPort.StopBits));
                        
                        // ポート状態間使用に名前を格納
                        current_opening_port = serialPort.PortName;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                        serialPort = null;
                        connect.Content = new MaterialDesignThemes.Wpf.PackIcon
                        {
                            Kind = MaterialDesignThemes.Wpf.PackIconKind.Cancel,
                            Height = 24,
                            Width = 24
                        };

                        DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0.5) };
                        timer.Start();
                        timer.Tick += (s, args) =>
                        {
                            timer.Stop();
                            connect.Content = "CONNECT";
                        };
                        
                    }
                }
            }
        }

        private int length_idx = 0;
        // 送信処理
        private void serialPort_SendDataProcess()
        {
            // 繋がっていない場合は処理しない。
            if (serialPort == null) return;
            if (serialPort.IsOpen == false) return;


            // テキストボックスから、送信するテキストを取り出す。
            String data = sendText.Text;
            data = data.TrimEnd();
            string[] str_bytes = data.Split(" ");
            byte[] data_bytes = new byte[str_bytes.Length];

            try
            {
                // シリアルポートからテキストを送信する。
                if (hex_mode.IsChecked == false)
                {
                    if (check_cr.IsChecked == true)
                    {
                        data += "\r";
                    }
                    if (check_lf.IsChecked == true)
                    {
                        data += "\n";
                    }
                    serialPort.Write(data);
                }
                else
                {
                    
                    for (int i = 0;i < str_bytes.Length;i++)
                    {   
                        try
                        {
                            data_bytes[i] = byte.Parse(str_bytes[i], NumberStyles.AllowHexSpecifier);
                        }
                        catch
                        {
                            sendText.Text = string.Empty;

                            send.Content = new MaterialDesignThemes.Wpf.PackIcon
                            {
                                Kind = MaterialDesignThemes.Wpf.PackIconKind.Cancel,
                                Height = 24,
                                Width = 24
                            };

                            DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0.5) };
                            timer.Start();
                            timer.Tick += (s, args) =>
                            {
                                timer.Stop();
                                send.Content = "SEND";
                            };

                            return;
                        }
                    }
                    serialPort.Write(data_bytes, 0, data_bytes.Length);


                }
                

                // 送信ログの表示
                receiveText.Dispatcher.Invoke(
                    new Action(() =>
                    {


                        if (check_send_log.IsChecked == true)
                        {
                            if (check_timestamp.IsChecked == true && hex_mode.IsChecked == false)
                            {
                                // 最後の文字を取得
                                if (receiveText.Text.Equals("") || receiveText.Text.EndsWith("\r") || receiveText.Text.EndsWith("\n"))
                                {
                                    receiveText.AppendText(FastDateTime.Now.ToString("HH:mm:ss.fff "));
                                }
                            }

                            if (hex_mode.IsChecked == true) {
                            
                                if (!receiveText.Text.Equals("") && !receiveText.Text.EndsWith("\r") && !receiveText.Text.EndsWith("\n"))
                                {
                                    receiveText.AppendText("\n");
                                    length_idx = 0;
                                }
                                receiveText.AppendText("[SEND] -> ");
                                foreach (byte b in data_bytes)
                                {

                                    receiveText.AppendText(b.ToString("X2"));
                                    receiveText.AppendText(" ");
                                }
                                receiveText.AppendText("\n");
                            }
                            else
                            {
                                receiveText.AppendText(String.Format("[SEND] -> {0}\n", data.TrimEnd()));
                            }
                            

                            if (check_autoscroll.IsChecked == true)
                            {
                                receiveText.ScrollToEnd();
                            }
                        }

                    })
                );

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        // 行頭のみ（空行は除外）
        private Regex reg = new Regex(@"^(?!\s*$)", RegexOptions.Multiline);

        //データ受信イベントで呼び出される関数
        private void serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            // 繋がっていない場合は処理しない。
            if (serialPort == null) return;
            if (serialPort.IsOpen == false) return;

            receiveText.Dispatcher.BeginInvoke(
                DispatcherPriority.Background, new Action(() =>
                {


                    // 通常モードであればそのままテキストを入れる
                    if (hex_mode.IsChecked == false)
                    {

                        // ポートから取得した文字列を追加
                        if (serialPort != null)
                        {
                            // １文字ずつ取得する（行で入れる 改行がないと無理）
                            // タイムスタンプの挿入
                            /*if (check_timestamp.IsChecked == true && hex_mode.IsChecked == false)
                            {
                                // 最後の文字を取得
                                string end_str = "";
                                if (receiveText.Text != string.Empty)
                                {
                                    end_str = receiveText.Text.Substring(receiveText.Text.Length - 1);
                                }
                                // 行頭にタイムスタンプを入れる
                                //if (receiveText.Text == String.Empty || end_str.Equals("\n") || end_str.Equals("\r"))
                                if ((receiveText.Text == String.Empty && recv_buff.Length == 0) || end_str.Equals("\n") || end_str.Equals("\r"))
                                {
                                    //receiveText.AppendText(FastDateTime.Now.ToString("HH:mm:ss.fff "));
                                    recv_buff.Append(FastDateTime.Now.ToString("HH:mm:ss.fff "));

                                    // コールバックモードの場合に受信ヘッダを入れる
                                    if (check_send_log.IsChecked == true)
                                    {
                                        //receiveText.AppendText("[RECV] ");
                                        recv_buff.Append("[RECV] ");
                                    }
                                    //receiveText.AppendText("-> ");
                                    recv_buff.Append("-> ");
                                }

                            }*/

                            // 受信した文字列を入れる
                            //receiveText.AppendText(serialPort.ReadLine());

                            // １文字ずつ取得する（重い）
                            /*int read_byte = 0x00;
                            try
                            {  
                                read_byte = serialPort.ReadByte();
                            }
                            catch
                            {
                                //recv_buff.Append(serialPort.ReadExisting());
                                serialPort.DiscardInBuffer();
                            }

                            // 初回の挿入
                            if (!is_fisrt_stamp)
                            {
                                // タイムスタンプの挿入
                                if (check_timestamp.IsChecked == true && hex_mode.IsChecked == false)
                                {
                                    recv_buff.Append(FastDateTime.Now.ToString("HH:mm:ss.fff -> "));
                                }

                                // コールバックモードの場合に受信ヘッダを入れる
                                if (check_send_log.IsChecked == true)
                                {
                                    recv_buff.Append("[RECV] -> ");
                                }
                                is_fisrt_stamp = true;
                            }

                            recv_buff.Append((char)read_byte);

                            // 受信したデータが改行コードであれば
                            if (read_byte == '\n')
                            {
                                // タイムスタンプの挿入
                                if (check_timestamp.IsChecked == true && hex_mode.IsChecked == false)
                                {
                                    recv_buff.Append(FastDateTime.Now.ToString("HH:mm:ss.fff -> "));
                                }

                                // コールバックモードの場合に受信ヘッダを入れる
                                if (check_send_log.IsChecked == true)
                                {
                                    recv_buff.Append("[RECV] -> ");
                                }
                            }*/



                            //StringBuilder read_str = new StringBuilder();
                            string read_str = "";
                            try
                            {
                                //recv_buff.Append(serialPort.ReadExisting());
                                read_str = serialPort.ReadExisting();
                            }
                            catch
                            {
                                //recv_buff.Append(serialPort.ReadExisting());
                                serialPort.DiscardInBuffer();
                            }

                            // 初回の挿入
                            /*if (!is_fisrt_stamp)
                            {
                                // タイムスタンプの挿入
                                if (check_timestamp.IsChecked == true && hex_mode.IsChecked == false)
                                {
                                    recv_buff.Append(FastDateTime.Now.ToString("HH:mm:ss.fff -> "));
                                }

                                // コールバックモードの場合に受信ヘッダを入れる
                                if (check_send_log.IsChecked == true)
                                {
                                    recv_buff.Append("[RECV] -> ");
                                }
                                is_fisrt_stamp = true;
                            }*/

                            //recv_buff.Append(read_str);

                            // コールバックモードの場合に受信ヘッダを入れる

                            // タイムスタンプの挿入
                            if (check_timestamp.IsChecked == true && hex_mode.IsChecked == false)
                            {
                                if (check_send_log.IsChecked == true)
                                {
                                    //recv_buff.Append(read_str.Replace("\n", FastDateTime.Now.ToString("\nHH:mm:ss.fff [RECV] -> ")));
                                    read_str = reg.Replace(read_str, FastDateTime.Now.ToString("HH:mm:ss.fff [RECV] -> "));
                                }
                                else
                                {
                                    //recv_buff.Append(read_str.Replace("\n", FastDateTime.Now.ToString("\nHH:mm:ss.fff -> ")));
                                    read_str = reg.Replace(read_str, FastDateTime.Now.ToString("HH:mm:ss.fff -> "));
                                }
                                recv_buff.Append(read_str);
                            }
                            else
                            {
                                if (check_send_log.IsChecked == true)
                                {
                                    //recv_buff.Append(read_str.Replace("\n", "\n[RECV] -> "));
                                    read_str = reg.Replace(read_str, FastDateTime.Now.ToString("[RECV] -> "));
                                }
                                /*
                                {
                                    recv_buff.Append(read_str);
                                }*/
                                recv_buff.Append(read_str);
                            }


                        }

                    }

                    // HEXモードであれば挿入する
                    else
                    {
                        int bytes_length = serialPort.BytesToRead;
                        byte[] buffer = new byte[bytes_length];
                        string display_txt = string.Empty;
                        StringBuilder sb = new StringBuilder();

                        try
                        {
                            serialPort.Read(buffer, 0, bytes_length);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                            serialPort.DiscardInBuffer();
                        }

                        foreach (byte b in buffer)
                        {
                            sb.Append(b.ToString("X2"));
                            sb.Append(" ");
                            length_idx++;
                            if (length_idx == 16)
                            {
                                sb.Append("\n");
                                length_idx = 0;
                            }
                        }
                        //receiveText.AppendText(sb.ToString());
                        recv_buff.Append(sb.ToString());
                    }

                        

                }
            ));


            
        }


        /*----------------------------
         * 切断ボタンを押した時
         *---------------------------*/
        private void disconnect_Click(object sender, RoutedEventArgs e)
        {
            if (serialPort != null && serialPort.IsOpen)
            {
                serialPort.Close();
                serialPort = null;
                disconnect.IsEnabled = false;
                connect.IsEnabled = true;
                send.IsEnabled = false;
            }

        }

        // 送信テキスト履歴保存処理
        private void text_save()
        {
            _history.Add(sendText.Text);
            if (_historyIndex < 0 || _historyIndex == _history.Count - 2)
            {
                _historyIndex = _history.Count - 1;
            }
            sendText.Text = String.Empty;

            _historyIndex++;
            if (_historyIndex == 100)
            {
                _historyIndex = 1;
            }
        }

        /*----------------------------
         * 送信ボタンを押した時
         *---------------------------*/
        private void send_Click(object sender, RoutedEventArgs e)
        {
            serialPort_SendDataProcess();

            text_save();
            sendText.Focus();
        }


        // シリアルポートを選択
        private void cmb_DropDownOpened(object sender, EventArgs e)
        {
            sel_port();
        }

        

        // ウィンドウを閉じたとき
        private void Window_Closed(object sender, EventArgs e)
        {
            Properties.Settings.Default.baund_rate_index = baud_rate.SelectedIndex;
            Properties.Settings.Default.date_length_index = data_bit.SelectedIndex;
            Properties.Settings.Default.parity_index = parity_bit.SelectedIndex;
            Properties.Settings.Default.stop_bit_index = stop_bit.SelectedIndex;
            Properties.Settings.Default.cr_ischeck = (bool)check_cr.IsChecked;
            Properties.Settings.Default.lf_ischeck = (bool)check_lf.IsChecked;
            Properties.Settings.Default.hexmode_ischeck = (bool)hex_mode.IsChecked;
            Properties.Settings.Default.loggingsend_ischeck = (bool)check_send_log.IsChecked;
            Properties.Settings.Default.autoscroll_ischeck = (bool)check_autoscroll.IsChecked;
            Properties.Settings.Default.timestamp_ischeck = (bool)check_timestamp.IsChecked;
            Properties.Settings.Default.linewrap_ischeck = (bool)check_lineWrap.IsChecked;
            Properties.Settings.Default.Save();

            if (serialPort != null && serialPort.IsOpen)
            {
                serialPort.Close();
                serialPort = null;

            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            

        }

        // ウィンドウが非アクティブ化したとき
        private void Window_Deactivated(object sender, EventArgs e)
        {
            if (auto_disconn.IsChecked == true)
            {
                if (serialPort != null && serialPort.IsOpen)
                {
                    serialPort.Close();
                    serialPort = null;
                    disconnect.IsEnabled = false;
                    connect.IsEnabled = true;
                    send.IsEnabled = false;
                }
            }
        }

        private bool is_pressd_backspace = false;


        // エンターキーを押しても送信する、上下キーで履歴の表示、バックスペースキーの入力フラグ
        private void sendText_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (serialPort != null && serialPort.IsOpen)
                {
                    serialPort_SendDataProcess();
                    text_save();
                }

                return;
            }

            TextBox textBox = (TextBox)sender;
            if (e.Key == Key.Up)
            {
                if (_historyIndex > 0)
                {
                    _historyIndex--;
                    textBox.Text = _history[_historyIndex];
                }

                return;
            }

            if (e.Key == Key.Down)
            {
                if (_historyIndex < _history.Count - 1)
                {
                    _historyIndex++;
                    textBox.Text = _history[_historyIndex];
                }

                return;
            }

            if(e.Key == Key.Back || e.Key == Key.Delete)
            {
                is_pressd_backspace = true;
            }
        }


        // HEXモードであれば0-9,A-Fしか受け付けない
        private void sendText_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {   
            if (hex_mode.IsChecked == true) {
                int hexNumber;
                e.Handled = !int.TryParse(e.Text, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out hexNumber);
            }
            else {
                return;
            }

        }

        


        // HEXモードのときは2文字ずつ空白を上挿入する
        private void sendText_TextChanged(object sender, EventArgs e)
        {
            if (hex_mode.IsChecked == true)
            {   
                if (!is_pressd_backspace) {
                    // 最後が空白ではないか
                    if (!sendText.Text.EndsWith(" "))
                    {
                        string str_space_trim = sendText.Text.Replace(" ", "");
                        if (str_space_trim.Length % 2 == 0 && str_space_trim.Length != 0)
                        {
                            sendText.Text += " ";
                            sendText.Select(sendText.Text.Length, 0);
                        }
                    }
                }
                else
                {
                    sendText.Text = sendText.Text.TrimEnd();
                    sendText.Select(sendText.Text.Length, 0);
                    is_pressd_backspace = false;
                }    
            }
            else {
                return;
            }

        }
        


        // クリアボタンの挙動
        private void log_clear_Click(object sender, RoutedEventArgs e)
        {
            receiveText.Dispatcher.Invoke(
                new Action(() =>
                {
                    //receiveText.Text = String.Empty;
                    receiveText.Clear();
                })
            );
        }


        // 受信ログのテキスト折返し機能
        private void check_lineWrap_Checked(object sender, RoutedEventArgs e)
        {
            receiveText.TextWrapping = TextWrapping.Wrap;
        }

        private void check_lineWrap_UnChecked(object sender, RoutedEventArgs e)
        {
            receiveText.TextWrapping = TextWrapping.NoWrap;
        }



        // クリップボードにコピー
        private void log_clipboard_Click(object sender, RoutedEventArgs e)
        {
            if (receiveText.Text != "")
            {
                Clipboard.SetData(DataFormats.Text, receiveText.Text);
                log_clipboard.Content = new MaterialDesignThemes.Wpf.PackIcon {
                    Kind = MaterialDesignThemes.Wpf.PackIconKind.Check,
                    Height = 24,
                    Width =  24
                };

                
            }
            else
            {
                log_clipboard.Content = new MaterialDesignThemes.Wpf.PackIcon
                {
                    Kind = MaterialDesignThemes.Wpf.PackIconKind.Cancel,
                    Height = 24,
                    Width = 24
                };
            }

            DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0.5) };
            timer.Start();
            timer.Tick += (s, args) =>
            {
                timer.Stop();
                log_clipboard.Content = "COPY TO CLIPBOARD";
            };

        }


        // ファイルにログを保存
        private void save_log_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            string[] filters = new string[]
            {
                "Log files(*.log)|*.log",
                "All files(*.*)|*.*"
            };
            dlg.AddExtension = true; // ユーザーが拡張子を省略したときに、自動的に拡張子を付けるか。規定値はtrue。
            dlg.CheckFileExists = false; // ユーザーが存在しないファイルを指定したときに、警告するか。規定値はfalse。
            dlg.CheckPathExists = true; // ユーザーが存在しないパスを指定したときに、警告するか。規定値はtrue。
            dlg.CreatePrompt = false; // ユーザーが存在しないファイルを指定したときに、作成の許可を求めるか。規定値はfalse。
            dlg.CustomPlaces = null; // ダイアログ左側のショートカットのリスト。
            dlg.DefaultExt = string.Empty; // ダイアログに表示するファイルの拡張子。規定値はEmpty。
            dlg.DereferenceLinks = false; // ショートカットが参照先を返す場合はtrue。リンクファイルを返す場合はfalse。規定値はfalse。
            dlg.FileName = "serial_" + DateTime.Now.ToString("yyyy_MM_dd_HH_ss") + ".log"; // 選択されたファイルのフルパス。
            dlg.Filter = String.Join("|", filters); // ダイアログで表示するファイルの種類のフィルタを指定する文字列。
            dlg.FilterIndex = 1; // 選択されたFilterのインデックス。規定値は1。
            dlg.InitialDirectory = string.Empty; // ダイアログの初期ディレクトリ。規定値はEmpty。
            dlg.OverwritePrompt = true; // 存在するファイルを指定したときに、警告するか。規定値はtrue。
            dlg.Title = "Save Log"; // ダイアログのタイトル。
            dlg.ValidateNames = true; // ファイル名がWin32に適合するか検査するかどうか。規定値はfalse。

            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                string filename = dlg.FileName;

                Encoding enc = Encoding.GetEncoding("UTF-8");
                try
                {
                    File.WriteAllText(filename, receiveText.Text, enc);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    return;
                }
            }
        }


        // HEXモードのチェックボタンでの挙動
        private void hex_mode_Checked(object sender, RoutedEventArgs e)
        {
            check_lf.IsEnabled = false;
            check_cr.IsEnabled = false;
            check_timestamp.IsEnabled = false;
            sendText.Text = string.Empty;
            sendText.CharacterCasing = CharacterCasing.Upper;
            InputMethod.SetIsInputMethodEnabled(sendText, false);
            receiveText.Dispatcher.Invoke(
                new Action(() =>
                {
                    receiveText.Text = String.Empty;
                })
            );
        }

        private void hex_mode_Unchecked(object sender, RoutedEventArgs e)
        {
            check_lf.IsEnabled = true;
            check_cr.IsEnabled = true;
            check_timestamp.IsEnabled = true;
            sendText.CharacterCasing = CharacterCasing.Normal;
            InputMethod.SetIsInputMethodEnabled(sendText, true);
            receiveText.Dispatcher.Invoke(
                new Action(() =>
                {
                    receiveText.Text = String.Empty;
                })
            );
        }


       
        private void receiveText_TextChanged(object sender, TextChangedEventArgs e)
        {
            // 自動スクロール
            if (check_autoscroll.IsChecked == true)
            {
                receiveText.ScrollToEnd();
            }

           

        }


        // マウスホイール操作の挙動
        private void cmb_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta <= 0)
            {
                if (cmb.SelectedIndex < cmb.Items.Count)
                {
                    cmb.SelectedIndex++;
                }
            }
            else
            {
                if (cmb.SelectedIndex > 0)
                {
                    cmb.SelectedIndex--;
                }
            }
        }

        private void baud_rate_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta <= 0)
            {
                if (baud_rate.SelectedIndex < baud_rate.Items.Count)
                {
                    baud_rate.SelectedIndex++;
                }
            }
            else
            {
                if (baud_rate.SelectedIndex > 0)
                {
                    baud_rate.SelectedIndex--;
                }
            }
        }

        
    }

    public static class FastDateTime
    {
        public static TimeSpan LocalUtcOffset { get; private set; }

        public static DateTime Now
        {
            get { return DateTime.UtcNow + LocalUtcOffset; }
        }

        static FastDateTime()
        {
            LocalUtcOffset = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now);
        }
    }

}
