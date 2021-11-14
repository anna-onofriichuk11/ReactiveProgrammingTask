using System;
using System.Collections.Generic;
using ProcessingApp.Trade_Service.Src.Domain;

namespace ProcessingApp.Trade_Service.Src.Repository
{
    public interface ITradeRepository
    {
        IObservable<int> SaveAll(List<Trade> input);
    }
}
