using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace bibliochat.ConversationHistory
{
    public class MessageLogEntity : TableEntity
    {
        public string Body
        {
            get;
            set;
        }
    }
}
