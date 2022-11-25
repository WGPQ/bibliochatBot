using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace bibliochat.Common.Models
{
    public class ClienteVerificado
    {
        public string token { get; set; }
        public ClienteEntity cliente { get; set; }
        public static ClienteVerificado fromJson(string json) => JsonConvert.DeserializeObject<ClienteVerificado>(json);
    }
}
