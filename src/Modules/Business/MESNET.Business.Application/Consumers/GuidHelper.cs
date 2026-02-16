using System.Security.Cryptography;
using System.Text;

namespace MESNET.Business.Application.Consumers;

internal static class GuidHelper
{
    internal static Guid Combine(Guid parentId, string key)
    {
        var input = $"{parentId}:{key}";
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(input));
        return new Guid(hash);
    }
}
