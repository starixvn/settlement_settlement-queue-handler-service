using AutoMapper;
using Settlement.Shared.Models;

namespace Settlement.Shared;
public class MapperProfile : Profile
{
    public MapperProfile()
    {
        CreateMap<MatchDetail, NotificationModel>();
    }
}
