using bibliochat.Bots;
using bibliochat.Common.Models;
using bibliochat.Common.Models.BotState;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;

using System.Threading;
using System.Threading.Tasks;

namespace bibliochat.Dialogs.Authenticate
{
    public class FinalizarDialog : ComponentDialog
    {
        private readonly IStatePropertyAccessor<AuthStateModel> _authUserState;
        private readonly IStatePropertyAccessor<UserVerificadoEntity> _dataUserState;
        public  FinalizarDialog(UserState authUserState, UserState dataUserState)
        {
            _authUserState = authUserState.CreateProperty<AuthStateModel>(BibliochatBot<MainDialog>.authUser);
            _dataUserState = dataUserState.CreateProperty<UserVerificadoEntity>(BibliochatBot<MainDialog>.dataUser);
            var waterfallSteps = new WaterfallStep[]
            {
                ToShowOptions,
                ValidateOption
            };
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
        }

        private async Task<DialogTurnResult> ToShowOptions(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var dataUserState = await _dataUserState.GetAsync(stepContext.Context, () => new UserVerificadoEntity());

            await stepContext.Context.SendActivityAsync($"{dataUserState.usuario.nombre_completo} ya tienes una convresacion inicada 😅.", cancellationToken: cancellationToken);
            return await stepContext.PromptAsync(

              nameof(TextPrompt),
              new PromptOptions
              {
                  Prompt = ButtonConfirmation()
              }, cancellationToken);
        }
        private async Task<DialogTurnResult> ValidateOption(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userStateData = await _authUserState.GetAsync(stepContext.Context, () => new AuthStateModel());
            var dataUserState = await _dataUserState.GetAsync(stepContext.Context, () => new UserVerificadoEntity());

            var userOption = stepContext.Context.Activity.Text.ToLower();
            await Task.Delay(1000);
            if (userOption== "no")
            {
                userStateData.session = null;
                await stepContext.Context.SendActivityAsync($"Ok {dataUserState.usuario.nombre_completo} 👋 espero verte pronto. {Environment.NewLine} Cuidate!! ", cancellationToken: cancellationToken);
            }
            else if (userOption == "si")
            {
                await stepContext.Context.SendActivityAsync("¿En qué más puedo ayudarte?", cancellationToken: cancellationToken);
            }
            return await stepContext.ContinueDialogAsync(cancellationToken: cancellationToken);
        }
        private Activity ButtonConfirmation()
        {
            var reply = MessageFactory.Text("¿Deseas continuar este dialogo?");
            reply.SuggestedActions = new SuggestedActions()
            {
                Actions = new List<CardAction>()
                {
                    new CardAction()
                    {
                        Title = "Si",
                        Value = "Si",
                        Type = ActionTypes.ImBack
                    }, new CardAction()
                    {
                        Title = "No",
                        Value = "No",
                        Type = ActionTypes.ImBack
                    }
                }
            };
            return reply;
        }


    }
}
