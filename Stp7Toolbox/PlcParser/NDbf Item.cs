using System;
using System.Linq;
using System.Text;
using NDbfReaderEx; //Can read Memo fiels (MC7 etc is in memo), but not write.

namespace Stp7Toolbox
{
    public partial class PlcParser
    {
        public class Item
        {

            public DatabaseType GeneralType { set; get; }
            public string DatabaseId { set; get; }
            public string BlockNumber { set; get; }
            public ushort bBlockNumber { set; get; }
            public string BlockType { set; get; }
            public byte bBlockType { private set; get; }
            public int iBlockType { set; get; }
            public string BlockLanguage { set; get; }
            public byte bBlockLanguage { private set; get; }
            public DateTime IntfTime { set; get; }
            private byte[] IntfTime_ms { set; get; }
            private byte[] IntfTime_dy { set; get; }
            public DateTime CodeTime { set; get; }
            private byte[] CodeTime_ms { set; get; }
            private byte[] CodeTime_dy { set; get; }
            public ushort Checksum { set; get; }

            ushort Mc5Len { set; get; }
            uint uintMc5Len { set; get; }
            ushort SsbLen { set; get; }
            uint uintSsbLen { set; get; }
            ushort AddLen { set; get; }
            uint uintAddLen { set; get; }
            ushort LocDataLen { set; get; }
            uint uintLocDataLen { set; get; }
            public uint BlockTotLen { set; get; } //remove public after SDB emthod
            uint LoadMemLen { set; get; }
            byte Attribute { set; get; }
            byte[] AgReserved { set; get; }
            byte Password { set; get; }
            string Author { set; get; }
            byte[] bAuthor { set; get; }
            string Family { set; get; }
            byte[] bFamily { set; get; }
            string Name { set; get; }
            byte[] bName { set; get; }
            byte Version { set; get; }

            public string MC5CODE { set; get; }
            private byte[] bMC5CODE { set; get; }
            public string SSBPART { set; get; }
            private byte[] bSSBPART { set; get; }
            public string ADDINFO { set; get; }
            private byte[] bADDINFO { set; get; }

            // Blockslist columns
            uint NumFB { set; get; }
            uint NumFC { set; get; }
            uint NumDB { set; get; }
            uint NumOB { set; get; }
            public int TotNumberOfBlocks { private set; get; }

            // SDB List data
            string SystemDataMLFB { set; get; }
            public byte[] bSystemDataMLFB {private set; get; }
            // SDB PG file data
            byte[] PGcode { set; get; } // header byte 4
            byte[] PGfooter { set; get; } // header byte 4
            byte PGbyte0 { set; get; } // header byte 4
            byte PGbyte2 { set; get; }// header byte 5

            public Item(DbfRow row, DatabaseType GeneralType, string path = "")
            {
                switch (GeneralType)
                {
                    case (PlcParser.DatabaseType.SystemDataBlock):
                        parseSystemDataBlock(row, path);
                        break;
                    case (PlcParser.DatabaseType.NormalBlocks):
                        parseNormalBlock(row);
                        break;
                    case (PlcParser.DatabaseType.NormalBlockFolders):
                        parseBlockFolders(row);
                        break;
                    case (PlcParser.DatabaseType.SystemDataFolders):
                        parseSystemDataFolders(row);
                        break;
                }
            }

