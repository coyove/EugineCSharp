using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Eugine
{
    static class InteropHelper
    {
        public static SValue ObjectToSValue(object obj)
        {
            if (obj is string)
                return new SString((string)obj);
            else if (obj is Byte  || obj is SByte
                || obj is UInt16  || obj is UInt32 || obj is UInt64
                || obj is Int16   || obj is Int32  || obj is Int64
                || obj is Decimal || obj is Double || obj is Single)
                return new SNumber((Decimal)Convert.ChangeType(obj, typeof(Decimal)));
            else if (obj is bool)
                return new SBool((bool)obj);
            else if (obj == null)
                return new SNull();
            else if (typeof(IEnumerable).IsAssignableFrom(obj.GetType()))
            {
                List<SValue> ret = new List<SValue>();
                var en = ((IEnumerable)obj).GetEnumerator();
                while (en.MoveNext()) ret.Add(ObjectToSValue(en.Current));

                return new SList(ret);
            }

            return new SObject(obj);
        }

        public static Tuple<List<Type>, List<Object>> 
            BuildMethodPattern(List<SExpression> args, ExecEnvironment env, SExprAtomic headAtom)
        {
            List<Type> pattern = new List<Type>();
            List<object> arguments = new List<object>();

            args.ForEach(arg =>
            {
                var a = arg.Evaluate(env);
                if (a is SList && (a as SList).Get<List<SValue>>().Count == 2)
                {
                    var list = (a as SList).Get<List<SValue>>();
                    var typeName = list[0] as SString;
                    if (typeName == null) throw new VMException("type name must be a string", headAtom);

                    var t = Type.GetType(typeName.Get<String>());
                    pattern.Add(t);

                    if (t == typeof(Object))
                        arguments.Add(list[1].Underlying);
                    else
                        arguments.Add(Convert.ChangeType(list[1].Underlying, t));
                }
                else
                {
                    if (a is SString)
                    {
                        pattern.Add(typeof(string));
                        arguments.Add((string)a.Underlying);
                    }
                    else if (a is SBool)
                    {
                        pattern.Add(typeof(bool));
                        arguments.Add((bool)a.Underlying);
                    }
                    else
                        throw new VMException("you must specify a type to avoid ambiguousness", headAtom);
                }
            });

            return new Tuple<List<Type>, List<object>>(pattern, arguments);
        }
    }

    class SEInteropGetType : SExpression
    {
        private SExpression typeName;

        public SEInteropGetType(SExprAtomic ha, SExprComp c) : base(ha, c)
        {
            if (c.Atomics.Count != 1) throw new VMException("it takes 1 argument", ha);
            typeName = SExpression.Cast(c.Atomics.Pop());
        }

        public override SValue Evaluate(ExecEnvironment env)
        {
            var type = Type.GetType((this.typeName.Evaluate(env) as SString)?.Get<String>());
            if (type == null) throw new VMException("cannot get type", headAtom);

            return new SObject(type);
        }
    }

    class SEInteropInvokeStaticMethod : SExpression
    {
        private SExpression type;
        private SExpression methodName;
        private List<SExpression> arguments;

        public SEInteropInvokeStaticMethod(SExprAtomic ha, SExprComp c) : base(ha, c)
        {
            if (c.Atomics.Count < 2) throw new VMException("it takes at least 2 arguments", ha);
            
            type = SExpression.Cast(c.Atomics.Pop());
            methodName = SExpression.Cast(c.Atomics.Pop());
            arguments = c.Atomics.Select(a => SExpression.Cast(a)).ToList();
        }

        public override SValue Evaluate(ExecEnvironment env)
        {
            object subObj = this.type.Evaluate(env).Underlying;
            var type = subObj as Type;
            var mip = InteropHelper.BuildMethodPattern(this.arguments, env, headAtom);

            List<Type> pattern = mip.Item1;
            List<object> arguments = mip.Item2;
            
            var method = methodName.Evaluate(env) as SString;
            if (method == null) throw new VMException("method name must be a string", headAtom);

            if (type != null)
            {
                var m = type.GetMethod(method.Get<String>(), pattern.ToArray());
                if (m == null) throw new VMException("cannot get the method", headAtom);

                return InteropHelper.ObjectToSValue(m.Invoke(null, arguments.ToArray()));
            }
            else if (subObj != null)
            {
                var m = subObj.GetType().GetMethod(method.Get<String>(), pattern.ToArray());
                if (m == null) throw new VMException("cannot get the method", headAtom);

                return InteropHelper.ObjectToSValue(m.Invoke(subObj, arguments.ToArray()));
            }
            else
                throw new VMException("calling on a null object", headAtom);

            //DateTime.Today.GetDateTimeFormats();
        }
    }

    class SEInteropGetSetMember : SExpression
    {
        private SExpression obj;
        private SExpression propName;

        public SEInteropGetSetMember(SExprAtomic ha, SExprComp c) : base(ha, c)
        {
            if (c.Atomics.Count < 2) throw new VMException("it takes at least 2 arguments", ha);

            obj = SExpression.Cast(c.Atomics.Pop());
            propName = SExpression.Cast(c.Atomics.Pop());
        }

        public override SValue Evaluate(ExecEnvironment env)
        {
            object subObj = this.obj.Evaluate(env).Underlying;

            var propName = this.propName.Evaluate(env) as SString;
            if (propName == null) throw new VMException("property name must be a string", headAtom);

            if (subObj == null) throw new VMException("property operation on a null object", headAtom);

            var info = subObj.GetType().GetMember(propName.Get<String>()).Select(m =>
            {
                if (m is FieldInfo)
                    return ((FieldInfo)m).GetValue(subObj);
                else
                    return ((PropertyInfo)m).GetValue(subObj, null);
            });
            
            return InteropHelper.ObjectToSValue(info);
        }
    }

    class SEInteropNew : SExpression
    {
        private SExpression type;
        private List<SExpression> arguments;

        public SEInteropNew(SExprAtomic ha, SExprComp c) : base(ha, c)
        {
            if (c.Atomics.Count < 1) throw new VMException("it takes at least 1 argument", ha);

            type = SExpression.Cast(c.Atomics.Pop());
            arguments = c.Atomics.Select(a => SExpression.Cast(a)).ToList();
        }

        public override SValue Evaluate(ExecEnvironment env)
        {
            var type = this.type.Evaluate(env).Underlying as Type;
            var mip = InteropHelper.BuildMethodPattern(this.arguments, env, headAtom);

            List<Type> pattern = mip.Item1;
            List<object> arguments = mip.Item2;

            if (type == null) throw new VMException("invalid type", headAtom);

            var ctor = type.GetConstructor(pattern.ToArray());
            if (ctor == null) throw new VMException("cannot get a valid constructor", headAtom);
            return InteropHelper.ObjectToSValue(ctor.Invoke(arguments.ToArray()));
            
        }
    }
}
