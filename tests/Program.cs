using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Encodings.Web;
using Absynthium_Demo;
using Microsoft.Extensions.Logging.Abstractions;

if (args is ["--write-default-config", var configPath])
{
    File.WriteAllText(configPath, JsonSerializer.Serialize(new PluginConfig(), new JsonSerializerOptions
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    }) + Environment.NewLine);
    return;
}

var root = Path.Combine(Path.GetTempPath(), "Absynthium_Demo-tests-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
try
{
    TestDiscordPayload();
    TestConfiguration();
    var game = Path.Combine(root, "game");
    var csgo = Path.Combine(game, "csgo");
    Check(DemoPaths.GetCsgoDirectory(game) == csgo, "game root resolves to csgo");
    Check(DemoPaths.GetCsgoDirectory(csgo + Path.DirectorySeparatorChar) == csgo, "csgo root is not duplicated");
    await Task.WhenAll(TestDirectRecording(), TestDelayedRecording(), TestZip(), TestEmpty(), TestCollision());
    Console.WriteLine("All regression checks passed.");
}
finally
{
    Directory.Delete(root, recursive: true);
}

void TestDiscordPayload()
{
    var template = DiscordPayload.CreateDefault();
    var original = template.ToJsonString();
    var values = new Dictionary<string, string>
    {
        ["webhook_name"] = "Absynthium_Demo",
        ["webhook_avatar"] = "",
        ["message_text"] = "",
        ["embed_title"] = "Démo disponible",
        ["server_name"] = "Serveur \"test\" \\ Paris\n{map}",
        ["map"] = "de_dust2",
        ["length"] = "01:12:34",
        ["round"] = "24",
        ["player_count"] = "10",
        ["date"] = "2026-10-06",
        ["time"] = "19:30:00",
        ["fileName"] = "Absynthium_Demo_de_dust2",
        ["fileSizeInKB"] = "512",
        ["ftp_link"] = "",
        ["file_size_warning"] = "",
        ["iso_timestamp"] = "2026-10-06T17:30:00Z"
    };
    var payload = JsonNode.Parse(DiscordPayload.Render(template, values))!.AsObject();
    var embed = payload["embeds"]![0]!;
    Check(embed["description"]!.GetValue<string>() == "Enregistrement terminé sur **" + values["server_name"] + "**.", "payload safely preserves quotes, backslashes, newlines and literal placeholder text");
    Check(!payload.ContainsKey("avatar_url") && !payload.ContainsKey("content"), "blank optional Discord settings omitted");
    Check(embed["fields"]!.AsArray().Count == 7, "unavailable FTP link and empty warning omitted");
    Check(embed["color"]!.GetValue<int>() == 5763719 && embed["fields"]![0]!["inline"]!.GetValue<bool>(), "payload preserves number and boolean types");
    Check(embed["title"]!.GetValue<string>() == values["embed_title"], "configured embed title applied");
    Check(template.ToJsonString() == original, "render does not mutate configured payload across uploads");

    values["ftp_link"] = "ftp://example.test/demos/demo.zip";
    values["file_size_warning"] = "Archive trop volumineuse.";
    values["webhook_avatar"] = "https://example.test/avatar.png";
    values["message_text"] = "Texte personnalisé\n@everyone";
    payload = JsonNode.Parse(DiscordPayload.Render(template, values))!.AsObject();
    Check(payload["embeds"]![0]!["fields"]!.AsArray().Count == 9, "FTP link and size warning included when available");
    Check(payload["content"]!.GetValue<string>() == values["message_text"], "custom content preserved");

    var custom = JsonNode.Parse("""{"content":"{message_text}","embeds":[{"title":"Custom {map}","fields":[{"name":"Custom","value":"{fileName}"}]}],"allowed_mentions":{"parse":[]},"extra":null}""")!.AsObject();
    payload = JsonNode.Parse(DiscordPayload.Render(custom, values))!.AsObject();
    Check(payload["embeds"]![0]!["title"]!.GetValue<string>() == "Custom de_dust2", "custom payload layout supported");
    Check(payload["allowed_mentions"]!["parse"]!.AsArray().Count == 0 && payload["extra"] == null, "custom nested arrays and nulls preserved");
}

void TestConfiguration()
{
    var config = new PluginConfig();
    Check(config.AutoRecord.Enabled && config.Version == 14, "new configuration enables automatic recording");
    var legacy = JsonSerializer.Deserialize<PluginConfig>("""
        {"ConfigVersion":13,"general":{"default-file-name":"retakes4","delete-demo-after-upload":false},
         "discord":{"webhook-url":"https://example.test/webhook","embed-title":"Custom title"},
         "auto-record":{"enabled":false,"min-player-start-record":4,"demo-request":true},
         "ftp":{"enabled":true,"host":"example.test","retention-hours":96},"demo-request":true}
        """)!;
    Check(legacy.General.DefaultFileName == "retakes4" && !legacy.General.DeleteDemoAfterUpload, "legacy filenames and retention choices preserved");
    Check(!legacy.AutoRecord.Enabled && legacy.AutoRecord.MinPlayerStartRecord == 4, "explicit auto-record settings preserved");
    Check(legacy.Ftp.Enabled && legacy.Ftp.RetentionHours == 96, "FTP configuration preserved");
    Check(legacy.Discord.Payload["embeds"]!.AsArray().Count == 1 && legacy.Discord.EmbedTitle == "Custom title", "legacy configuration gains an embedded payload");
    string serialized = JsonSerializer.Serialize(legacy);
    Check(!serialized.Contains("demo-request"), "obsolete demo-request settings not serialized");
    legacy.Discord.Payload["content"] = "Custom {map}";
    var roundTrip = JsonSerializer.Deserialize<PluginConfig>(JsonSerializer.Serialize(legacy))!;
    Check(roundTrip.Discord.Payload["content"]!.GetValue<string>() == "Custom {map}", "embedded custom payload survives configuration round trip");
}

async Task TestDelayedRecording()
{
    var csgo = Path.Combine(root, "delayed", "csgo");
    string[] candidates = [Path.Combine(csgo, "unused.dem"), Path.Combine(csgo, "addons", "metamod", "legacy.dem")];
    Directory.CreateDirectory(Path.GetDirectoryName(candidates[1])!);
    var destination = Path.Combine(csgo, "discord_demos", "demo.dem");
    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
    var finalize = FileManager.FinalizeDemoAsync(candidates, destination, NullLogger.Instance);
    await Task.Delay(600); // Engine creates the file after stop was dispatched.
    using (var writer = new FileStream(candidates[1], FileMode.CreateNew, FileAccess.Write, FileShare.None))
    {
        writer.Write([1, 2, 3]);
        writer.Flush();
        await Task.Delay(2100);
        Check(!finalize.IsCompleted && !File.Exists(destination), "active writer is not moved");
        writer.Write([4, 5, 6]);
    }
    Check(await finalize, "delayed recording found in Metamod search path");
    Check(File.ReadAllBytes(destination).SequenceEqual(new byte[] { 1, 2, 3, 4, 5, 6 }), "finalized demo includes final writes");
    Check(!File.Exists(candidates[1]), "finalized demo moved out of engine directory");
}

async Task TestDirectRecording()
{
    var csgo = Path.Combine(root, "direct server", "game", "csgo");
    var directory = Path.Combine(csgo, "discord_demos");
    Directory.CreateDirectory(directory);
    var name = "autodemo_de_dust2_2026-09-24_22-39-55.dem";
    var destination = Path.Combine(directory, name);
    var commandPath = DemoPaths.GetRecordingPath(directory, name);
    Check(Path.IsPathFullyQualified(commandPath), "recording command uses an absolute path");
    Check(Path.GetFileName(commandPath) == name, "recording command preserves configured filename");
    var engineDirectory = Path.Combine(csgo, "addons", "metamod");
    Check(Path.GetFullPath(Path.Combine(engineDirectory, commandPath)) == Path.GetFullPath(destination), "Metamod engine directory cannot redirect recording path");
    Check(commandPath.Contains("direct server") && !commandPath.Contains('\\'), "recording path preserves spaces and uses forward slashes");
    File.WriteAllBytes(commandPath, [1, 2, 3, 4]);
    Check(await FileManager.FinalizeDemoAsync([commandPath], destination, NullLogger.Instance), "direct recording finalizes in place");
    Check(File.ReadAllBytes(destination).SequenceEqual(new byte[] { 1, 2, 3, 4 }), "in-place finalization retains original bytes");
    var zip = Path.ChangeExtension(destination, ".zip");
    Check(await FileManager.ZipDemoAsync(destination, zip, NullLogger.Instance), "direct recording compressed");
    using var archive = ZipFile.OpenRead(zip);
    Check(archive.Entries.Single().Name == name, "archive uses configured demo filename");
}

async Task TestZip()
{
    var source = Path.Combine(root, "zip.dem");
    var zip = Path.Combine(root, "zip.zip");
    byte[] bytes = new byte[1024 * 128];
    Random.Shared.NextBytes(bytes);
    using (var writer = new FileStream(source, FileMode.CreateNew, FileAccess.Write, FileShare.None))
    {
        writer.Write(bytes);
        var pending = FileManager.ZipDemoAsync(source, zip, NullLogger.Instance);
        Check(!pending.IsCompleted, "compression waits for writer");
        writer.Dispose();
        Check(await pending, "compression succeeds after writer closes");
    }
    using var archive = ZipFile.OpenRead(zip);
    using var contents = archive.Entries.Single().Open();
    using var output = new MemoryStream();
    await contents.CopyToAsync(output);
    Check(archive.Entries.Single().Name == "zip.dem" && output.ToArray().SequenceEqual(bytes), "zip preserves filename and every byte");
}

async Task TestEmpty()
{
    var source = Path.Combine(root, "empty.dem");
    var zip = Path.Combine(root, "empty.zip");
    File.WriteAllBytes(source, []);
    Check(!await FileManager.ZipDemoAsync(source, zip, NullLogger.Instance), "empty demo rejected");
    Check(!File.Exists(zip) && File.Exists(source), "rejected source retained without archive");
    Check(!await FileManager.FinalizeDemoAsync([Path.Combine(root, "missing.dem")], Path.Combine(root, "missing-final.dem"), NullLogger.Instance), "missing demo times out without success");
}

async Task TestCollision()
{
    var source = Path.Combine(root, "collision.dem");
    var zip = Path.Combine(root, "collision.zip");
    File.WriteAllBytes(source, [1, 2, 3]);
    File.WriteAllBytes(zip, [9, 8, 7]);
    Check(!await FileManager.ZipDemoAsync(source, zip, NullLogger.Instance), "existing archive rejected");
    Check(File.ReadAllBytes(zip).SequenceEqual(new byte[] { 9, 8, 7 }), "existing archive preserved");
    var destination = Path.Combine(root, "collision-final.dem");
    File.WriteAllBytes(destination, [4, 5, 6]);
    Check(!await FileManager.FinalizeDemoAsync([source], destination, NullLogger.Instance), "existing demo destination rejected");
    Check(File.Exists(source) && File.ReadAllBytes(destination).SequenceEqual(new byte[] { 4, 5, 6 }), "move collision preserves both demos");
}

static void Check(bool condition, string name)
{
    if (!condition) throw new Exception("FAIL: " + name);
    Console.WriteLine("PASS: " + name);
}
