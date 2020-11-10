using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using NDbfReaderEx; //Can read Memo fiels (MC7 etc is in memo), but not write.

namespace Stp7Toolbox
{
    public partial class PlcParser
    {

        private static Encoding encoding = Encoding.GetEncoding(1252);//1252);  //1252 Western European (Windows) - This encoding works with Siemens DBF/DBT files.
        private string basePath = "";
        private string s7pPath = "";
        private string blocksPath = "";
        private string blocksListPath = "";
        private string sdbPath = "";

        public string getS7pPath()
        {
            return s7pPath;
        }
        public PlcParser()
        {            
        }

        public PlcParser(string path)
        {
            setPath(path);
        }

        public PlcParser(string path, Encoding Encoding)
        {
            encoding = Encoding;
            setPath(path);
        }

        public void setPath(string path)
        {
            setBasePath(path);
            setBlocksPath();
            setSdbPath();
        }

        private void setBasePath(string path)
        {
            string folder = Path.GetDirectoryName(path);
            string[] files = Directory.GetFiles(folder, "*.s7p", SearchOption.AllDirectories);

            if (files.Count() > 1) {
                throw new System.Exception("Multiple S7 projects found. Check path.");
            }
            if (files.Count() == 0 )
            {
                throw new System.Exception("No S7 projects found. Check path.");
            }
            s7pPath = files[0];
            basePath = Directory.GetParent(files[0]).FullName;
        }

        private void setBlocksPath(string option = "")
        {
            List<Item> BlockFolders = new List<Item>();
            string blockFoldersList = basePath + @"\ombstx\offline\BSTCNTOF.DBF";
            BlockFolders = getBlocks(blockFoldersList, DatabaseType.NormalBlockFolders);

            //Decide which block-folder to use. Each folder is a "program" in S7.
            string id = "";
            int maxNumOfBlock = 0;
            foreach(Item folder in BlockFolders)
            {
                //Number of blocks is not good enough measure where
                //multiple similar programs are used.
                //TODO: Find out what MLFB this prog belongs to.
                if (folder.TotNumberOfBlocks > maxNumOfBlock)
                {
                    maxNumOfBlock = folder.TotNumberOfBlocks;
                    id = folder.DatabaseId;
                }
            }
            id = int.Parse(id).ToString("x").ToLower(); // Folder name are ID in Hex.

            string folderPath = basePath + @"\ombstx\offline\";
            blocksPath = folderPath + id.PadLeft(8, '0') + @"\SUBBLK.DBF";
            blocksListPath = blocksPath.Replace("SUBBLK", "BAUSTEIN");
        }

        private void setSdbPath(string option = "")
        {
            List<Item> SdbFolders = new List<Item>();
            string blocksList = basePath + @"\sdb\S0112001.DBF";
            SdbFolders = getBlocks(blocksList, DatabaseType.SystemDataFolders);
            //Console.WriteLine(BlockFolders[0].ToString());
            //Decide which SDB-folder to use. Each folder is a "program" in S7.
            string id = "";

            TESTprintTimestampFromMLFB(SdbFolders);
            // hOmSave7\s7hstatx\Hobject1.Dbf : Name=Station-name, ID is the file name: "ID.s7H" holding more info..
            // Get station-name as option-input.  Find id in record matchin station name (Hobjec1.dbf).
            // Open Id.s7H file, given MLFB number should be found in this file.

            //Use first... TODO: Find out what MLFB this prog belongs to.
            id = SdbFolders[0].DatabaseId;
            id = int.Parse(id).ToString("x").ToLower(); // Folder name are ID in Hex.

            string folderPath = basePath + @"\sdb\";
            string fileName = id.PadLeft(8, '0');
            sdbPath = folderPath + fileName + "\\" + "list" + "\\" + fileName + ".DBF";
        }

