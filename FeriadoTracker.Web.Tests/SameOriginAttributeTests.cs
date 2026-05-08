using FeriadoTracker.Web.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace FeriadoTracker.Web.Tests;

public class SameOriginAttributeTests
{
    private ActionExecutingContext CreateContext(string host, string? origin, string? referer)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString(host);

        if (origin != null) httpContext.Request.Headers.Origin = origin;
        if (referer != null) httpContext.Request.Headers.Referer = referer;

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object?>(), controller: null!);
    }

    [Fact]
    public void OnActionExecuting_PermiteQuandoOriginIgualAoHost()
    {
        var context = CreateContext("localhost:5000", "http://localhost:5000", null);
        var filter = new SameOriginAttribute();

        filter.OnActionExecuting(context);

        Assert.Null(context.Result); // Não bloqueou
    }

    [Fact]
    public void OnActionExecuting_BloqueiaQuandoOriginDiferente()
    {
        var context = CreateContext("localhost:5000", "http://malicioso.com", null);
        var filter = new SameOriginAttribute();

        filter.OnActionExecuting(context);

        Assert.IsType<StatusCodeResult>(context.Result);
        Assert.Equal(403, ((StatusCodeResult)context.Result).StatusCode);
    }

    [Fact]
    public void OnActionExecuting_PermiteRefererQuandoOriginAusente()
    {
        var context = CreateContext("localhost:5000", null, "https://localhost:5000/pagina");
        var filter = new SameOriginAttribute();

        filter.OnActionExecuting(context);

        Assert.Null(context.Result); // Não bloqueou
    }

    [Fact]
    public void OnActionExecuting_BloqueiaRefererQuandoDiferente()
    {
        var context = CreateContext("localhost:5000", null, "https://malicioso.com/pagina");
        var filter = new SameOriginAttribute();

        filter.OnActionExecuting(context);

        Assert.IsType<StatusCodeResult>(context.Result);
        Assert.Equal(403, ((StatusCodeResult)context.Result).StatusCode);
    }

    [Fact]
    public void OnActionExecuting_BloqueiaQuandoSemOriginNemReferer()
    {
        var context = CreateContext("localhost:5000", null, null);
        var filter = new SameOriginAttribute();

        filter.OnActionExecuting(context);

        Assert.IsType<StatusCodeResult>(context.Result);
        Assert.Equal(403, ((StatusCodeResult)context.Result).StatusCode);
    }
}
