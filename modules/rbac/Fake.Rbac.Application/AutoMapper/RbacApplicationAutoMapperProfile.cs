using AutoMapper;
using Fake.Rbac.Application.Dtos.Menu;
using Fake.Rbac.Application.Dtos.Permission;
using Fake.Rbac.Application.Dtos.Role;
using Fake.Rbac.Application.Dtos.User;
using Fake.Rbac.Domain.MenuAggregate;
using Fake.Rbac.Domain.Permissions;
using Fake.Rbac.Domain.RoleAggregate;
using Fake.Rbac.Domain.UserAggregate;

namespace Fake.Rbac.Application.AutoMapper;

public class RbacApplicationAutoMapperProfile : Profile
{
    public RbacApplicationAutoMapperProfile()
    {
        // Permission mappings
        CreateMap<PermissionDefinition, PermissionDefinitionDto>();
        // User mappings
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.Roles, opt => opt.Ignore()); // 手动处理
        
        CreateMap<UserDto, UserInfoDto>()
            .ForMember(dest => dest.Permissions, opt => opt.Ignore())
            .ForMember(dest => dest.Menus, opt => opt.Ignore());
        
        CreateMap<User, UserSimpleDto>();
        
        CreateMap<UserCreateDto, User>()
            .ConstructUsing((src, ctx) => new User(src.Name, src.Account, src.Password, src.Email, src.Avatar));

        // Role mappings
        CreateMap<Role, RoleDto>()
            .ForMember(dest => dest.Permissions, 
                opt => opt.MapFrom(src => src.Permissions.Select(p => p.PermissionCode).ToList()));
        
        CreateMap<Role, RoleSimpleDto>();
        
        CreateMap<RoleCreateDto, Role>()
            .ConstructUsing((src, ctx) => new Role(src.Name, src.Code));

        // Menu mappings
        CreateMap<Menu, MenuDto>();
        
        CreateMap<Menu, MenuTreeDto>()
            .ForMember(dest => dest.Children, opt => opt.Ignore()); // 手动处理
        
        CreateMap<MenuCreateDto, Menu>()
            .ConstructUsing((src, ctx) => new Menu(
                src.PId ?? Guid.Empty, 
                src.Name, 
                src.Type, 
                src.PermissionCode, 
                src.Icon, 
                src.Route, 
                src.Component, 
                src.Order, 
                src.IsHidden, 
                src.IsCached, 
                src.Description));
    }
}

