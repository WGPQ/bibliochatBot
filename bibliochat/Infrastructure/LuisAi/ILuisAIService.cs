﻿using Microsoft.Bot.Builder.AI.Luis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace bibliochat.Services.LuisAi
{
    public interface ILuisAIService
    {
        public LuisRecognizer _luisRecognizer { get; }
    }
}
