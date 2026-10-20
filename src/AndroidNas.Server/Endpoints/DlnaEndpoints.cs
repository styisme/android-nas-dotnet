using AndroidNas.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Text;

namespace AndroidNas.Server.Endpoints;

public static class DlnaEndpoints
{
    public static IEndpointRouteBuilder MapDlnaEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/dlna/description.xml", (HttpContext ctx) =>
        {
            var alias = System.Security.SecurityElement.Escape(AppSettings.DeviceAlias) ?? "Android NAS";
            var uuid = AppSettings.Fingerprint;

            var xml =
                "<?xml version=\"1.0\"?>" +
                "<root xmlns=\"urn:schemas-upnp-org:device-1-0\">" +
                "<specVersion><major>1</major><minor>0</minor></specVersion>" +
                "<device>" +
                "<deviceType>urn:schemas-upnp-org:device:MediaServer:1</deviceType>" +
                "<friendlyName>" + alias + "</friendlyName>" +
                "<manufacturer>AndroidNAS</manufacturer>" +
                "<modelName>Android NAS</modelName>" +
                "<modelNumber>4.0</modelNumber>" +
                "<UDN>uuid:" + uuid + "</UDN>" +
                "<serviceList>" +
                "<service>" +
                "<serviceType>urn:schemas-upnp-org:service:ContentDirectory:1</serviceType>" +
                "<serviceId>urn:upnp-org:serviceId:ContentDirectory</serviceId>" +
                "<controlURL>/dlna/control</controlURL>" +
                "<eventSubURL>/dlna/event</eventSubURL>" +
                "<SCPDURL>/dlna/content-directory.xml</SCPDURL>" +
                "</service>" +
                "</serviceList>" +
                "</device>" +
                "</root>";

            return Results.Text(xml, "application/xml; charset=utf-8");
        });

        app.MapGet("/dlna/content-directory.xml", () =>
        {
            const string scpd =
                "<?xml version=\"1.0\"?>" +
                "<scpd xmlns=\"urn:schemas-upnp-org:service-1-0\">" +
                "<specVersion><major>1</major><minor>0</minor></specVersion>" +
                "<actionList><action>" +
                "<name>Browse</name>" +
                "<argumentList>" +
                "<argument><name>ObjectID</name><direction>in</direction><relatedStateVariable>A_ARG_TYPE_ObjectID</relatedStateVariable></argument>" +
                "<argument><name>BrowseFlag</name><direction>in</direction><relatedStateVariable>A_ARG_TYPE_BrowseFlag</relatedStateVariable></argument>" +
                "<argument><name>Filter</name><direction>in</direction><relatedStateVariable>A_ARG_TYPE_Filter</relatedStateVariable></argument>" +
                "<argument><name>StartingIndex</name><direction>in</direction><relatedStateVariable>A_ARG_TYPE_Index</relatedStateVariable></argument>" +
                "<argument><name>RequestedCount</name><direction>in</direction><relatedStateVariable>A_ARG_TYPE_Count</relatedStateVariable></argument>" +
                "<argument><name>SortCriteria</name><direction>in</direction><relatedStateVariable>A_ARG_TYPE_SortCriteria</relatedStateVariable></argument>" +
                "<argument><name>Result</name><direction>out</direction><relatedStateVariable>A_ARG_TYPE_Result</relatedStateVariable></argument>" +
                "<argument><name>NumberReturned</name><direction>out</direction><relatedStateVariable>A_ARG_TYPE_Count</relatedStateVariable></argument>" +
                "<argument><name>TotalMatches</name><direction>out</direction><relatedStateVariable>A_ARG_TYPE_Count</relatedStateVariable></argument>" +
                "<argument><name>UpdateID</name><direction>out</direction><relatedStateVariable>A_ARG_TYPE_UpdateID</relatedStateVariable></argument>" +
                "</argumentList>" +
                "</action></actionList>" +
                "<serviceStateTable>" +
                "<stateVariable sendEvents=\"no\"><name>A_ARG_TYPE_ObjectID</name><dataType>string</dataType></stateVariable>" +
                "<stateVariable sendEvents=\"no\"><name>A_ARG_TYPE_BrowseFlag</name><dataType>string</dataType></stateVariable>" +
                "<stateVariable sendEvents=\"no\"><name>A_ARG_TYPE_Filter</name><dataType>string</dataType></stateVariable>" +
                "<stateVariable sendEvents=\"no\"><name>A_ARG_TYPE_Index</name><dataType>ui4</dataType></stateVariable>" +
                "<stateVariable sendEvents=\"no\"><name>A_ARG_TYPE_Count</name><dataType>ui4</dataType></stateVariable>" +
                "<stateVariable sendEvents=\"no\"><name>A_ARG_TYPE_SortCriteria</name><dataType>string</dataType></stateVariable>" +
                "<stateVariable sendEvents=\"no\"><name>A_ARG_TYPE_Result</name><dataType>string</dataType></stateVariable>" +
                "<stateVariable sendEvents=\"no\"><name>A_ARG_TYPE_UpdateID</name><dataType>ui4</dataType></stateVariable>" +
                "</serviceStateTable>" +
                "</scpd>";
            return Results.Text(scpd, "application/xml; charset=utf-8");
        });

        return app;
    }
}
