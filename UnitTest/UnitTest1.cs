using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Stp7Toolbox;

namespace UnitTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestS7Time()
        {           
            DateTime now = DateTime.Now;
            now = now.AddTicks(-(now.Ticks % 10000)); // Remove reslution lower than milliseconds
            DateTime time = PlcParser.timeFroms7time(PlcParser.s7timeFromDateTime(now));
            Assert.AreEqual(now, time);

            DateTime exampleDate = new DateTime(2018, 3, 20, 9, 43, 10, 848);
            byte[] exampleDateHex = { 0x02, 0x15, 0xEB, 0x00, 0x30, 0xD1 };

            PlcParser.parseS7Time(exampleDateHex, 
                out DateTime outDate, 
                out byte[] outByteMs, 
                out byte[] outByteDays);

            Assert.AreEqual(BitConverter.ToString(outByteMs), BitConverter.ToString(exampleDateHex,0,4));
            Assert.AreEqual(BitConverter.ToString(outByteDays), BitConverter.ToString(exampleDateHex, 4, 2));
            Assert.AreEqual(exampleDate, outDate);

            byte[] outDateHex = PlcParser.s7timeFromDateTime(exampleDate);

            Assert.AreEqual(BitConverter.ToString(exampleDateHex), BitConverter.ToString(outDateHex));
        }
    


        // This method accepts two strings the represent two files to 
        // compare. A return value of 0 indicates that the contents of the files
        // are the same. A return value of any other value indicates that the 
        // files are not the same.
        private bool FileCompare(string file1, string file2)
        {
            int file1byte;
            int file2byte;
            FileStream fs1;
            FileStream fs2;

            // Determine if the same file was referenced two times.
            if (file1 == file2)
            {
                // Return true to indicate that the files are the same.
                return true;
            }

            // Open the two files.
            fs1 = new FileStream(file1, FileMode.Open, FileAccess.Read);
            fs2 = new FileStream(file2, FileMode.Open, FileAccess.Read);

            // Check the file sizes. If they are not the same, the files 
            // are not the same.
            if (fs1.Length != fs2.Length)
            {
                // Close the file
                fs1.Close();
                fs2.Close();

                // Return false to indicate files are different
                return false;
            }

            // Read and compare a byte from each file until either a
            // non-matching set of bytes is found or until the end of
            // file1 is reached.
            do
            {
                // Read one byte from each file.
                file1byte = fs1.ReadByte();
                file2byte = fs2.ReadByte();
            }
            while ((file1byte == file2byte) && (file1byte != -1));

            // Close the files.
            fs1.Close();
            fs2.Close();

            // Return the success of the comparison. "file1byte" is 
            // equal to "file2byte" at this point only if the files are 
            // the same.
            return ((file1byte - file2byte) == 0);
        }
    }
}