            public void parseNormalBlock(DbfRow row)
            {
                GeneralType = PlcParser.DatabaseType.NormalBlocks;

                DatabaseId = row.GetValue("OBJECTID").ToString();

                BlockNumber = row.GetValue("BLKNUMBER").ToString();
                bBlockNumber = ushort.Parse(BlockNumber);

                string lanStrig = row.GetValue("BLKLANG").ToString();
                bBlockLanguage = byte.Parse(lanStrig);
                try
                {
                    BlockLanguage = BlockLanguages[int.Parse(lanStrig)];
                }
                catch { }

                string blockTypeNr = row.GetValue("SUBBLKTYP").ToString();
                bBlockType = byte.Parse(blockTypeNr);
                BlockType = BlockTypes.TryGetValue(bBlockType, out string BlkType) ? BlkType : blockTypeNr;

                string time1 = row.GetValue("TIMESTAMP1").ToString();
                byte[] A = encoding.GetBytes(time1);
                parseS7Time(A,
                    out DateTime time,
                    out byte[] time_ms,
                    out byte[] time_dy);
                CodeTime = time;
                CodeTime_ms = time_ms;
                CodeTime_dy = time_dy;

                string time2 = row.GetValue("TIMESTAMP2").ToString();
                byte[] B = encoding.GetBytes(time2);
                parseS7Time(B,
                    out DateTime time_2,
                    out time_ms,
                    out time_dy);
                IntfTime = time_2;
                IntfTime_ms = time_ms;
                IntfTime_dy = time_dy;

                Checksum = ushort.Parse(row.GetValue("CHECKSUM").ToString());

                if (bBlockType != 6 && bBlockType != 5)
                {
                    Mc5Len = ushort.Parse(row.GetValue("MC5LEN").ToString());
                    SsbLen = ushort.Parse(row.GetValue("SSBLEN").ToString());
                    AddLen = ushort.Parse(row.GetValue("ADDLEN").ToString());
                    LocDataLen = ushort.Parse(row.GetValue("LOCDATALEN").ToString());

                }
                else
                {
                    uintMc5Len = uint.Parse(row.GetValue("MC5LEN").ToString());
                    uintSsbLen = uint.Parse(row.GetValue("SSBLEN").ToString());
                    uintAddLen = uint.Parse(row.GetValue("ADDLEN").ToString());
                    uintLocDataLen = uint.Parse(row.GetValue("LOCDATALEN").ToString());
                }

                string blkLength = row.GetValue("BLKTOTLEN").ToString();
                BlockTotLen = uint.Parse(blkLength);
                LoadMemLen = uint.Parse((BlockTotLen - 6).ToString());

                /// Attribute
                /// Non-retain: 00X0 0000: 1:Non retain=yes, 0=Non retain=no
                string attr = row.GetValue("ATTRIBUTE").ToString();
                Attribute = byte.Parse(attr);

                AgReserved = encoding.GetBytes(row.GetValue("AGRESERVED").ToString());

                Password = byte.Parse(row.GetValue("PASSWORD").ToString());

                Author = row.GetValue("USERNAME").ToString();
                bAuthor = encoding.GetBytes(Author);

                Family = row.GetValue("BLOCKFNAME").ToString();
                bFamily = encoding.GetBytes(Family);

                Name = row.GetValue("BLOCKNAME").ToString();
                bName = encoding.GetBytes(Name);

                Version = byte.Parse(row.GetValue("VERSION").ToString());

                try
                {
                    MC5CODE = row.GetValue("MC5CODE").ToString();
                    bMC5CODE = encoding.GetBytes(MC5CODE);

                    SSBPART = row.GetValue("SSBPART").ToString();
                    bSSBPART = encoding.GetBytes(SSBPART);

                    ADDINFO = row.GetValue("ADDINFO").ToString();
                    bADDINFO = encoding.GetBytes(ADDINFO);
                }
                catch (Exception Ex)
                {
                    Console.WriteLine(Ex);
                }



            }

            public void parseSystemDataBlock(DbfRow row, string path)
            {
                GeneralType = PlcParser.DatabaseType.SystemDataBlock;

                DatabaseId = row.GetValue("ID").ToString();

                BlockNumber = row.GetValue("NUMBER").ToString();
                bBlockNumber = ushort.Parse(BlockNumber);

                string blockType = row.GetValue("TYPE").ToString();
                iBlockType = int.Parse(blockType);
                BlockType = "SDB";

                string time1 = row.GetValue("TS1").ToString();
                byte[] A = encoding.GetBytes(time1);
                parseS7Time(A,
                    out DateTime time,
                    out byte[] time_ms,
                    out byte[] time_dy);
                CodeTime = time;
                CodeTime_ms = time_ms;
                CodeTime_dy = time_dy;

                string time2 = row.GetValue("TS2").ToString();
                byte[] B = encoding.GetBytes(time2);
                parseS7Time(B,
                    out time,
                    out time_ms,
                    out time_dy);
                IntfTime = time;
                IntfTime_ms = time_ms;
                IntfTime_dy = time_dy;

                string blkLength = row.GetValue("SIZE").ToString();
                BlockTotLen = uint.Parse(blkLength);
                LoadMemLen = uint.Parse((BlockTotLen - 6).ToString());

                string attr = row.GetValue("ATTRIBUTE").ToString();
                Attribute = byte.Parse(attr);

                Author = row.GetValue("PRODUCER").ToString();
                bAuthor = encoding.GetBytes(Author);

                Name = row.GetValue("SDBNAME").ToString();
                bName = encoding.GetBytes(Name);

                Version = byte.Parse(row.GetValue("VERSION").ToString());

                parsePGfile(path);
            }

