using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PCBridgeGuest;

public sealed class MainForm : Form
{
    [StructLayout(LayoutKind.Sequential)] struct INPUT { public uint type; public InputUnion U; }
    [StructLayout(LayoutKind.Explicit)] struct InputUnion { [FieldOffset(0)] public KEYBDINPUT ki; }
    [StructLayout(LayoutKind.Sequential)] struct KEYBDINPUT { public ushort wVk,wScan; public uint dwFlags,time; public UIntPtr dwExtraInfo; }
    [DllImport("user32.dll", SetLastError=true)] static extern uint SendInput(uint n, INPUT[] inputs, int size);
    [DllImport("user32.dll")] static extern bool SetCursorPos(int X,int Y);
    [DllImport("user32.dll")] static extern void mouse_event(uint f,uint x,uint y,uint d,UIntPtr e);
    const uint INPUT_KEYBOARD=1, KEYEVENTF_UNICODE=4, KEYEVENTF_KEYUP=2, MOUSE_DOWN=2, MOUSE_UP=4;
    readonly SemaphoreSlim commandLock = new(1,1);
    HttpListener? server; CancellationTokenSource? cts; int active=0;
    readonly TextBox tokenBox=new(){Width=300}, rootBox=new(){Width=420,Text=@"C:\Games\Counter-Strike Source v34"}, log=new(){Multiline=true,ScrollBars=ScrollBars.Vertical,Dock=DockStyle.Fill};
    readonly Button start=new(){Text="Start agent"}, stop=new(){Text="Stop"};
    public MainForm(){
        Text="PCBridge Guest Agent"; Width=760; Height=500;
        tokenBox.Text=Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var bar=new FlowLayoutPanel{Dock=DockStyle.Top,Height=65,Padding=new Padding(8)};
        bar.Controls.Add(new Label{Text="Bearer token",AutoSize=true});bar.Controls.Add(tokenBox);bar.Controls.Add(new Label{Text="Allowed root",AutoSize=true});bar.Controls.Add(rootBox);bar.Controls.Add(start);bar.Controls.Add(stop);
        Controls.Add(log);Controls.Add(bar);start.Click+=async(_,__)=>await StartServer();stop.Click+=(_,__)=>StopServer();
        Log("Stopped by default; loopback only.");
    }
    void Log(string s){if(IsDisposed)return;try{BeginInvoke(()=>{if(!IsDisposed)log.AppendText(DateTime.Now.ToString("HH:mm:ss ")+s+"\r\n");});}catch{}}
    async Task StartServer(){
        if(server!=null)return;
        server=new HttpListener();server.Prefixes.Add("http://127.0.0.1:8765/");cts=new();
        try{server.Start();start.Enabled=false;Log("Listening on 127.0.0.1:8765");
            while(server.IsListening){HttpListenerContext ctx;try{ctx=await server.GetContextAsync();}catch{break;} _=Handle(ctx,cts.Token);}
        }catch(Exception e){Log("Server error: "+e.Message);StopServer();}
    }
    void StopServer(){cts?.Cancel();try{server?.Stop();server?.Close();}catch{}server=null;cts?.Dispose();cts=null;start.Enabled=true;Log("Stopped");}
    bool TokenOk(string supplied){byte[] a=Encoding.UTF8.GetBytes(supplied),b=Encoding.UTF8.GetBytes(tokenBox.Text);return a.Length==b.Length&&CryptographicOperations.FixedTimeEquals(a,b);}
    async Task Handle(HttpListenerContext c,CancellationToken ct){
        try{
            if(c.Request.HttpMethod!="POST"){await Reply(c,405,new{error="POST required"});return;}
            if(!TokenOk(c.Request.Headers["X-PCBridge-Token"]??"")){await Reply(c,401,new{error="Unauthorized"});return;}
            if(c.Request.ContentLength64>65536){await Reply(c,413,new{error="Request limit 64KB"});return;}
            using var ms=new MemoryStream();var buf=new byte[8192];int n;while((n=await c.Request.InputStream.ReadAsync(buf,ct))>0){if(ms.Length+n>65536){await Reply(c,413,new{error="Request limit 64KB"});return;}ms.Write(buf,0,n);}
            using var doc=JsonDocument.Parse(ms.ToArray());var root=doc.RootElement;string action=root.GetProperty("action").GetString()??"";
            if(action=="status"){await Reply(c,200,new{ok=true,agent="PCBridge",version="0.2",listening=true,display=Screen.PrimaryScreen?.Bounds.ToString(),active_commands=Volatile.Read(ref active),serialized=true});return;}
            await commandLock.WaitAsync(ct);Interlocked.Increment(ref active);
            try{
                object result=action switch{
                    "screenshot"=>Screenshot(),
                    "open_url"=>OpenUrl(root.GetProperty("url").GetString()??""),
                    "open_folder"=>OpenFolder(root.GetProperty("path").GetString()??""),
                    "click"=>Click(root.GetProperty("x").GetInt32(),root.GetProperty("y").GetInt32()),
                    "type"=>TypeUnicode(root.GetProperty("text").GetString()??""),
                    "key"=>Key(root.GetProperty("key").GetString()??""),
                    "run_confirmed"=>await RunPowerShell(root.GetProperty("command").GetString()??""),
                    "file_list"=>FileList(root.GetProperty("path").GetString()??""),
                    "file_read"=>FileRead(root.GetProperty("path").GetString()??""),
                    "file_write"=>FileWrite(root.GetProperty("path").GetString()??"",root.GetProperty("content").GetString()??""),
                    "file_hash"=>FileHash(root.GetProperty("path").GetString()??""),
                    _=>new{error="Unknown action"}
                };
                await Reply(c,200,new{ok=true,action,result});
            }finally{Interlocked.Decrement(ref active);commandLock.Release();}
        }catch(JsonException e){await Reply(c,400,new{error="Invalid JSON",detail=e.Message});}
        catch(OperationCanceledException){await Reply(c,503,new{error="Agent stopping"});}
        catch(Exception e){await Reply(c,400,new{error=e.Message});}
    }
    string ResolveAllowed(string relativeOrFull){
        string root=Path.GetFullPath(rootBox.Text.Trim());if(!Directory.Exists(root))throw new Exception("Allowed root folder does not exist");
        string full=Path.GetFullPath(Path.IsPathRooted(relativeOrFull)?relativeOrFull:Path.Combine(root,relativeOrFull));
        string prefix=root.TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar)+Path.DirectorySeparatorChar;
        if(!full.Equals(root,StringComparison.OrdinalIgnoreCase)&&!full.StartsWith(prefix,StringComparison.OrdinalIgnoreCase))throw new Exception("Path is outside configured allowed root");
        return full;
    }
    object FileList(string path){string p=ResolveAllowed(path);if(!Directory.Exists(p))throw new Exception("Directory not found");return new{path=p,dirs=Directory.GetDirectories(p).Select(Path.GetFileName),files=Directory.GetFiles(p).Select(x=>new FileInfo(x)).Select(x=>new{name=x.Name,size=x.Length,modified=x.LastWriteTimeUtc})};}
    object FileRead(string path){string p=ResolveAllowed(path);if(!File.Exists(p))throw new Exception("File not found");var fi=new FileInfo(p);if(fi.Length>2_000_000)throw new Exception("File exceeds 2 MB; use targeted log extraction");return new{path=p,content=File.ReadAllText(p),size=fi.Length};}
    object FileWrite(string path,string content){string p=ResolveAllowed(path);if(content.Length>1_000_000)throw new Exception("Write limit 1 MB");Directory.CreateDirectory(Path.GetDirectoryName(p)!);File.WriteAllText(p,content,new UTF8Encoding(false));return new{path=p,written=true,bytes=new FileInfo(p).Length};}
    object FileHash(string path){string p=ResolveAllowed(path);if(!File.Exists(p))throw new Exception("File not found");using var sha=SHA256.Create();return new{path=p,sha256=Convert.ToHexString(sha.ComputeHash(File.ReadAllBytes(p)))};}
    string OpenUrl(string u){if(!Uri.TryCreate(u,UriKind.Absolute,out var z)||(z.Scheme!="https"&&z.Scheme!="http"))throw new Exception("Only HTTP(S) URLs allowed");Process.Start(new ProcessStartInfo(z.ToString()){UseShellExecute=true});return "opened";}
    string OpenFolder(string p){if(!Directory.Exists(p))throw new Exception("Folder does not exist");Process.Start(new ProcessStartInfo("explorer.exe"){UseShellExecute=true,ArgumentList={p}});return "opened";}
    object Screenshot(){var b=Screen.PrimaryScreen?.Bounds??throw new Exception("No display");using var bmp=new Bitmap(b.Width,b.Height);using(var g=Graphics.FromImage(bmp))g.CopyFromScreen(b.Location,Point.Empty,b.Size);using var ms=new MemoryStream();bmp.Save(ms,System.Drawing.Imaging.ImageFormat.Png);return new{mime="image/png",base64=Convert.ToBase64String(ms.ToArray()),width=b.Width,height=b.Height};}
    string Click(int x,int y){var b=Screen.PrimaryScreen?.Bounds??throw new Exception("No display");if(x<0||y<0||x>=b.Width||y>=b.Height)throw new Exception("Coordinates outside screen");SetCursorPos(x,y);mouse_event(MOUSE_DOWN,0,0,0,UIntPtr.Zero);mouse_event(MOUSE_UP,0,0,0,UIntPtr.Zero);return "clicked";}
    string TypeUnicode(string s){if(s.Length>2000)throw new Exception("Text limit 2000 chars");var arr=new List<INPUT>(s.Length*2);foreach(char ch in s){arr.Add(new INPUT{type=INPUT_KEYBOARD,U=new InputUnion{ki=new KEYBDINPUT{wScan=ch,dwFlags=KEYEVENTF_UNICODE}}});arr.Add(new INPUT{type=INPUT_KEYBOARD,U=new InputUnion{ki=new KEYBDINPUT{wScan=ch,dwFlags=KEYEVENTF_UNICODE|KEYEVENTF_KEYUP}}});}if(arr.Count>0&&SendInput((uint)arr.Count,arr.ToArray(),Marshal.SizeOf<INPUT>())!=arr.Count)throw new Exception("SendInput failed");return $"typed {s.Length} UTF-16 chars";}
    string Key(string s){var map=new Dictionary<string,ushort>(StringComparer.OrdinalIgnoreCase){{"ENTER",0x0D},{"ESC",0x1B},{"TAB",0x09},{"SPACE",0x20},{"BACKSPACE",0x08},{"F10",0x79},{"UP",0x26},{"DOWN",0x28},{"LEFT",0x25},{"RIGHT",0x27}};if(!map.TryGetValue(s,out ushort vk))throw new Exception("Key not allowlisted");var a=new[]{new INPUT{type=INPUT_KEYBOARD,U=new InputUnion{ki=new KEYBDINPUT{wVk=vk}}},new INPUT{type=INPUT_KEYBOARD,U=new InputUnion{ki=new KEYBDINPUT{wVk=vk,dwFlags=KEYEVENTF_KEYUP}}}};if(SendInput(2,a,Marshal.SizeOf<INPUT>())!=2)throw new Exception("SendInput failed");return "key sent";}
    async Task<object> RunPowerShell(string cmd){if(cmd.Length>4000)throw new Exception("Command limit 4000 chars");var consent=await AskConsent(cmd);if(!consent)return new{cancelled=true};using var p=new Process();p.StartInfo=new ProcessStartInfo("powershell.exe"){UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true,CreateNoWindow=true};p.StartInfo.ArgumentList.Add("-NoProfile");p.StartInfo.ArgumentList.Add("-NonInteractive");p.StartInfo.ArgumentList.Add("-Command");p.StartInfo.ArgumentList.Add(cmd);p.Start();var stdout=p.StandardOutput.ReadToEndAsync();var stderr=p.StandardError.ReadToEndAsync();using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(15));try{await p.WaitForExitAsync(timeout.Token);}catch(OperationCanceledException){p.Kill(true);return new{timeout=true};}return new{exit_code=p.ExitCode,stdout=await stdout,stderr=await stderr};}
    Task<bool> AskConsent(string cmd){var tcs=new TaskCompletionSource<bool>();BeginInvoke(()=>tcs.SetResult(MessageBox.Show(this,"Allow PowerShell command?\n\n"+cmd,"PCBridge consent",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)==DialogResult.Yes));return tcs.Task;}
    static async Task Reply(HttpListenerContext c,int status,object value){try{byte[] b=JsonSerializer.SerializeToUtf8Bytes(value);c.Response.StatusCode=status;c.Response.ContentType="application/json; charset=utf-8";c.Response.ContentLength64=b.Length;await c.Response.OutputStream.WriteAsync(b);c.Response.Close();}catch{}}
    protected override void OnFormClosing(FormClosingEventArgs e){StopServer();base.OnFormClosing(e);}
}