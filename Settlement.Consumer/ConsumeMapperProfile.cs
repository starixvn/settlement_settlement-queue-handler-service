using AutoMapper;
using Fanex.Settlement.MessageBridge.JackTime;
using Settlement.Consumer.Criteria;
using Settlement.Consumer.Models;

namespace Settlement.Consumer;
public class ConsumeMapperProfile : Profile
{
    public ConsumeMapperProfile()
    {
        CreateMap<SoccerResult, CashOutSelCriteria>();
        CreateMap<MatchDetail, NotificationModel>();
        CreateMap<SoccerResult, SoccerGetTicketInfoCriteria>();
    }
}
