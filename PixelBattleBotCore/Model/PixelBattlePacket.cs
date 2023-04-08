using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelBattleBotCore.Model
{
    public class PixelBattlePacket
    {
        public PixelData PixelData { get; set; }
        public uint UserId { get; set; }
        public uint GroupId { get; set; }
        private PixelBattlePacket()
        {
            PixelData = null!;
        }
        public PixelBattlePacket(PixelData pixelData, uint userId, uint groupId)
        {
            this.PixelData = pixelData;
            this.UserId = userId;
            this.GroupId = groupId;
        }
        public static PixelBattlePacket Parse(PlaceInfo placeInfo, byte[] data, int offset)
        {
            return new PixelBattlePacket()
            {
                UserId = BitConverter.ToUInt32(data, offset + 4),
                GroupId = BitConverter.ToUInt32(data, offset + 8),
                PixelData = PixelData.Parse(placeInfo, data, offset),
            };
        }
    }
}
