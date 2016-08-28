using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eugine
{
    class SEFor : SExpression
    {
        private SExpression list;
        private SExpression body;

        public SEFor(SExprAtomic ha, SExprComp c) : base(ha, c)
        {
            if (c.Atomics.Count < 2) throw new VMException("it takes 2 arguments", ha);

            list = SExpression.Cast(c.Atomics.Pop());
            body = SExpression.Cast(c.Atomics.Pop());
        }

        private SValue execLoop(SClosure body, SValue v, Decimal idx)
        {
            var newEnv = new ExecEnvironment();
            if (body.Arguments.Count >= 1)
                newEnv[body.Arguments[0]] = v;

            if (body.Arguments.Count == 2)
                newEnv[body.Arguments[1]] = new SNumber(idx);

            newEnv.ParentEnv = body.InnerEnv;
            return body.Body.Evaluate(newEnv);
        }

        public override SValue Evaluate(ExecEnvironment env)
        {
            var body = this.body.Evaluate(env) as SClosure;
            if (body == null)
                throw new VMException("the second argument must be a lambda or a named function", headAtom);

            SValue _list = this.list.Evaluate(env);
            List<SValue> values = new List<SValue>();
            SList list = new SList(values);

            bool whileLoop = false;
            bool condAlwaysTrue = false;

            if (_list is SList)
            {
                list = _list as SList;
                values = list.Get<List<SValue>>();
                if (values.Count == 0)
                {
                    whileLoop = true;
                    condAlwaysTrue = true;
                }
            }
            else if (_list is SBool)
            {
                whileLoop = true;
                condAlwaysTrue = false;
            }
            else
                throw new VMException("the first argument must be a list or a bool", headAtom);
            
            if (whileLoop)
                while (condAlwaysTrue || this.list.Evaluate(env).Get<bool>())
                {
                    var ret = execLoop(body, new SNull(), 0);
                    if (ret.Is<bool>() && !ret.Get<bool>()) break;
                }
            else
                for (var i = 0; i < values.Count; i++)
                {
                    var ret = execLoop(body, values[i], i);
                    if (ret.Is<bool>() && !ret.Get<bool>()) break;
                }
            

            return new SBool(true);
        }
    }

    class SEIf : SExpression
    {
        private SExpression condition;
        private SExpression trueBranch;
        private SExpression falseBranch;

        public SEIf(SExprAtomic ha, SExprComp c)
        {
            if (c.Atomics.Count == 0) throw new VMException("missing true branch", ha);
            if (c.Atomics.Count == 1) throw new VMException("missing false branch", ha);

            condition = SExpression.Cast(c.Atomics.Pop());
            trueBranch = SExpression.Cast(c.Atomics.Pop());
            falseBranch = SExpression.Cast(c.Atomics.Pop());
        }

        public override SValue Evaluate(ExecEnvironment env)
        {
            if (condition.Evaluate(env).Get<bool>())
                return trueBranch.Evaluate(env);
            else
            {
                if (falseBranch != null)
                    return falseBranch.Evaluate(env);
                else
                    return new SNull();
            }
        }
    }

    class SECond : SExpression
    {
        private SExpression condition;
        private List<Tuple<SExpression, SExpression>> branches;
        private SExpression defaultBranch;

        public SECond(SExprAtomic ha, SExprComp c)
        {
            if (c.Atomics.Count < 2) throw new VMException("at least 1 branch must be defined", ha);

            condition = SExpression.Cast(c.Atomics.Pop());
            branches = new List<Tuple<SExpression, SExpression>>();

            foreach (var a in c.Atomics)
            {
                var b = a as SExprComp;
                if (b == null || b.Atomics.Count < 2) throw new VMException("each branch must be a compound", ha);

                var cond = b.Atomics.Pop();

                if (cond is SExprAtomic &&
                    ((SExprAtomic)cond).Token.TType == SToken.TokenType.ATOMIC &&
                    (string)((SExprAtomic)cond).Token.TValue == "_")
                    defaultBranch = SExpression.Cast(b.Atomics.Pop());
                else
                    branches.Add(new Tuple<SExpression, SExpression>(
                        SExpression.Cast(cond),
                        SExpression.Cast(b.Atomics.Pop())
                    ));
            }
        }

        public override SValue Evaluate(ExecEnvironment env)
        {
            var cond = condition.Evaluate(env).Get<object>();

            foreach (var b in branches)
            {
                if (b.Item1.Evaluate(env).Get<object>().Equals(cond))
                    return b.Item2.Evaluate(env);
            }

            if (defaultBranch != null)
                return defaultBranch.Evaluate(env);

            return new SBool(false);
        }
    }
}
