﻿using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;
using System.Collections.Generic;
using System.Text;

using SCC_Detection.Datastructures;
using SCC_Detection.SCCDetectors;
using System.Linq;

namespace SCC_Detection_Test
{
    [TestClass]
    public class DCSCTest
    {
        [TestMethod]
        public void emptyGraphTest()
        {
            Dictionary<int, List<int>> testMap = new Dictionary<int, List<int>>();

            Graph g = new Graph(testMap);

            DCSC dcsc = new DCSC(1);
            ResultSet results = dcsc.Compute(g);

            Assert.AreEqual(results.Count(), 0);
        }

        [TestMethod]
        public void singletonTest()
        {
            Dictionary<int, List<int>> testMap = new Dictionary<int, List<int>>();

            testMap[0] = new List<int>();

            Graph g = new Graph(testMap);

            DCSC dcsc = new DCSC(1);
            ResultSet results = dcsc.Compute(g);

            Assert.IsTrue(results.List.Count == 1);
            CollectionAssert.AreEquivalent(results.List[0].ToList(), new int[] { 0 });
        }

        [TestMethod]
        public void trivialComponentsTest()
        {
            Dictionary<int, List<int>> testMap = new Dictionary<int, List<int>>();

            testMap[0] = new List<int>();
            testMap[1] = new List<int>();
            testMap[2] = new List<int>();

            Graph g = new Graph(testMap);

            DCSC dcsc = new DCSC(1);
            ResultSet results = dcsc.Compute(g);

            for (int i = 0; i < 3; i++)
            {
                Assert.IsTrue(results.List.Count == 3);
                Assert.IsTrue(results.Contains(i));
            }
        }

        [TestMethod]
        public void singleComponentTest()
        {
            Dictionary<int, List<int>> testMap = new Dictionary<int, List<int>>();

            testMap[0] = new List<int>();
            testMap[1] = new List<int>();
            testMap[2] = new List<int>();

            testMap[0].Add(1);
            testMap[1].Add(2);
            testMap[2].Add(0);

            Graph g = new Graph(testMap);

            DCSC dcsc = new DCSC(1);
            ResultSet results = dcsc.Compute(g);

            Assert.IsTrue(results.List.Count == 1);
            CollectionAssert.AreEquivalent(results.List[0].ToList(), new int[] { 0, 1, 2 });
        }

        [TestMethod]
        public void NonTrivialComponentsTest()
        {
            Dictionary<int, List<int>> testMap = new Dictionary<int, List<int>>();

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

            Graph g = new Graph(testMap);

            DCSC dcsc = new DCSC(1);
            ResultSet results = dcsc.Compute(g);

            Assert.IsTrue(results.List.Count == 2);
            CollectionAssert.AreEquivalent(results.SCCById(0).ToList(), new int[] { 0, 1, 2 });
            CollectionAssert.AreEquivalent(results.SCCById(3).ToList(), new int[] { 3, 4 });
        }

        [TestMethod]
        public void concurrencyTrivialComponentsTest()
        {
            Dictionary<int, List<int>> testMap = new Dictionary<int, List<int>>();

            for (int i = 0; i < 10; i++)
            {
                testMap[i] = new List<int>();
            }

            Graph g = new Graph(testMap);

            // BUG when using more threads, see github issue
            DCSC dcsc = new DCSC(1);
            ResultSet results = dcsc.Compute(g);

            Assert.IsTrue(results.List.Count == 10);

            for (int i = 0; i < 10; i++)
            {
                Assert.IsTrue(results.Contains(i));
            }
        }
    }
}
