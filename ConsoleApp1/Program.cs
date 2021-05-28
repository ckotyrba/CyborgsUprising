using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

/**
 * Auto-generated code below aims at helping you parse
 * the standard input according to the problem statement.
 **/
namespace Player
{
    public class Player
    {
        public static List<Troop> troops;
        private static List<Bomb> bombs = new List<Bomb>();
        public static List<distanceStruct> distances = new List<distanceStruct>();

        private static FactoriesHelper factories;

        private static int bombsRemaining = 2;

        public struct distanceStruct
        {
            public int factory1;
            public int factory2;
            public int distance;

            public distanceStruct(int factory1, int factory2, int distance)
            {
                this.factory1 = factory1;
                this.factory2 = factory2;
                this.distance = distance;
            }
        }

        static void Main(string[] args)
        {
            string[] inputs;
            int factoryCount = int.Parse(Console.ReadLine()); // the number of factories
            int linkCount = int.Parse(Console.ReadLine()); // the number of links between factories
            for (int i = 0; i < linkCount; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                int factory1 = int.Parse(inputs[0]);
                int factory2 = int.Parse(inputs[1]);
                int distance = int.Parse(inputs[2]);

                distances.Add(new distanceStruct(factory1, factory2, distance));
            }

            // game loop
            while (true)
            {
                var factoryList = new List<Factory>();
                troops = new List<Troop>();
                bombIds = new List<int>();

                int entityCount = int.Parse(Console.ReadLine()); // the number of entities (e.g. factories and troops)
                for (int i = 0; i < entityCount; i++)
                {
                    inputs = Console.ReadLine().Split(' ');
                    int entityId = int.Parse(inputs[0]);
                    string entityType = inputs[1];
                    int arg1 = int.Parse(inputs[2]);
                    int arg2 = int.Parse(inputs[3]);
                    int arg3 = int.Parse(inputs[4]);
                    int arg4 = int.Parse(inputs[5]);
                    int arg5 = int.Parse(inputs[6]);

                    readField(entityId, entityType, arg1, arg2, arg3, arg4, arg5, factoryList);
                }

                bombs.RemoveAll(bomb => !bombIds.Contains(bomb.Id));

                factories = new FactoriesHelper(factoryList);


                var pickedMoves = new List<Troop>();
                var pickedBombs = new List<Bomb>();
                string actions = "";



                // bomb
                foreach (var bomb in bombs.Where(bomb => bomb.owner == Owner.Player))
                {
                    bomb.factoryTo.Cyborgs -= Math.Min(10, bomb.factoryTo.Cyborgs / 2);
                }
                if (bombsRemaining > 0 && factories.ProductionRate(Owner.Opponent) >= factories.ProductionRate(Owner.Player))
                {
                    foreach (var factoryToBomb in factories.FactoriesOrderedByDistanceTo(factories.MyFactories.First(), Owner.Opponent).Where(fac => fac.productionRate == 3))
                    {
                        if (!BombAlreadySentToFactory(factoryToBomb))
                        {
                            var playerFactory = factories.FactoriesOrderedByDistanceTo(factoryToBomb, Owner.Player).First();
                            pickedBombs.Add(new Bomb(0, Owner.Player, playerFactory, factoryToBomb, 1));
                            bombsRemaining--;
                            var troop = playerFactory.Attack(factoryToBomb, playerFactory.CyborgsToOffer());
                        }
                    }
                }

                // bomb detection
                foreach (var bomb in bombs.Where(bomb => bomb.owner == Owner.Opponent))
                {
                    foreach (var myFactory in factories.MyFactories)
                    {
                        if (myFactory.Distance(bomb.factoryFrom) == bomb.TurnsAlive + 1)
                        {

                            Console.Error.WriteLine($"Rette {myFactory.Id} wegen {bomb}");
                            var nearestPlayerFactory = factories.FactoriesOrderedByDistanceTo(myFactory, Owner.Player).First();
                            var troop = new Troop(Owner.Player, myFactory, nearestPlayerFactory, myFactory.Cyborgs, 0);
                            pickedMoves.Add(troop);
                            ApplyMoveToCurrentState(new List<Troop> { troop });
                        }
                    }

                }

                // protect eigene
                foreach (var factory in factories.MyFactories.OrderByDescending(fac => fac.productionRate))
                {
                    if (factory.numberOfTurnsForProduction == 0 && factory.productionRate > 0)
                    {
                        List<Troop> troops = Protect(factory);
                        ApplyMoveToCurrentState(troops);
                        pickedMoves.AddRange(troops);
                    }
                }

                // strategie erobern
                if (false && factories.ProductionRate(Owner.Player) <= factories.ProductionRate(Owner.Opponent))
                {
                    // erober naheste zu player die näher an player als gegner sind

                    // nehme alle freien, die näher an player als minDistance enemy sind
                    int minDistanceToEnemy = factories.MinDistanceTo(Owner.Opponent);
                    var factoriesNearerToPlayerThanEnemy = factories.FactoryList
                        .Where(fac => fac.Owner == Owner.Empty)
                        .Where(fac => factories.FactoriesOrderedByDistanceTo(fac, Owner.Player).First().Distance(fac) < minDistanceToEnemy)
                        .OrderBy(fac => factories.MyFactories.Min(f2 => f2.Distance(fac)));

                    foreach (var factory in factoriesNearerToPlayerThanEnemy.Where(fac => fac.productionRate > 0))
                    {
                        List<Troop> troops = Attack(factory);
                        ApplyMoveToCurrentState(troops);
                        pickedMoves.AddRange(troops);
                    }

                    Upgrade(ref actions, pickedMoves);

                    foreach (var factory in factoriesNearerToPlayerThanEnemy.Where(fac => fac.productionRate == 0))
                    {
                        List<Troop> troops = Attack(factory);
                        ApplyMoveToCurrentState(troops);
                        pickedMoves.AddRange(troops);
                    }

                }

                // angriff
                else
                {


                    // attack 
                    foreach (var factory in factories.NearestFactoriesToPlayerWeighted().Where(fac => fac.productionRate > 0))
                    {
                        List<Troop> troops = Attack(factory);
                        ApplyMoveToCurrentState(troops);
                        pickedMoves.AddRange(troops);
                    }


                    Upgrade(ref actions, pickedMoves);


                    // verteilen
                    if (factories.EnemyFactories.Count > 0)
                    {
                        foreach (var factory in factories.MyFactories.Where(fac => fac.productionRate == 3))
                        {
                            var cyborgsToOffer = factory.CyborgsToOffer();
                            // TODO 0er angreifen
                            if (cyborgsToOffer > 10)
                            {
                                var nearestToEnemy = factories.MyFactories.OrderBy(fac =>
                                {
                                    int minDistanceToEnemy = factories.EnemyFactories.Min(enemyFac => enemyFac.Distance(fac));
                                    return minDistanceToEnemy;
                                }).First();
                                if (nearestToEnemy == factory) continue;
                                var troop = new Troop(Owner.Player, factory, nearestToEnemy, cyborgsToOffer, 10);
                                pickedMoves.Add(troop);
                                ApplyMoveToCurrentState(new List<Troop> { troop });
                            }
                        }
                    }

                }

                foreach (var troop in pickedMoves)
                {
                    if (actions != "")
                        actions += ";";
                    actions += $"MOVE {troop.factoryFrom} {troop.factoryTo} {troop.numbersOfCyborgs}";
                }

                foreach (var bomb in pickedBombs)
                {
                    if (actions != "")
                        actions += ";";
                    actions += $"BOMB {bomb.factoryFrom} {bomb.factoryTo}";
                }



                if (actions == "")
                {
                    Console.WriteLine("WAIT");
                }
                else
                {
                    Console.WriteLine(actions);
                }
            }
        }

