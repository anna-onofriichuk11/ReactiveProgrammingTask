using System;
using System.Collections.Generic;

namespace ProcessingApp.Crypto_Service_Idl.Src.Service
{
    public interface ICryptoService
    {
        IObservable<Dictionary<string, object>> EventsStream();
    }
}
