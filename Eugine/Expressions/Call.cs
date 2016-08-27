using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eugine
{
    class SEExplode : SExpression
    {
        private SExpression list;

        public SEExplode(SExprAtomic ha, SExprComp c) : base(ha, c)
        {
            if (c.Atomics.Count != 1) throw new VMException("it takes 1 argument", ha);

            list = SExpression.Cast(c.Atomics.Pop());
        }

        public override SValue Evaluate(ExecEnvironment env)
        {
            var list = this.list.Evaluate(env) as SList;
            if (list == null) throw new VMException("only lists can be exploded", headAtom);

            return new SExploded(list.Get<List<SValue>>());
        }
    }

    class SEChain : SExpression
    {
        protected List<SExpression> expressions;

        public SEChain(SExprComp c)
        {
            expressions = (from e in c.Atomics select SExpression.Cast(e)).ToList();
        }

        public SEChain(IEnumerable<SExpression> ie)
        {
            expressions = ie.ToList();
        }

        public override SValue Evaluate(ExecEnvironment env)
        {
            SValue ret = new SBool(false);
            foreach (SExpression se in expressions) ret = se.Evaluate(env);

            return ret;
        }
    }

    class SEReverseChain : SEChain
    {
        public SEReverseChain(SExprComp c) :
            base((from e in c.Atomics select SExpression.Cast(e)).Reverse())
        { }
    }

    class SEMake : SExpression
    {
        private List<SExpression> arguments;
        private SExpression obj;

        public SEMake(SExprAtomic ha, SExprComp c) : base(ha, c)
        {
            if (c.Atomics.Count < 1) throw new VMException("it takes at least 1 argument", ha);

            obj = SExpression.Cast(c.Atomics.Pop());
            arguments = c.Atomics.Select(a => SExpression.Cast(a)).ToList();
        }

        public override SValue Evaluate(ExecEnvironment env)
        {
            var obj = Type.GetType((this.obj.Evaluate(env) as SString)?.Get<String>());
            if (obj == null) throw new VMException("cannot get type", headAtom);

            return new SNull();
        }
    }

    class SELambda : SExpression
    {
        private SExpression body;
        private List<string> arguments;

        public static List<string> CompoundToArguments(SExprComp c, SExprAtomic pos)
        {
            List<string> ret = new List<string>();

            for (var i = 0; i < c.Atomics.Count; i++)
            {
                var a = c.Atomics[i];
                if (!(a is SExprAtomic) || (a as SExprAtomic).Token.TType != SToken.TokenType.ATOMIC)
                    throw new VMException("argument name must be an atom", pos);

                var name = (string)(a as SExprAtomic).Token.TValue;
                if (i != c.Atomics.Count - 1 && name.Length > 3 && name.Substring(name.Length - 3) == "...")
                    throw new VMException("argument list must be at the end of the declaration", pos);

                ret.Add(name);
            }

            return ret;
        }

        public SELambda(SExprAtomic ha, SExprComp c)
        {
            if (c.Atomics.Count < 2) throw new VMException("missing lambda body", ha);
            if (!(c.Atomics[0] is SExprComp))
                throw new VMException("the first argument must be the declaration", ha);

            arguments = SELambda.CompoundToArguments(c.Atomics.Pop() as SExprComp, ha);
            body = SExpression.Cast(c.Atomics.Pop());
        }

        public override SValue Evaluate(ExecEnvironment env)
        {
            return new SClosure(env, arguments, body);
        }
    }

    class SEDefun : SExpression
    {
        private string func;
        private SExpression body;
        private List<string> arguments;
        private string description;

        public SEDefun(SExprAtomic ha, SExprComp c)
        {
            if (c.Atomics.Count < 3) throw new VMException("missing function body", ha);
            if (!(c.Atomics[1] is SExprComp))
                throw new VMException("the second argument must be the declaration", ha);

            var n = c.Atomics.Pop();
            if (!(n is SExprAtomic) || (n as SExprAtomic).Token.TType != SToken.TokenType.ATOMIC)
                throw new VMException("function name must be an atom", ha);

            func = (string)(n as SExprAtomic).Token.TValue;
            arguments = SELambda.CompoundToArguments(c.Atomics.Pop() as SExprComp, ha);

            var tmp = c.Atomics.Pop();
            if (tmp is SExprAtomic && (tmp as SExprAtomic).Token.TType == SToken.TokenType.STRING)
            {
                description = (string)(tmp as SExprAtomic).Token.TValue;
                if (c.Atomics.Count == 0) throw new VMException("missing function body", ha);
                body = SExpression.Cast(c.Atomics.Pop());
            } else
                body = SExpression.Cast(tmp);
        }

        public override SValue Evaluate(ExecEnvironment env)
        {
            env[func] = new SClosure(env, arguments, body);
            return new SBool(true);
        }
    }

    class SECall : SExpression
    {
        private string closureName;
        private SExpression lambdaObject;
        private List<SExpression> arguments;

        public SECall(string cls, List<SExpression> args, SExprAtomic ha, SExprComp c) : base (ha, c)
        {
            closureName = cls;
            arguments = args;
        }

        public SECall(SExpression cls, List<SExpression> args, SExprAtomic ha, SExprComp c) : base(ha, c)
        {
            lambdaObject = cls;
            arguments = args;
        }

        public override SValue Evaluate(ExecEnvironment env)
        {
            var newEnv = new ExecEnvironment();
            SValue _closure;
            SClosure closure = null;
            if (lambdaObject == null)
            {
                if (env.ContainsKey(closureName))
                {
                    _closure = env[closureName].Evaluate(env);
                    closure = _closure as SClosure;
                }
                else
                {
                    _closure = new SString(closureName);
                    env[closureName] = _closure;
                }
            }
            else
            {
                _closure = lambdaObject.Evaluate(env);
                closure = _closure as SClosure;
            }

            List<SValue> arguments = SExpression.EvalSExpressions(this.arguments, env);

            if (closure == null)
            {
                List<SValue> ret = new List<SValue>();
                ret.Add(_closure);
                foreach (var a in arguments) ret.Add(a);

                return new SList(ret);
            }

            if (closure.Arguments.Count() > arguments.Count())
            {
                var argNames = closure.Arguments.Skip(arguments.Count);
                return new SClosure(
                    env, argNames.ToList(), new SECall(
                        closure, 
                        arguments.ConvertAll(a => (SExpression)a)
                        .Concat(argNames.Select(a => new SEVariable(a, headAtom, tailCompound))).ToList(),
                        headAtom, tailCompound
                    )
                );
            }

            // prepare the executing environment
            for (int i = 0; i < closure.Arguments.Count(); i++)
            {
                string argName = closure.Arguments[i];
                if (argName.Length > 3 && argName.Substring(argName.Length - 3) == "...")
                {
                    argName = argName.Substring(0, argName.Length - 3);
                    newEnv[argName] = new SList(arguments.Skip(i).ToList());
                    break;
                }
                else
                    newEnv[argName] = arguments[i];
            }

            newEnv.ParentEnv = closure.InnerEnv;
            return closure.Body.Evaluate(newEnv);
        }
    }
}
