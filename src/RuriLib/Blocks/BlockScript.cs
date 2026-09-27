using IronPython.Compiler;
using IronPython.Hosting;
using IronPython.Runtime;
using Jint;
using RuriLib.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace RuriLib
{
    /// <summary>A single OUTPUT declaration of a <see cref="BlockScript"/> (type + name, @-prefix = capture).</summary>
    public class ScriptOutput
    {
        /// <summary>String, Int, Float, Bool, ListOfStrings, DictionaryOfStrings or ByteArray.</summary>
        public string Type { get; set; } = "String";

        /// <summary>The output variable name. A leading @ marks it as a capture.</summary>
        public string Name { get; set; } = "";

        /// <summary>Whether this output is flagged for capture.</summary>
        public bool IsCapture => Name.StartsWith("@");
    }

    /// <summary>
    /// OB2-style script block:
    /// BLOCK:Script
    ///   INTERPRETER:Jint
    ///   INPUT x,y
    ///   BEGIN SCRIPT
    ///   var result = x + y;
    ///   END SCRIPT
    ///   OUTPUT String @result
    /// ENDBLOCK
    /// </summary>
    public class BlockScript : BlockBase
    {
        /// <summary>The script interpreter: Jint (JavaScript) or IronPython.</summary>
        public string Interpreter { get; set; } = "Jint";

        /// <summary>The variables the script consumes (all bot variables are injected regardless).</summary>
        public List<string> InputVariables { get; set; } = new List<string>();

        /// <summary>Comma-separated view of <see cref="InputVariables"/> for single-line editing.</summary>
        public string InputVariablesText
        {
            get => string.Join(",", InputVariables);
            set => InputVariables = (value ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(v => v.Trim()).ToList();
        }

        /// <summary>The declared outputs of the script.</summary>
        public List<ScriptOutput> Outputs { get; set; } = new List<ScriptOutput>();

        /// <summary>The script source code.</summary>
        public string Script { get; set; } = "";

        /// <summary>Creates a Script block.</summary>
        public BlockScript() { Label = "SCRIPT"; }

        /// <inheritdoc />
        public override BlockBase FromLS(string script)
        {
            var lines = script.Split('\n').Select(l => l.TrimEnd('\r')).ToList();

            var inBody = false;
            var body = new List<string>();

            foreach (var raw in lines)
            {
                var t = raw.Trim();

                if (inBody)
                {
                    if (t.StartsWith("END SCRIPT", StringComparison.OrdinalIgnoreCase)) inBody = false;
                    else body.Add(raw);
                    continue;
                }

                if (t.StartsWith("BEGIN SCRIPT", StringComparison.OrdinalIgnoreCase)) { inBody = true; continue; }
                if (t.Equals("ENDBLOCK", StringComparison.OrdinalIgnoreCase)) break;
                if (t.Length == 0) continue;

                if (t.StartsWith("INTERPRETER", StringComparison.OrdinalIgnoreCase))
                    Interpreter = HeaderValue(t, "INTERPRETER");
                else if (t.StartsWith("INPUT", StringComparison.OrdinalIgnoreCase))
                    InputVariablesText = HeaderValue(t, "INPUT");
                else if (t.StartsWith("OUTPUT", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = HeaderValue(t, "OUTPUT").Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    var o = new ScriptOutput();
                    if (parts.Length == 1) o.Name = parts[0];
                    if (parts.Length > 1) { o.Type = parts[0]; o.Name = parts[1]; }
                    Outputs.Add(o);
                }
            }

            Script = string.Join("\n", body);
            return this;
        }

        /// <inheritdoc />
        public override string ToLS(bool indent = true)
        {
            var sb = new StringBuilder();
            sb.Append(Disabled ? "!" : "");
            if (!string.IsNullOrEmpty(Label) && Label != "SCRIPT") sb.Append($"#{Label} ");
            sb.Append("BLOCK:Script\n");
            sb.Append($"INTERPRETER:{Interpreter}\n");
            sb.Append($"INPUT {string.Join(",", InputVariables)}\n");
            sb.Append("BEGIN SCRIPT\n");
            sb.Append(Script);
            sb.Append("\nEND SCRIPT\n");
            foreach (var o in Outputs)
                sb.Append($"OUTPUT {o.Type} {o.Name}\n");
            sb.Append("ENDBLOCK");
            return sb.ToString();
        }

        /// <inheritdoc />
        public override void Process(BotData data)
        {
            var sw = new StringWriter();
            Console.SetOut(sw);
            Console.SetError(sw);

            var isPython = Interpreter.StartsWith("IronPython", StringComparison.OrdinalIgnoreCase)
                || Interpreter.Equals("Python", StringComparison.OrdinalIgnoreCase);
            var isNode = Interpreter.StartsWith("Node", StringComparison.OrdinalIgnoreCase);

            try
            {
                if (isPython) RunPython(sw, data);
                else if (isNode) RunNodeJs(sw, data);
                else RunJavaScript(sw, data);
            }
            catch (Exception ex)
            {
                data.Log(new LogEntry($"Script block failed: {ex.Message}", Colors.Tomato));
            }
        }

        private void RunJavaScript(StringWriter sw, BotData data)
        {
            var engine = new Engine().SetValue("log", new Action<object>(Console.WriteLine));

            foreach (var variable in data.Variables.All)
            {
                try
                {
                    if (variable.Type == CVar.VarType.List)
                        engine.SetValue(variable.Name, (variable.Value as List<string>).ToArray());
                    else
                        engine.SetValue(variable.Name, variable.Value.ToString());
                }
                catch { }
            }

            var result = engine.Execute(Script);

            data.Log(new LogEntry($"DEBUG LOG: {sw}", Colors.White));
            data.Log(new LogEntry($"Parsing {Outputs.Count} variables", Colors.White));

            foreach (var output in Outputs)
            {
                var name = output.Name.TrimStart('@');
                if (name.Length == 0) continue;
                try
                {
                    var value = engine.Global.GetProperty(name).Value;
                    SetOutput(data, name, output.IsCapture, output.Type,
                        single: JsToString(value, output.Type),
                        list: value.IsArray() ? value.TryCast<List<string>>() : null,
                        dict: JsToDict(value));
                    data.Log(new LogEntry($"SET VARIABLE {name} WITH VALUE {value}", Colors.Yellow));
                }
                catch { data.Log(new LogEntry($"COULD NOT FIND VARIABLE {name}", Colors.Tomato)); }
            }

            if (result != null && result.ToString() != "undefined")
                data.Log(new LogEntry($"Completion value: {result}", Colors.White));
        }

        private void RunNodeJs(StringWriter sw, BotData data)
        {
            // Inject all bot variables as globals
            var sb = new StringBuilder();
            foreach (var variable in data.Variables.All)
            {
                try
                {
                    object val = variable.Type == CVar.VarType.List
                        ? variable.Value as List<string>
                        : variable.Type == CVar.VarType.Dictionary
                            ? (object)(variable.Value as Dictionary<string, string>)
                            : variable.Value?.ToString();
                    sb.Append($"globalThis[{Newtonsoft.Json.JsonConvert.SerializeObject(variable.Name)}]={Newtonsoft.Json.JsonConvert.SerializeObject(val)};");
                }
                catch { }
            }
            sb.AppendLine();
            sb.AppendLine(Script);

            // Emit the requested outputs as a JSON line on stdout
            sb.Append("try{console.log(\"\\n__OBVARS__\"+JSON.stringify({");
            var first = true;
            foreach (var o in Outputs)
            {
                var n = o.Name.TrimStart('@');
                if (n.Length == 0) continue;
                var key = Newtonsoft.Json.JsonConvert.SerializeObject(n);
                sb.Append($"{(first ? "" : ",")}{key}:(globalThis[{key}]===undefined?null:globalThis[{key}])");
                first = false;
            }
            sb.AppendLine("}))}catch(e){}");

            var tmp = Path.Combine(Path.GetTempPath(), $"obscript_{Guid.NewGuid():N}.js");
            try
            {
                File.WriteAllText(tmp, sb.ToString());
                var psi = new System.Diagnostics.ProcessStartInfo("node", $"\"{tmp}\"")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                var p = System.Diagnostics.Process.Start(psi);
                var stdout = p.StandardOutput.ReadToEnd();
                var stderr = p.StandardError.ReadToEnd();
                p.WaitForExit(60000);

                var marker = stdout.LastIndexOf("__OBVARS__", StringComparison.Ordinal);
                var console = marker >= 0 ? stdout.Substring(0, marker) : stdout;
                data.Log(new LogEntry($"DEBUG LOG: {console.TrimEnd()}{stderr}", Colors.White));

                if (marker < 0) return;
                var json = stdout.Substring(marker + "__OBVARS__".Length).Trim();
                var obj = Newtonsoft.Json.Linq.JObject.Parse(json);

                data.Log(new LogEntry($"Parsing {Outputs.Count} variables", Colors.White));
                foreach (var output in Outputs)
                {
                    var name = output.Name.TrimStart('@');
                    if (name.Length == 0) continue;
                    try
                    {
                        var token = obj[name];
                        SetOutput(data, name, output.IsCapture, output.Type,
                            single: token?.Type == Newtonsoft.Json.Linq.JTokenType.Null ? "" : NodeToString(token, output.Type),
                            list: token is Newtonsoft.Json.Linq.JArray arr ? arr.Select(t => t.ToString()) : null,
                            dict: token is Newtonsoft.Json.Linq.JObject jo ? jo.Properties().ToDictionary(pr => pr.Name, pr => pr.Value?.ToString() ?? "") : null);
                        data.Log(new LogEntry($"SET VARIABLE {name} WITH VALUE {token}", Colors.Yellow));
                    }
                    catch { data.Log(new LogEntry($"COULD NOT FIND VARIABLE {name}", Colors.Tomato)); }
                }
            }
            finally { try { File.Delete(tmp); } catch { } }
        }

        private static string NodeToString(Newtonsoft.Json.Linq.JToken token, string type)
        {
            if (token == null || token.Type == Newtonsoft.Json.Linq.JTokenType.Null) return "";
            switch ((type ?? "").Replace(" ", "").ToLowerInvariant())
            {
                case "int": return token.ToObject<double>().ToString("0", CultureInfo.InvariantCulture);
                case "float": return token.ToObject<double>().ToString(CultureInfo.InvariantCulture);
                case "bool": return token.ToObject<bool>() ? "True" : "False";
                case "bytearray":
                    try { return Convert.ToBase64String(token.ToObject<byte[]>()); } catch { return token.ToString(); }
                default: return token.Type == Newtonsoft.Json.Linq.JTokenType.String ? token.ToString() : token.ToString(Newtonsoft.Json.Formatting.None);
            }
        }

        private void RunPython(StringWriter sw, BotData data)
        {
            var runtime = Python.CreateRuntime();
            var engine = runtime.GetEngine("py");
            var pco = (PythonCompilerOptions)engine.GetCompilerOptions();
            pco.Module &= ~ModuleOptions.Optimized;

            var scope = engine.CreateScope();
            var code = engine.CreateScriptSourceFromString(Script);

            foreach (var variable in data.Variables.All)
                try { scope.SetVariable(variable.Name, variable.Value); } catch { }

            code.Execute(scope);

            data.Log(new LogEntry($"DEBUG LOG: {sw}", Colors.White));
            data.Log(new LogEntry($"Parsing {Outputs.Count} variables", Colors.White));

            foreach (var output in Outputs)
            {
                var name = output.Name.TrimStart('@');
                if (name.Length == 0) continue;
                try
                {
                    var value = scope.GetVariable(name);
                    SetOutput(data, name, output.IsCapture, output.Type,
                        single: PyToString(value, output.Type),
                        list: value as IEnumerable<string> ?? (value as Array)?.Cast<string>(),
                        dict: value as Dictionary<string, string>);
                    data.Log(new LogEntry($"SET VARIABLE {name} WITH VALUE {value}", Colors.Yellow));
                }
                catch { data.Log(new LogEntry($"COULD NOT FIND VARIABLE {name}", Colors.Tomato)); }
            }
        }

        private void SetOutput(BotData data, string name, bool isCapture, string type, string single, IEnumerable<string> list, Dictionary<string, string> dict)
        {
            switch ((type ?? "String").Replace(" ", "").ToLowerInvariant())
            {
                case "listofstrings":
                    InsertVariable(data, isCapture, (list ?? Array.Empty<string>()).ToList(), name);
                    break;
                case "dictionaryofstrings":
                    data.Variables.Set(new CVar(name, dict ?? new Dictionary<string, string>(), isCapture));
                    break;
                default:
                    InsertVariable(data, isCapture, single ?? "", name);
                    break;
            }
        }

        private static string JsToString(Jint.Native.JsValue value, string type)
        {
            switch ((type ?? "").Replace(" ", "").ToLowerInvariant())
            {
                case "int": return ((int)value.AsNumber()).ToString(CultureInfo.InvariantCulture);
                case "float": return value.AsNumber().ToString(CultureInfo.InvariantCulture);
                case "bool": return value.AsBoolean() ? "True" : "False";
                case "bytearray":
                    try { return Convert.ToBase64String(value.TryCast<byte[]>()); } catch { return value.ToString(); }
                default: return value.ToString();
            }
        }

        private static Dictionary<string, string> JsToDict(Jint.Native.JsValue value)
        {
            var dict = new Dictionary<string, string>();
            try
            {
                if (value.IsObject())
                    foreach (var p in value.AsObject().GetOwnProperties())
                        dict[p.Key.ToString()] = p.Value.Value?.ToString() ?? "";
            }
            catch { }
            return dict;
        }

        private static string PyToString(object value, string type)
        {
            if (value == null) return "";
            switch ((type ?? "").Replace(" ", "").ToLowerInvariant())
            {
                case "int":
                case "float": return Convert.ToString(value, CultureInfo.InvariantCulture);
                case "bool": return (value is bool b && b) ? "True" : "False";
                case "bytearray":
                    if (value is byte[] bytes) return Convert.ToBase64String(bytes);
                    return value.ToString();
                default: return value.ToString();
            }
        }

        private static string HeaderValue(string line, string key)
        {
            var v = line.Substring(key.Length).Trim();
            if (v.StartsWith(":")) v = v.Substring(1).Trim();
            return v;
        }
    }
}
