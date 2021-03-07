using Microsoft.VisualStudio.TestTools.UnitTesting;
using SCC_Detection.Datastructures;
using SCC_Detection.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace SCC_Detection_Test
{
    [TestClass]
    public class RandomGraphTest
    {
        [TestMethod]
        public void emptyTest()
        {
            Graph g = RandomGraph.Generate(0, 0, 1);

            Assert.AreEqual(0, g.GetMap().Count);
        }

        [TestMethod]
        public void normalTest()
        {
            Graph g = RandomGraph.Generate(20, 0.5, 1);

            Assert.AreEqual(20, g.GetMap().Count);

            foreach (KeyValuePair<int, List<int>> entry in g.GetMap())
            {
                Assert.IsNotNull(entry.Value);
            }
        }

        [TestMethod]
        public void noConnectionsTest()
        {
            Graph g = RandomGraph.Generate(20, 0, 1);

            Assert.AreEqual(20, g.GetMap().Count);

            foreach (KeyValuePair<int, List<int>> entry in g.GetMap())
            {
                Assert.AreEqual(0, entry.Value.Count);
            }
        }

        [TestMethod]
        public void tooManyConnectionsTest()
        {
            Graph g = RandomGraph.Generate(20, 1.1, 1);

            Assert.AreEqual(20, g.GetMap().Count);

            foreach (KeyValuePair<int, List<int>> entry in g.GetMap())
            {
                Assert.AreEqual(19, entry.Value.Count);
            }
        }
    }
}
