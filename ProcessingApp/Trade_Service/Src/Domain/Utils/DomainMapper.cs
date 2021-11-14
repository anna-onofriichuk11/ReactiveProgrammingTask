using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using ProcessingApp.Common.Src.Dto;

namespace ProcessingApp.Trade_Service.Src.Domain.Utils
{
    public static class DomainMapper
    {
		public static Trade MapToDomain(MessageDTO<MessageTrade> tradeMessageDto)
		{
			var trade = new Trade
			{
				Id = Guid.NewGuid().ToString(),
				Price = tradeMessageDto.Data.Price,
				Amount = tradeMessageDto.Data.Amount,
				Currency = tradeMessageDto.Currency,
				Market = tradeMessageDto.Market,
				Timestamp = tradeMessageDto.Timestamp
			};

			return trade;
		}

		public static BsonDocument MapToMongoDocument(Trade trade)
		{
			var dictionary = new Dictionary<string, object>();
			foreach (var propertyInfo in trade.GetType().GetProperties())
					dictionary[propertyInfo.Name] = propertyInfo.GetValue(trade);

			return new BsonDocument(dictionary);
		}
	}
}
