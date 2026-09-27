namespace RuriLib.Models
{
    /// <summary>
    /// A simple RGBA color struct that replaces System.Windows.Media.Color
    /// for cross-platform compatibility.
    /// </summary>
    public struct Color
    {
        public byte A;
        public byte R;
        public byte G;
        public byte B;

        public Color(byte a, byte r, byte g, byte b)
        {
            A = a; R = r; G = g; B = b;
        }

        public static Color FromRgb(byte r, byte g, byte b) => new Color(255, r, g, b);
        public static Color FromArgb(byte a, byte r, byte g, byte b) => new Color(a, r, g, b);

        public override string ToString() => $"#{A:X2}{R:X2}{G:X2}{B:X2}";

        public static implicit operator Color(System.Drawing.Color c) => new Color(c.A, c.R, c.G, c.B);

        public override bool Equals(object obj) => obj is Color c && c.A == A && c.R == R && c.G == G && c.B == B;
        public override int GetHashCode() => (A << 24) | (R << 16) | (G << 8) | B;
        public static bool operator ==(Color a, Color b) => a.Equals(b);
        public static bool operator !=(Color a, Color b) => !a.Equals(b);
    }

    /// <summary>
    /// Static color definitions replacing System.Windows.Media.Colors.
    /// </summary>
    public static class Colors
    {
        public static readonly Color AliceBlue = new Color(255, 240, 248, 255);
        public static readonly Color AntiqueWhite = new Color(255, 250, 235, 215);
        public static readonly Color Aqua = new Color(255, 0, 255, 255);
        public static readonly Color Aquamarine = new Color(255, 127, 255, 212);
        public static readonly Color Azure = new Color(255, 240, 255, 255);
        public static readonly Color Beige = new Color(255, 245, 245, 220);
        public static readonly Color Bisque = new Color(255, 255, 228, 196);
        public static readonly Color Black = new Color(255, 0, 0, 0);
        public static readonly Color BlancheDAlmond = new Color(255, 255, 235, 205);
        public static readonly Color Blue = new Color(255, 0, 0, 255);
        public static readonly Color BlueViolet = new Color(255, 138, 43, 226);
        public static readonly Color Brown = new Color(255, 165, 42, 42);
        public static readonly Color BurlyWood = new Color(255, 222, 184, 135);
        public static readonly Color CadetBlue = new Color(255, 95, 158, 160);
        public static readonly Color Chartreuse = new Color(255, 127, 255, 0);
        public static readonly Color Chocolate = new Color(255, 210, 105, 30);
        public static readonly Color Coral = new Color(255, 255, 127, 80);
        public static readonly Color CornflowerBlue = new Color(255, 100, 149, 237);
        public static readonly Color Cornsilk = new Color(255, 255, 248, 220);
        public static readonly Color Crimson = new Color(255, 220, 20, 60);
        public static readonly Color Cyan = new Color(255, 0, 255, 255);
        public static readonly Color DarkBlue = new Color(255, 0, 0, 139);
        public static readonly Color DarkCyan = new Color(255, 0, 139, 139);
        public static readonly Color DarkGoldenrod = new Color(255, 184, 134, 11);
        public static readonly Color DarkGray = new Color(255, 169, 169, 169);
        public static readonly Color DarkGreen = new Color(255, 0, 100, 0);
        public static readonly Color DarkKhaki = new Color(255, 189, 183, 107);
        public static readonly Color DarkMagenta = new Color(255, 139, 0, 139);
        public static readonly Color DarkOliveGreen = new Color(255, 85, 107, 47);
        public static readonly Color DarkOrange = new Color(255, 255, 140, 0);
        public static readonly Color DarkOrchid = new Color(255, 153, 50, 204);
        public static readonly Color DarkRed = new Color(255, 139, 0, 0);
        public static readonly Color DarkSalmon = new Color(255, 233, 150, 122);
        public static readonly Color DarkSeaGreen = new Color(255, 143, 188, 143);
        public static readonly Color DarkSlateBlue = new Color(255, 72, 61, 139);
        public static readonly Color DarkSlateGray = new Color(255, 47, 79, 79);
        public static readonly Color DarkTurquoise = new Color(255, 0, 206, 209);
        public static readonly Color DarkViolet = new Color(255, 148, 0, 211);
        public static readonly Color DeepPink = new Color(255, 255, 20, 147);
        public static readonly Color DeepSkyBlue = new Color(255, 0, 191, 255);
        public static readonly Color DimGray = new Color(255, 105, 105, 105);
        public static readonly Color DodgerBlue = new Color(255, 30, 144, 255);
        public static readonly Color Firebrick = new Color(255, 178, 34, 34);
        public static readonly Color FloralWhite = new Color(255, 255, 250, 240);
        public static readonly Color ForestGreen = new Color(255, 34, 139, 34);
        public static readonly Color Fuchsia = new Color(255, 255, 0, 255);
        public static readonly Color Gainsboro = new Color(255, 220, 220, 220);
        public static readonly Color GhostWhite = new Color(255, 248, 248, 255);
        public static readonly Color Gold = new Color(255, 255, 215, 0);
        public static readonly Color Goldenrod = new Color(255, 218, 165, 32);
        public static readonly Color Gray = new Color(255, 128, 128, 128);
        public static readonly Color Green = new Color(255, 0, 128, 0);
        public static readonly Color GreenYellow = new Color(255, 173, 255, 47);
        public static readonly Color Honeydew = new Color(255, 240, 255, 240);
        public static readonly Color HotPink = new Color(255, 255, 105, 180);
        public static readonly Color IndianRed = new Color(255, 205, 92, 92);
        public static readonly Color Indigo = new Color(255, 75, 0, 130);
        public static readonly Color Ivory = new Color(255, 255, 255, 240);
        public static readonly Color Khaki = new Color(255, 240, 230, 140);
        public static readonly Color Lavender = new Color(255, 230, 230, 250);
        public static readonly Color LavenderBlush = new Color(255, 255, 240, 245);
        public static readonly Color LawnGreen = new Color(255, 124, 252, 0);
        public static readonly Color LemonChiffon = new Color(255, 255, 250, 205);
        public static readonly Color LightBlue = new Color(255, 173, 216, 230);
        public static readonly Color LightCoral = new Color(255, 240, 128, 128);
        public static readonly Color LightCyan = new Color(255, 224, 255, 255);
        public static readonly Color LightGoldenrodYellow = new Color(255, 250, 250, 210);
        public static readonly Color LightGray = new Color(255, 211, 211, 211);
        public static readonly Color LightGreen = new Color(255, 144, 238, 144);
        public static readonly Color LightPink = new Color(255, 255, 182, 193);
        public static readonly Color LightSalmon = new Color(255, 255, 160, 122);
        public static readonly Color LightSeaGreen = new Color(255, 32, 178, 170);
        public static readonly Color LightSkyBlue = new Color(255, 135, 206, 250);
        public static readonly Color LightSlateGray = new Color(255, 119, 136, 153);
        public static readonly Color LightSteelBlue = new Color(255, 176, 196, 222);
        public static readonly Color LightYellow = new Color(255, 255, 255, 224);
        public static readonly Color Lime = new Color(255, 0, 255, 0);
        public static readonly Color LimeGreen = new Color(255, 50, 205, 50);
        public static readonly Color Linen = new Color(255, 250, 240, 230);
        public static readonly Color Magenta = new Color(255, 255, 0, 255);
        public static readonly Color Maroon = new Color(255, 128, 0, 0);
        public static readonly Color MediumAquamarine = new Color(255, 102, 205, 170);
        public static readonly Color MediumBlue = new Color(255, 0, 0, 205);
        public static readonly Color MediumOrchid = new Color(255, 186, 85, 211);
        public static readonly Color MediumPurple = new Color(255, 147, 112, 219);
        public static readonly Color MediumSeaGreen = new Color(255, 60, 179, 113);
        public static readonly Color MediumSlateBlue = new Color(255, 123, 104, 238);
        public static readonly Color MediumSpringGreen = new Color(255, 0, 250, 154);
        public static readonly Color MediumTurquoise = new Color(255, 72, 209, 204);
        public static readonly Color MediumVioletRed = new Color(255, 199, 21, 133);
        public static readonly Color MidnightBlue = new Color(255, 25, 25, 112);
        public static readonly Color MintCream = new Color(255, 245, 255, 250);
        public static readonly Color MistyRose = new Color(255, 255, 228, 225);
        public static readonly Color Moccasin = new Color(255, 255, 228, 181);
        public static readonly Color NavajoWhite = new Color(255, 255, 222, 173);
        public static readonly Color Navy = new Color(255, 0, 0, 128);
        public static readonly Color OldLace = new Color(255, 253, 245, 230);
        public static readonly Color Olive = new Color(255, 128, 128, 0);
        public static readonly Color OliveDrab = new Color(255, 107, 142, 35);
        public static readonly Color Orange = new Color(255, 255, 165, 0);
        public static readonly Color OrangeRed = new Color(255, 255, 69, 0);
        public static readonly Color Orchid = new Color(255, 218, 112, 214);
        public static readonly Color PaleGoldenrod = new Color(255, 238, 232, 170);
        public static readonly Color PaleGreen = new Color(255, 152, 251, 152);
        public static readonly Color PaleTurquoise = new Color(255, 175, 238, 238);
        public static readonly Color PaleVioletRed = new Color(255, 219, 112, 147);
        public static readonly Color PapayaWhip = new Color(255, 255, 239, 213);
        public static readonly Color PeachPuff = new Color(255, 255, 218, 185);
        public static readonly Color Peru = new Color(255, 205, 133, 63);
        public static readonly Color Pink = new Color(255, 255, 192, 203);
        public static readonly Color Plum = new Color(255, 221, 160, 221);
        public static readonly Color PowderBlue = new Color(255, 176, 224, 230);
        public static readonly Color Purple = new Color(255, 128, 0, 128);
        public static readonly Color Red = new Color(255, 255, 0, 0);
        public static readonly Color RosyBrown = new Color(255, 188, 143, 143);
        public static readonly Color RoyalBlue = new Color(255, 65, 105, 225);
        public static readonly Color SaddleBrown = new Color(255, 139, 69, 19);
        public static readonly Color Salmon = new Color(255, 250, 128, 114);
        public static readonly Color SandyBrown = new Color(255, 244, 164, 96);
        public static readonly Color SeaGreen = new Color(255, 46, 139, 87);
        public static readonly Color SeaShell = new Color(255, 255, 245, 238);
        public static readonly Color Sienna = new Color(255, 160, 82, 45);
        public static readonly Color Silver = new Color(255, 192, 192, 192);
        public static readonly Color SkyBlue = new Color(255, 135, 206, 235);
        public static readonly Color SlateBlue = new Color(255, 106, 90, 205);
        public static readonly Color SlateGray = new Color(255, 112, 128, 144);
        public static readonly Color Snow = new Color(255, 255, 250, 250);
        public static readonly Color SpringGreen = new Color(255, 0, 255, 127);
        public static readonly Color SteelBlue = new Color(255, 70, 130, 180);
        public static readonly Color Tan = new Color(255, 210, 180, 140);
        public static readonly Color Teal = new Color(255, 0, 128, 128);
        public static readonly Color Thistle = new Color(255, 216, 191, 216);
        public static readonly Color Tomato = new Color(255, 255, 99, 71);
        public static readonly Color Transparent = new Color(0, 0, 0, 0);
        public static readonly Color Turquoise = new Color(255, 64, 224, 208);
        public static readonly Color Violet = new Color(255, 238, 130, 238);
        public static readonly Color Wheat = new Color(255, 245, 222, 179);
        public static readonly Color White = new Color(255, 255, 255, 255);
        public static readonly Color WhiteSmoke = new Color(255, 245, 245, 245);
        public static readonly Color Yellow = new Color(255, 255, 255, 0);
        public static readonly Color YellowGreen = new Color(255, 154, 205, 50);
    }
}
