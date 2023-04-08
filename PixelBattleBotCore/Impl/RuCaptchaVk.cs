using HtmlAgilityPack;
using PixelBattleBotCore.Abstractions;
using PixelBattleBotCore.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwoCaptcha;
using TwoCaptcha.Captcha;

namespace PixelBattleBotCore.Impl
{
    public class RuCaptchaVk : IVkCaptchaSolver
    {
        TwoCaptcha.TwoCaptcha TwoCaptcha;
        public RuCaptchaVk(TwoCaptcha.TwoCaptcha twoCaptcha)
        {
            TwoCaptcha = twoCaptcha;   
        }
        public async Task<ICaptchaSolverResponse> Solve(VkCaptcha captcha)
        {
            Normal normal = new Normal();
            normal.SetBase64(captcha.Base64);
            await TwoCaptcha.Solve(normal);
            return new RuCaptchaResponse(normal.Id, normal.Code, TwoCaptcha);
        }
    }
}
