# Stp7Toolbox

## This project implements the following functions:

* Create MC7 (binary) blocks from a Step7 project (OB, FC, FB, DB, SDB).
These binary blocks are identical to the blocks you can upload online from a PLC running the given program. 
Using e.g. Snap7 you can download these blocks to the PLC without using Simatic Manager.
Tests have shown that the generated MC7 files are binary identical to files uploaded from the PLC.
 
   Limitations: 
  > * If the project containts multiple Hardware Configs, the Stp7Toolbox does not yet have a deterministic way of knowing which SDBs (System data blocks) belong to the correct S7 program. (see https://github.com/dotnetprojects/DotNetSiemensPLCToolBoxLibrary/issues/151) 
  > * Downloading SDBs to a PLC has been reported must happen in a specific order. That is not addressed here.
  > * If the project contains multiple programs, it will choose the program with the highest number of blocks.

* Create an AWL file for a whole project, symbolically, and in correct dependency order.
  
   Limitations: 
  > DotNetSiemensPLCToolBoxLibrary  is mainly used for this task, and this process still produces some source code that does not compile in Simatic Manager, depending on the complexity of the program, and functions/code used.

* Clear/Set the "Non retain" attribute on all Step7 DBs in a project that does not have the wanted attribute state.
  > NB: The timestamp of the changed blocks are also updated.

## Roadmap
This project was started before I found out about DotNetSiemensPLCToolBoxLibrary, that would have saved me a ton of time. 
In time I hope to implement the MC7 binary creation into DotNetSiemensPLCToolBoxLibrary, but time flies.
In the meantime I post my code here in case it can be of use for anyone.

Using DotNetSiemensPLCToolBoxLibrary, it will be possible to select the program to generate MC7 from, and one of the future goals is to also figure out how the SDB folders are connected to the different programs (Block folders and Stations).

## Libraries used:

* FastDBF - Copyright (c) 2016, Social Explorer - https://github.com/SocialExplorer/FastDBF -  BSD-2-Clause License
* NBdfReaderEx - https://github.com/emelhu/NDbfReaderEx - Original code by Stanislav Fajfr ( eXavera ) - MIT License
* DotNetSiemensPLCToolBoxLibrary - https://github.com/dotnetprojects/DotNetSiemensPLCToolBoxLibrary - LGPL-2.1 License
* MedallionTopologicalSort - https://www.nuget.org/packages/MedallionTopologicalSort/ - MIT License

## License
All work is under MIT License. All libraries used are under their respective license.