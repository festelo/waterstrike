using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VkNet;
using VkNet.Enums.SafetyEnums;
using VkNet.Model;
using VkNet.Model.RequestParams;
using VkNet.Utils;
using WaterStrike.LongPoll;
using WaterStrike.LongPoll.EventArgs;

namespace WaterStrike
{
    class VkController : IDisposable
    {
        private readonly VkApi api = new VkApi();
        private readonly ulong groupid;
        private readonly VkLongPollClient longPollClient = new VkLongPollClient(); 

        public GamesController GamesController = new GamesController();

        public VkController(ulong groupid, string apptoken)
        {
            this.groupid = groupid;
            api.Authorize(new ApiAuthParams() {AccessToken = apptoken});
            longPollClient.Message += MessageReceived;
            longPollClient.Error += ErrorReceived;
            GamesController.GameCreated += OnGameCreated;
        }

        private void OnGameCreated(object sender, GameCreatedEventArgs e)
        {
            void SendMessage(long id)
            {
                api.Messages.Send(new MessagesSendParams() { Message = "Игра найдена!", UserId = id});
            }
            SendMessage(e.FirstPlayer.Id);
            SendMessage(e.SecondPlayer.Id);
        }

        private async void ErrorReceived(object sender, VkLongPollErrorEventArgs e)
        {
            if (e.InnerException is VkLongPollException)
            {
                await StartLongPoll();
            }
        }

        public async Task StartLongPoll()
        {
            if (longPollClient.IsListening)
            {
                longPollClient.Stop();
            }
            var longpoll = await api.Groups.GetLongPollServerAsync(groupid);
            await longPollClient.StartListener(longpoll.Key, longpoll.Server, longpoll.Ts);
        }
        
        private void MessageReceived(object sender, VkLongPollUpdateEventArgs e)
        {
            if(e.Update.Type != "message_new") return;

            var obj = e.Update.Object;
            var id = (long)obj["user_id"];
            var message = obj.ToObject<Message>();

            if (message.Body.ToLower().Contains("игра") && GamesController.TryToAddToSearch(id))
            {
                api.Messages.Send(new MessagesSendParams() { Message = "Вы добавлены в поиск!", UserId = id });
                return;
            }

            if (message.Body.ToLower().Contains("пока") && GamesController.TryToGetField(id, out var field))
            {
                var buttons = new List<MessageKeyboardButton[]>();

                var firstline = new List<MessageKeyboardButton> { GetButton(KeyboardButtonColor.Primary, " ") };
                firstline.AddRange(Place.AvailableColumns.Select(column => GetButton(KeyboardButtonColor.Primary, column.ToString())));
                buttons.Add(firstline.ToArray());

                for (int i = 0; i < 10; i++)
                {
                    var line = new List<MessageKeyboardButton>() { GetButton(KeyboardButtonColor.Primary, i+1.ToString()) };
                    for (int j = 0; j < 10; j++)
                    {
                        var color = KeyboardButtonColor.Default;
                        var label = " ";
                        switch (field[i,j])
                        {
                            case FieldPoint.None:
                                break;
                            case FieldPoint.Ship:
                                label = "🛳";
                                break;
                            case FieldPoint.Shooted:
                                color = KeyboardButtonColor.Negative;
                                break;
                            case FieldPoint.Shooted | FieldPoint.Ship:
                                color = KeyboardButtonColor.Positive;
                                break;
                        }
                        line.Add(GetButton(color, label));
                    }
                    buttons.Add(line.ToArray());
                }
                var keyboard = new MessageKeyboard() { Buttons = buttons.Select(b => b.ToReadOnlyCollection()).ToReadOnlyCollection()};
                api.Messages.Send(new MessagesSendParams() { Keyboard = keyboard, Message = "Ok"});
                return;
            }

            var regex = new Regex("([А,Б,В,Г,Д,Е,Ж,З,И,К]\\d)");
            var match = regex.Match(message.Body);
            if (match.Success)
            {
                var points = new List<string>();
                while (match.Success)
                {
                    points.Add(match.Groups[1].Value);
                    match = match.NextMatch();
                }
                if (GamesController.TryToSetShip(id, points.Select(p => new Place(p)).ToArray()))
                {
                    api.Messages.Send(new MessagesSendParams() { Message = "Корабль добавлен", UserId = id });
                    return;
                }
            }

            api.Messages.Send(new MessagesSendParams() { Message = "Не понимаю о чем вы", UserId = id });
        }

        public void Dispose()
        {
            longPollClient?.Dispose();
            api?.Dispose();
        }

        public MessageKeyboardButton GetButton(KeyboardButtonColor color, string label)
        {
            return new MessageKeyboardButton()
            {
                Color = color,
                Action = new MessageKeyboardButtonAction()
                {
                    Label = label,
                    Type = KeyboardButtonActionType.Text
                }
            };
        }
    }
}
