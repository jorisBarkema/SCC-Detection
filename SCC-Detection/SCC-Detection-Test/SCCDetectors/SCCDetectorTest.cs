using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;
using System.Collections.Generic;
using System.Text;

using SCC_Detection.Datastructures;
using SCC_Detection.SCCDetectors;
using SCC_Detection.Input;
using System.Linq;

namespace SCC_Detection_Test
{
    [TestClass]
    public class SCCDetectorTest
    {
        private SCCDetector[] detectors;
        private SCCDetector[] concurrentDetectors;
        private Dictionary<int, List<int>> testMap;

        [TestInitialize]
        public void InitializeTest()
        {
            this.detectors = new SCCDetector[] {  new OBFR(1), new DCSC(1) };
            this.concurrentDetectors = new SCCDetector[] { new OBFR(10), new DCSC(10) };
            //this.detectors = new SCCDetector[] { new DCSC(1) };
            this.testMap = new Dictionary<int, List<int>>();
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

                for (int i = 0; i < 3; i++)
                {
                    Assert.AreEqual(3, results.List.Count);
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

                Assert.IsTrue(results.List.Count == 2);
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
            int size = 50;

            foreach(SCCDetector detector in this.concurrentDetectors)
            {
                Graph g = RandomGraph.Generate(size, 0.05);

                // BUG when using more threads, see github issue
                ResultSet results = detector.Compute(g);

                for (int i = 0; i < results.List.Count; i++)
                {
                    // This does not work right now because OBFR changes the actual graph
                    // by removing nodes, so of course in this changed graph the SCCs are not correct.
                    Assert.IsTrue(g.IsSCC(results.List[i]));
                }
            }
        }

        // Takes too long for unit test
        [TestMethod]
        public void concurrencySampleGraphTest()
        {
            //int size = 300;

            /*
            Graph g = GraphParser.ReadFileSNAP(@"D:\Documents\computing_science\master thesis\graphs\Wiki-Vote.txt");

            // BUG when using more threads, see github issue
            DCSC dcsc = new DCSC(10);
            ResultSet results = dcsc.Compute(g);

            for (int i = 0; i < results.List.Count; i++)
            {
                Assert.IsTrue(g.IsSCC(results.List[i]));
            }
            */
        }
    }
}
