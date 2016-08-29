using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eugine
{
    abstract class SExpression
    {
        protected SExprAtomic headAtom;
        protected SExprComp tailCompound;

        public SExpression(SExprAtomic ha, SExprComp c)
        {
            headAtom = ha; tailCompound = c;
        }

        public SExpression() {}

        public abstract SValue Evaluate(ExecEnvironment env);

        public static Random DefaultRandom = 
            new Random(DateTime.UtcNow.Subtract(new DateTime(1993, 7, 31)).Milliseconds % Int32.MaxValue);

        public static CallsEnvironment KeywordsLookup = new CallsEnvironment
        {
            { "exit",   (h, c) => new SEExit(h, c) },
            { "set",    (h, c) => new SESet(h, c, false) },
            { "const",  (h, c) => new SESet(h, c, true) },
            { "=",      (h, c) => new SESet(h, c, false) },
            { "var",    (h, c) => new SEVar(h, c, false) },
            { ":=",     (h, c) => new SEVar(h, c, false) },
            { "if",     (h, c) => new SEIf(h, c) },
            { "cond",   (h, c) => new SECond(h, c) },
            { "->",     (h, c) => new SEChain(c) },
            { "chain",  (h, c) => new SEChain(c) },
            { "<-",     (h, c) => new SEReverseChain(c)},
            { "lambda", (h, c) => new SELambda(h, c) },
            { "=>",     (h, c) => new SELambda(h, c) },
            { "defun",  (h, c) => new SEDefun(h, c) },
            { "list",   (h, c) => new SEList(c) },
            { "dict",   (h, c) => new SEDict(h, c) },
            { "keys",   (h, c) => new SEKeys(h, c) },
            { "del",    (h, c) => new SEDel(h, c) },
            { ":",      (h, c) => new SEGet(h, c) },
            { "range",  (h, c) => new SERange(h, c) },
            { "for",    (h, c) => new SEFor(h, c) },
            { "loop",   (h, c) => new SEFor(h, c) },
            { "<",      (h, c) => new SEMultiCore(h, c, "<") },
            { "<=",     (h, c) => new SEMultiCore(h, c, "<=") },
            { ">",      (h, c) => new SEMultiCore(h, c, ">") },
            { ">=",     (h, c) => new SEMultiCore(h, c, ">=") },
            { "==",     (h, c) => new SEMultiCore(h, c, "==") },
            { "<>",     (h, c) => new SEMultiCore(h, c, "<>") },
            { "!=",     (h, c) => new SEMultiCore(h, c, "<>") },
            { "+",      (h, c) => new SEMultiCore(h, c, "+") },
            { "-",      (h, c) => new SEMultiCore(h, c, "-") },
            { "*",      (h, c) => new SEMultiCore(h, c, "*") },
            { "/",      (h, c) => new SEMultiCore(h, c, "/") },
            { "%",      (h, c) => new SEMultiCore(h, c, "%") },
            { "&&",     (h, c) => new SEMultiCore(h, c, "&&") },
            { "||",     (h, c) => new SEMultiCore(h, c, "||") },
            { "and",    (h, c) => new SEMultiCore(h, c, "&&") },
            { "or",     (h, c) => new SEMultiCore(h, c, "||") },
            { "++",     (h, c) => new SEIncDec(h, c, true) },
            { "--",     (h, c) => new SEIncDec(h, c, false) },
            { "+=",     (h, c) => new SESelfOperator(h, c, "+") },
            { "-=",     (h, c) => new SESelfOperator(h, c, "-") },
            { "*=",     (h, c) => new SESelfOperator(h, c, "*") },
            { "/=",     (h, c) => new SESelfOperator(h, c, "/") },
            { "print", 	(h, c) => new SEPrint(h, c, "") },
            { "println",(h, c) => new SEPrint(h, c, Environment.NewLine) },
            { "str",    (h, c) => new SEStr(h, c, false) },
            { "chr",    (h, c) => new SEStr(h, c, true) },
            { "num",    (h, c) => new SENum(h, c, false) },
            { "asc",    (h, c) => new SENum(h, c, true) },
            { "sub",    (h, c) => new SESub(h, c) },
            { "len",    (h, c) => new SELen(h, c) },
            { "head",   (h, c) => new SEHead(h, c) },
            { "tail",   (h, c) => new SETail(h, c) },
            { "type",   (h, c) => new SEType(h, c) },
            { "eval",   (h, c) => new SEEval(h, c) },
            { "split",  (h, c) => new SESplit(h, c) },
            { "!",      (h, c) => new SESingleCore(h, c, "not") },
            { "not",    (h, c) => new SESingleCore(h, c, "not") },
            { "sin",    (h, c) => new SESingleCore(h, c, "sin") },
            { "cos",    (h, c) => new SESingleCore(h, c, "cos") },
            { "tan",    (h, c) => new SESingleCore(h, c, "tan") },
            { "abs",    (h, c) => new SESingleCore(h, c, "abs") },
            { "asin",   (h, c) => new SESingleCore(h, c, "asin") },
            { "acos",   (h, c) => new SESingleCore(h, c, "acos") },
            { "atan",   (h, c) => new SESingleCore(h, c, "atan") },
            { "sqrt",   (h, c) => new SESingleCore(h, c, "sqrt") },
            { "time",   (h, c) => new SESingleCore(h, c, "time") },
            { "round",  (h, c) => new SESingleCore(h, c, "round") },
            { "floor",  (h, c) => new SESingleCore(h, c, "floor") },
            { "random", (h, c) => new SESingleCore(h, c, "random") },
            { "explode",(h, c) => new SEExplode(h, c) },
            { "regex",  (h, c) => new SERegex(h, c) },
            { "match",  (h, c) => new SERegexMatch(h, c) },

            { ".net-type",              (h, c) => new SEInteropGetType(h, c) },
            { ".net-static-method",     (h, c) => new SEInteropInvokeStaticMethod(h, c) },
            { ".net-get-member",        (h, c) => new SEInteropGetSetMember(h, c) },
            { ".net-new",               (h, c) => new SEInteropNew(h, c) },
        };

        public static List<SValue> EvalSExpressions(List<SExpression> arguments, ExecEnvironment env)
        {

            List<SValue> ret = new List<SValue>();
            foreach (var e in arguments)
            {
                var v = e.Evaluate(env);
                if (v is SExploded)
                    ret.AddRange((v as SExploded).Comps);
                else
                    ret.Add(v);
            }

            return ret;
        }

        public static SExpression Cast(SExpr e)
        {
            if (e is SExprAtomic)
                return Cast(e as SExprAtomic);

            if (e is SExprComp)
                return Cast(e as SExprComp);

            return null;
        }

        public static SExpression Cast(SExprAtomic e)
        {
            if (e.Token.TType == SToken.TokenType.STRING)
            {
                return new SString((string)e.Token.TValue);
            }
            else if (e.Token.TType == SToken.TokenType.NUMBER)
            {
                return new SNumber((decimal)e.Token.TValue);
            }
            else if (e.Token.TType == SToken.TokenType.SEXPRESSION)
            {
                return (SExpression)e.Token.TValue;
            }
            else
            {
                return new SEVariable((string)e.Token.TValue, e, null);
            }
        }

        public static SExpression Cast(SExprComp c)
        {
            if (c.Atomics.Count == 0) return new SEList(c);

            var _head = c.Atomics.Pop();

            if (_head is SExprComp)
            {
                if (c.Atomics.Count == 0)
                    return SExpression.Cast(_head);
                else
                {
                    var first = _head;
                    while (first is SExprComp)
                        first = (first as SExprComp).Atomics[0];

                    return new SECall(SExpression.Cast(_head),
                        (from a in c.Atomics select SExpression.Cast(a)).ToList(), 
                        first as SExprAtomic, c);
                }
            }

            var head = _head as SExprAtomic;

            if (head.Token.TType == SToken.TokenType.ATOMIC)
            {
                string tvalue = (string)head.Token.TValue;
                if (SExpression.KeywordsLookup.ContainsKey(tvalue))
                {
                    return SExpression.KeywordsLookup[tvalue](head, c);
                }
                else
                {
                    if (c.Atomics.Count == 0)
                        return new SEVariable(tvalue, head, c);
                    else
                        return new SECall(tvalue, (from a in c.Atomics select SExpression.Cast(a)).ToList(), head, c);
                }
            }
            else if (head.Token.TType == SToken.TokenType.STRING || head.Token.TType == SToken.TokenType.NUMBER)
            {
                c.Atomics.Insert(0, head);
                return new SEList(c);
            }
            else
            {
                throw new VMException("invalid cast", head);
            }
        }
    }

    abstract class SValue : SExpression
    {
        public SDict RefDict;
        public string RefDictKey;

        public SList RefList;
        public int RefListIndex;

        public object Underlying;
        public bool Immutable;

        public SValue(object underlying)
        {
            Underlying = underlying;
            Immutable = false;
        }

        public SValue(object underlying, bool imm)
        {
            Underlying = underlying;
            Immutable = imm;
        }

        public T Get<T>()
        {
            return (T)Underlying;
        }

        public bool Is<T>()
        {
            return Underlying is T;
        }

        public override SValue Evaluate(ExecEnvironment env)
        {
            return this;
        }

        // It clones the SValue, but not includes the object Underlaying
        public abstract SValue Clone();

        public static void CopyAttributes(SValue to, SValue from)
        {
            to.Immutable = from.Immutable;
            to.RefDict = from.RefDict;
            to.RefDictKey = from.RefDictKey;
            to.RefList = from.RefList;
            to.RefListIndex = from.RefListIndex;
        }
    }

    class SObject : SValue
    {
        public SObject(object obj) : base(obj) { }

        public override SValue Clone()
        {
            SObject ret = new SObject(Underlying);
            SValue.CopyAttributes(ret, this);
            return ret;
        }
    }

    class SBool : SValue
    {
        public SBool(bool b): base(b) {}
        public SBool(bool b, bool imm) : base(b, imm) { }

        public override SValue Clone()
        {
            SBool ret = new SBool((bool)Underlying);
            SValue.CopyAttributes(ret, this);
            return ret;
        }
    }

    class SString : SValue
    {
        public SString(String str) : base(str) { }
        public SString(String str, bool imm) : base(str, imm) { }

        public override SValue Clone()
        {
            SString ret = new SString((string)Underlying);
            SValue.CopyAttributes(ret, this);
            return ret;
        }
    }

    class SNumber : SValue
    {
        public SNumber(Decimal num) : base(num) { }
        public SNumber(Decimal num, bool imm) : base(num, imm) { }

        public override SValue Clone()
        {
            SNumber ret = new SNumber((decimal)Underlying);
            SValue.CopyAttributes(ret, this);
            return ret;
        }
    }

    class SList : SValue
    {
        public SList(List<SValue> list) : base(list) { }

        public override SValue Clone()
        {
            SList ret = new SList((List<SValue>)Underlying);
            SValue.CopyAttributes(ret, this);
            return ret;
        }
    }

    class SDict : SValue
    {
        public SDict(Dictionary<string, SValue> dict) : base(dict) { }

        public override SValue Clone()
        {
            SDict ret = new SDict((Dictionary<string, SValue>)Underlying);
            SValue.CopyAttributes(ret, this);
            return ret;
        }
    }

    class SExploded : SValue
    {
        public List<SValue> Comps;

        public SExploded(List<SValue> list) : base(list)
        {
            Comps = list.Select(v => v).ToList();
        }

        public override SValue Clone()
        {
            SExploded ret = new SExploded(Comps);
            SValue.CopyAttributes(ret, this);
            return ret;
        }
    }

    class SNull : SValue
    {
        public SNull() : base(null) { }
        public SNull(bool imm) : base(null, imm) { }

        public override SValue Clone()
        {
            return new SNull();
        }
    }

    class SClosure : SValue
    {
        public ExecEnvironment InnerEnv;
        public List<string> Arguments;
        public SExpression Body;

        public SClosure(ExecEnvironment env, List<string> args, SExpression b) : base(b)
        {
            InnerEnv = env;
            Arguments = args;
            Body = b;
        }

        public override SValue Clone()
        {
            SClosure ret = new SClosure(InnerEnv, Arguments, Body);
            SValue.CopyAttributes(ret, this);
            return ret;
        }
    }
    
}
