using PCSC;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MifareClassic
{
    class MifareTool
    {
        string[] szReaders;
        SCardContext hContext;
        SCardReader reader;
        IntPtr pioSendPci;

        public String[] GetReaders()
        {
            return szReaders;
        }

        public void OnbuzzerLED()
        {
            //0xFF,0x00,0x40,0x82,0x04,0x01,0x01,0x01,0x03
            sendCommand(reader, pioSendPci, new byte[] { 0xFF, 0x00, 0x40, 0x82, 0x04, 0x01, 0x01, 0x01, 0x03 });
        }

        public void Connect(int position)
        {
            if (hContext.CheckValidity() != PCSC.SCardError.Success)
                init();
            // Create a reader object using the existing context
            reader = new SCardReader(hContext);
            // Connect to the card
            SCardError err = reader.Connect(szReaders[position],
                SCardShareMode.Shared,
                SCardProtocol.T0 | SCardProtocol.T1);
            CheckErr(err);

            switch (reader.ActiveProtocol)
            {
                case SCardProtocol.T0:
                    pioSendPci = SCardPCI.T0;
                    break;
                case SCardProtocol.T1:
                    pioSendPci = SCardPCI.T1;
                    break;
                default:
                    throw new PCSCException(SCardError.ProtocolMismatch,
                        "Protocol not supported: "
                        + reader.ActiveProtocol.ToString());
            }

        }

        public void Disconnect()
        {
            hContext.Release();
        }

        protected void init()
        {
            // Establish SCard context
            hContext = new SCardContext();
            hContext.Establish(SCardScope.System);

            // Retrieve the list of Smartcard readers
            szReaders = hContext.GetReaders();
            if (szReaders.Length <= 0)
                throw new PCSCException(SCardError.NoReadersAvailable,
                    "Could not find any Smartcard reader.");
        }

        public MifareTool()
        {
            init();
        }

        static void CheckErr(SCardError err)
        {
            if (err != SCardError.Success)
                throw new PCSCException(err,
                    SCardHelper.StringifyError(err));
        }

        public static string ByteArrayToString(byte[] ba, bool addSpace = true)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
            {
                if (addSpace)
                    hex.AppendFormat("{0:x2} ", b);
                else
                    hex.AppendFormat("{0:x2}", b);
            }

            return hex.ToString();
        }

        public byte[] GetUid()
        {
            // Get UID
            byte[] apdu = new byte[] { 0xFF, 0xCA, 0x00, 0x00, 0x00 };
            return trim(sendCommand(reader, pioSendPci, apdu)).ToArray();
        }

        public bool AuthenticateBlock(String hexKey,string blockNo)
        {
            //load Auth key 
            hexKey = "FF82000006" + hexKey;
            byte[] apdu = StringToByteList(hexKey).ToArray();
            //Console.WriteLine(ByteArrayToString(apdu));
            byte[] output = sendCommand(reader, pioSendPci, apdu);
            //Console.WriteLine(ByteArrayToString(output));
            blockNo = "FF860000050100" + blockNo + "6000";
            apdu = StringToByteList(blockNo).ToArray();
            //Console.WriteLine(ByteArrayToString(apdu));
            return ByteArrayCompare(sendCommand(reader, pioSendPci, apdu), new Byte[] {0x90 ,0x00 });
        }

        public byte[] ReadValueBlock(string blockNo)
        {
            byte[] apdu = StringToByteList("FFB100" + blockNo + "04").ToArray();
            byte[] output = sendCommand(reader, pioSendPci, apdu);
            //check for 6300 (error)
            if(ByteArrayCompare(output,new Byte[] {0x63,0x00 }))
            {
                throw new System.ArgumentException("Read Value Block Failed!", "sendCommand");
            }
            //Console.WriteLine(ConvertBytesToInteger(trim(output).ToArray()));
            return trim(output).ToArray();
        }

        public bool WriteValueBlock(String hexBlockNo, int value)
        {
            string hexValue = value.ToString("X4");
            while (true)
            {
                if (hexValue.Length < 10)
                    hexValue = "0" + hexValue;
                else
                    break;
            }
            byte[] apdu = StringToByteList("FFD700" + hexBlockNo + "05" + hexValue).ToArray();
            byte[] output = sendCommand(reader, pioSendPci, apdu);
            Console.WriteLine(ByteArrayToString(apdu));
            Console.WriteLine(ByteArrayToString(output));
            //check for 90 00 (success)
            if (ByteArrayCompare(output, new Byte[] { 0x90, 0x00 }))
            {
                return true;
            }
            return false;
        }

        public static int ConvertBytesToInteger(byte[] bytes)
        {
            // If the system architecture is little-endian (that is, little end first),
            // reverse the byte array.
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);

            return BitConverter.ToInt32(bytes, 0);
        }


        public static List<byte> StringToByteList(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToList();
        }


        static List<byte> trim(byte[] data)
        {
            List<byte> resp = new List<byte>(data);
            byte sl = 0x90;
            byte l = 0x00;
            if(resp[resp.Count-2]==sl && resp[resp.Count - 1]==l)
                resp.RemoveRange(resp.Count - 2, 2);
            return resp;
        }

        public static bool ByteArrayCompare(byte[] a1, byte[] a2)
        {
            return StructuralComparisons.StructuralEqualityComparer.Equals(a1, a2);
        }

        public static byte[] sendCommand(SCardReader reader, IntPtr pioSendPci, byte[] apdu)
        {
            SCardError err;
            byte[] pbRecvBuffer = new byte[256];
            err = reader.Transmit(pioSendPci, apdu, ref pbRecvBuffer);
            CheckErr(err);
            return pbRecvBuffer;
        }


        


    }
}