        private static bool BombAlreadySentToFactory(Factory factory)
        {

            foreach (var bomb in bombs)
            {
                if (bomb.owner == Owner.Player && bomb.factoryTo.Equals(factory))
                {
                    return true;
                }

            }
            return false;
        }

        private static void Upgrade(ref string actions, List<Troop> pickedMoves)
        {
            foreach (var factory in factories.MyFactories)
            {
                if (factory.CyborgsToOffer() >= 10)
                {
                    if (factory.productionRate < 3)
                    {
                        factory.Cyborgs -= 10;
                        if (actions != "")
                            actions += ";";
                        actions += "INC " + factory;
                    }
                }
            }
        }

        private static void ApplyMoveToCurrentState(List<Troop> troops)
        {
            foreach (var troop in troops)
            {
                troop.factoryFrom.Cyborgs -= troop.numbersOfCyborgs;
                Player.troops.Add(troop);
            }
        }

        private static List<Troop> Attack(Factory factoryToAttack)
        {
            if (factoryToAttack.Owner != Owner.Player)
            {
                List<Troop> result = new List<Troop>();
                var nearestFactorysWithCapacity = factories.FactoriesOrderedByDistanceTo(factoryToAttack, Owner.Player)
                    .Where(fac => fac.CyborgsToOffer() > 0)
                    .Where(fac => fac.productionRate > 0);
                int cyborgsNeeded = factoryToAttack.EffectiveCyborgsToBeat();
                if (factoryToAttack.productionRate == 0)
                    cyborgsNeeded += 9;

                foreach (var nearestFactoryWithCapacity in nearestFactorysWithCapacity)
                {
                    // skip upgrade leute
                    if (nearestFactoryWithCapacity.Cyborgs >= 10 && nearestFactoryWithCapacity.productionRate == 0)
                    {
                        Console.Error.WriteLine("skip" + nearestFactoryWithCapacity + "für " + factoryToAttack);
                        continue;
                    }
                    int tempProductionRate = factoryToAttack.ProducedInDistance(nearestFactoryWithCapacity);
                    int cyborgsCanBeOffered = nearestFactoryWithCapacity.CyborgsToOffer();
                    Console.Error.WriteLine("canOffer: " + cyborgsCanBeOffered);

                    if (tempProductionRate > cyborgsCanBeOffered)
                        continue;
                    int helpingEnemies = factoryToAttack.HelpingEnemiesForDistance(nearestFactoryWithCapacity.Distance(factoryToAttack));
                    Console.Error.WriteLine("helping " + helpingEnemies);
                    cyborgsNeeded += tempProductionRate + helpingEnemies;

                    int cyborgsToSent = Math.Min(cyborgsCanBeOffered, cyborgsNeeded);
                    Console.Error.WriteLine("toSent " + cyborgsToSent);

                    if (cyborgsToSent > 0)
                    {
                        Console.Error.WriteLine($"attackiere {nearestFactoryWithCapacity}=>{factoryToAttack} mit {cyborgsToSent} weil gebraucht: {cyborgsNeeded}");
                        result.Add(nearestFactoryWithCapacity.Attack(factoryToAttack, cyborgsToSent));
                        cyborgsNeeded -= cyborgsToSent;
                    }
                    if (cyborgsNeeded <= 0)
                    {
                        return result;
                    }
                    Console.Error.WriteLine($"kann {factoryToAttack} nicht angreifen mit {cyborgsToSent} weil gebraucht: {cyborgsNeeded}");

                }
            }
            return new List<Troop>();
        }

