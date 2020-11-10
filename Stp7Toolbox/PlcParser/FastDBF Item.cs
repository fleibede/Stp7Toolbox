using System;
using System.Collections.Generic;
using System.IO;
using SocialExplorer.IO.FastDBF; // Can write to regular fields, but not read Memo.

namespace Stp7Toolbox
{
    public partial class PlcParser
    {
        /// <summary>
        /// Class that holds a dictionary of the FastDBF records
        /// where attributes will change or have changed.
        /// </summary>
        public class DbItemsCollection
        {
            private Dictionary<int, Dictionary<byte, DbItem>> dbItems =
                new Dictionary<int, Dictionary<byte, DbItem>>();

            public void Add(int id, DbItem dbItem)
            {
                if (!dbItems.ContainsKey(id))
                {
                    Dictionary<byte, DbItem> blockItems = new Dictionary<byte, DbItem>();

                    blockItems.Add(dbItem.bBlockType, dbItem);
                    dbItems.Add(id, blockItems);
                }
                else
                {
                    dbItems[id].Add(dbItem.bBlockType, dbItem);
                }
            }

            public void Remove(int id)                 
            {
                dbItems.Remove(id);
            }

            public Dictionary<int, Dictionary<byte, DbItem>> getDbItems()
            {
                return dbItems;
            }
        }

