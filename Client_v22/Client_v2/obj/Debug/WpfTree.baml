   M S B A M L   `   `   `  ����D  @Client_v2, Version=1.0.0.0, Culture=neutral, PublicKeyToken=nullC&clr-namespace:WpfTreeViewBinding.ModelWpfTreeViewBinding.Model  5   	   #    WpfTreeViewBinding.MainWindow   5   	   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   8Model&clr-namespace:WpfTreeViewBinding.Model     5   	     � Title$  
MainWindow��5   	   $	��519��6   $	��484��6)   ���	#FF2234A9 � Icon$ Resources/icon.jpg��6L    � 	Resources 5       �� DataType,   &WpfTreeViewBinding.Model.DirectoryItem �� ItemsSource �� Path � ToolTip'   !WpfTreeViewBinding.Model.FileItem��%  (��       5	   #   *, +)(�� �     5      *, +)�� 5	   
   "  6#    5
   #   �� *Items+.R�5      �� ��6   �� $
 Name� 63   �� $
 Path�5      �� 5   
   "  6   .R�5      �� ��6   �� $
 Name� 63   �� $
 Path�5      5      .��5      � $��10,60,10,10q�6   ���	#FFFF7911.+�5   
   �� -   5         ��Name$ image��5      $
��Left=�6   $��53��6:   $��
-2,-56,0,0q�6F   $	��Top=�6Z   $	��128��6r   $��Resources/frontimage.png��6~    J� OpacityMask 5      �� 5      	 �� ImageSource$	 Resources/frontimage.png��6   
 j� Stretch$
 UniformToFill=�6D   5      �� 5   
   -   5         ��Name$ Title��5      $��PoliBox��6   $��Right=�61   $��0,-56,6.333,0q�6M   
�������$��13.333(�6w   $��26��6�   $	��Top=�6�   A� 5   
   -   5         A�Name$
 ell2��5      
���  ��
���   � J� RenderTransformOrigin$ -0.111,-5.409&�6:   $��0,-19,1.556,0q�6`   $��12��6w   $	��Top=�6�   $��Right=�6�   $��12��6�   �� 5   
   -   5         ��Name$ Status��5      $��Right=�6$   $��0,-19,19.556,0q�6@   $
��Wrap=�6X   $��Not connected��6l   $	��Top=�6�   $��63��6�   
�������$��9.333(�6�    �� TextAlignment$ Right=�6�   R� 5   
   $��20,40q�6+   ���	#FFFF7911
�������$��9.333(�6e   ��6   �� 6e   �� 5   
   -   5        $ 	textBlock��5      $��
20,10,20,0q�6'   $
��Wrap=�6;   $%��Contents of directory monitored��6O   $	��Top=�6v   $��13.333(�6�   $ Center=�6�   
�������$��30��6�   $ 0.5,0.5&�6�   z�5      V� 5      .~�5       �� �� 5!      �� 5"       �� Angle$ 0.497Q�6&   T� 5#      5$      5&      5'      5(      