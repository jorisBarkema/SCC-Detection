using Microsoft.VisualStudio.TestTools.UnitTesting;
using SCC_Detection.Datastructures;
using SCC_Detection.SCCDetectors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SCC_Detection_Test
{
    [TestClass]
    public class OBFRTest
    {
        private OBFR obfr;
        private Dictionary<int, List<int>> testMap;

        [TestInitialize]
        public void InitializeTest()
        {
            //SCCDetectorTest.detectors = new SCCDetector[] { new DCSC(1), new OBFR(1) };
            this.obfr = new OBFR(1);
            this.testMap = new Dictionary<int, List<int>>();
        }

        /// <summary>
        /// 0 --> 1 --> 2 <--> 3 <-- 4
        /// </summary>
        private void singleLoopGraph()
        {
            testMap[0] = new List<int>();
            testMap[1] = new List<int>();
            testMap[2] = new List<int>();
            testMap[3] = new List<int>();
            testMap[4] = new List<int>();

            testMap[0].Add(1);
            testMap[1].Add(2);
            testMap[2].Add(3);
            testMap[3].Add(2);
            testMap[4].Add(3);
        }

        private void singleEdgeGraph()
        {
            testMap[0] = new List<int>();
            testMap[1] = new List<int>();
            testMap[2] = new List<int>();
            testMap[3] = new List<int>();
            testMap[4] = new List<int>();

            testMap[0].Add(1);
            testMap[1].Add(2);
            testMap[2].Add(3);
            testMap[3].Add(4);
        }

        [TestMethod]
        public void trimTestEmpty()
        {
            Graph g = new Graph(this.testMap);
            // The full graph with 0 as seed
            Slice slice = new Slice(new HashSet<int>(), new HashSet<int>());

            Slice result = obfr.Trim(slice, g);

            Assert.AreEqual(0, result.subgraph.Count);
            Assert.AreEqual(0, result.seeds.Count);
        }

        [TestMethod]
        public void trimTestFullyRemoved()
        {
            this.singleEdgeGraph();

            Graph g = new Graph(this.testMap);
            // The full graph with 0 as seed
            Slice slice = new Slice(new HashSet<int>(), new HashSet<int>());

            Slice result = obfr.Trim(slice, g);

            Assert.AreEqual(0, result.subgraph.Count);
            Assert.AreEqual(0, result.seeds.Count);
        }

        [TestMethod]
        public void trimTestOneSeed()
        {
            this.singleLoopGraph();

            Graph g = new Graph(this.testMap);
            // The full graph with 0 as seed
            Slice slice = new Slice(new HashSet<int>(new int[] { 0, 1, 2, 3, 4 }), new HashSet<int>(new int[] { 0 }));

            Slice result = obfr.Trim(slice, g);

            Assert.IsFalse(result.subgraph.Contains(0));
            Assert.IsFalse(result.subgraph.Contains(1));
            Assert.IsTrue(result.subgraph.Contains(2));
            Assert.IsTrue(result.subgraph.Contains(3));
            Assert.IsTrue(result.subgraph.Contains(4));
        }

        [TestMethod]
        public void trimTestTwoSeeds()
        {
            this.singleLoopGraph();

            Graph g = new Graph(this.testMap);
            // The full graph with 0 as seed
            Slice slice = new Slice(new HashSet<int>(new int[] { 0, 1, 2, 3, 4 }), new HashSet<int>(new int[] { 0, 4 }));

            Slice result = obfr.Trim(slice, g);

            Assert.IsFalse(result.subgraph.Contains(0));
            Assert.IsFalse(result.subgraph.Contains(1));
            Assert.IsFalse(result.subgraph.Contains(4));
            Assert.IsTrue(result.subgraph.Contains(2));
            Assert.IsTrue(result.subgraph.Contains(3));
        }

        [TestMethod]
        public void trimTestResult()
        {
            this.singleLoopGraph();

            Graph g = new Graph(this.testMap);
            // The full graph with 0 as seed
            Slice slice = new Slice(new HashSet<int>(new int[] { 0, 1, 2, 3, 4 }), new HashSet<int>(new int[] { 0 }));

            this.obfr.Trim(slice, g);

            ResultSet result = this.obfr.Result;

            Assert.IsTrue(result.Contains(0));
            Assert.IsTrue(result.Contains(1));
            CollectionAssert.AreEquivalent(new int[] { 0 }, result.SCCById(0).ToList());
            CollectionAssert.AreEquivalent(new int[] { 1 }, result.SCCById(1).ToList());
        }
    }
}
