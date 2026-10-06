using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Absynthium_Demo;

internal static partial class DiscordPayload
{
    public static JsonObject CreateDefault() => JsonNode.Parse("""
        {
          "username": "{webhook_name}",
          "avatar_url": "{webhook_avatar}",
          "content": "{message_text}",
          "embeds": [
            {
              "title": "{embed_title}",
              "description": "Enregistrement terminé sur **{server_name}**.",
              "color": 5763719,
              "fields": [
                { "name": "Carte", "value": "{map}", "inline": true },
                { "name": "Durée", "value": "{length}", "inline": true },
                { "name": "Joueurs", "value": "{player_count}", "inline": true },
                { "name": "Round", "value": "{round}", "inline": true },
                { "name": "Date", "value": "{date} à {time}", "inline": true },
                { "name": "Taille de l'archive", "value": "{fileSizeInKB} Ko", "inline": true },
                { "name": "Fichier", "value": "`{fileName}.dem`\nArchive : `{fileName}.zip`", "inline": false },
                { "name": "Téléchargement FTP", "value": "{ftp_link}", "inline": false },
                { "name": "Information", "value": "{file_size_warning}", "inline": false }
              ],
              "footer": { "text": "Absynthium_Demo • {server_name}" },
              "timestamp": "{iso_timestamp}"
            }
          ]
        }
        """)!.AsObject();

    public static string Render(JsonObject template, IReadOnlyDictionary<string, string> values)
    {
        // Replace string values before JSON serialization so quotes, slashes and
        // newlines in server names or configurable text cannot corrupt the payload.
        var payload = Expand(template, values)!.AsObject();
        RemoveEmptyString(payload, "avatar_url");
        RemoveEmptyString(payload, "content");

        if (payload["embeds"] is JsonArray embeds)
        {
            foreach (var embed in embeds.OfType<JsonObject>())
            {
                if (embed["fields"] is not JsonArray fields)
                    continue;
                for (int i = fields.Count - 1; i >= 0; i--)
                {
                    if (fields[i] is JsonObject field &&
                        string.IsNullOrWhiteSpace(field["value"]?.GetValue<string>()))
                        fields.RemoveAt(i);
                }
            }
        }

        return payload.ToJsonString();
    }

    private static JsonNode? Expand(JsonNode? node, IReadOnlyDictionary<string, string> values)
    {
        if (node is JsonObject obj)
        {
            var result = new JsonObject();
            foreach (var property in obj)
                result[property.Key] = Expand(property.Value, values);
            return result;
        }
        if (node is JsonArray array)
            return new JsonArray(array.Select(item => Expand(item, values)).ToArray());
        if (node is JsonValue value && value.TryGetValue<string>(out var text))
            return JsonValue.Create(Placeholder().Replace(text, match =>
                values.TryGetValue(match.Groups[1].Value, out var replacement) ? replacement : match.Value));
        return node?.DeepClone();
    }

    private static void RemoveEmptyString(JsonObject obj, string key)
    {
        if (obj[key] is JsonValue value && value.TryGetValue<string>(out var text) && string.IsNullOrWhiteSpace(text))
            obj.Remove(key);
    }

    [GeneratedRegex(@"\{([A-Za-z_][A-Za-z0-9_]*)\}")]
    private static partial Regex Placeholder();
}
