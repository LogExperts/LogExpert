using Newtonsoft.Json.Linq;

namespace LogExpert.Core.Classes.IPC
{
    public class IpcMessage
    {
        public int Version { get; set; }

        public IpcMessageType Type { get; set; } = IpcMessageType.Load;

        public JObject Payload { get; set; } = [];
    }
}