        private static List<Troop> Protect(Factory factoryToProtect)
        {
            if (factoryToProtect.Owner == Owner.Player)
            {
                List<Troop> result = new List<Troop>();

                int cyborgsNeeded = factoryToProtect.EffectiveCyborgsToBeat();
                int minArrive = troopsEnemyAttackingFactory(factoryToProtect).Min(troop => (int?)troop.turnsToArrive) ?? 0;
                cyborgsNeeded += factoryToProtect.ProducedInDistance(minArrive);
                if (factoryToProtect.productionRate == 0)
                    cyborgsNeeded += 10;

                if (cyborgsNeeded == 0)
                    return new List<Troop>();
                Console.Error.WriteLine($"protect {factoryToProtect} braucht {cyborgsNeeded}");

                foreach (var nearestFactoryWithCapacity in factories.FactoriesOrderedByDistanceTo(factoryToProtect, Owner.Player).Where(fac => fac.productionRate > 0))
                {
                    Console.Error.WriteLine("untersuche " + nearestFactoryWithCapacity);
                    if (minArrive < nearestFactoryWithCapacity.Distance(factoryToProtect))
                        continue;
                    int cyborgsCanBeOffered = nearestFactoryWithCapacity.CyborgsToOffer();
                    int cyborgsToSent = Math.Min(cyborgsCanBeOffered, cyborgsNeeded);
                    Console.Error.WriteLine($"protect {nearestFactoryWithCapacity} canOffer {cyborgsToSent} needed {cyborgsNeeded}");

                    if (cyborgsToSent > 0)
                    {
                        result.Add(nearestFactoryWithCapacity.Attack(factoryToProtect, cyborgsToSent));
                        Console.Error.WriteLine($"beschütze {nearestFactoryWithCapacity}=>{factoryToProtect} mit {cyborgsToSent} weil gebraucht: {cyborgsNeeded}");
                        cyborgsNeeded -= cyborgsToSent;
                    }
                    if (cyborgsNeeded <= 0)
                    {
                        return result;
                    }
                }
            }
            return new List<Troop>();
        }

