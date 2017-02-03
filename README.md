# KTM Komuter Link Card Balance Reader

[![N|Solid](https://cldup.com/dTxpPi9lDf.thumb.png)](#)

The program provides a button to modify card balance. By using the program you aware of using the modified balance is illegal. The author shall not be liable for any direct, indirect, incidental, special, exemplary, or consequential damages however caused arising in any way out of the use of this software. You must use this software for educational purposes only.

The NFC reader used here is ACR122U (http://invl.co/ocv)

## Preparation
In order to read/write the value, the program require the key to sector 02 of the card. I'm not going to explain in detail here. But as a summary

1.Boot your computer to Kali Linux

2.Connect NFC Reader and the card. 

3.Execute from terminal. Once the command executed without errors, you may want to save the output.mfd to cloud storage or external USB storage to be used in the next step
```sh
$ mfoc -O output.mfd
```
4.From Windows, read back the output.mfd using https://github.com/zhovner/mfdread and copy the sector 02 key A (pink color)
```sh
$ mfdread.py ./output.mfd
```
** The exploit is due to default keys for sector 05 and above and of course the usage of classic mifare which been proven insecure since 2008.

## Installation
1. Open the project using Microsoft Visual Studio 2015
2. Install PCSC using NuGet 
3. Connect the card to NFC Reader
4. Run/Debug

Once a dialog prompt the key, enter the sector 02, A key, the key will be saved into persistance.json

### Todos

 - Hmmm, perhaps improve this documentation

License
----

MIT
