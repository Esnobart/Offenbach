using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using NetCord.Services.ComponentInteractions;
using System.Text.Json;


namespace Offenbach
{
    public class BlackjackGame
    {
        public string PlayerID { get; set; }
        public List<string> Deck { get; set; } = new();
        public List<string> PlayerCards { get; set; } = new();
        public List<string> BotCards { get; set; } = new();
        public int CardNumber { get; set; } = 4;
    }

    public static class GameStorage
    {
        public static readonly string ProjectRoot =
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\.."));

        public static readonly string DBPath =
            Path.Combine(ProjectRoot, "games.json");
        public static async Task SaveBlackjackGameAsync(ulong userId, BlackjackGame game)
        {
            Dictionary<ulong, BlackjackGame> games;

            if (File.Exists(DBPath))
            {
                string json = await File.ReadAllTextAsync(DBPath);
                games = JsonSerializer.Deserialize<Dictionary<ulong, BlackjackGame>>(json)
                        ?? new Dictionary<ulong, BlackjackGame>();
            }
            else
            {
                games = new Dictionary<ulong, BlackjackGame>();
            }

            games[userId] = game;

            string updatedJson = JsonSerializer.Serialize(games, new JsonSerializerOptions { WriteIndented = true });

            try
            {
                await File.WriteAllTextAsync(DBPath, updatedJson);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при записи JSON: {ex.Message}");
            }
        }


        public static int CalculateHandValue(List<string> hand)
        {
            int total = 0;
            int aceCount = 0;

            foreach (var card in hand)
            {
                string rank = card[..^1];

                if (int.TryParse(rank, out int value))
                {
                    total += value;
                }
                else if (rank is "J" or "Q" or "K")
                {
                    total += 10;
                }
                else if (rank == "A")
                {
                    total += 11;
                    aceCount++;
                }
            }

            while (total > 21 && aceCount > 0)
            {
                total -= 10;
                aceCount--;
            }

            return total;
        }
    }

    public class BlackJack : ApplicationCommandModule<ApplicationCommandContext>
    {
        public static List<string> GetShuffledDeck()
        {
            var suits = new[] { "♠", "♥", "♦", "♣" };
            var ranks = new[]
            {
                    "2", "3", "4", "5", "6", "7", "8", "9", "10",
                    "J", "Q", "K", "A"
                };

            var deck = new List<string>();

            foreach (var suit in suits)
            {
                foreach (var rank in ranks)
                {
                    deck.Add($"{rank}{suit}");
                }
            }

            return deck.OrderBy(_ => new Random().Next()).ToList();
        }

        [SlashCommand("blackjack", "Сыграй в Блэк Джэк с ботом")]
        public async Task<InteractionMessageProperties> BlackjackAsync()
        {
            var deck = GetShuffledDeck();

            var playerCards = new List<string> { deck[0], deck[2] };
            var botCards = new List<string> { deck[1], deck[3] };

            var game = new BlackjackGame
            {
                PlayerID = Context.User.Id.ToString(),
                Deck = deck,
                PlayerCards = playerCards,
                BotCards = botCards,
                CardNumber = 4
            };

            await GameStorage.SaveBlackjackGameAsync(Context.User.Id, game);

            return new InteractionMessageProperties
            {
                Content = $"**Карты бота:** {string.Join(", ", botCards)}\n" +
                          $"**Ваши карты:** {string.Join(", ", playerCards)}\n",
                Components = new[]
                {
                        new ActionRowProperties(new IButtonProperties[]
                        {
                            new ButtonProperties("hit_button", "Взять карту", ButtonStyle.Primary),
                            new ButtonProperties("stand_button", "Остаться", ButtonStyle.Secondary)
                        })
                    }
            };
        }
    }

