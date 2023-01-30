using System;
using Newtonsoft.Json;

namespace bibliochat.Common.Models
{
	public class ConfiguracionEntity
	{

		public bool disponibilidad { get; set; }
        public static ConfiguracionEntity fromJson(string json) => JsonConvert.DeserializeObject<ConfiguracionEntity>(json);

    }
}

