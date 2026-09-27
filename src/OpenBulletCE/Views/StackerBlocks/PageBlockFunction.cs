using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using RuriLib;
using RuriLib.Functions.Crypto;
using RuriLib.Functions.UserAgent;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace OpenBulletCE.Views.StackerBlocks;

public class PageBlockFunction : BlockPage
{
    private readonly BlockFunction _b;
    private readonly Grid _panels = new();
    private readonly TextBlock _info = new() { TextWrapping = TextWrapping.Wrap, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Avalonia.Thickness(0, 10, 0, 0) };

    private static readonly Dictionary<BlockFunction.Function, string> InfoDic = new()
    {
        { BlockFunction.Function.Constant, "Stores the value of the function's input string in a variable with the chosen name." },
        { BlockFunction.Function.Base64Encode, "Encodes the input string in base64." },
        { BlockFunction.Function.Base64Decode, "Decodes the input string encoded in base64." },
        { BlockFunction.Function.Hash, "Hashes the input string through the specified hashing function" },
        { BlockFunction.Function.HMAC, "Hashes the input string through the specified hashing function using a key." },
        { BlockFunction.Function.Translate, "Translates the content of the function's input string. The dictionary is defined as 'normalname' 'translatedname' each on a different line, like a wordlist." },
        { BlockFunction.Function.DateToUnixTime, "Converts a date in the specified format into a unix time." },
        { BlockFunction.Function.Length, "Computes the length of the input string and returns it." },
        { BlockFunction.Function.ToLowercase, "Turns the input string into lowercase." },
        { BlockFunction.Function.ToUppercase, "Turns the input string into uppercase." },
        { BlockFunction.Function.Replace, "Replaces a substring with another inside the input string." },
        { BlockFunction.Function.RegexMatch, "Matches a regex pattern inside the input string and returns the matched value." },
        { BlockFunction.Function.URLEncode, "Encodes the input string so it can be sent in a URL, replacing dangerous characters with % followed by the hex value." },
        { BlockFunction.Function.URLDecode, "Decodes a URL-encoded input string." },
        { BlockFunction.Function.Unescape, "Unescapes characters that have been escaped with a \\ followed by their hex representation (e.g. \\x5f)." },
        { BlockFunction.Function.HTMLEntityEncode, "Encodes the input string into HTML entities." },
        { BlockFunction.Function.HTMLEntityDecode, "Decodes HTML entities inside the input string." },
        { BlockFunction.Function.UnixTimeToDate, "Converts a unix time to a date of the specified format." },
        { BlockFunction.Function.CurrentUnixTime, "Returns the current unix time." },
        { BlockFunction.Function.UnixTimeToISO8601, "Converts a unix time to an ISO8601 date." },
        { BlockFunction.Function.RandomNum, "Generates a random number between the specified values. Accepts variables parsed by the input string" },
        { BlockFunction.Function.RandomString, "Generates a random string following a mask: ?l lowercase, ?u uppercase, ?d digit, ?s symbol, ?h hex lowercase, ?H hex uppercase, ?m all, ?a alphanumeric." },
        { BlockFunction.Function.Ceil, "Ceils the given number (rounds it to the next integer number if there is something after the comma)." },
        { BlockFunction.Function.Floor, "Floors the given number (rounds it to the previous integer number if there is something after the comma)." },
        { BlockFunction.Function.Round, "Rounds the given number to the nearest integer number." },
        { BlockFunction.Function.Compute, "Computes a mathematical expression e.g. (10+2)*3 will return 36." },
        { BlockFunction.Function.CountOccurrences, "Counts how many times the given string occurs inside the input string." },
        { BlockFunction.Function.ClearCookies, "Clears all the cookies inside the cookie container." },
        { BlockFunction.Function.RSAEncrypt, "Encrypts the input string using a public RSA key (modulus and exponent)." },
        { BlockFunction.Function.RSAPKCS1PAD2, "Encrypts the input string using a public RSA key with PKCS1PAD2 padding." },
        { BlockFunction.Function.Delay, "Delays the execution for the given amount of milliseconds." },
        { BlockFunction.Function.CharAt, "Returns the character at the specified index of the input string." },
        { BlockFunction.Function.Substring, "Returns a substring of the input string between a start index and a given length." },
        { BlockFunction.Function.GetRandomUA, "Generates a random User Agent, optionally for a specific browser." },
        { BlockFunction.Function.AESEncrypt, "Encrypts the input string using AES (must be base64 or the IV won't work)." },
        { BlockFunction.Function.AESDecrypt, "Decrypts the input string using AES." },
        { BlockFunction.Function.PBKDF2PKCS5, "Generates a key using a password based KDF." },
    };

