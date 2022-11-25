using bibliochat.Common.Cards;
using bibliochat.Common.Helpers;
using bibliochat.Common.Models;
using bibliochat.Common.Models.BotState;
using bibliochat.Services.BotConfig;
using bibliochat.Services.LuisAi;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.bibliochat.QnA;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using bibliochat.Bots;

namespace bibliochat.Dialogs.Authenticate
{
    public class AuthUserDialog : ComponentDialog

    {
        public readonly ILuisAIService _luisAIServices;
        private readonly IDataservices _dataservices;
        public readonly IQnAMakerServices _qnmakerService;
       //public static UserVerificadoEntity userData = new UserVerificadoEntity();
        //public static ChatEntity chat;
        private static string EMAIL_USER_PROMPT = "EMAIL_USER_PROMPT";
        private string WATER_fULL_STEP_REGISTER = "REGISTER_USER";
        private string WATER_fULL_STEP_AUTH = "WATER_fULL_STEP_AUTH";
        private readonly IStatePropertyAccessor<UserVerificadoEntity> _dataBotState;
        private readonly IStatePropertyAccessor<UserVerificadoEntity> _dataUserState;
        private readonly IStatePropertyAccessor<AuthStateModel> _authUserState;
        private readonly IStatePropertyAccessor<ChatEntity> _chatState;




        public AuthUserDialog(IDataservices dataservices, UserState authUserState, UserState dataBotState, UserState dataUserState, UserState chatState, IQnAMakerServices qnmakerService, ILuisAIService luisAIServices) : base(nameof(AuthUserDialog))
        {
            _luisAIServices = luisAIServices;
            _dataservices = dataservices;
            _authUserState = authUserState.CreateProperty<AuthStateModel>(BibliochatBot<MainDialog>.authUser);
            _dataBotState = dataBotState.CreateProperty<UserVerificadoEntity>(BibliochatBot<MainDialog>.dataBot);
            _dataUserState = dataUserState.CreateProperty<UserVerificadoEntity>(BibliochatBot<MainDialog>.dataUser);
            _chatState = chatState.CreateProperty<ChatEntity>(BibliochatBot<MainDialog>.chatSession);
            _qnmakerService = qnmakerService;

            //AddDialog(new MenuDialog(dataservices, qnmakerService,luisAIServices));
            // USER AUTH
            var waterfullStepAuth = new WaterfallStep[]
            {
                //SolicitarEmail,
                ValidateUser,
                RegistrarCliente,
                FinalAuth

            };
            AddDialog(new WaterfallDialog(WATER_fULL_STEP_AUTH, waterfullStepAuth));
            AddDialog(new TextPrompt(EMAIL_USER_PROMPT, EmailValidator));
            AddDialog(new TextPrompt("texto"));
          
            InitialDialogId = WATER_fULL_STEP_AUTH;
        }

        private async Task<DialogTurnResult> SolicitarEmail(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var promptOptions = new PromptOptions
            {
                Prompt = MessageFactory.Text("Por favor ingresa tu correo electrónico", "soicitar correo", InputHints.ExpectingInput),
                RetryPrompt = MessageFactory.Text("Debe ingresar un correo valido"),
            };

            return await stepContext.PromptAsync(EMAIL_USER_PROMPT, promptOptions, cancellationToken);
        }


