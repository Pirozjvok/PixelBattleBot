using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelBattleBotCore.Model
{

    [Serializable]
    public class IncorrectCaptchaException : Exception
    {
        public IncorrectCaptchaException() { }
        public IncorrectCaptchaException(string message) : base(message) { }
        public IncorrectCaptchaException(string message, Exception inner) : base(message, inner) { }
        protected IncorrectCaptchaException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