        private DbItemsCollection getRelevantBlocks(bool NonRetain)
        {
            DbfFile dbf = new DbfFile(encoding);
            dbf.Open(blocksPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            DbfRecord record = new DbfRecord(dbf.Header);

            DbItemsCollection dbItemsCollection = new DbItemsCollection();

            while (dbf.ReadNext(record))
            {
                if (record.IsDeleted)
                    continue;
                try
                {
                    DbItem dbItem = new DbItem(record, DatabaseType.NormalBlocks);

                    if (dbItem.bBlockType == DbType10 ||
                        dbItem.bBlockType == DbType20) //DB
                    {
                        dbItemsCollection.Add(dbItem.DatabaseId, dbItem);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }

            dbf.Close();

            // Remove entries that only contain DbType10 or 20, or where
            // the attribute is identical.
            List<int> removeIndexes = new List<int>();
            foreach (var blockItem in dbItemsCollection.getDbItems())
            {
                if (!blockItem.Value.ContainsKey(DbType10) ||
                    !blockItem.Value.ContainsKey(DbType20) ||
                    blockItem.Value[DbType10].currentNonRetain == NonRetain)
                    removeIndexes.Add(blockItem.Key);
            }
            foreach (var key in removeIndexes)
            {
                dbItemsCollection.Remove(key);
            }
            return dbItemsCollection;
        }


        /// <summary>
        /// Update SUBBLK.DBF
        /// This file contains the all blocks, which contains 
        /// the actual NonRetain attribute and timestamp
        /// that will be shown in the block online.
        /// </summary>
        /// <param name="dbItemsCollection"></param>
        /// <param name="NonRetain"></param>
        /// <param name="newTime"></param>
        private void updateBlockItemsAttr(
           DbItemsCollection dbItemsCollection,
           bool NonRetain,
           string newTime = null)
        {
            DbfFile dbf = new DbfFile(encoding);
            dbf.Open(blocksPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);

            foreach (var blockItems in dbItemsCollection.getDbItems().Values)
            {
                if (!blockItems.ContainsKey(DbType10))
                    continue;

                // Update only if the current attribute state is different from the given one.
                if (!blockItems[DbType10].currentNonRetain == NonRetain)
                {
                    // update reader with the saved record
                    DbfRecord rec = dbf.Read(blockItems[DbType10].recIndex);
                    // Modify the record stored in memory
                    blockItems[DbType10].updateDbItemAttribute(
                        rec, NonRetain, newTime);
                    // Write the modified record to the database
                    dbf.Update(rec);

                    if (blockItems.ContainsKey(DbType20))
                    {
                        // update reader with the saved record
                        rec = dbf.Read(blockItems[DbType20].recIndex);
                        // Modify the record stored in memory
                        blockItems[DbType20].updateDbItemAttribute(
                            rec, NonRetain, newTime);
                        // Write the modified record to the database
                        dbf.Update(rec);
                    }
                }
                else
                {

                }
            }
            dbf.Close();
        }

        /// <summary>
        /// Update BAUSTEIN.DBF (only timestamp, attribs buildup not known)
        /// This is needed for the compare function to detect the difference.
        /// </summary>
        /// <param name="NonRetain"></param>
        /// <param name="newTime"></param>
        private void updateBlocksListItemsAttr(
        DbItemsCollection dbItemsCollection,
        bool NonRetain,
        string newTime = null)
        {
            DbfFile dbf = new DbfFile(encoding);
            dbf.Open(blocksListPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            DbfRecord record = new DbfRecord(dbf.Header);
            while (dbf.ReadNext(record))
            {
                if (record.IsDeleted)
                    continue;
                try
                {
                    DbItem dbItem = new DbItem(record, DatabaseType.NormalBlocksList);

                    //Only one record with each ID
                    if (dbItemsCollection.getDbItems().ContainsKey(dbItem.DatabaseId) &&
                        dbItemsCollection.getDbItems()
                        [dbItem.DatabaseId][DbType10].currentNonRetain != NonRetain)
                    {
                        //Update attribute with new timestamp1
                        dbItem.updateDbItemAttribs(record, newTime);
                        dbf.Update(record);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            dbf.Close();
        }


        public class DbItem
        {
            public DatabaseType GeneralType { set; get; }
            public long recIndex { private set; get; }
            public int DatabaseId { private set; get; }
            public int NonRetainBitPos { private set; get; } = 5;
            public byte attr { set; get; }
            public string attribs { set; get; }
            public byte psw { private set; get; }
            public ushort uBlockNumber { private set; get; }
            public byte bBlockType { private set; get; }
            private string time1 { set; get; }
            private string time2 { set; get; }

            public bool currentNonRetain
            {
                get
                {
                    return isBitSet(attr, NonRetainBitPos);
                }
            }

            public DbItem(DbfRecord record, DatabaseType GeneralType)
            {
                recIndex = record.RecordIndex;
                switch (GeneralType)
                {
                    case (PlcParser.DatabaseType.NormalBlocks):
                        parseSUBBLKDbItem(record, GeneralType);
                        break;
                    case (PlcParser.DatabaseType.NormalBlocksList):
                        parseBAUSTEINDbItem(record, GeneralType);
                        break;
                }
            }

            /// <summary>
            /// Parse the items of the sub-block databse file.
            /// This is where the actual blocks are stored.
            /// </summary>
            /// <param name="record"></param>
            /// <param name="generalType"></param>
            private void parseSUBBLKDbItem(DbfRecord record, DatabaseType generalType)
            {
                GeneralType = generalType;
                DatabaseId = int.Parse(record["OBJECTID"]);
                attr = byte.Parse(record["ATTRIBUTE"]);
                psw = byte.Parse(record["PASSWORD"]);
                uBlockNumber = ushort.Parse(record["BLKNUMBER"]);
                bBlockType = byte.Parse(record["SUBBLKTYP"]);
                time1 = record["TIMESTAMP1"];
                time2 = record["TIMESTAMP2"];
            }

            /// <summary>
            /// Parse the items of the Block list database file.
            /// </summary>
            /// <param name="record"></param>
            /// <param name="generalType"></param>
            private void parseBAUSTEINDbItem(DbfRecord record, DatabaseType generalType)
            {
                GeneralType = generalType;
                DatabaseId = int.Parse(record["ID"]);
                attribs = record["ATTRIBS"];
                uBlockNumber = ushort.Parse(record["NUMMER"]);
                bBlockType = byte.Parse(record["TYP"]);
                time1 = attribs.Substring(22, 6);
            }

            /// Uses the provided timestamp in newTime.
            public void updateDbItemAttribute(DbfRecord record, bool nonRetain, string newTime)
            {
                if (newTime == null)
                    updateDbItemAttribute(record, nonRetain, time1, time2); //Uses the timestamp from the original record.
                else
                    updateDbItemAttribute(record, nonRetain, newTime, newTime);
            }

            /// Updates the attribute and timestamp fields in the database record
            /// stored in memory.
            private void updateDbItemAttribute(DbfRecord record, bool nonRetain, string newTime1, string newTime2)
            {
                if (bBlockType == DbType10) //DB (MC7)
                {
                    attr = modifyBitInByte(attr, NonRetainBitPos, nonRetain);
                    record["ATTRIBUTE"] = attr.ToString();
                    record["TIMESTAMP1"] = newTime1;
                }
                if (bBlockType == DbType20)
                {
                    record["TIMESTAMP1"] = newTime1;
                    record["TIMESTAMP2"] = newTime2;
                }
            }

            public void updateDbItemAttribs(DbfRecord record, string newTime)
            {
                if (newTime == null)
                    newTime = time1;
                if (newTime.Length != 6)
                    throw new Exception("Updating Baustein.dbf with " +
                        "timestamp string with length != 6.");
                string tempAttribs = attribs.Remove(22, 6);
                tempAttribs = tempAttribs.Insert(22, newTime);

                record["ATTRIBS"] = tempAttribs;
            }

        }
    }
}
