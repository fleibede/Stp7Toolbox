
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DotNetSiemensPLCToolBoxLibrary;
using Medallion.Collections;

namespace Stp7Toolbox
{
    public partial class PlcParser
    {

        public class Srcblk
        {
            public string name { set; get; }
            public string awl { set; get; }
            public string[] deps { set; get; }
            public IEnumerable<string> sDeps { set; get; }
            public int id { set; get; }


        };

        /// <summary>
        /// Returns the whole program as an awl (string).
        /// Should return similar result as if all blocks are selected in
        /// Simatic Step7 "Generate Source" symbolically, and ordered.
        /// </summary>
        /// <returns></returns>
        public string getSourceAwl()
        {

            string path = s7pPath;

            DotNetSiemensPLCToolBoxLibrary.Projectfiles.Step7ProjectV5 pr = new DotNetSiemensPLCToolBoxLibrary.Projectfiles.Step7ProjectV5(path, false, Encoding.GetEncoding(1252));
            pr.ProjectLanguage = DotNetSiemensPLCToolBoxLibrary.DataTypes.MnemonicLanguage.English;

            Dictionary<string, Srcblk> programBlocks = new Dictionary<string, Srcblk>();
            Dictionary<string, IEnumerable<string>> allBlocksDependencies = new Dictionary<string, IEnumerable<string>>();
            List<string> listOfBlockNames = new List<string>();
            
            //Console.WriteLine("\n...Reading blocks...\n");

            foreach (var blockfolder in pr.S7ProgrammFolders)
            {
                // Skip program folders without offline blocks in it.
                if (blockfolder.BlocksOfflineFolder == null)
                    continue;
                //var name = blockfolder.Name;
                var blocksList = blockfolder.BlocksOfflineFolder.BlockInfos;
                // Skip programs with less than 4 (arbitrary number) blocks.
                if (blocksList.Count < 4)
                    continue;

                var offlineBlocks = blockfolder.BlocksOfflineFolder;

                foreach (var blockInfo in blocksList)
                {

                    var blk = blockInfo.GetBlock();

                    if (!(blk.BlockType.Equals(DotNetSiemensPLCToolBoxLibrary.DataTypes.PLCBlockType.FC) ||
                    blk.BlockType.Equals(DotNetSiemensPLCToolBoxLibrary.DataTypes.PLCBlockType.FB) ||
                    blk.BlockType.Equals(DotNetSiemensPLCToolBoxLibrary.DataTypes.PLCBlockType.OB) ||
                    blk.BlockType.Equals(DotNetSiemensPLCToolBoxLibrary.DataTypes.PLCBlockType.UDT) ||
                    blk.BlockType.Equals(DotNetSiemensPLCToolBoxLibrary.DataTypes.PLCBlockType.DB)))
                    {
                        continue;
                    }

                    var dependencies = blk.Dependencies;
                    // Remove SFC/SFB dependencies.
                    dependencies = dependencies.Where(x => !(x.Contains("SFC") || x.Contains("SFB")));

                    var re = new Regex(@"(ARRAY [[0-9]+..[0-9]+] OF )");
                    // Remove any dependencies that matches with the above regex.
                    dependencies = dependencies.Select(x => re.Replace(x, string.Empty));
                    var depsArr = dependencies.ToArray();

                    var symbolic = true;
                    if (blk.BlockType.Equals(DotNetSiemensPLCToolBoxLibrary.DataTypes.PLCBlockType.UDT))
                        symbolic = false;
                    var awl = offlineBlocks.GetSourceBlock(blockInfo, symbolic);
                    var id = offlineBlocks.ID;
                    var srcblk = new Srcblk
                    {
                        name = blk.BlockType.ToString() + blk.BlockNumber,
                        awl = awl,
                        deps = depsArr,
                        sDeps = dependencies,
                        id = id
                    };

                    programBlocks.Add(srcblk.name, srcblk);
                    allBlocksDependencies.Add(srcblk.name, dependencies);
                    listOfBlockNames.Add(srcblk.name);
                }
            }

            //Console.WriteLine("\n...Sorting...\n");
            
            // Medallion sort topologically
            // Prepare local function to be used to sort
            IEnumerable<string> GetDependencies(string s) =>
                allBlocksDependencies.TryGetValue(s, out var itemDependencies) ?
                itemDependencies : Enumerable.Empty<string>();
            // Order topologically
            var sortedBlockNames = listOfBlockNames.OrderTopologicallyBy(
                getDependencies: x => GetDependencies(x));

            StringBuilder orderedAwl = new StringBuilder();
            foreach (var blockName in sortedBlockNames)
            {
                orderedAwl.AppendLine(programBlocks[blockName].awl);
                orderedAwl.AppendLine();
            }

            return orderedAwl.ToString();

        }        
    }
}