            public void parseBlockFolders(DbfRow row)
            {
                GeneralType = PlcParser.DatabaseType.NormalBlockFolders;

                DatabaseId = row.GetValue("ID").ToString();

                NumFB = ushort.Parse(row.GetValue("ANZFB").ToString());
                NumFC = ushort.Parse(row.GetValue("ANZFC").ToString());
                NumOB = ushort.Parse(row.GetValue("ANZOB").ToString());
                NumDB = ushort.Parse(row.GetValue("ANZDB").ToString());
                TotNumberOfBlocks = (int)NumFB + (int)NumFC + (int)NumOB + (int)NumDB;

                string time1 = row.GetValue("Lastmod").ToString();
                byte[] A = encoding.GetBytes(time1);
                parseS7Time(A,
                    out DateTime time,
                    out byte[] time_ms,
                    out byte[] time_dy);
                CodeTime = time;
                CodeTime_ms = time_ms;
                CodeTime_dy = time_dy;

                string time2 = row.GetValue("Crdate").ToString();
                byte[] B = encoding.GetBytes(time2);
                parseS7Time(B,
                    out time,
                    out time_ms,
                    out time_dy);
                IntfTime = time;
                IntfTime_ms = time_ms;
                IntfTime_dy = time_dy;

            }

            public void parseSystemDataFolders(DbfRow row)
            {
                GeneralType = PlcParser.DatabaseType.SystemDataFolders;

                DatabaseId = row.GetValue("ID").ToString();

                SystemDataMLFB = row.GetValue("MLFB").ToString();
                bSystemDataMLFB = encoding.GetBytes(SystemDataMLFB);
            }




            public override string ToString()
            {
                switch (GeneralType)
                {
                    case (PlcParser.DatabaseType.SystemDataBlock):
                        return printSystemDataList();

                    case (PlcParser.DatabaseType.NormalBlocks):
                        return printNormalBlock();

                    case (PlcParser.DatabaseType.SystemDataFolders):
                        return printSystemDataMain();

                    case (PlcParser.DatabaseType.NormalBlockFolders):
                        //TODO return printSystemDataList();
                        return "";

                    default:
                        return "General blocktype not found/r/n";
                }

            }

            public string printNormalBlock()
            {
                StringBuilder result = new StringBuilder();
                result.Append("ID: " + DatabaseId);
                result.Append(" Block Num: " + BlockNumber);
                result.Append(" Block Lan: " + BlockLanguage);
                result.Append(" BlkType: " + BlockType);
                result.Append(" IntfTime: " + IntfTime.ToString("o"));
                result.Append(" CodeTime: " + CodeTime.ToString("o"));
                result.Append(" Psw: " + (Password == 3) + " (" + Password + ")");
                result.Append(" Attr: " + Attribute);
                result.Append("\r\n");
                result.Append(" IntfTime: " + BitConverter.ToString(IntfTime_ms) + "-" + BitConverter.ToString(IntfTime_dy));
                result.Append("\r\n");
                result.Append(" CodeTime: " + BitConverter.ToString(CodeTime_ms) + "-" + BitConverter.ToString(CodeTime_dy));
                result.Append("\r\n");
                result.Append("MC5CODE \r\n");
                result.Append(MC5CODE + "\r\n");
                result.Append("\r\n");
                result.Append("SSBPART (length: " + SsbLen + "\r\n");
                result.Append(SSBPART + "\r\n");
                result.Append("ADDINFO (length: " + AddLen + "\r\n");
                result.Append(ADDINFO + "\r\n");
                return result.ToString();
            }

            public string printSystemDataList()
            {
                StringBuilder result = new StringBuilder();
                result.Append("ID: " + DatabaseId);
                result.Append(" (hex:  " + int.Parse(DatabaseId).ToString("x"));
                result.Append(") Block Num: " + BlockNumber);
                result.Append(" BlkType: " + BlockType);
                result.Append(" / " + iBlockType);
                result.Append(" IntfTime: " + IntfTime.ToString("o"));
                result.Append(" CodeTime: " + CodeTime.ToString("o"));
                result.Append(" Size: " + BlockTotLen);
                result.Append("\r\n");
                result.Append("PG code \r\n");
                result.Append(encoding.GetString(PGcode) + "\r\n");
                result.Append("PG footer \r\n");
                result.Append(encoding.GetString(PGfooter) + "\r\n");
                return result.ToString();
            }

