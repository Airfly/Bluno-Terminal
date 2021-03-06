## Bluno Terminal/Bluno终端

This is Bluno Terminal's Windows 8 Store App project [Bluno-Terminal](https://github.com/Airfly/Bluno-Terminal).

这是Bluno终端的Windows 8 Store App 项目。

Bluno Terminal is a terminal of Bluno which has an on-board BLE chip: TI CC2540 of Arduino UNO development board. The app can connect to and communicate with Bluno's Serial via BLE (Bluetooth 4.0). We can send AT command to config the BLE through the app. We can also read/write data between the app and an Arduino Code (Programming), in this case, just initialize the Serial with baud rate of 115200 (Statement: Serial.begin(115200);), then reads/writes data.

Bluno终端是拥有集成蓝牙4.0的Bluno开发板并兼容Arduino UNO的无线终端。本应用可以通过蓝牙4.0（BLE）连接到并与Bluno开发板交互免费应用。我们可以通过本应用发送AT命令来配置BLE。我们也可以在本应用和Arduino代码程序之间读写数据，只需要初始化相应串口速率为115200（语法： Serial.begin(115200);），然后就可以读写数据了。也可以把Bluno终端看成是一款串口助手应用。

### Tips/提示

* Sending +++ to switch into AT Mode, then sending AT+PASSWORD=[your passcode] for authentication.
* 发送 +++ 可切换到AT指令模式，然后发送 AT+PASSWORD=[你的密码] 进行认证。

* Sending AT+EXIT to switch back to normal mode.
* 发送 AT+EXIT 切换回到普通模式。

### Related/相关的

We also publish an iOS version in App Store, check it out below:
https://itunes.apple.com/us/app/bluno-terminal/id794109935?mt=8
我们还发布了一款iOS版本的Bluno终端，可以从下列链接中获取：
https://itunes.apple.com/cn/app/bluno-zhong-duan/id794109935?mt=8

For more information about Bluno, please visit web page: http://www.dfrobot.com/wiki/index.php/Bluno_SKU:DFR0267
Bluno开发板更多信息，请访问网页：
http://wiki.dfrobot.com.cn/index.php/(SKU:DFR0267)Bluno%E8%93%9D%E7%89%994.0%E6%8E%A7%E5%88%B6%E5%99%A8_%E5%85%BC%E5%AE%B9Arduino

### Screen shot/屏幕截图

![蓝牙配对](screenshot/1-pair.png "蓝牙配对")

![磁贴](screenshot/2-tile.png "磁贴")

![蓝牙访问许可](screenshot/3-running.png "蓝牙访问许可")

![数据发送及接收](screenshot/4-running.png "数据发送及接收")

### Note/说明

This project was built and tested under Windows 8.1 with Visula Studio 2013. And did not test on Windows 8 yet.

这个项目在Windows 8.1下使用Visual Studio 2013创建和测试的。还没有在Windows 8上测试过，并且如果在Visual Stduio 2012无法打开的情况下，可能需要另外创建空白项目，并导入或拷贝代码。

## License/许可

This code is distributed under the terms and conditions of the [MIT license](LICENSE).
本项目代码采用 [MIT license](LICENSE) 发布。

## Donate/捐赠

Your kind donations will help [me](https://github.com/Airfly) on my open source. If you like it you can buy me a drink, too. Thanks.

>Donate $1.00 USD
>
><a target="_top" href="https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&amp;hosted_button_id=5YY6R2LN33SSC"><img src="https://www.paypalobjects.com/en_US/C2/i/btn/btn_buynowCC_LG.gif" border="0" name="submit" alt="PayPal - The safer, easier way to pay online!"></a>

您的好意将会对我的开源代码有很大的帮助，如果您喜欢这些代码或是这些代码对您有用，我将会更加高兴，如果您买一支冰啤给我。感谢。我的支付宝帐号是：[1272000@163.com](http://alipay.com) 。当然您也可以使用PayPal操作：

>捐赠我1美元
>
><a target="_top" href="https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&amp;hosted_button_id=3KSDZPY9GCAMW"><img src="https://www.paypalobjects.com/zh_XC/C2/i/btn/btn_buynowCC_LG.gif" border="0" name="submit" alt="PayPal——最安全便捷的在线支付方式！"></a>
