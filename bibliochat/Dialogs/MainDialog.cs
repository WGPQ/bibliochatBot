using bibliochat.Common.Cards;
using bibliochat.Common.Models;
using bibliochat.Common.Models.BotState;
using bibliochat.Dialogs.Authenticate;
using bibliochat.Services.BotConfig;
using bibliochat.Services.LuisAi;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.bibliochat.QnA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using bibliochat.Bots;

namespace bibliochat.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        public readonly ILuisAIService _luisAIServices;
        public readonly IQnAMakerServices _qnmakerService;
        private readonly IDataservices _dataservices;
        private readonly ILogger _logger;
        private static string INITIAL_WATERFALL = "INITIAL_WATERFALL_STEPS";
        //public static UserVerificadoEntity _bot = new UserVerificadoEntity();
        private readonly IStatePropertyAccessor<AuthStateModel> _authBotState;
        private readonly IStatePropertyAccessor<AuthStateModel> _authUserState;
        private readonly IStatePropertyAccessor<UserVerificadoEntity> _dataBotState;
        private readonly IStatePropertyAccessor<UserVerificadoEntity> _dataUserState;
        private readonly IStatePropertyAccessor<ChatEntity> _chatState;

        public MainDialog(ILuisAIService luisAIServices, UserState authBotState, UserState authUserState, UserState dataUserState, UserState dataBotState, UserState chatState, IQnAMakerServices qnmakerService, ILogger<MainDialog> logger, IDataservices dataservices) : base(nameof(MainDialog))
        {
            _luisAIServices = luisAIServices;
            _qnmakerService = qnmakerService;
            _logger = logger;
            _dataservices = dataservices;
            _authBotState = authBotState.CreateProperty<AuthStateModel>(BibliochatBot<MainDialog>.authBot);
            _authUserState = authUserState.CreateProperty<AuthStateModel>(BibliochatBot<MainDialog>.authUser);
            _dataBotState = dataBotState.CreateProperty<UserVerificadoEntity>(BibliochatBot<MainDialog>.dataBot);
            _dataUserState = dataUserState.CreateProperty<UserVerificadoEntity>(BibliochatBot<MainDialog>.dataUser);
            _chatState = chatState.CreateProperty<ChatEntity>(BibliochatBot<MainDialog>.chatSession);



            var waterfallSteps = new WaterfallStep[]
            {

                //IntroStepAsync,
                ActStepAsync,
                FinalStepAsync,
            };
            AddDialog(new WaterfallDialog(INITIAL_WATERFALL, waterfallSteps));
            AddDialog(new AuthUserDialog(dataservices, authUserState, dataBotState, dataUserState, chatState, qnmakerService, luisAIServices));
            AddDialog(new FinalizarDialog(authUserState, dataUserState));
            AddDialog(new CalificarDialog());
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new TextPrompt(nameof(TextPrompt)));


            InitialDialogId = INITIAL_WATERFALL;



        }



        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            //_bot = await _dataservices.AuthRepositori.Auth("bibliochatutn@outlook.com");
            //if (_bot.token != null)
            //{
            //    var botStateData = await _botState.GetAsync(stepContext.Context, () => new AuthStateModel());
            //}
            var dataBotState = await _dataBotState.GetAsync(stepContext.Context, () => new UserVerificadoEntity());

            var fraceEntity = await _dataservices.FracesRepositori.Frace("Bienvenida", dataBotState.token);
            if (fraceEntity != null)
            {

                //await OnBoarding.ToShow(fraceEntity.frace, stepContext, cancellationToken);
            }

            return await stepContext.BeginDialogAsync(nameof(AuthUserDialog), null, cancellationToken);
            //var weekLaterDate = DateTime.Now.AddDays(7).ToString("MMMM d, yyyy");
            //var messageText = stepContext.Options?.ToString() ?? $"What can I help you with today?\nSay something like \"Book a flight from Paris to Berlin on {weekLaterDate}\"";
            //var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
            //return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }




        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            var luisResult = await _luisAIServices._luisRecognizer.RecognizeAsync(stepContext.Context, cancellationToken);
            return await ManageIntentions(stepContext, luisResult, cancellationToken);
        }

        private async Task<DialogTurnResult> ManageIntentions(WaterfallStepContext stepContext, RecognizerResult luisResult, CancellationToken cancellationToken)
        {
            string frace = "";
            var userStateData = await _authUserState.GetAsync(stepContext.Context, () => new AuthStateModel());
            var dataBotState = await _dataBotState.GetAsync(stepContext.Context, () => new UserVerificadoEntity());
            var dataUserState = await _dataUserState.GetAsync(stepContext.Context, () => new UserVerificadoEntity());


            if (userStateData.session == null)
            {
                return await stepContext.BeginDialogAsync(nameof(AuthUserDialog), null, cancellationToken);
            }
            var (intent, score) = luisResult.GetTopScoringIntent();
            var fraceEntity = await _dataservices.FracesRepositori.Frace(intent, dataBotState.token);
            if (fraceEntity.frace != null)
            {
                frace = fraceEntity.frace.Replace("#user", dataUserState.usuario.nombre_completo);
            }

            switch (intent)
            {
                case "Iniciar":
                    return await IntentIniciar(stepContext, userStateData, luisResult, cancellationToken);
                case "Acerca":
                    await IntentAcerca(stepContext, luisResult, cancellationToken);
                    break;
                case "Saludar":
                    await IntentSaludar(stepContext, luisResult, frace, cancellationToken);
                    break;
                case "Opciones":
                    await IntentOpciones(stepContext, luisResult, cancellationToken);
                    break;
                case "Agradecer":
                    await IntentAgradecer(stepContext, luisResult, cancellationToken);
                    break;
                case "Calificar":
                    return await IntentCalificar(stepContext, luisResult, cancellationToken);
                case "Despedirse":
                    if (score > 0.6){
                        await IntentDespedirse(stepContext, luisResult, frace, cancellationToken);
                    } else{
                        await IntentNone(stepContext, luisResult, cancellationToken);
                    }
                    break;
                case "Ayuda":
                    await IntentAyuda(stepContext, luisResult, frace, cancellationToken);
                    break;
                case "None":
                    await IntentNone(stepContext, luisResult, cancellationToken);
                    break;
                default:
                    await IntentDefault(stepContext, luisResult, cancellationToken);
                    break;
            }
            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task IntentAyuda(WaterfallStepContext stepContext, RecognizerResult luisResult, string frace, CancellationToken cancellationToken)
        {
            await stepContext.Context.SendActivityAsync(frace, cancellationToken: cancellationToken);
        }

        private async Task IntentRepositorio(WaterfallStepContext stepContext, RecognizerResult luisResult, CancellationToken cancellationToken)
        {
            await stepContext.Context.SendActivityAsync("Este es nuestro catalogo", cancellationToken: cancellationToken);
            var resp = await _dataservices.SemilleroRepositori.GetCollections();
            await CardCatalogos.ToShowCatalogo(resp, stepContext, cancellationToken);
        }

        private async Task IntentOpciones(WaterfallStepContext stepContext, RecognizerResult luisResult, CancellationToken cancellationToken)
        {
            //IMessageActivity message = Activity.CreateMessageActivity();

            //await stepContext.Context.SendActivityAsync(message, cancellationToken);
            await stepContext.Context.SendActivityAsync($"Aquí tengo mis opciones", cancellationToken: cancellationToken);
            //await MenuOptions.ToShow(stepContext, cancellationToken);
            string menuBibliochat = "Recuerda que puedes realizar las siguientes acciones." +
                $"{Environment.NewLine} 1. Consultar dudas frecuentes." +
                $"{Environment.NewLine} 2. Calificar la interaccion.";
            await stepContext.Context.SendActivityAsync(menuBibliochat, cancellationToken: cancellationToken);

        }

        private async Task IntentDefault(WaterfallStepContext stepContext, RecognizerResult luisResult, CancellationToken cancellationToken)
        {
            await stepContext.Context.SendActivityAsync("Disculpa no entendi. ¿Puesdes escribirlo de otra manera?", cancellationToken: cancellationToken);

        }

        private async Task IntentDespedirse(WaterfallStepContext stepContext, RecognizerResult luisResult, string frace, CancellationToken cancellationToken)
        {
            var userStateData = await _authUserState.GetAsync(stepContext.Context, () => new AuthStateModel());
            userStateData.session = null;
            await stepContext.Context.SendActivityAsync(frace, cancellationToken: cancellationToken);

        }

        private async Task<DialogTurnResult> IntentIniciar(WaterfallStepContext stepContext, AuthStateModel userStateData, RecognizerResult luisResult, CancellationToken cancellationToken)
        {
            if (userStateData.session == null)
            {
                return await stepContext.BeginDialogAsync(nameof(AuthUserDialog), null, cancellationToken);
            }
            var dataUserState = await _dataUserState.GetAsync(stepContext.Context, () => new UserVerificadoEntity());
            await stepContext.Context.SendActivityAsync($"{dataUserState.usuario.nombre_completo} ya tienes una convresacion inicada 😅.", cancellationToken: cancellationToken);
            await stepContext.Context.SendActivityAsync("¿En qué más puedo ayudarte?", cancellationToken: cancellationToken);
            return await stepContext.ContinueDialogAsync(cancellationToken: cancellationToken);
        }



        private async Task IntentTerminos(WaterfallStepContext stepContext, RecognizerResult luisResult, CancellationToken cancellationToken)
        {
            await stepContext.Context.SendActivityAsync("Estos son nuestros terminos", cancellationToken: cancellationToken);
        }

        private async Task IntentAcerca(WaterfallStepContext stepContext, RecognizerResult luisResult, CancellationToken cancellationToken)
        {
            await stepContext.Context.SendActivityAsync("Esto es Acerca de:", cancellationToken: cancellationToken);

        }

        private async Task IntentSaludar(WaterfallStepContext stepContext, RecognizerResult luisResult, string frace, CancellationToken cancellationToken)
        {
            await stepContext.Context.SendActivityAsync(frace, cancellationToken: cancellationToken);

        }


        private async Task IntentAgradecer(WaterfallStepContext stepContext, RecognizerResult luisResult, CancellationToken cancellationToken)
        {
            await stepContext.Context.SendActivityAsync("No te preocupes me gusta ayudar.", cancellationToken: cancellationToken);

        }

        private async Task<DialogTurnResult> IntentCalificar(WaterfallStepContext stepContext, RecognizerResult luisResult, CancellationToken cancellationToken)
        {
            return await stepContext.BeginDialogAsync(nameof(CalificarDialog), null, cancellationToken);
        }

        private async Task IntentNone(WaterfallStepContext stepContext, RecognizerResult luisResult, CancellationToken cancellationToken)
        {
            var resultQnA = await _qnmakerService._qnamaker.GetAnswersAsync(stepContext.Context);

            var score = resultQnA.FirstOrDefault()?.Score;
            string response = resultQnA.FirstOrDefault()?.Answer;

            if (score >= 0.5)
            {
                await stepContext.Context.SendActivityAsync(response, cancellationToken: cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync($"No entiendo lo que me dices", cancellationToken: cancellationToken);
                await Task.Delay(1000);
                await IntentOpciones(stepContext, luisResult, cancellationToken);
            }

        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }

    }
}
