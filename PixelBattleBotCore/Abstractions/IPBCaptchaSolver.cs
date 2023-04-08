using PixelBattleBotCore.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelBattleBotCore.Abstractions
{
    public interface IPBCaptchaSolver
    {
        Task<ICaptchaSolverResponse> Solve(PixelBattleCaptcha captcha);
    }
}
