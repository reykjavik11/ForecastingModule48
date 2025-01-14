using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ForecastingModule.exception
{
    internal class DBException : Exception
    {
        public DBException(string message) : base("DB error: " + message)
        {
        }
    }
}
