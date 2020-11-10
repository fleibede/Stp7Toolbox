using System;
using System.Collections.Generic;
using System.Linq;

namespace Stp7Toolbox
{
    public partial class PlcParser
    {        public enum DatabaseType
        {
            NormalBlocks,
            NormalBlocksList,
            NormalBlockFolders,
            SystemDataFolders,
            SystemDataBlock
        }

        public static Dictionary<int, string> BlockTypes = new Dictionary<int, string>()
        {
            {1, "UDT"},
            {8,  "OB"},
            {10, "DB"},
            {12, "FC"},
            {14, "FB"},
        };

        public const byte DbType10 = 10;//DB (MC7) (Normal block type)
        public const byte DbType20 = 20;//DB extra info shown in Simatic Manager


        public static Dictionary<int, string> BlockLanguages = new Dictionary<int, string>()
        {
           {1   ,"AWL" },
           {2   ,"KOP"},
           {3   ,"FUP"},
           {4   ,"SCL"},
           {5   ,"DB"},
           {6   ,"GRAPH"},
           {7   ,"SDB"},
           {8   ,"CPU DB"},
           {9   ,"ASSEMBLER"},
           {10  ,"DB (of FB)"},
           {11  ,"DB (of SFB)"},
           {12  ,"DB (of UDT)"},
           {13  ,"GD"},
           {14  ,"PARATOOL"},
           {15  ,"??00015"},
           {16  ,"NCM"},
           {17  ,"CPU SDB"},
           {18  ,"STL S7-200"},
           {19  ,"LAD ST-200"},
           {20  ,"MCU IBN"},
           {21  ,"C for S7"},
           {22  ,"HIGRAPH"},
           {23  ,"CFC"},
           {24  ,"SFC"},
           {25  ,"CFC/SFC"},
           {26  ,"S7-PDIAG"},
           {27  ,"reserve 27"},
           {28  ,"reserve 28"},
           {29  ,"SFM"},
           {30  ,"CHART"},
           {31  ,"AWL F"},
           {32  ,"KOP F"},
           {33  ,"FUP F"},
           {34  ,"DB F"},
           {35  ,"CALL F"},
           {36  ,"D7-SYS"},
           {37  ,"Tech Obj"},
           {38  ,"KOP F (unchecked)"},
           {39  ,"FUP F (unchecked)"},
           {40  ,"SFM"}
        };

        static bool isBitSet(byte b, int bitPos)
        {
            return (b & (1 << bitPos)) != 0;
        }

        static byte modifyBitInByte(byte b, int bitPos, bool state)
        {
            if (bitPos > 7 || bitPos < 0)
                throw new Exception("Bitposition cannot be greater than 7 or less than zero.");
            byte mask = (byte)(1 << bitPos);
            if (state)
                b = (byte)(b | mask);
            else
                b = (byte)(b & ~mask);
            return b;
        }

        public static byte[] SwapUIntToByte(uint Value)
        {
            return BitConverter.GetBytes(SwapUInt(Value));
        }

        public static byte[] SwapUShortToByte(ushort Value)
        {
            return BitConverter.GetBytes(SwapUShort(Value));
        }

        public static ushort SwapUShort(ushort Value)
        {
            return (ushort)(((Value >> 8) & 0xFF) | ((Value << 8) & 0xFF00));
        }


        public static uint SwapUInt(uint Value)
        {
            return (Value >> 24) | ((Value << 8) & 0x00FF0000) | 
                ((Value >> 8) & 0x0000FF00) | (Value << 24);
        }

        public static ushort SwapUInt(ushort Value)
        {
            return (ushort)(((Value >> 8) & 0xFF) | 
                ((Value << 8) & 0xFF00));
        }

        public static void parseS7Time(
            byte[] rawTime,
            out DateTime time,
            out byte[] timeMs,
            out byte[] timeDy
            )
        {
            rawTime = PadByteArray(rawTime, 6, false);
            time = timeFroms7time(rawTime);
            timeMs = new byte[4];
            timeDy = new byte[2];
            if (rawTime.Length == 6)
            {
                Array.Copy(rawTime, 4, timeDy, 0, 2);
                Array.Copy(rawTime, 0, timeMs, 0, 4);
            }
        }

        public static DateTime timeFroms7time(byte[] input)
        {
            DateTime s7time = new DateTime(1984, 1, 1);
            //s7time.AddSeconds(441763200L); /* 1.1.1984 00:00:00 */

            if (input.Length < 6)
                return s7time;
            byte[] number = input;
            UInt16 days = GetUIntAt(number, 4);
            UInt32 day_msec = GetUDIntAt(number, 0);

            s7time = s7time.AddSeconds(days * (24 * 60 * 60));
            s7time = s7time.AddMilliseconds(day_msec);

            return s7time;
        }

        public static byte[] s7timeFromDateTime(DateTime input)
        {
            DateTime time1981 = new DateTime(1984, 1, 1);
            TimeSpan s7Time = input - time1981;
            UInt16 days = (UInt16)s7Time.Days;
            UInt32 msec = (UInt32)((s7Time.Hours * 3600 + s7Time.Minutes 
                * 60 + s7Time.Seconds) * 1000 + s7Time.Milliseconds);
            byte[] bDays = GetBytesUint16(SwapUInt(days));
            byte[] bmsec = GetBytesUint32(SwapUInt(msec));
            byte[] s7time = bmsec.Concat(bDays).ToArray();
            return s7time;
        }

        public static byte[] PadByteArray(
           byte[] bytes,
           int totSize,
           bool toLeft = false)
        {
            if (bytes.Length >= totSize)
                return bytes;
            if (toLeft)
                return (new byte[totSize - bytes.Length]).Concat(bytes).ToArray();
            else
                return bytes.Concat(new byte[totSize - bytes.Length]).ToArray();
        }

        public static byte[] GetBytesUint32(UInt32 input)
        {
            byte[] Result = new byte[4];
            Result[0] = (byte)(input & 0x000000FF);
            Result[1] = (byte)((input >> 8) & 0x000000FF);
            Result[2] = (byte)((input >> 16) & 0x000000FF);
            Result[3] = (byte)((input >> 24) & 0x000000FF);
            return Result;
        }

        public static byte[] GetBytesUint16(UInt16 input)
        {
            byte[] Result = new byte[2];
            Result[0] = (byte)(input & 0x000000FF);
            Result[1] = (byte)((input >> 8) & 0x000000FF);
            return Result;
        }


        #region Get/Set 32 bit unsigned value (S7 UDInt) 0..4294967296
        public static UInt32 GetUDIntAt(byte[] Buffer, int Pos)
        {
            UInt32 Result;
            Result = Buffer[Pos]; Result <<= 8;
            Result |= Buffer[Pos + 1]; Result <<= 8;
            Result |= Buffer[Pos + 2]; Result <<= 8;
            Result |= Buffer[Pos + 3];
            return Result;
        }
        #endregion
        #region Get/Set 16 bit unsigned value (S7 UInt) 0..65535
        public static UInt16 GetUIntAt(byte[] Buffer, int Pos)
        {
            return (UInt16)((Buffer[Pos] << 8) | Buffer[Pos + 1]);
        }
        #endregion


    }
}
