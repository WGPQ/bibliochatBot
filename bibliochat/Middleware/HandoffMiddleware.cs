using bibliochat.Bots;
using bibliochat.CommandHandling;
using bibliochat.Common.Models;
using bibliochat.Common.Models.BotState;
using bibliochat.ConversationHistory;
using bibliochat.Dialogs;
using bibliochat.MessageRouting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Underscore.Bot.MessageRouting;
using Underscore.Bot.MessageRouting.DataStore;
using Underscore.Bot.MessageRouting.DataStore.Azure;
using Underscore.Bot.MessageRouting.DataStore.Local;
using Underscore.Bot.MessageRouting.Results;

namespace bibliochat.Middleware
{
    public class HandoffMiddleware : IMiddleware
    {
        private const string KeyAzureTableStorageConnectionString = "AzureTableStorageConnectionString";
        private const string KeyRejectConnectionRequestIfNoAggregationChannel = "RejectConnectionRequestIfNoAggregationChannel";
        private const string KeyPermittedAggregationChannels = "PermittedAggregationChannels";
        private const string KeyNoDirectConversationsWithChannels = "NoDirectConversationsWithChannels";
        private readonly IStatePropertyAccessor<AuthStateModel> _userState;
        private readonly IStatePropertyAccessor<DisponibilidadModel> _disponibilidad;
        private readonly IStatePropertyAccessor<UserVerificadoEntity> _dataUserState;
        private bool isSend = false;
        public IConfiguration Configuration
        {
            get;
            protected set;
        }

        public MessageRouter MessageRouter
        {
            get;
            protected set;
        }

        public MessageRouterResultHandler MessageRouterResultHandler
        {
            get;
            protected set;
        }

        public CommandHandler CommandHandler
        {
            get;
            protected set;
        }

        public MessageLogs MessageLogs
        {
            get;
            protected set;
        }