        private static int CyborgsInTroops(IEnumerable<Troop> troops)
        {
            return troops.Sum(troop => troop.numbersOfCyborgs);
        }


        private static List<Troop> troopsEnemyAttackingFactory(Factory factory)
        {
            List<Troop> result = new List<Troop>();
            foreach (var troop in troops)
            {
                // gleichzeitig erreichbar
                if (troop.Attacks(factory) && troop.Owner == Owner.Opponent)
                {
                    result.Add(troop);
                }
            }

            return result;
        }

        private static List<Troop> troopsPlayerAttackingFactory(Factory factory)
        {
            List<Troop> result = new List<Troop>();
            foreach (var troop in troops)
            {
                // gleichzeitig erreichbar
                if (troop.Attacks(factory) && troop.Owner == Owner.Player)
                {
                    result.Add(troop);
                }
            }
            return result;
        }

        private static List<int> bombIds = new List<int>();

        private static void readField(int entityId, string entityType, int arg1, int arg2, int arg3, int arg4, int arg5, List<Factory> factoryList)
        {
            if (entityType == "FACTORY")
            {
                Factory factory = new Factory(entityId, (Owner)arg1, arg2, arg3, arg4);
                factoryList.Add(factory);
            }
            else if (entityType == "TROOP")
            {
                troops.Add(new Troop((Owner)arg1, factories.getFactoryByEntityId(arg2), factories.getFactoryByEntityId(arg3), arg4, arg5));
            }
            else
            {
                Bomb? oldBomb = bombs.Where(bomb => bomb.Id == entityId).FirstOrDefault();
                if (oldBomb != null)
                {
                    oldBomb.TurnsAlive++;
                }
                else
                {

                    var bomb = new Bomb(entityId, (Owner)arg1, factories.getFactoryByEntityId(arg2), factories.getFactoryByEntityId(arg3), arg4);
                    bombs.Add(bomb);
                }
                bombIds.Add(entityId);

            }

        }

        public class FactoriesHelper
        {
            public List<Factory> FactoryList { get; }
            public List<Factory> MyFactories { get; }
            public List<Factory> EnemyFactories { get; }

            public FactoriesHelper(List<Factory> factories)
            {
                this.FactoryList = factories;
                this.MyFactories = new List<Factory>(factories.Where(fac => fac.Owner == Owner.Player));
                this.EnemyFactories = new List<Factory>(factories.Where(fac => fac.Owner == Owner.Opponent));
            }

            public IOrderedEnumerable<Factory> FactoriesOrderedByDistanceTo(Factory targetFactory, params Owner[] allowedOwner)
            {
                return from factory in FactoryList
                       where allowedOwner.Contains(factory.Owner)
                       where !factory.Equals(targetFactory)
                       orderby factory.Distance(targetFactory)
                       select factory;
            }

            public Factory getFactoryByEntityId(int entityId)
            {
                return FactoryList.FirstOrDefault(factory => factory.Id == entityId);
            }

            public IOrderedEnumerable<Factory> MyFactoriesGetsAttacked()
            {
                return factories.MyFactories.Where(fac => fac.TroopsAttacking() != 0).OrderByDescending(fac => fac.productionRate);
            }

            public IOrderedEnumerable<Factory> NearestFactoriesToPlayerWeighted()
            {
                return FactoryList
                                    .Where(fac => fac.Owner != Owner.Player)
                                    .OrderBy(f =>
                                    {
                                        var realDistance = FactoriesOrderedByDistanceTo(f, Owner.Player).First().Distance(f);
                                        if (f.productionRate == 0)
                                            return realDistance * 2;
                                        return realDistance;
                                    });
            }

