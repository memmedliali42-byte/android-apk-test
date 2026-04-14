using UnityEngine;

namespace InternalDebugMenu
{
    public sealed class SampleNetworkSimulationService : NetworkSimulationServiceBase
    {
        private int latencyMs;
        private int packetLossPercent;
        private bool syncLogsEnabled;

        public override void SetLatencyMs(int latencyMs)
        {
            this.latencyMs = latencyMs;
            Debug.Log($"SampleNetworkSimulationService: Latency set to {this.latencyMs} ms");
        }

        public override void SetPacketLossPercent(int packetLossPercent)
        {
            this.packetLossPercent = packetLossPercent;
            Debug.Log($"SampleNetworkSimulationService: Packet loss set to {this.packetLossPercent}%");
        }

        public override void SetSyncDebugLogs(bool enabled)
        {
            syncLogsEnabled = enabled;
            Debug.Log($"SampleNetworkSimulationService: Sync logs {(syncLogsEnabled ? "enabled" : "disabled")}");
        }
    }
}
