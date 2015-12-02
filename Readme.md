# Factom C# API Implementation
##Now on NuGet!


What It can do:

 * Create new Chains on the Factom Blockchain
 * Add Entries to existing Chains
 * Retrieve data from the Factom Blockchain

### **How to use:**
I reccommend reading up on how Factom works before using this API, but it is not required.
There are 2 classes that are currently built; Entry and Chain.

### Retrieving Data From Factom
If you have the chain ID of a chain, you can get all the entries like so.
```cs
string chainIDString = "CHAIN-ID-HERE";
// First Convert the chain id string into a byte[]. The API has a build in converter.
byte[] chainIDBytes = Strings.DecodeHexIntoBytes(chainIdString);
// Now we can get all the entries in the chain, a List of EntryData will be returned.
var entries = Chain.GetAllChainEntries(chainIDBytes);
//Each index in entries contains the Entry hash and timestamp. To get it's content, use:
index = 0; // Index of entry you want to get
byte[] content = Entry.GetEntryData(entries[index].Entryhash);
````

### Uploading Data To Factom
#### Find the Cost First
In the next two sections the creation of an entry is required. To get the cost of the entry, simply pass the created entry like so:
```cs
int cost = Entry.EntryCost(entry);
```
For commiting a chain add '10' to the cost.
```cs
cost += 10;
```
#### To Commit a Chain
First we need to make a new Entry, as a first entry is required to make a chain.
```cs
/// The call to make a new entry is Entry.NewEntry(byte[] content, byte[][] extIds, byte[] chainId)
/// content: The data you wish the entry to store
/// extIds: Used to generate a UNIQUE chainID. These will take up space in the Entry, and make these unique
///         for each new chain you create.
/// chainId: Leave null for the first entry, as it is computed by the extIds

byte[] content = Encoding.UTF8.GetBytes("This is my entry content");
// One idea is to use a hash of your content, or use a random number. You can use any number of extIds,
// here I use 2.
byte[][] extIDs = new[] {
            Encoding.UTF8.GetBytes("random-string-of-things"),
            Encoding.UTF8.GetBytes("this-will-hopefully-be-unique")
            }
var firstEntry = Entry.NewEntry(content, extIds, null);
// Now we can make our chain to commit.
var chain = Chain.NewChain(firstEntry);

// Factom is not free, you need a entry credit wallet. Put the name of your wallet here:
string walletName = "What you named your wallet";
byte[] chainID = Chain.CommitChain(chain, walletName);

Thread.Sleep(10000); // Must wait 10 seconds

Chain.RevealChain(chain); // Must reveal the entry
// If all went well your entry is now on Factom!
// DO NOT LOSE 'chainID'. This is how you find your chain after it is made, so make it a form of output
// and hang onto it

```
Some notes:
- Each entry can be  a max of 10kb in size. (See "Entry" section of [here](https://github.com/FactomProject/FactomDocs/blob/master/factomDataStructureDetails.md) to predict the size of your entry)
- The chain commit cost 10 entry credits + 1 entry credit per kb of data. Max chain initial commit = 20 entry credits
- There is no "are you sure?" once you commit, the entry credits are spent, so be careful when testing.

#### To Commit an Entry
So now you want to add more to an existing chain? Easy!
```cs
// We have to make another Entry like before, but now we do not need the extIds.
byte[] content = Encoding.UTF8.GetBytes("Some more content to add!");
// Convert the chainID string into byte[] if it's not already. I use a function I made, you can too.
// You can also use the chainID returned from the commit earlier.
byte[] chainId = Strings.DecodeHexIntoBytes("HEX-STRING-HERE"); //Should be in hex, case does not matter
var entry = Entry.NewEntry(content, null, chainId);

// Factom is not free, you need a entry credit wallet. Put the name of your wallet here:
string walletName = "What you named your wallet";
Entry.CommitEntry(entry, factomWalletName);

Thread.Sleep(10000); // Must wait 10 seconds

Entry.RevealEntry(factomEntry);
// Your entry is now on the chain if there are no exceptions.
```
Some notes:
- You can not "reserve a chain" as in anyone can add entries to your chains
- This means you need to verify your entries
- Entries cost 1 entry credit per kb

---

### Factom Resources
The Factom API rest calls can be found here: [Factom Rest](https://github.com/FactomProject/FactomDocs/blob/master/FactoidAPI.md) 

The GoLang full implementation can be found here: [Factom GoLang](https://github.com/FactomProject/factom/)

Blog post using GoLang library: [Blog Post](http://www.factom.com/monitoring-the-poloniex-exchange-with-factom/)

General Factom Docs: [Factom Docs](https://github.com/FactomProject/FactomDocs)
