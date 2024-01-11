using System.IO.Ports;
using System.Threading;
using Microsoft.Extensions.Logging;
using nanoFramework.Logging;

namespace BluetoothBeacon
{
    internal class HealthMonitor
    {
        private ILogger _logger;
        private Timer _timer;
        private const int HealthUpdatePeriodMs = 5000;

        public HealthMonitor()
        {
            _logger = this.GetCurrentClassLogger();
        }

        public void Start()
        {
            _timer = new Timer(Callback, this, HealthUpdatePeriodMs, HealthUpdatePeriodMs);
        }

        private void Callback(object state)
        {
            _logger.LogInformation("Healthy");
        }

        //private void RunThread()
        //{
        //    _logger.LogInformation("Starting Health Monitor");

        //    while (true)
        //    {
        //        _logger.LogDebug("Healthy");
        //        Thread.Sleep(HealthUpdatePeriodMs);
        //    }
        //}
    }
}
