using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FactomAPI;

namespace FactomAPITests {
    static class TestVariables {
        public static DataStructs.EntryData Entry {get;} = new DataStructs.EntryData {
            Content =
                Encoding.UTF8.GetBytes("Each directory listed in the Go path must have a prescribed structure:"),
            ChainId = Encoding.UTF8.GetBytes("00511c298668bc5032a64b76f8ede6f119add1a64482c8602966152c0b936c77"),
            ExtIDs = new[] {
            Encoding.UTF8.GetBytes("a136bf2a5b81a671d3f0c168f4"),
            Encoding.UTF8.GetBytes("b35f223db2dced312581d22c46ba4117702d03")
            }
        };
    }
}
