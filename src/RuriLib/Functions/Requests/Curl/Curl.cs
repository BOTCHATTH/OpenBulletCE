using System.Runtime.InteropServices;

namespace RuriLib.Functions.Requests
{
    /// <summary>
    /// Minimal P/Invoke surface over libcurl-impersonate (win-x64).
    /// curl_easy_setopt is variadic in C — on the Windows x64 ABI the trailing
    /// varargs are passed like fixed args, so typed DllImport overloads work.
    /// </summary>
    internal static class Curl
    {
        internal const string Lib = "libcurl-impersonate";

        static Curl()
        {
            NativeLibrary.SetDllImportResolver(typeof(Curl).Assembly, (name, asm, path) =>
            {
                if (!name.StartsWith("libcurl")) return IntPtr.Zero;
                var dir = AppContext.BaseDirectory;
                foreach (var cand in new[]
                {
                    Path.Combine(dir, name + ".dll"),
                    Path.Combine(dir, "native", name + ".dll"),
                    Path.Combine(dir, "Plugins", "native", name + ".dll"),
                    Path.Combine(dir, "runtimes", "win-x64", "native", name + ".dll"),
                })
                {
                    if (File.Exists(cand))
                        return NativeLibrary.Load(cand);
                }
                return IntPtr.Zero;
            });
        }

        internal static void EnsureResolver() { } // touch to run the static ctor

        [DllImport(Lib)] internal static extern IntPtr curl_easy_init();
        [DllImport(Lib)] internal static extern void curl_easy_cleanup(IntPtr h);
        [DllImport(Lib)] internal static extern int curl_easy_perform(IntPtr h);
        [DllImport(Lib)] internal static extern int curl_easy_impersonate(IntPtr h, string target, int default_headers);
        [DllImport(Lib)] internal static extern IntPtr curl_easy_strerror(int code);
        [DllImport(Lib)] internal static extern IntPtr curl_version();

        // setopt overloads (varargs simulated via fixed signatures)
        [DllImport(Lib, EntryPoint = "curl_easy_setopt")] internal static extern int SetoptPtr(IntPtr h, int opt, IntPtr val);
        [DllImport(Lib, EntryPoint = "curl_easy_setopt")] internal static extern int SetoptLong(IntPtr h, int opt, long val);
        [DllImport(Lib, EntryPoint = "curl_easy_setopt")] internal static extern int SetoptStr(IntPtr h, int opt, string val);
        [DllImport(Lib, EntryPoint = "curl_easy_setopt")] internal static extern int SetoptCb(IntPtr h, int opt, WriteCallback cb);

        [DllImport(Lib, EntryPoint = "curl_easy_getinfo")] internal static extern int GetInfoLong(IntPtr h, int info, out long val);

        [DllImport(Lib)] internal static extern IntPtr curl_slist_append(IntPtr list, string s);
        [DllImport(Lib)] internal static extern void curl_slist_free_all(IntPtr list);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate nuint WriteCallback(IntPtr ptr, nuint size, nuint nmemb, IntPtr userdata);

        internal const int OPT_URL = 10002;
        internal const int OPT_WRITEFUNCTION = 20011;
        internal const int OPT_HEADERFUNCTION = 20079;
        internal const int OPT_FOLLOWLOCATION = 52;
        internal const int OPT_MAXREDIRS = 68;
        internal const int OPT_TIMEOUT_MS = 155;
        internal const int OPT_CUSTOMREQUEST = 10036;
        internal const int OPT_POSTFIELDS = 10015;
        internal const int OPT_POSTFIELDSIZE_LARGE = 30120;
        internal const int OPT_HTTPHEADER = 10023;
        internal const int OPT_COOKIE = 10022;
        internal const int OPT_PROXY = 10004;
        internal const int OPT_PROXYTYPE = 101;
        internal const int OPT_PROXYUSERPWD = 10006;
        internal const int OPT_SSL_VERIFYPEER = 64;
        internal const int OPT_SSL_VERIFYHOST = 81;
        internal const int OPT_ENCODING = 10102;      // CURLOPT_ACCEPT_ENCODING ("" = all)

        internal const int INFO_RESPONSE_CODE = 0x200002;

        internal const int PROXY_HTTP = 0;
        internal const int PROXY_SOCKS4 = 4;
        internal const int PROXY_SOCKS4A = 6;
        internal const int PROXY_SOCKS5 = 5;
        internal const int PROXY_SOCKS5_HOSTNAME = 7;
    }
}
