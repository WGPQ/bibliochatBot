// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio EchoBot v4.16.0

using AutoMapper;
using bibliochat.CommandHandling;
using bibliochat.Common.Cards;
using bibliochat.Common.Models;
using bibliochat.Common.Models.BotState;
using bibliochat.Dialogs.Authenticate;
using bibliochat.RecursosBot;
using bibliochat.Services.BotConfig;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace bibliochat.Bots
{
    public class BibliochatBot<T> : ActivityHandler where T : Dialog
    {
        public const string WelcomeText = @"Para poder acceder al servcio debe eligir cual sera su modo de ingreso, para obtener mas informacion hacerca del modo de ingreso viste https://wwww.mododeingreso.com ";
        private readonly IDataservices _dataservices;
        protected readonly BotState _conversationState;
        protected readonly BotState _userState;
        protected readonly Dialog _dialog;
        private readonly IStore _store;
        public static string authUser = "authUser";
        public static string authBot = "authBot";
        public static string dataBot = "dataBot";
        public static string dataUser = "dataUser";
        public static string chatSession = "chatSession";
        private readonly IStatePropertyAccessor<AuthStateModel> _authClientState;
        public readonly IStatePropertyAccessor<AuthStateModel> _authBotState;
        private readonly IStatePropertyAccessor<UserVerificadoEntity> _dataUserState;
        private readonly IStatePropertyAccessor<UserVerificadoEntity> _dataBotState;
        private readonly IStatePropertyAccessor<ChatEntity> _chatState;
        private const string SampleUrl = "https://github.com/tompaana/intermediator-bot-sample";


        public BibliochatBot(ConversationState conversationState, UserState userState, UserState botState, UserState dataUserState, UserState dataBotState, UserState clientState, UserState chatState, T dialog, IDataservices dataservices, IStore store)

        {
            _conversationState = conversationState;
            _authClientState = clientState.CreateProperty<AuthStateModel>(authUser);
            _authBotState = botState.CreateProperty<AuthStateModel>(authBot);
            _dataUserState = dataUserState.CreateProperty<UserVerificadoEntity>(dataUser);
            _dataBotState = dataBotState.CreateProperty<UserVerificadoEntity>(dataBot);
            _chatState = chatState.CreateProperty<ChatEntity>(chatSession);
            _userState = userState;
            _dialog = dialog;
            _dataservices = dataservices;
            _store = store;
        }
        //protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        //{
        //    var replyText = $"Echo: {turnContext.Activity.Text}";
        //    await turnContext.SendActivityAsync(MessageFactory.Text(replyText, replyText), cancellationToken);
        //}

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            //var welcomeText = "Hello and welcome!";
            Command showOptionsCommand = new Command(Commands.ShowOptions);
            HeroCard heroCard = new HeroCard()
            {
                Title = "Hello!",
                Subtitle = "I am Intermediator Bot",
                Text = $"My purpose is to serve as a sample on how to implement the human hand-off. Click/tap the button below or type \"{new Command(Commands.ShowOptions).ToString()}\" to see all possible commands. To learn more visit <a href=\"{SampleUrl}\">{SampleUrl}</a>.",
                Buttons = new List<CardAction>()
                {
                    new CardAction()
                    {
                        Title = "Show options",
                        Value = showOptionsCommand.ToString(),
                        Type = ActionTypes.ImBack
                    }
                }
            };
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {

                    //var optionsAttachments = new List<Attachment>() { heroCard.ToAttachment() };
                    //await turnContext.SendActivityAsync(MessageFactory.Attachment(optionsAttachments));
                    LoginResponse resp = await _dataservices.AuthRepositori.Auth("bibliochatutn@outlook.com");
                    //_bot
                    if (resp.exito)
                    {
                        UserVerificadoEntity _bot = await _dataservices.AuthRepositori.VerificationToken(resp.token);
                        var authBotState = await _authBotState.GetAsync(turnContext, () => new AuthStateModel());
                        var dataBotState = await _dataBotState.GetAsync(turnContext, () => new UserVerificadoEntity());
                        dataBotState.token = _bot.token;
                        dataBotState.usuario = _bot.usuario;

                        var fraceEntity = await _dataservices.FracesRepositori.Frace("Introduccion", _bot.token);
                        if (fraceEntity != null)
                        {
                            // await OnBoarding.ToShow(fraceEntity.frace, turnContext, cancellationToken);
                            await turnContext.SendActivityAsync(fraceEntity.frace, cancellationToken: cancellationToken);
                            //await _dialog.RunAsync(turnContext,_conversationState.CreateProperty<DialogState>(nameof(DialogState)),cancellationToken);
                        }
                    }

                }
            }
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            await base.OnTurnAsync(turnContext, cancellationToken);
            await _conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await _userState.SaveChangesAsync(turnContext, false, cancellationToken);

        }
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {

            var key = $"{turnContext.Activity.ChannelId}/conversations/{turnContext.Activity.Conversation?.Id}";
            //turnContext.Activity.From.Name = "William";
            //turnContext.Activity.From.Properties = 
            var authUserState = await _authClientState.GetAsync(turnContext, () => new AuthStateModel());
            var dataBotState = await _dataBotState.GetAsync(turnContext, () => new UserVerificadoEntity());



            if (authUserState.session != null)
            {
                var dataUserState = await _dataUserState.GetAsync(turnContext, () => new UserVerificadoEntity());
                var chatState = await _chatState.GetAsync(turnContext, () => new ChatEntity());
                NewMessageEntity messageUser = new NewMessageEntity();
                messageUser.id_usuario = dataUserState.usuario.id;
                messageUser.id_session = authUserState.session;
                messageUser.contenido = turnContext.Activity.Text;
                messageUser.answerBy = dataUserState.usuario.id;
                messageUser.id_chat = chatState.id_chat;

                bool resp = await _dataservices.ChatRepositori.NewMessage(messageUser, dataUserState.token);



                // The execution sits in a loop because there might be a retry if the save operation fails.
                while (true)
                {
                    // Load any existing state associated with this key
                    var (oldState, etag) = await _store.LoadAsync(key);

                    // Run the dialog system with the old state and inbound activity, the result is a new state and outbound activities.
                    var (activities, newState) = await DialogHost.RunAsync(_dialog, turnContext.Activity, oldState, cancellationToken);

                    // Save the updated state associated with this key.
                    var success = await _store.SaveAsync(key, newState, etag);
                    /*
                       idUsuario
                       idSession
                       tokenUser
                       idChat
                       content
                       
                        */
                    //NewMessageEntity messageBot = new NewMessageEntity();
                    //messageBot.contenido = activities.First(a =>a.Type=="message").Text;
                    //messageBot.id_chat = AuthUserDialog.chat.id_chat;
                    //messageBot.id_session = "";
                    //var traceLuis = activities.First(a => a.Type == "trace").Value;
                    //var result = ((IEnumerable)traceLuis).Cast<object>().ToList();
                    //var luisResult = ((IEnumerable)result.FirstOrDefault()).Cast<object>().ToList();
                    //RecognizerResult luis = JsonConvert.DeserializeObject<RecognizerResult>(luisResult.FirstOrDefault().ToString());
                    //var topIntet = luis.GetTopScoringIntent();
                    //if ( topIntet.intent== "None")
                    //{
                    //    var traceQnM = activities.First(a => a.Type == "trace" && a.Name== "QnAMaker").Value;
                    //}
                    //else
                    //{
                    //    messageBot.answereBy = activities.First(a => a.Type == "trace").Name;
                    //}

                    //messageBot.id_usuario = "id bot";
                    NewMessageEntity messageBot = new NewMessageEntity();
                    foreach (var activity in activities)
                    {
                        if (activity.Text != null)
                        {
                            messageBot.contenido = activity.Text;
                            messageBot.id_chat = chatState.id_chat;
                            messageBot.id_session = authUserState.session;
                            messageBot.answerBy = dataBotState.usuario.id;
                            messageBot.id_usuario = dataBotState.usuario.id;
                            bool saveMessage = await _dataservices.ChatRepositori.NewMessage(messageBot, dataBotState.token);

                        }
                    }


                    // Following a successful save, send any outbound Activities, otherwise retry everything.
                    if (success)
                    {
                        //await  _dataservices.ChatRepositori.
                        if (activities.Any())
                        {
                            // This is an actual send on the TurnContext we were given and so will actual do a send this time.
                            //await turnContext.SendActivitiesAsync(activities, cancellationToken);

                        }

                        break;
                    }
                }
            }
            await _dialog.RunAsync(
              turnContext,
              _conversationState.CreateProperty<DialogState>(nameof(DialogState)),
              cancellationToken
            );
        }
    }
}
