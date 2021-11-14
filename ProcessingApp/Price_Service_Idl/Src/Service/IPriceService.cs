using System;
using ProcessingApp.Common.Src.Dto;

namespace ProcessingApp.Price_Service_Idl.Src.Service
{
    public interface IPriceService
    {
        IObservable<MessageDTO<float>> PricesStream(IObservable<long> intervalPreferencesStream);
    }
}
