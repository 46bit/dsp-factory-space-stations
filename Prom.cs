using Prometheus;
using System.Collections.Generic;

namespace DSPMetricExporter
{
    public class Prom
    {
        private static readonly Counter Updates = Metrics.CreateCounter("updates", "Counter of update calls");
        private static readonly Counter GameTicks = Metrics.CreateCounter("game_ticks", "Counter of game ticks");
        private static readonly Gauge Stations = Metrics.CreateGauge("stations_total", "Number of stations, by planet", new GaugeConfiguration { LabelNames = new[] { "planetDisplayName" } });
        private static readonly Gauge Deposits = Metrics.CreateGauge("deposits_total", "Number of ore deposits, by planet", new GaugeConfiguration { LabelNames = new[] { "planetDisplayName", "ore" } });
        private static readonly Gauge UnoccupiedDeposits = Metrics.CreateGauge("deposits_unoccupied", "Number of unoccupied ore deposits, by planet", new GaugeConfiguration { LabelNames = new[] { "planetDisplayName", "ore" } });

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
                        if (planetFactory.transport.stationPool[i] == null || planetFactory.transport.stationPool[i].id != i)
                        {
                            continue;
                        }
                        numberOfTransportStations += 1;
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

                    Dictionary<EVeinType, int> deposits = new Dictionary<EVeinType, int>();
                    Dictionary<EVeinType, int> unoccupiedDeposits = new Dictionary<EVeinType, int>();
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

                        deposits.TryGetValue(veinGroup[0].type, out var depositCount);
                        deposits[veinGroup[0].type] = depositCount + 1;
                        if (!occupied)
                        {
                            unoccupiedDeposits.TryGetValue(veinGroup[0].type, out var unoccupiedDepositCount);
                            unoccupiedDeposits[veinGroup[0].type] = unoccupiedDepositCount + 1;
                        }
                    }

                    foreach (KeyValuePair<EVeinType, int> entry in deposits)
                    {
                        Deposits.WithLabels(planetFactory.planet.displayName, entry.Key.ToString()).Set(entry.Value);
                    }
                    foreach (KeyValuePair<EVeinType, int> entry in unoccupiedDeposits)
                    {
                        UnoccupiedDeposits.WithLabels(planetFactory.planet.displayName, entry.Key.ToString()).Set(entry.Value);
                    }
                }
            }
        }
    }
}
