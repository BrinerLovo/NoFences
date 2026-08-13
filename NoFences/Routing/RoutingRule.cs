using System;
using System.Collections.Generic;

namespace NoFences.Routing
{
    public sealed class RoutingRule
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "New rule";
        public string SourceFolder { get; set; }
        public List<string> Extensions { get; set; } = new List<string>();
        public Guid DestinationFenceId { get; set; }
        public bool Enabled { get; set; } = true;
    }
}
