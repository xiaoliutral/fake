using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Fake.AspNetCore.ApplicationServiceConventions;

public interface IApplicationServiceActionHelper
{
    string GetRoute(ActionModel action, string httpVerb, string rootPath);

    string GetHttpVerb(ActionModel action);
}