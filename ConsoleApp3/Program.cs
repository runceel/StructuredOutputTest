using Azure.AI.OpenAI;
using Azure.Identity;
using OpenAI.Chat;
using System.ComponentModel;
using System.Text.Encodings.Web;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using System.Text.Unicode;

var personJsonSchema = JsonSchemaExporter.GetJsonSchemaAsNode(
    SourceGenerationContext.Default.Person,
    exporterOptions: new()
    {
        TreatNullObliviousAsNonNullable = true,
        // Description を追加する
        TransformSchemaNode = (context, schema) =>
        {
            var attributeProvider = context.PropertyInfo is not null ?
                context.PropertyInfo.AttributeProvider :
                context.TypeInfo.Type;

            var description = (DescriptionAttribute?)attributeProvider?.GetCustomAttributes(false)
                .FirstOrDefault(x => x is DescriptionAttribute);

            if (description == null) return schema;

            if (schema is JsonObject jsonObject)
            {
                jsonObject.Insert(0, "description", description.Description);
            }

            return schema;
        },
    })
    .ToJsonString(new JsonSerializerOptions
    {
        // 見やすいようにインデントと日本語が含まれる場合のエンコードを指定
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
    });

// AOAI のクライアントを作成する
var openAiClient = new AzureOpenAIClient(
    // モデルのバージョンが 2024-08-06 以上の gpt-4o をデプロイしている
    // Azure OpenAI Service のエンドポイントを指定する
    new("https://<<AOAI のリソース名>>.openai.azure.com/"),
    // Managed ID で認証する
    new DefaultAzureCredential(options: new()
    {
        ExcludeVisualStudioCredential = true,
    }));

// チャットクライアントを取得する
var chatClient = openAiClient.GetChatClient("gpt-4o");
// Structured Output を使って JSON Schema を指定して呼び出す
var result = await chatClient.CompleteChatAsync(
    [
        new SystemChatMessage(
            "ユーザーの発言内容から名前と年齢を抽出して JSON 形式に整形してください。"),
        new UserChatMessage("""
            私の名前は太郎です。17歳です！
            日本の東京都と大阪府の2拠点生活をしています！
            よろしくお願いいたします。
            """),
    ], 
    options: new()
    {
        ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
            "person",
            BinaryData.FromString(personJsonSchema))
    });

// 結果を表示する
Console.WriteLine(result.Value.Content.First().Text);


[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(Person))]
partial class SourceGenerationContext : JsonSerializerContext;

[Description("ユーザー情報")]
class Person
{
    [Description("ユーザーの名前")]
    public string Name { get; set; } = "";
    [Description("ユーザーの年齢")]
    public int Age { get; set; }
    [Description("ユーザーの住所")]
    public string[] Addresses { get; set; } = [];
}
