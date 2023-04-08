using PixelBattleBotCore.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwoCaptcha.Captcha;

namespace PixelBattleBotCore.Impl
{
    public class RuCaptchaResponse : ICaptchaSolverResponse
    {
        private readonly string _captchaId;

        private readonly string _code;

        private readonly TwoCaptcha.TwoCaptcha _twoCaptcha;
        public RuCaptchaResponse(string captchaId, string code, TwoCaptcha.TwoCaptcha twoCaptcha)
        {
            _captchaId = captchaId;
            _code = code;
            _twoCaptcha = twoCaptcha;
        }
        public string Code => _code;
        public async Task Report(bool result)
        {
            await _twoCaptcha.Report(_captchaId, result);
        }
    }
}
