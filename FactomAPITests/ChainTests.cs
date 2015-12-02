using Microsoft.VisualStudio.TestTools.UnitTesting;
using FactomAPI;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FactomAPITests;

namespace FactomAPI.Tests {
    [TestClass()]
    public class ChainTests {
        [TestMethod()]
        public void NewChainTest() {
            //Assert.Fail();
        }

        [TestMethod()]
        public void GetChainHeadTest() {
            var val = Strings.DecodeHexIntoBytes("475fbcef5e3a4e1621ed9a6fda5840c1d654715e55a8f5e514af0fb879ce0aec");
            var chainHead = Chain.GetChainHead(val);
            Assert.IsFalse(Bytes.Equality(chainHead.ChainHead, Encoding.UTF8.GetBytes("4c195ddcba466d2e15ce29d150c985bd7aaadaf881fa9a0abca0ee6ab07159f7")));
        }

        [TestMethod()]
        public void GetAllChainEntriesTest() {
            //Assert.Fail();
        }

        [TestMethod()]
        public void GetAllChainEntriesTest1() {
            //Assert.Fail();
        }

        [TestMethod()]
        public void CommitChainTest() {
            //Assert.Fail();
        }

        [TestMethod()]
        public void RevealChainTest() {
            //Assert.Fail();
        }
    }
}