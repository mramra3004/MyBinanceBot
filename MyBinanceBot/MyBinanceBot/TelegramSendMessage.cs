using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using MyBinanceBot.Constant;
using MyBinanceBot.Services;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

#pragma warning disable 618

namespace MyBinanceBot
{
    public class TelegramSendMessage : BackgroundService
    {
        private static readonly TelegramBotClient Bot =
            new TelegramBotClient("1802515515:AAHVZ3y8SyG8McmtSz9ryWYT_o1K1Lm4LVg");

        private static readonly IBinanceService BinanceService = new BinanceService();


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var me = await Bot.GetUpdatesAsync(cancellationToken: stoppingToken);
                Bot.OnMessage += BotMessage;
                Bot.StartReceiving(cancellationToken: stoppingToken);
                Console.ReadLine();
                Bot.StopReceiving();
                await Task.Delay(1000, stoppingToken);
            }
        }

        /// <summary>  
        /// Handle bot webhook  
        /// </summary>  
        /// <param name="sender"></param>  
        /// <param name="e"></param>  
        private static void BotMessage(object sender, MessageEventArgs e)
        {
            if (e.Message.Type == MessageType.Text)
                PrepareQuestionnaires(e);
        }

        private static void PrepareQuestionnaires(MessageEventArgs e)
        {
            var message = e.Message.Text.ToLower();
            if (string.IsNullOrWhiteSpace(message)) return;
            try
            {
                switch (message)
                {
                    case CommandConstant.Start:
                        Bot.SendPhotoAsync(e.Message.Chat.Id,
                            "https://tlgrm.eu/_/stickers/8a1/9aa/8a19aab4-98c0-37cb-a3d4-491cb94d7e12/2.jpg",
                            caption:
                            $"*Xin ch??o @{e.Message.Chat.Username}*, ch??c b???n m???t ng??y t???t l??nh. `B???n h??y g?? \"/menu\" ????? ???????c h?????ng d???n s??? d???ng</i>`",
                            parseMode: ParseMode.Markdown, replyToMessageId: e.Message.MessageId);
                        break;
                    case CommandConstant.Menu:
                        Bot.SendTextMessageAsync(e.Message.Chat.Id,
                            $"*1. Xem gi?? c???a ?????ng coin*{Environment.NewLine}B???n h??y g?? `/giahientai <?????ng coin c???n xem gi??><?????ng ti???n c???n quy ?????i>`" +
                            $"{Environment.NewLine}{Environment.NewLine}*V?? d???:* ????? xem gi?? ?????ng coin BTC ?????i sang ?????ng USDT {Environment.NewLine}b???n h??y g?? `/giahientai BTCUSDT`",
                            parseMode: ParseMode.Markdown, replyToMessageId: e.Message.MessageId,
                            replyMarkup: new InlineKeyboardMarkup(InlineKeyboardButton.WithUrl("Xem chi ti???t t???i",
                                "https://core.telegram.org/bots/api#sendmessage")));
                        break;
                }

                GetMessageCurrentPrice(e, message);
            }
            catch (Exception exception)
            {
                Bot.SendTextMessageAsync(e.Message.Chat.Id,
                    "???? c?? l???i x???y ra" + Environment.NewLine +
                    "C?? th??? b???n nh???p m?? coin kh??ng ????ng ho???c l???i t??? m??y ch???. Xin vui l??ng th??? l???i!");
            }
        }

        private static void GetMessageCurrentPrice(MessageEventArgs e, string message)
        {
            if (!message.Contains(CommandConstant.CurrentPrice)) return;
            var tasks = new List<Task>();
            var contents = message.Split(" ");
            var messageChat = string.Empty;
            if (contents.Length <= 1) return;
            var code = contents[1].ToUpper();
            var responseCurrentPriceTask = BinanceService.GetCurrentPrice(code);
            var responseInformation24HTask = BinanceService.GetInformation24H(code);
            tasks.Add(responseCurrentPriceTask);
            tasks.Add(responseInformation24HTask);
            Task.WhenAll(tasks);
            var responseCurrentPrice = responseCurrentPriceTask.Result;
            var responseInformation24H = responseInformation24HTask.Result;
            if (responseCurrentPrice is { ResponseStatusCode : HttpStatusCode.OK })
            {
                messageChat = $"Gi?? hi???n t???i c???a {code}: {responseCurrentPrice.Data.Price}$";
            }

            if (responseInformation24H is { ResponseStatusCode: HttpStatusCode.OK })
            {
                messageChat +=
                    $"{Environment.NewLine}Gi?? ch??o mua t???t nh???t c???a {code}: {responseInformation24H.Data.BidPrice}$" +
                    $"{Environment.NewLine}Gi?? cao nh???t trong ng??y c???a {code}: {responseInformation24H.Data.HighPrice}$" +
                    $"{Environment.NewLine}Gi?? th???p nh???t trong ng??y c???a {code}: {responseInformation24H.Data.LowPrice}$";
            }

            if (!string.IsNullOrEmpty(messageChat))
                Bot.SendTextMessageAsync(e.Message.Chat.Id, messageChat);
            else
            {
                Bot.SendTextMessageAsync(e.Message.Chat.Id,
                    "???? c?? l???i x???y ra" + Environment.NewLine +
                    "C?? th??? b???n nh???p m?? coin kh??ng ????ng ho???c l???i t??? m??y ch???. Xin vui l??ng th??? l???i!");
            }
        }
    }
}