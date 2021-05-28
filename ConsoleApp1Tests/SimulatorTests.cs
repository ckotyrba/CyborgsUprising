using Microsoft.VisualStudio.TestTools.UnitTesting;
using Player;
using System;
using System.Collections.Generic;
using System.Text;
using static AssertNet.Assertions;


namespace Player.Tests
{
    [TestClass()]
    public class SimulatorTests
    {
        [TestMethod()]
        public void ErobernTest()
        {
            Player.Factory factoryPlayer = new Player.Factory(0, Player.Owner.Player, 5, 1, 0);
            Player.Factory factoryEnemy = new Player.Factory(1, Player.Owner.Opponent, 5, 1, 0);
            Player.Factory factoryEmpty = new Player.Factory(2, Player.Owner.Empty, 1, 3, 0);
            var distances = new List<Player.distanceStruct> {
                new Player.distanceStruct(0, 2, 5),
                new Player.distanceStruct(1, 2, 5)
            };

            var factories = new List<Player.Factory> { factoryPlayer, factoryEnemy, factoryEmpty };
            Player.Simulator.SimulationResult simulationResult = new Player.Simulator(factories, new List<Player.Troop>(), new List<Player.Bomb>(), distances)
                .SimulateMove(new List<Player.Troop> { new Player.Troop(Player.Owner.Player, factoryPlayer, factoryEmpty, 2, 5) });
            AssertThat(simulationResult.ProductionRatePlayer).IsEqualTo(4);
            AssertThat(simulationResult.ProductionRateEnemy).IsEqualTo(1);
            AssertThat(simulationResult.TroopCountPlayer).IsEqualTo(10);
            AssertThat(simulationResult.TroopCountEnemy).IsEqualTo(11);

        }

        [TestMethod()]
        public void UnterstützenTest()
        {
            Player.Factory factoryPlayer = new Player.Factory(0, Player.Owner.Player, 5, 1, 0);
            Player.Factory factoryPlayer2 = new Player.Factory(1, Player.Owner.Player, 0, 1, 0);

            var distances = new List<Player.distanceStruct> {
                new Player.distanceStruct(0, 1, 5),
            };

            var factories = new List<Player.Factory> { factoryPlayer, factoryPlayer2 };
            List<Player.Troop> troops = new List<Player.Troop>();

            Player.Simulator.SimulationResult simulationResult = new Player.Simulator(factories, troops, new List<Player.Bomb>(), distances)
                .SimulateMove(new List<Player.Troop> { new Player.Troop(Player.Owner.Player, factoryPlayer, factoryPlayer2, 2, 5) });
            AssertThat(simulationResult.ProductionRatePlayer).IsEqualTo(2);
            AssertThat(simulationResult.TroopCountPlayer).IsEqualTo(17);
            AssertThat(simulationResult.ProductionRateEnemy).IsEqualTo(0);
            AssertThat(simulationResult.TroopCountEnemy).IsEqualTo(0);

            AssertThat(factoryPlayer2.Cyborgs).IsEqualTo(0);
        }


        [TestMethod()]
        [ExpectedException(typeof(InvalidOperationException))]
        public void FalscherMove()
        {
            Player.Factory factoryPlayer = new Player.Factory(0, Player.Owner.Player, 5, 1, 0);
            Player.Factory factoryPlayer2 = new Player.Factory(1, Player.Owner.Player, 0, 1, 0);

            var distances = new List<Player.distanceStruct> {
                new Player.distanceStruct(0, 1, 5),
            };

            var factories = new List<Player.Factory> { factoryPlayer, factoryPlayer2 };
            List<Player.Troop> troops = new List<Player.Troop>();

            Player.Simulator.SimulationResult simulationResult = new Player.Simulator(factories, troops, new List<Player.Bomb>(), distances)
                .SimulateMove(new List<Player.Troop> { new Player.Troop(Player.Owner.Player, factoryPlayer, factoryPlayer2, 6, 5) });

        }


        [TestMethod()]
        public void AngriffGleichStandTest()
        {
            Player.Factory factoryPlayer = new Player.Factory(0, Player.Owner.Player, 3, 1, 0);
            Player.Factory factoryEnemy = new Player.Factory(1, Player.Owner.Opponent, 3, 1, 0);
            Player.Factory factoryEmpty = new Player.Factory(2, Player.Owner.Empty, 1, 3, 0);
            var distances = new List<Player.distanceStruct> {
                new Player.distanceStruct(0, 2, 5),
                new Player.distanceStruct(1, 2, 5)
            };

            var factories = new List<Player.Factory> { factoryPlayer, factoryEnemy, factoryEmpty };

            List<Player.Troop> troops = new List<Player.Troop> {
                new Player.Troop(Player.Owner.Player, factoryPlayer, factoryEmpty, 2, 5),
                new Player.Troop(Player.Owner.Opponent, factoryEnemy, factoryEmpty, 2, 5)
            };
            Player.Simulator.SimulationResult simulationResult = new Player.Simulator(factories, troops, new List<Player.Bomb>(), distances)
                .SimulateMove(null);
            AssertThat(simulationResult.ProductionRatePlayer).IsEqualTo(1);
            AssertThat(simulationResult.ProductionRateEnemy).IsEqualTo(1);
            AssertThat(simulationResult.TroopCountPlayer).IsEqualTo(8);
            AssertThat(simulationResult.TroopCountEnemy).IsEqualTo(8);
        }


