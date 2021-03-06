using Microsoft.VisualStudio.TestTools.UnitTesting;
using SCC_Detection.Datastructures;
using SCC_Detection.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace SCC_Detection_Test
{
    [TestClass]
    public class GraphParserTest
    {
        [TestMethod]
        public void ReadSnapTest()
        {
            Graph g = GraphParser.ReadFileSNAP(@"D:\Documents\computing_science\scriptie\graphs\SNAP_test.txt", 1);

            Assert.AreEqual(6, g.Vertices().Count);
            string s = g.ToString();
            Assert.AreEqual("0 -->  1 2\n1 -->  2\n2 --> \n3 -->  4 5\n4 -->  5\n5 -->  0\n", s);
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void InvalidSnapTest()
        {
            Graph g = GraphParser.ReadFileSNAP(@"D:\Documents\computing_science\scriptie\graphs\invalid_SNAP_test.txt", 1);
        }

        [TestMethod]
        public void ReadListTest()
        {
            Graph g = GraphParser.ReadFile(@"D:\Documents\computing_science\scriptie\graphs\list_test.txt", 1);

            Assert.AreEqual(6, g.Vertices().Count);
            string s = g.ToString();
            Assert.AreEqual("0 -->  1 2\n1 -->  2\n2 --> \n3 -->  4 5\n4 -->  5\n5 -->  0\n", s);
        }
    }
}
