using System;
using ProcessingApp.Common.Src.Dto;

namespace ProcessingApp.Trade_Service_Idl.Src.Service
{
    public interface ITradeService
    {
        IObservable<MessageDTO<MessageTrade>> TradesStream();
    }
}
