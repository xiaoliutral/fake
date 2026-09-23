using AutoMapper;
using Fake.FileManagement.Application.Dtos;
using Fake.FileManagement.Domain.FileAggregate;

namespace Fake.FileManagement.Application.AutoMapper;

public class FileManagementAutoMapperProfile : Profile
{
    public FileManagementAutoMapperProfile()
    {
        CreateMap<StoredFile, FileDto>()
            .ForMember(d => d.Url, opt => opt.Ignore());
    }
}
