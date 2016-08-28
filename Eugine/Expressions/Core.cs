using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eugine
{
    class SEMultiCore : SExpression
    {
        private List<SValue> arguments;
        private List<SExpression> argumentExprs;
        private string func;

        public SEMultiCore(SExprAtomic ha, SExprComp c, string f) : base(ha, c)
        {
            argumentExprs = (from a in c.Atomics select SExpression.Cast(a)).ToList();
            func = f;

            if (argumentExprs.Count < 1) throw new VMException("it takes at least 1 argument", ha);
        }

        private bool compareNumber(int[] trueSigns, ExecEnvironment env)
        {
            if (arguments.Count < 2) throw new VMException("it takes at least 2 arguments", headAtom);

            for (var i = 0; i < arguments.Count() - 1; i++)
            {
                var v1 = arguments[i].Evaluate(env);
                var v2 = arguments[i + 1].Evaluate(env);

                if (v1.Is<Decimal>() && v2.Is<Decimal>())
                {
                    var sgn = Math.Sign(v1.Get<Decimal>() - v2.Get<Decimal>());
                    if (!trueSigns.Contains(sgn))
                        return false;
                }
                else
                    throw new VMException("inconsistent comparison of types", headAtom);
            }

            return true;
        }

        private bool compareBool(string andor, ExecEnvironment env)
        {
            if (arguments.Count < 2) throw new VMException("it takes at least 2 arguments", headAtom);

            bool lastState = andor == "and";

            for (var i = 0; i < arguments.Count(); i++)
            {
                var v1 = arguments[i].Evaluate(env);
                if (v1 is SBool)
                {
                    var v = v1.Get<bool>();
                    if (andor == "and")
                        lastState = lastState && v;
                    else
                        lastState = lastState || v;
                }
                else if (v1 is SNull)
                {
                    if (andor == "and") lastState = false;
                }
                else
                    throw new VMException("inconsistent comparison of types", headAtom);
            }

            return lastState;
        }

        private bool objectEquality(bool compareEqual, ExecEnvironment env)
        {
            var f = arguments[0].Evaluate(env);
            if (f.Underlying == null)
            {
                foreach (var a in arguments.Skip(1))
                {
                    var n = a.Evaluate(env);
                    if (n.Underlying == null && !compareEqual) return false;
                    if (n.Underlying != null && compareEqual) return false;
                }
            }
            else {

                foreach (var a in arguments.Skip(1))
                {
                    var n = a.Evaluate(env);
                    if (!f.Underlying.Equals(n.Underlying) && compareEqual) return false;
                    if (f.Underlying.Equals(n.Underlying) && !compareEqual) return false;
                }
            }

            return true;

        }

        private SValue tryPlus(ExecEnvironment env)
        {
            object f = arguments[0].Evaluate(env).Underlying;

            for (var i = 1; i < arguments.Count(); i++)
            {
                var n = arguments[i].Evaluate(env);
                if (f is String && n.Underlying is String)
                    f = (String)f + (String)n.Underlying;
                else if (f is Decimal && n.Underlying is Decimal)
                    f = (Decimal)f + (Decimal)n.Underlying;
                else if (f is List<SValue>)
                {
                    f = ((List<SValue>)f).Select(v => v).ToList();
                    ((List<SValue>)f).Add(n);
                }
                else
                    throw new VMException("it only apply to numbers, strings or lists", headAtom);
            }

            if (f is String)
                return new SString(f as String);
            else if (f is Decimal)
                return new SNumber((decimal)f);
            else if (f is List<SValue>)
                return new SList(f as List<SValue>);
            else
                throw new VMException("failed plus", headAtom);
        }

        public override SValue Evaluate(ExecEnvironment env)
        {
            arguments = SExpression.EvalSExpressions(argumentExprs, env);

            try {
                switch (func)
                {
                    case "<":   return new SBool(compareNumber(new int[] { -1 }, env));
                    case "<=":  return new SBool(compareNumber(new int[] { -1, 0 }, env));
                    case ">":   return new SBool(compareNumber(new int[] { 1 }, env));
                    case ">=":  return new SBool(compareNumber(new int[] { 1, 0 }, env));
                    case "==":  return new SBool(objectEquality(true, env));
                    case "<>":  return new SBool(objectEquality(false, env));
                    case "&&":  return new SBool(compareBool("and", env));
                    case "||":  return new SBool(compareBool("or", env));
                    case "+":   return tryPlus(env);
                    case "-": case "*": case "/": case "%":
                        var r = arguments[0].Evaluate(env);
                        if (!r.Is<Decimal>()) throw new VMException("it only apply to numbers", headAtom);

                        var host = r.Get<Decimal>();
                        for (var i = 1; i < arguments.Count(); i++)
                        {
                            var v1 = arguments[i].Evaluate(env);

                            if (v1.Is<Decimal>())
                            {
                                switch (func)
                                {
                                    case "-": host -= v1.Get<Decimal>(); break;
                                    case "*": host *= v1.Get<Decimal>(); break;
                                    case "/":
                                    case "%":
                                        Decimal tmp = v1.Get<Decimal>();
                                        if (tmp != 0)
                                        {
                                            if (func == "/")
                                                host /= tmp;
                                            else
                                                host %= tmp;
                                        }
                                        else
                                            throw new VMException("divided by 0", headAtom);
                                        break;
                                    default:
                                        throw new NotImplementedException();
                                }
                            }
                            else
                                throw new VMException("it only apply to numbers", headAtom);
                        }

                        return new SNumber(host);
                    default:
                        throw new NotImplementedException();
                }
            }
            catch (System.OverflowException)
            {
                throw new VMException("arithmetic operation failed due to overflow", headAtom);
            }
        }
    }

    class SESingleCore : SExpression
    {
        private SExpression argument;
        private string func;

        public SESingleCore(SExprAtomic ha, SExprComp c, string f) : base(ha, c)
        {
            argument = c.Atomics.Count > 0 ? SExpression.Cast(c.Atomics.Pop()) : null;
            func = f;
        }

        public override SValue Evaluate(ExecEnvironment env)
        {
            if (argument == null)
                return new SClosure(env, new List<string>() { "x" }, SExpression.Cast(SExpr.MakeLambda(func, "x")));

            var arg = argument.Evaluate(env);
            Decimal n = 0;

            if (arg is SNumber)
                n = arg.Get<Decimal>();
            else if (arg is SBool)
                n = arg.Get<bool>() ? 1 : 0;

            switch (func)
            {
                case "sin":
                    return new SNumber((Decimal)Math.Sin((double)n));
                case "cos":
                    return new SNumber((Decimal)Math.Cos((double)n));
                case "tan":
                    return new SNumber((Decimal)Math.Tan((double)n));
                case "asin":
                    return new SNumber((Decimal)Math.Asin((double)n));
                case "acos":
                    return new SNumber((Decimal)Math.Acos((double)n));
                case "atan":
                    return new SNumber((Decimal)Math.Atan((double)n));
                case "round":
                    return new SNumber((Decimal)Math.Round((double)n));
                case "floor":
                    return new SNumber((Decimal)Math.Floor((double)n));
                case "abs":
                    return new SNumber((Decimal)Math.Abs((double)n));
                case "sqrt":
                    return new SNumber((Decimal)Math.Sqrt((double)n));
                case "random":
                    if (n != 0)
                    {
                        var rand = new Random((int)(n % Int32.MaxValue));
                        return new SNumber((Decimal)rand.NextDouble());
                    }
                    else
                        return new SNumber((Decimal)SExpression.DefaultRandom.NextDouble());
                case "time":
                    var t = (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)));
                    return new SNumber((Decimal)t.TotalMilliseconds / n);
                case "not":
                    return new SBool(Math.Abs(n - 1) == 1);
                default:
                    throw new NotImplementedException();
            }
        }
    }

    class SEIncDec : SESet
    {
        private static SExprComp wrap(SExprAtomic ha, SExprComp c, bool inc)
        {
            if (c.Atomics.Count != 1) throw new VMException("it takes 1 argument", ha);
            SExprComp ret = new SExprComp();
            SExprComp op = new SExprComp();
            ret.Atomics = c.Atomics.Take(1).ToList();

            op.Atomics.Add(new SExprAtomic(new SToken(SToken.TokenType.ATOMIC, inc ? "+" : "-")));
            op.Atomics.Add(ret.Clone());
            op.Atomics.Add(new SExprAtomic(new SToken(SToken.TokenType.NUMBER, new Decimal(1))));

            ret.Atomics.Add(op);
            return ret;
        }

        public SEIncDec(SExprAtomic ha, SExprComp c, bool i) : base(ha, SEIncDec.wrap(ha, c, i), false)
        { }
    }

    class SESelfOperator : SESet
    {
        private static SExprComp wrap(SExprAtomic ha, SExprComp c, string o)
        {
            if (c.Atomics.Count != 2) throw new VMException("it takes 2 arguments", ha);
            SExprComp ret = new SExprComp();
            SExprComp op = new SExprComp();
            ret.Atomics = c.Atomics.Take(1).ToList();

            op.Atomics.Add(new SExprAtomic(new SToken(SToken.TokenType.ATOMIC, o)));
            op.Atomics.Add(ret.Clone());
            op.Atomics.Add(c.Atomics.Skip(1).Take(1).First().Clone());

            ret.Atomics.Add(op);
            return ret;
        }

        public SESelfOperator(SExprAtomic ha, SExprComp c, string op) 
            : base(ha, SESelfOperator.wrap(ha, c, op), false)
        { }
    }
}
