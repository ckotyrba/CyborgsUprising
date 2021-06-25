using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using Player;
using static AssertNet.Assertions;
using System.Linq;

namespace Player.Tests
{
    [TestClass()]
    public class RoutenPlanerTests
    {

        private List<distanceStruct> distances = new List<distanceStruct> {
        new distanceStruct(0, 1, 2),
        new distanceStruct(0, 2, 3),
        new distanceStruct(0, 3, 5),
        new distanceStruct(1, 2, 3),
        new distanceStruct(1, 3, 5),
        new distanceStruct(2, 3, 2),
        new distanceStruct(0, 4, 1),
        new distanceStruct(1, 4, 2),
        new distanceStruct(2, 4, 4),
        new distanceStruct(3, 4, 6),
    };

        [TestMethod()]
        public void getNextFactoryIdDirect()
        {
            var routenPlaner = new RoutenPlaner(distances, Enumerable.Range(0, 4).ToList());
            AssertThat(routenPlaner.getNextFactoryId(0, 1)).IsEqualTo(1);
        }

        [TestMethod()]
        public void getNextFactoryIdTransitive()
        {
            var routenPlaner = new RoutenPlaner(distances, Enumerable.Range(0, 4).ToList());
            AssertThat(routenPlaner.getNextFactoryId(0, 3)).IsEqualTo(2);
        }

        [TestMethod()]
        public void getNextFactoryIdPool()
        {
            List<distanceStruct> distances = new List<distanceStruct> {
        new distanceStruct(0, 1, 1),
        new distanceStruct(1, 2, 1),
        new distanceStruct(0, 2, 2)
    };


            var routenPlaner = new RoutenPlaner(distances, new List<int>() { 0, 2 });
            AssertThat(routenPlaner.getNextFactoryId(0, 2)).IsEqualTo(2);

        }
    }
}