            public int ProductionRate(Owner owner)
            {
                int result = 0;
                foreach (var factory in FactoryList.Where(fac => fac.Owner == owner))
                {
                    result += factory.productionRate;
                }
                return result;
            }

            public int CyborgCount(Owner owner)
            {
                int result = 0;
                foreach (var factory in FactoryList.Where(fac => fac.Owner == owner))
                {
                    result += factory.Cyborgs;
                }
                foreach (var troop in troops.Where(troop => troop.Owner == owner))
                {
                    result += troop.numbersOfCyborgs;
                }
                return result;
            }

            public int MinDistanceTo(Owner owner)
            {
                return MyFactories.Min(fac => FactoriesOrderedByDistanceTo(fac, owner).First().Distance(fac));
            }

        }


        public class Factory
        {
            public int Id { get; }
            public Owner Owner { get; set; }
            public int Cyborgs { get; set; }
            public int productionRate { get; }
            public int numberOfTurnsForProduction { get; set; }

            public Factory(int id, Owner owner, int cyborgs, int productionRate, int numberOfTurnsForProduction)
            {
                this.Id = id;
                this.Owner = owner;
                this.Cyborgs = cyborgs;
                this.productionRate = productionRate;
                this.numberOfTurnsForProduction = numberOfTurnsForProduction;
            }

            public Factory(Factory other)
            {
                Id = other.Id;
                Owner = other.Owner;
                Cyborgs = other.Cyborgs;
                productionRate = other.productionRate;
                numberOfTurnsForProduction = other.numberOfTurnsForProduction;
            }

            public int Distance(Factory other)
            {
                foreach (var distance in distances)
                {
                    if (distance.factory1 == Id || distance.factory2 == Id)
                    {
                        if (distance.factory1 == other.Id || distance.factory2 == other.Id)
                        {
                            return distance.distance;
                        }
                    }
                }

                return Int32.MaxValue;
            }

            public override string ToString() => Id.ToString();

            public override bool Equals(object obj)
            {
                return obj is Factory factory &&
                       Id == factory.Id;
            }

            public Troop Attack(Factory other, int count)
            {
                return new Troop(Owner.Player, this, other, count, this.Distance(other));
            }

            public int TroopsAttacking()
            {
                return troops.Where(troop => troop.Attacks(this)).Sum(troop => troop.numbersOfCyborgs);
            }

            public int ProducedInDistance(Factory other)
            {
                return ProducedInDistance(Distance(other) + 1);
            }

            public int ProducedInDistance(int distance)
            {
                if (Owner == Owner.Empty)
                    return 0;
                int cyborgsProduced = distance * this.productionRate * (numberOfTurnsForProduction == 0 ? 1 : 0);
                return cyborgsProduced;
            }

            public int EffectiveCyborgsToBeat()
            {
                var troopsEnemy = troopsEnemyAttackingFactory(this);
                int troopsPlayer = CyborgsInTroops(troopsPlayerAttackingFactory(this));
                int troopsOtherOwner = CyborgsInTroops(troopsEnemy);

                /// TODO: das kann auch transitiv kommen. schon fahrende truppen auf andere fabriken könnten noch rechtzeitig da sein
                if (troopsEnemy != null && troopsEnemy.Count > 0)
                    troopsPlayer += this.ProducedInDistance(troopsEnemyAttackingFactory(this).Min(Troop => Troop.turnsToArrive));
                int result = 0;
                if (this.Owner == Owner.Player)
                {
                    result = troopsOtherOwner - (troopsPlayer + Cyborgs);
                }
                else
                {
                    //+1 weil wir die fabrik einnehmen müssen
                    result = (Cyborgs + 1) + troopsOtherOwner - troopsPlayer;

                }
                result = Math.Max(0, result);
                Console.Error.WriteLine(this + "toBeat: " + result);
                return result;
            }


            public int RelativeCyborgsToBeatOffset(Factory from)
            {
                int producedCyborgs = this.ProducedInDistance(from);
                int helpingEnemies = this.HelpingEnemiesForDistance(from.Distance(this));
                return producedCyborgs + helpingEnemies;
            }


