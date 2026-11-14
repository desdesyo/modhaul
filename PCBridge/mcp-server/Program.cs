using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

var endpoint = Environment.GetEnvironmentVariable("PCBRIDGE_URL") ?? "http://127.0.0.1:8765/";
var token = Environment.GetEnvironmentVariable("PCBRIDGE_TOKEN") ?? "";
var http = new HttpClient { Timeout = TimeSpan.FromSeconds(25) };
http.DefaultRequestHeaders.Add("X-PCBridge-Token", token);
var input = Console.OpenStandardInput();
var output = Console.OpenStandardOutput();
while (true) {
    var line = await ReadLineAsync(input);
    if (line is null) break;
    JsonDocument? doc = null;
    try {
        doc = JsonDocument.Parse(line);
        var root = doc.RootElement;
        var id = root.TryGetProperty("id", out var idv) ? idv.Clone() : JsonDocument.Parse("null").RootElement;
        var method = root.GetProperty("method").GetString();
        object? response = method switch {
            "initialize" => new { jsonrpc="2.0", id, result=new { protocolVersion="2024-11-05", capabilities=new { tools=new {} }, serverInfo=new { name="pcbridge", version="0.3.0" } } },
            "notifications/initialized" => null,
            "ping" => new { jsonrpc="2.0", id, result=new {} },
            "tools/list" => new { jsonrpc="2.0", id, result=new { tools=ToolDefs } },
            "tools/call" => await CallToolAsync(root, id),
            _ => new { jsonrpc="2.0", id, error=new { code=-32601, message="Method not found" } }
        };
        if (response is not null) await WriteAsync(output, JsonSerializer.Serialize(response));
    } catch (Exception e) {
        await WriteAsync(output, JsonSerializer.Serialize(new { jsonrpc="2.0", id=(object?)null, error=new { code=-32603, message=e.Message } }));
    } finally { doc?.Dispose(); }
}

async Task<object> CallToolAsync(JsonElement root, JsonElement id) {
    var p=root.GetProperty("params"); var name=p.GetProperty("name").GetString()!;
    var a=p.TryGetProperty("arguments",out var av)?av:JsonDocument.Parse("{}").RootElement;
    object payload = name switch {
      "pc_status" => new { action="status" },
      "pc_screenshot" => new { action="screenshot" },
      "pc_click" => new { action="click", x=a.GetProperty("x").GetInt32(), y=a.GetProperty("y").GetInt32() },
      "pc_type_text" => new { action="type", text=a.GetProperty("text").GetString() },
      "pc_press_key" => new { action="key", key=a.GetProperty("key").GetString() },
      "pc_open_url" => new { action="open_url", url=a.GetProperty("url").GetString() },
      "pc_open_folder" => new { action="open_folder", path=a.GetProperty("path").GetString() },
      "pc_powershell" => new { action="run_confirmed", command=a.GetProperty("command").GetString() },
      _ => throw new Exception("Unknown tool")
    };
    using var req=new HttpRequestMessage(HttpMethod.Post,endpoint){Content=new StringContent(JsonSerializer.Serialize(payload),Encoding.UTF8,"application/json")};
    using var resp=await http.SendAsync(req); var body=await resp.Content.ReadAsStringAsync();
    return new { jsonrpc="2.0", id, result=new { content=new[]{new { type="text", text=body }}, isError=!resp.IsSuccessStatusCode } };
}
async Task WriteAsync(Stream s,string text) { var b=Encoding.UTF8.GetBytes(text+"\n"); await s.WriteAsync(b); await s.FlushAsync(); }
static async Task<string?> ReadLineAsync(Stream s) { var b=new List<byte>(); var one=new byte[1]; while(await s.ReadAsync(one)==1){if(one[0]==10)break;if(one[0]!=13)b.Add(one[0]);}return b.Count==0?null:Encoding.UTF8.GetString(b.ToArray());}
static object[] ToolDefs => new object[] {
 new {name="pc_status",description="Check PCBridge guest agent status",inputSchema=new {type="object",properties=new {},required=Array.Empty<string>()}},
 new {name="pc_screenshot",description="Capture guest desktop screenshot (PNG base64 in JSON)",inputSchema=new {type="object",properties=new {},required=Array.Empty<string>()}},
 new {name="pc_click",description="Click guest screen coordinates",inputSchema=new {type="object",properties=new {x=new {type="integer"},y=new {type="integer"}},required=new[]{"x","y"}}},
 new {name="pc_type_text",description="Type Unicode text into focused guest application",inputSchema=new {type="object",properties=new {text=new {type="string"}},required=new[]{"text"}}},
 new {name="pc_press_key",description="Press allowlisted key (ENTER, ESC, TAB, SPACE, BACKSPACE, F10, UP, DOWN, LEFT, RIGHT)",inputSchema=new {type="object",properties=new {key=new {type="string"}},required=new[]{"key"}}},
 new {name="pc_open_url",description="Open HTTP(S) URL in guest browser",inputSchema=new {type="object",properties=new {url=new {type="string"}},required=new[]{"url"}}},
 new {name="pc_open_folder",description="Open existing folder in guest Explorer",inputSchema=new {type="object",properties=new {path=new {type="string"}},required=new[]{"path"}}},
 new {name="pc_powershell",description="Run PowerShell command only after guest consent dialog; limited to 15 seconds",inputSchema=new {type="object",properties=new {command=new {type="string"}},required=new[]{"command"}}}
};
