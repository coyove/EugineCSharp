using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Eugine
{
    abstract class SExpr
    {
        public abstract SExpr Clone();

        public static Dictionary<string, SToken.TokenType> Lookup = 
            new Dictionary<string, SToken.TokenType>()
        {
            { "(", SToken.TokenType.LPAREN },
            { ")", SToken.TokenType.RPAREN },
            { "[", SToken.TokenType.LBRACK },
            { "]", SToken.TokenType.RBRACK },
        };

        public static SExprComp MakeLambda(string func, params object[] atoms)
        {
            var comp = new SExprComp();
            comp.Atomics.AddRange(atoms.Select(a => {
                if (a is string)
                    return (SExpr)new SExprAtomic(new SToken(SToken.TokenType.ATOMIC, a));
                else
                    return (SExpr)new SExprAtomic(new SToken(SToken.TokenType.SEXPRESSION, a));
            }));
            return comp;
        }
    }

    class SExprAtomic : SExpr
    {
        public SToken Token;

        public SExprAtomic(SToken tok)
        {
            Token = tok;
        }

        public override SExpr Clone()
        {
            return new SExprAtomic(Token);
        }
    }

    class SExprComp: SExpr
    {
        public List<SExpr> Atomics = new List<SExpr>();
        public string Namespace;

        public override SExpr Clone()
        {
            SExprComp ret = new SExprComp();
            ret.Atomics = Atomics.Select(a => a.Clone()).ToList();
            return ret;
        }
    }

    class SToken
    {
        public enum TokenType
        {
            STRING, LPAREN, RPAREN, LBRACK, RBRACK, ATOMIC, NUMBER, SEXPRESSION
        }

        public TokenType TType;
        public object TValue;
        public Tuple<int, int> LineIndex;
        public string Source;

        public SToken() {}

        public SToken(TokenType tt, object tv)
        {
            TType = tt;
            TValue = tv;
        }
    }

    class Parser
    {
        private Regex reString = new Regex(
            String.Join("|", new string[] {
                @"(@""(?<rawstr>(""""|[^""])*)"")",
                @"(""(?<str>(\\.|[^""\\])*)"")",
                //@"(\{(?<rawstr>(\{\{|\}\}|[^\{\}])*)\})",
                @"(?<paren>[\(\)\[\]])",
                @"((?<atom>[^\(\)\[\]\s\r\n\;]+)(?=[\s\r\n\(\)\[\]]+))",
                @"(?<comment>\;.*?(\n|$))",
            })
        );

        private string basePath;

        public SExprComp Parse(string text, string path, string source)
        {
            if (text.Length < 2) throw new VMException("Invalid code");
            basePath = path;

            List<SToken> tokens = new List<SToken>();

            var m = reString.Match(text);
            var finder = new LineFinder(text);

            while (m.Success)
            {
                SToken tok = new SToken();
                tok.Source = source;

                if (m.Groups["str"].Success)
                {
                    tok.LineIndex = finder.FindIndexAtLineIndex(m.Groups["str"].Index);
                    tok.TType = SToken.TokenType.STRING;
                    tok.TValue = Regex.Unescape(m.Groups["str"].Value).Replace(@"\""", "\"");
                }
                else if (m.Groups["rawstr"].Success)
                {
                    tok.LineIndex = finder.FindIndexAtLineIndex(m.Groups["rawstr"].Index);
                    tok.TType = SToken.TokenType.STRING;
                    tok.TValue = m.Groups["rawstr"].Value.Replace("\"\"", "\""); //.Replace("}}", "}");
                }
                else if (m.Groups["paren"].Success)
                {
                    tok.LineIndex = finder.FindIndexAtLineIndex(m.Groups["paren"].Index);
                    tok.TValue = m.Groups["paren"].Value;
                    tok.TType = SExpr.Lookup[(string)tok.TValue];
                }
                else if (m.Groups["atom"].Success)
                {
                    string v = m.Groups["atom"].Value;
                    decimal dum;
                    tok.LineIndex = finder.FindIndexAtLineIndex(m.Groups["atom"].Index);

                    if (Decimal.TryParse(v, out dum))
                    {
                        tok.TType = SToken.TokenType.NUMBER;
                        tok.TValue = dum;
                    }
                    else
                    {
                        tok.TType = SToken.TokenType.ATOMIC;
                        tok.TValue = v;
                    }
                }
                else
                {
                    m = m.NextMatch();
                    continue;
                }

                tokens.Add(tok);
                m = m.NextMatch();
            }

            SExprComp chain = new SExprComp();
            chain.Atomics.Add(new SExprAtomic(new SToken(SToken.TokenType.ATOMIC, "chain")));

            while (tokens.Count > 0) chain.Atomics.Add(ParseNext(tokens));

            return chain;
        }

        private SExpr ParseNext(List<SToken> tokens)
        {
            var token = tokens.Pop();
            if (token == null) return null;

            if (token.TType == SToken.TokenType.RPAREN || token.TType == SToken.TokenType.RBRACK)
                throw new VMException("unexpected character", token);

            if (token.TType == SToken.TokenType.LPAREN || token.TType == SToken.TokenType.LBRACK)
            {
                var ending = token.TType == SToken.TokenType.LPAREN ? SToken.TokenType.RPAREN : SToken.TokenType.RBRACK;
                var comp = new SExprComp();

                while (tokens.First().TType != ending)
                    comp.Atomics.Add(ParseNext(tokens));

                if (tokens.Count == 0 || tokens[0].TType != ending)
                    throw new VMException("unexpected character", token);

                tokens.Pop();

                if (token.TType == SToken.TokenType.LBRACK && comp.Atomics.Count >= 2)
                {
                    var tmp = comp.Atomics[0];
                    comp.Atomics[0] = comp.Atomics[1];
                    comp.Atomics[1] = tmp;
                }

                if (comp.Atomics.Count == 2 && (comp.Atomics[0] as SExprAtomic)?.Token.TValue.ToString() == "~include")
                {
                    var path = (comp.Atomics[1] as SExprAtomic);
                    if (path != null && path.Token.TType == SToken.TokenType.STRING)
                    {
                        var codePath = basePath + (string)path.Token.TValue;
                        var codeFolder = EugineVM.GetDirectoryName(codePath);
                        var codeSource = Path.GetFileName(codePath);

                        try {
                            var p = new Parser();
                            SExprComp inc = p.Parse(File.ReadAllText(codePath), codeFolder, codeSource);
                            comp.Atomics = new List<SExpr>();
                            comp.Atomics.Add(inc);
                        }
                        catch (Exception ex)
                        {
                            throw new VMException("error when reading " + codePath + ", " + ex.Message,
                                comp.Atomics[0] as SExprAtomic);
                        }
                    }
                    else
                        throw new VMException("it must be a static string", comp.Atomics[0] as SExprAtomic);
                }

                return comp;
            }

            return new SExprAtomic(token);
        }
    }

    class EugineVM
    {
        public static string GetDirectoryName(string path)
        {
            string ret = Path.GetDirectoryName(path);
            if (ret.Last() != '\\' && ret.Last() != '/') ret += "/";

            return ret;
        }

        public ExecEnvironment DefaultEnvironment = new ExecEnvironment()
        {
            { "null", new SNull() },
            { "#nil", new SNull() },
            { "true", new SBool(true) },
            { "false", new SBool(false) },
            { "#t", new SBool(true) },
            { "#f", new SBool(false) }
        };

        public object ExecuteString(string code, ExecEnvironment ee)
        {
            var p = new Parser();
            var path = "";
            var source = "";

            if (ee.ContainsKey("~path")) path = ee["~path"].Evaluate(null).Get<String>();
            if (ee.ContainsKey("~source")) source = ee["~source"].Evaluate(null).Get<String>();

            var s = p.Parse(code, path, source);
            return SExpression.Cast(s).Evaluate(ee).Get<object>();
        }

        public object ExecuteFile(string filePath, ExecEnvironment ee)
        {
            string code = File.ReadAllText(filePath);
            
            var p = new Parser();
            var path = EugineVM.GetDirectoryName(filePath);
            var source = Path.GetFileName(filePath);

            ee["~path"] = new SString(path);
            ee["~source"] = new SString(source);

            var s = p.Parse(code, path, source);
            return SExpression.Cast(s).Evaluate(ee).Get<object>();
        }
    }
}