        [TestMethod()]
        public void AngriffVerlustTest()
        {
            Player.Factory factoryPlayer = new Player.Factory(0, Player.Owner.Player, 3, 1, 0);
            Player.Factory factoryEnemy = new Player.Factory(1, Player.Owner.Opponent, 2, 1, 0);
            Player.Factory factoryEmpty = new Player.Factory(2, Player.Owner.Empty, 1, 3, 0);
            var distances = new List<Player.distanceStruct> {
                new Player.distanceStruct(0, 2, 5),
                new Player.distanceStruct(1, 2, 5)
            };

            var factories = new List<Player.Factory> { factoryPlayer, factoryEnemy, factoryEmpty };

            List<Player.Troop> troops = new List<Player.Troop> {
                new Player.Troop(Player.Owner.Player, factoryPlayer, factoryEmpty, 2, 5),
                new Player.Troop(Player.Owner.Opponent, factoryEnemy, factoryEmpty, 3, 5)
            };
            Player.Simulator.SimulationResult simulationResult = new Player.Simulator(factories, troops, new List<Player.Bomb>(), distances)
                .SimulateMove(null);
            AssertThat(simulationResult.ProductionRatePlayer).IsEqualTo(1);
            AssertThat(simulationResult.ProductionRateEnemy).IsEqualTo(1);
            AssertThat(simulationResult.TroopCountPlayer).IsEqualTo(8);
            AssertThat(simulationResult.TroopCountEnemy).IsEqualTo(7);
        }

        [TestMethod()]
        public void AngriffVerlustEroberungTest()
        {
            Player.Factory factoryPlayer = new Player.Factory(0, Player.Owner.Player, 3, 1, 0);
            Player.Factory factoryEnemy = new Player.Factory(1, Player.Owner.Opponent, 1, 1, 0);
            Player.Factory factoryEmpty = new Player.Factory(2, Player.Owner.Empty, 1, 3, 0);
            var distances = new List<Player.distanceStruct> {
                new Player.distanceStruct(0, 2, 5),
                new Player.distanceStruct(1, 2, 5)
            };

            var factories = new List<Player.Factory> { factoryPlayer, factoryEnemy, factoryEmpty };

            List<Player.Troop> troops = new List<Player.Troop> {
                new Player.Troop(Player.Owner.Player, factoryPlayer, factoryEmpty, 2, 5),
                new Player.Troop(Player.Owner.Opponent, factoryEnemy, factoryEmpty, 4, 5)
            };
            Player.Simulator.SimulationResult simulationResult = new Player.Simulator(factories, troops, new List<Player.Bomb>(), distances)
                .SimulateMove(null);
            AssertThat(simulationResult.ProductionRatePlayer).IsEqualTo(1);
            AssertThat(simulationResult.ProductionRateEnemy).IsEqualTo(4);
            AssertThat(simulationResult.TroopCountPlayer).IsEqualTo(8);
            AssertThat(simulationResult.TroopCountEnemy).IsEqualTo(7);
        }

        [TestMethod()]
        public void AngriffKomplexTest()
        {
            Player.Factory factoryPlayer = new Player.Factory(0, Player.Owner.Player, 0, 1, 0);
            Player.Factory factoryEnemy = new Player.Factory(1, Player.Owner.Opponent, 3, 1, 0);
            Player.Factory factoryEmpty = new Player.Factory(2, Player.Owner.Empty, 1, 3, 0);
            Player.Factory factoryEmpty2 = new Player.Factory(3, Player.Owner.Empty, 0, 2, 0);
            var distances = new List<Player.distanceStruct> {
                new Player.distanceStruct(factoryPlayer.Id, factoryEmpty.Id, 5),
                new Player.distanceStruct(factoryEnemy.Id, factoryEmpty.Id, 5),
                new Player.distanceStruct(factoryPlayer.Id, factoryEmpty2.Id, 5),
            };

            var factories = new List<Player.Factory> { factoryPlayer, factoryEnemy, factoryEmpty, factoryEmpty2 };

            List<Player.Troop> troops = new List<Player.Troop> {
                new Player.Troop(Player.Owner.Player, factoryPlayer, factoryEmpty, 4, 5),
                new Player.Troop(Player.Owner.Opponent, factoryEnemy, factoryEmpty, 2, 5),
                new Player.Troop(Player.Owner.Player, factoryPlayer, factoryEmpty2, 2, 10),
            };
            Player.Simulator.SimulationResult simulationResult = new Player.Simulator(factories, troops, new List<Player.Bomb>(), distances)
                .SimulateMove(null);
            AssertThat(simulationResult.ProductionRatePlayer).IsEqualTo(6);
            AssertThat(simulationResult.ProductionRateEnemy).IsEqualTo(1);
            AssertThat(simulationResult.TroopCountPlayer).IsEqualTo(28);
            AssertThat(simulationResult.TroopCountEnemy).IsEqualTo(13);
        }
    }
}