using System;
using System.Collections.Generic;
using System.Net;
using FactomAPI.Exceptions;
using Newtonsoft.Json;
using RestSharp;

namespace FactomAPI {
    public static class Entry {
        /// <summary>
        /// Constructs a new EntryData object
        /// </summary>
        /// <param name="content">Content of Entry (message pack)</param>
        /// <param name="extIds">Unique Ids used for first entry of chain to construct a unique chain ID</param>
        /// <param name="chainId">ChainID of chain</param>
        /// <returns>EntryData object</returns>
        public static DataStructs.EntryData NewEntry(byte[] content, byte[][] extIds, byte[] chainId) {
            var entry = new DataStructs.EntryData();
            entry.Content = content;
            entry.ExtIDs = extIds;
            entry.ChainId = chainId;
            return entry;
        }

        /// <summary>
        /// Returns an EntryBlock
        /// </summary>
        /// <param name="hash">Chainhead</param>
        /// <returns>EntryBlockData</returns>
        public static DataStructs.EntryBlockData GetEntryBlockByKeyMR(DataStructs.ChainHeadData chainHead) {
            return GetEntryBlockByKeyMR(chainHead.ChainHead);
        }


        /// <summary>
        /// Returns an EntryBlock
        /// </summary>
        /// <param name="hash">String of KeyMr</param>
        /// <returns>EntryBlockData</returns>
        public static DataStructs.EntryBlockData GetEntryBlockByKeyMR(byte[] keyMR) {
            var req = new RestRequest("/entry-block-by-keymr/{hash}", Method.GET);
            var keyMRString = Arrays.ByteArrayToHex(keyMR);
            req.AddUrlSegment("hash", keyMRString);

            var resp = StaticValues.clientWallet.Execute(req);
            if (resp.Content == "EBlock not found") {
                throw new FactomEntryException("EBlock not Found, Zerohash looked up");
            }
            var entryBlock = JsonConvert.DeserializeObject<DataStructs.EntryBlockDataStringFormat>(resp.Content);

            return DataStructs.ConvertStringFormatToByteFormat(entryBlock);
        }

        /// <summary>
        ///  Returns the data of an entry.
        /// </summary>
        /// <param name="hash">Entry hash as EntryBlockData.entry</param>
        /// <returns>EntryData object</returns>
        public static DataStructs.EntryData GetEntryData(DataStructs.EntryBlockData.EntryData entry) {
            return GetEntryData(entry.EntryHash);
        }

        /// <summary>
        /// Returns the data of an entry.
        /// </summary>
        /// <param name="entryHash">Entryhash of entry</param>
        /// <returns>The entry data</returns>
        public static DataStructs.EntryData GetEntryData(byte[] entryHash) {
            var req = new RestRequest("/entry-by-hash/{hash}", Method.GET);
            req.AddUrlSegment("hash", Arrays.ByteArrayToHex(entryHash));

            var resp = StaticValues.clientWallet.Execute(req);
            var entryType = JsonConvert.DeserializeObject<DataStructs.EntryDataStringFormat>(resp.Content);
            return DataStructs.ConvertStringFormatToByteFormat(entryType);
        }

        /// <summary>
        /// Commits an entry to the Factom blockchain. Must wait 10 seconds if succeeds then call RevealEntry
        /// </summary>
        /// <param name="entry">Entry to be committed</param>
        /// <param name="name">Name of entry credit wallet</param>
        /// <returns>ChainID of commited Entry</returns>
        public static byte[] CommitEntry(DataStructs.EntryData entry, string name) {
            var byteList = new List<byte>();

            // 1 byte version
            byteList.Add(0);

            // 6 byte milliTimestamp (truncated unix time)
            byteList.AddRange(Times.MilliTime());

            // 32 byte Entry Hash
            byteList.AddRange(Entries.HashEntry(entry));

            // 1 byte number of entry credits to pay
            var cost = Entries.EntryCost(entry); // TODO: check errors
            byteList.Add(BitConverter.GetBytes(cost)[0]);

            var com = new WallerCommit();
            com.Message = Arrays.ByteArrayToHex(byteList.ToArray()); //Hex encoded string on bytelist

            var json = JsonConvert.SerializeObject(com);

            Console.WriteLine("CE Json = " + json); //TODO: Remove

            var req = new RestRequest("/commit-entry/{name}", Method.POST);
            req.RequestFormat = DataFormat.Json;
            req.AddParameter("application/json", json, ParameterType.RequestBody);
            req.AddUrlSegment("name", name);
            var resp = StaticValues.clientD.Execute(req);
            if (resp.StatusCode != HttpStatusCode.OK) {
                throw new FactomEntryException("Entry Commit Failed. Message: " + resp.ErrorMessage);
            }
            //Console.WriteLine("CommitEntry Resp = " + resp.StatusCode + "|" + resp.StatusCode);
            if (entry.ExtIDs != null) {
                return Entries.ChainIdOfFirstEntry(entry);
            }
            return entry.ChainId;
        }

        /// <summary>
        /// Second and final step in adding an entry to a chain on the factom blockchain
        /// </summary>
        /// <param name="entry">Entry to be added</param>
        /// <returns>Boolean true/false for success/failure</returns>
        public static bool RevealEntry(DataStructs.EntryData entry) {
            var rev = new Reveal();
            var marshaledEntry = Entries.MarshalBinary(entry);
            rev.Entry = Arrays.ByteArrayToHex(marshaledEntry);
            var req = new RestRequest("/reveal-entry/", Method.POST);
            var json = JsonConvert.SerializeObject(rev);
            Console.WriteLine("RE Json = " + json);

            req.RequestFormat = DataFormat.Json;
            req.AddParameter("application/json", json, ParameterType.RequestBody);
            IRestResponse resp = StaticValues.clientWallet.Execute<RestRequest>(req);
            Console.WriteLine("RevealEntry Resp = " + resp.StatusCode + "|" + resp.StatusCode);

            if (resp.StatusCode != HttpStatusCode.OK) {
                throw new FactomEntryException("Entry Reveal Failed. Message: " + resp.ErrorMessage);
            }
            return true;
        }

        /// <summary>
        ///     Used to send json object as POST data
        /// </summary>
        private class WallerCommit {
            public string Message { get; set; }
        }

        /// <summary>
        ///     Used to serialize json as POST data
        /// </summary>
        private class Reveal {
            public string Entry { get; set; }
        }
    }
}