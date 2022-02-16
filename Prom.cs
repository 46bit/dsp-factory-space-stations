using Prometheus;

namespace DSPMetricExporter
{
    public class Prom
    {
        private static readonly Counter Updates = Metrics.CreateCounter("updates", "Counter of game ticks");

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
    }
}
