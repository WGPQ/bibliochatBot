using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace bibliochat.Common.Models
{
    public class NewMessageEntity
    {
        public string id_usuario { get; set; }
        public string id_session { get; set; }
        public string id_chat { get; set; }
        public string contenido { get; set; }
        public string answerBy { get; set; }
    }
}
