using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PatcherServer.Models
{
    public class Patcher
    {
        public int Id { get; set; }
        public string Sha256 { get; set; }
        public string DeltaLink { get; set; }
        public bool isPatchZ { get; set; }

    }
}