   M S B A M L   `   `   `  ����D  @Server_v2, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null%clr-namespace:Server_v2	Server_v2  5   	       Server_v2.MainWindow   5   	   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   )localclr-namespace:Server_v2     5   	   -   5          �Name$  PoliBox_Server��5   	    � Title$ 
MainWindow��5	   	   $	��622��6   $��903.465��6)   ���	#FF2234A9
������� � Icon$ Resources/icon.jpg��6d   ���	#FFF8F8F8 � WindowStartupLocation$ Manual=�6�    � 
ResizeMode$ CanMinimize=�6�   .��5
      � ���	#FFFC7A08$��10,95,10.333,10.333q�6"    � ColumnDefinitions 5   
   �� 5      $��0* �6   �� 5      .+�5   
   �� -   5         ��Name$ UtentiConnessi��5      $	��438��6*   $��672,40,10.111,10q�67   $	��Top=�6Q   
�������
����������	#FF2234A9$��1��6�   �� 5   
   -   5         ��Name$ Utenti_Connessi��5      $��0,15,10.111,0q�6-   $
��Wrap=�6D   $��Online��6X   $	��Top=�6f   $��20��6~    �� TextAlignment$ Center=�6�   
�������$��1��6�   $��Right=�6�   $	��197��6�   �� 5   
   -   5        	 ��Name$	 
txtConsole��5      $��10,40,212.111,124.444q�6&   
 �� TextWrapping$

 Wrap=�6E   
�������
����������	#FF2234A9 �� SelectionBrush �	#FFFFC342$ �� HorizontalScrollBarVisibility$ Visible=�6�   " �� VerticalScrollBarVisibility$ Visible=�6�   ��. $��1��6G  �� 5   
   -   5        $ 	textBlock��5      $
��Left=�6'   $��1,15,0,0q�6B   $
��Wrap=�6T   $��Console��6h   $	��Top=�6w   $��20��6�   $	��666��6�   $ Center=�6�   ���	#FFFEFEFE$��1��6�   �� 5   
   -   5         ��Name$ 
PrintUsers��5      $��Print users��6%   $
��Left=�6;   $��	10,0,0,80q�6V   $	��167��6i   $��1��6�   $��26��6�   $��Bottom=�6�   
�������
����������	#FFFC7A086�   �� 5   
   -   5         ��Name$ image��5      $
��Left=�6   $��93��6:   $��1,-99,0,492.222q�6F   $��Bottom=�6_   $	��213��6z   $��Resources/frontimage.png��6�   $��2��6�    J� OpacityMask 5      �� 5       �� ImageSource$ Resources/frontimage.png��6    j� Stretch$ UniformToFill=�6D   5      �� 5   
   -   5         ��Name$ label��5      $��PoliBox - Server��6   $
��Left=�6:   $��720,-74,0,528.222q�6U   $��Bottom=�6p   
�������$��18.667(�6�   $��32��6�   $��1��6�   $	��147��6�   �� 5   
   -	   5        $ PrintFolders��5      $��Print Folders��6'   $
��Left=�6?   $��	10,0,0,49q�6Z   $	��167��6m   $��1��6�   $��26��6�   $��Bottom=�6�   
�������
����������	#FFFC7A086�   �� 5   
   -
   5        $ 
PrintFiles��5      $��Print Files��6%   $
��Left=�6;   $��	10,0,0,18q�6V   $	��167��6i   $��1��6�   $��26��6�   $��Bottom=�6�   
�������
����������	#FFFC7A086�   5      5      