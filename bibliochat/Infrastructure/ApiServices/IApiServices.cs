﻿using bibliochat.Common.Models;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace bibliochat.Services.BotConfig
{
    public interface IApiServices<T> where T : new()
    {
        Task<FracesEntiti> Frace(string intencion, string token);
    }
    public interface IFracesRepositori : IApiServices<FracesEntiti> { }

    public interface IAutentication<R, U> where U : new()
    {
        Task<R> Auth(string email);
        Task<U> VerificationToken(string token);

    }
    public interface IAutenticationRepositori : IAutentication<LoginResponse, UserVerificadoEntity> { }
    public interface IChat<T> where T : new()
    {
        Task<T> Interaction(InteractionEntity interaction, string token);
        Task<bool> NewMessage(NewMessageEntity message, string token);
    }
    public interface IChatRepositori : IChat<ChatEntity> { }

    public interface ICliente<T, C, R> where T : new()
    {
        Task<R> NewUser(UsuarioEntity cliente, string token);

    }
    public interface IClienteRepositori : ICliente<ClienteEntity, ClienteVerificado, ResultadoEntity> { }
    public interface IStore
    {
        Task<(JObject content, string etag)> LoadAsync(string key);

        Task<bool> SaveAsync(string key, JObject content, string etag);
    }
    public interface IStoreRepositori : IStore { }
    public interface ISemillero<T> where T : new()
    {
        Task<List<T>> GetCollections();

    }
    public interface ISemilleroRepositori : ISemillero<SetsModels> { }

}
