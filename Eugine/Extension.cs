using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eugine
{
    class LineFinder
    {
        private string text;
        private int lastIndex;
        private int lastLine;
        private int lineIndex;

        public LineFinder(string t)
        {
            text = t;
            lastIndex = 0;
            lastLine = 0;
            lineIndex = 0;
        }

        public Tuple<int, int> FindIndexAtLineIndex(int index)
        {
            while (lastIndex < text.Length)
            {
                if (text[lastIndex] == '\n')
                {
                    lastLine++;
                    lineIndex = 0;
                }

                if (lastIndex == index) break;

                lastIndex++;
                lineIndex++;
            }

            return new Tuple<int, int>(lastLine, lineIndex);
        }
    }

    class VMException : Exception
    {
        public VMException()
            : base()
        { }

        public VMException(string msg) : base(msg)
        { }

        public VMException(string msg, SExprAtomic c) :
            base(String.Format("'{0}': {1}, File:{2}, Line:{3}:{4}",
                (string)c.Token.TValue, msg, c.Token.Source, c.Token.LineIndex?.Item1 + 1, c.Token.LineIndex?.Item2 + 1))
        { }

        public VMException(string msg, SToken t) :
            base(String.Format("'{0}': {1}, File:{2}, Line:{3}:{4}",
                (string)t.TValue, msg, t.Source, t.LineIndex?.Item1 + 1, t.LineIndex?.Item2 + 1))
        { }
    }

    public static class ListExtensions
    {
        public static T Pop<T>(this List<T> lst)
        {
            if (lst.Count == 0) return default(T);

            T first = lst.First();
            lst.RemoveAt(0);

            return first;
        }
    }

    class CallsEnvironment : Dictionary<string, Func<SExprAtomic, SExprComp, SExpression>> { }

    class ExecEnvironment : Dictionary<string, SValue>
    {
        public ExecEnvironment ParentEnv;
        private bool strict = false;

        public bool ContainsKey(string key)
        {
            if (ParentEnv != null)
            {
                if (base.ContainsKey(key))
                    return true;
                else
                    return ParentEnv.ContainsKey(key);
            }
            else
            {
                return base.ContainsKey(key);
            }
        }

        public SValue this[string key]
        {
            get
            {
                if (ParentEnv != null)
                {
                    if (base.ContainsKey(key))
                        return base[key];
                    else 
                        return ParentEnv[key];
                }
                else
                    return base[key];
            }

            set
            {
                if (ParentEnv != null)
                {
                    if (base.ContainsKey(key) || !ParentEnv.ContainsKey(key))
                        base[key] = value;
                    else
                        ParentEnv[key] = value;
                }
                else
                {
                    if (!base.ContainsKey(key) && strict) throw new VMException(key + ": strict mode");

                    base[key] = value;

                    if (key == "~strict") strict = true;
                }
            }
        }

        public void NewVar(string key, SValue value)
        {
            base[key] = value;
        }
    }
}
