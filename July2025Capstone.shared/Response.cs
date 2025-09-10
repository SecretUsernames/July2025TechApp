using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace July2025Capstone.Shared
{
    public class Response
    {
        public bool Succeeded { get; set; }
        public string Message { get; set; }
        public Dictionary<string, string[]> Errors { get; set; }

        public Response()
        {
            Errors = new Dictionary<string, string[]>();
        }

        public Response(bool succeeded, string message)
        {
            Succeeded = succeeded;
            Message = message;
            Errors = new Dictionary<string, string[]>();
        }

        public Response(string message)
        {
            Succeeded = false;
            Message = message;
            Errors = new Dictionary<string, string[]>();
        }
    }
}
