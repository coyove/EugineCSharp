using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Eugine
{
    class SEExit : SExpression
    {
        private SExpression argument;

        public SEExit(SExprAtomic ha, SExprComp c) : base(ha, c)
        {
            if (c.Atomics.Count != 1) throw new VMException("it takes 1 argument");

            argument = SExpression.Cast(c.Atomics.Pop());
        }

        public override SValue Evaluate(ExecEnvironment env)
        {
            var msg = argument.Evaluate(env) as SString;
            if (msg == null) throw new VMException("message must be a string", headAtom);

            throw new VMException(msg.Get<String>(), headAtom);
        }
    }

    class SESub : SExpression
    {
        private List<SExpression> arguments;

        public SESub(SExprAtomic ha, SExprComp c) : base(ha, c)
        {
            if (c.Atomics.Count != 2 && c.Atomics.Count != 3) throw new VMException("it takes 2 or 3 arguments", ha);

            arguments = (from a in c.Atomics select SExpression.Cast(a)).ToList();
        }

        public override SValue Evaluate(ExecEnvironment env)
        {
            var arguments = SExpression.EvalSExpressions(this.arguments, env);
            var subObj = arguments[0];
            try
            {
                if (subObj is SString)
                {
                    SString subStr = subObj as SString;

                    if (arguments.Count == 2)
                        return new SString(subStr.Get<string>().Substring(
                            (int)arguments[1].Get<Decimal>()
                        ));
                    else
                        return new SString(subStr.Get<string>().Substring(
                            (int)arguments[1].Get<Decimal>(),
                            (int)arguments[2].Get<Decimal>()
                        ));

                }
                else if (subObj is SList)
                {
                    SList subList = subObj as SList;
                    if (arguments.Count == 2)
                        return new SList(subList.Get<List<SValue>>().Skip(
                            (int)arguments[1].Get<Decimal>()
                        ).ToList());
                    else
                        return new SList(subList.Get<List<SValue>>().Skip(
                            (int)arguments[1].Get<Decimal>()).Take(
                            (int)arguments[2].Get<Decimal>()
                        ).ToList());

                }
                else
                    throw new VMException("the first argument must be a string or a list", headAtom);
            }
            catch
            {
                throw new VMException("invalid arguments", headAtom);
            }
        }
    }

    class SEDel : SExpression
    {
        private SExpression host;
        private SExpression index;

        public SEDel(SExprAtomic ha, SExprComp c) : base(ha, c)
        {
            if (c.Atomics.Count != 2) throw new VMException("it takes 2 arguments", ha);

            host = SExpression.Cast(c.Atomics.Pop());
            index = SExpression.Cast(c.Atomics.Pop());
        }

        public override SValue Evaluate(ExecEnvironment env)
        {
            var subObj = host.Evaluate(env);
            var idx = this.index.Evaluate(env);
            int index = 0;
            string key = "";

            if (idx is SNumber)
                index = (int)idx.Get<Decimal>();
            else if (idx is SString)
                key = idx.Get<String>();
            else
                throw new VMException("the second argument must a number or a string", headAtom);
            
            if (subObj is SString)
                return new SString(subObj.Get<String>().Remove(index, 1));
            else if (subObj is SList)
            {
                List<SValue> subList = subObj.Get<List<SValue>>();
                subList.RemoveAt(index);
                return subObj;
            }
            else if (subObj is SDict)
            {
                Dictionary<string, SValue> dict = subObj.Get<Dictionary<string, SValue>>();
                dict.Remove(key);
                return subObj;
            }
            else
                throw new VMException("the first argument must be a string, a list or a dict", headAtom);
        }
    }

    class SEStr : SExpression
    {
        private SExpression argument;
        private bool convertChr;

        public SEStr(SExprAtomic ha, SExprComp c, bool chr) : base(ha, c)
        {
            if (c.Atomics.Count != 1) throw new VMException("it takes 1 argument");

            argument = SExpression.Cast(c.Atomics.Pop());
            convertChr = chr;
        }

        private SValue convert(SValue num)
        {
            if (num is SNumber)
            {
                var n = num.Get<Decimal>();
                if (convertChr)
                    return new SString(((char)n).ToString());
                else
                    return new SString(n.ToString());
            }
            else if (num is SBool)
                return new SString(num.Get<bool>().ToString());
            else if (num is SList)
                return new SList((num as SList).Get<List<SValue>>().Select(n => convert(n)).ToList());
            else
                return num;
        }

        public override SValue Evaluate(ExecEnvironment env)
        {
            return convert(argument.Evaluate(env));
        }
    }

    class SENum : SExpression
    {
        private SExpression argument;
        private bool convertAsc;

        public SENum(SExprAtomic ha, SExprComp c, bool asc) : base(ha, c)
        {
            if (c.Atomics.Count != 1) throw new VMException("it takes 1 argument");

            argument = SExpression.Cast(c.Atomics.Pop());
            convertAsc = asc;
        }

        private SValue convert(SValue str)
        {
            if (str is SString)
            {
                var s = str.Get<String>();
                if (convertAsc && s.Length >= 1)
                    return new SNumber(s[0]);
                else
                {
                    Decimal tmp;
                    if (Decimal.TryParse(s, out tmp))
                        return new SNumber(tmp);
                    else
                        return new SNull();
                }
            }
            else if (str is SList)
                return new SList((str as SList).Get<List<SValue>>().Select(n => convert(n)).ToList());
            else
                return str;
        }

        public override SValue Evaluate(ExecEnvironment env)
        {
            return convert(argument.Evaluate(env));
        }
    }

    class SESplit : SExpression
    {
        private SExpression text;
        private SExpression delim;

        public SESplit(SExprAtomic ha, SExprComp c) : base(ha, c)
        {
            if (c.Atomics.Count != 2) throw new VMException("it takes 2 arguments", ha);

            text = SExpression.Cast(c.Atomics.Pop());
            delim = SExpression.Cast(c.Atomics.Pop());
        }

        public override SValue Evaluate(ExecEnvironment env)
        {
            var text = this.text.Evaluate(env) as SString;
            if (text == null)
                throw new VMException("the first argument must be a string", headAtom);

            var _delim = this.delim.Evaluate(env);
            List<string> delims = new List<string>();

            if (_delim is SString)
                delims.Add(_delim.Get<String>());
            else if (_delim is SList)
                _delim.Get<List<SValue>>().ForEach(v =>
                {
                    var _v = v.Evaluate(env) as SString;
                    if (_v != null) delims.Add(_v.Get<String>());
                });
            else
                throw new VMException("the second argument must be a string or a list of strings", headAtom);

            if (delims.Count > 0)
            {
                var ret = text.Get<String>().Split(delims.ToArray(), StringSplitOptions.RemoveEmptyEntries);
                return new SList(ret.Select(r => (SValue)(new SString(r))).ToList());
            }
            else
            {
                return new SList(text.Get<String>().ToCharArray()
                    .Select(c => (SValue)(new SString(c.ToString()))).ToList());
            }
        }
    }

    class SELen : SExpression
    {
        private SExpression argument;

        public SELen(SExprAtomic ha, SExprComp c) : base(ha, c)
        {
            if (c.Atomics.Count != 1) throw new VMException("it takes 1 argument");

            argument = SExpression.Cast(c.Atomics.Pop());
        }

        public override SValue Evaluate(ExecEnvironment env)
        {
           
            SValue lenValue = argument.Evaluate(env);
            if (lenValue is SString)
                return new SNumber(lenValue.Get<string>().Length);
            else if (lenValue is SList)
                return new SNumber(lenValue.Get<List<SValue>>().Count);
            else
                return new SNumber(0);
        }
    }

    class SEEval : SExpression
    {
        private SExpression text;
        private SExpression env;

        public SEEval(SExprAtomic ha, SExprComp c) : base(ha, c)
        {
            if (c.Atomics.Count != 1 && c.Atomics.Count != 2) throw new VMException("it takes 1 or 2 arguments");

            text = SExpression.Cast(c.Atomics.Pop());
            if (c.Atomics.Count > 0)
                env = SExpression.Cast(c.Atomics.Pop());
        }

        public override SValue Evaluate(ExecEnvironment env)
        {

            var text = this.text.Evaluate(env) as SString;
            if (text == null) throw new VMException("must eval a string", headAtom);

            if (this.env != null)
            {
                var e = this.env.Evaluate(env) as SDict;
                if (e == null) throw new VMException("the second argument must be a dict if provided", headAtom);

                env = new ExecEnvironment();
                foreach (var kv in e.Get<Dictionary<string, SValue>>()) env[kv.Key] = kv.Value;
            }

            var p = new Parser();
            var s = p.Parse(text.Get<String>(), "", "<eval>");
            return SExpression.Cast(s).Evaluate(env);
        }
    }

    class SEKeys : SExpression
    {
        private SExpression dict;

        public SEKeys(SExprAtomic ha, SExprComp c) : base(ha, c)
        {
            if (c.Atomics.Count != 1) throw new VMException("it takes 1 argument");

            dict = SExpression.Cast(c.Atomics.Pop());
        }

        public override SValue Evaluate(ExecEnvironment env)
        {

            SValue lenValue = dict.Evaluate(env);
            if (lenValue is SDict)
                return new SList((from k in lenValue.Get<Dictionary<string, SValue>>().Keys.ToList()
                                 select (SValue)new SString(k)).ToList());
            else
                throw new VMException("it only take a dict as the argument", headAtom);
        }
    }

    class SEPrint : SExpression
    {
        private List<SExpression> arguments;
        private string delim;

        public SEPrint(SExprAtomic ha, SExprComp c, string d) : base(ha, c)
        {
            delim = d;
            if (c.Atomics.Count == 0) throw new VMException("it takes at least 1 argument", ha);

            arguments = (from a in c.Atomics select SExpression.Cast(a)).ToList();
        }

        private string printSValue(SValue re, ExecEnvironment env, int padding)
        {
            if (re is SList)
                return ("(" +
                    String.Join(" ",
                        from v in re.Get<List<SValue>>()
                        select printSValue(v.Evaluate(env), env, padding)) + ")");
            else if (re is SDict)
                return ("\n" + "(".PadLeft(padding - 1) + "\n" + String.Join("\n",
                    re.Get<Dictionary<String, SValue>>().Select(kv =>
                        "".PadLeft(padding) + kv.Key + "=" + printSValue(kv.Value.Evaluate(env), env, padding + 2)
                    )) + "\n" + ")".PadLeft(padding - 1));
            else if (re is SNull)
                return "null";
            else
                return String.Format("{0}", re.Underlying);
        }

        public override SValue Evaluate(ExecEnvironment env)
        {
            SValue ret = new SBool(false);
            foreach (var e in SExpression.EvalSExpressions(arguments, env))
            {
                ret = e;
                Console.Write(printSValue(e, env, 2));
            }
            Console.Write(delim);

            return ret;
        }
    }

    class SEHead : SExpression
    {
        private SExpression list;

        public SEHead(SExprAtomic ha, SExprComp c) : base(ha, c)
        {
            if (c.Atomics.Count != 1) throw new VMException("it takes 1 argument", ha);

            list = SExpression.Cast(c.Atomics.Pop());
        }

        public override SValue Evaluate(ExecEnvironment env)
        {
            var listObj = this.list.Evaluate(env);

            if (listObj is SList)
            {
                var list = listObj.Get<List<SValue>>();
                return list.Count > 0 ? list[0] : new SNull();
            }
            else
                throw new VMException("it can only get the head of a list", headAtom);
        }
    }

    class SETail : SExpression
    {
        private SExpression list;

        public SETail(SExprAtomic ha, SExprComp c) : base(ha, c)
        {
            if (c.Atomics.Count != 1) throw new VMException("it takes 1 argument", ha);
            
            list = SExpression.Cast(c.Atomics.Pop());
        }

        public override SValue Evaluate(ExecEnvironment env)
        {
            var listObj = this.list.Evaluate(env);

            if (listObj is SList)
                return new SList(listObj.Get<List<SValue>>().Skip(1).ToList());
            else
                throw new VMException("it can only get the tail of a list", headAtom);
        }
    }

    class SERegex : SExpression
    {
        private SExpression re;
        private SExpression option;

        public SERegex(SExprAtomic ha, SExprComp c) : base(ha, c)
        {
            if (c.Atomics.Count > 0)
            {
                re = SExpression.Cast(c.Atomics.Pop());
                if (c.Atomics.Count > 0)
                    option = SExpression.Cast(c.Atomics.Pop());
                else
                    option = new SString("None");
            }
            else
                throw new VMException("it takes 1 or 2 arguments", ha);
        }

        public override SValue Evaluate(ExecEnvironment env)
        {
            var re = this.re.Evaluate(env) as SString;
            if (re == null) throw new VMException("the first argument must be a string", headAtom);

            var option = this.option.Evaluate(env) as SString;
            if (option == null) throw new VMException("the second argument must be a string", headAtom);

            RegexOptions ro = RegexOptions.None;
            Enum.TryParse(option.Get<String>(), true, out ro);

            return new SValue(new Regex(re.Get<String>(), ro));
        }
    }

    class SERegexMatch : SExpression
    {
        private SExpression regex;
        private SExpression text;
        private SExpression start;
        private SExpression length;

        public SERegexMatch(SExprAtomic ha, SExprComp c) : base(ha, c)
        {
            if (c.Atomics.Count > 1)
            {
                regex = SExpression.Cast(c.Atomics.Pop());
                text = SExpression.Cast(c.Atomics.Pop());

                if (c.Atomics.Count > 0)
                    start = SExpression.Cast(c.Atomics.Pop());
                else
                    start = new SNumber(0);

                if (c.Atomics.Count > 0)
                    length = SExpression.Cast(c.Atomics.Pop());
                else
                    length = new SNumber(-1);
            }
            else
                throw new VMException("it takes at least 2 arguments", ha);
        }

        public override SValue Evaluate(ExecEnvironment env)
        {
            var regex = this.regex.Evaluate(env);
            if (!(regex.Underlying is Regex))
                throw new VMException("the first argument must be a regex object", headAtom);

            var text = this.text.Evaluate(env) as SString;
            if (text == null) throw new VMException("the second argument must be a string", headAtom);

            var start = this.start.Evaluate(env) as SNumber;
            if (start == null) throw new VMException("the start position must be a number", headAtom);

            var length = this.length.Evaluate(env) as SNumber;
            if (length == null) throw new VMException("the length must be a number", headAtom);
            var len = (int)length.Get<Decimal>();
            if (len == -1) len = text.Get<String>().Length - (int)start.Get<Decimal>();

            Regex re = regex.Underlying as Regex;

            try {
                Match m = re.Match(text.Get<String>(), (int)start.Get<Decimal>(), len);
                var capturedGroups = new Dictionary<string, SValue>();

                foreach (string gn in re.GetGroupNames())
                {
                    if (m.Groups[gn].Success && m.Groups[gn].Captures.Count > 0)
                    {
                        capturedGroups[gn] = new SDict(new Dictionary<string, SValue>()
                        {
                            { "capture", new SString(m.Groups[gn].Value) },
                            { "index", new SNumber(m.Groups[gn].Index) },
                            { "length", new SNumber(m.Groups[gn].Length) },
                        });
                    }
                }

                return new SDict(capturedGroups);
            }
            catch
            {
                throw new VMException("error ocurred when matching", headAtom);
            }
        }
    }
}
