using AndroidNas.Core;
namespace AndroidNas.Server.Endpoints;

public static class DlnaEndpoints
{
    public static string GetDescriptionXml()
    {
        var alias = System.Security.SecurityElement.Escape(AppSettings.DeviceAlias) ?? "Android NAS";
        var uuid = AppSettings.Fingerprint;
        return "<?xml version=\"1.0\"?><root xmlns=\"urn:schemas-upnp-org:device-1-0\">" +
               "<specVersion><major>1</major><minor>0</minor></specVersion><device>" +
               "<deviceType>urn:schemas-upnp-org:device:MediaServer:1</deviceType>" +
               "<friendlyName>" + alias + "</friendlyName><manufacturer>AndroidNAS</manufacturer>" +
               "<modelName>Android NAS</modelName><UDN>uuid:" + uuid + "</UDN></device></root>";
    }
}
