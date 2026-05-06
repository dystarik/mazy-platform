namespace MazyPlatform.Scenario.Telegram.UseCases;

using System.Text;

using MazyPlatform.Scenario.Abstractions.Actions;

internal static class TelegramInlineKeyboardBuilder
{
    public static object Build(ButtonLayout buttons)
    {
        foreach (var button in buttons.Buttons)
        {
            var payloadLength = Encoding.UTF8.GetByteCount(button.Payload);

            if (payloadLength is < 1 or > 64)
            {
                throw new InvalidOperationException(
                    $"Telegram callback_data для кнопки \"{button.Label}\" должен занимать от 1 до 64 байт.");
            }
        }

        return new
        {
            inline_keyboard = buttons.Rows
                .Select(static row => row.Select(BuildButton).ToArray())
                .ToArray(),
        };
    }

    private static Dictionary<string, object> BuildButton(ButtonDefinition button)
    {
        var result = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["text"] = button.Label,
            ["callback_data"] = button.Payload,
        };

        var style = button.Style switch
        {
            ButtonStyle.Primary => "primary",
            ButtonStyle.Success => "success",
            ButtonStyle.Danger => "danger",
            _ => null,
        };

        if (style is not null)
        {
            result["style"] = style;
        }

        return result;
    }
}
