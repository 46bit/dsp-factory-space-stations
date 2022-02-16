using Prometheus;
using System.Collections.Generic;

namespace DSPMetricExporter
{
    public class Prom
    {
        private static readonly Counter Updates = Metrics.CreateCounter("updates", "Counter of update calls");
        private static readonly Counter GameTicks = Metrics.CreateCounter("game_ticks", "Counter of game ticks");
        private static readonly Gauge Stations = Metrics.CreateGauge("stations_total", "Number of stations, by planet", new GaugeConfiguration { LabelNames = new[] { "planetDisplayName" } });
        private static readonly Gauge Items = Metrics.CreateGauge("items_buffered", "Number of items in logistics towers", new GaugeConfiguration { LabelNames = new[] { "itemId" } });
        private static readonly Gauge Deposits = Metrics.CreateGauge("deposits_total", "Number of ore deposits, by solar system", new GaugeConfiguration { LabelNames = new[] { "star", "ore" } });
        private static readonly Gauge UnoccupiedDeposits = Metrics.CreateGauge("deposits_unoccupied", "Number of unoccupied ore deposits, by solar system", new GaugeConfiguration { LabelNames = new[] { "star", "ore" } });

        private MetricServer prometheusServer;

        public Prom()
        {
            prometheusServer = new MetricServer(port: 4646);
            prometheusServer.Start();
        }

        public void Update()
        {
            Updates.Inc();
        }

        public void GameTick(long time, GameData gameData)
        {
            GameTicks.Inc();

            Dictionary<int, int> items = new Dictionary<int, int>();

            Dictionary<(StarData, EVeinType), int> deposits = new Dictionary<(StarData, EVeinType), int>();
            Dictionary<(StarData, EVeinType), int> unoccupiedDeposits = new Dictionary<(StarData, EVeinType), int>();

            foreach (var planetFactory in gameData.factories)
            {
                if (planetFactory == null)
                {
                    continue;
                }

                // FIXME: Remove old entries
                if (planetFactory.transport != null) {
                    long numberOfTransportStations = 0;
                    for (int i = 1; i < planetFactory.transport.stationCursor; i++)
                    {
                        var station = planetFactory.transport.stationPool[i];
                        if (station == null || station.id != i)
                        {
                            continue;
                        }

                        foreach(var store in station.storage)
                        {
                            items.TryGetValue(store.itemId, out var itemCount);
                            items[store.itemId] = itemCount + store.count;
                        }

                        numberOfTransportStations++;
                    }
                    Stations.WithLabels(planetFactory.planet.displayName).Set(numberOfTransportStations);
                }

                // FIXME: Remove old entries
                if (planetFactory.veinPool != null)
                {
                    // Group veins into groups of touching veins ("deposits")
                    // Then count how many deposits exist and how many are unoccupied (no miners anywhere on the deposit)
                    // Then file as Prometheus metrics

                    Dictionary<int, List<VeinData>> veinGroups = new Dictionary<int, List<VeinData>>();
                    for (int i = 1; i < planetFactory.veinCursor; i++)
                    {
                        VeinData vein = planetFactory.veinPool[i];
                        if (vein.id != i)
                        {
                            continue;
                        }
                        if (!veinGroups.ContainsKey(vein.groupIndex))
                        {
                            veinGroups[vein.groupIndex] = new List<VeinData>();
                        }
                        veinGroups[vein.groupIndex].Add(vein);
                    }

                    foreach (var veinGroup in veinGroups.Values)
                    {
                        if (veinGroup.Count == 0)
                        {
                            continue;
                        }

                        var occupied = false;
                        foreach (var vein in veinGroup)
                        {
                            if (vein.minerCount > 0)
                            {
                                occupied = true;
                                break;
                            }
                        }

                        var key = (planetFactory.planet.star, veinGroup[0].type);
                        deposits.TryGetValue(key, out var depositCount);
                        deposits[key] = depositCount + 1;
                        if (!occupied)
                        {
                            unoccupiedDeposits.TryGetValue(key, out var unoccupiedDepositCount);
                            unoccupiedDeposits[key] = unoccupiedDepositCount + 1;
                        }
                    }
                }
            }

            foreach (KeyValuePair<int, int> entry in items)
            {
                Items.WithLabels(entry.Key.ToString()).Set(entry.Value);
            }

            foreach (KeyValuePair<(StarData, EVeinType), int> entry in deposits)
            {
                Deposits.WithLabels(entry.Key.Item1.name, entry.Key.Item2.ToString()).Set(entry.Value);
            }
            foreach (KeyValuePair<(StarData, EVeinType), int> entry in unoccupiedDeposits)
            {
                UnoccupiedDeposits.WithLabels(entry.Key.Item1.name, entry.Key.Item2.ToString()).Set(entry.Value);
            }
        }
    }
}
