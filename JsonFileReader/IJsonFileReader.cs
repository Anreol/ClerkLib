using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClerkLib.FileReader
{
    internal interface IJsonFileReader<T> where T : class
    {
        public T Data { get; }
    }

}
