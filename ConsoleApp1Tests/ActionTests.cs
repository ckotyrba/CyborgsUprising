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
        private TroopAction actionA;

        [TestMethod()]
        public void CompareToTest()
        {
            Player.distances.Add(new distanceStruct(1, 3, 2));
            Factory factoryFrom = new Factory(1, Owner.Player, 18, 0, 0);
            Factory factoryTo = new Factory(3, Owner.Empty, 1, 2, 0);
            TroopAction troopAction = new TroopAction("test", new List<Troop>() { new Troop(Owner.Player, factoryFrom, factoryTo, 2, 2) }, factoryTo);
            var updateAction = new UpdateAction(new Factory(1, Owner.Player, 18, 0, 0));

            AssertThat(troopAction.CostsCyborgsPerProduction(3, 1)).IsEqualTo(1.0);
            //AssertThat(troopAction.(updateAction)).IsGreaterThan(0);
        }

        [TestMethod()]
        public void UpdateActionCosts()
        {
            var updateAction = new UpdateAction(new Factory(1, Owner.Player, 18, 0, 0));

            AssertThat(updateAction.CostsCyborgsPerProduction(3, 1)).IsEqualTo(8.0);
        }

        [TestMethod()]
        public void TroopActionEqlas()
        {
            var factory1 = new Factory(1, Owner.Player, 1, 1, 1);
            var factory2 = new Factory(2, Owner.Player, 1, 1, 1);
            var factory3 = new Factory(3, Owner.Player, 1, 1, 1);

            var actionA = new TroopAction("Attack", new Troop(Owner.Player, factory1, factory2, 1, 5), factory3);
            var actionB = new TroopAction("Attack", new Troop(Owner.Player, factory1, factory2, 1, 5), factory3);

            AssertThat(actionA).IsEqualTo(actionB);
        }


        [TestMethod()]
        public void TroopActionEqualsList()
        {
            var factory1 = new Factory(1, Owner.Player, 1, 1, 1);
            var factory2 = new Factory(2, Owner.Player, 1, 1, 1);
            var factory3 = new Factory(3, Owner.Player, 1, 1, 1);


            Troop troopA = new Troop(Owner.Player, factory1, factory2, 1, 5);
            Troop troopB = new Troop(Owner.Player, factory1, factory2, 1, 4);
            var actionA = new TroopAction("Attack", new List<Troop>() { troopA, troopB }, factory3);
            var actionB = new TroopAction("Attack", new List<Troop>() { troopB, troopA }, factory3);

            AssertThat(actionA).IsEqualTo(actionB);
        }
    }
}