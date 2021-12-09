using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static AssertNet.Assertions;

namespace Player.Tests
{
    [TestClass()]
    public class RemoteTest
    {

        [TestMethod()]
        public void factoryEquals()
        {
            List<Factory> a = new List<Factory> { new Factory(1, Owner.Player, 1, 1, 1) };
            List<Factory> b = new List<Factory> { new Factory(1, Owner.Player, 1, 1, 1) };

            AssertThat(a.Intersect(b)).HasSize(1);
        }

        [TestMethod()]
        public void TroopEquals()
        {
            var factory1 = new Factory(1, Owner.Player, 1, 1, 1);
            var factory2 = new Factory(2, Owner.Player, 1, 1, 1);

            var troopA = new Troop(Owner.Player, factory1, factory2, 2, 10, null);
            var troopB = new Troop(Owner.Player, factory1, factory2, 2, 10, null);

            AssertThat(troopA).IsEqualTo(troopB);
        }


    }
}