    public PageBlockFunction(BlockFunction block) : base(block)
    {
        _b = block;

        // Function + output
        var fnRow = new DockPanel();
        fnRow.Children.Add(new TextBlock { Text = "Function:", VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(0, 0, 8, 0) });
        var fnCombo = new ComboBox();
        foreach (var f in System.Enum.GetValues<BlockFunction.Function>()) fnCombo.Items.Add(f);
        fnCombo.SelectedItem = _b.FunctionType;
        fnCombo.SelectionChanged += (s, e) =>
        {
            if (fnCombo.SelectedItem is BlockFunction.Function f)
            {
                _b.FunctionType = f;
                UpdatePanels();
                _info.Text = InfoDic.TryGetValue(f, out var d) ? d : "";
            }
        };
        fnRow.Children.Add(fnCombo);
        Panel.Children.Add(fnRow);

        Panel.Children.Add(Row("Input String:", Txt(nameof(BlockFunction.InputString))));

        var capRow = new DockPanel();
        capRow.Children.Add(Check("Is Capture", nameof(BlockFunction.IsCapture)));
        capRow.Children.Add(new TextBlock { Text = "  Var/Cap Name:", VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(10, 0, 5, 0) });
        capRow.Children.Add(Txt(nameof(BlockFunction.VariableName)));
        Panel.Children.Add(capRow);

        Panel.Children.Add(_panels);
        Panel.Children.Add(_info);

        // Build sub-panels
        // Hash
        var hash = VPanel();
        hash.Children.Add(Row("Hash type:", EnumCombo<Hash>(_b.HashType, v => _b.HashType = v)));
        hash.Children.Add(Check("Base64 Input", nameof(BlockFunction.InputBase64)));
        _panels.Children.Add(hash);

        // HMAC
        var hmac = VPanel();
        hmac.Children.Add(Row("Hash type:", EnumCombo<Hash>(_b.HashType, v => _b.HashType = v)));
        hmac.Children.Add(Row("Key:", Txt(nameof(BlockFunction.HmacKey))));
        hmac.Children.Add(Wrap(
            Check("Base64 Input", nameof(BlockFunction.InputBase64)),
            Check("Base64 Key", nameof(BlockFunction.KeyBase64)),
            Check("Base64 HMAC", nameof(BlockFunction.HmacBase64))));
        _panels.Children.Add(hmac);

        // Translate
        var tr = VPanel();
        tr.Children.Add(Check("Stop after first match", nameof(BlockFunction.StopAfterFirstMatch)));
        tr.Children.Add(new TextBlock { Text = "Dictionary ('normalname' 'translatedname' per line):" });
        tr.Children.Add(MultiBox(_b.GetDictionary(), v => _b.SetDictionary(v.Split('\n', StringSplitOptions.RemoveEmptyEntries))));
        _panels.Children.Add(tr);

        // Date
        var date = VPanel();
        date.Children.Add(Row("Format:", Txt(nameof(BlockFunction.DateFormat))));
        _panels.Children.Add(date);

        // Replace
        var rep = VPanel();
        rep.Children.Add(Row("Find:", Txt(nameof(BlockFunction.ReplaceWhat))));
        rep.Children.Add(Row("Replace with:", Txt(nameof(BlockFunction.ReplaceWith))));
        rep.Children.Add(Check("Use Regex", nameof(BlockFunction.UseRegex)));
        _panels.Children.Add(rep);

        // RegexMatch
        var rm = VPanel();
        rm.Children.Add(Row("Regex:", Txt(nameof(BlockFunction.RegexMatch))));
        _panels.Children.Add(rm);

        // RandomNum
        var rn = VPanel();
        var rnRow = new DockPanel();
        rnRow.Children.Add(new TextBlock { Text = "Min:", VerticalAlignment = VerticalAlignment.Center });
        rnRow.Children.Add(Txt(nameof(BlockFunction.RandomMin)));
        rn.Children.Add(rnRow);
        var rnRow2 = new DockPanel();
        rnRow2.Children.Add(new TextBlock { Text = "Max:", VerticalAlignment = VerticalAlignment.Center });
        rnRow2.Children.Add(Txt(nameof(BlockFunction.RandomMax)));
        rn.Children.Add(rnRow2);
        rn.Children.Add(Check("Pad with zeros", nameof(BlockFunction.RandomZeroPad)));
        _panels.Children.Add(rn);

        // CountOccurrences
        var co = VPanel();
        co.Children.Add(Row("String to find:", Txt(nameof(BlockFunction.StringToFind))));
        _panels.Children.Add(co);

        // RSA (shared by RSAEncrypt + RSAPKCS1PAD2)
        var rsa = VPanel();
        rsa.Children.Add(Row("Public Modulus (n):", Txt(nameof(BlockFunction.RsaN))));
        rsa.Children.Add(Row("Public Key Exponent (e):", Txt(nameof(BlockFunction.RsaE))));
        _panels.Children.Add(rsa);

        // CharAt
        var ca = VPanel();
        ca.Children.Add(Row("Index:", Txt(nameof(BlockFunction.CharIndex))));
        _panels.Children.Add(ca);

        // Substring
        var ss = VPanel();
        ss.Children.Add(Row("Start Index:", Txt(nameof(BlockFunction.SubstringIndex))));
        ss.Children.Add(Row("Length:", Txt(nameof(BlockFunction.SubstringLength))));
        _panels.Children.Add(ss);

        // RandomUA
        var ua = VPanel();
        var uaRow = new DockPanel();
        uaRow.Children.Add(Check("Generate only for", nameof(BlockFunction.UserAgentSpecifyBrowser)));
        uaRow.Children.Add(EnumCombo<UserAgent.Browser>(_b.UserAgentBrowser, v => _b.UserAgentBrowser = v));
        ua.Children.Add(uaRow);
        _panels.Children.Add(ua);

        // AES
        var aes = VPanel();
        aes.Children.Add(Row("Key:", Txt(nameof(BlockFunction.AesKey))));
        aes.Children.Add(Row("IV:", Txt(nameof(BlockFunction.AesIV))));
        aes.Children.Add(Row("Mode:", EnumCombo<CipherMode>(_b.AesMode, v => _b.AesMode = v)));
        aes.Children.Add(Row("Padding:", EnumCombo<PaddingMode>(_b.AesPadding, v => _b.AesPadding = v)));
        _panels.Children.Add(aes);

        // KDF
        var kdf = VPanel();
        kdf.Children.Add(Row("Salt (optional):", Txt(nameof(BlockFunction.KdfSalt))));
        kdf.Children.Add(Row("Generated Salt Size (bytes):", Num(nameof(BlockFunction.KdfSaltSize), 0, int.MaxValue)));
        kdf.Children.Add(Row("Iterations:", Num(nameof(BlockFunction.KdfIterations), 0, int.MaxValue)));
        kdf.Children.Add(Row("Output Key Size (bytes):", Num(nameof(BlockFunction.KdfKeySize), 0, int.MaxValue)));
        kdf.Children.Add(Row("Algorithm:", EnumCombo<Hash>(_b.KdfAlgorithm, v => _b.KdfAlgorithm = v)));
        _panels.Children.Add(kdf);

        _info.Text = InfoDic.TryGetValue(_b.FunctionType, out var d0) ? d0 : "";
        UpdatePanels();
    }

