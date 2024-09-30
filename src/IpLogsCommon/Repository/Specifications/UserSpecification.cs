using IpLogsCommon.Repository.Entities;
using IpLogsCommon.Repository.Helpers;

namespace IpLogsCommon.Repository.Specifications;

public abstract class UserSpecification
{
    public sealed class GetUserById : BaseSpecification<User>
    {
        public GetUserById(long id, bool asNoTracking)
        {
            AsNoTracking = asNoTracking;
            AddCriteria(i => i.Id == id);
        }
    }

    public sealed class GetUserIpsById : BaseSpecification<UserIP, string>
    {
        public GetUserIpsById(long id, bool asNoTracking)
        {
            AsNoTracking = asNoTracking;
            AddCriteria(i => i.UserId == id);
            Selector = s => s.IPAddress;
            Distinct = true;
        }
    }

    public sealed class FindUsersByIpPart : BaseSpecification<UserIP, long>
    {
        public FindUsersByIpPart(string ipPart, bool asNoTracking)
        {
            AsNoTracking = asNoTracking;
            AddCriteria(i => i.IPAddress.StartsWith(ipPart));
            Selector = s => s.UserId;
            Distinct = true;
        }
    }
}