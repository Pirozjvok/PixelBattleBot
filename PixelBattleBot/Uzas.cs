using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelBattleBot
{
    public class Uzas
    {
        private ArgumentsParser parser;
        public string? Accounts { get; set; }
        public int X { get; set;}
        public int Y { get; set;}
        public string? Img { get; set; }
        public int? Workers { get; set; }
        public bool R { get; set; }
        public bool IV { get; set; }
        public int M { get; set; }

        public string? Proxy { get; set; }
        public string? ApiKey { get; set; }
        public Uzas() 
        { 
            parser = new ArgumentsParser();
            parser.Register("-A", ArgA); //Аккаунты -A accs.txt
            parser.Register("-I", ArgImg); //Картинка -I x y "c:\iad\asda.png"
            parser.Register("-T", ArgT); // -T Api от рекапчи
            parser.Register("-W", ArgW); // -W 100 количество потоков
            parser.Register("-R", ArgR); // -R запись полотна в папку images
            parser.Register("-IV", ArgIV); //Игнорить капчу вк
            parser.Register("-M", ArgIV); //Количество молний
            parser.Register("-P", ArgP); //Прокси лист
        }

        public void Parse(string[] args)
        {
            parser.Parse(args);
        }
        public void ArgA(Arg arg)
        {
            arg.Skip = 1;
            Accounts = arg.Args[1];
        }


        public void ArgImg(Arg arg)
        {
            arg.Skip = 3;
            X = int.Parse(arg.Args[1]);
            Y = int.Parse(arg.Args[2]);
            Img = arg.Args[3];
        }

        public void ArgT(Arg arg)
        {
            arg.Skip = 1;
            ApiKey = arg.Args[1];
        }

        public void ArgW(Arg arg)
        {
            arg.Skip = 1;
            Workers = int.Parse(arg.Args[1]);
        }

        public void ArgM(Arg arg)
        {
            arg.Skip = 1;
            M = int.Parse(arg.Args[1]);
        }
        public void ArgR(Arg arg)
        {
            arg.Skip = 0;
            R = true;
        }

        public void ArgIV(Arg arg)
        {
            arg.Skip = 0;
            IV = true;
        }

        public void ArgP(Arg arg)
        {
            arg.Skip = 1;
            Proxy = arg.Args[1];
        }
    }

    //Сохдать директорию с капчей
}
