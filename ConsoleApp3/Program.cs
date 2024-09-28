//モデル：gpt-4o
//モデルのバージョン：2024-08-06 以上
//APIのバージョン：2024-08-01-preview 以上

using Azure.AI.OpenAI;
using Azure.Identity;
using OpenAI.Chat;
using System.Text.Json;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;

var personJsonSchema = JsonSchemaExporter.GetJsonSchemaAsNode(
    SourceGenerationContext.Default.Person,
    exporterOptions: new()
    {
        TreatNullObliviousAsNonNullable = true,
    })
    .ToJsonString();

// AOAI のクライアントを作成する
var openAiClient = new AzureOpenAIClient(
    // モデルのバージョンが 2024-08-06 以上の gpt-4o をデプロイしている
    // Azure OpenAI Service のエンドポイントを指定する
    new("https://<<AOAIのリソース名>>.openai.azure.com/"),
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
            "ユーザーの発言内容から名前と年齢を抽出して JSON で形式に整形してください。"),
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


[JsonSerializable(typeof(Person))]
partial class SourceGenerationContext : JsonSerializerContext;

class Person
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public string[] Addresses { get; set; } = [];
}