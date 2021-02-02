using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;
using System.Collections.Generic;
using System.Text;

using SCC_Detection.Datastructures;
using System.Threading.Tasks;

namespace SCC_Detection_Test
{
    [TestClass]
    public class ResultSetTest
    {
        [TestMethod]
        public void AddTest()
        {
            ResultSet results = new ResultSet();

            results.Add(new HashSet<int> { 1, 3});

            Assert.IsTrue(results.Contains(1));
            Assert.IsTrue(results.Contains(3));

            Assert.IsFalse(results.Contains(2));

            results.Add(new HashSet<int> { 2 });

            Assert.IsTrue(results.Contains(2));
        }

        [TestMethod]
        public void CountTest()
        {
            ResultSet results = new ResultSet();

            Assert.AreEqual(results.Count(), 0);

            results.Add(new HashSet<int> { 1, 3 });

            Assert.AreEqual(results.Count(), 1);

            results.Add(new HashSet<int> { 2 });

            Assert.AreEqual(results.Count(), 2);
        }

        [TestMethod]
        public void ConcurrencyTest()
        {
            ResultSet results = new ResultSet();

            Task[] tasks = new Task[50];

            for (int i = 0; i < 50; i++)
            {
                // Make a copy to capture the variable
                // https://stackoverflow.com/questions/271440/captured-variable-in-a-loop-in-c-sharp
                int copy = i;
                tasks[i] = Task.Factory.StartNew(() => AddToResultSet(results, copy));
            }

            Task.WaitAll(tasks);

            Assert.AreEqual(results.Count(), 50);

            for(int i = 0; i < 50; i++)
            {
                Assert.IsTrue(results.Contains(i));
            }
        }

        private void AddToResultSet(ResultSet r, int i)
        {
            r.Add(new HashSet<int> { i });
        }
    }
}