            public string printSystemDataMain()
            {
                StringBuilder result = new StringBuilder();
                result.Append("ID: " + DatabaseId);
                result.Append(" MLFB: " + SystemDataMLFB);
                result.Append("\r\n");
                return result.ToString();
            }

            public byte[] getFullCode()
            {
                switch (GeneralType)
                {
                    case (PlcParser.DatabaseType.SystemDataBlock):
                        return getSdbFull();

                    case (PlcParser.DatabaseType.NormalBlocks):
                        return getMC7Full();

                    default:
                        return new byte[1]; ;
                }
            }

            public byte[] getMC7Full()
            {
                byte[] header = getMC7Header();
                byte[] code = getMC7Code();
                byte[] footer = getMC7Footer();                
                byte[] mc7 = new byte[header.Length + code.Length + footer.Length];
                Buffer.BlockCopy(header, 0, mc7, 0, header.Length);
                Buffer.BlockCopy(code, 0, mc7, header.Length, code.Length);
                Buffer.BlockCopy(footer, 0, mc7, header.Length + code.Length, footer.Length);
                return mc7;
            }

            public byte[] getMC7Code()
            {
                byte[] mc7 = new byte[Mc5Len];
                Buffer.BlockCopy(bMC5CODE, 0, mc7, 0, Mc5Len);
                return mc7;
            }

            public byte[] getMC7Header()
            {
                byte[] header = new byte[36];

                header[0] = 0x70; //p 0
                header[1] = 0x70; //p 1
                header[2] = 0x01; //01 == blocks, 03 == systemdatablock
                header[3] = Attribute; // Flags? 3
                header[4] = bBlockLanguage; //4
                header[5] = bBlockType; // 5
                byte[] tmp = SwapUShortToByte(bBlockNumber);
                header[6] = tmp[0]; header[7] = tmp[1];// 6-7
                tmp = SwapUIntToByte(LoadMemLen);
                header[8] = tmp[0]; header[9] = tmp[1]; // TotLen - 8-9-10-11
                header[10] = tmp[2]; header[11] = tmp[3]; // TotLen - 8-9-10-11
                header[12] = 0x00; header[13] = 0x00; header[14] = 0x00;// //??? 12-13-14
                header[15] = Password; //BlkSec 15
                header[16] = CodeTime_ms[0]; header[17] = CodeTime_ms[1]; // 16-17-18-19
                header[18] = CodeTime_ms[2]; header[19] = CodeTime_ms[3]; // 16-17-18-19
                header[20] = CodeTime_dy[0]; header[21] = CodeTime_dy[1]; // 20-21
                header[22] = IntfTime_ms[0]; header[23] = IntfTime_ms[1]; // 22-23-24-25
                header[24] = IntfTime_ms[2]; header[25] = IntfTime_ms[3]; // 22-23-24-25
                header[26] = IntfTime_dy[0]; header[27] = IntfTime_dy[1]; // 26-27
                tmp = SwapUShortToByte(SsbLen);
                header[28] = tmp[0]; header[29] = tmp[1]; // 28-29
                tmp = SwapUShortToByte(AddLen);
                header[30] = tmp[0]; header[31] = tmp[1]; // 30-31
                tmp = SwapUShortToByte(LocDataLen);
                header[32] = tmp[0]; header[33] = tmp[1]; // 32-33
                tmp = SwapUShortToByte(Mc5Len);
                header[34] = tmp[0]; header[35] = tmp[1]; // 34-35
                return header;
            }

