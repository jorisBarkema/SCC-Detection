using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;
using System.Collections.Generic;
using System.Linq;

using SCC_Detection.Datastructures;

namespace SCC_Detection_Test
{
    [TestClass]
    public class GraphTest
    {
        private Dictionary<int, List<int>> testMap;

        [TestInitialize] //this doesn't work!
        public void InitializeTest()
        {
            this.testMap = new Dictionary<int, List<int>>();
        }

        /// <summary>
        /// 0 --> 1 --> 2 --> 3 --> 4
        /// </summary>
        private void singleLineGraph()
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

        /// <summary>
        /// 0 --> 1 --> 2 <-- 3 <-- 4
        /// </summary>
        private void pyramidGraph()
        {
            testMap[0] = new List<int>();
            testMap[1] = new List<int>();
            testMap[2] = new List<int>();
            testMap[3] = new List<int>();
            testMap[4] = new List<int>();

            testMap[0].Add(1);
            testMap[1].Add(2);
            testMap[3].Add(2);
            testMap[4].Add(3);
        }

        [TestMethod]
        public void EmptyDictNoErrorsTest()
        {
            Graph g = new Graph(testMap);

            Assert.AreEqual(g.GetMap().Keys.Count, 0);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception),
        "Graph map includes non-existing ID 3.")]
        public void InvalidGraphTest()
        {
            testMap[0] = new List<int>();
            testMap[0].Add(1);
            testMap[0].Add(3);
            testMap[1] = new List<int>();

            Graph g = new Graph(testMap);
        }

        [TestMethod]
        public void InitializationTest()
        {
            testMap[0] = new List<int>();
            testMap[0].Add(1);
            testMap[0].Add(2);
            testMap[1] = new List<int>();
            testMap[1].Add(2);
            testMap[2] = new List<int>();

            CollectionAssert.AreEquivalent(new int[] { 1, 2 }.ToList(), testMap[0]);
            CollectionAssert.AreEquivalent(new int[] { 2 }.ToList(), testMap[1]);
            CollectionAssert.AreEquivalent(new int[] { }.ToList(), testMap[2]);

            Graph g = new Graph(testMap);

            Dictionary<int, List<int>> graphMap = g.GetMap();

            CollectionAssert.AreEquivalent(new int[] { 1, 2 }.ToList(), graphMap[0]);
            CollectionAssert.AreEquivalent(new int[] { 2 }.ToList(), graphMap[1]);
            CollectionAssert.AreEquivalent(new int[] { }.ToList(), graphMap[2]);
            
            Dictionary<int, List<int>> transposedGraphMap = g.GetTransposedMap();

            CollectionAssert.AreEquivalent(new int[] { }.ToList(), transposedGraphMap[0]);
            CollectionAssert.AreEquivalent(new int[] { 0 }.ToList(), transposedGraphMap[1]);
            CollectionAssert.AreEquivalent(new int[] { 0, 1 }.ToList(), transposedGraphMap[2]);
        }

        [TestMethod]
        public void ReachableTest()
        {
            testMap[0] = new List<int>();
            testMap[0].Add(1);
            testMap[0].Add(2);
            testMap[1] = new List<int>();
            testMap[1].Add(2);
            testMap[2] = new List<int>();
            testMap[2].Add(3);
            testMap[3] = new List<int>();

            Graph g = new Graph(testMap);
            Dictionary<int, List<int>> graphMap = g.GetMap();
            HashSet<int> totalSet = new HashSet<int>(graphMap.Keys);
            HashSet<int> withoutThree = new HashSet<int>(new int[] { 0, 1, 2 });

            CollectionAssert.AreEquivalent(new int[] { 0, 1, 2, 3 }.ToList(), g.Reachable(new HashSet<int>(new int[] { 0 }), totalSet, graphMap).ToList());
            CollectionAssert.AreEquivalent(new int[] { 0, 1, 2 }.ToList(), g.Reachable(new HashSet<int>(new int[] { 0 }), withoutThree, graphMap).ToList());

            CollectionAssert.AreEquivalent(new int[] { 1, 2, 3 }.ToList(), g.Reachable(new HashSet<int>(new int[] { 1 }), totalSet, graphMap).ToList());
            CollectionAssert.AreEquivalent(new int[] { 3 }.ToList(), g.Reachable(new HashSet<int>(new int[] { 3 }), totalSet, graphMap).ToList());
        }

        [TestMethod]
        public void SingleSCCTest()
        {
            testMap[0] = new List<int>();
            testMap[0].Add(1);
            testMap[0].Add(2);
            testMap[1] = new List<int>();
            testMap[1].Add(2);
            testMap[2] = new List<int>();
            testMap[2].Add(0);

            Graph g = new Graph(testMap);

            Assert.IsTrue(g.IsSCC(new HashSet<int> { 0, 1, 2}));
        }

        [TestMethod]
        public void MoreSCCsTest()
        {
            testMap[0] = new List<int>();
            testMap[0].Add(1);
            testMap[0].Add(2);
            testMap[1] = new List<int>();
            testMap[1].Add(0);
            testMap[1].Add(2);
            testMap[2] = new List<int>();

            Graph g = new Graph(testMap);

            Assert.IsTrue(g.IsSCC(new HashSet<int> { 0, 1 }));
            Assert.IsTrue(g.IsSCC(new HashSet<int> { 2 }));
            Assert.IsFalse(g.IsSCC(new HashSet<int> { 0, 1, 2 }));
        }

        [TestMethod]
        public void TrivialSCCsTest()
        {
            testMap[0] = new List<int>();
            testMap[1] = new List<int>();
            testMap[2] = new List<int>();

            Graph g = new Graph(testMap);

            Assert.IsTrue(g.IsSCC(new HashSet<int> { 0 }));
            Assert.IsTrue(g.IsSCC(new HashSet<int> { 1 }));
            Assert.IsTrue(g.IsSCC(new HashSet<int> { 2 }));

            Assert.IsFalse(g.IsSCC(new HashSet<int> { 0, 1 }));
            Assert.IsFalse(g.IsSCC(new HashSet<int> { 1, 2 }));
            Assert.IsFalse(g.IsSCC(new HashSet<int> { 0, 2 }));
        }

        [TestMethod]
        public void RemoveConnectionTest()
        {
            this.singleLoopGraph();

            Graph g = new Graph(testMap);

            Dictionary<int, List<int>> graphMap = g.GetMap();

            CollectionAssert.AreEquivalent(new int[] { 1 }.ToList(), graphMap[0]);

            g.RemoveConnection(0, 1);
            graphMap = g.GetMap();

            CollectionAssert.AreEquivalent(new int[] { }.ToList(), graphMap[0]);
            CollectionAssert.AreEquivalent(new int[] { 3 }.ToList(), graphMap[2]);

            g.RemoveConnection(2, 3);
            graphMap = g.GetMap();

            CollectionAssert.AreEquivalent(new int[] { }.ToList(), graphMap[2]);
        }

        [TestMethod]
        public void RemoveNodeTest()
        {
            this.singleLoopGraph();

            Graph g = new Graph(testMap);

            Dictionary<int, List<int>> graphMap = g.GetMap();

            CollectionAssert.AreEquivalent(new int[] { 1 }.ToList(), graphMap[0]);

            Assert.IsTrue(graphMap.ContainsKey(1));
            g.RemoveNode(1);
            graphMap = g.GetMap();

            CollectionAssert.AreEquivalent(new int[] { }.ToList(), graphMap[0]);
            Assert.IsFalse(graphMap.ContainsKey(1));
        }

        [TestMethod]
        public void ImmediateSuccessorsTest()
        {
            this.pyramidGraph();

            Graph g = new Graph(testMap);

            CollectionAssert.AreEquivalent(new int[] { 1 }.ToList(), g.ImmediateSuccessors(0));
            CollectionAssert.AreEquivalent(new int[] { 3 }.ToList(), g.ImmediateSuccessors(4));
        }

        [TestMethod]
        public void ImmediatePredecessorsTest()
        {
            this.pyramidGraph();

            Graph g = new Graph(testMap);

            CollectionAssert.AreEquivalent(new int[] { 0 }.ToList(), g.ImmediatePredecessors(1));
            CollectionAssert.AreEquivalent(new int[] { 4 }.ToList(), g.ImmediatePredecessors(3));
            CollectionAssert.AreEquivalent(new int[] { 1, 3 }.ToList(), g.ImmediatePredecessors(2));
        }

        [TestMethod]
        public void ImmediateSuccessorsSubgraphTest()
        {
            this.pyramidGraph();

            Graph g = new Graph(testMap);

            HashSet<int> seeds = new HashSet<int>();
            seeds.Add(0);
            seeds.Add(4);

            HashSet<int> subgraph = new HashSet<int>();
            subgraph.Add(0);
            subgraph.Add(1);
            subgraph.Add(2);

            CollectionAssert.AreEquivalent(new int[] { 1, 3 }.ToList(), g.ImmediateSuccessors(seeds).ToList());
            CollectionAssert.AreEquivalent(new int[] { 1 }.ToList(), g.ImmediateSuccessors(seeds, subgraph).ToList());
        }

        [TestMethod]
        public void AddConnectionTest()
        {
            this.singleLineGraph();

            Graph g = new Graph(testMap);

            Dictionary<int, List<int>> graphMap = g.GetMap();

            CollectionAssert.AreEquivalent(new int[] { 1 }.ToList(), graphMap[0]);

            g.AddConnection(0, 2);

            graphMap = g.GetMap();

            CollectionAssert.AreEquivalent(new int[] { 1, 2 }.ToList(), graphMap[0]);

            g.AddConnection(0, 3);

            graphMap = g.GetMap();

            CollectionAssert.AreEquivalent(new int[] { 1, 2, 3 }.ToList(), graphMap[0]);
        }

        [TestMethod]
        public void DepthLimitedBFSTest()
        {
            // 0 --> 1 --> 2 <--> 3 <-- 4
            this.singleLoopGraph();

            Graph g = new Graph(testMap);

            HashSet<int> reachable = g.DepthLimitedBFS(0, 2);

            CollectionAssert.AreEquivalent(new int[] { 0, 1, 2 }.ToList(), reachable.ToList());

            reachable = g.DepthLimitedBFS(0, 10);

            CollectionAssert.AreEquivalent(new int[] { 0, 1, 2, 3 }.ToList(), reachable.ToList());
        }
    }
}
