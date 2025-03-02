﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Hourglass
{
    public class TabUpdate
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "";

        [JsonPropertyName("urls")]
        public List<string> Urls { get; set; } = new();
    }
}