        private async Task<DialogTurnResult> ValidateUser(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var dataBotState = await _dataBotState.GetAsync(stepContext.Context, () => new UserVerificadoEntity(), cancellationToken);
            var dataUserState = await _dataUserState.GetAsync(stepContext.Context, () => new UserVerificadoEntity(),cancellationToken);
            var chatState = await _chatState.GetAsync(stepContext.Context, () => new ChatEntity(),cancellationToken);
            try
            {
                string correo = stepContext.Context.Activity.Text?.Trim();
                LoginResponse resp=await _dataservices.AuthRepositori.Auth(correo);
                if (resp.exito)
                {
                    UserVerificadoEntity userData = await _dataservices.AuthRepositori.VerificationToken(resp.token);
                    dataUserState.token = userData.token;
                    dataUserState.usuario = userData.usuario;
                    var authUserState = await _authUserState.GetAsync(stepContext.Context, () => new AuthStateModel());
                    if (userData.token != null)
                    {
                        authUserState.session = resp.id_session;
                        ChatEntity chat = await getChat(userData, dataBotState.usuario);
                        chatState.id_chat = chat.id_chat;
                        var fraceEntity = await _dataservices.FracesRepositori.Frace("Bienvenida", dataBotState.token);
                        string wecome = fraceEntity.frace.Replace("#user", userData.usuario.nombre_completo);
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text(wecome), cancellationToken);
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text("¿En que te puedo ayudar ?"), cancellationToken: cancellationToken);

                        return await stepContext.NextAsync(cancellationToken: cancellationToken);

                    }
                  

                }
                else
                {
                    dataUserState.usuario = new UsuarioEntity();
                    dataUserState.usuario.correo = correo;

                     //await stepContext.Context.SendActivityAsync("Este correo no se encuentra registrado como una cuenta", cancellationToken:cancellationToken);
                    var promptOptions = new PromptOptions
                    {
                        Prompt = MessageFactory.Text("Ahora igrese su nombre", "soicitar nombre", InputHints.ExpectingInput),
                    };

                    return await stepContext.PromptAsync("texto", promptOptions, cancellationToken);
 
                }


            }
            catch (Exception e)
            {
                await stepContext.Context.SendActivityAsync(e.Message.ToString(), cancellationToken: cancellationToken);
            }
            return await stepContext.NextAsync(cancellationToken: cancellationToken);

        }

        

       
        private async Task<DialogTurnResult> RegistrarCliente(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var dataBotState = await _dataBotState.GetAsync(stepContext.Context, () => new UserVerificadoEntity(), cancellationToken);
            var authUserState = await _authUserState.GetAsync(stepContext.Context, () => new AuthStateModel(), cancellationToken);
            var dataUserState = await _dataUserState.GetAsync(stepContext.Context, () => new UserVerificadoEntity(), cancellationToken);
            var chatState = await _chatState.GetAsync(stepContext.Context, () => new ChatEntity(), cancellationToken);
            if (authUserState.session==null)
            {
                dataUserState.usuario.nombres = stepContext.Context.Activity.Text?.Trim();
                var respuesta = await _dataservices.ClienteRepositori.NewUser(dataUserState.usuario, dataBotState.token);
                if (respuesta.exito)
                {
                    LoginResponse resp = await _dataservices.AuthRepositori.Auth(dataUserState.usuario.correo);
                    if (resp.exito)
                    {
                        UserVerificadoEntity userData = await _dataservices.AuthRepositori.VerificationToken(resp.token);
                        authUserState.session = resp.id_session;
                        ChatEntity chat = await getChat(userData, dataBotState.usuario);
                        chatState.id_chat = chat.id_chat;
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Ok {userData.usuario.nombre_completo} Empecemos!!"), cancellationToken);
                        return await stepContext.NextAsync(cancellationToken: cancellationToken);
                    }
                   
                }
                else
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("Lo siento no se ha podido iniciar esta conversacion 😢"), cancellationToken);
                    return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                }

            }

            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }

      

        private async Task<DialogTurnResult> FinalAuth(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await Task.Delay(1000, cancellationToken);
            await stepContext.Context.SendActivityAsync("¿En qué puedo ayudarte?", cancellationToken: cancellationToken);
           return await stepContext.ContinueDialogAsync(cancellationToken: cancellationToken);
        }



        private Task<bool> EmailValidator(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            bool isOk = IsValidEmail(promptContext.Recognized.Value);

            return Task.FromResult(promptContext.Recognized.Succeeded && isOk);
        }
        static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
        private async Task<ChatEntity> getChat(UserVerificadoEntity user, UsuarioEntity bot)
        {
            var interaction = new InteractionEntity
            {
                usuario_created = user.usuario.id,
                usuario_interacted = bot.id
            };
            return await _dataservices.ChatRepositori.Interaction(interaction, user.token);
        }

    }
}
