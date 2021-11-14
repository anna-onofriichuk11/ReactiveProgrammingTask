using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ProcessingApp.Crypto_Service_Idl.Src.Service;
using ProcessingApp.Crypto_Service.Src.Service.External.Utils;

namespace ProcessingApp.Crypto_Service.Src.Service.External
{
    public class CryptoCompareService : ICryptoService
    {
        public static readonly int CACHE_SIZE = 3;

        private readonly IObservable<Dictionary<string, object>> _connectedClient;

        public CryptoCompareService(ILogger<CryptoCompareClient> logger, IEnumerable<IMessageUnpacker> messageUnpackers)
        {
            _connectedClient = new CryptoCompareClient(logger)
                    .Connect(
                        new List<string> { "5~CCCAGG~BTC~USD", "0~Coinbase~BTC~USD", "0~Cexio~BTC~USD" }.ToObservable(),
                        messageUnpackers.ToList()
                    )
                    .Let(ProvideResilience)
                    .Let(ProvideCaching);
        }

        public IObservable<Dictionary<string, object>> EventsStream()
        {
            return _connectedClient;
        }

        // TODO: implement resilience such as retry with delay
        private static IObservable<T> ProvideResilience<T>(IObservable<T> input)
        {
            return Observable.Never<T>();
        }

        // TODO: implement caching of 3 last elements & multi subscribers support
        private static IObservable<T> ProvideCaching<T>(IObservable<T> input)
        {
            return Observable.Never<T>();
        }
    }
}
