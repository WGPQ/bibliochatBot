using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace bibliochat.Common.Models
{
    public class ChatEntity
    {
        public string id_chat { get; set; }
        public static ChatEntity fromJson(string json) => JsonConvert.DeserializeObject<ChatEntity>(json);

    }
}
