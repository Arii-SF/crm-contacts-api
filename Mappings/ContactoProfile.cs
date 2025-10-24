using AutoMapper;
using CrmContactsApi.Models;
using CrmContactsApi.DTOs;

namespace CrmContactsApi.Mappings
{
    public class ContactoProfile : Profile
    {
        public ContactoProfile()
        {
            // Mapeo de Contacto a ContactoDto
            CreateMap<Contacto, ContactoDto>();

            // Mapeo de CreateContactoRequest a Contacto
            CreateMap<CreateContactoRequest, Contacto>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.FechaCreacion, opt => opt.MapFrom(src => DateTime.Now))
                .ForMember(dest => dest.FechaActualizacion, opt => opt.MapFrom(src => DateTime.Now))
                .ForMember(dest => dest.UsuarioActualizacion, opt => opt.Ignore())
                .ForMember(dest => dest.Activo, opt => opt.MapFrom(src => true));

            // Mapeo de UpdateContactoRequest a Contacto
            CreateMap<UpdateContactoRequest, Contacto>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore())
                .ForMember(dest => dest.FechaActualizacion, opt => opt.MapFrom(src => DateTime.Now))
                .ForMember(dest => dest.UsuarioCreacion, opt => opt.Ignore());

            CreateMap<CalificacionContacto, CalificacionDto>();
            CreateMap<CreateCalificacionRequest, CalificacionContacto>();
            CreateMap<Contacto, ContactoConCalificacionDto>()
                .IncludeBase<Contacto, ContactoDto>();
        }
    }
}