        private void TESTprintTimestampFromMLFB(List<Item> BlockFolders)
        {
            foreach(Item folder in BlockFolders)
            {
                for (int i = 0; i < folder.bSystemDataMLFB.Length - 6; i++)
                {
                    DateTime time = timeFroms7time(folder.bSystemDataMLFB.Skip(i).Take(6).ToArray());
                    Console.WriteLine("Time from SDB folder list MLFB: ID=" +
                        folder.DatabaseId + " i=" + i + ": " + time.ToString("o"));
                }
            }
        }

        public void writeBlocksToFiles(string outputDir)
        {
            List<Item> Blocks = getAllBlocks();
            writeBlocksToFiles(Blocks, outputDir);
        }

        public void writeBlocksToZipfile(string outputDir)
        {
            List<Item> Blocks = getAllBlocks();
            writeBlocksToZipfile(Blocks, outputDir);
        }


        public void writeItemsInfoToFile(List<Item> Blocks, string path)
        {
            Directory.CreateDirectory(Directory.GetParent(path).FullName);
            List<string> lines = new List<string>();
            foreach (Item block in Blocks)
            {                
                lines.Add(block.ToString() + Environment.NewLine);
            }
            if(lines.Count > 0)
                System.IO.File.WriteAllLines(path, lines.ToArray(), encoding);
        }

        public void writeBlocksToFiles(List<Item> Blocks, string outputDir)
        {
            Directory.CreateDirectory(outputDir);
            foreach (Item block in Blocks)
            {
                if (!(block.BlockType.Contains("FC") ||
                    block.BlockType.Contains("FB") ||
                    block.BlockType.Contains("OB") ||
                    block.BlockType.Contains("DB") ||
                    block.BlockType.Contains("SDB")))
                {
                    continue;
                }

                byte[] bytes = block.getFullCode();

                string filepath = Path.Combine(outputDir,
                    block.BlockType + "_" + block.bBlockNumber + ".s7.");

                filepath += block.BlockType == "DB" || block.BlockType == "SDB" ?
                    "dat" : "bin";

                System.IO.File.WriteAllBytes(filepath, bytes);                               
            }
        }

        public void writeBlocksToZipfile(List<Item> Blocks, string outputDir)
        {
            Directory.CreateDirectory(outputDir);
            // Create a zip of all the blocks. 
            // https://stackoverflow.com/questions/17217077/create-zip-file-from-byte
            using (var zipStream = new MemoryStream())
            {
                //Create an archive and store the stream in memory.
                using (var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Create, false))
                {
                    foreach (Item block in Blocks)
                    {
                        if (!(block.BlockType.Contains("FC") ||
                        block.BlockType.Contains("FB") ||
                        block.BlockType.Contains("OB") ||
                        block.BlockType.Contains("DB") ||
                        block.BlockType.Contains("SDB")))
                        {
                            continue;
                        }

                        byte[] bytes = block.getFullCode();

                        string filename = block.BlockType + "_" +
                            block.bBlockNumber + ".s7.";

                        filename += block.BlockType == "DB" || block.BlockType == "SDB" ?
                            "dat" : "bin";

                        //Create a zip entry for each attachment
                        var zipEntry = zipArchive.CreateEntry(filename, CompressionLevel.Fastest);

                        //Get the stream of the attachment
                        using (var originalFileStream = new MemoryStream(bytes))
                        using (var zipEntryStream = zipEntry.Open())
                        {
                            //Copy the attachment stream to the zip entry stream
                            originalFileStream.CopyTo(zipEntryStream);
                        }
                    }                    
                }
                System.IO.File.WriteAllBytes(Path.Combine(outputDir, "LoadMemory.zip"), zipStream.ToArray());
            }
        }


        /// For SUBBLK.dbf// alle blokker
        ///     For blocknumber X:
        ///       if subbloktype = 10
        ///           Timestamp1 er nytt
        ///           Attribute : set bit number 5av7
        ///       if subbloktype = 20
        ///           Timestamp1 og 2  er nytt
        /// For Baustein.dbf //liste over alle blokker
        ///     For blocknumber X:
        ///       if typ = 10        ///           
        ///           Attribs: ? Timestamp1 er nytt // Unknown how this is built up, so skipping this step.
        /// For BSTCNTOF.dbf // Block folder list // modifiser timestamp. 

