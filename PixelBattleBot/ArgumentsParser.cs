using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelBattleBot
{
    public class ArgumentsParser
    {
        private Dictionary<string, Action<Arg>> Args = new Dictionary<string, Action<Arg>>();
        public ArgumentsParser() 
        { 
            
        
        }
        public void Parse(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (Args.ContainsKey(args[i]))
                {
                    Arg arg = new Arg();
                    arg.Args = args.Skip(i).ToArray();
                    Args[args[i]].Invoke(arg);
                    i += arg.Skip;
                }
            }
        }
        public void Register(string arg, Action<Arg> action)
        {
            Args[arg] = action;
        }
    }

    public class Arg
    {
        public int Skip { get; set; }
        public string[] Args { get; set; } = null!;
    }
}
