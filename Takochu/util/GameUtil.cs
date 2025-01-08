using System;
using System.Collections.Generic;
using System.Linq;

namespace Takochu.util
{
    public static class GameUtil
    {
        private static Game sGame;

        public static readonly string[] GalaxyLayersCommon = new string[]
        {
            "Common",
            "LayerA",
            "LayerB",
            "LayerC",
            "LayerD",
            "LayerE",
            "LayerF",
            "LayerG",
            "LayerH",
            "LayerI",
            "LayerJ",
            "LayerK",
            "LayerL",
            "LayerM",
            "LayerN",
            "LayerO",
            "LayerP"
        };

        public static readonly string[] GalaxyLayers = new string[]
        {
            "LayerA",
            "LayerB",
            "LayerC",
            "LayerD",
            "LayerE",
            "LayerF",
            "LayerG",
            "LayerH",
            "LayerI",
            "LayerJ",
            "LayerK",
            "LayerL",
            "LayerM",
            "LayerN",
            "LayerO",
            "LayerP"
        };

        public static List<string> GetGalaxyLayers(int mask)
        {
            List<string> layers = new List<string>();
            // 1,2 で先頭が大文字小文字がちがう。
            if (GameUtil.IsSMG1()) {
                layers = new List<string>
                {
                    "common",
                };
            }
            else if (GameUtil.IsSMG2())
            {
                layers = new List<string>
                {
                    "Common",
                };
            }

            // 16 はゾーンフラグがu16型に保存されているから？
            for (int i = 0; i < 16; i++)
            {
                if (((mask >> i) & 0x1) != 0x0)
                {
                    if (GameUtil.IsSMG1())
                    {
                        layers.Add(GameUtil.GalaxyLayers[i].ToLower());
                    }
                    else
                    {
                        layers.Add(GameUtil.GalaxyLayers[i]);
                    }
                }
            }

            return layers;
        }

        public static int SetLayerOnMask(string layer, int mask, bool setBit)
        {
            int idx;
            if (GameUtil.IsSMG1())
                idx = GalaxyLayers.Select(x => x.ToLower()).ToList().IndexOf(layer);
            else
                idx = GalaxyLayers.ToList().IndexOf(layer);

            if (setBit)
                mask |= 1 << idx;
            else
                mask &= ~(1 << idx);

            return mask;
        }

        public enum Game
        {
            SMG1,
            SMG2
        }

        public static void SetGame(Game game)
        {
            sGame = game;
        }

        public static bool IsSMG1()
        {
            return sGame == Game.SMG1;
        }

        public static bool IsSMG2()
        {
            return sGame == Game.SMG2;
        }
    }
}
