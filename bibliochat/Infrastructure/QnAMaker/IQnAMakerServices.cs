// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.AI.QnA;

namespace Microsoft.bibliochat.QnA
{
    public interface IQnAMakerServices
    {
        QnAMaker _qnamaker { get; set; }
    }
}