        /// <summary>
        /// Clear the NonRetain attribute of all DataBlocks.
        /// </summary>
        /// <returns>A collection of the modified DataBlocks</returns>
        public DbItemsCollection clearNonRetainAttr()
        {            
            DateTime now = DateTime.Now;            
            string newTime = encoding.GetString(s7timeFromDateTime(now));            

            DbItemsCollection dbItemsCollection = getRelevantBlocks(NonRetain: false);

            updateBlockItemsAttr(dbItemsCollection, NonRetain: false, newTime: newTime);
            updateBlocksListItemsAttr(dbItemsCollection, NonRetain: false, newTime: newTime);

            return dbItemsCollection;
        }

        /// <summary>
        /// Undo clearing the NonRetain attribute.
        /// Must be called after clearNonRetainAttr(), with
        /// its result as an input.
        /// </summary>
        /// <param name="dbItemsCollection"></param>
        public void undoClearingNonRetainAttr(DbItemsCollection dbItemsCollection)
        {         
            updateBlockItemsAttr(dbItemsCollection, NonRetain: true);
            updateBlocksListItemsAttr(dbItemsCollection, NonRetain: true);
        }

        public List<string> getMLFBsfromProject(string path)
        {
            List<string> mlfbs = new List<string>();

            DotNetSiemensPLCToolBoxLibrary.Projectfiles.Step7ProjectV5 pr = 
                new DotNetSiemensPLCToolBoxLibrary.Projectfiles.
                Step7ProjectV5(path, false, encoding);

            foreach (var cpufolder in pr.CPUFolders)
            {
                var mlfb = cpufolder.MLFB_OrderNumber;
                mlfbs.Add(mlfb);
            }
            return mlfbs;
        }

        public List<Item> getAllBlocks()
        {
            List<Item> Blocks = getBlocks(blocksPath, DatabaseType.NormalBlocks);
            List<Item> SdbBlocks = getBlocks(sdbPath, DatabaseType.SystemDataBlock);

            Blocks.AddRange(SdbBlocks);
            return Blocks;
        }


        private List<Item> getBlocks(string path, DatabaseType GeneralType)
        {
            //NDbfReaderEx ---------------------------------      
            List<Item> Blocks = new List<Item>();
            using (DbfTable table = DbfTable.Open(path, encoding))
            {
                foreach (DbfRow row in table)
                {
                    try
                    {
                        //row.CacheMemos();
                        Item block = new Item(row, GeneralType, path);
                        Blocks.Add(block);
                    }
                    catch (Exception ex) 
                    { 
                        Console.WriteLine(ex.ToString()); 
                    }
                }
            }
            sortItemList(Blocks, GeneralType);
            return Blocks;
        }

        private void sortItemList(List<Item> Blocks, DatabaseType GeneralType)
        {
            switch (GeneralType)
            {
                case (PlcParser.DatabaseType.SystemDataBlock):
                    Blocks.Sort((a, b) => 
                        a.bBlockNumber.CompareTo(b.bBlockNumber));
                    break;
                case (PlcParser.DatabaseType.NormalBlocks):
                    Blocks.Sort((a, b) =>
                        a.bBlockNumber.CompareTo(b.bBlockNumber));
                    break;
                case (PlcParser.DatabaseType.NormalBlockFolders):
                    Blocks.Sort((a, b) =>
                        a.DatabaseId.CompareTo(b.DatabaseId));
                    break;
                case (PlcParser.DatabaseType.SystemDataFolders):
                    Blocks.Sort((a, b) =>
                        a.DatabaseId.CompareTo(b.DatabaseId));
                    break;
            }
        }           
    }
}