using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using FactomAPI.Exceptions;
using Newtonsoft.Json;
using RestSharp;
using FactomAPI;

namespace FactomAPI {
    public static class Chain {
        /// <summary>
        /// Creates a new Chain
        /// </summary>
        /// <param name="entry">First entry in chain</param>
        /// <returns></returns>
        public static ChainType NewChain(DataStructs.EntryData entry) {
            var c = new ChainType();
            c.FirstEntry = entry;
            var chainHash = new List<byte>();
            if (entry.ExtIDs != null) {
                foreach (var extId in entry.ExtIDs) {
                    var h = SHA256.Create().ComputeHash(extId);
                    chainHash.AddRange(h);
                }
            }
            c.ChainId = SHA256.Create().ComputeHash(chainHash.ToArray());
            c.FirstEntry.ChainId = c.ChainId;
            return c;
        }

        /// <summary>
        /// Takes in an entry chain hash and returns Key MR of the first entry. Can be used to 
        /// get all the entries
        /// </summary>
        /// <param name="hash">ChainID of chain</param>
        /// <returns>KeyMR of first entry (last in list)</returns>
        public static DataStructs.ChainHeadData GetChainHead(byte[] hash) {
            var hashString = Arrays.ByteArrayToHex(hash);
            var req = new RestRequest("/chain-head/{hash}", Method.GET);
            // var x = Arrays.ByteArrayToHex(hash);
            req.AddUrlSegment("hash", hashString);
            var resp = StaticValues.clientWallet.Execute(req);
            if (resp.Content.Contains("Chain not found")) throw new FactomChainException("Chain not found");
            try {
                var chainHead = JsonConvert.DeserializeObject<DataStructs.ChainHeadDataStringFormat>(resp.Content);
                return DataStructs.ConvertStringFormatToByteFormat(chainHead);
            } catch (Exception) {
                throw new FactomEntryException("Error when serializing the chainhead. In GetChainHead: " + resp.Content);
            }
        }

        /// <summary>
        /// Returns all the entries in a Chain. Type of entry has timestamp and entryhash value
        /// </summary>
        /// <param name="chainHeadID">ChainID of chain</param>
        /// <returns>List of all entrtries</returns>
        public static List<DataStructs.EntryBlockData.EntryData> GetAllChainEntries(byte[] chainHeadID) {
            var chainHead = GetChainHead(chainHeadID);
            return GetAllChainEntries(chainHead);
        }

        /// <summary>
        /// Returns all the entries in a Chain. Type of entry has timestamp and entryhash value
        /// </summary>
        /// <param name="chainHead">ChainHeadData type</param>
        /// <returns>List of all chain entries</returns>
        public static List<DataStructs.EntryBlockData.EntryData> GetAllChainEntries(DataStructs.ChainHeadData chainHead) {
            var block = Entry.GetEntryBlockByKeyMR(chainHead);
            var blockPointer = block;
            var dataList = new List<DataStructs.EntryBlockData.EntryData>();

            while (!Bytes.Equality(blockPointer.Header.PrevKeyMr, StaticValues.ZeroHash)) {
                dataList.AddRange(blockPointer.EntryList); // Add all entries in current MR
                blockPointer = Entry.GetEntryBlockByKeyMR(blockPointer.Header.PrevKeyMr);
            }
            dataList.AddRange(blockPointer.EntryList);
            return dataList;
        }

        /// <summary>
        /// First method to add a chain to factom. Spends the entry credits, must wait 10seconds and call
        /// RevealChain() to finalize the commit.
        /// </summary>
        /// <param name="c">Chain to be added</param>
        /// <param name="name">Name of Entry Credit wallet</param>
        /// <returns>ChainID of chain added, do not lose this!</returns>
        public static byte[] CommitChain(ChainType c, string name) {
            var byteList = new List<byte>();

            //1 byte version
            byteList.Add(0);

            // 6 byte milliTimestamp (truncated unix time)
            byteList.AddRange(Times.MilliTime());

            var entry = c.FirstEntry;

            // 32 Byte ChainID Hash
            //byte[] chainIDHash = Encoding.ASCII.GetBytes(c.ChainId);
            var chainIDHash = c.ChainId;
            chainIDHash = SHA256.Create().ComputeHash(chainIDHash);
            chainIDHash = SHA256.Create().ComputeHash(chainIDHash);
            byteList.AddRange(chainIDHash);

            // 32 byte Weld; sha256(sha256(EntryHash + ChainID))
            var cid = c.ChainId;
            var s = Entries.HashEntry(c.FirstEntry);
            var weld = new byte[cid.Length + s.Length];
            s.CopyTo(weld, 0);
            cid.CopyTo(weld, s.Length);
            weld = SHA256.Create().ComputeHash(weld);
            weld = SHA256.Create().ComputeHash(weld);
            byteList.AddRange(weld);

            // 32 byte Entry Hash of the First Entry
            byteList.AddRange(Entries.HashEntry(c.FirstEntry));

            // 1 byte number of Entry Credits to pay
            var cost = (sbyte) (Entries.EntryCost(entry) + 10); // TODO: check errors
            byteList.Add(BitConverter.GetBytes(cost)[0]);

            var com = new WalletCommit();
            com.Message = Arrays.ByteArrayToHex(byteList.ToArray());

            var json = JsonConvert.SerializeObject(com);

            var req = new RestRequest("/commit-chain/" + name, Method.POST);
            req.RequestFormat = DataFormat.Json;
            req.AddParameter("application/json", json, ParameterType.RequestBody);
            req.AddUrlSegment("name", name);
            var resp = StaticValues.clientD.Execute(req);

            Console.WriteLine("CommitChain Resp = " + resp.StatusCode); // TODO: Remove
            Console.WriteLine("Message= " + com.Message); // TODO: Remove

            if (resp.StatusCode != HttpStatusCode.OK) {
                throw new FactomChainException("Chain Commit Failed. Message: " + resp.ErrorMessage);
            }
            return Entries.ChainIdOfFirstEntry(c.FirstEntry);
        }

        /// <summary>
        /// Second step in committing a new chain. Only run this if CommitChain was successful.
        /// </summary>
        /// <param name="c">Chain to be added</param>
        /// <returns>Boolean true/false for success/failure</returns>
        public static bool RevealChain(ChainType c) {
            var r = new Reveal();
            var b = Entries.MarshalBinary(c.FirstEntry);
            r.Entry = Arrays.ByteArrayToHex(b);

            var json = JsonConvert.SerializeObject(r);
            var byteJson = Encoding.ASCII.GetBytes(json);

            var req = new RestRequest("/reveal-chain/", Method.POST);
            req.RequestFormat = DataFormat.Json;
            req.AddParameter("application/json", json, ParameterType.RequestBody);
            var resp = StaticValues.clientWallet.Execute(req);
            Console.WriteLine("RevealChain Resp = " + resp.StatusCode); //TODO: Remove

            if (resp.StatusCode != HttpStatusCode.OK) {
                throw new FactomChainException("Chain Reveal Failed. Message: " + resp.ErrorMessage);
            }
            return true;
        }
 
        public class ChainType {
            public byte[] ChainId { get; set; }
            public DataStructs.EntryData FirstEntry { get; set; }
        }

        /// <summary>
        /// Used to send json object as POST data
        /// </summary>
        private class WalletCommit {
            public string Message { get; set; }
        }

        /// <summary>
        /// Used to serialize json as POST data
        /// </summary>
        private class Reveal {
            public string Entry { get; set; }
        }
    }
}