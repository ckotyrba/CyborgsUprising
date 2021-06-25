using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using static Player.Player;
using System.Diagnostics.CodeAnalysis;

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

        public static FactoriesHelper factories;

        private static int bombsRemaining = 2;

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


                var pickedActions = new List<Action>();

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
                            var bomb = new Bomb(0, Owner.Player, playerFactory, factoryToBomb, 1);
                            pickedActions.Add(new BombAction(bomb));
                            bombsRemaining--;
                        }
                    }
                }


                // protect eigene
                foreach (var factory in factories.MyFactories.OrderByDescending(fac => fac.productionRate))
                {
                    if (factory.numberOfTurnsForProduction != 0 || factory.productionRate > 0)
                    {
                        List<Troop> troops = Protect(factory);
                        if (troops.Count > 0)
                        {
                            ApplyMoveToCurrentState(troops);
                            pickedActions.Add(new TroopAction("Protect", troops));
                        }
                    }
                }

                // list update und eroberung
                while (true)
                {
                    var possibleActions = new List<Action>();
                    possibleActions.AddRange(UpgradeOhneAnwendung());
                    foreach (var factory in factories.FactoryList.Where(fac => fac.Owner == Owner.Empty))
                    {
                        var troops = Attack(factory);

                        if (troops.Count > 0)
                        {
                            possibleActions.Add(new TroopAction("Attack", troops));
                        }
                    }

                    if (possibleActions.Count == 0)
                    {
                        Console.Error.WriteLine("possibleACtions leeer");
                        break;

                    }

                    possibleActions.Sort();
                    var action = possibleActions.First();
                    action.Apply();
                    pickedActions.Add(action);

                    Console.Error.WriteLine("------" + action.ToString());
                }

                // attack 
                foreach (var factory in factories.NearestFactoriesToPlayer().Where(fac => fac.productionRate > 0)
                    .Where(fac => factories.MinDistanceTo(fac, factories.MyFactories) < factories.MinDistanceTo(fac, factories.EnemyFactories)))
                {
                    List<Troop> troops = Attack(factory);
                    if (troops.Count > 0)
                    {
                        ApplyMoveToCurrentState(troops);
                        pickedActions.Add(new TroopAction("Angriff", troops));
                    }
                }

                // verteilen
                if (factories.EnemyFactories.Count > 0)
                {
                    foreach (var factory in factories.MyFactories.Where(fac => fac.productionRate == 3))
                    {
                        var cyborgsToOffer = factory.CyborgsToOffer();
                        // TODO 0er angreifen
                        if (cyborgsToOffer > 2)
                        {
                            Factory nearestToEnemy = factories.NearestFactoryToEnemy();
                            if (nearestToEnemy == factory) continue;
                            var nextFactory = factories.getFactoryByEntityId(new RoutenPlaner(distances, factories.MyFactories.Select(fac => fac.Id).ToList()).getNextFactoryId(factory.Id, nearestToEnemy.Id));
                            var troop = new Troop(Owner.Player, factory, nextFactory, cyborgsToOffer, 10);
                            Console.Error.WriteLine($"verteile {factory}=>{nextFactory} mit {cyborgsToOffer}");
                            pickedActions.Add(new TroopAction("Verteilung", troop));
                            ApplyMoveToCurrentState(new List<Troop> { troop });
                        }
                    }
                }

                // bomb detection
                foreach (var bomb in bombs.Where(bomb => bomb.owner == Owner.Opponent))
                {
                    foreach (var myFactory in factories.MyFactories.Where(fac => fac.Cyborgs > 0))
                    {
                        if (myFactory.Distance(bomb.factoryFrom) == bomb.TurnsAlive + 1)
                        {
                            var nearestPlayerFactory = factories.FactoriesOrderedByDistanceTo(myFactory, Owner.Player).First();
                            var troop = new Troop(Owner.Player, myFactory, nearestPlayerFactory, myFactory.Cyborgs, 100);
                            pickedActions.Add(new TroopAction("Bomb detection", troop));
                            ApplyMoveToCurrentState(new List<Troop> { troop });
                        }
                    }

                }


                // blind angriff!
                if (factories.EnemyFactories.Count > 0)
                {
                    var nearestFactoryToEnemy = factories.NearestFactoryToEnemy();

                    var nearestFactory = factories.FactoriesOrderedByDistanceTo(nearestFactoryToEnemy, Owner.Opponent, Owner.Empty).First();

                    if ((nearestFactory.Owner == Owner.Opponent) &&
                        (factories.CyborgCount(Owner.Player) > factories.CyborgCount(Owner.Opponent)))
                    {
                        var troop = nearestFactoryToEnemy.Attack(nearestFactory, nearestFactoryToEnemy.CyborgsToOffer());
                        ApplyMoveToCurrentState(new List<Troop> { troop });
                        pickedActions.Add(new TroopAction("Verzweiflungs Angriff", troop));
                    }
                }

                if (pickedActions.Count == 0)
                {
                    Console.WriteLine("WAIT");
                    continue;
                }
                else
                {
                    var output = String.Join(";", pickedActions.Select(action => action.Output()));
                    Console.WriteLine(output);
                    foreach (var action in pickedActions)
                    {
                        Console.Error.WriteLine(action);
                    }
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

        private static List<UpdateAction> Upgrade()
        {
            var result = new List<UpdateAction>();
            foreach (var factory in factories.MyFactories)
            {
                if (factory.CyborgsToOffer() >= 10)
                {
                    if (factory.productionRate < 3)
                    {
                        factory.Cyborgs -= 10;
                        result.Add(new UpdateAction(factory));
                    }
                }
            }
            return result;
        }

        private static List<UpdateAction> UpgradeOhneAnwendung()
        {
            var result = new List<UpdateAction>();
            foreach (var factory in factories.MyFactories)
            {
                if (factory.CyborgsToOffer() >= 10)
                {
                    if (factory.productionRate < 3)
                    {
                        result.Add(new UpdateAction(factory));
                    }
                }
            }
            return result;
        }

        public static void ApplyMoveToCurrentState(List<Troop> troops)
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
                    .Where(fac => fac.CyborgsToOffer() > 0);
                int cyborgsNeeded = factoryToAttack.EffectiveCyborgsToBeat();
                if (factoryToAttack.productionRate == 0)
                    cyborgsNeeded += 9;

                foreach (var nearestFactoryWithCapacity in nearestFactorysWithCapacity)
                {
                    int tempProductionRate = factoryToAttack.ProducedInDistance(nearestFactoryWithCapacity);
                    int cyborgsCanBeOffered = nearestFactoryWithCapacity.CyborgsToOffer();

                    if (tempProductionRate > cyborgsCanBeOffered)
                        continue;
                    int helpingEnemies = factoryToAttack.HelpingEnemiesForDistance(nearestFactoryWithCapacity.Distance(factoryToAttack));
                    cyborgsNeeded += tempProductionRate + helpingEnemies;

                    int cyborgsToSent = Math.Min(cyborgsCanBeOffered, cyborgsNeeded);

                    if (cyborgsToSent > 0)
                    {
                        result.Add(nearestFactoryWithCapacity.Attack(factoryToAttack, cyborgsToSent));
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

        private static List<Troop> Protect(Factory factoryToProtect)
        {
            if (factoryToProtect.Owner == Owner.Player)
            {
                List<Troop> result = new List<Troop>();

                int cyborgsNeeded = factoryToProtect.EffectiveCyborgsToBeat();
                int minArrive = troopsEnemyAttackingFactory(factoryToProtect).Min(troop => (int?)troop.turnsToArrive) ?? 0;
                cyborgsNeeded -= factoryToProtect.ProducedInDistance(minArrive);
                if (factoryToProtect.productionRate == 0)
                    cyborgsNeeded += 10;

                if (cyborgsNeeded == 0)
                    return new List<Troop>();

                foreach (var nearestFactoryWithCapacity in factories.FactoriesOrderedByDistanceTo(factoryToProtect, Owner.Player).Where(fac => fac.productionRate > 0))
                {
                    if (minArrive < nearestFactoryWithCapacity.Distance(factoryToProtect))
                        continue;
                    int cyborgsCanBeOffered = nearestFactoryWithCapacity.CyborgsToOffer();
                    int cyborgsToSent = Math.Min(cyborgsCanBeOffered, cyborgsNeeded);

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

        public static int CyborgsInTroops(IEnumerable<Troop> troops)
        {
            return troops.Sum(troop => troop.numbersOfCyborgs);
        }


        public static List<Troop> troopsEnemyAttackingFactory(Factory factory)
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

        public static List<Troop> troopsPlayerAttackingFactory(Factory factory)
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
                var troop = new Troop((Owner)arg1, factories.getFactoryByEntityId(arg2), factories.getFactoryByEntityId(arg3), arg4, arg5);
                troops.Add(troop);
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

            public IOrderedEnumerable<Factory> NearestFactoriesToPlayer()
            {
                return FactoryList
                                    .Where(fac => fac.Owner != Owner.Player)
                                    .OrderBy(f =>
                                    {
                                        return FactoriesOrderedByDistanceTo(f, Owner.Player).First().Distance(f);
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

            public int MinDistanceTo(Factory factory, List<Factory> pool)
            {
                return pool.OrderBy(fac => fac.Distance(factory)).First().Distance(factory);
            }

            public Factory NearestFactoryToEnemy()
            {
                if (factories.EnemyFactories.Count == 0) return MyFactories[0];
                return MyFactories.OrderBy(fac =>
                {
                    int minDistanceToEnemy = factories.EnemyFactories.Min(enemyFac => enemyFac.Distance(fac));
                    return minDistanceToEnemy;
                }).First();
            }


        }


    }

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
            return "troop:" + factoryFrom + "=>" + factoryTo + ":" + numbersOfCyborgs + "arrive: " + turnsToArrive;
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

    public abstract class Action : IComparable<Action>
    {
        protected string Title;

        protected Action(string title)
        {
            this.Title = title;
        }

        public int CompareTo([AllowNull] Action other)
        {
            int maxDuration = Math.Max(this.Duration(), other.Duration());
            var thisCosts = CostsCyborgsPerProduction(maxDuration);
            var otherCosts = other.CostsCyborgsPerProduction(maxDuration);
            Console.Error.WriteLine(targetFactory() + ":costs=" + thisCosts + " " + other.targetFactory() + ":costs=" + otherCosts);
            return thisCosts.CompareTo(otherCosts);
        }

        protected abstract int Duration();
        public abstract double CostsCyborgsPerProduction(int duration);

        public abstract string Output();

        public abstract override string ToString();

        public abstract Factory targetFactory();

        public abstract void Apply();

    }

    public class TroopAction : Action
    {
        private List<Troop> troops;

        public TroopAction(string title, List<Troop> troop) : base(title)
        {
            if (troop.Count == 0)
                throw new InvalidOperationException("troop null");
            this.troops = troop;
        }

        public TroopAction(string title, Troop troop) : base(title)
        {
            this.troops = new List<Troop> { troop };
        }

        private string outputTroop(Troop troop) => $"MOVE {troop.factoryFrom} {troop.factoryTo} {troop.numbersOfCyborgs}";

        public override string Output() => string.Join(";", troops.Select(tro => outputTroop(tro)));

        private string toStringTroop(Troop troop) => $"{base.Title} {troop.factoryFrom}=>{troop.factoryTo}:{troop.numbersOfCyborgs}";

        public override string ToString() => string.Join("\n", troops.Select(tro => toStringTroop(tro)));

        protected override int Duration()
        {
            return troops.Max(troop => troop.factoryFrom.Distance(troop.factoryTo));
        }

        public override double CostsCyborgsPerProduction(int duration)
        {
            var factoryToConquer = troops[0].factoryTo;

            //int totalCyborgs = troops.Sum(troop => troop.numbersOfCyborgs);
            int cyborgsToBeat = factoryToConquer.EffectiveCyborgsToBeat();// + factoryToConquer.RelativeCyborgsToBeatOffset(TODO); ;
            // production nach eroberung
            int durationDiff = Math.Abs(this.Duration() - duration);
            int produced = factoryToConquer.ProducedInDistance(durationDiff);

            int totalCyborgsNeeded = Math.Min(0, cyborgsToBeat - produced);
            int productionGewinn = factoryToConquer.productionRate;
            if (productionGewinn == 0)
            {
                totalCyborgsNeeded += 10;
                productionGewinn = 1;
            }
            double result = ((double)totalCyborgsNeeded) / productionGewinn;
            return result;
        }

        public override void Apply()
        {
            Player.ApplyMoveToCurrentState(troops);
        }

        public override Factory targetFactory()
        {
            return troops[0].factoryTo;
        }
    }

    public class BombAction : Action
    {
        private Bomb bomb;

        public BombAction(Bomb bomb) : base("Bombing")
        {
            this.bomb = bomb;
        }

        public override string Output() => $"BOMB {bomb.factoryFrom} {bomb.factoryTo}";

        public override string ToString() => $"{base.Title} {bomb.factoryFrom}=>{bomb.factoryTo}";

        public override double CostsCyborgsPerProduction(int duration)
        {
            return 0;
        }

        protected override int Duration()
        {
            return 0;
        }

        public override void Apply()
        {
        }

        public override Factory targetFactory()
        {
            return bomb.factoryTo;
        }
    }

    public class UpdateAction : Action
    {
        private Factory factoryToUpdate;

        public UpdateAction(Factory factoryToUpdate) : base("Update")
        {
            this.factoryToUpdate = factoryToUpdate;
        }

        public override string Output() => $"INC {factoryToUpdate.Id}";

        public override string ToString() => $"{base.Title} {factoryToUpdate.Id}";

        public override double CostsCyborgsPerProduction(int duration)
        {
            // production fängt erst eins später an
            factoryToUpdate.productionRate++;
            int produced = factoryToUpdate.ProducedInDistance(duration - 1);
            factoryToUpdate.productionRate--;
            double result = (10 - produced) / 1.0;
            return result;
        }

        protected override int Duration()
        {
            return 0;
        }

        public override void Apply()
        {
            factoryToUpdate.Cyborgs -= 10;
        }

        public override Factory targetFactory()
        {
            return factoryToUpdate;
        }
    }

    public class RoutenPlaner
    {
        private List<distanceStruct> distances;
        public RoutenPlaner(List<distanceStruct> distances, List<int> pool)
        {
            this.distances = distances.Where(dist => pool.Contains(dist.factory1) && pool.Contains(dist.factory2)).ToList();
        }

        public int getNextFactoryId(int startFactory, int targetFactory)
        {
            var directDistance = DirectDistance(startFactory, targetFactory);
            // nehme nähesten, der distanz verringert
            var minDistance = distances
                .Where(dist =>
                {
                    int directDistance = Distance(startFactory, targetFactory);
                    if (dist.factory1 == startFactory)
                    {
                        return Distance(dist.factory2, targetFactory) < directDistance;
                    }
                    else if (dist.factory2 == startFactory)
                    {
                        return Distance(dist.factory1, targetFactory) < directDistance;
                    }
                    return false;
                }
            ).OrderBy(dist => dist.distance).DefaultIfEmpty(directDistance).First();
            return minDistance.factory1 == startFactory ? minDistance.factory2 : minDistance.factory1;
        }


        public int Distance(int start, int target)
        {
            return DirectDistance(start, target).distance;
        }

        private distanceStruct DirectDistance(int start, int target)
        {
            foreach (var distance in distances)
            {
                if (distance.factory1 == start || distance.factory2 == start)
                {
                    if (distance.factory1 == target || distance.factory2 == target)
                    {
                        return distance;
                    }
                }
            }

            throw new InvalidOperationException("keine distanz gefunden");
        }
    }

    public class Factory
    {
        public int Id { get; }
        public Owner Owner { get; set; }
        public int Cyborgs { get; set; }
        public int productionRate { get; set; }
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
            return new RoutenPlaner(distances, factories.FactoryList.Select(fac => fac.Id).ToList()).Distance(Id, other.Id);
        }

        public override string ToString() => Id.ToString();

        public override bool Equals(object obj)
        {
            return obj is Factory factory &&
                   Id == factory.Id;
        }

        public Troop Attack(Factory other, int count)
        {
            //+1 weil erst nächste runde zieht
            return new Troop(Owner.Player, this, other, count, this.Distance(other) + 1);
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
            int maxTurns = Player.troopsEnemyAttackingFactory(this).Max(troop => (int?)troop.turnsToArrive) ?? 0;
            List<Troop> troopsEnemy = copyTroops(troopsEnemyAttackingFactory(this));
            List<Troop> troopsPlayer = copyTroops(troopsPlayerAttackingFactory(this));

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
                // TODO result darf nur kleiner werden, weil sonst die vorherigen runden verloren gehen. das führt aber zu starkem zurückhalten -.-
                result -= Math.Max(0, attackAfterFight);
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
            var possibleEnemyFactories = Player.factories.FactoriesOrderedByDistanceTo(this, Owner.Opponent).Where(fac => fac.Distance(this) <= distance);
            foreach (var factory in possibleEnemyFactories)
            {
                result += factory.Cyborgs;
                result += factory.ProducedInDistance(distance - factory.Distance(this));
            }
            return result;
        }

    }



}
