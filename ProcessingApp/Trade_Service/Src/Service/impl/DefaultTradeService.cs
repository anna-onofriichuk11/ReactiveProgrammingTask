using ProcessingApp.Common.Src.Service.Utils;
using ProcessingApp.Trade_Service.Src.Domain.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ProcessingApp.Common.Src.Dto;
using ProcessingApp.Crypto_Service_Idl.Src.Service;
using ProcessingApp.Trade_Service_Idl.Src.Service;
using ProcessingApp.Trade_Service.Src.Domain;
using ProcessingApp.Trade_Service.Src.Repository;

namespace ProcessingApp.Trade_Service.Src.Service.impl
{
    public class DefaultTradeService : ITradeService
    {
        private readonly ILogger<DefaultTradeService> _logger;
        private readonly ICryptoService _cryptoService;
        private readonly IEnumerable<ITradeRepository> _tradeRepositories;

        public DefaultTradeService(
            ILogger<DefaultTradeService> logger,
            ICryptoService service,
            IEnumerable<ITradeRepository> tradeRepositories)
        {
            _logger = logger;
            _cryptoService = service ?? throw new ArgumentNullException(nameof(service));
            _tradeRepositories = tradeRepositories ?? throw new ArgumentNullException(nameof(tradeRepositories));
        }

        public IObservable<MessageDTO<MessageTrade>> TradesStream()
        {
            return _cryptoService.EventsStream()
                .Let(FilterAndMapTradingEvents)
                .Let(trades =>
                {
                    trades.Let(MapToDomainTrade)
                        .Let(f => ResilientlyStoreByBatchesToAllRepositories(f, _tradeRepositories.First(),
                            _tradeRepositories.Last()))
                        .Subscribe(new Subject<int>());

                    return trades;
                });
        }


        private IObservable<MessageDTO<MessageTrade>> FilterAndMapTradingEvents(
            IObservable<Dictionary<string, object>> input)
        {
            // TODO: Add implementation to produce trading events
            return input.Where(message => MessageMapper.IsTradeMessageType(message)).Select(message => MessageMapper.MapToTradeMessage(message));
        }

        private IObservable<Trade> MapToDomainTrade(IObservable<MessageDTO<MessageTrade>> input)
        {
            // TODO: Add implementation to mapping to com.example.part_10.domain.Trade
            return input.Select(message => DomainMapper.MapToDomain(message));
        }

        private static IObservable<int> ResilientlyStoreByBatchesToAllRepositories(
            IObservable<Trade> input,
            ITradeRepository tradeRepository1,
            ITradeRepository tradeRepository2)
        {
            var bhSubject = new BehaviorSubject<object>(new object());
            bhSubject.OnNext(new object());

            IObservable<(long, object)> bounds = Observable.Interval(TimeSpan.FromSeconds(1)).Zip(bhSubject, (n, o) => (n, o));

            return input
                .Buffer(bounds)
                .SelectMany(trades =>
                {
                    var tradesList = trades.ToList();

                    return Save(tradeRepository1, tradesList)
                        .Merge(Save(tradeRepository2, tradesList))
                        .Do(onNext: x => { },
                            onCompleted: () => bhSubject.OnNext(new object())
                            );
                });

            static IObservable<int> Save
                (ITradeRepository tradeRepository, List<Trade> tradesList)
                => tradeRepository
                            .SaveAll(tradesList)
                            .Timeout(TimeSpan.FromSeconds(1)).Retry(100);
        }
    }
}