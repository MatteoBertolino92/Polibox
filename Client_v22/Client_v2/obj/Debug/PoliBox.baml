   M S B A M L   `   `   `  ����D  @Client_v2, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null%clr-namespace:Client_v2	Client_v2  5   	       Client_v2.PoliBox   5   	   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   )localclr-namespace:Client_v2     5   	   -   5          � Title$  PoliBox��5	   	   $��239.925��6    $��315.113��61   ���	#FF2233A8 � Icon$ Resources/icon.jpg��6X   .��5
      � ���	#FFFF7C11$��10,60,10,10q�6"   .+�5   
   �� -   5         ��Name$ image��5      $
��Left=�6   $��53��6:   $��
-2,-56,0,0q�6F   $	��Top=�6Z   $	��128��6r   $��Resources/frontimage.png��6~    J� OpacityMask 5      �� 5       �� ImageSource$ Resources/frontimage.png��6    j� Stretch$ UniformToFill=�6D   5      �� 5   
   -   5         ��Name$ Title��5      $��PoliBox��6   $��Right=�61   $��0,-56,1.444,0q�6M   
�������$��13.333(�6w   $��26��6�   $	��Top=�6�   A� 5   
   -   5         A�Name$
 ell1��5      
���  ��
���   � J� RenderTransformOrigin$ -0.111,-5.409&�6:   $��0,-19,1.444,0q�6`   $��12��6w   $	��Top=�6�   $��Right=�6�   $��12��6�   �� 5   
   -   5        	 ��Name$	 Status1��5      $��Right=�6%   $��0,-19,18.444,0q�6A   $
��Wrap=�6Y   $��Not connected��6m   $	��Top=�6�   $��63��6�   
�������$��9.333(�6�   
 �� TextAlignment$
 Right=�6�   �� 5   
   -   5        $	 WelcomeTextBlock��5      $��
10,10,10,0q�6.   $
��Wrap=�6B   $��Welcome��6V   $	��Top=�6e   $��16(�6}   
�������$
 Center=�6�   ��6�   Z� 6�   �� 5   
   -   5         ��Name$ DisconnectButton��5      $��
Disconnect��6+   $��10,111,10,0q�6@   $	��Top=�6U   
����������	#FFFF7C11
�������6  �� 5   
   -   5        $ RecoveryButton��5      $��Recovery��6)   $��
10,86,10,0q�6<   $	��Top=�6P   
����������	#FFFF7C11
�������6  �� 5   
   -	   5        $ ShowContentButton��5      $��Show Content��6,   $��
10,61,10,0q�6C   $	��Top=�6W   
����������	#FFFF7C11
�������6!  �� 5   
   -
   5        $ SyncronizeButton_Copy��5      $��
Sincronize��60   $��
10,36,10,0q�6E   
����������	#FFFF7C11
�������$��20��6$  $	��Top=�60  5      5      