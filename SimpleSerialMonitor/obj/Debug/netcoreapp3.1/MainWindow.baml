   M S B A M L   `   `   `  ����N  JSimpleSerialMonitor, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null9!clr-namespace:SimpleSerialMonitorSimpleSerialMonitor  5   	   $    SimpleSerialMonitor.MainWindow   5   	   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   b ^MaterialDesignThemes.Wpf, Version=3.2.0.1979, Culture=neutral, PublicKeyToken=df2a72020bd7962aFmaterialDesign1http://materialdesigninxaml.net/winfx/xaml/themes  5   	   -local!clr-namespace:SimpleSerialMonitor  5   	   -   5          � Title$  Simple Serial Terminal��5	   	   $	��750��6(   $	��700��65   $	��700��6X   $	��400��6g   .��5
      � .+�5   
   �� -   5        $	��cmb��5      )  #MaterialDesignThemes.Wpf.HintAssist   Hint$ Select COM Port��6   $
��Left=�6O   $��
111,26,0,0q�6j   $	��Top=�6~   $	��277��6�   6�   �� 5   
   -   5        $��connect��5      $��CONNECT��6    $
��Left=�62   $	��Top=�6M   $��
436,22,0,0q�6e   $��12(�6�   $��87��6�   �� 5   
   -   5        $��
disconnect��5      $��
DISCONNECT��6#   $
��Left=�68   $��
556,22,0,0q�6S    $   MaterialDesignRaisedAccentButton#��[  6g   $	��Top=�6�   ${�False��6�   $��	10,4,10,4q�6�   $��12(�6�    � Cursor$
 Hand��6  �� 5   
   -   5        $��sendText��5      $ 	Send Text��6"    % !MaterialDesignFloatingHintTextBox#��[ 6M   $
��Left=�6�   $��
33,121,0,0q�6�    �� TextWrapping$
 Wrap=�6�   $�� ��6�   $	��Top=�6�   $	��511��6�   $
��Left=�6   $��Consolas)�6B  6�  �� 5   
   -   5        $
��send��5      $
��SEND��6   $
��Left=�6,   $��576,135,0,0q�6G   $	��Top=�6\   $��74��6t   ${�False��6�    J� RenderTransformOrigin$ 0.801,0.432&�6�   $��12(�6�   �� 5   
   $��COM Port��6   $
��Left=�6#   $��	33,28,0,0q�6>   $	��Top=�6Q   �� 5   
   -   5         ��Name$ 	baud_rate��5      $
��Left=�6&   $��
111,76,0,0q�6A   $	��Top=�6U   $��95��6m   $��2,6,0,6q�6x   $��Center=�6�    �� 
IsEditable$
 True��6�   .@�5      �� .��6   48006!   �� 5      .��6   96006"   �� 5      .��6   192006"   �� 5      .��6   384006"   �� 5      .��6   576006"   �� 5      .��6   748006"   �� 5      .��6   1152006#   �� 5      .��6   2304006#   �� 5      .��6   2500006#   5      �� 5   
   $��	Baud Rate��6   $
��Left=�6$   $��	33,79,0,0q�6?   $	��Top=�6R   $
��Auto��6j   �� 5   
   $��Parity��6   $
��Left=�6!   $��
416,79,0,0q�6<   $	��Top=�6P   $
��Auto��6h   �� 5   
   -   5        $ 
parity_bit��5      $��
462,77,0,0q�6'   $	��Top=�6;   $��Center=�6S   $��Center=�6w   $��2,6,0,6q�6�   $
��Left=�6�   $��62��6�   $
��Auto��6�   �� 5    
   -	   5        $ stop_bit��5       $��
614,77,0,0q�6%   $	��Top=�69   $��Center=�6Q   $��Center=�6u   $��2,6,0,6q�6�   $
��Left=�6�   $��36��6�   $
��Auto��6�   �� 5!   
   $��	Stop Bits��6   $��
550,78,0,0q�6$   $	��Top=�68   $
��Left=�6P   �� 5"   
   $��	Data Bits��6   $��
293,79,0,0q�6$   $	��Top=�68   $
��Left=�6P   �� 5#   
   -
   5        $ data_bit��5#      $
��Left=�6%   $��
357,77,0,0q�6@   $	��Top=�6T   $��34��6l   $��Center=�6w   $	��Top=�6�   $��2,6,0,6q�6�   .@�5$      �� $��8��6   $��Center=�6'   �� 5%      $��7��6   $��Center=�6'   5&      �� 5'   
   -   5         ��Name$ check_cr��5'      $��CR��6%   $
��Left=�62   $��453,180,0,0q�6M   $	��Top=�6b   $��4,-2,0,0q�6z   �� 5(   
   -   5        $ check_lf��5(      $��LF��6%   $
��Left=�62   $��510,180,0,0q�6M   $	��Top=�6b   $��4,-2,0,0q�6z   �� 5)   
   -   5        $ hex_mode��5)      $��HEX Mode��6%   $
��Left=�68   $��127,180,0,0q�6S   $	��Top=�6h   $��4,-2,0,0q�6�   6�   �� 5*   
   $��20,235,20,65q�6   ���	#FF777777$��2��6=   ���	#FFFAFAFA6Q   �� 5+   
   -   5         ��Name$ receiveText��5+      $��20,235,20,65q�6'   $�� ��6=   .  (MaterialDesignThemes.Wpf.TextFieldAssist	   DecorationVisibility$	 Hidden=�6E   $ NoWrap=�6�   $��0q�6�   $��Stretch=�6�   $��Stretch=�6�   $��Consolas)�6�   
 �� 
IsReadOnly$

 True��6
  $��4,2,2,4q�6  " �� VerticalScrollBarVisibility$
 Auto=�6.  $ �� HorizontalScrollBarVisibility$
 Auto=�6Q  6v  �� 5,   
   -   5        $ check_send_log��5,      $��	Echo Back��6+   $
��Left=�6?   $��
31,180,0,0q�6Z   $	��Top=�6n   $��4,-2,0,0q�6�   �� 5-   
   -   5         ��Name$ 	log_clear��5-      $��CLEAR��6$   #��[  64   $��	0,0,21,17q�6n   $��12(�6�   $��	15,4,16,4q�6�   $
 Hand��6�   $��32��6�   $��Bottom=�6�   $��Right=�6�   $��80��6�   6�   �� 5.   
   -   5        $ check_timestamp��5.      $��Show timestamp��6,   $
��Left=�6E   $��
210,0,0,24q�6`   $��4,-2,0,0q�6t   $��18��6�   $��Bottom=�6�   �� 5/   
   -   5        $ check_autoscroll��5/      $��
Autoscroll��6-   $
��Left=�6B   $��	20,0,0,24q�6]   $��4,-2,0,0q�6p   $��18��6�   $��Bottom=�6�   �� 50   
   -   5        $ check_lineWrap��50      $��	Line Wrap��6+   $
��Left=�6?   $��
114,0,0,24q�6Z   $��4,-2,0,0q�6n   $��18��6�   $��Bottom=�6�   6�   �� 51   
   -   5        $ log_clipboard��51      $��COPY TO CLIPBOARD��6(   $��
0,0,113,17q�6D   $��12(�6X   $��5,4,5,4q�6f   $
 Hand��6x   $��32��6�   $��Bottom=�6�   $��Right=�6�   $	��131��6�   6�   �� 53   
   -   5        $ save_log��53      $��SAVE LOG��6#   $��
0,0,257,17q�66   $��12(�6J   $��5,4,5,4q�6X   $
 Hand��6j   $��32��6x   $��Bottom=�6�   $��Right=�6�   $��86��6�   6�   56      57      