            public int CyborgsToOffer()
            {
                int result = Cyborgs;
                int currentCyborgs = Cyborgs;
                int maxTurns = troopsEnemyAttackingFactory(this).Max(troop => (int?)troop.turnsToArrive) ?? 0;
                List<Troop> troopsEnemy = copyTroops(troopsEnemyAttackingFactory(this));
                List<Troop> troopsPlayer = copyTroops(troopsPlayerAttackingFactory(this));

                var simulator = new Simulator(factories.FactoryList, troops, bombs, distances);
                while (maxTurns > 0)
                {
                    // moveTroops
                    foreach (var troop in troopsEnemy)
                    {
                        troop.turnsToArrive--;
                    }
                    foreach (var troop in troopsPlayer)
                    {
                        troop.turnsToArrive--;
                    }

                    // fight()
                    int attack = troopsEnemy.Where(troop => troop.turnsToArrive == 0).Sum(top => top.numbersOfCyborgs);
                    int protect = troopsPlayer.Where(troop => troop.turnsToArrive == 0).Sum(top => top.numbersOfCyborgs);
                    int protectAfterFight = Math.Max(0, protect - attack);
                    int attackAfterFight = Math.Max(0, attack - protect);
                    // opfer produced
                    int produced = ProducedInDistance(1);
                    int producedAfterFight = Math.Max(0, produced - attackAfterFight);
                    attackAfterFight -= produced;
                    // opfer rest aus cyborgs
                    currentCyborgs -= attackAfterFight;
                    result -= attackAfterFight;
                    if (result < 0)
                    {
                        return 0;
                    }
                    // currentCyborgs bekommen produced rest und protected
                    currentCyborgs += producedAfterFight;
                    currentCyborgs += producedAfterFight;
                    // entferne leere troops
                    troopsEnemy = troopsEnemy.Where(tro => tro.turnsToArrive > 0).ToList();
                    troopsPlayer = troopsPlayer.Where(tro => tro.turnsToArrive > 0).ToList();

                    maxTurns--;
                }
                return result;
            }


            private List<Troop> copyTroops(List<Troop> toCopy)
            {
                var result = new List<Troop>();
                foreach (var troop in toCopy)
                {
                    result.Add(
                    new Troop(
                     troop.Owner,
                     troop.factoryFrom,
                     troop.factoryTo,
                     troop.numbersOfCyborgs,
                     troop.turnsToArrive));
                }
                return result;
            }


            public double CyborgsNeededPerProductionIncrease(Factory from)
            {
                double effectiveCyborgs = this.EffectiveCyborgsToBeat() + RelativeCyborgsToBeatOffset(from);
                double result;

                if (productionRate == 0)
                {
                    effectiveCyborgs += 10;
                    result = 1 / effectiveCyborgs;
                }
                else
                {
                    result = productionRate / effectiveCyborgs;
                }
                return result;
            }


            internal int HelpingEnemiesForDistance(int distance)
            {
                int result = 0;
                var possibleEnemyFactories = factories.FactoriesOrderedByDistanceTo(this, Owner.Opponent).Where(fac => fac.Distance(this) < distance);
                foreach (var factory in possibleEnemyFactories)
                {
                    result += factory.Cyborgs;
                    result += factory.ProducedInDistance(distance - factory.Distance(this));
                }
                return result;
            }

        }

        public enum Owner
        {
            Player = 1,
            Opponent = -1,
            Empty = 0
        }

        public class Troop
        {
            public Owner Owner { get; }
            public Factory factoryFrom { get; }
            public Factory factoryTo { get; }
            public int numbersOfCyborgs { get; }
            public int turnsToArrive { get; set; }

            public Troop(Owner owner, Factory factoryFrom, Factory factoryTo, int numbersOfCyborgs, int turnsToArrive)
            {
                this.Owner = owner;
                this.factoryFrom = factoryFrom;
                this.factoryTo = factoryTo;
                this.numbersOfCyborgs = numbersOfCyborgs;
                this.turnsToArrive = turnsToArrive;
            }

            public bool Attacks(Factory factory)
            {
                return factoryTo.Equals(factory);
            }

            public override string ToString()
            {
                return "troop:" + factoryFrom + "=>" + factoryTo + ":" + numbersOfCyborgs;
            }
        }

