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
    public class FloydWarshallTests
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
            var routenPlaner = new FloydWarshall(5, distances, null);
            AssertThat(routenPlaner.Path(0, 1)).ContainsExactly(1);
        }

        [TestMethod()]
        public void getNextFactoryIdTransitive()
        {
            var routenPlaner = new FloydWarshall(5, distances, null);
            AssertThat(routenPlaner.Path(0, 3)).ContainsExactly(2, 3);
        }

        [TestMethod()]
        public void pathUmwegBeiGleicherLaenge_NichtGenommenWeilHopZeitKostet()
        {
            List<distanceStruct> distances = new List<distanceStruct> {
        new distanceStruct(0, 2, 5),
        new distanceStruct(0, 1, 2),
        new distanceStruct(1, 2, 3) };

            var routenPlaner = new FloydWarshall(3, distances, null);
            AssertThat(routenPlaner.Path(0, 2)).ContainsExactly(2);
        }

        [TestMethod()]
        public void pathUmwegBeiGleicherLaenge_MitHop()
        {
            List<distanceStruct> distances = new List<distanceStruct> {
        new distanceStruct(0, 2, 6),
        new distanceStruct(0, 1, 2),
        new distanceStruct(1, 2, 3) };

            var routenPlaner = new FloydWarshall(3, distances, null);
            AssertThat(routenPlaner.Path(0, 2)).ContainsExactly(1, 2);
        }


        [TestMethod()]
        public void bevorzugeProduction()
        {
            List<distanceStruct> distances = new List<distanceStruct> {
        new distanceStruct(0, 1, 1),
        new distanceStruct(0, 2, 1),
        new distanceStruct(0, 3, 2),
        new distanceStruct(1, 3, 1),
        new distanceStruct(2, 3, 1)
            };

            var factory0 = new Factory(0, Owner.Player, 5, 3, 0);
            var factory1 = new Factory(1, Owner.Empty, 5, 0, 0);
            var factory2 = new Factory(2, Owner.Empty, 5, 1, 0);
            var factory3 = new Factory(3, Owner.Opponent, 5, 3, 0);

            var factories = new Player.FactoriesHelper(new List<Factory> { factory0, factory1, factory2, factory3 });

            var routenPlaner = new FloydWarshall(4, distances, factories);
            AssertThat(routenPlaner.Path(0, 3)).ContainsExactly(2, 3);
        }

  


    }

}