using PixelBattleBotCore.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelBattleBotCore.Abstractions
{
    public interface IVkCaptchaSolver
    {
        Task<ICaptchaSolverResponse> Solve(VkCaptcha captcha);
    }
}
