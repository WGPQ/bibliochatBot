using bibliochat.Common.Helpers;
using bibliochat.Common.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace bibliochat.Services.BotConfig.Repositories
{
    public class ClienteRepository: IClienteRepositori
    {
        private readonly string baseUrl;

        public ClienteRepository(string baseUrl)
        {
            this.baseUrl = baseUrl;
        }



        public async Task<ResultadoEntity> NewUser(UsuarioEntity cliente, string token)
        {
            var url = $"{baseUrl}/usuario/Insertar";
            var result = new ResultadoEntity();

            if (cliente != null)
            {

                cliente.id_rol = Encript64.EncryptString("4");

                using (var client = new HttpClient())
                {
                    StringContent content = new StringContent(JsonConvert.SerializeObject(cliente), Encoding.UTF8, "application/json");
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

                    var resp = await client.PostAsync(url, content);

                    if (resp.IsSuccessStatusCode)
                    {
                        var apiResponse = resp.Content.ReadAsStringAsync().Result;

                        result = JsonConvert.DeserializeObject<ResultadoEntity>(apiResponse);

                    }
                }
            }


            return result;
        }
    }
}
