using System.Collections.Generic;
using System.IO;
using DotNetSiemensPLCToolBoxLibrary;


namespace Stp7Toolbox
{
    class Program
    {

        static void Main(string[] args)
        {

            // ----- Example usage of PlcParser -----


            //string projectPath = @"D:\Prosjekter\Program\Xxxxx.s7p";
            //string outputDir   = @"D:\Prosjekter\ProgramOutput";
            string projectPath = args[0];
            string outputDir = args[1];

            PlcParser plcParser = new PlcParser(projectPath);
            List<PlcParser.Item> Blocks = plcParser.getAllBlocks();

            // Get all blocks in MC7 and zipped
            plcParser.writeBlocksToZipfile(Blocks, outputDir);

            // Get all blocks in MC7
            plcParser.writeBlocksToFiles(Blocks, Path.Combine(outputDir,"MC7"));

            // Get info on all blocks written to a file
            plcParser.writeItemsInfoToFile(Blocks, outputDir + @"\BlocksInfo.txt");

            // Get program blocks as a symbolic single awl.
            string awl = plcParser.getSourceAwl();

            // Get the symbol table formated as SDF.
            string symboltable = plcParser.GetSymbolTable();


            System.IO.File.WriteAllText(outputDir + @"\Program.awl", awl);            
            System.IO.File.WriteAllText(outputDir + @"\Program.sdf", symboltable);

            
            // ---- Uncomment only if it is safe to edit the given PLC program. ----

            //// Remove NonRetain attribute
            //PlcParser.DbItemsCollection dbItemsCollection = plcParser.clearNonRetainAttr();
            //// Unde remove NonRetain attribute 
            //plcParser.undoClearingNonRetainAttr(dbItemsCollection);

        }




    }
}
