using PixelBattleBotCore.Model;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PixelBattleBot
{
    public class BotObserver : IObserver<PixelBattlePacket>
    {
        private FileStream fileStream;

        private StreamWriter writer;

        private Dictionary<uint, string> UsersCache = new Dictionary<uint, string>();

        private Dictionary<uint, string> GroupsCache = new Dictionary<uint, string>();

        private HttpClient HttpClient = new HttpClient();
        public BotObserver()
        {
            fileStream = new FileStream("logs/log.txt", FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read, 4096, true); 
            writer = new StreamWriter(fileStream);
        }
        public void OnCompleted()
        {
            
        }
        public void OnError(Exception error)
        {
            
        }

        private static readonly Regex userRegex = new Regex(@"<ya:firstName>(.*?)</ya:firstName>(.|\n)*<ya:secondName>(.*?)</ya:secondName>", RegexOptions.Compiled);
        private string GetUser(uint id)
        {
            if (UsersCache.ContainsKey(id))
                return UsersCache[id];

            string info = HttpClient.GetStringAsync($"https://vk.com/foaf.php?id={id}").Result;
            Match match = userRegex.Match(info);
            string result = match.Groups[1].Value + " " + match.Groups[3].Value;
            UsersCache[id] = result;
            return result;
        }
        public void OnNext(PixelBattlePacket packet)
        {
            string data = string.Format("{0} {1} {2} {3} {4} {5} {6}", packet.PixelData.X, packet.PixelData.Y, packet.PixelData.Color, packet.UserId, packet.GroupId, Typ(packet.PixelData.Flag), GetUser(packet.UserId));
            Console.WriteLine(data);
            writer.WriteLine(data);
            writer.Flush();
        }

        public string Typ(int flag)
        {
            switch (flag)
            {
                case 0:
                    return "SinglePixel";
                case 1:
                    return "Bomb";
                case 2:
                    return "Freeze";
                case 3:
                    return "SuperPixel";
                default:
                    return "xz";
            }
        }
    }

    public static class BotFilters
    {
        public static bool RectFilter(Rectangle rect, PixelBattlePacket packet)
        {
            return rect.Contains(packet.PixelData.X, packet.PixelData.Y);
        }

        public static bool GroupFilter(uint[] groups,  PixelBattlePacket packet)
        {
            return !groups.Contains(packet.GroupId);
        }

        public static bool ImageFilter(PixelBattleImage image, int x, int y, PixelBattlePacket packet)
        {
            int xx = packet.PixelData.X - x;
            int yy = packet.PixelData.Y - y;
            if (xx < 0 && yy < 0)
                return false;

            return image.GetPixel(xx, yy) != packet.PixelData.Color;
        }
    }

}
