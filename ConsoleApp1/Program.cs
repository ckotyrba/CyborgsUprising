using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Diagnostics;
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
        public static int Round = 0;
        public static List<Troop> troops;
        public static List<Bomb> bombs = new List<Bomb>();
        public static List<distanceStruct> distances = new List<distanceStruct>();

        public static FactoriesHelper factories;

        private static int bombsRemaining = 2;

        public static HashSet<int> updateFactoryIds = new HashSet<int>();

        public static FloydWarshall floydWarshall;

        public static List<effectiveTroopEntry> effectivePlayerTroops = new List<effectiveTroopEntry>();
        public static List<(Factory sourceFactory, Factory targetFactory, int neededCyborgs)> waitingFactories = new List<(Factory sourceFactory, Factory targetFactory, int neededCyborgs)>();

        public static Stopwatch stopwatch = new Stopwatch();
        public static void Main(string[] args)
        {
            string[] inputs;
            int factoryCount = int.Parse(ReadConsole()); // the number of factories
            int linkCount = int.Parse(ReadConsole()); // the number of links between factories
            for (int i = 0; i < linkCount; i++)
            {
                inputs = ReadConsole().Split(' ');
                int factory1 = int.Parse(inputs[0]);
                int factory2 = int.Parse(inputs[1]);
                int distance = int.Parse(inputs[2]);

                distances.Add(new distanceStruct(factory1, factory2, distance));
                // Console.Error.WriteLine($"distance {factory1}=>{factory2}:{distance}");
            }

            // game loop
            while (true)
            {
                Round++;
                Console.Error.WriteLine("Round: " + Round);
                for (int i = effectivePlayerTroops.Count - 1; i >= 0; i--)
                {
                    var entry = effectivePlayerTroops[i];
                    entry.timeToLive--;
                    if (entry.timeToLive <= 0)
                    {
                        effectivePlayerTroops.RemoveAt(i);
                    }
                }
                troops = new List<Troop>();
                bombIds = new List<int>();
                factories = new FactoriesHelper();

                int entityCount = int.Parse(ReadConsole()); // the number of entities (e.g. factories and troops)
                for (int i = 0; i < entityCount; i++)
                {
                    inputs = ReadConsole().Split(' ');
                    int entityId = int.Parse(inputs[0]);
                    string entityType = inputs[1];
                    int arg1 = int.Parse(inputs[2]);
                    int arg2 = int.Parse(inputs[3]);
                    int arg3 = int.Parse(inputs[4]);
                    int arg4 = int.Parse(inputs[5]);
                    int arg5 = int.Parse(inputs[6]);

                    readField(entityId, entityType, arg1, arg2, arg3, arg4, arg5, factories);
                }
                stopwatch.Start();

                //if (effectivePlayerTroops.Sum(ept => ept.troopCount) != troops.Where(tr => tr.Owner == Owner.Player).Sum(tr => tr.numbersOfCyborgs))
                //    throw new InvalidProgramException($"effective Count != actual troop count");

                bombs.RemoveAll(bomb => !bombIds.Contains(bomb.Id));


                printStates();

                floydWarshall = new FloydWarshall(factoryCount, distances, factories);


                var pickedActions = new List<Action>();

                // update vorgemerkte
                updateFactoryIds.RemoveWhere(factoryId =>
                {
                    var factory = factories.getFactoryByEntityId(factoryId);
                    if (factory.Owner == Owner.Opponent)
                        return true;
                    Console.Error.WriteLine($"versuche update bei {factory} {factory.Cyborgs}");


                    var action = factory.Upgrade();
                    if (action != null)
                    {
                        pickedActions.Add(action);
                        action.Apply();
                    }
                    else
                    {
                        // reduzieren damit nicht weggenommen wird
                        factory.Cyborgs = 0;
                    }


                    return action != null;
                });

                // aktualisiere wartende
                for (int i = waitingFactories.Count - 1; i >= 0; i--)
                {
                    var waitingFactoryEntry = waitingFactories[i];
                    // sammlung reicht schon => schicke troops und nehme fabrik aus wartend
                    if (waitingFactoryEntry.neededCyborgs == waitingFactoryEntry.sourceFactory.Cyborgs)
                    {
                        var action = new TroopAction("Angriff von Waiting",
                            waitingFactoryEntry.sourceFactory.Attack(waitingFactoryEntry.targetFactory, waitingFactoryEntry.neededCyborgs),
                            waitingFactoryEntry.targetFactory);
                        action.Apply();
                        pickedActions.Add(action);
                        waitingFactories.RemoveAt(i);
                    }
                    // sammlung reicht noch nicht => setze verfügbare auf 0
                    else
                    {
                        waitingFactoryEntry.sourceFactory.Cyborgs = 0;
                    }

                }


                // bomb detection
                foreach (var bomb in bombs)
                {
                    Console.Error.WriteLine(bomb);
                }
                BombDetection(pickedActions);
                BombSuggestion();
                // bomb
                SendBombs(pickedActions);

                // protect eigene
                foreach (var factory in factories.MyFactories.Where(fac => fac.productionRate > 0 || fac.numberOfTurnsForProduction != 0).OrderByDescending(fac => fac.productionRate))
                {
                    List<Troop> troops = Protect(factory);
                    if (troops.Count > 0)
                    {
                        ApplyMoveToCurrentState(troops);
                        pickedActions.Add(new TroopAction("Protect", troops, factory));
                    }
                }

                ProtectFactoryFromBombs(pickedActions);
                // list update und eroberung
                while (true)
                {
                    var possibleActions = new List<Action>();
                    possibleActions.AddRange(UpgradeOhneAnwendung());

                    // Update Support troops
                    foreach (var factory in factories.MyFactories.Where(fac => fac.productionRate < 3))
                    {
                        var troops = SupportForUpdate(factory);
                        if (troops.Count > 0)
                        {
                            possibleActions.Add(new SupportAction(troops, factory));
                        }
                    }

                    HashSet<Factory> factoriesToAttack = new HashSet<Factory>(factories.FactoryList);
                    factoriesToAttack.UnionWith(factories.EnemyFactoriesAfterConquer());


                    //neue variante in verteil szenario besser, protect funktioniert aber in schritt 26 nicht, weil fabrik 11 verliert
                    //            alle anderen szenarien verlieren
                    foreach (var factory in factoriesToAttack)
                    {
                        var attack = Attack(factory);
                        if (attack != null)
                        {
                            possibleActions.Add(attack);
                        }
                    }

                    /*

                                        factoriesToAttack = new HashSet<Factory>(factories.FactoryList);

                                        foreach (var factory in factoriesToAttack.Where(fac => fac.Owner == Owner.Empty || fac.Owner == Owner.Opponent).Where(fac => fac.productionRate > 0 || fac.numberOfTurnsForProduction > 0))
                                        {
                                            var attack = Attack(factory);
                                            if (attack != null)
                                            {
                                                possibleActions.Add(attack);
                                            }
                                        }

                                        if (factories.MyFactories.All(fac => fac.productionRate == 3))
                                        {
                                            foreach (var factory in factoriesToAttack.Where(fac => fac.Owner == Owner.Empty).Where(fac => factories.MinDistanceTo(fac, factories.MyFactories) < 10).Where(fac => fac.productionRate == 0))
                                            {
                                                var attack = Attack(factory);
                                                if (attack != null)
                                                {
                                                    possibleActions.Add(attack);
                                                }
                                            }
                                        }*/


                    if (possibleActions.Count == 0)
                    {
                        Console.Error.WriteLine("possibleACtions leeer");
                        break;

                    }
                    possibleActions.Sort();

                    var action = possibleActions.First();
                    Console.Error.WriteLine("nehme action: " + action);

                    action.Apply();
                    pickedActions.Add(action);

                    Console.Error.WriteLine("------" + action.ToString());
                }
                stopwatch.Stop();
                Console.Error.WriteLine("~~~Duration=" + stopwatch.ElapsedMilliseconds);
                /// ****** NEU
                foreach (var playerFactory in factories.MyFactories)
                {
                    ///  Warte wenn nicht prod=3
                    if (playerFactory.productionRate != 3 && playerFactory.numberOfTurnsForProduction == 0)
                        continue;

                    var nearestFactoryNotFullList = factories.FactoryList
                        .OrderBy(fac => fac.Distance(playerFactory))
                        .Where(fac => fac.Id != playerFactory.Id && ((fac.Owner == Owner.Player && fac.productionRate < 3 && fac.productionRate != 0) || (fac.Owner == Owner.Opponent)))
                        .ToList();
                    int cyborgsToOffer = playerFactory.CyborgsToOffer();
                    while (cyborgsToOffer > 0 && nearestFactoryNotFullList.Count > 0)
                    {
                        var nearestFactoryNotFull = nearestFactoryNotFullList.FirstOrDefault();

                        ///  greife naheste rand gegnerfabrik oder empty (erst gegner) an über routenplaner
                        if (nearestFactoryNotFull != null)
                        {
                            nearestFactoryNotFullList.RemoveAt(0);
                            var nextFactory = floydWarshall.Path(playerFactory, nearestFactoryNotFull, factories).First();
                            if (nearestFactoryNotFull.Owner == Owner.Player)
                            {
                                // supporte nur gleich nahe
                                if (factories.MinDirectDistanceTo(nearestFactoryNotFull, factories.EnemyFactories) > factories.MinDirectDistanceTo(playerFactory, factories.EnemyFactories))
                                    continue;
                                int productionDiff = 3 - nearestFactoryNotFull.productionRate;
                                int cyborgsNeededForUpdate = productionDiff * 10 - nearestFactoryNotFull.Cyborgs;
                                cyborgsNeededForUpdate -= CyborgsInTroops(troopsPlayerAttackingFactory(nearestFactoryNotFull));
                                cyborgsNeededForUpdate += CyborgsInTroops(troopsEnemyAttackingFactory(nearestFactoryNotFull));
                                cyborgsNeededForUpdate = Math.Max(0, cyborgsNeededForUpdate);

                                int cyborgsSent = Math.Min(playerFactory.CyborgsToOffer(), cyborgsNeededForUpdate);
                                var supportAction = new TroopAction("Supporte naheste Fabrik", playerFactory.Attack(nextFactory, cyborgsSent), nearestFactoryNotFull);
                                pickedActions.Add(supportAction);
                                supportAction.Apply();
                                cyborgsToOffer -= cyborgsSent;

                            }
                            else if (nearestFactoryNotFull.Owner == Owner.Opponent)
                            {
                                var attackAction = new TroopAction("Attackiere näheste Gegner " + nearestFactoryNotFull, playerFactory.Attack(nextFactory, playerFactory.CyborgsToOffer()), nearestFactoryNotFull);
                                pickedActions.Add(attackAction);
                                attackAction.Apply();
                                continue;
                            }
                        }
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

        private static string ReadConsole()
        {
            string consoleLine = Console.ReadLine();
            Console.Error.WriteLine($"debug:^{consoleLine}debug:$");
            return consoleLine;
        }

        private static void ProtectFactoryFromBombs(List<Action> pickedActions)
        {
            foreach (var troop in BombSuggestion())
            {
                var factoryToProtect = troop.factoryTo;
                int troopDistToTarget = troop.turnsToArrive;
                var ownFactoriesSameDistThanTroop = factories.MyFactories.Where(fac => fac.Id != factoryToProtect.Id && fac.Distance(factoryToProtect) + 1 == troopDistToTarget).OrderByDescending(fac => fac.CyborgsToOffer()).ToList();
                if (ownFactoriesSameDistThanTroop.Count > 0)
                {
                    foreach (var supportFactory in ownFactoriesSameDistThanTroop)
                    {
                        int enemyAttackingFactory = CyborgsInTroops(troopsEnemyAttackingFactory(factoryToProtect));
                        int helpingEnemys = factoryToProtect.HelpingEnemiesForDistance(supportFactory.Distance(factoryToProtect));
                        int playerSupportAfterBomb = CyborgsInTroops(troopsPlayerAttackingFactory(factoryToProtect).Where(troop => troop.turnsToArrive >= troopDistToTarget));
                        int neededCyborgs = Math.Max(0, enemyAttackingFactory + helpingEnemys - playerSupportAfterBomb);
                        if (neededCyborgs == 0)
                            break;
                        if (supportFactory.CyborgsToOffer() >= neededCyborgs)
                        {
                            TroopAction troopAction = new TroopAction("Protect Bomb", supportFactory.Attack(factoryToProtect, neededCyborgs), factoryToProtect);
                            troopAction.Apply();
                            pickedActions.Add(troopAction);
                            break;
                        }
                    }
                }

            }
        }


        private static List<Troop> BombSuggestion()
        {
            List<Factory> factorysWithOneEnemyNoChanceToWin = factories.MyFactories
                .Where(fac => CyborgsInTroops(troopsEnemyAttackingFactory(fac)) == 1 && fac.Cyborgs >= 1).ToList();
            List<Troop> result = new List<Troop>();

            foreach (var factory in factorysWithOneEnemyNoChanceToWin)
            {
                if (troopsEnemyAttackingFactory(factory).Count != 1)
                    throw new InvalidOperationException("Es darf nur eine troop geben");
                result.Add(troopsEnemyAttackingFactory(factory).First());
            }
            return result;
        }

        private static void SendBombs(List<Action> pickedActions)
        {
            if (bombsRemaining > 0)
            {

                var factoryToBomb = factories.NearestFactoriesToPlayer().Where(fac => fac.Owner == Owner.Opponent && fac.productionRate == 3 &&
                                                                                !BombAlreadySentToFactory(fac) && fac.numberOfTurnsForProduction == 0).FirstOrDefault();
                if (factoryToBomb != null)
                {
                    var playerFactory = factories.FactoriesOrderedByDistanceTo(factoryToBomb, Owner.Player).FirstOrDefault();

                    var bomb = new Bomb(0, Owner.Player, playerFactory, factoryToBomb, playerFactory.Distance(factoryToBomb));
                    var bombAction = new BombAction("2. Bombe", bomb);
                    pickedActions.Add(bombAction);
                    bombAction.Apply();
                    bombsRemaining--;
                }
            }

        }

        private static int CyborgsDestroyedAfterBomb(Factory factory)
        {
            return Math.Min(10, factory.Cyborgs / 2);
        }

        private static void BombDetection(List<Action> pickedActions)
        {
            // bomb detection
            foreach (var bomb in bombs.Where(bomb => bomb.owner == Owner.Opponent))
            {
                foreach (var possibleTarget in bomb.PossibleTargets.Where(fac => fac.Cyborgs > 0 && fac.Owner == Owner.Player))
                {
                    if (possibleTarget.DirectDistance(bomb.factoryFrom) == bomb.TurnsAlive + 1)
                    {
                        var nearestPlayerFactory = factories.FactoriesOrderedByDistanceTo(possibleTarget, Owner.Player).DefaultIfEmpty(factories.getFactoryByEntityId(0)).First();
                        var troop = new Troop(Owner.Player, possibleTarget, nearestPlayerFactory, possibleTarget.Cyborgs, possibleTarget.Distance(nearestPlayerFactory) + 1, possibleTarget);
                        var action = new TroopAction("Bomb detection", troop, nearestPlayerFactory);
                        pickedActions.Add(action);
                        action.Apply();
                    }
                }
            }
        }

        private static void printStates()
        {
            Console.Write($"MSG Player:{factories.ProductionRate(Owner.Player)} Gegner:{factories.ProductionRate(Owner.Opponent)};");
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

        private static List<UpdateAction> UpgradeAllPossible()
        {
            var result = new List<UpdateAction>();
            foreach (var factory in factories.MyFactories)
            {
                var updateAction = factory.Upgrade();
                if (updateAction != null)
                    result.Add(updateAction);
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

            // Trage alle wartende ein
            // wenn waiting dabei, dürfen nicht ausreichende truppen nicht geschickt werden
            foreach (var troop in troops.Where(tr => tr is WaitingTroop))
            {
                AddTroopToWaitingList(troop);
            }

            // prüfe bei truppen, ob fabriken schon in wartend sind. Wenn ja, dann addiere needed von normale truppen auch noch
            foreach (var troop in troops.Where(tr => !(tr is WaitingTroop)))
            {
                if (SrcFactoryAlreadyInWaitingList(troop))
                {
                    AddTroopToWaitingList(troop);
                }
                else
                {
                    troop.factoryFrom.Cyborgs -= troop.numbersOfCyborgs;
                    Player.troops.Add(troop);
                    effectivePlayerTroops.Add(new effectiveTroopEntry(troop.numbersOfCyborgs, troop.effectiveTargetFactory, troop.turnsToArrive));
                }

            }
        }

        private static void AddTroopToWaitingList(Troop troop)
        {
            if (SrcFactoryAlreadyInWaitingList(troop))
            {
                var entry = waitingFactories.Find(entry => entry.sourceFactory == troop.factoryFrom);
                entry.neededCyborgs += troop.numbersOfCyborgs;
            }
            else
            {
                waitingFactories.Add((troop.factoryFrom, troop.factoryTo, troop.numbersOfCyborgs));
            }
            troop.factoryFrom.Cyborgs = 0;
        }

        private static bool SrcFactoryAlreadyInWaitingList(Troop troop)
        {
            return waitingFactories.Any(entry => entry.sourceFactory == troop.factoryFrom);
        }

        private static TroopAction Attack(Factory factoryToAttack)
        {
            if (factoryToAttack.Owner != Owner.Player)
            {
                bool zeroProductionFactory = factoryToAttack.productionRate == 0;
                List<Troop> troops = new List<Troop>();

                List<(int distance, Factory factory)> NeighborsOrderedByDistance = new List<(int distance, Factory factory)>
                    (factoryToAttack.NeighborsOrderedByDistance(Owner.Player)
                    .Where(fac => fac.CyborgsToOffer() > 0)
                     .Select(fac => (distance: fac.Distance(factoryToAttack), factory: fac))
                     .ToList());
                if (NeighborsOrderedByDistance.Count == 0)
                    return null;

                int cyborgsNeeded = factoryToAttack.EffectiveCyborgsToBeat();
                if (zeroProductionFactory)
                {
                    cyborgsNeeded += 9;
                }

                // ersetze nach jedem angriff fabrik mit ihrer wartenden ausgabe und erhöhter distanz. 
                // ersetzen: distanz zu factoryToAttack auf +1 canoffer: eigene produktion  angriff kann nicht sein weil toOffer>0
                // schaue dann nochmal von vorne nach niedrigster distanz
                int realMaxDist = NeighborsOrderedByDistance.Last().distance;
                while (NeighborsOrderedByDistance.Count() > 0 && cyborgsNeeded > 0)
                {
                    NeighborsOrderedByDistance.Sort((entryA, entryB) => entryA.distance.CompareTo(entryB.distance));
                    var nearestFactoryWithCapacity = NeighborsOrderedByDistance.First().factory;
                    NeighborsOrderedByDistance.RemoveAt(0);

                    // ATTACK ------
                    int tempProductionRate = factoryToAttack.ProducedInDistance(nearestFactoryWithCapacity);
                    int cyborgsCanBeOffered = nearestFactoryWithCapacity.CyborgsToOffer();

                    if (tempProductionRate > cyborgsCanBeOffered)
                        continue;
                    int distanceToTarget = nearestFactoryWithCapacity.Distance(factoryToAttack);
                    int helpingEnemies = factoryToAttack.HelpingEnemiesForDistance(distanceToTarget);
                    int bombDestruction = BombDestructionInDistance(distanceToTarget, factoryToAttack);

                    int cyborgsToSent = Math.Min(cyborgsCanBeOffered, cyborgsNeeded + tempProductionRate + helpingEnemies - bombDestruction);
                    int cyborgsReduced = cyborgsToSent - tempProductionRate - helpingEnemies + bombDestruction;

                    if (cyborgsToSent > 0)
                    {
                        troops.Add(nearestFactoryWithCapacity.Attack(factoryToAttack, cyborgsToSent));
                        cyborgsNeeded -= cyborgsReduced;
                    }
                    if (cyborgsNeeded <= 0 && troops.Count > 0)
                    {
                        if (zeroProductionFactory)
                            return new ZeroProductionFactoryTroopAction(troops, factoryToAttack);
                        return new TroopAction("Attack", troops, factoryToAttack);
                    }
                    // Replace -----
                    var replacement = ReplaceFactoryWithWaiting(nearestFactoryWithCapacity, factoryToAttack);
                    if (replacement.distance < realMaxDist && replacement.factory.CyborgsToOffer() > 0)
                        NeighborsOrderedByDistance.Add(replacement);
                }

            }
            return null;
        }

        private static (int distance, WaitingFactory factory) ReplaceFactoryWithWaiting(Factory nearestFactoryWithCapacity, Factory factoryToAttack)
        {
            var replacementFactory = new WaitingFactory(nearestFactoryWithCapacity);
            return (replacementFactory.Distance(factoryToAttack), replacementFactory);
        }

        private class WaitingFactory : Factory
        {
            private Factory realFactory;

            public WaitingFactory(Factory realFactory) : base(realFactory)
            {
                this.realFactory = realFactory;
            }

            public new int CyborgsToOffer()
            {
                return realFactory.ProducedInDistance(1);
            }

            public override int Distance(Factory other)
            {
                // wenn schon eintrag erhalten, dann reihe hinten an
                if (!(realFactory is WaitingFactory) && waitingFactories.Any(entry => entry.sourceFactory.Id == this.Id))
                {
                    var entry = waitingFactories.Find(entry => entry.sourceFactory.Id == this.Id);
                    if (this.productionRate == 0) return int.MaxValue;
                    return (entry.neededCyborgs + this.ProducedInDistance(1)) / this.productionRate;
                }
                return realFactory.Distance(other) + 1;
            }

            public override Troop Attack(Factory other, int count)
            {
                return new WaitingTroop(base.Attack(other, count));
            }

            public override string ToString()
            {
                return " Waiting-Factory: " + base.ToString();
            }

        }

        public class WaitingTroop : Troop
        {
            public WaitingTroop(Troop troop) : base(troop.Owner, troop.factoryFrom, troop.factoryTo, troop.numbersOfCyborgs, troop.turnsToArrive)
            {

            }
        }

        /**
         * Berechnet cyborg schaden für angreifer entfernung
         * */
        private static int BombDestructionInDistance(int attackerDistance, Factory target)
        {
            foreach (var bomb in bombs)
            {
                if (bomb.factoryTo != null && bomb.factoryTo == target)
                {
                    // +5 weil bombe 5 runden auf 0 setzt
                    if (bomb.turnsToArrive <= attackerDistance + 5)
                        return CyborgsDestroyedAfterBomb(bomb.factoryTo);
                }
            }
            return 0;
        }

        private static List<Troop> Protect(Factory factoryToProtect)
        {
            if (factoryToProtect.Owner == Owner.Player)
            {
                List<Troop> result = new List<Troop>();

                int cyborgsNeeded = factoryToProtect.EffectiveCyborgsToBeat();
                int minArrive = factoryToProtect.WillFallIn();
                if (factoryToProtect.productionRate == 0 && factoryToProtect.numberOfTurnsForProduction == 0)
                    cyborgsNeeded += 10;

                Console.Error.WriteLine($"protect {factoryToProtect} needed {cyborgsNeeded} mindestensEntfernt {minArrive}");
                if (cyborgsNeeded == 0)
                    return new List<Troop>();

                var nearestFactories = factoryToProtect.NeighborsOrderedByDistance(Owner.Player);

                foreach (var nearestFactoryWithCapacity in nearestFactories)
                {
                    //+1 weil im ersten schritt gesendet wird
                    if (minArrive < nearestFactoryWithCapacity.Distance(factoryToProtect) + 1)
                        continue;
                    int cyborgsCanBeOffered = nearestFactoryWithCapacity.CyborgsToOffer();
                    int cyborgsToSent = Math.Min(cyborgsCanBeOffered, cyborgsNeeded);

                    if (cyborgsToSent > 0)
                    {
                        result.Add(nearestFactoryWithCapacity.Attack(factoryToProtect, cyborgsToSent));
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

        private static List<Troop> SupportForUpdate(Factory factoryToSupport)
        {
            if (factoryToSupport.productionRate == 3)
            {
                return new List<Troop>();
            }
            List<Troop> result = new List<Troop>();
            var nearestFactorysWithCapacity = factoryToSupport.NeighborsOrderedByDistance(Owner.Player)
                .Where(fac => fac.productionRate == 3)
                .Where(fac => fac.CyborgsToOffer() > 0);
            int cyborgsNeeded = 10 - factoryToSupport.CyborgsToOffer();
            cyborgsNeeded -= CyborgsInTroops(troopsPlayerAttackingFactory(factoryToSupport));

            foreach (var nearestFactoryWithCapacity in nearestFactorysWithCapacity)
            {
                int producedTillArrive = factoryToSupport.ProducedInDistance(nearestFactoryWithCapacity);
                if (producedTillArrive >= cyborgsNeeded)
                    continue;
                int cyborgsCanBeOffered = nearestFactoryWithCapacity.CyborgsToOffer();
                int cyborgsToSent = Math.Min(cyborgsCanBeOffered, cyborgsNeeded - producedTillArrive);

                if (cyborgsToSent > 0)
                {
                    result.Add(nearestFactoryWithCapacity.Attack(factoryToSupport, cyborgsToSent));
                    Console.Error.WriteLine($"Supporte {nearestFactoryWithCapacity}=>{factoryToSupport} mit {cyborgsToSent} weil gebraucht: {cyborgsNeeded}");
                    cyborgsNeeded -= cyborgsToSent;
                }
                if (cyborgsNeeded <= 0)
                {
                    return result;
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

        private static void readField(int entityId, string entityType, int arg1, int arg2, int arg3, int arg4, int arg5, FactoriesHelper factoryList)
        {
            if (entityType == "FACTORY")
            {
                Factory factory = new Factory(entityId, (Owner)arg1, arg2, arg3, arg4);
                factoryList.Add(factory);
            }
            else if (entityType == "TROOP")
            {
                var troop = new Troop((Owner)arg1, factories.getFactoryByEntityId(arg2), factories.getFactoryByEntityId(arg3), arg4, arg5, null);
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

            public FactoriesHelper()
            {
                this.FactoryList = new List<Factory>();
                this.MyFactories = new List<Factory>();
                this.EnemyFactories = new List<Factory>();
            }

            public FactoriesHelper(List<Factory> factories) : this()
            {

                foreach (var factory in factories)
                {
                    Add(factory);
                }

            }

            public void Add(Factory factory)
            {
                if (factory.Owner == Owner.Player)
                    MyFactories.Add(factory);
                else if (factory.Owner == Owner.Opponent)
                    EnemyFactories.Add(factory);
                FactoryList.Add(factory);
            }

            public IOrderedEnumerable<Factory> FactoriesOrderedByDistanceTo(Factory targetFactory, params Owner[] allowedOwner)
            {
                return from factory in FactoryList
                       where allowedOwner.Contains(factory.Owner)
                       where !factory.Equals(targetFactory)
                       orderby factory.Distance(targetFactory), factory.productionRate descending
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

            public int MinDistanceTo(Factory factory, List<Factory> pool)
            {
                if (pool.Count() == 0)
                    return int.MaxValue;

                // skip target factory
                return pool.OrderBy(fac => fac.Distance(factory)).Where(fac => fac.Id != factory.Id).First().Distance(factory);
            }

            public int MinDirectDistanceTo(Factory factory, List<Factory> pool)
            {
                if (pool.Count() == 0)
                    return int.MaxValue;

                // skip target factory
                return pool
                    .Where(fac => fac.Id != factory.Id)
                    .Select(fac => fac.DirectDistance(factory))
                    .OrderBy(distance => distance)
                    .First();
            }

            public List<Factory> myFactoriesAfterConquer()
            {
                var simulator = new Simulator(FactoryList, troops, bombs, distances).SimulateMove();
                return simulator.FactoryIdsOfOwner(Owner.Player);
            }

            public List<Factory> EnemyFactoriesAfterConquer()
            {
                var simulator = new Simulator(FactoryList, troops, bombs, distances).SimulateMove();
                return simulator.FactoryIdsOfOwner(Owner.Opponent);
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

    public class effectiveTroopEntry
    {
        public int troopCount;
        public Factory effectiveTarget;
        public int timeToLive;

        public effectiveTroopEntry(int troopCount, Factory effectiveTarget, int timeToLive)
        {
            this.troopCount = troopCount;
            this.effectiveTarget = effectiveTarget;
            this.timeToLive = timeToLive;
        }

        public override string ToString()
        {
            return $"Count: {troopCount} target: {effectiveTarget} ttl:{timeToLive}";
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

        public Factory effectiveTargetFactory;

        public Troop(Owner owner, Factory factoryFrom, Factory factoryTo, int numbersOfCyborgs, int turnsToArrive, Factory effectiveTarget = null)
        {
            this.Owner = owner;
            this.factoryFrom = factoryFrom;
            this.factoryTo = factoryTo;
            this.numbersOfCyborgs = numbersOfCyborgs;
            this.turnsToArrive = turnsToArrive;
            this.effectiveTargetFactory = effectiveTarget;
        }

        public bool Attacks(Factory factory)
        {
            return factoryTo.Equals(factory);
        }

        public override string ToString()
        {
            return "troop:" + factoryFrom + "=>" + factoryTo + ":" + numbersOfCyborgs + "arrive: " + turnsToArrive;
        }

        public override bool Equals(object obj)
        {
            return obj is Troop troop &&
                   Owner == troop.Owner &&
                   EqualityComparer<Factory>.Default.Equals(factoryFrom, troop.factoryFrom) &&
                   EqualityComparer<Factory>.Default.Equals(factoryTo, troop.factoryTo) &&
                   numbersOfCyborgs == troop.numbersOfCyborgs &&
                   turnsToArrive == troop.turnsToArrive;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Owner, factoryFrom, factoryTo, numbersOfCyborgs, turnsToArrive);
        }
    }

    public class Bomb
    {
        public int Id;
        public Owner owner;
        public Factory factoryFrom;
        public Factory factoryTo;
        public int turnsToArrive;
        public int TurnsAlive = 0;
        private List<int> possibleTargets = new List<int>();
        public List<Factory> PossibleTargets { get { return possibleTargets.Select(id => factories.getFactoryByEntityId(id)).ToList(); } set { possibleTargets = value.Select(fac => fac.Id).ToList(); } }


        public Bomb(int Id, Owner owner, Factory factoryFrom, Factory factoryTo, int turnsToArrive)
        {
            this.Id = Id;
            this.owner = owner;
            this.factoryFrom = factoryFrom;
            this.factoryTo = factoryTo;
            this.turnsToArrive = turnsToArrive;

            if (factoryTo == null)
            {
                // Besitz
                possibleTargets = factories.myFactoriesAfterConquer().Where(fac => fac.productionRate > 0).Select(fac => fac.Id).ToList();
                // TODO angreigbare
            }
        }

        public override string ToString()
        {
            string factoryToId = factoryTo != null ? factoryTo.Id.ToString() : "unkown";
            return "Bomb " + Id + " " + owner.ToString() + factoryFrom + "=>" + factoryTo + " alive: " + TurnsAlive + " possibleTargets: " + string.Join(",", possibleTargets);
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
            int maxProduction = Math.Max(GainedProductionRate(), other.GainedProductionRate());
            var thisCosts = CostsCyborgsPerProduction(maxDuration, maxProduction);
            var otherCosts = other.CostsCyborgsPerProduction(maxDuration, maxProduction);
            Console.Error.WriteLine(ToString() + ":costs=" + thisCosts + " " + other.ToString() + ":costs=" + otherCosts);
            return thisCosts.CompareTo(otherCosts);
        }

        protected abstract int Duration();

        protected abstract int GainedProductionRate();
        public abstract double CostsCyborgsPerProduction(int maxDuration, int maxProductionRate);

        public abstract string Output();

        public abstract override string ToString();

        public abstract Factory TargetFactory();

        public abstract void Apply();
    }

    public class TroopAction : Action, IComparable<Action>
    {
        private List<Troop> troops;
        private Factory targetFactory;

        public TroopAction(string title, List<Troop> troop, Factory targetFactory) : base(title)
        {
            if (troop.Count == 0)
                throw new InvalidOperationException("troop null");
            this.troops = troop;
            this.targetFactory = targetFactory;
        }

        public TroopAction(string title, Troop troop, Factory target) : base(title)
        {
            this.troops = new List<Troop> { troop };
            this.targetFactory = target;
        }

        private string outputTroop(Troop troop) => $"MOVE {troop.factoryFrom} {troop.factoryTo} {troop.numbersOfCyborgs}";

        public override string Output() => string.Join(";", troops.Where(tro => !(tro is WaitingTroop)).Select(tro => outputTroop(tro)));

        private string toStringTroop(Troop troop) => $"{base.Title} {troop.factoryFrom}=>{targetFactory}:{troop.numbersOfCyborgs}";

        public override string ToString() => string.Join("\n", troops.Select(tro => toStringTroop(tro)));

        protected override int Duration()
        {
            return troops.Max(troop => troop.factoryFrom.Distance(troop.factoryTo));
        }

        public new int CompareTo([AllowNull] Action other)
        {
            int baseResult = base.CompareTo(other);
            if (baseResult == 0 && other is TroopAction otherAction)
            {
                var myFutureFactories = factories.myFactoriesAfterConquer();

                int minDistanceThis = factories.MinDistanceTo(TargetFactory(), myFutureFactories);
                int minDistanceOther = factories.MinDistanceTo(other.TargetFactory(), myFutureFactories);
                Console.Error.WriteLine($"Gleichstand! minDistanceThis:{minDistanceThis} minOther:{minDistanceOther}");
                if (minDistanceThis == minDistanceOther)
                {
                    var enemyFactories = factories.EnemyFactoriesAfterConquer();
                    int minDistanceThisToEnemy = factories.MinDistanceTo(TargetFactory(), enemyFactories);
                    int minDistanceOtherToEnemy = factories.MinDistanceTo(otherAction.TargetFactory(), enemyFactories);
                    Console.Error.WriteLine($"Gleichstand 2 minDistanceThisEnemy:{minDistanceThisToEnemy} minOther:{minDistanceOtherToEnemy}");
                    // andersrum, damit weiter weg gewinnt
                    return minDistanceOtherToEnemy.CompareTo(minDistanceThisToEnemy);
                }
                return minDistanceThis.CompareTo(minDistanceOther);
            }
            return baseResult;
        }


        public override double CostsCyborgsPerProduction(int duration, int maxProductionrate)
        {
            var factoryToConquer = TargetFactory();

            //int totalCyborgs = troops.Sum(troop => troop.numbersOfCyborgs);
            int cyborgsToBeat = factoryToConquer.EffectiveCyborgsToBeat();// + factoryToConquer.RelativeCyborgsToBeatOffset(TODO); ;
                                                                          // production nach eroberung
            int durationDiff = Math.Abs(this.Duration() - duration);
            int produced = factoryToConquer.ProducedInDistance(durationDiff);

            int totalCyborgsNeeded = Math.Max(0, cyborgsToBeat - produced);
            int productionGewinn = factoryToConquer.productionRate;
            if (productionGewinn == 0)
            {
                totalCyborgsNeeded += 10;
                productionGewinn = 1;
            }
            if (GainedProductionRate() < maxProductionrate)
                totalCyborgsNeeded += 10 * Math.Abs(GainedProductionRate() - maxProductionrate);
            double result = ((double)totalCyborgsNeeded) / productionGewinn;

            return result;
        }

        public override void Apply()
        {
            Player.ApplyMoveToCurrentState(troops);
        }

        public override Factory TargetFactory()
        {
            return targetFactory;
        }

        protected override int GainedProductionRate()
        {
            return TargetFactory().productionRate;
        }

        public override bool Equals(object obj)
        {
            if (obj is TroopAction action &&
                   Title == action.Title && targetFactory.Equals(action.targetFactory))
            {
                return this.troops.All(action.troops.Contains) && troops.Count == action.troops.Count;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Title, troops, targetFactory);
        }
    }

    public class BombAction : Action
    {
        private Bomb bomb;

        public BombAction(string title, Bomb bomb) : base(title)
        {
            this.bomb = bomb;
        }

        public override string Output() => $"BOMB {bomb.factoryFrom} {bomb.factoryTo}";

        public override string ToString() => $"{base.Title} {bomb.factoryFrom}=>{bomb.factoryTo}";

        public override double CostsCyborgsPerProduction(int duration, int maxProductionRate)
        {
            return 0;
        }

        protected override int Duration()
        {
            return 0;
        }

        public override void Apply()
        {
            Player.bombs.Add(bomb);
        }

        public override Factory TargetFactory()
        {
            return bomb.factoryTo;
        }

        protected override int GainedProductionRate()
        {
            return 0;
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

        public override double CostsCyborgsPerProduction(int duration, int maxProductionRate)
        {
            factoryToUpdate.productionRate++;
            // production fängt erst eins später an
            int produced = factoryToUpdate.ProducedInDistance(duration - 1);
            factoryToUpdate.productionRate--;
            int totalCyborgsNeeded = 10;
            if (GainedProductionRate() < maxProductionRate)
                totalCyborgsNeeded += 10 * Math.Abs(GainedProductionRate() - maxProductionRate);
            double result = (totalCyborgsNeeded - produced) / 1.0;
            return result;
        }

        protected override int Duration()
        {
            return 0;
        }

        public override void Apply()
        {
            factoryToUpdate.Cyborgs -= 10;
            factoryToUpdate.productionRate++;
            updateFactoryIds.Remove(TargetFactory().Id);
        }

        public override Factory TargetFactory()
        {
            return factoryToUpdate;
        }

        protected override int GainedProductionRate()
        {
            return 1;
        }
    }

    public class ZeroProductionFactoryTroopAction : TroopAction
    {
        public ZeroProductionFactoryTroopAction(List<Troop> troops, Factory target) : base("Empty Attack", troops, target)
        {
        }

        public override void Apply()
        {
            Console.Error.WriteLine($"Markiere Update: {TargetFactory().Id} angriff von: {string.Join(";", troops)}");
            Player.updateFactoryIds.Add(TargetFactory().Id);
            base.Apply();
        }
    }


    public class SupportAction : TroopAction
    {
        public SupportAction(List<Troop> troops, Factory target) : base("Support", troops, target)
        {
        }

        public override double CostsCyborgsPerProduction(int duration, int maxProductionRate)
        {
            var factoryToSupport = TargetFactory();
            int troopsArrivingDuration = Duration();
            int productionBeforeUpdate = 0;

            // production ohne update 
            if (duration >= troopsArrivingDuration)
                productionBeforeUpdate = factoryToSupport.ProducedInDistance(troopsArrivingDuration);

            // production mit update 
            // update erst 1 nach arrive wegen update befehl
            int durationAfterUpdate = Math.Min(0, duration - 1 - troopsArrivingDuration);
            factoryToSupport.productionRate++;
            int productionAfterUpdate = factoryToSupport.ProducedInDistance(durationAfterUpdate);
            factoryToSupport.productionRate--;

            int cyborgsNeeded = 10;
            if (GainedProductionRate() < maxProductionRate)
                cyborgsNeeded += 10 * Math.Abs(GainedProductionRate() - maxProductionRate);
            double result = (cyborgsNeeded - productionBeforeUpdate - productionAfterUpdate) / 1.0;
            return result;
        }

        protected override int GainedProductionRate()
        {
            return TargetFactory().productionRate + 1;
        }
    }


    public class FloydWarshall
    {
        private int[,] dist;
        private int[,] next;

        public FloydWarshall(int factoryCount, List<distanceStruct> distancesList, FactoriesHelper factories)
        {
            dist = new int[factoryCount, factoryCount];
            next = new int[factoryCount, factoryCount];

            for (int y = 0; y < factoryCount; y++)
            {
                for (int x = 0; x < factoryCount; x++)
                {
                    dist[y, x] = int.MaxValue;
                }
            }

            foreach (var distance in distancesList)
            {
                dist[distance.factory1, distance.factory2] = distance.distance;
                dist[distance.factory2, distance.factory1] = distance.distance;
                next[distance.factory1, distance.factory2] = distance.factory2;
                next[distance.factory2, distance.factory1] = distance.factory1;
            }

            for (int i = 0; i < factoryCount; i++)
            {
                dist[i, i] = 0;
                next[i, i] = i;
            }


            for (int k = 0; k < factoryCount; k++)
            {
                for (int source = 0; source < factoryCount; source++)
                {
                    for (int target = 0; target < factoryCount; target++)
                    {
                        // skippe schritte zu sich selbst 
                        if (source == k || target == k)
                            continue;

                        int costViaNodeK = dist[source, k] + dist[k, target];
                        //+1 weil wir wieder los müssen
                        if (k != target)
                            costViaNodeK += 0;
                        // maximiere hops, bei gleicher strecke
                        if (costViaNodeK <= dist[source, target])
                        {
                            // bevorzuge bei = und empty größte produktion zwischenstop
                            if (dist[source, target] == costViaNodeK)
                            {
                                var currentNext = factories.getFactoryByEntityId(next[source, target]);
                                var factoryK = factories.getFactoryByEntityId(k);

                                // direkter weg sollte nicht genommen werden
                                if (currentNext.Id != target && factoryK.productionRate < currentNext.productionRate)
                                {
                                    continue;
                                }
                            }
                            dist[source, target] = costViaNodeK;
                            next[source, target] = next[source, k];
                        }

                    }
                }
            }

        }

        public List<Factory> Path(Factory start, Factory target, FactoriesHelper factories)
        {
            List<Factory> path = new List<Factory>();

            int current = start.Id;
            while (current != target.Id)
            {
                current = next[current, target.Id];
                path.Add(factories.getFactoryByEntityId(current));
            }
            return path;
        }

        public List<int> Path(int startId, int targetId)
        {
            List<int> path = new List<int>();

            int current = startId;
            while (current != targetId)
            {
                current = next[current, targetId];
                path.Add(current);
            }
            return path;
        }

        public List<Factory> Neighbors(Factory factory, FactoriesHelper helper)
        {
            HashSet<Factory> neighbors = new HashSet<Factory>();
            for (int i = 0; i < next.GetLength(0); i++)
            {
                var neighbor = helper.getFactoryByEntityId(next[factory.Id, i]);
                if (!neighbors.Contains(neighbor))
                    neighbors.Add(neighbor);
            }

            neighbors.Remove(factory);
            return neighbors.ToList();
        }

        internal int Dist(Factory factory, Factory other)
        {
            return dist[factory.Id, other.Id];
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

        public virtual int Distance(Factory other)
        {
            return floydWarshall.Dist(this, other);
        }

        public int DirectDistance(Factory other)
        {
            return distances.Where(dist => dist.factory1 == this.Id || dist.factory2 == this.Id).Where(dist => dist.factory1 == other.Id || dist.factory2 == other.Id).FirstOrDefault().distance;
        }

        public override string ToString() => Id.ToString();

        public override bool Equals(object obj)
        {
            return obj is Factory factory &&
                   Id == factory.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public virtual Troop Attack(Factory other, int count)
        {
            //   var next = floydWarshall.Path(this, other, factories).First();
            //+1 weil erst nächste runde zieht
            return new Troop(Owner.Player, this, other, count, this.Distance(other) + 1, other);
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
            int troopsPlayer = CyborgsInTroops(troopsPlayerAttackingFactory(this)); //Player.effectivePlayerTroops                .Where(entry => entry.effectiveTarget.Equals(this))                .Sum(entry => entry.troopCount);
            int waitingTroopsPlayer = waitingFactories.Where(entry => entry.targetFactory == this).Sum(entry => entry.neededCyborgs);
            troopsPlayer += waitingTroopsPlayer;
            int troopsOtherOwner = CyborgsInTroops(troopsEnemy);


            Factory factoryNearestPlayer = factories.FactoriesOrderedByDistanceTo(this, Owner.Player).FirstOrDefault();
            Factory factoryNearestEnemy = factories.FactoriesOrderedByDistanceTo(this, Owner.Opponent).FirstOrDefault();

            int minDistancePlayer = factoryNearestPlayer != null ? this.Distance(factoryNearestPlayer) : 0;
            int minDistanceEnemy = factoryNearestEnemy != null ? this.Distance(factoryNearestEnemy) : int.MaxValue;

            int helpingEnemy = 0;
            // falls gegner gleich nah dran wie wir, addiere gegner
            if (minDistanceEnemy == minDistancePlayer)
            {
                helpingEnemy += factoryNearestEnemy.Cyborgs;
            }

            int result = 0;
            if (this.Owner == Owner.Player)
            {
                if (troopsEnemy != null && troopsEnemy.Count > 0)
                    troopsPlayer += this.ProducedInDistance(troopsEnemyAttackingFactory(this).Min(Troop => Troop.turnsToArrive));

                result = Math.Max(0, troopsOtherOwner + helpingEnemy - (troopsPlayer + Cyborgs));
            }
            else
            {
                //+1 weil wir die fabrik einnehmen müssen
                result = (Cyborgs + 1) + troopsOtherOwner + helpingEnemy - troopsPlayer;
            }

            //result = Math.Max(0, result);
            return result;
        }


        public int RelativeCyborgsToBeatOffset(Factory from)
        {
            int producedCyborgs = this.ProducedInDistance(from);
            int helpingEnemies = this.HelpingEnemiesForDistance(from.Distance(this));
            if (Owner == Owner.Player)
            {
                return helpingEnemies - producedCyborgs;
            }
            return producedCyborgs + helpingEnemies;
        }

        public int CyborgsToOffer()
        {
            return SimulateAttacks().cyborgsLeft;
        }

        private (int cyborgsLeft, int fallIn) SimulateAttacks()
        {
            int result = Cyborgs;
            int currentCyborgs = Cyborgs;
            int maxTurns = Player.troopsEnemyAttackingFactory(this).Max(troop => (int?)troop.turnsToArrive) ?? 0;
            List<Troop> troopsEnemy = copyTroops(troopsEnemyAttackingFactory(this));
            List<Troop> troopsPlayer = copyTroops(troopsPlayerAttackingFactory(this));
            int turn = 0;
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
                    return (0, turn + 1);
                }
                // currentCyborgs bekommen produced rest und protected
                currentCyborgs += producedAfterFight;
                currentCyborgs += producedAfterFight;
                // entferne leere troops
                troopsEnemy = troopsEnemy.Where(tro => tro.turnsToArrive > 0).ToList();
                troopsPlayer = troopsPlayer.Where(tro => tro.turnsToArrive > 0).ToList();

                maxTurns--;
                turn++;
            }
            return (result, turn);
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
                 troop.turnsToArrive,
                 troop.effectiveTargetFactory));
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

            // indirekte truppen die noch unterwegs sind
            var simulator = new Simulator(factories.FactoryList, troops, bombs, distances).SimulateMove();
            foreach (var futureFactory in simulator.FactoryIdsOfOwner(Owner.Opponent).Where(fac => fac.Distance(this) < distance))
            {
                int turnsNeededToConquer = simulator.TurnsToCaptureFactory(futureFactory);
                if (turnsNeededToConquer + futureFactory.Distance(this) < distance)
                    result += futureFactory.Cyborgs;
            }

            return result;
        }

        public UpdateAction Upgrade()
        {
            if (CyborgsToOffer() >= 10)
            {
                if (productionRate < 3)
                {
                    Cyborgs -= 10;
                    return new UpdateAction(this);
                }
            }
            return null;
        }

        public List<Factory> NeighborsOrderedByDistance(params Owner[] owner)
        {
            return floydWarshall.Neighbors(this, factories).Where(fac => owner.Count() == 0 || owner.Contains(fac.Owner)).OrderBy(fac => fac.Distance(this)).ToList();
        }

        public int WillFallIn()
        {
            return SimulateAttacks().fallIn;
        }
    }

    public class Simulator
    {
        private List<Factory> factories;
        private List<Troop> troops;
        private List<Bomb> bombs;
        private List<distanceStruct> distances;
        private Dictionary<Factory, int> turnsToCaptureFactory = new Dictionary<Factory, int>();

        public Simulator(List<Factory> factories, List<Troop> troops, List<Bomb> bombs, List<distanceStruct> distances)
        {
            this.factories = new List<Factory>(factories.Select(factory => new Factory(factory)));
            this.troops = new List<Troop>(troops.Select(troop => copyTroopWithNewRefs(troop)));
            this.bombs = bombs;
            this.distances = distances;
            foreach (var factory in factories)
            {
                turnsToCaptureFactory.Add(factory, 0);
            }
        }

        private Troop copyTroopWithNewRefs(Troop toCopy)
        {
            FactoriesHelper factoryiesHelper = new FactoriesHelper(factories);
            return new Troop(
                 toCopy.Owner,
                 factoryiesHelper.getFactoryByEntityId(toCopy.factoryFrom.Id),
                 factoryiesHelper.getFactoryByEntityId(toCopy.factoryTo.Id),
                 toCopy.numbersOfCyborgs,
                 toCopy.turnsToArrive,
                 toCopy.effectiveTargetFactory);
        }

        public Simulator SimulateMove()
        {
            while (troops.Count > 0)
            {
                // move
                foreach (var troop in troops)
                {
                    troop.turnsToArrive--;
                    turnsToCaptureFactory[troop.factoryTo]++;
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
            }
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

        public int TurnsToCaptureFactory(Factory factory)
        {
            return turnsToCaptureFactory[factory];
        }

        public List<Factory> FactoryIdsOfOwner(Owner owner)
        {
            return factories.Where(fac => fac.Owner == owner).ToList();
        }

    }

}