    private static StackPanel VPanel() => new() { Spacing = 4 };

    private static StackPanel Wrap(params Control[] c)
    {
        var p = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        foreach (var x in c) p.Children.Add(x);
        return p;
    }

    private void UpdatePanels()
    {
        var idx = _b.FunctionType switch
        {
            BlockFunction.Function.Hash => 0,
            BlockFunction.Function.HMAC => 1,
            BlockFunction.Function.Translate => 2,
            BlockFunction.Function.DateToUnixTime or BlockFunction.Function.UnixTimeToDate => 3,
            BlockFunction.Function.Replace => 4,
            BlockFunction.Function.RegexMatch => 5,
            BlockFunction.Function.RandomNum => 6,
            BlockFunction.Function.CountOccurrences => 7,
            BlockFunction.Function.RSAEncrypt or BlockFunction.Function.RSAPKCS1PAD2 => 8,
            BlockFunction.Function.CharAt => 9,
            BlockFunction.Function.Substring => 10,
            BlockFunction.Function.GetRandomUA => 11,
            BlockFunction.Function.AESEncrypt or BlockFunction.Function.AESDecrypt => 12,
            BlockFunction.Function.PBKDF2PKCS5 => 13,
            _ => -1
        };
        for (int i = 0; i < _panels.Children.Count; i++)
            _panels.Children[i].IsVisible = i == idx;
    }
}
