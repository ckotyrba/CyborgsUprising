using Microsoft.VisualStudio.TestTools.UnitTesting;
using Player;
using System;
using System.Collections.Generic;
using System.Text;
using static AssertNet.Assertions;


namespace Player.Tests
{
    [TestClass()]
    public class ActionTests
    {

        [TestMethod()]
        public void CompareToTest()
        {
            Player.distances.Add(new distanceStruct(1, 3, 2));
            TroopAction troopAction = new TroopAction("test", new List<Troop>() { new Troop(Owner.Player, new Factory(1, Owner.Player, 18, 0, 0), new Factory(3, Owner.Empty, 1, 2, 0), 2, 2) });
            var updateAction = new UpdateAction(new Factory(1, Owner.Player, 18, 0, 0));

            AssertThat(troopAction.CostsCyborgsPerProduction(3)).IsEqualTo(1.0);
            //AssertThat(troopAction.(updateAction)).IsGreaterThan(0);
        }

        [TestMethod()]
        public void UpdateActionCosts()
        {
            var updateAction = new UpdateAction(new Factory(1, Owner.Player, 18, 0, 0));

            AssertThat(updateAction.CostsCyborgsPerProduction(3)).IsEqualTo(8.0);
        }
    }
}