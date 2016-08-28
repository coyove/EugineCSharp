using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eugine
{
    class SESet : SExpression
    {
        private string varName;
        private SExpression nameExpr;
        private SExpression varValue;
        private bool makeImmutable;

        public SESet(SExprAtomic ha, SExprComp c, bool imm) : base(ha, c)
        {
            if (c.Atomics.Count < 2) throw new VMException("it takes 2 arguments", ha);

            var n = c.Atomics.Pop();
            if (n is SExprAtomic && (n as SExprAtomic).Token.TType == SToken.TokenType.ATOMIC)
                varName = (string)(n as SExprAtomic).Token.TValue;
            else
                nameExpr = SExpression.Cast(n);

            varValue = SExpression.Cast(c.Atomics.Pop());
            makeImmutable = imm;
        }

        public override SValue Evaluate(ExecEnvironment env)
        {
            SValue v = varValue.Evaluate(env);
            SValue ret = v;

            if (!makeImmutable && v.Immutable)
            {
                ret = v.Clone();
                ret.Immutable = false;
            }

            if (makeImmutable) ret.Immutable = true;
            
            ret.RefDict = null;
            ret.RefList = null;

            if (nameExpr == null)
            {
                if (env.ContainsKey(varName) && env[varName].Immutable)
                    throw new VMException(varName + ": variable is immutable", headAtom);

                env[varName] = ret;
            }
            else
            {
                var n = nameExpr.Evaluate(env);
                if (n.RefDict?.Immutable == true || n.RefList?.Immutable == true)
                    throw new VMException(varName + ": variable is immutable", headAtom);

                if (n.RefDict != null)
                    n.RefDict.Get<Dictionary<string, SValue>>()[n.RefDictKey] = ret;
                else if (n.RefList != null)
                    n.RefList.Get<List<SValue>>()[n.RefListIndex] = ret;
                else
                    throw new VMException("invalid variable setting", headAtom);
            }

            return ret;
        }
    }

    class SEList : SExpression
    {
        private List<SExpression> values;

        public SEList(SExprComp c)
        {
            values = (from v in c.Atomics select SExpression.Cast(v)).ToList();
        }

        public override SValue Evaluate(ExecEnvironment env)
        {
            return new SList((from v in values select v.Evaluate(env)).ToList());
        }
    }

    class SEDict : SExpression
    {
        private List<SExpression> values;

        public SEDict(SExprAtomic ha, SExprComp c) : base(ha, c)
        {
            values = c.Atomics.Select(a =>
            {
                if (!(a is SExprComp) || (a as SExprComp).Atomics.Count != 2)
                    throw new VMException("each element of the dict must be a list with 2 elements", ha);
                return SExpression.Cast(a);
            }).ToList();
        }

        public override SValue Evaluate(ExecEnvironment env)
        {
            Dictionary<String, SValue> ret = new Dictionary<String, SValue>();
            foreach (var v in values)
            {
                var c = v.Evaluate(env).Get<List<SValue>>();
                var key = c[0] as SString;
                if (key == null) throw new VMException("key must be string", headAtom);

                ret[key.Get<String>()] = c[1];
            }

            return new SDict(ret);
        }
    }

    class SEType : SExpression
    {
        private string vname;
        private SExpression nameExpr;

        public SEType(SExprAtomic ha, SExprComp c) : base(ha, c)
        {
            if (c.Atomics.Count == 0) throw new VMException("it takes 1 argument", ha);

            var n = c.Atomics.Pop();
            if (n is SExprAtomic && (n as SExprAtomic).Token.TType == SToken.TokenType.ATOMIC)
                vname = (string)(n as SExprAtomic).Token.TValue;
            else
                nameExpr = SExpression.Cast(n);
        }

        public override SValue Evaluate(ExecEnvironment env)
        {
            SValue ret = (nameExpr == null) ? ret = env[vname].Evaluate(env) : nameExpr.Evaluate(env);
            return new SString(ret.GetType().Name.Substring(1).ToLower());
        }
    }

    class SEGet : SExpression
    {
        private SExpression dict;
        private List<SExpression> keys;

        public SEGet(SExprAtomic ha, SExprComp c) : base(ha, c)
        {
            if (c.Atomics.Count < 2) throw new VMException("it takes at least 2 arguments", ha);

            dict = SExpression.Cast(c.Atomics.Pop());
            keys = c.Atomics.Select(a => SExpression.Cast(a)).ToList();
        }

        public override SValue Evaluate(ExecEnvironment env)
        {
            var dict = this.dict.Evaluate(env);
            
            for (var i = 0; i < keys.Count; i++)
            {
                if (dict is SDict)
                {
                    var key = this.keys[i].Evaluate(env) as SString;
                    if (key == null) throw new VMException("it only take a string as the key", headAtom);

                    var d = dict.Get<Dictionary<String, SValue>>();
                    var k = key.Get<String>();

                    if (!d.ContainsKey(k)) d[k] = new SNull();

                    d[k].RefDict = dict as SDict;
                    d[k].RefDictKey = k;
                    d[k].RefList = null;

                    dict = d[k];
                }
                else if (dict is SList)
                {
                    var index = this.keys[i].Evaluate(env) as SNumber;
                    if (index == null) throw new VMException("it only take a number as the index", headAtom);

                    var l = dict.Get<List<SValue>>();
                    var idx = (int)index.Get<Decimal>();

                    if (idx >= l.Count) throw new VMException("index out of range", headAtom);

                    l[idx].RefList = dict as SList;
                    l[idx].RefListIndex = idx;
                    l[idx].RefDict = null;

                    dict = l[idx];
                }
                else
                    throw new VMException("it only take a list or a dict as the first argument", headAtom);
            }

            return dict;
        }
    }

    class SERange : SExpression
    {
        private SExpression start;
        private SExpression interval;
        private SExpression end;

        public SERange(SExprAtomic ha, SExprComp c) : base(ha, c)
        {
            var args = (from v in c.Atomics select SExpression.Cast(v)).ToList();

            if (args.Count == 2)
            {
                start = args[0];
                end = args[1];
                interval = new SNumber(1);
            }
            else if (args.Count == 3)
            {
                start = args[0];
                interval = args[1];
                end = args[2];
            }
            else
                throw new VMException("it takes 2 or 3 arguments", ha);
        }

        public override SValue Evaluate(ExecEnvironment env)
        {
            var start = this.start.Evaluate(env);
            var interval = this.interval.Evaluate(env);
            var end = this.end.Evaluate(env);

            if (!(start is SNumber) || !(interval is SNumber) || !(end is SNumber))
                throw new VMException("it only accept numbers as arguments", headAtom);

            List<SValue> ret = new List<SValue>();
            for (var i = start.Get<Decimal>();
                i < end.Get<Decimal>();
                i += interval.Get<Decimal>()) ret.Add(new SNumber(i));

            return new SList(ret);
        }
    }

    class SEVariable : SExpression
    {
        private string varName;

        public SEVariable(string n, SExprAtomic ha, SExprComp c) : base(ha, c)
        {
            varName = n;
        }

        public override SValue Evaluate(ExecEnvironment env)
        {
            if (!env.ContainsKey(varName))
            {
                SValue tmp = (varName.First() == '@') ? 
                    (SValue)new SString(varName.Substring(1), true) : new SNull(true);

                if (env.ContainsKey(varName)) return env[varName];

                env[varName] = tmp;
                return tmp;
            }

            var imd = env[varName].Evaluate(env);

            if (imd is SClosure && (imd as SClosure).Arguments.Count == 0)
                // it is a closure that accepts 0 arguments, so just directly evaluate it
                return (imd as SClosure).Body.Evaluate(env);
            else
                return imd;
        }
    }
}
