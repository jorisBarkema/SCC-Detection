using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;
using System.Collections.Generic;
using System.Text;

using SCC_Detection.Datastructures;
using SCC_Detection.SCCDetectors;
using SCC_Detection.Input;
using System.Linq;
using System.Collections.Concurrent;

namespace SCC_Detection_Test
{
    [TestClass]
    public class SCCDetectorTest
    {
        private SCCDetector[] detectors;
        private SCCDetector[] concurrentDetectors;
        private ConcurrentDictionary<int, List<int>> testMap;

        [TestInitialize]
        public void InitializeTest()
        {
            this.detectors = new SCCDetector[] {  new OBFR(1), new DCSC(1), new MultiPivot(1) };
            this.concurrentDetectors = new SCCDetector[] { new OBFR(10), new DCSC(10), new MultiPivot(10) };
            //this.detectors = new SCCDetector[] { new DCSC(1) };
            this.testMap = new ConcurrentDictionary<int, List<int>>();
        }

        [TestMethod]
        public void emptyGraphTest()
        {
            Graph g = new Graph(testMap);

            foreach(SCCDetector detector in this.detectors)
            {
                ResultSet results = detector.Compute(g);

                Assert.AreEqual(results.Count(), 0);
            }
        }

        [TestMethod]
        public void singletonTest()
        {
            testMap[0] = new List<int>();
            
            foreach (SCCDetector detector in this.detectors)
            {
                Graph g = new Graph(testMap);
                ResultSet results = detector.Compute(g);

                Assert.AreEqual(1, results.List.Count);
                CollectionAssert.AreEquivalent(results.List[0].ToList(), new int[] { 0 });
            }
        }

        [TestMethod]
        public void trivialComponentsTest()
        {
            testMap[0] = new List<int>();
            testMap[1] = new List<int>();
            testMap[2] = new List<int>();
            

            foreach (SCCDetector detector in this.detectors)
            {
                Graph g = new Graph(testMap);
                ResultSet results = detector.Compute(g);

                Assert.AreEqual(3, results.List.Count);

                for (int i = 0; i < 3; i++)
                {
                    Assert.IsTrue(results.Contains(i));
                }
            }
        }

        [TestMethod]
        public void singleComponentTest()
        {
            testMap[0] = new List<int>();
            testMap[1] = new List<int>();
            testMap[2] = new List<int>();

            testMap[0].Add(1);
            testMap[1].Add(2);
            testMap[2].Add(0);
            

            foreach (SCCDetector detector in this.detectors)
            {
                Graph g = new Graph(testMap);
                ResultSet results = detector.Compute(g);
                
                Assert.IsTrue(results.List.Count == 1);
                CollectionAssert.AreEquivalent(results.List[0].ToList(), new int[] { 0, 1, 2 });
            }
        }

        [TestMethod]
        public void NonTrivialComponentsTest()
        {
            testMap[0] = new List<int>();
            testMap[1] = new List<int>();
            testMap[2] = new List<int>();
            testMap[3] = new List<int>();
            testMap[4] = new List<int>();

            testMap[0].Add(1);
            testMap[1].Add(2);
            testMap[2].Add(0);
            testMap[2].Add(3);
            testMap[3].Add(4);
            testMap[4].Add(3);
            
            foreach (SCCDetector detector in this.detectors)
            {
                // 0/1/2 --> 3/4 two cycles
                Graph g = new Graph(testMap);

                ResultSet results = detector.Compute(g);

                Assert.AreEqual(2, results.List.Count);
                CollectionAssert.AreEquivalent(results.SCCById(0).ToList(), new int[] { 0, 1, 2 });
                CollectionAssert.AreEquivalent(results.SCCById(3).ToList(), new int[] { 3, 4 });
            }
        }

        [TestMethod]
        public void concurrencyTrivialComponentsTest()
        {
            int size = 100;

            for (int i = 0; i < size; i++)
            {
                testMap[i] = new List<int>();
            }

            foreach (SCCDetector detector in this.concurrentDetectors)
            {
                Graph g = new Graph(testMap);

                ResultSet results = detector.Compute(g);

                Assert.IsTrue(results.List.Count == size);

                for (int i = 0; i < size; i++)
                {
                    Assert.IsTrue(results.Contains(i));
                }
            }
        }

        [TestMethod]
        public void concurrencyRandomGraphTest()
        {
            int size = 60;

            foreach(SCCDetector detector in this.concurrentDetectors)
            {
                Graph g = RandomGraph.Generate(size, 0.05, 4);
                Graph original = new Graph(g.GetMap());

                ResultSet results = detector.Compute(g);

                for (int i = 0; i < results.List.Count; i++)
                {
                    Assert.IsTrue(original.IsSCC(results.List[i]));
                }
            }
        }
        
        [TestMethod]
        public void concurrencySampleGraphTest()
        {
            foreach (SCCDetector detector in this.concurrentDetectors)
            {
                Graph g = GraphParser.ReadFile(@"D:\Documents\computing_science\master_thesis\graphs\test_graph.txt", 4);
                Graph original = new Graph(g.GetMap());

                ResultSet results = detector.Compute(g);

                for (int i = 0; i < results.List.Count; i++)
                {
                    Assert.IsTrue(original.IsSCC(results.List[i]));
                }
            }
        }

        [TestMethod]
        public void concurrencySampleGraphTwoTest()
        {
            foreach (SCCDetector detector in this.concurrentDetectors)
            {
                Graph g = GraphParser.ReadFile(@"D:\Documents\computing_science\master_thesis\graphs\test_graph2.txt", 4);
                Graph original = new Graph(g.GetMap());

                ResultSet results = detector.Compute(g);

                for (int i = 0; i < results.List.Count; i++)
                {
                    Assert.IsTrue(original.IsSCC(results.List[i]));
                }
            }
        }
    }
}
