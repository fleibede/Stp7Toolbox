using System.Text;
using DotNetSiemensPLCToolBoxLibrary;


namespace Stp7Toolbox
{
    public partial class PlcParser
    {


        /// <summary>
        /// Returns the symbol table formated as SDF.
        /// </summary>
        /// <returns></returns>
        public string GetSymbolTable()
        {
            string path = s7pPath;

            DotNetSiemensPLCToolBoxLibrary.Projectfiles.Step7ProjectV5 pr = new DotNetSiemensPLCToolBoxLibrary.Projectfiles.Step7ProjectV5(path, false, Encoding.GetEncoding(1252));
            pr.ProjectLanguage = DotNetSiemensPLCToolBoxLibrary.DataTypes.MnemonicLanguage.English;

            string symboltableSdf = "";
            
            foreach (var blockfolder in pr.S7ProgrammFolders)
            {
                // Skip program folders without offline blocks in it.
                if (blockfolder.BlocksOfflineFolder == null)
                    continue;

                var blocksList = blockfolder.BlocksOfflineFolder.BlockInfos;
                // Skip programs with less than 4 (arbitrary number) blocks.
                if (blocksList.Count < 4)
                    continue;

                var table = (DotNetSiemensPLCToolBoxLibrary.DataTypes.Projectfolders.Step7V5.SymbolTable)blockfolder.SymbolTable;
                symboltableSdf = table.GetSymbolTableAsSdf();
            }

            return symboltableSdf;
        }        
    }
}