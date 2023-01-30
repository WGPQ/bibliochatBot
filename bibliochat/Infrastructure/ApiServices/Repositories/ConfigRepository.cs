using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using bibliochat.Common.Models;
using bibliochat.Services.BotConfig;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace bibliochat.Infrastructure.ApiServices.Repositories
{
    public class ConfigOperador : IConfigRepositori
    {
    private readonly string baseUrl; 
	public ConfigOperador(string baseUrl)
		{
            this.baseUrl = baseUrl;
        }

        public async Task<ConfiguracionEntity> GetDisponibilidad(string token)
        {
            var url = $"{baseUrl}/disponibilidad/Verificar";
            ConfiguracionEntity status = new ConfiguracionEntity();

            using (var client = new HttpClient())
            {

                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

                var resp = await client.GetAsync(url);

                if (resp.IsSuccessStatusCode)
                {
                    var apiResponse = resp.Content.ReadAsStringAsync().Result;
                    status = JsonConvert.DeserializeObject<ConfiguracionEntity>(apiResponse); ;

                }
            }

            return status;
        }
    }
}

