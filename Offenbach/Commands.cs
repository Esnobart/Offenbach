using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace MyBot;

public class ExampleModule : ApplicationCommandModule<ApplicationCommandContext>
{
    private static readonly string[] responses = {
        "Бесспорно!",
        "Это предрешено судьбой",
        "Никаких сомнений",
        "Определённо да",
        "Можешь быть уверен в этом",
        "Мне кажется — да",
        "Вероятнее всего",
        "Хорошие перспективы",
        "Знаки говорят — да",
        "Да",
        "Пока не ясно, попробуй снова",
        "Спроси позже",
        "Лучше не рассказывать",
        "Сейчас нельзя предсказать",
        "Сконцентрируйся и спроси опять",
        "Даже не думай",
        "Мой ответ — нет",
        "По моим данным — нет",
        "Перспективы не очень хорошие",
        "Весьма сомнительно"
    };

    [SlashCommand("8ball", "Задай вопрос и получи ответ")]
    public string EightBall([SlashCommandParameter(Description = "Вопрос")] string question)
    {
        if (!question.EndsWith("?"))
            question += "?";
        return $"{question} {responses[new Random().Next(responses.Length)]}";
    }

    [UserCommand("Аватар")]
    public async Task<InteractionMessageProperties> AvatarAsync(User user)
    {
        var restUser = await Context.Client.Rest.GetUserAsync(user.Id);
        bool isGif = restUser.AvatarHash?.StartsWith("a_") ?? false;
        ImageUrl? avatarImage = isGif ? restUser.GetAvatarUrl(ImageFormat.Gif) : restUser.GetAvatarUrl(ImageFormat.Png);
        string avatarUrl = avatarImage?.ToString() ?? restUser.DefaultAvatarUrl.ToString();
        var uri = new Uri(avatarUrl);
        string baseUrl = uri.GetLeftPart(UriPartial.Path);
        string resizedUrl = $"{baseUrl}?size=512";

        return new InteractionMessageProperties()
        {
            Content = $"Аватар URL: {resizedUrl}",
            Embeds = new[]
            {
                new EmbedProperties()
                {
                    Title = $"Аватар пользователя {restUser.Username}",
                    Image = new EmbedImageProperties(resizedUrl),
                    Color = new Color(0x5865F2),
                }
            }
        };
    }
}