        public HandoffMiddleware(IConfiguration configuration, UserState userState, UserState dataUserState,UserState disponibilidadState)
        {
            Configuration = configuration;
            string connectionString = Configuration[KeyAzureTableStorageConnectionString];
            IRoutingDataStore routingDataStore = null;
            _dataUserState = dataUserState.CreateProperty<UserVerificadoEntity>(BibliochatBot<MainDialog>.dataUser);
            _disponibilidad = disponibilidadState.CreateProperty<DisponibilidadModel>(BibliochatBot<MainDialog>.disponibilidaKey);
            _userState = userState.CreateProperty<AuthStateModel>(BibliochatBot<MainDialog>.authUser);
            if (string.IsNullOrEmpty(connectionString))
            {
                System.Diagnostics.Debug.WriteLine($"WARNING!!! No connection string found - using {nameof(InMemoryRoutingDataStore)}");
                routingDataStore = new InMemoryRoutingDataStore();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Found a connection string - using {nameof(AzureTableRoutingDataStore)}");
                routingDataStore = new AzureTableRoutingDataStore(connectionString);
            }

            MessageRouter = new MessageRouter(
                routingDataStore,
                new MicrosoftAppCredentials(Configuration["MicrosoftAppId"], Configuration["MicrosoftAppPassword"]));

            MessageRouter.Logger = new Logging.AggregationChannelLogger(MessageRouter);

            MessageRouterResultHandler = new MessageRouterResultHandler(MessageRouter);

            ConnectionRequestHandler connectionRequestHandler =
                new ConnectionRequestHandler(GetChannelList(KeyNoDirectConversationsWithChannels));

            CommandHandler = new CommandHandler(
                MessageRouter,
                MessageRouterResultHandler,
                connectionRequestHandler,
                GetChannelList(KeyPermittedAggregationChannels));

            MessageLogs = new MessageLogs(connectionString);

        }
        public async Task OnTurnAsync(ITurnContext context, NextDelegate next, CancellationToken ct)
        {
            var userStateData = await _userState.GetAsync(context, () => new AuthStateModel(),ct);
            var disponoibilidadState = new DisponibilidadModel();
            if (!isSend) { 
             disponoibilidadState = await _disponibilidad.GetAsync(context, () => new DisponibilidadModel(), ct);
	         }
            var dataUserState = await _dataUserState.GetAsync(context, () => new UserVerificadoEntity(), ct);
            Activity activity = context.Activity;

            if (activity.Type is ActivityTypes.Message && userStateData.session != null)
            {



                _ = bool.TryParse(
                Configuration[KeyRejectConnectionRequestIfNoAggregationChannel],
                out bool rejectConnectionRequestIfNoAggregationChannel);

                // Store the conversation references (identities of the sender and the recipient [bot])
                // in the activity
                MessageRouter.StoreConversationReferences(activity);

                AbstractMessageRouterResult messageRouterResult = null;

                // Check the activity for commands
                if (await CommandHandler.HandleCommandAsync(context) == false)
                {
                    // No command detected/handled

                    // Let the message router route the activity, if the sender is connected with
                    // another user/bot
                    messageRouterResult = await MessageRouter.RouteMessageIfSenderIsConnectedAsync(activity);

                    if (messageRouterResult is MessageRoutingResult
                        && (messageRouterResult as MessageRoutingResult).Type == MessageRoutingResultType.NoActionTaken)
                    {
                        // No action was taken by the message router. This means that the user
                        // is not connected (in a 1:1 conversation) with a human
                        // (e.g. customer service agent) yet.

                        // Check for cry for help (agent assistance)
                        if (!string.IsNullOrWhiteSpace(activity.Text) && activity.Text.ToLower().Contains("human"))
                        //if (disponoibilidadState.disponible && !disponoibilidadState.solicitudEnviada && dataUserState.usuario.rol=="Cliente")
                        {
                            disponoibilidadState.solicitudEnviada = true;
                            isSend = true;
                            //activity.From.Properties = JObject.FromObject(dataUserState.usuario);
                            //activity.From.Name = dataUserState.usuario.nombre_completo;
                            //JObject obj = JObject.FromObject(dataUserState.usuario);
                            //activity.From.Properties = obj;
                            //activity.From.AadObjectId = chat;
                            //if (userStateData.IsAutenticate) { 
                            // Create a connection request on behalf of the sender
                            // Note that the returned result must be handled
                            messageRouterResult = MessageRouter.CreateConnectionRequest(MessageRouter.CreateSenderConversationReference(activity),rejectConnectionRequestIfNoAggregationChannel);
                            //}
                        }
                        else
                        {
                            // No action taken - this middleware did not consume the activity so let it propagate
                            await next(ct).ConfigureAwait(false);
                        }
                    }
                }

                // Uncomment to see the result in a reply (may be useful for debugging)
                //if (messageRouterResult != null)
                //{
                //    await MessageRouter.ReplyToActivityAsync(activity, messageRouterResult.ToString());
                //}

                // Handle the result, if necessary
                await MessageRouterResultHandler.HandleResultAsync(messageRouterResult);
                
            }
            else
            {
                // No action taken - this middleware did not consume the activity so let it propagate
                await next(ct).ConfigureAwait(false);
            }
        }
        /// <summary>
        /// Extracts the channel list from the settings matching the given key.
        /// </summary>
        /// <returns>The list of channels or null, if none found.</returns>
        private IList<string> GetChannelList(string key)
        {
            IList<string> channelList = null;

            string channels = Configuration[key];

            if (!string.IsNullOrWhiteSpace(channels))
            {
                System.Diagnostics.Debug.WriteLine($"Channels by key \"{key}\": {channels}");
                string[] channelArray = channels.Split(',');

                if (channelArray.Length > 0)
                {
                    channelList = new List<string>();

                    foreach (string channel in channelArray)
                    {
                        channelList.Add(channel.Trim());
                    }
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"No channels defined by key \"{key}\" in app settings");
            }

            return channelList;
        }
    }
}