    public class BlackJackButtons : ComponentInteractionModule<ButtonInteractionContext>
    {
        [ComponentInteraction("hit_button")]
        public async Task HandleHitButtonAsync()
        {
            var interaction = Context.Interaction;

            await interaction.SendResponseAsync(InteractionCallback.DeferredMessage());

            string json = await File.ReadAllTextAsync(GameStorage.DBPath);

            var games = JsonSerializer.Deserialize<Dictionary<ulong, BlackjackGame>>(json)
                ?? new Dictionary<ulong, BlackjackGame>();

            if (!games.TryGetValue(interaction.User.Id, out var game))
            {
                await interaction.ModifyResponseAsync(msg =>
                {
                    msg.Content = "Игра не найдена.";
                    msg.Components = Array.Empty<ActionRowProperties>();
                });
                return;
            }

            if (game.CardNumber >= game.Deck.Count)
            {
                await interaction.ModifyResponseAsync(msg =>
                {
                    msg.Content = "Вы не можете взять больше карт, так как колода исчерпана.";
                    msg.Components = Array.Empty<ActionRowProperties>();
                });
                return;
            }

            var newCard = game.Deck[game.CardNumber];
            game.PlayerCards.Add(newCard);
            game.CardNumber++;

            int playerScore = GameStorage.CalculateHandValue(game.PlayerCards);

            if (playerScore > 21)
            {
                games.Remove(interaction.User.Id);
                await File.WriteAllTextAsync(GameStorage.DBPath,
                    JsonSerializer.Serialize(games, new JsonSerializerOptions { WriteIndented = true }));

                await interaction.ModifyResponseAsync(msg =>
                {
                    msg.Content = $"Вы взяли карту: {newCard}\nВаши карты: {string.Join(", ", game.PlayerCards)}\n" +
                                  $"**Перебор! Ваш счёт: {playerScore}. Вы проиграли.**";
                    msg.Components = Array.Empty<ActionRowProperties>();
                });
                return;
            }

            await GameStorage.SaveBlackjackGameAsync(interaction.User.Id, game);

            await interaction.ModifyResponseAsync(msg =>
            {
                msg.Content = $"Вы взяли карту: {newCard}\nВаши карты: {string.Join(", ", game.PlayerCards)}";
                msg.Components = new[]
                {
                    new ActionRowProperties(new IButtonProperties[]
                    {
                        new ButtonProperties("hit_button", "Взять карту", ButtonStyle.Primary),
                        new ButtonProperties("stand_button", "Остаться", ButtonStyle.Secondary)
                    })
                };
            });
        }

        [ComponentInteraction("stand_button")]
        public async Task HandleStandButtonAsync()
        {
            var interaction = Context.Interaction;

            await interaction.SendResponseAsync(InteractionCallback.DeferredMessage());

            string json = await File.ReadAllTextAsync(GameStorage.DBPath);
            var games = JsonSerializer.Deserialize<Dictionary<ulong, BlackjackGame>>(json)
                ?? new Dictionary<ulong, BlackjackGame>();

            if (!games.TryGetValue(interaction.User.Id, out var game))
            {
                await interaction.ModifyResponseAsync(msg =>
                {
                    msg.Content = "Игра не найдена.";
                    msg.Components = Array.Empty<ActionRowProperties>();
                });
                return;
            }

            int playerScore = GameStorage.CalculateHandValue(game.PlayerCards);
            int dealerScore = GameStorage.CalculateHandValue(game.BotCards);

            Random rnd = new();

            while (game.CardNumber < game.Deck.Count)
            {
                double probability = (21.0 - dealerScore) / 21.0;
                if (rnd.NextDouble() >= probability)
                    break;
                var newCard = game.Deck[game.CardNumber];
                game.BotCards.Add(newCard);
                game.CardNumber++;
                dealerScore = GameStorage.CalculateHandValue(game.BotCards);
                await interaction.SendFollowupMessageAsync($"**Диллер взял карту {newCard}!**\n");
            }

            string result;
            if (dealerScore > 21 || playerScore > dealerScore)
            {
                result = "**Вы выиграли!**";
            }
            else if (playerScore < dealerScore)
            {
                result = "*Вы проиграли.*";
            }
            else
            {
                result = "Ничья.";
            }
            games.Remove(interaction.User.Id);
            await File.WriteAllTextAsync(GameStorage.DBPath,
                JsonSerializer.Serialize(games, new JsonSerializerOptions { WriteIndented = true }));
            await interaction.ModifyResponseAsync(msg =>
            {
                msg.Content = $"**Игра окончена!**\n\n" +
                              $"**Карты бота:** {string.Join(", ", game.BotCards)} (Счёт: {dealerScore})\n" +
                              $"**Ваши карты:** {string.Join(", ", game.PlayerCards)} (Счёт: {playerScore})\n\n" +
                              $"{result}";
                msg.Components = Array.Empty<ActionRowProperties>();
            });
        }
    }
}
