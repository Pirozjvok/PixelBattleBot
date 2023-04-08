using PixelBattleBotCore.Abstractions;
using PixelBattleBotCore.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwoCaptcha.Captcha;

namespace PixelBattleBotCore.Impl
{
    public class RuCaptchaPb : IPBCaptchaSolver
    {
        TwoCaptcha.TwoCaptcha TwoCaptcha;
        public RuCaptchaPb(TwoCaptcha.TwoCaptcha twoCaptcha)
        {
            TwoCaptcha = twoCaptcha;
        }
        public async Task<ICaptchaSolverResponse> Solve(PixelBattleCaptcha captcha)
        {
            Normal normal = new Normal();
            normal.SetFile(captcha.Path);
            normal.SetMaxLen(4);
            normal.SetMinLen(4);
            normal.SetLang("en");
            normal.SetCaseSensitive(true);
            await TwoCaptcha.Solve(normal);
            return new RuCaptchaResponse(normal.Id, normal.Code, TwoCaptcha);
        }
    }
}