            public byte[] getSDBHeader()
            {
                byte[] header = new byte[36];

                header[0] = 0x70; //p 0
                header[1] = 0x70; //p 1
                header[2] = 0x03; //01 == blocks, 03 == systemdatablock
                header[3] = Attribute; // Flags? 3
                header[4] = PGbyte0; //4
                header[5] = PGbyte2; // 5
                byte[] tmp = SwapUShortToByte(bBlockNumber);
                header[6] = tmp[0]; header[7] = tmp[1];// 6-7
                tmp = SwapUIntToByte(LoadMemLen);
                header[8] = tmp[0]; header[9] = tmp[1]; // TotLen - 8-9-10-11
                header[10] = tmp[2]; header[11] = tmp[3]; // TotLen - 8-9-10-11
                header[12] = 0x80; header[13] = 0x00; header[14] = 0x00;// //??? 12-13-14
                header[15] = Password; //BlkSec 15
                header[16] = CodeTime_ms[0]; header[17] = CodeTime_ms[1]; // 16-17-18-19
                header[18] = CodeTime_ms[2]; header[19] = CodeTime_ms[3]; // 16-17-18-19
                header[20] = CodeTime_dy[0]; header[21] = CodeTime_dy[1]; // 20-21
                header[22] = IntfTime_ms[0]; header[23] = IntfTime_ms[1]; // 22-23-24-25
                header[24] = IntfTime_ms[2]; header[25] = IntfTime_ms[3]; // 22-23-24-25
                header[26] = IntfTime_dy[0]; header[27] = IntfTime_dy[1]; // 26-27
                tmp = SwapUShortToByte(SsbLen);
                header[28] = tmp[0]; header[29] = tmp[1]; // 28-29
                tmp = SwapUShortToByte(AddLen);
                header[30] = tmp[0]; header[31] = tmp[1]; // 30-31
                tmp = SwapUShortToByte(LocDataLen);
                header[32] = tmp[0]; header[33] = tmp[1]; // 32-33
                tmp = SwapUShortToByte(Mc5Len);
                header[34] = tmp[0]; header[35] = tmp[1]; // 34-35
                return header;
            }

            public byte[] getMC7Footer()
            {
                int extraLen = SsbLen + AddLen;
                byte[] footer = new byte[extraLen + 36];
                Buffer.BlockCopy(bSSBPART, 0, footer, 0, SsbLen);
                Buffer.BlockCopy(bADDINFO, 0, footer, SsbLen, AddLen);
                Buffer.BlockCopy(PadByteArray(bAuthor, 8), 0, footer, extraLen, 8);
                Buffer.BlockCopy(PadByteArray(bFamily, 8), 0, footer, extraLen + 8, 8);
                Buffer.BlockCopy(PadByteArray(bName, 8), 0, footer, extraLen + 16, 8);
                footer[extraLen + 24] = Version; // B1.
                // B2, 0x00. No need to assign this to slot [extraLen+25]
                byte[] tmp = BitConverter.GetBytes(Checksum);
                footer[extraLen + 26] = tmp[0]; footer[extraLen + 27] = tmp[1];
                //Last 8 bytes is 0x00.
                return footer;
            }

            private byte[] getSdbFull()
            {
                byte[] bytesHeader = getSDBHeader();
                byte[] sdb = new byte[bytesHeader.Length + PGcode.Length + PGfooter.Length];
                Buffer.BlockCopy(bytesHeader, 0, sdb, 0, bytesHeader.Length);
                Buffer.BlockCopy(PGcode, 0, sdb, bytesHeader.Length, PGcode.Length);
                Buffer.BlockCopy(PGfooter, 0, sdb, bytesHeader.Length + PGcode.Length, PGfooter.Length);
                return sdb;
            }

            private void parsePGfile(string path)
            {
                // The SDB data itself is stored in separate.PG files.
                string file = int.Parse(DatabaseId).ToString("x").PadLeft(8, '0');
                file = System.IO.Directory.GetParent(
                    System.IO.Directory.GetParent(path).ToString())
                    + "\\" + file + ".PG";
                byte[] PG = System.IO.File.ReadAllBytes(file);

                Mc5Len = (ushort)((PG[13] << 8) & 0xFF00 | (PG[12] & 0x00FF));
                AddLen = (ushort)((PG[21] << 8) & 0xFF00 | (PG[20] & 0x00FF));

                PGbyte0 = PG[0];
                PGbyte2 = PG[2];

                int headerPGSize = 42; //Header contains some info.
                                       //Second header comes after Header. Will be used as footer later.
                int secondHeaderSize = 77 - headerPGSize + 1;
                int secondHeaderStart = headerPGSize;
                int codeStart = headerPGSize + secondHeaderSize;
                int codeSize = (int)BlockTotLen - secondHeaderSize - headerPGSize;
                PGfooter = PG.Skip(secondHeaderStart).Take(secondHeaderSize).ToArray();
                PGcode = PG.Skip(codeStart).Take(codeSize).ToArray();
            }


        }
    }
}
