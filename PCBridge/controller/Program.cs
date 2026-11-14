using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

var guest = Environment.GetEnvironmentVariable("PCBRIDGE_URL") ?? "http://127.0.0.1:8765/";
var token = Environment.GetEnvironmentVariable("PCBRIDGE_TOKEN") ?? "";
var ollama = Environment.GetEnvironmentVariable("OLLAMA_URL") ?? "http://127.0.0.1:11434/";
var model = Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "qwen2.5:3b";
using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
http.DefaultRequestHeaders.Add("X-PCBridge-Token", token);
Console.WriteLine($"CSS Dev Controller | model={model} | guest={guest}");
Console.WriteLine("Commands: /status, /ls [path], /read path, /hash path, /help, /quit");
while (true) {
 Console.Write("\n> "); var input=Console.ReadLine(); if(input is null||input=="/quit")break;
 try {
  if(input=="/help"){Console.WriteLine("Use natural language for Ollama agent tasks. Local direct commands: /status, /ls [path], /read relative-file, /hash relative-file. The model can read/write only under guest configured game root.");continue;}
  if(input=="/status"){Console.WriteLine(await Guest(new{action="status"}));continue;}
  if(input.StartsWith("/ls")){Console.WriteLine(await Guest(new{action="file_list",path=input.Length>3?input[4..]:""}));continue;}
  if(input.StartsWith("/read ")){Console.WriteLine(await Guest(new{action="file_read",path=input[6..]}));continue;}
  if(input.StartsWith("/hash ")){Console.WriteLine(await Guest(new{action="file_hash",path=input[6..]}));continue;}
  await Agent(input);
 }catch(Exception e){Console.WriteLine("ERROR: "+e.Message);}
}
async Task<string> Guest(object body){var res=await http.PostAsync(guest,new StringContent(JsonSerializer.Serialize(body),Encoding.UTF8,"application/json"));var s=await res.Content.ReadAsStringAsync();if(!res.IsSuccessStatusCode)throw new Exception($"PCBridge HTTP {(int)res.StatusCode}: {s}");return s;}
async Task Agent(string task){
 var messages=new JsonArray{new JsonObject{["role"]="system",["content"]="You are a cautious Counter-Strike Source v34 development agent. Use tools to inspect the guest game directory, diagnose, and propose fixes. Never overwrite files unless user explicitly asked to implement/fix; before file_write first create a backup using read and write to a .pcbridge-backup file. No destructive shell or commands. Explain every action briefly."},new JsonObject{["role"]="user",["content"]=task}};
 for(int step=0;step<12;step++){
  var payload=new JsonObject{["model"]=model,["stream"]=false,["messages"]=messages,["tools"]=Tools()};
  var response=await http.PostAsync(new Uri(new Uri(ollama), "api/chat"),new StringContent(payload.ToJsonString(),Encoding.UTF8,"application/json"));
  var raw=await response.Content.ReadAsStringAsync();if(!response.IsSuccessStatusCode)throw new Exception("Ollama "+raw);
  var root=JsonNode.Parse(raw)!;var msg=root["message"]!.AsObject();messages.Add(msg.DeepClone());
  var calls=msg["tool_calls"]?.AsArray();
  if(calls is null||calls.Count==0){Console.WriteLine(msg["content"]?.ToString());return;}
  foreach(var call in calls){
   var fn=call!["function"]!;var name=fn["name"]!.ToString();var args=fn["arguments"] as JsonObject??new();
   Console.WriteLine($"[tool] {name} {args}");
   string result;
   try{result=await Execute(name,args);}catch(Exception e){result="TOOL ERROR: "+e.Message;}
   messages.Add(new JsonObject{["role"]="tool",["name"]=name,["content"]=result});
  }
 }
 Console.WriteLine("Остановлено: лимит 12 итераций агента.");
}
async Task<string> Execute(string name,JsonObject a){
 string path=a["path"]?.ToString()??"";
 return name switch{
 "pc_status"=>await Guest(new{action="status"}),
 "pc_file_list"=>await Guest(new{action="file_list",path}),
 "pc_file_read"=>await Guest(new{action="file_read",path}),
 "pc_file_hash"=>await Guest(new{action="file_hash",path}),
 "pc_file_write"=>await Write(path,a["content"]?.ToString()??""),
 "pc_screenshot"=>await Guest(new{action="screenshot"}),
 _=>"Unsupported tool"
 };
}
async Task<string> Write(string path,string data){
 Console.Write("Confirm file write (y/N): ");if(Console.ReadLine()?.Trim().ToLower()!="y")return "User declined write.";
 return await Guest(new{action="file_write",path,content=data});
}
JsonArray Tools()=>JsonNode.Parse("""
[
 {"type":"function","function":{"name":"pc_status","description":"Check guest agent and display status","parameters":{"type":"object","properties":{},"required":[]}}},
 {"type":"function","function":{"name":"pc_screenshot","description":"Capture guest VM screen as PNG base64","parameters":{"type":"object","properties":{},"required":[]}}},
 {"type":"function","function":{"name":"pc_file_list","description":"List files/folders under configured game root; path relative to root","parameters":{"type":"object","properties":{"path":{"type":"string"}},"required":["path"]}}},
 {"type":"function","function":{"name":"pc_file_read","description":"Read UTF-8 text file under game root, up to 2MB","parameters":{"type":"object","properties":{"path":{"type":"string"}},"required":["path"]}}},
 {"type":"function","function":{"name":"pc_file_hash","description":"Compute SHA256 of file under game root","parameters":{"type":"object","properties":{"path":{"type":"string"}},"required":["path"]}}},
 {"type":"function","function":{"name":"pc_file_write","description":"Write UTF-8 file under game root; requires user confirmation","parameters":{"type":"object","properties":{"path":{"type":"string"},"content":{"type":"string"}},"required":["path","content"]}}}
]
""")!.AsArray();
