using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eugine
{
    class Program
    {
        static void Main(string[] args)
        {
            var vm = new EugineVM();
            vm.ExecuteFile("./tests/test.lisp", vm.DefaultEnvironment);
        }
    }
}
