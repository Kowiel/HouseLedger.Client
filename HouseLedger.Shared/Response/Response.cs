using System;
using System.Collections.Generic;
using System.Text;

namespace HouseLedger.Shared.Response
{
    public class ServiceResponse<T>
    {
        public T Data { get; set; }
        public bool Success { get; set; } = true;
        public string Message { get; set; } = null;
    
    }
}