        public class Bomb
        {
            public int Id;
            public Owner owner;
            public Factory factoryFrom;
            public Factory factoryTo;
            private int turnsToArrive;
            public int TurnsAlive = 0;

            public Bomb(int Id, Owner owner, Factory factoryFrom, Factory factoryTo, int turnsToArrive)
            {
                this.Id = Id;
                this.owner = owner;
                this.factoryFrom = factoryFrom;
                this.factoryTo = factoryTo;
                this.turnsToArrive = turnsToArrive;
            }

            public override string ToString()
            {
                string factoryToId = factoryTo != null ? factoryTo.Id.ToString() : "unkown";
                return "Bomb " + Id + " " + owner.ToString() + factoryFrom + "=>" + factoryTo + " alive: " + TurnsAlive;
            }
        }
        public class Simulator
        {
            private List<Factory> factories;
            private List<Troop> troops;
            private List<Bomb> bombs;
            private List<distanceStruct> distances;

            public Simulator(List<Factory> factories, List<Troop> troops, List<Bomb> bombs, List<distanceStruct> distances)
            {
                this.factories = new List<Factory>(factories.Select(factory => new Factory(factory)));
                this.troops = new List<Troop>(troops.Select(troop => copyTroopWithNewRefs(troop)));
                this.bombs = bombs;
                this.distances = distances;
            }

            private Troop copyTroopWithNewRefs(Troop toCopy)
            {
                FactoriesHelper factoryiesHelper = new FactoriesHelper(factories);
                return new Troop(
                     toCopy.Owner,
                     factoryiesHelper.getFactoryByEntityId(toCopy.factoryFrom.Id),
                     factoryiesHelper.getFactoryByEntityId(toCopy.factoryTo.Id),
                     toCopy.numbersOfCyborgs,
                     toCopy.turnsToArrive);
            }

            public Simulator SimulateMove()
            {
                // move
                foreach (var troop in troops)
                {
                    troop.turnsToArrive--;
                }

                foreach (var factory in factories)
                {
                    if (factory.numberOfTurnsForProduction > 0)
                        factory.numberOfTurnsForProduction--;
                }


                // Produce
                foreach (var factory in factories)
                {
                    if (factory.Owner != Owner.Empty)
                    {
                        if (factory.numberOfTurnsForProduction == 0)
                        {
                            factory.Cyborgs += factory.productionRate;
                        }
                    }
                }


                // Attack
                List<Troop> toRemove = new List<Troop>();
                foreach (var troop in troops)
                {
                    if (troop.turnsToArrive <= 0)
                    {
                        IEnumerable<Troop> troopsAttackingSameFactory = troops.Where(tp => tp.factoryTo.Id == troop.factoryTo.Id).Where(tp => tp.turnsToArrive <= 0).Except(toRemove);
                        int playerCyborgs = troopsAttackingSameFactory.Where(tp => tp.Owner == Owner.Player).Sum(tp => tp.numbersOfCyborgs);
                        int enemyCyborgs = troopsAttackingSameFactory.Where(tp => tp.Owner == Owner.Opponent).Sum(tp => tp.numbersOfCyborgs);
                        Owner winner = playerCyborgs > enemyCyborgs ? Owner.Player : Owner.Opponent;

                        int troopCount = Math.Abs(playerCyborgs - enemyCyborgs);
                        AttackFactory(troopCount, winner, troop.factoryTo);

                        toRemove.Add(troop);
                        toRemove.AddRange(troopsAttackingSameFactory);
                    }

                }
                troops = troops.Except(toRemove).ToList();

                return this;

            }

            private void AttackFactory(int troopCount, Owner winner, Factory factory)
            {
                if (troopCount == 0)
                    return;
                if (factory.Owner == winner)
                {
                    factory.Cyborgs += troopCount;
                }
                else
                {
                    int diff = factory.Cyborgs - troopCount;
                    if (diff < 0)
                    {
                        factory.Cyborgs = Math.Abs(diff);
                        factory.Owner = winner;
                    }
                    else
                    {
                        factory.Cyborgs = diff;
                    }
                }
            }
            public Factory getFactory(int factoryId)
            {
                FactoriesHelper factoryiesHelper = new FactoriesHelper(factories);
                return factoryiesHelper.getFactoryByEntityId(factoryId);
            }
        }
    }
}
