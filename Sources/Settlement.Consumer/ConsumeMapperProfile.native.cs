using AutoMapper;
using Fanex.Settlement.MessageBridge.JackTime;
using Settlement.Consumer.Criteria;

namespace Settlement.Consumer;
public class ConsumeMapperProfile : Profile
{
    public ConsumeMapperProfile()
    {
        CreateMap<SoccerResult, CashOutSelCriteria>();
    }
}
