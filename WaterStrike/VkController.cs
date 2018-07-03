using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VkNet;
using VkNet.Enums.SafetyEnums;
using VkNet.Model;
using VkNet.Model.Attachments;
using VkNet.Model.RequestParams;
using VkNet.Utils;
using WaterStrike.EventArgs;
using WaterStrike.Exceptions;
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
        public WebApi WebApi = new WebApi();

        public VkController(ulong groupid, string apptoken)
        {
            this.groupid = groupid;
            api.Authorize(new ApiAuthParams() {AccessToken = apptoken});
            longPollClient.Message += MessageReceived;
            longPollClient.Error += ErrorReceived;
            GamesController.GameCreated += OnGameCreated;
            GamesController.PlayerReady += OnPlayerReady;
            GamesController.GameStarted += OnGameStarted;
            GamesController.StrokeMaked += OnStrokeMaked;
            GamesController.GameEnded += OnGameEnded;
        }

        private void OnGameEnded(object sender, GameEventArgs e)
        {
            void SendMessage(long id)
            {
                api.Messages.Send(new MessagesSendParams()
                {
                    Message = "Игра завершена",
                    UserId = id
                });
            }
            SendMessage(e.FirstPlayerId);
            SendMessage(e.SecondPlayerId);
        }

        private void OnStrokeMaked(object sender, StrokeMakedEventArgs e)
        {
            var msg = "";
            switch (e.StrokeResult)
            {
                case 0:
                    msg = "Противник промахнулся! Ваш ход.";
                    break;
                case 1:
                    msg = "Противник подбил ваш корабль. Он продолжает ходить";
                    break;
                case 2:
                    msg = "Противник уничтожил ваш корабль. Он продолжает ходить";
                    break;
            }
            api.Messages.Send(new MessagesSendParams()
            {
                Message = msg,
                UserId = e.EnemyId
            });
        }

        private void OnGameStarted(object sender, GameEventArgs e)
        {
            void SendMessage(long id, string message)
            {
                api.Messages.Send(new MessagesSendParams()
                {
                    Message = message,
                    UserId = id
                });
            }
            SendMessage(e.FirstPlayerId, "Игра началась. Ваш ход!");
            SendMessage(e.SecondPlayerId, "Игра началась. Ход противника!");
        }

        private void OnPlayerReady(object sender, PlayerReadyEventArgs e)
        {
            if (!e.IsEnemyReady)
            {
                api.Messages.Send(new MessagesSendParams() { Message = "Вы поставили все корабли! Ожидание противника", UserId = e.PlayerId });
            }
        }

        private void OnGameCreated(object sender, GameEventArgs e)
        {
            void SendMessage(long id)
            {
                api.Messages.Send(new MessagesSendParams() { Message = "Игра найдена!" +
                                                                       "\n\nНапишите 'Свои корабли', чтобы отобразить свои корабли, или 'Корабли противника', чтобы отобразить корабли противника." +
                                                                       "\n\nНапишите 'Сколько', чтобы узнать сколько кораблей вам осталось поставить." +
                                                                       "\n\nВведите координаты, чтобы поставить свой корабль или сделать залп по вражескому." +
                                                                       "\nКоманда 'А1А2А3' поставит трехпалубный корабль на этих координатах" +
                                                                       "\n\nПостройте свою флотилию!", Keyboard = new MessageKeyboard()
                {
                    Buttons = new ReadOnlyCollection<ReadOnlyCollection<MessageKeyboardButton>>(
                        new List<ReadOnlyCollection<MessageKeyboardButton>>()
                        {
                            new ReadOnlyCollection<MessageKeyboardButton>(
                                new List<MessageKeyboardButton>()
                                {
                                    GetButton(KeyboardButtonColor.Default, "Свои корабли"),
                                    GetButton(KeyboardButtonColor.Default, "Помощь"),
                                    GetButton(KeyboardButtonColor.Default, "Корабли противника"),
                                }
                            )
                        }
                    )
                }, UserId = id});
            }
            SendMessage(e.FirstPlayerId);
            SendMessage(e.SecondPlayerId);
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
        
        private async void MessageReceived(object sender, VkLongPollUpdateEventArgs e)
        {
            if(e.Update.Type != "message_new") return;

            var obj = e.Update.Object;
            var id = (long)obj["user_id"];
            var message = obj.ToObject<Message>();

            var ingame = GamesController.IsInGame(id);
            var isready = ingame && GamesController.IsReady(id);

            if (message.Body.ToLower().Contains("игра") && !ingame)
            {
                GamesController.AddToSearch(id);
                api.Messages.Send(new MessagesSendParams() { Message = "Вы добавлены в поиск!", UserId = id });
                return;
            }

            if (message.Body.ToLower().Contains("свои") && ingame)
            {
                var field = GamesController.GetField(id);
                await SendField(id, field);
                return;
            }

            if (message.Body.ToLower().Contains("противник") && ingame)
            {
                var field = GamesController.GetHiddenEnemyField(id);
                await SendField(id, field);
                return;
            }

            if (message.Body.ToLower().Contains("сколько") && ingame && !isready)
            {
                var ships = GamesController.GetAvailableShipsCount(id);
                var str = ships.Aggregate("", (current, ship) => current + $"Вы можете добавить {ship.Value} кораблей размера {ship.Key}\n");
                api.Messages.Send(new MessagesSendParams()
                {
                    Message = str,
                    UserId = id
                });
                return;
            }
            if (message.Body.ToLower().Contains("кон") && ingame)
            {
                GamesController.EndGame(id);
                return;
            }

            if (ingame && !isready)
            {
                var regex = new Regex("((?:(?:[А,Б,В,Г,Д,Е,Ж,З,И,К][1-9])|(?:[1-9][А,Б,В,Г,Д,Е,Ж,З,И,К]))+)(?:\\s|$)");
                var match = regex.Match(message.Body.ToUpper());
                if (match.Success)
                {
                    while (match.Success)
                    {
                        var str = match.Groups[1].Value;
                        var points = Enumerable.Range(0, str.Length / 2).Select(i => str.Substring(i * 2, 2));
                        match = match.NextMatch();
                        try
                        {
                            GamesController.SetShip(id, points.Select(p => new Place(p)).ToArray());
                        }
                        catch (ShipNotAvailableException)
                        {
                            api.Messages.Send(new MessagesSendParams()
                            {
                                Message = "Вы больше не можете поставить корабли такого размера",
                                UserId = id
                            });
                            return;
                        }
                        catch (ArgumentException)
                        {
                            api.Messages.Send(new MessagesSendParams()
                            {
                                Message = "Корабль слишком большой или вы неправильно указали координаты",
                                UserId = id
                            });
                            return;
                        }
                    }
                    api.Messages.Send(new MessagesSendParams()
                    {
                        Message = "Корабль добавлен",
                        UserId = id
                    });
                    return;
                }
            }
            if (ingame && isready)
            {
                var regex = new Regex("((?:(?:[А,Б,В,Г,Д,Е,Ж,З,И,К][1-9])|(?:[1-9][А,Б,В,Г,Д,Е,Ж,З,И,К])))");
                var match = regex.Match(message.Body.ToUpper());
                if (match.Success)
                {
                    var str = match.Groups[1].Value;
                    var place = new Place(str);
                    try
                    {
                        var ret = GamesController.Shoot(id, place);
                        var msg = "";
                        switch (ret)
                        {
                            case 0:
                                msg = "Вы не попали. Ход противника.";
                                break;
                            case 1:
                                msg = "Попадание. Ваш ход.";
                                break;
                            case 2:
                                msg = "Корабль уничтожен. Ваш ход.";
                                break;
                        }
                        api.Messages.Send(new MessagesSendParams()
                        {
                            Message = msg,
                            UserId = id
                        });
                        return;
                    }
                    catch (PlayerStatusException)
                    {
                        api.Messages.Send(new MessagesSendParams()
                        {
                            Message = "Не ваш ход",
                            UserId = id
                        });
                        return;
                    }
                    catch (ArgumentException)
                    {
                        api.Messages.Send(new MessagesSendParams()
                        {
                            Message = "Вы уже стреляли сюда",
                            UserId = id
                        });
                        return;
                    }
                }
            }

            api.Messages.Send(new MessagesSendParams() { Message = "Не понимаю о чем вы", UserId = id });
        }

        public async Task SendField(long id, FieldPoint[,] field)
        {
            try
            {
                var data = await api.Photo.GetMessagesUploadServerAsync(id);
                var image = await WebApi.GetFieldImage(field);
                var response = await UploadImage(data.UploadUrl, image);
                var photos = api.Photo.SaveMessagesPhoto(response);
                api.Messages.Send(new MessagesSendParams
                {
                    Attachments = photos,
                    UserId = id
                });
            }
            catch
            {
                api.Messages.Send(new MessagesSendParams
                {
                    Message = "Сервис временно недоступен. Повторите попытку позже.",
                    UserId = id
                });
            }
        }
        private async Task<string> UploadImage(string url, byte[] data)
        {
            using (var client = new HttpClient())
            {
                var requestContent = new MultipartFormDataContent();
                var imageContent = new ByteArrayContent(data);
                imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
                requestContent.Add(imageContent, "photo", "image.jpg");
                var response = await client.PostAsync(url, requestContent);

                return await response.Content.ReadAsStringAsync();
            }
        }

        public void Dispose()
        {
            longPollClient?.Dispose();
            api?.Dispose();
            WebApi.Dispose();
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
