using PixelBattleBotCore.Model;
using PixelBattleBotCore.Impl;
using PixelBattleBotCore.Extensions;
using System.Net;
using System.Text;
using System.Threading.Channels;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using PixelBattleBotCore;
using PixelBattleBot;
using System.Reactive.Linq;
using PixelBattleBotCore.Comparers;
using PixelBattleBotCore.Abstractions;
using TwoCaptcha;
using System.Text.Json.Nodes;
using System.Text.Json;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

if (!Directory.Exists("captcha"))
    Directory.CreateDirectory("captcha");

if (!Directory.Exists("images"))
    Directory.CreateDirectory("images");

CancellationTokenSource cts = new CancellationTokenSource();

Uzas uzas = new Uzas();
uzas.Parse(args);

Config config = new Config();
if (File.Exists("config.json"))
{
    config = JsonSerializer.Deserialize<Config>(File.ReadAllText("config.json")) ?? new Config();
}
else
{
    File.WriteAllText("config.json", JsonSerializer.Serialize(config));
}

if (uzas.ApiKey != null)
    config.RuCaptchaApiKey = uzas.ApiKey;

if (uzas.Img != null)
{
    config.Image = uzas.Img;
    config.ImageX = uzas.X;
    config.ImageY = uzas.Y;
}

if (uzas.Workers != null)
{
    config.Workers = uzas.Workers;
}
else if (config.Workers == null)
{
    config.Workers = 10;
}

//SaveConfig
File.WriteAllText("config.json", JsonSerializer.Serialize(config));

if (config.Image == null)
{
    Console.WriteLine("Нету изображения");
    return;
}

//и убраг ограничения на потоки

Image<Rgba32> image = new Image<Rgba32>(1590, 400);
ImageSharpPBImage imageSharpPBImage = new ImageSharpPBImage(image);

using FileStream fs = new FileStream(config.Image, FileMode.Open, FileAccess.Read);
Image<Rgba32> current = Image.Load<Rgba32>(fs);
ImageSharpPBImage currentImage = new ImageSharpPBImage(current);
currentImage.Constraint();
currentImage.Commit();
PixelBattleTask pixelBattleTask = new PixelBattleTask(currentImage, config.ImageX, config.ImageY);

PixelBattleBotMaster pixelBattleBot = new PixelBattleBotMaster();

if (config.RuCaptchaApiKey != null)
{
    TwoCaptcha.TwoCaptcha twoCaptcha = new TwoCaptcha.TwoCaptcha(config.RuCaptchaApiKey);
    HttpClientHandler handler = new HttpClientHandler();
    if (config.RuCaptchaProxy != null)
    {
        handler.UseProxy = true;
        WebProxy webProxy = new WebProxy(config.RuCaptchaProxy);
        if (config.RuCaptchaProxyUser != null)
            webProxy.Credentials = new NetworkCredential(config.RuCaptchaProxyUser, config.RuCaptchaProxyPassw);
        handler.Proxy = webProxy;
    }
    HttpClient client = new HttpClient(handler);
    twoCaptcha.SetApiClient(new SuperApiClient(client));
    if (!uzas.IV)
        pixelBattleBot.VkCaptchaSolver = new RuCaptchaVk(twoCaptcha);
    pixelBattleBot.PBCaptchaSolver = new RuCaptchaPb(twoCaptcha);
}

pixelBattleBot.SetTask(pixelBattleTask);
pixelBattleBot.SetPlace(imageSharpPBImage);
pixelBattleBot.SuperPixels = uzas.M;

if (uzas.Proxy != null)
{
    List<IWebProxy> proxies = new List<IWebProxy>();

    foreach (var item in File.ReadAllLines(uzas.Proxy))
    {
        try
        {
            proxies.Add(new WebProxy(item));
        }
        catch
        {

        }

    }
    pixelBattleBot.ProxyFactory = new ProxyFactory(proxies.ToArray());
}

if (uzas.Accounts != null)
{
    List<BotInfo> bots = new List<BotInfo>();
    string[] lines = File.ReadAllLines(uzas.Accounts);
    bool first = true;
    foreach (var line in lines)
    {
        string[] auth = line.Split(':');
        BotInfo botInfo = BotInfo.CrateByPassword(auth[0], auth[1]);
        if (!first && uzas.Proxy != null)
        {
            botInfo.ProxyRequired = true;
            botInfo.ReplaceProxyIfDied = true;
            botInfo.WebProxy = pixelBattleBot.ProxyFactory.GetProxy(0);
        }
        first = false;
        bots.Add(botInfo);
    }
    await pixelBattleBot.AddBots(bots.ToArray());
}

if (uzas.R)
{
    Task renderer = Task.Run(() => Renderer());
}
Task task = pixelBattleBot.Start(config.Workers ?? 10, cts.Token);
Task a = NextPixel(pixelBattleBot);

Console.ReadLine();

Console.WriteLine("Cancelling");

cts.Cancel();

await task;

//Rectangle rectangle = new Rectangle(801, 275, 109, 125);

/*
botInfo.Bot!.PacketObserver.Where(x => BotFilters.RectFilter(rectangle, x))
                           .Where(x => BotFilters.ImageFilter(filterImg, 801, 275, x))
                           .Subscribe(new BotObserver());
*/

//FileStream fileStream = new FileStream("images/" + "last" + ".png", FileMode.Open, FileAccess.Write, FileShare.Read, 4096, true);


async Task Renderer()
{
    while (true)
    {
        imageSharpPBImage.Render();
        await image.SaveAsPngAsync("images/" + DateTime.UtcNow.ToString("dd-MM-yyyy-HH-mm-ss") + ".png");
        //await image.SaveAsPngAsync(fileStream);
        //await fileStream.FlushAsync();
        //fileStream.Seek(0, SeekOrigin.Begin);
        await Task.Delay(10000);
    }
}

async Task NextPixel(PixelBattleBotMaster master)
{

    while (true)
    {
        var bots = master.Bots.Where(x => x.Bot != null).Select(x => x.Bot);
        IBot? next = bots.Where(x => x!.BotEnabled && x.BotReady).MinBy(x => x!.NextTryAllowAt);
        if (next != null)
            Console.WriteLine($"Next pixel after {next.NextTryAllowAt - DateTime.Now}");

        int count = bots.Count(x => x!.BotReady);
        int superPixel = bots.Sum(x => x?.Pixel ?? 0);
        Console.WriteLine($"{count} ready");
        Console.WriteLine($"superPixel {superPixel}");
        int cnt = master.PixelBattleTask!.Image.Width  * master.PixelBattleTask!.Image.Height;
        Console.WriteLine($"Progress: {cnt - master.Progress}/{cnt} ({(float)(cnt - master.Progress) * 100 / cnt :f2}%)");
        await Task.Delay(10000);
    }
}